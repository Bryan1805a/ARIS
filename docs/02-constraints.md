# 2. Constraints

> **arc42 §2** · ARIS 3.0 · [Documentation hub](README.md)

A constraint is a decision that has already been made and cannot be revisited inside
this project. Each constraint below is normative: code and documentation must conform.

---

## 2.1 Technical constraints

| ID | Constraint | Consequence |
|---|---|---|
| `CON-PROJ-01` | **Platform:** .NET 10 (C#), Windows Forms desktop, EF Core, SQL Server 2022. | WinForms is required by the course. The UI is Windows-only. No web or mobile client. |
| `CON-PROJ-02` | **SDK:** .NET SDK `10.0.401`, pinned in `global.json` with `rollForward: latestFeature`. | A different SDK may change analyzers and test-runner behaviour. `dotnet --version` must print `10.0.401`. |
| `CON-PROJ-03` | **Database engine:** Microsoft SQL Server 2022 Developer Edition running in a local Docker container (`mcr.microsoft.com/mssql/server:2022-latest`). | The schema may use SQL Server-specific features: `rowversion`, filtered indexes, `CONCAT_WS`, `ISJSON`, `THROW`. |
| `CON-PROJ-04` | **Database location:** `localhost,1433`, database `ArisDb`, data persisted in the `mssql-data` Docker volume. | Every developer runs their own database. No shared cloud instance, no network dependency, no firewall configuration. |
| `CON-PROJ-05` | **Schema authority:** [`aris_schema_v2.sql`](aris_schema_v2.sql) is the only DDL. | No EF Core migrations. No hand-edited database that is not in the script. A schema change is a change to the script. |
| `CON-PROJ-06` | **Test runner:** Microsoft.Testing.Platform, configured in `global.json`. | Never pass `--nologo` to `dotnet test`; it is forwarded to the xUnit executable and produces a misleading "Zero tests ran". |
| `CON-PROJ-07` | **Build quality:** the solution must build with **0 errors and 0 warnings**. | `TreatWarningsAsErrors` is `false`, so warnings do not fail the build automatically. Reviewers must enforce them. |
| `CON-PROJ-08` | **Language features:** `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, .NET analyzers enabled at `latest`. | Nullable warnings are meaningful. Do not suppress analyzers to silence a real design problem. |

---

## 2.2 Security constraints

| ID | Constraint | Consequence |
|---|---|---|
| `CON-SEC-01` | **Passwords are never stored in plaintext.** Hash with a salt using PBKDF2 or BCrypt. | `UserAccount.PasswordHash` stores only a salted hash. Passwords, hashes, and attempted passwords are never logged. |
| `CON-SEC-02` | **Secrets never enter Git.** Connection strings and passwords use user-secrets or environment variables. | `.env` is git-ignored. `appsettings.json` contains no credentials. The PR checklist rejects committed secrets. |
| `CON-AUTH-01` | **Authentication is required for all operations.** | No screen may perform a business operation without an authenticated `IUserSession`. |
| `CON-AUTH-02` | **Role-based authorization** (`OFFICER`, `ADMIN`) is enforced in the Application layer, not only by hiding buttons. | A hidden button is usability; a service check is security. |
| `CON-DB-01` | **Least-privilege database role.** The application connects as `aris_officer`, a member of `aris_app`. | `GRANT SELECT, INSERT, UPDATE` on `dbo`; `DENY DELETE` on `dbo`; `DENY UPDATE` on `AuditLog`. `DENY` overrides `GRANT`. |
| `CON-SEC-03` | **The `sa` account is used only for schema deployment**, never by the application and never committed. | The deployment script prompts for or reads the local `sa` password. Runtime configuration uses `aris_officer`. |

---

## 2.3 Architectural constraints

These are the rules introduced in §1.3 of the hub and detailed in
[`08-crosscutting-concepts.md`](08-crosscutting-concepts.md).

| ID | Constraint |
|---|---|
| `CON-ARCH-01` | **Dependency direction:** `UI → Application → Domain`; `Infrastructure → Application/Domain`. The Domain layer references nothing. |
| `CON-ARCH-02` | **The UI never sees a domain entity, a `DbContext`, or SQL.** It exchanges DTOs and receives `Result<T>`. |
| `CON-ARCH-03` | **No static keys for time-varying facts:** no `Citizen.HouseholdId`, no `Citizen.AddressId`, no `Household.CurrentAddressId`. |
| `CON-ARCH-04` | **Append-only history** for `HouseholdMembership`, `Residence`, and `HouseholdAddress`. Changes use close-and-open with `EndDate = E - 1`. |
| `CON-ARCH-05` | **Maker-checker** on every relocation: `DecidedBy <> RequestedBy`, enforced by `CK_TR_Decision`. |
| `CON-ARCH-06` | **Relocations execute in one `IsolationLevel.Serializable` transaction.** |
| `CON-ARCH-07` | **Every mutation is audited.** Success inside the transaction; failure on an independent connection after rollback. |
| `CON-ARCH-08` | **Optimistic concurrency:** all nine mutable tables map `RowVersion` with `.IsRowVersion()`; conflicts surface as `DbUpdateConcurrencyException`. |

---

## 2.4 Organisational constraints

| ID | Constraint | Consequence |
|---|---|---|
| `CON-ORG-01` | **Academic scope and timeline.** The project is delivered in seven phases by a five-person team. | Features that do not serve the course scope are rejected, even if technically interesting. |
| `CON-ORG-02` | **Git flow:** `feature/<task-id>` → Pull Request → `develop` → `main`. | Direct commits to `main` or `develop` are forbidden. Force-pushing a shared branch is forbidden. |
| `CON-ORG-03` | **Vertical slices by owner.** Coder 1 owns citizens, Coder 2 owns households/transfers, Coder 3 owns shell/auth/reports, the Database Designer owns persistence, the Team Leader owns architecture and review. | See the RACI in [`appendix/D-team-workflow.md`](appendix/D-team-workflow.md). |
| `CON-ORG-04` | **Definition of Done is mandatory.** Build, tests, Golden Invariant, repository hygiene, and review. | A feature is not "done" because the happy path works on the author's machine. |
| `CON-ORG-05` | **Local-first development.** The team must be able to work offline, with no cloud account, no firewall rule, and no per-developer database provisioning. | This is why the shared Azure database was retired; see `D15` in [`09-architecture-decisions.md`](09-architecture-decisions.md). |

---

## 2.5 Convention constraints

| Area | Rule |
|---|---|
| Naming | Database tables and columns use the exact names in the DDL. C# properties match column names. `PascalCase` types and members, `_camelCase` private fields, `Async` suffix on asynchronous methods. |
| Dates | `DateOnly` for SQL `date`; `DateTime` (UTC) for SQL `datetime2`. |
| Money/decimals | Not applicable in the current scope. |
| DTOs | Immutable `record` types with `required`/`init`, under `Aris.Application/<Module>/Dtos/`. |
| Validation | FluentValidation, executed **before** a transaction is opened. |
| Error handling | Expected business failures return `Result<T>.Failure`; only programmer errors and infrastructure failures throw. |
| Audit action names | Stable uppercase strings, ≤ 50 characters, e.g. `REGISTER_HOUSEHOLD`, `EXECUTE_TRANSFER`. |
| Entity types | Singular table names (`Citizen`, not `Citizens`); EF `DbSet`s are plural (`Citizens`). |

The full coding standard, including the canonical service and UI patterns, is
[`appendix/C-coding-standards.md`](appendix/C-coding-standards.md).

---

## 2.6 What is deliberately *not* constrained

- The visual design of WinForms screens (within the shell layout).
- The choice of logging library (there is no logging library; the audit trail is the
  record).
- The report export format (CSV or print preview are both acceptable).
- The password hashing algorithm, as long as it is salted PBKDF2 or BCrypt.
- The internal structure of Application services, as long as the canonical pattern and
  layer boundaries are respected.
