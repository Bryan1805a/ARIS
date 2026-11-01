namespace Aris.Application.Addresses.Dtos;

/// <summary>Read model for address lists and pickers (<c>ADR-02</c>, <c>SRCH-03</c>).</summary>
public sealed record AddressDto {
    public required int AddressId { get; init; }
    public required string AddressCode { get; init; }
    public required string AddressLine { get; init; }
    public required string AddressText { get; init; }
    public required string Ward { get; init; }
    public string? District { get; init; }
    public required string Province { get; init; }
    public required string Status { get; init; }
    public required DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public int? ReplacedByAddressId { get; init; }
    public DateTime? DeactivatedAt { get; init; }
}