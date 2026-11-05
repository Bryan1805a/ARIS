using Aris.Application.TemporaryResidences.Dtos;
using Aris.Application.TemporaryResidences.Validators;
using Xunit;

namespace Aris.UnitTests.TemporaryResidences;

public sealed class TemporaryResidenceValidatorTests
{
    private static RegisterTemporaryResidenceDto ValidRegister() => new(
        NationalId: "012345678901",
        AddressId: 1,
        StartDate: DateOnly.FromDateTime(DateTime.Today),
        EndDate: DateOnly.FromDateTime(DateTime.Today.AddMonths(6)),
        StayReason: "Thue nha tro / Rent house",
        HostInfo: "Nguyen Van B",
        Notes: "Ghi chu dang ky"
    );

    [Fact]
    public void Valid_register_dto_passes()
    {
        var validator = new RegisterTemporaryResidenceValidator();
        var result = validator.Validate(ValidRegister());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345678901")]
    [InlineData("1234567890123")]
    [InlineData("12345678901A")]
    public void NationalId_must_be_exactly_12_digits(string nationalId)
    {
        var validator = new RegisterTemporaryResidenceValidator();
        var dto = ValidRegister() with { NationalId = nationalId };
        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.NationalId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddressId_must_be_positive(int addressId)
    {
        var validator = new RegisterTemporaryResidenceValidator();
        var dto = ValidRegister() with { AddressId = addressId };
        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.AddressId));
    }

    [Fact]
    public void EndDate_must_not_be_earlier_than_StartDate()
    {
        var validator = new RegisterTemporaryResidenceValidator();
        var dto = ValidRegister() with
        {
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 5, 1)
        };
        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.EndDate));
    }

    [Fact]
    public void Duration_must_not_exceed_two_years()
    {
        var validator = new RegisterTemporaryResidenceValidator();
        var dto = ValidRegister() with
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2028, 1, 10) // > 2 years
        };
        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.EndDate));
    }

    [Fact]
    public void StayReason_is_required()
    {
        var validator = new RegisterTemporaryResidenceValidator();
        var dto = ValidRegister() with { StayReason = "" };
        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.StayReason));
    }
}

