using LiteDB;

namespace RgaaStudio.Core.Audits;

public sealed class StoredAuditDocument
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string? LegacySourceKey { get; set; }
    public int? LegacySqliteMissionId { get; set; }
    public string? LegacyJsonDossierId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
    public string ReferenceFrameworkId { get; set; } = string.Empty;
    public string ReferenceFrameworkName { get; set; } = string.Empty;
    public string ReferenceVersionId { get; set; } = string.Empty;
    public string ReferenceStableId { get; set; } = string.Empty;
    public string ReferenceVersion { get; set; } = string.Empty;
    public string TargetLevel { get; set; } = string.Empty;
    public string Status { get; set; } = "En préparation";
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public string? FaviconFile { get; set; }
    public byte[]? FaviconData { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public int PageScopeRevision { get; set; }
    public List<StoredAuditPage> Pages { get; set; } = [];
    public List<StoredAuditEvidence> Evidence { get; set; } = [];
}

public sealed class StoredAuditEvidence
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string AuditId { get; set; } = string.Empty;
    public string AuditPageId { get; set; } = string.Empty;
    public string CriterionId { get; set; } = string.Empty;
    public string FindingId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SelectedElementTag { get; set; } = string.Empty;
    public string SelectedElementText { get; set; } = string.Empty;
    public string CaptureMethod { get; set; } = string.Empty;
    public string? LegacyImagePath { get; set; }
    public byte[]? ImageData { get; set; }
    public string ImageMimeType { get; set; } = string.Empty;
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public string Context { get; set; } = string.Empty;
    public string DataHandling { get; set; } = string.Empty;
}

public sealed class StoredAuditPage
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public int? LegacySqlitePageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SamplingReason { get; set; } = string.Empty;
    public bool IsPriority { get; set; }
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastOpenedAtUtc { get; set; }
    public List<StoredAuditTestResult> TestResults { get; set; } = [];
    public List<StoredAuditSuggestion> Suggestions { get; set; } = [];
    public List<StoredAuditFinding> Findings { get; set; } = [];
}

public sealed class StoredAuditTestResult
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public int? LegacySqliteId { get; set; }
    public int? LegacySqliteTestId { get; set; }
    public string ReferenceTestId { get; set; } = string.Empty;
    public string Status { get; set; } = "À vérifier";
    public string Notes { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StoredAuditSuggestion
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public int? LegacySqliteId { get; set; }
    public int? LegacySqliteTestId { get; set; }
    public string ReferenceTestId { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
    public string AnalyzerVersion { get; set; } = string.Empty;
    public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
    public string RuleId { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = "À vérifier";
}

public sealed class StoredAuditFinding
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public int? LegacySqliteId { get; set; }
    public int? LegacySqliteTestId { get; set; }
    public string ReferenceTestId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Locator { get; set; } = string.Empty;
    public string Origin { get; set; } = "Observation d’audit";
    public string SourceRuleId { get; set; } = string.Empty;
    public bool RequiresAuditorReview { get; set; }
    public string Status { get; set; } = "À corriger";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<StoredFindingHistoryEvent> History { get; set; } = [];
    public List<StoredExternalIssueLink> ExternalIssueLinks { get; set; } = [];
}

public sealed class StoredFindingHistoryEvent
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public int? LegacySqliteId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StoredExternalIssueLink
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Provider { get; set; } = string.Empty;
    public string InstanceUrl { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string IssueId { get; set; } = string.Empty;
    public string IssueKey { get; set; } = string.Empty;
    public int? IssueIid { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FindingStatusAtSync { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StoredReferenceVersion
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string StableId { get; set; } = string.Empty;
    public int? LegacySqliteId { get; set; }
    public string FrameworkId { get; set; } = string.Empty;
    public string FrameworkName { get; set; } = string.Empty;
    public string VersionCode { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string ContentTypesJson { get; set; } = "[]";
    public string DataFormat { get; set; } = string.Empty;
    public int? ExpectedThemeCount { get; set; }
    public int? ExpectedCriterionCount { get; set; }
    public int? ExpectedTestCount { get; set; }
    public string LicenseName { get; set; } = string.Empty;
    public string LicenseUrl { get; set; } = string.Empty;
    public bool RequireMethodologies { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceRevision { get; set; } = string.Empty;
    public string WcagVersion { get; set; } = string.Empty;
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ImportedAtUtc { get; set; }
    public string CatalogStatus { get; set; } = "À importer";
    public string? CatalogChecksum { get; set; }
    public List<StoredCatalogTheme> Themes { get; set; } = [];
}

public sealed class StoredCatalogTheme
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public int? LegacySqliteId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<StoredCatalogCriterion> Criteria { get; set; } = [];
}

public sealed class StoredCatalogCriterion
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public int? LegacySqliteId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TechnicalNote { get; set; } = string.Empty;
    public string ParticularCases { get; set; } = string.Empty;
    public string ReferencesJson { get; set; } = "[]";
    public int Position { get; set; }
    public List<StoredCatalogTest> Tests { get; set; } = [];
}

public sealed class StoredCatalogTest
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public int? LegacySqliteId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public string Methodology { get; set; } = string.Empty;
    public int Position { get; set; }
}

internal sealed class DatabaseMigrationMarker
{
    [BsonId] public string Id { get; set; } = string.Empty;
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
    public int ImportedDocuments { get; set; }
}

internal sealed class StoredDecisionEvent
{
    [BsonId(false)] public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string AuditId { get; set; } = string.Empty;
    public int AuditPageScopeRevision { get; set; }
    public string EventType { get; set; } = "LegacyDecision";
    public string AuditPageId { get; set; } = string.Empty;
    public string CriterionId { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
    public string RuleSuggestion { get; set; } = string.Empty;
    public string HumanDecision { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public string Applicability { get; set; } = "À déterminer";
    public string ApplicabilitySnapshotJson { get; set; } = "[]";
    public bool ReviewComplete { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
