# Kit média — RGAA Studio

Captures de l’application réelle et déclinaisons éditoriales destinées à la fiche produit, au site de présentation et aux animations.

## Contenu

- `captures/` — captures sans habillage, PNG, issues de l’application Windows.
- `store/` — mêmes écrans sur canevas exact 1920 × 1080, prêts à importer dans des fiches de boutique.
- `compositions/` — visuels éditoriaux construits à partir des captures réelles.
- `web/` — dérivés légers pour les animations et intégrations web.
- `CONTACT-SHEET.png` — planche de repérage des captures.
- `media-manifest.json` — inventaire, tailles et dimensions des fichiers.
- `ANIMATION-STORYBOARD.md` — propositions de séquences pour le site de présentation.

Le kit comprend 26 captures originales (1920 × 1032), les mêmes écrans centrés sur un canevas 16:9 en 1920 × 1080 dans `store/`, quatre compositions éditoriales 1920 × 1080 et 30 exports WebP de 1600 pixels de large dans `web/`.

Les captures du navigateur utilisent le site public [www.numera11y.fr](https://www.numera11y.fr), comme demandé. Le dossier d’audit est créé dans une base LiteDB isolée ; l’outil de préparation n’importe aucun ancien dossier, ne lance pas l’analyse de découverte de pages et ne demande pas de favicon. Le navigateur charge le site réel pour les captures, et les résultats axe-core reflètent le contenu servi au moment de la capture. Aucune décision humaine de conformité n’est préremplie ni enregistrée. Le contenu du site et les résultats automatiques peuvent évoluer après la date de capture.

Le jeu de données utilisé est isolé de tes dossiers d’audit existants. Pour recréer les captures, utiliser un nouveau chemin de base de données vide avec `tools/media/Capture-PressKit.ps1`; le script refuse les bases, journaux ou préférences déjà présents. Le script définit `RGAA_STUDIO_SKIP_LEGACY_IMPORT=1` pendant la capture. Puis lancer `python tools/media/Build-MediaKit.py` pour reconstruire les compositions, formats boutique, exports WebP, planche et manifeste.
