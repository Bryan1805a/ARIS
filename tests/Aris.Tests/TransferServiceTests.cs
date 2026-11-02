using Aris.Application.Households.Dtos;
using Aris.Application.Households.Validators;
using Aris.Application.Transfers.Dtos;
using Aris.Application.Transfers.Validators;
using Aris.Infrastructure.Households;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Transfers;
using Aris.Domain.Entities;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Service-level tests for <c>UC-06</c> against real SQL Server: the pending phase,
/// maker-checker rejection, and a full <c>JOIN_EXISTING</c> approval.
/// </summary>
public sealed class TransferServiceTests {
    private const string MakerUsername = "it-service-transfer-maker";
    private const string CheckerUsername = "it-service-transfer-checker";

    private static readonly DateOnly RegisteredOn = new(2020, 1, 1);
    private static readonly DateOnly MemberAddedOn = new(2020, 1, 2);
    private static readonly DateOnly TransferEffectiveOn = new(2020, 1, 5);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateRequest_LeavesTenuresActive_AndWritesPendingRows() {
        var (options, makerId, _, audit) = await PrepareAsync();
        var fixture = await CreateFixtureAsync(options, makerId, audit);

        await using var context = new ArisDbContext(options);
        var service = CreateService(context, audit);

        var create = await service.CreateRequestAsync(
            BuildJoinExistingDto(fixture),
            makerId,
            Token);

        Assert.True(create.IsSuccess, string.Join("; ", create.Errors));
        Assert.True(create.Value is int requestId, "Expected the new request id.");

        await using var read = new ArisDbContext(options);

        var request = await read.TransferRequests
            .AsNoTracking()
            .SingleAsync(r => r.RequestId == requestId, Token);

        Assert.Equal("JOIN_EXISTING", request.RequestType);
        Assert.Equal("PENDING", request.Status);
        Assert.Equal(makerId, request.RequestedBy);
        Assert.Equal(fixture.SourceHouseholdId, request.SourceHouseholdId);
        Assert.Equal(fixture.DestinationHouseholdId, request.DestinationHouseholdId);
        Assert.Null(request.DecidedBy);
        Assert.Null(request.DecidedAt);

        var item = await read.TransferRequestItems
            .AsNoTracking()
            .SingleAsync(i => i.RequestId == requestId, Token);

        Assert.Equal(fixture.MemberCitizenId, item.CitizenId);
        Assert.Equal("PENDING", item.Status);

        // Phase 1 must not move anything.
        var membership = await read.HouseholdMemberships
            .AsNoTracking()
            .SingleAsync(m => m.CitizenId == fixture.MemberCitizenId
                && m.Status == "ACTIVE", Token);

        Assert.Equal(fixture.SourceHouseholdId, membership.HouseholdId);
        Assert.Equal("MEMBER", membership.Role);

        var residence = await read.Residences
            .AsNoTracking()
            .SingleAsync(r => r.CitizenId == fixture.MemberCitizenId
                && r.Status == "ACTIVE", Token);

        Assert.Equal(fixture.SourceAddressId, residence.AddressId);

        var entry = audit.SuccessEntries
            .Single(e => e.Action == "CREATE_TRANSFER_REQUEST");

        Assert.Equal(makerId, entry.ActorUserId);
        Assert.Equal("TransferRequest", entry.EntityType);
        Assert.Equal(requestId, entry.EntityId);

        // Leave no open item behind for the unique pending-item index.
        var cancel = await service.CancelAsync(
            requestId, makerId, "test cleanup", Token);
        Assert.True(cancel.IsSuccess, string.Join("; ", cancel.Errors));
    }

    [Fact]
    public async Task Maker_Cannot_Approve_Own_Request() {
        var (options, makerId, _, audit) = await PrepareAsync();
        var fixture = await CreateFixtureAsync(options, makerId, audit);

        await using var context = new ArisDbContext(options);
        var service = CreateService(context, audit);

        var create = await service.CreateRequestAsync(
            BuildJoinExistingDto(fixture),
            makerId,
            Token);

        Assert.True(create.IsSuccess, string.Join("; ", create.Errors));
        Assert.True(create.Value is int requestId);

        int itemId;

        await using (var read = new ArisDbContext(options)) {
            itemId = await read.TransferRequestItems
                .AsNoTracking()
                .Where(i => i.RequestId == requestId)
                .Select(i => i.ItemId)
                .SingleAsync(Token);
        }

        var decision = new TransferDecisionDto {
            RequestId = requestId,
            Items = new[]
            {
                new TransferItemDecisionDto
                {
                    ItemId = itemId,
                    Approve = true
                }
            },
            DecisionNote = "self-approval attempt"
        };

        var decide = await service.DecideAsync(decision, makerId, Token);

        Assert.True(decide.IsFailure);
        Assert.Contains(
            "different officer",
            string.Join(" ", decide.Errors),
            StringComparison.OrdinalIgnoreCase);

        await using var verify = new ArisDbContext(options);

        var request = await verify.TransferRequests
            .AsNoTracking()
            .SingleAsync(r => r.RequestId == requestId, Token);
        Assert.Equal("PENDING", request.Status);
        Assert.Null(request.DecidedBy);

        var item = await verify.TransferRequestItems
            .AsNoTracking()
            .SingleAsync(i => i.ItemId == itemId, Token);
        Assert.Equal("PENDING", item.Status);

        Assert.DoesNotContain(
            audit.SuccessEntries,
            e => e.Action == "EXECUTE_TRANSFER");

        var cancel = await service.CancelAsync(
            requestId, makerId, "test cleanup", Token);
        Assert.True(cancel.IsSuccess, string.Join("; ", cancel.Errors));
    }

