# 1. Introduction and Goals

> **arc42 §1** · ARIS 3.0 · [Documentation hub](README.md)

## 1.1 Purpose and vision

ARIS (**Administrative Residence Information System**) is a Windows desktop application
for local residence-management officers. It records citizens, households, administrative
addresses, and residency over time, and it processes relocations through a two-person
maker-checker workflow. Every change is auditable, and no historical fact is ever
overwritten.

The system exists to solve one concrete problem:

> A paper residence register cannot answer *"who lived where, with whom, from when to
> when, and who authorised the change?"* without losing history. ARIS makes that
> question answerable for every citizen, at every point in time.

### Academic context

ARIS is a course project for a Windows Programming module. It demonstrates:

- layered desktop application design with .NET 10 and WinForms;
- relational modelling with temporal (append-only) history;
- database-enforced integrity using constraints, filtered indexes, and triggers;
- EF Core persistence with optimistic concurrency;
- secure authentication, role-based authorization, and immutable auditing;
- reproducible local development with Docker.

It is **not** a production population registry. It stores synthetic data, does not
integrate with VNeID or any national system, and does not process legally binding
residence transactions.

---

## 1.2 Scope

| In scope | Out of scope |
|---|---|
| Citizen demographic registry (`UNASSIGNED`, `ACTIVE`, `INACTIVE`) | Birth, death, marriage, and other vital records |
| Normalized administrative addresses with historic lineage | Issuance or biometric capture of CCCD/passports |
| Household lifecycle: registration, membership, head assignment | Real-time VNeID or National Population Database APIs |
| Temporal residency tracking with non-overlapping intervals | Cross-province partitioning or multi-tenant operation |
| Maker-checker relocations (`JOIN_EXISTING`, `NEW_HOUSEHOLD`, `HOUSEHOLD_MOVE`) | Mobile or web clients |
| Append-only audit trail and administrative reports | Payments, fees, or case-management workflow |
| Authentication, account administration, system configuration | Integration with external identity providers (OIDC, AD) |

---

## 1.3 Functional requirements

Requirement IDs are stable and are used by tests, commits, and the PR checklist.

### Authentication and administration

| ID | Requirement |
|---|---|
| `AUTH-01` | Authenticate with username and password. Lock the account after `MAX_FAILED_LOGINS` failures for `LOCKOUT_MINUTES`. Force a password change when `MustChangePassword = 1`. Establish the user session. |
| `ACC-01` | Administrators create officer accounts, lock/unlock, activate/deactivate, and reset passwords with forced change on next login. |
| `PERM-01` | Enforce role-based access (`OFFICER` vs `ADMIN`) at the screen and service boundary. |
| `AUD-01` | Administrators filter and inspect immutable audit entries by date, actor, entity, action, and result. |
| `SYS-01` | Administrators edit key/value system parameters in `SystemConfiguration`. |

### Citizen management

| ID | Requirement |
|---|---|
| `CIT-01` | Register a citizen with `NationalId`, `FullName`, `DateOfBirth`, and `Gender`. Initial status is `UNASSIGNED`. |
| `CIT-02` | View demographics, current household, canonical address, and the full residency timeline. |
| `CIT-03` | Update non-identifying demographics and write a before/after audit snapshot. |
| `CIT-04` | Deactivate a citizen (`INACTIVE`), close their active membership and residence, designate a successor head when required, and deactivate an emptied household. |

### Addresses

| ID | Requirement |
|---|---|
| `ADR-01` | Register an address (`AddressLine`, `Ward`, `District`, `Province`); the database computes `AddressText`. |
| `ADR-02` | Search addresses and view current households and residents. |
| `ADR-03` | Retire an address when boundaries change, linking it to its successor through `ReplacedByAddressId`. Historical residences remain untouched. |

### Households and residency

| ID | Requirement |
|---|---|
| `HH-01` | Register a household atomically: household + address + `HEAD` membership + residence + citizen `ACTIVE`. |
| `HH-02` | Add an `UNASSIGNED` citizen to an active household as `MEMBER`, with a matching residence. |
| `HH-03` | Remove a member only through a transfer (`UC-06`) or deactivation (`CIT-04`); no orphaned citizens. |
| `HH-04` | Change the head using append-only close-and-open for both the former head and the successor. |
| `HH-05` | Transfer household/residence through the maker-checker workflow described in `UC-06`. |
| `HH-06` | View household address history and member tenure history. |
| `RES-01` | Register a residence — only as an internal step of `HH-01`, `HH-02`, `HH-05`, or `CIT-04`. |
| `RES-02` | End a residence — only as an internal step of the same composite operations. |
| `RES-03` | Retrieve the validated current residence address of a citizen. |
| `HIST-01` | Void an erroneous historical row and insert a corrected row linked by `Corrects...Id`. |

