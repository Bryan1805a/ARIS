using Aris.Application.Addresses.Dtos;
using Aris.Application.Addresses.Validators;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Citizens.Validators;
using Aris.Application.Households.Dtos;
using Aris.Application.Households.Validators;
using Aris.Application.Rectifications.Dtos;
using Aris.Application.Rectifications.Validators;
using Aris.Application.Transfers.Dtos;
using Aris.Application.Transfers.Validators;
using Aris.Infrastructure.Addresses;
using Aris.Infrastructure.Auditing;
using Aris.Infrastructure.Citizens;
using Aris.Infrastructure.Households;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Rectifications;
using Aris.Infrastructure.Resilience;
using Aris.Infrastructure.Transfers;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Committed end-to-end harness for the main residence workflow:
/// citizen → household → maker-checker transfer → historical rectification → audit.
/// </summary>
/// <remarks>
/// Uses the real services and the real <c>AuditService</c>, so the audit rows persist.
/// The data is invariant-safe and intentionally left behind; names and usernames are
/// identifiable by the <c>E2E</c> / <c>it-e2e-</c> prefixes.
/// </remarks>
public sealed class EndToEndWorkflowTests {
    private const string MakerUsername = "it-e2e-maker";
    private const string CheckerUsername = "it-e2e-checker";

