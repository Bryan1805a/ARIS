# 11. Risks and Technical Debt

> **arc42 §11** · ARIS 3.9 · [Documentation hub](README.md)

## 11.1 Risk assessment

Probability and impact are **High / Medium / Low**. A risk with a mitigation is not
closed; it is managed and reviewed.

| ID | Risk | Prob. | Impact | Mitigation | Owner |
|---|---|:---:|:---:|---|---|
| `R1` | **EF save ordering violates a filtered unique index.** Close-and-open requires the old row to be updated before the new row is inserted; EF may batch them in the wrong order (for example two active heads or two active residences in one batch). | High | High | Save closures first (`SaveChanges`) and then open new rows inside the same transaction; add integration tests for `HH-04` and transfer approval. | Coder 2 |
| `R2` | **Application and trigger rules drift.** A service permits something the trigger rejects, producing SQL errors instead of friendly failures. | Medium | High | Treat the DDL as the specification; add a negative test per trigger code; validate in the service first. | DB Designer + Coder 2 |
| `R3` | **Serializable transfers deadlock or time out** under concurrent approvals. | Medium | Medium | Keep transactions short; no user interaction inside them; consistent table access order; catch and report deadlock (1205) as retryable. | Coder 2 |
| `R4` | **Failure audit is lost or hides the original error.** | Medium | High | `LogFailureIndependentAsync` after rollback; swallow audit-write failures after recording them; integration test forces a rollback. | DB Designer |
| `R5` | **Audit log grows without bound** and cannot be deleted by the application role. | Low | Medium | Out of scope for the prototype; document an archival procedure requiring a privileged DBA and a controlled trigger disable window. | Team Leader |
| `R6` | **Docker/WSL resource problems** prevent SQL Server from starting. | Medium | High | Document the 4 GB requirement; check `docker logs`; allow a native SQL Server instance via a different connection string. | All |
| `R7` | **Port 1433 conflict** with another SQL Server or container. | Medium | Medium | Detect before setup; document how to change the host port and connection strings together. | All |
| `R8` | **Concurrency conflicts confuse users** or are "fixed" by overwriting. | Medium | Medium | Consistent reload-and-retry dialog; code review rejects last-write-wins; integration test with two contexts. | Coder 1 |
| `R9` | **Secret leakage** through a committed `.env`, connection string, or audit detail. | Low | High | git-ignore, user-secrets, PR scan, no connection strings in logs. | All |
| `R10` | **False confidence from in-memory tests.** EF InMemory does not enforce triggers, filtered indexes, computed columns, or `rowversion`. | High | High | Limit unit tests to pure logic and mapping; prove database rules only in `Aris.Tests`; release gate requires integration tests. | Team Leader |
| `R11` | **Password hashing implemented incorrectly** (fast hash, reused salt, non-constant-time compare). | Medium | High | Use a vetted PBKDF2/BCrypt implementation; fixed iteration count; unit tests for salt uniqueness and verification. | Coder 3 |
| `R12` | **Scope creep or schedule slip.** | Medium | High | Follow the seven-phase roadmap and the phase task briefs; reject out-of-scope features. | Team Leader |
| `R13` | **Schema drift** because a local database was hand-edited and the DDL was not updated. | Medium | High | The DDL is authoritative; reset and redeploy after schema changes; integration tests compare expected objects. | DB Designer |
| `R14` | **Transaction held open during UI interaction**, locking rows for minutes. | Low | High | Transfers are two transactions; no `await` on user input inside a transaction; code review. | All |
| `R15` | **No first administrator** after a fresh deployment. | High | Low | **Resolved:** the login screen offers a one-time **Create first administrator** flow backed by `BootstrapService`, which refuses to run once any account exists. | Coder 3 |
| `R16` | **Report queries slow down** as data grows. | Low | Medium | Use the existing indexes; aggregate in SQL; add indexes only with evidence. | Coder 3 |
| `R17` | **Address lineage exceeds 20 levels or contains a cycle.** | Low | Medium | Depth limit; validate on write; report `C5`/`H3` if the chain is broken. | Coder 2 |
| `R18` | **Local data loss** from `docker compose down -v` or a disk failure. | Medium | Low | Intentional resets only; document the command's destructive effect; seed data is replaceable. | All |
| `R19` | **Invalid audit JSON** rejected by `CK_Audit_Details`. | Medium | Low | Always use `JsonSerializer.Serialize`; `AuditEntryValidator` validates before persistence. | All |
| `R20` | **Case/accent-insensitive search does not behave as expected** because of collation. | Medium | Medium | Verify the database collation; use `COLLATE` or normalized search columns if needed; test with Vietnamese diacritics. | Coder 1 |
| `R21` | **The login-screen Developer mode bypasses administrator authorization.** It can create an `ADMIN` account and seed data without a signed-in administrator if the local machine is left unlocked. | Low | High | Confined to the local login screen with a visible warning; every action is audited (`CREATE_ACCOUNT` / `SEED_DEMO_DATA`); documented as `D21`; remove or gate before any non-academic deployment. | Team Leader |

