# ADR 0006 — État de revue et résultat de conformité

- **Statut :** décision actualisée ; préférence utilisateur pour une suggestion et granularité de revue confirmées. Les invariants de revue/résultat sont confirmés ; algorithme détaillé recommandé, à valider contre les sources officielles et le catalogue v1
- **Date :** 2026-10-28

## Contexte

L’auditeur définit les résultats de conformité par critère. Pour un lot, il souhaite une seule conclusion par critère, étayée par l’applicabilité, les tests, constats et preuves rattachés à chaque page. L’état de revue (« Revue terminée ») reste distinct du résultat ; le niveau WCAG cible A, AA ou AAA est également distinct. L’audit est complet lorsque tous les critères du lot ont un résultat confirmé et une applicabilité déterminée sur chaque page. Les rapports d’étape restent autorisés si les critères restants sont explicitement signalés. Les résultats « Non applicable » et « Non vérifiable » avec justification sont permis.

Le référentiel est unique par audit. La version exacte doit être enregistrée. WCAG 2.2 (source W3C consultée le 28 octobre 2026) comporte des niveaux A, AA et AAA ; un audit WCAG exige un niveau cible explicitement choisi au démarrage, sans niveau prérempli. Le RGAA n’utilise pas cette échelle WCAG. Le conseil a vérifié que RGAA 4.1.2 utilise une méthode par critères/tests : tous les tests applicables sur les éléments concernés doivent réussir pour qu’un critère soit conforme sur une page ; un échec applicable suffit à le rendre non conforme. Le détail de cette agrégation est à confronter au référentiel officiel au moment d’implémenter.

