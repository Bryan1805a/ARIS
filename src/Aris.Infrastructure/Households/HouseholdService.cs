using System.Text.Json;

using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Households;
using Aris.Application.Households.Dtos;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Households;

/// <summary>
/// Household lifecycle: atomic registration, add member, change head, and history
/// (<c>HH-01</c>, <c>HH-02</c>, <c>HH-04</c>, <c>HH-06</c>, <c>SRCH-02</c>).
/// </summary>
/// <remarks>
/// Every composite operation runs in one transaction. Close-and-open writes persist the
/// closures before inserting the new active rows so the filtered unique indexes never
/// observe two active heads or two active tenures (R1).
/// </remarks>
public sealed class HouseholdService : IHouseholdService
{
    private const string RegisterAction = "REGISTER_HOUSEHOLD";
    private const string AddMemberAction = "ADD_HOUSEHOLD_MEMBER";
    private const string ChangeHeadAction = "CHANGE_HOUSEHOLD_HEAD";
    private const int MaxSearchResults = 500;

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;
    private readonly IValidator<RegisterHouseholdDto> _registerValidator;
    private readonly IValidator<AddMemberDto> _addMemberValidator;
    private readonly IValidator<ChangeHeadDto> _changeHeadValidator;

    public HouseholdService(
        ArisDbContext context,
        IAuditService audit,
        IValidator<RegisterHouseholdDto> registerValidator,
        IValidator<AddMemberDto> addMemberValidator,
        IValidator<ChangeHeadDto> changeHeadValidator)
    {
        _context = context;
        _audit = audit;
        _registerValidator = registerValidator;
        _addMemberValidator = addMemberValidator;
        _changeHeadValidator = changeHeadValidator;
    }

    public async Task<Result<int>> RegisterHouseholdAsync(
        RegisterHouseholdDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        if (!await IsActiveActorAsync(currentUserId, ct))
        {
            return Result<int>.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _registerValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result<int>.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var citizen = await _context.Citizens
                .FirstOrDefaultAsync(c => c.CitizenId == dto.HeadCitizenId, ct);

            if (citizen is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("The selected citizen was not found.");
            }

            if (citizen.Status != "UNASSIGNED")
            {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "Only an UNASSIGNED citizen can become the head of a new household.");
            }

            var hasTenure = await _context.HouseholdMemberships.AnyAsync(
                    m => m.CitizenId == citizen.CitizenId && m.Status == "ACTIVE", ct)
                || await _context.Residences.AnyAsync(
                    r => r.CitizenId == citizen.CitizenId && r.Status == "ACTIVE", ct);

            if (hasTenure)
            {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "The citizen already has an active membership or residence. Run the invariant check.");
            }

