using System.Text.Json;

using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Rectifications;
using Aris.Application.Rectifications.Dtos;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Rectifications;

/// <summary>
/// Void-and-correct workflow for append-only tenure rows (<c>HIST-01</c>).
/// </summary>
/// <remarks>
/// <para>
/// One rectification is one transaction and two writes: the erroneous row becomes
/// <c>VOID</c>, then a replacement linked by <c>Corrects...Id</c> is inserted. The closure
/// is saved first so the filtered unique index never observes two active rows for the same
/// owner (<c>R1</c>).
/// </para>
/// <para>
/// An <c>ACTIVE</c> row may only have its start date corrected. Changing the address,
/// role, household, or end date of current state belongs to the operation that owns it
/// (transfer, change head, deactivation), because those operations also adjust the related
/// rows the Golden Invariant compares.
/// </para>
/// </remarks>
public sealed class RectificationService : IRectificationService {
    private const string VoidResidenceAction = "VOID_RESIDENCE";
    private const string CorrectResidenceAction = "CORRECT_RESIDENCE";
    private const string RectifyResidenceAction = "RECTIFY_RESIDENCE";

    private const string VoidMembershipAction = "VOID_MEMBERSHIP";
    private const string CorrectMembershipAction = "CORRECT_MEMBERSHIP";
    private const string RectifyMembershipAction = "RECTIFY_MEMBERSHIP";

    private const string VoidHouseholdAddressAction = "VOID_HOUSEHOLD_ADDRESS";
    private const string CorrectHouseholdAddressAction = "CORRECT_HOUSEHOLD_ADDRESS";
    private const string RectifyHouseholdAddressAction = "RECTIFY_HOUSEHOLD_ADDRESS";

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;
    private readonly IValidator<RectifyResidenceDto> _residenceValidator;
    private readonly IValidator<RectifyMembershipDto> _membershipValidator;
    private readonly IValidator<RectifyHouseholdAddressDto> _householdAddressValidator;

    public RectificationService(
        ArisDbContext context,
        IAuditService audit,
        IValidator<RectifyResidenceDto> residenceValidator,
        IValidator<RectifyMembershipDto> membershipValidator,
        IValidator<RectifyHouseholdAddressDto> householdAddressValidator) {
        _context = context;
        _audit = audit;
        _residenceValidator = residenceValidator;
        _membershipValidator = membershipValidator;
        _householdAddressValidator = householdAddressValidator;
    }

    // HIST-01: corrects an erroneous Residence row
    public async Task<Result<int>> RectifyResidenceAsync(
        RectifyResidenceDto dto,
        int currentUserId,
        CancellationToken ct = default) {
        if (!await IsActiveActorAsync(currentUserId, ct)) {
            return Result<int>.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _residenceValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) {
            return Result<int>.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try {
            var source = await _context.Residences
                .FirstOrDefaultAsync(r => r.ResidenceId == dto.ResidenceId, ct);

            if (source is null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("Residence not found.");
            }

            if (source.Status == "VOID") {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("A voided residence cannot be corrected again.");
            }

            var originalStatus = source.Status;
            var isActive = originalStatus == "ACTIVE";

            if (isActive && dto.AddressId != source.AddressId) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "An active residence cannot change address. Use a transfer to move the citizen.");
            }

            if (isActive && dto.EndDate is not null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "An active residence cannot have an end date. Use a transfer or deactivation.");
            }

            if (await _context.TransferRequestItems.AnyAsync(
                    i => i.SourceResidenceId == source.ResidenceId && i.Status == "PENDING", ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "A pending transfer references this residence. Decide or cancel the transfer first.");
            }

            if (!await _context.Addresses.AnyAsync(a => a.AddressId == dto.AddressId, ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("The selected address was not found.");
            }

            var effectiveEndDate = isActive
                ? null
                : dto.EndDate ?? source.EndDate;

            if (!isActive && effectiveEndDate is null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("An ended residence must keep an end date.");
            }

            if (effectiveEndDate is DateOnly endDate && endDate < dto.StartDate) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("End date must be on or after the start date.");
            }

