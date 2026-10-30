# Appendix C — Coding Standards

> ARIS 3.0 · [Documentation hub](../README.md) · Companion: [§8](../08-crosscutting-concepts.md) · [§10](../10-quality-requirements.md)

This appendix is the house style. It merges the former coding conventions, AI guardrails,
and PR checklist. The architectural rules themselves live in §8; this page shows how to
write code that obeys them.

---

## C.1 Layer rules recap

| Project | May reference | Must never reference |
|---|---|---|
| `Aris.Domain` | Nothing | Application, Infrastructure, UI, EF Core |
| `Aris.Application` | Domain | WinForms, EF Core provider, `Microsoft.Data.SqlClient` |
| `Aris.Infrastructure` | Application, Domain | WinForms |
| `Aris.UI` | Application; Infrastructure only in `Program.cs` | SQL, `ArisDbContext`, EF Core |

A form calls an Application interface. Its implementation calls a repository-equivalent
(`ArisDbContext`) and `IAuditService`: repository-backed services live in Application,
while services that need `ArisDbContext` or a transaction live in Infrastructure
(`D18`). A domain entity calls nothing.

---

## C.2 Naming and style

| Element | Convention | Example |
|---|---|---|
| Namespace | `Aris.<Layer>.<Module>` | `Aris.Application.Citizens` |
| Class / record | PascalCase | `CitizenService`, `CreateCitizenDto` |
| Interface | `I` + PascalCase | `ICitizenService` |
| Method | PascalCase, `Async` suffix | `CreateCitizenAsync` |
| Private field | `_camelCase` | `_context`, `_audit` |
| Constant | PascalCase | `MaxActionLength` |
| Parameter / local | camelCase | `currentUserId`, `effectiveDate` |
| Database object | Exact DDL name | `HouseholdMembership`, `vw_GoldenInvariantViolations` |
| Audit action | UPPER_SNAKE, ≤ 50 chars | `EXECUTE_TRANSFER` |
| DTO | `...Dto`, `...Criteria`, `...Decision` | `RegisterHouseholdDto` |
| Async return | `Task<Result<T>>` or `Task<Result>` | `Task<Result<int>>` |

Additional rules:

- One public type per file, named after the type.
- File-scoped namespaces.
- Nullable reference types are enabled; do not use `!` to silence a real null risk.
- Prefer `DateOnly` for business dates.
- Keep methods short; extract private helpers rather than nesting.
- Comments explain **why**, not what. Reference requirement or decision IDs where useful.

---

## C.3 Canonical service pattern

Every mutating service follows this shape. Do not invent a variant without updating this
appendix.

> **Placement note (`D18`).** The sample below shows the flow. In this repository a
> service that injects `ArisDbContext` or opens a transaction lives in
> `Aris.Infrastructure` and implements the Application interface; the interface, DTOs and
> validators stay in Application. Repository-backed services
> (`AuthService`, `AccountService`, `ReportService`, `AuditQueryService`) remain in
> Application.

```csharp
public sealed class HouseholdService : IHouseholdService
{
    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;
    private readonly IValidator<RegisterHouseholdDto> _validator;

    public HouseholdService(
        ArisDbContext context,
        IAuditService audit,
        IValidator<RegisterHouseholdDto> validator)
    {
        _context = context;
        _audit = audit;
        _validator = validator;
    }

    public async Task<Result<int>> RegisterHouseholdAsync(
        RegisterHouseholdDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        // 1. Validation BEFORE the transaction.
        var validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result<int>.Failure(
                validation.Errors.Select(e => e.ErrorMessage));
        }

        var correlationId = Guid.NewGuid();

        // 2. One atomic transaction.
        await using var transaction =
            await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var effectiveDate = dto.EffectiveDate;

            var household = new Household
            {
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            _context.Households.Add(household);
            await _context.SaveChangesAsync(ct);

            _context.HouseholdAddresses.Add(new HouseholdAddress
            {
                HouseholdId = household.HouseholdId,
                AddressId = dto.AddressId,
                StartDate = effectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            });

            _context.HouseholdMemberships.Add(new HouseholdMembership
            {
                CitizenId = dto.HeadCitizenId,
                HouseholdId = household.HouseholdId,
                Role = "HEAD",
                StartDate = effectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            });

            _context.Residences.Add(new Residence
            {
                CitizenId = dto.HeadCitizenId,
                AddressId = dto.AddressId,
                StartDate = effectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            });

            var citizen = await _context.Citizens
                .FindAsync(new object[] { dto.HeadCitizenId }, ct);
            if (citizen is null)
            {
                return Result<int>.Failure("Citizen not found.");
            }

            citizen.Status = "ACTIVE";
            citizen.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            // 3. Success audit shares the transaction.
            await _audit.LogAsync(new AuditEntry
            {
                ActorUserId = currentUserId,
                Action = "REGISTER_HOUSEHOLD",
                EntityType = "Household",
                EntityId = household.HouseholdId,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    after = new { household.HouseholdId, dto.AddressId }
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(household.HouseholdId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);

            var connectionString = _context.Database.GetConnectionString()
                ?? throw new InvalidOperationException(
                    "Missing connection string for failure audit.");

            await _audit.LogFailureIndependentAsync(connectionString, new AuditFailureEntry
            {
                ActorUserId = currentUserId,
                Action = "REGISTER_HOUSEHOLD",
                EntityType = "Household",
                DetailsJson = JsonSerializer.Serialize(new
                {
                    reason = ex.Message,
                    exception = ex.GetType().Name
                }),
                CorrelationId = correlationId
            }, ct);

            return Result<int>.Failure($"Registration failed: {ex.Message}");
        }
    }
}
```

