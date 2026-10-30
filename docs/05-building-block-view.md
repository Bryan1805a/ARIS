# 5. Building Block View

> **arc42 §5** · ARIS 3.0 · [Documentation hub](README.md)

The view is described at three levels: the system as a whole, the layers and their
components, and the database objects. Entity columns live in
[`appendix/A-domain-model.md`](appendix/A-domain-model.md); database objects live in
[`appendix/B-database-reference.md`](appendix/B-database-reference.md).

---

## 5.1 Level 1 — the system

```mermaid
flowchart TB
    subgraph ARIS["ARIS desktop application"]
        UI["Presentation<br/><i>Aris.UI</i>"]
        APP["Application<br/><i>Aris.Application</i>"]
        DOM["Domain<br/><i>Aris.Domain</i>"]
        INF["Infrastructure<br/><i>Aris.Infrastructure</i>"]
    end
    DB[("SQL Server 2022<br/>ArisDb")]

    UI -->|"DTO + Result&lt;T&gt;"| APP
    APP --> DOM
    INF --> APP
    INF --> DOM
    UI -. "composition root only" .-> INF
    INF -->|"EF Core / ADO.NET"| DB
```

| Block | Responsibility | Must not |
|---|---|---|
| `Aris.UI` | Windows shell, screens, view models, user session, input presentation | Contain SQL, `DbContext`, or business rules |
| `Aris.Application` | Use cases, orchestration, validation, DTO mapping, authorization, auditing calls | Reference WinForms or `System.Data.SqlClient` |
| `Aris.Domain` | Entities, lifecycle states, invariants that are pure C# | Reference any other project or infrastructure type |
| `Aris.Infrastructure` | EF Core context and mappings, audit persistence, connection resilience, DI registration | Contain business decisions |
| SQL Server | Schema, constraints, triggers, views, role security, seed configuration | — |

> **Composition-root exception.** `Aris.UI` has a project reference to
> `Aris.Infrastructure` solely so `Program.cs` can call `AddInfrastructure(...)`. No form,
> view model, or control may use a type from Infrastructure. This is the only permitted
> exception to `UI → Application`.

---

## 5.2 Level 2 — layers and components

### 5.2.1 Presentation layer (`src/Aris.UI/`)

| Component | Responsibility | Status |
|---|---|---|
| `Program` | Composition root: configuration (including user-secrets), DI, start the shell | Implemented |
| `MainForm` + `MainForm.Designer` | Sidebar navigation, content host, status bar (user, role, connection), language button | Implemented |
| `IUserSession` implementation (`Session/UserSession`) | Holds the authenticated user for the lifetime of the shell | Implemented |
| `CitizenControl`, `CitizenDetailForm`, `CitizenEditForm` | Citizen directory, profile, create/update | Implemented |
| `HouseholdControl`, `HouseholdDetailForm`, `RegisterHouseholdForm`, `AddMemberForm`, `ChangeHeadForm`, `AddressEditForm` | Household, member, head-change and address screens | Implemented |
| `TransferControl`, `TransferRequestForm`, `TransferDecisionForm` | Maker wizard and checker inbox | Implemented |
| `LoginForm`, `ChangePasswordForm` | Authentication screens | Implemented |
| `BootstrapForm` | One-time first-administrator creation on an empty database | Implemented |
| `Localization/LanguageManager` | EN/VI switch, default Vietnamese; labels, titles and grid headers | Implemented |
| `AccountManagementControl`, `AuditViewerControl`, `SystemConfigurationControl` | Admin screens | Implemented |
| `ReportsControl` | Report selection, preview, CSV export | Implemented |
| Feature view models | Async orchestration, `Result<T>` presentation, no business rules | Partially: controls call Application services directly; no separate view-model layer |

**Rule:** every screen is registered as a transient in `Program.cs`; the shell resolves
it and hosts it in `contentPanel`. A screen never opens another top-level window for
navigation.

### 5.2.2 Application layer (`src/Aris.Application/`)

