using Microsoft.Data.Sqlite;

var sourcePath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Numera11y", "RGAAStudio", "rgaa-studio.db");

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Ancienne base SQLite introuvable : {sourcePath}");
    return 2;
}

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = sourcePath,
    Mode = SqliteOpenMode.ReadOnly,
    Pooling = false
}.ToString();

using var connection = new SqliteConnection(connectionString);
connection.Open();

Console.WriteLine("Inventaire read-only de l’ancienne base RGAA Studio (aucune valeur métier détaillée n’est affichée).");
var migrationVersion = ScalarLong("SELECT COALESCE(MAX(version), 0) FROM schema_migrations;");
Console.WriteLine($"Schéma SQLite version {migrationVersion}.");

foreach (var table in new[]
{
    "missions", "audit_pages", "reference_versions", "rgaa_themes", "rgaa_criteria", "rgaa_tests",
    "audit_test_results", "audit_suggestions", "audit_findings", "audit_finding_history", "external_issue_links"
})
{
    if (TableExists(table))
        Console.WriteLine($"{table}: {ScalarLong($"SELECT COUNT(*) FROM [{table}];")} documents/lignes");
}

if (TableExists("reference_versions"))
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT framework_id, version_code, catalog_status,
               (SELECT COUNT(*) FROM rgaa_themes WHERE reference_version_id = v.id),
               (SELECT COUNT(*) FROM rgaa_criteria WHERE reference_version_id = v.id),
               (SELECT COUNT(*) FROM rgaa_tests WHERE reference_version_id = v.id)
        FROM reference_versions v
        ORDER BY framework_id, version_code;
        """;
    using var reader = command.ExecuteReader();
    while (reader.Read())
        Console.WriteLine($"Référentiel {reader.GetString(0)}:{reader.GetString(1)} ({reader.GetString(2)}) — thèmes {reader.GetInt32(3)}, critères {reader.GetInt32(4)}, tests {reader.GetInt32(5)}");
}

return 0;

long ScalarLong(string sql)
{
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    return Convert.ToInt64(command.ExecuteScalar());
}

bool TableExists(string name)
{
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
    command.Parameters.AddWithValue("$name", name);
    return command.ExecuteScalar() is not null;
}
