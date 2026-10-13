# Work Breakdown Structure (WBS) & Implementation Plan

**Document Code:** `WKF-WBS-v2.0`  
**Platform:** .NET 10 (C#) WinForms · Visual Studio 2026 · SQL Server 2019+ · EF Core  
**Single Source of Truth:** [`../architecture/srs.md`](../architecture/srs.md) · [`../database/design-rationale.md`](../database/design-rationale.md)

---

## 1. Implementation Strategy: Vertical Feature Slicing

ARIS rejects horizontal layering silos ("one person writes all UI, one writes all queries"). Instead, developers own **vertical feature slices end-to-end**:

```text
Presentation Layer (UI / Views / ViewModels)
       ↓
Application Layer (Use Cases / Services / DTOs / FluentValidation)
       ↓
Domain Layer (Entities / Invariants / Business Rules)
       ↓
Infrastructure Layer (EF Core DbContext / Mappings / ADO.NET)
       ↓
Database Layer (SQL Server 2019+ / Constraints / Triggers)
```

Each developer implements their assigned feature vertically from the UI down to the database transaction boundary.

---

## 2. Team Structure & Role Distribution

```text
                        ┌────────────────────────┐
                        │      Team Leader       │
                        │ Architecture & Reviews │
                        └───────────┬────────────┘
                                    │
          ┌─────────────────────────┼─────────────────────────┐
          │                         │                         │
┌─────────▼──────────┐    ┌─────────▼──────────┐    ┌─────────▼──────────┐
│ Database Designer  │    │      Coder 1       │    │      Coder 2       │
│ Data & Persistence │    │ Citizen & Search   │    │ Household & Move   │
└────────────────────┘    └────────────────────┘    └────────────────────┘
                                    │
                          ┌─────────▼──────────┐
                          │      Coder 3       │
                          │ Auth, Shell & Rpts │
                          └────────────────────┘
```

| Role | Primary Feature Ownership | Key Deliverables |
|---|---|---|
| **Team Leader** | Solution architecture, project scaffolding, layer boundaries, PR reviews. | Base abstractions, PR review checklist, architectural reviews, invariant verification. |
| **Database Designer** | SQL Server schema v2 deployment, EF Core mappings, seed data, DB triggers/constraints. | `ArisDbContext`, entity Fluent API mappings, ADO.NET `AuditService`, constraint integration tests. |
| **Coder 1** | **Module A:** Citizen Management (`CIT-01`–`CIT-04`) & Directory Search (`SRCH-01`). | **First Vertical Slice** reference implementation, citizen DTOs/validators, `CitizenService`, Citizen UI forms. |
| **Coder 2** | **Module B:** Household, Residence & Relocations Engine (`HH-01`–`HH-06`, `UC-06`). | Maker-checker transfer engine, `HouseholdService`, `TransferService`, Household UI, Transfer Wizard/Inbox. |
| **Coder 3** | **Module C:** Authentication (`AUTH-01`, `ACC-01`), Main Desktop Shell, Reports (`RPT-01`–`RPT-03`). | Desktop navigation shell, `AuthenticationService`, role-based screen visibility, account management & reports UI. |

---

## 3. RACI Responsibility Assignment Matrix

**R** = Responsible (Does the work) | **A** = Accountable (Final architectural sign-off)  
**C** = Consulted (Two-way input) | **I** = Informed (Kept updated)

| Feature / Work Area | Team Leader | Database Designer | Coder 1 | Coder 2 | Coder 3 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Solution Architecture & Layer Boundaries** | **A / R** | C | I | I | I |
| **Database Schema v2 & Migrations** | A | **R** | I | C | I |
| **EF Core DbContext & Entity Mappings** | A | **R** | C | C | C |
| **Citizen Management (`CIT-01`–`CIT-04`)** | A | C | **R** | I | I |
| **Directory Search (`SRCH-01`–`SRCH-03`)** | A | C | **R** | I | C |
| **Household & Membership (`HH-01`–`HH-04`)** | A | C | C | **R** | I |
| **Residence Tracking (`RES-01`–`RES-03`)** | A | C | I | **R** | I |
| **Maker-Checker Transfer Engine (`UC-06`)** | A | C | I | **R** | I |
| **Record Rectification (`HIST-01`)** | A | C | I | **R** | I |
| **Authentication & User Accounts (`AUTH-01`, `ACC-01`)** | A | I | I | I | **R** |
| **Desktop UI Shell & Navigation Container** | A | I | C | C | **R** |
| **Reports Engine (`RPT-01`–`RPT-03`)** | A | C | I | I | **R** |
| **Cross-Cutting Audit Logging** | **A / R** | C | C | C | C |
| **Golden Invariant Test Verification** | **A** | **R** | I | C | I |
| **Optimistic Concurrency Verification (`RowVersion`)** | A | **R** | I | C | I |

---

## 4. 7-Phase Implementation Roadmap

```text
Phase 1: Foundation & Baseline (Days 1–3)
   │
Phase 2: First Vertical Slice - Citizen (Days 4–6)  <-- Reference Architecture
   │
Phase 3: Core Household & Residence Domain (Days 7–11)
   │
Phase 4: Maker-Checker Transfer Engine (Days 12–16) <-- Critical Path
   │
Phase 5: Auth, Admin, Reports & Audit Logging (Days 17–19)
   │
Phase 6: End-to-End Integration & Golden Invariant Verification (Days 20–22)
   │
Phase 7: Hardening, Concurrency Testing & Final Acceptance (Days 23–25)
```

---

## 5. Definition of Done (DoD)

A user story or task is only marked as **Done** when all of the following criteria are met:

1. **Functional Completeness:**
   - Happy path functions end-to-end via the UI.
   - Negative and boundary flows handled (invalid inputs, empty fields, past dates).
   - Inputs validated using FluentValidation before service dispatch.
2. **Architectural Compliance:**
   - UI code contains **zero SQL** and **zero `DbContext` logic**.
   - UI communicates with Application services exclusively via DTOs and `Result<T>`.
   - All multi-table transitions wrapped in a single database transaction.
3. **Database & Historical Integrity:**
   - History records are append-only (never updated in place or deleted).
   - Closed records have `EndDate = EffectiveDate - 1`.
   - Overlap check triggers pass without error.
   - `SELECT * FROM dbo.vw_GoldenInvariantViolations;` returns **0 rows**.
4. **Code Quality:**
   - `dotnet build Aris.slnx` compiles with **0 errors and 0 warnings**.
   - Automated tests pass; PR reviewed and approved by the Team Leader against [`../standards/pr-review-checklist.md`](../standards/pr-review-checklist.md).