    [Fact]
    public async Task Checker_Approves_JoinExisting_MovesCitizen() {
        var (options, makerId, checkerId, audit) = await PrepareAsync();
        var fixture = await CreateFixtureAsync(options, makerId, audit);

        await using var context = new ArisDbContext(options);
        var service = CreateService(context, audit);

        var create = await service.CreateRequestAsync(
            BuildJoinExistingDto(fixture),
            makerId,
            Token);

        Assert.True(create.IsSuccess, string.Join("; ", create.Errors));
        Assert.True(create.Value is int requestId);

        int itemId;

        await using (var read = new ArisDbContext(options)) {
            itemId = await read.TransferRequestItems
                .AsNoTracking()
                .Where(i => i.RequestId == requestId)
                .Select(i => i.ItemId)
                .SingleAsync(Token);
        }

        var decision = new TransferDecisionDto {
            RequestId = requestId,
            Items = new[]
            {
                new TransferItemDecisionDto
                {
                    ItemId = itemId,
                    Approve = true
                }
            },
            DecisionNote = "approved by checker"
        };

        var decide = await service.DecideAsync(decision, checkerId, Token);

        Assert.True(decide.IsSuccess, string.Join("; ", decide.Errors));

        await using var verify = new ArisDbContext(options);

        var request = await verify.TransferRequests
            .AsNoTracking()
            .SingleAsync(r => r.RequestId == requestId, Token);

        Assert.Equal("APPROVED", request.Status);
        Assert.Equal(checkerId, request.DecidedBy);
        Assert.NotNull(request.DecidedAt);
        Assert.Equal(fixture.DestinationHouseholdId, request.ResultHouseholdId);

        var item = await verify.TransferRequestItems
            .AsNoTracking()
            .SingleAsync(i => i.ItemId == itemId, Token);
        Assert.Equal("APPROVED", item.Status);

        var memberships = await verify.HouseholdMemberships
            .AsNoTracking()
            .Where(m => m.CitizenId == fixture.MemberCitizenId)
            .OrderBy(m => m.MembershipId)
            .ToListAsync(Token);

        Assert.Equal(2, memberships.Count);

        var oldMembership = memberships.Single(m => m.Status == "ENDED");
        Assert.Equal(fixture.SourceHouseholdId, oldMembership.HouseholdId);
        Assert.Equal("MEMBER", oldMembership.Role);
        Assert.Equal(TransferEffectiveOn.AddDays(-1), oldMembership.EndDate);
        Assert.Equal(checkerId, oldMembership.EndedBy);

        var newMembership = memberships.Single(m => m.Status == "ACTIVE");
        Assert.Equal(fixture.DestinationHouseholdId, newMembership.HouseholdId);
        Assert.Equal("MEMBER", newMembership.Role);
        Assert.Equal(TransferEffectiveOn, newMembership.StartDate);
        Assert.Equal(requestId, newMembership.OpenedByRequestId);
        Assert.Equal(checkerId, newMembership.CreatedBy);

        var residences = await verify.Residences
            .AsNoTracking()
            .Where(r => r.CitizenId == fixture.MemberCitizenId)
            .OrderBy(r => r.ResidenceId)
            .ToListAsync(Token);

        Assert.Equal(2, residences.Count);

        var oldResidence = residences.Single(r => r.Status == "ENDED");
        Assert.Equal(fixture.SourceAddressId, oldResidence.AddressId);
        Assert.Equal(TransferEffectiveOn.AddDays(-1), oldResidence.EndDate);

        var newResidence = residences.Single(r => r.Status == "ACTIVE");
        Assert.Equal(fixture.DestinationAddressId, newResidence.AddressId);
        Assert.Equal(TransferEffectiveOn, newResidence.StartDate);
        Assert.Equal(requestId, newResidence.OpenedByRequestId);

        var citizen = await verify.Citizens
            .AsNoTracking()
            .SingleAsync(c => c.CitizenId == fixture.MemberCitizenId, Token);
        Assert.Equal("ACTIVE", citizen.Status);

        // Both heads remain active: the source head stayed, and the destination head
        // did not change when a MEMBER joined.
        var sourceHead = await verify.HouseholdMemberships
            .AsNoTracking()
            .SingleAsync(m => m.HouseholdId == fixture.SourceHouseholdId
                && m.CitizenId == fixture.SourceHeadCitizenId
                && m.Status == "ACTIVE", Token);
        Assert.Equal("HEAD", sourceHead.Role);

        var destinationHead = await verify.HouseholdMemberships
            .AsNoTracking()
            .SingleAsync(m => m.HouseholdId == fixture.DestinationHouseholdId
                && m.CitizenId == fixture.DestinationHeadCitizenId
                && m.Status == "ACTIVE", Token);
        Assert.Equal("HEAD", destinationHead.Role);

        Assert.Contains(
            audit.SuccessEntries,
            e => e.Action == "CREATE_TRANSFER_REQUEST" && e.EntityId == requestId);
        Assert.Contains(
            audit.SuccessEntries,
            e => e.Action == "EXECUTE_TRANSFER" && e.EntityId == requestId);
    }

