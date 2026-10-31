'use strict';

const STORAGE_KEY = 'rgaa-workflow-prototype-v1';
const PROTOTYPE_RULES_BY_PAGE = ['buttonName', 'fieldLabel', 'keyboardUse', 'headingOrder'];
const DEMO_RULES = {
  buttonName: { title: 'Nom accessible de la commande', scope: 'element', description: 'Vérifier que le nom communiqué identifie l’action du contrôle.', hint: 'Aide de démonstration : examiner le nom accessible, sans déduire la conformité.' },
  fieldLabel: { title: 'Étiquette du champ', scope: 'element', description: 'Vérifier que le champ de recherche possède une étiquette compréhensible.', hint: 'Aide de démonstration : examiner l’association entre le champ et son étiquette.' },
  keyboardUse: { title: 'Utilisation au clavier', scope: 'page', description: 'Parcourir le formulaire et ses résultats sans souris.', hint: 'Vérification manuelle de page ; aucun élément unique ne suffit.' },
  headingOrder: { title: 'Structure des titres', scope: 'page', description: 'Vérifier la structure et l’ordre des titres de la page.', hint: 'Vérification de page ; l’auditrice décide du résultat.' }
};

const DEFAULT_AUDITS = [{
  id: 'demo-audit-01',
  name: 'Portail de services — démonstration',
  url: 'https://exemple.test',
  reference: 'RGAA',
  version: '4.1.2',
  level: '',
  updatedAt: '2026-10-28T14:20:00.000Z',
  currentPageId: 'search',
  pages: [
    { id: 'home', title: 'Accueil', url: 'https://exemple.test/', source: 'Lien du menu de démonstration', status: 'in-progress', reviewComplete: false, results: {}, proofs: [] },
    { id: 'search', title: 'Recherche', url: 'https://exemple.test/recherche', source: 'Lien du menu de démonstration', status: 'in-progress', reviewComplete: false, results: {}, proofs: [] },
    { id: 'results', title: 'Résultats', url: 'https://exemple.test/resultats', source: 'Lien interne de démonstration', status: 'todo', reviewComplete: false, results: {}, proofs: [] },
    { id: 'contact', title: 'Contact', url: 'https://exemple.test/contact', source: 'Lien du pied de page de démonstration', status: 'todo', reviewComplete: false, results: {}, proofs: [] }
  ]
}];

const views = ['home', 'setup', 'pages', 'workspace'];
const byId = id => document.getElementById(id);
let audits = loadAudits();
let draftAudit = null;
let activeAuditId = null;
let activePageId = null;
let selectedElement = null;
let selectedRuleId = null;
let inspectMode = false;
let lastFocusedTarget = null;
let includeCompletedAudits = false;
let toastTimer = 0;

function loadAudits() {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) return structuredClone(DEFAULT_AUDITS);
    const parsed = JSON.parse(stored);
    return Array.isArray(parsed) ? parsed : structuredClone(DEFAULT_AUDITS);
  } catch {
    return structuredClone(DEFAULT_AUDITS);
  }
}

function saveAudits() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(audits));
    const status = byId('autosave-status'); status.replaceChildren();
    status.append(makeElement('span', 'saved-dot'), document.createTextNode('Démonstration locale enregistrée'));
  } catch {
    announce('Le navigateur ne peut pas conserver cette démonstration. Relancez-la depuis un serveur local.');
  }
}

function currentAudit() { return audits.find(audit => audit.id === activeAuditId) || null; }
function currentPage() { return currentAudit()?.pages.find(page => page.id === activePageId) || null; }
function announce(message) {
  const toast = byId('toast');
  toast.textContent = message;
  toast.classList.add('visible');
  window.clearTimeout(toastTimer);
  toastTimer = window.setTimeout(() => toast.classList.remove('visible'), 3600);
}

function showView(name, focusMain = true) {
  for (const view of views) byId(`view-${view}`).hidden = view !== name;
  byId('nav-audits').classList.add('active');
  byId('nav-audits').setAttribute('aria-current', 'page');
  if (name === 'workspace') {
    byId('app-body').classList.add('workspace-open');
  } else {
    byId('app-body').classList.remove('workspace-open');
  }
  if (focusMain) requestAnimationFrame(() => (byId(`view-${name}`)?.querySelector('h1') || byId('main-content')).focus());
}

function pageCount(audit) { return audit.pages.filter(page => page.reviewComplete).length; }
function latestDate(value) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Récemment';
  return new Intl.DateTimeFormat('fr-FR', { day: 'numeric', month: 'short' }).format(date);
}

