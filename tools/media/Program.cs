using RgaaStudio.Core.Audits;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: PressKit.Seed <path-to-temporary-litedb>");
    return 2;
}

var databasePath = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
var logPath = Path.Combine(Path.GetDirectoryName(databasePath)!, Path.GetFileNameWithoutExtension(databasePath) + "-log" + Path.GetExtension(databasePath));
var settingsPath = Path.Combine(Path.GetDirectoryName(databasePath)!, "settings.json");
if (File.Exists(databasePath) || File.Exists(logPath) || File.Exists(settingsPath))
{
    Console.Error.WriteLine("Refusing to overwrite an existing database, LiteDB log, or settings file. Choose a fresh, dedicated press-kit directory.");
    return 3;
}

using var database = new EmbeddedAuditDatabase(databasePath, importLegacyData: false);
database.StoreReferenceVersion(new StoredReferenceVersion
{
    StableId = "press-kit:rgaa:4.1.2",
    FrameworkId = "rgaa",
    FrameworkName = "RGAA",
    VersionCode = "4.1.2",
    Jurisdiction = "France",
    Language = "fr",
    LicenseName = "Licence ouverte Etalab 2.0",
    CatalogStatus = "Importé"
});
database.StoreReferenceVersion(new StoredReferenceVersion
{
    StableId = "press-kit:wcag:2.2",
    FrameworkId = "wcag",
    FrameworkName = "WCAG",
    VersionCode = "2.2",
    Jurisdiction = "International",
    Language = "en",
    LicenseName = "W3C Document License",
    CatalogStatus = "Importé"
});

var candidates = new SitePageCandidate[]
{
    new("Accueil", "https://www.numera11y.fr/", "Accueil", true),
    new("Audit d’accessibilité", "https://www.numera11y.fr/offres/audit-accessibilite/", "Autre page", false),
    new("Offres", "https://www.numera11y.fr/offres/", "Autre page", false),
    new("Réalisations", "https://www.numera11y.fr/realisations/", "Autre page", false),
    new("Ressources", "https://www.numera11y.fr/ressources/", "Autre page", false),
    new("Mentions légales", "https://www.numera11y.fr/mentions-legales/", "Mentions légales", true),
    new("Rendez-vous", "https://www.numera11y.fr/rendez-vous/", "Autre page", false)
};
var audit = database.Create(
    new NewAudit("https://www.numera11y.fr", "RGAA", "4.1.2", null), candidates, null);
var pages = database.GetPages(audit.Id);

Console.WriteLine($"Created isolated press-kit dossier for: {audit.SiteUrl}");
Console.WriteLine($"Database: {databasePath}");
Console.WriteLine($"Pages: {pages.Count}; the seed step fetches no page content and no favicon.");
return 0;
