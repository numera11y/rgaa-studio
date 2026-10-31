# ADR 0002 — Étude Jira non bloquante et périmètre candidat

- **Statut :** préférence utilisateur confirmée pour une intégration depuis l’application, non prioritaire et non bloquante ; périmètre technique recommandé sous condition ; édition et authentification ouvertes
- **Date :** 2026-10-28

## Contexte

L’utilisateur souhaite à terme se connecter depuis l’application au Jira du client ou au sien, créer des tickets, éviter les doublons, suivre les corrections et faire revérifier la page. Il ne connaît pas l’édition Jira et précise que la fonctionnalité n’est pas prioritaire et ne doit pas ralentir le développement du reste. Il accepte une lecture du statut Jira à la demande, puis un retest humain séparé. Il préfère éviter un service auxiliaire ; Cloud ou Data Center restent inconnus. Les avis spécialisés convergent sur la séparation du suivi d’audit et des statuts Jira. Ils divergent sur l’étendue d’un connecteur v1 : produit/UX proposent liaison et création explicite ; architecture et open source recommandent de découpler Jira du cœur et de n’ajouter qu’une portée testée.

Les avis ne démontrent ni la faisabilité d’OAuth desktop, ni les API/champs/permissions disponibles dans les instances visées, ni la parité Cloud/Data Center. Ce sont des inconnues, pas des choix arrêtés.

## Décision recommandée

Recommander que le cœur d’audit, les rapports et les exports soient réalisés et livrables sans Jira. Mener une étude Jira indépendante en parallèle, sans mettre en attente le travail principal. Un connecteur desktop direct et facultatif peut être ajouté en v1 uniquement si le spike est concluant et ne retarde pas la livraison ; sinon, le reporter après v1.0.0. Aucun service auxiliaire n’est prévu dans l’approche actuelle. Le flux candidat, à confirmer après l’étude :

1. Configurer une instance autorisée et choisir un projet. Le mécanisme de connexion dépendra de l’édition cible et reste à instruire.
2. Depuis un constat examiné et confirmé par l’auditeur, rechercher des tickets candidats, les examiner et soit lier explicitement un ticket existant, soit prévisualiser puis confirmer la création.
3. Conserver localement la référence (instance, clé, URL) liée au constat, sans secrets dans audit, rapports, exports ou journaux.
4. Consulter en ligne l’état Jira à la demande et ouvrir le ticket. L’utilisateur accepte cette lecture à la demande ; les commentaires synchronisés et transitions écrites restent différés.
5. Présenter l’état Jira comme information distante et datée. L’état d’audit/correction reste distinct. Après correction, l’auditeur revient sur la page, reteste manuellement le constat et consigne résultat, preuve et date dans l’audit. Ce retest ne change pas automatiquement Jira ; l’utilisateur peut ensuite agir dans Jira.
6. Garder un historique local append-only des événements utiles (liaison/création, consultation d’état, retest, dissociation), sans prétendre reproduire l’historique des commentaires/transitions Jira. JSON demeure un repli autonome, pas une intégration Jira en soi.

La recherche de doublons est une aide : afficher des candidats et permettre liaison ou création confirmée. Ne pas promettre une détection exhaustive ni l’absence garantie de doublons ; résultats et droits API peuvent être limités. Une création reste toujours explicitement confirmée, même sans candidat.

La stratégie Cloud/Data Center n’est pas décidée. Séparer le contrat fonctionnel des adaptateurs d’API/authentification. Faire un spike time-boxé hors chemin critique : identifier une instance/édition représentative, version, autorisation, champs, permissions et exigences de distribution, puis tester le flux minimal depuis l’application desktop. Si l’authentification directe n’est pas faisable sans service auxiliaire, si aucun environnement représentatif n’est disponible ou si le spike risque de retarder le cœur, reporter Jira ; l’audit et le JSON restent pleinement utilisables.

## Avis, dissension et arbitrage