    private static readonly DateOnly RegisteredOn = new(2020, 1, 1);
    private static readonly DateOnly MemberAddedOn = new(2020, 1, 2);
    private static readonly DateOnly TransferEffectiveOn = new(2020, 1, 5);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FullResidenceWorkflow_RegistersTransfersRectifies_AndAudits() {
        var (options, makerId, checkerId, connectionString) = await PrepareAsync();

        // ---------------------------------------------------------------- addresses
        int sourceAddressId;
        int destinationAddressId;

        await using (var context = new ArisDbContext(options)) {
            var audit = new AuditService(context);
            var service = new AddressService(
                context,
                audit,
                new CreateAddressValidator(),
                new RetireAddressValidator());

            var source = await service.CreateAsync(
                new CreateAddressDto {
                    AddressCode = ServiceTestSupport.Code("E2E-S"),
                    AddressLine = "1 End-to-End Source Street",
                    Ward = "Ward 1",
                    District = "District 1",
                    Province = "Test Province",
                    ValidFrom = RegisteredOn
                },
                makerId,
                Token);

            Assert.True(source.IsSuccess, string.Join("; ", source.Errors));
            Assert.True(source.Value is int sourceId);
            sourceAddressId = sourceId;

            var destination = await service.CreateAsync(
                new CreateAddressDto {
                    AddressCode = ServiceTestSupport.Code("E2E-D"),
                    AddressLine = "2 End-to-End Destination Street",
                    Ward = "Ward 2",
                    District = "District 2",
                    Province = "Test Province",
                    ValidFrom = RegisteredOn
                },
                makerId,
                Token);

            Assert.True(destination.IsSuccess, string.Join("; ", destination.Errors));
            Assert.True(destination.Value is int destinationId);
            destinationAddressId = destinationId;
        }

        // ----------------------------------------------------------------- citizens
        int sourceHeadId;
        int movingMemberId;
        int destinationHeadId;

        await using (var context = new ArisDbContext(options)) {
            var audit = new AuditService(context);
            var service = new CitizenService(
                context,
                audit,
                new CreateCitizenValidator(),
                new UpdateCitizenValidator());

            var head = await service.CreateCitizenAsync(
                new CreateCitizenDto {
                    NationalId = ServiceTestSupport.UniqueNationalId(),
                    FullName = "E2E Source Head",
                    DateOfBirth = new DateOnly(1990, 1, 1),
                    Gender = "OTHER"
                },
                makerId,
                Token);

            Assert.True(head.IsSuccess, string.Join("; ", head.Errors));
            Assert.True(head.Value is int headId);
            sourceHeadId = headId;

            var member = await service.CreateCitizenAsync(
                new CreateCitizenDto {
                    NationalId = ServiceTestSupport.UniqueNationalId(),
                    FullName = "E2E Moving Member",
                    DateOfBirth = new DateOnly(1991, 1, 1),
                    Gender = "OTHER"
                },
                makerId,
                Token);

            Assert.True(member.IsSuccess, string.Join("; ", member.Errors));
            Assert.True(member.Value is int memberId);
            movingMemberId = memberId;

            var destinationHead = await service.CreateCitizenAsync(
                new CreateCitizenDto {
                    NationalId = ServiceTestSupport.UniqueNationalId(),
                    FullName = "E2E Destination Head",
                    DateOfBirth = new DateOnly(1992, 1, 1),
                    Gender = "OTHER"
                },
                makerId,
                Token);

            Assert.True(
                destinationHead.IsSuccess,
                string.Join("; ", destinationHead.Errors));
            Assert.True(destinationHead.Value is int destinationHeadIdValue);
            destinationHeadId = destinationHeadIdValue;
        }

        // -------------------------------------------------------------- households
        int sourceHouseholdId;
        int destinationHouseholdId;

        await using (var context = new ArisDbContext(options)) {
            var audit = new AuditService(context);
            var service = new HouseholdService(
                context,
                audit,
                new RegisterHouseholdValidator(),
                new AddMemberValidator(),
                new ChangeHeadValidator());

            var source = await service.RegisterHouseholdAsync(
                new RegisterHouseholdDto {
                    HeadCitizenId = sourceHeadId,
                    AddressId = sourceAddressId,
                    EffectiveDate = RegisteredOn
                },
                makerId,
                Token);

            Assert.True(source.IsSuccess, string.Join("; ", source.Errors));
            Assert.True(source.Value is int sourceId);
            sourceHouseholdId = sourceId;

            var addMember = await service.AddMemberAsync(
                new AddMemberDto {
                    HouseholdId = sourceHouseholdId,
                    CitizenId = movingMemberId,
                    EffectiveDate = MemberAddedOn
                },
                makerId,
                Token);

            Assert.True(addMember.IsSuccess, string.Join("; ", addMember.Errors));

            var destination = await service.RegisterHouseholdAsync(
                new RegisterHouseholdDto {
                    HeadCitizenId = destinationHeadId,
                    AddressId = destinationAddressId,
                    EffectiveDate = RegisteredOn
                },
                makerId,
                Token);

            Assert.True(destination.IsSuccess, string.Join("; ", destination.Errors));
            Assert.True(destination.Value is int destinationId);
            destinationHouseholdId = destinationId;
        }

        // -------------------------------------------------------------- transfer
        int requestId;

        await using (var context = new ArisDbContext(options)) {
            var audit = new AuditService(context);
            var service = new TransferService(
                context,
                audit,
                new CreateTransferRequestValidator(),
                new TransferDecisionValidator());

            var create = await service.CreateRequestAsync(
                new CreateTransferRequestDto {
                    RequestType = "JOIN_EXISTING",
                    SourceHouseholdId = sourceHouseholdId,
                    CitizenIds = new[] { movingMemberId },
                    DestinationHouseholdId = destinationHouseholdId,
                    EffectiveDate = TransferEffectiveOn
                },
                makerId,
                Token);

            Assert.True(create.IsSuccess, string.Join("; ", create.Errors));
            Assert.True(create.Value is int requestIdValue);
            requestId = requestIdValue;
        }

        int itemId;

        await using (var read = new ArisDbContext(options)) {
            itemId = await read.TransferRequestItems
                .AsNoTracking()
                .Where(i => i.RequestId == requestId)
                .Select(i => i.ItemId)
                .SingleAsync(Token);
        }

        await using (var context = new ArisDbContext(options)) {
            var audit = new AuditService(context);
            var service = new TransferService(
                context,
                audit,
                new CreateTransferRequestValidator(),
                new TransferDecisionValidator());

            var decide = await service.DecideAsync(
                new TransferDecisionDto {
                    RequestId = requestId,
                    Items = new[]
                    {
                        new TransferItemDecisionDto
                        {
                            ItemId = itemId,
                            Approve = true
                        }
                    },
                    DecisionNote = "end-to-end approval"
                },
                checkerId,
                Token);

            Assert.True(decide.IsSuccess, string.Join("; ", decide.Errors));
        }

        // ---------------------------------------------------------- rectify history
        int endedResidenceId;
        int correctedResidenceId;
        DateOnly endedStart;
        DateOnly endedEnd;

        await using (var read = new ArisDbContext(options)) {
            var ended = await read.Residences
                .AsNoTracking()
                .SingleAsync(r => r.CitizenId == movingMemberId
                    && r.Status == "ENDED", Token);

            endedResidenceId = ended.ResidenceId;
            endedStart = ended.StartDate;
            endedEnd = ended.EndDate!.Value;
        }

        await using (var context = new ArisDbContext(options)) {
            var audit = new AuditService(context);
            var service = new RectificationService(
                context,
                audit,
                new RectifyResidenceValidator(),
                new RectifyMembershipValidator(),
                new RectifyHouseholdAddressValidator());

            var rectify = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = endedResidenceId,
                    AddressId = destinationAddressId,
                    StartDate = endedStart,
                    EndDate = endedEnd,
                    Reason = "E2E: the historical source address was recorded incorrectly."
                },
                checkerId,
                Token);

