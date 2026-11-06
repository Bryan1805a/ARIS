using Aris.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

public sealed class CivilStatusRecordConfiguration : IEntityTypeConfiguration<CivilStatusRecord> {
    public void Configure(EntityTypeBuilder<CivilStatusRecord> builder) {
        builder.ToTable("CivilStatusRecord");

        builder.HasKey(x => x.CivilStatusId);

        builder.Property(x => x.CivilStatusId)
            .HasColumnType("int")
            .UseIdentityColumn();

        builder.Property(x => x.RecordType)
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.CitizenId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.RelatedCitizenId)
            .HasColumnType("int");

        builder.Property(x => x.AddressId)
            .HasColumnType("int");

        builder.Property(x => x.EventDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.RegistrationDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.DocumentNumber)
            .HasColumnType("varchar(30)");

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

        builder.Property(x => x.CancelledBy)
            .HasColumnType("int");

        builder.Property(x => x.CancelledAt)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.CancelReason)
            .HasColumnType("nvarchar(300)");

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasOne<Citizen>()
            .WithMany()
            .HasForeignKey(x => x.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Citizen>()
            .WithMany()
            .HasForeignKey(x => x.RelatedCitizenId)
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
            .HasForeignKey(x => x.CancelledBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}