# 9. Architecture Decisions

> **arc42 §9** · ARIS 3.0 · [Documentation hub](README.md)

Each decision records the context, the choice, and the consequences we accept. IDs are
stable. `Status` is one of **Accepted**, **Superseded**, or **Withdrawn**.

| ID | Decision | Status |
|---|---|---|
| `D1` | Maker-checker restricted to relocations | Accepted |
| `D2` | Tri-state citizen lifecycle (`UNASSIGNED`, `ACTIVE`, `INACTIVE`) | Accepted |
| `D3` | Append-only temporal history | Accepted |
| `D4` | No `Household.CurrentAddressId` | Accepted |
| `D5` | Unified tri-modal relocation schema | Accepted |
| `D6` | Traceable verification lineage (`OpenedByRequestId`) | Accepted |
| `D7` | Independent post-rollback failure auditing | Accepted |
| `D8` | Normalized administrative address hierarchy | Accepted |
| `D9` | Address lineage via self-referencing N → 1 mergers | Accepted |
| `D10` | Local account security (lockout, forced change, roles) | Accepted |
| `D11` | Dynamic `SystemConfiguration` table | Accepted |
| `D12` | Least-privilege database role `aris_app` | Accepted |
| `D13` | Invariant and canonical views | Accepted |
| `D14` | Optimistic concurrency on all mutable tables | Accepted |
| `D15` | Local Docker SQL Server instead of shared Azure SQL | Accepted (supersedes the cloud approach) |
| `D16` | DDL script is authoritative; no EF Core migrations | Accepted |
| `D17` | EF retry-on-failure disabled because mutations own explicit transactions | Accepted |
| `D18` | Application interfaces with Infrastructure service implementations | Accepted |

---

## D1 — Maker-checker restricted to relocations

**Context.** Relocations can move citizens between households, empty a household, or
change who is legally resident at an address. These are high-risk, cross-household
mutations. Demographic edits are frequent and low-risk.

**Decision.** Enforce two-person control only on `TransferRequest`, through
`CK_TR_Decision` (`DecidedBy <> RequestedBy`) and the two-phase service flow. All other
operations are single-officer actions protected by immutable auditing.

**Consequences.**
- Relocations require two authenticated officers and two transactions separated by human
  review.
- Routine work is not slowed by a second approver.
- The database, not just the UI, rejects self-approval.

---

## D2 — Tri-state citizen lifecycle

**Context.** A citizen can be registered before they belong to a household. Requiring a
household at creation creates a circular dependency: the household needs a head, the head
needs a citizen, and the citizen needs a household.

**Decision.** Introduce `UNASSIGNED` as the initial state. `HH-01`/`HH-02` transition a
citizen to `ACTIVE`. `CIT-04` transitions to `INACTIVE`.

**Consequences.**
- Registration and household assignment are separate, order-independent steps.
- The Golden Invariant applies only to `ACTIVE` citizens, so `UNASSIGNED` rows are
  legitimate.
- `CK_Citizen_Lifecycle` ties `INACTIVE` to a non-null `DeactivatedAt`.

---

## D3 — Append-only temporal history

**Context.** In-place updates of tenure, role, or address destroy the legal record and make
audit investigation impossible.

**Decision.** `HouseholdMembership`, `Residence`, and `HouseholdAddress` are append-only.
Changes use close-and-open. Errors are rectified with `Status = 'VOID'` plus a new row
linked by `Corrects...Id`. Guard triggers enforce immutability and non-overlap.

**Consequences.**
- History is reconstructible and tamper-evident.
- Every change is at least two rows; queries must reason about intervals.
- Rectification is explicit rather than silent.

---

## D4 — No `Household.CurrentAddressId`

**Context.** A `CurrentAddressId` column duplicates `HouseholdAddress` and is overwritten
on every move, destroying the household's location history.

**Decision.** Remove it. Resolve current state through `vw_HouseholdCurrent`, which joins
the active `HouseholdAddress`.

**Consequences.**
- One source of truth for household location.
- "Current address" is a query, not a column.
- The view must be used by screens and reports that need the current address.

---

## D5 — Unified tri-modal relocation schema

**Context.** Relocations take three shapes: a member joins another household, a group
splits into a new household, or an entire household moves.

**Decision.** One `TransferRequest` header with `RequestType` and one
`TransferRequestItem` per moving citizen. `CK_TR_Shape` enforces which destination fields
each type may carry. Item-level status supports partial approval.

