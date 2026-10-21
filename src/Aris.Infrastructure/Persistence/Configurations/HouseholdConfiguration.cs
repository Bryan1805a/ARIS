using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.Household</c>; the address lives in <c>HouseholdAddress</c> (<c>D4</c>).</summary>
public sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("Household");

        builder.HasKey(x => x.HouseholdId);

        builder.Property(x => x.HouseholdId)
            .HasColumnType("int")
            .UseIdentityColumn();

        builder.Property(x => x.Status)
            .HasColumnType("varchar(10)")
            .IsRequired()
            .HasDefaultValue("ACTIVE");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.InactivatedAt)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
