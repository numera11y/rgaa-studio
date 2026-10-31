'use strict';

const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { app, BaseWindow, WebContentsView, ipcMain, session } = require('electron');
const { createAuditStore } = require('./audit-store.cjs');

const ROOT = __dirname;
const FIXTURE_URL = pathToFileURL(path.join(ROOT, 'fixture.html')).href;
const AUDIT_FILE = path.join(ROOT, 'audit.html');
const PRELOAD_FILE = path.join(ROOT, 'preload.cjs');
let win;
let targetView;
let auditView;
let lastSuccessfulUrl = '';
let requestedUrl = FIXTURE_URL;
let auditStore;

app.setPath('userData', path.join(ROOT, 'user-data'));
const hasInstanceLock = app.requestSingleInstanceLock();
if (!hasInstanceLock) app.quit();

function isHttpUrl(value) {
  try {
    const url = new URL(value);
    return ['http:', 'https:'].includes(url.protocol) && !url.username && !url.password && String(value).length <= 2048;
  } catch { return false; }
}

function navigationUrl(detailsOrUrl) {
  return typeof detailsOrUrl === 'string' ? detailsOrUrl : detailsOrUrl?.url;
}

function trustedAuditSender(event) {
  return !!auditView && event.sender === auditView.webContents &&
    event.senderFrame === event.sender.mainFrame &&
    event.senderFrame.url === pathToFileURL(AUDIT_FILE).href;
}

function guarded(channel, handler) {
  ipcMain.handle(channel, async (event, ...args) => {
    if (!trustedAuditSender(event)) throw new Error('Appel interne non autorisé.');
    return handler(...args);
  });
}

function setStatus(message) {
  if (!auditView || auditView.webContents.isDestroyed()) return;
  auditView.webContents.send('status', { lastSuccessfulUrl, requestedUrl, message });
}

async function navigate(raw) {
  if (String(raw).trim() === FIXTURE_URL) {
    requestedUrl = FIXTURE_URL;
    setStatus('Chargement de la page locale de démonstration…');
    try { await targetView.webContents.loadURL(FIXTURE_URL); }
    catch (error) { setStatus(`Échec du chargement : ${error.message}. Vous pouvez essayer une URL http(s).`); throw new Error('Échec du rechargement ; le panneau d’audit reste disponible.'); }
    return FIXTURE_URL;
  }
  if (typeof raw !== 'string' || raw.trim().length > 2048) throw new Error('L’URL doit contenir au maximum 2048 caractères.');
  let url;
  try { url = new URL(raw.trim()); } catch { throw new Error('Saisissez une URL http(s) valide.'); }
  if (!isHttpUrl(url.href)) throw new Error('Seules les URL http(s) sans identifiants intégrés sont autorisées.');
  requestedUrl = url.href;
  setStatus('Chargement de la page cible…');
  try { await targetView.webContents.loadURL(requestedUrl); }
  catch (error) { setStatus(`Échec de navigation : ${error.message}. Vous pouvez essayer une autre URL ou recharger.`); throw new Error('Échec de navigation ; le panneau d’audit reste disponible.'); }
  return requestedUrl;
}

