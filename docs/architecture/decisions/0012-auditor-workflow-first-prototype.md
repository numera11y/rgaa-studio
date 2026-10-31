# ADR 0012 — Prioriser un prototype centré sur le parcours auditeur

- **Statut :** recommandé par le conseil ; aligné sur le parcours métier exprimé par l’utilisateur ; l’architecture finale reste non choisie.
- **Date :** 29 octobre 2026
- **Décideur :** propriétaire produit, avec le conseil spécialisé

## Contexte

L’utilisateur a indiqué ne pas comprendre la logique du prototype actuel et a précisé le logiciel attendu : à l’ouverture, reprendre un audit en cours ou en créer un ; configurer URL et référentiel (avec cible A/AA/AAA explicite pour WCAG) ; ajouter au lot des pages importantes/obligatoires existantes sur le site ; examiner chaque page et pointer un élément pour consulter les détails et règles pertinentes ; auditer ces règles et conserver l’élément/état ainsi qu’une capture comme preuves.

Le prototype Electron existant est centré sur un dossier synthétique, une observation artificielle et une inspection DOM limitée. Il valide quelques questions techniques de démarrage et de persistance, mais **ne représente pas le parcours métier attendu**. Il ne démontre ni la sélection à la souris, ni la proposition de pages existantes, ni un vrai catalogue de règles, ni le rattachement de captures à un audit réel. Il ne faut pas le présenter comme une version même partielle de l’application métier.

Après l’avoir essayé, l’utilisateur a qualifié l’interface actuelle de catastrophique et très éloignée d’une interface logicielle satisfaisante. Ce retour confirme que l’expérience présentée est rejetée, mais ne permet pas à lui seul de conclure si son objection vise Electron comme runtime ou seulement le panneau expérimental rudimentaire. Aucune décision de moteur ne doit être déduite de ce commentaire ; la prochaine UI doit être conçue comme une vraie expérience métier avant toute comparaison visuelle de shells.

Une première maquette navigable indépendante du shell existe désormais dans `spikes/workflow-ui/`. L’utilisateur a choisi la piste « rail d’annotation » pour cette maquette parmi trois directions. Ce choix n’approouve ni une identité permanente ni un moteur. Les smoke checks Edge couvrent le parcours synthétique ; les tests clavier/lecteur d’écran manuels restent à faire.

Les contraintes déjà confirmées restent applicables : un référentiel/version par audit ; niveau WCAG cible distinct du résultat ; sélection explicite des pages ; résultat final décidé par l’auditeur ; contrôles automatisés distingués des conclusions humaines ; audit et preuves locaux, profils navigateur séparés ; interface française et utilisable au clavier.

## Recommandation

**Réorienter le prochain prototype vers une tranche métier de bout en bout**, et cesser d’ajouter des fonctionnalités au panneau de stockage/inspection isolé. Le shell Electron actuel peut servir provisoirement de banc pour la tranche, sans que le moteur ou le framework soit retenu pour v1.0.0.

La démonstration doit suivre cet ordre :

1. **Accueil** — afficher des audits de démonstration en cours à reprendre et proposer de créer un audit.
2. **Création** — saisir URL, référentiel et version ; pour WCAG, demander un choix explicite A/AA/AAA sans valeur par défaut.
3. **Pages candidates** — proposer un petit nombre de pages importantes repérées dans un site de test et vérifiées comme existantes ; permettre l’ajout manuel ; demander confirmation avant chaque inclusion. Indiquer que le lot n’est pas exhaustif.
4. **Audit page par page** — ouvrir individuellement chaque page confirmée dans le navigateur et conserver visible le contexte de l’audit.
5. **Examen par élément ou par règle** — permettre de pointer un élément et de voir les informations disponibles ainsi que des règles candidates ; offrir aussi une voie directe par règle pour les contrôles page entière, parcours, clavier ou technologies d’assistance.
6. **Décision et preuve** — montrer les observations automatiques comme aides ; enregistrer un résultat humain distinct et relier au dossier/page/règle une observation d’élément/état et la capture d’écran prévue par l’attente utilisateur.
7. **Reprise** — fermer puis rouvrir un dossier synthétique et retrouver les pages, décisions, preuves et leur contexte sans mélange entre audits.

Employer d’abord des fixtures et des règles de démonstration clairement marquées. Tout mécanisme simulé (découverte, pertinence de règles, arbre d’accessibilité) doit être annoncé comme tel ; aucune simulation ne peut être présentée comme un résultat réel ou une conformité du site.

## Invariants UX et modèle métier

