using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the canonical <c>docs/aris_schema_v2.sql</c> schema.
/// </summary>
/// <remarks>
/// The DDL is authoritative (<c>D16</c>): there are no migrations. Every mapping is
/// configured explicitly in <c>Persistence/Configurations</c> so the model follows the
/// script rather than relying on conventions.
/// </remarks>
public sealed class ArisDbContext : DbContext
{
    public ArisDbContext(DbContextOptions<ArisDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();

    public DbSet<Citizen> Citizens => Set<Citizen>();

    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<Household> Households => Set<Household>();

    public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();

    public DbSet<HouseholdAddress> HouseholdAddresses => Set<HouseholdAddress>();

    public DbSet<HouseholdMembership> HouseholdMemberships => Set<HouseholdMembership>();

    public DbSet<Residence> Residences => Set<Residence>();

    public DbSet<TransferRequestItem> TransferRequestItems => Set<TransferRequestItem>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArisDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
