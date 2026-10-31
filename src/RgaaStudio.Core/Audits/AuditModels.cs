namespace RgaaStudio.Core.Audits;

public sealed record AuditManifest(
    string Id,
    string SiteUrl,
    string Reference,
    string Version,
    string Level,
    string Title,
    DateTimeOffset UpdatedAt,
    string? FaviconFile = null,
    string Organization = "",
    string Status = "En préparation",
    string ReferenceVersionId = "",
    DateTimeOffset CreatedAt = default,
    bool IsArchived = false,
    DateTimeOffset? ArchivedAt = null,
    int PageScopeRevision = 0);

public sealed record NewAudit(string SiteUrl, string Reference, string Version, string? Level);

public sealed record AuditDecisionEvent(
    string Id,
    string AuditId,
    string Page,
    string RuleSuggestion,
    string HumanDecision,
    string Justification,
    bool ReviewComplete,
    DateTimeOffset UpdatedAt,
    string AuditPageId = "",
    string CriterionId = "",
    string Applicability = "À déterminer",
    string EventType = "LegacyDecision",
    int AuditPageScopeRevision = 0,
    string ApplicabilitySnapshotJson = "[]");

public sealed record AuditCriterionTest(string Id, string Code, string Question, string Requirements, string Methodology);

public sealed record AuditCriterion(string Id, string Code, string Title, string ThemeNumber, string ThemeTitle, int TestCount,
    string Level, string TechnicalNote, string ParticularCases, IReadOnlyList<AuditCriterionTest> Tests)
{
    public string Label => $"{Code} — {Title}";
}

public sealed record AuditReviewProgress(bool IsAvailable, int CompletedCriteria, int TotalCriteria,
    int PageApplicabilitiesRecorded, int TotalPageApplicabilities)
{
    public int PendingCriteria => Math.Max(0, TotalCriteria - CompletedCriteria);
    public double Percentage => !IsAvailable || TotalCriteria == 0 ? 0 : CompletedCriteria * 100d / TotalCriteria;
}