- La reprise et la création sont les deux actions principales à l’ouverture.
- L’auditeur confirme les pages candidates ; la liste n’est jamais présentée comme la liste complète ou comme un crawl exhaustif.
- La sélection à la souris accélère l’inspection mais n’est pas le seul moyen de commencer ou terminer un contrôle.
- Les liens audit↔page↔élément/état↔règle↔décision↔preuve sont explicites et permettent des relations plusieurs-à-plusieurs ; une règle peut concerner la page ou un parcours entier.
- Ni un élément pointé, ni un résultat automatique, ni une capture ne produit à lui seul une conclusion de conformité.
- La page cible et le panneau d’audit sont distincts ; le parcours essentiel reste praticable au clavier et avec technologie d’assistance.
- Les preuves restent dans le dossier d’audit ; cookies, jetons et profil navigateur ne sont jamais copiés dans la preuve.

## Critères d’acceptation du prototype

1. L’auditeur peut choisir un audit existant et reprendre son dernier contexte, ou créer un nouveau dossier.
2. La création présente référentiel/version et impose la cible WCAG si WCAG est choisi.
3. La liste de pages candidates montre des pages présentes dans la fixture contrôlée ; l’auditeur choisit explicitement les pages à ajouter et peut ajouter une URL manuellement.
4. Les pages retenues sont ouvertes individuellement ; les décisions et preuves se rattachent à la bonne page du bon audit.
5. Un élément de fixture peut être pointé ; l’interface affiche les informations disponibles et des règles pertinentes, clairement présentées comme suggestions si elles ne sont pas normativement validées.
6. Une règle page-level peut être examinée sans sélection d’élément.
7. L’auditeur saisit une décision humaine distincte de l’observation automatique et de l’état de revue.
8. Une preuve d’élément/état et une capture sont rattachées à l’audit ; leur consultation après fermeture/réouverture retrouve le bon contexte.
9. Le parcours inclut les transitions clavier entre page et panneau, avec tests d’accessibilité manuels requis avant d’accepter le prototype.

## Points ouverts — ne pas les transformer en décisions silencieuses

- Le sens fonctionnel de « pages obligatoires » et les catégories initiales attendues ; le mécanisme de découverte (liens, sitemap, liste métier, saisie/import) doit éviter toute promesse d’exhaustivité.
- Le traitement des pages redirigées, protégées, inexistantes, dupliquées ou dont l’accès échoue.
- La portée exacte des informations montrées pour un élément : DOM, nom accessible, état dynamique, arbre d’accessibilité, limites de fiabilité et association aux règles.
- L’utilisateur attend une capture d’écran associée à l’élément/état ; reste à confirmer si elle doit être créée automatiquement et conservée pour chaque observation, ou si l’auditeur peut la reprendre/omettre avant conservation/export. Les captures peuvent contenir des données sensibles.
- La forme de référence d’un élément lorsque le DOM change ; une sélection ponctuelle ne garantit pas de retrouver durablement le même nœud.
- La méthode officielle d’applicabilité et d’agrégation des tests RGAA, toujours à valider avant d’afficher une suggestion calculée.
- Les essais avec auditeurs et technologies d’assistance ; aucune validation de ce prototype n’est réputée réalisée par son implémentation.

## Conséquences et validation

- La prochaine tranche est pilotée par le workflow visible, non par l’architecture de stockage ou l’inspection DOM prise isolément.
- L’ancien code Electron demeure un prototype technique réutilisable ou remplaçable ; il ne définit ni le modèle métier final ni l’UI de production.
- La découverte de pages et l’inspection d’éléments sont des capacités à prototyper et vérifier ; garder le moteur, les règles de sélection et les dépendances d’inspection remplaçables.
- Faire une revue produit/UX des écrans, puis un spike technique sur sélection souris, contexte DOM/état, captures et conservation locale. Tester séparément la navigation clavier et le lecteur d’écran.
- Les preuves visibles dans un prototype doivent être synthétiques. Avant tout usage réel, définir l’aperçu, l’omission/masquage, la conservation et les protections des captures conformément aux ADR 0005/0008.
- Cet ADR ne choisit pas un moteur, ne constitue pas un avis juridique et n’autorise pas la publication d’une version.

## Consultation du conseil

- **Product-strategist :** recommande le flux de bout en bout et une découverte indicative ; confiance élevée sur le parcours, moyenne sur discovery/sélection.
- **Auditor-ux :** recommande reprise/création, confirmation du lot, voies élément/état et règle/page, captures utiles mais non bloquantes pour une décision, et tests clavier/lecteur d’écran ; les comportements restent à tester.
- **Browser-architecture :** juge le parcours plausible, mais précise que le prototype actuel ne prouve pas la sélection souris, le lien page/règle/preuve, les profils réels ni le packaging ; confiance moyenne pour la faisabilité globale avant spikes.
- **Opensource-steward :** recommande local-first, profils séparés et protections des captures pouvant contenir des données sensibles ; les dépendances/licences de capture et de moteur restent à vérifier ; confiance moyenne.
- **Decision-facilitator :** recommande de retenir le parcours métier mais de construire un prototype UX réellement complet avant de choisir un moteur ; aucun arbitrage utilisateur supplémentaire n’est nécessaire pour la tranche proposée.

Ces positions sont des recommandations reçues durant la session, pas une approbation de chaque détail d’implémentation par l’utilisateur.
