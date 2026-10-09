# ARIS — Phase 1 Task Briefs

**Issued:** by the Team Leader · **Baseline:** `main` · **Source of truth:** [`work-breakdown.md`](work-breakdown.md) Phase 1, [`srs.md`](srs.md), [`database_design_v2.md`](database_design_v2.md)

---

## 0. Where the project actually stands

**Done and verified — do not rebuild any of this:**

| Thing | State |
|---|---|
| 4-layer solution (`Domain`, `Application`, `Infrastructure`, `UI`) | Builds clean, 0 warnings |
| Base abstractions (`EntityBase`, `Result<T>`, `ValidationException`, `BaseService`) | Merged |
| Azure SQL Database `ARIS-DB` | Deployed, schema applied |
| Least-privilege login `aris_officer` in role `aris_app` | Created and **proven**: `UPDATE` on `AuditLog` is refused |
| Test harness (`tests/Aris.Tests`, xUnit v3) | 17 tests green against Azure |
| Transient-fault retry for SQL connections | `Aris.Infrastructure/Resilience/SqlResilience.cs` |

**Nothing in `Aris.Domain` entities, `ArisDbContext`, services, DTOs, or UI features exists yet.** That is what Phase 1–2 builds.

---

## 1. Shared protocol — read this once, follow it always

### Branches

`main` is the trunk. **Never commit to it directly.**

```powershell
git checkout main; git pull
git checkout -b feature/<your-task-id-lowercase>     # e.g. feature/p1-db2
# ... work ...
git push -u origin feature/p1-db2
```

Open a PR into `main`. The Team Leader reviews against [`pr-review-checklist.md`](pr-review-checklist.md).

> The docs describe `feature/* → develop → main`. We are **not** using `develop`: it was never
> merged into and is now strictly behind `main`. Treat `main` as the integration branch.

### Before every push

```powershell
dotnet build Aris.slnx        # must be 0 warnings, 0 errors
dotnet test  Aris.slnx        # must be 0 failed
```

**Do not pass `--nologo` to `dotnet test`.** .NET 10 routes it through Microsoft.Testing.Platform,
which forwards unknown flags to the test executable; the app rejects them and the run fails with
the misleading `Zero tests ran` / exit code 5.

### Connecting to the database

Never `new SqlConnection(cs).OpenAsync()`. Always go through the retry helper — the database is
Azure serverless and auto-pauses, so the first connection after idle can fail transiently:

```csharp
await using var connection = await SqlResilience.OpenAsync(connectionString, cancellationToken: ct);
```

### Secrets

Connection strings go in **user-secrets**, never in `appsettings.json`, never in source (`CON-SEC-02`):

```powershell
cd src/Aris.UI
dotnet user-secrets set "ConnectionStrings:ArisDb" "Server=tcp:aris-demo-b1805.database.windows.net,1433;Database=ARIS-DB;User Id=aris_officer;Password=<get from the Team Leader>;Encrypt=True;"
```

Tests read the environment variable `ARIS_TEST_CONNECTION`.

### Layering (`CON-PROJ-01`, enforced in review)

```
UI → Application → Domain          Infrastructure → Application/Domain
```

No EF Core or SQL in UI code. Business invariants never live in a `button_Click`. Use `Result<T>`
for expected failures; exceptions only for the unexpected.

### The pattern you must copy

[`ai-coding-guidelines.md` §4.1](ai-coding-guidelines.md) has the canonical service shape: validate
**before** opening a transaction, one atomic transaction around all writes, commit, and on failure
roll back and write the failure audit on an independent connection. Coder 1's slice becomes the
reference the whole team copies.

---

## 2. Sequence — who is blocked on whom

