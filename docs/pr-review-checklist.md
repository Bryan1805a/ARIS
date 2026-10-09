# Pull Request Review & Merge Checklist
## ARIS — Administrative Residence Information System

**Version:** 1.0  
**Owner:** Team Leader — sole designated reviewer for all Pull Requests  
**Applies to:** every Pull Request targeting `develop`  
**Derived from:** [`work-breakdown.md`](work-breakdown.md) §6 (Definition of Done) and §7 (Git Workflow) · [`srs.md`](srs.md) §10 (Acceptance Checklist)  
**Reference material:** [`database_design_v2.md`](database_design_v2.md) · [`aris_schema_v2.sql`](aris_schema_v2.sql) · [`ai-coding-guidelines.md`](ai-coding-guidelines.md)

---

## 0. How To Use This Checklist

- **Author:** complete Section 1 (self-check) and paste the evidence into the PR description **before** requesting review.
- **Reviewer (Team Leader):** work through Sections 2–10, then complete Section 11 before merging.
- **A single unchecked box is a blocking request for changes.** Do not approve "with follow-ups" for architectural or invariant items.
- Items that genuinely do not apply must be marked `N/A` **with a one-line reason** — silent skips are treated as unreviewed.
- Requirement, rule and constraint IDs below (`CIT-01`, `BR-RES-07`, `CON-SEC-02`, …) refer to [`srs.md`](srs.md) and must be quoted in review comments so traceability survives.

---

## 1. Author Self-Check (Before Requesting Review)

### 1.1. PR Hygiene
- [ ] Branch follows the naming rule `feature/<module-name>` and is **not** `main` or `develop` (`work-breakdown.md` §7, rule 1).
- [ ] Branch has been rebased/merged with the latest `develop`; no unresolved merge conflicts.
- [ ] PR description states: what changed, which requirement IDs it satisfies (`CIT-01`, `HH-05`, `RPT-02`, …), and how to reproduce the verification locally.
- [ ] UI changes include screenshots of the affected screens (default and error states).
- [ ] Commit messages are meaningful and scoped (e.g. `feat(household): close-and-open head reassignment (HH-04)`).
- [ ] No secrets, connection strings, `.env` files, or `bin/`/`obj/` artifacts are included (`CON-SEC-02`).

### 1.2. Build & Test Evidence
- [ ] `dotnet build Aris.slnx` succeeds with **0 errors and 0 warnings**.
- [ ] All automated tests pass locally; the raw output is pasted into the PR.
- [ ] The Golden Invariant check returns **0 rows** after the feature's happy-path scenario:
      `SELECT * FROM dbo.vw_GoldenInvariantViolations;`
- [ ] Self-review of the full diff has been performed on GitHub before review is requested.

---

## 2. Architecture & Layer Boundaries

| Check | Rule / ID |
|---|---|
| [ ] UI code contains **zero** SQL, **zero** `DbContext` usage, and **zero** business-validation logic; the UI talks only to Application services via DTOs and `Result<T>`. | `srs.md` §4.1 rule, DoD §2 |
| [ ] Dependency direction is respected: `UI → Application → Domain` and `Infrastructure → Application/Domain`. No `Application → UI` or `Domain → Infrastructure` references were introduced. | SRS §4.1 |
| [ ] New services follow the canonical `Result<T>` pattern: expected failures are **returned**, not thrown. | `ai-coding-guidelines.md` §4.1 |
| [ ] Validation happens in FluentValidation validators **before** the transaction opens. | DoD §1 |
| [ ] No new project, package, or layer was added without Team Leader approval. | WBS RACI |

---

## 3. Database & Schema Conformance

| Check | Rule / ID |
|---|---|
| [ ] Every table, column, index and view name matches [`aris_schema_v2.sql`](aris_schema_v2.sql) — no hallucinated or v1-era fields. | `ai-coding-guidelines.md` §3 |
| [ ] **No `Citizen.HouseholdId` and no `Household.CurrentAddressId`.** Membership lives in `HouseholdMembership`; address history lives in `HouseholdAddress`. | `database_design_v2.md` §2 rows 4a–4b · D3–D4, `srs.md` §10 |
| [ ] EF Core mappings declare `.ToTable(t => t.HasTrigger("…"))` for every table carrying a trigger, otherwise `SaveChanges` fails. | `database_design_v2.md` §8, item 4 |
| [ ] Every mutable entity maps concurrency with `.IsRowVersion()`; new mutable tables include `RowVersion`. | `CON-CONC-01`, D14 |
| [ ] `AddressText` is treated as a computed column (never assigned). | D8, `database_design_v2.md` §8, item 4 |
| [ ] Any change to `aris_schema_v2.sql` still runs cleanly on an **empty** database, and the PR documents the re-deploy step for existing dev databases. | script header, `database_design_v2.md` §8, item 1 |

