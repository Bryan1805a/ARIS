using Aris.Infrastructure.Resilience;

using Microsoft.Data.SqlClient;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Proves the ADR-03 / D9 lineage shape: an INACTIVE address with
/// ReplacedByAddressId resolves to its ACTIVE successor through
/// dbo.vw_AddressCanonical.
/// </summary>
public sealed class AddressCanonicalTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Retiring_an_address_resolves_through_the_canonical_view() {
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        await using var connection = await SqlResilience.OpenAsync(
            TestDatabase.ConnectionString, cancellationToken: Token);

        if (!await TestDatabase.ObjectExistsAsync(
                connection, "dbo", "vw_AddressCanonical", Token)) {
            Assert.Skip(
                "dbo.vw_AddressCanonical is not deployed. Run .\\scripts\\deploy-database.ps1 first.");
        }

        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(Token);

        try {
            var newAddressId = await InsertAddressAsync(
                connection, transaction, "ACTIVE", null, null);

            var oldAddressId = await InsertAddressAsync(
                connection, transaction, "INACTIVE", newAddressId,
                new DateOnly(2026, 12, 31));

            var canonical = await ReadCanonicalAsync(
                connection, transaction, oldAddressId);

            Assert.Equal(newAddressId, canonical.AddressId);
            Assert.Equal("ACTIVE", canonical.Status);

            var self = await ReadCanonicalAsync(
                connection, transaction, newAddressId);

            Assert.Equal(newAddressId, self.AddressId);
        } finally {
            await transaction.RollbackAsync(Token);
        }
    }

    private static async Task<int> InsertAddressAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string status,
        int? replacedBy,
        DateOnly? validTo) {
        var code = $"IT-ADDR-{Guid.NewGuid():N}";
        code = code[..30];

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.Address
                (AddressCode, AddressLine, Ward, District, Province,
                 Status, ValidFrom, ValidTo, ReplacedByAddressId, DeactivatedAt)
            OUTPUT INSERTED.AddressId
            VALUES
                (@code, N'1 Test Street', N'Test Ward', NULL, N'Test Province',
                 @status, '2026-01-01', @validTo, @replacedBy,
                 CASE WHEN @status = 'INACTIVE' THEN SYSUTCDATETIME() ELSE NULL END);
            """;

        command.Parameters.AddWithValue("@code", code);
        command.Parameters.AddWithValue("@status", status);

        command.Parameters.Add("@validTo", System.Data.SqlDbType.Date).Value =
            validTo.HasValue
                ? (object)validTo.Value.ToDateTime(TimeOnly.MinValue)
                : DBNull.Value;

        command.Parameters.Add("@replacedBy", System.Data.SqlDbType.Int).Value =
            replacedBy.HasValue ? (object)replacedBy.Value : DBNull.Value;

        var result = await command.ExecuteScalarAsync(Token);
        return (int)(result
            ?? throw new InvalidOperationException("Address insert returned no identity."));
    }

    private static async Task<(int AddressId, string Status)> ReadCanonicalAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int addressId) {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT CanonicalAddressId, CanonicalStatus
            FROM dbo.vw_AddressCanonical
            WHERE AddressId = @id;
            """;
        command.Parameters.AddWithValue("@id", addressId);

        await using var reader = await command.ExecuteReaderAsync(Token);

        if (!await reader.ReadAsync(Token)) {
            throw new InvalidOperationException(
                $"No canonical row for address {addressId}.");
        }

        return (reader.GetInt32(0), reader.GetString(1));
    }
}