Non-negotiable points:

1. Validate before opening a transaction.
2. One transaction per use case.
3. Audit success inside the transaction.
4. Roll back before writing the failure audit.
5. Failure audit uses a **new connection**.
6. Return `Result<T>`; do not throw for expected outcomes.
7. Do not catch and swallow without auditing.

### Registration

```csharp
// Application
services.AddValidatorsFromAssemblyContaining<CreateCitizenValidator>();
services.AddScoped<ICitizenService, CitizenService>();
services.AddScoped<IHouseholdService, HouseholdService>();

// Infrastructure
services.AddDbContext<ArisDbContext>(options =>
    // Explicit user transactions are used throughout, so do not enable
    // SqlServerRetryingExecutionStrategy here.
    options.UseSqlServer(connectionString));
services.AddScoped<IAuditService, AuditService>();
```

---

## C.4 Auditing code rules

| Rule | Detail |
|---|---|
| Always JSON | Use `JsonSerializer.Serialize`; `CK_Audit_Details` rejects invalid JSON. |
| Actor or username | Authenticated actions set `ActorUserId`; unknown-user failures set `AttemptedUsername`. |
| Correlation | One `Guid.NewGuid()` per use case, passed to every audit call. |
| No secrets | Never include passwords, hashes, connection strings, or tokens. |
| Stable action names | Reports group by `Action`; changing a name breaks history. |
| Entity type | Singular table name (`Citizen`, `Household`, `TransferRequest`). |
| Failure path | `RollbackAsync` first, then `LogFailureIndependentAsync`. |

---

## C.5 DTO and validation rules

```csharp
public sealed record CreateCitizenDto
{
    public required string NationalId { get; init; }
    public required string FullName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
}
```

- DTOs live under `src/Aris.Application/<Module>/Dtos/`.
- Use `record` with `required`/`init`; no setters.
- Never expose a domain entity to the UI.
- One validator per input DTO, under `<Module>/Validators/`.
- Mirror the database rules: lengths, formats, enum values, cross-field shape.
- Register validators with `AddValidatorsFromAssemblyContaining<...>()`.

Example:

```csharp
RuleFor(x => x.NationalId)
    .NotEmpty()
    .Matches("^[0-9]{12}$")
    .WithMessage("National ID must be exactly 12 digits (char(12)).");

RuleFor(x => x.Gender)
    .Must(g => g is "MALE" or "FEMALE" or "OTHER");
```

---

## C.6 UI pattern

- Shell: sidebar navigation + `contentPanel` + status strip.
- Screens are child forms, hosted inside `contentPanel`; no new top-level windows for
  navigation.
- Event handlers delegate to an `async` view-model method.
- Never block with `.Result` or `.Wait()`.
- Show `result.Errors`, not exception stack traces.
- Hide admin navigation for `OFFICER`, and enforce the same rule in the service.

```csharp
public sealed class RegisterHouseholdViewModel
{
    private readonly IHouseholdService _households;
    private readonly IUserSession _session;

    public RegisterHouseholdViewModel(
        IHouseholdService households, IUserSession session)
    {
        _households = households;
        _session = session;
    }

    public async Task<bool> SaveAsync(RegisterHouseholdDto dto)
    {
        var result = await _households.RegisterHouseholdAsync(
            dto, _session.CurrentUserId);

        if (result.IsFailure)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, result.Errors),
                "Cannot save household",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        MessageBox.Show("Household registered.", "Success",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return true;
    }
}
```

