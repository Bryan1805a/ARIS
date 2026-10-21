using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.Address</c> (<c>D8</c>, <c>D9</c>).</summary>
public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Address");

        builder.HasKey(x => x.AddressId);

        builder.Property(x => x.AddressId)
            .HasColumnType("int")
            .UseIdentityColumn();

        builder.Property(x => x.AddressCode)
            .HasColumnType("varchar(30)")
            .IsRequired();

        builder.HasIndex(x => x.AddressCode)
            .IsUnique()
            .HasDatabaseName("UQ_Address_Code");

        builder.Property(x => x.AddressLine)
            .HasColumnType("nvarchar(200)")
            .IsRequired();

        builder.Property(x => x.Ward)
            .HasColumnType("nvarchar(100)")
            .IsRequired();

        builder.Property(x => x.District)
            .HasColumnType("nvarchar(100)");

        builder.Property(x => x.Province)
            .HasColumnType("nvarchar(100)")
            .IsRequired();

        // Database-generated display text. Application code must never assign it.
        builder.Property(x => x.AddressText)
            .HasColumnType("nvarchar(500)")
            .HasComputedColumnSql("CONCAT_WS(N', ', AddressLine, Ward, District, Province)");

        builder.Property(x => x.Status)
            .HasColumnType("varchar(10)")
            .IsRequired()
            .HasDefaultValue("ACTIVE");

        builder.Property(x => x.ValidFrom)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.ValidTo)
            .HasColumnType("date");

        builder.Property(x => x.ReplacedByAddressId)
            .HasColumnType("int");

        builder.HasOne<Address>()
            .WithMany()
            .HasForeignKey(x => x.ReplacedByAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.Province, x.Ward })
            .HasDatabaseName("IX_Address_Area");

        builder.HasIndex(x => x.ReplacedByAddressId)
            .HasDatabaseName("IX_Address_ReplacedBy")
            .HasFilter("[ReplacedByAddressId] IS NOT NULL");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.DeactivatedAt)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