---

## 11.2 Technical debt

Debt is recorded deliberately. Each item has a plan; none is silently accepted forever.

| ID | Debt | Impact | Plan |
|---|---|---|---|
| `T1` | Baseline lacked the domain entities, `ArisDbContext`, configurations, and `AuditService`. | Blocked every feature. | **Resolved in `P1-DB2`** (entities, context, configurations, both audit paths). |
| `T2` | Feature screens and the relocation engine existed only as stubs. | The core relocation workflow could not be demonstrated. | **Resolved:** citizen, household, transfer, auth, admin and report screens are implemented; the maker-checker engine executes all three request shapes. |
| `T3` | Service-level tests were missing for several core slices. | Regressions were caught late. | **Resolved:** `CitizenServiceTests`, `HouseholdServiceTests` and `TransferServiceTests` exercise the services against real SQL with a recording audit double; `AccountService` and `SystemConfigurationService` have unit tests. Address and rectification remain covered by database-level integration tests. |
| `T4` | No committed UI automation or end-to-end harness; WinForms flows were verified manually (temporary automation scripts were not in the repository). | UI wiring regressions were caught late. | **Resolved:** `EndToEndWorkflowTests` runs citizen → household → transfer → rectification → audit against real SQL Server; Appendix E is the committed manual WinForms release/demo checklist. |
| `T5` | No CI pipeline; gates are manual. | A red build can reach `develop`. | **Resolved:** `.github/workflows/ci.yml` runs build/unit tests on Windows and SQL Server integration tests on Linux; `main` and `develop` require the checks. |
| `T6` | First-admin bootstrap is undocumented in code. | Fresh install has no way to log in. | **Resolved:** `BootstrapService` + `BootstrapForm` implement the one-time first-admin path. |
| `T7` | `SqlResilience` included cloud service-tier error numbers (for example 40613, 49918–49920) that are unlikely to occur against a local container. | It could retry irrelevant errors or miss a restart-specific failure. | **Resolved:** the list is narrowed to local-container failures — `233`, `1205`, `1222`, `4060`, `10053`, `10054`, `10060` — and `SqlResilienceTests` proves the Azure service-tier numbers are no longer transient. |
| `T8` | Reports and admin screens were implemented, `AUD-01` filtering/detail was completed. | The audit viewer could demonstrate the full required filtering and detail flow. | **Resolved:** the viewer filters by date, actor, entity, action and result, validates criteria, and shows read-only row detail. `AuditQueryFilterTests` and `AuditQueryServiceTests` prove the query paths. |
| `T9` | No optimistic-concurrency integration test. | `D14` was unproven. | **Resolved:** `OptimisticConcurrencyTests` loads one row in two contexts, commits the first save, and asserts the stale save raises `DbUpdateConcurrencyException` without overwriting. |
| `T10` | No automated schema-object inventory test. | A missing trigger could go unnoticed. | **Resolved:** `SchemaInventoryTests` asserts 12 tables, 5 triggers, 3 views, the `aris_app` role, and the 4 seed configuration keys. |
| `T11` | Seed data is minimal (configuration only). | Demos require manual data entry. | **Resolved:** `DemoDataService` seeds an idempotent demo dataset; it is exposed as **Accounts → Seed demo data** and keeps the Golden Invariant at zero violations. |
| `T12` | No localization; UI text was English while domain data is Vietnamese. | Minor usability issue. | **Resolved:** Vietnamese is the default; `LanguageManager` and the login/status-bar buttons switch the static UI between Vietnamese and English. Service validation messages remain English. |
| `T13` | Restore reported NU1902/NU1903 vulnerabilities from transitive `System.Security.Cryptography.Pkcs` 9.0.4. | The build was not warning-free, so the "0 warnings" gate was unmet. | **Resolved:** `Pkcs` is pinned to 10.0.13, the solution builds with 0 warnings, and CI treats warnings as errors. |
| `T14` | Account administration and system-configuration mutations were not written to `AuditLog`, and those services threw for expected business failures instead of returning `Result<T>`. | `AUD-01`/`CON-ARCH-07` was completed for admin actions. | **Resolved:** `AccountService` and `SystemConfigurationService` return `Result`, run each mutation and success audit inside `IUnitOfWork` (`D20`), and write independent failure audits. `AdministrationAuditTests` plus service unit tests prove the actions and secret exclusion. |
| `T15` | The login-screen Developer mode (`D21`) bypasses administrator authorization and has no automated test. | An authorization exception is shipped without a regression guard, so a future change could widen it unnoticed. | Keep the warning visible and the service confined to `LoginForm`; add `DeveloperService` tests (create, duplicate username, weak password, seed) and remove or gate the feature before any non-academic deployment. |
| `T16` | `PASSWORD_MIN_LENGTH` is honoured only by `BootstrapService`; `AccountService` and `DeveloperService` use a fixed 8-character minimum. `SESSION_TIMEOUT_MINUTES` is seeded but no idle timeout is enforced. | Administrators can change two configuration keys that the runtime does not fully honour. | Either inject `ISystemConfigurationRepository` into the account/developer services and add a shell idle timer, or document the fixed policy explicitly. Keep `Q-SEC-06` and Appendix E aligned with whichever choice is made. |