    private sealed record TransferFixture(
        int SourceHouseholdId,
        int DestinationHouseholdId,
        int SourceAddressId,
        int DestinationAddressId,
        int SourceHeadCitizenId,
        int MemberCitizenId,
        int DestinationHeadCitizenId);

    private static CreateTransferRequestDto BuildJoinExistingDto(
        TransferFixture fixture) {
        return new CreateTransferRequestDto {
            RequestType = "JOIN_EXISTING",
            SourceHouseholdId = fixture.SourceHouseholdId,
            CitizenIds = new[] { fixture.MemberCitizenId },
            DestinationHouseholdId = fixture.DestinationHouseholdId,
            EffectiveDate = TransferEffectiveOn
        };
    }

    private static TransferService CreateService(
        ArisDbContext context,
        RecordingAuditService audit) {
        return new TransferService(
            context,
            audit,
            new CreateTransferRequestValidator(),
            new TransferDecisionValidator());
    }

    private static async Task<(
        DbContextOptions<ArisDbContext> Options,
        int MakerId,
        int CheckerId,
        RecordingAuditService Audit)> PrepareAsync() {
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        var connectionString = TestDatabase.ConnectionString!;
        await ServiceTestSupport.EnsureSchemaAsync(connectionString, Token);

        var options = ServiceTestSupport.Options(connectionString);

        await using var setup = new ArisDbContext(options);

        var makerId = await ServiceTestSupport.EnsureUserAsync(
            setup, MakerUsername, "OFFICER", "ACTIVE", Token);
        var checkerId = await ServiceTestSupport.EnsureUserAsync(
            setup, CheckerUsername, "OFFICER", "ACTIVE", Token);

        return (options, makerId, checkerId, new RecordingAuditService());
    }

    private static async Task<TransferFixture> CreateFixtureAsync(
        DbContextOptions<ArisDbContext> options,
        int actorId,
        RecordingAuditService audit) {
        await using var context = new ArisDbContext(options);

        var householdService = new HouseholdService(
            context,
            audit,
            new RegisterHouseholdValidator(),
            new AddMemberValidator(),
            new ChangeHeadValidator());

        var sourceAddress = await ServiceTestSupport.CreateAddressAsync(
            context, "1 Transfer Source Street", Token);
        var destinationAddress = await ServiceTestSupport.CreateAddressAsync(
            context, "2 Transfer Destination Street", Token);

        var sourceHead = await ServiceTestSupport.CreateCitizenAsync(
            context, "Transfer Source Head", Token);
        var sourceMember = await ServiceTestSupport.CreateCitizenAsync(
            context, "Transfer Source Member", Token);
        var destinationHead = await ServiceTestSupport.CreateCitizenAsync(
            context, "Transfer Destination Head", Token);

        var sourceRegister = await householdService.RegisterHouseholdAsync(
            new RegisterHouseholdDto {
                HeadCitizenId = sourceHead.CitizenId,
                AddressId = sourceAddress.AddressId,
                EffectiveDate = RegisteredOn
            },
            actorId,
            Token);

        Assert.True(sourceRegister.IsSuccess, string.Join("; ", sourceRegister.Errors));
        Assert.True(sourceRegister.Value is int sourceHouseholdId);

        var addMember = await householdService.AddMemberAsync(
            new AddMemberDto {
                HouseholdId = sourceHouseholdId,
                CitizenId = sourceMember.CitizenId,
                EffectiveDate = MemberAddedOn
            },
            actorId,
            Token);

        Assert.True(addMember.IsSuccess, string.Join("; ", addMember.Errors));

        var destinationRegister = await householdService.RegisterHouseholdAsync(
            new RegisterHouseholdDto {
                HeadCitizenId = destinationHead.CitizenId,
                AddressId = destinationAddress.AddressId,
                EffectiveDate = RegisteredOn
            },
            actorId,
            Token);

        Assert.True(
            destinationRegister.IsSuccess,
            string.Join("; ", destinationRegister.Errors));
        Assert.True(destinationRegister.Value is int destinationHouseholdId);

        return new TransferFixture(
            sourceHouseholdId,
            destinationHouseholdId,
            sourceAddress.AddressId,
            destinationAddress.AddressId,
            sourceHead.CitizenId,
            sourceMember.CitizenId,
            destinationHead.CitizenId);
    }
}