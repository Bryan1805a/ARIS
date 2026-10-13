# AI Coding Guidelines & Anti-Hallucination Playbook

**Document Code:** `STD-AI-v2.0`  
**Target Audience:** All Software Engineers & AI Coding Assistants (Copilot, Cursor, Claude, ChatGPT, Gemini)  
**Single Sources of Truth:** [`../architecture/srs.md`](../architecture/srs.md) · [`../architecture/domain-model.md`](../architecture/domain-model.md) · [`../database/design-rationale.md`](../database/design-rationale.md)

---

## 1. Why Grounding AI Assistants is Critical

When Large Language Models (LLMs) generate code without architectural grounding, they consistently produce subtle bugs that crash during execution or violate database constraints:
1. **Hallucinate Obsolete Schema Columns:** Introducing `Citizen.HouseholdId` or `Household.CurrentAddressId`.
2. **Violate Trigger Immutability:** Attempting in-place `UPDATE` on `Role` or `StartDate`, tripping database triggers `51001` or `51011`.
3. **Breach Layer Boundaries:** Embedding direct `DbContext` calls or SQL statements inside WinForms event handlers.
4. **Incorrect Transaction & Auditing Patterns:** Writing failure audits inside the business transaction, causing them to be wiped out during `RollbackAsync()`.

---

## 2. Canonical AI Prompt Header (Copy-Paste)

> [!TIP]
> Always prepend this prompt context block when instructing AI tools to write or modify code for ARIS:

```text
[PROJECT CONTEXT & CONSTRAINTS]
Project: Administrative Residence Information System (ARIS)
Platform: .NET 10 (C#), WinForms Desktop, Visual Studio 2026, SQL Server 2019+ via Docker/Azure, EF Core.
Architecture: Strict 4-Layer (UI -> Application -> Domain -> Infrastructure).
Single Sources of Truth:
- Schema DDL: docs/aris_schema_v2.sql
- Domain Specification: docs/architecture/domain-model.md
- Design Decisions D1-D14: docs/database/design-rationale.md

MANDATORY RULES (DO NOT DEVIATE):
1. NO STATIC KEYS ON CITIZEN: Citizen has NO HouseholdId or AddressId.
2. NO STATIC ADDRESS ON HOUSEHOLD: Household has NO CurrentAddressId. Household address is in HouseholdAddress.
3. CITIZEN STATUS: Initial status is UNASSIGNED. Transitions to ACTIVE upon household assignment.
4. APPEND-ONLY HISTORY: HouseholdMembership, Residence, and HouseholdAddress are strictly append-only.
   - NEVER execute an in-place UPDATE on CitizenId, HouseholdId, AddressId, Role, or StartDate.
   - Role change = close old row (EndDate = E - 1, Status = 'ENDED') + open new row (StartDate = E, Status = 'ACTIVE').
5. MAKER-CHECKER TRANSFERS: TransferRequest requires RequestedBy != DecidedBy.
6. SERIALIZABLE TRANSACTIONS: All transfer mutations must execute inside a transaction at IsolationLevel.Serializable.
7. ARCHITECTURAL BOUNDARY: NO SQL or DbContext in UI code. The UI only communicates with Application Services via DTOs and Result<T>.
8. GOLDEN INVARIANT: Every ACTIVE citizen MUST have exactly 1 active membership and 1 active residence matching the household's active canonical address.
9. AUDIT RESILIENCE: Log failure audits on an INDEPENDENT database connection AFTER transaction rollback.
```

---

## 3. The 10 AI Hallucination Traps

Reject any generated code that exhibits these patterns:

| # | Common AI Hallucination | Failure Mode in ARIS | Mandatory Correct Pattern |
|---|---|---|---|
| **1** | `citizen.HouseholdId = id;` | Column does not exist in Schema v2; destroys historical lineage. | Create a `HouseholdMembership` entity referencing `CitizenId` and `HouseholdId`. |
| **2** | `household.CurrentAddressId = id;` | Column does not exist in Schema v2; loses address history. | Create a `HouseholdAddress` entity. Query current address via view `vw_HouseholdCurrent`. |
| **3** | Updating `membership.Role = "HEAD";` in-place | Trigger `trg_Membership_Guard` aborts with error **51011**. | Close the old `MEMBER` record (`EndDate = E - 1, Status = 'ENDED'`), then insert a new `HEAD` record (`StartDate = E`). |
| **4** | Creating a citizen with `Status = "ACTIVE"` before household | Golden Invariant violation `C1`/`C2` (`vw_GoldenInvariantViolations`). | Citizen is created as `UNASSIGNED`. Transitions to `ACTIVE` upon `RegisterHousehold` or `AddMember`. |
| **5** | Splitting transfers across multiple transactions | Violates ACID atomicity; leaves orphaned records if any step fails. | Wrap the entire workflow in `BeginTransactionAsync(IsolationLevel.Serializable)`. |
| **6** | Calling `UPDATE` or `DELETE` on `AuditLog` | Trigger `51030` and database role `aris_app` `DENY` reject the operation. | Audit entries are strictly append-only (`INSERT` only). |
| **7** | Logging a `FAILURE` audit inside the business transaction | `RollbackAsync()` discards the failure audit entry. | Roll back the transaction first, then call `LogFailureIndependentAsync` using a fresh connection. |
| **8** | Self-approving transfers (`DecidedBy == RequestedBy`) | Check constraint `CK_TR_Decision` aborts with a database error. | Ensure the approval flow mandates `DecidedBy != RequestedBy`. |
| **9** | Calling `_context.Citizens.ToList()` in WinForms views | Breaches layer boundary, breaks testability, bypasses validation. | Call `_citizenService.SearchCitizensAsync(queryDto)`. |
| **10** | Setting overlapping dates ($StartDate_2 < EndDate_1$) | Trigger `trg_Residence_Guard` aborts with error **51002**. | Ensure effective date $E > StartDate_{old}$, set $EndDate_{old} = E - 1$, and $StartDate_{new} = E$. |

---

## 4. Verification Checklist for AI-Generated Code

Before accepting AI-generated pull requests or code blocks, run this self-check:

- [ ] Does the code compile with **0 warnings and 0 errors**?
- [ ] Are all entities instantiated without static foreign keys on `Citizen` or `Household`?
- [ ] Does every role or address modification use the close-and-open append-only pattern?
- [ ] Is the failure audit written on an independent connection post-rollback?
- [ ] Does `SELECT * FROM dbo.vw_GoldenInvariantViolations;` return 0 rows?