---

## C.7 Transaction and temporal code rules

```csharp
// Close old rows first, then open new rows (same transaction).
oldMembership.EndDate = effectiveDate.AddDays(-1);
oldMembership.Status = "ENDED";
oldMembership.EndedBy = currentUserId;
oldMembership.EndedAt = DateTime.UtcNow;

await _context.SaveChangesAsync(ct);   // closures persisted

_context.HouseholdMemberships.Add(new HouseholdMembership
{
    CitizenId = citizenId,
    HouseholdId = householdId,
    Role = "MEMBER",
    StartDate = effectiveDate,
    Status = "ACTIVE",
    CreatedBy = currentUserId,
    CreatedAt = DateTime.UtcNow
});

await _context.SaveChangesAsync(ct);   // new active row
```

- `effectiveDate` must be later than every `StartDate` being closed.
- Never assign `Role`, `CitizenId`, `HouseholdId`, `AddressId`, or `StartDate` on an
  existing row.
- Use `Status = "VOID"` only for rectification, and insert a correcting row.
- Transfer execution uses `IsolationLevel.Serializable`.

---

## C.8 Testing rules

| Test type | Project | What it may prove |
|---|---|---|
| Validator / pure logic | `Aris.UnitTests` | Input rules, `Result<T>`, audit entry validation, mapping defaults |
| Database rule | `Aris.Tests` | Triggers, CHECK constraints, filtered indexes, views, `rowversion`, Golden Invariant |

- In-memory EF does **not** enforce database rules. Never use it to prove one.
- Integration tests skip when `ARIS_TEST_CONNECTION` is unset.
- Use transactions and roll back destructive fixtures.
- Keep the negative control for the Golden Invariant.
- Test names state the behaviour: `ChangeHead_ClosesOldRows_And_OpensTwoNewRows`.

---

## C.9 AI coding guardrails

### Prompt header

Prepend this when asking an AI assistant to change ARIS code:

```text
[PROJECT CONTEXT & CONSTRAINTS]
Project: Administrative Residence Information System (ARIS)
Platform: .NET 10 (C#), WinForms, SQL Server 2022 in local Docker, EF Core.
Architecture: strict 4-layer (UI -> Application -> Domain; Infrastructure -> Application/Domain).
Sources of truth:
- Schema: docs/aris_schema_v2.sql
- Entities: docs/appendix/A-domain-model.md
- Decisions: docs/09-architecture-decisions.md
- Rules: docs/08-crosscutting-concepts.md

MANDATORY RULES (DO NOT DEVIATE):
1. Citizen has NO HouseholdId or AddressId.
2. Household has NO CurrentAddressId; address history is HouseholdAddress.
3. Citizen starts UNASSIGNED; HH-01/HH-02 makes them ACTIVE.
4. HouseholdMembership, Residence, HouseholdAddress are append-only.
   Never UPDATE CitizenId, HouseholdId, AddressId, Role, or StartDate.
   Role change = close (EndDate = E-1, ENDED) + open (StartDate = E, ACTIVE).
5. TransferRequest requires RequestedBy != DecidedBy.
6. Transfer execution uses one IsolationLevel.Serializable transaction.
7. No SQL or DbContext in the UI. DTOs + Result<T> only.
8. Every ACTIVE citizen has exactly 1 active membership and 1 active residence
   matching the household's active canonical address.
9. Failure audit is written on an INDEPENDENT connection AFTER rollback.
10. The DDL is authoritative. Do not add EF migrations.
```

### Ten hallucination traps

| # | Wrong pattern | Failure | Correct pattern |
|---|---|---|---|
| 1 | `citizen.HouseholdId = id` | Column does not exist; destroys lineage | Create `HouseholdMembership` |
| 2 | `household.CurrentAddressId = id` | Column does not exist | Create `HouseholdAddress`; query `vw_HouseholdCurrent` |
| 3 | `membership.Role = "HEAD"` in place | Trigger `51011` | Close old row, open new `HEAD` row |
| 4 | Creating a citizen as `ACTIVE` | Invariant `C1`/`C2` | Create `UNASSIGNED` |
| 5 | Splitting a transfer across transactions | Orphaned/partial state | One serializable transaction |
| 6 | `UPDATE`/`DELETE` on `AuditLog` | Trigger `51030`, role `DENY` | Insert only |
| 7 | Failure audit inside the transaction | Lost on rollback | `LogFailureIndependentAsync` after rollback |
| 8 | Self-approval | `CK_TR_Decision` | Require a different checker |
| 9 | `_context.Citizens.ToList()` in a form | Layer violation | Call `ICitizenService` |
| 10 | Overlapping dates | Trigger `51002`/`51012`/`51022` | `E > StartDate`; `EndDate = E - 1` |