            if (await OverlapsAnotherResidenceAsync(source, dto.StartDate, effectiveEndDate, ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "The corrected period overlaps another residence of this citizen.");
            }

            if (await _context.Residences.AnyAsync(
                    r => r.CorrectsResidenceId == source.ResidenceId && r.Status != "VOID", ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("This residence has already been corrected.");
            }

            var hasChanges = dto.AddressId != source.AddressId
                || dto.StartDate != source.StartDate
                || effectiveEndDate != source.EndDate;

            if (!hasChanges) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("The corrected values are identical to the current row.");
            }

            var before = new {
                source.CitizenId,
                source.AddressId,
                source.StartDate,
                source.EndDate,
                source.Status
            };

            // Save the VOID first: the filtered unique index must never see two active rows.
            source.Status = "VOID";
            await _context.SaveChangesAsync(ct);

            var corrected = new Residence {
                CitizenId = source.CitizenId,
                AddressId = dto.AddressId,
                StartDate = dto.StartDate,
                EndDate = effectiveEndDate,
                Status = originalStatus,
                CreatedBy = currentUserId,
                CreatedAt = now,
                EndedBy = isActive ? null : source.EndedBy ?? currentUserId,
                EndedAt = isActive ? null : source.EndedAt ?? now,
                OpenedByRequestId = source.OpenedByRequestId,
                CorrectsResidenceId = source.ResidenceId
            };

            _context.Residences.Add(corrected);
            await _context.SaveChangesAsync(ct);

            await _audit.LogAsync(new AuditEntry {
                ActorUserId = currentUserId,
                Action = VoidResidenceAction,
                EntityType = "Residence",
                EntityId = source.ResidenceId,
                DetailsJson = JsonSerializer.Serialize(new {
                    reason = dto.Reason.Trim(),
                    before,
                    correctedResidenceId = corrected.ResidenceId
                }),
                CorrelationId = correlationId
            }, ct);

            await _audit.LogAsync(new AuditEntry {
                ActorUserId = currentUserId,
                Action = CorrectResidenceAction,
                EntityType = "Residence",
                EntityId = corrected.ResidenceId,
                DetailsJson = JsonSerializer.Serialize(new {
                    correctedFromResidenceId = source.ResidenceId,
                    after = new {
                        corrected.CitizenId,
                        corrected.AddressId,
                        corrected.StartDate,
                        corrected.EndDate,
                        corrected.Status
                    }
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(corrected.ResidenceId);
        } catch (DbUpdateConcurrencyException ex) {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(
                currentUserId, RectifyResidenceAction, "Residence",
                dto.ResidenceId, correlationId, ex, ct);
            return Result<int>.Failure(
                "This residence was changed by another user. Reload the record and try again.");
        } catch (Exception ex) {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(
                currentUserId, RectifyResidenceAction, "Residence",
                dto.ResidenceId, correlationId, ex, ct);
            return Result<int>.Failure($"The residence could not be corrected: {ex.Message}");
        }
    }

    // HIST-01: corrects an erroneous HouseholdMembership row
    public async Task<Result<int>> RectifyMembershipAsync(
        RectifyMembershipDto dto,
        int currentUserId,
        CancellationToken ct = default) {
        if (!await IsActiveActorAsync(currentUserId, ct)) {
            return Result<int>.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _membershipValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) {
            return Result<int>.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try {
            var source = await _context.HouseholdMemberships
                .FirstOrDefaultAsync(m => m.MembershipId == dto.MembershipId, ct);

            if (source is null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("Membership not found.");
            }

            if (source.Status == "VOID") {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("A voided membership cannot be corrected again.");
            }

            var originalStatus = source.Status;
            var isActive = originalStatus == "ACTIVE";
            var role = dto.Role.Trim().ToUpperInvariant();

            if (isActive && dto.HouseholdId != source.HouseholdId) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "An active membership cannot change household. Use a transfer.");
            }

            if (isActive && role != source.Role) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "An active membership cannot change role. Use the change-head operation.");
            }

            if (isActive && dto.EndDate is not null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "An active membership cannot have an end date. Use a transfer or deactivation.");
            }

