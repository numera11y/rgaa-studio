'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const http = require('node:http');
const net = require('node:net');
const os = require('node:os');
const path = require('node:path');
const { spawn, execFileSync } = require('node:child_process');

const ROOT = __dirname;
const EDGE = process.env.EDGE_PATH || 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const MIME = { '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.js': 'text/javascript; charset=utf-8' };

function listen(server, port = 0) {
  return new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(port, '127.0.0.1', () => { server.removeListener('error', reject); resolve(server.address().port); });
  });
}

async function freePort() {
  const server = net.createServer();
  const port = await listen(server);
  await new Promise(resolve => server.close(resolve));
  return port;
}

async function waitForJson(url, child) {
  const end = Date.now() + 15000;
  while (Date.now() < end) {
    if (child.exitCode !== null) throw new Error(`Edge exited early with code ${child.exitCode}.`);
    try { return await (await fetch(url)).json(); } catch { await new Promise(resolve => setTimeout(resolve, 150)); }
  }
  throw new Error(`Timed out waiting for ${url}`);
}

async function run() {
  assert.ok(fs.existsSync(EDGE), `Edge not found: ${EDGE}`);
  const server = http.createServer((request, response) => {
    const pathname = decodeURIComponent(new URL(request.url, 'http://127.0.0.1').pathname);
    const relative = pathname === '/' ? 'index.html' : pathname.slice(1);
    const file = path.resolve(ROOT, relative);
    if (!file.startsWith(`${path.resolve(ROOT)}${path.sep}`) && file !== path.join(ROOT, 'index.html')) {
      response.writeHead(403).end('Forbidden'); return;
    }
    fs.readFile(file, (error, content) => {
      if (error) { response.writeHead(404).end('Not found'); return; }
      response.writeHead(200, { 'content-type': MIME[path.extname(file)] || 'application/octet-stream' });
      response.end(content);
    });
  });

  const webPort = await listen(server);
  const debugPort = await freePort();
  const userData = fs.mkdtempSync(path.join(os.tmpdir(), 'workflow-ui-edge-'));
  const child = spawn(EDGE, [
    '--headless=new', '--disable-gpu', '--no-first-run', '--disable-extensions', '--disable-background-networking',
    '--remote-allow-origins=*', `--remote-debugging-port=${debugPort}`, `--user-data-dir=${userData}`,
    `http://127.0.0.1:${webPort}/`
  ], { stdio: 'ignore', windowsHide: true });

  let socket;
  try {
    const targets = await waitForJson(`http://127.0.0.1:${debugPort}/json/list`, child);
    const page = targets.find(target => target.type === 'page' && target.webSocketDebuggerUrl);
    assert.ok(page, 'No Edge page target found.');
    socket = new WebSocket(page.webSocketDebuggerUrl);
    await new Promise((resolve, reject) => { socket.addEventListener('open', resolve, { once: true }); socket.addEventListener('error', reject, { once: true }); });
    let sequence = 0; const pending = new Map();
    socket.addEventListener('message', event => {
      const response = JSON.parse(event.data); const handler = pending.get(response.id);
      if (handler) { pending.delete(response.id); response.error ? handler.reject(new Error(response.error.message)) : handler.resolve(response.result); }
    });
    const cdp = (method, params = {}) => new Promise((resolve, reject) => {
      const id = ++sequence; pending.set(id, { resolve, reject }); socket.send(JSON.stringify({ id, method, params }));
    });
    await cdp('Runtime.enable');
    const evaluate = async expression => {
      const result = await cdp('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
      if (result.exceptionDetails) throw new Error(result.exceptionDetails.text || 'Browser evaluation failed.');
      return result.result?.value;
    };
    const wait = () => new Promise(resolve => setTimeout(resolve, 80));
    const decideRule = async (ruleId, outcome, justification = '') => {
      await evaluate(`document.querySelector('[data-rule-id="${ruleId}"]').click()`);
      await evaluate(`(() => { const choice=document.querySelector('input[name="result"][value="${outcome}"]'); choice.checked=true; choice.dispatchEvent(new Event('change',{bubbles:true})); document.getElementById('justification').value=${JSON.stringify(justification)}; document.getElementById('save-decision').click(); })()`);
    };
    const waitFor = async (expression, predicate, label, timeout = 10000) => {
      const end = Date.now() + timeout;
      while (Date.now() < end) {
        try { const value = await evaluate(expression); if (predicate(value)) return value; } catch { /* page context is still loading */ }
        await wait();
      }
      throw new Error(`Timed out waiting for ${label}.`);
    };

    await waitFor(`document.querySelectorAll('#audit-table-body tr').length`, value => value === 1, 'the seeded resume dashboard');
    await evaluate(`document.getElementById('home-create').click()`);
    assert.equal(await evaluate(`!document.getElementById('view-setup').hidden`), true, 'Create action should open configuration.');
    await evaluate(`(() => { const url=document.getElementById('site-url'); url.value='https://exemple.test'; const ref=document.getElementById('reference'); ref.value='WCAG'; ref.dispatchEvent(new Event('change',{bubbles:true})); document.getElementById('reference-version').value='2.2'; document.getElementById('setup-form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true})); })()`);
    assert.match(await evaluate(`document.getElementById('setup-error').textContent`), /A, AA ou AAA/, 'WCAG target must be explicitly chosen.');
    await evaluate(`(() => { document.querySelector('input[name="wcag-level"][value="AA"]').checked=true; document.getElementById('setup-form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true})); })()`);
    assert.equal(await evaluate(`!document.getElementById('view-pages').hidden`), true, 'A valid setup should open page candidates.');
    assert.equal(await evaluate(`document.querySelectorAll('#candidate-list input[type="checkbox"]:not(:disabled)').length`), 4, 'Only found demo candidates should be selectable.');
    await evaluate(`(() => { for (const id of ['search','results']) { const check=document.querySelector('#candidate-list input[value="'+id+'"]'); check.checked=true; check.dispatchEvent(new Event('change',{bubbles:true})); } document.getElementById('start-audit').click(); })()`);
    assert.equal(await evaluate(`!document.getElementById('view-workspace').hidden`), true, 'Confirming pages should open the page workspace.');
    await evaluate(`document.getElementById('inspect-mode-button').click(); document.querySelector('.sample-action').click()`);
    assert.match(await evaluate(`document.getElementById('selected-element-title').textContent`), /Bouton Rechercher/, 'Pointer mode should select the demo button.');
    await evaluate(`document.querySelector('#candidate-rules .rule-item').click()`);
    assert.equal(await evaluate(`document.getElementById('rule-dialog').open`), true, 'Selecting a candidate rule should open its decision form.');
    await evaluate(`(() => { document.querySelector('input[name="result"][value="Non applicable"]').checked=true; document.querySelector('input[name="result"][value="Non applicable"]').dispatchEvent(new Event('change',{bubbles:true})); document.getElementById('justification').value='Motif synthétique'; document.getElementById('save-decision').click(); })()`);
    assert.match(await evaluate(`document.getElementById('workbench-state').textContent`), /1 décision sur 4/, 'A saved result should remain distinct from page review completion.');
    await evaluate(`document.querySelector('.sample-site input').focus(); document.getElementById('select-focused-button').click()`);
    assert.match(await evaluate(`document.getElementById('selected-element-title').textContent`), /Champ de recherche/, 'Keyboard focus path should select the focused control.');
    await decideRule('fieldLabel', 'Conforme');
    await evaluate(`document.getElementById('tab-rules').click()`);
    await decideRule('keyboardUse', 'Non vérifiable', 'Contrôle de démonstration incomplet.');
    await decideRule('headingOrder', 'Conforme');
    await evaluate(`document.getElementById('finish-page-review').click()`);
    assert.match(await evaluate(`document.getElementById('workbench-state').textContent`), /ne signifie pas que la page est conforme/, 'Review completion must not be displayed as conformity.');
    await evaluate(`document.getElementById('add-proof').click(); document.getElementById('proof-note').value='Observation de démonstration'; document.getElementById('save-proof').click()`);
    const saved = await evaluate(`JSON.parse(localStorage.getItem('rgaa-workflow-prototype-v1'))`);
    const created = saved.find(audit => audit.id !== 'demo-audit-01');
    assert.ok(created, 'New audit should be retained in local demo storage.');
    assert.equal(created.pages.length, 2, 'Only confirmed pages should be in the dossier.');
    assert.equal(created.pages[0].proofs.length, 1, 'The demo proof should attach to the selected page and element.');
    await evaluate(`document.getElementById('return-dashboard').click()`);
    await wait();
    await evaluate(`document.querySelector('#audit-table-body tr:first-child [data-resume]').click()`);
    assert.equal(await evaluate(`!document.getElementById('view-workspace').hidden`), true, 'Saved audit should reopen.');
    assert.match(await evaluate(`document.getElementById('proof-list').textContent`), /capture simulée/, 'Reopened audit should show its attached demo proof.');
    await evaluate(`document.querySelector('#proof-list .proof-item').click()`);
    assert.equal(await evaluate(`document.getElementById('proof-view-dialog').open`), true, 'Saved demo proof should be reopenable for review.');
    assert.match(await evaluate(`document.getElementById('proof-view-context').textContent`), /Recherche.*exemple\.test/, 'Reopened proof should show its page context.');
    console.log('PASS: Edge workflow smoke — resume/create, WCAG target, page confirmation, element/rule decision, demo proof persistence and reopen.');
  } finally {
    try { socket?.close(); } catch { /* test cleanup */ }
    if (child.exitCode === null) child.kill();
    if (process.platform === 'win32' && child.pid) {
      try { execFileSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], { stdio: 'ignore' }); } catch { /* process may already have exited */ }
    }
    await new Promise(resolve => server.close(resolve));
    await new Promise(resolve => setTimeout(resolve, 300));
    fs.rmSync(userData, { recursive: true, force: true, maxRetries: 5, retryDelay: 300 });
  }
}

run().catch(error => { console.error(error); process.exitCode = 1; });