### AI output self-check

- [ ] Compiles with 0 errors and 0 warnings.
- [ ] No static keys on `Citizen` or `Household`.
- [ ] Role/address changes use close-and-open.
- [ ] Success audit inside the transaction; failure audit on a new connection.
- [ ] `vw_GoldenInvariantViolations` returns 0 rows after the operation.
- [ ] No EF migration was generated.
- [ ] No secret or connection string added to a tracked file.

---

## C.10 PR review checklist

### Author self-check

- [ ] Branch is `feature/<task-id>`.
- [ ] `dotnet build Aris.slnx` → 0 errors, 0 warnings.
- [ ] `dotnet test Aris.slnx` → 0 failed (never `--nologo`).
- [ ] Integration tests executed with a local database for schema-sensitive changes.
- [ ] `SELECT * FROM dbo.vw_GoldenInvariantViolations;` → 0 rows.
- [ ] No secrets, `.env`, `bin/`, `obj/`, or `TestResults/` staged.
- [ ] PR description lists requirement IDs and tests.

### Reviewer checklist

**Architecture**

- [ ] UI has zero SQL and zero `DbContext` references.
- [ ] Dependency direction is correct.
- [ ] Expected failures return `Result<T>`; exceptions are reserved for bugs and
      infrastructure.
- [ ] Validation runs before the transaction.

**Schema and EF Core**

- [ ] Names match the DDL verbatim.
- [ ] No `Citizen.HouseholdId` or `Household.CurrentAddressId`.
- [ ] `.HasTrigger(...)` on the five trigger tables.
- [ ] `.IsRowVersion()` on the nine mutable tables only.
- [ ] `.HasComputedColumnSql(...)` for `AddressText`.
- [ ] Composite FKs on `TransferRequestItem`.
- [ ] No cascade deletes.
- [ ] No EF migrations.

**Temporal and transfer rules**

- [ ] No in-place update of tenure identity columns.
- [ ] `EndDate = E - 1`; `StartDate = E`; `E > StartDate`.
- [ ] Closures are saved before new active rows (filtered indexes).
- [ ] Transfer execution is one serializable transaction.
- [ ] Partial approval leaves rejected members in place.
- [ ] Emptied source household becomes `INACTIVE` and its address closes.

**Audit and security**

- [ ] Every mutation writes an audit row with actor, action, entity, result, JSON,
      correlation ID.
- [ ] Failure audit is independent and after rollback.
- [ ] Passwords are salted hashes; no plaintext.
- [ ] Service-level role authorization.
- [ ] No secret in the diff.

### Twelve automatic rejections

| # | Pattern | Why |
|---|---|---|
| 1 | `citizen.HouseholdId = ...` | Column does not exist; lineage destroyed |
| 2 | `household.CurrentAddressId = ...` | Column removed; history lost |
| 3 | In-place `membership.Role = "HEAD"` | Trigger `51011` |
| 4 | Citizen created as `ACTIVE` | Invariant `C1`/`C2` |
| 5 | Relocation split across transactions | Atomicity/isolation broken |
| 6 | `UPDATE`/`DELETE` on `AuditLog` | Trigger `51030`, role `DENY` |
| 7 | Failure audit inside the rolled-back transaction | Audit lost |
| 8 | Self-approval | `CK_TR_Decision` |
| 9 | Raw `_context` or SQL in a form | Layer violation |
| 10 | Overlapping dates | Trigger `51002`/`51012`/`51022` |
| 11 | Committed secret, `.env`, or build output | Security/hygiene |
| 12 | Direct commit to `main`/`develop` without PR | Workflow violation |

### Reviewer sign-off template

```text
REVIEW: ARIS PR #<number> — <title>
Reviewer: <name>                       Date: <yyyy-mm-dd>
Branch: feature/<task-id>  ->  develop

Verification evidence:
  dotnet build Aris.slnx ................. 0 errors / 0 warnings
  dotnet test  Aris.slnx ................. <pass> passed, 0 failed
  vw_GoldenInvariantViolations ........... 0 rows
  triggers/views inventory ............... 5 triggers / 3 views

Requirement IDs verified: <e.g. CIT-01, HH-04, UC-06>
Result: [APPROVE | REQUEST CHANGES]
Blocking items: <none, or list>
Non-blocking suggestions: <none, or list>
```
