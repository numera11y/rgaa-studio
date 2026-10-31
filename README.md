# RGAA Studio

Application desktop d’aide aux audits d’accessibilité RGAA et WCAG. Les dossiers d’audit sont locaux ; les suggestions automatiques ne remplacent jamais la décision de l’auditeur.

> **Préversion `1.0.0-preview.1`** — l’interface Avalonia et la persistance des dossiers sont en cours de migration. L’aperçu intégré utilise actuellement une page de démonstration locale ; la navigation vers les sites réels n’est pas encore activée.

## Développement

Prérequis : SDK .NET 10. Sous Linux, le navigateur intégré requiert également WebKitGTK 4.1 et libsoup 3.

```powershell
dotnet restore .\RgaaStudio.slnx
dotnet build .\RgaaStudio.slnx --configuration Release
dotnet run --project .\src\RgaaStudio.Desktop\RgaaStudio.Desktop.csproj
dotnet run --project .\src\RgaaStudio.Core.Checks\RgaaStudio.Core.Checks.csproj --configuration Release
```

Les vérifications utilisent une base temporaire et ne modifient pas les données d’audit de l’utilisateur.

## Préparer les paquets

Le script `tools/packaging/Build-Release.ps1` produit la préversion `1.0.0-preview.1` et son fichier `SHA256SUMS.txt` : Windows x64 (installateur et archive portable), Debian/Ubuntu x64 (`.deb`) et Linux x86_64 (`.rpm`).

La génération complète nécessite NSIS 3 et WSL avec Ubuntu 24.04, `dpkg-deb` et `rpmbuild`. Les paquets Linux n’ont pas encore été installés ni validés à l’exécution sur Ubuntu 26.04 ou Fedora 44 ; l’installateur Windows n’est pas signé. Voir [`tools/packaging/README.md`](tools/packaging/README.md) avant toute distribution.

## Documentation

- [Architecture et décisions](docs/architecture/README.md)
- [Périmètre et prochaines étapes](docs/architecture/PROJECT-PLAN.md)
- [Construction des paquets](tools/packaging/README.md)

## Licence

Le code de RGAA Studio est publié sous licence MIT. Les référentiels et dépendances conservent leurs attributions et licences respectives ; les notices de redistribution sont incluses dans les paquets.
