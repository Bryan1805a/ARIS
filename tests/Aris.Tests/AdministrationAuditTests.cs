using Aris.Application.Accounts;
using Aris.Application.Common;
using Aris.Application.SystemConfiguration;
using Aris.Domain.Entities;
using Aris.Infrastructure.Auditing;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Repositories;
using Aris.Infrastructure.Resilience;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Proves <c>T14</c> against real SQL Server: account and configuration mutations write
/// their success audit rows, with the admin as actor and no password material.
/// </summary>
public sealed class AdministrationAuditTests {
    private const string AdminUsername = "it-admin-audit";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AdminMutations_WriteAuditRows_WithActorsAndNoSecrets() {
        // This integration test uses real SQL Server so it verifies persistence,
        // audit attribution, and secret exclusion together.
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        var connectionString = TestDatabase.ConnectionString!;
        await EnsureSchemaAsync(connectionString);

        var officerUsername = $"it-audit-officer-{Guid.NewGuid():N}"[..30];
        var options = new DbContextOptionsBuilder<ArisDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        try {
            int adminId;

            // Reuse a stable administrator account while creating a unique
            // officer for each run to avoid collisions between test executions.
            await using (var setup = new ArisDbContext(options)) {
                adminId = await EnsureAdminAsync(setup);
            }

            int officerId;

            await using (var context = new ArisDbContext(options)) {
                // Use one service context for the account mutations, then use
                // fresh contexts below to verify committed database state.
                var service = new AccountService(
                    new UserAccountRepository(context),
                    new AuditService(context),
                    new UnitOfWork(context));

                var create = await service.CreateAccountAsync(
                    officerUsername,
                    "Administration Audit Officer",
                    "Password1!",
                    "OFFICER",
                    adminId,
                    Token);

                Assert.True(create.IsSuccess, string.Join("; ", create.Errors));
                Assert.True(create.Value is int createdId, "Expected the new account id.");
                officerId = createdId;

                var lockResult = await service.ChangeAccountStatusAsync(
                    officerId, "LOCKED", adminId, Token);

                Assert.True(lockResult.IsSuccess, string.Join("; ", lockResult.Errors));
            }

            await using (var read = new ArisDbContext(options)) {
                // Verify both account state and the audit records produced by
                // creation and locking, including the absence of plaintext data.
                var officer = await read.UserAccounts
                    .AsNoTracking()
                    .SingleAsync(u => u.Username == officerUsername, Token);

                Assert.Equal("LOCKED", officer.Status);

                var createRow = await read.AuditLogs
                    .AsNoTracking()
                    .SingleAsync(a => a.Action == "CREATE_ACCOUNT"
                        && a.ActorUserId == adminId
                        && a.Details != null
                        && a.Details.Contains(officerUsername), Token);

                Assert.Equal("UserAccount", createRow.EntityType);
                Assert.Equal(officer.UserId, createRow.EntityId);
                Assert.NotNull(createRow.CorrelationId);
                Assert.False(
                    createRow.Details!.Contains("Password1!", StringComparison.Ordinal));

                var lockRow = await read.AuditLogs
                    .AsNoTracking()
                    .SingleAsync(a => a.Action == "LOCK_ACCOUNT"
                        && a.EntityId == officer.UserId, Token);

                Assert.Equal(adminId, lockRow.ActorUserId);
                Assert.Equal("UserAccount", lockRow.EntityType);
                Assert.NotNull(lockRow.CorrelationId);
            }

            await using (var context = new ArisDbContext(options)) {
                var repository = new SystemConfigurationRepository(context);
                var original = await repository.GetValueAsync("MAX_FAILED_LOGINS") ?? "5";
                var session = new TestUserSession(adminId, AdminUsername, "ADMIN");

                // Write the existing value so the test exercises the complete
                // update-and-audit path without changing shared configuration.
                var service = new SystemConfigurationService(
                    repository,
                    session,
                    new AuditService(context),
                    new UnitOfWork(context));

                var update = await service.UpdateAsync(
                    "MAX_FAILED_LOGINS", original, Token);

                Assert.True(update.IsSuccess, string.Join("; ", update.Errors));
            }

            await using (var read = new ArisDbContext(options)) {
                // Select the latest matching row because the shared database
                // may contain audit entries from earlier integration runs.
                var configRow = await read.AuditLogs
                    .AsNoTracking()
                    .Where(a => a.Action == "UPDATE_SYSTEM_CONFIGURATION"
                        && a.ActorUserId == adminId)
                    .OrderByDescending(a => a.AuditLogId)
                    .FirstAsync(Token);

                Assert.Equal("SystemConfiguration", configRow.EntityType);
                Assert.Null(configRow.EntityId);
                Assert.NotNull(configRow.CorrelationId);
                Assert.Contains(
                    "MAX_FAILED_LOGINS",
                    configRow.Details!,
                    StringComparison.Ordinal);
            }
        } finally {
            await TryDeleteOfficerAsync(connectionString, officerUsername);
        }
    }

    private static async Task EnsureSchemaAsync(string connectionString) {
        // Skip rather than fail when the developer has not deployed the schema
        // or the required audit table for integration testing.
        await using var connection = await SqlResilience.OpenAsync(
            connectionString, cancellationToken: Token);

        if (!await TestDatabase.SchemaIsDeployedAsync(connection, Token)) {
            Assert.Skip(
                "The ARIS schema is not deployed. Run .\\scripts\\deploy-database.ps1 first.");
        }

        if (!await TestDatabase.ObjectExistsAsync(connection, "dbo", "AuditLog", Token)) {
            Assert.Skip("dbo.AuditLog is not deployed.");
        }
    }

    private static async Task<int> EnsureAdminAsync(ArisDbContext context) {
        // Keep the reusable test account active and administrative even if a
        // previous run or manual database change altered its state.
        var admin = await context.UserAccounts
            .FirstOrDefaultAsync(u => u.Username == AdminUsername, Token);

        if (admin is null) {
            admin = new UserAccount {
                Username = AdminUsername,
                PasswordHash = "integration-test-not-a-real-hash",
                FullName = "Administration Audit Test Admin",
                Role = "ADMIN",
                Status = "ACTIVE",
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow
            };

            context.UserAccounts.Add(admin);
            await context.SaveChangesAsync(Token);
        } else if (admin.Role != "ADMIN" || admin.Status != "ACTIVE") {
            admin.Role = "ADMIN";
            admin.Status = "ACTIVE";
            await context.SaveChangesAsync(Token);
        }

        return admin.UserId;
    }

    private static async Task TryDeleteOfficerAsync(
        string connectionString,
        string username) {
        try {
            // Remove only this run's unique officer; the shared admin and audit
            // rows intentionally remain available for subsequent runs.
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM dbo.UserAccount WHERE Username = @username;";
            command.Parameters.AddWithValue("@username", username);

            await command.ExecuteNonQueryAsync(Token);
        } catch {
            // DELETE may be denied for the least-privilege runtime role, so
            // cleanup remains best-effort and does not hide the test result.
        }
    }

    private sealed class TestUserSession : IUserSession {
        public TestUserSession(int userId, string username, string role) {
            UserId = userId;
            Username = username;
            Role = role;
        }

        public int? UserId { get; private set; }
        public string? Username { get; private set; }
        public string? Role { get; private set; }
        public bool IsAuthenticated => UserId.HasValue;

        public void SignIn(int userId, string username, string role) {
            // Provide the minimal mutable session behavior required by the
            // configuration service's administrator check.
            UserId = userId;
            Username = username;
            Role = role;
        }

        public void SignOut() {
            UserId = null;
            Username = null;
            Role = null;
        }
    }
}