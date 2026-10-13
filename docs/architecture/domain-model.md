# ARIS Domain Model & Entity Reference

**Document Code:** `DOM-ARIS-v2.0`  
**Single Source of Truth:** Schema v2 ([`../aris_schema_v2.sql`](../aris_schema_v2.sql)) · [`golden-invariant.md`](golden-invariant.md)  
**Visual Diagrams:** [`../erd_v2.svg`](../erd_v2.svg) · [`../erd_v2.png`](../erd_v2.png)

---

## 1. Core Mental Model & Decoupling Rationale

The ARIS domain model strictly decouples **6 core concepts** to eliminate static foreign key anomalies and historical data loss:

```text
                  ┌───────────┐
                  │  Citizen  │  (Who is this individual?)
                  └─────┬─────┘
                        │
             ┌──────────┴──────────┐
             │                     │
             ▼                     ▼
   ┌──────────────────┐   ┌────────────────┐
   │ HouseholdMember- │   │   Residence    │  (Where does this person live,
   │      ship        │   │                │   over what time period?)
   └────────┬─────────┘   └───────┬────────┘
            │                     │
            ▼                     │
      ┌───────────┐               │
      │ Household │ (Which group?)│
      └─────┬─────┘               │
            │                     │
            ▼                     ▼
   ┌──────────────────┐   ┌────────────────┐
   │ HouseholdAddress │──►│    Address     │  (Normalized canonical location)
   └──────────────────┘   └────────────────┘
```

### Why Static Keys are Prohibited
1. **No `Citizen.HouseholdId`:** An individual belongs to different households across distinct time intervals (e.g. growing up in family household, moving out, marrying). Static keys overwrite tenure history.
2. **No `Citizen.CurrentAddressId`:** Residency is temporal ($StartDate \rightarrow EndDate$).
3. **No `Household.CurrentAddressId`:** When a family moves, overwriting the household address column destroys the chronological record of where the household resided previously. Household addresses are tracked exclusively in `HouseholdAddress`.

---

## 2. Domain Entity Specifications

### 2.1. Citizen
Represents an individual managed within the administrative boundary.
- **`CitizenId`** (`int`, PK, IDENTITY)
- **`NationalId`** (`char(12)`, Unique, CHECK: 12 numeric digits)
- **`FullName`** (`nvarchar(100)`, Indexed)
- **`DateOfBirth`** (`date`)
- **`Gender`** (`varchar(10)`, CHECK: `MALE`, `FEMALE`, `OTHER`)
- **`Status`** (`varchar(12)`, CHECK: `UNASSIGNED`, `ACTIVE`, `INACTIVE`)
- **`CreatedAt`** (`datetime2(3)`)
- **`UpdatedAt`** (`datetime2(3)`)
- **`DeactivatedAt`** (`datetime2(3)`, nullable)
- **`RowVersion`** (`rowversion`)

### 2.2. Household
Represents a legal administrative family unit.
- **`HouseholdId`** (`int`, PK, IDENTITY)
- **`Status`** (`varchar(10)`, CHECK: `ACTIVE`, `INACTIVE`)
- **`CreatedAt`** (`datetime2(3)`)
- **`InactivatedAt`** (`datetime2(3)`, nullable)
- **`RowVersion`** (`rowversion`)

### 2.3. HouseholdAddress
Append-only historical timeline of addresses occupied by a household.
- **`HouseholdAddressId`** (`int`, PK, IDENTITY)
- **`HouseholdId`** (`int`, FK $\rightarrow$ Household)
- **`AddressId`** (`int`, FK $\rightarrow$ Address)
- **`StartDate`** (`date`)
- **`EndDate`** (`date`, nullable)
- **`Status`** (`varchar(10)`, CHECK: `ACTIVE`, `ENDED`, `VOID`)
- **`CreatedBy`** (`int`, FK $\rightarrow$ UserAccount)
- **`CreatedAt`** (`datetime2(3)`)
- **`EndedBy`** (`int`, FK $\rightarrow$ UserAccount, nullable)
- **`EndedAt`** (`datetime2(3)`, nullable)
- **`OpenedByRequestId`** (`int`, FK $\rightarrow$ TransferRequest, nullable)
- **`CorrectsHouseholdAddressId`** (`int`, FK $\rightarrow$ HouseholdAddress, nullable)
- **`RowVersion`** (`rowversion`)
- *Unique Constraint:* `UX_HA_OneActivePerHousehold` (filtered unique: at most 1 `ACTIVE` address per household).

