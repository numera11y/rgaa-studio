# ADR 0013 — Shell Windows natif, Store et maintien de Linux

- **Statut :** direction d’interface Windows confirmée ; préférence actuelle pour build/install communautaire ; recommandation de shell et de distribution, runtime final non décidé.
- **Date :** 29 octobre 2026
- **Décideur :** propriétaire produit, avec le conseil technique et open source

## Contexte

L’utilisateur apprécie le parcours de la maquette shell-independent, mais rejette son apparence comme interface de livraison. Il veut une **vraie application Windows**, pas un site servi par navigateur. La capture fournie précise la barre supérieure souhaitée : menu hamburger/options, accès projets, onglets de projets fermables et réordonnables, plus/nouveau projet, indicateur de mise à jour, bascule du panneau latéral d’audit, et commandes réduire/agrandir/fermer. Il ne veut pas de logo d’application. La capture fixe la structure et le type de rendu, sans demander de copier la marque IntelliJ.

Les exigences déjà confirmées incluent Windows **et Linux** en v1, avec Ubuntu 26.04 LTS et Fedora Workstation 44 x86-64 comme cibles Linux d’installation garantie et formats `.deb`/`.rpm` à prototyper. Linux n’est pas retiré. La maquette `spikes/workflow-ui/` reste une référence de parcours, pas l’interface de livraison ni un choix de technologie. Le spike Electron demeure un banc technique rudimentaire et son interface n’est pas acceptée comme interface métier.

Le poste courant possède .NET SDK 10.0.402, Rust et Python, mais `cl`/MSVC et Docker n’ont pas été détectés ; Ubuntu 26.04 et Fedora 44 ne sont pas disponibles dans WSL. La preuve WPF se construit localement, mais un install/build communautaire propre et Linux ne sont pas encore prouvés.

## Position produit confirmée

- L’interface livrable ne doit pas être un site que l’utilisateur ouvre dans son navigateur ; le produit doit s’installer et fonctionner comme application desktop.
- Pour Windows, l’utilisateur demande explicitement une fenêtre frameless, un menu moderne et Lucide.
- Barre confirmée par l’exemple utilisateur : menu/options, accès projets, onglets closables/réordonnables, plus/nouveau projet, indicateur de mise à jour, bascule du panneau latéral de l’audit, commandes système ; aucun logo d’application.
- **Distribution actuelle :** préférence utilisateur pour install/build communautaire pour le moment ; Microsoft Store n’est pas un gate v1 actuel. Le Store reste envisageable ultérieurement, sans promesse d’acceptation ni de certification.
- Le parcours métier déjà apprécié reste la référence fonctionnelle : reprise/création, configuration, pages confirmées, travail page par page, inspection élément/règle, décision humaine et preuves.
- La fenêtre frameless ne peut pas supprimer ou rendre inaccessibles les commandes système, le déplacement/redimensionnement, le focus clavier ou les technologies d’assistance.

## Recommandation technique — candidate, pas choix final


Si WinUI 3 ne peut pas être construit dans l’environnement disponible ou s’il échoue au workflow/accessibilité, comparer une voie Windows .NET/Win32 avec WebView2. Electron reste un candidat possible à une comparaison instrumentée mais n’est ni choisi ni présenté comme le produit. L’évaluation esthétique d’un shell doit utiliser la même hiérarchie UX et les mêmes tâches ; aucune conclusion de runtime ne vient de la qualité du panneau HTML rudimentaire.

**Ne pas retirer Linux par défaut.** Si Linux reste cible v1 — statut actuel du cadrage — il peut nécessiter un shell et un backend navigateur distincts, tout en partageant les concepts, données, workflow et règles métier. Un partage de code ne doit pas obliger à partager une UI que l’utilisateur ne souhaite pas. Le shell Linux reste à instruire et doit être éprouvé sur Ubuntu/Fedora, notamment pour WebKitGTK ou toute autre intégration choisie.

## Microsoft Store et packaging

L’utilisateur privilégie maintenant un **build/install communautaire documenté** pour l’instant. Le Store n’est pas requis dans le gate v1 actuel ; son éventuelle adoption ultérieure comme canal additionnel pourra être réévaluée. Un build installable communautaire, sa chaîne de construction, le runtime WebView2, les dépendances et leurs licences restent à instruire avant toute promesse de distribution. MSIX, signature, certification et politiques Store ne sont à engager que si le canal Store redevient une priorité. Le Store est un canal de distribution, pas un moteur d’interface ni une licence open source.

Lucide est l’iconographie demandée. Le registre de composants devra inclure le paquet et ses avis/licences ; la documentation Lucide indique des conditions ISC et des avis MIT pour des icônes dérivées de Feather. Vérifier les textes exacts du paquet livré et inclure les notices dans chaque artefact. Aucune conclusion juridique globale ne découle du nom de licence.

## Spécification et spike Windows suivant

Réutiliser le flow confirmé dans `spikes/workflow-ui/` et faire évoluer le prototype WPF vers le shell de navigation confirmé. Le prochain petit slice doit contenir :

