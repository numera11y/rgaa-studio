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
    DateTimeOffset? ArchivedAt = null);

public sealed record NewAudit(string SiteUrl, string Reference, string Version, string? Level);

public sealed record AuditDecisionEvent(
    string Id,
    string AuditId,
    string Page,
    string RuleSuggestion,
    string HumanDecision,
    string Justification,
    bool ReviewComplete,
    DateTimeOffset UpdatedAt);
