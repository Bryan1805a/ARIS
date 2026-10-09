using Microsoft.Data.SqlClient;
using Aris.Infrastructure.Resilience;
using Xunit;

namespace Aris.Tests;

/// <summary>
/// Reports what the configured database actually is. Useful when the shared Azure
/// SQL Database is unreachable (firewall, auto-pause) because the golden-invariant
/// tests only say "skipped" and never explain why.
/// </summary>
public sealed class DatabaseConnectivityTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Database_IsReachable_And_SchemaIsDeployed()
    {
        if (TestDatabase.ConnectionString is null)
        {
            Assert.Skip($"Set {TestDatabase.ConnectionVariable} to run this test.");
        }

        await using var connection = await SqlResilience.OpenAsync(
            TestDatabase.ConnectionString!, cancellationToken: Token);

        var server = await ScalarAsync(connection, "SELECT CAST(SERVERPROPERTY('ServerName') AS nvarchar(256));");
        var edition = await ScalarAsync(connection, "SELECT CAST(SERVERPROPERTY('Edition') AS nvarchar(256));");
        var database = connection.Database;

        // Deliberately NOT asserting the database name: the name is a deployment choice
        // (this project uses 'ARIS-DB' on Azure and 'ArisDb' in docker-compose), and
        // hardcoding it made the test fail for a purely cosmetic reason. What matters is
        // that the schema is deployed and readable.
        Assert.True(
            await TestDatabase.SchemaIsDeployedAsync(connection, Token),
            $"Connected to '{server}' ({edition}), database '{database}', but the schema is not deployed. " +
            "Run scripts/deploy-database.ps1 or docs/aris_schema_v2.sql against it.");
    }

    private static async Task<string> ScalarAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (await command.ExecuteScalarAsync(Token))?.ToString() ?? "(unknown)";
    }
}

/// <summary>
/// Guards the transient-fault classification. If these rules are wrong, retry
/// silently never happens and an Azure SQL serverless auto-resume surfaces to the
/// user as a crash.
/// </summary>
public sealed class SqlResilienceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>
    /// Negligible backoff so the retry tests do not sleep through the production
    /// delays (2s, 4s, ...). The jitter added by the backoff is 0-250ms.
    /// </summary>
    private static readonly TimeSpan TestDelay = TimeSpan.Zero;

    [Theory]
    [InlineData(40613)]  // database not currently available (serverless auto-resume)
    [InlineData(10928)]  // resource limit reached
    [InlineData(10054)]  // connection reset by peer
    [InlineData(49918)]  // too many operations for the subscription
    [InlineData(4060)]   // cannot open database requested by the login
    [InlineData(40197)]  // service error processing the request
    public void SqlErrors_KnownToBeTransient_AreTransient(int errorNumber)
    {
        Assert.True(SqlResilience.IsTransientErrorNumber(errorNumber));
    }

    [Theory]
    [InlineData(2627)]   // unique constraint violation - a real bug, must not be retried
    [InlineData(547)]    // foreign key violation - a real bug, must not be retried
    [InlineData(51001)]  // trg_Residence_Guard append-only violation
    [InlineData(51030)]  // trg_AuditLog_AppendOnly immutability violation
    public void SqlErrors_ThatAreRealBugs_AreNotTransient(int errorNumber)
    {
        Assert.False(SqlResilience.IsTransientErrorNumber(errorNumber));
    }

    [Fact]
    public void TimeoutAndWrappedExceptions_AreTransient()
    {
        Assert.True(SqlResilience.IsTransient(new TimeoutException()));
        Assert.True(SqlResilience.IsTransient(
            new InvalidOperationException("wrapper", new TimeoutException())));
        Assert.False(SqlResilience.IsTransient(new InvalidOperationException("plain")));
    }

    [Fact]
    public async Task ExecuteAsync_RetriesTransientFailures_ThenSucceeds()
    {
        var attempts = 0;

        var result = await SqlResilience.ExecuteAsync(_ =>
        {
            attempts++;
            // Two transient failures, then success.
            return attempts < 3
                ? Task.FromException<int>(new TimeoutException("simulated"))
                : Task.FromResult(42);
        }, cancellationToken: Token, baseDelay: TestDelay);

        Assert.Equal(3, attempts);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryPermanentFailures()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SqlResilience.ExecuteAsync<int>(_ =>
            {
                attempts++;
                return Task.FromException<int>(new InvalidOperationException("permanent"));
            }, cancellationToken: Token, baseDelay: TestDelay));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_GivesUpAfterTheAttemptLimit_AndRethrowsTheRealError()
    {
        var attempts = 0;

        var error = await Assert.ThrowsAsync<TimeoutException>(() =>
            SqlResilience.ExecuteAsync<int>(
                _ =>
                {
                    attempts++;
                    return Task.FromException<int>(new TimeoutException("still down"));
                },
                attempts: 2,
                cancellationToken: Token,
                baseDelay: TestDelay));

        Assert.Equal(2, attempts);
        Assert.Equal("still down", error.Message);
    }
}
