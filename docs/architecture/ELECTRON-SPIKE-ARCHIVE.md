# Archive du spike Electron

## Statut

Expérience technique clôturée. L’interface Electron ne représente pas le parcours métier souhaité et Avalonia est l’application desktop retenue (ADR 0018). Le code prototype a été retiré de l’arborescence active ; l’historique des sources reste disponible dans Git.

## Ce qui a été essayé

Le prototype Windows utilisait Electron 45.1.0, une fixture locale synthétique, un panneau d’audit séparé et un stockage JSON de démonstration. Il éprouvait notamment l’isolation A/B des dossiers, la validation du schéma et des horodatages, ainsi que la distinction entre observations, décisions humaines et fin de revue. Il ne gérait ni vrais sites ni données de production.

## Résultats conservés

- `npm ci`, les 7 tests Node, le smoke statique et les vérifications syntaxiques avaient réussi ; l’audit npm n’avait signalé aucune vulnérabilité.
- Une revue statique a conduit au durcissement du schéma, de l’index et des événements de navigation.
- `npm start` était resté actif jusqu’au délai de smoke, sans inspection visuelle de l’interface.

## Limites

Navigation réelle, rendu et interactions visuelles, clavier complet, NVDA, permissions/redirections et récupération après interruption n’ont pas été validés. Les réussites des tests automatisés ne prouvaient donc ni l’accessibilité du prototype ni l’aptitude d’Electron à devenir le shell produit.

La maquette fonctionnelle indépendante du shell reste dans [`spikes/workflow-ui/`](../../spikes/workflow-ui/README.md). Le code WPF demeure temporairement dans `spikes/windows/wpf-native-spike/` comme référence de migration, jusqu’à reprise et vérification des fonctions encore utiles.