```text
   Database Designer          Coder 1                Coder 2              Coder 3
   ─────────────────          ───────                ───────              ───────
   P1-DB1 ✅ done
        │
        ├──► P1-DB2  ────────► P2-C1 ──► P2-UI1 ──► P2-TL1 ──► P3-C2A ──► P4-C2A
        │   DbContext          slice    UI        review     household   transfer
        │                      │                                    │
        │                      └──► P3-C1 (profile UI)              └──► P4-C2B
        │
   meanwhile, independently:
        P1-C1 (Coder 1 DTOs) · P1-C2 (Coder 2 interfaces) · P1-C3 (Coder 3 shell) ──► P5-C3A
```

**Critical path: P1-DB2 → P2-C1 → P2-TL1 → P3-C2A → P4-C2A → P4-C2B.**

Coder 2 and Coder 3 are **not** idle while waiting: their Phase 1 contract work (P1-C2, P1-C3) and
Coder 3's authentication work (P5-C3A) have no dependency on the `DbContext`.

---

## 3. Database Designer

### P1-DB1 ✅ — already complete

Schema deployed to `ARIS-DB`, all 5 triggers and 3 views created, role permissions verified.
Nothing to do. *(Done by the Team Leader; recorded here so you know where the baseline is.)*

### P1-DB2 — `ArisDbContext` and EF Core mappings

**This unblocks both other coders. Do it first.**

**Deliverables**

1. `src/Aris.Domain/` entities — one class per table, each mapping a real column set:
   `Citizen`, `Address`, `Household`, `HouseholdAddress`, `HouseholdMembership`, `Residence`,
   `TransferRequest`, `TransferRequestItem`, `UserAccount`, `SystemConfiguration`, `AuditLog`.
2. `src/Aris.Infrastructure/Persistence/ArisDbContext.cs` with a `DbSet<T>` per table.
3. One `IEntityTypeConfiguration<T>` class per entity in
   `src/Aris.Infrastructure/Persistence/Configurations/`.
4. Register in `AddInfrastructure` — it is still a `TODO` stub:

   ```csharp
   services.AddDbContext<ArisDbContext>(options =>
       options.UseSqlServer(configuration.GetConnectionString("ArisDb"),
           sql => sql.EnableRetryOnFailure()));
   ```

   Add the `Microsoft.EntityFrameworkCore.SqlServer` package to `Aris.Infrastructure`.

**Definition of Done:** EF Core can connect to `ARIS-DB`, read every table, and insert + update an
entity. `dotnet build` clean. A test proving the context connects.

**Five mapping details that will bite you — all verified against the schema:**

1. **`.ToTable(t => t.HasTrigger("trg_..."))` is mandatory** for the 5 tables that have triggers:
   `Residence`, `HouseholdMembership`, `HouseholdAddress` (`*_Guard`), `TransferRequest`
   (`trg_TransferRequest_Frozen`), `AuditLog` (`trg_AuditLog_AppendOnly`). EF Core 7+ requires this
   to be told a table has triggers, or `SaveChanges` fails at runtime in a way that looks like data
   corruption. [WBS §3.2](work-breakdown.md) says the same — it is not optional.
2. **`RowVersion`** → `.IsRowVersion()` for optimistic concurrency (`CON-CONC-01`/D14). Exactly
   **9 tables** have it: `UserAccount`, `Citizen`, `Address`, `Household`, `TransferRequest`,
   `HouseholdAddress`, `HouseholdMembership`, `Residence`, `TransferRequestItem`.
   **`AuditLog` and `SystemConfiguration` do not** — do not inherit `EntityBase` for those two.
3. **`Address.AddressText` is a computed column** (`CONCAT_WS` over line/ward/district/province).
   Map with `.HasComputedColumnSql()` and never let EF write it.
4. **`TransferRequestItem` has composite foreign keys** — `(SourceMembershipId, CitizenId)` and
   `(SourceResidenceId, CitizenId)`, which target the `UQ_HM_Id_Citizen` / `UQ_Res_Id_Citizen`
   unique constraints. Configure them explicitly; EF's conventions will not infer these.
