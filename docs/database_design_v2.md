# Database Design v2 (Final) — Residence Information Management System

> **Companion Files:** `residence_schema_v2.sql` (DB creation script, SQL Server 2019+) · `erd_v2.png` / `erd_v2.svg` (ERD) · this document (rationale and rules).  
> **Status:** Draft v2.0 — pending team review and execution verification on SQL Server (see Section 8).

---

## 1. Quick Reference

The design divides data protection into 4 concentric layers, with the cheapest enforcement executing first:

| Layer | Mechanism | What It Prevents |
|---|---|---|
| 1 | Data types, `NOT NULL`, `CHECK` constraints | Invalid values, self-contradictory state (e.g., `ACTIVE` record with an `EndDate`) |
| 2 | `FK`, `UNIQUE`, filtered unique indexes | "At most one ACTIVE record per entity" |
| 3 | Triggers | Append-only history integrity, temporal overlaps, immutable audit logs |
| 4 | View `vw_GoldenInvariantViolations` + Application layer | Complex cross-table invariants that SQL Server constraints cannot natively express |

Golden Rule when coding: **every business state transition must run within a single transaction**, and after each operation (during automated tests), execute `SELECT * FROM dbo.vw_GoldenInvariantViolations;` — the result must return **0 rows**.

---

## 2. ERD v1 Issues & Resolutions

| # | Issue in v1 | Resolution | Code |
|---|---|---|---|
| 1 | Isolated FRs (End Residence, Remove Member…) break the Golden Invariant; `CIT-01` creates an ACTIVE citizen without any household | Introduce `UNASSIGNED` status; isolated actions become internal atomic steps of composite use cases | D2 |
| 2 | Main flow lacks PENDING state; single role handles everything; `VerifiedBy` duplicated across 3 tables; lacks traceability | Maker-checker for Transfer; remove `Verified*` from Membership/Residence, replaced with `OpenedByRequestId` + `CreatedBy` | D1, D6 |
| 3 | `TransferRequest` lacks CitizenId, EffectiveDate, new/successor head; 1 request = 1 citizen; redundant `DestinationAddressId` | Separate into `TransferRequest` + `TransferRequestItem`; add missing columns; enforce `CK_TR_Shape` per request type | D5 |
| 4a | Changing household head via `UPDATE Role` erases history | Close previous record, open a new record (append-only) | D3 |
| 4b | `Household.CurrentAddressId` is overwritten, losing household address history | Introduce `HouseholdAddress` table; drop `Household.CurrentAddressId` (query via view `vw_HouseholdCurrent`) | D4 |
| 4c | Record rectification (BR-HIST-02) collides with non-overlapping rule (BR-RES-03) | Introduce `VOID` status + `Corrects...Id` column; overlap-check trigger excludes `VOID` rows | D3 |
| 4d | A1 creates a new household, but HH-05 preserves the existing household: contradictory | Introduce `HOUSEHOLD_MOVE` request type (entire household relocates while preserving `HouseholdId`) | D5 |
| 5 | Replacing Address A with A' causes residence/household records to point to an INACTIVE address | "Valid address" = terminal record in the `ReplacedByAddressId` lineage chain that remains ACTIVE (`vw_AddressCanonical`); supports N→1 merges | D9 |
| 6 | Audit `FAILURE` records roll back together with the business transaction; retention conflicts with immutability | Write FAILURE records on a separate DB connection after rollback; retention policy is archival, not deletion | D7 |
| 7 | No provision for concurrency conflict detection (E3) | Add `RowVersion` (`rowversion`) column to all mutable tables | D14 |
| 8 | "At all times" constraint conflicts with SQL Server's lack of deferred constraints; `Status`/`EndDate`/`EndedAt` redundantly encode the same state | Enforcement matrix (Section 5); `CHECK` constraints guarantee the 3 columns stay synchronized | D12 |
| 9 | Missing configuration table, roles lack authorization scope, account security columns missing, failed login auditing absent | Introduce `SystemConfiguration`; role-based access (no geographic partitions); add security columns to `UserAccount` and `AuditLog` | D10, D11 |

---

## 3. Decision Log

