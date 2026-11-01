RGAA Studio — paquet Linux x86_64

Le paquet inclut le runtime .NET. Le navigateur natif Avalonia utilise WebKitGTK 4.1 avec libsoup 3. Dépendances système déclarées par le paquet :
- GTK 3
- WebKitGTK 4.1
- libsoup 3

Installation Ubuntu depuis le dépôt signé numera11y : suivre APT-REPOSITORY.md après publication du dépôt. Installation manuelle pour tester le paquet : sudo apt install ./rgaa-studio_*.deb (Ubuntu peut afficher un avertissement de provenance pour un fichier local).
Installation Fedora : sudo dnf install ./rgaa-studio-*.rpm
Lancer l’application depuis le menu Applications (« RGAA Studio ») ou avec : rgaa-studio

Le paquet déclare sa taille installée et inclut les métadonnées AppStream pour le nom, la licence MIT, la page d’accueil et la date de version. Installer directement un fichier .deb téléchargé peut encore déclencher l’avertissement de provenance d’Ubuntu. Le dépôt APT signé numera11y est documenté dans APT-REPOSITORY.md.

Les catalogues complets RGAA 4.1.2 et WCAG 2.2 sont inclus dans l’application et ajoutés à la base locale au premier démarrage. Les catalogues et leurs licences d’origine sont décrits dans le fichier copyright du paquet.

Les dossiers d’audit sont conservés dans le répertoire LocalApplicationData de l’utilisateur. La désinstallation du paquet ne supprime pas ces données.

La construction a été réalisée sous Ubuntu 24.04 WSL. Les paquets ciblent amd64/x86_64 ; leur installation et le moteur WebView doivent être validés sur Ubuntu 26.04 et Fedora 44 avant une diffusion générale. Le code applicatif est sous licence MIT ; voir LICENSE.txt.

Les avis des composants tiers sont livrés dans /opt/rgaa-studio/Assets/ et dans la documentation du paquet.