function renderHome() {
  const body = byId('audit-table-body');
  const sidebar = byId('sidebar-audits');
  body.replaceChildren();
  sidebar.replaceChildren();
  const inProgress = audits.filter(audit => audit.pages.some(page => !page.reviewComplete));
  const visibleAudits = includeCompletedAudits ? audits : inProgress;
  byId('audit-count').textContent = String(inProgress.length);
  byId('summary-active').textContent = String(inProgress.length);
  byId('summary-pages').textContent = String(inProgress.reduce((sum, audit) => sum + audit.pages.filter(page => !page.reviewComplete).length, 0));
  byId('summary-date').textContent = audits.length ? latestDate(audits.map(audit => audit.updatedAt).sort().at(-1)) : '—';
  byId('home-list-count').textContent = `${visibleAudits.length} ${visibleAudits.length === 1 ? 'dossier' : 'dossiers'}`;
  byId('empty-audits').hidden = visibleAudits.length > 0;
  byId('audit-table').hidden = visibleAudits.length === 0;
  byId('show-all-audits').textContent = includeCompletedAudits ? 'Audits en cours' : 'Tout voir';
  byId('show-all-audits').setAttribute('aria-pressed', String(includeCompletedAudits));

  for (const audit of visibleAudits) {
    const done = pageCount(audit);
    const total = audit.pages.length;
    const percent = total ? Math.round((done / total) * 100) : 0;
    const refLabel = `${audit.reference}${audit.version ? ` ${audit.version}` : ''}${audit.level ? ` · ${audit.level}` : ''}`;
    const tr = document.createElement('tr');
    const siteCell = document.createElement('td'); siteCell.className = 'site-cell';
    siteCell.append(makeElement('strong', '', audit.name), makeElement('span', '', audit.url));
    const refCell = document.createElement('td'); refCell.append(makeElement('span', 'ref-badge', refLabel));
    const progressCell = document.createElement('td'); progressCell.className = 'progress-cell';
    const progressText = makeElement('span', 'progress-text');
    progressText.append(makeElement('span', '', `${done} page${done > 1 ? 's' : ''} terminée${done > 1 ? 's' : ''}`), makeElement('span', '', `${total} au lot`));
    const progressTrack = makeElement('div', 'progress-track');
    progressTrack.setAttribute('role', 'progressbar'); progressTrack.setAttribute('aria-valuenow', String(done)); progressTrack.setAttribute('aria-valuemax', String(total)); progressTrack.setAttribute('aria-label', `${done} page sur ${total} terminée`);
    const progressFill = makeElement('span'); progressFill.style.width = `${percent}%`; progressTrack.append(progressFill); progressCell.append(progressText, progressTrack);
    const dateCell = makeElement('td', '', latestDate(audit.updatedAt));
    const actionCell = document.createElement('td'); const resume = makeElement('button', 'resume-button', audit.pages.every(page => page.reviewComplete) ? 'Consulter' : 'Reprendre'); resume.type = 'button'; resume.dataset.resume = audit.id; actionCell.append(resume);
    tr.append(siteCell, refCell, progressCell, dateCell, actionCell);
    body.append(tr);

    const sideButton = document.createElement('button');
    sideButton.className = 'sidebar-audit';
    sideButton.type = 'button';
    sideButton.dataset.resume = audit.id;
    if (!audit.pages.every(page => page.reviewComplete)) {
      sideButton.append(makeElement('strong', '', audit.name), makeElement('span', '', `${refLabel} · ${done}/${total} pages`));
      sidebar.append(sideButton);
    }
  }

  body.querySelectorAll('[data-resume]').forEach(button => button.addEventListener('click', () => openAudit(button.dataset.resume)));
  sidebar.querySelectorAll('[data-resume]').forEach(button => button.addEventListener('click', () => openAudit(button.dataset.resume)));
}

function beginSetup() {
  draftAudit = null;
  byId('setup-form').reset();
  byId('setup-error').hidden = true;
  byId('level-field').hidden = true;
  const version = byId('reference-version');
  version.replaceChildren(new Option('Choisir le référentiel d’abord', ''));
  version.disabled = true;
  showView('setup');
  byId('site-url').focus();
}

function setVersionOptions(reference) {
  const version = byId('reference-version');
  version.replaceChildren(new Option('Choisir une version', ''));
  version.disabled = !reference;
  if (reference === 'RGAA') version.add(new Option('4.1.2', '4.1.2'));
  if (reference === 'WCAG') version.add(new Option('2.2', '2.2'));
}