| Code | Decision | Rationale | Reversal / Alteration Path |
|---|---|---|---|
| **D1** | Only **Transfer** adopts maker-checker: Officer A creates request (`PENDING`), Officer B ≠ A approves/rejects. Constraint enforced via `CK_TR_Decision`. All other operations are performed by a single Officer and audited. | SRS specified PENDING/APPROVED but main flow omitted it; Transfer carries the highest operational risk. | If single-step transfer is preferred: remove `DecidedBy <> RequestedBy` in `CK_TR_Decision`; workflow remains executable. |
| **D2** | `Citizen.Status` ∈ `UNASSIGNED`, `ACTIVE`, `INACTIVE`. The Golden Invariant applies strictly to `ACTIVE`. `INACTIVE` is a terminal state in the MVP. | Resolves the chicken-and-egg dilemma of "create citizen first, assign to household subsequently". | — |
| **D3** | `HouseholdMembership`, `Residence`, `HouseholdAddress` are **append-only**: records transition `ACTIVE→ENDED` or `→VOID` at most once. Identity columns (Citizen, Household/Address, Role, StartDate) are immutable. Role change = close old row + open new row. Correction = `VOID` faulty row + create new row referencing `Corrects...Id`. | Preserves BR-HIST-01/02 without violating BR-RES-03. | — |
| **D4** | Household address is stored exclusively in `HouseholdAddress`. `Household.CurrentAddressId` is removed. | Single source of truth with complete address history (HH-06). | For instant lookups: query `vw_HouseholdCurrent`. |
| **D5** | Three request types: `JOIN_EXISTING` (join existing household), `NEW_HOUSEHOLD` (split into new household), `HOUSEHOLD_MOVE` (entire household moves, preserving `HouseholdId`). Each citizen is represented as a `TransferRequestItem` with individual status (supporting A4 partial approval). **A1 in SRS is replaced by `HOUSEHOLD_MOVE`.** | Unified mechanism for all relocation flows; destination address for `JOIN_EXISTING` is inferred directly from target household, eliminating inconsistency. | — |
| **D6** | New records generated via Transfer include `OpenedByRequestId`; all history records include `CreatedBy`/`EndedBy`. Dropped `VerifiedBy/VerifiedAt` from Membership/Residence. In `TransferRequest`, renamed `VerifiedBy/At` to `DecidedBy/At` (as outcomes include REJECTED/CANCELLED). | Single, unified, traceable verification mechanism. | — |
| **D7** | Audit: Renamed `Timestamp` to `OccurredAt` (avoids SQL keyword conflict); `ActorUserId` allows NULL + adds `AttemptedUsername`; added `CorrelationId` grouping actions; `Details` is JSON (`before`/`after`/`reason`). `FAILURE` entries are logged via a separate connection post-rollback; if DB is unreachable, write to log file. Retention policy is **archival**, never physical deletion. | See Section 2, item 6. | — |
| **D8** | `Address` separates `AddressLine`, `Ward`, `District` (nullable, for legacy records only), `Province`; `AddressText` is a computed column. | RPT-01 requires demographic grouping by administrative zone, which cannot be reliably queried from a raw string. | — |
| **D9** | Address records are not updated in place when administrative boundaries change: create a new address; old address becomes `INACTIVE` + points to `ReplacedByAddressId`. Existing residence and household records are **not** migrated. Supports N→1 merges only; 1→N splits are out of scope. In-place typo corrections are permitted but must audit `before/after`. | Prevents artificial "relocation" records in historical data. | — |
| **D10** | `UserAccount` adds `FullName`, `FailedLoginCount`, `LockedUntil`, `MustChangePassword`, `PasswordChangedAt`, `CreatedAt`. Authorization scope = role-based (`OFFICER` / `ADMIN`), **without** geographic subdivisions. | Satisfies account lockout and password rotation requirements (ACC-01, SYS-01); geographic partitioning is beyond MVP scope. | — |
| **D11** | Key-value table `SystemConfiguration` (pre-seeded with 4 system parameters). | Satisfies SYS-01. | — |
| **D12** | Triggers: `trg_Residence_Guard`, `trg_Membership_Guard`, `trg_HouseholdAddress_Guard` (append-only + non-overlapping), `trg_TransferRequest_Frozen`, `trg_AuditLog_AppendOnly`. Role `residence_app`: `SELECT/INSERT/UPDATE`, **`DENY DELETE`** on schema, `DENY UPDATE` on `AuditLog`. | Final line of defense against application-level bugs. | — |
| **D13** | Views: `vw_AddressCanonical`, `vw_HouseholdCurrent`, `vw_GoldenInvariantViolations` (9 validation checks C1–C5, H1–H4). | Transforms the Golden Invariant into an auditable, deterministic query. | — |
| **D14** | `RowVersion rowversion` on all mutable tables; EF Core mapped via `IsRowVersion()`; throws `DbUpdateConcurrencyException` matching error E3 in SRS. | Fulfills SRS requirement for concurrency conflict detection. | — |

