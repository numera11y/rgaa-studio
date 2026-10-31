# ADR 0008 — Conservation locale des dossiers d’audit et preuves

- **Statut :** recommandé — règle provisoire d’architecture, non présentée comme une durée légale ni comme une décision utilisateur ; durée obligatoire à instruire
- **Date :** 2026-10-29

## Contexte et éléments établis

L’utilisateur confirme que les preuves doivent rester disponibles afin de pouvoir les demander ou les revoir bien plus tard. La durée légale/obligatoire n’est pas connue et aucune durée d’expiration automatique n’a été définie. Les types de preuves attendus sont notes, étapes de reproduction, captures d’écran et extraits DOM. Ils sont conservés localement dans le dossier d’audit ; l’auditeur les sélectionne explicitement pour le PDF client. Les secrets sont exclus. Les profils navigateur, qui contiennent notamment l’état d’authentification, sont stockés séparément et indépendamment des audits. Aucun code produit n’existe encore.

Avis spécialistes reçus et communiqués pour cet arbitrage :
- **Product strategist :** preuves comme enregistrements de première classe liés à l’audit/page/constat, avec dates et contexte ; stockage local et portabilité ; ne pas affirmer une durée légale ni supprimer arbitrairement ; suppression explicite ; une preuve n’assure pas la reproduction future de la page vivante ; avis juridique pour la durée.
- **Auditor UX :** parcours de reprise d’audit, visibilité de l’état enregistré et accès aux preuves sauvegardées ; contextualiser et décrire honnêtement les limites ; flux clair d’export/suppression ; ne pas inventer de durée ou d’expiration ; distinguer cycle de vie des preuves et de l’audit.
- **Browser architecture :** stockage local versionné, paquet portable, métadonnées/intégrité par empreinte sans prétention d’authenticité ; captures/DOM ne garantissent pas la reproduction future ; capture atomique, sauvegardes et migrations à prévoir ; secrets exclus ; revue juridique.
- **Open-source steward :** local-first/hors ligne, audit et preuves portables, aucun cloud requis ; ne pas supprimer silencieusement ; données sensibles, documenter sauvegarde/effacement ; avis juridique nécessaire.

Ces positions convergent sur l’absence d’expiration implicite, la séparation des profils et la nécessité d’une politique juridique future. Aucun élément reçu ne détermine une durée légale applicable. Ces avis sont ceux fournis au conseil ; ils ne constituent pas une nouvelle consultation indépendante durant la rédaction de cet ADR.

## Décision / recommandation

1. Modéliser l’audit comme dossier local canonique contenant ses preuves et leurs liens audit/page/constat/règle, ainsi que dates et contexte disponible (notamment URL). Distinguer nettement le profil navigateur stocké séparément : suppression, copie ou export d’un audit ne doit ni inclure ni modifier le profil.
2. **Valeur par défaut provisoire :** conserver les dossiers et preuves jusqu’à suppression explicite par l’utilisateur. Ne pas appliquer de délai d’expiration, purge ou effacement automatique arbitraire. Ce choix rend possible la consultation longtemps après et évite de faire passer une durée inventée pour une obligation ; il ne préjuge pas de la future règle légale, contractuelle ou organisationnelle.
3. Prévoir dès la conception suppression explicite compréhensible, sauvegarde/restauration et export portable versionné. Un export PDF/ODT/JSON est une sortie choisie, pas une sauvegarde complète garantie du dossier ; préciser le contenu et les limites de chaque format. L’utilisateur doit pouvoir examiner les pièces et choisir celles du PDF ; aucune preuve brute n’est incluse implicitement dans une sortie. Ne jamais exporter cookies, jetons, identifiants ou données du profil.
4. Prévoir métadonnées de capture et contrôles d’intégrité (par exemple empreinte) comme aide à détecter une altération accidentelle, sans les présenter comme preuve d’origine, horodatage certifié ou authenticité. Les captures, notes et DOM décrivent un état observé ; elles ne garantissent ni la disponibilité future du site, ni sa reproduction fidèle.
5. Avant publication, établir avec une revue juridique adaptée aux usages/juridictions cibles les obligations de conservation, suppression, minimisation et éventuelle conservation légale. Tant que ce travail n’est pas fait, communiquer que la durée applicable reste à déterminer ; ne revendiquer aucune conformité à un délai légal.

## Alternatives et arbitrage

- **Expiration automatique après une durée arbitraire :** rejetée, car aucune durée n’est étayée et l’utilisateur demande de pouvoir revoir les preuves bien plus tard ; risque irréversible de perte.
- **Prétendre que la conservation indéfinie satisfait une obligation légale :** rejeté, faute d’analyse juridique, de juridiction et de contexte contractuel.
- **Séparer les preuves des dossiers ou les conserver dans le profil navigateur :** rejeté, contraire au besoin de dossier local autonome et risque de mélange avec des secrets/session.
- **Cloud/service de conservation obligatoire :** rejeté pour le MVP ; non nécessaire au workflow, augmente dépendances, exposition et complexité de distribution. Une synchronisation future nécessiterait une décision distincte.
- **Questionner l’utilisateur sur une durée légale :** non retenu : cette durée dépend d’une analyse juridique et d’un contexte qui ne peuvent être remplacés par une préférence personnelle. Aucun choix produit bloquant ne requiert maintenant un délai précis.

## Conséquences, risques et validation

Le modèle local doit survivre à la fermeture et permettre de reprendre un audit avec ses preuves ; prévoir sauvegarde/restauration, migrations versionnées et export portable. Les copies de sauvegarde héritent de la sensibilité des preuves ; la suppression du dossier ne peut prétendre effacer les copies sans politique et interface explicites. Le comportement de suppression, y compris pièces jointes, index et sauvegardes, reste à concevoir et à tester. Captures/DOM peuvent contenir des données personnelles ou confidentielles : aperçu, sélection, omission/masquage avant export et avertissement sont nécessaires. La politique de sauvegardes et l’effacement sécurisé dépendent du système de fichiers et ne doivent pas être surpromis.

Validation avant implémentation/publication : tests de fermeture/reprise, migration, export/import portable, panne pendant capture, empreinte après transfert, suppression explicite, absence de secrets dans dossier/export/journaux, et parcours clavier/lecteur d’écran ; documenter précisément ce qui est sauvegardé et supprimé. Revue juridique à planifier avant toute déclaration de conformité ou engagement sur une durée. Réexaminer cet ADR après cette revue et avant gel de la politique de conservation v1.

## Statut des inputs

- **Confirmé par l’utilisateur :** preuves destinées à pouvoir être demandées/revues bien plus tard ; notes/reproduction/capture/DOM ; conservation locale dans l’audit ; sélection explicite pour PDF client ; exclusion des secrets ; profils navigateur séparés et indépendants ; aucune implémentation produit à ce jour.
- **Non déterminé :** durée légale/obligatoire, juridictions et contextes contractuels applicables ; aucune auto-expiration n’a été définie.
- **Recommandation du conseil, non approbation utilisateur :** conservation locale jusqu’à suppression explicite, sans purge automatique provisoire ; portabilité, sauvegardes/migrations et communication transparente des limites.
