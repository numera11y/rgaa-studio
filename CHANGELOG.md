# Journal des changements

Les changements notables de RGAA Studio sont consignés dans ce fichier. Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et les versions suivent [Semantic Versioning](https://semver.org/lang/fr/).

## [Non publié]

## [1.0.0-preview.1] - 2026-10-31

### Ajouté

- Nouvelle application desktop Avalonia pour Windows et Linux.
- Création, listing, reprise et archivage de dossiers d’audit conservés localement.
- Choix progressif du référentiel et de sa version ; niveau A/AA/AAA pour WCAG.
- Shell frameless avec navigation par dossiers et onglets d’audit.
- Fixture HTML locale avec inspection DOM limitée et suggestions séparées des décisions humaines.
- Paquets Windows x64, Debian/Ubuntu x64 et Linux x86_64, accompagnés de `SHA256SUMS.txt`.
- Workflow CI de compilation et de vérification du cœur sur Windows.

### Limites connues

- Le navigateur intégré ne navigue pas encore vers des sites réels ; il affiche la fixture locale.
- Les paquets macOS de la publication précédente ne sont pas reconduits.
- L’installateur Windows n’est pas signé.
- Les paquets Linux ont été assemblés sous Ubuntu 24.04 WSL, mais l’installation et le rendu doivent encore être validés sur Ubuntu 26.04 et Fedora 44.

[Non publié]: https://github.com/numera11y/rgaa-studio/compare/v1.0.0-preview.1...HEAD
[1.0.0-preview.1]: https://github.com/numera11y/rgaa-studio/releases/tag/v1.0.0-preview.1
