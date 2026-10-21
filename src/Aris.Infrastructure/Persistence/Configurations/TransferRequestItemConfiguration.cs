using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.TransferRequestItem</c> (<c>D5</c>).</summary>
public sealed class TransferRequestItemConfiguration : IEntityTypeConfiguration<TransferRequestItem>
{
    public void Configure(EntityTypeBuilder<TransferRequestItem> builder)
    {
        builder.ToTable("TransferRequestItem");

        builder.HasKey(x => x.ItemId);

        builder.Property(x => x.ItemId)
            .HasColumnType("int")
            .UseIdentityColumn();

        builder.Property(x => x.RequestId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.CitizenId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.SourceMembershipId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.SourceResidenceId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnType("varchar(10)")
            .IsRequired()
            .HasDefaultValue("PENDING");

        builder.Property(x => x.RejectReason)
            .HasColumnType("nvarchar(300)");

        builder.HasOne<TransferRequest>()
            .WithMany()
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Citizen>()
            .WithMany()
            .HasForeignKey(x => x.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite FKs: the source tenure must belong to the same citizen as the item.
        // The alternate keys on HouseholdMembership / Residence make these targets valid.
        builder.HasOne<HouseholdMembership>()
            .WithMany()
            .HasForeignKey(x => new { x.SourceMembershipId, x.CitizenId })
            .HasPrincipalKey(x => new { x.MembershipId, x.CitizenId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Residence>()
            .WithMany()
            .HasForeignKey(x => new { x.SourceResidenceId, x.CitizenId })
            .HasPrincipalKey(x => new { x.ResidenceId, x.CitizenId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RequestId, x.CitizenId })
            .IsUnique()
            .HasDatabaseName("UQ_TRI_Request_Citizen");

        // UX_TRI_OneOpenPerCitizen: one PENDING item per citizen across all requests.
        builder.HasIndex(x => x.CitizenId)
            .IsUnique()
            .HasDatabaseName("UX_TRI_OneOpenPerCitizen")
            .HasFilter("[Status] = 'PENDING'");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
