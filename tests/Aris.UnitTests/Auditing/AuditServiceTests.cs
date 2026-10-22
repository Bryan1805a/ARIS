using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Infrastructure.Auditing;
using Aris.Infrastructure.Persistence;
using Aris.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aris.UnitTests.Auditing;

/// <summary>
/// Behaviour of the audit writer that can be proven without SQL Server: validation,
/// field mapping, and the failure path's no-throw contract.
/// </summary>
/// <remarks>
/// The in-memory provider does not enforce the CHECK constraints or the append-only
/// trigger on <c>dbo.AuditLog</c>. That the row really lands in SQL Server, and that
/// the independent path survives a rollback, is covered by
/// <c>tests/Aris.Tests/AuditServiceTests</c>.
/// </remarks>
public sealed class AuditServiceTests : InMemoryDatabaseTestBase<ArisDbContext>
{
    protected override ArisDbContext CreateContext(DbContextOptions<ArisDbContext> options)
        => new(options);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LogAsync_Writes_a_SUCCESS_row_with_every_field_mapped()
    {
        var correlationId = Guid.NewGuid();

        await using var context = Context;
        var service = new AuditService(context);

        await service.LogAsync(new AuditEntry
        {
            ActorUserId = 7,
            Action = "REGISTER_HOUSEHOLD",
            EntityType = "Household",
            EntityId = 42,
            DetailsJson = """{"after":{"status":"ACTIVE"}}""",
            CorrelationId = correlationId
        }, Token);

        await using var read = Context;
        var row = await read.AuditLogs.SingleAsync(Token);

        Assert.Equal(7, row.ActorUserId);
        Assert.Null(row.AttemptedUsername);
        Assert.Equal("REGISTER_HOUSEHOLD", row.Action);
        Assert.Equal("Household", row.EntityType);
        Assert.Equal(42, row.EntityId);
        Assert.Equal("SUCCESS", row.Result);
        Assert.Equal("""{"after":{"status":"ACTIVE"}}""", row.Details);
        Assert.Equal(correlationId, row.CorrelationId);
        Assert.NotEqual(default, row.OccurredAt);
    }

    [Fact]
    public async Task LogAsync_Rejects_an_invalid_entry_before_writing_anything()
    {
        await using var context = Context;
        var service = new AuditService(context);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.LogAsync(new AuditEntry
            {
                // A SUCCESS row must name a real actor (the rule the database leaves open).
                ActorUserId = 0,
                Action = "",
                EntityType = ""
            }, Token));

        Assert.NotEmpty(exception.Errors);

        await using var read = Context;
        Assert.Empty(read.AuditLogs);
    }

    [Fact]
    public async Task LogAsync_Null_entry_throws_instead_of_writing_an_unattributed_row()
    {
        await using var context = Context;
        var service = new AuditService(context);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.LogAsync(null!, Token));
    }

    [Fact]
    public async Task LogFailureIndependentAsync_Invalid_entry_does_not_throw()
    {
        // No actor and no attempted username violates CK_Audit_Actor. The failure path
        // must not replace the caller's real error with a validation exception, so the
        // entry is reported out-of-band and skipped. The connection string is
        // deliberately unreachable: validation must short-circuit before any I/O.
        await using var context = Context;
        var service = new AuditService(context);

        await service.LogFailureIndependentAsync(
            "Server=127.0.0.1,1;Database=ArisDb;User Id=nobody;Password=unused;TrustServerCertificate=True;Connect Timeout=1;",
            new AuditFailureEntry
            {
                Action = "LOGIN",
                EntityType = "UserAccount"
            },
            Token);
    }

    [Fact]
    public async Task LogFailureIndependentAsync_Write_failure_does_not_throw()
    {
        // The best-effort contract in its strongest form: the audit write fails, the
        // caller still gets control back with the original exception intact.
        // An unknown connection-string keyword is rejected by SqlClient immediately and
        // is not a transient fault, so SqlResilience does not retry it - the test stays
        // fast while still exercising the catch.
        await using var context = Context;
        var service = new AuditService(context);

        await service.LogFailureIndependentAsync(
            "Server=127.0.0.1,1;Database=ArisDb;User Id=nobody;Password=unused;TrustServerCertificate=True;NotARealKeyword=true;",
            new AuditFailureEntry
            {
                AttemptedUsername = "unreachable.audit.test",
                Action = "LOGIN",
                EntityType = "UserAccount"
            },
            Token);
    }

    [Fact]
    public async Task LogFailureIndependentAsync_Blank_connection_string_is_a_programmer_error()
    {
        await using var context = Context;
        var service = new AuditService(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.LogFailureIndependentAsync(
                "   ",
                new AuditFailureEntry
                {
                    AttemptedUsername = "someone",
                    Action = "LOGIN",
                    EntityType = "UserAccount"
                },
                Token));
    }
}
