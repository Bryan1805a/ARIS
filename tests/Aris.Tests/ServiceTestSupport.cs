using Aris.Application.Auditing;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Resilience;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Shared setup for real-SQL service tests: schema check, users, addresses, citizens,
/// and a recording audit double. Service tests leave their unique data behind; every
/// fixture is invariant-safe.
/// </summary>
internal static class ServiceTestSupport {
    private static int _nationalIdCounter;

    public static DbContextOptions<ArisDbContext> Options(string connectionString) {
        // Centralize SQL Server options so all integration fixtures use the
        // same provider configuration.
        return new DbContextOptionsBuilder<ArisDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }

    public static async Task EnsureSchemaAsync(
        string connectionString,
        CancellationToken ct) {
        // Skip database-dependent tests when the required schema is absent;
        // this keeps local unit-test runs independent from SQL Server setup.
        await using var connection = await SqlResilience.OpenAsync(
            connectionString, cancellationToken: ct);

        if (!await TestDatabase.SchemaIsDeployedAsync(connection, ct)) {
            Assert.Skip(
                "The ARIS schema is not deployed. Run .\\scripts\\deploy_database.ps1 first.");
        }

        if (!await TestDatabase.ObjectExistsAsync(connection, "dbo", "AuditLog", ct)) {
            Assert.Skip("dbo.AuditLog is not deployed.");
        }
    }

    public static async Task<int> EnsureUserAsync(
        ArisDbContext context,
        string username,
        string role,
        string status,
        CancellationToken ct) {
        // Reuse named test users to avoid duplicates, while restoring their
        // role and status so each test starts from the requested state.
        var user = await context.UserAccounts
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null) {
            // The hash is a marker only; these fixtures authenticate by user ID
            // and never use this account for real password verification.
            user = new UserAccount {
                Username = username,
                PasswordHash = "integration-test-not-a-real-hash",
                FullName = "Service Test User",
                Role = role,
                Status = status,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow
            };

            context.UserAccounts.Add(user);
            await context.SaveChangesAsync(ct);
        } else {
            var changed = false;

            if (user.Role != role) {
                user.Role = role;
                changed = true;
            }

            if (user.Status != status) {
                user.Status = status;
                changed = true;
            }

            if (changed) {
                // Persist only when the reusable fixture needed normalization.
                await context.SaveChangesAsync(ct);
            }
        }

        return user.UserId;
    }

    public static async Task<Address> CreateAddressAsync(
        ArisDbContext context,
        string addressLine,
        CancellationToken ct) {
        // Generate a unique code because address codes are constrained by the
        // database and fixtures may remain after a test completes.
        var address = new Address {
            AddressCode = Code("ADR"),
            AddressLine = addressLine,
            Ward = "Ward 1",
            District = "District 1",
            Province = "Test Province",
            Status = "ACTIVE",
            ValidFrom = new DateOnly(2020, 1, 1),
            CreatedAt = DateTime.UtcNow
        };

        context.Addresses.Add(address);
        await context.SaveChangesAsync(ct);

        return address;
    }

    public static async Task<Citizen> CreateCitizenAsync(
        ArisDbContext context,
        string fullName,
        CancellationToken ct) {
        // UNASSIGNED is the invariant-safe starting state before a test creates
        // household, membership, or residence relationships.
        var citizen = new Citizen {
            NationalId = UniqueNationalId(),
            FullName = fullName,
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "OTHER",
            Status = "UNASSIGNED",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync(ct);

        return citizen;
    }

    public static string UniqueNationalId() {
        // Combine UTC ticks with an interlocked counter to avoid collisions
        // when fixtures are created concurrently.
        var counter = Interlocked.Increment(ref _nationalIdCounter);
        var value = (DateTime.UtcNow.Ticks + counter) % 1_000_000_000_000;
        return value.ToString("D12");
    }

    public static string Code(string prefix) {
        // Keep generated identifiers within the schema's maximum length while
        // retaining a readable prefix for troubleshooting.
        return $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 1 + 24, 30)];
    }
}

internal sealed class RecordingAuditService : IAuditService {
    // In-memory audit collections let service tests assert successful and
    // failure events without requiring audit persistence for every scenario.
    public List<AuditEntry> SuccessEntries { get; } = new();
    public List<AuditFailureEntry> FailureEntries { get; } = new();

    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default) {
        // Preserve the original entry so tests can inspect its correlation and
        // actor metadata after the service call.
        SuccessEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogFailureIndependentAsync(
        string connectionString,
        AuditFailureEntry entry,
        CancellationToken cancellationToken = default) {
        FailureEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogLoginFailureAsync(
        AuditFailureEntry entry,
        CancellationToken cancellationToken = default) {
        FailureEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogFailureIndependentAsync(
        AuditFailureEntry entry,
        CancellationToken cancellationToken = default) {
        FailureEntries.Add(entry);
        return Task.CompletedTask;
    }
}