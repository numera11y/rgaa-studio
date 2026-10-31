# ADR 0017 — États de la barre, menu et commandes navigateur

- **Statut :** référence d’interface fournie explicitement par l’utilisateur ; conformité de l’implémentation et validation d’accessibilité en cours.
- **Date :** 30 octobre 2026
- **Décideur :** propriétaire produit
- **Référence :** [`RGAA Studio · États de la barre et du menu.svg`](../../_sources/images/RGAA%20Studio%20%C2%B7%20%C3%89tats%20de%20la%20barre%20et%20du%20menu.svg) et son PNG voisin.
- **Référence des lignes du menu :** [`États des items.svg`](../../_sources/images/%C3%89tats%20des%20items.svg).

## Contexte

Les captures précédentes montraient une seule apparence de la barre. L’utilisateur a fourni une planche de design system décrivant les états statiques utiles du navigateur intégré et du menu. La planche se déclare comme maquettes non connectées : ses états et annotations constituent le contrat d’implémentation, pas une preuve que les interactions sont déjà fonctionnelles. L’interface conserve la barre et l’ordre confirmés dans l’ADR 0013, les marges/glyphes du README du spike, et le sens projet/onglet de l’ADR 0014.

## Contrat de référence

### Fenêtre et inspection

- La barre distingue fenêtre active et inactive. Perdre l’activation de la fenêtre **ne désactive pas l’inspection** ; le badge d’inspection active reste visible lorsque la fenêtre passe en arrière-plan.
- En inspection active, afficher « Inspection active · Échap hors menu » dans la barre. Cette information ne dépend pas de la couleur seule.
- Échap termine l’inspection uniquement si le menu est fermé. Si le menu est ouvert, le premier Échap ferme le menu seulement.
- La commande « Activer l’inspection des éléments (F6) » est indisponible lorsque l’inspection est déjà active ; « Terminer l’inspection (Échap) » est alors disponible. Une commande désactivée ne doit jamais s’exécuter par clic, Entrée, Espace ou raccourci.
- L’icône Projets n’utilise pas de soulignement pour marquer sa vue active. Avalonia indique cette vue avec une tuile sélectionnée `#E9EFF9` et un contour `#214B9A`, distincts du focus clavier.

### Bouton Options et menu

- Le bouton Options conserve une cible de 40 DIP ; sa tuile graphique de 20 DIP reste entourée par le focus visible.
- Chaque bouton utilise les états du frame : repos blanc + contour `#7B8190` 1 DIP ; survol `#E9EFF9` ; appui `#D5E1F5` + contour `#214B9A` ; focus contour `#214B9A` 2 DIP ; état indisponible `#E4E3DD`. Focus+survol cumule le fond de survol et le contour focus.
- Le menu ouvert a un état persistant bleu plein `#214B9A` avec icône et repère blancs jusqu’à sa fermeture. L’ouverture+focus ajoute l’anneau bleu extérieur avec un espace blanc.
- Les lignes du menu déroulant sont textuelles, sans icône, et reprennent les états et la géométrie de `États des items.svg` (repos, survol pleine ligne, focus avec repère latéral, appui et indisponible). Le hover ne déplace pas le focus clavier.
- Le menu contient les commandes, libellés, ordre et états d’activation dessinés dans la planche. Une ligne survolée reçoit le fond prévu sans exécuter la commande ni voler le focus clavier.
- Clic, Entrée et Espace ouvrent le menu. À l’ouverture clavier, le focus se place sur la première commande disponible. Haut/Bas parcourt les commandes dans l’ordre affiché en sautant séparateurs et commandes indisponibles. Entrée active exactement une commande disponible.
- Échap ferme le menu avant toute action d’inspection et rend le focus au bouton Options. Un clic extérieur ou la désactivation de la fenêtre ferme le menu ; la perte d’activation ne termine pas l’inspection.
- F6 est le raccourci d’activation indiqué par la planche. Son effet lorsque l’inspection est déjà active n’est pas établi ; recommandation de travail : ne rien faire, utiliser Échap hors menu pour arrêter l’inspection.

