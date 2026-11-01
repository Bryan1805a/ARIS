using Aris.Application.Addresses.Dtos;
using Aris.Application.Addresses.Validators;

using Xunit;

namespace Aris.UnitTests.Addresses;

public sealed class RetireAddressValidatorTests {
    [Fact]
    public void Valid_retirement_passes() {
        var result = new RetireAddressValidator().Validate(new RetireAddressDto {
            AddressId = 1,
            ReplacedByAddressId = 2,
            ValidTo = new DateOnly(2026, 12, 31)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Retirement_requires_ids_and_a_date() {
        var result = new RetireAddressValidator().Validate(new RetireAddressDto {
            AddressId = 0,
            ReplacedByAddressId = 0,
            ValidTo = default
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.PropertyName == nameof(RetireAddressDto.AddressId));
        Assert.Contains(result.Errors,
            e => e.PropertyName == nameof(RetireAddressDto.ReplacedByAddressId));
        Assert.Contains(result.Errors,
            e => e.PropertyName == nameof(RetireAddressDto.ValidTo));
    }

    [Fact]
    public void An_address_cannot_be_replaced_by_itself() {
        var result = new RetireAddressValidator().Validate(new RetireAddressDto {
            AddressId = 7,
            ReplacedByAddressId = 7,
            ValidTo = new DateOnly(2026, 12, 31)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.PropertyName == nameof(RetireAddressDto.ReplacedByAddressId));
    }
}