# ADR 0016 — État actif Projets et présentation de la liste des dossiers

- **Statut :** direction UX acceptée par l’utilisateur le 29 octobre 2026 ; clavier/lecteur d’écran à valider.
- **Date :** 29 octobre 2026
- **Décideur :** propriétaire produit

## Contexte confirmé

L’utilisateur demande que l’icône « Projets » de la barre supérieure soit visiblement marquée comme active quand la liste des dossiers est affichée. Il demande que les dossiers soient présentés sous forme de blocs/cartes avec un état de survol, et souhaite une proposition d’amélioration de la présentation actuelle.

La liste représente des projets/dossiers d’audit. Chaque dossier correspond à un site et un référentiel. Les cartes contiennent actuellement un titre, l’URL, le référentiel/version et l’heure de mise à jour, avec un favicon ou un globe de secours. Le composant de liste peut produire une sélection large sur toute la ligne, et un bouton général « Reprendre l’audit sélectionné » sépare l’action de la carte.

## Direction de présentation retenue

1. **Projets actif :** quand `HomeView` (liste Projets) est visible, appliquer au bouton d’accès projets un fond sélectionné discret et un repère de bordure/forme distincts du survol et du focus clavier. Exposer également l’état « Projets — vue active » à UI Automation/lecteur d’écran. Quand un dossier est ouvert, le bouton perd cet état.
2. **Cartes en repos :** une seule colonne de blocs lisibles, alignés et espacés. Petite icône/favicon décorative à gauche ; au centre, titre du dossier puis domaine et référentiel/version, puis date de mise à jour. Conserver le texte d’identité même si le favicon est absent. Utiliser la largeur disponible sans étirer la sélection sur toute la fenêtre.
3. **Survol :** léger changement de surface et de bordure, sans déplacer/redimensionner la carte, sans cacher l’action et sans coder le seul indice par couleur.
4. **Focus clavier :** contour net autour de la carte, indépendant du survol et de la sélection ; nom accessible contenant l’identité du dossier. Flèches pour parcourir le `ListBox` restent possibles.
5. **Sélection :** état distinct du survol et du focus (surface accentuée claire et bordure/repère), sans large bande pleine largeur qui dépasse la carte. Une carte sélectionnée n’est pas une revue ou un résultat d’audit.
6. **Action principale (mise à jour utilisateur) :** toute la ligne est directement activable par clic simple, Entrée et Espace. Ne pas afficher de texte ni de bouton « Reprendre » redondant dans la ligne. Le nom accessible de la ligne annonce l’action d’ouverture. Fermer un onglet ne supprime jamais le dossier.
7. **État vide :** garder « Aucun audit enregistré. Créez un audit pour commencer. » avec l’action « Créer un audit » ; aucune action Reprendre détachée.

## Décision utilisateur confirmée

L’utilisateur confirme **un clic simple sur la carte pour ouvrir le dossier** et précise ensuite que l’action étant déjà évidente par le clic, aucun libellé « Reprendre » n’est nécessaire. Entrée et Espace déclenchent la même action. La liste utilise des lignes plus larges, centrées, où le site, le référentiel/version et la date sont hiérarchisés ; l’icône Projets apparaît active uniquement quand cette vue est ouverte.

## Justification et tradeoffs

Cette proposition garde l’interface attendue familière, relie directement l’action à son dossier et évite que l’état actif du bouton Projets soit confondu avec son focus ou son survol. Une carte cliquable accélère la reprise, mais peut confondre sélection et ouverture ; le libellé visible et les tests clavier doivent rendre la conséquence claire. Une liste à une colonne reste le choix de travail : la capture ne demande pas de comparaison en grille.

Les favicon/globes sont purement décoratifs. L’identité accessible reste le texte du dossier, du site et du référentiel/version. Les états ne reposent pas uniquement sur la couleur.

## Vérification restante

- Le hover est un état pointeur uniquement ; focus clavier et sélection ont des traitements propres.
- Vérifier manuellement le clic simple, Entrée/Espace, le retour focus après ouverture/retour à la liste, les états visuels actif/focus/hover/sélection et l’absence de bande de sélection pleine largeur.
- Valider les annonces et noms avec Narrateur/NVDA ; aucun résultat de conformité d’accessibilité n’est revendiqué avant ces tests.
