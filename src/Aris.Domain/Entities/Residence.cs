using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Append-only tenure of a citizen at an address (<c>dbo.Residence</c>, <c>D3</c>).
/// </summary>
/// <remarks>
/// A move is close-and-open, never an in-place <c>AddressId</c> update
/// (trigger <c>51001</c>/<c>51002</c>). A citizen has at most one active residence.
/// </remarks>
public sealed class Residence : EntityBase
{
    public int ResidenceId { get; set; }

    public int CitizenId { get; set; }

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
    public int? CorrectsResidenceId { get; set; }
}
