using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Legal family unit (<c>dbo.Household</c>, <c>D4</c>).
/// </summary>
/// <remarks>
/// There is intentionally no <c>CurrentAddressId</c>. The current location is the
/// active <see cref="HouseholdAddress"/> row, exposed by <c>vw_HouseholdCurrent</c>.
/// </remarks>
public sealed class Household : EntityBase
{
    public int HouseholdId { get; set; }

    /// <summary><c>ACTIVE</c> or <c>INACTIVE</c>.</summary>
    public string Status { get; set; } = "ACTIVE";

    public DateTime CreatedAt { get; set; }

    /// <summary>Required when <see cref="Status"/> is <c>INACTIVE</c>.</summary>
    public DateTime? InactivatedAt { get; set; }
}
