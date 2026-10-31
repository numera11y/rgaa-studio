# RGAA Studio

[![CI](https://github.com/numera11y/rgaa-studio/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/numera11y/rgaa-studio/actions/workflows/ci.yml)
[![Licence MIT](https://img.shields.io/badge/licence-MIT-blue.svg)](LICENSE)
[![Préversion 1.0.0-preview.1](https://img.shields.io/badge/version-1.0.0--preview.1-orange)](https://github.com/numera11y/rgaa-studio/releases/tag/v1.0.0-preview.1)

Application desktop open source pour auditer des pages web selon le RGAA et les WCAG, organiser le travail par dossier et conserver les décisions localement.

> **Préversion — `1.0.0-preview.1`.** L’application Avalonia est disponible pour Windows et Linux. Elle peut ouvrir et analyser de vrais sites : l’auditeur sélectionne d’abord les pages à inclure dans son dossier, puis les ouvre dans le navigateur intégré. La navigation reste limitée aux pages HTTPS sélectionnées sur le même site. Les paquets Linux ont été générés, mais leur installation et leur fonctionnement restent à tester sur Ubuntu 26.04 et Fedora 44.

## Télécharger

La préversion actuelle est publiée sur [GitHub Releases](https://github.com/numera11y/rgaa-studio/releases/tag/v1.0.0-preview.1). Elle comprend :

- **Windows x64** : installateur `Setup.exe` et archive portable `.zip` ; l’installateur configure WebView2 s’il manque.
- **Debian / Ubuntu x64** : paquet `.deb`.
- **Linux x86_64** : paquet `.rpm`.
- **SHA-256** : fichier [`SHA256SUMS.txt`](https://github.com/numera11y/rgaa-studio/releases/download/v1.0.0-preview.1/SHA256SUMS.txt), en plus des empreintes calculées et affichées par GitHub pour chaque fichier.

Les paquets macOS de l’ancienne publication ne font pas partie de cette préversion. L’installateur Windows n’est pas signé. Les paquets Linux nécessitent GTK 3, WebKitGTK 4.1 et libsoup 3 ; ils ont été assemblés sous Ubuntu 24.04 WSL et ne sont pas encore validés à l’exécution sur Ubuntu 26.04 ou Fedora 44.

## État fonctionnel

Cette préversion permet de créer, lister, reprendre et archiver des dossiers locaux, de choisir un référentiel et une version, d’analyser les pages retenues avec axe-core et d’utiliser la pipette de contraste. Le scan propose des candidates à partir de la page d’accueil et du sitemap ; il n’est pas exhaustif et l’auditeur confirme les pages à inclure. Les contrôles automatiques sont des suggestions, jamais un verdict de conformité.

La navigation bloque les pages non sélectionnées et les liens vers un autre site. L’inspection d’éléments par pointeur reste disponible sur la page d’exemple intégrée ; sur les pages réelles, l’analyse axe-core et la pipette sont utilisables. L’installation automatique de mises à jour n’est pas encore disponible. Voir [`CHANGELOG.md`](CHANGELOG.md) et le [plan de projet](docs/architecture/PROJECT-PLAN.md) pour le détail de cette préversion.

## Développer

Prérequis : SDK .NET 10. Sous Linux, l’application utilise également GTK 3, WebKitGTK 4.1 et libsoup 3.

```powershell
dotnet restore .\RgaaStudio.slnx
dotnet build .\RgaaStudio.slnx --configuration Release
dotnet run --project .\src\RgaaStudio.Desktop\RgaaStudio.Desktop.csproj
dotnet run --project .\src\RgaaStudio.Core.Checks\RgaaStudio.Core.Checks.csproj --configuration Release
```

Les vérifications du cœur utilisent une base temporaire et ne modifient pas les dossiers d’audit de l’utilisateur. La CI exécute la compilation et ces vérifications sur chaque push et pull request vers `main`.

## Construire les paquets

Sur Windows, avec NSIS 3 et WSL Ubuntu 24.04 configuré avec `dpkg-deb` et `rpmbuild` :

```powershell
winget install --id NSIS.NSIS --exact
.\tools\packaging\Build-Release.ps1
```

La version provient de [`Directory.Build.props`](Directory.Build.props) ; un paramètre `-Version` permet de la remplacer explicitement. Les paquets et leurs sommes sont créés dans `artifacts/packages/<version>/`. Lire le [guide de packaging](tools/packaging/README.md) avant de distribuer une préversion.

## Versionnage et documentation

Le projet suit Semantic Versioning, avec le préfixe `v` dans les tags Git. La convention, la source unique de version et le format des entrées du changelog sont décrits dans [`VERSIONING.md`](VERSIONING.md). Les décisions de produit et d’architecture sont dans [`PRODUCT.md`](PRODUCT.md) et [`docs/architecture/`](docs/architecture/README.md).

## Licence

Le code de RGAA Studio est publié sous licence MIT. Les référentiels, ressources et dépendances tierces conservent leurs licences et attributions ; les notices de redistribution sont fournies avec les paquets.
