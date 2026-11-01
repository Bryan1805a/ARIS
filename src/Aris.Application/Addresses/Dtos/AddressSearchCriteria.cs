namespace Aris.Application.Addresses.Dtos;

/// <summary>Address search filters (<c>SRCH-03</c>, <c>ADR-02</c>).</summary>
public sealed record AddressSearchCriteria {
    /// <summary>
    /// Matches code, street, ward, district, province, or the full computed
    /// <c>AddressText</c>.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary><c>ACTIVE</c> or <c>INACTIVE</c>; null means all.</summary>
    public string? Status { get; init; }
}