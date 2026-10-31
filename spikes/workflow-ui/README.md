# Prototype UX du parcours d’audit (indépendant du shell)

Ce prototype est une maquette navigable du logiciel métier attendu : reprise/création d’audit, configuration, lot de pages candidates, audit page par page, inspection d’élément/règle, décision humaine et preuve. Il a été conçu sans dépendre d’Electron, Tauri ou d’un moteur navigateur final. Il sert à juger la logique et l’interface, pas à auditer un vrai site.

## Lancer

Depuis ce répertoire, dans PowerShell :

```powershell
python -m http.server 4173 --bind 127.0.0.1
```

Ouvrir `http://127.0.0.1:4173/` dans un navigateur. Les données de démonstration sont sauvegardées dans le stockage local du navigateur. Pour réinitialiser la maquette, supprimer la clé `rgaa-workflow-prototype-v1` dans les outils de développement du navigateur, ou utiliser un profil de navigateur vierge.

Vérifications : `node --check app.js`, `node smoke.cjs` (structure/labels) et, sous Windows avec Edge installé, `node smoke-browser.cjs` (flux navigateur automatisé de bout en bout).

**Dernière exécution locale :** syntaxe JS valide ; `node smoke.cjs` passe ; le test Edge passe sur reprise/création, niveau WCAG explicite, confirmation de pages, pointage et sélection clavier, décisions par règle, fin de revue distincte, preuve simulée sauvegardée/réouverte. Les captures de revue à 1440×900 sont sous `.impeccable/review/`. Aucun essai clavier/NVDA manuel n’a encore été effectué.

Pour inspecter directement chaque écran dans un navigateur (sans suivre les clics), utiliser `?screen=setup`, `?screen=pages` ou `?screen=workspace`, par exemple `http://127.0.0.1:4173/?screen=workspace`.

## Ce que le prototype permet de parcourir

- reprendre l’audit synthétique déjà présent ou créer un audit ;
- saisir l’URL, choisir RGAA/WCAG et version, sélectionner explicitement A/AA/AAA si WCAG ;
- choisir des pages de démonstration déjà signalées comme candidates ou ajouter une URL du domaine synthétique ;
- ouvrir les pages retenues séparément ;
- pointer un élément dans la page simulée ou le sélectionner après y avoir placé le focus ;
- voir des règles candidates pour l’élément et des règles portant sur la page entière ;
- enregistrer un résultat humain par règle et terminer séparément la revue de page ;
- ajouter une preuve de démonstration liée à l’élément.
- rouvrir une preuve pour relire sa page, son URL et sa note synthétique.

## Limites explicites

- Tous les sites, pages, règles et constats sont synthétiques ; la maquette refuse les domaines autres que `exemple.test`.
- La découverte des pages est une liste codée en dur de démonstration, pas une vérification réelle du site ni un crawl.
- La zone centrale est une page simulée dans le même document, pas un navigateur intégré ; le pointeur et les informations DOM ne prouvent aucune capacité Electron/Tauri.
- Le bouton de capture n’enregistre pas une image réelle : la preuve est étiquetée « capture simulée ». La vraie capture et sa politique obligatoire/facultative restent à construire.
- Les suggestions de règles sont des exemples, pas le catalogue normatif ni une décision de conformité.
- Le `localStorage` est uniquement une commodité de maquette et ne constitue ni une sauvegarde durable, ni un stockage sécurisé, ni le dossier canonique prévu.
- Aucune connexion réelle, profil navigateur persistant, export, collaboration ou audit d’un site externe n’est implémenté.

## Essais de parcours

1. Reprendre l’audit d’exemple puis revenir à la liste.
2. Créer un audit WCAG et vérifier que la cible n’est jamais pré-sélectionnée ; refaire avec RGAA.
3. Confirmer seulement deux pages candidates, puis examiner chacune dans l’espace d’audit.
4. Pointer un bouton et un champ ; utiliser aussi « Élément au focus » au clavier.
5. Ouvrir une règle élémentaire et une règle page entière, enregistrer une décision, puis observer que la détection ne la remplace pas.
6. Tester justification requise pour « Non applicable » et « Non vérifiable ».
7. Ajouter une preuve simulée, changer de page et d’audit, puis revenir ; vérifier le rattachement.
8. Parcourir le flux entier au clavier et avec un lecteur d’écran ; consigner les défauts avant d’accepter l’UX.

Les captures automatisées de revue sont sous `../../.impeccable/review/` ; elles contrôlent la disposition seulement, pas l’usage au clavier ou au lecteur d’écran. La première passe du détecteur Impeccable a relevé du texte trop petit, un pictogramme empilé au-dessus d’un titre et un manque d’inset ; la plupart des tailles et le titre ont été corrigés en gardant la palette choisie. Le détecteur n’a été lancé qu’une fois ; une nouvelle passe finale n’est pas encore faite.

## Direction visuelle

Le concept choisi est le **rail d’annotation** : une zone de page clairement visible, une navigation de périmètre persistante et un panneau latéral qui relie élément/état, règle, décision et preuve. La palette ivoire, encre et vermillon est un langage de travail de ce prototype, pas une identité finale approuvée.
