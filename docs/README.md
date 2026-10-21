# ARIS — Architecture Documentation

**Administrative Residence Information System**

> Academic prototype for a Windows Programming course. ARIS is **not** a production
> population registry and does not integrate with VNeID or any national database.

This documentation follows the **arc42** template: twelve sections that answer the same
questions in the same order for every architecture. Sections are numbered and are the
single entry point for design, development, review, and grading.

| | |
|---|---|
| **Document version** | 3.0 (arc42 restructure) |
| **Last updated** | 2026-10-20 |
| **Target platform** | .NET 10 (C#) · WinForms · SQL Server 2022 (local Docker) · EF Core |
| **Canonical database DDL** | [`aris_schema_v2.sql`](aris_schema_v2.sql) |
| **ERD** | [`erd_v2.svg`](erd_v2.svg) · [`erd_v2.png`](erd_v2.png) |

---

## 1. The arc42 sections

| # | Section | Question it answers | Document |
|---|---|---|---|
| 1 | Introduction and Goals | Why does ARIS exist, who uses it, what must it achieve? | [`01-introduction-and-goals.md`](01-introduction-and-goals.md) |
| 2 | Constraints | What limits our design freedom? | [`02-constraints.md`](02-constraints.md) |
| 3 | Context and Scope | What is inside ARIS, and what does it talk to? | [`03-context-and-scope.md`](03-context-and-scope.md) |
| 4 | Solution Strategy | What are the handful of ideas the whole design rests on? | [`04-solution-strategy.md`](04-solution-strategy.md) |
| 5 | Building Block View | What are the parts, and how are they structured? | [`05-building-block-view.md`](05-building-block-view.md) |
| 6 | Runtime View | How do the important scenarios actually execute? | [`06-runtime-view.md`](06-runtime-view.md) |
| 7 | Deployment View | How does it run on a developer machine? | [`07-deployment-view.md`](07-deployment-view.md) |
| 8 | Crosscutting Concepts | Which rules apply everywhere? | [`08-crosscutting-concepts.md`](08-crosscutting-concepts.md) |
| 9 | Architecture Decisions | Why was each significant choice made? | [`09-architecture-decisions.md`](09-architecture-decisions.md) |
| 10 | Quality Requirements | How good is it, and how is that proven? | [`10-quality-requirements.md`](10-quality-requirements.md) |
| 11 | Risks and Technical Debt | What could go wrong, and what is unfinished? | [`11-risks-and-technical-debt.md`](11-risks-and-technical-debt.md) |
| 12 | Glossary | What do these words mean? | [`12-glossary.md`](12-glossary.md) |

Supporting reference material (not part of the twelve sections):

| Appendix | Content |
|---|---|
| [`appendix/A-domain-model.md`](appendix/A-domain-model.md) | Entity catalogue, lifecycle state machines, state-transition matrix |
| [`appendix/B-database-reference.md`](appendix/B-database-reference.md) | Tables, constraints, triggers, error codes, views, EF Core mapping rules |
| [`appendix/C-coding-standards.md`](appendix/C-coding-standards.md) | Canonical service pattern, auditing code, UI pattern, AI guardrails, PR checklist |
| [`appendix/D-team-workflow.md`](appendix/D-team-workflow.md) | Roles, RACI, roadmap, Phase 1 task briefs, local onboarding, Git workflow |

---

## 2. Reading paths

Choose the path that matches what you are about to do.

| You are about to… | Read, in order |
|---|---|
| **Join the team** | 01 → 02 → 07 → Appendix D |
| **Write your first feature** | 05 → 06 → 08 → Appendix C |
| **Work on the database or EF Core** | Appendix A → Appendix B → 08 → 09 |
| **Review a pull request** | 08 → 10 → Appendix C (PR checklist) |
| **Change an architectural rule** | 04 → 09 → 10 → 11 |
| **Prepare for the course defence** | 01 → 04 → 05 → 06 → 09 → 10 |

Do not read all twelve sections front to back on day one. Section 1, section 7, and
Appendix D are enough to get the application running.

---

## 3. The six non-negotiable invariants

These are the rules that the rest of the documentation explains. They are repeated in
[`08-crosscutting-concepts.md`](08-crosscutting-concepts.md) with their enforcement
mechanisms.

1. **Strict four-layer architecture.** `UI → Application → Domain`, and
   `Infrastructure → Application/Domain`. The UI communicates only through DTOs and
   `Result<T>`. No SQL and no `DbContext` in a WinForms form.
2. **No static keys for time-varying facts.** `Citizen` has no `HouseholdId` or
   `AddressId`; `Household` has no `CurrentAddressId`. Membership, residence, and
   household address live in append-only history tables.
3. **The Golden Invariant.** Every `ACTIVE` citizen has exactly one active membership
   and one active residence at the household's active canonical address.
   `vw_GoldenInvariantViolations` must return **zero rows** after every commit.
4. **Append-only history.** `HouseholdMembership`, `Residence`, and `HouseholdAddress`
   are never edited in place. A change closes the old row
   (`EndDate = E - 1, Status = 'ENDED'`) and opens a new one
   (`StartDate = E, Status = 'ACTIVE'`).
5. **Maker-checker relocations.** A `TransferRequest` is approved or rejected by an
   officer other than its requester (`DecidedBy <> RequestedBy`), inside one
   `IsolationLevel.Serializable` transaction.
6. **Immutable, failure-resistant auditing.** Every mutation writes an `AuditLog` row.
   A failure is logged on an independent connection **after** the rollback.

---

## 4. Documentation rules

- **The DDL is the single source of truth for the schema.** If a document and
  `aris_schema_v2.sql` disagree, the DDL is correct and the document must be fixed.
- **Requirement IDs are stable.** `CIT-01`, `HH-04`, `UC-06`, `CON-SEC-02`, `D14`,
  `C3`, `51011` — do not renumber them. They are referenced by tests, commits, and
  the PR checklist.
- **One fact, one home.** Section 8 owns crosscutting rules; Section 9 owns rationale;
  Appendix A owns entity columns; Appendix B owns database objects. Link instead of
  copying where possible.
- **Diagrams are Mermaid** so they render in GitHub, Visual Studio, and the course
  report without a separate tool.

---

## 5. Verification commands

```powershell
dotnet --version                    # must print 10.0.401 (global.json)
docker compose up -d                # starts SQL Server 2022 on localhost:1433
.\scripts\deploy-database.ps1       # creates ArisDb, schema, and aris_officer
dotnet build Aris.slnx              # 0 errors, 0 warnings
dotnet test  Aris.slnx              # never pass --nologo
```

After every committed business operation:

```sql
SELECT * FROM dbo.vw_GoldenInvariantViolations;   -- must return 0 rows
```

Full setup details are in [`07-deployment-view.md`](07-deployment-view.md).
