RGAA Studio — paquet Windows x64
Version du paquet : @APP_VERSION@

Installation et mise à jour (par utilisateur, sans droits administrateur)
L’installateur rgaa-studio-@APP_VERSION@-windows-x64-setup.exe est livré à côté du ZIP dans le dossier de la release. Il installe ou met à jour RGAA Studio dans le profil utilisateur, crée le raccourci du menu Démarrer et préserve les dossiers d’audit locaux. Fermer l’application avant une mise à jour.

Si tu as seulement le ZIP windows-x64 : extraire tous ses fichiers puis exécuter Install-Windows.ps1. Uninstall-Windows.ps1 retire les fichiers de l’application et son raccourci sans toucher aux dossiers d’audit locaux.

Le Setup embarque l’installateur hors ligne Microsoft Edge WebView2 Evergreen x64 et l’exécute si le runtime est absent. Le runtime installé reste actualisé par Microsoft. Le ZIP seul nécessite WebView2 déjà présent.

Cette release de préversion n’est pas signée. Le programme de désinstallation retire les fichiers de l’application et ses raccourcis, mais conserve les données de dossiers sous %LOCALAPPDATA%.

Le runtime .NET est inclus. Éditeur : numera11y. Licence de l’application : MIT (LICENSE.txt). Conserver également les avis tiers dans Assets.
