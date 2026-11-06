using Aris.Application.CivilStatuses.Dtos;
using Aris.Application.CivilStatuses.Validators;
using Aris.Domain.Entities;
using Aris.Infrastructure.CivilStatuses;
using Aris.Infrastructure.Persistence;

using Xunit;

namespace Aris.Tests;

public sealed class CivilStatusServiceTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Register_birth_persists_record_and_success_audit() {
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        await using var context = new ArisDbContext(
            ServiceTestSupport.Options(TestDatabase.ConnectionString));

        await ServiceTestSupport.EnsureSchemaAsync(
            TestDatabase.ConnectionString, Token);

        var userId = await ServiceTestSupport.EnsureUserAsync(
            context, "civil_status_test_user", "OFFICER", "ACTIVE", Token);

        var address = await ServiceTestSupport.CreateAddressAsync(
            context, "Civil status test address", Token);

        var citizen = await ServiceTestSupport.CreateCitizenAsync(
            context, "Civil Status Test Citizen", Token);

        var audit = new RecordingAuditService();
        var service = new CivilStatusService(
            context,
            audit,
            new RecordCivilStatusValidator(),
            new CancelCivilStatusValidator());

        var dto = new RecordCivilStatusDto(
            "BIRTH",
            citizen.CitizenId,
            null,
            address.AddressId,
            new DateOnly(2020, 1, 2),
            new DateOnly(2020, 1, 5),
            null,
            "Integration test birth");

        var result = await service.RegisterAsync(dto, userId, Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));

        var record = await context.CivilStatusRecords
            .FindAsync(new object[] { result.Value }, Token);

        Assert.NotNull(record);
        Assert.Equal("BIRTH", record!.RecordType);
        Assert.Equal("ACTIVE", record.Status);
        Assert.Single(audit.SuccessEntries);
        Assert.Equal("REGISTER_CIVIL_STATUS", audit.SuccessEntries[0].Action);
    }

    [Fact]
    public async Task Marriage_is_normalized_and_duplicate_pair_is_rejected() {
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        await using var context = new ArisDbContext(
            ServiceTestSupport.Options(TestDatabase.ConnectionString));

        await ServiceTestSupport.EnsureSchemaAsync(
            TestDatabase.ConnectionString, Token);

        var userId = await ServiceTestSupport.EnsureUserAsync(
            context, "civil_status_test_user", "OFFICER", "ACTIVE", Token);

        var address = await ServiceTestSupport.CreateAddressAsync(
            context, "Civil status marriage address", Token);

        var first = await ServiceTestSupport.CreateCitizenAsync(
            context, "Marriage Citizen A", Token);

        var second = await ServiceTestSupport.CreateCitizenAsync(
            context, "Marriage Citizen B", Token);

        var audit = new RecordingAuditService();
        var service = new CivilStatusService(
            context,
            audit,
            new RecordCivilStatusValidator(),
            new CancelCivilStatusValidator());

        var forward = new RecordCivilStatusDto(
            "MARRIAGE",
            second.CitizenId,
            first.CitizenId,
            address.AddressId,
            new DateOnly(2020, 2, 2),
            new DateOnly(2020, 2, 3),
            null,
            null);

        var firstResult = await service.RegisterAsync(forward, userId, Token);
        Assert.True(firstResult.IsSuccess, string.Join("; ", firstResult.Errors));

        var record = await context.CivilStatusRecords
            .FindAsync(new object[] { firstResult.Value }, Token);

        Assert.NotNull(record);
        Assert.True(record!.CitizenId < record.RelatedCitizenId);

        var reverse = new RecordCivilStatusDto(
            "MARRIAGE",
            first.CitizenId,
            second.CitizenId,
            address.AddressId,
            new DateOnly(2020, 2, 2),
            new DateOnly(2020, 2, 3),
            null,
            null);

        var secondResult = await service.RegisterAsync(reverse, userId, Token);

        Assert.True(secondResult.IsFailure);
    }

    [Fact]
    public async Task Death_record_cannot_be_cancelled() {
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        await using var context = new ArisDbContext(
            ServiceTestSupport.Options(TestDatabase.ConnectionString));

        await ServiceTestSupport.EnsureSchemaAsync(
            TestDatabase.ConnectionString, Token);

        var userId = await ServiceTestSupport.EnsureUserAsync(
            context, "civil_status_test_user", "OFFICER", "ACTIVE", Token);

        var address = await ServiceTestSupport.CreateAddressAsync(
            context, "Civil status death address", Token);

        var citizen = await ServiceTestSupport.CreateCitizenAsync(
            context, "Death Test Citizen", Token);

        var audit = new RecordingAuditService();
        var service = new CivilStatusService(
            context,
            audit,
            new RecordCivilStatusValidator(),
            new CancelCivilStatusValidator());

        var dto = new RecordCivilStatusDto(
            "DEATH",
            citizen.CitizenId,
            null,
            address.AddressId,
            new DateOnly(2020, 3, 3),
            new DateOnly(2020, 3, 4),
            null,
            null);

        var register = await service.RegisterAsync(dto, userId, Token);
        Assert.True(register.IsSuccess, string.Join("; ", register.Errors));

        var cancel = await service.CancelAsync(
            register.Value,
            new CancelCivilStatusDto("Should not be allowed"),
            userId,
            Token);

        Assert.True(cancel.IsFailure);
    }
}