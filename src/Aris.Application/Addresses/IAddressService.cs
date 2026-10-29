using Aris.Application.Addresses.Dtos;
using Aris.Application.Common;

namespace Aris.Application.Addresses;

/// <summary>Address registration and lookup (<c>ADR-01</c>, used by household registration).</summary>
public interface IAddressService
{
    Task<Result<int>> CreateAsync(
        CreateAddressDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result<IEnumerable<AddressDto>>> GetActiveAsync(CancellationToken ct = default);
}
