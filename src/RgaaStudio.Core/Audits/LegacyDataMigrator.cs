using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace RgaaStudio.Core.Audits;

internal static class LegacyDataMigrator
{
    private const string JsonMigrationId = "legacy-json-dossiers-v1";
    private const string SqliteMigrationId = "legacy-sqlite-v10-v1";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal static void ImportIfPresent(EmbeddedAuditDatabase database)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("RGAA_STUDIO_SKIP_LEGACY_IMPORT"), "1", StringComparison.Ordinal))
            return;

        var jsonRoot = Environment.GetEnvironmentVariable("RGAA_STUDIO_AUDIT_ROOT") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RgaaStudioSpike", "AuditDossier");
        if (!database.HasMigrationMarker(JsonMigrationId) && Directory.Exists(jsonRoot))
            ImportJsonDossiers(database, jsonRoot);

        var sqlitePath = Environment.GetEnvironmentVariable("RGAA_STUDIO_LEGACY_SQLITE_PATH") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Numera11y", "RGAAStudio", "rgaa-studio.db");
        if (!database.HasMigrationMarker(SqliteMigrationId) && File.Exists(sqlitePath))
            ImportSqliteDatabase(database, sqlitePath);
    }

    private static void ImportJsonDossiers(EmbeddedAuditDatabase database, string root)
    {
        var audits = new List<StoredAuditDocument>();
        var decisions = new List<StoredDecisionEvent>();
        var evidence = new List<StoredAuditEvidence>();

        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var legacyId = Path.GetFileName(directory);
            if (!Guid.TryParse(legacyId, out var auditGuid))
                continue;

            AuditManifest? manifest;
            try { manifest = JsonSerializer.Deserialize<AuditManifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")), JsonOptions); }
            catch (IOException) { continue; }
            catch (JsonException) { continue; }
            if (manifest is null || !Guid.TryParse(manifest.Id, out var manifestGuid) || manifestGuid != auditGuid)
                continue;

            var auditId = auditGuid.ToString("D");
            var frameworkId = manifest.Reference.Equals("WCAG", StringComparison.OrdinalIgnoreCase) ? "wcag" : "rgaa";
            var frameworkName = frameworkId == "wcag" ? "WCAG" : "RGAA";
            var versionCode = StripFrameworkPrefix(manifest.Version, frameworkName);
            var stableId = $"{frameworkId}:{versionCode}";
            var reference = database.References.FindOne(item => item.StableId == stableId);
            audits.Add(new StoredAuditDocument
            {
                Id = auditId,
                LegacySourceKey = $"json-dossier:{auditId}",
                LegacyJsonDossierId = legacyId,
                Title = manifest.Title,
                Organization = manifest.Organization,
                SiteUrl = manifest.SiteUrl,
                ReferenceFrameworkId = frameworkId,
                ReferenceFrameworkName = frameworkName,
                ReferenceVersionId = reference?.Id ?? string.Empty,
                ReferenceStableId = stableId,
                ReferenceVersion = versionCode,
                TargetLevel = manifest.Level,
                Status = manifest.Status,
                FaviconFile = manifest.FaviconFile,
                CreatedAtUtc = manifest.CreatedAt == default ? manifest.UpdatedAt.UtcDateTime : manifest.CreatedAt.UtcDateTime,
                UpdatedAtUtc = manifest.UpdatedAt.UtcDateTime
            });

            ReadJsonDecisions(directory, auditId, decisions);
            ReadJsonEvidence(directory, auditId, evidence);
        }

        database.ExecuteImportTransaction(() =>
        {
            foreach (var audit in audits)
                database.ImportAudit(audit);
            foreach (var decision in decisions)
                database.ImportDecision(decision);
            foreach (var item in evidence)
                database.ImportEvidence(item);
            database.MarkMigrationComplete(JsonMigrationId, audits.Count);
        });
    }

    private static void ReadJsonDecisions(string directory, string auditId, ICollection<StoredDecisionEvent> target)
    {
        var decisionDirectory = Path.Combine(directory, "decisions");
        if (!Directory.Exists(decisionDirectory))
            return;

        foreach (var path in Directory.EnumerateFiles(decisionDirectory, "*.json"))
        {
            try
            {
                var record = JsonSerializer.Deserialize<AuditDecisionEvent>(File.ReadAllText(path), JsonOptions);
                if (record is null || !Guid.TryParse(record.Id, out var eventId))
                    continue;

                target.Add(new StoredDecisionEvent
                {
                    Id = eventId.ToString("D"),
                    AuditId = auditId,
                    Page = record.Page,
                    RuleSuggestion = record.RuleSuggestion,
                    HumanDecision = record.HumanDecision,
                    Justification = record.Justification,
                    ReviewComplete = record.ReviewComplete,
                    UpdatedAtUtc = record.UpdatedAt.UtcDateTime
                });
            }
            catch (IOException) { }
            catch (JsonException) { }
        }
    }

    private static void ReadJsonEvidence(string directory, string auditId, ICollection<StoredAuditEvidence> target)
    {
        foreach (var path in Directory.EnumerateFiles(directory, "synthetique-*.json"))
        {
            try
            {
                var record = JsonSerializer.Deserialize<LegacyJsonProof>(File.ReadAllText(path), JsonOptions);
                if (record is null || !Guid.TryParse(record.Id, out var evidenceId))
                    continue;

                var imageName = Path.GetFileName(record.ScreenshotFile);
                var imagePath = string.Equals(imageName, record.ScreenshotFile, StringComparison.Ordinal) &&
                                File.Exists(Path.Combine(directory, imageName))
                    ? Path.GetFullPath(Path.Combine(directory, imageName))
                    : null;
                target.Add(new StoredAuditEvidence
                {
                    Id = evidenceId.ToString("D"),
                    AuditId = auditId,
                    Source = record.Fixture,
                    Page = record.Page,
                    Url = record.Url,
                    SelectedElementTag = record.SelectedElementTag,
                    SelectedElementText = record.SelectedElementText,
                    CaptureMethod = record.CaptureMethod,
                    LegacyImagePath = imagePath,
                    CapturedAtUtc = record.CapturedAt.UtcDateTime,
                    Context = record.Context,
                    DataHandling = record.DataHandling
                });
            }
            catch (IOException) { }
            catch (JsonException) { }
        }
    }

    private static void ImportSqliteDatabase(EmbeddedAuditDatabase database, string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(path),
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        connection.Open();

        if (!TableExists(connection, "schema_migrations") || ScalarInt(connection, "SELECT COALESCE(MAX(version), 0) FROM schema_migrations;") < 10)
            throw new InvalidDataException("La base SQLite historique n’a pas le schéma versionné attendu ; elle reste intacte et n’a pas été migrée.");

        var referenceIds = new Dictionary<long, StoredReferenceVersion>();
        var testIds = new Dictionary<long, string>();
        var references = ReadReferences(connection, referenceIds, testIds);
        var audits = ReadMissions(connection, referenceIds, testIds);
        ValidateReadCounts(connection, references, audits);

        database.ExecuteImportTransaction(() =>
        {
            foreach (var reference in references)
                database.ImportReference(reference);

            foreach (var audit in audits)
            {
                audit.ReferenceVersionId = database.ResolveReferenceId(audit.ReferenceStableId);
                database.ImportAudit(audit);
            }

            database.MarkMigrationComplete(SqliteMigrationId, references.Count + audits.Count);
        });
    }

    private static List<StoredReferenceVersion> ReadReferences(
        SqliteConnection connection,
        IDictionary<long, StoredReferenceVersion> referenceIds,
        IDictionary<long, string> testIds)
    {
        var versions = new List<SqliteReferenceVersion>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, framework_id, framework_name, version_code, jurisdiction, language, content_types_json,
                       data_format, expected_theme_count, expected_criterion_count, expected_test_count,
                       license_name, license_url, require_methodologies, source_url, source_revision, wcag_version,
                       published_at, imported_at, catalog_status, catalog_checksum
                FROM reference_versions ORDER BY framework_id, version_code;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                versions.Add(new SqliteReferenceVersion(
                    reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
                    ReadNullableInt(reader, 8), ReadNullableInt(reader, 9), ReadNullableInt(reader, 10),
                    reader.GetString(11), reader.GetString(12), reader.GetInt32(13) == 1,
                    reader.GetString(14), reader.GetString(15), reader.GetString(16),
                    ReadNullableDate(reader, 17), ReadNullableDate(reader, 18), reader.GetString(19),
                    reader.IsDBNull(20) ? null : reader.GetString(20)));
            }
        }

        var documents = new List<StoredReferenceVersion>();
        foreach (var version in versions)
        {
            var doc = new StoredReferenceVersion
            {
                Id = Guid.NewGuid().ToString("D"),
                LegacySqliteId = checked((int)version.Id),
                StableId = $"{version.FrameworkId}:{version.VersionCode}",
                FrameworkId = version.FrameworkId,
                FrameworkName = version.FrameworkName,
                VersionCode = version.VersionCode,
                Jurisdiction = version.Jurisdiction,
                Language = version.Language,
                ContentTypesJson = version.ContentTypesJson,
                DataFormat = version.DataFormat,
                ExpectedThemeCount = version.ExpectedThemeCount,
                ExpectedCriterionCount = version.ExpectedCriterionCount,
                ExpectedTestCount = version.ExpectedTestCount,
                LicenseName = version.LicenseName,
                LicenseUrl = version.LicenseUrl,
                RequireMethodologies = version.RequireMethodologies,
                SourceUrl = version.SourceUrl,
                SourceRevision = version.SourceRevision,
                WcagVersion = version.WcagVersion,
                PublishedAtUtc = version.PublishedAtUtc,
                ImportedAtUtc = version.ImportedAtUtc,
                CatalogStatus = version.CatalogStatus,
                CatalogChecksum = version.CatalogChecksum
            };

            var themeIds = ReadThemes(connection, version.Id, doc);
            var criterionIds = ReadCriteria(connection, version.Id, themeIds, doc);
            ReadTests(connection, version.Id, criterionIds, testIds, doc);
            referenceIds.Add(version.Id, doc);
            documents.Add(doc);
        }
        return documents;
    }

    private static Dictionary<long, StoredCatalogTheme> ReadThemes(SqliteConnection connection, long versionId, StoredReferenceVersion target)
    {
        var map = new Dictionary<long, StoredCatalogTheme>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, theme_number, title, position FROM rgaa_themes WHERE reference_version_id=$id ORDER BY position;";
        command.Parameters.AddWithValue("$id", versionId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt64(0);
            var theme = new StoredCatalogTheme
            {
                Id = Guid.NewGuid().ToString("D"),
                LegacySqliteId = checked((int)id),
                Number = reader.GetString(1),
                Title = reader.GetString(2),
                Position = reader.GetInt32(3)
            };
            target.Themes.Add(theme);
            map.Add(id, theme);
        }
        return map;
    }

    private static Dictionary<long, StoredCatalogCriterion> ReadCriteria(SqliteConnection connection, long versionId, IDictionary<long, StoredCatalogTheme> themes, StoredReferenceVersion target)
    {
        var map = new Dictionary<long, StoredCatalogCriterion>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, theme_id, criterion_code, title, technical_note, particular_cases, references_json, position FROM rgaa_criteria WHERE reference_version_id=$id ORDER BY position;";
        command.Parameters.AddWithValue("$id", versionId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt64(0);
            var themeId = reader.GetInt64(1);
            if (!themes.TryGetValue(themeId, out var theme))
                throw new InvalidDataException($"Le catalogue de référence {versionId} contient un critère sans thématique.");
            var criterion = new StoredCatalogCriterion
            {
                Id = Guid.NewGuid().ToString("D"),
                LegacySqliteId = checked((int)id),
                Code = reader.GetString(2),
                Title = reader.GetString(3),
                TechnicalNote = reader.GetString(4),
                ParticularCases = reader.GetString(5),
                ReferencesJson = reader.GetString(6),
                Position = reader.GetInt32(7)
            };
            theme.Criteria.Add(criterion);
            map.Add(id, criterion);
        }
        return map;
    }

    private static void ReadTests(SqliteConnection connection, long versionId, IDictionary<long, StoredCatalogCriterion> criteria, IDictionary<long, string> testIds, StoredReferenceVersion target)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, criterion_id, test_code, question, requirements, methodology, position FROM rgaa_tests WHERE reference_version_id=$id ORDER BY position;";
        command.Parameters.AddWithValue("$id", versionId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var oldId = reader.GetInt64(0);
            var criterionId = reader.GetInt64(1);
            if (!criteria.TryGetValue(criterionId, out var criterion))
                throw new InvalidDataException($"Le catalogue de référence {versionId} contient un test sans critère.");
            var test = new StoredCatalogTest
            {
                Id = Guid.NewGuid().ToString("D"),
                LegacySqliteId = checked((int)oldId),
                Code = reader.GetString(2),
                Question = reader.GetString(3),
                Requirements = reader.GetString(4),
                Methodology = reader.GetString(5),
                Position = reader.GetInt32(6)
            };
            criterion.Tests.Add(test);
            testIds[oldId] = test.Id;
        }
    }

    private static List<StoredAuditDocument> ReadMissions(SqliteConnection connection, IReadOnlyDictionary<long, StoredReferenceVersion> references, IReadOnlyDictionary<long, string> testIds)
    {
        var missions = new List<SqliteMission>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, name, organization, website_url, rgaa_version, reference_version_id, created_at, status FROM missions ORDER BY created_at, id;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                missions.Add(new SqliteMission(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetInt64(5), ReadDate(reader.GetString(6)), reader.GetString(7)));
        }

        var documents = new List<StoredAuditDocument>();
        foreach (var mission in missions)
        {
            var version = mission.ReferenceVersionId is long versionId && references.TryGetValue(versionId, out var foundVersion)
                ? foundVersion
                : null;
            var frameworkId = version?.FrameworkId ?? "rgaa";
            var frameworkName = version?.FrameworkName ?? "RGAA";
            var versionCode = version?.VersionCode ?? StripFrameworkPrefix(mission.VersionCode, frameworkName);
            var audit = new StoredAuditDocument
            {
                Id = Guid.NewGuid().ToString("D"),
                LegacySourceKey = $"sqlite-mission:{mission.Id}",
                LegacySqliteMissionId = checked((int)mission.Id),
                Title = mission.Name,
                Organization = mission.Organization,
                SiteUrl = mission.WebsiteUrl,
                ReferenceFrameworkId = frameworkId,
                ReferenceFrameworkName = frameworkName,
                ReferenceVersionId = version?.Id ?? string.Empty,
                ReferenceStableId = version?.StableId ?? $"{frameworkId}:{versionCode}",
                ReferenceVersion = versionCode,
                Status = mission.Status,
                CreatedAtUtc = mission.CreatedAtUtc,
                UpdatedAtUtc = mission.CreatedAtUtc
            };

            var pages = ReadPages(connection, mission.Id);
            var pageMap = new Dictionary<long, StoredAuditPage>();
            foreach (var pageRow in pages)
            {
                var page = new StoredAuditPage
                {
                    Id = Guid.NewGuid().ToString("D"),
                    LegacySqlitePageId = checked((int)pageRow.Id),
                    Name = pageRow.Name,
                    Url = pageRow.Url,
                    SamplingReason = pageRow.SamplingReason,
                    AddedAtUtc = pageRow.AddedAtUtc
                };
                audit.Pages.Add(page);
                pageMap.Add(pageRow.Id, page);
            }

            ReadTestResults(connection, mission.Id, pageMap, testIds);
            ReadSuggestions(connection, mission.Id, pageMap, testIds);
            ReadFindings(connection, mission.Id, pageMap, testIds);
            documents.Add(audit);
        }
        return documents;
    }

    private static List<SqlitePage> ReadPages(SqliteConnection connection, long missionId)
    {
        var pages = new List<SqlitePage>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, url, sampling_reason, added_at FROM audit_pages WHERE mission_id=$id ORDER BY added_at, id;";
        command.Parameters.AddWithValue("$id", missionId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
            pages.Add(new SqlitePage(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), ReadDate(reader.GetString(4))));
        return pages;
    }

    private static void ReadTestResults(SqliteConnection connection, long missionId, IReadOnlyDictionary<long, StoredAuditPage> pages, IReadOnlyDictionary<long, string> testIds)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, audit_page_id, rgaa_test_id, status, notes, evidence_json, updated_at FROM audit_test_results WHERE mission_id=$id ORDER BY updated_at, id;";
        command.Parameters.AddWithValue("$id", missionId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var pageId = reader.GetInt64(1);
            if (!pages.TryGetValue(pageId, out var page))
                continue;
            var oldTestId = reader.GetInt64(2);
            page.TestResults.Add(new StoredAuditTestResult
            {
                Id = Guid.NewGuid().ToString("D"),
                LegacySqliteId = checked((int)reader.GetInt64(0)),
                LegacySqliteTestId = checked((int)oldTestId),
                ReferenceTestId = testIds.GetValueOrDefault(oldTestId, string.Empty),
                Status = reader.GetString(3),
                Notes = reader.GetString(4),
                EvidenceJson = reader.GetString(5),
                UpdatedAtUtc = ReadDate(reader.GetString(6))
            });
        }
    }

    private static void ReadSuggestions(SqliteConnection connection, long missionId, IReadOnlyDictionary<long, StoredAuditPage> pages, IReadOnlyDictionary<long, string> testIds)
    {
        if (!TableExists(connection, "audit_suggestions"))
            return;

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, audit_page_id, rgaa_test_id, outcome, summary, evidence_json, analyzer_version, analyzed_at FROM audit_suggestions WHERE mission_id=$id ORDER BY analyzed_at, id;";
        command.Parameters.AddWithValue("$id", missionId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var pageId = reader.GetInt64(1);
            if (!pages.TryGetValue(pageId, out var page))
                continue;
            var oldTestId = reader.GetInt64(2);
            page.Suggestions.Add(new StoredAuditSuggestion
            {
                Id = Guid.NewGuid().ToString("D"),
                LegacySqliteId = checked((int)reader.GetInt64(0)),
                LegacySqliteTestId = checked((int)oldTestId),
                ReferenceTestId = testIds.GetValueOrDefault(oldTestId, string.Empty),
                Outcome = reader.GetString(3),
                Summary = reader.GetString(4),
                EvidenceJson = reader.GetString(5),
                AnalyzerVersion = reader.GetString(6),
                AnalyzedAtUtc = ReadDate(reader.GetString(7))
            });
        }
    }

    private static void ReadFindings(SqliteConnection connection, long missionId, IReadOnlyDictionary<long, StoredAuditPage> pages, IReadOnlyDictionary<long, string> testIds)
    {
        if (!TableExists(connection, "audit_findings"))
            return;

        var findings = new List<SqliteFinding>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, audit_page_id, rgaa_test_id, title, description, locator, status, created_at, updated_at FROM audit_findings WHERE mission_id=$id ORDER BY created_at, id;";
            command.Parameters.AddWithValue("$id", missionId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                findings.Add(new SqliteFinding(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), ReadDate(reader.GetString(7)), ReadDate(reader.GetString(8))));
        }

        foreach (var source in findings)
        {
            if (!pages.TryGetValue(source.PageId, out var page))
                continue;
            var oldFindingId = source.Id;
            var finding = new StoredAuditFinding
            {
                Id = Guid.NewGuid().ToString("D"),
                LegacySqliteId = checked((int)oldFindingId),
                LegacySqliteTestId = checked((int)source.TestId),
                ReferenceTestId = testIds.GetValueOrDefault(source.TestId, string.Empty),
                Title = source.Title,
                Description = source.Description,
                Locator = source.Locator,
                Status = source.Status,
                CreatedAtUtc = source.CreatedAtUtc,
                UpdatedAtUtc = source.UpdatedAtUtc
            };
            ReadFindingHistory(connection, oldFindingId, finding);
            ReadIssueLinks(connection, oldFindingId, finding);
            page.Findings.Add(finding);
        }
    }

    private static void ReadFindingHistory(SqliteConnection connection, long findingId, StoredAuditFinding target)
    {
        if (!TableExists(connection, "audit_finding_history"))
            return;

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, status, comment, recorded_at FROM audit_finding_history WHERE audit_finding_id=$id ORDER BY recorded_at, id;";
        command.Parameters.AddWithValue("$id", findingId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            target.History.Add(new StoredFindingHistoryEvent
            {
                Id = Guid.NewGuid().ToString("D"),
                LegacySqliteId = checked((int)reader.GetInt64(0)),
                Status = reader.GetString(1),
                Comment = reader.GetString(2),
                RecordedAtUtc = ReadDate(reader.GetString(3))
            });
        }
    }

    private static void ReadIssueLinks(SqliteConnection connection, long findingId, StoredAuditFinding target)
    {
        if (!TableExists(connection, "external_issue_links"))
            return;

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT provider, instance_url, project, issue_id, issue_key, issue_iid, url, status, created_at, updated_at, finding_status_at_sync FROM external_issue_links WHERE finding_id=$id ORDER BY provider, instance_url, project;";
        command.Parameters.AddWithValue("$id", findingId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            target.ExternalIssueLinks.Add(new StoredExternalIssueLink
            {
                Id = Guid.NewGuid().ToString("D"),
                Provider = reader.GetString(0),
                InstanceUrl = reader.GetString(1),
                Project = reader.GetString(2),
                IssueId = reader.GetString(3),
                IssueKey = reader.GetString(4),
                IssueIid = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Url = reader.GetString(6),
                Status = reader.GetString(7),
                CreatedAtUtc = ReadDate(reader.GetString(8)),
                UpdatedAtUtc = ReadDate(reader.GetString(9)),
                FindingStatusAtSync = reader.GetString(10)
            });
        }
    }

    private static bool TableExists(SqliteConnection connection, string name)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
        command.Parameters.AddWithValue("$name", name);
        return command.ExecuteScalar() is not null;
    }

    private static int ScalarInt(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void ValidateReadCounts(SqliteConnection connection, IReadOnlyCollection<StoredReferenceVersion> references, IReadOnlyCollection<StoredAuditDocument> audits)
    {
        var expected = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["reference_versions"] = references.Count,
            ["rgaa_themes"] = references.Sum(reference => reference.Themes.Count),
            ["rgaa_criteria"] = references.Sum(reference => reference.Themes.Sum(theme => theme.Criteria.Count)),
            ["rgaa_tests"] = references.Sum(reference => reference.Themes.Sum(theme => theme.Criteria.Sum(criterion => criterion.Tests.Count))),
            ["missions"] = audits.Count,
            ["audit_pages"] = audits.Sum(audit => audit.Pages.Count),
            ["audit_test_results"] = audits.Sum(audit => audit.Pages.Sum(page => page.TestResults.Count)),
            ["audit_suggestions"] = audits.Sum(audit => audit.Pages.Sum(page => page.Suggestions.Count)),
            ["audit_findings"] = audits.Sum(audit => audit.Pages.Sum(page => page.Findings.Count)),
            ["audit_finding_history"] = audits.Sum(audit => audit.Pages.Sum(page => page.Findings.Sum(finding => finding.History.Count))),
            ["external_issue_links"] = audits.Sum(audit => audit.Pages.Sum(page => page.Findings.Sum(finding => finding.ExternalIssueLinks.Count)))
        };

        foreach (var (table, importedCount) in expected)
        {
            if (TableExists(connection, table) && ScalarInt(connection, $"SELECT COUNT(*) FROM [{table}];") != importedCount)
                throw new InvalidDataException($"L’import SQLite n’a pas pu relier toutes les lignes de {table} à leur dossier ou référentiel.");
        }
    }

    private static int? ReadNullableInt(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static DateTime? ReadNullableDate(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : ReadDate(reader.GetString(ordinal));

    private static DateTime ReadDate(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();

    private static string StripFrameworkPrefix(string version, string frameworkName) =>
        version.StartsWith(frameworkName + " ", StringComparison.OrdinalIgnoreCase)
            ? version[(frameworkName.Length + 1)..]
            : version;

    private sealed record SqliteReferenceVersion(long Id, string FrameworkId, string FrameworkName, string VersionCode, string Jurisdiction, string Language, string ContentTypesJson, string DataFormat, int? ExpectedThemeCount, int? ExpectedCriterionCount, int? ExpectedTestCount, string LicenseName, string LicenseUrl, bool RequireMethodologies, string SourceUrl, string SourceRevision, string WcagVersion, DateTime? PublishedAtUtc, DateTime? ImportedAtUtc, string CatalogStatus, string? CatalogChecksum);
    private sealed record SqliteMission(long Id, string Name, string Organization, string WebsiteUrl, string VersionCode, long? ReferenceVersionId, DateTime CreatedAtUtc, string Status);
    private sealed record SqlitePage(long Id, string Name, string Url, string SamplingReason, DateTime AddedAtUtc);
    private sealed record SqliteFinding(long Id, long PageId, long TestId, string Title, string Description, string Locator, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
    private sealed record LegacyJsonProof(string Id, string Application, string AuditId, string Fixture, string Url, string Page, string SelectedElementTag, string SelectedElementText, DateTimeOffset CapturedAt, string CaptureMethod, string ScreenshotFile, string DataHandling, string Context);
}
