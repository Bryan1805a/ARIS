using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.TransferRequest</c> (<c>D1</c>, <c>D5</c>, <c>D6</c>).</summary>
public sealed class TransferRequestConfiguration : IEntityTypeConfiguration<TransferRequest>
{
    public void Configure(EntityTypeBuilder<TransferRequest> builder)
    {
        builder.ToTable("TransferRequest", table => table.HasTrigger("trg_TransferRequest_Frozen"));

        builder.HasKey(x => x.RequestId);

        builder.Property(x => x.RequestId)
            .HasColumnType("int")
            .UseIdentityColumn();

        builder.Property(x => x.RequestType)
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.SourceHouseholdId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.DestinationHouseholdId)
            .HasColumnType("int");

        builder.Property(x => x.DestinationAddressId)
            .HasColumnType("int");

        builder.Property(x => x.EffectiveDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.NewHeadCitizenId)
            .HasColumnType("int");

        builder.Property(x => x.SuccessorHeadCitizenId)
            .HasColumnType("int");

        builder.Property(x => x.ResultHouseholdId)
            .HasColumnType("int");

        builder.Property(x => x.Status)
            .HasColumnType("varchar(10)")
            .IsRequired()
            .HasDefaultValue("PENDING");

        builder.Property(x => x.RequestedBy)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.RequestedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.DecidedBy)
            .HasColumnType("int");

        builder.Property(x => x.DecidedAt)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.DecisionNote)
            .HasColumnType("nvarchar(500)");

        builder.HasOne<Household>()
            .WithMany()
            .HasForeignKey(x => x.SourceHouseholdId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Household>()
            .WithMany()
            .HasForeignKey(x => x.DestinationHouseholdId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Household>()
            .WithMany()
            .HasForeignKey(x => x.ResultHouseholdId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Address>()
            .WithMany()
            .HasForeignKey(x => x.DestinationAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Citizen>()
            .WithMany()
            .HasForeignKey(x => x.NewHeadCitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Citizen>()
            .WithMany()
            .HasForeignKey(x => x.SuccessorHeadCitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.RequestedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.DecidedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.Status, x.RequestedAt })
            .HasDatabaseName("IX_TR_Status");

        builder.HasIndex(x => x.SourceHouseholdId)
            .HasDatabaseName("IX_TR_SourceHH");

        builder.HasIndex(x => x.DestinationHouseholdId)
            .HasDatabaseName("IX_TR_DestHH")
            .HasFilter("[DestinationHouseholdId] IS NOT NULL");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
