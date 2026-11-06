using Aris.Application.CivilStatuses.Dtos;
using Aris.Application.CivilStatuses.Validators;

using Xunit;

namespace Aris.UnitTests.CivilStatuses;

public sealed class CivilStatusValidatorTests {
    private readonly RecordCivilStatusValidator _validator = new();

    private static RecordCivilStatusDto Birth(int citizenId = 1)
        => new(
            "BIRTH",
            citizenId,
            null,
            1,
            new DateOnly(2020, 1, 1),
            new DateOnly(2020, 1, 2),
            null,
            null);

    [Fact]
    public async Task Birth_without_related_citizen_is_valid() {
        var result = await _validator.ValidateAsync(
            Birth(), TestContext.Current.CancellationToken);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Marriage_requires_a_spouse() {
        var dto = new RecordCivilStatusDto(
            "MARRIAGE", 1, null, 1,
            new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 2), null, null);

        var result = await _validator.ValidateAsync(
            dto, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.RelatedCitizenId));
    }

    [Fact]
    public async Task Registration_date_before_event_date_is_rejected() {
        var dto = Birth() with { RegistrationDate = new DateOnly(2019, 12, 31) };

        var result = await _validator.ValidateAsync(
            dto, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.RegistrationDate));
    }

    [Fact]
    public async Task Death_with_a_related_citizen_is_rejected() {
        var dto = new RecordCivilStatusDto(
            "DEATH", 1, 2, 1,
            new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 2), null, null);

        var result = await _validator.ValidateAsync(
            dto, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(dto.RelatedCitizenId));
    }
}