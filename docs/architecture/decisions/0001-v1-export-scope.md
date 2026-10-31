# ADR 0001 — Périmètre des sorties d’export v1.0.0

- **Statut :** sorties, contenu du rapport, rapports d’étape et sélection explicite des preuves confirmés par l’utilisateur ; contrat JSON détaillé et modèle canonique recommandés, non encore approuvés en détail ; Jira suivi dans l’ADR 0002
- **Date :** 2026-10-28

## Contexte

Les usages confirmés demandent un PDF client, un document modifiable d’équipe et des données structurées pour le suivi technique. L’utilisateur confirme PDF, préfère ODT et accepte JSON. L’utilisateur confirme également un besoin de connexion et création de tickets Jira avec suivi de correction ; le périmètre détaillé est traité dans l’ADR 0002.

Le contenu minimal du PDF client est confirmé : client et site, périmètre des pages, référentiel et version, résumé des résultats, constats avec critères et preuves, limites des vérifications automatisées et vérifications restant à faire. L’ODT est une version modifiable de ce même rapport. Le client/organisation y figure dans le PDF ; selon la préférence utilisateur, il n’est pas nécessaire dans l’ODT ni le JSON.

L’utilisateur confirme qu’un rapport peut être produit même si des vérifications restent à faire, à condition que son caractère incomplet et les éléments restants soient clairement signalés. Il confirme aussi que les preuves incluses dans le rapport sont sélectionnées explicitement par l’auditeur.

## Décision recommandée

Respecter les trois sorties choisies par l’utilisateur pour la v1.0.0 : (1) PDF destiné au client, (2) ODT modifiable pour l’équipe, (3) JSON structuré pour le suivi technique. Recommandation d’architecture : produire ces sorties à partir d’un modèle canonique d’audit, plutôt que de maintenir trois sources distinctes. Le schéma JSON doit être versionné/documenté et préserver des identifiants stables, versions de référentiels/règles, pages, constats, preuves, informations de correction et distinction entre origine automatique et jugement humain.

Le PDF et l’ODT doivent restituer le contenu de rapport confirmé ci-dessus. Les résultats automatisés sont clairement identifiés comme tels et ne valent pas une conclusion de conformité ; contrôles et vérifications restant sont visibles. Aucun score ou verdict de conformité global ne doit être déduit de l’automatisation seule. Les suggestions RGAA non confirmées restent hors du PDF/ODT ; elles ne deviennent un résultat de rapport qu’après décision de l’auditeur.

Permettre l’export d’un rapport d’étape à tout moment. Si des vérifications restent en attente, afficher une mention proéminente « Rapport d’étape — incomplet » (libellé recommandé, à valider dans la spécification UX), l’état et le nombre/nature des contrôles restant ; la génération ne change pas l’état de l’audit. Les preuves du rapport sont choisies explicitement par l’auditeur ; les pièces non sélectionnées restent absentes, sans que leur absence soit interprétée comme une absence de problème.

L’utilisateur préfère ODT même si Word est fréquent chez les destinataires ; la compatibilité des documents produits dans LibreOffice et Word doit être testée. L’ODT modifiable est la version du même rapport, pas un outil de suivi distinct. Les modifications réalisées hors de l’application ne mettent pas à jour l’audit canonique. L’export JSON reste autonome et versionné. Il n’est pas présumé être automatiquement importable par Jira ; l’intégration directe Jira est décrite séparément dans l’ADR 0002.

## Alternatives et arbitrage

- **DOCX à la place d’ODT :** l’usage fréquent de Word rend DOCX plausible, mais l’utilisateur a explicitement préféré ODT. Ne changer ce choix qu’avec une nouvelle décision utilisateur fondée sur des tests de compatibilité et besoins des destinataires.
- **DOCX et ODT en v1 :** couverture plus large, mais coût de génération et validation accru ; pas demandé à ce stade.
- **JSON seul pour tickets Jira :** l’utilisateur a aussi exprimé le besoin de connexion et création de tickets ; voir l’ADR 0002 qui recommande une portée intégrée limitée avec JSON indépendant comme repli.
- **Un seul format :** ne satisfait pas les trois usages confirmés.

Le modèle canonique suivi de trois rendus répond aux usages tout en limitant la divergence entre sorties. Les détails d’intégration Jira ne relèvent pas de cet ADR ; voir l’ADR 0002.

Une modification ultérieure de l’audit donne lieu à une nouvelle génération ; un fichier exporté ne modifie pas l’audit canonique et n’est pas resynchronisé automatiquement.

## Conséquences et validation

Spécifier le contrat canonique, le versionnement et la compatibilité ; documenter le JSON avec exemples et validation. Tester la fidélité des liens critères-constats, preuves, provenance et état d’audit. Tester ODT dans LibreOffice et Word (pagination, tableaux, caractères français, images, édition et accessibilité), sur les plateformes visées ; vérifier les licences des bibliothèques. Tester aussi pagination et complétude du PDF. Les limites de taille et le détail des champs restent à spécifier.

Tester explicitement les rapports incomplets, la visibilité des contrôles restant, la séparation des origines automatiques/humaines, la sélection des preuves, les cas sans preuve sélectionnée, la génération à partir d’un même état d’audit, et l’absence d’écriture de retour depuis l’ODT.

## Statut des inputs

L’utilisateur a confirmé les trois usages et préfère les formats PDF, ODT et JSON. Il a confirmé le contenu de base du rapport PDF client, que l’ODT est sa version modifiable, que les rapports d’étape sont autorisés si leur incomplétude est signalée, que les preuves sont sélectionnées explicitement et que le libellé client figure seulement dans le PDF client. Le modèle canonique d’audit et le libellé exact des états incomplets restent des recommandations à spécifier.