function validateSetup() {
  const urlField = byId('site-url');
  const ref = byId('reference').value;
  const version = byId('reference-version').value;
  const level = document.querySelector('input[name="wcag-level"]:checked')?.value || '';
  const referenceField = byId('reference');
  const versionField = byId('reference-version');
  const levelField = byId('level-field');
  [urlField, referenceField, versionField, ...document.querySelectorAll('input[name="wcag-level"]')].forEach(field => { field.removeAttribute('aria-invalid'); field.removeAttribute('aria-describedby'); });
  let error = '';
  let invalidTarget = null;
  try {
    const url = new URL(urlField.value.trim());
    if (!['http:', 'https:'].includes(url.protocol)) error = 'Utilisez une adresse de site http ou https.';
    else if (url.hostname !== 'exemple.test') error = 'Cette maquette ne charge pas de vrais sites. Utilisez https://exemple.test pour le parcours de démonstration.';
  } catch { error = 'Saisissez une URL complète, par exemple https://exemple.fr.'; }
  if (error) invalidTarget = urlField;
  if (!error && !ref) { error = 'Choisissez un référentiel.'; invalidTarget = referenceField; }
  if (!error && !version) { error = 'Choisissez la version du référentiel.'; invalidTarget = versionField; }
  if (!error && ref === 'WCAG' && !level) { error = 'Choisissez une cible WCAG : A, AA ou AAA.'; invalidTarget = document.querySelector('input[name="wcag-level"]'); }
  if (invalidTarget) { invalidTarget.setAttribute('aria-invalid', 'true'); invalidTarget.setAttribute('aria-describedby', 'setup-error'); }
  if (ref === 'WCAG') levelField.setAttribute('aria-describedby', 'setup-error');
  else levelField.removeAttribute('aria-describedby');
  byId('setup-error').textContent = error;
  byId('setup-error').hidden = !error;
  if (invalidTarget) invalidTarget.focus();
  return !error;
}

function beginPageSelection() {
  if (!validateSetup()) return;
  const url = new URL(byId('site-url').value.trim());
  const name = byId('audit-name').value.trim() || `Audit de ${url.hostname}`;
  const reference = byId('reference').value;
  draftAudit = {
    id: makeId(), name, url: url.origin, reference,
    version: byId('reference-version').value,
    level: reference === 'WCAG' ? document.querySelector('input[name="wcag-level"]:checked')?.value || '' : '',
    updatedAt: new Date().toISOString(), currentPageId: '', pages: []
  };
  renderCandidates();
  showView('pages');
}

const candidates = [
  { id: 'home', title: 'Accueil', path: '/', found: true, source: 'Lien principal du site de démonstration' },
  { id: 'search', title: 'Recherche', path: '/recherche', found: true, source: 'Lien du menu de démonstration' },
  { id: 'results', title: 'Résultats de recherche', path: '/resultats', found: true, source: 'Lien de la page de recherche de démonstration' },
  { id: 'contact', title: 'Contact', path: '/contact', found: true, source: 'Lien du pied de page de démonstration' },
  { id: 'accessibility', title: 'Déclaration d’accessibilité', path: '/accessibilite', found: false, source: 'Type de page non repéré dans cette démo' }
];

function renderCandidates() {
  const host = byId('candidate-list');
  host.replaceChildren();
  byId('pages-error').hidden = true;
  byId('pages-subtitle').textContent = `${draftAudit.name} · ${draftAudit.reference} ${draftAudit.version}${draftAudit.level ? ` · cible ${draftAudit.level}` : ''}`;
  for (const candidate of candidates) {
    const label = document.createElement('label');
    label.className = 'candidate-row';
    const check = document.createElement('input');
    check.type = 'checkbox'; check.value = candidate.id; check.disabled = !candidate.found; check.setAttribute('aria-label', `${candidate.title}${candidate.found ? ', ajouter au lot' : ', non repérée'}`);
    const text = document.createElement('span');
    text.append(makeElement('span', 'candidate-title', candidate.title), makeElement('span', 'candidate-url', `${draftAudit.url + candidate.path} · ${candidate.source}`));
    const signal = document.createElement('span');
    signal.className = `candidate-signal${candidate.found ? '' : ' missing'}`;
    signal.append(makeElement('span', 'signal-dot'), document.createTextNode(candidate.found ? 'Présente dans la démo' : 'Non repérée'));
    label.append(check, text, signal); host.append(label);
  }
  host.querySelectorAll('input[type="checkbox"]').forEach(check => check.addEventListener('change', updateSelectedCount));
  updateSelectedCount();
}

function updateSelectedCount() {
  const count = byId('candidate-list').querySelectorAll('input:checked').length;
  byId('selected-count').textContent = count ? `${count} page${count > 1 ? 's' : ''} sélectionnée${count > 1 ? 's' : ''}` : 'Aucune page sélectionnée';
  byId('start-audit').disabled = count === 0;
}

