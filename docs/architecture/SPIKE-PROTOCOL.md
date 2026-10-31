# Protocole des spikes d’architecture

**Statut :** protocole de travail pour la phase de spikes ; les résultats et le choix d’architecture restent à établir.

**Date :** 2026-10-29
**Décision associée :** [ADR 0010 — gate d’architecture et équipe de spike](decisions/0010-architecture-gate-and-spike-team.md)

## 1. But et limites

Établir par des essais reproductibles si au moins une approche permet le parcours central d’audit sur les plateformes confirmées, avant de choisir un framework ou moteur. Il s’agit d’un prototype technique jetable et d’un dossier de preuves, pas d’une pré-version, d’une démonstration de conformité ni d’une autorisation de développer la v1.0.0.

Le spike éprouve :

- l’usage d’une page web rendue dans le workflow de l’application et l’accès au contexte nécessaire (URL, éléments DOM observables, capture) ;
- la frontière clavier/focus/lecteur d’écran entre page auditée et outil ;
- la persistance du profil navigateur, son affectation explicite et le transfert de contrôle si nécessaire ;
- l’isolation entre profil et dossier, puis entre deux dossiers d’audit ;
- la sauvegarde/reprise d’un dossier avec une décision humaine distincte des observations automatiques ;
- la faisabilité de build, d’installation et de construction communautaire.

Hors périmètre : catalogue complet RGAA/WCAG, déduction de conformité, audit multi-pages complet, exports finaux PDF/ODT/JSON, Jira, collaboration, licence finale du projet, signature et publication. Les tests utilisent uniquement des pages et données synthétiques contrôlées ; aucun secret ou identifiant réel.

## 2. Candidats comparés

Le lead technique consigne les versions exactes et applique le même scénario de travail aux candidats retenus. La liste est une sélection de pistes, pas un classement :

1. **Shell desktop avec Chromium intégré** (Electron est un candidat à vérifier) ;
2. **WebView système dans un shell desktop** (Tauri/WebView2 sous Windows et WebKitGTK sous Linux constituent une piste à vérifier) ;
3. **Playwright avec navigateur piloté**, seulement comme comparateur si la page est réellement utilisable dans le workflow visible de l’application ; une fenêtre externe seule ne valide pas l’exigence de navigateur intégré ;
4. **Qt WebEngine**, étude de pré-faisabilité limitée si elle apporte une comparaison distincte utile ou si les deux premières voies présentent un blocage.

Comparer au minimum deux approches d’intégration plausibles. Ne pas engager du temps sur un candidat après un échec bloquant sans mitigation testable. Pour chaque candidat, préciser ce qui est mesuré, ce qui est simulé et ce qui reste inconnu. Les capacités affirmées dans la documentation d’un composant ne sont pas des résultats de ce spike.

## 3. Jeu d’essai commun

Préparer un petit site local ou contrôlé avec :

- une page longue comportant liens, formulaire, bouton, sections et navigation clavier reproductible ;
- un défaut d’accessibilité connu et un état accessible de comparaison ;
- un changement d’état dynamique, une redirection, une popup et un téléchargement ;
- une page d’authentification factice avec données jetables, expiration ou déconnexion contrôlée ;
- un cas d’échec de navigation déterministe.

Les scénarios ne doivent pas contacter un site client. Les captures, traces, DOM, logs et rapports de test ne doivent contenir aucun vrai secret. Consigner pour chaque exécution : candidat et versions, OS, matériel, réglages, étapes, résultat observé, verdict et preuve expurgée.

## 4. Scénarios communs et verdicts

Utiliser les mêmes étapes pour chaque candidat, dans la mesure où la fonction est applicable.