| Component | Responsibility | Status |
|---|---|---|
| `Common/Result`, `Result<T>` | Explicit success/failure outcome without exceptions | Implemented |
| `Common/ValidationException` | Thrown only for malformed input or programmer error | Implemented |
| `Common/BaseService` | Shared service scaffolding | Implemented |
| `Common/IUserSession` | Session contract used by services and view models | Implemented |
| `Auditing/IAuditService`, `AuditEntry`, `AuditFailureEntry` | Audit contract | Implemented |
| `Auditing/AuditEntryValidator` | Enforces column widths and required actor before SQL sees the row | Implemented |
| `Citizens/ICitizenService` + DTOs + validators | `CIT-01`–`CIT-04`, `SRCH-01` (implementation in Infrastructure) | Implemented |
| `Addresses/IAddressService` + DTOs | `ADR-01`, address lookup for households (implementation in Infrastructure) | Implemented |
| `Households/IHouseholdService` + DTOs | `HH-01`, `HH-02`, `HH-04`, `HH-06`, `SRCH-02` (implementation in Infrastructure) | Implemented |
| `Residences/IResidenceService` | `RES-03` current-residence query | Not a separate service; exposed through `CitizenService.GetByIdAsync` |
| `Transfers/ITransferService` + DTOs | `UC-06` maker and checker phases (implementation in Infrastructure) | Implemented |
| `Authentication/IAuthService` | `AUTH-01`, lockout, password change | Implemented |
| `Administration/IAccountService`, `IAuditQueryService`, `SystemConfigurationService` | `ACC-01`, `AUD-01` (recent view), `SYS-01` | Implemented |
| `Accounts/IBootstrapService` | One-time first-admin creation on a fresh database (`R15`, `T6`) | Implemented |
| `Accounts/IDemoDataService` | Idempotent demo dataset exposed to administrators (`T11`) | Implemented |
| `Reporting/IReportService` | `RPT-01`–`RPT-03` | Implemented |

Application owns the interfaces, DTOs and validators. Services that need `ArisDbContext`,
an explicit transaction or serializable isolation are implemented in Infrastructure
(`D18`); repository-backed services remain in Application. All follow the canonical
pattern in [`appendix/C-coding-standards.md`](appendix/C-coding-standards.md): validate
first, open one transaction, mutate, audit, commit, and log failures independently after
rollback.

### 5.2.3 Domain layer (`src/Aris.Domain/`)

| Component | Responsibility |
|---|---|
| `Common/EntityBase` | `RowVersion` for the nine mutable entities |
| `Entities/Citizen` | Person identity and lifecycle (`UNASSIGNED`, `ACTIVE`, `INACTIVE`) |
| `Entities/Household` | Legal family unit (`ACTIVE`, `INACTIVE`) |
| `Entities/Address` | Normalized location, validity, successor link, computed text |
| `Entities/HouseholdAddress` | Append-only household location history |
| `Entities/HouseholdMembership` | Append-only citizen–household tenure and role |
| `Entities/Residence` | Append-only citizen–address tenure |
| `Entities/TransferRequest` | Relocation header, maker-checker state |
| `Entities/TransferRequestItem` | One moving citizen per line, partial approval |
| `Entities/UserAccount` | Local account, role, lockout, password state |
| `Entities/SystemConfiguration` | Key/value operational parameters (no `RowVersion`) |
| `Entities/AuditLog` | Append-only audit record (no `RowVersion`) |
| `Enums/` | Strongly typed lifecycle values (planned refinement of string columns) |

The Domain layer contains no EF Core attributes and no database code.

### 5.2.4 Infrastructure layer (`src/Aris.Infrastructure/`)

| Component | Responsibility | Status |
|---|---|---|
| `Persistence/ArisDbContext` | Eleven `DbSet`s, applies configurations from assembly | Implemented |
| `Persistence/Configurations/*Configuration` | Table names, keys, lengths, triggers, row versions, computed column, FKs, delete behaviour | Implemented |
| `Auditing/AuditService` | `LogAsync` in the caller's transaction; `LogFailureIndependentAsync` on a new connection | Implemented |
| `Accounts/BootstrapService` | Serialized first-admin creation with transactional audit | Implemented |
| `Accounts/DemoDataService` | Idempotent demo addresses, citizens and households with invariant-safe tenures | Implemented |
| `Citizens/CitizenService` | Citizen registration, search, profile, update and deactivation | Implemented |
| `Addresses/AddressService` | Address registration and active-address lookup | Implemented |
| `Households/HouseholdService` | Household registration, add member, change head, search and history | Implemented |
| `Transfers/TransferService` | Maker-checker request creation, serializable execution, partial approval, cancellation | Implemented |
| `Resilience/SqlResilience` | Opens connections with exponential backoff for genuinely transient SQL errors | Implemented |
| `DependencyInjection.AddInfrastructure` | Registers `ArisDbContext`, audit, bootstrap/demo, citizen, address, household and transfer services | Implemented |

