using Aris.Application.Addresses.Dtos;
using Aris.Application.Addresses.Validators;
using Aris.Application.Households.Dtos;
using Aris.Application.Households.Validators;
using Xunit;

namespace Aris.UnitTests.Households;

public sealed class HouseholdValidatorTests
{
    [Fact]
    public void Valid_registration_passes()
    {
        var result = new RegisterHouseholdValidator().Validate(new RegisterHouseholdDto
        {
            HeadCitizenId = 1,
            AddressId = 1,
            EffectiveDate = new DateOnly(2026, 1, 1)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Registration_requires_ids_and_an_effective_date()
    {
        var result = new RegisterHouseholdValidator().Validate(new RegisterHouseholdDto
        {
            HeadCitizenId = 0,
            AddressId = 0,
            EffectiveDate = default
        });

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void Adding_a_member_requires_both_ids_and_a_date()
    {
        var result = new AddMemberValidator().Validate(new AddMemberDto
        {
            HouseholdId = 0,
            CitizenId = 0,
            EffectiveDate = default
        });

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void Changing_the_head_requires_both_ids_and_a_date()
    {
        var result = new ChangeHeadValidator().Validate(new ChangeHeadDto
        {
            HouseholdId = 0,
            NewHeadCitizenId = 0,
            EffectiveDate = default
        });

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void Valid_address_passes()
    {
        var result = new CreateAddressValidator().Validate(new CreateAddressDto
        {
            AddressCode = "ADR-001",
            AddressLine = "12 Nguyen Hue",
            Ward = "Ben Nghe",
            District = "Quan 1",
            Province = "Ho Chi Minh City",
            ValidFrom = new DateOnly(2026, 1, 1)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Address_requires_code_line_ward_province_and_valid_from()
    {
        var result = new CreateAddressValidator().Validate(new CreateAddressDto
        {
            AddressCode = "",
            AddressLine = "",
            Ward = "",
            Province = "",
            ValidFrom = default
        });

        Assert.False(result.IsValid);
        Assert.Equal(5, result.Errors.Count);
    }

    [Fact]
    public void Address_code_is_bounded_to_thirty_characters()
    {
        var result = new CreateAddressValidator().Validate(new CreateAddressDto
        {
            AddressCode = new string('A', 31),
            AddressLine = "12 Nguyen Hue",
            Ward = "Ben Nghe",
            Province = "Ho Chi Minh City",
            ValidFrom = new DateOnly(2026, 1, 1)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressDto.AddressCode));
    }
}