if (hasInstanceLock) app.whenReady().then(() => {
  auditStore = createAuditStore(path.join(ROOT, 'user-data', 'audits'));
  // Keep target browsing state separate from the local audit renderer.
  const targetSession = session.fromPartition('spike-target-memory');
  targetSession.setPermissionRequestHandler((_wc, _permission, callback) => callback(false));
  targetSession.setPermissionCheckHandler(() => false);
  targetSession.on('will-download', (event) => event.preventDefault());

  win = new BaseWindow({ width: 1280, height: 820, minWidth: 900, minHeight: 600, title: 'Prototype d’architecture RGAA (Windows)' });
  targetView = new WebContentsView({ webPreferences: { session: targetSession, contextIsolation: true, nodeIntegration: false, sandbox: true } });
  auditView = new WebContentsView({ webPreferences: { preload: PRELOAD_FILE, contextIsolation: true, nodeIntegration: false, sandbox: true } });
  win.contentView.addChildView(targetView);
  win.contentView.addChildView(auditView);

  const resize = () => {
    const { width, height } = win.getContentBounds();
    const panelWidth = Math.max(390, Math.min(470, Math.round(width * 0.36)));
    targetView.setBounds({ x: 0, y: 0, width: width - panelWidth, height });
    auditView.setBounds({ x: width - panelWidth, y: 0, width: panelWidth, height });
  };
  win.on('resize', resize);
  resize();

  const blockTargetNavigation = (event, detailsOrUrl, legacyIsMainFrame) => {
    const url = navigationUrl(detailsOrUrl);
    const isMainFrame = typeof detailsOrUrl === 'object' && detailsOrUrl !== null
      ? detailsOrUrl.isMainFrame !== false
      : legacyIsMainFrame !== false;
    if (url === FIXTURE_URL || isHttpUrl(url)) {
      if (isMainFrame) {
        requestedUrl = url;
        setStatus('Navigation de la page en cours…');
      }
      return;
    }
    event.preventDefault();
    setStatus('Navigation bloquée : seuls les schémas http(s) sont autorisés.');
  };
  targetView.webContents.on('will-navigate', blockTargetNavigation);
  targetView.webContents.on('will-frame-navigate', blockTargetNavigation);
  targetView.webContents.on('will-redirect', blockTargetNavigation);
  targetView.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  auditView.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  targetView.webContents.on('will-download', event => event.preventDefault());
  const commitNavigation = url => {
    lastSuccessfulUrl = url;
    requestedUrl = url;
    setStatus('Page cible chargée.');
  };
  targetView.webContents.on('did-navigate', (_event, url) => commitNavigation(url));
  targetView.webContents.on('did-navigate-in-page', (_event, url, isMainFrame) => {
    if (isMainFrame) commitNavigation(url);
  });
  targetView.webContents.on('did-fail-load', (_event, code, description, validatedURL, isMainFrame) => {
    if (isMainFrame && code !== -3 && validatedURL === requestedUrl) {
      setStatus(`Échec de chargement de ${requestedUrl} (${code}) : ${description}. Dernière URL chargée avec succès : ${lastSuccessfulUrl || 'aucune'}. Saisissez une autre URL ou réessayez.`);
    }
  });

  const blockAuditNavigation = (event, detailsOrUrl) => {
    const url = navigationUrl(detailsOrUrl);
    if (url === pathToFileURL(AUDIT_FILE).href) return;
    event.preventDefault();
  };
  auditView.webContents.on('will-navigate', blockAuditNavigation);
  auditView.webContents.on('will-frame-navigate', blockAuditNavigation);
  auditView.webContents.on('will-redirect', blockAuditNavigation);

  for (const wc of [targetView.webContents, auditView.webContents]) {
    wc.on('before-input-event', (event, input) => {
      if (input.type === 'keyDown' && input.control && input.shift && input.code === 'KeyA') {
        event.preventDefault();
        if (wc === targetView.webContents) auditView.webContents.focus();
        else targetView.webContents.focus();
      }
    });
  }

  guarded('navigate', navigate);
  guarded('inspect', async () => {
    if (!lastSuccessfulUrl || (!isHttpUrl(lastSuccessfulUrl) && lastSuccessfulUrl !== FIXTURE_URL)) throw new Error('Aucune page autorisée chargée avec succès.');
    const data = await targetView.webContents.executeJavaScript(`(() => {
      const el = document.activeElement;
      const clip = (v, n = 160) => String(v || '').slice(0, n);
      return { pageTitle: clip(document.title, 120), activeElement: el ? {
        tag: clip(el.tagName, 32), role: clip(el.getAttribute('role'), 64),
        accessibleNameHint: clip(el.getAttribute('aria-label') || el.getAttribute('title') || el.innerText || el.value || '', 160),
        id: clip(el.id, 80), className: clip(typeof el.className === 'string' ? el.className : '', 120)
      } : null, elementCount: document.querySelectorAll('*').length };
    })()`, true);
    return data;
  });
  guarded('focus-target', () => { targetView.webContents.focus(); return true; });
  guarded('focus-audit', () => { auditView.webContents.focus(); return true; });
  guarded('audit-list', () => auditStore.list());
  guarded('audit-create', title => auditStore.create(title));
  guarded('audit-open', id => auditStore.open(id));
  guarded('audit-save-result', (id, outcome, justification) => {
    if (lastSuccessfulUrl !== FIXTURE_URL) throw new Error('Pour ce prototype, seule la page locale synthétique peut être associée à un dossier enregistré.');
    return auditStore.saveResult(id, outcome, justification, 'fixture://synthetic-page');
  });
  guarded('audit-observation', (id, disposition) => auditStore.setObservation(id, disposition));
  guarded('audit-complete', id => auditStore.complete(id));

  auditView.webContents.loadFile(AUDIT_FILE).then(() => {
    setStatus('Page locale prête. Aucune page n’a encore été chargée avec succès.');
  }).catch(error => setStatus(`Échec du chargement du panneau : ${error.message}`));
  targetView.webContents.loadURL(FIXTURE_URL).catch(error => setStatus(`Échec du chargement de la page de démonstration : ${error.message}`));
  win.on('closed', () => { targetView.webContents.close(); auditView.webContents.close(); });
});

app.on('window-all-closed', () => app.quit());
