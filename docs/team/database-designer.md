# Role Brief - Database Designer

**Document Code:** `WKF-TEAM-DB-v2.0`
**Owner:** `<name>`
**Primary task (Phase 1):** `P1-DB2` - `ArisDbContext` & EF Core mappings
**Companion docs:** [`../workflow/phase1-tasks.md`](../workflow/phase1-tasks.md) §2 · [`../database/design-rationale.md`](../database/design-rationale.md) · [`../database/triggers-and-security.md`](../database/triggers-and-security.md)

> [!IMPORTANT]
> **You are on the critical path.** Everyone else is blocked until the entity classes and
> the `ArisDbContext` exist. Deliver `P1-DB2` early and in small pushes so Coders 1-3 can
> start. If you must choose what to do first, do §4 steps A-D and tell the team.

---

## 1. What You Own

| Area | Deliverable |
|---|---|
| Domain entities | `src/Aris.Domain/Entities/*.cs` - 11 classes matching the schema |
| EF Core context & mappings | `src/Aris.Infrastructure/Persistence/` |
| Audit persistence | `src/Aris.Infrastructure/Auditing/AuditService.cs` (ADO.NET + `SqlResilience`) |
| Composition | Register the context + audit service in `AddInfrastructure` |
| Tests | Unit tests for mapping defaults; integration tests for the DB rules |

The schema itself is **already written and deployed** ([`../aris_schema_v2.sql`](../aris_schema_v2.sql)).
You are mapping C# to it - you do **not** design new tables, and you do **not** use EF migrations.

---

## 2. Read First

1. [`../architecture/domain-model.md`](../architecture/domain-model.md) - every entity and its columns.
2. [`../database/triggers-and-security.md`](../database/triggers-and-security.md) §5 - the 5 EF Core mapping requirements.
3. [`../aris_schema_v2.sql`](../aris_schema_v2.sql) - the literal source of truth for names and types.
4. [`../standards/coding-conventions.md`](../standards/coding-conventions.md) §3 - the canonical service pattern.

---

## 3. One-Time Setup

```powershell
# Make sure your branch starts from the latest develop
git checkout develop
git pull origin develop
git checkout -b feature/p1-db2
```

---

## 4. `P1-DB2` - Step by Step

### Step A - Add the EF Core SQL Server provider

The repository currently has **no** EF Core SQL Server provider. Add it to Infrastructure,
pinned to `10.0.12` so it matches the existing `Microsoft.Extensions.*` and
`Microsoft.EntityFrameworkCore.InMemory` versions:

```powershell
dotnet add src/Aris.Infrastructure/Aris.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.12
```

> [!NOTE]
> `Microsoft.Data.SqlClient` is already referenced by Infrastructure - do not add it again.

### Step B - Create the 11 domain entities

Put them in `src/Aris.Domain/Entities/`. **Nine** of them are mutable and must derive from
[`EntityBase`](../../src/Aris.Domain/Common/EntityBase.cs) (for `RowVersion`). `SystemConfiguration`
and `AuditLog` have **no** `RowVersion`, so they do **not** derive from it.

| Entity | Derives `EntityBase`? |
|---|:---:|
| `Citizen`, `Household`, `Address`, `HouseholdAddress`, `HouseholdMembership`, `Residence`, `TransferRequest`, `TransferRequestItem`, `UserAccount` | **Yes** (9) |
| `SystemConfiguration`, `AuditLog` | **No** |

Use `DateOnly` for SQL `date` columns and `DateTime` for `datetime2`.

<details>
<summary><b>Template: <code>Citizen.cs</code></b></summary>

```csharp
using Aris.Domain.Common;

namespace Aris.Domain.Entities;

public class Citizen : EntityBase
{
    public int CitizenId { get; set; }

    /// <summary>char(12) - exactly 12 numeric digits (CK_Citizen_NationalId).</summary>
    public string NationalId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }

    /// <summary>MALE | FEMALE | OTHER (CK_Citizen_Gender).</summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>UNASSIGNED | ACTIVE | INACTIVE (CK_Citizen_Status). Starts UNASSIGNED.</summary>
    public string Status { get; set; } = "UNASSIGNED";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}
```
</details>

<details>
<summary><b>Template: <code>Address.cs</code> (has a computed column)</b></summary>

```csharp
using Aris.Domain.Common;

namespace Aris.Domain.Entities;

public class Address : EntityBase
{
    public int AddressId { get; set; }
    public string AddressCode { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;

    /// <summary>Nullable: legacy addresses only.</summary>
    public string? District { get; set; }

    public string Province { get; set; } = string.Empty;

    /// <summary>Database-computed (CONCAT_WS). Never set this in C#.</summary>
    public string AddressText { get; private set; } = string.Empty;

    public string Status { get; set; } = "ACTIVE";
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public int? ReplacedByAddressId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}
```
</details>

<details>
<summary><b>Template: <code>AuditLog.cs</code> (no RowVersion)</b></summary>