**Consequences.**
- One approval UI, one audit vocabulary, one set of preconditions.
- Partial approval is possible without a second request.
- `HOUSEHOLD_MOVE` must include every active member; this is a service precondition.

---

## D6 — Traceable verification lineage

**Context.** Determining why a residence exists previously required reconstructing the
audit log.

**Decision.** Store `OpenedByRequestId` on tenure rows created by a transfer. Store
`CreatedBy`/`EndedBy` on all tenure rows. Do not add redundant `VerifiedBy` fields.

**Consequences.**
- Any tenure row links directly to its originating request.
- Accountability is explicit for creation and closure.
- The transfer service must pass its request ID into every row it opens.

---

## D7 — Independent post-rollback failure auditing

**Context.** An audit row written inside a transaction that later rolls back is discarded.
The failure record disappears exactly when it is most needed.

**Decision.** On failure: roll back first, then write the `FAILURE` row through
`LogFailureIndependentAsync` on a new connection via `SqlResilience`.

**Consequences.**
- Failure records survive rollback.
- There are two audit code paths; developers must choose correctly.
- A failure-audit write must not throw a business exception that hides the original error.

---

## D8 — Normalized administrative address hierarchy

**Context.** Free-text addresses cannot be aggregated for reports and are ambiguous.

**Decision.** Decompose into `AddressLine`, `Ward`, `District` (nullable for legacy), and
`Province`. Expose `AddressText` as a computed column using `CONCAT_WS`.

**Consequences.**
- Regional reporting is possible (`RPT-01`).
- `District` is nullable, so queries must handle legacy rows.
- `AddressText` is database-generated; C# never sets it.

---

## D9 — Address lineage via self-referencing N → 1 mergers

**Context.** Administrative boundaries change. Residents do not move when a ward is
renamed or merged.

**Decision.** Retire the old address (`INACTIVE`, `ValidTo`, `DeactivatedAt`) and set
`ReplacedByAddressId` to the successor. Never mutate historical residences.
`vw_AddressCanonical` resolves the terminal address.

**Consequences.**
- Historical records remain legally valid.
- Current-state comparisons must use canonical IDs.
- Splits (one address → many) are out of scope; only mergers are supported.
- A chain is limited to 20 levels.

---

## D10 — Local account security

**Context.** A desktop administrative application needs brute-force resistance without an
external identity provider.

**Decision.** `UserAccount` stores a salted hash, `FailedLoginCount`, `LockedUntil`,
`MustChangePassword`, and `PasswordChangedAt`. Roles are `OFFICER` and `ADMIN`.
`SystemConfiguration` supplies thresholds.

**Consequences.**
- No SSO or central identity; the application owns credentials.
- Lockout policy is configurable without recompilation.
- Administrators must bootstrap the first account through the application.

---

## D11 — Dynamic system configuration

**Context.** Lockout duration and password length should not require a rebuild.

**Decision.** Store operational parameters as key/value rows in `SystemConfiguration`,
seeded by the DDL.

**Consequences.**
- Administrators can change policy at runtime (`SYS-01`).
- Configuration reads must tolerate missing keys with documented defaults.
- Changes to configuration should be audited.

---

## D12 — Least-privilege database role

**Context.** A desktop client connects directly to SQL Server. A bug or a malicious user
could attempt to delete history or rewrite audit rows.

**Decision.** The application connects as `aris_officer`, a member of `aris_app`, with
`SELECT/INSERT/UPDATE` on `dbo`, `DENY DELETE` on `dbo`, and `DENY UPDATE` on
`AuditLog`. `sa` is used only for schema deployment.

**Consequences.**
- Destructive operations fail at the database even if application code tries them.
- The application cannot "clean up" rows; corrections must be append-only.
- Deployment and runtime use different credentials.

---

## D13 — Invariant and canonical views

**Context.** Some rules span multiple tables and cannot be expressed as a single CHECK
constraint.

**Decision.** Provide `vw_AddressCanonical`, `vw_HouseholdCurrent`, and
`vw_GoldenInvariantViolations` (`C1`–`C5`, `H1`–`H4`). Tests assert the violations view
returns zero rows.

**Consequences.**
- Cross-table consistency is continuously observable.
- Views become part of the public database contract.
- A view change can break tests and reports; changes must be coordinated.

---

## D14 — Optimistic concurrency on all mutable tables

**Context.** Multiple officers may edit the same record. Last-write-wins silently loses
data; pessimistic locking holds transactions across think time.

**Decision.** Add `rowversion` to all nine mutable tables and map it with
`.IsRowVersion()`. Surface conflicts as `DbUpdateConcurrencyException`.

