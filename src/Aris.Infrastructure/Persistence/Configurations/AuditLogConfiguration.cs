using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.AuditLog</c>; append-only, no <c>rowversion</c> (<c>D7</c>).</summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog", table => table.HasTrigger("trg_AuditLog_AppendOnly"));

        builder.HasKey(x => x.AuditLogId);

        builder.Property(x => x.AuditLogId)
            .HasColumnType("bigint")
            .UseIdentityColumn();

        builder.Property(x => x.ActorUserId)
            .HasColumnType("int");

        builder.Property(x => x.AttemptedUsername)
            .HasColumnType("nvarchar(50)");

        builder.Property(x => x.Action)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnType("int");

        builder.Property(x => x.OccurredAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.Result)
            .HasColumnType("varchar(10)")
            .IsRequired();

        builder.Property(x => x.Details)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CorrelationId)
            .HasColumnType("uniqueidentifier");

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt })
            .HasDatabaseName("IX_Audit_Entity");

        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAt })
            .HasDatabaseName("IX_Audit_Actor");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_Audit_Correlation")
            .HasFilter("[CorrelationId] IS NOT NULL");
    }
}
