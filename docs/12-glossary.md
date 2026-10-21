# 12. Glossary

> **arc42 §12** · ARIS 3.0 · [Documentation hub](README.md)

## 12.1 Domain terms

| Term | Meaning |
|---|---|
| **Active record** | A row with `Status = 'ACTIVE'` that currently represents the truth for its entity. |
| **Address lineage** | The chain of successor addresses created when administrative boundaries change or addresses merge (N → 1). |
| **Append-only** | A table whose existing rows are never edited; changes add rows. In ARIS, `HouseholdMembership`, `Residence`, and `HouseholdAddress`. |
| **Canonical address** | The terminal address of a lineage chain, as resolved by `vw_AddressCanonical`. |
| **Checker** | The officer who approves or rejects a transfer request; must differ from the maker. |
| **Citizen** | An individual managed by the residence office. Identity only; no household or address column. |
| **Close-and-open** | The append-only change pattern: close the old row (`EndDate = E - 1`) and open a new one (`StartDate = E`). |
| **Correlation ID** | A `Guid` shared by every audit row produced by one business action. |
| **Effective date (`E`)** | The business date on which a change takes effect. Must be later than the start date of any row being closed. |
| **Golden Invariant** | Every active citizen has exactly one active membership and one active residence at the household's active canonical address. |
| **Head of household** | The single active member with `Role = 'HEAD'` in a household. |
| **Household** | A legal administrative family unit. Location lives in `HouseholdAddress`, not on the household row. |
| **Maker** | The officer who creates a transfer request. |
| **Maker-checker** | Dual control: the requester cannot approve their own request (`DecidedBy <> RequestedBy`). |
| **Membership** | The tenure and role of a citizen in a household (`HouseholdMembership`). |
| **Relocation / transfer** | Moving one or more citizens to another household or address through `UC-06`. |
| **Residence** | The tenure of a citizen at an address (`Residence`). |
| **Rectification** | Correcting history by voiding a row (`Status = 'VOID'`) and inserting a corrected row linked by `Corrects...Id`. |
| **Residence officer** | The `OFFICER` role: registers, searches, requests and approves relocations. |
| **System administrator** | The `ADMIN` role: accounts, configuration, audit inspection, reports. |
| **Tenure** | A period during which a relationship holds. Used for membership, residence, and household address. |
| **Void** | A historical row marked `Status = 'VOID'` because it was erroneous. Frozen permanently. |

---

## 12.2 Lifecycle values

| Entity | Values | Notes |
|---|---|---|
| `Citizen.Status` | `UNASSIGNED`, `ACTIVE`, `INACTIVE` | Starts `UNASSIGNED`; `INACTIVE` requires `DeactivatedAt` |
| `Household.Status` | `ACTIVE`, `INACTIVE` | `INACTIVE` requires `InactivatedAt`; must have no active members |
| `HouseholdMembership.Role` | `HEAD`, `MEMBER` | At most one active `HEAD` per household |
| `HouseholdMembership.Status` | `ACTIVE`, `ENDED`, `VOID` | `ENDED` requires `EndDate` and `EndedAt` |
| `Residence.Status` | `ACTIVE`, `ENDED`, `VOID` | Same lifecycle rules |
| `HouseholdAddress.Status` | `ACTIVE`, `ENDED`, `VOID` | At most one active address per household |
| `Address.Status` | `ACTIVE`, `INACTIVE` | `INACTIVE` requires `ValidTo`, `DeactivatedAt`; may set `ReplacedByAddressId` |
| `TransferRequest.RequestType` | `JOIN_EXISTING`, `NEW_HOUSEHOLD`, `HOUSEHOLD_MOVE` | Shape enforced by `CK_TR_Shape` |
| `TransferRequest.Status` | `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED` | Decided requests are frozen |
| `TransferRequestItem.Status` | `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED` | `REJECTED` requires `RejectReason` |
| `UserAccount.Role` | `OFFICER`, `ADMIN` | — |
| `UserAccount.Status` | `ACTIVE`, `LOCKED` | Bidirectional |
| `AuditLog.Result` | `SUCCESS`, `FAILURE` | — |

---

## 12.3 Technical terms

