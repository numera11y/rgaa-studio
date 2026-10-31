'use strict';

const fs = require('node:fs');
const path = require('node:path');

const SCHEMA_VERSION = 1;
const OUTCOMES = ['Conforme', 'Non conforme', 'Non applicable', 'Non vérifiable'];
const OBSERVATIONS = ['unreviewed', 'confirmed', 'rejected', 'needs-follow-up'];
const REVIEWS = ['not-started', 'in-progress', 'complete'];
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const isRecord = value => value !== null && typeof value === 'object' && !Array.isArray(value);
const hasExactKeys = (value, keys) => isRecord(value) && Object.keys(value).length === keys.length && keys.every(key => Object.hasOwn(value, key));
const isTimestamp = value => typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/.test(value) &&
  !Number.isNaN(Date.parse(value)) && new Date(value).toISOString() === value;

function validPageUrl(value) {
  return value === '' || value === 'fixture://synthetic-page';
}

function createAuditStore(directory) {
  const createId = require('node:crypto').randomUUID;
  function fileFor(id) {
    if (typeof id !== 'string' || !UUID.test(id)) throw new Error('Identifiant de dossier invalide.');
    return path.join(directory, `${id}.json`);
  }
  function validate(dossier) {
    if (!hasExactKeys(dossier, ['schemaVersion', 'id', 'title', 'createdAt', 'updatedAt', 'context', 'observation', 'humanResult', 'review']) ||
        dossier.schemaVersion !== SCHEMA_VERSION || typeof dossier.id !== 'string' || !UUID.test(dossier.id) ||
        typeof dossier.title !== 'string' || !dossier.title.trim() || dossier.title.length > 120 ||
        !isTimestamp(dossier.createdAt) || !isTimestamp(dossier.updatedAt) ||
        !hasExactKeys(dossier.context, ['pageUrl']) || !validPageUrl(dossier.context.pageUrl) ||
        !hasExactKeys(dossier.observation, ['disposition']) || !OBSERVATIONS.includes(dossier.observation.disposition) ||
        !hasExactKeys(dossier.humanResult, ['outcome', 'justification']) || ![null, ...OUTCOMES].includes(dossier.humanResult.outcome) ||
        typeof dossier.humanResult.justification !== 'string' || dossier.humanResult.justification.length > 2000 ||
        !hasExactKeys(dossier.review, ['state']) || !REVIEWS.includes(dossier.review.state)) throw new Error('Dossier d’audit invalide.');
    if (['Non applicable', 'Non vérifiable'].includes(dossier.humanResult.outcome) && !dossier.humanResult.justification.trim()) throw new Error('Une justification non vide est obligatoire.');
    return dossier;
  }
  function write(dossier) {
    validate(dossier);
    fs.mkdirSync(directory, { recursive: true });
    const target = fileFor(dossier.id);
    const temp = `${target}.${createId()}.tmp`;
    try { fs.writeFileSync(temp, `${JSON.stringify(dossier, null, 2)}\n`, { flag: 'wx' }); fs.renameSync(temp, target); }
    finally { if (fs.existsSync(temp)) fs.unlinkSync(temp); }
    return dossier;
  }
  return Object.freeze({
    create(title) {
      if (typeof title !== 'string' || !title.trim() || title.trim().length > 120) throw new Error('Saisissez un titre (120 caractères maximum).');
      const id = createId();
      const now = new Date().toISOString();
      return write({ schemaVersion: SCHEMA_VERSION, id, title: title.trim(), createdAt: now, updatedAt: now,
        context: { pageUrl: '' }, observation: { disposition: 'unreviewed' },
        humanResult: { outcome: null, justification: '' }, review: { state: 'not-started' } });
    },
    list() {
      if (!fs.existsSync(directory)) return [];
      return fs.readdirSync(directory).filter(name => UUID.test(name.replace(/\.json$/, '')) && name.endsWith('.json'))
        .map(name => { try { const value = JSON.parse(fs.readFileSync(path.join(directory, name), 'utf8')); if (value.id !== name.slice(0, -5)) return null; return validate(value); } catch { return null; } })
        .filter(Boolean).map(({ id, title, updatedAt, review, humanResult, observation }) => ({ id, title, updatedAt, review, humanResult, observation }));
    },
    open(id) { const value = JSON.parse(fs.readFileSync(fileFor(id), 'utf8')); if (value.id !== id) throw new Error('L’identifiant du dossier ne correspond pas.'); return validate(value); },
    saveResult(id, outcome, justification, pageUrl) {
      if (!OUTCOMES.includes(outcome)) throw new Error('Résultat invalide.');
      if (typeof justification !== 'string' || justification.length > 2000 || (['Non applicable', 'Non vérifiable'].includes(outcome) && !justification.trim())) throw new Error('La justification doit compter au maximum 2000 caractères et ne peut pas être vide pour ce résultat.');
      const value = this.open(id);
      const nextPageUrl = typeof pageUrl === 'string' ? pageUrl : value.context.pageUrl;
      if (!validPageUrl(nextPageUrl)) throw new Error('URL de page invalide.');
      const changed = value.humanResult.outcome !== outcome || value.humanResult.justification !== justification || value.context.pageUrl !== nextPageUrl;
      value.humanResult = { outcome, justification };
      value.context.pageUrl = nextPageUrl;
      if (changed && value.review.state === 'complete') value.review.state = 'in-progress';
      else if (value.review.state === 'not-started') value.review.state = 'in-progress';
      value.updatedAt = new Date().toISOString(); return write(value);
    },
    setObservation(id, disposition) {
      if (!OBSERVATIONS.includes(disposition)) throw new Error('Observation invalide.');
      const value = this.open(id); value.observation.disposition = disposition; value.updatedAt = new Date().toISOString(); return write(value);
    },
    complete(id) {
      const value = this.open(id);
      if (!value.humanResult.outcome) throw new Error('Une décision humaine enregistrée est nécessaire avant de terminer la revue.');
      value.review.state = 'complete'; value.updatedAt = new Date().toISOString(); return write(value);
    }
  });
}

module.exports = { createAuditStore, OUTCOMES, OBSERVATIONS, REVIEWS, SCHEMA_VERSION };
