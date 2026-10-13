# Pull Request Review & Quality Gate Checklist

**Document Code:** `STD-PR-v2.0`  
**Review Owner:** Team Leader (Sole Merge Approver for `main` and `develop`)  
**Derived From:** [`../workflow/work-breakdown.md`](../workflow/work-breakdown.md) §5 (DoD) · [`coding-conventions.md`](coding-conventions.md)

---

## 1. Author Self-Check (Pre-Submission Gate)

Before submitting a Pull Request for team review, the author must confirm:

- [ ] **Branch Naming:** Branch strictly conforms to `feature/<task-id>` (e.g. `feature/p1-c1`, `feature/p2-c1`).
- [ ] **Clean Compilation:** `dotnet build Aris.slnx` completes with **0 errors and 0 warnings**.
- [ ] **Automated Test Pass:** All tests pass in `Aris.UnitTests` and `Aris.Tests` (never pass `--nologo`).
- [ ] **Golden Invariant Check:** Executing `SELECT * FROM dbo.vw_GoldenInvariantViolations;` returns **0 rows**.
- [ ] **Repository Hygiene:** No connection strings, passwords, `.env` files, or `bin/`/`obj/` build artifacts are staged.

---

## 2. Reviewer Checklist (Team Leader Review Gate)

### 2.1. Architectural & Layer Boundaries
- [ ] UI code contains **zero** direct SQL, **zero** `DbContext` access, and no business validation rules.
- [ ] Dependency direction strictly flows inward: `UI → Application → Domain` and `Infrastructure → Application/Domain`.
- [ ] Application services return [`Result<T>`](file:///C:/Users/Bryan/Documents/ARIS/src/Aris.Application/Common/Result.cs) for expected business outcomes rather than throwing raw exceptions.
- [ ] Input validation executes via FluentValidation **prior** to opening database transactions.

### 2.2. Database & Schema Conformance
- [ ] All table and column names match [`aris_schema_v2.sql`](../aris_schema_v2.sql) verbatim.
- [ ] **No `Citizen.HouseholdId` or `Household.CurrentAddressId`** are introduced or referenced.
- [ ] Entity mappings in `Aris.Infrastructure` declare `.ToTable(t => t.HasTrigger("..."))` on the 5 trigger tables.
- [ ] All 9 mutable entities map `.IsRowVersion()` on `RowVersion`.
- [ ] Computed column `Address.AddressText` is mapped via `.HasComputedColumnSql(...)`.

### 2.3. Temporal & Append-Only History Rules
- [ ] `HouseholdMembership`, `Residence`, and `HouseholdAddress` are **never updated in place**. Modifications adhere strictly to close-and-open:
  $$\text{Close Old Row: } EndDate = E - 1, \; Status = \text{'ENDED'}$$
  $$\text{Open New Row: } StartDate = E, \; Status = \text{'ACTIVE'}$$
- [ ] Effective date condition holds: $E > StartDate$ of any record being closed.
- [ ] Historical rectifications transition rows to `VOID` and link new entries via `Corrects...Id`.

### 2.4. Maker-Checker Transfer Workflow
- [ ] Dual-control constraint enforced: `DecidedBy <> RequestedBy` (`CK_TR_Decision`).
- [ ] System state remains completely untouched while `TransferRequest` is in `PENDING` status.
- [ ] Granular line-item decisions supported (`TransferRequestItem.Status`).
- [ ] Execution executes inside an atomic transaction at `IsolationLevel.Serializable`.

### 2.5. Auditing & Security
- [ ] Every mutation writes an `AuditLog` row detailing Actor, Action, Entity, Result, and JSON details.
- [ ] Audit logs are append-only (`INSERT` only); no `UPDATE` or `DELETE` statements.
- [ ] In `catch` blocks, the transaction is rolled back first, and `LogFailureIndependentAsync` logs the failure on an **independent database connection**.
- [ ] Passwords hashed with salt (PBKDF2/BCrypt, never plaintext).
- [ ] Service layer enforces role authorization (`OFFICER` vs `ADMIN`).

---

## 3. The 12 Automatic Rejection Traps

Any single pattern below results in an **immediate, non-negotiable request for changes**:

| # | Code Pattern in Diff | Rejection Rationale |
|---|---|---|
| **1** | `citizen.HouseholdId = ...` | Field does not exist in Schema v2; destroys historical lineage. |
| **2** | `household.CurrentAddressId = ...` | Field removed in Schema v2; address history would be lost. |
| **3** | In-place update `membership.Role = "HEAD"` | Trigger `trg_Membership_Guard` aborts with error 51011. |
| **4** | Citizen created directly as `Status = "ACTIVE"` | Violates Golden Invariant checks C1 and C2. |
| **5** | Relocation mutations split across multiple transactions | Breaks atomicity and serializable concurrency isolation. |
| **6** | `UPDATE` or `DELETE` on `dbo.AuditLog` | Trigger `51030` and role `aris_app` `DENY` reject the statement. |
| **7** | Failure audit logged inside rolled-back transaction | Transaction rollback discards the audit record. |
| **8** | Self-approval permitted (`DecidedBy == RequestedBy`) | Check constraint `CK_TR_Decision` aborts the transaction. |
| **9** | Raw EF Core `_context` or SQL query in UI Forms | Violates architectural layering boundaries. |
| **10** | Overlapping dates ($StartDate_2 < EndDate_1$) | Trigger `trg_Residence_Guard` aborts with error 51002. |
| **11** | Committed credentials, secrets, `.env`, or build binaries | Violates security and repository hygiene (`CON-SEC-02`). |
| **12** | Direct commit to `main` or `develop` without PR review | Violates Git team workflow rules. |

---

## 4. Standard Reviewer Sign-Off Template

Copy this template into the GitHub Pull Request review comment upon completion:

```text
REVIEW: ARIS PR #<number> — <title>
Reviewer: <name>                       Date: <yyyy-mm-dd>
Branch: feature/<module-name>  ->  develop / main

Verification evidence:
  dotnet build Aris.slnx ................. 0 errors / 0 warnings
  automated tests ........................ <pass count> passing
  SELECT * FROM dbo.vw_GoldenInvariantViolations;  -> 0 rows
  trigger/view inventory ................. 5 triggers / 3 views

Requirement IDs verified: <e.g. CIT-01, HH-01, HH-04, BR-RES-07, BR-AUD-03>

Result: [APPROVE | REQUEST CHANGES]
Blocking items: <none, or list>
Non-blocking suggestions: <none, or list>
```