            if (await _context.TransferRequestItems.AnyAsync(
                    i => i.SourceMembershipId == source.MembershipId && i.Status == "PENDING", ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "A pending transfer references this membership. Decide or cancel the transfer first.");
            }

            if (!await _context.Households.AnyAsync(h => h.HouseholdId == dto.HouseholdId, ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("The selected household was not found.");
            }

            var effectiveEndDate = isActive
                ? null
                : dto.EndDate ?? source.EndDate;

            if (!isActive && effectiveEndDate is null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("An ended membership must keep an end date.");
            }

            if (effectiveEndDate is DateOnly endDate && endDate < dto.StartDate) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("End date must be on or after the start date.");
            }

            if (await OverlapsAnotherMembershipAsync(source, dto.StartDate, effectiveEndDate, ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "The corrected period overlaps another membership of this citizen.");
            }

            if (await _context.HouseholdMemberships.AnyAsync(
                    m => m.CorrectsMembershipId == source.MembershipId && m.Status != "VOID", ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("This membership has already been corrected.");
            }

            var hasChanges = dto.HouseholdId != source.HouseholdId
                || role != source.Role
                || dto.StartDate != source.StartDate
                || effectiveEndDate != source.EndDate;

            if (!hasChanges) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("The corrected values are identical to the current row.");
            }

            var before = new {
                source.CitizenId,
                source.HouseholdId,
                source.Role,
                source.StartDate,
                source.EndDate,
                source.Status
            };

            // Save the VOID first: the filtered unique indexes must never see two active rows.
            source.Status = "VOID";
            await _context.SaveChangesAsync(ct);

            var corrected = new HouseholdMembership {
                CitizenId = source.CitizenId,
                HouseholdId = dto.HouseholdId,
                Role = role,
                StartDate = dto.StartDate,
                EndDate = effectiveEndDate,
                Status = originalStatus,
                CreatedBy = currentUserId,
                CreatedAt = now,
                EndedBy = isActive ? null : source.EndedBy ?? currentUserId,
                EndedAt = isActive ? null : source.EndedAt ?? now,
                OpenedByRequestId = source.OpenedByRequestId,
                CorrectsMembershipId = source.MembershipId
            };

            _context.HouseholdMemberships.Add(corrected);
            await _context.SaveChangesAsync(ct);

            await _audit.LogAsync(new AuditEntry {
                ActorUserId = currentUserId,
                Action = VoidMembershipAction,
                EntityType = "HouseholdMembership",
                EntityId = source.MembershipId,
                DetailsJson = JsonSerializer.Serialize(new {
                    reason = dto.Reason.Trim(),
                    before,
                    correctedMembershipId = corrected.MembershipId
                }),
                CorrelationId = correlationId
            }, ct);

