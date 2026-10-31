# ADR 0004 — Libellé client/organisation et réutilisation des profils

- **Statut :** partiellement accepté — champ facultatif et inclusion dans le rapport PDF client confirmés ; exclusion ODT/JSON interprétée depuis « pour le reste ce n’est pas nécessaire » ; autres règles d’usage recommandées.
- **Date :** 2026-10-28

## Contexte

L’utilisateur a confirmé un champ client/organisation facultatif en plus du site/domaine, afin de faciliter l’identification de l’audit et d’aider à prévenir les réutilisations inappropriées de profils. Il exige que les traces d’audit restent séparées et que toute réutilisation inter-client soit signalée et confirmée. Il précise que le nom doit apparaître dans le rapport client ; pour les autres sorties il n’est pas nécessaire.

Avis consultés :
- **Auditor UX :** champ facultatif à la création avant choix du profil, éditable ensuite ; alertes montrant client et domaine ; comparaison comme indice, non comme validation d’identité ; prudence si client inconnu ; possibilité de choisir un profil neuf ; confirmation inter-client ; avertissement accessible.
- **Product strategist :** métadonnée d’audit, pas attribut du profil ; visible dans l’audit et les listes ; inclusion envisageable dans résumé/JSON si fourni, avec confiance modérée pour les exports ; éviter CRM ou assurance d’identité.
- **Open-source steward :** métadonnée locale uniquement ; par défaut exclue PDF/ODT client, JSON seulement via schéma explicite ; jamais dans journaux/télémétrie/noms de fichiers ; aucune assurance d’identité ni service externe ; avertissement prudent si valeur absente ou différente.

Convergence : valeur indicative attachée à l’audit, séparation stricte des dossiers d’audit, et absence de garantie d’identité. L’incertitude principale porte sur les exports ; aucun besoin produit ne justifie d’y exposer cette donnée par défaut.

## Décision

1. Ajouter à chaque audit un champ texte facultatif « Client / organisation », distinct du domaine/site et du profil navigateur. Stocker la valeur normalisée (espaces de bord supprimés, chaîne vide traitée comme absente) dans les métadonnées locales de l’audit. Aucun registre client, compte, CRM ou service distant.
2. Champ proposé lors de la création, avant sélection du profil, mais modifiable ensuite. Afficher la valeur lorsqu’elle existe dans les vues de détail et les listes d’audits. Une modification ne change ni n’agrège les constats, preuves, notes ou historiques : ils demeurent propres à chaque dossier d’audit.
3. Pour une réutilisation de profil, comparer les libellés connus à titre d’indice seulement. Libellés identiques ne signifient pas même entité ; libellés différents ou absents ne prouvent pas des clients distincts. Le domaine est présenté séparément et n’est pas une identité client. Ne jamais afficher « identité vérifiée » ou l’équivalent.
4. Avant affectation explicite d’un profil déjà utilisé, montrer dans une confirmation accessible le libellé client et le domaine de l’audit courant et des audits connus liés au profil, ainsi que l’état « non renseigné » si applicable. En cas de valeurs différentes, absentes, ou de correspondance inconnue, avertissement prudent et confirmation explicite requise ; proposer un profil neuf sans l’assigner automatiquement. Si valeurs identiques, garder une confirmation de réutilisation explicite et rappeler que le profil peut partager cookies/état de session selon ADR 0003. L’interface ne prétend pas inférer l’identité réelle.
5. Inclure le champ dans le rapport PDF client lorsqu’il est renseigné. L’exclure de l’ODT modifiable d’équipe et du JSON structuré, conformément à la préférence utilisateur actuelle. Le champ reste exclu des noms de fichiers, journaux et télémétrie. Toute extension future de l’export nécessite une décision explicite et, pour JSON, une mise à jour documentée du schéma versionné.

## Alternatives et arbitrage

- **Champ obligatoire ou objet client structuré :** rejeté, car dépasse le besoin d’identification et ajoute collecte/gestion de données ainsi que complexité MVP.
- **Stocker le client sur le profil :** rejeté, car un profil est réutilisable par plusieurs audits et ne doit pas devenir une identité de client ; incompatible avec le partage multi-audits.
- **Déduire que les identités sont égales à partir du libellé ou domaine :** rejeté, faux positifs/alias et domaines partagés possibles ; aucune preuve d’identité disponible.
- **Inclure dans tous les exports :** rejeté pour v1 ; l’utilisateur souhaite le nom dans le rapport client seulement et indique que les autres formats n’en ont pas besoin.
- **Ne jamais inclure dans JSON :** non retenu comme interdiction permanente ; un champ versionné et explicitement documenté peut être utile à l’intégration. Exclusion par défaut jusqu’à ce contrat.

## Conséquences, risques et validation

Cette métadonnée apporte un indice UX sans prétendre résoudre la gestion d’identité. Elle augmente légèrement les données personnelles/confidentielles locales ; conserver les principes de minimisation, accès local et absence de télémétrie. Les données d’audit restent isolées même lorsqu’un profil est partagé ; cookies et état web peuvent, eux, être partagés comme décrit dans ADR 0003.

Tests requis : cas client identique/différent/inconnu, domaine identique/différent, champ vide et renommage après affectation ; vérifier avertissements clavier/lecteur d’écran, confirmation avant réutilisation, proposition de profil neuf, et absence de mélange des traces. Tester que le PDF client contient le libellé renseigné et que l’ODT, JSON, noms de fichiers, logs et télémétrie l’excluent.

## Statut des inputs

- **Confirmé explicitement par l’utilisateur :** champ client/organisation facultatif en sus du site/domaine, pour identification et aide aux alertes ; traces séparées ; inclusion du nom dans le rapport client.
- **Accepté/interprété :** « pour le reste ce n’est pas nécessaire » signifie exclusion de l’ODT et du JSON ; corriger si cette interprétation est erronée.
- **Décision/recommandation du conseil :** type texte facultatif, portée audit, stockage local, sémantique indicative, confirmation explicite avant réutilisation ; exclure des noms de fichiers, journaux et télémétrie.
- **Encore recommandé :** emplacement exact dans le parcours, texte des alertes et normalisation des chaînes ; aucun ne bloque le cadrage du MVP.
