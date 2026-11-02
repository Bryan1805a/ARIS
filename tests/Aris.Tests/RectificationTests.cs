using Aris.Application.Rectifications.Dtos;
using Aris.Application.Rectifications.Validators;
using Aris.Domain.Entities;
using Aris.Infrastructure.Auditing;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Rectifications;
using Aris.Infrastructure.Resilience;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Aris.Tests;

/// <summary>
/// Proves the void-and-correct workflow against real SQL Server (<c>HIST-01</c>):
/// the old row becomes <c>VOID</c>, a linked replacement is inserted, both audit rows
/// share one correlation ID, and the Golden Invariant survives.
/// </summary>
/// <remarks>
/// <para>
/// The service owns its transaction, so the fixture must be committed before the call.
/// Cleanup is best-effort because the test connection may run as <c>aris_officer</c>,
/// which is denied <c>DELETE</c>. Audit rows are append-only and intentionally remain.
/// </para>
/// <para>
/// Every test skips when <see cref="TestDatabase.ConnectionVariable"/> is unset, so a
/// developer without a local database is never blocked.
/// </para>
/// </remarks>
public sealed class RectificationTests {
    private const string OfficerUsername = "it-rectify-officer";

    private static readonly DateOnly EndedStart = new(2020, 1, 1);
    private static readonly DateOnly EndedEnd = new(2020, 12, 31);
    private static readonly DateOnly ActiveStart = new(2021, 1, 1);

    private static int _nationalIdCounter;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RectifyEndedResidence_ReplacesRow_AndWritesCorrelatedAudit() {
        var (connectionString, fixture) = await PrepareAsync();
        try {
            await using var context = CreateContext(connectionString);
            var service = CreateService(context);

            // Correcting an ended row preserves the original record as VOID and
            // creates a linked replacement instead of rewriting history in place.
            var result = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = fixture.EndedResidenceId,
                    AddressId = fixture.OtherAddressId,
                    StartDate = EndedStart,
                    EndDate = EndedEnd,
                    Reason = "The street name was recorded against the wrong address."
                },
                fixture.OfficerId,
                Token);

            Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
            Assert.True(result.Value is int correctedId, "Expected the new residence id.");

            // Use a fresh context so these assertions verify the committed database
            // state rather than entities tracked by the service context.
            await using var read = CreateContext(connectionString);
            var source = await read.Residences
                .AsNoTracking()
                .SingleAsync(r => r.ResidenceId == fixture.EndedResidenceId, Token);
            var corrected = await read.Residences
                .AsNoTracking()
                .SingleAsync(r => r.ResidenceId == correctedId, Token);

            Assert.Equal("VOID", source.Status);
            Assert.Equal(fixture.AddressId, source.AddressId);

            Assert.Equal("ENDED", corrected.Status);
            Assert.Equal(fixture.CitizenId, corrected.CitizenId);
            Assert.Equal(fixture.OtherAddressId, corrected.AddressId);
            Assert.Equal(EndedStart, corrected.StartDate);
            Assert.Equal(EndedEnd, corrected.EndDate);
            Assert.Equal(fixture.EndedResidenceId, corrected.CorrectsResidenceId);
            Assert.Equal(fixture.OfficerId, corrected.CreatedBy);

