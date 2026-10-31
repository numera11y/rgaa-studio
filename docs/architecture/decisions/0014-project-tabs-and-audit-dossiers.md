# ADR 0014 — Sémantique des projets et onglets

- **Statut :** décision utilisateur confirmée le 29 octobre 2026.
- **Date :** 29 octobre 2026
- **Décideur :** propriétaire produit

## Contexte

L’utilisateur demande une barre inspirée de sa capture de référence : accès aux projets, onglets de projets ouverts qui peuvent être fermés et déplacés, action plus/nouveau, icône d’update, volet latéral d’audit et commandes de fenêtre. Il a confirmé que le mot « projet » désigne le dossier d’audit pour un site et un référentiel, et que l’icône ouvre la liste des dossiers.

Le modèle confirmé définit un dossier d’audit indépendant pour un seul site, avec un référentiel/version figé, une page ou un lot de pages, et ses propres décisions/preuves/historique. Il ne définit pas de conteneur supérieur « projet » qui regroupe plusieurs audits. Deux audits différents peuvent concerner le même site sans fusionner leurs dossiers. Les profils navigateur constituent une donnée séparée, assignée explicitement ; un onglet distinct ne signifie pas une session navigateur isolée.

## Décision confirmée

**Un projet est un dossier d’audit pour un site et un référentiel.** L’icône d’accès projets ouvre la liste des dossiers ; un onglet représente un dossier. Il n’y a pas de conteneur projet au-dessus de plusieurs dossiers dans le modèle v1.

## Invariants et conséquences de la décision

- L’identifiant stable de l’onglet est l’identifiant du dossier, jamais le domaine seul, le titre visible ou le profil.
- Deux audits du même site restent distinguables (client, référentiel/version, intitulé ou dates) et gardent des données séparées.
- Fermer un onglet ferme la vue, jamais le dossier ; l’accès/reprise reste disponible depuis la vue Projets.
- Réordonner les onglets ne change ni l’identité, ni l’affectation de profil, ni les données de l’audit.
- L’action plus crée un dossier nouveau ; elle ne clone pas silencieusement les résultats, preuves ou le profil du dossier actif.
- L’onglet actif détermine le contexte d’audit utilisé par les décisions et preuves. Les opérations asynchrones doivent conserver l’identifiant de leur audit source pour éviter une sauvegarde croisée lors d’un changement d’onglet.
- Si plusieurs dossiers partagent un profil navigateur, l’interface annonce que cookies/état de session peuvent être partagés ; les onglets ne promettent pas des sessions indépendantes. Toute limite d’un seul propriétaire interactif nécessite un transfert explicite et visible.
- La barre et les onglets sont utilisables au clavier : sélection, activation, fermeture avec nom accessible, réordonnancement autrement qu’au glisser-déposer seul, focus visible et annonces de changement d’onglet.

## Alternatives

1. **Onglet = dossier d’audit (retenu).** Correspond à la décision utilisateur et au modèle existant.
2. **Onglet = conteneur de plusieurs dossiers.** Rejeté pour le modèle v1 ; aucun conteneur supérieur n’est prévu.
3. **Onglet = page auditée.** Non retenu : la page est une partie du périmètre d’un audit et ne doit pas remplacer son dossier comme unité de revue/reprise.

## Validation attendue

Tester avec des auditeurs la capacité à distinguer deux dossiers du même site, à comprendre qu’un onglet ne supprime pas le dossier, à rouvrir un dossier fermé, et à repérer quel dossier/profil contrôle le navigateur. Valider au clavier et avec lecteur d’écran, y compris fermeture/réordonnancement des onglets.
