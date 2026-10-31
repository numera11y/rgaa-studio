# Spécifications d’architecture — plateforme d’audit d’accessibilité

**Statut :** document de cadrage initial — les choix techniques restent à confirmer.
**Version :** 0.1

**Retour utilisateur sur l’interface :** le panneau du spike Electron est jugé inacceptable comme interface logicielle ; il reste un banc technique rudimentaire et ne choisit pas le runtime final. La maquette [`spikes/workflow-ui/`](../../spikes/workflow-ui/README.md) valide une référence de parcours, mais n’est pas l’interface de livraison voulue.

**Direction Windows confirmée :** application desktop frameless avec la barre supérieure de la capture utilisateur : menu hamburger/options, accès projets, onglets fermables/réordonnables, plus/nouveau, indicateur de mise à jour, bascule du panneau latéral, commandes réduire/agrandir/fermer. Aucun logo d’application. Dans la liste des dossiers, l’accès projets doit être marqué actif ; les dossiers doivent se présenter comme des blocs avec état de survol. [ADR 0013](decisions/0013-windows-native-shell-and-store-scope.md) consigne les exigences et les candidats techniques, sans choix final. La préférence actuelle de distribution est build/install communautaire ; Store n’est pas un gate pour le moment. Linux demeure dans le périmètre v1 confirmé.

**Interaction de la liste confirmée :** chaque dossier est une ligne complète activable au clic, Entrée ou Espace ; aucun bouton/texte « Reprendre » n’est ajouté à la ligne. L’icône Projets reste persistamment active dans la vue liste ; survol et focus restent visibles. Les informations principales exploitent une largeur centrée : site, référentiel/version, dernière mise à jour. [ADR 0016](decisions/0016-audit-list-cards-and-active-project-state.md).

**Modèle des onglets confirmé :** l’utilisateur confirme qu’un projet est un dossier d’audit pour un site et un référentiel ; l’icône ouvre la liste et chaque onglet représente un dossier. Aucun conteneur supérieur n’est prévu en v1. [ADR 0014](decisions/0014-project-tabs-and-audit-dossiers.md).

**Favicon du spike :** option visible et cochée par défaut à la création, opt-out, requête HTTPS indépendante sur `/favicon.ico`, DNS public épinglé à la connexion, limites de transfert/décodage, PNG local et fallback globe. Cette métadonnée n’est pas une preuve et n’actualise pas l’audit. WebView2 demeure strictement fixture-only ; aucun support de navigation réelle n’est impliqué. Voir [ADR 0015](decisions/0015-project-favicon-retrieval-sequencing.md).

**État du spike natif :** une preuve WPF/.NET + WebView2 est disponible dans `spikes/windows/wpf-native-spike/`. Le build .NET 10 réussit (0 avertissement/erreur) et l’audit NuGet ne signale aucun package vulnérable. Le prototype utilise uniquement une fixture synthétique locale ; quelques smoke tests clavier/UI Automation sont passés, mais le clavier complet, Narrateur/NVDA, les vrais sites, Linux et l’installation propre ne sont pas validés. Il n’est pas Store-ready et ne constitue pas un choix d’architecture ni une implémentation produit.

**État de la tranche native :** l’application Avalonia crée et reprend des dossiers persistés, ouvre dans le WebView les pages HTTPS retenues dans le dossier, lance axe-core et propose la pipette de contraste. Une vérification UI Automation a créé deux dossiers synthétiques, relancé l’app et restauré une décision/preuve différente depuis chacun ; `verify-audit-store.ps1` contrôle manifestes, événements et captures. La découverte propose des pages de la page d’accueil et du sitemap, sans prétendre être exhaustive ; seuls les URL sélectionnés du même site sont navigables. L’inspection par pointeur demeure limitée à la fixture locale. Clavier complet, lecteurs d’écran, authentification, isolation de profil, Linux et installation restent à valider.