| ID | Essai | Oracle observable |
|---|---|---|
| S1 — Navigation/context | Ouvrir une URL, suivre redirection, revenir/avancer, ouvrir popup et télécharger. | Page visible dans l’application ; URL initiale et finale identifiables ; politique de popup/téléchargement explicite ; échec de chargement récupérable. |
| S2 — Inspection | Consulter URL, DOM/élément ciblé et capture après une interaction. Distinguer DOM et arbre d’accessibilité. | Les données réellement observables sont énumérées et associées à la page ; limites documentées ; aucune prétention que le DOM représente à lui seul l’expérience perçue. |
| S3 — Clavier/focus | Parcourir la cible, entrer dans l’outil, choisir un critère, puis revenir à la cible au clavier seul ; répéter avec lecteur d’écran. | Pas de piège ni de vol silencieux de focus ; zones et transitions compréhensibles ; focus de reprise prévisible ; commandes de la cible utilisables. |
| S4 — Profil | Créer/choisir explicitement un profil de test, se connecter, fermer et relancer, réutiliser le profil dans deux audits assignés. Tenter deux contrôleurs. | Persistance réelle démontrée ; profil distinct des dossiers ; partage affiché ; propriétaire ou transfert explicite si un seul contrôleur ; aucune affectation automatique. |
| S5 — Isolation et secrets | Créer deux dossiers A/B, ajouter constats et preuves distincts, naviguer/changer de dossier, provoquer une opération asynchrone tardive ; inspecter dossiers, exports et logs. | Aucune écriture croisée ; profil hors des dossiers ; cookies/jetons absents des preuves, exports et logs ; mécanismes d’accès du contenu distant examinés. |
| S6 — Décision et reprise | Enregistrer une observation automatique factice, une décision humaine et une justification ; fermer/réouvrir le dossier et produire un état de progression incomplet. | La provenance, la décision et l’état de revue sont distincts ; aucune détection ne devient une conclusion ; données sauvegardées présentes ; progression restante et caractère incomplet visibles. |
| S7 — Packaging/build | Construire depuis un checkout propre selon des instructions documentées ; installer, lancer le parcours synthétique et désinstaller/effacer les données synthétiques. | Commandes, versions, dépendances, artefacts et empreintes consignés ; build sans secret/service propriétaire obligatoire ; installation testée sur les cibles prioritaires, échecs détaillés. |

Pour chaque scénario, utiliser l’une des conclusions suivantes :

- **PASS** : démontré par une preuve reproductible sur la cible et la version indiquées ;
- **FAIL** : échec observé, ou exigence que le candidat ne peut satisfaire sans compromis incompatible ;
- **UNKNOWN** : non testé, non reproductible, seulement documenté par un tiers, ou données insuffisantes.

Un `UNKNOWN` sur une exigence bloquante empêche le choix du candidat jusqu’à preuve complémentaire ou révision explicite du périmètre. Ne pas convertir un résultat manquant en succès. Une limite récupérable et explicitement communiquée peut être un avertissement plutôt qu’un blocage ; expliquer le choix.

## 5. Plateformes et accessibilité à éprouver

- **Windows :** système et édition exacts à choisir et consigner avant l’exécution ; le cadrage confirme Windows sans fixer de version.
- **Linux installation garantie :** Ubuntu 26.04 LTS x86-64 et Fedora Workstation 44 x86-64 ; vérifier le chemin d’installation et de lancement des formats `.deb` et `.rpm` prévus.
- **Linux test seulement :** Ubuntu 26.10, sans promesse d’installation.
- **Technologies d’assistance proposées pour le spike :** NVDA sous Windows et Orca sous Linux, avec versions consignées. C’est une matrice minimale d’essai, pas une déclaration de compatibilité exhaustive ni un critère WCAG de la v1.
- Réaliser les parcours essentiels clavier seul puis lecteur d’écran + clavier. Vérifier la découvrabilité des zones, noms/états des commandes, annonce des transitions, focus visible, erreurs et absence de dépendance à la couleur seule.

Tout échec reproductible dans une action centrale est documenté avec étapes exactes et résultat attendu/réel. Un défaut bloquant d’accessibilité de l’outil ne peut pas être écarté au motif que l’application sert à auditer d’autres sites.

## 6. Responsabilités et séquence

| Rôle | Livrable principal | Peut avancer en parallèle ? |
|---|---|---|
| Lead technique / architecture | Scénarios versionnés, versions candidates, tableau consolidé PASS/FAIL/UNKNOWN, rapport et proposition ADR. | Coordonne l’ensemble. |
| Spécialiste desktop/navigateur | Prototypes candidats minimaux, navigation/inspection/profil et limites observées. | Oui, candidats et espaces de travail distincts ; mêmes scénarios. |
| Exécutant UX/accessibilité | Un seul parcours prototype ; résultats clavier/lecteur d’écran et frontière page/outil. | Oui, avec contrat commun ; ne crée pas de second produit concurrent. |
| Packaging/open source | Build/install par cibles, contraintes runtime/dépendances, inventaire initial et instructions communautaires. | Oui, indépendante des détails de l’interface. |
| QA/revue humaine | Exécution reproductible, vérification des preuves et revue indépendante UX/accessibilité/open source. | Oui ; ne certifie pas son propre travail. |

Séquence recommandée, plafonnée à **cinq jours ouvrés pour le premier gate** :

