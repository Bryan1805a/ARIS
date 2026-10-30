# 6. Runtime View

> **arc42 §6** · ARIS 3.0 · [Documentation hub](README.md)

This section shows how the important use cases execute across the layers. All sequences
assume an authenticated session and a working local SQL Server.

Notation: `E` is the **effective date** supplied by the user. Any row being closed must
satisfy `E > StartDate`; the closed row gets `EndDate = E - 1`.

---

## 6.1 Login and lockout (`AUTH-01`)

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant F as LoginForm
    participant S as AuthenticationService
    participant DB as SQL Server

    U->>F: username + password
    F->>S: LoginAsync(username, password)
    S->>DB: read UserAccount by username
    alt unknown username
        S->>DB: AuditLog FAILURE (AttemptedUsername, no ActorUserId)
        S-->>F: Result.Failure("Invalid credentials")
    else account locked and LockedUntil > now
        S->>DB: AuditLog FAILURE (ActorUserId, reason = locked)
        S-->>F: Result.Failure("Account is locked")
    else password mismatch
        S->>DB: increment FailedLoginCount (and lock if threshold reached)
        S->>DB: AuditLog FAILURE
        S-->>F: Result.Failure("Invalid credentials")
    else password valid
        S->>DB: reset FailedLoginCount, clear LockedUntil
        alt MustChangePassword
            S-->>F: Result.Success(requiresPasswordChange = true)
        else
            S->>DB: AuditLog SUCCESS (LOGIN_SUCCESS)
            S-->>F: Result.Success(session)
        end
    end
```

Rules:

- Passwords are verified against a salted PBKDF2/BCrypt hash; neither the password nor
  the hash is ever written to the audit log.
- `MAX_FAILED_LOGINS`, `LOCKOUT_MINUTES`, `PASSWORD_MIN_LENGTH`, and
  `SESSION_TIMEOUT_MINUTES` are read from `SystemConfiguration`, not hardcoded.
- Failed logins are expected business outcomes (`Result.Failure`), not exceptions.
- A failure for an unknown username uses `AuditFailureEntry` with
  `AttemptedUsername`, because `CK_Audit_Actor` requires an actor or an attempted name.

---

## 6.2 `UC-06` — maker-checker transfer

This is the critical scenario. It is intentionally split into two independent
transactions, separated by human review.

### Phase 1 — Maker creates the request

```mermaid
sequenceDiagram
    autonumber
    actor M as Officer A (Maker)
    participant VM as TransferRequestViewModel
    participant T as TransferService
    participant V as Validator
    participant DB as SQL Server

    M->>VM: choose type, source household,<br/>moving citizens, destination, effective date
    VM->>T: CreateRequestAsync(dto, requestedBy = A)
    T->>V: validate shape and preconditions
    V-->>T: valid
    T->>DB: assert each citizen is ACTIVE in source household
    T->>DB: assert no PENDING TransferRequestItem for each citizen
    T->>DB: insert TransferRequest (PENDING)
    T->>DB: insert TransferRequestItem rows (PENDING)
    T->>DB: AuditLog SUCCESS (CREATE_TRANSFER_REQUEST)
    T-->>VM: Result.Success(requestId)
    VM-->>M: request queued for review
```

**No business state changes in Phase 1.** The household, memberships, residences, and
addresses are untouched while the request is `PENDING`.

Preconditions:

1. Requester is authenticated and authorized as `OFFICER`.
2. Every candidate is `ACTIVE` and holds an active membership and residence in the source
   household.
3. No candidate has another open `TransferRequestItem` (`UX_TRI_OneOpenPerCitizen`).
4. `E` is strictly greater than the `StartDate` of every row that will be closed.
5. Shape matches `CK_TR_Shape`:
   - `JOIN_EXISTING`: destination household required, different from source; no destination address, no new head.
   - `NEW_HOUSEHOLD`: destination address and `NewHeadCitizenId` required; no destination household.
   - `HOUSEHOLD_MOVE`: destination address required; no destination household, no new head, no successor head; the request must contain **all** active members.
6. If the source head is leaving while other members remain, `SuccessorHeadCitizenId` is required.

### Phase 2 — Checker decides

```mermaid
sequenceDiagram
    autonumber
    actor C as Officer B (Checker)
    participant VM as TransferInboxViewModel
    participant T as TransferService
    participant DB as SQL Server

    C->>VM: open PENDING request, decide per item
    VM->>T: ExecuteDecisionAsync(decision, decidedBy = B)
    T->>T: assert B ≠ A (backstop: CK_TR_Decision)
    T->>DB: BEGIN TRANSACTION (IsolationLevel.Serializable)
    T->>DB: re-read request, items, source household state
    T->>DB: re-verify approved citizens are still ACTIVE in source
    T->>DB: close approved old Residences (EndDate = E-1, ENDED)
    T->>DB: close approved old Memberships (EndDate = E-1, ENDED)
    alt source head leaves and members remain
        T->>DB: close old HEAD row, promote SuccessorHeadCitizenId (StartDate = E)
    else source household becomes empty
        T->>DB: set Household INACTIVE (InactivatedAt)
        T->>DB: close its active HouseholdAddress
    end
    alt JOIN_EXISTING
        T->>DB: open MEMBER membership + Residence at destination household address
    else NEW_HOUSEHOLD
        T->>DB: create Household + HouseholdAddress
        T->>DB: open HEAD for NewHeadCitizenId and MEMBER for others
        T->>DB: open Residences at destination address
    else HOUSEHOLD_MOVE
        T->>DB: close source HouseholdAddress, open destination HouseholdAddress
        T->>DB: open Residences at destination address (same HouseholdId)
    end
    T->>DB: set item statuses, request APPROVED / REJECTED
    T->>DB: AuditLog SUCCESS (EXECUTE_TRANSFER, shared CorrelationId)
    T->>DB: COMMIT
    T-->>VM: Result.Success()
    VM-->>C: transfer applied
