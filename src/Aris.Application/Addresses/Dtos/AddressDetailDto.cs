namespace Aris.Application.Addresses.Dtos;

/// <summary>Full address profile, lineage, and current occupancy (<c>ADR-02</c>, <c>ADR-03</c>).</summary>
public sealed record AddressDetailDto {
    public required int AddressId { get; init; }
    public required string AddressCode { get; init; }
    public required string AddressLine { get; init; }
    public required string Ward { get; init; }
    public string? District { get; init; }
    public required string Province { get; init; }
    public required string AddressText { get; init; }
    public required string Status { get; init; }
    public required DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }

    public int? ReplacedByAddressId { get; init; }
    public string? ReplacedByAddressText { get; init; }

    public required DateTime CreatedAt { get; init; }
    public DateTime? DeactivatedAt { get; init; }

    /// <summary>Terminal address of this address's lineage (<c>D9</c>).</summary>
    public int? CanonicalAddressId { get; init; }
    public string? CanonicalAddressText { get; init; }
    public string? CanonicalStatus { get; init; }

    public IReadOnlyList<AddressHouseholdDto> CurrentHouseholds { get; init; }
        = Array.Empty<AddressHouseholdDto>();

    public IReadOnlyList<AddressResidentDto> CurrentResidents { get; init; }
        = Array.Empty<AddressResidentDto>();
}