**Décision Avalonia (30 octobre 2026) :** l’utilisateur choisit Avalonia comme application desktop principale Windows/Linux après validation visuelle du shell et du dropdown. L’application est intégrée sous `src/RgaaStudio.Desktop/`; le modèle de dossiers et sa persistance sont dans `src/RgaaStudio.Core/`. `Avalonia.Controls.WebView` ouvre les pages HTTPS ajoutées au dossier d’audit, et refuse les destinations hors périmètre ou hors origine. axe-core et la pipette fonctionnent sur les pages retenues ; l’inspection d’éléments par pointeur reste limitée à la fixture. Les prérequis/installateurs Linux et les parcours avec lecteur d’écran restent à valider. Le spike WPF reste une référence de migration temporaire, pas une deuxième interface produit. Voir [ADR 0018](decisions/0018-avalonia-desktop-application.md).

**Contrat de la planche des états et implémentation WPF (30 octobre 2026) :** la planche `docs/_sources/images/RGAA Studio · États de la barre et du menu.svg` est la référence stricte, tracée par [ADR 0017](decisions/0017-toolbar-menu-and-browser-chrome-states.md). Tous les boutons partagent la tuile 20×20 DIP et la palette exacte du frame : blanc/contour `#7B8190` au repos, `#E9EFF9` au survol, `#D5E1F5` avec contour `#214B9A` à l’appui, contour focus `#214B9A` 2 DIP. Le burger ouvert est plein `#214B9A` avec glyph/soulignement blancs ; menu déroulant sans icônes. Les hit targets restent 40 DIP (46×44 fenêtres). Le spike affiche le badge d’inspection, le conserve à la désactivation, traite Échap menu-first, marque Projets actif, désactive les commandes inspection selon l’état, souligne l’onglet actif de 3 DIP, grise la chrome inactive et change le glyphe agrandir/restaurer. Le menu reçoit le focus sur la première commande ; Haut/Bas parcourt les commandes affichées en sautant séparateurs et indisponibles ; Entrée active une seule commande. Un smoke clavier synthétique (`SendKeys`) + UIA a vérifié Space, Haut/Bas (dont le saut de la commande désactivée), Entrée, F6, Échap dans le menu puis Échap hors menu ; le pointeur survolant une ligne ne vole pas le focus clavier. Builds Debug/Release, contrôles SVG/dossiers, 14 checks favicon et scan vulnérabilités passent. Perte réelle du premier plan pendant inspection, comparaison visuelle pixel à pixel, clavier physique non simulé et Narrateur/NVDA restent à vérifier ; ne pas revendiquer une conformité complète.

**Pipeline d’icônes SVG du spike :** la barre et la liste de pages utilisent les SVG Lucide fournis sous `docs/_sources/images/`; l’icône d’inspection réutilise `Assets/inspect.svg`, et l’icône Plus manquante a été ajoutée à la collection depuis Lucide. `SharpVectors.Wpf` 1.8.6 rend les ressources directement en WPF, avec tailles explicites de 12–18 DIPs pour le viewBox 24×24. Les avis Lucide et SharpVectors/Brotli/MinIoC sont inclus. Ce renderer est une dépendance du spike seulement, pas un choix final de moteur ou d’UI multiplateforme.

**Build Windows de test (30 octobre 2026) :** le spike peut être publié comme EXE x64 autonome côté runtime .NET avec `dotnet publish` Release/self-contained/single-file ; le dernier build est dans la sortie locale ignorée par Git `spikes/windows/wpf-native-spike/artifacts/design-system-states-20261030/`. L’EXE nécessite encore Microsoft Edge WebView2 Evergreen Runtime et le dossier voisin `Assets`. Ce n’est pas un installateur, le binaire n’est pas signé et le prototype reste fixture-only. La publication Release a réussi. Voir le README du spike.

**Étape suivante :** valider manuellement au clavier le shell complet, puis prouver un build/install communautaire Windows propre (dont installation WebView2 et parcours d’installation). Les checks favicon déterministes utilisent un handler HTTP et une résolution DNS simulés ; ils ne testent pas le service DNS, le TLS ni des hôtes réels. Aucune validation Narrateur/NVDA n’est revendiquée. Ces limites ne constituent pas une décision finale de framework ou moteur.

## 1. Objectif

Définir le parcours et les principes d’architecture d’un outil spécialisé dans l’audit de l’accessibilité de pages et de sites web selon le RGAA et/ou les WCAG. Le produit prend la forme d’une application dédiée qui intègre un navigateur conçu pour le travail d’audit. Une intégration à l’IDE pourra être envisagée ultérieurement pour relier les constats au code source.

## 2. Orientation produit

