# Pull Request Review & Quality Checklist

**Document Code:** `STD-PR-v2.0`  
**Owner:** Team Leader (Sole Reviewer for `main` / `develop`)  
**Derived From:** [`../workflow/work-breakdown.md`](../workflow/work-breakdown.md) §6 (DoD) · [`coding-conventions.md`](coding-conventions.md)

---

## 1. Author Self-Check (Before Requesting Review)

- [ ] **PR Hygiene:** Branch follows naming convention `feature/<task-id>` (e.g., `feature/p1-c1`).
- [ ] **Clean Build:** `dotnet build Aris.slnx` compiles with **0 errors and 0 warnings**.
- [ ] **Tests Green:** All unit tests pass in `Aris.UnitTests`; integration tests pass in `Aris.Tests` without failures (never pass `--nologo`).
- [ ] **Golden Invariant Check:** Executing `SELECT * FROM dbo.vw_GoldenInvariantViolations;` returns **0 rows**.
- [ ] **No Secrets:** No connection strings, passwords, `.env` files, or `bin/`/`obj/` binaries committed.

---

## 2. Reviewer Checklist (Team Leader)

### 2.1. Architecture & Layer Boundaries
- [ ] UI code contains **zero** direct SQL, **zero** `DbContext` usage, and no business validation rules.
- [ ] Dependency direction is strictly respected: `UI → Application → Domain` and `Infrastructure → Application/Domain`.
- [ ] Services return `Result<T>` for business outcomes rather than throwing raw exceptions.
- [ ] Validation is performed using FluentValidation **prior** to opening database transactions.

### 2.2. Database & Schema Conformance
- [ ] All table and column names exactly match [`aris_schema_v2.sql`](../aris_schema_v2.sql).
- [ ] **No `Citizen.HouseholdId` or `Household.CurrentAddressId`** are referenced or introduced.
- [ ] EF Core mappings declare `.ToTable(t => t.HasTrigger("..."))` for the 5 tables governed by triggers.
- [ ] Mutable entity mappings declare `.IsRowVersion()` on `RowVersion`.
- [ ] Computed column `Address.AddressText` is mapped via `.HasComputedColumnSql()`.

### 2.3. Temporal & Append-Only History Rules
- [ ] `HouseholdMembership`, `Residence`, and `HouseholdAddress` are **never updated in place**. Changes use close-and-open:
  $$\text{Close Old Row: } EndDate = E - 1, \; Status = \text{'ENDED'}$$
  $$\text{Open New Row: } StartDate = E, \; Status = \text{'ACTIVE'}$$
- [ ] Effective date condition holds: $E > StartDate$ of any record being closed.
- [ ] Historical rectifications transition rows to `VOID` and link new entries via `Corrects...Id`.

### 2.4. Maker-Checker Transfers
- [ ] Dual-control constraint enforced: `DecidedBy <> RequestedBy` (`CK_TR_Decision`).
- [ ] State remains completely unchanged while `TransferRequest` is in `PENDING` status.
- [ ] Individual item status resolution supported (`TransferRequestItem.Status`).
- [ ] Execution runs inside an atomic transaction with `IsolationLevel.Serializable`.

### 2.5. Auditing & Security
- [ ] Every mutation logs an `AuditLog` entry detailing Who, What, When, Entity, Result, and JSON details.
- [ ] Audit logs are append-only (`INSERT` only); no `UPDATE` or `DELETE` statements.
- [ ] In `catch` blocks, transaction is rolled back first, and `LogFailureIndependentAsync` logs the failure on an **independent connection**.
- [ ] Passwords hashed with salt (PBKDF2/BCrypt, never plaintext).
- [ ] Service layer enforces role-based authorization (`OFFICER` vs `ADMIN`).

---

## 3. The 12 Automatic Rejection Triggers

Any single pattern below results in an **immediate request for changes**:

| # | Pattern in the Diff | Rejection Reason |
|---|---|---|
| **1** | `citizen.HouseholdId = ...` | Field does not exist in Schema v2; destroys historical lineage. |
| **2** | `household.CurrentAddressId = ...` | Field removed in Schema v2; address history would be lost. |
| **3** | In-place update `membership.Role = "HEAD"` | Trigger `trg_Membership_Guard` aborts with error 51011. |
| **4** | Citizen created directly as `Status = "ACTIVE"` | Violates Golden Invariant checks C1 and C2. |
| **5** | Relocation mutations split across multiple transactions | Breaks atomicity and serializable concurrency isolation. |
| **6** | `UPDATE` or `DELETE` on `dbo.AuditLog` | Trigger `51030` and role `aris_app` `DENY` reject the statement. |
| **7** | Failure audit logged inside rolled-back transaction | Transaction rollback discards the audit record. |
| **8** | Self-approval allowed (`DecidedBy == RequestedBy`) | Check constraint `CK_TR_Decision` aborts the transaction. |
| **9** | Raw EF Core `_context` or SQL query in UI Forms | Violates architectural layering boundaries. |
| **10** | Overlapping dates ($StartDate_2 < EndDate_1$) | Trigger `trg_Residence_Guard` aborts with error 51002. |
| **11** | Committed credentials, secrets, `.env`, or build binaries | Violates security and repository hygiene (`CON-SEC-02`). |
| **12** | Direct commit to `main` without PR review | Violates Git workflow policy. |

---

## 4. Reviewer Sign-Off Template

```text
REVIEW: ARIS PR #<number> — <title>
Reviewer: <name>                       Date: <yyyy-mm-dd>
Branch: feature/<module-name>  ->  main

Verification evidence:
  dotnet build Aris.slnx ................. 0 errors / 0 warnings
  automated tests ........................ <pass count> passing
  SELECT * FROM dbo.vw_GoldenInvariantViolations;  -> 0 rows
  trigger/view inventory ................. 5 triggers / 3 views

Requirement IDs verified: <e.g. CIT-01, HH-01, HH-04, BR-RES-07, BR-AUD-03>

Result: APPROVE | REQUEST CHANGES
Blocking items: <none, or list>
Non-blocking suggestions: <none, or list>
```
