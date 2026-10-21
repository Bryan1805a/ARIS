using Aris.Domain.Common;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Aris.UnitTests.Infrastructure;

/// <summary>
/// Verifies the EF Core model against the canonical DDL without opening a connection.
/// </summary>
/// <remarks>
/// These assertions protect the mapping rules in
/// <c>docs/appendix/B-database-reference.md</c> §B.6. They do <b>not</b> prove that SQL
/// Server enforces anything: triggers, filtered indexes and CHECK constraints are
/// verified in <c>Aris.Tests</c> against the real database.
/// </remarks>
public sealed class ArisDbContextModelTests
{
    private static ArisDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ArisDbContext>()
            // Never opened: building the model does not connect.
            .UseSqlServer("Server=localhost,1433;Database=ArisDb;User Id=sa;Password=unused;TrustServerCertificate=True;")
            .Options;

        return new ArisDbContext(options);
    }

    [Fact]
    public void Model_maps_the_eleven_tables_from_the_DDL()
    {
        using var context = CreateContext();

        var tables = context.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .ToHashSet(StringComparer.Ordinal);

        var expected = new[]
        {
            "UserAccount", "SystemConfiguration", "Citizen", "Address", "Household",
            "TransferRequest", "HouseholdAddress", "HouseholdMembership", "Residence",
            "TransferRequestItem", "AuditLog"
        };

        Assert.Equal(expected.Length, tables.Count);
        Assert.True(tables.SetEquals(expected),
            $"Mapped tables differ from the DDL. Actual: {string.Join(", ", tables.Order())}");
    }

    [Fact]
    public void RowVersion_is_mapped_on_the_nine_mutable_tables_only()
    {
        using var context = CreateContext();

        var tablesWithRowVersion = context.Model.GetEntityTypes()
            .Where(e => e.FindProperty(nameof(EntityBase.RowVersion)) is not null)
            .Select(e => e.GetTableName())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(9, tablesWithRowVersion.Count);
        Assert.DoesNotContain("AuditLog", tablesWithRowVersion);
        Assert.DoesNotContain("SystemConfiguration", tablesWithRowVersion);
    }

    [Fact]
    public void The_five_trigger_tables_declare_their_trigger()
    {
        using var context = CreateContext();

        var expected = new Dictionary<Type, string>
        {
            [typeof(Residence)] = "trg_Residence_Guard",
            [typeof(HouseholdMembership)] = "trg_Membership_Guard",
            [typeof(HouseholdAddress)] = "trg_HouseholdAddress_Guard",
            [typeof(TransferRequest)] = "trg_TransferRequest_Frozen",
            [typeof(AuditLog)] = "trg_AuditLog_AppendOnly"
        };

        foreach (var (clrType, triggerName) in expected)
        {
            var entityType = context.Model.FindEntityType(clrType);
            Assert.NotNull(entityType);

            var trigger = entityType.GetTriggers().SingleOrDefault();

            Assert.NotNull(trigger);
            Assert.Equal(triggerName, trigger.ModelName);
        }
    }

    [Fact]
    public void AddressText_is_a_database_computed_column()
    {
        using var context = CreateContext();

        var address = context.Model.FindEntityType(typeof(Address));
        Assert.NotNull(address);

        var addressText = address.FindProperty(nameof(Address.AddressText));
        Assert.NotNull(addressText);

        var sql = addressText.GetComputedColumnSql();
        Assert.NotNull(sql);
        Assert.Contains("CONCAT_WS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransferRequestItem_has_the_two_composite_foreign_keys()
    {
        using var context = CreateContext();

        var item = context.Model.FindEntityType(typeof(TransferRequestItem));
        Assert.NotNull(item);

        var compositeTargets = item.GetForeignKeys()
            .Where(fk => fk.Properties.Count == 2)
            .Select(fk => fk.PrincipalEntityType.GetTableName())
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(compositeTargets.SetEquals(new[] { "HouseholdMembership", "Residence" }),
            $"Expected composite FKs to HouseholdMembership and Residence; got {string.Join(", ", compositeTargets)}");
    }

    [Fact]
    public void The_composite_foreign_key_targets_have_alternate_keys()
    {
        using var context = CreateContext();

        AssertHasAlternateKey(context, typeof(HouseholdMembership), "MembershipId", "CitizenId");
        AssertHasAlternateKey(context, typeof(Residence), "ResidenceId", "CitizenId");
    }

    [Fact]
    public void No_foreign_key_cascades()
    {
        using var context = CreateContext();

        var cascades = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade)
            .Select(fk => $"{fk.DeclaringEntityType.GetTableName()} -> {fk.PrincipalEntityType.GetTableName()}")
            .ToList();

        Assert.True(cascades.Count == 0,
            $"The application role denies DELETE, so no FK may cascade. Cascades: {string.Join(", ", cascades)}");
    }

    [Fact]
    public void AuditLog_identity_is_bigint()
    {
        using var context = CreateContext();

        var auditLog = context.Model.FindEntityType(typeof(AuditLog));
        Assert.NotNull(auditLog);

        var id = auditLog.FindProperty(nameof(AuditLog.AuditLogId));
        Assert.NotNull(id);
        Assert.Equal(typeof(long), id.ClrType);
    }

    private static void AssertHasAlternateKey(DbContext context, Type clrType, params string[] properties)
    {
        var entityType = context.Model.FindEntityType(clrType);
        Assert.NotNull(entityType);

        var found = entityType.GetKeys().Any(key =>
            !key.IsPrimaryKey() &&
            key.Properties.Select(p => p.Name).SequenceEqual(properties, StringComparer.Ordinal));

        Assert.True(found,
            $"{clrType.Name} must declare an alternate key on ({string.Join(", ", properties)}) " +
            "to be a valid composite-FK target.");
    }
}
