# Administrative Residence Information System (ARIS)
## Software Requirements Specification & Domain Model — v2.0

> **Document Purpose:** Foundational document defining overall software requirements, domain model, business rules, and technical constraints. This document serves as the "Single Source of Truth" aligned with Database Design v2 (`database_design_v2.md` and `aris_schema_v2.sql`) to ensure consistency before and during application development.
> 
> **Academic Note:** This project is an academic prototype for a Windows Programming course, not a production national population registry, and does not connect directly to the National Population Database or the VNeID / Digital ID application.

---

## 1. Project Overview & Vision

### 1.1. Project Vision
The Administrative Residence Information System (ARIS) is a desktop application designed for local residence management officers, aimed at centralizing the administration and lookup of citizen residency data.

The system supports officers in receiving, verifying physical documentation against digital records, and recording verified residency changes into the database. All critical residency transitions are formally reviewed and authorized while maintaining an append-only historical audit trail and immutable audit logs.

### 1.2. Problem Statement
Historically, residency information was maintained on paper registers or fragmented data silos, resulting in several key challenges:
- Difficulties in performing fast cross-lookups between citizens and households.
- Risks of data corruption, inconsistency, or orphaned records during relocations.
- Inability to trace lineage: *Where did Citizen A live previously? When did they depart? Who processed and approved the transfer?*
- Absence of database-level invariants during household splits and transfers.

### 1.3. Goals
Deliver a reliable, closed-loop workflow:
$$\text{Intake} \longrightarrow \text{Verification} \longrightarrow \text{Approval (Maker-Checker)} \longrightarrow \text{Atomic State Transition} \longrightarrow \text{Inquiry / Reporting}$$

### 1.4. Scope & Boundaries
- **In Scope:**
  - Citizen demographic profiles required for residency tracking.
  - Standardized address management and administrative lineage (`Address Lineage`).
  - Household management, household address timelines, active member tracking, and household head designation.
  - Current and historical residency tracking.
  - Household registration, household splitting, transfers, and head reassignment.
  - Maker-checker workflow for transfer requests (`TransferRequest` + `TransferRequestItem`).
  - Append-only audit trail and statistical reporting.
- **Out of Scope:**
  - Vital records management (birth, death, marriage certificates).
  - Issuance or renewal of National Citizen ID (CCCD), driver's licenses, or social insurance cards.
  - Direct API integration with the National Population Database or VNeID.
  - Geographic access partitioning (all officers operate within the administrative domain).

---

## 2. Stakeholders & User Roles

The system serves 2 primary actor roles:

| Actor | Role & Key Responsibilities |
| :--- | :--- |
| **Residence Management Officer (Officer)** | - Review and verify citizen paperwork against digital records.<br>- Manage citizen records (`UNASSIGNED`, `ACTIVE`, `INACTIVE`) and address entries.<br>- Perform household registration, member additions, head changes, and record corrections.<br>- Initiate transfer requests (`Maker`) and review/decide transfer requests initiated by other officers (`Checker`).<br>- Query citizen, household, and address histories; generate statistical reports. |
| **System Administrator (Admin)** | - Manage officer accounts (create, activate, lock, unlock, reset passwords).<br>- Manage role-based permissions (`OFFICER`, `ADMIN`).<br>- Review immutable system audit logs (`AuditLog`).<br>- Configure system parameters (password policies, session timeouts, login retry limits). |

---

## 3. Core Concepts & Mental Model

The system strictly decouples 6 core domain concepts to eliminate redundancy and structural anomalies:

```text
                  +-----------+
                  |  Citizen  | (Who is this individual?)
                  +-----+-----+
                        |
             +----------+----------+
             |                     |
             v                     v
   +------------------+    +----------------+
   | HouseholdMember- |    |   Residence    |
   |      ship        |    | (Where does    |
   +--------+---------+    |  this person   |
            |              |  live, over    |
            |              |  what period?) |
            v              +-------+--------+
      +-----------+                |
      | Household |                |
      | (Which    |                |
      |  group?)  |                |
      +-----+-----+                |
            |                      |
            | 1..*                 |
            v                      v
   +------------------+    +----------------+
   | HouseholdAddress |    |    Address     |
   | (Address timeline|--->| (Physical      |
   |  of household)   |    |  canonical loc)|
   +------------------+    +----------------+
```

1. **Citizen:** Represents an individual uniquely identified by a 12-digit National ID (`NationalId`). A citizen moves through lifecycle states: `UNASSIGNED` (registered but not yet in any household), `ACTIVE` (assigned to a household and residence), or `INACTIVE` (management terminated).
2. **Household:** A distinct legal and administrative family unit. A household contains one or more members.
3. **HouseholdAddress:** Tracks the historical timeline of addresses occupied by a Household. A household has at most one `ACTIVE` address at any given time.
4. **Address:** A normalized geographic location within the administrative boundary hierarchy. When administrative borders change, existing address records are preserved; new addresses are introduced and linked via `ReplacedByAddressId` (Address Lineage).
5. **Residence:** The temporal relationship between a **Citizen** and an **Address** (`StartDate` $\rightarrow$ `EndDate`).
   $$\text{Residence} \neq \text{Address}$$
   *(Address represents physical space; Residence captures the historical occupancy of an individual at that space over time).*