### Onglets et commandes système

- L’onglet actif est blanc, identifiable par un soulignement bleu de 3 DIP ; l’inactif est distinct et plus discret. Focus, survol, activation et fermeture restent visibles et accessibles.
- L’état de repos de l’onglet actif n’ajoute pas de cadre complet : fond clair + soulignement bleu de 3 DIP. Le cadre complet est réservé au survol (ou au focus visible) ; l’onglet inactif conserve son contour discret.
- Fermer un onglet ferme la vue, jamais le dossier d’audit.
- Réduire, agrandir/restaurer et fermer conservent leurs états repos/survol/focus/appui. Fermer une fenêtre ne signifie jamais supprimer le dossier. L’icône agrandir/restaurer suit l’état réel de la fenêtre.
- Les autres commandes de la barre gardent leurs fonctions déjà documentées ; la planche ne leur attribue pas de nouvelles actions ni de nouveaux raccourcis.

## Implémentation et validation

Conserver le `ContextMenu` natif WPF si les transitions clavier et focus peuvent être démontrées. Éviter de gérer l’inspection à travers le contenu WebView2 lorsque le menu se ferme : l’état de l’inspection est indépendant du focus de fenêtre, et le transfert de focus vers la page cible doit rester une action définie, jamais un effet collatéral d’Échap.

### Implémentation du spike (30 octobre 2026)

- La tuile ouverte du burger est bleu plein avec le glyph blanc ; focus clavier dispose d’un anneau extérieur persistant. Le badge d’inspection est visible et sa mise à jour est exposée comme région live UIA. La désactivation de fenêtre ne modifie pas `inspectMode`, ferme le menu et applique la surface inactive.
- Le menu reçoit le focus sur la première commande disponible ; le routage Haut/Bas parcourt les commandes disponibles ; Entrée n’émet qu’un seul clic routé ; Échap dans le menu ferme le menu seulement ; F6 active depuis le menu uniquement si la commande est disponible. Échap hors menu arrête l’inspection.
- Les deux commandes d’inspection mettent à jour `IsEnabled`, une explication UIA et le badge. Les onglets actifs reçoivent un underline bleu de 3 DIP et l’icône système passe entre agrandir et restaurer.
- **Vérifié en smoke clavier synthétique (`SendKeys`)/UIA :** menu ouvert, première ligne focalisée, Space, Haut/Bas avec saut de la ligne désactivée, Entrée une fois, F6 et double Échap (menu d’abord, inspection ensuite). Les commandes activer/terminer et le badge changent correctement d’état ; le focus revient au déclencheur. Le pointeur sur une ligne ne déplace pas le focus clavier. La fermeture par commande extérieure et le changement maximisé/restauré sont vérifiés via UIA. Builds Debug/Release, vérificateur SVG, intégrité des dossiers, 14 checks favicon et scan vulnérabilités passent.
- **Non vérifié / gate restant :** perte réelle du premier plan pendant inspection, captures visuelles comparatives de chaque état et combinaison focus+hover sur le même item, clavier physique non simulé, lecteur d’écran Narrateur/NVDA, interactions WebView2 hors fixture. Les envois clavier/UIA de smoke ne remplacent pas un parcours clavier manuel complet ni la validation d’accessibilité.

Critères de validation :

1. Menu repos/ouvert/survolé/focus/appuyé ; inspection inactive/active ; fenêtre active/inactive.
2. Désactivation correcte des deux commandes inspection selon l’état ; aucune activation indirecte par raccourci.
3. Parcours clavier complet : ouvrir, première commande, Haut/Bas avec sauts, Entrée une fois, Échap menu-first, fermeture extérieure et retour de focus.
4. Onglet actif/inactif, soulignement 3 DIP, fermeture sans suppression et mises à jour maximisé/restauré.
5. Tests manuels clavier et UI Automation ; Narrateur/NVDA avant toute revendication d’accessibilité complète.

Le smoke visuel ou UI Automation partiel ne constitue pas une validation complète. Les états inactive-window, les transitions combinées menu/inspection et le transfert de focus avec WebView2 restent des gates de test dédiés.
