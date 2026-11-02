using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Repositories;
using Aris.Infrastructure.Resilience;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Proves the audit viewer's database filters against real SQL Server (<c>AUD-01</c>).
/// </summary>
/// <remarks>
/// The test inserts rows with a run-unique action prefix. Audit rows are append-only by
/// design, so they intentionally remain after the test; they are identifiable by the
/// <c>ITQ-</c> prefix and the <c>it-audit-query-actor</c> username.
/// </remarks>
public sealed class AuditQueryFilterTests {
    private const string ActorUsername = "it-audit-query-actor";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Filters_By_Date_Actor_Entity_Action_And_Result() {
        // Run against real SQL Server so filter behavior covers repository
        // translation and database comparison semantics, not just in-memory code.
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        var connectionString = TestDatabase.ConnectionString!;
        await EnsureSchemaAsync(connectionString);

        var options = new DbContextOptionsBuilder<ArisDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var run = Guid.NewGuid().ToString("N")[..10];
        var alpha = $"ITQ-{run}-ALPHA";
        var beta = $"ITQ-{run}-BETA";
        var gamma = $"ITQ-{run}-GAMMA";
        var ghost = $"itq-ghost-{run}";
        var origin = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var actorId = await EnsureActorAsync(options);

        await using (var context = new ArisDbContext(options)) {
            // Seed one success, one authenticated failure, and one failed login
            // attempt to exercise both actor identity paths.
            context.AuditLogs.AddRange(
                new AuditLog {
                    ActorUserId = actorId,
                    Action = alpha,
                    EntityType = "Citizen",
                    EntityId = 1,
                    OccurredAt = origin,
                    Result = "SUCCESS",
                    Details = """{"row":"alpha"}""",
                    CorrelationId = Guid.NewGuid()
                },
                new AuditLog {
                    ActorUserId = actorId,
                    Action = beta,
                    EntityType = "Household",
                    EntityId = 2,
                    OccurredAt = origin.AddHours(1),
                    Result = "FAILURE",
                    Details = """{"row":"beta"}""",
                    CorrelationId = Guid.NewGuid()
                },
                new AuditLog {
                    ActorUserId = null,
                    AttemptedUsername = ghost,
                    Action = gamma,
                    EntityType = "UserAccount",
                    EntityId = null,
                    OccurredAt = origin.AddHours(2),
                    Result = "FAILURE",
                    Details = """{"row":"gamma"}""",
                    CorrelationId = Guid.NewGuid()
                });

            await context.SaveChangesAsync(Token);
        }

        await using var read = new ArisDbContext(options);
        // Use an admin session because the query service protects audit access
        // before delegating filter criteria to the repository.
        var service = new AuditQueryService(
            new AuditRepository(read),
            new TestUserSession());

        var alphaResult = await service.SearchAsync(
            new AuditSearchCriteria { Action = alpha, Limit = 10 }, Token);

        // Exact action filtering should return only the authenticated actor's
        // citizen event and preserve its entity metadata.
        Assert.True(alphaResult.IsSuccess, string.Join("; ", alphaResult.Errors));
        var alphaRow = Assert.Single(alphaResult.Value!);
        Assert.Equal(alpha, alphaRow.Action);
        Assert.Equal(ActorUsername, alphaRow.Username);
        Assert.Equal(actorId, alphaRow.ActorUserId);
        Assert.Equal("Citizen", alphaRow.EntityType);
        Assert.Equal(1, alphaRow.EntityId);

        var failures = await service.SearchAsync(
            new AuditSearchCriteria {
                Action = $"ITQ-{run}-",
                Result = "FAILURE",
                Limit = 10
            },
            Token);

        // Prefix action filtering combined with FAILURE must exclude the
        // successful alpha row while retaining both failure variants.
        Assert.True(failures.IsSuccess, string.Join("; ", failures.Errors));
        Assert.Equal(2, failures.Value!.Count);
        Assert.DoesNotContain(failures.Value, row => row.Action == alpha);
        Assert.Contains(failures.Value, row => row.Action == beta);
        Assert.Contains(failures.Value, row => row.Action == gamma);

        var ghostRows = await service.SearchAsync(
            new AuditSearchCriteria { Username = ghost, Limit = 10 }, Token);

        // Failed login rows use AttemptedUsername because no account actor
        // exists; the query surface should expose that value as the username.
        Assert.True(ghostRows.IsSuccess, string.Join("; ", ghostRows.Errors));
        var ghostRow = Assert.Single(ghostRows.Value!);
        Assert.Equal(ghost, ghostRow.Username);
        Assert.Null(ghostRow.ActorUserId);
        Assert.Equal(ghost, ghostRow.AttemptedUsername);

        var entityRows = await service.SearchAsync(
            new AuditSearchCriteria {
                Action = $"ITQ-{run}-",
                EntityType = "Household",
                Limit = 10
            },
            Token);

        // Entity filtering narrows the run's action prefix to the household row.
        Assert.True(entityRows.IsSuccess, string.Join("; ", entityRows.Errors));
        Assert.Single(entityRows.Value!);

        var dateRows = await service.SearchAsync(
            new AuditSearchCriteria {
                Action = $"ITQ-{run}-",
                FromUtc = origin.AddMinutes(30),
                ToUtcExclusive = origin.AddHours(2),
                Limit = 10
            },
            Token);

        // The exclusive upper bound at 02:00 excludes gamma while the lower
        // bound excludes alpha, leaving only beta.
        Assert.True(dateRows.IsSuccess, string.Join("; ", dateRows.Errors));
        var dateRow = Assert.Single(dateRows.Value!);
        Assert.Equal(beta, dateRow.Action);

        var limited = await service.SearchAsync(
            new AuditSearchCriteria {
                Action = $"ITQ-{run}-",
                Limit = 1
            },
            Token);

        // Results are ordered newest first, so a limit of one returns gamma.
        Assert.True(limited.IsSuccess, string.Join("; ", limited.Errors));
        Assert.Single(limited.Value!);
        Assert.Equal(gamma, limited.Value![0].Action);
    }

