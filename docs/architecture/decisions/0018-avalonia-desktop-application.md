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

La tranche intégrée sait créer, lister et reprendre un dossier d’audit local, configurer RGAA ou WCAG de façon progressive (version après référentiel, niveau A/AA/AAA uniquement après version WCAG), afficher une fixture locale synthétique dans le navigateur natif, inspecter des éléments de cette fixture, enregistrer des décisions humaines append-only avec une fin de revue explicite et distincte du résultat, et garder plusieurs dossiers ouverts dans des onglets qui survivent au retour à la liste. Fermer un onglet ne supprime jamais le dossier. La navigation vers des sites réels reste fermée. Les captures de preuve, profils authentifiés, collecte facultative du favicon, réordonnancement des onglets et packaging doivent être migrés/validés en tranches séparées.

La migration conserve le chemin et le schéma des manifestes existants (`RgaaStudioSpike/AuditDossier`) pour reprendre les dossiers du prototype Windows. Un changement de format ou d’emplacement nécessitera une migration explicite et vérifiée.

## Conséquences

- Les nouvelles fonctions UI et les nouveaux parcours métier ciblent Avalonia.
- Les validations de compilation Windows ne suffisent pas à confirmer Linux : vérifier la disponibilité du WebKit natif, les interactions clavier/focus WebView, l’installation et les paquets `.deb`/`.rpm` sur Ubuntu 26.04 et Fedora 44.
- Continuer de distinguer la décision humaine, les suggestions automatisées, la revue terminée et les preuves ; aucun prototype ne doit présenter la fixture comme un site audité.

## Vérification

- Builds .NET 10 Debug/Release de `src/RgaaStudio.Desktop/`.
- Harness de persistance déterministe de `src/RgaaStudio.Core.Checks/`.
- Vérifier manuellement la reprise de dossiers issus de WPF et l’usage du shell/menu avec clavier.
- Gate restant : exécuter une build et une installation Linux, puis valider WebView GTK/WPE sur les cibles convenues.
