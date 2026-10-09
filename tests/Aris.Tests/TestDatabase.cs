using Microsoft.Data.SqlClient;

namespace Aris.Tests;

/// <summary>
/// Central place for resolving the test database and the shared SQL fragments.
/// </summary>
internal static class TestDatabase
{
    /// <summary>
    /// Environment variable holding the test connection string. Preferred over any
    /// file so no secret is ever committed and each developer/CI can point at their
    /// own SQL Server instance.
    /// </summary>
    public const string ConnectionVariable = "ARIS_TEST_CONNECTION";

    /// <summary>View and tables the golden-invariant check depends on.</summary>
    public const string ViolationsView = "dbo.vw_GoldenInvariantViolations";

    /// <summary>
    /// Resolved connection string, or <c>null</c> when the environment variable is
    /// unset/blank. A null result means the caller must skip rather than fail.
    /// </summary>
    public static string? ConnectionString
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(ConnectionVariable);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    /// <summary>
    /// Copy-paste guidance shown in skip messages. The password is intentionally not
    /// hardcoded here - read it from the git-ignored .env file (CON-SEC-02).
    /// </summary>
    public const string SetupHint =
        "Server=localhost,1433;Database=ArisDb;User Id=sa;Password=<your .env password>;TrustServerCertificate=True";

    public static async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connectionString = ConnectionString
            ?? throw new InvalidOperationException(
                $"{ConnectionVariable} is not set. Set it to a SQL Server connection string, e.g. {SetupHint}");

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>True when an object with <paramref name="objectName"/> exists in <paramref name="schemaName"/>.</summary>
    public static async Task<bool> ObjectExistsAsync(
        SqlConnection connection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sys.objects WHERE schema_id = SCHEMA_ID(@schema) AND name = @name;";
        command.Parameters.AddWithValue("@schema", schemaName);
        command.Parameters.AddWithValue("@name", objectName);
        var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count > 0;
    }

    /// <summary>True when the violations view and the tables it reads all exist.</summary>
    public static async Task<bool> SchemaIsDeployedAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        if (!await ObjectExistsAsync(connection, "dbo", "vw_GoldenInvariantViolations", cancellationToken))
            return false;
        if (!await ObjectExistsAsync(connection, "dbo", "HouseholdMembership", cancellationToken))
            return false;
        if (!await ObjectExistsAsync(connection, "dbo", "Residence", cancellationToken))
            return false;
        return true;
    }
}