            Assert.True(rectify.IsSuccess, string.Join("; ", rectify.Errors));
            Assert.True(rectify.Value is int correctedId);
            correctedResidenceId = correctedId;
        }

        // -------------------------------------------------------------- verify
        await using (var verify = new ArisDbContext(options)) {
            var oldResidence = await verify.Residences
                .AsNoTracking()
                .SingleAsync(r => r.ResidenceId == endedResidenceId, Token);
            Assert.Equal("VOID", oldResidence.Status);

            var corrected = await verify.Residences
                .AsNoTracking()
                .SingleAsync(r => r.ResidenceId == correctedResidenceId, Token);
            Assert.Equal("ENDED", corrected.Status);
            Assert.Equal(destinationAddressId, corrected.AddressId);
            Assert.Equal(endedStart, corrected.StartDate);
            Assert.Equal(endedEnd, corrected.EndDate);
            Assert.Equal(endedResidenceId, corrected.CorrectsResidenceId);

            var activeResidence = await verify.Residences
                .AsNoTracking()
                .SingleAsync(r => r.CitizenId == movingMemberId
                    && r.Status == "ACTIVE", Token);
            Assert.Equal(destinationAddressId, activeResidence.AddressId);
            Assert.Equal(TransferEffectiveOn, activeResidence.StartDate);
            Assert.Equal(requestId, activeResidence.OpenedByRequestId);

            var request = await verify.TransferRequests
                .AsNoTracking()
                .SingleAsync(r => r.RequestId == requestId, Token);
            Assert.Equal("APPROVED", request.Status);
            Assert.Equal(makerId, request.RequestedBy);
            Assert.Equal(checkerId, request.DecidedBy);
            Assert.Equal(destinationHouseholdId, request.ResultHouseholdId);

            var auditActions = await verify.AuditLogs
                .AsNoTracking()
                .Where(a => a.ActorUserId == makerId || a.ActorUserId == checkerId)
                .Select(a => a.Action)
                .ToListAsync(Token);

            Assert.Contains("CREATE_ADDRESS", auditActions);
            Assert.Contains("CREATE_CITIZEN", auditActions);
            Assert.Contains("REGISTER_HOUSEHOLD", auditActions);
            Assert.Contains("ADD_HOUSEHOLD_MEMBER", auditActions);
            Assert.Contains("CREATE_TRANSFER_REQUEST", auditActions);
            Assert.Contains("EXECUTE_TRANSFER", auditActions);
            Assert.Contains("VOID_RESIDENCE", auditActions);
            Assert.Contains("CORRECT_RESIDENCE", auditActions);

            var executeAudit = await verify.AuditLogs
                .AsNoTracking()
                .CountAsync(a => a.Action == "EXECUTE_TRANSFER"
                    && a.EntityId == requestId, Token);
            Assert.True(executeAudit >= 1);

            var voidAudit = await verify.AuditLogs
                .AsNoTracking()
                .CountAsync(a => a.Action == "VOID_RESIDENCE"
                    && a.EntityId == endedResidenceId, Token);
            Assert.True(voidAudit >= 1);

            Assert.Equal(
                0,
                await CountInvariantViolationsAsync(connectionString, Token));
        }
    }

    private static async Task<(
        DbContextOptions<ArisDbContext> Options,
        int MakerId,
        int CheckerId,
        string ConnectionString)> PrepareAsync() {
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

        return (options, makerId, checkerId, connectionString);
    }

    private static async Task<int> CountInvariantViolationsAsync(
        string connectionString,
        CancellationToken ct) {
        await using var connection = await SqlResilience.OpenAsync(
            connectionString, cancellationToken: ct);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM dbo.vw_GoldenInvariantViolations;";

        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }
}