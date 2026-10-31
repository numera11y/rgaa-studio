# ADR 0018 — Application desktop principale Avalonia

- **Statut :** décision utilisateur acceptée le 30 octobre 2026 ; intégration produit commencée.
- **Contexte :** le shell Avalonia a restitué plus fidèlement le design fourni par le client que le shell WPF. L’utilisateur a explicitement choisi cette approche pour poursuivre le développement et l’intégrer au code existant.

## Décision

- Avalonia devient l’application desktop principale pour Windows et Linux.
- Le shell validé (barre frameless, SVG Lucide, dropdown textuel et états de menu) est intégré dans `src/RgaaStudio.Desktop/`.
- Le métier et la persistance non liés à l’UI résident dans `src/RgaaStudio.Core/` et réutilisent le format de dossiers existant pour préserver leur reprise.
- Le spike WPF reste temporairement dans `spikes/windows/wpf-native-spike/` comme référence de migration des décisions, preuves, favicons et parcours déjà éprouvés. Il ne constitue pas une deuxième interface produit à poursuivre.
- L’application Avalonia utilise actuellement `Avalonia.Controls.WebView` : WebView2 sur Windows et WebKitGTK/WPE WebKit sur Linux. Cette dépendance est un choix d’implémentation à valider sur les distributions Linux cibles, pas une revendication de compatibilité complète.

## Portée actuelle et suites

La préversion intégrée sait créer, lister et reprendre un dossier d’audit local, configurer RGAA ou WCAG de façon progressive (version après référentiel, niveau A/AA/AAA uniquement après version WCAG), ouvrir dans le WebView les pages HTTPS retenues dans un dossier, lancer axe-core et utiliser la pipette de contraste. La découverte des pages propose des liens de la page d’accueil et du sitemap ; l’auditeur confirme les pages à inclure et la navigation reste limitée à ces URL du même site. Les décisions humaines sont conservées séparément des suggestions automatiques ; fermer un onglet ne supprime jamais le dossier. L’inspection par pointeur d’éléments reste limitée à la fixture locale. L’authentification, une navigation générale hors périmètre, l’isolation de profils, et la validation Linux complète restent hors de cette préversion.

La migration conserve le chemin et le schéma des manifestes existants (`RgaaStudioSpike/AuditDossier`) pour reprendre les dossiers du prototype Windows. Un changement de format ou d’emplacement nécessitera une migration explicite et vérifiée.

## Conséquences

- Les nouvelles fonctions UI et les nouveaux parcours métier ciblent Avalonia.
- Les validations de compilation Windows ne suffisent pas à confirmer Linux : vérifier la disponibilité du WebKit natif, les interactions clavier/focus WebView, l’installation et les paquets `.deb`/`.rpm` sur Ubuntu 26.04 et Fedora 44.
- Continuer de distinguer la décision humaine, les suggestions automatisées, la revue terminée et les preuves ; afficher les limites de périmètre du WebView et de l’inspection par pointeur sans confondre page d’exemple et page réelle.

## Vérification

- Builds .NET 10 Debug/Release de `src/RgaaStudio.Desktop/`.
- Harness de persistance déterministe de `src/RgaaStudio.Core.Checks/`.
- Vérifier manuellement la reprise de dossiers issus de WPF et l’usage du shell/menu avec clavier.
- Gate restant : exécuter une build et une installation Linux, puis valider WebView GTK/WPE sur les cibles convenues.
