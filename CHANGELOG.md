# Journal des changements

Les changements notables de RGAA Studio sont consignés dans ce fichier. Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et les versions suivent [Semantic Versioning](https://semver.org/lang/fr/).

## [À venir]

Cette section regroupe les changements déjà apportés au code mais qui ne sont pas encore inclus dans une version publiée.

## [1.0.0-preview.5] - 2026-10-31

### Ajouté

- Revue des critères de l’audit par recherche de code, intitulé et tests, avec filtre par thème.
- Résultat de conformité unique par critère pour le lot sélectionné, distinct de l’applicabilité, des constats et des preuves de chaque page.
- Panneau de page qui conserve le critère sélectionné, montre son applicabilité, ses tests, constats et preuves, et permet d’ajouter un constat lié à cette page.
- Progression du lot calculée sur les critères seulement après détermination de l’applicabilité de toutes ses pages ; les changements de pages, applicabilité, tests ou constats rouvrent la revue concernée.

### Modifié

- Séparation visuelle entre l’inspection d’une page et le résultat du critère pour le lot.
- Critères du lot accessibles depuis la barre latérale et la barre d’outils avec recherche, thèmes et progression.

### Limites connues

- Un lot est l’échantillon de pages sélectionnées par l’auditeur ; ses résultats ne constituent pas automatiquement une conclusion sur l’ensemble du site.
- L’inspection d’éléments par pointeur reste disponible uniquement sur la page d’exemple intégrée.
- L’installateur Windows n’est pas signé.
- Les paquets Linux sont assemblés sous Ubuntu 24.04 WSL ; l’installation et le rendu restent à valider sur Ubuntu 26.04 et Fedora 44.

## [1.0.0-preview.1] - 2026-10-31

### Ajouté

- Nouvelle application desktop Avalonia pour Windows et Linux.
- Création, listing, reprise et archivage de dossiers d’audit conservés localement.
- Choix progressif du référentiel et de sa version ; niveau A/AA/AAA pour WCAG.
- Shell frameless avec navigation par dossiers et onglets d’audit.
- Ouverture et analyse de pages HTTPS réelles sélectionnées dans le dossier ; navigation limitée au site et aux pages retenues.
- Scan non exhaustif des liens de la page d’accueil et du sitemap, avec confirmation manuelle des pages à auditer.
- Analyse axe-core et pipette de contraste sur les pages sélectionnées ; inspection par pointeur conservée sur la page d’exemple locale.
- Paquets Windows x64, Debian/Ubuntu x64 et Linux x86_64, accompagnés de `SHA256SUMS.txt`.
- Workflow CI de compilation et de vérification du cœur sur Windows.

### Limites connues

- La navigation du navigateur est limitée aux pages HTTPS sélectionnées du site associé au dossier ; les liens hors périmètre sont bloqués.
- L’inspection d’éléments par pointeur reste disponible uniquement sur la page d’exemple intégrée.
- Les paquets macOS de la publication précédente ne sont pas reconduits.
- L’installateur Windows n’est pas signé.
- Les paquets Linux ont été assemblés sous Ubuntu 24.04 WSL, mais l’installation et le rendu doivent encore être validés sur Ubuntu 26.04 et Fedora 44.

[À venir]: https://github.com/numera11y/rgaa-studio/compare/v1.0.0-preview.5...HEAD
[1.0.0-preview.5]: https://github.com/numera11y/rgaa-studio/releases/tag/v1.0.0-preview.5
[1.0.0-preview.1]: https://github.com/numera11y/rgaa-studio/releases/tag/v1.0.0-preview.1
