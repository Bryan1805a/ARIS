using System.Text.Json;

using Aris.Application.Auditing;
using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Citizens;

/// <summary>
/// Citizen use cases: registration, demographics, search, profile and deactivation
/// (<c>CIT-01</c>-<c>CIT-04</c>, <c>SRCH-01</c>).
/// </summary>
/// <remarks>
/// Lives in Infrastructure because the composite operations need <see cref="ArisDbContext"/>
/// and the caller's transaction. The Application layer exposes only
/// <see cref="ICitizenService"/> and its DTOs, so the UI never sees EF Core.
/// </remarks>
public sealed class CitizenService : ICitizenService
{
    private const string CreateAction = "CREATE_CITIZEN";
    private const string UpdateAction = "UPDATE_CITIZEN";
    private const string DeactivateAction = "DEACTIVATE_CITIZEN";
    private const int MaxSearchResults = 500;

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;
    private readonly IValidator<CreateCitizenDto> _createValidator;
    private readonly IValidator<UpdateCitizenDto> _updateValidator;

    public CitizenService(
        ArisDbContext context,
        IAuditService audit,
        IValidator<CreateCitizenDto> createValidator,
        IValidator<UpdateCitizenDto> updateValidator)
    {
        _context = context;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<int>> CreateCitizenAsync(
        CreateCitizenDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        if (!await IsActiveActorAsync(currentUserId, ct))
        {
            return Result<int>.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result<int>.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var nationalId = dto.NationalId.Trim();
        var fullName = dto.FullName.Trim();
        var gender = dto.Gender.Trim().ToUpperInvariant();

        if (await _context.Citizens.AnyAsync(c => c.NationalId == nationalId, ct))
        {
            return Result<int>.Failure($"National ID {nationalId} is already registered.");
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var citizen = new Citizen
            {
                NationalId = nationalId,
                FullName = fullName,
                DateOfBirth = dto.DateOfBirth,
                Gender = gender,
                Status = "UNASSIGNED",
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Citizens.Add(citizen);
            await _context.SaveChangesAsync(ct);

            await _audit.LogAsync(new AuditEntry
            {
                ActorUserId = currentUserId,
                Action = CreateAction,
                EntityType = "Citizen",
                EntityId = citizen.CitizenId,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    nationalId,
                    fullName,
                    status = "UNASSIGNED"
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(citizen.CitizenId);
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, CreateAction, null, correlationId, ex, ct);
            return Result<int>.Failure($"The citizen could not be registered: {ex.Message}");
        }
    }

    public async Task<Result> UpdateCitizenAsync(
        int citizenId,
        UpdateCitizenDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        if (!await IsActiveActorAsync(currentUserId, ct))
        {
            return Result.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var correlationId = Guid.NewGuid();

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var citizen = await _context.Citizens
                .FirstOrDefaultAsync(c => c.CitizenId == citizenId, ct);

            if (citizen is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("Citizen not found.");
            }

            if (citizen.Status == "INACTIVE")
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("An inactive citizen cannot be edited.");
            }

            var before = new
            {
                citizen.FullName,
                citizen.DateOfBirth,
                citizen.Gender
            };

            citizen.FullName = dto.FullName.Trim();
            citizen.DateOfBirth = dto.DateOfBirth;
            citizen.Gender = dto.Gender.Trim().ToUpperInvariant();
            citizen.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _audit.LogAsync(new AuditEntry
            {
                ActorUserId = currentUserId,
                Action = UpdateAction,
                EntityType = "Citizen",
                EntityId = citizen.CitizenId,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    before,
                    after = new
                    {
                        citizen.FullName,
                        citizen.DateOfBirth,
                        citizen.Gender
                    }
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await TryRollbackAsync(transaction, ct);
            return Result.Failure(
                "This citizen was changed by another user. Reload the record and try again.");
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, UpdateAction, citizenId, correlationId, ex, ct);
            return Result.Failure($"The citizen could not be updated: {ex.Message}");
        }
    }

    public async Task<Result<CitizenDetailDto?>> GetByIdAsync(
        int citizenId,
        CancellationToken ct = default)
    {
        var citizen = await _context.Citizens
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CitizenId == citizenId, ct);

        if (citizen is null)
        {
            return Result<CitizenDetailDto?>.Success(null);
        }

        var membership = await _context.HouseholdMemberships
            .AsNoTracking()
            .Where(m => m.CitizenId == citizenId && m.Status == "ACTIVE")
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefaultAsync(ct);

        var residence = await _context.Residences
            .AsNoTracking()
            .Where(r => r.CitizenId == citizenId && r.Status == "ACTIVE")
            .OrderByDescending(r => r.StartDate)
            .FirstOrDefaultAsync(ct);

        var currentAddressText = residence is null
            ? null
            : await ResolveCanonicalAddressTextAsync(residence.AddressId, ct);

        var timeline = await (
            from r in _context.Residences.AsNoTracking()
            join a in _context.Addresses.AsNoTracking() on r.AddressId equals a.AddressId
            where r.CitizenId == citizenId && r.Status != "VOID"
            orderby r.StartDate descending, r.ResidenceId descending
            select new ResidencyPeriodDto
            {
                ResidenceId = r.ResidenceId,
                AddressId = r.AddressId,
                AddressText = a.AddressText,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                Status = r.Status
            }).ToListAsync(ct);

        var householdMembers = membership is null
            ? new List<CitizenDto>()
            : await (
                from m in _context.HouseholdMemberships.AsNoTracking()
                join c in _context.Citizens.AsNoTracking() on m.CitizenId equals c.CitizenId
                where m.HouseholdId == membership.HouseholdId && m.Status == "ACTIVE"
                orderby c.FullName
                select new CitizenDto
                {
                    CitizenId = c.CitizenId,
                    NationalId = c.NationalId,
                    FullName = c.FullName,
                    DateOfBirth = c.DateOfBirth,
                    Gender = c.Gender,
                    Status = c.Status
                }).ToListAsync(ct);

        return Result<CitizenDetailDto?>.Success(new CitizenDetailDto
        {
            CitizenId = citizen.CitizenId,
            NationalId = citizen.NationalId,
            FullName = citizen.FullName,
            DateOfBirth = citizen.DateOfBirth,
            Gender = citizen.Gender,
            Status = citizen.Status,
            CreatedAt = citizen.CreatedAt,
            UpdatedAt = citizen.UpdatedAt,
            DeactivatedAt = citizen.DeactivatedAt,
            HouseholdId = membership?.HouseholdId,
            HouseholdRole = membership?.Role,
            HouseholdMembers = householdMembers,
            CurrentAddressText = currentAddressText,
            ResidencyTimeline = timeline
        });
    }

    public async Task<Result<IEnumerable<CitizenDto>>> SearchAsync(
        CitizenSearchCriteria criteria,
        CancellationToken ct = default)
    {
        var query = _context.Citizens.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.NationalId))
        {
            var nationalId = criteria.NationalId.Trim();
            query = query.Where(c => c.NationalId.Contains(nationalId));
        }

        if (!string.IsNullOrWhiteSpace(criteria.FullName))
        {
            var fullName = criteria.FullName.Trim();
            query = query.Where(c => c.FullName.Contains(fullName));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            var status = criteria.Status.Trim().ToUpperInvariant();
            if (status is not ("UNASSIGNED" or "ACTIVE" or "INACTIVE"))
            {
                return Result<IEnumerable<CitizenDto>>.Failure(
                    "Status must be UNASSIGNED, ACTIVE, or INACTIVE.");
            }

            query = query.Where(c => c.Status == status);
        }

        var citizens = await query
            .OrderBy(c => c.FullName)
            .ThenBy(c => c.CitizenId)
            .Take(MaxSearchResults)
            .Select(c => new CitizenDto
            {
                CitizenId = c.CitizenId,
                NationalId = c.NationalId,
                FullName = c.FullName,
                DateOfBirth = c.DateOfBirth,
                Gender = c.Gender,
                Status = c.Status
            })
            .ToListAsync(ct);

        return Result<IEnumerable<CitizenDto>>.Success(citizens);
    }

    public async Task<Result> DeactivateCitizenAsync(
        int citizenId,
        int currentUserId,
        int? successorHeadCitizenId = null,
        DateOnly? effectiveDate = null,
        CancellationToken ct = default)
    {
        if (!await IsActiveActorAsync(currentUserId, ct))
        {
            return Result.Failure("The authenticated user was not found or is not active.");
        }

        var effective = effectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var citizen = await _context.Citizens
                .FirstOrDefaultAsync(c => c.CitizenId == citizenId, ct);

            if (citizen is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("Citizen not found.");
            }

            if (citizen.Status == "INACTIVE")
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("The citizen is already inactive.");
            }

            if (citizen.Status == "UNASSIGNED")
            {
                citizen.Status = "INACTIVE";
                citizen.DeactivatedAt = now;
                citizen.UpdatedAt = now;

                await _context.SaveChangesAsync(ct);
                await WriteDeactivationAuditAsync(
                    citizenId, currentUserId, correlationId,
                    effective, successorHeadCitizenId, householdInactivated: false, ct);

                await transaction.CommitAsync(ct);
                return Result.Success();
            }

            var membership = await _context.HouseholdMemberships
                .FirstOrDefaultAsync(m => m.CitizenId == citizenId && m.Status == "ACTIVE", ct);

            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.CitizenId == citizenId && r.Status == "ACTIVE", ct);

