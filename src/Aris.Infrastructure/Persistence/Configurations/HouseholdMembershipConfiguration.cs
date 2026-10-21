using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.HouseholdMembership</c>; append-only (<c>D3</c>).</summary>
public sealed class HouseholdMembershipConfiguration : IEntityTypeConfiguration<HouseholdMembership>
{
    public void Configure(EntityTypeBuilder<HouseholdMembership> builder)
    {
        builder.ToTable("HouseholdMembership", table => table.HasTrigger("trg_Membership_Guard"));

        builder.HasKey(x => x.MembershipId);

        builder.Property(x => x.MembershipId)
            .HasColumnType("int")
            .UseIdentityColumn();

        // Target of the composite FK from TransferRequestItem (membership + citizen).
        builder.HasAlternateKey(x => new { x.MembershipId, x.CitizenId })
            .HasName("UQ_HM_Id_Citizen");

        builder.Property(x => x.CitizenId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.HouseholdId)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.Role)
            .HasColumnType("varchar(10)")
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

        builder.Property(x => x.CorrectsMembershipId)
            .HasColumnType("int");

        builder.HasOne<Citizen>()
            .WithMany()
            .HasForeignKey(x => x.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Household>()
            .WithMany()
            .HasForeignKey(x => x.HouseholdId)
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

        builder.HasOne<HouseholdMembership>()
            .WithMany()
            .HasForeignKey(x => x.CorrectsMembershipId)
            .OnDelete(DeleteBehavior.Restrict);

        // UX_HM_OneActivePerCitizen: one active membership system-wide.
        builder.HasIndex(x => x.CitizenId)
            .IsUnique()
            .HasDatabaseName("UX_HM_OneActivePerCitizen")
            .HasFilter("[Status] = 'ACTIVE'");

        // UX_HM_OneActiveHeadPerHousehold: one active head per household.
        builder.HasIndex(x => x.HouseholdId)
            .IsUnique()
            .HasDatabaseName("UX_HM_OneActiveHeadPerHousehold")
            .HasFilter("[Status] = 'ACTIVE' AND [Role] = 'HEAD'");

        builder.HasIndex(x => new { x.HouseholdId, x.Status })
            .HasDatabaseName("IX_HM_Household")
            .IncludeProperties(x => new { x.CitizenId, x.Role });

        builder.HasIndex(x => new { x.CitizenId, x.StartDate })
            .HasDatabaseName("IX_HM_Citizen");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
