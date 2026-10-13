# Phase 1 Actionable Task Briefs & Roadmap

**Document Code:** `WKF-PHASE1-v2.0`  
**Baseline:** `main`  
**Single Source of Truth:** [`work-breakdown.md`](work-breakdown.md) · [`../architecture/srs.md`](../architecture/srs.md) · [`../database/design-rationale.md`](../database/design-rationale.md)

---

## 1. Execution Sequence & Critical Path

The project critical path runs directly through the Database Designer and Coder 1:

```text
Database Designer          Coder 1                Coder 2              Coder 3
─────────────────          ───────                ───────              ───────
P1-DB1 ✅ Done
     │
     ├──► P1-DB2  ────────► P2-C1 ──► P2-UI1 ──► P2-TL1 ──► P3-C2A ──► P4-C2A
     │   DbContext          slice    UI        review     household   transfer
     │                      │                                │
     │                      └──► P3-C1 (profile UI)          └──► P4-C2B
     │
meanwhile, independently:
     P1-C1 (Coder 1 DTOs) · P1-C2 (Coder 2 interfaces) · P1-C3 (Coder 3 shell) ──► P5-C3A
```

**Critical Path:** `P1-DB2` $\rightarrow$ `P2-C1` $\rightarrow$ `P2-TL1` $\rightarrow$ `P3-C2A` $\rightarrow$ `P4-C2A` $\rightarrow$ `P4-C2B`.

> [!NOTE]
> Coders 2 and 3 are **not blocked** while waiting for the DbContext: their contract definitions (`P1-C2`, `P1-C3`) and authentication logic (`P5-C3A`) have no dependency on EF Core.

---

## 2. Database Designer Brief