            await _audit.LogAsync(new AuditEntry {
                ActorUserId = currentUserId,
                Action = CorrectMembershipAction,
                EntityType = "HouseholdMembership",
                EntityId = corrected.MembershipId,
                DetailsJson = JsonSerializer.Serialize(new {
                    correctedFromMembershipId = source.MembershipId,
                    after = new {
                        corrected.CitizenId,
                        corrected.HouseholdId,
                        corrected.Role,
                        corrected.StartDate,
                        corrected.EndDate,
                        corrected.Status
                    }
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(corrected.MembershipId);
        } catch (DbUpdateConcurrencyException ex) {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(
                currentUserId, RectifyMembershipAction, "HouseholdMembership",
                dto.MembershipId, correlationId, ex, ct);
            return Result<int>.Failure(
                "This membership was changed by another user. Reload the record and try again.");
        } catch (Exception ex) {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(
                currentUserId, RectifyMembershipAction, "HouseholdMembership",
                dto.MembershipId, correlationId, ex, ct);
            return Result<int>.Failure($"The membership could not be corrected: {ex.Message}");
        }
    }

    // HIST-01: corrects an erroneous HouseholdAddress row
    public async Task<Result<int>> RectifyHouseholdAddressAsync(
        RectifyHouseholdAddressDto dto,
        int currentUserId,
        CancellationToken ct = default) {
        if (!await IsActiveActorAsync(currentUserId, ct)) {
            return Result<int>.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _householdAddressValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) {
            return Result<int>.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try {
            var source = await _context.HouseholdAddresses
                .FirstOrDefaultAsync(h => h.HouseholdAddressId == dto.HouseholdAddressId, ct);

            if (source is null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("Household address not found.");
            }

            if (source.Status == "VOID") {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("A voided household address cannot be corrected again.");
            }

            var originalStatus = source.Status;
            var isActive = originalStatus == "ACTIVE";

            if (isActive && dto.AddressId != source.AddressId) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "An active household address cannot change. Use a household move instead.");
            }

            if (isActive && dto.EndDate is not null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "An active household address cannot have an end date. Use a household move.");
            }

            if (!await _context.Addresses.AnyAsync(a => a.AddressId == dto.AddressId, ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("The selected address was not found.");
            }

            var effectiveEndDate = isActive
                ? null
                : dto.EndDate ?? source.EndDate;

            if (!isActive && effectiveEndDate is null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("An ended household address must keep an end date.");
            }

            if (effectiveEndDate is DateOnly endDate && endDate < dto.StartDate) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("End date must be on or after the start date.");
            }

            if (await OverlapsAnotherHouseholdAddressAsync(source, dto.StartDate, effectiveEndDate, ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "The corrected period overlaps another address period of this household.");
            }

            if (await _context.HouseholdAddresses.AnyAsync(
                    h => h.CorrectsHouseholdAddressId == source.HouseholdAddressId
                        && h.Status != "VOID", ct)) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("This household address has already been corrected.");
            }

            var hasChanges = dto.AddressId != source.AddressId
                || dto.StartDate != source.StartDate
                || effectiveEndDate != source.EndDate;

            if (!hasChanges) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("The corrected values are identical to the current row.");
            }

            var before = new {
                source.HouseholdId,
                source.AddressId,
                source.StartDate,
                source.EndDate,
                source.Status
            };

            // Save the VOID first: the filtered unique index must never see two active rows.
            source.Status = "VOID";
            await _context.SaveChangesAsync(ct);

            var corrected = new HouseholdAddress {
                HouseholdId = source.HouseholdId,
                AddressId = dto.AddressId,
                StartDate = dto.StartDate,
                EndDate = effectiveEndDate,
                Status = originalStatus,
                CreatedBy = currentUserId,
                CreatedAt = now,
                EndedBy = isActive ? null : source.EndedBy ?? currentUserId,
                EndedAt = isActive ? null : source.EndedAt ?? now,
                OpenedByRequestId = source.OpenedByRequestId,
                CorrectsHouseholdAddressId = source.HouseholdAddressId
            };

            _context.HouseholdAddresses.Add(corrected);
            await _context.SaveChangesAsync(ct);

            await _audit.LogAsync(new AuditEntry {
                ActorUserId = currentUserId,
                Action = VoidHouseholdAddressAction,
                EntityType = "HouseholdAddress",
                EntityId = source.HouseholdAddressId,
                DetailsJson = JsonSerializer.Serialize(new {
                    reason = dto.Reason.Trim(),
                    before,
                    correctedHouseholdAddressId = corrected.HouseholdAddressId
                }),
                CorrelationId = correlationId
            }, ct);