    private static async Task EnsureSchemaAsync(string connectionString) {
        // Integration tests skip when the local database has not been deployed,
        // allowing unit-test runs without SQL Server to remain useful.
        await using var connection = await SqlResilience.OpenAsync(
            connectionString, cancellationToken: Token);

        if (!await TestDatabase.SchemaIsDeployedAsync(connection, Token)) {
            Assert.Skip(
                "The ARIS schema is not deployed. Run .\\scripts\\deploy_database.ps1 first.");
        }

        if (!await TestDatabase.ObjectExistsAsync(connection, "dbo", "AuditLog", Token)) {
            Assert.Skip("dbo.AuditLog is not deployed.");
        }
    }

    private static async Task<int> EnsureActorAsync(
        DbContextOptions<ArisDbContext> options) {
        await using var context = new ArisDbContext(options);

        // Reuse a stable actor account so the audit rows can resolve its
        // username while the run-specific action prefix keeps queries isolated.
        var actor = await context.UserAccounts
            .FirstOrDefaultAsync(u => u.Username == ActorUsername, Token);

        if (actor is null) {
            actor = new UserAccount {
                Username = ActorUsername,
                PasswordHash = "integration-test-not-a-real-hash",
                FullName = "Audit Query Test Actor",
                Role = "OFFICER",
                Status = "ACTIVE",
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow
            };

            context.UserAccounts.Add(actor);
            await context.SaveChangesAsync(Token);
        } else if (actor.Status != "ACTIVE") {
            actor.Status = "ACTIVE";
            await context.SaveChangesAsync(Token);
        }

        return actor.UserId;
    }

    private sealed class TestUserSession : IUserSession {
        public int? UserId => 1;
        public string? Username => "admin";
        public string? Role => "ADMIN";
        public bool IsAuthenticated => true;

        // The integration test only needs a permanently authenticated admin
        // session; sign-in transitions are outside this query test's scope.
        public void SignIn(int userId, string username, string role) {
        }

        public void SignOut() {
        }
    }
}