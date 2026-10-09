# ARIS — Team Standards (Phase 1)

**Owner:** Team Leader · **Status:** binding for all Phase 1–2 work

These are the decisions that are *not* in the SRS or the WBS, because they are conventions
rather than requirements. They exist so four people don't invent four styles and you
don't arbitrate at review time. [`ai-coding-guidelines.md`](ai-coding-guidelines.md)
still governs *how* a service is written; this document settles the open questions.

---

## 1. Audit logging — the cross-cutting standard

**Every business mutation writes an audit row** (`BR-AUD-01`). The contract is
[`IAuditService`](../src/Aris.Application/Auditing/IAuditService.cs); validate entries with
[`AuditEntryValidator`](../src/Aris.Application/Auditing/AuditEntryValidator.cs).

### Two calls, two meanings

| Method | When | Connection |
|---|---|---|
| `LogAsync` | Inside the business transaction, **before** `CommitAsync` | the caller's transaction |
| `LogFailureIndependentAsync` | In a `catch`, **after** rollback | a **new** connection |

The distinction is the whole point. An audit row written inside a transaction that
then rolls back **disappears with it** — correct for success, useless for failure. A
failure must be written independently or it is lost (`BR-AUD-03`).

### The pattern every service follows

```csharp
public async Task<Result<int>> RegisterHouseholdAsync(RegisterHouseholdDto dto, int currentUserId, CancellationToken ct = default)
{
    // 1. Validate BEFORE opening a transaction.
    var validation = await _validator.ValidateAsync(dto, ct);
    if (!validation.IsValid)
        return Result<int>.Failure(validation.Errors.Select(e => e.ErrorMessage));

    // 2. One correlation id ties together every audit row this operation writes.
    var correlationId = Guid.NewGuid();

    await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
    try
    {
        // ... write Household, HouseholdAddress, Membership, Residence ...

        // 3. Audit inside the transaction, before commit.
        var audit = new AuditEntry
        {
            ActorUserId = currentUserId,
            Action = "REGISTER_HOUSEHOLD",
            EntityType = "Household",
            EntityId = household.HouseholdId,
            DetailsJson = JsonSerializer.Serialize(new { after = new { household.Status } }),
            CorrelationId = correlationId
        };

        // Fail fast on a malformed entry: this is a programming error, not a business one.
        if (!AuditEntryValidator.TryValidate(audit, out var problems))
            throw new ValidationException(problems);

        await _audit.LogAsync(audit, ct);

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result<int>.Success(household.HouseholdId);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);

        // 4. Independent connection: this survives the rollback.
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string available for failure auditing.");

        await _audit.LogFailureIndependentAsync(connectionString, new AuditFailureEntry
        {
            ActorUserId = currentUserId,
            Action = "REGISTER_HOUSEHOLD",
            EntityType = "Household",
            DetailsJson = JsonSerializer.Serialize(new { reason = ex.Message, exception = ex.GetType().Name }),
            CorrelationId = correlationId
        }, ct);

        return Result<int>.Failure($"Registration failed: {ex.Message}");
    }
}
```

### Rules

- **`Action` is a stable string**, e.g. `REGISTER_HOUSEHOLD`, `EXECUTE_TRANSFER`, `LOGIN`.
  Reports group by it; renaming one breaks history. ≤ 50 chars (`varchar(50)`).
- **`EntityType`** ≤ 50 chars and singular, matching the table: `Household`, `Citizen`.
- **`DetailsJson` must be well-formed JSON** — `CK_Audit_Details` runs `ISJSON(Details) = 1`
  and rejects anything else. Use `JsonSerializer.Serialize`, never string concatenation.
- **A successful operation must name a real actor.** `CK_Audit_Actor` accepts
  `ActorUserId OR AttemptedUsername`, so SQL Server would allow an unattributed success
  row. `AuditEntryValidator` closes that gap — do not bypass it.
- **Use `AttemptedUsername`** only for a failure where the user is unknown, e.g. a login
  with an unrecognised username.
- **Never** update or delete audit rows. The database denies it (`DENY UPDATE/DELETE`)
  and a trigger rejects it.

### Implementation note for the Database Designer (P1-DB2)

Implement `AuditService` in `Aris.Infrastructure` with **raw ADO.NET**, not EF Core.
`LogFailureIndependentAsync` must open its own connection via
[`SqlResilience.OpenAsync`](../src/Aris.Infrastructure/Resilience/SqlResilience.cs), because an
`ArisDbContext` bound to the rolled-back transaction cannot be reused:

```csharp
await using var connection = await SqlResilience.OpenAsync(connectionString, cancellationToken: ct);
// INSERT INTO dbo.AuditLog (ActorUserId, AttemptedUsername, Action, EntityType, EntityId, Result, Details, CorrelationId)
// VALUES (...);  -- Result is 'SUCCESS' or 'FAILURE' (CK_Audit_Result)
```

`AttemptedUsername` and `ActorUserId` are both nullable, but at least one is required.

---

## 2. Testing — which harness for what

Two test projects, and the split is deliberate:

| Project | Kind | Needs database | Use for |
|---|---|---|---|
| `tests/Aris.UnitTests` | Unit | No | Business logic, validation, mapping defaults |
| `tests/Aris.Tests` | Integration | **Yes** (`ARIS_TEST_CONNECTION`) | Anything the database enforces |

### The rule that matters

**An in-memory provider is not the database.** It has no triggers, no CHECK constraints,
no filtered unique indexes, no computed columns, and no `rowversion` generation. It will
**happily accept data that `ARIS-DB` rejects.**

So:

- ✅ In-memory: "does `RegisterHousehold` create four rows with the right values?"
- ❌ In-memory: "does the golden invariant hold?" / "is history append-only?" /
  "can a citizen have two ACTIVE memberships?" / "does the overlap trigger fire?"
- ✅ Real database (`Aris.Tests`): all of the above.

**Never re-implement a database constraint in C# to make an in-memory test pass.** That
doesn't test the system; it hides the divergence. If you find yourself adding a `CHECK`
in a validator that already exists in SQL Server, stop and write an integration test.

Use [`InMemoryDatabaseTestBase<TContext>`](../tests/Aris.UnitTests/Infrastructure/InMemoryDatabaseTestBase.cs)
— derive from it and override `CreateContext` once `ArisDbContext` exists.

### Conventions

- Test names are full sentences describing the rule:
  `Success_WithoutRealActor_Fails`, `Action_OverColumnLimit_Fails`.
- One behaviour per test. A `[Theory]` for each boundary value.
- Assert the **specific** error message so a wrong rejection is distinguishable from a
  right one.
- Tests that need the database must **skip** when `ARIS_TEST_CONNECTION` is unset, never
  fail — a teammate without a database must not see a red build.
- **Do not pass `--nologo` to `dotnet test`** (see the briefs; it corrupts the MTP run).

---

## 3. Remaining conventions

### DTOs

- Live in `Aris.Application`, one file per use case: `CreateCitizenDto`, `CitizenDetailDto`.
- **Never expose domain entities to the UI.** The UI sees DTOs and `Result<T>` only.
- Immutable: `record` with `init` properties and `required` where the value is mandatory.
- Name the verb into the type when the shape differs by operation
  (`CreateCitizenDto` vs `UpdateCitizenDto`).

### Errors — the one place the docs were ambiguous

[`ai-coding-guidelines.md`](ai-coding-guidelines.md) shows both `Result<T>` and
`ValidationException` for validation. The ruling:

| Situation | Mechanism |
|---|---|
| Expected business failure — validation, not found, conflict, permission | **`Result<T>.Failure(...)`** |
| Input that reaches a use case malformed — a guard clause, a programmer error | **`ValidationException`** |
| Anything genuinely unexpected | let it propagate |

Return `Result<T>` from services. Do not throw for a business outcome the UI must display.

### Dependency injection

Each layer owns one registration method, and **you only edit your own**:

- `Aris.Application/DependencyInjection.cs` → `AddApplication()`
- `Aris.Infrastructure/DependencyInjection.cs` → `AddInfrastructure(configuration)`

Both are still `TODO` stubs. Register concrete services there as you build them — this is
why the files exist, and why two people registering services in the same file is expected
rather than a conflict.

### Naming

- Tables are singular (`dbo.Citizen`); `DbSet` properties are plural (`context.Citizens`).
- Interfaces are `I`-prefixed and live beside their implementation's layer, not its location.
- Async methods end in `Async` and take `CancellationToken` last.

---

## 4. Open decisions for the Team Leader

Not yet ruled on. Raise them at the P2-TL1 review rather than guessing:

- CSV vs print-preview for report export (`RPT-01`–`RPT-03`).
- Whether `SystemConfiguration` is cached per session or read per use.
- Password hashing choice — PBKDF2 (`Rfc2898DeriveBytes`) or BCrypt (adds a package).