---

## 4. Domain Rules & the Golden Invariant

### 4.1. Append-Only History
- [ ] `HouseholdMembership`, `Residence`, and `HouseholdAddress` are **never** updated in place: role/date changes are implemented as *close old row* (`EndDate = E - 1`, `Status = 'ENDED'`) + *open new row* (`StartDate = E`, `Status = 'ACTIVE'`). (`BR-HIST-01`, `BR-HH-06`)
- [ ] Errors are corrected via `VOID` + a new row referencing `Corrects…Id` — never by editing the faulty row. (`BR-HIST-02`, `HIST-01`)
- [ ] Identity fields (`CitizenId`, `HouseholdId`, `AddressId`, `Role`, `StartDate`) are never mutated.

### 4.2. Temporal Rules
- [ ] Effective date sequencing holds: `E > StartDate` of any record being closed; the closed record ends at `E - 1`. (`BR-RES-07`)
- [ ] No overlapping intervals can be produced for the same citizen or household — the guard triggers (51002 / 51012 / 51022) stay silent on legitimate data. (`BR-RES-03`)
- [ ] Past residence/household records stay linked to their **original** `AddressId`; address reorganisation uses `ReplacedByAddressId` + `vw_AddressCanonical`. (`BR-RES-06`, `BR-RES-02`, D9)

### 4.3. Household, Residence & Citizen Lifecycle
- [ ] New citizens are created as `UNASSIGNED`; only `HH-01`/`HH-02`/transfer commit may set `ACTIVE`. (`BR-CIT-03`, `CIT-01`)
- [ ] An active household has exactly **one** active `HEAD`, who is an active member of that household. (`BR-HH-01`, `BR-HH-02`)
- [ ] An active household has exactly **one** active `HouseholdAddress`, resolving to an active canonical address. (`BR-HH-08`, `UX_HA_OneActivePerHousehold`, `vw_HouseholdCurrent`)
- [ ] A citizen holds at most one active membership and one active residence system-wide. (`BR-HH-03`, `BR-RES-01`)
- [ ] Leaving a household is only possible through Transfer (`HH-05`) or Deactivation (`CIT-04`); `HH-03` exposes no standalone "remove member" path. (`srs.md` §8.4)
- [ ] When the last member leaves, the household becomes `INACTIVE` and its active `HouseholdAddress` is closed. (`BR-HH-04`)
- [ ] A departing head with remaining members requires an explicitly designated `SuccessorHeadCitizenId` — never auto-selected. (`BR-HH-07`, A3)
- [ ] `RES-01`/`RES-02` execute **only** as internal atomic steps of composite flows, never as standalone operations. (`srs.md` §8.5)

### 4.4. Maker-Checker (Transfers)
- [ ] A `TransferRequest` must be approved by an officer other than the requester: `DecidedBy <> RequestedBy`. (`BR-VER-01`, `CK_TR_Decision`, E2)
- [ ] While `PENDING`, no membership/residence/household state is modified. (`BR-VER-02`)
- [ ] Each candidate has an independent item status; partial approvals are supported. (`BR-VER-04`, A4)
- [ ] Every record created by a transfer carries `OpenedByRequestId`. (`BR-VER-03`)
- [ ] A citizen may hold at most one open `PENDING` transfer item. (`UX_TRI_OneOpenPerCitizen`)
- [ ] Decided requests are frozen — no post-decision edits. (`trg_TransferRequest_Frozen`, 51040)

### 4.5. Invariant Verification
- [ ] All 9 checks (C1–C5, H1–H4) in `vw_GoldenInvariantViolations` are exercised by the feature's tests, and the view returns **0 rows** after every committed step. (`srs.md` §6.4, §10)

---

## 5. Transactions, Concurrency & Failure Handling

- [ ] Every multi-table transition runs inside a single atomic transaction. (`BR-CON-01`, `CON-TRANS-01`)
- [ ] Transfer execution uses `IsolationLevel.Serializable`; the isolation level is set on the same `DbContext`/connection that performs the writes. (`BR-CON-03`)
- [ ] The transaction has no partial commit paths, and `SaveChanges` is not called outside the transaction scope.
- [ ] Optimistic concurrency conflicts (`DbUpdateConcurrencyException`, E3) are caught and surfaced to the officer as a "data changed — reload" prompt, not as an unhandled crash. (`BR-CON-02`)
- [ ] On failure the transaction is rolled back **first**, and the `FAILURE` audit entry is written afterwards over an **independent** connection, so the rollback cannot discard it. (`BR-AUD-03`, E4)
- [ ] Boundary/negative flows are handled: invalid or inactive IDs (E1), empty fields, past or same-day effective dates, destination address missing (A5). (DoD §1)