6. **HouseholdMembership:** The temporal relationship between a **Citizen** and a **Household**, specifying role (`HEAD` or `MEMBER`) and occupancy interval (`StartDate` $\rightarrow$ `EndDate`).
   $$\text{Never store a direct static field } \texttt{Citizen.HouseholdId}$$
   *(A citizen belongs to different households across distinct time intervals).*

---

## 4. System Architecture & Constraints

### 4.1. Layered Architecture
The system follows a strict layered architecture:

```text
+-------------------------------------------------+
|      Presentation Layer (WinForms)              |
+------------------------+------------------------+
                         |
                         v
+-------------------------------------------------+
|    Application Layer (Services, DTOs, Use Cases)|
+------------------------+------------------------+
                         |
                         v
+-------------------------------------------------+
|      Domain Layer (Entities, Invariants, Rules) |
+------------------------+------------------------+
                         |
                         v
+-------------------------------------------------+
|   Infrastructure Layer (EF Core, SQL Server DB) |
+-------------------------------------------------+
```

> **Rule:** Business invariants and transaction boundaries must never be written inside UI event handlers (e.g., no direct SQL logic inside `button_Click`).

### 4.2. Technical Constraints
- **CON-PROJ-01:** Built with .NET 10 (C#) and a Windows Forms (WinForms) desktop UI, developed in Microsoft Visual Studio 2026, using Entity Framework Core and Microsoft SQL Server 2019+.
- **CON-PROJ-02:** Self-contained academic prototype with no dependencies on real-world government registries.
- **CON-SYS-01:** SQL Server database deployed as a centralized relational data store, running in a Docker container for local development.
- **CON-SYS-02:** Client application targets Windows desktop environments.
- **CON-SEC-01:** User passwords must never be stored in plaintext; secure salted password hashing is required.
- **CON-SEC-02:** Secrets and connection strings must not be hardcoded in application source code.
- **CON-AUTH-01:** Authentication is required for all application features.
- **CON-AUTH-02:** Role-based access control (`OFFICER` vs. `ADMIN`) governs functional authorization.
- **CON-TRANS-01:** Every state transition affecting multiple tables must execute within an atomic Database Transaction (`IsolationLevel.Serializable` for transfers).
- **CON-CONC-01:** All mutable tables include a `RowVersion` (`rowversion`) column to support optimistic concurrency conflict detection (`DbUpdateConcurrencyException`).

### 4.3. Development Environment & Tooling

| Component | Version / Choice |
|---|---|
| Language / Runtime | .NET 10 (C#) |
| SDK | .NET SDK `10.0.401` |
| IDE | Microsoft Visual Studio 2026 |
| UI Framework | Windows Forms (WinForms) |
| ORM | Entity Framework Core |
| Database | Microsoft SQL Server 2019+ |
| DB Hosting (local dev) | Docker container |
| Version Control | Git (workflow defined in `work-breakdown.md` §7) |

---

## 5. Domain Entities

Aligned with Database Schema v2 (`aris_schema_v2.sql`):

### 5.1. Citizen
- `CitizenId` (PK, int, IDENTITY)
- `NationalId` (char(12), Unique, 12 numeric digits)
- `FullName` (nvarchar(100))
- `DateOfBirth` (date)
- `Gender` (varchar(10): `MALE`, `FEMALE`, `OTHER`)
- `Status` (varchar(12): `UNASSIGNED`, `ACTIVE`, `INACTIVE`)
- `CreatedAt` (datetime2(3))
- `UpdatedAt` (datetime2(3))
- `DeactivatedAt` (datetime2(3), nullable)
- `RowVersion` (rowversion)

*(Note: `HouseholdId` and `CurrentAddressId` are never stored directly in Citizen).*

### 5.2. Household
- `HouseholdId` (PK, int, IDENTITY)
- `Status` (varchar(10): `ACTIVE`, `INACTIVE`)
- `CreatedAt` (datetime2(3))
- `InactivatedAt` (datetime2(3), nullable)
- `RowVersion` (rowversion)

*(Note: The household's address is stored exclusively in `HouseholdAddress`, queried for current state via view `vw_HouseholdCurrent`).*

### 5.3. HouseholdAddress
Tracks the address timeline of a household (append-only):
- `HouseholdAddressId` (PK, int, IDENTITY)
- `HouseholdId` (FK $\rightarrow$ Household)
- `AddressId` (FK $\rightarrow$ Address)
- `StartDate` (date)
- `EndDate` (date, nullable)
- `Status` (varchar(10): `ACTIVE`, `ENDED`, `VOID`)
- `CreatedBy` (FK $\rightarrow$ UserAccount)
- `CreatedAt` (datetime2(3))
- `EndedBy` (FK $\rightarrow$ UserAccount, nullable)
- `EndedAt` (datetime2(3), nullable)
- `OpenedByRequestId` (FK $\rightarrow$ TransferRequest, nullable)
- `CorrectsHouseholdAddressId` (FK $\rightarrow$ HouseholdAddress, nullable)
- `RowVersion` (rowversion)

*(Constraint: Filtered unique index `UX_HA_OneActivePerHousehold` guarantees at most one `ACTIVE` address per household).*

### 5.4. HouseholdMembership
Tracks who belongs to which household and their role (append-only):
- `MembershipId` (PK, int, IDENTITY)
- `CitizenId` (FK $\rightarrow$ Citizen)
- `HouseholdId` (FK $\rightarrow$ Household)
- `Role` (varchar(10): `HEAD`, `MEMBER`)
- `StartDate` (date)
- `EndDate` (date, nullable)
- `Status` (varchar(10): `ACTIVE`, `ENDED`, `VOID`)
- `CreatedBy` (FK $\rightarrow$ UserAccount)
- `CreatedAt` (datetime2(3))
- `EndedBy` (FK $\rightarrow$ UserAccount, nullable)
- `EndedAt` (datetime2(3), nullable)
- `OpenedByRequestId` (FK $\rightarrow$ TransferRequest, nullable)
- `CorrectsMembershipId` (FK $\rightarrow$ HouseholdMembership, nullable)
- `RowVersion` (rowversion)

*(Constraints: `UX_HM_OneActivePerCitizen` ensures at most one active membership per citizen; `UX_HM_OneActiveHeadPerHousehold` ensures at most one active head per household).*

### 5.5. Address
Normalized administrative address with lineage tracking:
- `AddressId` (PK, int, IDENTITY)
- `AddressCode` (varchar(30), Unique)
- `AddressLine` (nvarchar(200))
- `Ward` (nvarchar(100))
- `District` (nvarchar(100), nullable, legacy addresses only)
- `Province` (nvarchar(100))
- `AddressText` (Computed: `CONCAT_WS(', ', AddressLine, Ward, District, Province)`)
- `Status` (varchar(10): `ACTIVE`, `INACTIVE`)
- `ValidFrom` (date)
- `ValidTo` (date, nullable)
- `ReplacedByAddressId` (FK $\rightarrow$ Address, self-referencing lineage)
- `CreatedAt` (datetime2(3))
- `DeactivatedAt` (datetime2(3), nullable)
- `RowVersion` (rowversion)

### 5.6. Residence
Tracks where a citizen lives over time (append-only):
- `ResidenceId` (PK, int, IDENTITY)
- `CitizenId` (FK $\rightarrow$ Citizen)
- `AddressId` (FK $\rightarrow$ Address)
- `StartDate` (date)
- `EndDate` (date, nullable)
- `Status` (varchar(10): `ACTIVE`, `ENDED`, `VOID`)
- `CreatedBy` (FK $\rightarrow$ UserAccount)
- `CreatedAt` (datetime2(3))
- `EndedBy` (FK $\rightarrow$ UserAccount, nullable)
- `EndedAt` (datetime2(3), nullable)
- `OpenedByRequestId` (FK $\rightarrow$ TransferRequest, nullable)
- `CorrectsResidenceId` (FK $\rightarrow$ Residence, nullable)
- `RowVersion` (rowversion)

*(Constraint: Filtered unique index `UX_Res_OneActivePerCitizen` guarantees at most one active residence per citizen).*

### 5.7. TransferRequest
Header for residency relocations governed by maker-checker verification:
- `RequestId` (PK, int, IDENTITY)
- `RequestType` (varchar(20): `JOIN_EXISTING`, `NEW_HOUSEHOLD`, `HOUSEHOLD_MOVE`)
- `SourceHouseholdId` (FK $\rightarrow$ Household)
- `DestinationHouseholdId` (FK $\rightarrow$ Household, nullable)
- `DestinationAddressId` (FK $\rightarrow$ Address, nullable)
- `EffectiveDate` (date)
- `NewHeadCitizenId` (FK $\rightarrow$ Citizen, nullable)
- `SuccessorHeadCitizenId` (FK $\rightarrow$ Citizen, nullable)
- `ResultHouseholdId` (FK $\rightarrow$ Household, nullable, populated upon approval)
- `Status` (varchar(10): `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED`)
- `RequestedBy` (FK $\rightarrow$ UserAccount)
- `RequestedAt` (datetime2(3))
- `DecidedBy` (FK $\rightarrow$ UserAccount, nullable)
- `DecidedAt` (datetime2(3), nullable)
- `DecisionNote` (nvarchar(500), nullable)
- `RowVersion` (rowversion)

### 5.8. TransferRequestItem
Line items representing each individual citizen in a transfer request:
- `ItemId` (PK, int, IDENTITY)
- `RequestId` (FK $\rightarrow$ TransferRequest)
- `CitizenId` (FK $\rightarrow$ Citizen)
- `SourceMembershipId` (FK $\rightarrow$ HouseholdMembership)
- `SourceResidenceId` (FK $\rightarrow$ Residence)
- `Status` (varchar(10): `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED`)
- `RejectReason` (nvarchar(300), nullable)
- `RowVersion` (rowversion)

*(Constraint: `UX_TRI_OneOpenPerCitizen` ensures a citizen can exist in at most one `PENDING` request item at any time).*

### 5.9. AuditLog
Append-only immutable audit trail:
- `AuditLogId` (PK, bigint, IDENTITY)
- `ActorUserId` (FK $\rightarrow$ UserAccount, nullable)
- `AttemptedUsername` (nvarchar(50), nullable, for failed logins)
- `Action` (varchar(50), e.g., `LOGIN_SUCCESS`, `CREATE_CITIZEN`, `TRANSFER_APPROVE`)
- `EntityType` (varchar(50))
- `EntityId` (int, nullable)
- `OccurredAt` (datetime2(3))
- `Result` (varchar(10): `SUCCESS`, `FAILURE`)
- `Details` (nvarchar(max), JSON payload: before, after, reason)
- `CorrelationId` (uniqueidentifier, nullable, groups multi-table operations)

### 5.10. UserAccount
System user credentials and operational security state:
- `UserId` (PK, int, IDENTITY)
- `Username` (nvarchar(50), Unique)
- `PasswordHash` (varchar(255))
- `FullName` (nvarchar(100))
- `Role` (varchar(10): `OFFICER`, `ADMIN`)
- `Status` (varchar(10): `ACTIVE`, `LOCKED`)
- `FailedLoginCount` (int)
- `LockedUntil` (datetime2(3), nullable)
- `MustChangePassword` (bit)
- `PasswordChangedAt` (datetime2(3), nullable)
- `CreatedAt` (datetime2(3))
- `RowVersion` (rowversion)

### 5.11. SystemConfiguration
Centralized key-value system settings:
- `ConfigKey` (PK, varchar(100))
- `ConfigValue` (nvarchar(500))
- `Description` (nvarchar(300), nullable)
- `UpdatedAt` (datetime2(3))
- `UpdatedBy` (FK $\rightarrow$ UserAccount, nullable)

---

## 6. Business Rules

### 6.1. Citizen Rules
- **BR-CIT-01 (Unique National ID):** Every citizen is uniquely identified by a 12-digit numeric National ID (`NationalId`).
- **BR-CIT-02 (Mandatory Profile):** Citizen registration requires complete demographic attributes: National ID, full name, date of birth, and gender.
- **BR-CIT-03 (Citizen Lifecycle States):**
  - `UNASSIGNED`: Initial state upon creation (`CIT-01`). The citizen is recorded but not yet assigned to any household.
  - `ACTIVE`: The citizen belongs to exactly one active household and one active residence (conforms to the Golden Invariant).
  - `INACTIVE`: Terminal state in the MVP when management is discontinued (`CIT-04`). Inactive citizens cannot receive new memberships or residences, but all historical records remain preserved.
- **BR-CIT-04 (Single Active Household Invariant):** Every `ACTIVE` citizen must belong to exactly one active household via an active `HouseholdMembership`.
- **BR-CIT-05 (Single-Member Households):** A household may consist of a single citizen who serves as the `HEAD`.

### 6.2. Household Rules
- **BR-HH-01 (Single Active Head):** An active household must have exactly **one** active head (`Role = 'HEAD'`) at all committed database states.
- **BR-HH-02 (Head Must Be Member):** The household head must be an active member of that specific household.
- **BR-HH-03 (No Duplicate Memberships):** A citizen can hold at most one active membership across the entire system (`UX_HM_OneActivePerCitizen`).
- **BR-HH-04 (Empty Household Deactivation):** When the last active member leaves a household, the household's status transitions to `INACTIVE` and its active `HouseholdAddress` is closed.
- **BR-HH-05 (Household Relocation Types):** Three relocation shapes are supported:
  - `JOIN_EXISTING`: One or more members join an existing active household.
  - `NEW_HOUSEHOLD`: One or more members split off into a brand-new household at a destination address with a designated `HEAD`.
  - `HOUSEHOLD_MOVE`: The entire household relocates to a new address while preserving `HouseholdId`.
- **BR-HH-06 (Head Reassignment Is Append-Only):** Reassigning the household head is executed by closing the former head's `HEAD` record and the successor's `MEMBER` record, and opening a new `MEMBER` record for the former head and a new `HEAD` record for the successor (both with `StartDate = EffectiveDate`). In-place updates to `Role` are strictly forbidden.
- **BR-HH-07 (Head Succession Requirement):** If the current head departs a household while other members remain, a successor head (`SuccessorHeadCitizenId`) must be explicitly designated prior to approval. Automatic random selection is prohibited.
- **BR-HH-08 (Current Household Address):** Every active household has exactly one active record in `HouseholdAddress` (`UX_HA_OneActivePerHousehold`), resolving to an active canonical address.

### 6.3. Residence Rules
- **BR-RES-01 (Single Active Residence):** A citizen can hold at most one active residence at any time (`UX_Res_OneActivePerCitizen`).
- **BR-RES-02 (Canonical Valid Address):** An active residence must point to an active canonical address (the terminal active address in any `ReplacedByAddressId` lineage chain verified via `vw_AddressCanonical`).
- **BR-RES-03 (Non-Overlapping Intervals):** Residence intervals (`StartDate` $\rightarrow$ `EndDate`) for the same citizen must never overlap. Enforced by trigger `trg_Residence_Guard` (ignoring `VOID` rows) and serializable transactions.
- **BR-RES-04 (Append-Only History Preservation):** Past residence records are never overwritten or deleted. Status transitions proceed only as `ACTIVE→ENDED` or `ACTIVE/ENDED→VOID`.
- **BR-RES-05 (Residency Continuity):** An active citizen's current residence is closed only as part of opening a new valid residence within the same atomic transaction.
- **BR-RES-06 (Address Lineage Preservation):** When administrative boundaries change, existing addresses are deactivated and point to new addresses via `ReplacedByAddressId`. Historical residence records remain linked to their original address IDs to reflect true historical facts.
- **BR-RES-07 (Effective Date Sequencing):** In any transfer or relocation, the effective date $E$ must be strictly greater than the `StartDate` of any record being closed ($E > \text{StartDate}$). The closed record terminates at $E - 1$, and the new record starts at $E$.

### 6.4. The Golden Invariant
To ensure absolute data integrity, the system enforces the **Residency-Household Consistency Invariant**:

$$\text{Every ACTIVE Citizen must possess an ACTIVE HouseholdMembership in Household } H,$$
$$\text{and an ACTIVE Residence at Address } A, \text{ where } A \text{ matches Household } H\text{'s ACTIVE HouseholdAddress.}$$

```text
ACTIVE Citizen ──(exactly 1)──> ACTIVE HouseholdMembership ──> Household ──> ACTIVE HouseholdAddress (A)
       │                                                                                   │
       └─────────(exactly 1)──> ACTIVE Residence (A) ──────────────────────────────────────┘
```

The database view `vw_GoldenInvariantViolations` provides continuous automated verification of this invariant across 9 specific checks (C1–C5, H1–H4) and must return **0 rows** after every committed operation.

### 6.5. Verification & Workflow Rules (Maker-Checker)
- **BR-VER-01 (Maker-Checker for Relocations):** Relocations (`TransferRequest`) require dual-control verification: Officer A creates the request (`PENDING`), and Officer B ($\ne$ A) approves or rejects it. Enforced at the database level via `CK_TR_Decision`. All other administrative operations (creating citizens, updating demographics, direct household registration) are executed by a single officer and fully audited.
- **BR-VER-02 (Separation of Request and Domain State):** While a `TransferRequest` is in `PENDING` status, the active memberships, residences, and households of all candidate citizens remain completely unchanged.
- **BR-VER-03 (Traceable Authorization):** Every executed transfer records `OpenedByRequestId`, linking all resulting historical records directly to the approved request.
- **BR-VER-04 (Individual Item Resolution):** Each candidate citizen within a multi-person transfer request has an independent item status (`TransferRequestItem.Status`). A request may be approved with partial member rejections (A4).

### 6.6. History & Audit Rules
- **BR-HIST-01 (Append-Only Immutability):** Records in `HouseholdMembership`, `Residence`, and `HouseholdAddress` are append-only. Identity fields (`CitizenId`, `HouseholdId`, `AddressId`, `Role`, `StartDate`) are immutable. Rows transition at most once from `ACTIVE→ENDED` or to `VOID`.
- **BR-HIST-02 (Void and Rectification):** If a historical record was created in error, it must not be updated in place. The officer marks the faulty row as `VOID`, and inserts a corrected row referencing `Corrects...Id`. Overlap checks bypass `VOID` records.
- **BR-AUD-01 (Comprehensive Audit Trail):** Every business mutation (create, update, transfer, void, login attempt) generates an `AuditLog` entry detailing Who, What, When, Entity, Result, and before/after JSON snapshots.
- **BR-AUD-02 (Audit Log Immutability):** The audit log is strictly append-only. Triggers and database role permissions (`DENY UPDATE`, `DENY DELETE` on `AuditLog`) prevent tampering.
- **BR-AUD-03 (Autonomous Failure Auditing):** If a business transaction fails and rolls back, a `FAILURE` audit record must be written immediately using an independent database connection. Audit data is retained permanently (archived, never deleted).

### 6.7. Concurrency & Transactional Integrity
- **BR-CON-01 (Atomic Multi-Table Transitions):** Any state change affecting citizens, households, memberships, or residences must be wrapped in a single database transaction.
- **BR-CON-02 (Optimistic Concurrency Control):** All mutable tables include a `RowVersion` (`rowversion`) column. Application updates check concurrency versions to prevent lost updates (`DbUpdateConcurrencyException`).
- **BR-CON-03 (Serializable Execution for Relocations):** Transfer executions must run at `IsolationLevel.Serializable` to guarantee non-overlapping temporal constraints and eliminate race conditions between concurrent requests.

---

## 7. Use Case Catalog

```text
AUTHENTICATION & SYSTEM ADMINISTRATION
├── UC-01: User Login (AUTH-01)
├── UC-10: Manage Accounts (ACC-01)
├── UC-11: Manage Roles & Permissions (PERM-01)
├── UC-12: View Audit Logs (AUD-01)
└── UC-13: System Configuration (SYS-01)

CITIZEN & HOUSEHOLD MANAGEMENT
├── UC-02: Manage Citizen (CIT-01, CIT-02, CIT-03, CIT-04)
├── UC-03: Manage Address (ADR-01, ADR-02, ADR-03)
├── UC-04: Register Household (HH-01)
├── UC-05: Manage Household Members (HH-02, HH-04)
├── UC-06: Transfer Household & Residence (HH-05 / UC-HH-05)
├── UC-07: View Residence History (RES-03, HH-06)
└── UC-08: Void and Rectify Record (HIST-01)

SEARCH & REPORTING
├── UC-09: Search Directory (SRCH-01, SRCH-02, SRCH-03)
└── UC-14: View Statistical Reports (RPT-01, RPT-02, RPT-03)
```

---

## 8. Functional Requirements Specification

### 8.1. Authentication & System Administration
- **AUTH-01 (User Login):** Officers authenticate via Username and Password. The system validates credentials, enforces account lockout (`FailedLoginCount` $\ge$ threshold locks account for $N$ minutes), checks `MustChangePassword`, records audit logs, and initializes the role session.
- **ACC-01 (Manage Accounts):** Admins can create user accounts, lock/unlock accounts, toggle active status, and reset passwords with mandatory change on next login.
- **PERM-01 (Role Authorization):** Enforce role-based access (`OFFICER` vs. `ADMIN`) across UI screens and service operations.
- **AUD-01 (View Audit Logs):** Filter and inspect audit logs by date range, actor, entity type, entity ID, action, and success/failure outcome.
- **SYS-01 (System Configuration):** Admins configure global parameters stored in `SystemConfiguration` (`PASSWORD_MIN_LENGTH`, `SESSION_TIMEOUT_MINUTES`, `MAX_FAILED_LOGINS`, `LOCKOUT_MINUTES`).

### 8.2. Citizen Management
- **CIT-01 (Create Citizen):** Register a new citizen record with mandatory attributes (`NationalId`, `FullName`, `DateOfBirth`, `Gender`). The citizen is initialized with `Status = 'UNASSIGNED'`.
- **CIT-02 (View Citizen Profile):** Display comprehensive citizen details: demographic data, current household, current residence address, active membership role, and complete chronological residency history.
- **CIT-03 (Update Citizen Demographics):** Update non-identifying demographic data (e.g., spelling correction). Changes are directly updated on the `Citizen` entity, updating `UpdatedAt`, and logging full `before`/`after` details in `AuditLog`.
- **CIT-04 (Deactivate Citizen):** Transition an active citizen to `INACTIVE` when they are no longer managed. 
  - If the citizen is the `HEAD` of a multi-member household, the officer must designate a successor head from the remaining active members before proceeding.
  - Closes active `HouseholdMembership` and `Residence` records (`Status = 'ENDED'`, `EndDate = EffectiveDate - 1`).
  - If the citizen was the sole member, the household transitions to `INACTIVE` and its active `HouseholdAddress` is closed.

### 8.3. Address Management
- **ADR-01 (Create Address):** Register a standardized administrative address (`AddressLine`, `Ward`, `District`, `Province`). System computes canonical `AddressText`.
- **ADR-02 (Search & View Address):** Query addresses by administrative levels or street name; display active households and citizens currently residing at the address.
- **ADR-03 (Update Address & Lineage):** When administrative boundaries are reorganized, the old address is updated to `Status = 'INACTIVE'` with `ReplacedByAddressId` pointing to the newly created replacement address. Existing historical residence and household address records remain intact.

### 8.4. Household Management
- **HH-01 (Register Household):** Create a new household in a single atomic transaction:
  1. Create `Household(ACTIVE)` and `HouseholdAddress(ACTIVE, StartDate = E)`.
  2. Select an `UNASSIGNED` citizen to serve as the initial `HEAD`.
  3. Create `HouseholdMembership(Role = 'HEAD', Status = 'ACTIVE', StartDate = E)`.
  4. Create `Residence(Status = 'ACTIVE', StartDate = E)` pointing to the household address.
  5. Update `Citizen.Status = 'ACTIVE'`.
- **HH-02 (Add Household Member):** Add an existing `UNASSIGNED` citizen to an active household:
  1. Verify the citizen is `UNASSIGNED` and target household is `ACTIVE`.
  2. Create `HouseholdMembership(Role = 'MEMBER', Status = 'ACTIVE', StartDate = E)`.
  3. Create `Residence(Status = 'ACTIVE', StartDate = E)` pointing to the household's current active address.
  4. Update `Citizen.Status = 'ACTIVE'`.
- **HH-03 (Remove Household Member):** *Note: Member departures are executed strictly through Transfer (HH-05) or Deactivation (CIT-04) to prevent orphaned active citizens.*
- **HH-04 (Change Household Head):** Reassign the head role within an active household:
  1. Close the current head's `HouseholdMembership` (`Role = 'HEAD'`, `EndDate = E - 1`, `Status = 'ENDED'`).
  2. Close the successor's `HouseholdMembership` (`Role = 'MEMBER'`, `EndDate = E - 1`, `Status = 'ENDED'`).
  3. Open a new `HouseholdMembership` for the former head (`Role = 'MEMBER'`, `StartDate = E`, `Status = 'ACTIVE'`).
  4. Open a new `HouseholdMembership` for the successor (`Role = 'HEAD'`, `StartDate = E`, `Status = 'ACTIVE'`).
- **HH-05 (Transfer Household / Members):** Multi-person relocation workflow (detailed in Section 9).
- **HH-06 (View Household History):** Inspect historical timelines of household address changes (`HouseholdAddress`) and member entries/departures (`HouseholdMembership`).

### 8.5. Residence Management & Historical Corrections
- **RES-01 (Register Residence) & RES-02 (End Residence):** *Note: These operations are internal atomic steps of composite transactions (HH-01, HH-02, HH-05, CIT-04) and cannot be invoked as standalone rogue actions, ensuring the Golden Invariant cannot be violated.*
- **RES-03 (View Current Residence):** View the current validated residence address of a citizen.
- **HIST-01 (Void and Rectify Record):** Correct data entry mistakes in historical records (`HouseholdMembership`, `Residence`, `HouseholdAddress`):
  1. The officer selects the erroneous record and supplies a mandatory correction reason.
  2. In an atomic transaction, the erroneous record is updated to `Status = 'VOID'`.
  3. A new corrected record is inserted with `Corrects...Id` referencing the voided record.
  4. The action is audited with full rationale.

### 8.6. Search & Reporting
- **SRCH-01 (Search Citizens):** Search by National ID, full name, or status (`UNASSIGNED`, `ACTIVE`, `INACTIVE`).
- **SRCH-02 (Search Households):** Search by Household ID, head citizen name, head National ID, or address text.
- **SRCH-03 (Search Addresses):** Search by street name, ward, district, or province.
- **RPT-01 (Demographic Distribution Report):** Aggregate active population counts grouped by province, district, and ward.
- **RPT-02 (Household Summary Report):** Summarize total active/inactive households, average household size, and single-member household counts.
- **RPT-03 (Residency Movement Report):** Track residency movements over a date range (new registrations, household transfers, deactivations).

---

## 9. Core Use Case Specification: UC-06 — Transfer Household & Residence (HH-05)

| Attribute | Specification |
| :--- | :--- |
| **Use Case ID** | `UC-06` (also designated `HH-05` / `UC-HH-05`) |
| **Use Case Name** | Transfer Household & Residence |
| **Primary Actor** | Residence Management Officer (Officer) |
| **Priority** | High / Core (Must Have) |
| **Involved Entities** | `Citizen`, `Household`, `HouseholdAddress`, `HouseholdMembership`, `Residence`, `Address`, `TransferRequest`, `TransferRequestItem`, `AuditLog` |
| **Transaction Scope** | Mandatory single atomic transaction (`IsolationLevel.Serializable` during execution) |

### 9.1. Purpose
Enables officers to process residency relocations—joining an existing household, splitting off to establish a new household, or moving an entire household to a new address—under dual-control maker-checker verification while maintaining complete historical integrity and satisfying the Golden Invariant.

### 9.2. Preconditions
1. The initiating officer (Officer A) is authenticated and authorized.
2. Every candidate citizen is in `ACTIVE` status, possesses an active membership and residence, and belongs to the specified source household.
3. No candidate citizen is currently involved in another open `PENDING` request item (`UX_TRI_OneOpenPerCitizen`).
4. For `JOIN_EXISTING`: Destination household is `ACTIVE` with a valid active address.
5. For `NEW_HOUSEHOLD`: Destination address is `ACTIVE`, and a valid new household head is designated from among the departing members.
6. For `HOUSEHOLD_MOVE`: All active members of the household are included in the transfer.
7. The effective date $E$ must be strictly greater than the `StartDate` of any record being closed ($E > \text{StartDate}$).

### 9.3. Workflow: Two-Phase Maker-Checker

```text
PHASE 1: REQUEST CREATION (Maker - Officer A)
  ├── 1. Officer A selects Source Household and candidate members.
  ├── 2. Selects Request Type: JOIN_EXISTING | NEW_HOUSEHOLD | HOUSEHOLD_MOVE.
  ├── 3. Enters Destination Details, Effective Date (E), and Head designation (if applicable).
  ├── 4. System validates preconditions and inserts TransferRequest(PENDING) + TransferRequestItem(PENDING).
  └── 5. AuditLog records request creation.

PHASE 2: REVIEW & EXECUTION (Checker - Officer B ≠ Officer A)
  ├── 1. Officer B loads the PENDING request and verifies physical documentation.
  ├── 2. Re-validates all candidate citizens and target destinations against current committed DB state.
  ├── 3. Sets individual decision for each item (APPROVED or REJECTED with reason).
  └── 4. If at least one item is APPROVED, Officer B executes the transfer:
              │
              ▼
       SYSTEM OPENS SERIALIZABLE TRANSACTION:
         ├── a. For each approved item, close existing Residence (EndDate = E - 1, Status = ENDED).
         ├── b. For JOIN_EXISTING and NEW_HOUSEHOLD, close existing HouseholdMembership (EndDate = E - 1, Status = ENDED).
         ├── c. If source head departed and members remain, promote SuccessorHeadCitizenId (close MEMBER, open HEAD at E).
         ├── d. If source household has no remaining members, mark Household INACTIVE and close its HouseholdAddress.
         ├── e. Open new records (OpenedByRequestId = RequestId):
         │       • JOIN_EXISTING: open Membership(MEMBER) + Residence at destination household's address.
         │       • NEW_HOUSEHOLD: create Household, HouseholdAddress, Memberships (HEAD + MEMBERs), and Residences.
         │       • HOUSEHOLD_MOVE: close old HouseholdAddress, open new HouseholdAddress; open new Residences for all members.
         ├── f. Update TransferRequest status to APPROVED (or REJECTED), set DecidedBy = Officer B, DecidedAt = UtcNow.
         └── g. Write SUCCESS AuditLog sharing CorrelationId.
              │
              ▼
       TRANSACTION OUTCOME:
         ├── [SUCCESS] ──> COMMIT TRANSACTION ──> Notify Officer B of success.
         └── [FAILURE] ──> ROLLBACK TRANSACTION ──> Open separate connection ──> Write FAILURE AuditLog.
```

### 9.4. Alternative Flows & Edge Cases
- **A1 (Entire Household Moves - HOUSEHOLD_MOVE):** The entire family moves to a new location. `HouseholdId` is preserved; existing memberships remain untouched; old `HouseholdAddress` is ended and a new one opened; new `Residence` records are opened for all members starting at date $E$.
- **A2 (Multiple Members Split Off - NEW_HOUSEHOLD):** Multiple members depart a household to form a new household. The source household remains `ACTIVE` with remaining members; the new household is created with the designated `NewHeadCitizenId` as `HEAD`.
- **A3 (Departing Head with Remaining Members):** When the current head departs but other members remain in the source household, the officer must specify `SuccessorHeadCitizenId`. The system automatically transitions the successor from `MEMBER` to `HEAD` effective on date $E$.
- **A4 (Partial Item Approval):** In a multi-person request, if some members fail eligibility verification, Officer B marks those specific items as `REJECTED` with notes. The approved members proceed with relocation; rejected members remain unchanged in the source household.
- **A5 (Destination Address Not in Directory):** If the destination address does not exist, the officer creates the address (`ADR-01`) before finalizing the transfer request.

### 9.5. Exception Handling
- **E1 (Citizen or Target Household Inactive):** Precondition validation fails; the system rejects request submission or execution.
- **E2 (Approver is Requester):** Enforced by database constraint `CK_TR_Decision`: `DecidedBy` must not equal `RequestedBy`. The system prevents self-approval.
- **E3 (Concurrency Conflict):** If another officer modified any involved citizen, household, or address during review, an optimistic concurrency conflict (`DbUpdateConcurrencyException`) is raised. The transaction is aborted and the officer is prompted to reload fresh data.
- **E4 (Database Failure or Constraint Violation):** Any unexpected database error triggers an immediate `ROLLBACK`. The system catches the exception and logs a `FAILURE` entry to `AuditLog` over an independent connection.

### 9.6. Postconditions
- All approved citizens belong to exactly one active household and reside at that household's active canonical address (satisfying the Golden Invariant).
- Old historical records are properly ended (`EndDate = E - 1`), and new records are opened (`StartDate = E`).
- Complete traceability is maintained via `OpenedByRequestId` and immutable audit logs.
- Automated query `SELECT * FROM dbo.vw_GoldenInvariantViolations;` returns 0 rows.

---

## 10. Acceptance Checklist (Definition of Done)

Before approving application features and integration test suites, verify that:

- [ ] **Decoupled Relational Model:** Entities `Citizen`, `Household`, `HouseholdAddress`, `HouseholdMembership`, `Address`, and `Residence` are separated. No static `Citizen.HouseholdId` or `Household.CurrentAddressId` exists.
- [ ] **Golden Invariant Preserved:** Querying `vw_GoldenInvariantViolations` produces zero rows after every committed transaction across all test scenarios.
- [ ] **Append-Only History:** Historical tables permit only `ACTIVE→ENDED` or transitions to `VOID`. In-place updates to identity attributes or temporal boundaries are blocked by triggers.
- [ ] **Dual-Control Verification:** Transfer executions require dual control (`DecidedBy <> RequestedBy`), enforced by constraint `CK_TR_Decision`.
- [ ] **Optimistic Concurrency:** All entity updates check `RowVersion` to eliminate lost updates.
- [ ] **Immutable Auditing:** The `AuditLog` table denies `UPDATE` and `DELETE` operations. Failed operations log an audit record on an independent connection post-rollback.