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
docker-compose.yml             # local SQL Server container
src/
  Aris.Domain/          # entities, enums, invariants
  Aris.Application/     # use cases, DTOs, validators, Result<T>
  Aris.Infrastructure/  # EF Core DbContext, mappings, security
  Aris.UI/              # WinForms shell, views, view models
docs/                                 # specifications and database scripts
```

### Layering Rule

`UI → Application → Domain` and `Infrastructure → Application/Domain`. The UI
only talks to the Application layer through DTOs and `Result<T>`. No EF Core or
SQL in UI code.

## Prerequisites

- .NET SDK **10.0.401** (`dotnet --version`)
- Visual Studio 2026 (or the .NET CLI)
- Docker Desktop

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

## Configuration & Secrets

Connection strings and secrets are **never** committed (`CON-SEC-02`). Use .NET
user-secrets or environment variables for local development. See
`src/Aris.UI/appsettings.json` for the expected key
(`ConnectionStrings:ArisDb`).

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