---

## 6. Audit Logging & Security

- [ ] Every business mutation (create, update, transfer, void, login attempt) writes an `AuditLog` row with Who / What / When / Entity / Result and before-after JSON. (`BR-AUD-01`)
- [ ] No code path issues `UPDATE` or `DELETE` against `AuditLog`; access is `INSERT` + `SELECT` only. (`BR-AUD-02`, `trg_AuditLog_AppendOnly`, 51030)
- [ ] Related audit rows share a `CorrelationId` across the request/execution pair.
- [ ] Passwords are never stored or logged in plaintext; hashing is salted (PBKDF2/BCrypt). (`CON-SEC-01`, `AUTH-01`)
- [ ] Login enforces `MAX_FAILED_LOGINS` lockout via `FailedLoginCount`/`LockedUntil` and honours `MustChangePassword`. (`AUTH-01`, `SYS-01`)
- [ ] Role-based authorization is enforced in the **service layer**, not only by hiding UI controls. (`CON-AUTH-02`, `PERM-01`)
- [ ] Secrets/connection strings are read from user-secrets or environment variables; the committed `appsettings.json` keeps `ConnectionStrings:ArisDb` empty. (`CON-SEC-02`)
- [ ] Application DB access uses the least-privilege `aris_app` role rather than `sa`. (D12)

---

## 7. UI & Presentation

- [ ] Forms/ViewModels call application services only; no direct `DbContext` or raw SQL anywhere in `Aris.UI`. (`ai-coding-guidelines.md` §3, trap 9)
- [ ] Long-running operations keep the UI responsive (async/await, busy indicator) and never block the message loop.
- [ ] Validation errors are shown inline and in plain language; the user is never confronted with a raw exception dialog.
- [ ] Admin-only screens (Manage Accounts, System Configuration, Audit Logs) are hidden from `OFFICER` sessions. (`PERM-01`)
- [ ] Screens remain usable at the minimum supported window size; no clipped or overlapping controls were introduced.
- [ ] Grid/list views are backed by server-side or paged queries, not by loading whole tables (`_context.Citizens.ToList()`).

---

## 8. Tests & Verification Evidence

### 8.1. Required Negative Tests (each must be **rejected**)
- [ ] Two `ACTIVE` residences for the same citizen.
- [ ] Two `ACTIVE` heads in the same household.
- [ ] Two overlapping residence date intervals for the same citizen.
- [ ] `UPDATE` on an `ENDED` record.
- [ ] `UPDATE` or `DELETE` on `AuditLog`.
- [ ] A transfer request where `DecidedBy == RequestedBy`.
- [ ] A transfer request item referencing a residence belonging to a different citizen.

*(`database_design_v2.md` §8, item 2 — the expected failure is the documented trigger error code, see Appendix A.)*

### 8.2. Required Positive Tests
- [ ] One successful test per transfer shape: `JOIN_EXISTING`, `NEW_HOUSEHOLD`, `HOUSEHOLD_MOVE`. (`BR-HH-05`)
- [ ] After each successful scenario: `vw_GoldenInvariantViolations` returns **0 rows**.
- [ ] Citizen lifecycle transitions (`UNASSIGNED → ACTIVE → INACTIVE`) are covered, including head succession on deactivation. (`CIT-04`)
- [ ] Void-and-rectify (`HIST-01`) leaves the voided row frozen and the corrected row linked.
- [ ] Concurrency test: two officers modify the same household → `DbUpdateConcurrencyException` handled. (`P7-CONC`)
- [ ] Post-rollback failure logging produces a `FAILURE` audit row. (`P7-ERR`)

### 8.3. Test Quality
- [ ] Tests assert on domain outcomes (statuses, dates, invariant view), not merely on "no exception thrown".
- [ ] Integration tests run against the real SQL Server schema (`aris_schema_v2.sql`), not a substituted in-memory provider, wherever triggers or views are involved.
- [ ] New tests are deterministic and independent of execution order; no reliance on pre-existing database rows beyond seeded `SystemConfiguration`.

---

## 9. Documentation & Traceability

- [ ] The PR links the requirement/use-case IDs it implements and marks the relevant WBS task IDs (`P2-C1`, `P4-C2C`, …) as advanced.
- [ ] User-visible behaviour changes are reflected in the affected doc ([`srs.md`](srs.md), [`database_design_v2.md`](database_design_v2.md), [`work-breakdown.md`](work-breakdown.md)) in the same PR.
- [ ] Terminology matches the current project name and identifiers: **ARIS**, `Aris.*` projects, `ArisDbContext`, `ArisDb`, `aris_app`, `aris_schema_v2.sql`.
- [ ] New configuration keys are added to `SystemConfiguration` seeds and documented.
- [ ] No `TODO` without an owner and a linked task ID is merged into `develop`.