### Search and reporting

| ID | Requirement |
|---|---|
| `SRCH-01` | Search citizens by National ID, full name (case- and accent-insensitive), or status. |
| `SRCH-02` | Search households by ID, head name, head National ID, or address text. |
| `SRCH-03` | Search addresses by street, ward, district, or province. |
| `RPT-01` | Demographic distribution by administrative hierarchy, using `vw_AddressCanonical`. |
| `RPT-02` | Household summary: active/inactive counts, average size, single-member households. |
| `RPT-03` | Residency movement report for a date range: registrations, transfers, deactivations. |

### Core use case

`UC-06 — Transfer Household & Residence` is the most important and most complex flow.
Its two-phase execution is specified in [`06-runtime-view.md`](06-runtime-view.md#62-uc-06--maker-checker-transfer).

---

## 1.4 Quality goals

The top goals are ordered. When they conflict, the higher goal wins.

| Priority | Goal | Motivation | Realised by |
|---|---|---|---|
| 1 | **Historical integrity** | A residence register is worthless if the past can be rewritten. No update may destroy legal history. | Append-only tables, close-and-open mechanics, guard triggers, `VOID` + `Corrects...Id` |
| 2 | **Cross-record consistency** | A citizen, a household, and an address must never disagree about where the citizen lives. | Golden Invariant, `vw_GoldenInvariantViolations`, atomic composite operations |
| 3 | **Auditability and dual control** | Every change must name its actor, and a relocation must require two people. | `AuditLog`, correlation IDs, `CK_TR_Decision`, serializable transfer transaction |
| 4 | **Security and least privilege** | A compromised desktop client must not be able to delete history or rewrite audit rows. | PBKDF2/BCrypt hashing, `aris_app` role, `DENY DELETE`, `DENY UPDATE` on audit |
| 5 | **Maintainability** | Four students must work in parallel without breaking each other. | Strict layering, vertical slices, canonical service pattern, stable contracts |
| 6 | **Verifiability** | "It works" must be provable, not asserted. | Database views, integration tests against real SQL Server, PR gates |

Performance is a secondary goal: citizen search should feel instant on the expected
data volume (thousands, not millions, of rows), and no UI action may block the message
loop. See [`10-quality-requirements.md`](10-quality-requirements.md) for measurable
scenarios.

---

## 1.5 Stakeholders

| Stakeholder | Interest | View of the system |
|---|---|---|
| **Residence officer (`OFFICER`)** | Fast lookup, correct registration, safe relocations | Operates citizen, household, address, and transfer screens |
| **System administrator (`ADMIN`)** | Controlled accounts and parameters | Operates account, configuration, and audit screens |
| **Team Leader** | Architecture, boundaries, merge quality | Reads all sections; owns PR gate |
| **Database Designer** | Schema fidelity, mappings, audit persistence | Appendix A/B, §8, §9 |
| **Coder 1** | Citizen vertical slice | §5, §6, Appendix C |
| **Coder 2** | Household, residence, transfer engine | §5, §6, Appendix A/C |
| **Coder 3** | Shell, authentication, admin, reports | §5, §7, Appendix C |
| **Course instructor / examiner** | Demonstrable engineering rigour | §1, §4, §6, §9, §10 |
| **Future maintainer** | Understand why the system is shaped this way | §9, §11, §12 |

---

## 1.6 Documentation map

- Constraints that shape every decision: [`02-constraints.md`](02-constraints.md)
- System boundary and external interfaces: [`03-context-and-scope.md`](03-context-and-scope.md)
- The seven ideas the design rests on: [`04-solution-strategy.md`](04-solution-strategy.md)
- How to run it locally: [`07-deployment-view.md`](07-deployment-view.md)
- The rules that apply to every module: [`08-crosscutting-concepts.md`](08-crosscutting-concepts.md)
