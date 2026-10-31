# Journal des changements

Les changements notables de RGAA Studio sont consignés dans ce fichier. Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et les versions suivent [Semantic Versioning](https://semver.org/lang/fr/).

## [Non publié]

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

[Non publié]: https://github.com/numera11y/rgaa-studio/compare/v1.0.0-preview.1...HEAD
[1.0.0-preview.1]: https://github.com/numera11y/rgaa-studio/releases/tag/v1.0.0-preview.1
