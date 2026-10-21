using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.HouseholdAddress</c>; append-only (<c>D3</c>, <c>D4</c>).</summary>
public sealed class HouseholdAddressConfiguration : IEntityTypeConfiguration<HouseholdAddress>
{
    public void Configure(EntityTypeBuilder<HouseholdAddress> builder)
    {
        builder.ToTable("HouseholdAddress", table => table.HasTrigger("trg_HouseholdAddress_Guard"));

        builder.HasKey(x => x.HouseholdAddressId);

        builder.Property(x => x.HouseholdAddressId)
            .HasColumnType("int")
            .UseIdentityColumn();

        builder.Property(x => x.HouseholdId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.AddressId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnType("date");

        builder.Property(x => x.Status)
            .HasColumnType("varchar(10)")
            .IsRequired()
            .HasDefaultValue("ACTIVE");

        builder.Property(x => x.CreatedBy)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.EndedBy)
            .HasColumnType("int");

        builder.Property(x => x.EndedAt)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.OpenedByRequestId)
            .HasColumnType("int");

        builder.Property(x => x.CorrectsHouseholdAddressId)
            .HasColumnType("int");

        builder.HasOne<Household>()
            .WithMany()
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Address>()
            .WithMany()
            .HasForeignKey(x => x.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.EndedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TransferRequest>()
            .WithMany()
            .HasForeignKey(x => x.OpenedByRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<HouseholdAddress>()
            .WithMany()
            .HasForeignKey(x => x.CorrectsHouseholdAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        // A household has at most one ACTIVE address (UX_HA_OneActivePerHousehold).
        builder.HasIndex(x => x.HouseholdId)
            .IsUnique()
            .HasDatabaseName("UX_HA_OneActivePerHousehold")
            .HasFilter("[Status] = 'ACTIVE'");

        builder.HasIndex(x => new { x.AddressId, x.Status })
            .HasDatabaseName("IX_HA_Address");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