5. **Table names are singular** (`dbo.Citizen`) while `DbSet` properties are plural
   (`context.Citizens`). Map that explicitly.

**One consequence of the least-privilege role worth planning for:** `aris_officer` has
`DENY DELETE ON SCHEMA::dbo`. If a `DbContext` operation tries to delete a row, it will fail at
runtime. That is the point of the design (append-only), but it means your mappings must **never**
cascade-delete. Verify any `OnDelete(...)` you configure, and prefer `DeleteBehavior.Restrict` or
`NoAction` on history relationships.

### P2-DB1 — Citizen database operations

**Blocked on P2-C1.** Verify index performance and that `CK_Citizen_Lifecycle` genuinely prevents
invalid deactivations (status `INACTIVE` requires `DeactivatedAt IS NOT NULL`, and vice versa).
Turn the check into a test, not a manual observation.

### P4-DB1, P5-DB1, P3-DB1 — trigger and constraint verification

Later phases. Each is a *test*, not an inspection: prove `CK_TR_Decision` rejects a self-approved
transfer (`DecidedBy <> RequestedBy`), prove `trg_Membership_Guard` rejects an append-only
violation, prove `trg_AuditLog_AppendOnly` rejects an update. **`P5-DB1` is already partly proven** —
the deploy script confirmed `DENY UPDATE ON AuditLog` works for `aris_officer` — but the trigger
itself (which covers other principals) is not yet tested.

---

## 4. Coder 1 — Citizen Management (first vertical slice)

**You own the reference implementation. Everyone copies your structure, so set the standard well.**

### P1-C1 — Citizen contracts (start immediately, no dependency)

In `Aris.Application/`:

- `CitizenDto`, `CreateCitizenDto`, `UpdateCitizenDto`, `CitizenDetailDto`, `CitizenSearchCriteria`.
- FluentValidation validators: `NationalId` must be exactly **12 numeric characters**
  (`CK_Citizen_NationalId` rejects anything else — note `char(12)` pads, so a short value fails too),
  `FullName` required, `DateOfBirth` required, `Gender` in `MALE|FEMALE|OTHER`.
- The 3 lifecycle states: `UNASSIGNED` (initial) → `ACTIVE` (in a household) → `INACTIVE`.

### P2-C1 — `ICitizenService` / `CitizenService`

Create, view, update demographics, deactivate, and search (by National ID, name
case-insensitive with accent handling, and status). **Blocked on P1-DB2** — you need the `DbContext`.

Follow [`ai-coding-guidelines.md` §4.1](ai-coding-guidelines.md) exactly: validate first, then one
transaction, then commit; audit success; on failure roll back and audit on an independent connection.
Return `Result<T>` — never let an expected failure throw.

### P2-UI1 — Citizen Directory UI

Search bar, `DataGrid`, and a "Create Citizen" modal in `src/Aris.UI`. Resolve the form through DI
(the composition root already does `services.AddTransient<MainForm>()`), talk only to
`ICitizenService` via DTOs, and render `result.Errors` to the user rather than swallowing them.
**No SQL and no business rules in this layer.**

### P3-C1 — Citizen Profile UI

Current household, current active address, and full chronological residency history.

---

## 5. Coder 2 — Household, Residence & Transfer engine

**You have the hardest work and the critical path depends on Coder 1's slice.** Use the waiting time
on P1-C2.

### P1-C2 — Household/Residence/Transfer contracts (start immediately)

`IHouseholdService`, `IResidenceService`, `ITransferService` plus their DTOs, drafted in
`Aris.Application/`. Designing these now means P3-C2A starts the day Coder 1's slice is approved.

### P3-C2A — `RegisterHousehold` (`HH-01`)

Atomic creation of `Household` + `HouseholdAddress` + `HEAD` `HouseholdMembership` + `Residence`,
transitioning the citizen `UNASSIGNED → ACTIVE`. This is the same multi-table shape as the
[`ai-coding-guidelines.md` §4.1](ai-coding-guidelines.md) example — start from that.