**Consequences.**
- Stale writes are rejected.
- The UI must handle reload-and-retry.
- `AuditLog` and `SystemConfiguration` are exempt because they have no `rowversion`.

---

## D15 — Local Docker SQL Server instead of shared Azure SQL

**Status.** Accepted. Supersedes the earlier shared Azure SQL Database approach.

**Context.** The team initially used one shared Azure SQL Database so everyone worked
against the same data. In practice this made collaboration harder:

- Azure SQL rejects client connections until each developer's current public IP is
  allow-listed; changing networks broke the setup.
- A serverless database auto-pauses and the first connection can take tens of seconds,
  which looks like a failure.
- Shared state meant one developer's test or destructive experiment affected everyone.
- Provisioning, cost alerts, and credential distribution consumed project time.

**Decision.** Run SQL Server 2022 Developer Edition in Docker on each developer machine.
Use `docker-compose.yml`, a git-ignored `.env` for the `sa` password, `ArisDb` as the
database name, and one deployment script to create the schema and `aris_officer`.

**Consequences.**
- Onboarding is offline and repeatable: `docker compose up -d` then the deploy script.
- Any developer can reset their database without coordinating.
- There is no shared live dataset; seed data and integration tests replace it.
- Docker Desktop and ~4 GB of memory become prerequisites.
- The retry helper remains useful for container restarts and transient connection errors,
  but serverless auto-pause is no longer a concern.
- Application code and the schema are unchanged; only deployment and configuration differ.

**Superseded approach.** Shared Azure SQL Database with a firewall allow-list, serverless
compute, and a distributed `aris_officer` password. It is retained here only as history;
it is no longer supported by the documentation, scripts, or onboarding.

---

## D16 — DDL script is authoritative; no EF Core migrations

**Context.** EF Core migrations would create a second schema history alongside
`docs/aris_schema_v2.sql`. The DDL contains triggers, filtered indexes, computed columns,
views, role grants, and seed rows that are easy to lose or duplicate in scaffolded
migrations.

**Decision.** The SQL script is the only schema definition. EF Core maps to it. Schema
changes are made in the script, applied to a clean local database, and reflected in the
entity configurations. Migrations are forbidden.

**Consequences.**
- One inspectable source of truth, suitable for the course report.
- No migration history to drift from the deployed schema.
- Developers must reset the local database after a schema change.
- Production-grade incremental migration is out of scope.

---

## D17 — EF retry-on-failure disabled for explicit transactions

**Context.** The application context originally called `EnableRetryOnFailure()`. Every
mutating service opens an explicit transaction for atomicity, and EF Core's
`SqlServerRetryingExecutionStrategy` rejects user-initiated transactions. The first
citizen registration from the UI failed at runtime with "The configured execution
strategy does not support user-initiated transactions."

**Decision.** Do not enable the retrying execution strategy on `ArisDbContext`.
`SqlResilience` remains the retry path for the independent failure-audit connection.
If EF-level retry is required later, each transaction must be wrapped in
`Database.CreateExecutionStrategy().ExecuteAsync(...)`.

**Consequences.**
- Explicit transactions work with the real DI composition.
- EF queries and saves do not retry transient faults automatically; the local container
  is expected to be stable and connection-open retries still exist on the audit path.
- The retry policy is documented in §8.4 and §6.2 so it is not re-enabled accidentally.

---

## D18 — Application interfaces with Infrastructure service implementations

**Context.** The layering rule forbids `Aris.Application` from referencing EF Core or
`Microsoft.Data.SqlClient` (`CON-ARCH-01`, `CON-ARCH-02`). Composite use cases such as
household registration and transfer execution need `ArisDbContext`, an explicit
transaction and serializable isolation. The original canonical-service sample in
Appendix C placed such a service in Application while injecting `ArisDbContext`, which
contradicted the layering table.

**Decision.** Application owns the module contracts: interfaces, DTOs, validators and
`Result<T>`. Infrastructure implements the services that need persistence primitives
(`BootstrapService`, `DemoDataService`, `CitizenService`, `AddressService`,
`HouseholdService`, `TransferService`) behind those interfaces. Repository-backed
services (`AuthService`, `AccountService`, `ReportService`, `AuditQueryService`,
`SystemConfigurationService`) remain in Application. The UI resolves only the
Application interfaces through DI.

**Consequences.**
- Application remains persistence-free and the UI never sees EF Core.
- Some service implementations sit in Infrastructure; §5.2.2/§5.2.4 record this.
- Service-level tests can target the interfaces with a real database or fakes.
