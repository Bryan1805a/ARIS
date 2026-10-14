# Role Brief - Coder 1 (Citizen Management)

**Document Code:** `WKF-TEAM-C1-v2.0`
**Owner:** `<name>`
**Module A:** Citizen Management (`CIT-01`-`CIT-04`) & Directory Search (`SRCH-01`)
**Companion docs:** [`../workflow/phase1-tasks.md`](../workflow/phase1-tasks.md) §3 · [`../architecture/srs.md`](../architecture/srs.md) §4.2

> [!TIP]
> You are building the project's **first vertical slice**. Whatever pattern you establish
> here (DTOs → validator → service → UI) will be copied by Coders 2 and 3. Do it cleanly.

---

## 1. What You Own

Register citizens, view their profile + residency history, update demographics, deactivate
them, and search the directory. You work top to bottom: DTOs → validation → service → WinForms UI.

## 2. What You Can Start Today vs What Is Blocked

| Task | Status | Notes |
|---|---|---|
| `P1-C1` Citizen contracts & validation | **Start now** | No database needed. Nothing blocks you. |
| `P2-C1` `CitizenService` | Blocked | Waits on the Database Designer's `ArisDbContext` (`P1-DB2`) |
| `P2-UI1` Citizen Directory UI | Blocked | Waits on `P2-C1` |
| `P3-C1` Citizen Profile UI | Later | Phase 3 |

**Do not sit idle.** Finish `P1-C1` completely (including unit tests) before `P1-DB2` lands.

---

## 3. Step by Step - `P1-C1` (start now)

```powershell
git checkout develop
git pull origin develop
git checkout -b feature/p1-c1
```

### Step A - Add FluentValidation

It is not referenced yet. Add it to the Application layer:

```powershell
dotnet add src/Aris.Application/Aris.Application.csproj package FluentValidation
dotnet add src/Aris.Application/Aris.Application.csproj package FluentValidation.DependencyInjectionExtensions
```

### Step B - Create the DTOs

In `src/Aris.Application/Citizens/Dtos/`. Use immutable `record` types with `required`/`init`
([conventions §5](../standards/coding-conventions.md)). These are the **only** shapes the UI ever sees -
never hand a domain entity to a form.

<details>
<summary><b>Template: the five DTOs</b></summary>

```csharp
namespace Aris.Application.Citizens.Dtos;

public sealed record CreateCitizenDto
{
    public required string NationalId { get; init; }
    public required string FullName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
}

public sealed record UpdateCitizenDto
{
    public required int CitizenId { get; init; }
    public required string FullName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
}

public sealed record CitizenDto
{
    public required int CitizenId { get; init; }
    public required string NationalId { get; init; }
    public required string FullName { get; init; }
    public required string Status { get; init; }
}

public sealed record CitizenDetailDto
{
    public required int CitizenId { get; init; }
    public required string NationalId { get; init; }
    public required string FullName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public required string Status { get; init; }
    public int? HouseholdId { get; init; }
    public string? CurrentAddressText { get; init; }
}

public sealed record CitizenSearchCriteria
{
    public string? NationalId { get; init; }
    public string? FullName { get; init; }
    public string? Status { get; init; }
}
```
</details>

### Step C - Create the validators

In `src/Aris.Application/Citizens/Validators/`. The rules come straight from the schema:
`NationalId` is `char(12)`, so it must be **exactly 12 numeric digits**; `Gender` is one of three values.

<details>
<summary><b>Template: <code>CreateCitizenValidator.cs</code></b></summary>

```csharp
using Aris.Application.Citizens.Dtos;
using FluentValidation;

namespace Aris.Application.Citizens.Validators;

public sealed class CreateCitizenValidator : AbstractValidator<CreateCitizenDto>
{
    public CreateCitizenValidator()
    {
        RuleFor(x => x.NationalId)
            .NotEmpty()
            .Matches("^[0-9]{12}$")
            .WithMessage("National ID must be exactly 12 digits (char(12)).");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.");

        RuleFor(x => x.Gender)
            .NotEmpty()
            .Must(g => g is "MALE" or "FEMALE" or "OTHER")
            .WithMessage("Gender must be MALE, FEMALE or OTHER.");
    }
}
```
</details>

### Step D - Write the service contract (interface only for now)

`src/Aris.Application/Citizens/ICitizenService.cs`. The body comes in `P2-C1`; the interface
lets the UI and tests be written in parallel.

```csharp
using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;

namespace Aris.Application.Citizens;

public interface ICitizenService
{
    Task<Result<int>> CreateCitizenAsync(CreateCitizenDto dto, int currentUserId, CancellationToken ct = default);
    Task<Result<CitizenDetailDto>> GetCitizenAsync(int citizenId, CancellationToken ct = default);
    Task<Result> UpdateCitizenAsync(UpdateCitizenDto dto, int currentUserId, CancellationToken ct = default);
    Task<Result> DeactivateCitizenAsync(int citizenId, int currentUserId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CitizenDto>>> SearchCitizensAsync(CitizenSearchCriteria criteria, CancellationToken ct = default);
}
```