---

## 4. State Transition Table

State arrows not listed in this table are **prohibited** (and blocked by triggers/CHECK constraints where applicable).

| Entity | Allowed States | Valid Transitions |
|---|---|---|
| Citizen | `UNASSIGNED`, `ACTIVE`, `INACTIVE` | `UNASSIGNED→ACTIVE` (joins household), `ACTIVE→INACTIVE` (management terminated), `UNASSIGNED→INACTIVE` |
| Household | `ACTIVE`, `INACTIVE` | `ACTIVE→INACTIVE` when the last active member leaves |
| Membership / Residence / HouseholdAddress | `ACTIVE`, `ENDED`, `VOID` | `ACTIVE→ENDED`, `ACTIVE→VOID`, `ENDED→VOID`. `VOID` is permanently frozen |
| TransferRequest | `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED` | `PENDING→` any of the remaining three states; permanently frozen thereafter |
| TransferRequestItem | `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED` | Always `PENDING` while the parent request is `PENDING`; receives individual outcome when request is decided |
| Address | `ACTIVE`, `INACTIVE` | `ACTIVE→INACTIVE` (optionally populated with `ReplacedByAddressId`) |
| UserAccount | `ACTIVE`, `LOCKED` | Bidirectional |

---

## 5. Layer Responsibility Matrix

| Rule | DB Enforced? | Enforcement Mechanism |
|---|---|---|
| At most 1 ACTIVE residence / citizen (BR-RES-01) | ✔ | `UX_Res_OneActivePerCitizen` |
| At most 1 ACTIVE membership / citizen (BR-HH-03) | ✔ | `UX_HM_OneActivePerCitizen` |
| At most 1 ACTIVE HEAD / household | ✔ | `UX_HM_OneActiveHeadPerHousehold` |
| At most 1 ACTIVE address / household | ✔ | `UX_HA_OneActivePerHousehold` |
| Citizen can belong to at most 1 open request (Precondition 5) | ✔ | `UX_TRI_OneOpenPerCitizen` (convention: item `PENDING` ⇔ request `PENDING`) |
| `Status` synchronized with `EndDate` / `EndedAt` | ✔ | `CK_*_Lifecycle` |
| Request payload structure valid, approver ≠ requester | ✔ | `CK_TR_Shape`, `CK_TR_Decision` |
| Item membership/residence belongs to the item's citizen | ✔ | Composite FK `(Id, CitizenId)` |
| History is append-only; decided requests are frozen | ✔ | Database Triggers |
| Non-overlapping temporal intervals (BR-RES-03) | ◐ | Trigger reads committed state; **still requires** `SERIALIZABLE` transaction in application (Section 6) |
| Immutable audit log (BR-AUD-02) | ✔ | Trigger + `DENY UPDATE` permission |
| ACTIVE household must have **at least** 1 HEAD, at least 1 member | ✘ | Application layer + views `H1`, `H2` |
| Golden Invariant (membership + residence share household address) | ✘ | Application layer + views `C1`–`C5`, `H3` |
| ACTIVE citizen ⇔ has both ACTIVE membership + ACTIVE residence | ✘ | Application layer + views `C1`, `C2`, `C4` |

> [!NOTE]
> "At all times" in the SRS should be understood as "**at all committed states**": inside a transaction, a household may momentarily lack a HEAD (after closing the previous record and before opening the new one); this transient state is acceptable and expected.

