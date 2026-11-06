using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Birth, death or marriage registration (<c>dbo.CivilStatusRecord</c>).
/// Records the civil-status event only; residence changes remain CIT-04.
/// </summary>
public sealed class CivilStatusRecord : EntityBase {
    public int CivilStatusId { get; set; }

    /// <summary><c>BIRTH</c>, <c>DEATH</c>, or <c>MARRIAGE</c>.</summary>
    public string RecordType { get; set; } = string.Empty;

    public int CitizenId { get; set; }

    /// <summary>Spouse for <c>MARRIAGE</c>; null for birth and death.</summary>
    public int? RelatedCitizenId { get; set; }

    public int? AddressId { get; set; }

    public DateOnly EventDate { get; set; }

    public DateOnly RegistrationDate { get; set; }

    public string? DocumentNumber { get; set; }

    /// <summary><c>ACTIVE</c> or <c>CANCELLED</c>.</summary>
    public string Status { get; set; } = "ACTIVE";

    public string? Notes { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }
}