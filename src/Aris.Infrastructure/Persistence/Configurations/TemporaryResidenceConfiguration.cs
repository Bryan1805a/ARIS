using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

public sealed class TemporaryResidenceConfiguration : IEntityTypeConfiguration<TemporaryResidence>
{
    public void Configure(EntityTypeBuilder<TemporaryResidence> builder)
    {
        builder.ToTable("TemporaryResidence");

        builder.HasKey(x => x.TempResId);

        builder.Property(x => x.TempResId)
            .HasColumnType("int")
            .UseIdentityColumn();

        builder.Property(x => x.CitizenId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.AddressId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.StayReason)
            .HasColumnType("nvarchar(200)")
            .IsRequired();

        builder.Property(x => x.HostInfo)
            .HasColumnType("nvarchar(200)");

        builder.Property(x => x.Status)
            .HasColumnType("varchar(10)")
            .IsRequired()
            .HasDefaultValue("ACTIVE");

        builder.Property(x => x.Notes)
            .HasColumnType("nvarchar(500)");

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

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasOne<Citizen>()
            .WithMany()
            .HasForeignKey(x => x.CitizenId)
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
    }
}

