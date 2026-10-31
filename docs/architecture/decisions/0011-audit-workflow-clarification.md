# ADR 0011 — Parcours métier centré sur l’audit des pages, éléments et règles

- **Statut :** parcours métier principal confirmé par l’utilisateur ; modalités techniques et certains détails de preuve restent ouverts.
- **Date :** 29 octobre 2026
- **Contexte :** le prototype Electron a été jugé confus et éloigné du fonctionnement métier attendu. Le parcours doit guider l’auditeur sans faire passer les contrôles automatisés pour un audit complet.

## Décision

Retenir comme modèle de parcours de référence :

1. À l’ouverture, afficher les audits en cours et leurs informations de reprise ; proposer également la création d’un audit.
2. À la création, demander l’URL et le référentiel avec sa version exacte ; pour WCAG, demander explicitement la cible A, AA ou AAA, sans préselection ni confusion avec un résultat de conformité.
3. Proposer des pages importantes/indispensables candidates et vérifier qu’elles existent sur le site ; l’auditeur confirme explicitement celles qui entrent dans le périmètre. Ne pas présenter ces propositions comme un crawl exhaustif ni comme une liste obligatoire complète. Le sens exact de « page obligatoire » et le mécanisme de découverte restent à concevoir.
4. Faire entrer l’auditeur dans chaque page sélectionnée individuellement. Dans le contexte de la page, il peut pointer un élément à la souris, consulter ses informations détaillées et les critères/règles pertinents, puis consigner son examen des règles applicables.
5. Conserver comme preuve l’élément et son état lorsqu’ils sont examinés, ainsi qu’une capture d’écran associée dans le dossier d’audit. La capture est une preuve, pas une conclusion automatique ni une garantie d’authenticité. Le détail opérationnel — capture automatique et systématique ou capture que l’auditeur peut reprendre/omettre — reste ouvert. Une règle peut concerner plusieurs éléments/états et un élément peut être pertinent pour plusieurs règles : le modèle ne doit pas imposer une correspondance un-à-un.
6. Maintenir des résultats de conformité humains distincts de l’état de revue et des suggestions/détections automatisées, conformément aux ADR 0005 et 0006. Une page/élément sélectionné ne constitue pas en soi un résultat de conformité.

Le flux « page → élément/état → règle pertinente → décision humaine → preuves » est le chemin de travail principal, sans interdire les revues par critère, les contrôles clavier/lecteur d’écran ni les résultats portant sur une page entière. L’interface doit préserver ces autres modes de vérification : tous les critères ne se réduisent pas à un élément pointable.

## Options examinées

- **Parcours actuel générique, centré sur critères/résultats avant contexte page :** non retenu comme parcours principal, car il ne correspond pas au geste de travail décrit et a contribué à la confusion du prototype. Il reste possible comme vue complémentaire.
- **Crawl automatique exhaustif et inclusion automatique des pages découvertes :** rejeté. Cela surpromet l’exhaustivité, augmente le risque de faux périmètres et contredit l’exigence de confirmation explicite.
- **Associer chaque règle à un seul élément :** rejeté, car les règles peuvent s’appliquer à plusieurs éléments ou à une page/contexte. L’utilisateur attend qu’une capture d’écran accompagne les preuves d’élément/état ; la règle exacte de capture systématique et d’omission doit être clarifiée avant de la rendre obligatoire pour chaque résultat.
- **Traiter une détection ou suggestion automatique comme résultat d’audit :** rejeté, conformément aux priorités produit et aux décisions existantes.

## Motifs et arbitrage

Les exigences métier communiquées sont précises sur l’ordre du travail, le choix de référence/niveau, la confirmation des pages, l’inspection à la souris et la conservation des preuves. Elles précisent le parcours sans invalider le socle déjà documenté : un référentiel par audit, cible WCAG explicite, périmètre non exhaustif confirmé, preuves locales, distinction humain/automatique et accès clavier/lecteur d’écran.

Le compromis retenu fait de l’inspection élément/règle le parcours intégré le plus direct, mais ne l’impose pas comme ontologie exclusive. Cela évite de réduire abusivement les critères à des nœuds DOM, ce qui exclurait notamment les critères de parcours, de clavier, de présentation ou de comportement. Les pages candidates doivent être vérifiables sur le site et confirmées par l’auditeur ; leur mécanisme de détection reste à valider techniquement.

**Consultation du conseil (29 octobre 2026) :** product-strategist recommande un parcours de bout en bout et une découverte indicative, avec une confiance élevée sur le flux mais moyenne sur la découverte/sélection d’éléments. auditor-ux recommande deux voies de revue (élément/état et règle/page), captures utiles mais non bloquantes pour une décision et accès clavier/lecteur d’écran ; les comportements UI restent à tester. browser-architecture juge le parcours plausible, mais signale que le prototype Electron actuel ne démontre ni sélection souris, ni liens page/règle/preuve, ni profils réels, et que les références DOM sont fragiles ; confiance technique moyenne avant spike. opensource-steward recommande le local-first, preuves dans le dossier et profil séparé, en attirant l’attention sur les données personnelles/secrets dans les captures et sur les licences des composants ; confiance moyenne. Ces avis sont des recommandations, pas des approbations utilisateur. Le prototype actuel reste un banc technique et n’implémente pas encore le parcours métier attendu.

## Conséquences, dépendances et validation

- Le prototype UI doit être réorganisé autour de la reprise/création, de la sélection explicite des pages, puis de l’inspection dans le navigateur avec panneau de travail accessible.
- Le modèle canonique doit relier audit, page/contexte, règle, décision humaine et preuves ; il doit autoriser des relations multiples et conserver URL/état de navigation ainsi qu’un contexte d’élément lorsque disponible.
- L’identification et la capture d’élément sont dépendantes des capacités du moteur intégré. Ne pas faire de la sélection à la souris un prérequis pour saisir un résultat, et offrir des parcours clavier accessibles dans l’application.
- La sélection peut être fragile sur contenu dynamique ; les références d’élément doivent être contextualisées et ne pas prétendre permettre une reproduction pérenne du DOM.
- Validation avant généralisation du prototype : scénarios Windows/Linux, page statique et dynamique, élément réutilisé, règle sans élément unique, état interactif, navigation clavier, lecteur d’écran, pages suggérées inexistantes ou redirigées, reprise d’audit, et persistance/rattachement corrects des captures et notes. Vérifier qu’aucune suggestion automatisée n’est exportée comme résultat humain.
- Le catalogue exact des critères/règles et l’algorithme de découverte des pages ne sont pas décidés par cet ADR ; les suggestions sont une aide et doivent être testées pour éviter une présentation exhaustive trompeuse.

## Révision

Réexaminer après un test de parcours avec des auditeurs et un spike de sélection d’éléments sur le moteur retenu. Réviser si l’inspection élément/règle ralentit les vérifications page entière, empêche l’accès assistif, ou si le moteur ne peut pas fournir un rattachement fiable ; dans ce cas, conserver le flux métier avec preuves page/contexte et sélection d’élément facultative.