```csharp
namespace Aris.Domain.Entities;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public int? ActorUserId { get; set; }
    public string? AttemptedUsername { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public DateTime OccurredAt { get; set; }

    /// <summary>SUCCESS | FAILURE (CK_Audit_Result).</summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>JSON payload; the DB enforces ISJSON (CK_Audit_Details).</summary>
    public string? Details { get; set; }

    public Guid? CorrelationId { get; set; }
}
```
</details>

The remaining eight entities follow the same pattern exactly as
[`../architecture/domain-model.md`](../architecture/domain-model.md) §2 describes. Copy the
column names **verbatim** - EF maps by property name.

### Step C - Create `ArisDbContext`

`src/Aris.Infrastructure/Persistence/ArisDbContext.cs`:

```csharp
using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Persistence;

public class ArisDbContext : DbContext
{
    public ArisDbContext(DbContextOptions<ArisDbContext> options) : base(options) { }

    public DbSet<Citizen> Citizens => Set<Citizen>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<HouseholdAddress> HouseholdAddresses => Set<HouseholdAddress>();
    public DbSet<HouseholdMembership> HouseholdMemberships => Set<HouseholdMembership>();
    public DbSet<Residence> Residences => Set<Residence>();
    public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();
    public DbSet<TransferRequestItem> TransferRequestItems => Set<TransferRequestItem>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArisDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### Step D - Entity configurations (the 5 critical details)

One file per entity in `src/Aris.Infrastructure/Persistence/Configurations/`, implementing
`IEntityTypeConfiguration<T>`. These four details are graded in review:

**(1) Trigger tables must declare the trigger.** Otherwise EF emits an `OUTPUT` clause that
SQL Server rejects:

```csharp
builder.ToTable("Residence",          t => t.HasTrigger("trg_Residence_Guard"));
builder.ToTable("HouseholdMembership", t => t.HasTrigger("trg_Membership_Guard"));
builder.ToTable("HouseholdAddress",    t => t.HasTrigger("trg_HouseholdAddress_Guard"));
builder.ToTable("TransferRequest",     t => t.HasTrigger("trg_TransferRequest_Frozen"));
builder.ToTable("AuditLog",            t => t.HasTrigger("trg_AuditLog_AppendOnly"));
```

**(2) `RowVersion` on the 9 mutable tables only:**

```csharp
builder.Property(e => e.RowVersion).IsRowVersion();
```

**(3) The `Address.AddressText` computed column** (`AddressConfiguration.cs`):

```csharp
builder.Property(a => a.AddressText)
       .HasComputedColumnSql("CONCAT_WS(N', ', AddressLine, Ward, District, Province)");
```

**(4) `TransferRequestItem` composite keys** - the membership/residence must belong to the
same citizen as the item:

```csharp
builder.HasOne<HouseholdMembership>()
       .WithMany()
       .HasForeignKey(i => new { i.SourceMembershipId, i.CitizenId })
       .OnDelete(DeleteBehavior.Restrict);

builder.HasOne<Residence>()
       .WithMany()
       .HasForeignKey(i => new { i.SourceResidenceId, i.CitizenId })
       .OnDelete(DeleteBehavior.Restrict);
```

**(5) No cascade deletes anywhere** - the app role denies `DELETE`, so every relationship
uses `DeleteBehavior.Restrict` (or `NoAction`).

<details>
<summary><b>Full example: <code>ResidenceConfiguration.cs</code></b></summary>

```csharp
using Aris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aris.Infrastructure.Persistence.Configurations;