function addManualPage() {
  const input = byId('manual-page-url');
  const error = byId('pages-error');
  const raw = input.value.trim();
  let parsed;
  try { parsed = new URL(raw); } catch { parsed = null; }
  if (!parsed || !['http:', 'https:'].includes(parsed.protocol)) {
    error.textContent = 'Saisissez une URL http ou https valide.'; error.hidden = false; input.setAttribute('aria-invalid', 'true'); input.setAttribute('aria-describedby', 'pages-error'); input.focus(); return;
  }
  if (parsed.hostname !== 'exemple.test') {
    error.textContent = 'La maquette n’ouvre pas de sites réels. Utilisez uniquement une URL du site de démonstration.'; error.hidden = false; input.setAttribute('aria-invalid', 'true'); input.setAttribute('aria-describedby', 'pages-error'); input.focus(); return;
  }
  if (candidates.some(item => new URL(item.path, draftAudit.url).href === parsed.href)) {
    error.textContent = 'Cette page figure déjà dans les candidates de démonstration.'; error.hidden = false; input.setAttribute('aria-invalid', 'true'); input.setAttribute('aria-describedby', 'pages-error'); input.focus(); return;
  }
  const id = `manual-${makeId()}`;
  const row = document.createElement('label'); row.className = 'candidate-row';
  const checkbox = document.createElement('input'); checkbox.type = 'checkbox'; checkbox.value = id; checkbox.checked = true; checkbox.dataset.manualUrl = parsed.href;
  const text = document.createElement('span'); text.append(makeElement('span', 'candidate-title', parsed.pathname.split('/').filter(Boolean).at(-1) || parsed.hostname), makeElement('span', 'candidate-url', `${parsed.href} · Ajoutée manuellement ; existence non vérifiée dans cette maquette`));
  const signal = document.createElement('span'); signal.className = 'candidate-signal missing'; signal.append(makeElement('span', 'signal-dot'), document.createTextNode('À vérifier'));
  row.append(checkbox, text, signal); byId('candidate-list').append(row);
  checkbox.addEventListener('change', updateSelectedCount);
  input.value = ''; input.removeAttribute('aria-invalid'); input.removeAttribute('aria-describedby'); error.hidden = true; updateSelectedCount();
}

function commitAudit() {
  const checks = [...byId('candidate-list').querySelectorAll('input:checked')];
  if (!checks.length) {
    byId('pages-error').textContent = 'Sélectionnez au moins une page avant de continuer.';
    byId('pages-error').hidden = false;
    const firstCandidate = byId('candidate-list').querySelector('input:not(:disabled)');
    if (firstCandidate) firstCandidate.focus(); else byId('manual-page-url').focus();
    return;
  }
  draftAudit.pages = checks.map(check => {
    const candidate = candidates.find(item => item.id === check.value);
    const manualUrl = check.dataset.manualUrl;
    const pageUrl = manualUrl || `${draftAudit.url}${candidate.path}`;
    const title = candidate?.title || new URL(pageUrl).pathname.split('/').filter(Boolean).at(-1) || new URL(pageUrl).hostname;
    return { id: check.value, title, url: pageUrl, source: candidate?.source || 'Ajout manuel — non vérifiée', status: 'todo', reviewComplete: false, requiredRuleIds: [...PROTOTYPE_RULES_BY_PAGE], results: {}, proofs: [] };
  });
  draftAudit.currentPageId = draftAudit.pages[0].id;
  audits.unshift(draftAudit); activeAuditId = draftAudit.id; activePageId = draftAudit.currentPageId;
  saveAudits(); openAudit(activeAuditId);
}

