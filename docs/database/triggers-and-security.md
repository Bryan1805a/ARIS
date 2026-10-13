# Database Triggers, Error Codes & Security

**Document Code:** `DB-SECURITY-v2.0`  
**Target Engine:** SQL Server 2019+ · Azure SQL  
**Schema Script:** [`../aris_schema_v2.sql`](../aris_schema_v2.sql)

---

## 1. Database Triggers (Defense Layer 3)

SQL Server triggers serve as the final backstop against application bugs and improper updates. The schema implements **5 specific triggers**:

```text
dbo.Residence           ──► trg_Residence_Guard          (Append-only + temporal non-overlap)
dbo.HouseholdMembership ──► trg_Membership_Guard         (Append-only + temporal non-overlap)
dbo.HouseholdAddress    ──► trg_HouseholdAddress_Guard   (Append-only + temporal non-overlap)
dbo.TransferRequest     ──► trg_TransferRequest_Frozen   (Decided requests are frozen)
dbo.AuditLog            ──► trg_AuditLog_AppendOnly      (Audit records cannot be modified or deleted)
```

### Append-Only Mechanics
For `Residence`, `HouseholdMembership`, and `HouseholdAddress`:
- **Identity Immutability:** Any `UPDATE` statement attempting to alter `CitizenId`, `HouseholdId`, `AddressId`, `Role`, or `StartDate` causes an immediate `ROLLBACK` and raises a `510x1` error.
- **Frozen VOID rows:** Records with `Status = 'VOID'` are permanently immutable.
- **Ended records:** Records with `Status = 'ENDED'` may only transition to `'VOID'`.

### Temporal Non-Overlap Mechanics
- When inserting or updating a record that is not `VOID`, the trigger checks against other non-`VOID` records for the same citizen (or household).
- If $[StartDate_1, EndDate_1]$ overlaps with $[StartDate_2, EndDate_2]$, the trigger executes `ROLLBACK` and raises a `510x2` error.

---

## 2. Trigger Error Code Reference

When writing unit tests, integration tests, or exception handlers, reference these standardized error codes:

| Error Code | Source Trigger | Condition / Violation Meaning |
|---|---|---|
| **`51001`** | `trg_Residence_Guard` | Append-only violation on `Residence`: identity fields mutated or invalid state transition. |
| **`51002`** | `trg_Residence_Guard` | Temporal overlap: two non-`VOID` residence periods for the same citizen overlap. |
| **`51011`** | `trg_Membership_Guard` | Append-only violation on `HouseholdMembership`: role/dates updated in-place. |
| **`51012`** | `trg_Membership_Guard` | Temporal overlap: two membership intervals for the same citizen overlap. |
| **`51021`** | `trg_HouseholdAddress_Guard` | Append-only violation on `HouseholdAddress`. |
| **`51022`** | `trg_HouseholdAddress_Guard` | Temporal overlap: two address intervals for the same household overlap. |
| **`51030`** | `trg_AuditLog_AppendOnly` | Attempted `UPDATE` or `DELETE` on `dbo.AuditLog`. |
| **`51040`** | `trg_TransferRequest_Frozen` | Attempted update on an already decided (`APPROVED`/`REJECTED`/`CANCELLED`) `TransferRequest`. |

---

## 3. Least-Privilege Security Role (`aris_app`)

The application connects to the database using credentials belonging to the least-privilege database role **`aris_app`**:

```sql
IF DATABASE_PRINCIPAL_ID('aris_app') IS NULL
    CREATE ROLE aris_app;

GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO aris_app;
DENY  DELETE ON SCHEMA::dbo TO aris_app;           -- History cannot be physically deleted
DENY  UPDATE ON dbo.AuditLog TO aris_app;          -- Audit logs are strictly INSERT + SELECT
```

> [!NOTE]
> In SQL Server, **`DENY` takes precedence over `GRANT`**. Even if an application service inadvertently attempts a SQL `DELETE` or `UPDATE AuditLog`, the database engine will reject the query before it executes.

---

## 4. EF Core Configuration Requirements

When mapping the domain entities with Entity Framework Core in `Aris.Infrastructure`:

1. **Trigger Declaration (EF Core 7+):**
   Tables governed by triggers must be configured with `.ToTable(t => t.HasTrigger("..."))`:
   ```csharp
   builder.ToTable("Residence", t => t.HasTrigger("trg_Residence_Guard"));
   builder.ToTable("HouseholdMembership", t => t.HasTrigger("trg_Membership_Guard"));
   builder.ToTable("HouseholdAddress", t => t.HasTrigger("trg_HouseholdAddress_Guard"));
   builder.ToTable("TransferRequest", t => t.HasTrigger("trg_TransferRequest_Frozen"));
   builder.ToTable("AuditLog", t => t.HasTrigger("trg_AuditLog_AppendOnly"));
   ```
   *Omitting `HasTrigger` will cause EF Core's `SaveChanges` output-clause optimization to crash.*

2. **Optimistic Concurrency:**
   The **9 mutable tables** must map `RowVersion`:
   ```csharp
   builder.Property(e => e.RowVersion).IsRowVersion();
   ```
   *(Note: `AuditLog` and `SystemConfiguration` do NOT have `RowVersion`).*

3. **Computed Column:**
   `Address.AddressText` must be mapped as a computed column:
   ```csharp
   builder.Property(a => a.AddressText).HasComputedColumnSql("CONCAT_WS(N', ', AddressLine, Ward, District, Province)");
   ```

4. **Composite Foreign Keys:**
   `TransferRequestItem` must explicitly map composite FKs pointing to `(MembershipId, CitizenId)` and `(ResidenceId, CitizenId)`:
   ```csharp
   builder.HasOne<HouseholdMembership>()
          .WithMany()
          .HasForeignKey(i => new { i.SourceMembershipId, i.CitizenId });
   ```

5. **Delete Behavior:**
   Because `aris_app` has `DENY DELETE`, never configure cascade deletes. Use `DeleteBehavior.Restrict` or `DeleteBehavior.NoAction`.