---

## 11.3 Known limitations

These are accepted boundaries, not defects:

1. **Single-office, single-database.** No synchronization between offices.
2. **No shared live dataset.** Each developer has a local database by design (`D15`).
3. **N → 1 address mergers only.** Splitting one address into several is out of scope.
4. **No vital records.** Birth, death, and marriage are not modelled.
5. **No external identity.** Accounts are local; no SSO, MFA, or directory integration.
6. **No notifications.** Officers poll the transfer inbox; nothing is pushed or emailed.
7. **No backup/restore.** The Docker volume is disposable; production backup is out of scope.
8. **No production hardening.** Self-signed certificate trust and Developer Edition are
   local-development choices.
9. **No legal validity.** The prototype does not create legally binding residence records.
10. **Windows only.** WinForms and the SDK target `net10.0-windows`.
11. **Developer mode is a prototype convenience.** The login screen can create accounts
    and seed demo data without an administrator session (`D21`, `R21`). It is local-only,
    visibly labelled, audited, and not covered by automated tests (`T15`).

---

## 11.4 When to revisit this section

Re-open the risk register when any of the following occurs:

- a new table, trigger, or cross-table rule is added;
- the transfer engine changes shape or isolation level;
- the team moves from local Docker to any shared database;
- authentication or authorization changes;
- the Developer mode is removed, gated, or its scope changes (`D21`, `R21`);
- a production or multi-office deployment is proposed;
- a defect escapes a release gate;
- the course scope or deadline changes.
