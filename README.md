# ARIS — Administrative Residence Information System

A Windows desktop application for local residence-management officers: citizen
records, household management, address lineage, and maker-checker relocation
workflows with an append-only historical audit trail.

> Academic prototype for a Windows Programming course. It is **not** a
> production population registry and does not integrate with any national
> database or VNeID.

## Tech Stack

| Component | Version / Choice |
|---|---|
| Language / Runtime | .NET 10 (C#) |
| SDK | .NET SDK `10.0.401` |
| IDE | Microsoft Visual Studio 2026 |
| UI Framework | Windows Forms (WinForms) |
| ORM | Entity Framework Core |
| Database | Microsoft SQL Server 2019+ (Docker for local dev) |
| Version Control | Git (`feature/*` → `develop` → `main`) |

## Documentation (Single Sources of Truth)

- [`docs/srs.md`](docs/srs.md) — Software Requirements Specification & Domain Model v2
- [`docs/database_design_v2.md`](docs/database_design_v2.md) — database rationale & decision log (D1–D14)
- [`docs/aris_schema_v2.sql`](docs/aris_schema_v2.sql) — database creation script
- [`docs/ai-coding-guidelines.md`](docs/ai-coding-guidelines.md) — anti-hallucination rules & prompt header
- [`docs/pr-review-checklist.md`](docs/pr-review-checklist.md) — PR review & merge checklist
- [`docs/work-breakdown.md`](docs/work-breakdown.md) — WBS, roles, phases, RACI

## Repository Layout

```
Aris.slnx
Directory.Build.props          # shared MSBuild settings
global.json                    # pinned .NET SDK + MTP test runner
docker-compose.yml             # local SQL Server container
src/
  Aris.Domain/          # entities, enums, invariants
  Aris.Application/     # use cases, DTOs, validators, Result<T>
  Aris.Infrastructure/  # EF Core DbContext, mappings, security, Resilience/
  Aris.UI/              # WinForms shell, views, view models
tests/
  Aris.Tests/           # xUnit v3 integration tests (golden invariant)
scripts/
  deploy-database.ps1   # deploy schema + app user to Azure SQL
docs/                                 # specifications and database scripts
```

### Layering Rule

`UI → Application → Domain` and `Infrastructure → Application/Domain`. The UI
only talks to the Application layer through DTOs and `Result<T>`. No EF Core or
SQL in UI code.

## Prerequisites

- .NET SDK **10.0.401** (`dotnet --version`)
- Visual Studio 2026 (or the .NET CLI)
- Docker Desktop (for the local database path)
- **sqlcmd** (only needed to deploy the schema to Azure SQL):

  ```powershell
  winget install Microsoft.Sqlcmd
  ```

  Note it installs to `C:\Program Files\SqlCmd\` — open a new terminal afterwards,
  or call it by full path. [`scripts/deploy-database.ps1`](scripts/deploy-database.ps1)
  finds it either way.

## Getting Started

1. **Start SQL Server**

   ```powershell
   Copy-Item .env.example .env   # then edit .env and set a strong password
   docker compose up -d
   ```

2. **Create the database** (deploy the schema to an empty database):

   ```powershell
   # Using sqlcmd inside the container:
   docker exec -i aris-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<password>" -C -Q "CREATE DATABASE ArisDb"
   docker cp docs/aris_schema_v2.sql aris-sqlserver:/tmp/schema.sql
   docker exec -i aris-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<password>" -C -d ArisDb -i /tmp/schema.sql
   ```

3. **Build and run**

   ```powershell
   dotnet build Aris.slnx
   dotnet run --project src/Aris.UI
   ```

## Shared Database on Azure (optional)

The team can point at one shared Azure SQL Database instead of each running a local
container, so everyone works against the same data. Both paths are supported: use
Azure as the **shared** target and Docker as the **scratch** target.

### 1. Create the database

In the Azure Portal, create **SQL database** → *Serverless* compute tier (General
Purpose, Standard-series Gen5). **Serverless matters**: it auto-pauses when idle and
bills per second, so a team that works in bursts does not pay for an idle database.
A provisioned always-on database will consume a student credit quickly. Set a
**budget alert** under Cost Management on the day you create it.

Name the database `ArisDb` — the tests assert that name.

### 2. Allow your team through the firewall

Azure SQL is closed to the network by default. On the server's **Networking** page,
add a firewall rule for each developer's client IP.

> Do **not** enable "Allow Azure services and resources to access this server". Your
> WinForms client runs on laptops, not inside Azure, so that rule grants exposure
> without solving the problem. Client IPs change when people switch networks — that
> is the usual cause of "it worked yesterday".

### 3. Create the application user, and deploy the schema

Use [`scripts/deploy-database.ps1`](scripts/deploy-database.ps1). It applies the
schema, creates a team login, adds it to the least-privilege role, and then
**verifies** the result — including that the audit log really rejects updates. It
is safe to re-run (every step checks existence first) and it prompts for the admin
password, so the password never appears on the command line or in your shell history.

```powershell
# See what it would do, without touching the database or being asked for a password
.\scripts\deploy-database.ps1 -WhatIf

# Do it
.\scripts\deploy-database.ps1
```

It prints a generated password for the team login **once** — record it. Give
teammates that login, **never** the server admin credentials.

<details>
<summary>Doing it manually instead</summary>

The script wraps these two commands. Azure SQL requires an encrypted connection;
`-N` enforces it and `-C` trusts the server certificate (valid for
`*.database.windows.net`).

Deploy the schema to the empty database:

```powershell
sqlcmd -S "aris-demo-b1805.database.windows.net" -d ArisDb -U <admin> -P "<admin password>" -N -C -i docs/aris_schema_v2.sql
```

The script's final section creates the least-privilege role `aris_app`
(`SELECT/INSERT/UPDATE` on `dbo`, `DENY DELETE`, `DENY UPDATE` on `AuditLog`).
Now create a login for the team and put it in that role. The user and the role must
have **different** names:

```sql
-- Connect to the ArisDb database as the server admin, then:
CREATE USER aris_officer WITH PASSWORD = '<a strong password>';
ALTER ROLE aris_app ADD MEMBER aris_officer;
```

</details>

> `DENY` beats `GRANT`, so `aris_officer` genuinely cannot delete history or rewrite
> the audit log, no matter what the application code does. The deploy script asserts
> this rather than assuming it.

### 4. Connection string

Azure presents a valid certificate, so `TrustServerCertificate` is not needed (unlike
the local Docker setup, which uses a self-signed cert). For this project's server:

```
Server=tcp:aris-demo-b1805.database.windows.net,1433;Database=ArisDb;User Id=aris_officer;Password=<password>;Encrypt=True;
```

The server hostname is not a secret — it still requires credentials — so it is fine
in the README. The password never goes in the repo: see
[Configuration & Secrets](#configuration--secrets).

### 5. Auto-pause and retry

A paused serverless database resumes on the next connection, which can fail with a
transient error (typically **40613**) while it wakes up. Microsoft's serverless
guidance requires application-level retry, so do not connect directly — use
[`SqlResilience`](src/Aris.Infrastructure/Resilience/SqlResilience.cs), which retries
opening with exponential backoff and only retries genuinely transient errors:

```csharp
await using var connection = await SqlResilience.OpenAsync(connectionString);
```

When `ArisDbContext` is registered, also enable provider-level retry:

```csharp
options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
```

A cold start can take tens of seconds. That is the database resuming, not a crash.

## Configuration & Secrets

Connection strings and secrets are **never** committed (`CON-SEC-02`). Use .NET
user-secrets or environment variables for local development. See
`src/Aris.UI/appsettings.json` for the expected key
(`ConnectionStrings:ArisDb`).

Stop a shared Azure password from reaching Git:

```powershell
cd src/Aris.UI
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ArisDb" "Server=tcp:<server>.database.windows.net,1433;Database=ArisDb;User Id=aris_officer;Password=<password>;Encrypt=True;"
```

User-secrets are stored outside the repository, per developer. Never paste a real
connection string into `appsettings.json` — the PR checklist
([`docs/pr-review-checklist.md`](docs/pr-review-checklist.md)) explicitly blocks it.

## Testing

`tests/Aris.Tests` holds the database integration tests. The golden-invariant
test asserts that [`docs/aris_schema_v2.sql`](docs/aris_schema_v2.sql)'s view
`vw_GoldenInvariantViolations` returns **zero rows**, and a negative-control test
proves the view actually detects a violation.

Tests need a live database and **skip** (never fail) when one is not configured.

**Against the shared Azure database** (validates what the team actually uses):

```powershell
$env:ARIS_TEST_CONNECTION = "Server=tcp:<server>.database.windows.net,1433;Database=ArisDb;User Id=aris_officer;Password=<password>;Encrypt=True;"
dotnet test Aris.slnx
```

**Against the local Docker container** (fast, offline, safe to break):

```powershell
$env:ARIS_TEST_CONNECTION = "Server=localhost,1433;Database=ArisDb;User Id=sa;Password=<from your .env>;TrustServerCertificate=True"
dotnet test Aris.slnx
```

Without `ARIS_TEST_CONNECTION` the run reports the tests as skipped — that is a
pass, not a failure. The suite includes:

| Test | Purpose |
|---|---|
| `GoldenInvariant_HasNoViolations` | The core contract: the violations view returns 0 rows. |
| `GoldenInvariant_DetectsDeliberateViolation` | Negative control — inserts an invalid citizen in a rolled-back transaction and proves the view reports it, so a broken view cannot pass the test above. |
| `Database_IsReachable_And_IsArisDb` | Diagnoses firewall / auto-pause problems, which otherwise only show up as "skipped". |
| `SqlResilienceTests` | The retry policy: transient errors (40613 etc.) are retried; constraint and trigger violations are not. |

> **Do not pass `--nologo` to `dotnet test`.** .NET 10 routes `dotnet test`
> through Microsoft.Testing.Platform (see `global.json`), which forwards
> unrecognised flags to the test executable. The xUnit app then rejects them and
> the whole run fails with `Zero tests ran` / exit code 5 — a misleading message
> that hides the real cause. Run the test executable directly to see the actual
> error:
>
> ```powershell
> .\tests\Aris.Tests\bin\Debug\net10.0\Aris.Tests.exe --help   # lists valid options
> ```

## Branching

Never commit directly to `main` or `develop`. Branch from `develop`:

```
develop
  └── feature/<module-name>
```

Open a PR into `develop`; the Team Leader reviews using
[`docs/pr-review-checklist.md`](docs/pr-review-checklist.md). See
[`docs/work-breakdown.md`](docs/work-breakdown.md) §7 for the full workflow.

## Golden Invariant

After every committed operation, this query must return **0 rows**:

```sql
SELECT * FROM dbo.vw_GoldenInvariantViolations;
```
