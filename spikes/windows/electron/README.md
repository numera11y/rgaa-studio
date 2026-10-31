# Spike d’architecture Windows avec Electron (jetable)

Il s’agit d’une **expérience technique temporaire Windows**, pas du code de production, pas du prototype UX de référence et pas d’une décision d’utiliser Electron pour la stack finale. Le retour utilisateur confirme que son organisation actuelle ne représente pas le logiciel attendu (reprise/création, périmètre de pages et audit page par page). Electron 45.1.0 reste un candidat réversible. Cette tranche sert uniquement à tester une page locale synthétique, un panneau séparé, des dossiers JSON, une observation de démonstration et une inspection DOM bornée. Les données sont synthétiques uniquement : ce stockage prototype n’est pas sécurisé pour la production. N’y saisissez aucune donnée réelle.

## Version et sources

- Electron **45.1.0**, dépendance npm épinglée exactement, version et références relevées le 29 octobre 2026. Références officielles : <https://www.electronjs.org/docs/latest/api/base-window>, <https://www.electronjs.org/docs/latest/api/web-contents-view>, <https://www.electronjs.org/docs/latest/tutorial/security>.
- Les événements de navigation sont traités en acceptant la forme récente `details.url` et l’ancienne forme chaîne ; la référence officielle de `will-frame-navigate` a été consultée : <https://www.electronjs.org/docs/latest/api/web-contents#event-will-frame-navigate>.
- Architecture : `BaseWindow` avec deux `WebContentsView` séparées. La cible n’a ni preload applicatif ni IPC. Les deux contenus désactivent Node et activent l’isolation de contexte et le sandbox.
- Le panneau d’audit utilise une session renderer distincte ; la cible utilise une partition mémoire distincte. Les dossiers JSON sont exclusivement sous `user-data/audits/` et n’incluent pas de cookies, jetons ou état de session navigateur. La surface et la session de navigation sont partagées entre les dossiers. Ne jamais utiliser d’identifiants réels.

## Installation et lancement (PowerShell)

Depuis ce répertoire :

```powershell
npm install
npm test
npm run smoke
npm start
```

Node.js/npm sont requis. L’installation d’Electron télécharge son runtime et nécessite un accès réseau initial. `npm install` génère `package-lock.json` dans ce dossier. Pour réinitialiser les dossiers, fermer l’application puis supprimer `user-data/audits/`. Pour supprimer tous les artefacts locaux, fermer l’application puis supprimer entièrement `user-data/` (cela efface aussi les données locales Electron de ce spike) et éventuellement `node_modules/` ; conserver le lockfile pour reproduire l’installation.

## Vérifications déjà exécutées (29 octobre 2026)

- `npm ci` — réussi depuis le lockfile sur le poste Windows courant ; 13 paquets ajoutés, audit npm sans vulnérabilité signalée.
- `npm test` — réussi, 7 tests intégrés Node (création/liste/ouverture, isolation A/B, validation/justifications, rejet de schéma altéré et d’horodatage invalide, séparation observation/résultat/revue et invalidation après changement de page).
- `npm run smoke` — réussi ; contrôles statiques de séparation des contenus, CSP, dossiers et rendu texte sûr.
- `node --check main.cjs`, `preload.cjs`, `audit.js`, `audit-store.cjs`, `audit-store.test.cjs` et `smoke.cjs` — chaque vérification syntaxique a réussi.
- `npm exec electron -- --version` — `v45.1.0`.
- `npm start` — le processus Electron est resté actif jusqu’à l’expiration du délai de commande de 15 secondes, puis s’est fermé ; aucun processus Electron ne reste actif. Cela ne confirme ni le rendu visuel ni le comportement des interactions ; ces résultats restent **UNKNOWN** jusqu’à un essai manuel.
- La version/édition/build exacts de Windows, le matériel et les technologies d’assistance n’ont pas encore été consignés.

### Revues statiques indépendantes

