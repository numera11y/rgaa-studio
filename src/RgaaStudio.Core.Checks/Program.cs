using RgaaStudio.Core.Audits;
using RgaaStudio.Core.Navigation;
using System.Buffers.Binary;

var svgFavicon = """
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 257.144">
      <path fill="#214b9a" d="M57.1444 57.1444L114.289 0V57.1444H200V200L142.866 257.144V114.289H57.1444V200L7.00368e-06 257.144V114.289L57.1444 57.1444Z" />
    </svg>
    """;
    var normalizedSvgFavicon = SiteDiscoveryService.NormalizeFavicon(System.Text.Encoding.UTF8.GetBytes(svgFavicon))
        ?? throw new InvalidOperationException("The SVG favicon fixture did not normalize to PNG.");
var formattedHtml = SiteDiscoveryService.FormatHtmlSource("<!doctype html><html><body><main><h1>Audit</h1><p>Page</p></main></body></html>");
if (!formattedHtml.Contains('\n') || !formattedHtml.Contains("<main>", StringComparison.Ordinal))
    throw new InvalidOperationException("HTML source formatting did not produce readable structure.");
var formattedElement = SiteDiscoveryService.FormatHtmlFragment("<img class=\"brand-logo\" src=\"/logo.svg\" alt=\"\">");
if (!formattedElement.StartsWith("<img", StringComparison.OrdinalIgnoreCase) || formattedElement.Contains("<html", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException($"Element source formatting must only show the selected element: {formattedElement}");

var technologyFixture = """
    <!doctype html><html><head><meta name="generator" content="WordPress 6.7"><script src="/wp-includes/js/jquery/jquery-3.7.1.min.js"></script></head>
    <body><div data-reactroot class="elementor-page"><img src="brand.svg" alt=""><img src="hero.svg"></div></body></html>
    """;
var technologies = PageTechnologyDetector.Detect(technologyFixture, ["React"]);
if (!technologies.Any(item => item.Name == "WordPress") || !technologies.Any(item => item.Name == "Elementor") ||
    !technologies.Any(item => item.Name == "React") || !technologies.Any(item => item.Name == "jQuery"))
    throw new InvalidOperationException("Page technology fingerprints did not detect representative metadata, DOM, asset and runtime-global signals.");
if (technologies.Single(item => item.Name == "jQuery").Version != "3.7.1")
    throw new InvalidOperationException("The page technology detector did not extract the version from an asset filename.");
var moreTechnologyFixture = """
    <!doctype html><html data-wf-site="site-id"><head><meta name="generator" content="Joomla! - Open Source Content Management"></head>
    <body><astro-island component-url="/_astro/Counter.js"></astro-island><main data-wf-page="page-id"></main><!-- @nestjs/core marker --></body></html>
    """;
var moreTechnologies = PageTechnologyDetector.Detect(moreTechnologyFixture);
if (!moreTechnologies.Any(item => item.Name == "Joomla") || !moreTechnologies.Any(item => item.Name == "Webflow") ||
    !moreTechnologies.Any(item => item.Name == "Astro") || !moreTechnologies.Any(item => item.Name == "NestJS"))
    throw new InvalidOperationException("Page technology fingerprints missed Joomla, Webflow, Astro or a public NestJS marker.");
var blackOnWhite = ColorContrastAnalyzer.Measure("rgb(0, 0, 0)", ["rgba(255, 255, 255, 1)"]);
var grayOnWhite = ColorContrastAnalyzer.Measure("#777", ["#fff"]);
var translucentForeground = ColorContrastAnalyzer.Measure("rgba(0, 0, 0, 0.5)", ["#fff"]);
if (blackOnWhite is null || Math.Abs(blackOnWhite.Ratio - 21) > 0.01 || grayOnWhite is null || grayOnWhite.Ratio >= 4.5 ||
    translucentForeground is null || translucentForeground.Ratio is < 3.9 or > 4.1 ||
    ColorContrastAnalyzer.RequiredRatio(largeText: false, aaa: true) != 7 || ColorContrastAnalyzer.RequiredRatio(largeText: true, aaa: false) != 3)
    throw new InvalidOperationException("WCAG color contrast ratio or target thresholds were calculated incorrectly.");

var root = Path.Combine(Path.GetTempPath(), "rgaa-studio-core-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var databasePath = Path.Combine(root, "checks.litedb");
    using (var store = new EmbeddedAuditDatabase(databasePath, importLegacyData: false))
    {
        store.StoreReferenceVersion(new StoredReferenceVersion
        {
            StableId = "rgaa:4.1.2",
            FrameworkId = "rgaa",
            FrameworkName = "RGAA",
            VersionCode = "4.1.2",
            CatalogStatus = "Importé"
        });
        store.StoreReferenceVersion(new StoredReferenceVersion
        {
            StableId = "wcag:2.2",
            FrameworkId = "wcag",
            FrameworkName = "WCAG",
            VersionCode = "2.2",
            CatalogStatus = "Importé"
        });

        SitePageCandidate[] selectedPages =
        [
            new SitePageCandidate("Accueil", "https://audit.example/", "Accueil", true),
            .. Enumerable.Range(1, 1)
                .Select(index => new SitePageCandidate($"Page {index}", $"https://audit.example/page-{index}", "Autre page", false))
        ];
        var favicon = normalizedSvgFavicon;
        var rgaa = store.Create(new NewAudit("https://audit.example/", "RGAA", "RGAA 4.1.2", null), selectedPages, favicon);
        var wcag = store.Create(new NewAudit("https://audit.example/path", "WCAG", "WCAG 2.2", "AA"),
            [new SitePageCandidate("Accueil", "https://audit.example/path", "Accueil", true)], null);
        Check(rgaa.SiteUrl == "https://audit.example/", "site URL preserved");
        Check(rgaa.Level == string.Empty && wcag.Level == "AA", "reference-specific target stored");
        Check(store.List().Count == 2, "created dossiers listed");
        Check(store.Get(rgaa.Id)?.Id == rgaa.Id, "dossier can be resumed by GUID D");
        Check(Guid.TryParseExact(rgaa.Id, "D", out _), "audit ID is stored as GUID D");
        Check(store.GetPages(rgaa.Id).Count == 2, "selected audit pages are persisted");
        Check(store.MarkPageOpened(rgaa.Id, store.GetPages(rgaa.Id).Single(page => page.Name == "Accueil").Id), "opening a page records its audit activity");
        Check(store.GetPages(rgaa.Id).Single(page => page.Name == "Accueil").LastOpenedAtUtc is not null, "page activity is persisted for the audit overview");
        Check(store.GetFaviconData(rgaa.Id)?.SequenceEqual(favicon) == true, "favicon bytes are persisted with their audit");
        Check(store.Get("not-a-guid") is null, "invalid IDs are rejected");
        var homePage = store.GetPages(rgaa.Id).Single(page => page.Name == "Accueil");
        Check(store.AddSemanticFindingToAudit(rgaa.Id, homePage.Id, "html5.main.duplicate", "Élément main en double", "Un second élément main est présent sur la page.", "main:nth-of-type(2)"),
            "semantic finding can be added to the selected audit page");
        Check(!store.AddSemanticFindingToAudit(rgaa.Id, homePage.Id, "html5.main.duplicate", "Élément main en double", "Un second élément main est présent sur la page.", "main:nth-of-type(2)"),
            "the same semantic rule is not duplicated on a page");
        store.SaveAxeSuggestions(rgaa.Id, homePage.Id,
        [
            new StoredAuditSuggestion { RuleId = "image-alt", Outcome = "Violation", Summary = "Image alternative · 1 élément", AnalyzerVersion = "4.13.0", EvidenceJson = "[]" },
            new StoredAuditSuggestion { RuleId = "color-contrast", Outcome = "À vérifier", Summary = "Contraste · 1 élément", AnalyzerVersion = "4.13.0", EvidenceJson = "[]" }
        ]);
        var axeResult = store.GetPages(rgaa.Id).Single(page => page.Id == homePage.Id).Suggestions.Single(item => item.RuleId == "image-alt");
        Check(axeResult.AnalyzerVersion == "axe-core 4.13.0" && axeResult.ReviewStatus == "À vérifier", "axe result and analyzer version are persisted as an unreviewed suggestion");
        Check(store.SetAxeSuggestionReview(rgaa.Id, homePage.Id, axeResult.Id, "Retenu"), "axe suggestion can be retained by the auditor");
        store.SaveAxeSuggestions(rgaa.Id, homePage.Id,
        [new StoredAuditSuggestion { RuleId = "image-alt", Outcome = "Violation", Summary = "Image alternative · 1 élément", AnalyzerVersion = "4.13.0", EvidenceJson = "[]" }]);
        Check(store.GetPages(rgaa.Id).Single(page => page.Id == homePage.Id).Suggestions.Single(item => item.RuleId == "image-alt").ReviewStatus == "Retenu",
            "rerunning axe preserves an auditor's review status for a matching rule");
        var proof = store.AddEvidence(rgaa.Id, homePage.Id, "axe-core · image-alt", "IMG", "Logo", "DOM HTML",
            "<img src=\"logo.svg\" alt=\"\">", [1, 2, 3], "image/png");
        Check(store.GetEvidence(rgaa.Id, homePage.Id).Single().Id == proof.Id && proof.ImageData?.SequenceEqual(new byte[] { 1, 2, 3 }) == true,
            "page evidence context and image bytes persist with the audit");
        Check(store.GetEvidenceCount(rgaa.Id) == 1, "audit evidence count is available for its overview");
        Check(store.SetArchived(rgaa.Id, true), "audit can be archived");
        Check(store.List().Count == 1 && store.List(includeArchived: true).Single().IsArchived, "archive is hidden from active list and remains in archive list");
        Check(store.SetArchived(rgaa.Id, false) && store.List().Count == 2, "archived audit can be restored");

        var decision = store.AppendDecision(rgaa.Id, "fixture-locale-synthetique", "Contrôle manuel de démonstration", "Conforme");
        Check(store.ReadDecisions(rgaa.Id).Single().Id == decision.Id, "decision event can be read from its audit");
        ExpectArgument(() => store.AppendDecision(rgaa.Id, "fixture-locale-synthetique", "Critère manuel", "Non applicable"));
        var completed = store.CompleteLatestReview(rgaa.Id);
        Check(completed.ReviewComplete && store.ReadDecisions(rgaa.Id).Count == 2, "review completion is an append-only event");
        ExpectInvalidOperation(() => store.CompleteLatestReview(rgaa.Id));
        var addedCount = store.AddPagesToAudit(rgaa.Id,
            [new SitePageCandidate("Contact", "https://audit.example/contact", "Contact", true)]);
        Check(addedCount == 1 && store.GetPages(rgaa.Id).Count == 3, "newly selected page is added to an existing audit");
        Check(store.AddPagesToAudit(rgaa.Id,
            [new SitePageCandidate("Contact", "https://audit.example/contact", "Contact", true)]) == 0,
            "an already included page is not added a second time");
        ExpectArgument(() => store.AddPageToAudit(rgaa.Id, "Contact", "https://audit.example/contact"));
        ExpectArgument(() => store.AddPageToAudit(rgaa.Id, "Externe", "https://outside.example/page"));
        foreach (var index in Enumerable.Range(1, SiteDiscoveryService.MaximumOtherPages - 1))
            store.AddPageToAudit(rgaa.Id, $"Page manuelle {index}", $"https://audit.example/manual-{index}");
        ExpectArgument(() => store.AddPageToAudit(rgaa.Id, "Trop de pages", "https://audit.example/over-limit"));
        var removablePage = store.GetPages(rgaa.Id).Single(page => page.Name == "Page manuelle 1");
        Check(store.RemovePageFromAudit(rgaa.Id, removablePage.Id) && store.GetPages(rgaa.Id).Count == 26,
            "a page can be removed without deleting its audit");
        ExpectArgument(() => store.Create(new NewAudit("https://audit.example/", "RGAA", "RGAA 4.1.2", null),
            Enumerable.Range(1, SiteDiscoveryService.MaximumOtherPages + 1)
                .Select(index => new SitePageCandidate($"Page {index}", $"https://audit.example/extra-{index}", "Autre page", false)).ToArray(), null));
        store.AppendDecision(wcag.Id, "Accueil", "Test", "Non conforme");
        Check(store.DeleteAudit(wcag.Id), "audit can be permanently deleted");
        Check(store.Get(wcag.Id) is null && store.ReadDecisions(wcag.Id).Count == 0, "deletion removes the dossier and linked decisions");
        Check(store.SetArchived(rgaa.Id, true), "archive state can be saved before closing the database");
    }

    using (var reopened = new EmbeddedAuditDatabase(databasePath, importLegacyData: false))
    {
        Check(reopened.List().Count == 0 && reopened.List(includeArchived: true).Count == 1, "archive state and deletion persist after reopening the embedded database");
        var archivedAudit = reopened.List(includeArchived: true).Single();
        Check(reopened.GetPages(archivedAudit.Id).Count == 26 && reopened.GetPages(archivedAudit.Id).Any(page => page.Name == "Contact") &&
              reopened.GetPages(archivedAudit.Id).All(page => page.Name != "Page manuelle 1") &&
              reopened.GetPages(archivedAudit.Id).Single(page => page.Name == "Accueil").LastOpenedAtUtc is not null,
            "selected pages, page activity, manual additions and page deletion persist after reopening");
        Check(reopened.GetPages(archivedAudit.Id).Single(page => page.Name == "Accueil").Findings.Single().RequiresAuditorReview,
            "HTML semantic findings remain explicitly marked for auditor review");
        Check(reopened.SetArchived(archivedAudit.Id, false) && reopened.List().Count == 1, "an archived audit can be restored after reopening");
        Check(reopened.GetReferenceVersions().Count == 2, "reference versions persist in the embedded database");
    }

    var legacySqlite = Environment.GetEnvironmentVariable("RGAA_STUDIO_CHECK_LEGACY_SQLITE");
    if (!string.IsNullOrWhiteSpace(legacySqlite))
    {
        var migrationPath = Path.Combine(root, "legacy-import.litedb");
        var previousDatabasePath = Environment.GetEnvironmentVariable("RGAA_STUDIO_DATABASE_PATH");
        var previousLegacyPath = Environment.GetEnvironmentVariable("RGAA_STUDIO_LEGACY_SQLITE_PATH");
        var previousSkipImport = Environment.GetEnvironmentVariable("RGAA_STUDIO_SKIP_LEGACY_IMPORT");
        try
        {
            Environment.SetEnvironmentVariable("RGAA_STUDIO_DATABASE_PATH", migrationPath);
            Environment.SetEnvironmentVariable("RGAA_STUDIO_LEGACY_SQLITE_PATH", legacySqlite);
            Environment.SetEnvironmentVariable("RGAA_STUDIO_SKIP_LEGACY_IMPORT", null);
            using var migrated = new EmbeddedAuditDatabase(migrationPath);
            Check(migrated.List().Count >= 2, "legacy SQLite missions are imported alongside any legacy JSON dossiers");
            Check(migrated.GetReferenceVersions().Count == 2, "legacy SQLite reference versions are imported");
            Check(migrated.GetReferenceVersions().Sum(reference => reference.Themes.Sum(theme => theme.Criteria.Sum(criterion => criterion.Tests.Count))) == 344,
                "legacy SQLite catalog tests are imported");
        }
        finally
        {
            Environment.SetEnvironmentVariable("RGAA_STUDIO_DATABASE_PATH", previousDatabasePath);
            Environment.SetEnvironmentVariable("RGAA_STUDIO_LEGACY_SQLITE_PATH", previousLegacyPath);
            Environment.SetEnvironmentVariable("RGAA_STUDIO_SKIP_LEGACY_IMPORT", previousSkipImport);
        }
    }

    var roomyTabs = TabStripLayoutCalculator.Calculate(3, 1000);
    Check(roomyTabs.TabWidth == TabStripLayoutCalculator.PreferredTabWidth && !roomyTabs.NavigationRequired, "tabs keep preferred width when they fit");
    var compactTabs = TabStripLayoutCalculator.Calculate(5, 1100);
    Check(compactTabs.TabWidth == 212 && !compactTabs.NavigationRequired, "tabs shrink evenly before navigation is introduced");
    var overflowTabs = TabStripLayoutCalculator.Calculate(4, 398);
    Check(overflowTabs.TabWidth == TabStripLayoutCalculator.MinimumTabWidth && overflowTabs.NavigationRequired, "navigation appears only after tabs reach minimum width");
    ExpectArgument(() => SiteDiscoveryService.ValidateOrigin("http://audit.example"));
    var localScan = await new SiteDiscoveryService().ScanAsync("https://localhost");
    Check(localScan.TotalCount == 0, "site discovery does not fetch loopback destinations");
    Check(await new SiteDiscoveryService().FetchFaviconAsync("https://localhost") is null, "favicon fetch does not access loopback destinations");
    var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    var icoWithPng = new byte[30];
    BinaryPrimitives.WriteUInt16LittleEndian(icoWithPng.AsSpan(2, 2), 1);
    BinaryPrimitives.WriteUInt16LittleEndian(icoWithPng.AsSpan(4, 2), 1);
    BinaryPrimitives.WriteUInt32LittleEndian(icoWithPng.AsSpan(14, 4), (uint)pngSignature.Length);
    BinaryPrimitives.WriteUInt32LittleEndian(icoWithPng.AsSpan(18, 4), 22);
    pngSignature.CopyTo(icoWithPng, 22);
    Check(SiteDiscoveryService.ExtractPngFromIconContainer(icoWithPng)?.SequenceEqual(pngSignature) == true,
        "PNG image embedded in ICO favicon can be decoded by the cross-platform image pipeline");
    Check(normalizedSvgFavicon.Length > 8 && normalizedSvgFavicon.AsSpan().StartsWith(pngSignature),
        "real-world SVG favicon is sanitized, rasterized and normalized to PNG");
    var unsafeSvgFavicon = "<svg xmlns=\"http://www.w3.org/2000/svg\"><image href=\"https://evil.example/image.png\" /></svg>";
    Check(SiteDiscoveryService.NormalizeFavicon(System.Text.Encoding.UTF8.GetBytes(unsafeSvgFavicon)) is null,
        "SVG favicons with external image references are rejected before rasterization");
    var liveFaviconSite = Environment.GetEnvironmentVariable("RGAA_STUDIO_CHECK_FAVICON_URL");
    if (!string.IsNullOrWhiteSpace(liveFaviconSite))
    {
        var downloadedFavicon = await new SiteDiscoveryService().FetchFaviconAsync(liveFaviconSite);
        Check(downloadedFavicon is not null, "favicon is fetched from a live website");
        var normalizedFavicon = SiteDiscoveryService.NormalizeFavicon(downloadedFavicon!);
        Check(normalizedFavicon is { Length: > 8 } && normalizedFavicon.AsSpan().StartsWith(pngSignature),
            "live favicon is stored in displayable PNG format");
        var canonicalUrl = SiteDiscoveryService.ValidateOrigin(liveFaviconSite).AbsoluteUri;
        var scan = await new SiteDiscoveryService().ScanAsync(liveFaviconSite, [canonicalUrl], SiteDiscoveryService.MaximumOtherPages);
        Check(!scan.PriorityPages.Concat(scan.OtherPages).Any(page => page.Url == canonicalUrl),
            "pages already in the audit are excluded from the follow-up candidate scan");
    }

    Console.WriteLine("PASS: LiteDB audit/page/favicon persistence, bounded page scope, HTTPS validation, local-destination blocking and tab layout.");
}
finally
{
    if (Directory.Exists(root))
        Directory.Delete(root, recursive: true);
}

static void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException("FAIL: " + name);
}

static void ExpectArgument(Action action)
{
    try { action(); }
    catch (ArgumentException) { return; }
    throw new InvalidOperationException("Expected invalid input to be rejected.");
}

static void ExpectInvalidOperation(Action action)
{
    try { action(); }
    catch (InvalidOperationException) { return; }
    throw new InvalidOperationException("Expected invalid review state transition to be rejected.");
}