```

Rules and branches:

- **Partial approval.** Rejected items remain in the source household; their status becomes
  `REJECTED` with a non-null `RejectReason` (`CK_TRI_Reject`). Approved items proceed.
- **Rejected request.** If no item is approved, the request is `REJECTED`; no tenures move.
- **Entire household moves.** `HOUSEHOLD_MOVE` keeps the `HouseholdId`; only
  `HouseholdAddress` and member `Residence` rows change.
- **New household.** `NEW_HOUSEHOLD` creates a new `Household`, assigns
  `NewHeadCitizenId` as `HEAD`, and opens the destination address for the household.
- **Join existing.** `JOIN_EXISTING` adds members to an existing active household and
  reuses that household's active address.
- **Cancellation.** A maker may cancel a pending request. `CK_TR_Decision` requires
  `DecidedBy` and `DecidedAt` for `CANCELLED`; record who cancelled.
- **Frozen after decision.** `trg_TransferRequest_Frozen` rejects any later update to a
  request whose prior status was not `PENDING` (error `51040`).

### Why `Serializable`

The overlap triggers inspect committed rows. Two checkers approving overlapping moves at
the same time could each see a valid state and commit a combined overlap. `Serializable`
makes the two executions mutually exclusive. Because every mutation owns an explicit
transaction, EF's retrying execution strategy is **not** enabled on the application
context; `SqlResilience` still retries the independent failure-audit connection.
Constraint and trigger violations are **never** retried.

---

## 6.3 Register household (`HH-01`)

```mermaid
sequenceDiagram
    autonumber
    actor O as Officer
    participant S as HouseholdService
    participant DB as SQL Server

    O->>S: RegisterHouseholdAsync(headCitizenId, addressId, E)
    S->>S: validate: address ACTIVE, citizen UNASSIGNED, E valid
    S->>DB: BEGIN TRANSACTION
    S->>DB: insert Household (ACTIVE)
    S->>DB: insert HouseholdAddress (ACTIVE, StartDate = E)
    S->>DB: insert HouseholdMembership (HEAD, ACTIVE, StartDate = E)
    S->>DB: insert Residence (ACTIVE, StartDate = E, AddressId)
    S->>DB: update Citizen SET Status = ACTIVE
    S->>DB: AuditLog SUCCESS (REGISTER_HOUSEHOLD)
    S->>DB: COMMIT
    S-->>O: Result.Success(householdId)
```

All five writes are one transaction. A partial write would violate `C1`, `C2`, `H1`,
`H2`, or `H3`. The new citizen is `UNASSIGNED` until this operation succeeds.

---

## 6.4 Add a member (`HH-02`)

1. Validate the citizen is `UNASSIGNED` and the household is `ACTIVE` with an active
   address.
2. Insert `HouseholdMembership(MEMBER, ACTIVE, StartDate = E)`.
3. Insert `Residence(ACTIVE, StartDate = E)` at the household's active address.
4. Set `Citizen.Status = 'ACTIVE'`.
5. Audit and commit in the same transaction.

`UX_HM_OneActivePerCitizen` and `UX_Res_OneActivePerCitizen` reject a citizen who already
has an active tenure. Removal is not a standalone operation: it happens through a
transfer or deactivation.

---

## 6.5 Change the head (`HH-04`)

This is the classic invariant trap. It is four writes, and the order matters:

```mermaid
flowchart LR
    A["Close old HEAD<br/>EndDate = E-1, ENDED"] --> B["Close successor MEMBER<br/>EndDate = E-1, ENDED"]
    B --> C["Open former head as MEMBER<br/>StartDate = E, ACTIVE"]
    C --> D["Open successor as HEAD<br/>StartDate = E, ACTIVE"]
    D --> E["Audit + COMMIT"]