- **Sécurité/architecture :** les revues ont conduit à distinguer l’URL demandée de la dernière URL réussie, séparer les sessions de panneau/cible, valider l’IPC et fermer la navigation du panneau hors du document local. Les dossiers sont limités à un schéma strict, horodatages ISO, cohérence nom de fichier/UUID ; une seule instance Electron est autorisée. Le parseur accepte les payloads de navigation récents (`details.url`) et historiques. **Risques encore inconnus à l’exécution :** confirmer sur Electron 45.1.0 la couverture réelle des payloads `will-frame-navigate`/`will-redirect`, les événements de navigation et l’absence de perte/corruption en cas de fermeture pendant une écriture. Le format atomique n’est pas une preuve de durabilité transactionnelle.
- **Accessibilité de l’interface :** pas d’autre défaut certain dans le HTML/CSS/JS statiquement relu ; le champ de justification et les choix d’observation sont désormais associés/présentés séparément. Clavier réel, focus entre les vues, annonces NVDA, comportement à faible largeur et contrastes rendus restent à vérifier manuellement.

## Liste de vérification manuelle

- [ ] Lancer sur une édition/build Windows consignés ; vérifier que la page de test apparaît à côté du panneau.
- [ ] Vérifier l’URL courante, la page locale et la navigation `http(s)` ; rejeter `file:`, `javascript:` et les URL malformées saisies.
- [ ] Tenter popup, demande de permission, téléchargement et redirection vers un schéma non autorisé ; confirmer le blocage/annulation.
- [ ] Provoquer un échec déterministe de chargement `http(s)`, vérifier le message accessible puis reprendre avec une autre URL.
- [ ] Inspecter le bouton de test et un autre élément ayant le focus ; vérifier que seules des propriétés DOM bornées sont rendues comme texte.
- [ ] Utiliser le clavier seul dans les deux panneaux, passer entre eux par les boutons visibles et `Ctrl+Maj+A`, et vérifier que les touches ordinaires de la page restent utilisables.
- [ ] Choisir chaque action sur l’observation synthétique et chaque résultat humain. Vérifier qu’une justification est réellement exigée pour « Non applicable » et « Non vérifiable ».
- [ ] Répéter les vérifications clavier et lecteur d’écran avec NVDA ; consigner versions et résultats. Ces essais **n’ont pas encore été exécutés**.
- [ ] Créer les dossiers A et B, enregistrer une décision synthétique dans A, ouvrir B et vérifier qu’il reste intact ; fermer puis relancer pour rouvrir A.
- [ ] Vérifier qu’une décision non enregistrée reste temporaire, qu’une revue ne se termine que par l’action explicite, et qu’un changement de résultat remet la revue en cours.
- [ ] Confirmer qu’une page distante ne peut pas être associée à un dossier sauvegardé dans ce prototype ; seule la fixture locale est acceptée pour l’écriture.
- [ ] Fermer l’application puis supprimer `user-data/audits/` pour effacer les seuls dossiers synthétiques enregistrés.

## Périmètre et limites explicites

Le « bouton sans nom » est une **observation automatique synthétique** codée en dur, pas un moteur de règles ni un constat inféré. Sa disposition (`unreviewed`, `confirmed`, `rejected`, `needs-follow-up`) est distincte du résultat humain (Conforme, Non conforme, Non applicable, Non vérifiable) et de l’état de revue. Les résultats « Non applicable » et « Non vérifiable » exigent une justification. Une modification du résultat remet la revue à `in-progress` ; la fin de revue demande une action explicite. Seule une clé de fixture (`fixture://synthetic-page`) peut être associée à un dossier ; aucune URL distante, cookie ni jeton n’est sauvegardé. Les dossiers JSON versionnés sont sauvegardés par fichier temporaire puis renommage dans `user-data/audits/` et isolés par ID UUID. Une instance du prototype est autorisée à la fois. Il n’y a ni preuve, historique, rapport ou export. L’inspection extrait un petit ensemble fixe de propriétés DOM ; elle ne lit pas l’arbre d’accessibilité et n’évalue pas complètement la page. La cible est non fiable et cette tranche ne prétend pas fournir le durcissement d’un navigateur de production.

La couverture du protocole `../../../docs/architecture/SPIKE-PROTOCOL.md` est initiale : le lancement, la navigation et les interactions réelles, les vérifications clavier/NVDA/visuelles/plateforme, les autres candidats, le packaging multi-plateforme et la revue QA restent **UNKNOWN**. Les tests automatisés et vérifications statiques ne démontrent pas le comportement interactif de l’interface. Ce spike n’est ni une recommandation de stack ni une validation du gate d’architecture.
