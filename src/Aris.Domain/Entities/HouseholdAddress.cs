using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Append-only history of where a household is located (<c>dbo.HouseholdAddress</c>,
/// <c>D4</c>).
/// </summary>
/// <remarks>
/// A change is close-and-open: close the old row with <c>EndDate = E - 1</c> and
/// <c>Status = ENDED</c>, then open a new row with <c>StartDate = E</c>. Identity
/// columns are never updated (<c>CON-ARCH-04</c>, trigger <c>51021</c>/<c>51022</c>).
/// </remarks>
public sealed class HouseholdAddress : EntityBase
{
    public int HouseholdAddressId { get; set; }

    public int HouseholdId { get; set; }

    public int AddressId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    /// <summary><c>ACTIVE</c>, <c>ENDED</c>, or <c>VOID</c>.</summary>
    public string Status { get; set; } = "ACTIVE";

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? EndedBy { get; set; }

    public DateTime? EndedAt { get; set; }

    /// <summary>The transfer that opened this row, when applicable (<c>D6</c>).</summary>
    public int? OpenedByRequestId { get; set; }

    /// <summary>The voided row this row corrects (<c>HIST-01</c>).</summary>
    public int? CorrectsHouseholdAddressId { get; set; }
}
