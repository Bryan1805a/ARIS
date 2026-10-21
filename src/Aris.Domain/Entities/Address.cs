using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Normalized administrative location with lineage (<c>dbo.Address</c>, <c>ADR-01</c>,
/// <c>D8</c>, <c>D9</c>).
/// </summary>
/// <remarks>
/// Retiring an address never rewrites history: set <see cref="Status"/> to
/// <c>INACTIVE</c> and point <see cref="ReplacedByAddressId"/> at the successor.
/// Historical <see cref="Residence"/> and <see cref="HouseholdAddress"/> rows keep the
/// old id; <c>vw_AddressCanonical</c> resolves the terminal address.
/// </remarks>
public sealed class Address : EntityBase
{
    public int AddressId { get; set; }

    public string AddressCode { get; set; } = null!;

    public string AddressLine { get; set; } = null!;

    public string Ward { get; set; } = null!;

    /// <summary>Legacy only; nullable by schema (<c>D8</c>).</summary>
    public string? District { get; set; }

    public string Province { get; set; } = null!;

    /// <summary>Database-generated (<c>CONCAT_WS</c>); never assigned in C#.</summary>
    public string AddressText { get; set; } = null!;

    /// <summary><c>ACTIVE</c> or <c>INACTIVE</c>.</summary>
    public string Status { get; set; } = "ACTIVE";

    public DateOnly ValidFrom { get; set; }

    /// <summary>Required when <see cref="Status"/> is <c>INACTIVE</c>.</summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>Successor in an N to 1 merger; only set on an <c>INACTIVE</c> row.</summary>
    public int? ReplacedByAddressId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeactivatedAt { get; set; }
}
