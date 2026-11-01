using Aris.Application.Addresses.Dtos;
using Aris.Application.Common;

namespace Aris.Application.Addresses;

/// <summary>
/// Address registration, search, detail, and retirement
/// (<c>ADR-01</c>–<c>ADR-03</c>, <c>SRCH-03</c>).
/// </summary>
public interface IAddressService {
    Task<Result<int>> CreateAsync(
        CreateAddressDto dto,
        int currentUserId,
        CancellationToken ct = default
    );

    Task<Result<IEnumerable<AddressDto>>> GetActiveAsync(CancellationToken ct = default);

    Task<Result<IEnumerable<AddressDto>>> SearchAsync(
        AddressSearchCriteria criteria,
        CancellationToken ct = default
    );

    Task<Result<AddressDetailDto?>> GetByIdAsync(
        int addressId,
        CancellationToken ct = default
    );

    Task<Result> RetireAsync(
        RetireAddressDto dto,
        int currentUserId,
        CancellationToken ct = default
    );
}