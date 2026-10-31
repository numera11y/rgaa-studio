'use strict';

const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');

const root = __dirname;
const html = fs.readFileSync(path.join(root, 'index.html'), 'utf8');
const js = fs.readFileSync(path.join(root, 'app.js'), 'utf8');
const css = fs.readFileSync(path.join(root, 'styles.css'), 'utf8');
const ids = [...html.matchAll(/\bid="([^"]+)"/g)].map(match => match[1]);
const uniqueIds = new Set(ids);
assert.equal(uniqueIds.size, ids.length, 'HTML element IDs must be unique.');
assert.match(html, /<html lang="fr">/);
assert.match(html, /Mes audits/);
assert.match(html, /Pages à examiner/);
assert.match(html, /Pointer un élément/);
assert.match(html, /Règles de la page/);
assert.match(html, /Capture simulée/);
assert.match(js, /exemple\.test/);
assert.match(js, /Non applicable/);
assert.match(js, /Non vérifiable/);
assert.match(html, /Aucune image réelle de navigateur n’est capturée/);
assert.doesNotMatch(js, /innerHTML/);
assert.doesNotMatch(html, /\son(?:click|load|error)\s*=/i);
assert.match(css, /:focus-visible/);
assert.match(css, /prefers-reduced-motion/);

const referencedIds = [...js.matchAll(/byId\('([^']+)'\)/g)].map(match => match[1]);
const missing = [...new Set(referencedIds)].filter(id => !uniqueIds.has(id));
assert.deepEqual(missing, [], `JavaScript references missing HTML IDs: ${missing.join(', ')}`);

console.log(`PASS: ${uniqueIds.size} unique UI IDs; workflow, French labels, synthetic limits, safe text rendering, focus and reduced-motion checks.`);
