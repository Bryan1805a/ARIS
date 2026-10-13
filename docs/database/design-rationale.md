# Database Design Rationale & Decision Log

**Document Code:** `DB-DESIGN-v2.0`  
**Target Engine:** Microsoft SQL Server 2019+ (Azure SQL / Docker)  
**Schema Script:** [`../aris_schema_v2.sql`](../aris_schema_v2.sql)  
**ERD Diagrams:** [`../erd_v2.svg`](../erd_v2.svg) · [`../erd_v2.png`](../erd_v2.png)

---

## 1. The 4 Concentric Layers of Protection

Data protection is architected into 4 concentric defense rings, with the least expensive checks evaluating first:

```text
┌─────────────────────────────────────────────────────────────────┐
│ Layer 1: Data Types, NOT NULL, CHECK Constraints                │
│          (Prevents malformed data, illegal status strings)      │
├─────────────────────────────────────────────────────────────────┤
│ Layer 2: Foreign Keys, UNIQUE, Filtered Unique Indexes          │
│          (Prevents duplicate active memberships/residences)     │
├─────────────────────────────────────────────────────────────────┤
│ Layer 3: Database Triggers                                      │
│          (Append-only immutability, temporal non-overlap)       │
├─────────────────────────────────────────────────────────────────┤
│ Layer 4: View vw_GoldenInvariantViolations & Application Layer  │
│          (Complex cross-table invariants & transactional ACID)  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Decision Log (D1–D14)

| Code | Decision | Rationale | Reversal / Alteration Path |
|---|---|---|---|
| **D1** | **Maker-Checker exclusively on Relocations (`TransferRequest`):** Requester creates `PENDING`, different Approver executes (`CK_TR_Decision`). All other ops are single-officer audited. | Relocations carry the highest risk of cross-household corruption. | If single-step transfer desired, remove `DecidedBy <> RequestedBy` in check constraint. |
| **D2** | **Citizen Status:** `UNASSIGNED`, `ACTIVE`, `INACTIVE`. Golden Invariant applies only to `ACTIVE`. | Resolves the chicken-and-egg dilemma of registering a citizen prior to household assignment. | — |
| **D3** | **Append-Only History:** `HouseholdMembership`, `Residence`, and `HouseholdAddress` are append-only. No in-place `UPDATE` on identity/role/dates. Faulty rows are marked `VOID` and rectified with `Corrects...Id`. | Preserves historical legal truth and auditability without violating temporal overlap rules. | — |
| **D4** | **Drop static `Household.CurrentAddressId`:** Address history is recorded exclusively in `HouseholdAddress`. | Single source of truth with complete address timeline; current address queried via view `vw_HouseholdCurrent`. | — |
| **D5** | **Three Transfer Request Shapes:** `JOIN_EXISTING`, `NEW_HOUSEHOLD`, and `HOUSEHOLD_MOVE` (entire household moves preserving `HouseholdId`). Each candidate is a `TransferRequestItem` (enabling partial approval A4). | Unified architecture for all relocations; eliminates inconsistent destination inputs. | — |
| **D6** | **Traceable Verification:** Records created by a transfer store `OpenedByRequestId`. Historical rows track `CreatedBy`/`EndedBy`. Dropped redundant `VerifiedBy` fields. | Clean, auditable lineage for all state mutations. | — |
| **D7** | **Independent Failure Auditing:** Multi-table transaction rollback discards in-transaction logs; `FAILURE` entries are written on a separate database connection post-rollback. | Prevents silent failures while maintaining ACID transactional guarantees. | — |
| **D8** | **Normalized Address Hierarchy:** `Address` separates `AddressLine`, `Ward`, `District` (nullable for legacy), and `Province`. `AddressText` is a computed column. | Demographic reports (`RPT-01`) require grouping by administrative units, impossible from raw unparsed strings. | — |
| **D9** | **Address Lineage ($N \rightarrow 1$ Mergers):** Changing administrative boundaries creates new addresses; old addresses become `INACTIVE` with `ReplacedByAddressId`. Existing residences are NOT retroactively edited. | Prevents artificial residency movements in historic records. | — |
| **D10** | **User Account Security:** `UserAccount` adds `FailedLoginCount`, `LockedUntil`, `MustChangePassword`, `PasswordChangedAt`. Role-based (`OFFICER`/`ADMIN`) without geographic partitions. | Fulfills lockout and credential lifecycle requirements. | — |
| **D11** | **Dynamic System Configuration:** Key-value table `SystemConfiguration` seeds password policy, lockout limits, and session timeouts. | Configurable without redeploying code. | — |
| **D12** | **Least-Privilege Database Role `aris_app`:** `GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo`, `DENY DELETE ON SCHEMA::dbo`, `DENY UPDATE ON dbo.AuditLog`. | Eliminates risk of rogue application code deleting history or tampering with audit logs. | — |
| **D13** | **Automated Invariant Views:** `vw_AddressCanonical`, `vw_HouseholdCurrent`, `vw_GoldenInvariantViolations` (9 checks C1–C5, H1–H4). | Converts complex invariants into auditable deterministic queries. | — |
| **D14** | **Optimistic Concurrency Control:** `RowVersion` (`rowversion`) column on all 9 mutable tables; mapped via `.IsRowVersion()` in EF Core. | Throws `DbUpdateConcurrencyException` when concurrent modifications collide. | — |

---

## 3. Layer Responsibility Matrix

| Business Rule | Enforced at DB? | Enforcement Mechanism |
|---|:---:|---|
| At most 1 `ACTIVE` residence per citizen | ✔ | Filtered unique index `UX_Res_OneActivePerCitizen` |
| At most 1 `ACTIVE` membership per citizen | ✔ | Filtered unique index `UX_HM_OneActivePerCitizen` |
| At most 1 `ACTIVE` head per household | ✔ | Filtered unique index `UX_HM_OneActiveHeadPerHousehold` |
| At most 1 `ACTIVE` address per household | ✔ | Filtered unique index `UX_HA_OneActivePerHousehold` |
| Citizen in at most 1 `PENDING` transfer request item | ✔ | Filtered unique index `UX_TRI_OneOpenPerCitizen` |
| Status synchronized with `EndDate` / `EndedAt` | ✔ | `CK_*_Lifecycle` constraints |
| Valid transfer payload & Maker $\ne$ Checker | ✔ | `CK_TR_Shape`, `CK_TR_Decision` constraints |
| Append-only history & frozen decided requests | ✔ | Triggers: `trg_*_Guard`, `trg_TransferRequest_Frozen` |
| Non-overlapping temporal intervals | ◐ | Trigger reads committed state; requires `IsolationLevel.Serializable` |
| Immutable audit log | ✔ | Trigger `trg_AuditLog_AppendOnly` + `DENY UPDATE` permission |
| Active household must have at least 1 head & 1 member | ✘ | Application layer + Views `H1`, `H2` |
| Active citizen $\iff$ has active membership + active residence | ✘ | Application layer + Views `C1`, `C2`, `C4` |
| Active residence matches active household address | ✘ | Application layer + Views `C3`, `H3` |

> [!NOTE]
> Database constraints check committed states. During an in-flight multi-step transaction, a household may briefly transition from old head to new head; this transient intermediate state is fully valid before `COMMIT`.
