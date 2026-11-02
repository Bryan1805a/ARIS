using Aris.Application.Citizens.Dtos;
using Aris.Application.Citizens.Validators;
using Aris.Infrastructure.Citizens;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Service-level tests for the citizen slice (<c>CIT-01</c>, <c>CIT-03</c>, <c>CIT-04</c>)
/// against real SQL Server, with a recording audit double.
/// </summary>
public sealed class CitizenServiceTests {
    private const string ActiveActor = "it-service-citizen-actor";
    private const string LockedActor = "it-service-citizen-locked";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateCitizen_StartsUnassigned_AndWritesSuccessAudit() {
        // New citizens begin outside a household and must produce an audit
        // record that identifies the actor without exposing sensitive data.
        var (options, actorId, audit) = await PrepareAsync();
        var nationalId = ServiceTestSupport.UniqueNationalId();

        await using var context = new ArisDbContext(options);
        var service = new CitizenService(
            context,
            audit,
            new CreateCitizenValidator(),
            new UpdateCitizenValidator());

        var result = await service.CreateCitizenAsync(
            new CreateCitizenDto {
                NationalId = nationalId,
                FullName = "Service Citizen One",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Gender = "OTHER"
            },
            actorId,
            Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.True(result.Value is int citizenId, "Expected the new citizen id.");

        // Read through a fresh context to verify the committed database state.
        await using var read = new ArisDbContext(options);
        var citizen = await read.Citizens
            .AsNoTracking()
            .SingleAsync(c => c.CitizenId == citizenId, Token);

        Assert.Equal("UNASSIGNED", citizen.Status);
        Assert.Null(citizen.DeactivatedAt);

        var entry = Assert.Single(audit.SuccessEntries);
        Assert.Equal("CREATE_CITIZEN", entry.Action);
        Assert.Equal(actorId, entry.ActorUserId);
        Assert.Equal("Citizen", entry.EntityType);
        Assert.Equal(citizenId, entry.EntityId);

        var details = entry.DetailsJson!;
        Assert.Contains(nationalId, details, StringComparison.Ordinal);
        Assert.Contains("UNASSIGNED", details, StringComparison.Ordinal);
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task CreateCitizen_DuplicateNationalId_ReturnsFailure_AndWritesNoSecondAudit() {
        // The national ID uniqueness rule should reject only the second
        // attempt, leaving the first successful audit intact.
        var (options, actorId, audit) = await PrepareAsync();
        var nationalId = ServiceTestSupport.UniqueNationalId();

        var dto = new CreateCitizenDto {
            NationalId = nationalId,
            FullName = "Service Citizen Duplicate",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "OTHER"
        };

        await using var context = new ArisDbContext(options);
        var service = new CitizenService(
            context,
            audit,
            new CreateCitizenValidator(),
            new UpdateCitizenValidator());

        var first = await service.CreateCitizenAsync(dto, actorId, Token);
        Assert.True(first.IsSuccess, string.Join("; ", first.Errors));

        var second = await service.CreateCitizenAsync(dto, actorId, Token);

        Assert.True(second.IsFailure);
        Assert.Contains(
            "already registered",
            string.Join(" ", second.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.Single(audit.SuccessEntries);
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task UpdateCitizen_WritesBeforeAndAfterAudit() {
        // Updates must persist the new profile while retaining both values in
        // the audit details for accountability.
        var (options, actorId, audit) = await PrepareAsync();
        var nationalId = ServiceTestSupport.UniqueNationalId();

        await using var context = new ArisDbContext(options);
        var service = new CitizenService(
            context,
            audit,
            new CreateCitizenValidator(),
            new UpdateCitizenValidator());

        var create = await service.CreateCitizenAsync(
            new CreateCitizenDto {
                NationalId = nationalId,
                FullName = "Service Citizen Original",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Gender = "OTHER"
            },
            actorId,
            Token);

        Assert.True(create.IsSuccess, string.Join("; ", create.Errors));
        Assert.True(create.Value is int citizenId);

        var update = await service.UpdateCitizenAsync(
            citizenId,
            new UpdateCitizenDto {
                FullName = "Service Citizen Updated",
                DateOfBirth = new DateOnly(1991, 2, 2),
                Gender = "FEMALE"
            },
            actorId,
            Token);

        Assert.True(update.IsSuccess, string.Join("; ", update.Errors));

        await using var read = new ArisDbContext(options);
        var citizen = await read.Citizens
            .AsNoTracking()
            .SingleAsync(c => c.CitizenId == citizenId, Token);

        Assert.Equal("Service Citizen Updated", citizen.FullName);
        Assert.Equal(new DateOnly(1991, 2, 2), citizen.DateOfBirth);
        Assert.Equal("FEMALE", citizen.Gender);

        var updateEntry = audit.SuccessEntries
            .Single(e => e.Action == "UPDATE_CITIZEN");
        var details = updateEntry.DetailsJson!;

        Assert.Contains("Service Citizen Original", details, StringComparison.Ordinal);
        Assert.Contains("Service Citizen Updated", details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeactivateUnassignedCitizen_SetsInactive_AndWritesAudit() {
        // An unassigned citizen can be deactivated without a successor; the
        // service should set the terminal state and timestamp atomically.
        var (options, actorId, audit) = await PrepareAsync();
        var nationalId = ServiceTestSupport.UniqueNationalId();

        await using var context = new ArisDbContext(options);
        var service = new CitizenService(
            context,
            audit,
            new CreateCitizenValidator(),
            new UpdateCitizenValidator());

        var create = await service.CreateCitizenAsync(
            new CreateCitizenDto {
                NationalId = nationalId,
                FullName = "Service Citizen To Deactivate",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Gender = "OTHER"
            },
            actorId,
            Token);

        Assert.True(create.IsSuccess, string.Join("; ", create.Errors));
        Assert.True(create.Value is int citizenId);

        var deactivate = await service.DeactivateCitizenAsync(
            citizenId,
            actorId,
            successorHeadCitizenId: null,
            effectiveDate: new DateOnly(2020, 1, 2),
            Token);

        Assert.True(deactivate.IsSuccess, string.Join("; ", deactivate.Errors));

        await using var read = new ArisDbContext(options);
        var citizen = await read.Citizens
            .AsNoTracking()
            .SingleAsync(c => c.CitizenId == citizenId, Token);

        Assert.Equal("INACTIVE", citizen.Status);
        Assert.NotNull(citizen.DeactivatedAt);

        var entry = audit.SuccessEntries
            .Single(e => e.Action == "DEACTIVATE_CITIZEN");
        Assert.Equal(actorId, entry.ActorUserId);
        Assert.Equal(citizenId, entry.EntityId);
    }

    [Fact]
    public async Task CreateCitizen_WithLockedActor_ReturnsFailure_AndWritesNoAudit() {
        // A locked operator cannot create citizens, and authorization must
        // prevent both persistence and audit creation.
        var (options, actorId, audit) = await PrepareAsync(LockedActor, "LOCKED");
        var nationalId = ServiceTestSupport.UniqueNationalId();

        await using var context = new ArisDbContext(options);
        var service = new CitizenService(
            context,
            audit,
            new CreateCitizenValidator(),
            new UpdateCitizenValidator());

        var result = await service.CreateCitizenAsync(
            new CreateCitizenDto {
                NationalId = nationalId,
                FullName = "Service Citizen Locked Actor",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Gender = "OTHER"
            },
            actorId,
            Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "not active",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(audit.SuccessEntries);
        Assert.Empty(audit.FailureEntries);

        await using var read = new ArisDbContext(options);
        Assert.False(await read.Citizens.AnyAsync(
            c => c.NationalId == nationalId, Token));
    }

    private static async Task<(
        DbContextOptions<ArisDbContext> Options,
        int ActorId,
        RecordingAuditService Audit)> PrepareAsync(
            string actorUsername = ActiveActor,
            string actorStatus = "ACTIVE") {
        // Skip this suite when SQL Server is unavailable, then normalize the
        // named actor to the requested role/status for a deterministic fixture.
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        var connectionString = TestDatabase.ConnectionString!;
        await ServiceTestSupport.EnsureSchemaAsync(connectionString, Token);

        var options = ServiceTestSupport.Options(connectionString);

        await using var setup = new ArisDbContext(options);
        var actorId = await ServiceTestSupport.EnsureUserAsync(
            setup, actorUsername, "OFFICER", actorStatus, Token);

        return (options, actorId, new RecordingAuditService());
    }
}