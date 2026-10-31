# ADR 0006 — État de revue et résultat de conformité

- **Statut :** recommandation actualisée ; préférence utilisateur pour une suggestion confirmée. Les invariants de revue/résultat sont confirmés ; algorithme détaillé recommandé, à valider contre les sources officielles et le catalogue v1
- **Date :** 2026-10-28

## Contexte

L’utilisateur précise que la règle est évaluée par l’auditeur, qui doit définir un résultat de conformité par règle. Il accepte un état de revue distinct (« Revue terminée ») et confirme aussi un niveau WCAG cible A, AA ou AAA séparé des résultats par règle. La page est terminée lorsque toutes les règles de son périmètre sont traitées ; l’audit multi-pages l’est lorsque toutes ses pages le sont. Les rapports d’étape restent autorisés si les règles restantes sont explicitement signalées. Les décisions « non applicable » et « non vérifiable » avec justification sont permises.

Le référentiel est unique par audit. La version exacte doit être enregistrée. WCAG 2.2 (source W3C consultée le 28 octobre 2026) comporte des niveaux A, AA et AAA ; un audit WCAG exige un niveau cible explicitement choisi au démarrage, sans niveau prérempli. Le RGAA n’utilise pas cette échelle WCAG. Le conseil a vérifié que RGAA 4.1.2 utilise une méthode par critères/tests : tous les tests applicables sur les éléments concernés doivent réussir pour qu’un critère soit conforme sur une page ; un échec applicable suffit à le rendre non conforme. Le détail de cette agrégation est à confronter au référentiel officiel au moment d’implémenter.

