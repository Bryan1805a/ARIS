# The Golden Invariant Specification

**Document Code:** `INV-ARIS-v2.0`  
**Database View:** `dbo.vw_GoldenInvariantViolations` ([`../aris_schema_v2.sql`](../aris_schema_v2.sql#L577))  
**Integration Tests:** [`tests/Aris.Tests/GoldenInvariantTests.cs`](file:///C:/Users/Bryan/Documents/ARIS/tests/Aris.Tests/GoldenInvariantTests.cs)

---

## 1. Definition & Mathematical Formulation

The **Residency-Household Consistency Invariant** (the "Golden Invariant") is the fundamental integrity contract of ARIS:

$$\forall c \in \text{Citizen} \mid \text{Status}(c) = \text{ACTIVE} \implies$$
$$\exists! m \in \text{HouseholdMembership} \mid \text{Citizen}(m) = c \land \text{Status}(m) = \text{ACTIVE}$$
$$\land \; \exists! r \in \text{Residence} \mid \text{Citizen}(r) = c \land \text{Status}(r) = \text{ACTIVE}$$
$$\land \; \exists! ha \in \text{HouseholdAddress} \mid \text{Household}(ha) = \text{Household}(m) \land \text{Status}(ha) = \text{ACTIVE}$$
$$\land \; \text{CanonicalAddress}(\text{Address}(r)) = \text{CanonicalAddress}(\text{Address}(ha))$$
$$\land \; \text{Status}(\text{CanonicalAddress}(\text{Address}(r))) = \text{ACTIVE}$$

In plain terms:
> **Every `ACTIVE` citizen must belong to exactly one active household and have exactly one active residence whose canonical address matches their household's active canonical address.**

```text
ACTIVE Citizen ────(exactly 1)────► ACTIVE HouseholdMembership ────► Household ────► ACTIVE HouseholdAddress (A)
      │                                                                                       │
      └────────────(exactly 1)────► ACTIVE Residence (A) ─────────────────────────────────────┘
```

---

## 2. Canonical Address Resolution (`vw_AddressCanonical`)

When administrative borders change or wards are reorganized, physical locations receive new address codes. ARIS supports $N \rightarrow 1$ address mergers via `ReplacedByAddressId`.

The view `vw_AddressCanonical` uses a recursive Common Table Expression (CTE) to walk the `ReplacedByAddressId` chain to the terminal address:

```sql
WITH chain AS (
    SELECT a.AddressId AS OriginId, a.AddressId AS CurrentId, a.ReplacedByAddressId AS NextId, 0 AS Depth
    FROM dbo.Address a
    UNION ALL
    SELECT c.OriginId, n.AddressId, n.ReplacedByAddressId, c.Depth + 1
    FROM chain c
    JOIN dbo.Address n ON n.AddressId = c.NextId
    WHERE c.Depth < 20
)
SELECT c.OriginId AS AddressId, c.CurrentId AS CanonicalAddressId, a.Status AS CanonicalStatus
FROM chain c
JOIN dbo.Address a ON a.AddressId = c.CurrentId
WHERE c.NextId IS NULL;
```

A historical residence record pointing to an older address ID remains valid as long as its terminal canonical address is `ACTIVE`.

---

## 3. The 9 Automated Invariant Checks

The database view `dbo.vw_GoldenInvariantViolations` continuously checks 9 failure conditions. After every committed business transaction, this view must return **0 rows**:

```sql
SELECT * FROM dbo.vw_GoldenInvariantViolations;
```

### Citizen Invariants (C1–C5)

| Code | Violation Name | Condition Detected |
|---|---|---|
| **C1** | `CITIZEN_NO_ACTIVE_MEMBERSHIP` | A citizen is marked `Status = 'ACTIVE'`, but has no corresponding record with `Status = 'ACTIVE'` in `HouseholdMembership`. |
| **C2** | `CITIZEN_NO_ACTIVE_RESIDENCE` | A citizen is marked `Status = 'ACTIVE'`, but has no record with `Status = 'ACTIVE'` in `Residence`. |
| **C3** | `CITIZEN_ADDRESS_MISMATCH` | A citizen's active `Residence` canonical address differs from their active household's active `HouseholdAddress` canonical address. |
| **C4** | `CITIZEN_NOT_ACTIVE_BUT_ASSIGNED` | A citizen with `Status = 'UNASSIGNED'` or `'INACTIVE'` still holds an active `HouseholdMembership` or `Residence`. |
| **C5** | `RESIDENCE_ADDRESS_NOT_ACTIVE` | An active `Residence` points to an address whose canonical address is `INACTIVE` (or whose lineage chain is broken). |

### Household Invariants (H1–H4)

| Code | Violation Name | Condition Detected |
|---|---|---|
| **H1** | `HOUSEHOLD_ACTIVE_NO_HEAD` | An `ACTIVE` household does not possess exactly one member with `Role = 'HEAD'` and `Status = 'ACTIVE'`. |
| **H2** | `HOUSEHOLD_ACTIVE_NO_MEMBERS` | An `ACTIVE` household has zero active members in `HouseholdMembership`. |
| **H3** | `HOUSEHOLD_ACTIVE_NO_VALID_ADDRESS` | An `ACTIVE` household lacks an active `HouseholdAddress`, or its canonical address is not `ACTIVE`. |
| **H4** | `HOUSEHOLD_INACTIVE_HAS_MEMBERS` | An `INACTIVE` household still contains active member rows in `HouseholdMembership`. |

---

## 4. Verification in Integration Tests

Integration test suite [`tests/Aris.Tests/GoldenInvariantTests.cs`](file:///C:/Users/Bryan/Documents/ARIS/tests/Aris.Tests/GoldenInvariantTests.cs) provides two automated tests:

1. **`GoldenInvariant_HasNoViolations` (Positive Test):**
   Connects to the real database and asserts that `vw_GoldenInvariantViolations` yields 0 rows:
   ```csharp
   await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.vw_GoldenInvariantViolations;", conn);
   var count = (int)(await cmd.ExecuteScalarAsync())!;
   Assert.Equal(0, count);
   ```
2. **`GoldenInvariant_DetectsDeliberateViolation` (Negative Control):**
   Within a rolled-back transaction, inserts an illegal citizen with `Status = 'ACTIVE'` without household membership, and proves that the view detects it with violation code `CITIZEN_NO_ACTIVE_MEMBERSHIP`. This guarantees the view cannot falsely pass if its logic is broken.
