RGAA Studio — paquet Linux x86_64

Le paquet inclut le runtime .NET. Le navigateur natif Avalonia utilise WebKitGTK 4.1 avec libsoup 3. Dépendances système déclarées par le paquet :
- GTK 3
- WebKitGTK 4.1
- libsoup 3

Installation Debian/Ubuntu : sudo apt install ./rgaa-studio_*.deb
Installation Fedora : sudo dnf install ./rgaa-studio-*.rpm
Lancer l’application depuis le menu Applications (« RGAA Studio ») ou avec : rgaa-studio

Les dossiers d’audit sont conservés dans le répertoire LocalApplicationData de l’utilisateur. La désinstallation du paquet ne supprime pas ces données.

La construction a été réalisée sous Ubuntu 24.04 WSL. Les paquets ciblent amd64/x86_64 ; leur installation et le moteur WebView doivent être validés sur Ubuntu 26.04 et Fedora 44 avant une diffusion générale. Le code applicatif est sous licence MIT ; voir LICENSE.txt.

Les avis des composants tiers sont livrés dans /opt/rgaa-studio/Assets/ et dans la documentation du paquet.