| Term | Meaning |
|---|---|
| **arc42** | A template for architecture documentation with twelve fixed sections. |
| **ADO.NET** | The low-level .NET data-access API (`Microsoft.Data.SqlClient`); used for independent failure auditing. |
| **Close-and-open** | See §12.1. |
| **Composition root** | The single place where the object graph is built; `src/Aris.UI/Program.cs`. |
| **DBO** | The default database schema owner in SQL Server. |
| **DDL** | Data Definition Language; the schema script `aris_schema_v2.sql`. |
| **DTO** | Data Transfer Object; an immutable record exchanged between UI and Application. |
| **EF Core** | Entity Framework Core, the ORM used for persistence. |
| **Filtered unique index** | A unique index with a `WHERE` clause, e.g. "one active head per household". |
| **Golden Invariant view** | `vw_GoldenInvariantViolations`; returns rows only when the invariant is broken. |
| **Isolation level** | Transaction isolation; `Serializable` is required for transfer execution. |
| **MTP** | Microsoft.Testing.Platform, the test runner configured in `global.json`. |
| **Optimistic concurrency** | Detecting conflicting writes with a version token (`rowversion`) instead of locking. |
| **`rowversion`** | SQL Server automatically maintained binary version column. |
| **`Result<T>`** | The Application-layer return type for expected success or failure. |
| **Serializable** | Isolation level that makes concurrent transactions behave as if executed sequentially. |
| **Trigger** | SQL Server code that runs after `INSERT`/`UPDATE`/`DELETE`; used for append-only and overlap rules. |
| **User-secrets** | .NET configuration provider storing secrets outside the repository. |
| **`vw_`** | Prefix for SQL views. |
| **`trg_`** | Prefix for SQL triggers. |
| **`UX_` / `IX_` / `CK_` / `FK_`** | Naming prefixes for unique indexes, indexes, check constraints, and foreign keys. |

---

## 12.4 Trigger error codes

| Code | Trigger | Meaning |
|---|---|---|
| `51001` | `trg_Residence_Guard` | Residence identity/start mutated, VOID edited, or invalid ENDED transition |
| `51002` | `trg_Residence_Guard` | Overlapping residence periods for one citizen |
| `51011` | `trg_Membership_Guard` | Membership identity/household/role/start mutated, VOID edited, or invalid ENDED transition |
| `51012` | `trg_Membership_Guard` | Overlapping membership periods for one citizen |
| `51021` | `trg_HouseholdAddress_Guard` | Household-address identity/start mutated, VOID edited, or invalid ENDED transition |
| `51022` | `trg_HouseholdAddress_Guard` | Overlapping address periods for one household |
| `51030` | `trg_AuditLog_AppendOnly` | `UPDATE` or `DELETE` attempted on `AuditLog` |
| `51040` | `trg_TransferRequest_Frozen` | `UPDATE` attempted on a decided transfer request |

---

## 12.5 Requirement and identifier prefixes

| Prefix | Meaning | Example |
|---|---|---|
| `CIT` | Citizen requirement | `CIT-04` deactivate citizen |
| `ADR` | Address requirement | `ADR-03` address lineage |
| `HH` | Household requirement | `HH-04` change head |
| `RES` | Residence requirement | `RES-03` view current residence |
| `HIST` | Historical rectification | `HIST-01` void and correct |
| `AUTH` | Authentication | `AUTH-01` login |
| `ACC` | Account administration | `ACC-01` manage accounts |
| `PERM` | Authorization | `PERM-01` role enforcement |
| `AUD` | Audit | `AUD-01` view audit log |
| `SYS` | System configuration | `SYS-01` key/value settings |
| `SRCH` | Search | `SRCH-01` search citizens |
| `RPT` | Reporting | `RPT-03` movement report |
| `UC` | Use case | `UC-06` transfer household/residence |
| `CON` | Constraint | `CON-SEC-02` secrets |
| `D` | Architecture decision | `D15` local Docker database |
| `C1`–`C5` | Citizen invariant checks | `C3` address mismatch |
| `H1`–`H4` | Household invariant checks | `H2` active household without members |
| `Q` | Quality scenario | `Q-INT-02` |
| `R` | Risk | `R1` save ordering |
| `T` | Technical debt | `T1` missing persistence layer |

---

## 12.6 Abbreviations

| Abbreviation | Expansion |
|---|---|
| ARIS | Administrative Residence Information System |
| CCCD | Căn cước công dân (Vietnamese citizen identity card) — out of scope |
| CRUD | Create, Read, Update, Delete |
| DB | Database |
| DI | Dependency Injection |
| DTO | Data Transfer Object |
| ERD | Entity Relationship Diagram |
| FK | Foreign Key |
| MTP | Microsoft.Testing.Platform |
| ORM | Object–Relational Mapper |
| PK | Primary Key |
| PR | Pull Request |
| SDK | Software Development Kit |
| SRS | Software Requirements Specification (superseded by this arc42 set) |
| UI | User Interface |
| VNeID | Vietnamese national digital identity platform — out of scope |
| WBS | Work Breakdown Structure |