### P3-C2B — `AddMember` (`HH-02`) and `ChangeHead` (`HH-04`)

`HH-04` is subtle: close the former head's record and the successor's member record with
`EndDate = E - 1`, then open a new member record for the former head and a head record for the
successor with `StartDate = E`. Get the date arithmetic exactly right or the guard triggers will
reject the overlap — which is the triggers working correctly, not a bug.

### P4-C2A / P4-C2B — the maker-checker transfer engine

The two-phase `UC-06` workflow, three variants (`JOIN_EXISTING`, `NEW_HOUSEHOLD`,
`HOUSEHOLD_MOVE`), per-item resolution, departing-head succession, and empty source households going
`INACTIVE`. Phase 2 must run at `IsolationLevel.Serializable` (`BR-CONC-03`).

**Before you start, read [`srs.md`](srs.md) §6.5–6.7 and §9** — this is the most rule-dense part of
the system, and the database enforces much of it (`CK_TR_Decision`, `CK_TR_Shape`,
`trg_TransferRequest_Frozen`), so a naive implementation will be rejected by the database.

### P3-C2C / P4-C2C — Household and Transfer UI

Household list/detail/member grid/change-head dialog; Transfer Request Wizard (Maker) and Transfer
Decision Inbox (Checker).

---

## 6. Coder 3 — Shell, authentication, administration, reports

### P1-C3 — Main shell (start immediately, no dependency)

Replace the empty 1024×700 `MainForm` with a real container: navigation (sidebar/menu), placeholder
sections for Citizen Directory, Household Directory, Transfer Inbox, Reports, Administration, and a
status bar showing user/role. Keep the `partial class` designer split intact.

### P5-C3A — `AuthenticationService`

Login, password hashing (**never plaintext** — `CON-SEC-01`), lockout after `MAX_FAILED_LOGINS`, and
the `MustChangePassword` flow. Seed values live in `SystemConfiguration`
(`PASSWORD_MIN_LENGTH`, `SESSION_TIMEOUT_MINUTES`, `MAX_FAILED_LOGINS`, `LOCKOUT_MINUTES`).

Be aware: `UserAccount` **has** `RowVersion`, but its mapping comes from the Database Designer
(P1-DB2). Until then, build the hashing and lockout logic against an interface so you are not
blocked.

### P5-C3B / P5-C3C — Account admin, system config, reports

Account CRUD (create officer, reset password, lock/unlock), `SystemConfiguration` UI, and the three
reports (`RPT-01` demographic distribution using `vw_AddressCanonical`, `RPT-02` household summary,
`RPT-03` residency movements) with CSV or print-preview export.

**Role-based visibility is a security requirement, not styling** (`CON-AUTH-02`): `OFFICER` must not
see Manage Accounts, System Configuration, or Audit Logs. Hide them *and* enforce authorization in
the Application layer — hiding a menu item is not access control.

---

## 7. Team Leader (kept visible for accountability)

| Task | What it is |
|---|---|
| P1-TL1 ✅ | Solution structure, layers, repository — done |
| P2-TL1 | Architectural review of Coder 1's slice — the standard-setting review |
| P3-TL1 | Checkpoint 2: verify the golden invariant after household operations |
| P4-TL1 | Transfer transaction review, concurrency edge cases |
| P5-TL1 | Standardize cross-cutting audit logging across all services |

---

## 8. Reporting back

When you finish a task, open a PR containing: the task ID in the title, what you verified (a command
and its output, not a claim), and anything you deliberately left undone.

If you are blocked, say so early and specifically — "waiting on P1-DB2 for the `DbContext`" is
useful; silence is not. The critical path is narrow, so a blocked Coder 2 costs the whole team.

**Everyone: `dotnet build` clean and `dotnet test` green before you push.**
