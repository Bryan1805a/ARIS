# 4. Solution Strategy

> **arc42 §4** · ARIS 3.0 · [Documentation hub](README.md)

The architecture rests on a small number of ideas. Everything else is a consequence.

---

## 4.1 The ten strategic pillars

### 1. Strict four-layer clean architecture

```
UI (WinForms)  →  Application  →  Domain
                        ↑
                  Infrastructure
```

- **Domain** contains entities and invariants and depends on nothing.
- **Application** defines use-case contracts: interfaces, DTOs, validators, and
  `Result<T>`.
- **Infrastructure** implements persistence, auditing, and the services that need
  `ArisDbContext`/transactions (`D18`).
- **UI** composes screens and delegates every action to an Application interface.

**Why:** the same business rule must not exist twice. A UI that cannot reference
`ArisDbContext` cannot bypass validation or auditing, and services become testable
without a form.

**Trade-off:** more projects, interfaces, and mapping code. Accepted: this is the
course's central architectural lesson and the team works in parallel slices.

### 2. Decouple identity from time-varying facts

`Citizen`, `Household`, and `Address` are stable identities. *Where a citizen lives*,
*which household they belong to*, and *where a household is located* are **time-varying
relationships** with their own tables:

| Question | Table | Never stored as |
|---|---|---|
| Who is this person? | `Citizen` | — |
| Which household, in which role, when? | `HouseholdMembership` | `Citizen.HouseholdId` |
| Where does this person live, when? | `Residence` | `Citizen.AddressId` |
| Where is this household, when? | `HouseholdAddress` | `Household.CurrentAddressId` |

**Why:** a static foreign key overwrites history the moment a citizen moves, marries, or
changes role. The temporal tables answer "where in 2024?" and "who lived here before?" —
questions a paper register is expected to answer.

**Trade-off:** every read of "current" state joins or uses a view. Accepted: current
state is a query, not a column.

### 3. Append-only history with close-and-open

A change to tenure or location is two writes:

```text
Close:  EndDate = E - 1,  Status = 'ENDED',  EndedBy = actor,  EndedAt = now
Open:   StartDate = E,    Status = 'ACTIVE'
Rule:   E > StartDate of every row being closed
```

No in-place edit of `CitizenId`, `HouseholdId`, `AddressId`, `Role`, or `StartDate`.
Corrections are `Status = 'VOID'` plus a new row linked by `Corrects...Id`.

**Why:** legal history must be reconstructible; an audit trail cannot repair a
destroyed record.

**Trade-off:** more rows and more complex queries. Accepted: correctness over storage.

### 4. Defense in depth: four concentric rings

| Ring | Mechanism | Catches |
|---|---|---|
| 1 | Types, `NOT NULL`, CHECK constraints | Malformed values, invalid enums, inconsistent dates/status |
| 2 | Foreign keys, filtered unique indexes | Duplicate active head, membership, residence, address, pending item |
| 3 | Triggers | In-place history edits, temporal overlap, frozen decisions, audit mutation |
| 4 | Views + Application services + serializable transactions | Cross-table consistency, Golden Invariant, maker-checker |

**Why:** application code has bugs. The database is the last line that cannot be bypassed
by a forgotten validation or a stray `SaveChanges`.

**Trade-off:** trigger errors are less friendly than service errors. Accepted: services
validate first; the database guarantees correctness if they do not.

### 5. One Golden Invariant, continuously checked

> Every `ACTIVE` citizen has exactly one active membership, exactly one active residence,
> and the residence's canonical address equals the household's active canonical address.

`vw_GoldenInvariantViolations` implements checks `C1`–`C5` and `H1`–`H4`. It must return
zero rows after every commit. Integration tests assert this positively **and** with a
negative control that deliberately creates a violation and proves the view detects it.

**Why:** a green test that cannot fail is theatre. The negative control makes the
invariant credible.

### 6. Two-phase maker-checker relocations

Relocations are the only high-risk, cross-household operation, so they are the only
operation requiring two people:

1. **Maker** creates a `TransferRequest` (`PENDING`) with one item per moving citizen.
2. **Checker** (a different user) approves or rejects, item by item.
3. Execution closes old tenures, handles the source household, opens new tenures, and
   writes one correlated audit trail — in a single `Serializable` transaction.

**Why:** dual control prevents a single officer from silently moving a citizen or
emptying a household. Item-level decisions preserve the citizens who are not approved.

**Trade-off:** relocation takes two sessions. Accepted: this is a deliberate
administrative control, not friction to be optimized away.

