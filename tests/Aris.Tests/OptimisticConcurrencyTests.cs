using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Resilience;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Proves decision <c>D14</c>: a stale <c>rowversion</c> causes
/// <see cref="DbUpdateConcurrencyException"/> instead of silently overwriting a colleague.
/// </summary>
public sealed class OptimisticConcurrencyTests {
    private static int _nationalIdCounter;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Stale_Save_Raises_DbUpdateConcurrencyException() {
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        var connectionString = TestDatabase.ConnectionString!;
        await EnsureSchemaAsync(connectionString);

        var options = new DbContextOptionsBuilder<ArisDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        int citizenId;

        await using (var setup = new ArisDbContext(options)) {
            var citizen = new Citizen {
                NationalId = NationalId(),
                FullName = "Concurrency Original",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Gender = "OTHER",
                Status = "UNASSIGNED",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            setup.Citizens.Add(citizen);
            await setup.SaveChangesAsync(Token);
            citizenId = citizen.CitizenId;
        }

        try {
            // Two independent contexts load the same row with the same RowVersion.
            await using var first = new ArisDbContext(options);
            await using var second = new ArisDbContext(options);

            var firstCopy = await first.Citizens
                .SingleAsync(c => c.CitizenId == citizenId, Token);
            var secondCopy = await second.Citizens
                .SingleAsync(c => c.CitizenId == citizenId, Token);

            // First writer wins and commits a new RowVersion.
            firstCopy.FullName = "Concurrency First";
            firstCopy.UpdatedAt = DateTime.UtcNow;
            await first.SaveChangesAsync(Token);

            // Second writer still holds the old RowVersion; SQL affects zero rows.
            secondCopy.FullName = "Concurrency Second";
            secondCopy.UpdatedAt = DateTime.UtcNow;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => second.SaveChangesAsync(Token));

            // The first write must survive; the stale write must not overwrite it.
            await using var read = new ArisDbContext(options);
            var persisted = await read.Citizens
                .AsNoTracking()
                .SingleAsync(c => c.CitizenId == citizenId, Token);

            Assert.Equal("Concurrency First", persisted.FullName);
        } finally {
            await TryDeleteCitizenAsync(connectionString, citizenId);
        }
    }

    private static async Task EnsureSchemaAsync(string connectionString) {
        await using var connection = await SqlResilience.OpenAsync(
            connectionString, cancellationToken: Token);

        if (!await TestDatabase.SchemaIsDeployedAsync(connection, Token)) {
            Assert.Skip(
                "The ARIS schema is not deployed. Run .\\scripts\\deploy_database.ps1 first.");
        }
    }

    private static async Task TryDeleteCitizenAsync(
        string connectionString,
        int citizenId) {
        try {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM dbo.Citizen WHERE CitizenId = @citizenId;";
            command.Parameters.AddWithValue("@citizenId", citizenId);

            await command.ExecuteNonQueryAsync(Token);
        } catch {
            // DELETE is denied for the least-privilege runtime role. An UNASSIGNED
            // citizen left behind does not violate the Golden Invariant.
        }
    }

    private static string NationalId() {
        var counter = Interlocked.Increment(ref _nationalIdCounter);
        var value = (DateTime.UtcNow.Ticks + counter) % 1_000_000_000_000;
        return value.ToString("D12");
    }
}