Sources : [W3C — Understanding Conformance, WCAG 2.2](https://www.w3.org/WAI/WCAG22/Understanding/conformance) ; sources RGAA de méthode/évaluation consignées par l’agent err0r-jury dans son avis (l’accès direct de l’outil à la page RGAA a déjà retourné 403, à revérifier avant de figer le moteur de règles).

La méthode RGAA 4.1.2 s’appuie sur WCAG 2.1 niveaux A/AA ; elle ne doit pas être présentée comme une implémentation de WCAG 2.2. Sources signalées par le conseil : [RGAA — critères et tests](https://accessibilite.numerique.gouv.fr/methode/criteres-et-tests/), [RGAA — évaluation de conformité](https://accessibilite.numerique.gouv.fr/obligations/evaluation-conformite/), [RGAA — introduction à la méthode](https://accessibilite.numerique.gouv.fr/methode/introduction/) et [WCAG 2.2 — conformance requirements](https://www.w3.org/TR/WCAG22/#conformance-reqs).

## Décision confirmée

1. Séparer **état de revue** et **résultat d’accessibilité/conformité**.
2. « Revue terminée » signifie que l’auditeur a évalué la règle dans le contexte de son audit et a sélectionné un résultat. Ce statut ne signifie pas « conforme ».
3. Une règle/page restant à examiner conserve un état incomplet. Un audit multi-pages reste incomplet tant qu’une page du périmètre a des règles à traiter. Un rapport d’étape peut être exporté avec les éléments restants visibles.
4. Les détections automatiques ne définissent pas le résultat de conformité. L’auditeur doit sélectionner le résultat ; « aucun problème détecté » automatiquement n’équivaut pas à « conforme ».
5. « Non applicable » et « Non vérifiable » nécessitent une justification, conformément au principe accepté par l’utilisateur.
6. Pour un audit WCAG, enregistrer le niveau cible choisi (A, AA ou AAA) séparément des résultats par règle. Le niveau visé n’est pas un résultat atteint ni une déclaration de conformité.
7. Exiger une sélection explicite de ce niveau lors de la création d’un audit WCAG ; ne pas préremplir A, AA ou AAA. Ne pas présenter ce champ pour les audits RGAA.
8. **Granularité recommandée :** le résultat saisi par l’auditeur est au niveau du critère/règle et de la page. Pour RGAA, les tests et instances observés restent visibles sous le critère et étayent son résultat. Toute agrégation/suggestion de l’application suit la méthode officielle de la version retenue et ne remplace pas la décision de l’auditeur.
9. Pour WCAG, le niveau ciblé inclut les critères de ce niveau et des niveaux inférieurs (A, AA inclut A+AA, AAA inclut A+AA+AAA). Le niveau sélectionné décrit la cible, pas le résultat atteint.

## Vocabulaire de résultat accepté

Au niveau du critère/règle et de la page évaluée, l’auditeur choisit :

- **Conforme** — conclusion de l’auditeur pour le périmètre précisément évalué.
- **Non conforme** — un échec applicable a été constaté.
- **Non applicable** — justification requise ; ce n’est pas une preuve générale de conformité du site. Le RGAA définit quand un critère est non applicable ; vérifier et suivre les motifs officiels.
- **Non vérifiable** — statut de workflow proposé par le produit quand l’auditeur ne peut conclure ; justification requise et limite présentée dans le rapport. Ce libellé est une extension applicative, pas une catégorie normative RGAA/WCAG établie dans les sources consultées.

L’utilisateur a explicitement validé ces quatre résultats et rejeté « Partiellement conforme ». « Non vérifiable » reste une catégorie de workflow produit, pas un statut normatif RGAA/WCAG établi par les sources consultées.

### Implémentation v1 recommandée — préférence utilisateur confirmée, détails techniques non acceptés

L’utilisateur préfère désormais une suggestion calculée à partir des résultats des tests RGAA (« non j'aime l'idée d'une suggestion »). Cela confirme la préférence pour le mécanisme, pas l’acceptation de l’algorithme ci-dessous. En v1, proposer une suggestion par critère et page uniquement à partir des résultats explicitement évalués des tests RGAA et des informations d’applicabilité requises par la méthode/version figée. La suggestion est clairement « Suggestion — non enregistrée », expose sa base (comptages/éléments concernés et tests manquants), et n’est jamais une décision de conformité ni une certification. L’auditeur doit confirmer ou choisir un autre des quatre résultats ; aucune suggestion n’est enregistrée comme décision sans cette action.

L’utilisateur confirme que PDF/ODT n’affichent que le résultat confirmé par l’auditeur. Une suggestion non confirmée reste dans l’application ; dans un rapport d’étape, le critère est signalé comme restant à examiner sans exporter la suggestion.

Calcul recommandé par critère/page RGAA : **Non conforme** si au moins un test applicable sur un élément concerné est explicitement en échec ; **Conforme** seulement si la couverture est complète et que tous les tests applicables sur les éléments concernés sont explicitement réussis. Une absence de test applicable ne suffit pas à conclure « Non applicable » : cette voie exige la condition officielle d’inapplicabilité et une justification humaine vérifiée. « Non vérifiable » est une décision humaine justifiée, non une inférence automatique. Tests non évalués, applicabilité indéterminée, couverture partielle ou résultat contradictoire/ambigu donnent **aucune suggestion** (avec explication des manques), jamais une suggestion conforme par défaut. Une détection automatique brute ne constitue pas un résultat de test et n’alimente pas directement le calcul ; elle peut être montrée comme preuve/indice distinct, après qualification humaine elle seule peut alimenter un test évalué.

La suggestion porte au niveau du **critère pour une page**, pas au niveau d’un test : les tests/instances restent les unités d’observation et de justification. Pour plusieurs pages, conserver chaque résultat/suggestion par page. L’agrégation RGAA au niveau de l’échantillon/service suit la méthode officielle : le critère ne peut être déclaré conforme pour le service que s’il est conforme sur toutes les pages de l’échantillon où il s’applique ; un échec applicable sur une page empêche cette conclusion. Ne pas moyenner, extrapoler à tout le site, ni confondre fin de revue et conformité. La portée exacte d’une conclusion de service dépend de l’échantillon et du processus officiel, pas d’une simple agrégation d’écrans.

Si les résultats de tests changent après confirmation/sauvegarde, ne jamais écraser la décision de l’auditeur ; signaler que sa base a changé et permettre une réévaluation explicite. Ne jamais marquer « Revue terminée » à la génération d’une suggestion.

Séparer les résultats de sous-tests/contrôles, lorsque le référentiel en fournit, du résultat du critère. Pour RGAA, les tests et instances restent visibles sous le critère et étayent le résultat que l’auditeur confirme. La méthode officielle RGAA comporte une règle d’agrégation ; toute suggestion de résultat par l’application doit être vérifiée contre la version exacte avant implémentation. WCAG porte le résultat sur le critère de succès pour une page complète et tout processus complet applicable. Ne pas proposer « partiellement conforme » au niveau atomique ; une situation mixte reste visible dans les tests/constats et l’échec ne doit pas être masqué. Ne pas produire de score ou de conclusion de conformité globale à partir de l’avancement de revue.

## Modèle d’état recommandé

Pour chaque critère ou règle dans le périmètre :

| État de revue par page/critère | Résultat auditeur par page/critère | Effet sur complétude |
|---|---|---|
| À examiner / En cours | Aucun résultat final | Page/audit incomplet |
| Revue terminée | Conforme | Règle traitée ; conclusion conforme limitée au périmètre |
| Revue terminée | Non conforme | Règle traitée ; échec reste visible |
| Revue terminée | Non applicable + justification | Règle traitée avec exception documentée |
| Revue terminée | Non vérifiable + justification | Revue traitée mais résultat indéterminé ; limitation visible, ne compte pas comme conforme |

Pour un lot, chaque page conserve son résultat par critère ; l’audit devient « Revue terminée » uniquement quand toutes les règles/pages incluses ont reçu une décision. Pour RGAA, un critère est conforme pour le service audité seulement s’il l’est sur toutes les pages de l’échantillon où il s’applique ; préserver les résultats par page et ne pas moyenner pour masquer un échec. Pour WCAG, toute déclaration de conformité implique les pages complètes et les pages des processus complets du périmètre ; un lot de pages sélectionnées ne justifie pas une revendication concernant le site entier.

## Alternatives arbitrées

- **Un seul statut « Validé » :** rejeté, car confond l’examen du critère avec sa conformité.
- **Ajouter « Partiellement conforme » :** rejeté explicitement par l’utilisateur. Les tests ou constats particuliers décrivent les cas mixtes sans créer un résultat partiel ambigu au niveau du critère.
- **Déduire/enregistrer automatiquement Conforme d’un contrôle automatisé sans alerte :** rejeté, car l’automatisation n’établit pas la conformité et peut manquer des problèmes nécessitant jugement humain. Une suggestion fondée sur des tests RGAA évalués n’est qu’une aide, soumise à confirmation explicite.
- **« À déterminer » comme résultat final après revue terminée :** non retenu dans le modèle recommandé ; si l’auditeur ne peut pas conclure, « Non vérifiable » avec justification est plus explicite. Si la règle n’a pas été examinée, elle reste « À examiner » et l’audit incomplet.
- **Niveau WCAG sélectionné automatiquement :** rejeté ; le niveau est une préférence de mission/politique et demeure à choisir par l’utilisateur.

## Conséquences, validation et questions ouvertes

L’UI, les exports et le JSON distinguent au minimum le type de contrôle (automatique ou humain), le résultat observé, la décision de l’auditeur et l’état de revue. Une page peut être « Revue terminée » avec des non-conformités ; le rapport doit le montrer sans les masquer par un statut global.

Avant implémentation, vérifier la table d’agrégation et les cas d’applicabilité contre le texte officiel RGAA 4.1.2 exact et les données du catalogue ; conserver la version des règles/tests. Ajouter des tests unitaires couvrant échec applicable, réussite complète, tests manquants, applicabilité ambiguë, absence de tests applicables, non-applicabilité justifiée, non-vérifiable, changement après sauvegarde et agrégation multi-pages. Confirmer l’accessibilité des libellés, explications et actions. La préférence pour une suggestion est confirmée ; algorithme, seuils de couverture et interaction détaillée restent des recommandations du conseil.

Critères de test : un résultat automatique sans détection ne peut pas devenir Conforme ; un constat Non conforme peut coexister avec Revue terminée ; Non applicable/Non vérifiable exigent une justification ; contrôles en attente gardent rapport/audit incomplet ; audit multi-pages ne se termine pas tant qu’une page reste non traitée ; PDF/ODT/JSON préservent ces distinctions.

## Statut des inputs

- **Confirmé par l’utilisateur :** l’auditeur (et non une validation client) effectue la revue et définit le résultat par règle ; état « Revue terminée » distinct du résultat ; chaque audit WCAG exige le choix initial d’un niveau cible A/AA/AAA sans valeur par défaut ; les tests RGAA restent visibles sous le critère et étayent son résultat ; quatre résultats acceptés ; « Partiellement conforme » exclu ; les règles non applicable/non vérifiable sont justifiées.
- **Préférence utilisateur confirmée :** une suggestion calculée à partir des résultats des tests RGAA, le résultat final restant une décision de l’auditeur.
- **Confirmé par l’utilisateur :** PDF/ODT n’affichent que les résultats confirmés ; une suggestion non confirmée reste dans l’application et, en rapport d’étape, le critère est indiqué comme à examiner sans exporter la suggestion.
- **Recommandation du conseil, détails ouverts :** calcul au critère/page seulement quand résultats de tests et applicabilité sont suffisamment complets et non ambigus ; aucune suggestion dans les autres cas ; confirmation/override explicite ; pas d’écrasement d’une décision sauvegardée ; agrégation multi-pages distincte et fidèle à la méthode officielle. Les sources de test sont des évaluations explicites, non les détections brutes.
- **Non demandé à l’utilisateur pour l’instant :** la préférence générale est résolue ; le travail restant est une vérification normative/technique et des tests, pas un arbitrage de préférence.
