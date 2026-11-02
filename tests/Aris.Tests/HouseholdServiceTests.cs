using Aris.Application.Households.Dtos;
using Aris.Application.Households.Validators;
using Aris.Domain.Entities;
using Aris.Infrastructure.Households;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Service-level tests for the household slice (<c>HH-01</c>, <c>HH-02</c>, <c>HH-04</c>)
/// against real SQL Server, with a recording audit double.
/// </summary>
public sealed class HouseholdServiceTests {
    private const string ActorUsername = "it-service-household-actor";

    private static readonly DateOnly RegisteredOn = new(2020, 1, 1);
    private static readonly DateOnly MemberAddedOn = new(2020, 1, 2);
    private static readonly DateOnly HeadChangedOn = new(2020, 1, 3);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RegisterHousehold_CreatesAllRows_AndActivatesHead() {
        var (options, actorId, audit) = await PrepareAsync();
        var fixture = await CreateFixtureAsync(options);
        var head = fixture.Head;
        var address = fixture.Address;

        await using var context = new ArisDbContext(options);
        var service = CreateService(context, audit);

        var result = await service.RegisterHouseholdAsync(
            new RegisterHouseholdDto {
                HeadCitizenId = head.CitizenId,
                AddressId = address.AddressId,
                EffectiveDate = RegisteredOn
            },
            actorId,
            Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.True(result.Value is int householdId, "Expected the new household id.");

        await using var read = new ArisDbContext(options);

        var household = await read.Households
            .AsNoTracking()
            .SingleAsync(h => h.HouseholdId == householdId, Token);
        Assert.Equal("ACTIVE", household.Status);

        var householdAddress = await read.HouseholdAddresses
            .AsNoTracking()
            .SingleAsync(a => a.HouseholdId == householdId, Token);
        Assert.Equal(address.AddressId, householdAddress.AddressId);
        Assert.Equal(RegisteredOn, householdAddress.StartDate);
        Assert.Equal("ACTIVE", householdAddress.Status);
        Assert.Null(householdAddress.EndDate);

        var membership = await read.HouseholdMemberships
            .AsNoTracking()
            .SingleAsync(m => m.HouseholdId == householdId, Token);
        Assert.Equal(head.CitizenId, membership.CitizenId);
        Assert.Equal("HEAD", membership.Role);
        Assert.Equal("ACTIVE", membership.Status);
        Assert.Equal(RegisteredOn, membership.StartDate);

        var residence = await read.Residences
            .AsNoTracking()
            .SingleAsync(r => r.CitizenId == head.CitizenId, Token);
        Assert.Equal(address.AddressId, residence.AddressId);
        Assert.Equal(RegisteredOn, residence.StartDate);
        Assert.Equal("ACTIVE", residence.Status);

        var citizen = await read.Citizens
            .AsNoTracking()
            .SingleAsync(c => c.CitizenId == head.CitizenId, Token);
        Assert.Equal("ACTIVE", citizen.Status);

        var entry = Assert.Single(audit.SuccessEntries);
        Assert.Equal("REGISTER_HOUSEHOLD", entry.Action);
        Assert.Equal(actorId, entry.ActorUserId);
        Assert.Equal("Household", entry.EntityType);
        Assert.Equal(householdId, entry.EntityId);
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task AddMember_CreatesMembershipAndResidence_AndActivatesCitizen() {
        var (options, actorId, audit) = await PrepareAsync();
        var fixture = await CreateFixtureAsync(options);

        await using var context = new ArisDbContext(options);
        var service = CreateService(context, audit);

        var register = await service.RegisterHouseholdAsync(
            new RegisterHouseholdDto {
                HeadCitizenId = fixture.Head.CitizenId,
                AddressId = fixture.Address.AddressId,
                EffectiveDate = RegisteredOn
            },
            actorId,
            Token);

        Assert.True(register.IsSuccess, string.Join("; ", register.Errors));
        Assert.True(register.Value is int householdId);

        var add = await service.AddMemberAsync(
            new AddMemberDto {
                HouseholdId = householdId,
                CitizenId = fixture.Member.CitizenId,
                EffectiveDate = MemberAddedOn
            },
            actorId,
            Token);

        Assert.True(add.IsSuccess, string.Join("; ", add.Errors));

        await using var read = new ArisDbContext(options);

        var membership = await read.HouseholdMemberships
            .AsNoTracking()
            .SingleAsync(m => m.CitizenId == fixture.Member.CitizenId, Token);
        Assert.Equal(householdId, membership.HouseholdId);
        Assert.Equal("MEMBER", membership.Role);
        Assert.Equal("ACTIVE", membership.Status);
        Assert.Equal(MemberAddedOn, membership.StartDate);

        var residence = await read.Residences
            .AsNoTracking()
            .SingleAsync(r => r.CitizenId == fixture.Member.CitizenId, Token);
        Assert.Equal(fixture.Address.AddressId, residence.AddressId);
        Assert.Equal(MemberAddedOn, residence.StartDate);
        Assert.Equal("ACTIVE", residence.Status);

        var citizen = await read.Citizens
            .AsNoTracking()
            .SingleAsync(c => c.CitizenId == fixture.Member.CitizenId, Token);
        Assert.Equal("ACTIVE", citizen.Status);

        Assert.Equal(2, audit.SuccessEntries.Count);
        var entry = audit.SuccessEntries.Single(e => e.Action == "ADD_HOUSEHOLD_MEMBER");
        Assert.Equal(actorId, entry.ActorUserId);
        Assert.Equal("Household", entry.EntityType);
        Assert.Equal(householdId, entry.EntityId);
        Assert.Contains(
            fixture.Member.CitizenId.ToString(),
            entry.DetailsJson!,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangeHead_ClosesOldRows_AndOpensTwoNewRows() {
        var (options, actorId, audit) = await PrepareAsync();
        var fixture = await CreateFixtureAsync(options);
        var headId = fixture.Head.CitizenId;
        var memberId = fixture.Member.CitizenId;

        await using var context = new ArisDbContext(options);
        var service = CreateService(context, audit);

        var register = await service.RegisterHouseholdAsync(
            new RegisterHouseholdDto {
                HeadCitizenId = headId,
                AddressId = fixture.Address.AddressId,
                EffectiveDate = RegisteredOn
            },
            actorId,
            Token);

        Assert.True(register.IsSuccess, string.Join("; ", register.Errors));
        Assert.True(register.Value is int householdId);

        var add = await service.AddMemberAsync(
            new AddMemberDto {
                HouseholdId = householdId,
                CitizenId = memberId,
                EffectiveDate = MemberAddedOn
            },
            actorId,
            Token);

        Assert.True(add.IsSuccess, string.Join("; ", add.Errors));

        var change = await service.ChangeHeadAsync(
            new ChangeHeadDto {
                HouseholdId = householdId,
                NewHeadCitizenId = memberId,
                EffectiveDate = HeadChangedOn
            },
            actorId,
            Token);

        Assert.True(change.IsSuccess, string.Join("; ", change.Errors));

        await using var read = new ArisDbContext(options);

        var memberships = await read.HouseholdMemberships
            .AsNoTracking()
            .Where(m => m.HouseholdId == householdId)
            .OrderBy(m => m.MembershipId)
            .ToListAsync(Token);

        Assert.Equal(4, memberships.Count);

        var closedAt = HeadChangedOn.AddDays(-1);

        Assert.Contains(
            memberships,
            m => m.CitizenId == headId
                && m.Role == "HEAD"
                && m.Status == "ENDED"
                && m.EndDate == closedAt);

        Assert.Contains(
            memberships,
            m => m.CitizenId == headId
                && m.Role == "MEMBER"
                && m.Status == "ACTIVE"
                && m.StartDate == HeadChangedOn);

        Assert.Contains(
            memberships,
            m => m.CitizenId == memberId
                && m.Role == "MEMBER"
                && m.Status == "ENDED"
                && m.EndDate == closedAt);

        Assert.Contains(
            memberships,
            m => m.CitizenId == memberId
                && m.Role == "HEAD"
                && m.Status == "ACTIVE"
                && m.StartDate == HeadChangedOn);

        // The residences do not change when only the role changes.
        var headResidence = await read.Residences
            .AsNoTracking()
            .SingleAsync(r => r.CitizenId == headId, Token);
        Assert.Equal("ACTIVE", headResidence.Status);

        var memberResidence = await read.Residences
            .AsNoTracking()
            .SingleAsync(r => r.CitizenId == memberId, Token);
        Assert.Equal("ACTIVE", memberResidence.Status);

        Assert.Equal(3, audit.SuccessEntries.Count);
        var entry = audit.SuccessEntries.Single(e => e.Action == "CHANGE_HOUSEHOLD_HEAD");
        Assert.Equal(actorId, entry.ActorUserId);
        Assert.Equal("Household", entry.EntityType);
        Assert.Equal(householdId, entry.EntityId);
        Assert.Contains(headId.ToString(), entry.DetailsJson!, StringComparison.Ordinal);
        Assert.Contains(memberId.ToString(), entry.DetailsJson!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterHousehold_WithAssignedCitizen_ReturnsFailure_AndCreatesNoRows() {
        var (options, actorId, audit) = await PrepareAsync();
        var fixture = await CreateFixtureAsync(options);

        await using var context = new ArisDbContext(options);
        var service = CreateService(context, audit);

        var first = await service.RegisterHouseholdAsync(
            new RegisterHouseholdDto {
                HeadCitizenId = fixture.Head.CitizenId,
                AddressId = fixture.Address.AddressId,
                EffectiveDate = RegisteredOn
            },
            actorId,
            Token);

        Assert.True(first.IsSuccess, string.Join("; ", first.Errors));

        await using var before = new ArisDbContext(options);
        var householdsBefore = await before.Households.CountAsync(Token);

        var second = await service.RegisterHouseholdAsync(
            new RegisterHouseholdDto {
                HeadCitizenId = fixture.Head.CitizenId,
                AddressId = fixture.Address.AddressId,
                EffectiveDate = MemberAddedOn
            },
            actorId,
            Token);

        Assert.True(second.IsFailure);
        Assert.Contains(
            "UNASSIGNED",
            string.Join(" ", second.Errors),
            StringComparison.OrdinalIgnoreCase);

        await using var after = new ArisDbContext(options);
        Assert.Equal(householdsBefore, await after.Households.CountAsync(Token));
        Assert.Single(audit.SuccessEntries);
        Assert.Empty(audit.FailureEntries);
    }

    private static HouseholdService CreateService(
        ArisDbContext context,
        RecordingAuditService audit) {
        return new HouseholdService(
            context,
            audit,
            new RegisterHouseholdValidator(),
            new AddMemberValidator(),
            new ChangeHeadValidator());
    }

    private static async Task<(
        DbContextOptions<ArisDbContext> Options,
        int ActorId,
        RecordingAuditService Audit)> PrepareAsync() {
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        var connectionString = TestDatabase.ConnectionString!;
        await ServiceTestSupport.EnsureSchemaAsync(connectionString, Token);

        var options = ServiceTestSupport.Options(connectionString);

        await using var setup = new ArisDbContext(options);
        var actorId = await ServiceTestSupport.EnsureUserAsync(
            setup, ActorUsername, "OFFICER", "ACTIVE", Token);

        return (options, actorId, new RecordingAuditService());
    }

    private static async Task<(
        Address Address,
        Citizen Head,
        Citizen Member)> CreateFixtureAsync(
            DbContextOptions<ArisDbContext> options) {
        await using var context = new ArisDbContext(options);

        var address = await ServiceTestSupport.CreateAddressAsync(
            context, "1 Household Service Street", Token);
        var head = await ServiceTestSupport.CreateCitizenAsync(
            context, "Household Service Head", Token);
        var member = await ServiceTestSupport.CreateCitizenAsync(
            context, "Household Service Member", Token);

        return (address, head, member);
    }
}