            await _audit.LogAsync(new AuditEntry {
                ActorUserId = currentUserId,
                Action = CorrectHouseholdAddressAction,
                EntityType = "HouseholdAddress",
                EntityId = corrected.HouseholdAddressId,
                DetailsJson = JsonSerializer.Serialize(new {
                    correctedFromHouseholdAddressId = source.HouseholdAddressId,
                    after = new {
                        corrected.HouseholdId,
                        corrected.AddressId,
                        corrected.StartDate,
                        corrected.EndDate,
                        corrected.Status
                    }
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(corrected.HouseholdAddressId);
        } catch (DbUpdateConcurrencyException ex) {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(
                currentUserId, RectifyHouseholdAddressAction, "HouseholdAddress",
                dto.HouseholdAddressId, correlationId, ex, ct);
            return Result<int>.Failure(
                "This household address was changed by another user. Reload and try again.");
        } catch (Exception ex) {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(
                currentUserId, RectifyHouseholdAddressAction, "HouseholdAddress",
                dto.HouseholdAddressId, correlationId, ex, ct);
            return Result<int>.Failure($"The household address could not be corrected: {ex.Message}");
        }
    }

    private async Task<bool> OverlapsAnotherResidenceAsync(
        Residence source,
        DateOnly startDate,
        DateOnly? endDate,
        CancellationToken ct) {
        var others = await _context.Residences
            .AsNoTracking()
            .Where(r => r.CitizenId == source.CitizenId
                && r.ResidenceId != source.ResidenceId
                && r.Status != "VOID")
            .Select(r => new { r.StartDate, r.EndDate })
            .ToListAsync(ct);

        return others.Any(other => Overlaps(startDate, endDate, other.StartDate, other.EndDate));
    }

    private async Task<bool> OverlapsAnotherMembershipAsync(
        HouseholdMembership source,
        DateOnly startDate,
        DateOnly? endDate,
        CancellationToken ct) {
        var others = await _context.HouseholdMemberships
            .AsNoTracking()
            .Where(m => m.CitizenId == source.CitizenId
                && m.MembershipId != source.MembershipId
                && m.Status != "VOID")
            .Select(m => new { m.StartDate, m.EndDate })
            .ToListAsync(ct);

        return others.Any(other => Overlaps(startDate, endDate, other.StartDate, other.EndDate));
    }

    private async Task<bool> OverlapsAnotherHouseholdAddressAsync(
        HouseholdAddress source,
        DateOnly startDate,
        DateOnly? endDate,
        CancellationToken ct) {
        var others = await _context.HouseholdAddresses
            .AsNoTracking()
            .Where(h => h.HouseholdId == source.HouseholdId
                && h.HouseholdAddressId != source.HouseholdAddressId
                && h.Status != "VOID")
            .Select(h => new { h.StartDate, h.EndDate })
            .ToListAsync(ct);

        return others.Any(other => Overlaps(startDate, endDate, other.StartDate, other.EndDate));
    }

    /// <summary>
    /// Inclusive temporal overlap. An open interval extends to <see cref="DateOnly.MaxValue"/>,
    /// matching the guard triggers' <c>ISNULL(EndDate, '9999-12-31')</c>.
    /// </summary>
    private static bool Overlaps(
        DateOnly startDate,
        DateOnly? endDate,
        DateOnly otherStartDate,
        DateOnly? otherEndDate) {
        var thisEnd = endDate ?? DateOnly.MaxValue;
        var otherEnd = otherEndDate ?? DateOnly.MaxValue;
        return startDate <= otherEnd && otherStartDate <= thisEnd;
    }

    private async Task<bool> IsActiveActorAsync(int userId, CancellationToken ct) {
        return await _context.UserAccounts
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId && u.Status == "ACTIVE", ct);
    }

    // Helper: logs a failure on an independent connection (survives transaction rollback)
    private async Task LogFailureAsync(
        int actorUserId,
        string action,
        string entityType,
        int? entityId,
        Guid correlationId,
        Exception exception,
        CancellationToken ct) {
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) {
            return;
        }

        await _audit.LogFailureIndependentAsync(connectionString, new AuditFailureEntry {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(new {
                reason = exception.Message,
                exception = exception.GetType().Name
            }),
            CorrelationId = correlationId
        }, ct);
    }

    // Helper: safely rolls back, ignoring a trigger that already rolled the transaction back
    private static async Task TryRollbackAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken ct) {
        try {
            await transaction.RollbackAsync(ct);
        } catch {
            // A trigger may already have rolled the transaction back; keep the original error.
        }
    }
}