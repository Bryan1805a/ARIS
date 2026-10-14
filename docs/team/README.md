# ARIS Team Handbook & Role Briefs

**Document Code:** `WKF-TEAM-v2.0`
**Audience:** All five team members (Team Leader + Database Designer + Coder 1 + Coder 2 + Coder 3)
**Single Sources of Truth:** [`../workflow/work-breakdown.md`](../workflow/work-breakdown.md) · [`../workflow/phase1-tasks.md`](../workflow/phase1-tasks.md) · [`../standards/coding-conventions.md`](../standards/coding-conventions.md)

> [!NOTE]
> New to the project? Read this page end to end once. Then open **your own role brief**
> below and follow it top to bottom. You do not need to memorise everything - every
> rule here links to the full specification.

---

## 1. Who's Who

Fill in the names once, then everyone knows who to ask.

| Role | Name | Owns | Role brief |
|---|---|---|---|
| **Team Leader** | _(you)_ | Architecture, layer boundaries, PR gatekeeper | [`work-breakdown.md`](../workflow/work-breakdown.md) |
| **Database Designer** | `<name>` | Schema v2, EF Core mappings, `AuditService`, seed data | [**`database-designer.md`**](database-designer.md) |
| **Coder 1** | `<name>` | Citizen management (`CIT-01`-`CIT-04`) & directory search | [**`coder-1.md`**](coder-1.md) |
| **Coder 2** | `<name>` | Household, residence & maker-checker transfers (`UC-06`) | [**`coder-2.md`**](coder-2.md) |
| **Coder 3** | `<name>` | Desktop shell, authentication, admin, reports | [**`coder-3.md`**](coder-3.md) |

---

## 2. The 6 Rules Nobody Is Allowed to Break

Short version. The full reasoning is in [`../README.md`](../README.md) (§ The 6 Non-Negotiable Invariants).

1. **4 layers only.** `UI → Application → Domain`, and `Infrastructure → Application/Domain`. **Never** put SQL or `DbContext` in a WinForms form.
2. **No static keys.** `Citizen` has no `HouseholdId`; `Household` has no `CurrentAddressId`.
3. **The Golden Invariant.** Every `ACTIVE` citizen has exactly one active membership + one active residence matching the household's active address.
4. **Append-only history.** Never `UPDATE` `HouseholdMembership`, `Residence`, or `HouseholdAddress` in place - close the old row and open a new one.
5. **Maker ≠ Checker.** A transfer must be approved by someone other than its requester.
6. **Never lose a failure.** Log `FAILURE` audits on a fresh connection **after** the rollback.

> [!IMPORTANT]
> These rules come from the schema and the triggers. If you break one, the build or the
> database will tell you - loudly. When in doubt, re-read
> [`../standards/ai-coding-guidelines.md`](../standards/ai-coding-guidelines.md) §3.

---

## 3. One-Time Machine Setup (do this on day 1)

```powershell
# 1. Check your SDK - must print 10.0.401 (see global.json)
dotnet --version

# 2. Get the code
git clone <repo-url>
cd ARIS

# 3. Allow your current IP through the Azure SQL firewall.
#    Re-run this whenever you change networks (home / campus / tethering):
.\scripts\update-firewall.ps1

# 4. Point the app at the shared database, without committing the password
cd src/Aris.UI
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ArisDb" "Server=tcp:aris-demo-b1805.database.windows.net,1433;Database=ARIS-DB;User Id=aris_officer;Password=<password>;Encrypt=True;Connect Timeout=60;"
cd ../..
```

Full environment walkthrough: [`../workflow/onboarding.md`](../workflow/onboarding.md).

---

## 4. Daily Workflow Cheat Sheet

### Every task, in order

```powershell
# 1. Start from the latest develop
git checkout develop
git pull origin develop

# 2. Branch off it - name it after YOUR task id (see your role brief)
git checkout -b feature/p1-c1

# 3. Work. Build and test often.
dotnet build Aris.slnx          # must end with 0 errors, 0 warnings
dotnet test  Aris.slnx          # must be 0 failed (never add --nologo)

# 4. Commit in small steps with a task-prefixed message
git add .
git commit -m "P1-C1: add Citizen DTOs and validators"

# 5. Push and open a Pull Request into develop
git push -u origin feature/p1-c1
```

### Before you open the PR (the author self-check)

- [ ] `dotnet build Aris.slnx` → **0 errors, 0 warnings**
- [ ] `dotnet test Aris.slnx` → **0 failed** (tests that need the DB may show as skipped - that is fine)
- [ ] `SELECT * FROM dbo.vw_GoldenInvariantViolations;` → **0 rows** (once the DB is wired up)
- [ ] No secrets, `.env`, `bin/`, or `obj/` staged
- [ ] The checklist in [`../standards/pr-review-checklist.md`](../standards/pr-review-checklist.md) §1

> [!WARNING]
> **Never commit directly to `main` or `develop`, and never `git push --force` a shared branch.**
> All work goes through a `feature/*` branch and a PR the Team Leader reviews.

### When tests skip instead of run

The integration tests need `ARIS_TEST_CONNECTION`. If it is unset they **skip** (that is
a pass, not a failure). To actually run them:

```powershell
$env:ARIS_TEST_CONNECTION = "Server=tcp:aris-demo-b1805.database.windows.net,1433;Database=ARIS-DB;User Id=aris_officer;Password=<password>;Encrypt=True;"
dotnet test Aris.slnx
```

---

## 5. The Golden Invariant, In One Line

After every committed operation, this must return **0 rows**:

```sql
SELECT * FROM dbo.vw_GoldenInvariantViolations;
```

If it returns rows, the message tells you which rule broke (`CITIZEN_NO_ACTIVE_MEMBERSHIP`,
`HOUSEHOLD_ACTIVE_NO_HEAD`, ...). See [`../architecture/golden-invariant.md`](../architecture/golden-invariant.md).

---

## 6. How to Ask for Help (and get unblocked fast)

Beginners lose the most time being stuck silently. Post in the team chat with:

1. **What you were trying to do** (task id, e.g. `P1-C1`).
2. **The exact command** you ran.
3. **The full error text** (copy the whole block, not a paraphrase).
4. **What you already tried.**

Rules of thumb:

| Symptom | Most likely cause | First thing to check |
|---|---|---|
| "Cannot open server ... is not allowed to access the server" | Your IP changed | Re-run `.\scripts\update-firewall.ps1` |
| Login fails / "not currently available" (40613) | Azure SQL was auto-paused | Wait ~60s, retry; the app retries for you via `SqlResilience` |
| `dotnet test` says "Zero tests ran" | You passed `--nologo` | Re-run **without** `--nologo` |
| Trigger error `51001`/`51011`/`51021` | You tried to `UPDATE` history in place | Use close-and-open (see §2 rule 4) |
| `DbUpdateConcurrencyException` | Someone else edited the same row | Reload the record and retry |

---

## 7. Read These Next (in this order)

1. [`../workflow/onboarding.md`](../workflow/onboarding.md) - get running in 5 minutes.
2. [`../architecture/domain-model.md`](../architecture/domain-model.md) - the entities you will touch.
3. [`../standards/coding-conventions.md`](../standards/coding-conventions.md) - the house style and the canonical service pattern.
4. Your role brief:
   - [Database Designer](database-designer.md) · [Coder 1](coder-1.md) · [Coder 2](coder-2.md) · [Coder 3](coder-3.md)