```

- All four rows are in one transaction.
- Closes must be persisted **before** the new `HEAD` insert. Otherwise
  `UX_HM_OneActiveHeadPerHousehold` sees two active heads and rejects the batch.
- The `Role` column is never updated in place; doing so raises trigger error `51011`.
- The successor must be an active `MEMBER` of the same household.

---

## 6.6 Deactivate a citizen (`CIT-04`)

```mermaid
flowchart TD
    Start["DeactivateCitizenAsync(citizenId, E)"] --> Check{"Citizen ACTIVE?"}
    Check -->|no| Fail["Result.Failure"]
    Check -->|yes| Role{"Is citizen HEAD?"}
    Role -->|no| Close["Close active Membership + Residence<br/>EndDate = E-1, ENDED"]
    Role -->|yes| Others{"Other active members?"}
    Others -->|yes| Successor["Require and apply successor head<br/>(close + open, HH-04 pattern)"]
    Others -->|no| Empty["Set Household INACTIVE<br/>close active HouseholdAddress"]
    Successor --> Close
    Empty --> Close
    Close --> Status["Set Citizen INACTIVE + DeactivatedAt"]
    Status --> Audit["Audit SUCCESS"]
    Audit --> Commit["COMMIT"]
```

If the citizen is the sole member, the household becomes `INACTIVE` and its active
`HouseholdAddress` is closed. Leaving an empty household `ACTIVE`, or leaving an
`INACTIVE` citizen with an active tenure, violates `H2`/`H4` or `C4`.

---

## 6.7 Address lineage resolution (`ADR-03`, `C5`, `H3`)

When an administrative boundary changes, a new address is created and the old one is
marked `INACTIVE` with `ReplacedByAddressId` pointing to the successor. Historical
residences keep pointing at the old ID.

```mermaid
flowchart LR
    Old["Address #12<br/>INACTIVE<br/>ReplacedByAddressId = 40"]
    New["Address #40<br/>ACTIVE<br/>ReplacedByAddressId = NULL"]
    Old -->|"lineage"| New
    Res["Residence #900<br/>AddressId = 12<br/>ACTIVE"] --> Old
    View["vw_AddressCanonical<br/>12 → 40 (ACTIVE)"]
    Old --> View
```

`vw_AddressCanonical` follows the chain up to 20 levels and returns the terminal address.
The Golden Invariant compares the **canonical** address of the residence with the
**canonical** address of the household, so a valid historical ID does not break `C3`.
A broken or inactive terminal address raises `C5` / `H3`.

---

## 6.8 Failure path — rollback and independent audit

Every mutating service follows the same failure choreography.

```mermaid
sequenceDiagram
    autonumber
    participant S as Service
    participant DB as SQL Server (business transaction)
    participant A as AuditService
    participant DB2 as SQL Server (independent connection)

    S->>DB: BEGIN + mutations + success audit
    DB-->>S: exception (constraint, trigger, concurrency, network)
    S->>DB: ROLLBACK
    S->>A: LogFailureIndependentAsync(connectionString, failure)
    A->>DB2: INSERT AuditLog (FAILURE)
    DB2-->>A: committed
    S-->>S: return Result.Failure(friendly message)
```

- The failure audit must **not** use the rolled-back transaction; it would disappear with
  it.
- `LogFailureIndependentAsync` must not throw for business reasons. Losing a failure audit
  must not replace the original error the user needs to see.
- Trigger errors (`51001`–`51040`) and constraint violations are not transient and are
  not retried. `SqlResilience` retries only known transient connection errors.

---

## 6.9 Optimistic concurrency conflict (`CON-CONC-01`)

1. Officer A opens a citizen record; EF tracks its `RowVersion`.
2. Officer B updates the same row and commits.
3. Officer A saves; SQL Server finds no row with the original `RowVersion`.
4. EF raises `DbUpdateConcurrencyException`.
5. The service rolls back, logs the failure independently, and returns a
   `Result.Failure` asking the user to reload.
6. The UI reloads the record and lets the officer reapply the change.

No automatic merge is attempted. Silently overwriting a colleague's edit is worse than
asking the user to retry.

---

## 6.10 Scenario-to-component map

| Scenario | UI | Application | Infrastructure | Database |
|---|---|---|---|---|
| Login | `LoginForm` | `AuthenticationService` | `ArisDbContext`, `AuditService` | `UserAccount`, `SystemConfiguration`, `AuditLog` |
| Register household | `HouseholdEditForm` | `HouseholdService` | `ArisDbContext`, `AuditService` | `Household`, `HouseholdAddress`, `HouseholdMembership`, `Residence`, `Citizen` |
| Change head | `HouseholdDetailForm` | `HouseholdService` | `ArisDbContext`, `AuditService` | `HouseholdMembership` (close + open) |
| Create transfer | `TransferRequestForm` | `TransferService` | `ArisDbContext`, `AuditService` | `TransferRequest`, `TransferRequestItem`, `AuditLog` |
| Execute transfer | `TransferInboxForm` | `TransferService` | `ArisDbContext`, `AuditService` | nine tables in one serializable transaction |
| Deactivate citizen | `CitizenProfileForm` | `CitizenService` | `ArisDbContext`, `AuditService` | `Citizen`, `HouseholdMembership`, `Residence`, `Household`, `HouseholdAddress` |
| Address lineage | `AddressForm` | `AddressService` | `ArisDbContext` | `Address`, `vw_AddressCanonical` |
| Invariant verification | test runner | — | ADO.NET | `vw_GoldenInvariantViolations` |