Mapping rules are normative and listed in
[`appendix/B-database-reference.md`](appendix/B-database-reference.md#b6-ef-core-mapping-requirements).

### 5.2.5 Database layer (`docs/aris_schema_v2.sql` + running instance)

| Object group | Members |
|---|---|
| Mutable tables (9) | `UserAccount`, `Citizen`, `Address`, `Household`, `HouseholdAddress`, `HouseholdMembership`, `Residence`, `TransferRequest`, `TransferRequestItem` |
| Non-versioned tables (2) | `SystemConfiguration`, `AuditLog` |
| Enforcement triggers (5) | `trg_Residence_Guard`, `trg_Membership_Guard`, `trg_HouseholdAddress_Guard`, `trg_TransferRequest_Frozen`, `trg_AuditLog_AppendOnly` |
| Views (3) | `vw_AddressCanonical`, `vw_HouseholdCurrent`, `vw_GoldenInvariantViolations` |
| Security | Role `aris_app`, user `aris_officer`, `DENY DELETE`, `DENY UPDATE` on `AuditLog` |
| Seed | Four `SystemConfiguration` rows |

---

## 5.3 Level 3 — database structure

The table-level structure is owned by the DDL. Read it together with:

- [`appendix/A-domain-model.md`](appendix/A-domain-model.md) — entities, columns, state machines
- [`appendix/B-database-reference.md`](appendix/B-database-reference.md) — constraints, triggers, views, error codes, mapping rules
- [`erd_v2.svg`](erd_v2.svg) — visual entity relationship diagram

```mermaid
erDiagram
    Citizen ||--o{ HouseholdMembership : "tenure"
    Household ||--o{ HouseholdMembership : "has members"
    Household ||--o{ HouseholdAddress : "location history"
    Address ||--o{ HouseholdAddress : "located at"
    Citizen ||--o{ Residence : "lives at"
    Address ||--o{ Residence : "address of"
    Household ||--o{ TransferRequest : "source of"
    Household ||--o{ TransferRequest : "destination of"
    TransferRequest ||--o{ TransferRequestItem : "contains"
    Citizen ||--o{ TransferRequestItem : "moves"
    HouseholdMembership ||--o{ TransferRequestItem : "source tenure"
    Residence ||--o{ TransferRequestItem : "source residence"
    UserAccount ||--o{ AuditLog : "acts"
    Address ||--o| Address : "replaced by"
```

---

## 5.4 Dependency and communication rules

| From → To | UI | Application | Domain | Infrastructure | Database |
|---|:---:|:---:|:---:|:---:|:---:|
| UI | — | ✔ DTO/`Result<T>` | ✘ | ✔ only `Program.cs` | ✘ |
| Application | ✘ | — | ✔ | via interfaces | ✘ |
| Domain | ✘ | ✘ | — | ✘ | ✘ |
| Infrastructure | ✘ | ✔ | ✔ | — | ✔ EF Core/ADO.NET |
| Database | ✘ | ✘ | ✘ | ✘ | — |

**Allowed call sequence for a use case:**

```text
Form → ViewModel → I<Module>Service (Application) → ArisDbContext / IAuditService (Infrastructure)
                                                          ↓
                                                    SQL Server
```

A form never calls Infrastructure. A service never calls a form. A domain entity never
calls a service.

---

## 5.5 Implementation status

This documentation describes the **target architecture**. The core vertical slices are
implemented; the remaining gaps are address search/retirement, historical rectification,
audit filtering and test/UI-automation depth.

| Area | Implemented | Missing / next |
|---|---|---|
| Solution structure, 4 projects | ✔ | — |
| `Result<T>`, `ValidationException`, `BaseService` | ✔ | — |
| `EntityBase` | ✔ | — |
| `IAuditService` + `AuditEntryValidator` + `AuditService` | ✔ | — |
| First-admin bootstrap + demo seed | ✔ | — |
| `SqlResilience` | ✔ | — |
| DI composition | ✔ | — |
| Domain entities | ✔ | All 11 |
| `ArisDbContext` + configurations | ✔ | All |
| EF Core SQL Server provider | ✔ | — |
| Citizen slice (`CIT-01`–`CIT-04`, `SRCH-01`) | ✔ | — |
| Household slice (`HH-01`, `HH-02`, `HH-04`, `HH-06`, `SRCH-02`) | ✔ | — |
| Transfer engine (`UC-06`) | ✔ | — |
| Auth, accounts, audit viewer, configuration, reports | ✔ core | `AUD-01` filtering/detail; admin mutation auditing (`T14`); report depth |
| Address search and retirement (`ADR-02`, `ADR-03`, `SRCH-03`) | ✘ | Address search/lineage UI |
| Historical rectification (`HIST-01`) | ✘ | Void-and-correct flow |
| Vietnamese/English UI switch | ✔ | Service-layer validation messages remain English |
| Database schema | ✔ deployed by script | — |
| Golden-invariant integration tests | ✔ | Extend for new rules as they land |
| Service-level tests and UI automation | Partial | `T3`, `T4` in §11 |

The remaining work is tracked in [`appendix/D-team-workflow.md`](appendix/D-team-workflow.md).