1. **Préparer (jusqu’à une demi-journée)** : confirmer le protocole, versions/testeurs, machine de référence, pages synthétiques et modèle de preuve ; isoler les espaces de travail.
2. **Écran de faisabilité (environ une journée par candidat)** : établir la navigation intégrée, contexte URL/DOM minimal, clavier et capture. Arrêter un candidat après un FAIL bloquant sans mitigation crédible.
3. **Essais de risques (jusqu’à deux jours, en parallèle)** : profils/reprise/isolation, lecteur d’écran, scénarios de navigation secondaires, packaging et inventaire initial.
4. **Répétition et revue (jusqu’à une journée)** : refaire les tests essentiels sur les cibles disponibles, classer PASS/FAIL/UNKNOWN, présenter les preuves et tenir le gate.

Si l’accès à une machine, un paquet ou une technologie d’assistance manque dans ce délai, rapporter `UNKNOWN` avec responsable et action de suite, plutôt que d’étendre le spike sans décision. Toute modification du protocole pendant un test est versionnée et appliquée à tous les candidats concernés.

## 7. Packaging, dépendances et open source

Pendant le spike, inventorier les dépendances directes/transitives du prototype et les composants effectivement embarqués avec le navigateur, en notant version, source, rôle (build/test/runtime), licence déclarée, mode de livraison (embarqué/système/téléchargé) et inconnues. Inclure outils de packaging, dépendances natives, codecs/ressources, polices et autres assets si présents. Archiver les références officielles consultées ; distinguer l’inventaire préliminaire d’une revue juridique.

Consigner le lien commit → dépendances/versions → commandes/environnement → artefact et empreinte SHA-256. Refaire un build depuis checkout propre et expliquer les différences entre artefacts ; ne pas prétendre à une reproductibilité bit à bit si elle n’est pas démontrée. Documenter tout accès réseau requis. Un clone communautaire doit pouvoir construire sans compte privé, secret ou service propriétaire obligatoire pour l’usage essentiel.

Les résultats du spike ne sont **pas** le gate de release v1.0.0 : l’inventaire final des composants distribués, licences/avis, chaîne de publication, signatures et obligations seront réévalués avant diffusion. Aucune conclusion juridique ne peut être portée par cet inventaire seul.

## 8. Gate d’architecture

Le conseil peut proposer un candidat au choix uniquement si le dossier contient :

1. comparaison d’au moins deux voies d’intégration pertinentes sur scénarios communs, avec versions exactes et distinction entre faits mesurés, documentation et inconnues ;
2. preuves d’usage intégré, contexte URL/DOM, navigation, popups/téléchargements et comportement d’échec ;
3. essais clavier/lecteur d’écran et focus sur les cibles retenues, défauts bloquants et limites documentés ;
4. preuve de persistance et de contrôle du profil, ou démonstration du repli propriétaire unique/transfert explicite accepté ; séparation des dossiers et absence de secrets vérifiées ;
5. sauvegarde/reprise d’un audit synthétique et distinction explicite entre observations automatiques, décision humaine, revue et incomplétude ;
6. preuves de build/installation sur Windows, Ubuntu 26.04 et Fedora 44, ou échecs/UNKNOWN explicitement traités ; Ubuntu 26.10 rapporté séparément comme test seulement ;
7. inventaire initial, risques de sécurité/mise à jour/maintenance, instructions de build, rapports QA et revue humaine UX/accessibilité et open source.

Un FAIL critique de mélange/fuite de données, de piège clavier, de perte non récupérable de décisions ou de conclusion de conformité trompeuse bloque le candidat tant que corrigé et retesté. Le gate peut se conclure **sans choix** si les preuves sont insuffisantes. Dans ce cas, ouvrir un spike ciblé ou revoir explicitement une contrainte ; ne pas retenir tacitement le moins mauvais. Le franchissement du gate n’autorise que la préparation d’une tranche verticale selon un contrat produit revu, pas une publication ni une revendication de conformité du produit.

## 9. Modèle de compte rendu

Pour chaque candidat, conserver :

```text
Candidat / commit / versions :
Environnement (OS, édition, architecture, matériel, ATS) :
Scénario et version du protocole :
Verdict : PASS | FAIL | UNKNOWN
Étapes exécutées :
Résultat attendu / observé :
Preuves (chemins, empreintes, journaux expurgés) :
Écarts, limites et risque :
Reproductibilité (répété par qui, où, combien de fois) :
Action de suite / responsable / échéance :
Avis technique (distinct du fait mesuré) :
```

Les prototypes, journaux ou données produits par la suite devront être stockés dans un emplacement de travail explicitement isolé et ne contenir aucune donnée réelle d’audit ou de profil.
