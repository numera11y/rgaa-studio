using System.Text.Json;

namespace RgaaStudio.Core.Audits;

internal static class BundledReferenceCatalogs
{
    private sealed record BundledCatalog(string ResourceName, string StableId, int Themes, int Criteria, int Tests);

    private static readonly BundledCatalog[] Catalogs =
    [
        new("RgaaStudio.Core.ReferenceCatalogs.rgaa-4.1.2.json", "rgaa:4.1.2", 13, 106, 258),
        new("RgaaStudio.Core.ReferenceCatalogs.wcag-2.2.json", "wcag:2.2", 4, 86, 86)
    ];

    internal static void EnsureAvailable(EmbeddedAuditDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        var existingCatalogs = database.GetReferenceVersions();

        foreach (var bundled in Catalogs)
        {
            var catalog = ReadCatalog(bundled);
            var existing = existingCatalogs.FirstOrDefault(item => item.StableId == bundled.StableId);
            if (existing is not null && IsComplete(existing, bundled))
                continue;

            database.StoreReferenceVersion(catalog);
            existingCatalogs = database.GetReferenceVersions();
        }
    }

    private static StoredReferenceVersion ReadCatalog(BundledCatalog expected)
    {
        using var stream = typeof(BundledReferenceCatalogs).Assembly.GetManifestResourceStream(expected.ResourceName)
            ?? throw new InvalidDataException($"Bundled reference catalog resource is missing: {expected.ResourceName}");
        var catalog = JsonSerializer.Deserialize<StoredReferenceVersion>(stream)
            ?? throw new InvalidDataException($"Bundled reference catalog is empty: {expected.ResourceName}");

        if (expected.StableId == "wcag:2.2")
            RestoreWcagLevels(catalog);

        if (catalog.StableId != expected.StableId || !IsComplete(catalog, expected))
            throw new InvalidDataException($"Bundled reference catalog failed validation: {expected.StableId}");

        return catalog;
    }

    private static void RestoreWcagLevels(StoredReferenceVersion catalog)
    {
        foreach (var criterion in catalog.Themes.SelectMany(theme => theme.Criteria))
        {
            using var references = JsonDocument.Parse(criterion.ReferencesJson);
            var level = references.RootElement.ValueKind == JsonValueKind.Array
                ? references.RootElement.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.Object && item.TryGetProperty("wcagLevel", out _))
                    .Select(item => item.GetProperty("wcagLevel").GetString())
                    .FirstOrDefault(value => value is "A" or "AA" or "AAA")
                : null;
            if (level is null)
                throw new InvalidDataException($"The WCAG level is missing for criterion {criterion.Code}.");
            criterion.Level = level;
        }
    }

    private static bool IsComplete(StoredReferenceVersion catalog, BundledCatalog expected) =>
        string.Equals(catalog.CatalogStatus, "Importé", StringComparison.OrdinalIgnoreCase) &&
        catalog.ExpectedThemeCount == expected.Themes &&
        catalog.ExpectedCriterionCount == expected.Criteria &&
        catalog.ExpectedTestCount == expected.Tests &&
        catalog.Themes.Count == expected.Themes &&
        catalog.Themes.Sum(theme => theme.Criteria.Count) == expected.Criteria &&
        catalog.Themes.Sum(theme => theme.Criteria.Sum(criterion => criterion.Tests.Count)) == expected.Tests &&
        (expected.StableId != "wcag:2.2" || catalog.Themes.SelectMany(theme => theme.Criteria)
            .All(criterion => criterion.Level is "A" or "AA" or "AAA"));
}
