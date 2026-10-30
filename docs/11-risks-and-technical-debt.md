# 11. Risks and Technical Debt

> **arc42 §11** · ARIS 3.0 · [Documentation hub](README.md)

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

---

## 11.2 Technical debt

Debt is recorded deliberately. Each item has a plan; none is silently accepted forever.

| ID | Debt | Impact | Plan |
|---|---|---|---|
| `T1` | Baseline lacked all 11 entities, `ArisDbContext`, configurations, and `AuditService`. | Blocked every feature. | **Resolved in `P1-DB2`** (entities, context, configurations, both audit paths). |
| `T2` | Feature screens and the relocation engine existed only as stubs. | The core relocation workflow could not be demonstrated. | **Resolved:** citizen, household, transfer, auth, admin and report screens are implemented; the maker-checker engine executes all three request shapes. |
| `T3` | No service-level unit tests; only validator and test-infrastructure tests exist. | Regressions are caught late. | Add tests with each service; use fakes for `IAuditService` and a real DB for persistence. |
| `T4` | No UI automation or end-to-end harness; WinForms flows are verified manually. | UI wiring regressions are caught late. | Add a scripted smoke-test checklist; consider WinAppDriver or a presenter-level test suite later. |
| `T5` | No CI pipeline; gates are manual. | A red build can reach `develop`. | **Resolved:** `.github/workflows/ci.yml` runs build/unit tests on Windows and SQL Server integration tests on Linux; `main` and `develop` require the checks. |
| `T6` | First-admin bootstrap is undocumented in code. | Fresh install has no way to log in. | **Resolved:** `BootstrapService` + `BootstrapForm` implement the one-time first-admin path. |
| `T7` | `SqlResilience` includes cloud service-tier error numbers (for example 40613, 49918–49920) that are unlikely to occur against a local container. | It may retry irrelevant errors or miss a restart-specific failure. | Review the list against `Microsoft.Data.SqlClient` guidance; keep it small and documented. |
| `T8` | Reports (`RPT-01`–`RPT-03`) and admin screens are not implemented. | Incomplete course deliverable. | Phase 5. |
| `T9` | No optimistic-concurrency integration test. | `D14` is unproven. | Add a two-context test that asserts `DbUpdateConcurrencyException`. |
| `T10` | No automated schema-object inventory test. | A missing trigger could go unnoticed. | Add a test asserting 11 tables, 5 triggers, 3 views, and the `aris_app` role. |
| `T11` | Seed data is minimal (configuration only). | Demos require manual data entry. | **Resolved:** `DemoDataService` seeds an idempotent demo dataset; it is exposed as **Accounts → Seed demo data** and keeps the Golden Invariant at zero violations. |
| `T12` | No localization; UI text was English while domain data is Vietnamese. | Minor usability issue. | **Resolved:** Vietnamese is the default; `LanguageManager` and the login/status-bar buttons switch the static UI between Vietnamese and English. Service validation messages remain English. |
| `T13` | Restore reported NU1902/NU1903 vulnerabilities from transitive `System.Security.Cryptography.Pkcs` 9.0.4. | The build was not warning-free, so the "0 warnings" gate was unmet. | **Resolved:** `Pkcs` is pinned to 10.0.13, the solution builds with 0 warnings, and CI treats warnings as errors. |

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

---

## 11.4 When to revisit this section

Re-open the risk register when any of the following occurs:

- a new table, trigger, or cross-table rule is added;
- the transfer engine changes shape or isolation level;
- the team moves from local Docker to any shared database;
- authentication or authorization changes;
- a production or multi-office deployment is proposed;
- a defect escapes a release gate;
- the course scope or deadline changes.