### 2.4. HouseholdMembership
Append-only tenure relationship between a Citizen and a Household.
- **`MembershipId`** (`int`, PK, IDENTITY)
- **`CitizenId`** (`int`, FK $\rightarrow$ Citizen)
- **`HouseholdId`** (`int`, FK $\rightarrow$ Household)
- **`Role`** (`varchar(10)`, CHECK: `HEAD`, `MEMBER`)
- **`StartDate`** (`date`)
- **`EndDate`** (`date`, nullable)
- **`Status`** (`varchar(10)`, CHECK: `ACTIVE`, `ENDED`, `VOID`)
- **`CreatedBy`** (`int`, FK $\rightarrow$ UserAccount)
- **`CreatedAt`** (`datetime2(3)`)
- **`EndedBy`** (`int`, FK $\rightarrow$ UserAccount, nullable)
- **`EndedAt`** (`datetime2(3)`, nullable)
- **`OpenedByRequestId`** (`int`, FK $\rightarrow$ TransferRequest, nullable)
- **`CorrectsMembershipId`** (`int`, FK $\rightarrow$ HouseholdMembership, nullable)
- **`RowVersion`** (`rowversion`)
- *Constraints:* `UX_HM_OneActivePerCitizen` (at most 1 active membership system-wide), `UX_HM_OneActiveHeadPerHousehold` (at most 1 active head per household).

### 2.5. Address
Standardized geographic location with administrative lineage tracking.
- **`AddressId`** (`int`, PK, IDENTITY)
- **`AddressCode`** (`varchar(30)`, Unique)
- **`AddressLine`** (`nvarchar(200)`)
- **`Ward`** (`nvarchar(100)`)
- **`District`** (`nvarchar(100)`, nullable, for legacy records only)
- **`Province`** (`nvarchar(100)`)
- **`AddressText`** (`AS CONCAT_WS(N', ', AddressLine, Ward, District, Province)`, computed column)
- **`Status`** (`varchar(10)`, CHECK: `ACTIVE`, `INACTIVE`)
- **`ValidFrom`** (`date`)
- **`ValidTo`** (`date`, nullable)
- **`ReplacedByAddressId`** (`int`, FK $\rightarrow$ Address, self-referencing lineage)
- **`CreatedAt`** (`datetime2(3)`)
- **`DeactivatedAt`** (`datetime2(3)`, nullable)
- **`RowVersion`** (`rowversion`)

### 2.6. Residence
Append-only occupancy of a Citizen at an Address over time.
- **`ResidenceId`** (`int`, PK, IDENTITY)
- **`CitizenId`** (`int`, FK $\rightarrow$ Citizen)
- **`AddressId`** (`int`, FK $\rightarrow$ Address)
- **`StartDate`** (`date`)
- **`EndDate`** (`date`, nullable)
- **`Status`** (`varchar(10)`, CHECK: `ACTIVE`, `ENDED`, `VOID`)
- **`CreatedBy`** (`int`, FK $\rightarrow$ UserAccount)
- **`CreatedAt`** (`datetime2(3)`)
- **`EndedBy`** (`int`, FK $\rightarrow$ UserAccount, nullable)
- **`EndedAt`** (`datetime2(3)`, nullable)
- **`OpenedByRequestId`** (`int`, FK $\rightarrow$ TransferRequest, nullable)
- **`CorrectsResidenceId`** (`int`, FK $\rightarrow$ Residence, nullable)
- **`RowVersion`** (`rowversion`)
- *Unique Constraint:* `UX_Res_OneActivePerCitizen` (at most 1 active residence per citizen).

