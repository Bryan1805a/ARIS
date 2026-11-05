using Microsoft.Data.SqlClient;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Proves <c>T10</c>: the deployed schema still contains the 11 tables, 5 triggers,
/// 3 views, the <c>aris_app</c> role, and the 4 seed configuration rows.
/// </summary>
public sealed class SchemaInventoryTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Schema_Contains_The_Expected_Objects_And_Seed_Rows() {
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        await using var connection = new SqlConnection(TestDatabase.ConnectionString);
        await connection.OpenAsync(Token);

        if (!await TestDatabase.SchemaIsDeployedAsync(connection, Token)) {
            Assert.Skip(
                "The ARIS schema is not deployed. Run .\\scripts\\deploy_database.ps1 first.");
        }

        var tables = await ReadStringsAsync(
            connection,
            "SELECT name FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') ORDER BY name;");

        Assert.Equal(
            new[]
            {
                "Address",
                "AuditLog",
                "Citizen",
                "Household",
                "HouseholdAddress",
                "HouseholdMembership",
                "Residence",
                "SystemConfiguration",
                "TemporaryResidence",
                "TransferRequest",
                "TransferRequestItem",
                "UserAccount"
            },
            tables);

        var triggers = await ReadStringsAsync(
            connection,
            "SELECT name FROM sys.triggers WHERE parent_class = 1 ORDER BY name;");

        Assert.Equal(
            new[]
            {
                "trg_AuditLog_AppendOnly",
                "trg_HouseholdAddress_Guard",
                "trg_Membership_Guard",
                "trg_Residence_Guard",
                "trg_TransferRequest_Frozen"
            },
            triggers);

        var views = await ReadStringsAsync(
            connection,
            "SELECT name FROM sys.views WHERE schema_id = SCHEMA_ID('dbo') ORDER BY name;");

        Assert.Equal(
            new[]
            {
                "vw_AddressCanonical",
                "vw_GoldenInvariantViolations",
                "vw_HouseholdCurrent"
            },
            views);

        var roleCount = Convert.ToInt32(
            await ExecuteScalarAsync(
                connection,
                "SELECT COUNT(*) FROM sys.database_principals " +
                "WHERE name = N'aris_app' AND type = 'R';"));

        Assert.Equal(1, roleCount);

        var seedKeys = await ReadStringsAsync(
            connection,
            "SELECT ConfigKey FROM dbo.SystemConfiguration ORDER BY ConfigKey;");

        Assert.Equal(
            new[]
            {
                "LOCKOUT_MINUTES",
                "MAX_FAILED_LOGINS",
                "PASSWORD_MIN_LENGTH",
                "SESSION_TIMEOUT_MINUTES"
            },
            seedKeys);
    }

    private static async Task<List<string>> ReadStringsAsync(
        SqlConnection connection,
        string sql) {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(Token);

        while (await reader.ReadAsync(Token)) {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<object?> ExecuteScalarAsync(
        SqlConnection connection,
        string sql) {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(Token);
    }
}