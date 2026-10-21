using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.Residence</c>; append-only (<c>D3</c>).</summary>
public sealed class ResidenceConfiguration : IEntityTypeConfiguration<Residence>
{
    public void Configure(EntityTypeBuilder<Residence> builder)
    {
        builder.ToTable("Residence", table => table.HasTrigger("trg_Residence_Guard"));

        builder.HasKey(x => x.ResidenceId);

        builder.Property(x => x.ResidenceId)
            .HasColumnType("int")
            .UseIdentityColumn();

        // Target of the composite FK from TransferRequestItem (residence + citizen).
        builder.HasAlternateKey(x => new { x.ResidenceId, x.CitizenId })
            .HasName("UQ_Res_Id_Citizen");

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

        builder.Property(x => x.CorrectsResidenceId)
            .HasColumnType("int");

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

        builder.HasOne<TransferRequest>()
            .WithMany()
            .HasForeignKey(x => x.OpenedByRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Residence>()
            .WithMany()
            .HasForeignKey(x => x.CorrectsResidenceId)
            .OnDelete(DeleteBehavior.Restrict);

        // UX_Res_OneActivePerCitizen: one active residence system-wide.
        builder.HasIndex(x => x.CitizenId)
            .IsUnique()
            .HasDatabaseName("UX_Res_OneActivePerCitizen")
            .HasFilter("[Status] = 'ACTIVE'");

        builder.HasIndex(x => new { x.AddressId, x.Status })
            .HasDatabaseName("IX_Res_Address");

        builder.HasIndex(x => new { x.CitizenId, x.StartDate })
            .HasDatabaseName("IX_Res_Citizen")
            .IncludeProperties(x => new { x.EndDate, x.Status, x.AddressId });

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
