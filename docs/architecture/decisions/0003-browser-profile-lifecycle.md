# ADR 0003 — Profils navigateur : cycle de vie et concurrence

- **Statut :** assignation simultanée confirmée ; repli avec propriétaire interactif unique et transfert explicite accepté ; exigence de non-mélange des traces confirmée. Multi-vues interactives : capacité souhaitée, conditionnée au spike.
- **Date :** 2026-10-28

## Contexte

L’utilisateur confirme explicitement qu’un profil peut servir à plusieurs audits simultanément (« oui bien sûr et autant que possible »). Cette confirmation tranche l’assignation : elle ne garantit pas que le moteur peut fournir des sessions indépendantes ni un contrôle interactif concurrent sûr.

Avis reçus (positions consultées, non redéléguées) :
- **Product strategist :** assignation simultanée permise ; navigation/contrôle sérialisés par transfert de propriété ; ne pas promettre de sessions navigateur indépendantes avant preuve.
- **UX :** rendre le profil partagé et ses audits visibles ; un seul audit contrôle le navigateur actif à la fois en l’absence de capacité validée ; avertissements d’état/session ; données d’audit distinctes.
- **Browser architecture :** relation plusieurs-audits-vers-un-profil ; les vues peuvent partager cookies ; comportement stockage/auth/session à tester par spike ; distinguer données d’audit isolées de l’état partagé du profil ; permettre la concurrence testée, bloquer/avertir sinon.
- **Open-source steward :** réutilisation explicite et avertissement de confidentialité ; empêcher l’usage actif simultané jusqu’à validation, car l’état de session peut influer sur d’autres audits ; un même compte OS n’est pas une frontière de sécurité.

La convergence est forte sur l’assignation multiple et la séparation des données d’audit, mais les avis divergent sur la disponibilité initiale du contrôle interactif concurrent. La compatibilité réelle dépend du moteur qui n’est pas encore choisi.

## Décision

1. **Exigence confirmée :** plusieurs audits peuvent être explicitement assignés au même profil au même moment. Cette décision ne revient pas sur la possibilité de partage et ne signifie pas « autant de sessions indépendantes que souhaité ».
2. **Objectif produit :** permettre autant que possible le travail interactif simultané depuis plusieurs audits utilisant ce profil, sous réserve de tests concluants du moteur.
3. **Architecture progressive :** modéliser dès le départ plusieurs références d’audit vers un profil. Le coordinateur de profil traite séparément les vues/contrôleurs et leur état. Le spike doit chercher à prendre en charge plusieurs vues concurrentes, en vérifiant précisément le partage des cookies, du stockage, de l’authentification et des événements de navigation. Ne jamais présenter ces vues comme des sessions indépendantes si elles partagent l’état.
4. **Repli v1 accepté par l’utilisateur :** si le moteur ne démontre pas un fonctionnement concurrent compréhensible et sûr, conserver les assignations simultanées mais sérialiser le contrôle interactif : un seul audit est propriétaire du navigateur actif à la fois. Les autres audits restent ouverts et assignés, leurs données demeurent disponibles, et l’interface indique clairement l’audit propriétaire et permet un transfert explicite. Bloquer ou avertir les opérations qui risquent de modifier la session partagée. Ce repli limite le contrôle actif, mais ne retire pas l’assignation multiple confirmée.
5. **Invariants :** (a) constats, preuves, notes, progression et historique restent isolés par audit ; (b) tous les audits concernés voient qu’ils partagent le même profil ; (c) aucun profil n’est assigné automatiquement ; (d) l’interface décrit honnêtement si cookies/session/stockage sont partagés, et ne prétend pas à une isolation ou une indépendance non vérifiée ; (e) les secrets du profil sont exclus des exports, preuves et journaux ; (f) compte OS partagé n’est pas une frontière de sécurité.
6. Les profils sont locaux, persistants indépendamment des audits et supprimables séparément. Un profil neuf est proposé par défaut ; sélection et réutilisation restent explicites. Aucune invite de permission de synchronisation n’est décidée ni requise par cet ADR.
7. **Réutilisation inter-client :** l’utilisateur confirme qu’une réutilisation entre audits de clients différents doit être signalée et confirmée ; les traces ne doivent pas se mélanger. Si l’identité client n’est pas connue, l’interface ne doit pas prétendre avoir validé qu’il s’agit du même client.

## Alternatives et arbitrage

- **Interdire l’assignation multiple :** rejeté, car contredit l’exigence explicite de l’utilisateur.
- **Promettre immédiatement plusieurs sessions interactives indépendantes :** rejeté ; aucune preuve moteur disponible, et les profils partagés peuvent partager l’état d’authentification.
- **Verrou exclusif permanent sur le profil et interdiction d’assigner les autres audits :** rejeté. Le verrou de contrôle n’est retenu que comme repli v1 accepté pour l’interaction, pas comme règle d’assignation.
- **Autoriser toute concurrence sans garde-fous :** rejeté ; risque de changements de session inattendus, d’interférence et de confusion d’attribution.

L’utilisateur accepte le transfert du contrôle vers un audit propriétaire unique si l’usage interactif concurrent n’est pas validé. Plusieurs vues interactives restent l’objectif du spike, sans annoncer une capacité moteur non établie.

## Conséquences et validation

Le modèle lie un identifiant opaque de profil à plusieurs audits ; le contenu du profil n’est pas dupliqué dans les dossiers d’audit. Le coordinateur distingue assignation, vue ouverte et propriété du contrôle. Il doit rendre les changements d’état du site visibles aux audits liés et empêcher qu’une preuve ou un constat soit attribué au mauvais audit.

**Spike time-boxé :** responsable proposé : responsable architecture navigateur, avec revue UX et sécurité/open-source ; durée maximale de 2 semaines après sélection des moteurs candidats. Tester sur Windows et Linux les candidats réalistes : deux vues, navigation simultanée, cookies/session après connexion, stockage local, expiration/déconnexion, changements de page, attribution URL/preuve, fermeture/crash et transfert de propriétaire. Critères de sortie : matrice documentée des états partagés/isolés ; tests automatisés reproductibles ; aucune fuite de constat/preuve entre audits ; état de session et propriétaire toujours compréhensibles en UX ; comportement de repli déterministe. Si tous les critères passent pour un moteur/support donné, activer la concurrence uniquement dans ce périmètre vérifié. Sinon, conserver le repli sérialisé et documenter les limitations. La durée et le choix précis des moteurs sont une recommandation de planification, à confirmer par l’équipe.

Risques résiduels : une action dans une vue peut changer l’état de session observé par une autre ; serveur cible peut invalider une session ; comportement peut différer selon moteur/OS. Vérifier aussi secrets hors exports/logs, résistance aux crashs et accessibilité des avertissements/transferts.

## Statut des inputs

- **Confirmé explicitement par l’utilisateur :** un profil peut être assigné à plusieurs audits simultanément ; vues interactives concurrentes souhaitées « autant que possible » ; si le spike ne valide pas cette capacité, un seul audit contrôle le navigateur partagé à la fois, avec transfert explicite ; réutilisation entre clients signalée et confirmée ; les traces de chaque audit/site ne doivent pas être mélangées.
- **Recommandation d’architecture du conseil :** viser les vues concurrentes sous preuve ; gestionnaire et affichage du profil partagé ; profil neuf proposé par défaut ; aucune auto-assignation.
- **Non décidé :** toute invite de permission de synchronisation. Aucun choix utilisateur supplémentaire n’est nécessaire pour lancer le spike ou appliquer le repli.
