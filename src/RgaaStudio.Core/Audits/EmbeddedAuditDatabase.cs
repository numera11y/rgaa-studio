using LiteDB;

namespace RgaaStudio.Core.Audits;

public sealed class EmbeddedAuditDatabase : IDisposable
{
    private static readonly HashSet<string> AllowedDecisions = new(StringComparer.Ordinal)
    {
        "Conforme", "Non conforme", "Non applicable", "Non vérifiable"
    };

    private readonly LiteDatabase _database;
    private readonly ILiteCollection<StoredAuditDocument> _audits;
    private readonly ILiteCollection<StoredDecisionEvent> _decisions;
    private readonly ILiteCollection<StoredAuditEvidence> _evidence;
    private readonly ILiteCollection<StoredReferenceVersion> _references;
    private readonly ILiteCollection<DatabaseMigrationMarker> _migrationMarkers;

    public string DatabasePath { get; }

    public EmbeddedAuditDatabase(string? databasePath = null, bool importLegacyData = true)
    {
        DatabasePath = databasePath
            ?? Environment.GetEnvironmentVariable("RGAA_STUDIO_DATABASE_PATH")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Numera11y", "RGAAStudio", "rgaa-studio.litedb");

        var fullPath = Path.GetFullPath(DatabasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        DatabasePath = fullPath;
        _database = new LiteDatabase(DatabasePath);
        _audits = _database.GetCollection<StoredAuditDocument>("audits");
        _decisions = _database.GetCollection<StoredDecisionEvent>("audit_decisions");
        _evidence = _database.GetCollection<StoredAuditEvidence>("audit_evidence");
        _references = _database.GetCollection<StoredReferenceVersion>("reference_versions");
        _migrationMarkers = _database.GetCollection<DatabaseMigrationMarker>("database_migrations");

        _evidence.EnsureIndex(document => document.AuditId);

        _audits.EnsureIndex(document => document.LegacySourceKey);
        _audits.EnsureIndex(document => document.ReferenceStableId);
        _decisions.EnsureIndex(document => document.AuditId);
        _references.EnsureIndex(document => document.StableId, unique: true);

        if (importLegacyData)
            LegacyDataMigrator.ImportIfPresent(this);
    }

    public IReadOnlyList<AuditManifest> List(bool includeArchived = false) => _audits.FindAll()
        .Where(document => document.IsArchived == includeArchived)
        .OrderByDescending(document => document.UpdatedAtUtc)
        .Select(ToManifest)
        .ToArray();

    public AuditManifest? Get(string id)
    {
        if (!TryNormalizeGuid(id, out var normalized))
            return null;

        var document = _audits.FindById(normalized);
        return document is null ? null : ToManifest(document);
    }

    public IReadOnlyList<StoredAuditPage> GetPages(string auditId)
    {
        if (!TryNormalizeGuid(auditId, out var normalized) || _audits.FindById(normalized) is not { } audit)
            return [];
        return audit.Pages.OrderBy(page => page.SamplingReason, StringComparer.OrdinalIgnoreCase)
            .ThenBy(page => page.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public bool MarkPageOpened(string auditId, string pageId)
    {
        if (!TryGetAudit(auditId, out var audit) || audit.IsArchived || !TryNormalizeGuid(pageId, out var normalizedPageId) ||
            audit.Pages.FirstOrDefault(page => page.Id == normalizedPageId) is not { } page)
            return false;

        var now = DateTime.UtcNow;
        page.LastOpenedAtUtc = now;
        audit.UpdatedAtUtc = now;
        RunTransaction(() => _audits.Update(audit));
        return true;
    }

    public byte[]? GetFaviconData(string auditId)
    {
        if (!TryNormalizeGuid(auditId, out var normalized))
            return null;
        return _audits.FindById(normalized)?.FaviconData?.ToArray();
    }

    public IReadOnlyList<StoredAuditEvidence> GetEvidence(string auditId, string pageId)
    {
        if (!TryNormalizeGuid(auditId, out var normalizedAuditId) || !TryNormalizeGuid(pageId, out var normalizedPageId))
            return [];
        return _evidence.Find(item => item.AuditId == normalizedAuditId && item.AuditPageId == normalizedPageId)
            .OrderBy(item => item.CapturedAtUtc)
            .ToArray();
    }

    public int GetEvidenceCount(string auditId)
    {
        if (!TryNormalizeGuid(auditId, out var normalizedAuditId)) return 0;
        return _evidence.Find(item => item.AuditId == normalizedAuditId).Count();
    }

    public StoredAuditEvidence AddEvidence(string auditId, string pageId, string source, string selectedElementTag,
        string selectedElementText, string captureMethod, string context, byte[]? imageData = null, string imageMimeType = "")
    {
        if (!TryGetAudit(auditId, out var audit) || audit.IsArchived)
            throw new InvalidOperationException("L’audit est introuvable ou archivé.");
        if (!TryNormalizeGuid(pageId, out var normalizedPageId) || audit.Pages.FirstOrDefault(item => item.Id == normalizedPageId) is not { } page)
            throw new ArgumentException("La page sélectionnée ne figure pas dans cet audit.", nameof(pageId));
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Une preuve doit avoir une source et un contenu.");
        if (imageData is { Length: > 8_000_000 })
            throw new ArgumentException("La capture dépasse la limite de 8 Mo.", nameof(imageData));
        if (imageData is { Length: > 0 } && string.IsNullOrWhiteSpace(imageMimeType))
            throw new ArgumentException("Indiquez le format de l’image jointe.", nameof(imageMimeType));

        var evidence = new StoredAuditEvidence
        {
            Id = Guid.NewGuid().ToString("D"),
            AuditId = audit.Id,
            AuditPageId = page.Id,
            Source = source.Trim(),
            Page = page.Name,
            Url = page.Url,
            SelectedElementTag = selectedElementTag.Trim(),
            SelectedElementText = selectedElementText.Trim(),
            CaptureMethod = captureMethod.Trim(),
            Context = context,
            ImageData = imageData?.ToArray(),
            ImageMimeType = imageMimeType,
            DataHandling = "Preuve conservée localement dans le dossier d’audit ; vérifier et expurger toute donnée personnelle avant partage."
        };
        RunTransaction(() =>
        {
            _evidence.Insert(evidence);
            audit.UpdatedAtUtc = evidence.CapturedAtUtc;
            _audits.Update(audit);
        });
        return evidence;
    }

    public AuditManifest Create(NewAudit draft)
        => Create(draft, [], null);

    public AuditManifest Create(NewAudit draft, IReadOnlyCollection<SitePageCandidate> selectedPages, byte[]? faviconData)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(selectedPages);
        var siteUrl = ValidateSiteUrl(draft.SiteUrl);
        var (reference, level) = ResolveReference(draft.Reference, draft.Version, draft.Level);
        if (selectedPages.Count == 0)
            throw new ArgumentException("Sélectionnez au moins une page pour constituer le périmètre de l’audit.", nameof(selectedPages));
        var frameworkId = reference.FrameworkId;
        var frameworkName = reference.FrameworkName;
        var versionCode = reference.VersionCode;
        var now = DateTime.UtcNow;
        var host = new Uri(siteUrl).Host;
        var document = new StoredAuditDocument
        {
            Id = Guid.NewGuid().ToString("D"),
            Title = $"Audit de {host}",
            SiteUrl = siteUrl,
            ReferenceFrameworkId = frameworkId,
            ReferenceFrameworkName = frameworkName,
            ReferenceVersionId = reference.Id,
            ReferenceStableId = reference.StableId,
            ReferenceVersion = versionCode,
            TargetLevel = level,
            Status = "En préparation",
            FaviconFile = faviconData is { Length: > 0 } ? "embedded" : null,
            FaviconData = faviconData is { Length: > 0 } ? faviconData.ToArray() : null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var origin = new Uri(siteUrl);
        var pages = new List<StoredAuditPage>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var otherPageCount = 0;
        foreach (var candidate in selectedPages)
        {
            if (!Uri.TryCreate(candidate.Url, UriKind.Absolute, out var pageUri) || pageUri.Scheme != Uri.UriSchemeHttps ||
                !pageUri.IsDefaultPort || !pageUri.Host.Equals(origin.Host, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(pageUri.UserInfo))
                throw new ArgumentException("Les pages sélectionnées doivent appartenir au même site HTTPS.", nameof(selectedPages));
            var normalizedUrl = new UriBuilder(pageUri) { Fragment = string.Empty }.Uri.AbsoluteUri;
            if (!seenUrls.Add(normalizedUrl))
                continue;
            if (!candidate.IsPriority && ++otherPageCount > SiteDiscoveryService.MaximumOtherPages)
                throw new ArgumentException($"Vous pouvez ajouter au maximum {SiteDiscoveryService.MaximumOtherPages} autres pages.", nameof(selectedPages));

            pages.Add(new StoredAuditPage
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = candidate.Name,
                Url = normalizedUrl,
                SamplingReason = candidate.Category,
                IsPriority = candidate.IsPriority,
                AddedAtUtc = now
            });
        }
        document.Pages = pages;

        RunTransaction(() => _audits.Insert(document));
        return ToManifest(document);
    }

    public StoredAuditPage AddPageToAudit(string auditId, string name, string url)
    {
        var candidate = new SitePageCandidate(name, url, "Ajout manuel", false);
        if (AddPagesToAudit(auditId, [candidate]) == 0)
            throw new ArgumentException("Cette page figure déjà dans le périmètre de l’audit.", nameof(url));
        return GetPages(auditId).Single(page => string.Equals(page.Url, NormalizePageUrl(url), StringComparison.OrdinalIgnoreCase));
    }

    public int GetRemainingAdditionalPageCapacity(string auditId)
    {
        if (!TryGetAudit(auditId, out var audit))
            throw new ArgumentException("Le dossier d’audit n’existe pas.", nameof(auditId));
        return Math.Max(0, SiteDiscoveryService.MaximumOtherPages - audit.Pages.Count(page => !IsPriorityPage(page)));
    }

    public int AddPagesToAudit(string auditId, IReadOnlyCollection<SitePageCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!TryGetAudit(auditId, out var audit))
            throw new ArgumentException("Le dossier d’audit n’existe pas.", nameof(auditId));
        if (audit.IsArchived)
            throw new InvalidOperationException("Restaurez l’audit avant d’ajouter une page.");
        if (candidates.Count == 0)
            throw new ArgumentException("Sélectionnez au moins une nouvelle page.", nameof(candidates));

        var siteOrigin = new Uri(audit.SiteUrl);
        var knownUrls = new HashSet<string>(audit.Pages.Select(page => NormalizePageUrl(page.Url)), StringComparer.OrdinalIgnoreCase);
        var extraPageCount = audit.Pages.Count(page => !IsPriorityPage(page));
        var pagesToAdd = new List<StoredAuditPage>();
        foreach (var candidate in candidates)
        {
            if (!Uri.TryCreate(candidate.Url.Trim(), UriKind.Absolute, out var pageUri) || pageUri.Scheme != Uri.UriSchemeHttps ||
                !pageUri.IsDefaultPort || !string.IsNullOrEmpty(pageUri.UserInfo) ||
                !pageUri.Host.Equals(siteOrigin.Host, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Les pages sélectionnées doivent appartenir au même site HTTPS que l’audit.", nameof(candidates));

            var normalizedUrl = NormalizePageUrl(pageUri.AbsoluteUri);
            if (!knownUrls.Add(normalizedUrl))
                continue;
            if (!candidate.IsPriority && ++extraPageCount > SiteDiscoveryService.MaximumOtherPages)
                throw new ArgumentException($"La limite de {SiteDiscoveryService.MaximumOtherPages} pages supplémentaires est atteinte.", nameof(candidates));

            var normalizedName = candidate.Name.Trim();
            if (normalizedName.Length == 0)
                normalizedName = pageUri.AbsolutePath.Trim('/').Replace('/', ' ');
            if (normalizedName.Length == 0)
                normalizedName = pageUri.Host;

            pagesToAdd.Add(new StoredAuditPage
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = normalizedName,
                Url = normalizedUrl,
                SamplingReason = string.IsNullOrWhiteSpace(candidate.Category) ? "Ajout manuel" : candidate.Category,
                IsPriority = candidate.IsPriority,
                AddedAtUtc = DateTime.UtcNow
            });
        }

        if (pagesToAdd.Count == 0)
            return 0;

        audit.Pages.AddRange(pagesToAdd);
        audit.UpdatedAtUtc = DateTime.UtcNow;
        RunTransaction(() => _audits.Update(audit));
        return pagesToAdd.Count;
    }

    public bool RemovePageFromAudit(string auditId, string pageId)
    {
        if (!TryGetAudit(auditId, out var audit) || audit.IsArchived || !TryNormalizeGuid(pageId, out var normalizedPageId))
            return false;
        var removed = audit.Pages.RemoveAll(page => page.Id == normalizedPageId);
        if (removed == 0)
            return false;
        audit.UpdatedAtUtc = DateTime.UtcNow;
        RunTransaction(() => _audits.Update(audit));
        return true;
    }

    public bool AddSemanticFindingToAudit(
        string auditId,
        string pageId,
        string ruleId,
        string title,
        string description,
        string locator,
        string origin = "Analyse sémantique HTML5")
    {
        if (!TryGetAudit(auditId, out var audit))
            throw new ArgumentException("Le dossier d’audit n’existe pas.", nameof(auditId));
        if (audit.IsArchived)
            throw new InvalidOperationException("Restaurez l’audit avant d’ajouter un constat.");
        if (!TryNormalizeGuid(pageId, out var normalizedPageId) || audit.Pages.FirstOrDefault(page => page.Id == normalizedPageId) is not { } page)
            throw new ArgumentException("La page sélectionnée ne figure pas dans cet audit.", nameof(pageId));
        if (string.IsNullOrWhiteSpace(ruleId) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Le constat doit contenir une règle, un titre et une description.");
        if (page.Findings.Any(finding => finding.SourceRuleId == ruleId))
            return false;

        var now = DateTime.UtcNow;
        page.Findings.Add(new StoredAuditFinding
        {
            Id = Guid.NewGuid().ToString("D"),
            Title = title.Trim(),
            Description = description.Trim(),
            Locator = locator.Trim(),
            Origin = origin,
            SourceRuleId = ruleId.Trim(),
            RequiresAuditorReview = true,
            Status = "À corriger",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        audit.UpdatedAtUtc = now;
        RunTransaction(() => _audits.Update(audit));
        return true;
    }

    public void SaveAxeSuggestions(string auditId, string pageId, IReadOnlyCollection<StoredAuditSuggestion> suggestions)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        if (!TryGetAudit(auditId, out var audit) || audit.IsArchived)
            throw new InvalidOperationException("L’audit est introuvable ou archivé.");
        if (!TryNormalizeGuid(pageId, out var normalizedPageId) || audit.Pages.FirstOrDefault(item => item.Id == normalizedPageId) is not { } page)
            throw new ArgumentException("La page sélectionnée ne figure pas dans cet audit.", nameof(pageId));

        var old = page.Suggestions.Where(item => item.AnalyzerVersion.StartsWith("axe-core ", StringComparison.Ordinal)).ToDictionary(item => item.Id);
        foreach (var suggestion in suggestions)
        {
            if (string.IsNullOrWhiteSpace(suggestion.RuleId) || string.IsNullOrWhiteSpace(suggestion.Outcome))
                throw new ArgumentException("Chaque résultat axe doit avoir une règle et une catégorie.", nameof(suggestions));
            var prior = old.Values.FirstOrDefault(item => item.RuleId == suggestion.RuleId && item.Outcome == suggestion.Outcome);
            suggestion.Id = prior?.Id ?? Guid.NewGuid().ToString("D");
            suggestion.AnalyzedAtUtc = DateTime.UtcNow;
            suggestion.AnalyzerVersion = "axe-core " + suggestion.AnalyzerVersion.Replace("axe-core ", "", StringComparison.Ordinal);
            suggestion.ReviewStatus = prior?.ReviewStatus ?? "À vérifier";
        }
        page.Suggestions.RemoveAll(item => item.AnalyzerVersion.StartsWith("axe-core ", StringComparison.Ordinal));
        page.Suggestions.AddRange(suggestions);
        audit.UpdatedAtUtc = DateTime.UtcNow;
        RunTransaction(() => _audits.Update(audit));
    }

    public bool SetAxeSuggestionReview(string auditId, string pageId, string suggestionId, string reviewStatus)
    {
        if (reviewStatus is not ("À vérifier" or "Retenu" or "Écarté"))
            throw new ArgumentException("Choisissez À vérifier, Retenu ou Écarté.", nameof(reviewStatus));
        if (!TryGetAudit(auditId, out var audit) || audit.IsArchived || !TryNormalizeGuid(pageId, out var normalizedPageId))
            return false;
        var suggestion = audit.Pages.FirstOrDefault(item => item.Id == normalizedPageId)?.Suggestions.FirstOrDefault(item => item.Id == suggestionId);
        if (suggestion is null || !suggestion.AnalyzerVersion.StartsWith("axe-core ", StringComparison.Ordinal))
            return false;
        suggestion.ReviewStatus = reviewStatus;
        audit.UpdatedAtUtc = DateTime.UtcNow;
        RunTransaction(() => _audits.Update(audit));
        return true;
    }

    private static string NormalizePageUrl(string url) => new UriBuilder(url) { Fragment = string.Empty }.Uri.AbsoluteUri;

    public AuditManifest? Touch(string id)
    {
        if (!TryGetAudit(id, out var document))
            return null;

        document.UpdatedAtUtc = DateTime.UtcNow;
        _audits.Update(document);
        return ToManifest(document);
    }

    public bool SetArchived(string id, bool archived)
    {
        if (!TryGetAudit(id, out var document))
            return false;
        if (document.IsArchived == archived)
            return true;

        document.IsArchived = archived;
        document.ArchivedAtUtc = archived ? DateTime.UtcNow : null;
        document.UpdatedAtUtc = DateTime.UtcNow;
        _audits.Update(document);
        return true;
    }

    public bool DeleteAudit(string id)
    {
        if (!TryNormalizeGuid(id, out var normalized) || _audits.FindById(normalized) is null)
            return false;

        RunTransaction(() =>
        {
            _decisions.DeleteMany(document => document.AuditId == normalized);
            _evidence.DeleteMany(document => document.AuditId == normalized);
            _audits.Delete(normalized);
        });
        return true;
    }

    public IReadOnlyList<AuditDecisionEvent> ReadDecisions(string auditId)
    {
        if (!TryNormalizeGuid(auditId, out var normalized) || _audits.FindById(normalized) is null)
            return [];

        return _decisions.Find(document => document.AuditId == normalized)
            .OrderBy(document => document.UpdatedAtUtc)
            .Select(ToDecisionEvent)
            .ToArray();
    }

    public AuditDecisionEvent AppendDecision(string auditId, string page, string ruleSuggestion, string humanDecision, string? justification = null)
    {
        if (!TryGetAudit(auditId, out var audit))
            throw new ArgumentException("Le dossier d’audit n’existe pas.", nameof(auditId));
        if (string.IsNullOrWhiteSpace(page))
            throw new ArgumentException("Indiquez la page concernée.", nameof(page));
        if (string.IsNullOrWhiteSpace(ruleSuggestion))
            throw new ArgumentException("Indiquez le critère ou le contrôle concerné.", nameof(ruleSuggestion));
        if (!AllowedDecisions.Contains(humanDecision))
            throw new ArgumentException("Choisissez un résultat de revue.", nameof(humanDecision));

        var normalizedJustification = justification?.Trim() ?? string.Empty;
        if (humanDecision is "Non applicable" or "Non vérifiable" && normalizedJustification.Length == 0)
            throw new ArgumentException("Une justification est obligatoire pour ce résultat.", nameof(justification));

        var stored = new StoredDecisionEvent
        {
            Id = Guid.NewGuid().ToString("D"),
            AuditId = audit.Id,
            Page = page.Trim(),
            RuleSuggestion = ruleSuggestion.Trim(),
            HumanDecision = humanDecision,
            Justification = normalizedJustification,
            UpdatedAtUtc = DateTime.UtcNow
        };

        RunTransaction(() =>
        {
            _decisions.Insert(stored);
            audit.UpdatedAtUtc = stored.UpdatedAtUtc;
            _audits.Update(audit);
        });
        return ToDecisionEvent(stored);
    }

    public AuditDecisionEvent CompleteLatestReview(string auditId)
    {
        if (!TryGetAudit(auditId, out var audit))
            throw new ArgumentException("Le dossier d’audit n’existe pas.", nameof(auditId));

        var latest = _decisions.Find(document => document.AuditId == audit.Id)
            .OrderBy(document => document.UpdatedAtUtc)
            .LastOrDefault();
        if (latest is null)
            throw new InvalidOperationException("Enregistrez une décision avant de terminer la revue.");
        if (latest.ReviewComplete)
            throw new InvalidOperationException("La revue est déjà marquée comme terminée.");

        var completed = new StoredDecisionEvent
        {
            Id = Guid.NewGuid().ToString("D"),
            AuditId = latest.AuditId,
            Page = latest.Page,
            RuleSuggestion = latest.RuleSuggestion,
            HumanDecision = latest.HumanDecision,
            Justification = latest.Justification,
            ReviewComplete = true,
            UpdatedAtUtc = DateTime.UtcNow
        };
        RunTransaction(() =>
        {
            _decisions.Insert(completed);
            audit.UpdatedAtUtc = completed.UpdatedAtUtc;
            _audits.Update(audit);
        });
        return ToDecisionEvent(completed);
    }

    public IReadOnlyList<StoredReferenceVersion> GetReferenceVersions() => _references.FindAll()
        .OrderBy(document => document.FrameworkName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(document => document.VersionCode, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public string StoreReferenceVersion(StoredReferenceVersion document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (string.IsNullOrWhiteSpace(document.FrameworkId) || string.IsNullOrWhiteSpace(document.VersionCode))
            throw new ArgumentException("Le référentiel doit avoir un cadre et une version.", nameof(document));
        if (string.IsNullOrWhiteSpace(document.StableId))
            document.StableId = $"{document.FrameworkId.ToLowerInvariant()}:{document.VersionCode}";
        if (!IsGuidD(document.Id))
            document.Id = Guid.NewGuid().ToString("D");
        return ImportReference(document);
    }

    internal ILiteCollection<StoredAuditDocument> Audits => _audits;
    internal ILiteCollection<StoredDecisionEvent> Decisions => _decisions;
    internal ILiteCollection<StoredAuditEvidence> Evidence => _evidence;
    internal ILiteCollection<StoredReferenceVersion> References => _references;
    internal ILiteCollection<DatabaseMigrationMarker> MigrationMarkers => _migrationMarkers;
    internal LiteDatabase Database => _database;
    internal void ExecuteImportTransaction(Action action) => RunTransaction(action);

    internal bool HasMigrationMarker(string id) => _migrationMarkers.FindById(id) is not null;

    internal void MarkMigrationComplete(string id, int importedDocuments) =>
        _migrationMarkers.Upsert(new DatabaseMigrationMarker
        {
            Id = id,
            CompletedAtUtc = DateTime.UtcNow,
            ImportedDocuments = importedDocuments
        });

    internal string ImportReference(StoredReferenceVersion document)
    {
        var existing = _references.FindOne(version => version.StableId == document.StableId);
        if (existing is not null)
        {
            document.Id = existing.Id;
            _references.Update(document);
            return existing.Id;
        }

        if (!IsGuidD(document.Id))
            document.Id = Guid.NewGuid().ToString("D");
        _references.Insert(document);
        return document.Id;
    }

    internal string ResolveReferenceId(string stableId) =>
        _references.FindOne(version => version.StableId == stableId)?.Id ?? string.Empty;

    internal bool ImportAudit(StoredAuditDocument document)
    {
        if (!IsGuidD(document.Id))
            document.Id = Guid.NewGuid().ToString("D");
        if (!string.IsNullOrWhiteSpace(document.LegacySourceKey) &&
            _audits.FindOne(audit => audit.LegacySourceKey == document.LegacySourceKey) is not null)
            return false;
        if (_audits.FindById(document.Id) is not null)
            return false;
        _audits.Insert(document);
        return true;
    }

    internal bool ImportDecision(StoredDecisionEvent document)
    {
        if (!IsGuidD(document.Id))
            document.Id = Guid.NewGuid().ToString("D");
        if (_decisions.FindById(document.Id) is not null)
            return false;
        _decisions.Insert(document);
        return true;
    }

    internal bool ImportEvidence(StoredAuditEvidence document)
    {
        if (!IsGuidD(document.Id))
            document.Id = Guid.NewGuid().ToString("D");
        if (_evidence.FindById(document.Id) is not null)
            return false;
        _evidence.Insert(document);
        return true;
    }

    private bool TryGetAudit(string id, out StoredAuditDocument document)
    {
        if (TryNormalizeGuid(id, out var normalized) && _audits.FindById(normalized) is { } found)
        {
            document = found;
            return true;
        }

        document = null!;
        return false;
    }

    private void RunTransaction(Action action)
    {
        if (!_database.BeginTrans())
            throw new InvalidOperationException("Une transaction LiteDB est déjà active.");

        try
        {
            action();
            _database.Commit();
        }
        catch
        {
            _database.Rollback();
            throw;
        }
    }

    private static bool TryNormalizeGuid(string id, out string normalized)
    {
        if (Guid.TryParseExact(id, "D", out var guid))
        {
            normalized = guid.ToString("D");
            return true;
        }

        normalized = string.Empty;
        return false;
    }

    private static bool IsGuidD(string id) => Guid.TryParseExact(id, "D", out _);

    private static bool IsPriorityPage(StoredAuditPage page) => page.IsPriority ||
        page.SamplingReason is "Accueil" or "Contact" or "Mentions légales" or "Déclaration d’accessibilité" or "Plan du site";

    private static AuditManifest ToManifest(StoredAuditDocument document) => new(
        document.Id,
        document.SiteUrl,
        document.ReferenceFrameworkName,
        $"{document.ReferenceFrameworkName} {document.ReferenceVersion}",
        document.TargetLevel,
        document.Title,
        new DateTimeOffset(DateTime.SpecifyKind(document.UpdatedAtUtc, DateTimeKind.Utc)),
        document.FaviconFile,
        document.Organization,
        document.Status,
        document.ReferenceVersionId,
        new DateTimeOffset(DateTime.SpecifyKind(document.CreatedAtUtc, DateTimeKind.Utc)),
        document.IsArchived,
        document.ArchivedAtUtc is { } archivedAt
            ? new DateTimeOffset(DateTime.SpecifyKind(archivedAt, DateTimeKind.Utc))
            : null);

    private static AuditDecisionEvent ToDecisionEvent(StoredDecisionEvent document) => new(
        document.Id,
        document.AuditId,
        document.Page,
        document.RuleSuggestion,
        document.HumanDecision,
        document.Justification,
        document.ReviewComplete,
        new DateTimeOffset(DateTime.SpecifyKind(document.UpdatedAtUtc, DateTimeKind.Utc)));

    private static string ValidateSiteUrl(string value)
    {
        var normalized = value.Trim();
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(uri.Host))
            throw new ArgumentException("Saisissez une URL HTTPS valide, sans identifiants ni port personnalisé.", nameof(value));

        return uri.AbsoluteUri;
    }

    private (StoredReferenceVersion Reference, string Level) ResolveReference(string reference, string version, string? level)
    {
        var normalizedReference = reference.Trim();
        var normalizedVersion = version.Trim();
        var normalizedLevel = level?.Trim() ?? string.Empty;
        var framework = _references.FindAll().FirstOrDefault(item =>
            string.Equals(item.FrameworkName, normalizedReference, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.FrameworkId, normalizedReference, StringComparison.OrdinalIgnoreCase));
        if (framework is null)
            throw new ArgumentException("Choisissez un référentiel disponible dans la base locale.", nameof(reference));

        var versionCode = normalizedVersion.StartsWith(framework.FrameworkName + " ", StringComparison.OrdinalIgnoreCase)
            ? normalizedVersion[(framework.FrameworkName.Length + 1)..]
            : normalizedVersion;
        var selectedVersion = _references.FindOne(item =>
            item.FrameworkId == framework.FrameworkId && item.VersionCode == versionCode);
        if (selectedVersion is null)
            throw new ArgumentException("Choisissez une version installée du référentiel.", nameof(version));

        if (string.Equals(selectedVersion.FrameworkId, "wcag", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectedVersion.FrameworkName, "WCAG", StringComparison.OrdinalIgnoreCase))
        {
            if (normalizedLevel is not ("A" or "AA" or "AAA"))
                throw new ArgumentException("Choisissez explicitement une cible A, AA ou AAA.", nameof(level));
        }
        else if (normalizedLevel.Length > 0)
        {
            throw new ArgumentException("Aucun niveau cible n’est défini pour ce référentiel.", nameof(level));
        }

        return (selectedVersion, normalizedLevel);
    }

    public void Dispose() => _database.Dispose();
}
