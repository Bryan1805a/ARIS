# AI Coding Guidelines & Anti-Hallucination Playbook

**Document Code:** `STD-AI-v2.0`  
**Target:** All Developers & AI Assistants (Copilot, Cursor, ChatGPT, Claude, Gemini)  
**Single Source of Truth:** [`../architecture/srs.md`](../architecture/srs.md) · [`../architecture/domain-model.md`](../architecture/domain-model.md) · [`../database/design-rationale.md`](../database/design-rationale.md)

---

## 1. Why Grounding AI Is Critical

When AI coding assistants generate code without strict constraints, they consistently make specific mistakes:
1. **Hallucinate obsolete schema columns** (e.g., adding `Citizen.HouseholdId` or `Household.CurrentAddressId`).
2. **Violate database triggers** (e.g., executing in-place `UPDATE` on `Role` or `StartDate`, causing trigger errors 51001/51011).
3. **Breach layer boundaries** (e.g., writing direct SQL or `DbContext` queries inside WinForms event handlers).
4. **Mix up transaction & audit handling** (e.g., logging failure inside a rolled-back transaction where it gets discarded).

---

## 2. Standard AI Prompt Header (Copy-Paste)

> [!TIP]
> Paste this block at the top of your prompt whenever prompting an AI coding tool to write or refactor code for ARIS:

```text
[PROJECT CONTEXT & CONSTRAINTS]
Project: Administrative Residence Information System (ARIS) (.NET 10 C#, WinForms desktop app, Visual Studio 2026, SQL Server 2019+ via Docker/Azure, EF Core).
Architecture: Strict 4-Layer (UI -> Application -> Domain -> Infrastructure).
Single Sources of Truth:
- Schema v2 (docs/aris_schema_v2.sql)
- Domain Model (docs/architecture/domain-model.md)
- Design Decisions D1-D14 (docs/database/design-rationale.md)

STRICT RULES (DO NOT DEVIATE):
1. NO STATIC FOREIGN KEYS ON CITIZEN: Citizen DOES NOT have HouseholdId or AddressId.
2. NO STATIC ADDRESS ON HOUSEHOLD: Household DOES NOT have CurrentAddressId. Household address is in HouseholdAddress.
3. CITIZEN STATUS: Must be UNASSIGNED, ACTIVE, or INACTIVE. New citizens are UNASSIGNED until assigned to a household.
4. APPEND-ONLY HISTORY: HouseholdMembership, Residence, and HouseholdAddress are append-only.
   - NEVER execute in-place UPDATE on CitizenId, HouseholdId, AddressId, Role, or StartDate.
   - Role change = close old row (EndDate = E - 1, Status = 'ENDED') + open new row (StartDate = E, Status = 'ACTIVE').
5. MAKER-CHECKER FOR TRANSFERS: TransferRequest must have RequestedBy != DecidedBy.
6. SERIALIZABLE TRANSACTIONS: All transfer and relocation mutations MUST execute inside a single transaction with IsolationLevel.Serializable.
7. ARCHITECTURAL BOUNDARY: NO SQL or DbContext in UI code. The UI only talks to Application Services via DTOs and Result<T>.
8. GOLDEN INVARIANT: Every ACTIVE citizen MUST have exactly 1 active membership and 1 active residence matching the household's active canonical address.
```

---

## 3. The 10 AI Hallucination Traps

Reject any generated code that matches these red flags:

| # | Common AI Hallucination | Why It Breaks the System | Correct Pattern |
|---|---|---|---|
| **1** | Setting `citizen.HouseholdId = id;` | Column does not exist in Schema v2; destroys tenure history. | Create a `HouseholdMembership` entity referencing `CitizenId` and `HouseholdId`. |
| **2** | Setting `household.CurrentAddressId = id;` | Column does not exist in Schema v2; loses address history. | Create a `HouseholdAddress` entity. Query current address via `vw_HouseholdCurrent`. |
| **3** | Updating `membership.Role = "HEAD";` in-place | Trigger `trg_Membership_Guard` aborts with error **51011**. | Close the old `MEMBER` record (`EndDate = E - 1, Status = 'ENDED'`), then insert a new `HEAD` record (`StartDate = E`). |
| **4** | Creating a citizen with `Status = "ACTIVE"` before household | Golden Invariant violation C1/C2 (`vw_GoldenInvariantViolations`). | Citizen is created as `UNASSIGNED`. Transitions to `ACTIVE` upon `RegisterHousehold` or `AddMember`. |
| **5** | Splitting transfers across multiple transactions | Violates ACID atomicity; risks orphaned records if a step fails. | Wrap entire workflow in `BeginTransactionAsync(IsolationLevel.Serializable)`. |
| **6** | Calling `UPDATE` or `DELETE` on `AuditLog` | Trigger `51030` and database role `aris_app` `DENY` reject the operation. | Audit entries are strictly append-only (`INSERT` only). |
| **7** | Logging a `FAILURE` audit inside the business transaction | `RollbackAsync` discards the failure audit entry. | Roll back the transaction first, then call `LogFailureIndependentAsync` using a fresh connection. |
| **8** | Self-approving transfers (`DecidedBy == RequestedBy`) | Check constraint `CK_TR_Decision` aborts with a database error. | Ensure the approval flow mandates `DecidedBy != RequestedBy`. |
| **9** | Calling `_context.Citizens.ToList()` in WinForms views | Breaches layer boundary, breaks testability, bypasses validation. | Call `_citizenService.SearchCitizensAsync(queryDto)`. |
| **10** | Setting overlapping dates ($StartDate_2 < EndDate_1$) | Trigger `trg_Residence_Guard` aborts with error **51002**. | Ensure effective date $E > StartDate_{old}$, set $EndDate_{old} = E - 1$, and $StartDate_{new} = E$. |
