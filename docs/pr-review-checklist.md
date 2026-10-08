# Pull Request Review Checklist
## Residence Information Management System

**Owner:** Team Leader (designated reviewer for all PRs).  
**Applies to:** every Pull Request targeting `develop`.  
**Sources:** [`ai-coding-guidelines.md`](ai-coding-guidelines.md) · [`work-breakdown.md`](work-breakdown.md) §6–7 · [`srs.md`](srs.md) · [`database_design_v2.md`](database_design_v2.md) · [`residence_schema_v2.sql`](residence_schema_v2.sql).

> A PR may only be merged when **every box below is ticked** or an explicit,
> written waiver is recorded in the PR comments.

---

## 0. Before Opening the PR (Author)

- [ ] Branch is named `feature/<module-name>` and was branched from `develop`.
- [ ] The solution builds with **0 warnings, 0 errors** (`dotnet build ResidenceManagement.slnx`).
- [ ] All new/updated tests pass locally.
- [ ] No secrets, connection strings, or `.env` files are committed (`CON-SEC-02`).
- [ ] Commit messages are meaningful and scoped.
- [ ] PR description is filled in (use the template at the end of this document).
- [ ] UI changes include a screenshot or short recording.

---

## 1. Architecture & Layer Boundaries

- [ ] **No SQL or `DbContext` in UI code** — no `using (var context = ...)`, no LINQ-to-Entities, no raw SQL in forms/event handlers (`ai-coding-guidelines` §3 #9).
- [ ] UI talks to the Application layer **only** through service interfaces, DTOs, and `Result<T>`.
- [ ] No business rules or validation logic inside `button_Click` / event handlers (`srs.md` §4.1).
- [ ] `Domain` has no dependency on `Application`, `Infrastructure`, or UI.
- [ ] `Infrastructure` is referenced only at the composition root (DI), not by UI forms directly.

## 2. Schema Fidelity (No Hallucinations)

- [ ] Every table/column name matches `residence_schema_v2.sql`.
- [ ] **No `Citizen.HouseholdId`** and **no `Household.CurrentAddressId`** anywhere.
- [ ] `Citizen.Status` is only `UNASSIGNED` / `ACTIVE` / `INACTIVE`; new citizens start `UNASSIGNED`.
- [ ] No in-place `UPDATE` of `Role`, `CitizenId`, `HouseholdId`, `AddressId`, or `StartDate` — role change = close old row + open new row.
- [ ] Record corrections use the `VOID` + `Corrects...Id` pattern (no in-place edits).

## 3. Transactions, Dates & Concurrency

- [ ] Every multi-table state transition runs inside a **single transaction** (`BR-CON-01`).
- [ ] Transfers run with `IsolationLevel.Serializable` (`BR-CON-03`, `CON-TRANS-01`).
- [ ] Closed records use `EndDate = EffectiveDate - 1` and `E` is strictly greater than the record's `StartDate` (`BR-RES-07`).
- [ ] Optimistic concurrency is respected — `RowVersion` checked; `DbUpdateConcurrencyException` handled (`CON-CONC-01`).
- [ ] EF Core: tables with triggers use `.ToTable(t => t.HasTrigger("..."))`; `RowVersion` uses `.IsRowVersion()`; `AddressText` uses `.HasComputedColumnSql()`.

## 4. Auditing (`BR-AUD-*`)

- [ ] Every business mutation writes an `AuditLog` entry (Who / What / When / Entity / Result / before-after JSON).
- [ ] No `UPDATE` or `DELETE` against `AuditLog` (append-only).
- [ ] `FAILURE` audit entries are written **after rollback, on an independent connection** (`ai-coding-guidelines` §3 #7).
- [ ] Related entries share a `CorrelationId`.

## 5. Maker-Checker (`BR-VER-*`)

- [ ] `DecidedBy != RequestedBy` (self-approval blocked; `CK_TR_Decision`).
- [ ] Request payload matches `CK_TR_Shape` for its `RequestType`.
- [ ] Preconditions are **re-validated against committed state** at execution time.
- [ ] Individual items can be approved/rejected independently (`BR-VER-04`).

## 6. Data Integrity Verification

- [ ] `SELECT * FROM dbo.vw_GoldenInvariantViolations;` returns **0 rows** after the feature's operations.
- [ ] Relevant trigger/constraint integration tests exist and pass (append-only, overlap, self-approval, audit immutability).
- [ ] Golden Invariant holds for every new code path, including partial-approval and head-succession edge cases.

## 7. Code Quality & Tests

- [ ] Input validated with FluentValidation **before** service dispatch.
- [ ] Expected failures return `Result` / `Result<T>`; exceptions are reserved for unexpected faults.
- [ ] Code follows the repo `.editorconfig` and existing naming/style conventions.
- [ ] New logic has unit/integration test coverage; tests are deterministic.
- [ ] No commented-out dead code or leftover TODO scaffolding (tracked TODOs must reference a task ID).

---

## Review Decision

The reviewer records **one** of:

- **Approve** — all boxes ticked; safe to merge.
- **Request changes** — list each unmet item with the specific rule ID (e.g. `BR-HIST-01`, `CON-TRANS-01`) and file/line.

## Merge Criteria

- [ ] PR targets `develop` (never `main` directly).
- [ ] Review approved by the Team Leader.
- [ ] Build and tests green.
- [ ] Branch is up to date with `develop` (rebase or merge; resolve conflicts before merge).
- [ ] CI workflow (if present) passes.

---

## PR Description Template

```markdown
## Summary
<what changed and why>

## Related rules / tasks
<E.g. HH-04, BR-HH-06, P3-C2B>

## How it was tested
<steps, test names, or SQL queries run>

## Golden Invariant
- [ ] `vw_GoldenInvariantViolations` returns 0 rows after the new flow

## Screenshots (UI changes)
<attach here>

## Checklist
- [ ] Builds with 0 warnings / 0 errors
- [ ] No SQL / DbContext in UI
- [ ] Transactions + audit handled
- [ ] No schema hallucinations
```
