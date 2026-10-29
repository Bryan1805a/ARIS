using Aris.Application.Citizens.Dtos;
using Aris.Application.Citizens.Validators;
using Xunit;

namespace Aris.UnitTests.Citizens;

/// <summary>
/// Mirrors the citizen CHECK constraints and the input rules in
/// <c>docs/08-crosscutting-concepts.md</c> §8.3. Database rules are verified against real
/// SQL Server in <c>Aris.Tests</c>; these tests only prove the friendly pre-check.
/// </summary>
public sealed class CitizenValidatorTests
{
    private static CreateCitizenDto ValidCreate() => new()
    {
        NationalId = "000000000123",
        FullName = "Nguyen Van An",
        DateOfBirth = new DateOnly(1990, 1, 1),
        Gender = "MALE"
    };

    [Fact]
    public void Valid_create_dto_passes()
    {
        var result = new CreateCitizenValidator().Validate(ValidCreate());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678901")]
    [InlineData("1234567890123")]
    [InlineData("12345678901A")]
    public void National_id_must_be_exactly_twelve_digits(string nationalId)
    {
        var dto = ValidCreate() with { NationalId = nationalId };

        var result = new CreateCitizenValidator().Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.NationalId));
    }

    [Fact]
    public void Full_name_is_required_and_bounded()
    {
        var validator = new CreateCitizenValidator();

        Assert.False(validator.Validate(ValidCreate() with { FullName = "" }).IsValid);
        Assert.False(validator.Validate(
            ValidCreate() with { FullName = new string('A', 101) }).IsValid);
    }

    [Fact]
    public void Date_of_birth_must_be_in_the_past()
    {
        var validator = new CreateCitizenValidator();

        Assert.False(validator.Validate(ValidCreate() with
        {
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today)
        }).IsValid);

        Assert.False(validator.Validate(ValidCreate() with
        {
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        }).IsValid);
    }

    [Theory]
    [InlineData("male")]
    [InlineData("UNKNOWN")]
    [InlineData("")]
    public void Gender_must_be_a_schema_value(string gender)
    {
        var dto = ValidCreate() with { Gender = gender };

        var result = new CreateCitizenValidator().Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.Gender));
    }

    [Fact]
    public void Update_dto_does_not_include_the_national_id()
    {
        var validator = new UpdateCitizenValidator();

        var result = validator.Validate(new UpdateCitizenDto
        {
            FullName = "Tran Thi Binh",
            DateOfBirth = new DateOnly(1985, 5, 12),
            Gender = "FEMALE"
        });

        Assert.True(result.IsValid);
    }
}