---

## 6. Application Layer Procedures

All mutation routines must execute inside a single transaction with **`IsolationLevel.Serializable`**. The sequence of "close before open" is strictly mandatory to prevent violating filtered unique indexes. Let `E` represent `EffectiveDate`; the end date of closed historical records is always `E − 1`, and **`E` must be strictly greater than the `StartDate` of all records being closed** (otherwise `EndDate < StartDate`, violating CHECK constraints).

### 6.1 Create Request (Officer A)

1. Validate each candidate citizen: must be `ACTIVE`, belong to the source household, possess active membership and residence, and not exist in any other `PENDING` item.
2. Validate by request type:
   - `JOIN_EXISTING`: destination household must be `ACTIVE` with a valid address.
   - `NEW_HOUSEHOLD`: destination address must be `ACTIVE`, and `NewHeadCitizenId` must belong to the item set.
   - `HOUSEHOLD_MOVE`: items must comprise **all** current active members of the household.
3. If the current household head departs while other members remain: `SuccessorHeadCitizenId` is mandatory (must be an active member who is not included in the transfer items).
4. Insert `TransferRequest(PENDING)` + `TransferRequestItem(PENDING)` rows + log audit event.

### 6.2 Review and Execute Request (Officer B ≠ A)

1. Fetch request (check `RowVersion`), verify status is `PENDING`.
2. **Re-validate all preconditions from steps 1–3 in Section 6.1** (data may have changed since request submission). Any item no longer valid → mark `REJECTED` with reason (A4). If zero items remain eligible → reject the entire request.
3. For each approved item, **close** the existing `Residence`. For `JOIN_EXISTING` and `NEW_HOUSEHOLD`, also close the existing `HouseholdMembership`.
4. Source household handling: if `SuccessorHeadCitizenId` is specified → close that citizen's `MEMBER` record, **open** their new `HEAD` record (`StartDate = E`). If source household has no remaining active members → set `Household.Status = INACTIVE` and close its active `HouseholdAddress`.
5. **Open** new records (all assigned `OpenedByRequestId = RequestId`):
   - `JOIN_EXISTING`: `Membership(MEMBER)` + `Residence` pointing to the target household's active canonical address.
   - `NEW_HOUSEHOLD`: create new `Household`, `HouseholdAddress`, `Membership` (designated leader as `HEAD`, others as `MEMBER`), and `Residence`; record `ResultHouseholdId`.
   - `HOUSEHOLD_MOVE`: do **not** modify memberships; close previous `HouseholdAddress`, open new `HouseholdAddress`; open new `Residence` for each citizen at the new address.
6. Update request: set `Status = APPROVED`, populate `DecidedBy/At`, update items with their individual decisions.
7. Write `SUCCESS` audit log (sharing `CorrelationId`) within the same transaction → `COMMIT`.
8. On any unhandled failure: `ROLLBACK`, then record a `FAILURE` audit entry via an independent DB connection.

### 6.3 Other Operations

| Operation | Key Steps (All within a Single Transaction) |
|---|---|
| **Register Household (HH-01)** | Insert Household + HouseholdAddress → insert Membership (`HEAD`) + Residence for the head citizen (previously `UNASSIGNED`) → update Citizen to `ACTIVE` |
| **Add Member (HH-02)** | Valid only for `UNASSIGNED` citizens: insert Membership (`MEMBER`) + Residence at household address → update Citizen to `ACTIVE` |
| **Change Household Head (HH-04)** | Close former HEAD record and successor's MEMBER record → open new `MEMBER` record for former head, open new `HEAD` record for successor (both with `StartDate = E`) |
| **Deactivate Citizen (CIT-04)** | If citizen is HEAD and other members remain: choosing successor head is mandatory. Close Membership + Residence. If household becomes empty, mark Household `INACTIVE`. Update Citizen to `INACTIVE` |
| **Replace Address due to Boundary Change (ADR-03)** | Insert new Address → update old address to `INACTIVE` + set `ReplacedByAddressId`. Do **not** modify Residence or HouseholdAddress records |
| **Void and Rectify Record (BR-HIST-02)** | Mark erroneous record `VOID` → insert corrected record with `Corrects...Id` referencing the voided record |
| **Update Citizen Demographics (CIT-03)** | Direct `UPDATE` + `UpdatedAt`; full history tracked in AuditLog (`before/after`) |

