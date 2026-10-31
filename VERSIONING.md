# Versionnage et publications

RGAA Studio utilise **Semantic Versioning 2.0.0**. Le numéro de version applicatif est défini une seule fois dans `Directory.Build.props`; les paquets, les métadonnées de l’application et les tags de publication doivent rester alignés avec lui.

## Format

- Les tags Git sont préfixés par `v` : `vMAJOR.MINOR.PATCH`.
- Pendant le développement d’une version, employer un identifiant de préversion SemVer, par exemple `1.0.0-preview.2` ou `1.0.0-rc.1`.
- `MAJOR` indique une incompatibilité d’usage ou de données ; `MINOR` ajoute des capacités compatibles ; `PATCH` corrige des défauts sans changer le contrat existant.
- Une préversion n’est pas une version stable. Les restrictions de plateforme, de sécurité et de fonctionnalités doivent figurer dans ses notes de publication.

## Source de version

`Directory.Build.props` contient `Version`, `AssemblyVersion`, `FileVersion` et `InformationalVersion`. `tools/packaging/Build-Release.ps1` lit `Version` par défaut ; son option `-Version` sert aux builds ponctuels. Ne pas laisser un numéro de version différent dans les projets, le nom du package ou les notes de publication.

Pour préparer une publication :

1. Mettre à jour `Version` et les métadonnées numériques dans `Directory.Build.props`.
2. Ajouter une entrée datée dans `CHANGELOG.md` et déplacer le travail terminé de `[À venir]`.
3. Exécuter restore, build et `RgaaStudio.Core.Checks`.
4. Construire les paquets et vérifier `SHA256SUMS.txt` ainsi que leurs notices.
5. Créer le tag `v<Version>` et une GitHub Release marquée *pre-release* si la version contient un suffixe.
6. Joindre les installateurs, paquets portables, sommes SHA-256 et notes de limites connues.

## Changelog

`CHANGELOG.md` utilise les catégories **Ajouté**, **Modifié**, **Corrigé**, **Supprimé** et **Limites connues**. Les entrées décrivent l’impact visible par les utilisateurs ; les changements purement internes ne sont ajoutés que s’ils sont utiles aux personnes qui construisent ou distribuent le projet.