### P1-DB2: `ArisDbContext` & EF Core Mappings (CRITICAL PATH)
- **Goal:** Unblock the entire development team by providing domain entities and EF Core mapping.
- **Scope of Work:**
  1. Create Domain Entities in `src/Aris.Domain/` (`Citizen`, `Household`, `HouseholdMembership`, `Residence`, `Address`, `HouseholdAddress`, `TransferRequest`, `TransferRequestItem`, `UserAccount`, `SystemConfiguration`, `AuditLog`).
  2. Implement `ArisDbContext` in `src/Aris.Infrastructure/Persistence/` with plural `DbSet<T>` properties.
  3. Create entity configurations implementing `IEntityTypeConfiguration<T>` in `src/Aris.Infrastructure/Persistence/Configurations/`.
  4. Register `ArisDbContext` inside `AddInfrastructure` in `src/Aris.Infrastructure/DependencyInjection.cs`:
     ```csharp
     services.AddDbContext<ArisDbContext>(options =>
         options.UseSqlServer(configuration.GetConnectionString("ArisDb"),
             sql => sql.EnableRetryOnFailure()));
     ```
  5. Implement `AuditService` in `src/Aris.Infrastructure/` using raw ADO.NET and [`SqlResilience`](file:///C:/Users/Bryan/Documents/ARIS/src/Aris.Infrastructure/Resilience/SqlResilience.cs) (required for `LogFailureIndependentAsync`).
- **5 Critical Mapping Details:**
  - **Triggers:** Configure `.ToTable(t => t.HasTrigger(...))` on the 5 tables with triggers (`Residence`, `HouseholdMembership`, `HouseholdAddress`, `TransferRequest`, `AuditLog`).
  - **RowVersion:** Configure `.IsRowVersion()` on the 9 mutable tables (`AuditLog` and `SystemConfiguration` do NOT have RowVersion).
  - **Computed Column:** Map `Address.AddressText` via `.HasComputedColumnSql()`.
  - **Composite Keys:** Configure composite foreign keys on `TransferRequestItem` pointing to `(MembershipId, CitizenId)` and `(ResidenceId, CitizenId)`.
  - **No Cascade Deletes:** Use `DeleteBehavior.Restrict` or `NoAction` to conform with `aris_app`'s `DENY DELETE`.

---

## 3. Coder 1 Brief — Citizen Management (Reference Slice)

**Coder 1 builds the project's first vertical slice. All other developers will mirror this pattern.**

### P1-C1: Citizen Contracts & Validation (Immediate)
- In `src/Aris.Application/Citizens/`:
  - Create DTOs: `CitizenDto`, `CreateCitizenDto`, `UpdateCitizenDto`, `CitizenDetailDto`, `CitizenSearchCriteria`.
  - FluentValidation rules: `NationalId` must be exactly 12 numeric digits (`char(12)`), `FullName` required, `DateOfBirth` required, `Gender` in `MALE|FEMALE|OTHER`.

### P2-C1: `CitizenService` (Blocked on P1-DB2)
- Implement `ICitizenService`: Create citizen (`Status = 'UNASSIGNED'`), view profile, update demographics, deactivate citizen (`CIT-04`), and directory search (case/accent insensitive).
- Follow the canonical service pattern: pre-validation, atomic transaction, audit logging, independent failure logging post-rollback.

### P2-UI1: Citizen Directory UI
- Build WinForms screens in `src/Aris.UI/`: Search bar, DataGridView, and Create Citizen modal.
- Form interacts exclusively with `ICitizenService` via DTOs and handles `result.Errors` gracefully.

### P3-C1: Citizen Profile UI
- Display demographic summary, active household, canonical address, and complete chronological residency history.

---

## 4. Coder 2 Brief — Household, Residence & Transfer Engine

### P1-C2: Domain Contracts & Interfaces (Immediate)
- In `src/Aris.Application/`:
  - Draft `IHouseholdService`, `IResidenceService`, `ITransferService` and their DTOs (`RegisterHouseholdDto`, `AddMemberDto`, `ChangeHeadDto`, `CreateTransferRequestDto`).

### P3-C2A: `HouseholdService.RegisterHousehold` (`HH-01`)
- Atomic creation of `Household` + `HouseholdAddress` + `HEAD` `HouseholdMembership` + `Residence`, transitioning the citizen from `UNASSIGNED → ACTIVE`.

### P3-C2B: Member Addition & Head Change (`HH-02`, `HH-04`)
- `HH-02`: Add `UNASSIGNED` citizen as `MEMBER`.
- `HH-04`: Close former head (`EndDate = E - 1, Status = 'ENDED'`), close successor member (`EndDate = E - 1`), open new member for former head (`StartDate = E`), open new head for successor (`StartDate = E`).

### P4-C2A / P4-C2B: Maker-Checker Transfer Engine (`UC-06`)
- `CreateRequest` (Maker): Validates preconditions, creates `TransferRequest(PENDING)` and item rows.
- `ExecuteTransfer` (Checker): Runs at `IsolationLevel.Serializable`. Handles `JOIN_EXISTING`, `NEW_HOUSEHOLD`, and `HOUSEHOLD_MOVE`, designated successor heads, and emptying source households.

---

## 5. Coder 3 Brief — Shell, Authentication, Admin & Reports

### P1-C3: WinForms Main Shell Layout (Immediate)
- Update `MainForm`: Sidebar navigation, container panels for Citizen Directory, Household Directory, Transfer Inbox, Reports, and Administration, status bar with user and role info.

### P5-C3A: `AuthenticationService` & Login
- Login dialog, salted password hashing (PBKDF2/BCrypt, never plaintext), account lockout after `MAX_FAILED_LOGINS`, mandatory password change enforcement (`MustChangePassword`).

### P5-C3B / P5-C3C: Admin UI & Reports Engine
- Account administration UI (`ACC-01`), `SystemConfiguration` editor (`SYS-01`).
- Report views (`RPT-01` demographic distribution, `RPT-02` household summary, `RPT-03` movement report) with CSV or print preview export.
- Dynamic screen visibility: hide Admin screens from `OFFICER` accounts.

---

## 6. Team Leader Brief

| Task | Objective |
|---|---|
| **P1-TL1 ✅** | Solution structure, layer boundaries, base abstractions, audit standard, unit test harness — **Done**. |
| **P2-TL1** | Architectural review of Coder 1's vertical slice. Verify layer boundary, `Result<T>` pattern, and test coverage before approving merge. |
| **P3-TL1** | Checkpoint 2 Review: Verify Golden Invariant (`vw_GoldenInvariantViolations` = 0 rows) after household operations. |
| **P4-TL1** | Transfer engine review: inspect serializable isolation, edge case handling, and audit correlation. |
| **P5-TL1** | Enforce standardized audit logging across all services. |