---

## 7. SRS Synchronization Requirements

To ensure complete consistency across documentation and implementation:

- **Section 5 (Entities):** Align with the v2 schema; include `HouseholdAddress`, `TransferRequestItem`, `SystemConfiguration`; remove `Household.CurrentAddressId`.
- **BR-HIST-01:** Redefine "immutability" as append-only, where each record undergoes at most one state transition.
- **BR-HH-01:** Clarify "at all times" as "at all committed states".
- **Golden Invariant / BR-CIT-04:** Restrict scope to `ACTIVE` citizens; incorporate `UNASSIGNED` status for newly created citizens.
- **BR-RES-02, BR-HH-08:** Define "valid address" as the terminal address in the replacement chain that remains `ACTIVE`.
- **BR-VER-01:** Restrict maker-checker specifically to Transfer requests.
- **BR-AUD-01/02:** Explicitly note that FAILURE entries are recorded in a separate transaction post-rollback; retention is archival without deletion.
- **FRs:** Clarify that `RES-01`, `RES-02`, and `HH-03` are internal atomic steps of composite use cases (`HH-01`, `HH-02`, Transfer, `CIT-04`), not standalone rogue operations. Add explicit FR for "Void and Rectify Record (VOID)".
- **UC-HH-05 (Section 9):** Specify as a two-phase workflow (request creation → decision/execution); replace A1 with `HOUSEHOLD_MOVE`; use `SuccessorHeadCitizenId` in A3; support per-item resolution in A4; add constraint `EffectiveDate > StartDate` for closed records.
- **Identifiers:** Standardize functional IDs across the document (unifying `HH-05` / `UC-HH-05` / `UC-06`).

---

## 8. Limitations & Verification Notes

1. **Physical SQL Server Execution:** Validated against grammar parsers: tables, indexes, views, and trigger SELECT logic parse cleanly. Trigger procedural bodies (`THROW`) and `GRANT ... ON SCHEMA::` are verified manually against SQL Server specifications. Execute `residence_schema_v2.sql` on an empty database and run the test suite below.
2. Minimum required integration tests (each must be **rejected**):
   - Two ACTIVE residences for the same citizen.
   - Two ACTIVE heads in the same household.
   - Two overlapping residence date intervals for the same citizen.
   - `UPDATE` on an `ENDED` record.
   - `UPDATE` or `DELETE` on `AuditLog`.
   - Transfer request where `DecidedBy == RequestedBy`.
   - Transfer request item referencing a residence belonging to a different citizen.
   - And one **successful** test for each request type, validating that `vw_GoldenInvariantViolations` subsequently returns 0 rows.
3. Because trigger overlap checking inspects committed data, concurrent transactions could theoretically collide without isolation. Hence, Section 6 mandates `IsolationLevel.Serializable`.
4. **EF Core Configuration:** From EF Core 7+, tables with triggers must declare `.ToTable(t => t.HasTrigger("trigger_name"))`, otherwise `SaveChanges` may fail. When scaffolding, verify this configuration. `AddressText` is a computed column, and `RowVersion` requires `.IsRowVersion()`.
5. Beyond MVP Scope: 1→N address splits, reactivation of `INACTIVE` citizens, geographic partition security, same-day transfers where `EffectiveDate == StartDate` (`E` must be strictly greater than `StartDate` of the record being closed).
6. The initial `ADMIN` account must be seeded by the application layer (the SQL script deliberately omits plaintext or pre-hashed passwords).
7. The SQL script is strictly ASCII-encoded to prevent character encoding issues in SSMS; trigger error messages are standardized in English.

### Trigger Error Code Reference

| Code | Meaning |
|---|---|
| 51001 / 51011 / 51021 | Append-only violation (Residence / Membership / HouseholdAddress) |
| 51002 / 51012 / 51022 | Temporal interval overlap (Residence / Membership / HouseholdAddress) |
| 51030 | Attempted modification or deletion of AuditLog |
| 51040 | Attempted modification of an already-decided TransferRequest |