            if (membership is null || residence is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "The citizen is ACTIVE but has no active membership or residence. " +
                    "Run the Golden Invariant check before changing the record.");
            }

            if (effective <= membership.StartDate || effective <= residence.StartDate)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "The effective date must be later than the start date of the active tenures.");
            }

            var otherActiveMembers = await _context.HouseholdMemberships
                .Where(m => m.HouseholdId == membership.HouseholdId
                    && m.Status == "ACTIVE"
                    && m.CitizenId != citizenId)
                .ToListAsync(ct);

            HouseholdMembership? successorMembership = null;

            if (membership.Role == "HEAD"
                && otherActiveMembers.Count > 0
                && successorHeadCitizenId is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "This citizen is the household head. Choose a successor head before deactivating.");
            }

            if (membership.Role == "HEAD" && successorHeadCitizenId is int successorId)
            {
                successorMembership = otherActiveMembers
                    .FirstOrDefault(m => m.CitizenId == successorId && m.Role == "MEMBER");

                if (successorMembership is null)
                {
                    await TryRollbackAsync(transaction, ct);
                    return Result.Failure(
                        "The successor must be an active MEMBER of the same household.");
                }

                if (effective <= successorMembership.StartDate)
                {
                    await TryRollbackAsync(transaction, ct);
                    return Result.Failure(
                        "The effective date must be later than the successor's membership start date.");
                }
            }

            var householdBecomesEmpty = membership.Role == "HEAD" && otherActiveMembers.Count == 0;

            Household? household = null;
            HouseholdAddress? activeHouseholdAddress = null;

            if (householdBecomesEmpty)
            {
                household = await _context.Households
                    .FirstOrDefaultAsync(h => h.HouseholdId == membership.HouseholdId, ct);

                activeHouseholdAddress = await _context.HouseholdAddresses
                    .FirstOrDefaultAsync(a => a.HouseholdId == membership.HouseholdId
                        && a.Status == "ACTIVE", ct);

                if (activeHouseholdAddress is not null
                    && effective <= activeHouseholdAddress.StartDate)
                {
                    await TryRollbackAsync(transaction, ct);
                    return Result.Failure(
                        "The effective date must be later than the household address start date.");
                }
            }

            // Close tenures first: the filtered unique indexes must see no active rows
            // before the successor's new HEAD row is inserted (R1).
            membership.EndDate = effective.AddDays(-1);
            membership.Status = "ENDED";
            membership.EndedBy = currentUserId;
            membership.EndedAt = now;

            residence.EndDate = effective.AddDays(-1);
            residence.Status = "ENDED";
            residence.EndedBy = currentUserId;
            residence.EndedAt = now;

            if (successorMembership is not null)
            {
                successorMembership.EndDate = effective.AddDays(-1);
                successorMembership.Status = "ENDED";
                successorMembership.EndedBy = currentUserId;
                successorMembership.EndedAt = now;
            }

            await _context.SaveChangesAsync(ct);

            if (successorMembership is not null)
            {
                _context.HouseholdMemberships.Add(new HouseholdMembership
                {
                    CitizenId = successorMembership.CitizenId,
                    HouseholdId = membership.HouseholdId,
                    Role = "HEAD",
                    StartDate = effective,
                    Status = "ACTIVE",
                    CreatedBy = currentUserId,
                    CreatedAt = now
                });

                await _context.SaveChangesAsync(ct);
            }

            if (householdBecomesEmpty)
            {
                if (household is not null)
                {
                    household.Status = "INACTIVE";
                    household.InactivatedAt = now;
                }

                if (activeHouseholdAddress is not null)
                {
                    activeHouseholdAddress.EndDate = effective.AddDays(-1);
                    activeHouseholdAddress.Status = "ENDED";
                    activeHouseholdAddress.EndedBy = currentUserId;
                    activeHouseholdAddress.EndedAt = now;
                }

                await _context.SaveChangesAsync(ct);
            }

            citizen.Status = "INACTIVE";
            citizen.DeactivatedAt = now;
            citizen.UpdatedAt = now;
            await _context.SaveChangesAsync(ct);

            await WriteDeactivationAuditAsync(
                citizenId, currentUserId, correlationId,
                effective, successorHeadCitizenId, householdBecomesEmpty, ct);

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await TryRollbackAsync(transaction, ct);
            return Result.Failure(
                "This record was changed by another user. Reload it and try again.");
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, DeactivateAction, citizenId, correlationId, ex, ct);
            return Result.Failure($"The citizen could not be deactivated: {ex.Message}");
        }
    }


    private static async Task TryRollbackAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken ct)
    {
        try
        {
            await transaction.RollbackAsync(ct);
        }
        catch
        {
            // A trigger may already have rolled the transaction back; keep the original error.
        }
    }
    private async Task WriteDeactivationAuditAsync(
        int citizenId,
        int currentUserId,
        Guid correlationId,
        DateOnly effectiveDate,
        int? successorHeadCitizenId,
        bool householdInactivated,
        CancellationToken ct)
    {
        await _audit.LogAsync(new AuditEntry
        {
            ActorUserId = currentUserId,
            Action = DeactivateAction,
            EntityType = "Citizen",
            EntityId = citizenId,
            DetailsJson = JsonSerializer.Serialize(new
            {
                effectiveDate,
                successorHeadCitizenId,
                householdInactivated
            }),
            CorrelationId = correlationId
        }, ct);
    }

    private async Task<bool> IsActiveActorAsync(int userId, CancellationToken ct)
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId && u.Status == "ACTIVE", ct);
    }

    /// <summary>
    /// Follows <c>ReplacedByAddressId</c> up to the schema's 20-level limit and returns
    /// the terminal address text. Mirrors <c>vw_AddressCanonical</c>.
    /// </summary>
    private async Task<string?> ResolveCanonicalAddressTextAsync(
        int addressId,
        CancellationToken ct)
    {
        var currentId = addressId;

        for (var depth = 0; depth <= 20; depth++)
        {
            var address = await _context.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AddressId == currentId, ct);

            if (address is null)
            {
                return null;
            }

            if (address.ReplacedByAddressId is not int successorId)
            {
                return address.AddressText;
            }

            currentId = successorId;
        }

        return null;
    }

    private async Task LogFailureAsync(
        int actorUserId,
        string action,
        int? entityId,
        Guid correlationId,
        Exception exception,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await _audit.LogFailureIndependentAsync(connectionString, new AuditFailureEntry
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = "Citizen",
            EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(new
            {
                reason = exception.Message,
                exception = exception.GetType().Name
            }),
            CorrelationId = correlationId
        }, ct);
    }
}
