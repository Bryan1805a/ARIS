using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.SystemConfiguration</c>; no <c>rowversion</c>.</summary>
public sealed class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        // D16: no migrations. The table has no rowversion and is not an EntityBase.
        builder.ToTable("SystemConfiguration");

        builder.HasKey(x => x.ConfigKey);

        builder.Property(x => x.ConfigKey)
            .HasColumnType("varchar(100)");

        builder.Property(x => x.ConfigValue)
            .HasColumnType("nvarchar(500)")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnType("nvarchar(300)");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedBy)
            .HasColumnType("int");

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