L’application doit guider l’auditeur de la création d’un audit jusqu’à la production du rapport, tout en lui permettant d’inspecter la page dans le navigateur intégré. Elle doit aider à combiner des vérifications automatisables et des contrôles manuels, à documenter les constats et à produire un rapport exploitable. Elle ne doit pas présenter les contrôles automatiques comme un audit complet : les résultats doivent distinguer clairement ce qui a été détecté automatiquement de ce qui nécessite une vérification humaine.

L’interface doit rendre explicite la progression de l’audit, les actions attendues et les éléments restant à vérifier, sans empêcher l’auditeur de naviguer librement dans la page.

### Contraintes produit confirmées

- **Utilisateurs visés en v1.0.0 :** auditeurs indépendants ou en agence, et équipes accessibilité d’un organisme. Aucun ordre de priorité entre ces profils n’est établi à ce stade.
- **Plateformes :** Windows et Linux.
- **Distribution :** application desktop installable sur Windows et Linux. L’utilisateur préfère pour le moment un build/install communautaire documenté ; la publication Store n’est pas un gate v1 actuel et peut être réévaluée plus tard.
- **Windows UI :** fenêtre frameless, menu moderne, icônes Lucide, barre à onglets de projets et contrôles système accessibles. Aucun logo d’application dans la barre. Le shell doit rester utilisable au clavier/lecteur d’écran et ne pas masquer les commandes OS.
- **Barre confirmée par l’utilisateur :** menu/options, accès projets, tabs fermables et réordonnables, plus/nouveau projet, update indicator, bascule du panneau latéral d’audit, réduire/agrandir/fermer. Voir [ADR 0013](decisions/0013-windows-native-shell-and-store-scope.md).
- **Modèle des tabs :** un projet correspond à un dossier d’audit pour un site et un référentiel ; l’icône projets ouvre la liste, chaque onglet représente un dossier. Voir [ADR 0014](decisions/0014-project-tabs-and-audit-dossiers.md).
- **Cibles Linux prioritaires :** Ubuntu 26.04 LTS et Fedora Workstation 44 sur x86-64 avec installation garantie ; Ubuntu 26.10 est testée sans garantie d’installation (confirmation explicite). Paquets `.deb` pour Ubuntu et `.rpm` pour Fedora sont confirmés comme formats de distribution à prototyper. Les versions devront être revérifiées avant v1.0.0. Sources : [Ubuntu Desktop Downloads](https://ubuntu.com/download/desktop), [Ubuntu Releases](https://releases.ubuntu.com/), [Fedora Workstation](https://fedoraproject.org/workstation/download).
- **Langue v1.0.0 :** interface et rapports en français.
- **Sorties souhaitées :** rapport PDF pour un client, document ODT modifiable pour une équipe et export JSON pour le suivi technique. L’utilisateur préfère ODT malgré l’usage fréquent de Word par les destinataires ; les tests de compatibilité avec Word et LibreOffice restent à définir. Le schéma JSON et les données à inclure restent à définir.
- **Contenu du rapport v1 confirmé :** client/site, périmètre des pages, référentiel/version, résumé des résultats, constats avec critères et preuves, limites des contrôles automatiques et vérifications restantes. L’ODT est une version modifiable du même rapport. Le client apparaît dans le PDF client ; il n’est pas nécessaire dans l’ODT/JSON.
- **Rapports d’étape :** l’auditeur peut exporter avant la fin des vérifications ; le rapport doit alors signaler clairement qu’il est incomplet et lister ce qui reste à faire. Les preuves sont sélectionnées explicitement avant inclusion.
- **Conservation des audits et preuves :** l’utilisateur veut pouvoir retrouver/revoir les preuves bien plus tard ; elles sont locales au dossier d’audit (notes, reproduction, captures, DOM), sélectionnées explicitement pour le PDF et sans secrets. Les profils navigateur sont stockés à part et ont un cycle de vie indépendant. Durée obligatoire inconnue et aucune expiration définie ; recommandation provisoire : pas de purge automatique, conservation jusqu’à suppression explicite, sans prétention légale. Voir ADR 0008.
- **Identification de l’audit :** un champ facultatif « Client / organisation » accompagne le site/domaine ; il sert à distinguer les audits et à contextualiser les alertes de réutilisation de profil, sans vérifier l’identité. Il apparaît dans le rapport PDF client lorsqu’il est renseigné ; il est exclu de l’ODT et du JSON suivant la préférence utilisateur. Voir ADR 0004.
- **Connexion :** les sites nécessitant une authentification doivent être auditables. Les profils locaux persistent indépendamment des audits et peuvent être explicitement assignés à plusieurs audits simultanément. L’utilisateur exige que chaque audit garde ses propres constats, preuves et historique ; les traces ne doivent jamais être mélangées. Le contrôle interactif concurrent est souhaité « autant que possible », mais doit être démontré par spike. Si le moteur ne le permet pas sûrement, l’utilisateur accepte le repli v1 : audits ouverts et assignés, mais un seul audit contrôle le navigateur partagé à la fois, avec transfert explicite. Un profil neuf est proposé par défaut, sans affectation automatique. Voir ADR 0003.
- **Périmètre de pages :** chaque audit est un dossier distinct concernant un site unique ; à la création, l’application recherche les pages prioritaires (accueil, contact, mentions légales, déclaration d’accessibilité, plan du site), puis au maximum 25 autres candidates. Le scan reste une aide non exhaustive ; chaque page doit être sélectionnée explicitement par l’auditeur avant son inclusion.
- **Référentiels :** RGAA et WCAG sont tous deux disponibles, mais chaque audit porte sur un seul référentiel. Chaque audit WCAG exige la sélection explicite d’un niveau cible A/AA/AAA à la création, sans niveau prérempli ; le niveau reste distinct du résultat par règle. À la vérification officielle du 28 octobre 2026, le conseil a identifié RGAA 4.1.2 comme version publiée actuellement affichée et WCAG 2.2 comme dernière recommandation W3C stable. Le RGAA 5 et toute version WCAG en brouillon ne sont pas des versions finales. Revalider avant v1.0.0.
- **Export structuré :** JSON est accepté comme format de départ. Jira est une intégration souhaitée mais non prioritaire : l’utilisateur préfère une connexion depuis l’application, sans service auxiliaire qui retarde le reste. Cloud/Data Center et l’authentification restent à étudier.

## 3. Parcours principal

1. **Reprendre ou créer.** À l’ouverture, l’auditeur voit ses audits en cours, les informations nécessaires pour les retrouver et une action de reprise ; il peut aussi créer un audit.
2. **Configurer l’audit.** Saisir l’URL du site, choisir un référentiel unique et sa version. Pour WCAG, choisir explicitement une cible A/AA/AAA, sans niveau prérempli ; la cible n’est pas un résultat de conformité.
3. **Constituer le lot de pages.** Après le choix du référentiel, analyser la page d’accueil et les sitemaps publics same-origin. Présenter d’abord les candidates prioritaires repérées (accueil, contact, mentions légales, déclaration d’accessibilité, plan du site), puis jusqu’à 25 autres candidates. L’auditeur sélectionne explicitement les pages à ajouter ; la classification est heuristique, les candidates prioritaires ne sont pas ajoutées automatiquement et la liste ne promet pas l’exhaustivité.
4. **Auditer une page à la fois.** Ouvrir chaque page choisie dans le navigateur intégré, en conservant visible son contexte et sa progression.
5. **Examiner par élément ou par règle.** L’auditeur peut pointer un élément, consulter ses informations disponibles et les règles susceptibles de le concerner ; il peut aussi partir directement d’une règle pour les contrôles qui portent sur toute une page, un parcours, le clavier ou une technologie d’assistance. Un élément n’est pas un résultat de conformité.
6. **Décider et conserver les preuves.** Pour chaque règle, distinguer les détections/aides automatiques, les observations, la décision humaine et l’état de revue. Une observation d’élément/état et une capture d’écran doivent pouvoir être rattachées au bon audit et à la bonne page ; le caractère systématique ou facultatif de chaque capture reste à préciser.
7. **Reprendre, suivre et rapporter.** Retrouver les pages, règles, décisions et preuves enregistrées après interruption. Consulter le travail restant et produire un rapport complet ou d’étape clairement incomplet.

## 4. MVP proposé

Le MVP proposé couvre un cas d’usage restreint dans une application dédiée avec navigateur intégré :

- saisir une URL, ouvrir la page dans le navigateur intégré et démarrer un audit sur une page ou un lot explicitement sélectionné de pages importantes ;
- afficher dès l’ouverture les audits en cours à reprendre et une action claire de création d’audit ;
- guider la création par URL, référentiel/version et cible WCAG explicite si nécessaire ;
- ajouter des pages au lot en saisissant une URL, en important une liste, en examinant des suggestions découvertes ou en ajoutant la page courante pendant la navigation ; faire confirmer chaque page par l’auditeur avant inclusion ;
- ouvrir les pages retenues individuellement ; permettre une revue démarrée soit depuis un élément/état pointé, soit directement depuis une règle/page ;
- afficher les informations observables de l’élément sélectionné et les règles candidates pertinentes sans prétendre déduire automatiquement la conformité ;
- s’authentifier sur les sites cibles et conserver la session entre les visites, selon des règles de sécurité, de confidentialité et d’effacement à définir ;
- lancer une première série de contrôles automatisables ;
- afficher les constats liés à la page et aux critères concernés ;
- associer une observation d’élément/état et, selon le choix à préciser, une capture d’écran au dossier d’audit, à la page et aux règles concernées ;
- permettre à l’auditeur de confirmer, rejeter ou compléter chaque constat ;
- conserver la distinction entre résultats automatiques et vérifications manuelles ;
- permettre à l’auditeur de consigner un résultat unique par critère pour le lot, dans un champ distinct de l’état de revue ; conserver l’applicabilité, les constats et les preuves par page ; terminer le critère lorsque l’applicabilité est déterminée sur chaque page et son résultat global explicitement confirmé ; les résultats non applicable/non vérifiable exigent une justification ;
- générer à tout moment un rapport d’étape ou un rapport après revue, avec état de complétion visible ;
- laisser l’auditeur sélectionner les preuves à inclure.

Le rapport doit restituer les éléments de contenu ci-dessus. Les détections automatiques et suggestions restent distinctes des résultats de conformité définis par l’auditeur ; seuls les résultats confirmés figurent comme tels dans PDF/ODT. Une suggestion RGAA non confirmée reste dans l’application ; un rapport d’étape indique que le critère reste à examiner, sans exporter la suggestion. Les limites et vérifications restantes doivent être visibles et ne pas être résumées comme une conformité établie. L’absence de preuve sélectionnée n’est pas une preuve d’absence de problème. L’état « Revue terminée » indique uniquement que la règle a été examinée ; son résultat peut être conforme ou non conforme. Voir [ADR 0005](decisions/0005-evidence-and-report-completion.md) et [ADR 0006](decisions/0006-review-and-conformity-status.md).

Le PDF client, l’ODT modifiable et le JSON structuré sont les sorties retenues par l’utilisateur. Le modèle d’audit canonique comme source de ces rendus reste une recommandation d’architecture. JSON ne garantit pas une intégration directe aux outils. L’utilisateur souhaite à terme une connexion Jira depuis l’application, mais précise que l’étude de cette fonction ne doit pas ralentir le cœur d’audit. Voir [ADR 0001](decisions/0001-v1-export-scope.md) et [ADR 0002](decisions/0002-jira-v1-scope.md).

Les trois sorties sont recommandées comme rendus d’un même audit canonique ; elles ne doivent pas constituer trois sources de vérité distinctes. Jira est une intégration candidate, non prioritaire et non bloquante pour la livraison v1.0.0. Le cœur d’audit doit rester complet sans Jira.

La v1 ne comprend pas de gravité ni de priorité de correction : une non-conformité n’indique pas en soi l’urgence. Ces dimensions restent distinctes du résultat et de l’état de revue. Réévaluer après essais utilisateurs avant d’envisager un champ facultatif. Voir [ADR 0007](decisions/0007-severity-and-priority-v1.md).

L’exploration complète d’un site, les fonctionnalités avancées de collaboration et l’intégration à un IDE sont hors du périmètre initial, mais devront pouvoir s’ajouter sans remettre en cause le modèle des audits et des constats.

## 5. Principes d’architecture

### Intégration Jira — étude non bloquante

L’utilisateur souhaite à terme une connexion directe depuis l’application au Jira du client ou au sien, mais confirme que ce n’est pas prioritaire et ne doit pas ralentir le reste. L’ADR 0002 recommande de garder le cœur d’audit entièrement utilisable sans Jira, d’étudier l’intégration en parallèle et de ne l’ajouter à la v1.0.0 que si faisable sans retarder la livraison ; sinon elle sera reportée. L’utilisateur accepte la lecture du statut Jira à la demande ; le retest demeure humain et son résultat est historisé dans l’audit. Les modalités de création et de dédoublonnage restent à étudier. Aucun service auxiliaire n’est prévu. Cloud/Data Center et l’authentification restent ouverts.

### 5.1 Application d’audit avec navigateur intégré

L’audit porte sur une page rendue et interactive, et pas seulement sur les fichiers de son projet. L’application intègre un navigateur qui permet à l’auditeur de consulter la cible tout en accédant au contexte d’audit : critères, contrôles, constats, preuves et progression. L’architecture doit permettre d’associer les observations à une URL et, lorsque possible, à des éléments de la page.

L’interface doit articuler clairement au minimum deux espaces : la page web auditée et le panneau de travail de l’auditeur. Le parcours doit préserver l’accès à la page pendant la consultation et la saisie des résultats. Les modalités techniques d’intégration du moteur de navigateur, d’inspection et d’isolation restent à définir.

Le parcours métier recommandé à la suite de la clarification utilisateur commence par la reprise des audits en cours ou la création d’un audit, puis conduit page par page à l’inspection facultative d’éléments/états, à l’examen des règles pertinentes, à la décision humaine et à l’attachement des preuves (dont capture d’écran). Cette navigation élément→règle est une voie principale, pas une contrainte exclusive : les règles ne se réduisent pas toutes à un élément DOM et les vérifications clavier, lecteur d’écran ou page entière doivent rester possibles. Les pages proposées sont des candidates, jamais une promesse d’exhaustivité, et sont ajoutées au périmètre avec confirmation explicite. Voir [ADR 0011](decisions/0011-audit-workflow-clarification.md).

### 5.2 Résultats automatiques et jugement humain

Chaque résultat doit indiquer son origine et son état. Une détection automatique est une aide à l’audit ; elle ne vaut pas conclusion de conformité. L’auditeur consigne un résultat de conformité distinct de l’état de revue, et peut confirmer, rejeter ou enrichir les résultats automatisés. L’utilisateur a validé Conforme, Non conforme, Non applicable (avec justification) et Non vérifiable (avec justification), sans « Partiellement conforme ». Le résultat est unique par critère pour le lot ; les tests, constats, preuves et états d’applicabilité restent associés aux pages concernées. L’utilisateur préfère une suggestion calculée mais non enregistrée, que l’auditeur confirme ou modifie. Recommandation : la calculer au niveau critère/lot uniquement si couverture et applicabilité des tests évalués sur toutes les pages sont complètes et non ambiguës ; sinon ne rien suggérer. Les détections automatiques brutes ne suffisent pas. Une suggestion ne certifie pas la conformité et ne termine pas la revue. Les rapports présentent le résultat de l’auditeur, pas une suggestion non confirmée (recommandation à confirmer). Voir [ADR 0006](decisions/0006-review-and-conformity-status.md).

### 5.3 Référentiels et traçabilité

Les critères et leurs correspondances doivent être représentés de manière à pouvoir associer un constat à un référentiel et à un critère précis. Les versions des référentiels et des règles de contrôle devront être traçables dans les audits et les rapports.

Les audits doivent pouvoir utiliser le RGAA et les WCAG. Pour le cadrage daté du 28 octobre 2026, les versions proposées sont RGAA 4.1.2 et WCAG 2.2, identifiées comme versions publiées sur les sources officielles par le conseil. Un audit doit figer les versions de référentiel et de règles utilisées ; une mise à jour ne doit pas modifier rétroactivement un audit existant. Revalider les versions officielles avant le gel des règles et la publication v1.0.0.

### 5.4 Préparation à l’intégration au code

Les constats devraient conserver, lorsque disponible, suffisamment de contexte technique pour permettre ultérieurement un lien vers le code source. Un plugin IntelliJ ou d’autres intégrations de développement sont une évolution possible, et non une dépendance du MVP.

### 5.5 Sessions authentifiées et données locales

La session de navigation doit pouvoir être conservée entre les visites pour permettre l’audit de sites authentifiés. L’utilisateur confirme la persistance indépendante des audits et l’assignation simultanée d’un profil à plusieurs audits ; il accepte qu’un seul audit contrôle le navigateur à la fois si la concurrence interactive n’est pas validée. Les profils sont locaux à l’installation/au compte OS et distincts du dossier canonique ; cloud/serveur non requis. Invariants : données d’audit (constats, preuves, historique) isolées ; identité de profil partagé visible ; aucune affectation automatique ; description honnête de l’état de cookies/session et des limites de concurrence. Plusieurs audits assignés ne signifient pas à eux seuls plusieurs sessions indépendantes : les vues peuvent partager cookies et état serveur. Cible technique : plusieurs vues interactives si le moteur le permet sans confusion ; sinon propriétaire unique avec transfert explicite et avertissement/blocage de l’autre vue. Cookies, jetons et données de profil sont exclus des rapports, exports, preuves et journaux. Un compte OS partagé n’est pas une frontière de sécurité. Voir [ADR 0003](decisions/0003-browser-profile-lifecycle.md).

### 5.6 Intégration Jira — confiance, API et séparation des sources

L’audit local est la source de vérité des constats, preuves et vérifications de l’auditeur ; Jira est la source de vérité des tickets et de leur workflow. Une liaison vers une issue Jira ne transforme pas son statut « terminé/résolu » en preuve que la page est corrigée. Après correction, l’auditeur doit ouvrir la page dans le profil de navigation cible, effectuer une nouvelle vérification et enregistrer un événement daté dans l’historique de l’audit.

Avant création, le connecteur doit montrer des issues candidates et permettre de lier un ticket existant ou de prévisualiser puis confirmer la création. La recherche aide à réduire les doublons, sans garantir une déduplication exhaustive. Les secrets Jira sont séparés du profil du site audité et exclus des rapports, preuves et journaux. La cible d’édition et le flux d’autorisation restent à décider et à prototyper.

La documentation officielle Atlassian [OAuth 2.0 (3LO)](https://developer.atlassian.com/cloud/jira/platform/oauth-2-3lo-apps/) recommande une application distribuée unique, déconseille de collecter les API tokens des utilisateurs et signale des limites pour les applications autonomes. Un spike d’authentification est nécessaire avant toute promesse Cloud ; son échec reporte uniquement Jira, pas l’application d’audit. Aucune solution OAuth n’est choisie.

## 6. Concepts métier initiaux

- **Audit** : dossier distinct pour un site unique, couvrant une ou plusieurs pages sélectionnées, associé à un seul référentiel et sa version exacte (et à un niveau WCAG cible si WCAG est choisi), ainsi qu’à zéro ou un profil local explicitement affecté ; plusieurs audits peuvent être assignés au même profil simultanément.
- **Page auditée** : cible incluse dans le périmètre, identifiée par une URL finale et, lorsque nécessaire, par un contexte/état de navigation au-delà de l’URL seule.
- **Observation d’élément/état** : contexte recueilli lorsque l’auditeur pointe un élément ou examine un état de page ; sa référence DOM est indicative et peut devenir fragile si la page change. Elle peut être associée à plusieurs règles, comme une règle à plusieurs observations ; elle ne remplace pas une décision humaine.
- **Vérification** : contrôle automatique ou manuel rattaché à un critère.
- **Constat** : résultat observé, avec son origine, son statut, ses preuves et les informations de correction.
- **Preuve** : enregistrement local du dossier d’audit, lié à une page et au constat/critère concerné, avec contexte de capture disponible. Une preuve ne garantit ni authenticité certifiée ni reproduction future du site. Cycle de conservation provisoire distinct du profil navigateur ; voir ADR 0008.
- **Révérification** : observation ultérieure d’un constat après correction, datée et ajoutée à l’historique sans effacer le constat initial.
- **Liaison Jira** : association explicite d’un constat à une issue sur une instance/projet Jira, sans fusionner les statuts Jira et ceux de l’audit.
 - **Profil navigateur** : espace local persistant contenant notamment les sessions d’authentification ; sa durée de vie est indépendante des audits et il peut être explicitement affecté à plusieurs audits simultanément. La concurrence de vues/contrôles dépend du moteur : un profil partagé peut partager cookies et état de session. Il ne constitue pas une frontière de sécurité entre utilisateurs OS.
- **Client / organisation** : libellé texte facultatif porté par les métadonnées d’un audit, distinct du domaine et du profil navigateur. Il sert à identifier le dossier et à contextualiser une confirmation de réutilisation, mais ne vérifie aucune identité. Inclus dans le rapport PDF client s’il est renseigné ; exclu de l’ODT, du JSON et des journaux. Voir [ADR 0004](decisions/0004-audit-client-label.md).
- **Rapport** : restitution partageable des résultats d’un audit.

Ces concepts constituent une première base et devront être précisés avant la conception détaillée du modèle de données.

## 7. Questions à instruire

- Quelles versions de moteurs/dépendances et quelle cadence de mises à jour garantir sur les cibles Linux principales ?
- Comment l’application partage-t-elle le contexte entre la page affichée et les outils d’audit (URL, sélection d’élément, état de navigation) ?
- Comment l’interface identifie-t-elle le client/site et présente-t-elle la confirmation avant réutilisation d’un profil entre audits qui concernent des clients différents ? L’exigence de confirmation et de traces d’audit séparées est confirmée ; le contenu exact reste à concevoir.
- Comment enrichir la découverte des pages prioritaires, notamment pour les pages absentes des liens publics ou protégées, et comment permettre ensuite l’ajout manuel/import sans dépasser la limite de 25 autres pages ?
- Pour chaque élément/état pointé, la capture d’écran est-elle créée et conservée automatiquement comme preuve obligatoire, ou l’auditeur peut-il la reprendre/omettre ? La capture reste distincte de la sélection des preuves à exporter et peut contenir des données sensibles.
- Quelles informations détaillées de l’élément sont utiles et techniquement fiables (DOM, nom accessible, état, arbre d’accessibilité) ? Quel lien page/élément/règle/preuve reste assez compréhensible si la page change ? Voir ADR 0011/0012 ; valider par spike et essais auditeurs.
- Comment gérer les redirections, nouvelles fenêtres et téléchargements durant un audit authentifié ?
- Comment garantir une expérience claire et utilisable au clavier dans l’application elle-même, y compris lorsque l’auditeur interagit avec la page cible ?
- Comment authentifier et explorer les sites nécessitant une connexion ?
- Quels mécanismes de collecte de preuves sont nécessaires (capture d’écran, élément DOM, extrait de texte, parcours) ?
- Quels critères RGAA/WCAG et quelles règles automatisées entrent dans la première version ?
- Quelle implémentation détaillée de la suggestion RGAA satisfait les règles officielles d’applicabilité et d’agrégation pour le catalogue v1 ? Vérifier contre la version retenue avant implémentation.
- Comment permettre à l’auditeur de créer deux audits distincts pour le même site, chacun sous RGAA ou WCAG, sans mélanger constats, sessions ou rapports ?
- Quelle édition Jira (Cloud/Data Center), quelles versions/instances et quel flux d’authentification sont visés ? Quel degré de consultation du ticket est requis après sa création ?
- Quelle règle de rapprochement des doublons est attendue, et un constat peut-il être lié à plusieurs tickets ?
- Quels tests d’édition et de rendu ODT dans Word et LibreOffice sont nécessaires ?
- Quel libellé exact doit distinguer un rapport d’étape incomplet d’un rapport dont la revue est terminée ?
- L’ODT reprend-il les mêmes preuves sélectionnées que le PDF (recommandation actuelle) ?
- Quel schéma JSON et quelles données répondent au workflow de ticket, et quelles preuves peuvent être envoyées ?
- Quelle durée de conservation légale/contractuelle s’applique aux dossiers et preuves selon les usages/juridictions visés ? À instruire par revue juridique avant de déclarer une conformité ; voir ADR 0008. La recommandation provisoire sans expiration automatique n’est pas une réponse légale.

## 8. Évolutions envisagées

- crawl récursif/exhaustif et exploration automatisée avancée au-delà des suggestions de pages parcourues ;
- amélioration des parcours guidés et de la progression de l’auditeur ;
- enrichissement des contrôles manuels et des preuves ;
- suivi des audits et collaboration ;
- liaison des constats au code source ;
- plugin IntelliJ pour retrouver des constats dans le projet et assister leur correction.