            var address = await _context.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AddressId == dto.AddressId, ct);

            if (address is null || address.Status != "ACTIVE")
            {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("Select an active address.");
            }

            var household = new Household
            {
                Status = "ACTIVE",
                CreatedAt = now
            };

            _context.Households.Add(household);
            await _context.SaveChangesAsync(ct);

            _context.HouseholdAddresses.Add(new HouseholdAddress
            {
                HouseholdId = household.HouseholdId,
                AddressId = address.AddressId,
                StartDate = dto.EffectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = now
            });

            _context.HouseholdMemberships.Add(new HouseholdMembership
            {
                CitizenId = citizen.CitizenId,
                HouseholdId = household.HouseholdId,
                Role = "HEAD",
                StartDate = dto.EffectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = now
            });

            _context.Residences.Add(new Residence
            {
                CitizenId = citizen.CitizenId,
                AddressId = address.AddressId,
                StartDate = dto.EffectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = now
            });

            citizen.Status = "ACTIVE";
            citizen.UpdatedAt = now;

            await _context.SaveChangesAsync(ct);

            await WriteAuditAsync(
                RegisterAction,
                "Household",
                household.HouseholdId,
                currentUserId,
                correlationId,
                new
                {
                    householdId = household.HouseholdId,
                    headCitizenId = citizen.CitizenId,
                    addressId = address.AddressId,
                    effectiveDate = dto.EffectiveDate
                },
                ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(household.HouseholdId);
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, RegisterAction, null, correlationId, ex, ct);
            return Result<int>.Failure($"The household could not be registered: {ex.Message}");
        }
    }

    public async Task<Result> AddMemberAsync(
        AddMemberDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        if (!await IsActiveActorAsync(currentUserId, ct))
        {
            return Result.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _addMemberValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var household = await _context.Households
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HouseholdId == dto.HouseholdId, ct);

            if (household is null || household.Status != "ACTIVE")
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("The household is not active.");
            }

            var citizen = await _context.Citizens
                .FirstOrDefaultAsync(c => c.CitizenId == dto.CitizenId, ct);

            if (citizen is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("The selected citizen was not found.");
            }

            if (citizen.Status != "UNASSIGNED")
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "Only an UNASSIGNED citizen can be added as a new member.");
            }

            var hasTenure = await _context.HouseholdMemberships.AnyAsync(
                    m => m.CitizenId == citizen.CitizenId && m.Status == "ACTIVE", ct)
                || await _context.Residences.AnyAsync(
                    r => r.CitizenId == citizen.CitizenId && r.Status == "ACTIVE", ct);

            if (hasTenure)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "The citizen already has an active membership or residence. Run the invariant check.");
            }

            var householdAddress = await _context.HouseholdAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.HouseholdId == household.HouseholdId
                    && a.Status == "ACTIVE", ct);

            if (householdAddress is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("The household has no active address.");
            }

            if (dto.EffectiveDate < householdAddress.StartDate)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "The effective date cannot be earlier than the household address start date.");
            }

            _context.HouseholdMemberships.Add(new HouseholdMembership
            {
                CitizenId = citizen.CitizenId,
                HouseholdId = household.HouseholdId,
                Role = "MEMBER",
                StartDate = dto.EffectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = now
            });

            _context.Residences.Add(new Residence
            {
                CitizenId = citizen.CitizenId,
                AddressId = householdAddress.AddressId,
                StartDate = dto.EffectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = now
            });

            citizen.Status = "ACTIVE";
            citizen.UpdatedAt = now;

            await _context.SaveChangesAsync(ct);

            await WriteAuditAsync(
                AddMemberAction,
                "Household",
                household.HouseholdId,
                currentUserId,
                correlationId,
                new
                {
                    householdId = household.HouseholdId,
                    citizenId = citizen.CitizenId,
                    addressId = householdAddress.AddressId,
                    effectiveDate = dto.EffectiveDate
                },
                ct);

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, AddMemberAction, dto.HouseholdId, correlationId, ex, ct);
            return Result.Failure($"The member could not be added: {ex.Message}");
        }
    }

    public async Task<Result> ChangeHeadAsync(
        ChangeHeadDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        if (!await IsActiveActorAsync(currentUserId, ct))
        {
            return Result.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _changeHeadValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var household = await _context.Households
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HouseholdId == dto.HouseholdId, ct);

            if (household is null || household.Status != "ACTIVE")
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("The household is not active.");
            }

            var currentHead = await _context.HouseholdMemberships
                .FirstOrDefaultAsync(m => m.HouseholdId == dto.HouseholdId
                    && m.Status == "ACTIVE"
                    && m.Role == "HEAD", ct);

            if (currentHead is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("The household has no active head. Run the invariant check.");
            }

            if (currentHead.CitizenId == dto.NewHeadCitizenId)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("The selected citizen is already the head.");
            }

            var newHead = await _context.HouseholdMemberships
                .FirstOrDefaultAsync(m => m.HouseholdId == dto.HouseholdId
                    && m.CitizenId == dto.NewHeadCitizenId
                    && m.Status == "ACTIVE"
                    && m.Role == "MEMBER", ct);

            if (newHead is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "The new head must be an active MEMBER of the same household.");
            }

            if (dto.EffectiveDate <= currentHead.StartDate
                || dto.EffectiveDate <= newHead.StartDate)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "The effective date must be later than both membership start dates.");
            }

            var previousHeadCitizenId = currentHead.CitizenId;

            // Close first, then open: the filtered unique head index must see no active head.
            currentHead.EndDate = dto.EffectiveDate.AddDays(-1);
            currentHead.Status = "ENDED";
            currentHead.EndedBy = currentUserId;
            currentHead.EndedAt = now;

            newHead.EndDate = dto.EffectiveDate.AddDays(-1);
            newHead.Status = "ENDED";
            newHead.EndedBy = currentUserId;
            newHead.EndedAt = now;

            await _context.SaveChangesAsync(ct);

            _context.HouseholdMemberships.Add(new HouseholdMembership
            {
                CitizenId = previousHeadCitizenId,
                HouseholdId = dto.HouseholdId,
                Role = "MEMBER",
                StartDate = dto.EffectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = now
            });

            _context.HouseholdMemberships.Add(new HouseholdMembership
            {
                CitizenId = dto.NewHeadCitizenId,
                HouseholdId = dto.HouseholdId,
                Role = "HEAD",
                StartDate = dto.EffectiveDate,
                Status = "ACTIVE",
                CreatedBy = currentUserId,
                CreatedAt = now
            });

            await _context.SaveChangesAsync(ct);

            await WriteAuditAsync(
                ChangeHeadAction,
                "Household",
                dto.HouseholdId,
                currentUserId,
                correlationId,
                new
                {
                    householdId = dto.HouseholdId,
                    previousHeadCitizenId,
                    newHeadCitizenId = dto.NewHeadCitizenId,
                    effectiveDate = dto.EffectiveDate
                },
                ct);

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, ChangeHeadAction, dto.HouseholdId, correlationId, ex, ct);
            return Result.Failure($"The head could not be changed: {ex.Message}");
        }
    }

    public async Task<Result<HouseholdDetailDto?>> GetByIdAsync(
        int householdId,
        CancellationToken ct = default)
    {
        var household = await _context.Households
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HouseholdId == householdId, ct);

        if (household is null)
        {
            return Result<HouseholdDetailDto?>.Success(null);
        }

        var currentAddress = await (
            from ha in _context.HouseholdAddresses.AsNoTracking()
            join a in _context.Addresses.AsNoTracking() on ha.AddressId equals a.AddressId
            where ha.HouseholdId == householdId && ha.Status == "ACTIVE"
            select new { ha.AddressId, a.AddressText }).FirstOrDefaultAsync(ct);

        var head = await (
            from m in _context.HouseholdMemberships.AsNoTracking()
            join c in _context.Citizens.AsNoTracking() on m.CitizenId equals c.CitizenId
            where m.HouseholdId == householdId && m.Status == "ACTIVE" && m.Role == "HEAD"
            select new { m.CitizenId, c.FullName }).FirstOrDefaultAsync(ct);

        var members = await (
            from m in _context.HouseholdMemberships.AsNoTracking()
            join c in _context.Citizens.AsNoTracking() on m.CitizenId equals c.CitizenId
            where m.HouseholdId == householdId && m.Status != "VOID"
            orderby m.Status == "ACTIVE" descending, m.StartDate descending, m.MembershipId descending
            select new HouseholdMemberDto
            {
                MembershipId = m.MembershipId,
                CitizenId = m.CitizenId,
                NationalId = c.NationalId,
                FullName = c.FullName,
                Role = m.Role,
                StartDate = m.StartDate,
                EndDate = m.EndDate,
                Status = m.Status
            }).ToListAsync(ct);

        var addressHistory = await (
            from ha in _context.HouseholdAddresses.AsNoTracking()
            join a in _context.Addresses.AsNoTracking() on ha.AddressId equals a.AddressId
            where ha.HouseholdId == householdId && ha.Status != "VOID"
            orderby ha.Status == "ACTIVE" descending, ha.StartDate descending
            select new HouseholdAddressHistoryDto
            {
                HouseholdAddressId = ha.HouseholdAddressId,
                AddressId = ha.AddressId,
                AddressText = a.AddressText,
                StartDate = ha.StartDate,
                EndDate = ha.EndDate,
                Status = ha.Status
            }).ToListAsync(ct);

        return Result<HouseholdDetailDto?>.Success(new HouseholdDetailDto
        {
            HouseholdId = household.HouseholdId,
            Status = household.Status,
            CreatedAt = household.CreatedAt,
            InactivatedAt = household.InactivatedAt,
            CurrentAddressId = currentAddress?.AddressId,
            CurrentAddressText = currentAddress?.AddressText,
            HeadCitizenId = head?.CitizenId,
            HeadFullName = head?.FullName,
            Members = members,
            AddressHistory = addressHistory
        });
    }

    public async Task<Result<IEnumerable<HouseholdDto>>> SearchAsync(
        HouseholdSearchCriteria criteria,
        CancellationToken ct = default)
    {
        var query = _context.Households.AsNoTracking().AsQueryable();

        if (criteria.HouseholdId is int householdId)
        {
            query = query.Where(h => h.HouseholdId == householdId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            var status = criteria.Status.Trim().ToUpperInvariant();
            if (status is not ("ACTIVE" or "INACTIVE"))
            {
                return Result<IEnumerable<HouseholdDto>>.Failure(
                    "Status must be ACTIVE or INACTIVE.");
            }

            query = query.Where(h => h.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var text = criteria.SearchText.Trim();

            query = query.Where(h =>
                _context.HouseholdMemberships.Any(m =>
                    m.HouseholdId == h.HouseholdId
                    && m.Status == "ACTIVE"
                    && m.Role == "HEAD"
                    && _context.Citizens.Any(c =>
                        c.CitizenId == m.CitizenId
                        && (c.FullName.Contains(text) || c.NationalId.Contains(text))))
                || _context.HouseholdAddresses.Any(ha =>
                    ha.HouseholdId == h.HouseholdId
                    && ha.Status == "ACTIVE"
                    && _context.Addresses.Any(a =>
                        a.AddressId == ha.AddressId && a.AddressText.Contains(text))));
        }

        var households = await query
            .OrderBy(h => h.HouseholdId)
            .Take(MaxSearchResults)
            .Select(h => new HouseholdDto
            {
                HouseholdId = h.HouseholdId,
                Status = h.Status,
                HeadCitizenId = _context.HouseholdMemberships
                    .Where(m => m.HouseholdId == h.HouseholdId
                        && m.Status == "ACTIVE"
                        && m.Role == "HEAD")
                    .Select(m => (int?)m.CitizenId)
                    .FirstOrDefault(),
                HeadFullName = (
                    from m in _context.HouseholdMemberships
                    join c in _context.Citizens on m.CitizenId equals c.CitizenId
                    where m.HouseholdId == h.HouseholdId
                        && m.Status == "ACTIVE"
                        && m.Role == "HEAD"
                    select c.FullName).FirstOrDefault(),
                CurrentAddressText = (
                    from ha in _context.HouseholdAddresses
                    join a in _context.Addresses on ha.AddressId equals a.AddressId
                    where ha.HouseholdId == h.HouseholdId && ha.Status == "ACTIVE"
                    select a.AddressText).FirstOrDefault(),
                ActiveMemberCount = _context.HouseholdMemberships
                    .Count(m => m.HouseholdId == h.HouseholdId && m.Status == "ACTIVE")
            })
            .ToListAsync(ct);

        return Result<IEnumerable<HouseholdDto>>.Success(households);
    }

    private async Task<bool> IsActiveActorAsync(int userId, CancellationToken ct)
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId && u.Status == "ACTIVE", ct);
    }

    private async Task WriteAuditAsync(
        string action,
        string entityType,
        int entityId,
        int actorUserId,
        Guid correlationId,
        object details,
        CancellationToken ct)
    {
        await _audit.LogAsync(new AuditEntry
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(details),
            CorrelationId = correlationId
        }, ct);
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
            EntityType = "Household",
            EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(new
            {
                reason = exception.Message,
                exception = exception.GetType().Name
            }),
            CorrelationId = correlationId
        }, ct);
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
}
