# AI Coding Guidelines & Anti-Delusion Playbook
## Residence Information Management System

**Version:** 1.0  
**Target:** All Team Members & AI Assistants (ChatGPT, Claude, GitHub Copilot, Cursor, Gemini, etc.)  
**Single Source of Truth:** [`srs.md`](file:///C:/Users/Bryan/Documents/residence_information_management_system/docs/srs.md) · [`database_design_v2.md`](file:///C:/Users/Bryan/Documents/residence_information_management_system/docs/database_design_v2.md) · [`residence_schema_v2.sql`](file:///C:/Users/Bryan/Documents/residence_information_management_system/docs/residence_schema_v2.sql)

---

## 1. Why This Document Exists

When developers use AI coding assistants independently, AIs tend to:
1. **Hallucinate legacy schema fields** (e.g., using `Citizen.HouseholdId` or `Household.CurrentAddressId` from obsolete v1 designs).
2. **Violate database triggers** (e.g., executing in-place `UPDATE` on `Role` or `StartDate`, causing trigger exceptions 51001/51011).
3. **Pollute UI with business logic** (e.g., writing EF Core or SQL queries directly inside WinForms event handlers).
4. **Invent conflicting patterns** (e.g., one AI uses raw exceptions, another uses `Result<T>`, a third uses out-parameters).

**Rule for all developers:** Whenever prompting an AI assistant, include the **System Prompt Header** from Section 2 to ground the AI in this project's real constraints.

---

## 2. Standard AI Prompt Header (Copy-Paste into Every Prompt)

> Copy and paste this block at the top of your prompt whenever asking an AI to generate code for this project:

```text
[PROJECT CONTEXT & CONSTRAINTS]
Project: Residence Information Management System (.NET 10 C#, WinForms desktop app, Visual Studio 2026, SQL Server 2019+ via Docker, EF Core).
Architecture: Strict 4-Layer (UI -> Application -> Domain -> Infrastructure).
Single Sources of Truth:
- Schema v2 (residence_schema_v2.sql)
- SRS v2 (srs.md)
- Design Decisions D1-D14 (database_design_v2.md)

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

## 3. The 10 "Hallucination Traps" (AI Red Flags)

AI models frequently default to common boilerplate that contradicts this project. Reject any code containing these patterns:

| # | Common AI Hallucination | Why It Breaks the Project | Correct Project Implementation |
|---|---|---|---|
| **1** | Adding `citizen.HouseholdId = householdId;` | Field does not exist in Schema v2; breaks historical tracking. | Create a `HouseholdMembership` entity referencing `CitizenId` and `HouseholdId`. |
| **2** | Adding `household.CurrentAddressId = addressId;` | Field was removed in v2. Address history is lost. | Create a `HouseholdAddress` entity. Query current state via view `vw_HouseholdCurrent`. |
| **3** | Updating `membership.Role = "HEAD";` in-place | Database trigger `trg_Membership_Guard` will abort the transaction with error **51011**. | Close the old `MEMBER` record (`EndDate = E - 1, Status = 'ENDED'`), then insert a new `HEAD` record (`StartDate = E`). |
| **4** | Creating a citizen directly with `Status = "ACTIVE"` without a household | Violates the Golden Invariant (`vw_GoldenInvariantViolations` violation C1/C2). | New citizens start as `Status = "UNASSIGNED"`. They become `ACTIVE` when `HouseholdService.RegisterHousehold` or `AddMember` commits. |
| **5** | Performing transfers in multiple separate transactions or via direct UI calls | Orphaned records if network fails; violates ACID atomicity and concurrency control. | Encapsulate entire flow inside `TransferService.ExecuteTransfer` using `IsolationLevel.Serializable`. |
| **6** | Using `UPDATE` or `DELETE` on `AuditLog` | Database role `residence_app` has `DENY UPDATE` and `DENY DELETE` on `AuditLog`; trigger `51030` blocks changes. | Audit logs are strictly append-only (`INSERT` only). |
| **7** | Logging a `FAILURE` audit entry within the rolled-back transaction | Transaction rollback discards the audit log, losing the trace of the failure. | Catch the exception, execute `transaction.Rollback()`, then log the failure using a **separate, fresh database connection**. |
| **8** | Self-approving transfers (`DecidedBy == RequestedBy`) | Check constraint `CK_TR_Decision` will abort the transaction. | Ensure approval UI requires a different officer ID than the requester. |
| **9** | Direct `_context.Citizens.ToList()` in UI Form / View code | Destroys layer boundaries, breaks testability, bypasses validation. | Call `_citizenService.SearchCitizensAsync(queryDto)`. |
| **10** | Overlapping date intervals ($StartDate_2 < EndDate_1$) | Trigger `trg_Residence_Guard` throws error **51002**. | Ensure effective date $E > StartDate_{old}$, set $EndDate_{old} = E - 1$, and $StartDate_{new} = E$. |

---

## 4. Canonical Code Patterns (Feed to AI as Reference)

When asking AI to implement a new service or UI component, provide these reference patterns:

### 4.1. Application Service Pattern (`Result<T>` and Transaction Boundary)

```csharp
public class HouseholdService : IHouseholdService
{
    private readonly ResidenceDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IValidator<RegisterHouseholdDto> _validator;

    public HouseholdService(ResidenceDbContext context, IAuditService auditService, IValidator<RegisterHouseholdDto> validator)
    {
        _context = context;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<Result<int>> RegisterHouseholdAsync(RegisterHouseholdDto dto, int currentUserId)
    {
        // 1. Validation before transaction
        var valResult = await _validator.ValidateAsync(dto);
        if (!valResult.IsValid)
            return Result<int>.Failure(valResult.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Open atomic transaction
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var effectiveDate = dto.EffectiveDate;

            // Step A: Create Household
            var household = new Household { Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            _context.Households.Add(household);
            await _context.SaveChangesAsync();

            // Step B: Record Household Address
            var householdAddress = new HouseholdAddress
            {
                HouseholdId = household.HouseholdId,
                AddressId = dto.AddressId,
                StartDate = effectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.HouseholdAddresses.Add(householdAddress);

            // Step C: Link Head Member
            var membership = new HouseholdMembership
            {
                CitizenId = dto.HeadCitizenId,
                HouseholdId = household.HouseholdId,
                Role = "HEAD",
                StartDate = effectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.HouseholdMemberships.Add(membership);

            // Step D: Create Residence
            var residence = new Residence
            {
                CitizenId = dto.HeadCitizenId,
                AddressId = dto.AddressId,
                StartDate = effectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Residences.Add(residence);

            // Step E: Update Citizen Status
            var citizen = await _context.Citizens.FindAsync(dto.HeadCitizenId);
            citizen.Status = "ACTIVE";
            citizen.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Step F: Audit Success
            await _auditService.LogAsync(currentUserId, "REGISTER_HOUSEHOLD", "Household", household.HouseholdId, "SUCCESS", null);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return Result<int>.Success(household.HouseholdId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // Failure audit must run on a separate connection/transaction
            await _auditService.LogFailureIndependentAsync(currentUserId, "REGISTER_HOUSEHOLD", "Household", ex.Message);
            return Result<int>.Failure($"Registration failed: {ex.Message}");
        }
    }
}
```

### 4.2. UI Pattern (ViewModel / Presenter Calling Service)

```csharp
public class RegisterHouseholdViewModel : BaseViewModel
{
    private readonly IHouseholdService _householdService;
    private readonly IUserSession _userSession;

    public RegisterHouseholdViewModel(IHouseholdService householdService, IUserSession userSession)
    {
        _householdService = householdService;
        _userSession = userSession;
    }

    public async Task ExecuteSaveAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var dto = new RegisterHouseholdDto
        {
            HeadCitizenId = SelectedCitizenId,
            AddressId = SelectedAddressId,
            EffectiveDate = EffectiveDate
        };

        var result = await _householdService.RegisterHouseholdAsync(dto, _userSession.CurrentUserId);

        IsBusy = false;
        if (result.IsSuccess)
        {
            CloseDialogWithSuccess(result.Value);
        }
        else
        {
            ErrorMessage = string.Join("\n", result.Errors);
        }
    }
}
```

---

## 5. Pull Request Verification Checklist (For Human Reviewers)

Before approving any AI-generated PR, the reviewer must check:

- [ ] **No schema hallucinations:** Are all column and table names matching `residence_schema_v2.sql`?
- [ ] **No in-place role or date edits:** Does the code modify `Role` or `StartDate` via `UPDATE` instead of close-and-insert?
- [ ] **No UI leakage:** Does any UI code contain `using (var context = ...)` or raw SQL queries?
- [ ] **Transaction isolation:** Are multi-table operations wrapped in `BeginTransactionAsync(IsolationLevel.Serializable)`?
- [ ] **Failure audit handled:** Does exception handling record a failure audit log over an independent connection?
- [ ] **Golden Invariant verified:** After running the feature locally, does `SELECT * FROM dbo.vw_GoldenInvariantViolations;` return **0 rows**?
