# Work Breakdown Structure & Implementation Plan
## Residence Information Management System

**Version:** 2.0  
**Status:** Implementation Baseline  
**Phase:** Work Breakdown → Active Implementation  
**Target Platform:** .NET (C#) Desktop Application (WPF / Windows Forms) · SQL Server 2019+ · Entity Framework Core  

---

## 1. Purpose & Strategy

This document translates the Domain Model, Business Rules ([`doc.md`](file:///C:/Users/Bryan/Documents/residence_information_management_system/docs/doc.md)), and Database Schema v2 ([`database_design_v2.md`](file:///C:/Users/Bryan/Documents/residence_information_management_system/docs/database_design_v2.md), [`residence_schema_v2.sql`](file:///C:/Users/Bryan/Documents/residence_information_management_system/docs/residence_schema_v2.sql)) into a structured, actionable execution roadmap for the development team.

### Core Implementation Strategy: Vertical Feature Ownership
We reject the anti-pattern of horizontal slicing ("one developer writes all UI, one writes all SQL"). Instead, developers own **vertical feature slices end-to-end**:

```text
Presentation Layer (UI / Views / ViewModels)
       ↓
Application Layer (Use Cases / Services / DTOs / Validation)
       ↓
Domain Layer (Entities / Invariants / Business Rules)
       ↓
Infrastructure Layer (EF Core DbContext / Mappings / Repositories)
       ↓
Database Layer (SQL Server 2019+ / Constraints / Triggers)
```

Each coder understands and controls their assigned features from the UI down to the database transactions, ensuring feature autonomy and eliminating cross-layer bottlenecks.

---

## 2. Team Structure & Role Distribution

The team consists of 5 specialized members:

```text
                        ┌────────────────────────┐
                        │      Team Leader       │
                        │ Architecture & Reviews │
                        └───────────┬────────────┘
                                    │
          ┌─────────────────────────┼─────────────────────────┐
          │                         │                         │
┌─────────▼──────────┐    ┌─────────▼──────────┐    ┌─────────▼──────────┐
│ Database Designer  │    │      Coder 1       │    │      Coder 2       │
│ Data & Persistence │    │ Citizen & Search   │    │ Household & Move   │
└────────────────────┘    └────────────────────┘    └────────────────────┘
                                    │
                          ┌─────────▼──────────┐
                          │      Coder 3       │
                          │ Auth, Shell & Rpts │
                          └────────────────────┘
```

| Member | Assigned Role | Primary Ownership | Secondary / Collaborative Scope |
|---|---|---|---|
| **Member 1** | **Team Leader** | Solution architecture, project scaffolding, layer boundaries, cross-module integration, PR reviews. | Audit logging standards, transaction boundary oversight, complex transfer review. |
| **Member 2** | **Database Designer** | SQL Server schema v2 deployment, EF Core mappings, seed data, DB constraints, triggers, DB test harness. | Concurrency testing (`RowVersion`), query performance, view maintenance (`vw_GoldenInvariantViolations`). |
| **Member 3** | **Coder 1** | **Module A:** Citizen Management (`CIT-01`–`CIT-04`) & Directory Search (`SRCH-01`). | **First Vertical Slice** reference implementation, citizen validation, personal profile views. |
| **Member 4** | **Coder 2** | **Module B:** Household, Residence & Transfer Engine (`HH-01`–`HH-06`, `RES-01`–`RES-03`, `UC-06`). | Relocation maker-checker workflow, serializable transactions, head succession logic. |
| **Member 5** | **Coder 3** | **Module C:** Authentication (`AUTH-01`, `ACC-01`), Main Desktop Shell, and Reports (`RPT-01`–`RPT-03`). | UI styling conventions, role-based screen visibility (`OFFICER` vs `ADMIN`), system config (`SYS-01`). |

---

## 3. Detailed Work Allocation by Team Member

### 3.1. Team Leader — Architecture, Integration & Standards

The Team Leader maintains overall system integrity, establishes development guidelines, and orchestrates cross-module workflows.

#### Specific Responsibilities
- **Project Scaffolding:** Create the .NET solution structure enforcing the 4-layer architecture:
  - `ResidenceManagement.Domain` (Entities, Enums, Invariant interfaces)
  - `ResidenceManagement.Application` (Service contracts, DTOs, Validators, Use Cases)
  - `ResidenceManagement.Infrastructure` (EF Core `ResidenceDbContext`, Entity Configurations, Logging, Security)
  - `ResidenceManagement.UI` (Windows Desktop UI shell, Views, ViewModels / Form controllers)
- **Shared Coding Standards:** Define conventions for Dependency Injection (DI), Result/Response patterns, exception handling, and logging.
- **Cross-Module Workflows:** Oversee integration where multiple domains intersect (e.g., Transfer touching Citizen, Household, Membership, Residence, and AuditLog).
- **Code Reviews:** Review all Pull Requests into `develop`. Enforce that **no UI code contains direct SQL or business validation logic**.
- **Invariant Guardian:** Ensure that automated integration tests continuously execute:
  ```sql
  SELECT * FROM dbo.vw_GoldenInvariantViolations;
  ```
  and yield **0 rows**.

#### Key Deliverables
1. Clean Visual Studio solution and `.csproj` project references configured.
2. Base classes (`EntityBase`, `Result<T>`, `ValidationException`, `BaseService`).
3. Dependency injection registration container in UI startup.
4. Documented PR review guidelines and integration test suite.

---

### 3.2. Database Designer — Data Modeling, Persistence & Database Integrity

The Database Designer acts as the bridge between relational storage and EF Core persistence, ensuring strict constraint enforcement.

#### Specific Responsibilities
- **Database Deployment:** Execute and verify `residence_schema_v2.sql` on a dedicated SQL Server 2019+ instance.
- **Trigger & Constraint Verification:** Verify that database triggers compile and fire correctly:
  - `trg_Residence_Guard` (Append-only + temporal non-overlap, error 51001/51002)
  - `trg_Membership_Guard` (Append-only + temporal non-overlap, error 51011/51012)
  - `trg_HouseholdAddress_Guard` (Append-only + temporal non-overlap, error 51021/51022)
  - `trg_AuditLog_AppendOnly` (Immutability, error 51030)
  - `trg_TransferRequest_Frozen` (Frozen decided requests, error 51040)
- **EF Core Mapping & Configuration:** Configure Fluent API entity mappings (`IEntityTypeConfiguration<T>`):
  - Mark tables with triggers using `.ToTable(t => t.HasTrigger("trg_..."))` (EF Core 7+ requirement).
  - Map `RowVersion` columns using `.IsRowVersion()`.
  - Configure `AddressText` as a computed column with `.HasComputedColumnSql()`.
  - Configure composite foreign keys on `TransferRequestItem` (`(SourceMembershipId, CitizenId)` and `(SourceResidenceId, CitizenId)`).
- **Seed Data:** Seed initial lookup values, system configuration keys (`PASSWORD_MIN_LENGTH`, `SESSION_TIMEOUT_MINUTES`, `MAX_FAILED_LOGINS`, `LOCKOUT_MINUTES`), and sample test addresses.
- **Test Harness:** Provide SQL integration scripts verifying that illegal states (overlapping residence periods, duplicate active memberships, self-approvals) are blocked by the database.

#### Key Deliverables
1. Verified SQL Server database baseline.
2. `ResidenceDbContext` and complete EF Core entity configuration classes.
3. Seeding scripts / migration configuration for test and development environments.
4. Unit/Integration test harness for database constraint and trigger validation.

---

### 3.3. Coder 1 — Module A: Citizen Management & Directory Search

Coder 1 implements the first complete vertical slice of the application, serving as the blueprint for the other coders.

#### Specific Responsibilities
- **First Vertical Slice Implementation:**
  - Build Citizen Registration (`CIT-01`), Citizen Profile Viewing (`CIT-02`), Demographic Updates (`CIT-03`), and Citizen Search (`SRCH-01`).
  - Demonstrate proper layer decoupling: `CitizenView` $\rightarrow$ `CitizenService` $\rightarrow$ `ICitizenRepository` / EF Core $\rightarrow$ SQL Server.
- **Citizen Domain Logic:**
  - Enforce the 3 citizen lifecycle states: `UNASSIGNED` (initial), `ACTIVE` (assigned to household), `INACTIVE` (deactivated).
  - Validate 12-digit numeric National ID (`NationalId`), mandatory birth dates, and demographics.
- **Citizen Deactivation (`CIT-04`):**
  - Implement deactivation logic in coordination with Coder 2 (handling head succession if the deactivating citizen is currently a household `HEAD`).
  - Set `DeactivatedAt`, transition status to `INACTIVE`.
- **Search Capabilities:**
  - Fast search by National ID, full name (case-insensitive with accent handling), and status.
- **Citizen Profile Screen:**
  - Display personal demographic summary, current active household, current active residence, and complete chronological residency history.

#### Key Deliverables
1. `ICitizenService` and `CitizenService` implementation with FluentValidation.
2. Citizen UI forms: List/Search Citizen, Register Citizen dialog, Citizen Detail & History view.
3. Unit and integration tests for citizen lifecycle transitions.

---

### 3.4. Coder 2 — Module B: Household, Residence & Relocations Engine

Coder 2 is responsible for the core domain workflows, handling the most intricate business rules and multi-table transactions.

#### Specific Responsibilities
- **Household Management (`HH-01`–`HH-04`):**
  - `HH-01` (Create Household): Atomic creation of `Household` + `HouseholdAddress` + initial `HEAD` membership + `Residence` for an `UNASSIGNED` citizen $\rightarrow$ transition citizen to `ACTIVE`.
  - `HH-02` (Add Member): Assign an `UNASSIGNED` citizen as `MEMBER` with residence at the household's address $\rightarrow$ transition citizen to `ACTIVE`.
  - `HH-04` (Change Head): Close former head record and successor's member record (`EndDate = E - 1`), open new member record for former head and new head record for successor (`StartDate = E`).
  - `HH-06` (Household History): Display historical addresses (`HouseholdAddress`) and membership tenure (`HouseholdMembership`).
- **Residence Lifecycle (`RES-01`–`RES-03`):**
  - Embed residence registration and closing as atomic steps inside household registration, member addition, and transfer workflows.
- **Maker-Checker Transfer Engine (`UC-06` / `HH-05`):**
  - Implement the two-phase relocation workflow:
    - **Phase 1 (Maker - Officer A):** Validate candidate members, select transfer type (`JOIN_EXISTING`, `NEW_HOUSEHOLD`, `HOUSEHOLD_MOVE`), specify destination, and insert `TransferRequest(PENDING)` with `TransferRequestItem(PENDING)` rows.
    - **Phase 2 (Checker - Officer B $\ne$ A):** Review documentation, re-validate data against committed database state, approve/reject individual items, and execute the atomic state transition inside a `Serializable` transaction.
  - Implement the 3 transfer variants:
    - `JOIN_EXISTING`: Close old records $\rightarrow$ open `MEMBER` membership and residence at target household.
    - `NEW_HOUSEHOLD`: Close old records $\rightarrow$ create new `Household`, `HouseholdAddress`, memberships (`HEAD` + `MEMBER`s), and residences.
    - `HOUSEHOLD_MOVE`: Preserve `HouseholdId` and memberships $\rightarrow$ close old `HouseholdAddress`, open new `HouseholdAddress` $\rightarrow$ open new residences for all members.
  - Handle departing heads: require and validate `SuccessorHeadCitizenId`.
  - Handle empty source households: transition empty household to `INACTIVE` and close its `HouseholdAddress`.
- **Record Rectification (`HIST-01` / `BR-HIST-02`):**
  - Allow officers to void faulty historical records (`Status = 'VOID'`) and insert corrected records with `Corrects...Id`.

#### Key Deliverables
1. `IHouseholdService`, `IResidenceService`, and `ITransferService`.
2. UI screens for Household Registration, Household Details/Members, Head Reassignment, and Transfer Request (Creation & Approval).
3. Comprehensive transaction tests for all 3 transfer shapes, ensuring zero violations of `vw_GoldenInvariantViolations`.

---

### 3.5. Coder 3 — Module C: Authentication, Main Desktop Shell & Reports

Coder 3 delivers the application container, user security, system administration, and management reporting.

#### Specific Responsibilities
- **Authentication & Security (`AUTH-01`, `ACC-01`):**
  - Login window: authenticate user credentials, compute password hashes (PBKDF2 / BCrypt), enforce account lockout on exceeding `MAX_FAILED_LOGINS`.
  - Session state: manage the current authenticated user context (`UserId`, `Username`, `FullName`, `Role`).
  - Mandatory password change handling if `MustChangePassword == true`.
- **Main Desktop Shell & Navigation:**
  - Design the main application window with modular navigation (Sidebar / Menu / Ribbon).
  - Embed module entry points: Citizen Directory, Household Directory, Transfer Inbox, Reports, Administration.
  - Implement dynamic role-based UI visibility: hide Admin screens (`Manage Accounts`, `System Configuration`, `Audit Logs`) when logged in as `OFFICER`.
  - Display current user name, role, and active session status bar.
- **Account Administration (`ACC-01`, `PERM-01`):**
  - CRUD interface for User Accounts: create officer accounts, reset passwords, lock/unlock accounts.
- **Statistical Reports (`RPT-01`–`RPT-03`):**
  - `RPT-01` (Demographic Distribution): Aggregate active residents grouped by Province, District, and Ward using `vw_AddressCanonical`.
  - `RPT-02` (Household Summary): Statistics on total households, active vs. inactive, average household size, and single-member households.
  - `RPT-03` (Residency Movements): Date-range reporting on new registrations, inward/outward transfers, and citizen deactivations.
  - Provide export features (CSV or formatted print preview).
- **System Configuration (`SYS-01`):**
  - Admin UI to update runtime parameters in `SystemConfiguration`.

#### Key Deliverables
1. `IAuthenticationService`, `IAccountService`, and `IReportService`.
2. Login dialog and Main Desktop Shell window with dynamic navigation.
3. User Account Management UI and interactive Reports dashboard.

---

## 4. Phase-by-Phase Implementation Roadmap

```text
Phase 1: Foundation & Baseline (Days 1–3)
   │
Phase 2: First Vertical Slice - Citizen (Days 4–6)  <-- Architectural Reference
   │
Phase 3: Core Household & Residence Domain (Days 7–11)
   │
Phase 4: Maker-Checker Transfer Engine (Days 12–16) <-- Critical Path
   │
Phase 5: Auth, Admin, Reports & Audit Logging (Days 17–19)
   │
Phase 6: End-to-End Integration & Golden Invariant Verification (Days 20–22)
   │
Phase 7: Hardening, Concurrency Testing & Final Acceptance (Days 23–25)
```

---

### Phase 1: Foundation & Baseline

| Task ID | Task Description | Owner | Dependencies | Definition of Done |
|---|---|---|---|---|
| **P1-TL1** | Initialize solution structure (`Domain`, `Application`, `Infrastructure`, `UI`) and configure git repository | Team Leader | — | Solution builds cleanly; project references and architecture layers established. |
| **P1-DB1** | Deploy `residence_schema_v2.sql` to local/shared SQL Server; verify triggers and views | Database Designer | — | Script executes without errors; all 5 triggers and 3 views exist and compile. |
| **P1-DB2** | Build `ResidenceDbContext` with Fluent API mappings, `HasTrigger()`, and `RowVersion` | Database Designer | P1-DB1, P1-TL1 | EF Core can connect, read, and write entities against the database. |
| **P1-C3** | Implement base UI Shell layout, navigation container, and shared styling assets | Coder 3 | P1-TL1 | Shell loads with placeholder navigation sections; responsive styling. |
| **P1-C1** | Define Citizen entity contracts, DTOs, and validation rules in Application layer | Coder 1 | P1-TL1 | `CreateCitizenDto`, `CitizenDetailDto`, and FluentValidation rules compile. |
| **P1-C2** | Define Household and Residence entity contracts, DTOs, and Service interfaces | Coder 2 | P1-TL1 | `IHouseholdService`, `ITransferService` interfaces and DTOs drafted. |

---

### Phase 2: First Vertical Slice (Citizen Management Reference)

*Goal: Establish the end-to-end coding standard that the entire team will follow for the rest of the project.*

| Task ID | Task Description | Owner | Dependencies | Definition of Done |
|---|---|---|---|---|
| **P2-C1** | Implement `CitizenService` (Create, View, Update demographics, Search) | Coder 1 | P1-DB2, P1-C1 | Business logic decoupled from UI; validations pass; unit tests written. |
| **P2-UI1** | Build Citizen Directory UI: Search bar, DataGrid, and "Create Citizen" modal | Coder 1 | P2-C1, P1-C3 | UI communicates with `CitizenService`; handles validation errors gracefully. |
| **P2-TL1** | **Architectural Review of First Slice:** inspect code structure, error handling, DI, and PR quality | Team Leader | P2-UI1 | PR approved; coding conventions documented and shared with team. |
| **P2-DB1** | Verify Citizen database operations: index performance and `CK_Citizen_Lifecycle` | Database Designer | P2-C1 | SQL queries profiled; lifecycle constraint correctly prevents invalid deactivations. |

---

### Phase 3: Core Household & Residence Domain

| Task ID | Task Description | Owner | Dependencies | Definition of Done |
|---|---|---|---|---|
| **P3-C2A** | Implement `HouseholdService.RegisterHousehold` (`HH-01`) | Coder 2 | P2-TL1 | Atomic creation of Household, Address, Head Membership, and Residence; Citizen becomes `ACTIVE`. |
| **P3-C2B** | Implement `HouseholdService.AddMember` (`HH-02`) and `ChangeHead` (`HH-04`) | Coder 2 | P3-C2A | Add member creates membership and residence; Change Head performs append-only close/open. |
| **P3-C2C** | Build Household Management UI (Household list, detail view, member grid, change head dialog) | Coder 2 | P3-C2B | Officer can view household members and execute member operations from UI. |
| **P3-C1** | Update Citizen Profile UI to show current household, current address, and residency history | Coder 1 | P3-C2C | Citizen view displays active household details retrieved via `vw_HouseholdCurrent`. |
| **P3-DB1** | Test triggers `trg_Membership_Guard` and `trg_Residence_Guard` on append-only violations | Database Designer | P3-C2B | In-place updates to `Role` or `StartDate` fail with trigger errors 51011 / 51001. |
| **P3-TL1** | **Checkpoint 2 Review:** Verify Golden Invariant after household operations | Team Leader | P3-C2C, P3-DB1 | `vw_GoldenInvariantViolations` returns 0 rows after registering households and adding members. |

---

### Phase 4: Maker-Checker Transfer Engine

| Task ID | Task Description | Owner | Dependencies | Definition of Done |
|---|---|---|---|---|
| **P4-C2A** | Implement Phase 1 of Transfer: Request Creation (`TransferService.CreateRequest`) | Coder 2 | P3-TL1 | Validates preconditions; saves `TransferRequest(PENDING)` and `TransferRequestItem`s. |
| **P4-C2B** | Implement Phase 2 of Transfer: Review & Execution (`TransferService.ExecuteTransfer`) | Coder 2 | P4-C2A | Runs in `Serializable` transaction; executes `JOIN_EXISTING`, `NEW_HOUSEHOLD`, `HOUSEHOLD_MOVE`. |
| **P4-C2C** | Build Transfer UI: Transfer Request Wizard (Maker) and Transfer Decision Inbox (Checker) | Coder 2 | P4-C2B | Maker can submit requests; Checker can approve/reject individual items and commit. |
| **P4-DB1** | Verify `CK_TR_Decision` (Maker $\ne$ Checker) and `CK_TR_Shape` in SQL Server | Database Designer | P4-C2B | Self-approval is rejected by database constraint; invalid request payloads blocked. |
| **P4-TL1** | Code review and concurrency edge case inspection of Transfer transaction | Team Leader | P4-C2C, P4-DB1 | Verifies rollback handling, predecessor date constraints ($E > \text{StartDate}$), and audit log integration. |

---

### Phase 5: Authentication, System Administration, Reports & Audit Logging

| Task ID | Task Description | Owner | Dependencies | Definition of Done |
|---|---|---|---|---|
| **P5-C3A** | Implement `AuthenticationService`: Login, password hashing, lockout logic | Coder 3 | P1-DB2 | Validates credentials; locks accounts on 5 failed attempts; manages session state. |
| **P5-C3B** | Implement Account Management UI (`ACC-01`) and System Configuration UI (`SYS-01`) | Coder 3 | P5-C3A | Admin can create officers, reset passwords, and edit configuration parameters. |
| **P5-C3C** | Implement Report Engine & UI (`RPT-01`, `RPT-02`, `RPT-03`) | Coder 3 | P3-TL1, P5-C3A | Displays demographic distribution, household statistics, and movement reports. |
| **P5-TL1** | Standardize cross-cutting Audit Logging across all application services | Team Leader | All Services | `AuditLog` captures Who, What, When, Entity, Result, JSON details, and `CorrelationId`. |
| **P5-DB1** | Verify audit log immutability (`DENY UPDATE/DELETE`, trigger `trg_AuditLog_AppendOnly`) | Database Designer | P5-TL1 | Direct SQL updates or deletes on `AuditLog` fail with error 51030. |

---

### Phase 6: End-to-End Integration & Golden Invariant Verification

| Task ID | Task Description | Owner | Dependencies | Definition of Done |
|---|---|---|---|---|
| **P6-INT1** | Integrate UI Shell with all feature modules under role-based authorization | Coder 3, Team Leader | All Modules | Officers see only operational screens; Admins see system management screens. |
| **P6-INT2** | Execute comprehensive lifecycle integration scenario: Create Citizen $\rightarrow$ Form Household $\rightarrow$ Transfer $\rightarrow$ Report | All Members | P6-INT1 | Complete end-to-end citizen journey executes seamlessly through UI. |
| **P6-INV** | Run automated Golden Invariant checker after every scenario step | Database Designer, Team Leader | P6-INT2 | `SELECT * FROM dbo.vw_GoldenInvariantViolations;` returns **0 rows** throughout all tests. |

---

### Phase 7: Hardening, Concurrency Testing & Acceptance

| Task ID | Task Description | Owner | Dependencies | Definition of Done |
|---|---|---|---|---|
| **P7-CONC** | Execute concurrent modification test simulating two officers editing the same household | Database Designer, Coder 2 | P6-INT2 | `DbUpdateConcurrencyException` caught via `RowVersion`; user alerted to refresh data. |
| **P7-ERR** | Verify post-rollback failure logging: simulate DB error during transfer, verify FAILURE audit row | Team Leader, Coder 2 | P6-INT2 | Business transaction rolls back; `AuditLog` records `Result = 'FAILURE'` on separate connection. |
| **P7-POL** | UI polishing: validation error tooltips, progress bars, responsive table layouts | Coders 1, 2, 3 | P6-INT1 | Polished user interface without unhandled exceptions or visual glitches. |
| **P7-DOC** | Finalize technical documentation, user guide, and deployment instructions | Team Leader, All | P7-POL | Documentation aligns with implemented code. Ready for submission. |

---

## 5. RACI Responsibility Assignment Matrix

**R** = Responsible (Does the work)  
**A** = Accountable (Final approval & architectural sign-off)  
**C** = Consulted (Provides two-way input / assistance)  
**I** = Informed (Kept updated on progress)

| Feature / Work Area | Team Leader | Database Designer | Coder 1 | Coder 2 | Coder 3 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Solution Architecture & Layer Boundaries** | **A / R** | C | I | I | I |
| **Database Schema v2 & Migrations** | A | **R** | I | C | I |
| **EF Core DbContext & Entity Mappings** | A | **R** | C | C | C |
| **Citizen Management (`CIT-01`–`CIT-04`)** | A | C | **R** | I | I |
| **Directory Search (`SRCH-01`–`SRCH-03`)** | A | C | **R** | I | C |
| **Household & Membership (`HH-01`–`HH-04`)** | A | C | C | **R** | I |
| **Residence Tracking (`RES-01`–`RES-03`)** | A | C | I | **R** | I |
| **Maker-Checker Transfer Engine (`UC-06`)** | A | C | I | **R** | I |
| **Record Rectification (`HIST-01`)** | A | C | I | **R** | I |
| **Authentication & User Accounts (`AUTH-01`, `ACC-01`)** | A | I | I | I | **R** |
| **Desktop UI Shell & Navigation Container** | A | I | C | C | **R** |
| **Reports Engine (`RPT-01`–`RPT-03`)** | A | C | I | I | **R** |
| **Cross-Cutting Audit Logging** | **A / R** | C | C | C | C |
| **Golden Invariant Test Verification** | **A** | **R** | I | C | I |
| **Optimistic Concurrency Verification (`RowVersion`)** | A | **R** | I | C | I |

---

## 6. Definition of Done (DoD)

A user story or task is only marked as **Done** when all of the following criteria are satisfied:

### 1. Functional Completeness
- [ ] Primary flow (Happy Path) functions correctly through the UI.
- [ ] Negative flows and boundary conditions handled (invalid IDs, empty fields, past dates).
- [ ] User input validated using FluentValidation before service dispatch.

### 2. Architectural Compliance
- [ ] UI event handlers contain **zero SQL logic** and **zero business validation rules**.
- [ ] Presentation Layer only interacts with the Application Layer via DTOs and Service interfaces.
- [ ] All cross-table mutations are wrapped in an atomic database transaction.

### 3. Database & Historical Integrity
- [ ] History records (`HouseholdMembership`, `Residence`, `HouseholdAddress`) are never overwritten (`UPDATE`) or deleted (`DELETE`).
- [ ] Replaced records have their `EndDate` set to `EffectiveDate - 1`.
- [ ] Overlap check triggers pass without error.
- [ ] The Golden Invariant query (`SELECT * FROM dbo.vw_GoldenInvariantViolations;`) returns **0 rows**.

### 4. Code Quality & Version Control
- [ ] Code compiles without warnings or errors.
- [ ] Meaningful git commit messages on dedicated feature branch (`feature/<feature-name>`).
- [ ] Pull Request submitted against `develop`, reviewed, and approved by the Team Leader.

---

## 7. Git Workflow & Collaboration Guidelines

```text
main (Production / Submission Baseline)
  └── develop (Integration Branch)
        ├── feature/architecture-scaffolding  (Team Leader)
        ├── feature/database-persistence      (Database Designer)
        ├── feature/citizen-management        (Coder 1)
        ├── feature/household-residence       (Coder 2)
        ├── feature/transfer-engine           (Coder 2)
        ├── feature/auth-main-shell           (Coder 3)
        └── feature/reports                   (Coder 3)
```

### Git Rules for All Members
1. **Never commit directly to `main` or `develop`.** Always branch off `develop` using the format `feature/<module-name>`.
2. **Sync frequently:** Rebase or pull `develop` into your feature branch daily to prevent merge conflicts.
3. **Pull Request Protocol:**
   - Every PR requires a brief summary of changes, screenshots of UI (if applicable), and confirmation that local tests pass.
   - The Team Leader is the designated reviewer for all PRs.
   - PRs must build cleanly and pass all automated tests before merging.
4. **Integration Testing:** Merges into `develop` trigger local integration testing against the shared database.

---

## 8. Immediate Action Items (Next 48 Hours)

```text
Team Leader:
  [ ] Create Visual Studio Solution and 4 core project layers
  [ ] Configure Dependency Injection setup and base service abstractions
  [ ] Publish PR review checklist and Git branch policies

Database Designer:
  [ ] Execute residence_schema_v2.sql on SQL Server instance
  [ ] Verify all 5 triggers and 3 views
  [ ] Generate EF Core DbContext and entity configurations with HasTrigger() and IsRowVersion()

Coder 1:
  [ ] Scaffold Citizen DTOs and ICitizenService interface
  [ ] Begin First Vertical Slice (Citizen Create / View / Search)

Coder 2:
  [ ] Review Household/Residence schema and Transfer business rules
  [ ] Draft IHouseholdService and ITransferService contracts

Coder 3:
  [ ] Scaffold Main Desktop Window shell with navigation tabs
  [ ] Implement Login dialog and password hashing utility
```
