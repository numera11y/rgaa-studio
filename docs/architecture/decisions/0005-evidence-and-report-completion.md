# ADR 0005 — Preuves et états des rapports

- **Statut :** décisions fonctionnelles principales acceptées par l’utilisateur ; exigences de revue de sécurité/accessibilité et contrat JSON à finaliser
- **Date :** 2026-10-28

> Le cycle de vie, le stockage local et la conservation des dossiers/preuves sont précisés séparément par [ADR 0008](0008-audit-evidence-retention.md). La règle provisoire sans expiration est une recommandation, pas une durée légale ni une approbation utilisateur.

## Contexte

L’utilisateur veut produire un PDF client, un ODT modifiable de ce même rapport et un JSON de suivi technique. Il confirme les éléments obligatoires du PDF (client/site, périmètre, référentiel/version, résumé, constats/critères/preuves, limites des automatismes, vérifications restantes), l’export de rapports d’étape explicitement incomplets, et le choix explicite des preuves à inclure dans le PDF. Les formes de preuves importantes sont notes, étapes de reproduction, captures d’écran et extraits DOM/élément.

## Décisions confirmées par l’utilisateur

1. Les contrôles restants n’empêchent pas de générer un rapport d’étape ; ce dernier indique clairement son caractère incomplet et les vérifications restantes.
2. Les preuves sont choisies explicitement avant inclusion dans le PDF client.
3. Les quatre types de preuves suivants sont pris en charge dans le besoin v1 : notes, étapes de reproduction, captures d’écran et extraits DOM/élément de page.
4. L’ODT est une version modifiable du même rapport que le PDF. Le client/organisation figure dans le PDF ; il n’est pas nécessaire dans l’ODT ni le JSON.
5. L’audit n’est « terminé » que lorsque chaque critère du lot a reçu un résultat confirmé après revue des pages applicables ; le conseil recommande que ce statut signifie « revue effectuée », jamais « conformité assurée ».

## Recommandations et portée

- Générer PDF et ODT depuis le même état du rapport. Une modification ODT externe ne modifie pas l’audit canonique.
- Distinguer dans l’interface et les exports les détections automatiques, les conclusions humaines et l’avancement de revue. Une absence de détection automatique ou une vérification non effectuée ne vaut pas conformité.
- Exiger une justification pour « non applicable » et « non vérifiable » ; un critère reste en attente tant que l’applicabilité n’est pas déterminée sur chaque page et qu’un résultat global n’est pas confirmé.
- Rattacher chaque preuve à l’audit, à la page et au critère/constat concernés, avec type et contexte observable (URL/date/état quand disponible). Ne pas traiter un extrait DOM ou une capture comme preuve de conformité à lui seul.
- À la sélection des preuves, afficher les pièces choisies et un aperçu accessible avant génération. Captures/DOM peuvent contenir des données client/personnelles ; prévoir un moyen de les examiner, d’omettre ou de masquer les zones sensibles avant export. Cette protection est une recommandation de sécurité/UX à valider par prototype.
- Le champ client apparaît dans le PDF client ; il est exclu de l’ODT et du JSON selon la préférence utilisateur.
- Le schéma JSON et sa politique pour les preuves ne sont pas finalisés. Recommandation : n’inclure aucune preuve brute par défaut ; définir explicitement références et pièces autorisées dans un schéma versionné, sans cookies, jetons ni profil navigateur.

## Alternatives et arbitrage

- **Bloquer tout export tant que l’audit n’est pas fini :** rejeté par l’utilisateur, qui veut des rapports d’étape.
- **Inclure toutes les preuves automatiquement :** rejeté par le choix explicite de l’auditeur, et risqué pour la confidentialité.
- **Traiter « validé par l’utilisateur » comme « conforme » :** rejeté. Cette étiquette décrit l’achèvement de la revue ; les résultats et conclusions restent distincts.
- **ODT comme dossier de suivi différent du PDF :** rejeté au profit d’une version modifiable du même rapport.

## Conséquences et validation

Un export correspond à un instantané de l’audit ; une exportation ou une modification ODT ne change pas l’état de l’audit. Les rapports incomplets listent les contrôles/pages restant à traiter. Les preuves non sélectionnées sont absentes du PDF. La génération échouée ne perd pas les sélections ni les données d’audit.

Critères à tester : contenu identique PDF/ODT hors choix explicitement différent du client ; libellé incomplet visible ; critères en attente et états non-applicables/non-vérifiables correctement représentés ; sélection et omission de preuves avec clavier/lecteur d’écran ; cas sans preuve sélectionnée ; absence de secrets dans sorties et journaux ; ODT éditable dans Word et LibreOffice ; aucune inférence de conformité automatisée.

## Statut des inputs

- **Confirmé par l’utilisateur :** rapports d’étape autorisés si incomplet clairement signalé ; preuve explicitement sélectionnée pour le PDF ; quatre formes de preuve ; ODT modifiable du même rapport ; complétude par décision unique de chaque critère sur le lot après examen des pages.
- **Recommandé par le conseil :** libellé « Rapport d’étape — incomplet », preuves aperçues/masquables, justifications pour non applicable/non vérifiable, état de revue distinct du résultat et aucun score de conformité automatique.
- **À préciser :** libellé final français des états, règle exacte de justification et état « non vérifiable », politique de preuves dans JSON, niveau de masquage/caviardage et si la même sélection de preuves est appliquée au PDF et à l’ODT.