            await AssertCorrelatedAuditAsync(
                read, "VOID_RESIDENCE", "CORRECT_RESIDENCE",
                fixture.EndedResidenceId, correctedId, Token);
            await AssertInvariantAsync(connectionString);
        } finally {
            await TryCleanupAsync(connectionString, fixture);
        }
    }

    [Fact]
    public async Task RectifyActiveResidence_OnlyStartDateCanChange() {
        var (connectionString, fixture) = await PrepareAsync();
        try {
            await using var context = CreateContext(connectionString);
            var service = CreateService(context);

            // Active records are still part of the current state: their address
            // and end date are immutable, while a recorded start date may be fixed.
            var addressChange = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = fixture.ActiveResidenceId,
                    AddressId = fixture.OtherAddressId,
                    StartDate = ActiveStart,
                    EndDate = null,
                    Reason = "Trying to change the address of current state."
                },
                fixture.OfficerId,
                Token);

            Assert.True(addressChange.IsFailure);
            Assert.Contains("cannot change address", All(addressChange), StringComparison.OrdinalIgnoreCase);

            var endDateChange = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = fixture.ActiveResidenceId,
                    AddressId = fixture.AddressId,
                    StartDate = ActiveStart,
                    EndDate = new DateOnly(2021, 12, 31),
                    Reason = "Trying to end the active residence."
                },
                fixture.OfficerId,
                Token);

            Assert.True(endDateChange.IsFailure);
            Assert.Contains("cannot have an end date", All(endDateChange), StringComparison.OrdinalIgnoreCase);

            var correctedStart = new DateOnly(2021, 2, 1);
            var correction = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = fixture.ActiveResidenceId,
                    AddressId = fixture.AddressId,
                    StartDate = correctedStart,
                    EndDate = null,
                    Reason = "The occupancy start date was recorded one month late."
                },
                fixture.OfficerId,
                Token);

            Assert.True(correction.IsSuccess, string.Join("; ", correction.Errors));
            Assert.True(correction.Value is int correctedId, "Expected the new residence id.");

            // Verify that the successful correction also follows the same
            // void-and-replace model used for ended records.
            await using var read = CreateContext(connectionString);
            var source = await read.Residences
                .AsNoTracking()
                .SingleAsync(r => r.ResidenceId == fixture.ActiveResidenceId, Token);
            var corrected = await read.Residences
                .AsNoTracking()
                .SingleAsync(r => r.ResidenceId == correctedId, Token);

            Assert.Equal("VOID", source.Status);
            Assert.Equal("ACTIVE", corrected.Status);
            Assert.Equal(correctedStart, corrected.StartDate);
            Assert.Null(corrected.EndDate);
            Assert.Equal(fixture.AddressId, corrected.AddressId);
            Assert.Equal(fixture.ActiveResidenceId, corrected.CorrectsResidenceId);
            Assert.Equal(fixture.CitizenId, corrected.CitizenId);

            await AssertInvariantAsync(connectionString);
        } finally {
            await TryCleanupAsync(connectionString, fixture);
        }
    }

    [Fact]
    public async Task RectifyResidence_RejectsOverlap_AndLeavesSourceUntouched() {
        var (connectionString, fixture) = await PrepareAsync();
        try {
            await using var context = CreateContext(connectionString);
            var service = CreateService(context);

            // Extending the ended period into an existing active period would
            // create overlapping history, so the operation must be rejected.
            var result = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = fixture.EndedResidenceId,
                    AddressId = fixture.AddressId,
                    StartDate = EndedStart,
                    EndDate = new DateOnly(2021, 6, 30),
                    Reason = "Extending the ended period into the active period."
                },
                fixture.OfficerId,
                Token);

            Assert.True(result.IsFailure);
            Assert.Contains("overlaps", All(result), StringComparison.OrdinalIgnoreCase);

            await using var read = CreateContext(connectionString);
            var source = await read.Residences
                .AsNoTracking()
                .SingleAsync(r => r.ResidenceId == fixture.EndedResidenceId, Token);

            Assert.Equal("ENDED", source.Status);
            Assert.False(await read.Residences.AnyAsync(
                r => r.CorrectsResidenceId == fixture.EndedResidenceId, Token));
            await AssertInvariantAsync(connectionString);
        } finally {
            await TryCleanupAsync(connectionString, fixture);
        }
    }

    [Fact]
    public async Task RectifyResidence_RejectsNoOpAndSecondCorrection() {
        var (connectionString, fixture) = await PrepareAsync();
        try {
            await using var context = CreateContext(connectionString);
            var service = CreateService(context);

            // A rectification must represent a real change; identical values
            // should not create a needless void/replacement pair.
            var noOp = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = fixture.EndedResidenceId,
                    AddressId = fixture.AddressId,
                    StartDate = EndedStart,
                    EndDate = EndedEnd,
                    Reason = "Same values on purpose."
                },
                fixture.OfficerId,
                Token);

            Assert.True(noOp.IsFailure);
            Assert.Contains("identical", All(noOp), StringComparison.OrdinalIgnoreCase);

            var correction = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = fixture.EndedResidenceId,
                    AddressId = fixture.OtherAddressId,
                    StartDate = EndedStart,
                    EndDate = EndedEnd,
                    Reason = "The address was wrong."
                },
                fixture.OfficerId,
                Token);

            Assert.True(correction.IsSuccess, string.Join("; ", correction.Errors));

            // Once a source row has been voided, it cannot be corrected again.
            var second = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = fixture.EndedResidenceId,
                    AddressId = fixture.AddressId,
                    StartDate = EndedStart,
                    EndDate = EndedEnd,
                    Reason = "Trying to correct the same row twice."
                },
                fixture.OfficerId,
                Token);

            Assert.True(second.IsFailure);
            Assert.Contains("voided", All(second), StringComparison.OrdinalIgnoreCase);

            await using var read = CreateContext(connectionString);
            var corrections = await read.Residences
                .AsNoTracking()
                .CountAsync(r => r.CorrectsResidenceId == fixture.EndedResidenceId, Token);

            Assert.Equal(1, corrections);
            await AssertInvariantAsync(connectionString);
        } finally {
            await TryCleanupAsync(connectionString, fixture);
        }
    }

    [Fact]
    public async Task RectifyMembership_EndedRow_ChangesRole() {
        var (connectionString, fixture) = await PrepareAsync();
        try {
            await using var context = CreateContext(connectionString);
            var service = CreateService(context);

            // Historical membership corrections may change the role while
            // retaining the citizen, household, and original date range.
            var result = await service.RectifyMembershipAsync(
                new RectifyMembershipDto {
                    MembershipId = fixture.EndedMembershipId,
                    HouseholdId = fixture.HouseholdId,
                    Role = "HEAD",
                    StartDate = EndedStart,
                    EndDate = EndedEnd,
                    Reason = "The historical role was recorded as MEMBER instead of HEAD."
                },
                fixture.OfficerId,
                Token);

            Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
            Assert.True(result.Value is int correctedId, "Expected the new membership id.");

            await using var read = CreateContext(connectionString);
            var source = await read.HouseholdMemberships
                .AsNoTracking()
                .SingleAsync(m => m.MembershipId == fixture.EndedMembershipId, Token);
            var corrected = await read.HouseholdMemberships
                .AsNoTracking()
                .SingleAsync(m => m.MembershipId == correctedId, Token);

            Assert.Equal("VOID", source.Status);
            Assert.Equal("ENDED", corrected.Status);
            Assert.Equal("HEAD", corrected.Role);
            Assert.Equal(fixture.HouseholdId, corrected.HouseholdId);
            Assert.Equal(fixture.CitizenId, corrected.CitizenId);
            Assert.Equal(fixture.EndedMembershipId, corrected.CorrectsMembershipId);

            await AssertCorrelatedAuditAsync(
                read, "VOID_MEMBERSHIP", "CORRECT_MEMBERSHIP",
                fixture.EndedMembershipId, correctedId, Token);
            await AssertInvariantAsync(connectionString);
        } finally {
            await TryCleanupAsync(connectionString, fixture);
        }
    }

    [Fact]
    public async Task RectifyActiveMembership_RejectsRoleHouseholdAndEndDateChanges() {
        var (connectionString, fixture) = await PrepareAsync();
        try {
            await using var context = CreateContext(connectionString);
            var service = CreateService(context);

            // The current membership cannot be moved, reclassified, or ended
            // through rectification because those changes affect live household
            // relationships.
            var roleChange = await service.RectifyMembershipAsync(
                new RectifyMembershipDto {
                    MembershipId = fixture.ActiveMembershipId,
                    HouseholdId = fixture.HouseholdId,
                    Role = "MEMBER",
                    StartDate = ActiveStart,
                    EndDate = null,
                    Reason = "Trying to demote the active head."
                },
                fixture.OfficerId,
                Token);

            Assert.True(roleChange.IsFailure);
            Assert.Contains("cannot change role", All(roleChange), StringComparison.OrdinalIgnoreCase);

            var householdChange = await service.RectifyMembershipAsync(
                new RectifyMembershipDto {
                    MembershipId = fixture.ActiveMembershipId,
                    HouseholdId = fixture.HouseholdId + 1_000_000,
                    Role = "HEAD",
                    StartDate = ActiveStart,
                    EndDate = null,
                    Reason = "Trying to move the active membership."
                },
                fixture.OfficerId,
                Token);

            Assert.True(householdChange.IsFailure);
            Assert.Contains("cannot change household", All(householdChange), StringComparison.OrdinalIgnoreCase);

            var endDateChange = await service.RectifyMembershipAsync(
                new RectifyMembershipDto {
                    MembershipId = fixture.ActiveMembershipId,
                    HouseholdId = fixture.HouseholdId,
                    Role = "HEAD",
                    StartDate = ActiveStart,
                    EndDate = new DateOnly(2021, 12, 31),
                    Reason = "Trying to end the active membership."
                },
                fixture.OfficerId,
                Token);

            Assert.True(endDateChange.IsFailure);
            Assert.Contains("cannot have an end date", All(endDateChange), StringComparison.OrdinalIgnoreCase);

            await using var read = CreateContext(connectionString);
            var source = await read.HouseholdMemberships
                .AsNoTracking()
                .SingleAsync(m => m.MembershipId == fixture.ActiveMembershipId, Token);

            Assert.Equal("ACTIVE", source.Status);
            Assert.Equal("HEAD", source.Role);
            await AssertInvariantAsync(connectionString);
        } finally {
            await TryCleanupAsync(connectionString, fixture);
        }
    }

    [Fact]
    public async Task RectifyHouseholdAddress_EndedRow_ChangesAddress() {
        var (connectionString, fixture) = await PrepareAsync();
        try {
            await using var context = CreateContext(connectionString);
            var service = CreateService(context);

            // An ended household address can be corrected while preserving the
            // household and date range represented by the original row.
            var result = await service.RectifyHouseholdAddressAsync(
                new RectifyHouseholdAddressDto {
                    HouseholdAddressId = fixture.EndedHouseholdAddressId,
                    AddressId = fixture.OtherAddressId,
                    StartDate = EndedStart,
                    EndDate = EndedEnd,
                    Reason = "The historical household address pointed at the wrong street."
                },
                fixture.OfficerId,
                Token);

            Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
            Assert.True(result.Value is int correctedId, "Expected the new household address id.");

            await using var read = CreateContext(connectionString);
            var source = await read.HouseholdAddresses
                .AsNoTracking()
                .SingleAsync(h => h.HouseholdAddressId == fixture.EndedHouseholdAddressId, Token);
            var corrected = await read.HouseholdAddresses
                .AsNoTracking()
                .SingleAsync(h => h.HouseholdAddressId == correctedId, Token);

            Assert.Equal("VOID", source.Status);
            Assert.Equal("ENDED", corrected.Status);
            Assert.Equal(fixture.OtherAddressId, corrected.AddressId);
            Assert.Equal(fixture.HouseholdId, corrected.HouseholdId);
            Assert.Equal(fixture.EndedHouseholdAddressId, corrected.CorrectsHouseholdAddressId);

            await AssertCorrelatedAuditAsync(
                read, "VOID_HOUSEHOLD_ADDRESS", "CORRECT_HOUSEHOLD_ADDRESS",
                fixture.EndedHouseholdAddressId, correctedId, Token);
            await AssertInvariantAsync(connectionString);
        } finally {
            await TryCleanupAsync(connectionString, fixture);
        }
    }

    [Fact]
    public async Task RectifyActiveHouseholdAddress_RejectsAddressAndEndDateChanges() {
        var (connectionString, fixture) = await PrepareAsync();
        try {
            await using var context = CreateContext(connectionString);
            var service = CreateService(context);

            var addressChange = await service.RectifyHouseholdAddressAsync(
                new RectifyHouseholdAddressDto {
                    HouseholdAddressId = fixture.ActiveHouseholdAddressId,
                    AddressId = fixture.OtherAddressId,
                    StartDate = ActiveStart,
                    EndDate = null,
                    Reason = "Trying to move the active household address."
                },
                fixture.OfficerId,
                Token);

            Assert.True(addressChange.IsFailure);
            Assert.Contains("cannot change", All(addressChange), StringComparison.OrdinalIgnoreCase);

            var endDateChange = await service.RectifyHouseholdAddressAsync(
                new RectifyHouseholdAddressDto {
                    HouseholdAddressId = fixture.ActiveHouseholdAddressId,
                    AddressId = fixture.AddressId,
                    StartDate = ActiveStart,
                    EndDate = new DateOnly(2021, 12, 31),
                    Reason = "Trying to end the active household address."
                },
                fixture.OfficerId,
                Token);

            Assert.True(endDateChange.IsFailure);
            Assert.Contains("cannot have an end date", All(endDateChange), StringComparison.OrdinalIgnoreCase);

            await using var read = CreateContext(connectionString);
            var source = await read.HouseholdAddresses
                .AsNoTracking()
                .SingleAsync(h => h.HouseholdAddressId == fixture.ActiveHouseholdAddressId, Token);

            Assert.Equal("ACTIVE", source.Status);
            Assert.Equal(fixture.AddressId, source.AddressId);
            await AssertInvariantAsync(connectionString);
        } finally {
            await TryCleanupAsync(connectionString, fixture);
        }
    }

    [Fact]
    public async Task Rectify_RejectsActiveRowsReferencedByPendingTransfer() {
        var (connectionString, fixture) = await PrepareAsync();
        try {
            // Pending transfers depend on the active source rows. Rectification
            // must fail rather than changing data the transfer still references.
            await CreatePendingTransferAsync(connectionString, fixture);

            await using var context = CreateContext(connectionString);
            var service = CreateService(context);

            var residence = await service.RectifyResidenceAsync(
                new RectifyResidenceDto {
                    ResidenceId = fixture.ActiveResidenceId,
                    AddressId = fixture.AddressId,
                    StartDate = new DateOnly(2021, 2, 1),
                    EndDate = null,
                    Reason = "Trying to correct a residence that a transfer is using."
                },
                fixture.OfficerId,
                Token);

            Assert.True(residence.IsFailure);
            Assert.Contains("pending transfer", All(residence), StringComparison.OrdinalIgnoreCase);

            var membership = await service.RectifyMembershipAsync(
                new RectifyMembershipDto {
                    MembershipId = fixture.ActiveMembershipId,
                    HouseholdId = fixture.HouseholdId,
                    Role = "HEAD",
                    StartDate = new DateOnly(2021, 2, 1),
                    EndDate = null,
                    Reason = "Trying to correct a membership that a transfer is using."
                },
                fixture.OfficerId,
                Token);

            Assert.True(membership.IsFailure);
            Assert.Contains("pending transfer", All(membership), StringComparison.OrdinalIgnoreCase);

            await using var read = CreateContext(connectionString);
            Assert.Equal("ACTIVE", (await read.Residences
                .AsNoTracking()
                .SingleAsync(r => r.ResidenceId == fixture.ActiveResidenceId, Token)).Status);
            Assert.Equal("ACTIVE", (await read.HouseholdMemberships
                .AsNoTracking()
                .SingleAsync(m => m.MembershipId == fixture.ActiveMembershipId, Token)).Status);
            await AssertInvariantAsync(connectionString);
        } finally {
            await TryCleanupAsync(connectionString, fixture);
        }
    }

    // ---------------------------------------------------------------- helpers

    private sealed record Fixture(
        int OfficerId,
        int CitizenId,
        int HouseholdId,
        int AddressId,
        int OtherAddressId,
        int ActiveMembershipId,
        int EndedMembershipId,
        int ActiveResidenceId,
        int EndedResidenceId,
        int ActiveHouseholdAddressId,
        int EndedHouseholdAddressId);

    private static async Task<(string ConnectionString, Fixture Fixture)> PrepareAsync() {
        if (TestDatabase.ConnectionString is null) {
            Assert.Skip(
                $"Set {TestDatabase.ConnectionVariable} to run this test, e.g. {TestDatabase.SetupHint}");
        }

        var connectionString = TestDatabase.ConnectionString!;
        await EnsureSchemaAsync(connectionString);
        var fixture = await CreateFixtureAsync(connectionString);

        return (connectionString, fixture);
    }

    private static async Task EnsureSchemaAsync(string connectionString) {
        await using var connection = await SqlResilience.OpenAsync(
            connectionString, cancellationToken: Token);

        if (!await TestDatabase.SchemaIsDeployedAsync(connection, Token)) {
            Assert.Skip(
                "The ARIS schema is not deployed. Run .\\scripts\\deploy_database.ps1 first.");
        }

        if (!await TestDatabase.ObjectExistsAsync(connection, "dbo", "AuditLog", Token)) {
            Assert.Skip("dbo.AuditLog is not deployed.");
        }
    }

    private static async Task<Fixture> CreateFixtureAsync(string connectionString) {
        var now = DateTime.UtcNow;

        await using var context = CreateContext(connectionString);
        var officerId = await EnsureOfficerAsync(context);

        // Each test receives unique rows so committed fixtures from a previous
        // run cannot affect date-range, uniqueness, or correction assertions.
        var address = new Address {
            AddressCode = Code("ADR"),
            AddressLine = "1 Rectification Street",
            Ward = "Ward 1",
            District = "District 1",
            Province = "Test Province",
            Status = "ACTIVE",
            ValidFrom = EndedStart,
            CreatedAt = now
        };

        var otherAddress = new Address {
            AddressCode = Code("ADR"),
            AddressLine = "2 Correction Street",
            Ward = "Ward 1",
            District = "District 1",
            Province = "Test Province",
            Status = "ACTIVE",
            ValidFrom = EndedStart,
            CreatedAt = now
        };

        context.Addresses.AddRange(address, otherAddress);
        await context.SaveChangesAsync(Token);

        var citizen = new Citizen {
            NationalId = NationalId(),
            FullName = "Rectification Test Citizen",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "OTHER",
            Status = "ACTIVE",
            CreatedAt = now,
            UpdatedAt = now
        };

        var household = new Household {
            Status = "ACTIVE",
            CreatedAt = now
        };

        context.Citizens.Add(citizen);
        context.Households.Add(household);
        await context.SaveChangesAsync(Token);

        var endedAddress = new HouseholdAddress {
            HouseholdId = household.HouseholdId,
            AddressId = address.AddressId,
            StartDate = EndedStart,
            EndDate = EndedEnd,
            Status = "ENDED",
            CreatedBy = officerId,
            CreatedAt = now,
            EndedBy = officerId,
            EndedAt = now
        };

        var activeAddress = new HouseholdAddress {
            HouseholdId = household.HouseholdId,
            AddressId = address.AddressId,
            StartDate = ActiveStart,
            Status = "ACTIVE",
            CreatedBy = officerId,
            CreatedAt = now
        };

        context.HouseholdAddresses.AddRange(endedAddress, activeAddress);

        var endedMembership = new HouseholdMembership {
            CitizenId = citizen.CitizenId,
            HouseholdId = household.HouseholdId,
            Role = "MEMBER",
            StartDate = EndedStart,
            EndDate = EndedEnd,
            Status = "ENDED",
            CreatedBy = officerId,
            CreatedAt = now,
            EndedBy = officerId,
            EndedAt = now
        };

        var activeMembership = new HouseholdMembership {
            CitizenId = citizen.CitizenId,
            HouseholdId = household.HouseholdId,
            Role = "HEAD",
            StartDate = ActiveStart,
            Status = "ACTIVE",
            CreatedBy = officerId,
            CreatedAt = now
        };

        context.HouseholdMemberships.AddRange(endedMembership, activeMembership);

        var endedResidence = new Residence {
            CitizenId = citizen.CitizenId,
            AddressId = address.AddressId,
            StartDate = EndedStart,
            EndDate = EndedEnd,
            Status = "ENDED",
            CreatedBy = officerId,
            CreatedAt = now,
            EndedBy = officerId,
            EndedAt = now
        };

        var activeResidence = new Residence {
            CitizenId = citizen.CitizenId,
            AddressId = address.AddressId,
            StartDate = ActiveStart,
            Status = "ACTIVE",
            CreatedBy = officerId,
            CreatedAt = now
        };

        context.Residences.AddRange(endedResidence, activeResidence);
        await context.SaveChangesAsync(Token);

        return new Fixture(
            officerId,
            citizen.CitizenId,
            household.HouseholdId,
            address.AddressId,
            otherAddress.AddressId,
            activeMembership.MembershipId,
            endedMembership.MembershipId,
            activeResidence.ResidenceId,
            endedResidence.ResidenceId,
            activeAddress.HouseholdAddressId,
            endedAddress.HouseholdAddressId);
    }

    private static async Task<int> EnsureOfficerAsync(ArisDbContext context) {
        var officer = await context.UserAccounts
            .FirstOrDefaultAsync(u => u.Username == OfficerUsername, Token);

        if (officer is null) {
            officer = new UserAccount {
                Username = OfficerUsername,
                PasswordHash = "integration-test-not-a-real-hash",
                FullName = "Rectification Integration Test",
                Role = "OFFICER",
                Status = "ACTIVE",
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow
            };

            context.UserAccounts.Add(officer);
            await context.SaveChangesAsync(Token);
        } else if (officer.Status != "ACTIVE") {
            officer.Status = "ACTIVE";
            await context.SaveChangesAsync(Token);
        }

        return officer.UserId;
    }

    private static async Task CreatePendingTransferAsync(string connectionString, Fixture fixture) {
        await using var context = CreateContext(connectionString);

        // Link both active source rows to one pending request. This mirrors the
        // production relationship checked by the rectification service.
        var request = new TransferRequest {
            RequestType = "NEW_HOUSEHOLD",
            SourceHouseholdId = fixture.HouseholdId,
            DestinationAddressId = fixture.OtherAddressId,
            EffectiveDate = new DateOnly(2022, 1, 1),
            NewHeadCitizenId = fixture.CitizenId,
            Status = "PENDING",
            RequestedBy = fixture.OfficerId,
            RequestedAt = DateTime.UtcNow
        };

        context.TransferRequests.Add(request);
        await context.SaveChangesAsync(Token);

        context.TransferRequestItems.Add(new TransferRequestItem {
            RequestId = request.RequestId,
            CitizenId = fixture.CitizenId,
            SourceMembershipId = fixture.ActiveMembershipId,
            SourceResidenceId = fixture.ActiveResidenceId,
            Status = "PENDING"
        });

        await context.SaveChangesAsync(Token);
    }

    private static async Task AssertCorrelatedAuditAsync(
        ArisDbContext context,
        string voidAction,
        string correctAction,
        int oldId,
        int newId,
        CancellationToken cancellationToken) {
        // Both halves of one correction must be traceable as a single operation.
        var voidRow = await context.AuditLogs
            .AsNoTracking()
            .SingleAsync(a => a.Action == voidAction && a.EntityId == oldId && a.Result == "SUCCESS", cancellationToken);

        var correctRow = await context.AuditLogs
            .AsNoTracking()
            .SingleAsync(a => a.Action == correctAction && a.EntityId == newId && a.Result == "SUCCESS", cancellationToken);

        Assert.NotNull(voidRow.CorrelationId);
        Assert.Equal(voidRow.CorrelationId, correctRow.CorrelationId);
    }

    private static async Task AssertInvariantAsync(string connectionString) {
        await using var connection = await SqlResilience.OpenAsync(
            connectionString, cancellationToken: Token);

        // The view centralizes the cross-table Golden Invariant rules, so the
        // integration test checks the same consistency contract as production.
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT ViolationType, CitizenId, HouseholdId FROM dbo.vw_GoldenInvariantViolations;";

        var violations = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(Token);
        while (await reader.ReadAsync(Token)) {
            var citizenId = reader.IsDBNull(1) ? "-" : reader.GetInt32(1).ToString();
            var householdId = reader.IsDBNull(2) ? "-" : reader.GetInt32(2).ToString();
            violations.Add($"{reader.GetString(0)} CitizenId={citizenId} HouseholdId={householdId}");
        }

        Assert.True(
            violations.Count == 0,
            $"The golden invariant is violated:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// Best-effort cleanup. The test connection may be <c>aris_officer</c>, which is
    /// denied <c>DELETE</c>, so failures are ignored rather than failing the test.
    /// Audit rows and the shared officer account intentionally remain.
    /// </summary>
    private static async Task TryCleanupAsync(string connectionString, Fixture fixture) {
        try {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(Token);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM dbo.TransferRequestItem WHERE CitizenId = @citizenId;
                DELETE FROM dbo.Residence           WHERE CitizenId = @citizenId;
                DELETE FROM dbo.HouseholdMembership WHERE CitizenId = @citizenId;
                DELETE FROM dbo.HouseholdAddress    WHERE HouseholdId = @householdId;
                DELETE FROM dbo.TransferRequest     WHERE SourceHouseholdId = @householdId;
                DELETE FROM dbo.Household           WHERE HouseholdId = @householdId;
                DELETE FROM dbo.Address             WHERE AddressId IN (@addressId, @otherAddressId);
                DELETE FROM dbo.Citizen             WHERE CitizenId = @citizenId;
                """;

            command.Parameters.AddWithValue("@citizenId", fixture.CitizenId);
            command.Parameters.AddWithValue("@householdId", fixture.HouseholdId);
            command.Parameters.AddWithValue("@addressId", fixture.AddressId);
            command.Parameters.AddWithValue("@otherAddressId", fixture.OtherAddressId);

            await command.ExecuteNonQueryAsync(Token);
        } catch {
            // DELETE is denied for the least-privilege runtime role; leave the rows.
        }
    }

    private static RectificationService CreateService(ArisDbContext context) {
        return new RectificationService(
            context,
            new AuditService(context),
            new RectifyResidenceValidator(),
            new RectifyMembershipValidator(),
            new RectifyHouseholdAddressValidator());
    }

    private static ArisDbContext CreateContext(string connectionString) {
        var options = new DbContextOptionsBuilder<ArisDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ArisDbContext(options);
    }

    private static string All(Application.Common.Result result)
        => string.Join(" ", result.Errors);

    private static string Code(string prefix)
        => $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 1 + 24, 30)];

    private static string NationalId() {
        var counter = Interlocked.Increment(ref _nationalIdCounter);
        var value = (DateTime.UtcNow.Ticks + counter) % 1_000_000_000_000;
        return value.ToString("D12");
    }
}