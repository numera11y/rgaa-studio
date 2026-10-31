using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace RgaaStudio.Core.Navigation;

public sealed record TechnologyDetection(
    string Name,
    string Category,
    string Confidence,
    string Evidence,
    string Version = "");

/// <summary>Small, offline fingerprint engine over the rendered page's HTML, DOM and already-loaded asset URLs.</summary>
public static partial class PageTechnologyDetector
{
    public static IReadOnlyList<TechnologyDetection> Detect(string html, IEnumerable<string>? runtimeGlobals = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        var document = new HtmlParser().ParseDocument(html);
        var scriptUrls = document.QuerySelectorAll("script[src]").Select(element => element.GetAttribute("src") ?? "").ToArray();
        var styleUrls = document.QuerySelectorAll("link[rel~='stylesheet'][href]").Select(element => element.GetAttribute("href") ?? "").ToArray();
        var assets = string.Join('\n', scriptUrls.Concat(styleUrls));
        var inlineScripts = string.Join('\n', document.QuerySelectorAll("script:not([src])").Select(element => element.TextContent));
        var classes = string.Join(' ', document.QuerySelectorAll("[class]").Select(element => element.GetAttribute("class")));
        var lowerHtml = html.ToLowerInvariant();
        var lowerAssets = assets.ToLowerInvariant();
        var lowerInline = inlineScripts.ToLowerInvariant();
        var lowerClasses = classes.ToLowerInvariant();
        var globals = new HashSet<string>(runtimeGlobals ?? [], StringComparer.OrdinalIgnoreCase);
        var matches = new List<TechnologyDetection>();

        var generator = document.QuerySelector("meta[name='generator' i]")?.GetAttribute("content") ?? "";
        var HasGenerator = (string pattern) => Regex.IsMatch(generator, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var HasHtml = (string pattern) => Regex.IsMatch(lowerHtml, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var HasAssets = (string pattern) => Regex.IsMatch(lowerAssets, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var HasInline = (string pattern) => Regex.IsMatch(lowerInline, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var HasClass = (string pattern) => Regex.IsMatch(lowerClasses, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var HasGlobal = (string name) => globals.Contains(name);
        var HasDom = (string selector) => document.QuerySelector(selector) is not null;

        void Add(string name, string category, string? version, params (bool Matched, string Evidence)[] indicators)
        {
            var evidence = indicators.Where(item => item.Matched).Select(item => item.Evidence).Distinct(StringComparer.Ordinal).Take(3).ToArray();
            if (evidence.Length == 0)
                return;
            var confidence = evidence.Length > 1 ? "Confiance renforcée · plusieurs indices" : "Indice à confirmer";
            matches.Add(new TechnologyDetection(name, category, confidence, string.Join(" · ", evidence), version ?? ""));
        }

        Add("WordPress", "CMS", "",
            (HasGenerator("wordpress"), "Métadonnée generator WordPress"),
            (HasAssets("wp-(?:content|includes)"), "URL d’asset wp-content / wp-includes"),
            (HasHtml("wp-json|wp-embed|wp-block"), "Marqueur WordPress dans le HTML"),
            (HasClass("(?:^|\\s)wp-block-"), "Classes de blocs WordPress"));
        Add("Drupal", "CMS", "",
            (HasGenerator("drupal"), "Métadonnée generator Drupal"),
            (HasGlobal("Drupal") || HasGlobal("drupalSettings"), "Global JavaScript Drupal"),
            (HasAssets("/sites/(?:default|all)/|drupal\\.js"), "Asset Drupal"),
            (HasDom("[data-drupal-selector]"), "Attribut data-drupal-selector"));
        Add("Joomla", "CMS", "",
            (HasGenerator("joomla"), "Métadonnée generator Joomla"),
            (HasAssets("/media/(?:system|vendor)/|/components/com_"), "Chemin d’asset Joomla"),
            (HasHtml("com_content|joomla!|joomla-script-options"), "Marqueur Joomla dans le HTML"),
            (HasDom("[class*='joomla'],[id^='joomla-']"), "Classe ou identifiant Joomla dans le DOM"));
        Add("Shopify", "E-commerce", "",
            (HasGenerator("shopify"), "Métadonnée generator Shopify"),
            (HasGlobal("Shopify"), "Global JavaScript Shopify"),
            (HasAssets("cdn\\.shopify\\.com|shopifycloud"), "Asset Shopify"),
            (HasDom("[data-shopify]"), "Attribut data-shopify"),
            (HasClass("shopify-section"), "Classe shopify-section"));
        Add("PrestaShop", "E-commerce", "",
            (HasGenerator("prestashop"), "Métadonnée generator PrestaShop"),
            (HasGlobal("prestashop"), "Global JavaScript prestashop"),
            (HasAssets("/modules/[^/]+/(?:views|assets)/|prestashop"), "Chemin d’asset PrestaShop"),
            (HasHtml("prestashop\\.static_token|prestashop_version"), "Marqueur PrestaShop dans le HTML"));
        Add("Magento / Adobe Commerce", "E-commerce", "",
            (HasGenerator("magento"), "Métadonnée generator Magento"),
            (HasGlobal("requirejs") && HasAssets("/static/version"), "RequireJS et assets Magento versionnés"),
            (HasHtml("mage/cookies|mage\\.apply|data-mage-init"), "Marqueur Magento dans le HTML"));
        Add("Wix", "CMS", "",
            (HasGenerator("wix"), "Métadonnée generator Wix"),
            (HasAssets("static\\.wixstatic\\.com|wix-code-sdk"), "Asset Wix"),
            (HasHtml("wixsite\\.com|wix-thunderbolt"), "Marqueur Wix dans le HTML"));
        Add("Webflow", "CMS", "",
            (HasGenerator("webflow"), "Métadonnée generator Webflow"),
            (HasDom("[data-wf-site],[data-wf-page],[data-wf-domain]"), "Attributs data-wf-* dans le DOM"),
            (HasAssets("webflow\\.js|website-files\\.com|webflow\\.io"), "Asset ou domaine Webflow"),
            (HasClass("(?:^|\\s)w-webflow-"), "Classe Webflow dans le DOM"));
        Add("Squarespace", "CMS", "",
            (HasGenerator("squarespace"), "Métadonnée generator Squarespace"),
            (HasAssets("static\\.squarespace\\.com|squarespace-cdn"), "Asset Squarespace"),
            (HasDom("[data-squarespace]"), "Attribut data-squarespace"));
        Add("Ghost", "CMS", "",
            (HasGenerator("ghost"), "Métadonnée generator Ghost"),
            (HasAssets("ghost\\.org|/ghost/api/"), "Asset ou endpoint Ghost référencé dans la page"),
            (HasDom("[data-ghost]"), "Attribut data-ghost"));
        Add("TYPO3", "CMS", "",
            (HasGenerator("typo3"), "Métadonnée generator TYPO3"),
            (HasAssets("typo3conf|_assets/[^/]+/.*typo3"), "Chemin d’asset TYPO3"),
            (HasDom("[data-typo3]"), "Attribut data-typo3"));
        Add("Astro", "Framework / rendu", "",
            (HasGenerator("astro"), "Métadonnée generator Astro"),
            (HasDom("astro-island"), "Élément astro-island dans le DOM"),
            (HasAssets("/_astro/|astro/client"), "Asset compilé par Astro"),
            (HasInline("astro\\.js|astro-island"), "Marqueur Astro dans un script inline"));
        Add("NestJS", "Framework serveur", "",
            (HasGenerator("nestjs|nest\\.js"), "Métadonnée generator NestJS"),
            (HasHtml("@nestjs/|nestfactory|nestjs"), "Référence explicite à NestJS dans le HTML ou les scripts"),
            (HasAssets("@nestjs/|nestjs\\."), "Asset public portant un marqueur NestJS"));
        Add("Elementor", "Constructeur de pages", "",
            (HasAssets("elementor/assets|elementor-frontend"), "Asset Elementor"),
            (HasClass("elementor(?:-page|-widget|-element)"), "Classes Elementor dans le DOM"));

        Add("Next.js", "Framework / rendu", "",
            (HasDom("#__NEXT_DATA__"), "Élément __NEXT_DATA__"),
            (HasAssets("/_next/"), "Assets servis sous /_next/"),
            (HasGlobal("__NEXT_DATA__"), "Global JavaScript __NEXT_DATA__"));
        Add("Nuxt", "Framework / rendu", "",
            (HasGlobal("__NUXT__"), "Global JavaScript __NUXT__"),
            (HasAssets("/_nuxt/"), "Assets servis sous /_nuxt/"),
            (HasDom("#__nuxt"), "Élément racine #__nuxt"));
        Add("Gatsby", "Framework / rendu", "",
            (HasDom("#___gatsby"), "Élément racine Gatsby #___gatsby"),
            (HasAssets("/static/[^/]+/component---|gatsby-browser"), "Assets Gatsby"),
            (HasHtml("___gatsby|___loader"), "Marqueur Gatsby dans le HTML"));
        Add("React", "Framework JavaScript", "",
            (HasGlobal("React"), "Global JavaScript React"),
            (HasDom("[data-reactroot],[data-reactid]"), "Attribut React dans le DOM"),
            (HasAssets("react(?:-dom)?(?:\\.production)?(?:\\.min)?\\.js"), "Asset React"),
            (HasInline("react\\.production|react-dom"), "Référence React dans un script inline"));
        Add("Vue.js", "Framework JavaScript", "",
            (HasGlobal("Vue"), "Global JavaScript Vue"),
            (HasDom("[data-v-app],[data-vue-meta]"), "Attribut Vue dans le DOM"),
            (HasClass("(?:^|\\s)data-v-[a-f0-9]+"), "Classes de scope Vue"),
            (HasAssets("vue(?:\\.runtime)?(?:\\.global)?(?:\\.prod)?\\.js"), "Asset Vue"));
        Add("Angular", "Framework JavaScript", "",
            (HasGlobal("ng"), "Global JavaScript Angular"),
            (HasDom("[ng-version]"), "Attribut ng-version"),
            (HasAssets("angular(?:\\.min)?\\.js|zone\\.js"), "Asset Angular / Zone.js"));
        Add("Svelte", "Framework JavaScript", "",
            (HasDom("[class*='svelte-']"), "Classes compilées Svelte dans le DOM"),
            (HasAssets("svelte(?:\\.esm)?\\.js"), "Asset Svelte"),
            (HasInline("svelte/internal"), "Référence svelte/internal dans un script inline"));
        Add("Alpine.js", "Bibliothèque JavaScript", "",
            (HasGlobal("Alpine"), "Global JavaScript Alpine"),
            (HasDom("[x-data]"), "Attribut déclaratif x-data"),
            (HasAssets("alpine(?:\\.min)?\\.js"), "Asset Alpine.js"));
        Add("htmx", "Bibliothèque JavaScript", "",
            (HasGlobal("htmx"), "Global JavaScript htmx"),
            (HasDom("[hx-get],[hx-post],[hx-trigger]"), "Attributs hx-* dans le DOM"),
            (HasAssets("htmx(?:\\.min)?\\.js"), "Asset htmx"));
        Add("jQuery", "Bibliothèque JavaScript", GetJQueryVersion(assets + "\n" + inlineScripts),
            (HasGlobal("jQuery"), "Global JavaScript jQuery"),
            (HasAssets("jquery(?:[-/])(?:core/)?[0-9.]+|jquery(?:\\.min)?\\.js"), "Asset jQuery"),
            (HasInline("jQuery\\.fn\\.jquery|jquery-migrate"), "Signature jQuery dans un script inline"));
        Add("Swiper", "Bibliothèque JavaScript", "",
            (HasGlobal("Swiper"), "Global JavaScript Swiper"),
            (HasDom(".swiper,.swiper-container"), "Structure DOM Swiper"),
            (HasAssets("swiper(?:\\.bundle)?(?:\\.min)?\\.js"), "Asset Swiper"));

        Add("Bootstrap", "Interface / CSS", GetVersionFromAssets(assets, "bootstrap(?:\\.bundle)?(?:\\.min)?[-.]?(?<version>[0-9]+(?:\\.[0-9]+){1,2})?"),
            (HasAssets("bootstrap(?:\\.bundle)?(?:\\.min)?(?:[-.][0-9.]+)?\\.(?:js|css)"), "Asset Bootstrap"),
            (HasDom(".container > .row > [class*='col-'],.btn.btn-primary"), "Structure ou classes Bootstrap"),
            (HasClass("(?:^|\\s)bootstrap(?:\\s|$)"), "Classe bootstrap"));
        Add("Tailwind CSS", "Interface / CSS", "",
            (HasAssets("tailwind(?:css)?(?:\\.min)?\\.css"), "Asset Tailwind CSS"),
            (HasClass("(?:^|\\s)(?:flex|grid|items-center|justify-between|text-(?:xs|sm|base|lg)|(?:p|m|px|py)-[0-9]+)(?:\\s|$)"), "Classes utilitaires caractéristiques"));
        Add("Material UI", "Interface / composants", "",
            (HasClass("(?:^|\\s)Mui(?:Button|Typography|Paper|Box)-root(?:\\s|$)"), "Classes de composants Mui"),
            (HasAssets("@mui|material-ui"), "Asset Material UI"));
        Add("Font Awesome", "Icônes", "",
            (HasAssets("fontawesome|font-awesome"), "Asset Font Awesome"),
            (HasClass("(?:^|\\s)fa(?:s|r|b|l)?(?:\\s|$)"), "Classes d’icônes fa-*"));

        Add("Google Tag Manager", "Mesure / gestion de tags", "",
            (HasGlobal("google_tag_manager"), "Global google_tag_manager"),
            (HasAssets("googletagmanager\\.com/gtm\\.js"), "Script GTM référencé dans la page"),
            (HasHtml("googletagmanager\\.com/ns\\.html"), "Iframe noscript GTM"));
        Add("Google Analytics / gtag", "Mesure d’audience", "",
            (HasGlobal("gtag"), "Global gtag"),
            (HasAssets("googletagmanager\\.com/gtag/js|google-analytics\\.com/analytics\\.js"), "Script Google Analytics référencé"),
            (HasHtml("google-analytics\\.com/g/collect|google-analytics\\.com/collect"), "Endpoint Analytics présent dans le HTML"));
        Add("Matomo", "Mesure d’audience", "",
            (HasGlobal("_paq"), "Global _paq"),
            (HasAssets("matomo(?:\\.min)?\\.js|piwik(?:\\.min)?\\.js"), "Script Matomo / Piwik référencé"));
        Add("Plausible", "Mesure d’audience", "",
            (HasAssets("plausible\\.io/js/"), "Script Plausible référencé dans la page"));
        Add("Hotjar", "Mesure / expérience", "",
            (HasGlobal("hj"), "Global hj"),
            (HasAssets("static\\.hotjar\\.com|script\\.hotjar\\.com"), "Script Hotjar référencé"));
        Add("Microsoft Clarity", "Mesure / expérience", "",
            (HasGlobal("clarity"), "Global clarity"),
            (HasAssets("clarity\\.ms/tag/"), "Script Clarity référencé"));
        Add("Stripe", "Paiement", "",
            (HasGlobal("Stripe"), "Global Stripe"),
            (HasAssets("js\\.stripe\\.com/v[0-9]/"), "Script Stripe référencé"));
        Add("Cloudflare Web Analytics", "Mesure / infrastructure", "",
            (HasAssets("static\\.cloudflareinsights\\.com/beacon\\.min\\.js"), "Beacon Cloudflare Insights référencé"));

        return matches.OrderBy(item => item.Category, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static string GetJQueryVersion(string source)
    {
        var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        var match = Regex.Match(source, @"jquery(?:[-/])(?<version>[0-9]+\.[0-9]+(?:\.[0-9]+)?)", options);
        if (!match.Success)
            match = Regex.Match(source, """jquery[^\s'"]*?[?&](?:ver|version)=(?<version>[0-9]+\.[0-9]+(?:\.[0-9]+)?)""", options);
        if (!match.Success)
            match = Regex.Match(source, @"jQuery(?: JavaScript Library)?\s+v(?<version>[0-9]+\.[0-9]+(?:\.[0-9]+)?)", options);
        return match.Success ? match.Groups["version"].Value : string.Empty;
    }

    private static string GetVersionFromAssets(string source, string pattern)
    {
        var match = Regex.Match(source, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Groups["version"].Success ? match.Groups["version"].Value : string.Empty;
    }
}
