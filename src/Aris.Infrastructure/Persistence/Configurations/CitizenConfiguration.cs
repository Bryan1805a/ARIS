using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.Citizen</c> (<c>D2</c>).</summary>
public sealed class CitizenConfiguration : IEntityTypeConfiguration<Citizen>
{
    public void Configure(EntityTypeBuilder<Citizen> builder)
    {
        builder.ToTable("Citizen");

        builder.HasKey(x => x.CitizenId);

        builder.Property(x => x.CitizenId)
            .HasColumnType("int")
            .UseIdentityColumn();

        builder.Property(x => x.NationalId)
            .HasColumnType("char(12)")
            .IsRequired();

        builder.HasIndex(x => x.NationalId)
            .IsUnique()
            .HasDatabaseName("UQ_Citizen_NationalId");

        builder.Property(x => x.FullName)
            .HasColumnType("nvarchar(100)")
            .IsRequired();

        builder.HasIndex(x => x.FullName)
            .HasDatabaseName("IX_Citizen_FullName");

        builder.Property(x => x.DateOfBirth)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasColumnType("varchar(10)")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnType("varchar(12)")
            .IsRequired()
            .HasDefaultValue("UNASSIGNED");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Citizen_Status");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.DeactivatedAt)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
