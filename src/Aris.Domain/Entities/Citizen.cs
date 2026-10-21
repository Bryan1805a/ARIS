using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Person identity and lifecycle (<c>dbo.Citizen</c>, <c>CIT-01</c>, <c>D2</c>).
/// </summary>
/// <remarks>
/// Deliberately has no <c>HouseholdId</c> or <c>AddressId</c>: where a citizen lives
/// and which household they belong to are time-varying facts stored in
/// <see cref="HouseholdMembership"/> and <see cref="Residence"/> (<c>CON-ARCH-03</c>).
/// </remarks>
public sealed class Citizen : EntityBase
{
    public int CitizenId { get; set; }

    /// <summary>Exactly 12 numeric digits (<c>char(12)</c>, unique).</summary>
    public string NationalId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    /// <summary><c>MALE</c>, <c>FEMALE</c>, or <c>OTHER</c>.</summary>
    public string Gender { get; set; } = null!;

    /// <summary><c>UNASSIGNED</c>, <c>ACTIVE</c>, or <c>INACTIVE</c>.</summary>
    public string Status { get; set; } = "UNASSIGNED";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Required when <see cref="Status"/> is <c>INACTIVE</c> (<c>CK_Citizen_Lifecycle</c>).</summary>
    public DateTime? DeactivatedAt { get; set; }
}
