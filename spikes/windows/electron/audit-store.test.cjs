'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { createAuditStore } = require('./audit-store.cjs');

function setup() {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'rgaa-audits-'));
  return { root, store: createAuditStore(path.join(root, 'audits')), cleanup: () => fs.rmSync(root, { recursive: true, force: true }) };
}

test('create, list, open and reopen persisted schema', t => {
  const { store, cleanup } = setup(); t.after(cleanup);
  const dossier = store.create('Démonstration A');
  assert.equal(store.list().length, 1);
  assert.deepEqual(store.open(dossier.id), dossier);
  assert.equal(store.open(dossier.id).schemaVersion, 1);
});

test('rejects malformed records and unknown schema properties', t => {
  const { root, store, cleanup } = setup(); t.after(cleanup);
  const dossier = store.create('Schéma strict');
  const file = path.join(root, 'audits', `${dossier.id}.json`);
  const tampered = { ...dossier, context: { ...dossier.context, unexpected: 'value' } };
  fs.writeFileSync(file, JSON.stringify(tampered));
  assert.throws(() => store.open(dossier.id), /Dossier d’audit invalide/);
  assert.equal(store.list().length, 0);
});

test('rejects non-canonical timestamps and filename/content ID mismatches', t => {
  const { root, store, cleanup } = setup(); t.after(cleanup);
  const dossier = store.create('Horodatage strict');
  const dossierPath = path.join(root, 'audits', `${dossier.id}.json`);
  fs.writeFileSync(dossierPath, JSON.stringify({ ...dossier, updatedAt: 'October 29, 2026' }));
  assert.throws(() => store.open(dossier.id), /Dossier d’audit invalide/);

  const otherId = '123e4567-e89b-42d3-a456-426614174000';
  fs.writeFileSync(dossierPath, JSON.stringify({ ...dossier, id: otherId }));
  assert.equal(store.list().length, 0);
});

test('A/B dossier state remains isolated', t => {
  const { store, cleanup } = setup(); t.after(cleanup);
  const a = store.create('A'); const b = store.create('B');
  store.saveResult(a.id, 'Conforme', '', 'fixture://synthetic-page');
  store.setObservation(a.id, 'confirmed');
  assert.equal(store.open(b.id).humanResult.outcome, null);
  assert.equal(store.open(b.id).context.pageUrl, '');
  assert.equal(store.open(b.id).observation.disposition, 'unreviewed');
});

test('rejects traversal, invalid IDs/outcomes, and missing required justification', t => {
  const { store, cleanup } = setup(); t.after(cleanup);
  const dossier = store.create('Test');
  assert.throws(() => store.open('../outside'), /Identifiant de dossier invalide/);
  assert.throws(() => store.open('bad-id'), /Identifiant de dossier invalide/);
  assert.throws(() => store.saveResult(dossier.id, 'Maybe', '', ''), /Résultat invalide/);
  assert.throws(() => store.saveResult(dossier.id, 'Non applicable', '  ', ''), /justification/);
  assert.throws(() => store.saveResult(dossier.id, 'Non vérifiable', '', ''), /justification/);
  assert.throws(() => store.saveResult(dossier.id, 'Conforme', '', 'https://real.example/private?token=example'), /URL de page invalide/);
  assert.throws(() => store.complete(dossier.id), /décision humaine/);
});

test('automated observation, human result, and explicit review completion are independent', t => {
  const { store, cleanup } = setup(); t.after(cleanup);
  const dossier = store.create('Séparation');
  store.setObservation(dossier.id, 'confirmed');
  assert.equal(store.open(dossier.id).review.state, 'not-started');
  assert.equal(store.open(dossier.id).humanResult.outcome, null);
  store.saveResult(dossier.id, 'Non applicable', 'Motif synthétique', 'fixture://synthetic-page');
  assert.equal(store.open(dossier.id).review.state, 'in-progress');
  store.complete(dossier.id);
  assert.equal(store.open(dossier.id).review.state, 'complete');
  store.saveResult(dossier.id, 'Conforme', '', 'fixture://synthetic-page');
  assert.equal(store.open(dossier.id).review.state, 'in-progress');
});

test('changing the associated page URL invalidates a completed review', t => {
  const { store, cleanup } = setup(); t.after(cleanup);
  const dossier = store.create('Changement de page');
  store.saveResult(dossier.id, 'Conforme', '', 'fixture://synthetic-page');
  store.complete(dossier.id);
  store.saveResult(dossier.id, 'Conforme', '', '');
  assert.equal(store.open(dossier.id).review.state, 'in-progress');
  assert.equal(store.open(dossier.id).context.pageUrl, '');
});
