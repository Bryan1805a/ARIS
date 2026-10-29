using System.Text.Json;

using Aris.Application.Accounts;
using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Accounts;

/// <summary>
/// Seeds a small, consistent demo dataset (<c>T11</c>).
/// </summary>
/// <remarks>
/// The rows deliberately satisfy the Golden Invariant: every <c>ACTIVE</c> citizen has
/// one active membership and one active residence, both pointing at the household's
/// active address. The seed is idempotent and runs in one transaction, so a partial
/// dataset can never be left behind. It creates no accounts and no transfer requests.
/// </remarks>
public sealed class DemoDataService : IDemoDataService
{
    private const string Action = "SEED_DEMO_DATA";
    private const string FirstDemoNationalId = "000000000001";

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;

    public DemoDataService(ArisDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<Result<DemoSeedSummary>> SeedAsync(
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == currentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (actor is null || actor.Role != "ADMIN" || actor.Status != "ACTIVE")
        {
            return Result<DemoSeedSummary>.Failure(
                "Only an active administrator can seed demo data.");
        }

        if (await _context.Citizens
                .AnyAsync(c => c.NationalId == FirstDemoNationalId, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<DemoSeedSummary>.Failure(
                "Demo data is already seeded. Reset the database to seed it again.");
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            // --- Addresses -------------------------------------------------------
            var home1 = new Address
            {
                AddressCode = "DEMO-ADDR-001",
                AddressLine = "12 Nguyen Hue",
                Ward = "Ben Nghe",
                District = "Quan 1",
                Province = "Ho Chi Minh City",
                Status = "ACTIVE",
                ValidFrom = new DateOnly(2020, 1, 1),
                CreatedAt = now
            };

            var home2 = new Address
            {
                AddressCode = "DEMO-ADDR-002",
                AddressLine = "45 Le Loi",
                Ward = "Ben Thanh",
                District = "Quan 1",
                Province = "Ho Chi Minh City",
                Status = "ACTIVE",
                ValidFrom = new DateOnly(2021, 1, 1),
                CreatedAt = now
            };

            // --- Citizens --------------------------------------------------------
            var an = NewCitizen("000000000001", "Nguyen Van An", new DateOnly(1980, 1, 1), "MALE", "ACTIVE", now);
            var binh = NewCitizen("000000000002", "Tran Thi Binh", new DateOnly(1985, 5, 12), "FEMALE", "ACTIVE", now);
            var cu = NewCitizen("000000000003", "Nguyen Van Cu", new DateOnly(2010, 9, 20), "MALE", "ACTIVE", now);
            var dung = NewCitizen("000000000004", "Le Van Dung", new DateOnly(1975, 3, 8), "MALE", "ACTIVE", now);
            var em = NewCitizen("000000000005", "Pham Thi Em", new DateOnly(1979, 11, 30), "FEMALE", "ACTIVE", now);
            var phuc = NewCitizen("000000000006", "Hoang Van Phuc", new DateOnly(1995, 7, 15), "MALE", "UNASSIGNED", now);

            _context.Addresses.AddRange(home1, home2);
            _context.Citizens.AddRange(an, binh, cu, dung, em, phuc);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // --- Households ------------------------------------------------------
            var household1 = new Household { Status = "ACTIVE", CreatedAt = now };
            var household2 = new Household { Status = "ACTIVE", CreatedAt = now };

            _context.Households.AddRange(household1, household2);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // --- Location history ------------------------------------------------
            var start1 = new DateOnly(2024, 1, 1);
            var start2 = new DateOnly(2024, 6, 1);

            _context.HouseholdAddresses.AddRange(
                NewHouseholdAddress(household1.HouseholdId, home1.AddressId, start1, currentUserId, now),
                NewHouseholdAddress(household2.HouseholdId, home2.AddressId, start2, currentUserId, now));

            // --- Membership and residency ---------------------------------------
            _context.HouseholdMemberships.AddRange(
                NewMembership(an.CitizenId, household1.HouseholdId, "HEAD", start1, currentUserId, now),
                NewMembership(binh.CitizenId, household1.HouseholdId, "MEMBER", start1, currentUserId, now),
                NewMembership(cu.CitizenId, household1.HouseholdId, "MEMBER", start1, currentUserId, now),
                NewMembership(dung.CitizenId, household2.HouseholdId, "HEAD", start2, currentUserId, now),
                NewMembership(em.CitizenId, household2.HouseholdId, "MEMBER", start2, currentUserId, now));

            _context.Residences.AddRange(
                NewResidence(an.CitizenId, home1.AddressId, start1, currentUserId, now),
                NewResidence(binh.CitizenId, home1.AddressId, start1, currentUserId, now),
                NewResidence(cu.CitizenId, home1.AddressId, start1, currentUserId, now),
                NewResidence(dung.CitizenId, home2.AddressId, start2, currentUserId, now),
                NewResidence(em.CitizenId, home2.AddressId, start2, currentUserId, now));

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // --- Audit -----------------------------------------------------------
            await _audit.LogAsync(new AuditEntry
            {
                ActorUserId = currentUserId,
                Action = Action,
                EntityType = "Household",
                EntityId = household1.HouseholdId,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    addresses = 2,
                    citizens = 6,
                    households = 2,
                    unassignedCitizens = 1
                }),
                CorrelationId = correlationId
            }, cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<DemoSeedSummary>.Success(new DemoSeedSummary(2, 6, 2));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            await LogFailureAsync(currentUserId, correlationId, ex, cancellationToken).ConfigureAwait(false);

            return Result<DemoSeedSummary>.Failure(
                $"Demo data could not be seeded: {ex.Message}");
        }
    }

    private static Citizen NewCitizen(
        string nationalId,
        string fullName,
        DateOnly dateOfBirth,
        string gender,
        string status,
        DateTime now) => new()
        {
            NationalId = nationalId,
            FullName = fullName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static HouseholdAddress NewHouseholdAddress(
        int householdId,
        int addressId,
        DateOnly startDate,
        int actorId,
        DateTime now) => new()
        {
            HouseholdId = householdId,
            AddressId = addressId,
            StartDate = startDate,
            Status = "ACTIVE",
            CreatedBy = actorId,
            CreatedAt = now
        };

    private static HouseholdMembership NewMembership(
        int citizenId,
        int householdId,
        string role,
        DateOnly startDate,
        int actorId,
        DateTime now) => new()
        {
            CitizenId = citizenId,
            HouseholdId = householdId,
            Role = role,
            StartDate = startDate,
            Status = "ACTIVE",
            CreatedBy = actorId,
            CreatedAt = now
        };

    private static Residence NewResidence(
        int citizenId,
        int addressId,
        DateOnly startDate,
        int actorId,
        DateTime now) => new()
        {
            CitizenId = citizenId,
            AddressId = addressId,
            StartDate = startDate,
            Status = "ACTIVE",
            CreatedBy = actorId,
            CreatedAt = now
        };

    private async Task LogFailureAsync(
        int actorUserId,
        Guid correlationId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await _audit.LogFailureIndependentAsync(connectionString, new AuditFailureEntry
        {
            ActorUserId = actorUserId,
            Action = Action,
            EntityType = "Household",
            DetailsJson = JsonSerializer.Serialize(new
            {
                reason = exception.Message,
                exception = exception.GetType().Name
            }),
            CorrelationId = correlationId
        }, cancellationToken).ConfigureAwait(false);
    }
}
