# ADR 0007 — Gravité et priorité de correction en v1

- **Statut :** recommandé par le conseil ; non accepté explicitement par l’utilisateur
- **Date :** 2026-10-28

## Contexte

Le MVP enregistre, pour chaque règle et page, un résultat choisi par l’auditeur : Conforme, Non conforme, Non applicable (justification requise) ou Non vérifiable (justification requise). L’état de revue est distinct du résultat. Le niveau WCAG cible est sélectionné explicitement au démarrage d’un audit WCAG et n’est pas un indicateur de gravité.

Le besoin porte sur l’ajout éventuel d’une gravité ou priorité de correction. Les avis spécialisés réellement reçus sont :

- **Product strategist (confiance élevée) :** ne pas ajouter de gravité/priorité configurable en v1 sans méthode utilisateur ; une non-conformité ne révèle pas à elle seule l’urgence ; un classement arbitraire serait trompeur ; réexaminer si les essais démontrent le besoin.
- **Auditor UX (confiance élevée) :** conformité, revue et urgence sont des dimensions distinctes ; ne pas rendre un champ obligatoire. Si un besoin de planification est établi, préférer un champ facultatif distinct, vide par défaut, renseigné par l’auditeur, jamais calculé du résultat et non bloquant pour terminer la revue. L’agent suggère de demander si la priorisation est faite dans l’application, le rapport ou un outil externe.

Ces avis n’établissent pas où ni comment les utilisateurs priorisent effectivement les corrections. Le format JSON est prévu pour le suivi technique et Jira est un candidat ultérieur non bloquant ; cela ne prouve pas qu’une priorité produit soit requise dans le modèle v1.

## Décision recommandée

Ne pas inclure en v1 de champ de gravité, score de sévérité, priorité de correction, classement automatique, ni calcul de priorité à partir du résultat de conformité. Conserver les résultats et preuves de conformité comme dimension distincte. Ne pas associer implicitement « Non conforme » à « urgent ».

Ne pas faire dépendre la fin de revue ou l’export de la saisie d’une priorité. Lorsqu’un besoin concret de planification est démontré, envisager d’abord un champ facultatif distinct, sans valeur par défaut, saisi par l’auditeur et non calculé, avec portée et restitution définies séparément.

## Arbitrage et éléments non résolus

Les deux positions reçues convergent sur l’exclusion d’un champ obligatoire/configurable en v1 et la séparation conceptuelle entre conformité et urgence. Elles divergent surtout sur l’opportunité de clarifier maintenant le lieu du workflow : produit recommande de différer jusqu’à preuve issue des essais ; UX propose une question courte pour savoir si la priorité est gérée dans l’application, le rapport ou un outil externe.

Le conseil retient l’exclusion pour protéger un flux d’audit utile et éviter une sémantique non validée. La question UX est pertinente mais n’est pas un prérequis à la décision de scope : la réponse ne justifierait pas à elle seule un champ v1 sans méthode et besoin démontré. Aucun nouvel avis d’agent n’a été sollicité pour cet arbitrage.

## Alternatives considérées

- **Gravité/priorité obligatoire pour chaque non-conformité :** rejetée ; crée une charge et bloque potentiellement la revue sans méthode consensuelle, et confond constat avec urgence.
- **Gravité/priorité facultative en v1, vide par défaut :** option défendable si une demande de planification est démontrée, mais non retenue faute de preuve et de définition des valeurs, portée et destination (app/rapport/export/outil).
- **Gravité calculée automatiquement depuis la conformité, le niveau WCAG ou les contrôles automatiques :** rejetée ; ces signaux ne déterminent pas l’urgence contextuelle de correction et risquent de donner une fausse autorité au classement.
- **Inclure la priorité dans l’export JSON/Jira sans UI dédiée :** non retenue ; aucun contrat de données ou workflow n’est défini, et JSON ne signifie pas intégration Jira.

## Conséquences, risques et validation

Le MVP reste plus simple à spécifier et tester, sans jugement de priorité implicite. Les rapports peuvent présenter les non-conformités, preuves et éléments à corriger sans ordonner l’urgence. Les utilisateurs qui planifient ailleurs devront continuer à le faire dans leur outil de suivi ; l’export JSON reste disponible selon son schéma à définir.

Risque : certains auditeurs peuvent attendre une aide à la priorisation. Cette hypothèse n’est pas vérifiée. Réévaluer après essais avec les profils cibles, en observant où la priorité est décidée, si les utilisateurs ont déjà une méthode, et si un champ facultatif apporte une valeur sans détourner le flux d’audit.

Critères de réévaluation : au moins un besoin récurrent observé dans les essais utilisateurs ; méthode ou vocabulaire compréhensible et non arbitraire ; destination explicite (application, rapport, export ou outil externe) ; démonstration qu’il ne confond pas conformité et urgence et ne bloque pas la revue. Si ces conditions sont satisfaites, proposer un ADR amendé pour un champ facultatif séparé, initialement vide et saisi par l’auditeur. Aucun critère ne justifie à lui seul un score automatique.

## Statut des inputs

- Résultats de conformité et séparation revue/conformité : documentés et confirmés dans ADR 0006.
- Avis produit et UX ci-dessus : conseils reçus, non décisions utilisateur.
- Exclusion de gravité/priorité en v1 : recommandation de scope du conseil, non acceptation utilisateur.
- Workflow réel de priorisation : inconnu, à vérifier en essais ; aucune question utilisateur n’est requise pour arrêter le scope recommandé.
