using Aris.Application.Auditing;
using Aris.Domain.Entities;
using Aris.Infrastructure.Auditing;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aris.Tests;

/// <summary>
/// Proves both audit paths against real SQL Server: the success row joins the caller's
/// transaction, and the failure row is written on an independent connection after a
/// rollback (<c>D7</c>, <c>BR-AUD-01</c>/<c>BR-AUD-03</c>).
/// </summary>
/// <remarks>
/// <para>
/// The failure-path test deliberately commits one <c>FAILURE</c> row per run. That is the
/// only way to prove the independent connection works, and the audit trail is append-only
/// by design, so the row cannot (and must not) be cleaned up. It is identifiable by its
/// <c>AUDIT_SELFTEST_FAILURE</c> action and <c>it-audit-*</c> attempted username.
/// </para>
/// <para>
/// Both tests skip when <see cref="TestDatabase.ConnectionVariable"/> is unset.
/// </para>
/// </remarks>
public sealed class AuditServiceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LogAsync_writes_inside_the_callers_transaction_and_rolls_back_with_it()
    {
        if (TestDatabase.ConnectionString is null)
        {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        var connectionString = TestDatabase.ConnectionString!;
        await EnsureAuditTableAsync(connectionString);

        var options = new DbContextOptionsBuilder<ArisDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new ArisDbContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync(Token);

        var username = Username();
        var user = new UserAccount
        {
            Username = username,
            PasswordHash = "not-a-real-hash",
            FullName = "Audit Integration Test",
            Role = "OFFICER",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        context.UserAccounts.Add(user);
        await context.SaveChangesAsync(Token);

        var correlationId = Guid.NewGuid();
        var service = new AuditService(context);

        await service.LogAsync(new AuditEntry
        {
            ActorUserId = user.UserId,
            Action = "AUDIT_SELFTEST_SUCCESS",
            EntityType = "UserAccount",
            EntityId = user.UserId,
            DetailsJson = """{"reason":"integration test"}""",
            CorrelationId = correlationId
        }, Token);

        // Same context and therefore the same transaction: the uncommitted rows are
        // visible here and nowhere else.
        var row = await context.AuditLogs.SingleAsync(a => a.CorrelationId == correlationId, Token);

        Assert.Equal("SUCCESS", row.Result);
        Assert.Equal(user.UserId, row.ActorUserId);
        Assert.Null(row.AttemptedUsername);
        Assert.Equal("AUDIT_SELFTEST_SUCCESS", row.Action);
        Assert.Equal("UserAccount", row.EntityType);
        Assert.Equal(user.UserId, row.EntityId);
        Assert.Equal("""{"reason":"integration test"}""", row.Details);
        Assert.NotEqual(default, row.OccurredAt);

        await transaction.RollbackAsync(Token);

        // The rollback must take the audit row with it - the operation did not happen.
        await using var read = new ArisDbContext(options);
        Assert.False(await read.AuditLogs.AnyAsync(a => a.CorrelationId == correlationId, Token));
        Assert.False(await read.UserAccounts.AnyAsync(u => u.Username == username, Token));
    }

    [Fact]
    public async Task LogFailureIndependentAsync_writes_a_FAILURE_row_on_a_new_connection()
    {
        if (TestDatabase.ConnectionString is null)
        {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        var connectionString = TestDatabase.ConnectionString!;
        await EnsureAuditTableAsync(connectionString);

        var options = new DbContextOptionsBuilder<ArisDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var correlationId = Guid.NewGuid();
        var attemptedUsername = Username();

        await using (var context = new ArisDbContext(options))
        {
            var service = new AuditService(context);

            // No business transaction, no actor row required: exactly the shape used
            // after a rollback for an unknown-user login failure.
            await service.LogFailureIndependentAsync(connectionString, new AuditFailureEntry
            {
                AttemptedUsername = attemptedUsername,
                Action = "AUDIT_SELFTEST_FAILURE",
                EntityType = "UserAccount",
                DetailsJson = """{"reason":"independent connection test"}""",
                CorrelationId = correlationId
            }, Token);
        }

        // Read through a fresh context - a different connection from the writer - to
        // prove the row was committed independently rather than left in a transaction.
        await using var read = new ArisDbContext(options);
        var row = await read.AuditLogs.SingleAsync(a => a.CorrelationId == correlationId, Token);

        Assert.Equal("FAILURE", row.Result);
        Assert.Null(row.ActorUserId);
        Assert.Equal(attemptedUsername, row.AttemptedUsername);
        Assert.Equal("AUDIT_SELFTEST_FAILURE", row.Action);
        Assert.Equal("UserAccount", row.EntityType);
        Assert.Null(row.EntityId);
        Assert.Equal("""{"reason":"independent connection test"}""", row.Details);
        Assert.NotEqual(default, row.OccurredAt);
    }

    private static string Username() => $"it-audit-{Guid.NewGuid():N}"[..30];

    /// <summary>
    /// Skips the test when the schema has not been deployed instead of failing with
    /// "Invalid object name 'dbo.AuditLog'".
    /// </summary>
    private static async Task EnsureAuditTableAsync(string connectionString)
    {
        await using var connection = await SqlResilience.OpenAsync(
            connectionString, cancellationToken: Token);

        if (!await TestDatabase.ObjectExistsAsync(connection, "dbo", "AuditLog", Token))
        {
            Assert.Skip(
                "dbo.AuditLog is not deployed. Run .\\scripts\\deploy-database.ps1 first.");
        }
    }
}