---

## 10. Automatic Rejection Triggers

Any single item below is an **immediate request for changes**, regardless of how well the rest of the PR scores:

| # | Pattern in the diff | Why it is rejected |
|---|---|---|
| 1 | `citizen.HouseholdId = …` | Field does not exist in Schema v2; breaks historical tracking. |
| 2 | `household.CurrentAddressId = …` | Removed in v2; address history would be lost. |
| 3 | In-place `membership.Role = "HEAD"` | `trg_Membership_Guard` aborts with 51011. |
| 4 | Citizen created directly as `ACTIVE` | Violates the Golden Invariant (C1/C2). |
| 5 | Multi-step transfer across separate transactions or driven from UI code | Breaks atomicity and serializable isolation. |
| 6 | `UPDATE`/`DELETE` on `AuditLog` | `DENY` + `trg_AuditLog_AppendOnly` (51030). |
| 7 | `FAILURE` audit written inside the rolled-back transaction | The rollback discards the evidence. |
| 8 | Self-approval (`DecidedBy == RequestedBy`) | `CK_TR_Decision` rejects it. |
| 9 | Raw EF/SQL access from a Form or View | Destroys the layer boundary; use the Application service. |
| 10 | Overlapping date intervals (`StartDate₂ < EndDate₁`) | `trg_Residence_Guard` throws 51002. |
| 11 | Committed secrets, connection strings, `.env`, `bin/`/`obj/` | `CON-SEC-02`. |
| 12 | Direct commit to `main` or `develop`, or a PR opened against `main` | `work-breakdown.md` §7, rule 1. |

---

## 11. Merge Checklist (Reviewer Only)

- [ ] Sections 1–10 reviewed; all blocking items resolved or explicitly deferred by the Team Leader with a linked task ID.
- [ ] PR targets `develop` (never `main`) and the branch is up to date with `develop`.
- [ ] `dotnet build Aris.slnx` was re-run on the **merge commit**: 0 errors, 0 warnings.
- [ ] All automated tests pass on the merge commit.
- [ ] `SELECT * FROM dbo.vw_GoldenInvariantViolations;` returns **0 rows** on the shared integration database after merge.
- [ ] If the schema changed: `aris_schema_v2.sql` was re-deployed to the shared database and trigger/view counts verified (`expect 5 triggers`, 3 views).
- [ ] Every review conversation is resolved, not just acknowledged.
- [ ] Feature branch is deleted after merge.
- [ ] `main` is only updated by a reviewed, verified promotion from `develop` at a phase or submission gate.
- [ ] Contributors are informed when the merge affects their in-flight branch.

---

## Appendix A — Trigger Error Code Reference

| Code | Meaning | Trigger |
|---|---|---|
| 51001 / 51011 / 51021 | Append-only violation (Residence / Membership / HouseholdAddress) | `trg_Residence_Guard` / `trg_Membership_Guard` / `trg_HouseholdAddress_Guard` |
| 51002 / 51012 / 51022 | Temporal interval overlap (Residence / Membership / HouseholdAddress) | same guards as above |
| 51030 | Attempted modification or deletion of `AuditLog` | `trg_AuditLog_AppendOnly` |
| 51040 | Attempted modification of a decided `TransferRequest` | `trg_TransferRequest_Frozen` |

---

## Appendix B — Verification Commands

```sql
-- Golden Invariant: must return 0 rows after every committed operation
SELECT * FROM dbo.vw_GoldenInvariantViolations;

-- Deployed guardrail inventory: expect 5 triggers and 3 views
SELECT name FROM sys.triggers WHERE parent_class = 1 ORDER BY name;
SELECT name FROM sys.views ORDER BY name;
```

```powershell
# Build gate
dotnet build Aris.slnx

# Run the desktop shell against the local container
docker compose up -d
dotnet run --project src/Aris.UI
```

---

## Appendix C — Reviewer Sign-Off Template

```text
REVIEW: ARIS PR #<number> — <title>
Reviewer: <name>                       Date: <yyyy-mm-dd>
Branch: feature/<module-name>  ->  develop

Verification evidence:
  dotnet build Aris.slnx ................. 0 errors / 0 warnings
  automated tests ........................ <pass count> passing
  SELECT * FROM dbo.vw_GoldenInvariantViolations;  -> 0 rows
  trigger/view inventory ................. 5 triggers / 3 views

Requirement IDs verified: <e.g. HH-01, HH-04, BR-RES-07, BR-AUD-03, CON-SEC-02>

Result: APPROVE  |  REQUEST CHANGES
Blocking items: <numbered list, or "none">
Non-blocking suggestions: <numbered list, or "none">
Follow-up tasks filed: <task IDs, or "none">
```