### 7. Derive current state with views

| View | Derives |
|---|---|
| `vw_HouseholdCurrent` | Current address, current head, active member count |
| `vw_AddressCanonical` | Terminal address after following `ReplacedByAddressId` up to 20 levels |
| `vw_GoldenInvariantViolations` | All nine invariant violations |

**Why:** one source of truth. A "current" column would duplicate the history table and
drift.

### 8. Immutable, failure-resistant audit trail

- Success: `AuditLog` row written **inside** the business transaction, so data and audit
  commit together.
- Failure: transaction is rolled back first; the failure row is written on a **new
  connection** via `SqlResilience`, so it survives the rollback.
- Every row carries `CorrelationId`, grouping all entries produced by one use case.
- The database forbids `UPDATE` and `DELETE` on `AuditLog`; the application role is
  denied those operations as well.

**Why:** an audit that can be edited or lost is not an audit.

### 9. Optimistic concurrency everywhere

All nine mutable tables carry `rowversion`, mapped with `.IsRowVersion()`. A stale
`SaveChanges` raises `DbUpdateConcurrencyException`; the UI must reload and retry.

**Why:** officers may work concurrently. Last-write-wins would silently discard a
colleague's change. Pessimistic locking would hold transactions open across user
thinking time.

### 10. Local-first, reproducible database

SQL Server 2022 Developer Edition runs in Docker on `localhost:1433`. The schema is
deployed from one script. Every developer has an identical, disposable database with no
cloud account, firewall rule, or shared-state race.

**Why:** a shared cloud database made collaboration harder — network changes broke
connections, one developer's experiment corrupted another's data, and provisioning
consumed time that belonged to the project. Local Docker removes all three problems.

**Trade-off:** no shared live data across the team. Accepted: seed data and integration
tests replace it, and each developer can reset without asking permission.

---

## 4.2 How the strategy satisfies the quality goals

| Quality goal (§1.4) | Strategic response |
|---|---|
| Historical integrity | Temporal tables + append-only + guard triggers + `VOID`/`Corrects...Id` |
| Cross-record consistency | Atomic composite services + Golden Invariant view + integration tests |
| Auditability and dual control | Transactional success audit + independent failure audit + `CK_TR_Decision` |
| Security and least privilege | Salted hashing + `aris_app` DENY rules + service-level authorization + secrets outside Git |
| Maintainability | Four layers + vertical slices + canonical patterns + stable requirement IDs |
| Verifiability | Database-enforced rules + real-SQL integration tests + PR gates |
| Local-first operability | Docker Compose + one DDL script + user-secrets |

---

## 4.3 Technology choices

| Technology | Choice | Reason |
|---|---|---|
| Runtime | .NET 10 | Course requirement; current LTS-adjacent toolchain |
| UI | WinForms | Course requirement; fastest path to a desktop CRUD application |
| ORM | EF Core 10 | First-class SQL Server support, `rowversion`, computed columns, transactions |
| Provider | `Microsoft.EntityFrameworkCore.SqlServer` | Required for the target engine |
| Database | SQL Server 2022 Developer (Docker) | Required SQL Server features: filtered indexes, `rowversion`, `ISJSON`, `THROW` |
| Validation | FluentValidation | Declarative rules, testable without a database |
| Testing | xUnit v3 + Microsoft.Testing.Platform + real SQL Server | Database rules must be tested against the database |
| Password hashing | PBKDF2 or BCrypt | `CON-SEC-01`; no external identity dependency |
| Schema deployment | `sqlcmd` in the container, wrapped by a PowerShell script | Reproducible, inspectable, no migration framework |
| Documentation | arc42 + Mermaid | Stable structure, diagrams render without extra tooling |

---

## 4.4 What we deliberately did not do

| Rejected alternative | Why |
|---|---|
| Shared Azure SQL Database | Network fragility, cost, shared-state corruption, slow onboarding (`D15`) |
| EF Core migrations | A second schema history would conflict with the canonical DDL (`D16`) |
| Repository/Unit-of-Work abstraction over EF Core | EF Core already is the unit of work; an extra layer adds ceremony without a second implementation |
| Storing `CurrentAddressId` on `Household` | Dual source of truth; destroys history (`D4`) |
| Soft delete on history tables | `Status = 'VOID'` plus a correcting row is richer and keeps intervals honest (`D3`) |
| CQRS / event sourcing | Far beyond the course scope; the append-only tables already provide the needed temporal record |
| Microservices / web API | The required deliverable is a single Windows desktop application |