- **Product strategist :** création depuis constat examiné, liaison manuelle existante, clé/lien, retest humain ; pas de sync, mise à jour ou dédoublonnage automatique ; demande édition et identité/instance.
- **Auditor UX :** connexion, sélection projet, aperçu/confirmation, recherche de correspondances candidates, liaison ou création ; retest distinct ; demande auth, tenant et champs.
- **Browser architecture :** Jira non-dépendance du MVP audit ; si nécessaire, Cloud online comme scope initial, sélection site/projet, recherche, création/lien, sans sync générale ; identifiants locaux ; OAuth desktop à tester ; Data Center après précision.
- **Open-source steward :** connecteur desktop facultatif, sans service intermédiaire ; OAuth Cloud sous réserve d’approbation de distribution ou adaptateur Data Center ; état en ligne et retest distincts ; sync auto/multi-DC après validation.

La recommandation retient liaison manuelle et recherche de candidats (produit/UX) plutôt que la seule création, car elles répondent aux tickets déjà existants et au risque de doublon. Elle suit architecture/open source sur l’intégration facultative, l’autonomie du cœur d’audit et l’absence de synchronisation générale. La préférence architecture pour Cloud seul ne devient pas un choix : choisir Cloud sans preuve pourrait exclure le besoin client ; promettre les deux éditions serait également non étayé et coûteux. Le spike résout cette incertitude sans bloquer le reste du produit.

## Alternatives considérées

- **JSON seulement :** robuste et réversible, mais ne répond pas suffisamment au besoin exprimé de connexion, création et allers-retours ; repli si l’intégration n’est pas faisable.
- **Création seulement, sans liaison :** ne traite pas les tickets déjà présents ni correctement les doublons ; rejeté.
- **Sync bidirectionnelle des statuts/commentaires/transitions :** reportée ; conflits de sources de vérité, dépendances aux workflows/droits et risque de confondre statut Jira et résultat d’audit.
- **Cloud uniquement dès maintenant :** réduit potentiellement l’effort, mais la cible réelle est inconnue ; pas d’engagement sans validation.
- **Cloud et Data Center en v1 :** non retenu faute de versions, auth et instance confirmées ; risque de compromettre un MVP testable.

## Conséquences, risques et validation

Jira est source de vérité de ses tickets/statuts ; l’application est source de vérité des audits, constats, preuves et retests. Les pannes réseau, accès refusés, tickets supprimés et échecs partiels doivent être visibles sans perte de données locales. Secrets protégés, révocables/effaçables et exclus des sorties ; aucun service auxiliaire n’est prévu. L’UX doit expliciter le caractère non exhaustif des candidats. Le spike et le connecteur restent hors chemin critique.

Le spike vérifie édition/version, auth desktop et approbation de distribution, permissions minimales, découverte instance/projet, champs, recherche, liaison/création, limites API, protection locale et comportement hors ligne. Les critères à spécifier comprennent : liaison existante ou création avec aperçu/confirmation, référence persistante, état distant daté, retest manuel avec résultat/preuve/historique indépendant, erreurs sans perte et aucune écriture de transition/commentaire Jira en v1.

**Bloqueurs ouverts pour le connecteur uniquement (pas pour le cœur d’audit) :** Cloud/Data Center et versions, instance/tenant et autorisation de connexion, auth/distribution, champs/projet, données/preuves transmises, conservation et critères détaillés de retest. Ni édition, ni OAuth, ni détails d’API ne sont arrêtés.

## Statut des inputs

L’utilisateur souhaite une connexion Jira depuis l’application, mais dit que cette fonctionnalité n’est pas prioritaire et ne doit pas ralentir le reste ; il accepte la lecture de statut à la demande et le retest humain séparé. Le statut de l’intégration comme non bloquante est confirmé. La portée exacte de liaison/création, Cloud/Data Center et l’authentification restent ouverts ; aucune édition ou méthode technique n’est choisie.