Sources : [W3C — Understanding Conformance, WCAG 2.2](https://www.w3.org/WAI/WCAG22/Understanding/conformance) ; sources RGAA de méthode/évaluation consignées par l’agent err0r-jury dans son avis (l’accès direct de l’outil à la page RGAA a déjà retourné 403, à revérifier avant de figer le moteur de règles).

La méthode RGAA 4.1.2 s’appuie sur WCAG 2.1 niveaux A/AA ; elle ne doit pas être présentée comme une implémentation de WCAG 2.2. Sources signalées par le conseil : [RGAA — critères et tests](https://accessibilite.numerique.gouv.fr/methode/criteres-et-tests/), [RGAA — évaluation de conformité](https://accessibilite.numerique.gouv.fr/obligations/evaluation-conformite/), [RGAA — introduction à la méthode](https://accessibilite.numerique.gouv.fr/methode/introduction/) et [WCAG 2.2 — conformance requirements](https://www.w3.org/TR/WCAG22/#conformance-reqs).

## Décision confirmée

1. Séparer **état de revue** et **résultat d’accessibilité/conformité**.
2. « Revue terminée » signifie que l’auditeur a évalué la règle dans le contexte de son audit et a sélectionné un résultat. Ce statut ne signifie pas « conforme ».
3. Un critère du lot restant à examiner conserve un état incomplet. Un rapport d’étape peut être exporté avec les critères non terminés et les pages encore à vérifier visibles.
4. Les détections automatiques ne définissent pas le résultat de conformité. L’auditeur doit sélectionner le résultat ; « aucun problème détecté » automatiquement n’équivaut pas à « conforme ».
5. « Non applicable » et « Non vérifiable » nécessitent une justification, conformément au principe accepté par l’utilisateur.
6. Pour un audit WCAG, enregistrer le niveau cible choisi (A, AA ou AAA) séparément des résultats par règle. Le niveau visé n’est pas un résultat atteint ni une déclaration de conformité.
7. Exiger une sélection explicite de ce niveau lors de la création d’un audit WCAG ; ne pas préremplir A, AA ou AAA. Ne pas présenter ce champ pour les audits RGAA.
8. **Granularité confirmée :** l’auditeur enregistre un résultat unique par critère pour le lot de pages. Les tests, constats, preuves et décisions d’applicabilité restent rattachés aux pages qui les étayent ; ils ne deviennent pas des résultats de conformité concurrents au niveau de chaque page.
9. Pour WCAG, le niveau ciblé inclut les critères de ce niveau et des niveaux inférieurs (A, AA inclut A+AA, AAA inclut A+AA+AAA). Le niveau sélectionné décrit la cible, pas le résultat atteint.
10. Pour chaque paire critère/page, l’applicabilité est explicitement déterminée par l’auditeur. L’application ne l’infère pas du contenu inspecté ; « Non applicable » exige une justification. Un critère du lot ne compte comme terminé qu’après détermination de l’applicabilité sur toutes les pages, décision globale enregistrée et action explicite de fin de revue.

## Vocabulaire de résultat accepté

Au niveau du critère/règle pour le lot évalué, l’auditeur choisit :

- **Conforme** — conclusion de l’auditeur pour le périmètre précisément évalué.
- **Non conforme** — un échec applicable a été constaté.
- **Non applicable** — justification requise ; réservé au cas où le critère ne s’applique à aucune page du lot. Ce n’est pas une preuve générale de conformité du site. Le RGAA définit quand un critère est non applicable ; vérifier et suivre les motifs officiels.
- **Non vérifiable** — statut de workflow proposé par le produit quand l’auditeur ne peut conclure ; justification requise et limite présentée dans le rapport. Ce libellé est une extension applicative, pas une catégorie normative RGAA/WCAG établie dans les sources consultées.

L’utilisateur a explicitement validé ces quatre résultats et rejeté « Partiellement conforme ». « Non vérifiable » reste une catégorie de workflow produit, pas un statut normatif RGAA/WCAG établi par les sources consultées.

### Implémentation v1 recommandée — préférence utilisateur confirmée, détails techniques non acceptés

L’utilisateur préfère désormais une suggestion calculée à partir des résultats des tests RGAA (« non j'aime l'idée d'une suggestion »). Cela confirme la préférence pour le mécanisme, pas l’acceptation de l’algorithme ci-dessous. En v1, proposer une suggestion par critère et lot uniquement à partir des résultats explicitement évalués des tests RGAA et des informations d’applicabilité sur les pages selon la méthode/version figée. La suggestion est clairement « Suggestion — non enregistrée », expose sa base (pages, éléments concernés et tests manquants), et n’est jamais une décision de conformité ni une certification. L’auditeur doit confirmer ou choisir un autre des quatre résultats ; aucune suggestion n’est enregistrée comme décision sans cette action.

L’utilisateur confirme que PDF/ODT n’affichent que le résultat confirmé par l’auditeur. Une suggestion non confirmée reste dans l’application ; dans un rapport d’étape, le critère est signalé comme restant à examiner sans exporter la suggestion.

Calcul recommandé par critère/lot RGAA : **Non conforme** si au moins un test applicable sur une page du lot est explicitement en échec ; **Conforme** seulement si la couverture est complète sur toutes les pages où le critère s’applique et que tous les tests applicables sont explicitement réussis. Une absence de test applicable ne suffit pas à conclure « Non applicable » : cette voie exige la condition officielle d’inapplicabilité pour toutes les pages et une justification humaine vérifiée. « Non vérifiable » est une décision humaine justifiée, non une inférence automatique. Tests non évalués, applicabilité indéterminée, couverture partielle ou résultat contradictoire/ambigu donnent **aucune suggestion** (avec explication des manques), jamais une suggestion conforme par défaut. Une détection automatique brute ne constitue pas un résultat de test et n’alimente pas directement le calcul ; elle peut être montrée comme preuve/indice distinct, après qualification humaine elle seule peut alimenter un test évalué.

La suggestion porte au niveau du **critère pour le lot**, pas au niveau d’un test : tests et instances restent les unités d’observation et de justification, chacune conservée avec sa page. Un échec applicable observé sur une page empêche une conclusion Conforme pour le lot ; une conclusion Conforme exige une revue complète de toutes les pages où le critère s’applique et aucune applicabilité indéterminée. La portée exacte dépend de l’échantillon et du processus officiel ; le lot ne justifie pas une déclaration concernant le site entier.

Si les résultats de tests changent après confirmation/sauvegarde, ne jamais écraser la décision de l’auditeur ; signaler que sa base a changé et permettre une réévaluation explicite. Ne jamais marquer « Revue terminée » à la génération d’une suggestion.

Séparer les résultats de sous-tests/contrôles, lorsque le référentiel en fournit, du résultat du critère. Pour RGAA, les tests, constats et instances de chaque page étayent le seul résultat que l’auditeur confirme pour le lot. La méthode officielle RGAA comporte une règle d’agrégation ; toute suggestion de résultat par l’application doit être vérifiée contre la version exacte avant implémentation. WCAG porte le résultat sur le critère de succès appliqué aux pages complètes et à tout processus complet applicable ; l’interface conserve le résultat unique du lot sans transformer les constats page par page en verdicts distincts. Ne pas proposer « partiellement conforme » ; une situation mixte reste visible dans les tests/constats et l’échec ne doit pas être masqué. Ne pas produire de score de conformité à partir de l’avancement de revue.

## Modèle d’état

Pour chaque critère du lot, avec un état d’applicabilité distinct pour chacune de ses pages :

| Applicabilité par page | Résultat auditeur unique du lot | Effet sur complétude |
|---|---|---|
| À déterminer sur au moins une page | Aucun résultat final | Critère du lot incomplet |
| Déterminée pour chaque page | À examiner / En cours | Critère du lot incomplet |
| Déterminée pour chaque page | Conforme | Critère traité ; conclusion bornée à l’échantillon |
| Déterminée pour chaque page avec au moins une page applicable | Non conforme | Critère traité ; échec et page source restent visibles |
| Non applicable sur toutes les pages + justifications | Non applicable + justification globale | Critère traité comme exception, jamais comme Conforme |
| Déterminée pour chaque page | Non vérifiable + justification | Revue traitée avec limite explicite ; ne compte pas comme Conforme |

Les pages conservent leurs états d’applicabilité, tests, constats et preuves ; le résultat de conformité est unique pour le critère dans le lot. Toute modification d’applicabilité ou de périmètre après la décision rend la revue à reprendre. Pour RGAA, un critère est conforme pour le service audité seulement s’il l’est sur toutes les pages de l’échantillon où il s’applique ; préserver les éléments par page et ne pas moyenner pour masquer un échec. Pour WCAG, toute déclaration de conformité implique les pages complètes et les pages des processus complets du périmètre ; un lot de pages sélectionnées ne justifie pas une revendication concernant le site entier.

## Alternatives arbitrées

- **Un seul statut « Validé » :** rejeté, car confond l’examen du critère avec sa conformité.
- **Ajouter « Partiellement conforme » :** rejeté explicitement par l’utilisateur. Les tests ou constats particuliers décrivent les cas mixtes sans créer un résultat partiel ambigu au niveau du critère.
- **Déduire/enregistrer automatiquement Conforme d’un contrôle automatisé sans alerte :** rejeté, car l’automatisation n’établit pas la conformité et peut manquer des problèmes nécessitant jugement humain. Une suggestion fondée sur des tests RGAA évalués n’est qu’une aide, soumise à confirmation explicite.
- **« À déterminer » comme résultat final après revue terminée :** non retenu dans le modèle recommandé ; si l’auditeur ne peut pas conclure, « Non vérifiable » avec justification est plus explicite. Si la règle n’a pas été examinée, elle reste « À examiner » et l’audit incomplet.
- **Niveau WCAG sélectionné automatiquement :** rejeté ; le niveau est une préférence de mission/politique et demeure à choisir par l’utilisateur.

## Conséquences, validation et questions ouvertes

L’UI, les exports et le JSON distinguent au minimum le type de contrôle (automatique ou humain), le résultat observé, la décision unique du lot et l’état de revue. Un critère du lot peut être « Revue terminée » avec un résultat Non conforme ; le rapport le montre sans l’effacer derrière un statut global.

Pour afficher la progression du lot, le produit liste les critères de la version exacte du référentiel et calcule le dénominateur sur les critères du périmètre, une fois chacun. Un critère ne compte comme terminé que si l’applicabilité est déterminée sur toutes les pages et qu’un résultat global a été explicitement terminé. Pour WCAG, le catalogue doit porter le niveau A/AA/AAA de chaque critère afin de respecter la cible choisie. Si le catalogue est absent, incomplet ou ne permet pas de calculer le périmètre visé, l’application n’affiche pas de pourcentage global.

Avant implémentation, vérifier la table d’agrégation et les cas d’applicabilité contre le texte officiel RGAA 4.1.2 exact et les données du catalogue ; conserver la version des règles/tests. Ajouter des tests unitaires couvrant échec applicable, réussite complète, tests manquants, applicabilité ambiguë, absence de tests applicables, non-applicabilité justifiée, non-vérifiable, changement après sauvegarde et agrégation multi-pages. Confirmer l’accessibilité des libellés, explications et actions. La préférence pour une suggestion est confirmée ; algorithme, seuils de couverture et interaction détaillée restent des recommandations du conseil.

Critères de test : un résultat automatique sans détection ne peut pas devenir Conforme ; un résultat Non conforme peut coexister avec la revue terminée du critère ; Non applicable/Non vérifiable exigent une justification ; chaque page doit avoir son applicabilité déterminée avant de terminer le critère du lot ; l’ajout d’une page rouvre les critères dont l’applicabilité manque ; PDF/ODT/JSON préservent cette séparation.

## Statut des inputs

- **Confirmé par l’utilisateur :** l’auditeur (et non une validation client) effectue la revue et définit un résultat par critère pour le lot ; les pages conservent les éléments et applicabilités qui étayent ce résultat ; état « Revue terminée » distinct du résultat ; chaque audit WCAG exige le choix initial d’un niveau cible A/AA/AAA sans valeur par défaut ; quatre résultats acceptés ; « Partiellement conforme » exclu ; les résultats non applicable/non vérifiable sont justifiés.
- **Préférence utilisateur confirmée :** une suggestion calculée à partir des résultats des tests RGAA, le résultat final restant une décision de l’auditeur.
- **Confirmé par l’utilisateur :** un critère reçoit un résultat unique pour le lot ; l’applicabilité, les constats et les preuves restent localisés par page ; la recherche des critères doit être distincte de la navigation page par page.
- **Confirmé par l’utilisateur :** PDF/ODT n’affichent que les résultats confirmés ; une suggestion non confirmée reste dans l’application et, en rapport d’étape, le critère est indiqué comme à examiner sans exporter la suggestion.
- **Recommandation du conseil, détails ouverts :** calcul d’une suggestion au critère/lot seulement quand les résultats de tests et l’applicabilité de toutes les pages sont suffisamment complets et non ambigus ; aucune suggestion dans les autres cas ; confirmation/override explicite ; pas d’écrasement d’une décision sauvegardée. Les sources de test sont des évaluations explicites, non les détections brutes.
- **Non demandé à l’utilisateur pour l’instant :** la préférence générale est résolue ; le travail restant est une vérification normative/technique et des tests, pas un arbitrage de préférence.
