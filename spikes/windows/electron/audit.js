'use strict';

const byId = id => document.getElementById(id);
const status = byId('status');
const urlInput = byId('target-url');
const inspection = byId('inspection');
const currentUrl = byId('current-url');
const auditSelect = byId('audit-list');
const justification = byId('justification');
const decisionStatus = byId('decision-status');
const recordDecision = byId('record-decision');
let activeAudit = null;

const reviewLabels = { 'not-started': 'non commencée', 'in-progress': 'en cours', complete: 'terminée' };
const observationLabels = { unreviewed: 'à examiner', confirmed: 'confirmée', rejected: 'rejetée', 'needs-follow-up': 'à compléter' };
const choices = () => [...document.querySelectorAll('input[name="decision"]')];
const selectedOutcome = () => choices().find(input => input.checked)?.value || null;

function message(value) { status.textContent = value; }
function setText(id, value) { byId(id).textContent = value; }
function safeUrl(value) {
  try {
    const url = new URL(value);
    return ['http:', 'https:'].includes(url.protocol) && !url.username && !url.password && value.length <= 2048 ? url.href : null;
  } catch { return null; }
}

function updateDecisionControls() {
  const outcome = selectedOutcome();
  const needsJustification = outcome === 'Non applicable' || outcome === 'Non vérifiable';
  justification.required = needsJustification;
  const missingJustification = needsJustification && !justification.value.trim();
  recordDecision.disabled = !activeAudit || !outcome || missingJustification;
  byId('complete-review').disabled = !activeAudit || !activeAudit.humanResult.outcome || activeAudit.review.state === 'complete';
  if (!activeAudit) decisionStatus.textContent = 'Ouvrez un dossier pour commencer.';
  else if (!outcome) decisionStatus.textContent = `Résultat : ${activeAudit.humanResult.outcome || 'aucun'} · Revue : ${reviewLabels[activeAudit.review.state]}.`;
  else if (missingJustification) decisionStatus.textContent = `« ${outcome} » exige une justification non vide.`;
  else decisionStatus.textContent = `Résultat : ${activeAudit.humanResult.outcome || 'brouillon'} · Revue : ${reviewLabels[activeAudit.review.state]}.`;
}

function updateDossier() {
  setText('active-audit', activeAudit ? `${activeAudit.title} (${activeAudit.id})` : 'Aucun dossier ouvert.');
  const pageUrl = activeAudit?.context.pageUrl;
  setText('active-audit-page', pageUrl === 'fixture://synthetic-page' ? 'Page locale de démonstration (synthétique)' : pageUrl || 'Aucune page enregistrée.');
  setText('observation-status', activeAudit
    ? `Observation automatique : ${observationLabels[activeAudit.observation.disposition]}. Ce statut ne définit pas le résultat humain.`
    : 'Aucune action sur l’observation ; aucun résultat de conformité n’est déduit.');
  if (activeAudit) auditSelect.value = activeAudit.id;
  byId('open-audit').disabled = auditSelect.options.length === 0;
  for (const input of choices()) input.checked = activeAudit?.humanResult.outcome === input.value;
  justification.value = activeAudit?.humanResult.justification || '';
  for (const input of document.querySelectorAll('input[name="observation"]')) input.checked = activeAudit?.observation.disposition === input.value;
  updateDecisionControls();
}

async function refreshList() {
  const audits = await window.spike.listAudits();
  auditSelect.replaceChildren();
  for (const audit of audits) {
    const option = document.createElement('option');
    option.value = audit.id;
    option.textContent = `${audit.title} — revue ${reviewLabels[audit.review.state]}`;
    auditSelect.append(option);
  }
  if (activeAudit) auditSelect.value = activeAudit.id;
  byId('open-audit').disabled = auditSelect.options.length === 0;
}

window.spike.onStatus(payload => {
  if (payload && Object.hasOwn(payload, 'lastSuccessfulUrl')) {
    const loadedUrl = payload.lastSuccessfulUrl;
    currentUrl.textContent = loadedUrl || 'Aucune page chargée avec succès.';
    if (!urlInput.value && loadedUrl && !loadedUrl.startsWith('file:')) urlInput.value = loadedUrl;
  }
  if (payload?.message) message(payload.message);
});

byId('url-form').addEventListener('submit', async event => {
  event.preventDefault();
  const url = safeUrl(urlInput.value);
  if (!url) { message('Navigation refusée. Saisissez une URL http(s) valide, sans identifiants dans l’URL.'); urlInput.focus(); return; }
  try { await window.spike.navigate(url); } catch (error) { message(error.message); }
});

byId('reload').addEventListener('click', async () => {
  const value = currentUrl.textContent;
  try { await window.spike.navigate(value.startsWith('file:') ? value : safeUrl(value)); }
  catch (error) { message(error.message); }
});

byId('focus-target').addEventListener('click', async () => {
  try { await window.spike.focusTarget(); } catch (error) { message(error.message); }
});
byId('focus-audit').addEventListener('click', async () => {
  try { await window.spike.focusAudit(); } catch (error) { message(error.message); }
});

byId('inspect').addEventListener('click', async () => {
  inspection.textContent = 'Inspection en cours…';
  try { inspection.textContent = JSON.stringify(await window.spike.inspect(), null, 2); }
  catch (error) { inspection.textContent = error.message; }
});

byId('create-audit').addEventListener('click', async () => {
  try {
    activeAudit = await window.spike.createAudit(byId('audit-title').value);
    await refreshList();
    updateDossier();
    message('Dossier synthétique créé et ouvert.');
  } catch (error) { message(error.message); }
});

byId('open-audit').addEventListener('click', async () => {
  try {
    activeAudit = await window.spike.openAudit(auditSelect.value);
    updateDossier();
    message('Dossier ouvert. La page du navigateur n’a pas été changée.');
  } catch (error) { message(error.message); }
});

byId('decision-form').addEventListener('submit', async event => {
  event.preventDefault();
  if (!activeAudit) { message('Créez ou ouvrez un dossier avant d’enregistrer.'); return; }
  const outcome = selectedOutcome();
  if (!outcome) { message('Choisissez un résultat.'); choices()[0].focus(); return; }
  if (['Non applicable', 'Non vérifiable'].includes(outcome) && !justification.value.trim()) {
    message('Une justification non vide est obligatoire pour ce résultat.');
    justification.focus();
    return;
  }
  try {
    activeAudit = await window.spike.saveResult(activeAudit.id, outcome, justification.value);
    await refreshList();
    updateDossier();
    message('Résultat enregistré. La revue doit être terminée séparément.');
  } catch (error) { message(error.message); }
});

for (const input of choices()) input.addEventListener('change', updateDecisionControls);
justification.addEventListener('input', updateDecisionControls);

document.querySelectorAll('input[name="observation"]').forEach(input => input.addEventListener('change', async () => {
  if (!activeAudit) { message('Créez ou ouvrez un dossier avant de modifier cette observation.'); return; }
  try { activeAudit = await window.spike.setObservation(activeAudit.id, input.value); updateDossier(); }
  catch (error) { message(error.message); }
}));

byId('complete-review').addEventListener('click', async () => {
  if (!activeAudit) return;
  if (!activeAudit.humanResult.outcome) { message('Enregistrez une décision humaine avant de terminer la revue.'); return; }
  try {
    activeAudit = await window.spike.completeAudit(activeAudit.id);
    await refreshList();
    updateDossier();
    message('Revue explicitement marquée comme terminée. Cela ne signifie pas que le critère est conforme.');
  } catch (error) { message(error.message); }
});

updateDossier();
refreshList().catch(error => message(error.message));