### 2.7. TransferRequest & TransferRequestItem
Header and line items for relocations governed by maker-checker verification.
- **`TransferRequest`:**
  - `RequestId` (`int`, PK, IDENTITY)
  - `RequestType` (`varchar(20)`: `JOIN_EXISTING`, `NEW_HOUSEHOLD`, `HOUSEHOLD_MOVE`)
  - `SourceHouseholdId` (`int`, FK $\rightarrow$ Household)
  - `DestinationHouseholdId` (`int`, FK $\rightarrow$ Household, nullable)
  - `DestinationAddressId` (`int`, FK $\rightarrow$ Address, nullable)
  - `EffectiveDate` (`date`)
  - `NewHeadCitizenId` (`int`, FK $\rightarrow$ Citizen, nullable)
  - `SuccessorHeadCitizenId` (`int`, FK $\rightarrow$ Citizen, nullable)
  - `ResultHouseholdId` (`int`, FK $\rightarrow$ Household, nullable)
  - `Status` (`varchar(10)`: `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED`)
  - `RequestedBy` (`int`, FK $\rightarrow$ UserAccount)
  - `RequestedAt` (`datetime2(3)`)
  - `DecidedBy` (`int`, FK $\rightarrow$ UserAccount, nullable)
  - `DecidedAt` (`datetime2(3)`, nullable)
  - `DecisionNote` (`nvarchar(500)`, nullable)
  - `RowVersion` (`rowversion`)
  - *Check Constraint:* `CK_TR_Decision` (`DecidedBy <> RequestedBy` when decided).
- **`TransferRequestItem`:**
  - `ItemId` (`int`, PK, IDENTITY)
  - `RequestId` (`int`, FK $\rightarrow$ TransferRequest)
  - `CitizenId` (`int`, FK $\rightarrow$ Citizen)
  - `SourceMembershipId` (`int`, FK $\rightarrow$ HouseholdMembership)
  - `SourceResidenceId` (`int`, FK $\rightarrow$ Residence)
  - `Status` (`varchar(10)`: `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED`)
  - `RejectReason` (`nvarchar(300)`, nullable)
  - `RowVersion` (`rowversion`)
  - *Unique Constraint:* `UX_TRI_OneOpenPerCitizen` (a citizen can belong to at most 1 `PENDING` item).

### 2.8. UserAccount, SystemConfiguration & AuditLog
- **`UserAccount`:** `UserId`, `Username`, `PasswordHash`, `FullName`, `Role` (`OFFICER`/`ADMIN`), `Status` (`ACTIVE`/`LOCKED`), `FailedLoginCount`, `LockedUntil`, `MustChangePassword`, `PasswordChangedAt`, `CreatedAt`, `RowVersion`.
- **`SystemConfiguration`:** `ConfigKey` (PK), `ConfigValue`, `Description`, `UpdatedAt`, `UpdatedBy` (FK).
- **`AuditLog`:** `AuditLogId` (`bigint`, PK), `ActorUserId` (FK), `AttemptedUsername`, `Action`, `EntityType`, `EntityId`, `OccurredAt`, `Result` (`SUCCESS`/`FAILURE`), `Details` (`nvarchar(max)`, valid JSON), `CorrelationId` (`uniqueidentifier`).

---

## 3. Entity State Transition Table

State arrows not listed in this table are **prohibited**:

| Entity | Allowed States | Permitted Transitions | Trigger / Constraint |
|---|---|---|---|
| **Citizen** | `UNASSIGNED`, `ACTIVE`, `INACTIVE` | `UNASSIGNED → ACTIVE`<br>`ACTIVE → INACTIVE`<br>`UNASSIGNED → INACTIVE` | `CK_Citizen_Lifecycle` |
| **Household** | `ACTIVE`, `INACTIVE` | `ACTIVE → INACTIVE` (when last member departs) | View `H4` invariant |
| **Membership / Residence / HouseholdAddress** | `ACTIVE`, `ENDED`, `VOID` | `ACTIVE → ENDED`<br>`ACTIVE → VOID`<br>`ENDED → VOID`<br>*(VOID is permanently frozen)* | `trg_*_Guard`<br>(Error 51001, 51011, 51021) |
| **TransferRequest** | `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED` | `PENDING → APPROVED / REJECTED / CANCELLED`<br>*(Decided requests are frozen)* | `trg_TransferRequest_Frozen`<br>(Error 51040) |
| **TransferRequestItem** | `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED` | Mirrors parent request resolution; can be rejected individually (A4) | `CK_TRI_Status` |
| **Address** | `ACTIVE`, `INACTIVE` | `ACTIVE → INACTIVE` (optionally populates `ReplacedByAddressId`) | `CK_Address_Lifecycle` |
| **UserAccount** | `ACTIVE`, `LOCKED` | Bidirectional | `CK_UserAccount_Status` |
