# Dépôt APT signé

Les paquets Debian de RGAA Studio déclarent maintenant leur taille installée, leur licence MIT, leurs métadonnées AppStream et la date de la préversion. Installer un `.deb` téléchargé directement reste toutefois une installation hors d’une source APT approuvée.

Le dépôt APT signé sera servi par GitHub Pages à `https://numera11y.github.io/rgaa-studio/apt/` après activation de Pages. Le workflow `.github/workflows/publish-apt-repository.yml` construit les index APT/AppStream et signe le fichier `InRelease` avec une clé dédiée à la distribution.

## Préparer la clé de signature

La clé OpenPGP RSA 4096 du dépôt a été créée pour numera11y et enregistrée comme secret GitHub Actions. Son empreinte publique est `9F8E 45A2 9259 68C4 814F 7391 489A A675 B22C 64D1` (expiration 2029-10-31). La variable GitHub `APT_REPOSITORY_SIGNING_FINGERPRINT` est configurée ; le workflow compare cette empreinte à la clé secrète avant de signer.

Une sauvegarde locale chiffrée DPAPI est placée sous `%LOCALAPPDATA%\Numera11y\RGAAStudio\APTSigning\apt-signing-key.dpapi` ; elle est déchiffrable uniquement depuis le même compte Windows. Le secret GitHub Actions est utilisé pour publier. Ne jamais committer l’export privé. Garder l’empreinte et la clé publique disponibles pour que les auditeurs puissent vérifier la source.

Le workflow publie le paquet, les index `Packages`, le catalogue AppStream, les icônes, `InRelease`, `Release.gpg`, la clé publique et `KEY-FINGERPRINT.txt`.

## Installer depuis Ubuntu

Après une exécution réussie du workflow, vérifier l’empreinte de la clé publique publiée et la comparer à la variable GitHub `APT_REPOSITORY_SIGNING_FINGERPRINT`. Puis ajouter la clé et la source APT :

```bash
curl -fsSL https://numera11y.github.io/rgaa-studio/apt/numera11y-archive-key.asc \
  | sudo gpg --dearmor -o /usr/share/keyrings/numera11y-rgaa-studio.gpg
```

Créer `/etc/apt/sources.list.d/numera11y-rgaa-studio.sources` avec :

```text
Types: deb
URIs: https://numera11y.github.io/rgaa-studio/apt/
Suites: stable
Components: main
Architectures: amd64
Signed-By: /usr/share/keyrings/numera11y-rgaa-studio.gpg
```

Puis installer et mettre à jour avec `sudo apt update` et `sudo apt install rgaa-studio`.

Cette source APT authentifie les paquets de numera11y et retire l’avertissement lié à un `.deb` local non authentifié. Elle reste une source tierce : seul un éditeur publié et vérifié par Canonical peut être présenté comme provenant officiellement de l’Ubuntu App Center.
