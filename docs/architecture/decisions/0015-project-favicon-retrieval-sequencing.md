# ADR 0015 — Séquencement du favicon des dossiers d’audit

- **Statut :** implémenté dans le spike WPF avec consentement visible, politique réseau bornée et fallback local.
- **Date :** 29 octobre 2026
- **Décideur :** propriétaire produit

## Contexte

L’utilisateur demande qu’un favicon identifie le dossier d’audit dans la liste des projets et dans son onglet, et confirme qu’il veut le récupérer dès la création quand l’URL est saisie. Le domaine produit est confirmé : un projet est un dossier d’audit pour un site et un référentiel.

Le spike WPF n’ouvre dans WebView2 que la fixture locale synthétique. À la création, une option visible, cochée par défaut, autorise une requête HTTP indépendante vers le site saisi ; elle peut être décochée.

## Décision produit et périmètre du spike

**Politique implémentée :** HTTPS sur port 443 uniquement, sans userinfo ; requête GET exacte `/favicon.ico` même origine, redirections/proxy/cookies/credentials désactivés, aucun Referer/Authorization. Refus `.test`, localhost et adresses non publiques ; DNS résolu puis adresse publique épinglée à la connexion. Limites : 4 s, 64 KiB transférés, types raster seulement, décodage ≤256×256/65 536 pixels ; réencodage en `favicon.png` local fixe. Le résultat est une métadonnée UI, optionnelle au manifeste et hors preuves ; aucun changement d’UpdatedAt à l’issue du fetch. Échec/opt-out conserve le globe local, sans bloquer création ou ouverture. WebView2 garde exactement sa navigation fixture-only.

La récupération distante ne réutilise ni le profil WebView, ni les cookies, credentials ou en-têtes d’authentification. Le fetch est limité aux hôtes HTTPS publics sur le port standard ; les hôtes `.test`, loopback, privés, link-local ou refusés conservent le fallback. Les redirections et proxys sont hors support du spike.

## Invariants et exigences futures

- La récupération ne doit jamais bloquer la création, l’ouverture ou la reprise du dossier ; utiliser un fallback générique sans image cassée.
- Le favicon est une décoration d’identification, jamais une preuve que le site est conforme ou même réellement audité.
- Les onglets/listes conservent un nom et un domaine textuels accessibles ; l’image ne porte pas seule l’identité du dossier.
- Le favicon est une métadonnée locale distincte des preuves et du profil navigateur ; aucun cookie, jeton ou credential ne doit être envoyé ou stocké par cette fonction.
- Les erreurs, l’absence de favicon et le mode hors-ligne gardent le dossier pleinement utilisable.
- Aucun service tiers, requête cross-origin, redirection, découverte HTML `<link rel="icon">` ni SVG distant n’est pris en charge par ce spike.

## Options comparées

1. **Requête HTTP limitée à la création :** timing choisi et implémenté selon les limites et fallback ci-dessus.
2. **Favicon de la page via WebView2 :** piste future si l’on veut suivre la page réellement rendue après navigation ; ne remplace pas la demande favicon à la création.
3. **Service tiers de favicon :** rejeté ; introduit un destinataire externe non nécessaire et divulgue les domaines auditables.

## Limites du spike

Ce spike demande uniquement `/favicon.ico` sur la même origine HTTPS. Il ne suit pas les liens HTML `<link rel="icon">`, les redirections ni les URL d’icône tierces, et refuse les SVG distants. Si l’icône manque, si le réseau est indisponible ou si l’URL est refusée, le dossier est créé avec un globe local. Une découverte plus complète exigerait une nouvelle revue de sécurité.

## Vérification déterministe et limites restantes

Le projet sans package `spikes/windows/wpf-native-spike/favicon-checks` utilise une réponse HTTP factice et une résolution DNS injectée. Ses **14 checks** couvrent PNG valide et réencodage, 404, timeout réseau et corps bloqué, contenu malformé/surdimensionné, rejet SVG/redirection/URL/IPv4/IPv6 refusés, opt-out, fallback globe, association par ID lors de changements/fermetures d’onglet, templates WPF et cache/manifest/ImageSource gelée après rechargement. Aucun test ne contacte un vrai site. Les templates sont vérifiés statiquement ; un rendu WPF/UI Automation d’un favicon téléchargé n’est pas revendiqué.

Ces tests ne valident ni le DNS du système, ni TLS/certificats, ni l’adresse observée par le serveur distant, ni toutes les affectations spéciales IANA. Le filtre d’adresses publiques est une denylist et pas un registre IANA exhaustif. Le réseau réel n’est pas testé ; toute insuffisance de filtrage reste une limite de sécurité du prototype.

## Avis spécialisés reçus

- `browser-architecture` recommande un fetcheur HTTP limité comme expérience isolée, puis préférer l’API favicon WebView2 après navigation réelle.
- `auditor-ux` recommande favicon non bloquant avec fallback et soulève le rafraîchissement après changement d’URL.
- `err0r-hugo` relève un flux de données réseau à divulguer et recommande une politique de récupération explicite.
- `err0r-samira` recommande de bloquer tout fetch externe dans le spike actuel et détaille les garde-fous SSRF, redirect, formats et cache.
- `decision-facilitator` recommandait initialement de différer le fetch ; l’utilisateur a ensuite confirmé la requête dès la création. La préférence temporelle utilisateur est retenue, avec les garde-fous et limites du spike ci-dessus ; la navigation WebView reste fixture-only.
