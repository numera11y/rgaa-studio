# Séquences proposées pour le site de présentation

Tous les visuels sont des captures de RGAA Studio sur le site public numera11y.fr. Les traitements de mouvement proposés peuvent être montés à partir des PNG/WebP fournis, sans simuler de validation de conformité.

## 1. Du site au dossier d’audit · 7–9 s

1. `captures/01-mes-audits.png` — ouverture sur la liste des dossiers.
2. `captures/06-espace-audit-site.png` — arrivée sur le site dans le navigateur intégré.
3. `captures/07-tableau-de-bord.png` — bascule vers la progression du dossier.
4. `compositions/01-espace-audit.png` — plan final de présentation.

Mouvement : déplacement lent de gauche à droite sur la barre, les pages du dossier, puis la page active ; fondu court entre la vue du site et le tableau de bord.

## 2. Plusieurs angles sur une page · 8–10 s

1. `captures/06-espace-audit-site.png` — page observée.
2. `captures/09-code-source.png` — DOM formaté.
3. `captures/11-html5-semantique.png` — structure et titres.
4. `captures/13-vue-aria.png` — rôles et noms calculés.
5. `captures/14-technologies-seo.png` — résumé technique et indicateurs SEO.

Mouvement : conserver la barre et la page comme point d’ancrage ; faire apparaître le panneau d’analyse par glissement horizontal ou fondu.

## 3. Automatisation comme aide · 6–8 s

1. `captures/15-resultats-axe-core.png` — suggestions automatiques en attente d’examen.
2. `captures/22-pipette-contraste.png` — couleurs et ratio pour une zone pointée.
3. `captures/03-parametres.png` — préférences locales.
4. `compositions/02-suggestions-axe-core.png` — écran de conclusion.

Mouvement : surligner les cartes de suggestions une à une ; terminer sur le texte « Pas un verdict » de la composition. Ne pas animer de résultat humain de conformité : aucun n’est prérempli dans le dossier de démonstration.

## 4. Présenter les pages numera11y · 7–9 s

1. `captures/06-espace-audit-site.png` — accueil.
2. `captures/16-page-offre-audit.png` — offre d’audit.
3. `captures/18-page-realisations.png` — réalisations.
4. `captures/20-page-ressources.png` — ressources.
5. `captures/21-page-mentions-legales.png` — mentions légales.

Mouvement : transitions horizontales courtes, en gardant l’en-tête RGAA Studio visible pour montrer le contexte du poste d’audit.

## 5. Choisir son référentiel · 4–6 s

1. `captures/25-creation-audit.png` — URL numera11y.fr et version RGAA.
2. `captures/26-creation-wcag-niveau.png` — version WCAG 2.2 et niveau cible AA.

Mouvement : transition douce entre les deux choix ; conserver le texte qui distingue le niveau cible d’un résultat de conformité.

## Conseils de montage

- Formats de base : 1920 × 1080 pour les compositions et `store/`; WebP 1600 px de large pour les intégrations web.
- Garder les légendes courtes et externes aux zones de texte importantes de l’interface.
- Favoriser des fondus ou des translations modestes ; chaque écran contient beaucoup de détails fins.
- Ajouter une alternative textuelle et une commande pause/lecture pour les animations intégrées à la page.
- Revalider les résultats d’analyse si une nouvelle capture est réalisée : le site public et son contenu peuvent évoluer.
