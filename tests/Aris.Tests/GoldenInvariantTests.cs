using Microsoft.Data.SqlClient;
using Xunit;

namespace Aris.Tests;

/// <summary>
/// Verifies the project's core correctness contract (SRS §6.4, §10): the database
/// view <c>dbo.vw_GoldenInvariantViolations</c> must return zero rows.
/// </summary>
/// <remarks>
/// This is deliberately a DATABASE test. The invariant is enforced by filtered
/// unique indexes, triggers and the view itself, so it can only be verified against
/// real SQL Server - not through the application layer or an in-memory provider.
///
/// Requires the environment variable <see cref="TestDatabase.ConnectionVariable"/>.
/// When it is unset these tests SKIP rather than fail, so a teammate without a
/// database running is never blocked and never sees a misleading red build.
/// </remarks>
public sealed class GoldenInvariantTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GoldenInvariant_HasNoViolations()
    {
        var connectionString = TestDatabase.ConnectionString;
        if (connectionString is null)
        {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(Token);

        if (!await TestDatabase.SchemaIsDeployedAsync(connection, Token))
        {
            Assert.Skip(
                $"{TestDatabase.ViolationsView} not found in '{connection.Database}'. " +
                "Deploy docs/aris_schema_v2.sql first (see README > Getting Started).");
        }

        var violations = await GetViolationsAsync(connection, Token);

        Assert.True(
            violations.Count == 0,
            $"The golden invariant is violated - {violations.Count} row(s) in {TestDatabase.ViolationsView}:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Negative control: inserts a deliberately invalid ACTIVE citizen (no membership,
    /// no residence) inside a transaction that is always rolled back, and asserts the
    /// view reports it. Without this, a broken or empty view would let the positive
    /// test pass while detecting nothing.
    /// </summary>
    [Fact]
    public async Task GoldenInvariant_DetectsDeliberateViolation()
    {
        var connectionString = TestDatabase.ConnectionString;
        if (connectionString is null)
        {
            Assert.Skip($"Set {TestDatabase.ConnectionVariable} to run this test.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(Token);

        if (!await TestDatabase.SchemaIsDeployedAsync(connection, Token))
        {
            Assert.Skip($"{TestDatabase.ViolationsView} is not deployed - nothing to exercise.");
        }

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(Token);

        int citizenId;
        try
        {
            // An ACTIVE citizen with neither an ACTIVE membership nor an ACTIVE
            // residence violates checks C1 and C2. NationalId is UNIQUE and must be
            // 12 numeric characters (CK_Citizen_NationalId).
            var nationalId = DateTime.UtcNow.Ticks.ToString()[^12..];

            await using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO dbo.Citizen (NationalId, FullName, DateOfBirth, Gender, Status)
                    OUTPUT INSERTED.CitizenId
                    VALUES (@nationalId, N'Invariant Negative Control', '1990-01-01', 'OTHER', 'ACTIVE');
                    """;
                insert.Parameters.AddWithValue("@nationalId", nationalId);
                citizenId = (int)(await insert.ExecuteScalarAsync(Token)
                    ?? throw new InvalidOperationException("Citizen insert returned no identity."));
            }

            var violations = await GetViolationsAsync(connection, Token, transaction);
            var mine = violations.Where(v => v.Contains($"CitizenId={citizenId}", StringComparison.Ordinal)).ToList();

            Assert.True(
                mine.Count > 0,
                $"Expected the view to report the deliberately invalid citizen {citizenId}, but it reported nothing. " +
                $"Total violations seen: {violations.Count}. The detector may be broken.");
        }
        finally
        {
            // Never leave the control row behind: the schema guards history tables
            // against DELETE (append-only), so a rollback is the only safe cleanup.
            await transaction.RollbackAsync(Token);
        }
    }

    /// <summary>Reads every violation as a readable line: "TYPE CitizenId=.. HouseholdId=..".</summary>
    private static async Task<List<string>> GetViolationsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken,
        SqlTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = 30;
        command.CommandText =
            "SELECT ViolationType, CitizenId, HouseholdId FROM dbo.vw_GoldenInvariantViolations;";

        var violations = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var type = reader.GetString(0);
            var citizenId = reader.IsDBNull(1) ? "-" : reader.GetInt32(1).ToString();
            var householdId = reader.IsDBNull(2) ? "-" : reader.GetInt32(2).ToString();
            violations.Add($"  {type}  CitizenId={citizenId}  HouseholdId={householdId}");
        }

        return violations;
    }
}