public sealed class ResidenceConfiguration : IEntityTypeConfiguration<Residence>
{
    public void Configure(EntityTypeBuilder<Residence> builder)
    {
        builder.ToTable("Residence", t => t.HasTrigger("trg_Residence_Guard"));
        builder.HasKey(r => r.ResidenceId);

        builder.Property(r => r.Status).HasMaxLength(10).IsRequired();
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasOne<Citizen>().WithMany()
               .HasForeignKey(r => r.CitizenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Address>().WithMany()
               .HasForeignKey(r => r.AddressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany()
               .HasForeignKey(r => r.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany()
               .HasForeignKey(r => r.EndedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TransferRequest>().WithMany()
               .HasForeignKey(r => r.OpenedByRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Residence>().WithMany()
               .HasForeignKey(r => r.CorrectsResidenceId).OnDelete(DeleteBehavior.Restrict);
    }
}
```
</details>

### Step E - Register everything in `AddInfrastructure`

Edit [`src/Aris.Infrastructure/DependencyInjection.cs`](../../src/Aris.Infrastructure/DependencyInjection.cs):

```csharp
using Aris.Application.Auditing;
using Aris.Infrastructure.Auditing;
using Aris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aris.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ArisDb")
            ?? throw new InvalidOperationException(
                "Connection string 'ArisDb' is not configured. See README > Configuration & Secrets.");

        services.AddDbContext<ArisDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IAuditService, AuditService>();
        return services;
    }
}
```

### Step F - Implement `AuditService`

`src/Aris.Infrastructure/Auditing/AuditService.cs`. It implements
[`IAuditService`](../../src/Aris.Application/Auditing/IAuditService.cs):

- `LogAsync` writes **inside the caller's transaction** (so it commits or rolls back with the business data).
- `LogFailureIndependentAsync` writes on a **fresh connection via `SqlResilience`** so a `FAILURE` row survives a rollback.

<details>
<summary><b>Template: <code>AuditService.cs</code></b></summary>

```csharp
using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Resilience;

namespace Aris.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly ArisDbContext _context;

    public AuditService(ArisDbContext context) => _context = context;

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (!AuditEntryValidator.TryValidate(entry, out var errors))
            throw new ValidationException(errors);

        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = entry.ActorUserId,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Result = "SUCCESS",
            Details = entry.DetailsJson,
            CorrelationId = entry.CorrelationId,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogFailureIndependentAsync(
        string connectionString, AuditFailureEntry entry, CancellationToken cancellationToken = default)
    {
        if (!AuditEntryValidator.TryValidate(entry, out var errors))
            throw new ValidationException(errors);

        await using var connection = await SqlResilience.OpenAsync(
            connectionString, cancellationToken: cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.AuditLog
                (ActorUserId, AttemptedUsername, Action, EntityType, EntityId, Result, Details, CorrelationId)
            VALUES
                (@actor, @username, @action, @entityType, @entityId, 'FAILURE', @details, @correlationId);
            """;
        command.Parameters.AddWithValue("@actor",        (object?)entry.ActorUserId        ?? DBNull.Value);
        command.Parameters.AddWithValue("@username",     (object?)entry.AttemptedUsername ?? DBNull.Value);
        command.Parameters.AddWithValue("@action",       entry.Action);
        command.Parameters.AddWithValue("@entityType",   entry.EntityType);
        command.Parameters.AddWithValue("@entityId",     (object?)entry.EntityId         ?? DBNull.Value);
        command.Parameters.AddWithValue("@details",      (object?)entry.DetailsJson      ?? DBNull.Value);
        command.Parameters.AddWithValue("@correlationId",(object?)entry.CorrelationId    ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
```
</details>

### Step G - Tests

- **Unit (fast, no DB):** once the context exists, add tests in `tests/Aris.UnitTests` deriving
  from [`InMemoryDatabaseTestBase<TContext>`](../../tests/Aris.UnitTests/Infrastructure/InMemoryDatabaseTestBase.cs).
  Use these only for mapping defaults / "did we write the right rows".
- **Integration (real DB):** the existing `tests/Aris.Tests` already proves the schema. Run it
  with `ARIS_TEST_CONNECTION` set to confirm your mappings match the deployed tables.

> [!CAUTION]
> Do **not** write EF migrations (`dotnet ef migrations add ...`). The DDL script is the single
> source of truth and the Team Leader deploys it. Migrations would create a second, conflicting history.

---

## 5. Definition of Done for `P1-DB2`

- [ ] 11 entity classes exist; the 9 mutable ones derive `EntityBase`; `AuditLog`/`SystemConfiguration` do not.
- [ ] `ArisDbContext` exposes 11 `DbSet`s and applies configurations from its assembly.
- [ ] The 5 trigger tables call `.HasTrigger(...)`.
- [ ] `.IsRowVersion()` on the 9 mutable tables only.
- [ ] `Address.AddressText` mapped with `.HasComputedColumnSql(...)`.
- [ ] `TransferRequestItem` maps both composite FKs to `(id, CitizenId)`.
- [ ] No cascade deletes (`Restrict`/`NoAction` everywhere).
- [ ] `AddInfrastructure` registers the context (with `EnableRetryOnFailure`) and `IAuditService`.
- [ ] `AuditService.LogAsync` shares the transaction; `LogFailureIndependentAsync` uses `SqlResilience` on a new connection.
- [ ] `dotnet build Aris.slnx` → 0 errors, 0 warnings; `dotnet test Aris.slnx` → 0 failed.
- [ ] PR reviewed against [`../standards/pr-review-checklist.md`](../standards/pr-review-checklist.md) §2.2.

---

## 6. Traps That Will Get Your PR Rejected

| Trap | Why it fails |
|---|---|
| Adding `Citizen.HouseholdId` or `Household.CurrentAddressId` | Those columns do not exist. Membership/address live in `HouseholdMembership`/`HouseholdAddress`. |
| `.IsRowVersion()` on `AuditLog` or `SystemConfiguration` | Those two tables have no `rowversion` column. |
| Forgetting `.HasTrigger(...)` | EF emits `OUTPUT` and every insert/update on that table errors. |
| Cascade delete behaviour | The `aris_app` role denies `DELETE`; cascades silently break. |
| Generating EF migrations | Conflicts with the canonical DDL script. |
| `DateTime` for a SQL `date` column | Unnecessary; use `DateOnly`. |

---

## 7. When You Are Done

Ping the team (they are waiting), then move to your Phase-2+ work: seed data, constraint
integration tests, and verifying optimistic concurrency (`D14`). See
[`../workflow/phase1-tasks.md`](../workflow/phase1-tasks.md) and
[`../workflow/work-breakdown.md`](../workflow/work-breakdown.md) §3 (RACI).