1. barre supérieure frameless sans logo : hamburger/options, accès projets, onglets ouverts fermables/réordonnables, plus/nouveau, indicateur d’update, bascule du panneau latéral, commandes réduire/agrandir/fermer ;
2. toutes les icônes Lucide avec nom accessible, tooltip, focus visible et commandes clavier ; gestion des onglets sans suppression du dossier ;
3. UI métier dans le shell natif, non servie comme site externe ;
4. WebView2 limité à la fixture synthétique, page et panneau d’audit visibles ensemble ;
5. inspection souris/clavier d’un élément, métadonnées observées, règle candidate et état de vérification, sans prétendre que l’automatisation détermine la conformité ;
6. décisions et preuves rattachées au bon dossier/page/élément/règle, clairement synthétiques ;
7. build communautaire reproductible et installation locale documentés ; aucun MSIX/Store requis pour ce gate actuel.

### État du spike WPF (29 octobre 2026)

Une première preuve WPF/.NET 10 + WebView2 existe dans `spikes/windows/wpf-native-spike/`. Le build `dotnet build RgaaAuditor.Windows.csproj` réussit (0 avertissement, 0 erreur) ; `dotnet list ... package --vulnerable --include-transitive` ne signale pas de package vulnérable via NuGet. Le shell frameless, les menus, les icônes Lucide, une fixture française synthétique, l’inspection, une décision humaine, la reprise locale et une capture PNG ont été exercés manuellement au cours de la session.

Ce résultat ne satisfait qu’une partie du spike : il n’y a ni suite d’intégration automatisée, ni validation clavier complète ou Narrateur/NVDA, ni vrai site, authentification, tests de redirection/fenêtres, installation propre, validation Linux, ni approbation utilisateur de l’ancien rendu WPF. Une smoke UI Automation ponctuelle a exercé la liste de reprise au clavier simulé, F6/Escape dans la fixture et le cycle deux audits / décisions-preuves distinctes / redémarrage / reprise ; un script local vérifie les associations des manifestes, événements et PNG. La fixture reste la seule origine de navigation. Ce prototype confirme la faisabilité d’un candidat WPF local, pas la faisabilité WinUI 3, le choix de stack, la sécurité de production ou un chemin d’installation final.

**Recommandation de la prochaine étape :** garder la fixture comme limite de navigation, poursuivre les validations clavier/Narrateur (NVDA si disponible), puis implémenter la barre supérieure confirmée. L’ADR 0014 confirme maintenant un onglet par dossier d’audit. Toute navigation réelle reste un spike distinct, après tests de données/profil et sécurité.

### Gate de sortie du spike Windows

- l’app s’ouvre comme fenêtre desktop autonome ; aucune UI métier n’est fournie sous forme de site de production externe ;
- fenêtre frameless utilisable : déplacement/redimensionnement, boutons système, menu, clavier, focus et lecteur d’écran sont vérifiés ;
- même tâche d’audit comprise et réalisable qu’avec la maquette de parcours ;
- cible WebView2 séparée, accès limité, sélection/contextes/captures observables et leurs limites documentées ;
- preuve de build/install communautaire propre avec dépendances/runtime documentés ; MSIX/Store hors gate actuel ;
- revue humaine UX/accessibilité, sécurité et dépendances/licences ; aucune vulnérabilité critique non traitée ;
- le rapport du spike distingue PASS/FAIL/UNKNOWN et ne présente pas WinUI/WebView2 ou Microsoft Store comme définitivement retenus.

## Options écartées ou conservées

- **Continuer à développer le panneau Electron actuel comme UI métier :** écarté ; l’utilisateur rejette cette présentation et elle n’exprime pas le workflow attendu.
- **Déduire qu’Electron est techniquement impossible parce que son panneau est rudimentaire :** rejeté ; un moteur n’est pas une direction visuelle. Electron n’est cependant pas retenu par défaut.
- **Retirer Linux v1 à la suite de la préférence Windows :** non décidé et contraire au baseline précédent sans confirmation explicite.
- **Promettre tout de suite une publication Store :** non décidé ; certification, MSIX, signature et distribution du moteur restent à démontrer.
- **Adopter une seule UI web multicoque pour préserver le partage :** non retenu par défaut ; le cœur métier peut être partagé sans imposer la même surface d’UI à Windows et Linux.

## Questions ouvertes à l’utilisateur

1. Le style frameless est-il demandé uniquement sous Windows (hypothèse de travail) ou également pour l’application Linux ? Linux lui-même reste dans le périmètre confirmé.

## Sources techniques à vérifier avant décision

- Microsoft Learn, [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/) et [WebView2 overview/features/APIs](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/overview-features-apis), consultés le 29 octobre 2026.
- Microsoft Learn, [WebView2 distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution) et [Store policies](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies) — revérifier les versions/règles actuelles avant un engagement de distribution.
- Lucide, [licence](https://lucide.dev/license) — vérifier le paquet et les avis exacts livrés.
- Tauri v2, [versions de WebView par plateforme](https://v2.tauri.app/reference/webview-versions/) comme candidate multiplateforme à comparer, sans supposer la parité de moteur sur les distributions Linux.

## Revue indépendante

Le conseil conserve WinUI 3 + WebView2 comme candidat Windows possible, avec un shell Linux distinct tant que Linux reste v1 ; aucun runtime n’est décidé. L’utilisateur préfère désormais build/install communautaire pour le moment. La chaîne d’installation propre et la compatibilité du shell avec le parcours restent à démontrer. Le conseil recommande aussi de garder les onglets comme vues de dossiers d’audit jusqu’à clarification du mot « projet » (ADR 0014).
