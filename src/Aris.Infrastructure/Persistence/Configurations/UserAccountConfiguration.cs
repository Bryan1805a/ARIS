using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

/// <summary>Maps <c>dbo.UserAccount</c> (<c>D10</c>).</summary>
public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccount");

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .HasColumnType("int")
            .UseIdentityColumn();

        builder.Property(x => x.Username)
            .HasColumnType("nvarchar(50)")
            .IsRequired();

        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("UQ_UserAccount_Username");

        builder.Property(x => x.PasswordHash)
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasColumnType("nvarchar(100)")
            .IsRequired();

        builder.Property(x => x.Role)
            .HasColumnType("varchar(10)")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnType("varchar(10)")
            .IsRequired()
            .HasDefaultValue("ACTIVE");

        builder.Property(x => x.FailedLoginCount)
            .HasColumnType("int")
            .HasDefaultValue(0);

        builder.Property(x => x.LockedUntil)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.MustChangePassword)
            .HasColumnType("bit")
            .HasDefaultValue(true);

        builder.Property(x => x.PasswordChangedAt)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