function makeId() {
  return globalThis.crypto?.randomUUID ? crypto.randomUUID() : `demo-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function openAudit(id) {
  const audit = audits.find(item => item.id === id);
  if (!audit) { announce('Ce dossier n’est plus disponible dans les données de démonstration.'); return; }
  activeAuditId = audit.id;
  activePageId = audit.currentPageId || audit.pages.find(page => !page.reviewComplete)?.id || audit.pages[0]?.id || null;
  selectedElement = null; selectedRuleId = null; inspectMode = false; lastFocusedTarget = null;
  renderWorkspace(); showView('workspace');
}

function renderPageRail() {
  const audit = currentAudit(); const list = byId('workspace-page-list'); list.replaceChildren();
  for (const page of audit.pages) {
    const button = document.createElement('button'); button.type = 'button'; button.className = `page-item${page.id === activePageId ? ' active' : ''}`;
    const state = document.createElement('span'); state.className = `page-state${page.reviewComplete ? ' complete' : page.id === activePageId ? ' current' : ''}`; state.setAttribute('aria-hidden', 'true');
    const text = document.createElement('span'); text.append(makeElement('strong', '', page.title), makeElement('small', '', page.reviewComplete ? 'Revue terminée' : page.status === 'in-progress' ? 'En cours' : 'À examiner'));
    if (page.id === activePageId) button.setAttribute('aria-current', 'page');
    button.append(state, text); button.addEventListener('click', () => { activePageId = page.id; audit.currentPageId = page.id; selectedElement = null; selectedRuleId = null; lastFocusedTarget = null; saveAudits(); renderWorkspace(); }); list.append(button);
  }
  const done = audit.pages.filter(item => item.reviewComplete).length; byId('page-progress-label').textContent = `${done} page${done === 1 ? '' : 's'} sur ${audit.pages.length} revue${audit.pages.length === 1 ? '' : 's'} terminée${done === 1 ? '' : 's'}`;
  byId('page-progress-bar').style.width = `${audit.pages.length ? Math.round(done / audit.pages.length * 100) : 0}%`;
}

function makeElement(tag, cls, textValue) {
  const element = document.createElement(tag);
  if (cls) element.className = cls;
  if (textValue !== undefined) element.textContent = textValue;
  return element;
}

function renderSitePreview() {
  const page = currentPage(); const host = byId('site-preview'); host.replaceChildren();
  const site = makeElement('article', 'sample-site'); site.setAttribute('aria-label', `Page de démonstration ${page.title}`);
  const header = makeElement('header', 'sample-site-header');
  header.append(makeElement('strong', 'sample-brand', 'PORTAIL DE SERVICES'), makeElement('nav', 'sample-site-nav', 'Accueil   Services   Recherche   Contact'));
  const body = makeElement('div', 'sample-site-body');
  const title = makeElement('h2', '', page.title === 'Accueil' ? 'Des services accessibles à chacun' : page.title === 'Recherche' ? 'Rechercher un service' : page.title);
  const lead = makeElement('p', 'sample-lead', 'Page de démonstration destinée à vérifier le parcours d’audit. Le contenu et les règles affichés dans cette maquette sont synthétiques.');
  const banner = makeElement('div', 'sample-banner', page.title === 'Recherche' ? 'Recherchez une information dans le catalogue des services.' : 'Consultez les informations, puis poursuivez votre démarche.');
  const label = makeElement('label', '', page.title === 'Recherche' ? 'Votre recherche' : 'Adresse courriel'); label.htmlFor = 'demo-input';
  const input = document.createElement('input'); input.id = 'demo-input'; input.type = 'text'; input.value = page.title === 'Recherche' ? 'Accessibilité numérique' : ''; input.placeholder = page.title === 'Recherche' ? 'Saisir un mot-clé' : 'nom@exemple.fr'; input.dataset.inspectable = 'true'; input.dataset.elementName = page.title === 'Recherche' ? 'Champ de recherche' : 'Champ de contact'; input.dataset.elementKind = 'input'; input.setAttribute('aria-label', page.title === 'Recherche' ? 'Recherche par mot-clé' : 'Adresse courriel');
  const button = document.createElement('button'); button.type = 'button'; button.className = 'sample-action'; button.dataset.inspectable = 'true'; button.dataset.elementName = page.title === 'Recherche' ? 'Bouton Rechercher' : 'Bouton Continuer'; button.dataset.elementKind = 'button'; button.textContent = page.title === 'Recherche' ? 'Rechercher' : 'Continuer';
  const status = makeElement('p', 'sample-lead', 'Résultat de démonstration : aucune donnée réelle n’est envoyée.'); status.id = 'demo-site-status'; status.setAttribute('aria-live', 'polite');
  button.addEventListener('click', () => { if (!inspectMode) status.textContent = 'Action de démonstration activée ; aucun formulaire n’est envoyé.'; });
  const link = document.createElement('a'); link.href = '#demo-contact'; link.dataset.inspectable = 'true'; link.dataset.elementName = 'Lien Contact'; link.dataset.elementKind = 'link'; link.textContent = 'Consulter les coordonnées de contact';
  body.append(title, lead, banner, label, input, button, status, link); site.append(header, body); host.append(site);
  host.querySelectorAll('[data-inspectable]').forEach(element => {
    element.addEventListener('focusin', () => { lastFocusedTarget = element; });
    element.addEventListener('click', event => {
      if (!inspectMode) return;
      event.preventDefault(); event.stopPropagation(); selectElement(element);
    });
  });
  if (selectedElement) {
    const restored = [...host.querySelectorAll('[data-inspectable]')].find(element => element.dataset.elementName === selectedElement.name);
    if (restored) { restored.classList.add('selected-for-inspection'); lastFocusedTarget = restored; }
  }
  byId('inspect-instruction').hidden = !inspectMode;
  byId('inspect-mode-button').setAttribute('aria-pressed', String(inspectMode));
  byId('inspect-mode-button').querySelector('span').textContent = inspectMode ? 'Arrêter le pointage' : 'Pointer un élément';
}

function selectElement(element) {
  if (!element) { announce('Placez le focus sur un élément de la page avant de l’associer.'); return; }
  byId('site-preview').querySelectorAll('.selected-for-inspection').forEach(item => item.classList.remove('selected-for-inspection'));
  element.classList.add('selected-for-inspection');
  const name = element.dataset.elementName || element.textContent.trim() || element.tagName.toLowerCase();
  const kind = element.dataset.elementKind || element.tagName.toLowerCase();
  const accessibleName = element.getAttribute('aria-label') || element.textContent.trim() || element.value || 'Aucun nom visible dans la maquette';
  selectedElement = { name, kind, tag: element.tagName.toLowerCase(), accessibleName, pageId: activePageId, state: element.disabled ? 'désactivé' : 'actif' };
  selectedRuleId = null;
  renderInspector();
  announce(`${name} sélectionné dans la maquette. Les informations sont synthétiques.`);
}

function relevantRules() {
  if (!selectedElement) return [];
  if (selectedElement.kind === 'button') return ['buttonName', 'keyboardUse'];
  if (selectedElement.kind === 'input') return ['fieldLabel', 'keyboardUse'];
  return ['headingOrder', 'keyboardUse'];
}

function resultLabel(page, ruleId) { return page.results?.[ruleId]?.outcome || ''; }
function renderRuleButton(ruleId, page, isPageRule = false) {
  const rule = DEMO_RULES[ruleId];
  const button = makeElement('button', 'rule-item'); button.type = 'button';
  button.dataset.ruleId = ruleId;
  const title = makeElement('strong', '', rule.title);
  const meta = makeElement('span', 'rule-meta');
  const result = resultLabel(page, ruleId);
  meta.append(makeElement('span', result ? 'human-result' : 'origin-label', result ? `Décision : ${result}` : isPageRule ? 'Vérification manuelle' : 'À examiner par l’auditrice'));
  const description = makeElement('span', '', rule.description);
  button.append(title, meta, description);
  button.addEventListener('click', () => openRule(ruleId));
  return button;
}

function renderInspector() {
  const page = currentPage(); if (!page) return;
  byId('selected-element-title').textContent = selectedElement?.name || 'Aucun élément sélectionné';
  byId('selected-element-description').textContent = selectedElement
    ? 'Élément et état observés dans la page de démonstration. Les informations ne constituent pas un verdict.'
    : 'Pointez un élément dans la page ou placez-y le focus puis utilisez « Élément au focus ». Vous pouvez aussi examiner une règle de page entière.';
  const details = byId('element-details'); details.replaceChildren();
  if (selectedElement) {
    for (const [label, value] of [['Type', `<${selectedElement.tag}>`], ['Nom observé', selectedElement.accessibleName], ['État', selectedElement.state], ['Page', page.title]]) {
      const row = makeElement('div', 'detail-row'); row.append(makeElement('dt', '', label), makeElement('dd', '', value)); details.append(row);
    }
  }
  const ids = relevantRules(); byId('candidate-rule-count').textContent = String(ids.length);
  const candidateRules = byId('candidate-rules'); candidateRules.replaceChildren();
  if (!selectedElement) candidateRules.append(makeElement('p', 'empty-hint', 'Sélectionnez un élément pour voir les règles candidates associées.'));
  else ids.forEach(id => candidateRules.append(renderRuleButton(id, page)));
  const pageRules = byId('page-rules'); pageRules.replaceChildren();
  ['keyboardUse', 'headingOrder'].forEach(id => pageRules.append(renderRuleButton(id, page, true)));
  const proofList = byId('proof-list'); proofList.replaceChildren();
  const proofs = (page.proofs || []).filter(proof => !selectedElement || proof.elementName === selectedElement.name);
  if (!proofs.length) proofList.append(makeElement('p', 'empty-hint', selectedElement ? 'Aucune preuve associée à cet élément.' : 'Aucune preuve dans cette page.'));
  proofs.forEach(proof => {
    const item = makeElement('button', 'proof-item'); item.type = 'button';
    const info = makeElement('span'); info.append(makeElement('strong', '', proof.title), makeElement('small', '', `${proof.ruleName || 'Contexte de page'} · ${proof.kind === 'capture' ? 'capture simulée' : 'note'}`));
    item.append(info, makeElement('span', 'proof-type', 'Voir')); item.setAttribute('aria-label', `Consulter la preuve ${proof.title}, capture simulée`);
    item.addEventListener('click', () => openProofDetails(proof)); proofList.append(item);
  });
  byId('add-proof').disabled = !selectedElement;
}

function openRule(ruleId) {
  selectedRuleId = ruleId;
  const rule = DEMO_RULES[ruleId];
  byId('rule-dialog-title').textContent = rule.title;
  byId('rule-dialog-description').textContent = `${rule.description} ${rule.hint}`;
  byId('decision-error').hidden = true;
  const saved = currentPage().results?.[ruleId];
  document.querySelectorAll('input[name="result"]').forEach(input => { input.checked = saved?.outcome === input.value; });
  byId('justification').value = saved?.justification || '';
  updateJustification();
  byId('rule-dialog').showModal();
}

function updateJustification() {
  const outcome = document.querySelector('input[name="result"]:checked')?.value || '';
  byId('justification-wrap').hidden = !['Non applicable', 'Non vérifiable'].includes(outcome);
}

function saveDecision() {
  const outcome = document.querySelector('input[name="result"]:checked')?.value;
  const error = byId('decision-error');
  if (!outcome) { error.textContent = 'Choisissez un résultat pour cette règle.'; error.hidden = false; document.querySelector('input[name="result"]')?.focus(); return; }
  const justification = byId('justification').value.trim();
  if (['Non applicable', 'Non vérifiable'].includes(outcome) && !justification) {
    error.textContent = 'Une justification est nécessaire pour ce résultat.'; error.hidden = false; byId('justification').focus(); return;
  }
  const page = currentPage();
  page.results ||= {};
  page.results[selectedRuleId] = { outcome, justification, updatedAt: new Date().toISOString() };
  page.status = 'in-progress'; page.reviewComplete = false; currentAudit().updatedAt = new Date().toISOString();
  saveAudits(); byId('rule-dialog').close(); renderWorkspace();
  document.querySelector(`[data-rule-id="${selectedRuleId}"]`)?.focus();
  announce(`Résultat « ${outcome} » enregistré dans la maquette. La revue reste distincte.`);
}

function openProofDialog() {
  if (!selectedElement) return;
  byId('proof-note').value = '';
  byId('proof-preview-caption').textContent = `${currentPage().title} · ${selectedElement.name}`;
  byId('proof-error').hidden = true;
  byId('proof-dialog').showModal();
}

function openProofDetails(proof) {
  byId('proof-view-title').textContent = proof.title;
  byId('proof-view-context').textContent = `${currentAudit().name} · ${proof.pageTitle} · ${proof.pageUrl}${proof.ruleName ? ` · ${proof.ruleName}` : ''}`;
  byId('proof-view-note').textContent = proof.note || 'Aucune note ajoutée.';
  byId('proof-view-dialog').showModal();
}

function saveProof() {
  if (!selectedElement) return;
  const page = currentPage();
  page.proofs ||= [];
  page.proofs.push({ id: makeId(), auditId: currentAudit().id, pageId: page.id, pageTitle: page.title, pageUrl: page.url, kind: 'capture', title: selectedElement.name, elementName: selectedElement.name, elementState: selectedElement.state, ruleName: selectedRuleId ? DEMO_RULES[selectedRuleId].title : '', note: byId('proof-note').value.trim(), createdAt: new Date().toISOString(), simulated: true });
  currentAudit().updatedAt = new Date().toISOString(); saveAudits(); byId('proof-dialog').close(); renderWorkspace(); byId('add-proof').focus();
  announce('Preuve de démonstration ajoutée. Aucune capture réelle n’a été prise.');
}

function finishPageReview() {
  const page = currentPage();
  const required = page.requiredRuleIds || PROTOTYPE_RULES_BY_PAGE;
  const missing = required.filter(id => !page.results?.[id]?.outcome).length;
  if (missing) { announce(`${missing} vérification${missing === 1 ? '' : 's'} reste${missing === 1 ? '' : 'nt'} à décider. La page ne peut pas être marquée terminée.`); return; }
  page.reviewComplete = true; page.status = 'done'; currentAudit().updatedAt = new Date().toISOString(); saveAudits(); renderWorkspace(); announce('Revue de la page terminée. Cela ne signifie pas qu’elle est conforme.');
}

function updatePageCompletionControl() {
  const page = currentPage(); const button = byId('finish-page-review');
  if (!page || !button) return;
  const requiredIds = page.requiredRuleIds || PROTOTYPE_RULES_BY_PAGE;
  const decided = requiredIds.filter(id => page.results?.[id]?.outcome).length;
  const complete = decided === requiredIds.length;
  button.disabled = !complete || page.reviewComplete;
  button.textContent = page.reviewComplete ? 'Revue de la page terminée' : 'Marquer la revue de la page terminée';
  byId('workbench-state').textContent = page.reviewComplete
    ? 'Revue de page terminée · Ce statut ne signifie pas que la page est conforme.'
    : `${decided} décision${decided === 1 ? '' : 's'} sur ${requiredIds.length} enregistrée${decided === 1 ? '' : 's'} · Les aides automatiques ne sont pas des verdicts.`;
}

function renderWorkspace() {
  const audit = currentAudit(); if (!audit) { showView('home'); renderHome(); return; }
  renderWorkspaceTitle(); renderPageRail(); renderSitePreview(); renderInspector(); showView('workspace', false);
  updatePageCompletionControl();
}

function renderWorkspaceTitle() {
  const audit = currentAudit(), page = currentPage(); if (!audit || !page) return;
  byId('workspace-audit-crumb').textContent = audit.name;
  byId('workspace-title').textContent = page.title;
  byId('target-url-label').textContent = page.url;
}

function updateLastActivity() { const audit = currentAudit(); if (audit) audit.updatedAt = new Date().toISOString(); }

function startSelectedAudit() { commitAudit(); }

// Global navigation and creation flow.
['sidebar-create', 'home-create', 'empty-create'].forEach(id => byId(id).addEventListener('click', beginSetup));
byId('brand-home').addEventListener('click', () => { renderHome(); showView('home'); });
byId('nav-audits').addEventListener('click', () => { renderHome(); showView('home'); });
byId('show-all-audits').addEventListener('click', () => { includeCompletedAudits = !includeCompletedAudits; renderHome(); showView('home', false); });
document.querySelectorAll('[data-go-home]').forEach(button => button.addEventListener('click', () => { renderHome(); showView('home'); }));
byId('help-button').addEventListener('click', () => announce('Prototype de parcours : les pages, règles et preuves affichées sont synthétiques.'));
byId('profile-button').addEventListener('click', () => announce('Le profil de démonstration ne contient aucune session de site.'));
byId('nav-guidelines').addEventListener('click', () => announce('Le catalogue complet des référentiels n’est pas inclus dans cette maquette.'));
byId('setup-form').addEventListener('submit', event => { event.preventDefault(); beginPageSelection(); });
byId('reference').addEventListener('change', event => { setVersionOptions(event.target.value); byId('level-field').hidden = event.target.value !== 'WCAG'; });
byId('add-manual-page').addEventListener('click', addManualPage);
byId('manual-page-url').addEventListener('keydown', event => { if (event.key === 'Enter') { event.preventDefault(); addManualPage(); } });
byId('back-to-setup').addEventListener('click', () => showView('setup'));
byId('start-audit').addEventListener('click', startSelectedAudit);
byId('workspace-exit').addEventListener('click', () => { updateLastActivity(); saveAudits(); renderHome(); showView('home'); });
byId('return-dashboard').addEventListener('click', () => { updateLastActivity(); saveAudits(); renderHome(); showView('home'); });
byId('edit-page-list').addEventListener('click', () => announce('La gestion du lot est simulée dans cette tranche.'));
byId('add-page-workspace').addEventListener('click', () => announce('L’ajout de page est simulé dans cette tranche.'));
byId('reload-page').addEventListener('click', () => announce('Rechargement simulé : cette maquette n’ouvre pas de site distant.'));

byId('inspect-mode-button').addEventListener('click', () => {
  inspectMode = !inspectMode;
  byId('inspect-mode-button').setAttribute('aria-pressed', String(inspectMode));
  byId('inspect-mode-button').querySelector('span').textContent = inspectMode ? 'Arrêter le pointage' : 'Pointer un élément';
  byId('inspect-instruction').hidden = !inspectMode;
  announce(inspectMode ? 'Mode de pointage activé pour la page de démonstration.' : 'Mode de pointage désactivé.');
});
byId('select-focused-button').addEventListener('click', () => selectElement(lastFocusedTarget));
byId('clear-selection').addEventListener('click', () => { selectedElement = null; selectedRuleId = null; renderSitePreview(); renderInspector(); });
byId('tab-element').addEventListener('click', () => switchInspectorTab('element'));
byId('tab-rules').addEventListener('click', () => switchInspectorTab('rules'));
function switchInspectorTab(tab) {
  const elementActive = tab === 'element';
  byId('tab-element').classList.toggle('active', elementActive); byId('tab-element').setAttribute('aria-selected', String(elementActive));
  byId('tab-rules').classList.toggle('active', !elementActive); byId('tab-rules').setAttribute('aria-selected', String(!elementActive));
  byId('tab-element').tabIndex = elementActive ? 0 : -1;
  byId('tab-rules').tabIndex = elementActive ? -1 : 0;
  byId('panel-element').hidden = !elementActive; byId('panel-rules').hidden = elementActive;
}

for (const tab of [byId('tab-element'), byId('tab-rules')]) {
  tab.addEventListener('keydown', event => {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
    event.preventDefault();
    const tabs = [byId('tab-element'), byId('tab-rules')];
    const index = tabs.indexOf(tab);
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 :
      (index + (event.key === 'ArrowRight' ? 1 : tabs.length - 1)) % tabs.length;
    const target = next === 0 ? 'element' : 'rules';
    switchInspectorTab(target);
    byId(target === 'element' ? 'tab-element' : 'tab-rules').focus();
  });
}

document.querySelectorAll('input[name="result"]').forEach(input => input.addEventListener('change', updateJustification));
byId('save-decision').addEventListener('click', saveDecision);
byId('add-proof').addEventListener('click', openProofDialog);
byId('save-proof').addEventListener('click', saveProof);
byId('finish-page-review')?.addEventListener('click', finishPageReview);
byId('rule-form').addEventListener('submit', event => { event.preventDefault(); saveDecision(); });
byId('proof-form').addEventListener('submit', event => { event.preventDefault(); saveProof(); });
document.querySelectorAll('[data-close-dialog]').forEach(button => button.addEventListener('click', () => byId(button.dataset.closeDialog).close()));

function initializePrototype() {
  const screen = new URL(location.href).searchParams.get('screen');
  if (screen === 'setup') { beginSetup(); return; }
  if (screen === 'pages') {
    draftAudit = { id: 'preview-audit', name: 'Portail de services — démonstration', url: 'https://exemple.test', reference: 'WCAG', version: '2.2', level: '', pages: [] };
    renderCandidates(); showView('pages', false); return;
  }
  if (screen === 'workspace') { openAudit('demo-audit-01'); return; }
  renderHome(); showView('home', false);
}

// Start from a useful resume screen; all seeded content is synthetic.
initializePrototype();