### Step E - Write unit tests for the validator

`tests/Aris.UnitTests/Citizens/CreateCitizenValidatorTests.cs`. Validators are pure logic -
no database, so they run in milliseconds and always run.

<details>
<summary><b>Template: validator tests</b></summary>

```csharp
using Aris.Application.Citizens.Dtos;
using Aris.Application.Citizens.Validators;
using Xunit;

namespace Aris.UnitTests.Citizens;

public sealed class CreateCitizenValidatorTests
{
    private readonly CreateCitizenValidator _validator = new();

    [Fact]
    public void Accepts_a_valid_citizen()
    {
        var dto = new CreateCitizenDto
        {
            NationalId = "123456789012",
            FullName = "Nguyen Van A",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE",
        };

        Assert.True(_validator.Validate(dto).IsValid);
    }

    [Theory]
    [InlineData("12345")]        // too short
    [InlineData("12345678901a")] // not numeric
    [InlineData("")]             // empty
    public void Rejects_a_bad_national_id(string nationalId)
    {
        var dto = new CreateCitizenDto
        {
            NationalId = nationalId,
            FullName = "Nguyen Van A",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE",
        };

        Assert.False(_validator.Validate(dto).IsValid);
    }
}
```
</details>

### Step F - Register the validators

Edit [`src/Aris.Application/DependencyInjection.cs`](../../src/Aris.Application/DependencyInjection.cs):

```csharp
using Aris.Application.Citizens.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Aris.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateCitizenValidator>();
        return services;
    }
}
```

### Step G - Verify and open the PR

```powershell
dotnet build Aris.slnx     # 0 errors, 0 warnings
dotnet test  Aris.slnx     # 0 failed
git add .
git commit -m "P1-C1: citizen DTOs, validators and tests"
git push -u origin feature/p1-c1
```

Then open a PR into `develop` and complete your author self-check.

---

## 4. Phase 2 Preview - `P2-C1` `CitizenService` (after `P1-DB2`)

Once the Database Designer delivers `ArisDbContext`, implement `ICitizenService` following the
canonical pattern in [`../standards/coding-conventions.md`](../standards/coding-conventions.md) §3.
The non-negotiables for **your** service:

- **Validation runs first**, before opening any transaction:
  `var v = await _validator.ValidateAsync(dto, ct); if (!v.IsValid) return Result<int>.Failure(...)`
- **New citizens start `UNASSIGNED`.** Never insert `'ACTIVE'` - that would violate the Golden
  Invariant checks `C1`/`C2` immediately.
- **Deactivation (`CIT-04`)** closes the citizen's active membership and residence using
  close-and-open (`EndDate = E - 1, Status = 'ENDED'`), and if the citizen was the household
  `HEAD`, a successor must be designated, or the household becomes `INACTIVE` if it empties.
- **Audit everything:** `_audit.LogAsync(...)` inside the transaction on success;
  `_audit.LogFailureIndependentAsync(...)` on a **fresh connection after rollback** on failure.
- **Search** must be case- and accent-insensitive (see the SQL collation note in the SRS `SRCH-01`).

> [!WARNING]
> If you find yourself writing `citizen.Status = "ACTIVE"` in `CreateCitizenAsync`, stop - that is
> auto-rejection trap #4. `UNASSIGNED` is the correct initial state.

---

## 5. Definition of Done for `P1-C1`

- [ ] Five DTOs in `src/Aris.Application/Citizens/Dtos/`.
- [ ] `CreateCitizenValidator` (+ `UpdateCitizenValidator`) with the schema rules.
- [ ] `ICitizenService` interface.
- [ ] Validator unit tests, including negative cases.
- [ ] Validators registered in `AddApplication`.
- [ ] `dotnet build Aris.slnx` → 0 errors, 0 warnings; `dotnet test Aris.slnx` → 0 failed.
- [ ] PR reviewed against [`../standards/pr-review-checklist.md`](../standards/pr-review-checklist.md).

---

## 6. Traps Specific to Your Module

| Trap | Correct approach |
|---|---|
| Creating a citizen already `ACTIVE` | Create as `UNASSIGNED`; `HH-01`/`HH-02` makes them active |
| Exposing `Citizen` (the entity) to the UI | Return `CitizenDto` / `CitizenDetailDto` |
| Putting `DbContext` or SQL in a form | Call `ICitizenService` and show `result.Errors` |
| Setting `nationalId.Length == 12` but allowing letters | Enforce digits: `^[0-9]{12}$` |
| Using an in-memory test to "prove" a DB rule | In-memory has no triggers/indexes - use `tests/Aris.Tests` |

**Stuck?** Follow the escalation format in [`README.md`](README.md) §6 - task id, command, full error, what you tried.
