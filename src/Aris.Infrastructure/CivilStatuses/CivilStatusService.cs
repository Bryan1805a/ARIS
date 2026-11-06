using System.Text.Json;

using Aris.Application.Auditing;
using Aris.Application.CivilStatuses;
using Aris.Application.CivilStatuses.Dtos;
using Aris.Application.Common;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.CivilStatuses;

public sealed class CivilStatusService : ICivilStatusService {
    private const string RegisterAction = "REGISTER_CIVIL_STATUS";
    private const string CancelAction = "CANCEL_CIVIL_STATUS";
    private const int MaxSearchResults = 500;

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;
    private readonly IValidator<RecordCivilStatusDto> _registerValidator;
    private readonly IValidator<CancelCivilStatusDto> _cancelValidator;

    public CivilStatusService(
        ArisDbContext context,
        IAuditService audit,
        IValidator<RecordCivilStatusDto> registerValidator,
        IValidator<CancelCivilStatusDto> cancelValidator) {
        _context = context;
        _audit = audit;
        _registerValidator = registerValidator;
        _cancelValidator = cancelValidator;
    }

    public async Task<Result<IEnumerable<CivilStatusRecordDto>>> SearchAsync(
        CivilStatusSearchCriteria criteria,
        CancellationToken ct = default) {
        try {
            var query =
                from cs in _context.CivilStatusRecords.AsNoTracking()
                join c in _context.Citizens.AsNoTracking()
                    on cs.CitizenId equals c.CitizenId
                join rc in _context.Citizens.AsNoTracking()
                    on cs.RelatedCitizenId equals rc.CitizenId into rcJoin
                from related in rcJoin.DefaultIfEmpty()
                join a in _context.Addresses.AsNoTracking()
                    on cs.AddressId equals a.AddressId into aJoin
                from address in aJoin.DefaultIfEmpty()
                join u in _context.UserAccounts.AsNoTracking()
                    on cs.CreatedBy equals u.UserId into uJoin
                from creator in uJoin.DefaultIfEmpty()
                join cu in _context.UserAccounts.AsNoTracking()
                    on cs.CancelledBy equals cu.UserId into cuJoin
                from canceller in cuJoin.DefaultIfEmpty()
                select new {
                    cs.CivilStatusId,
                    cs.RecordType,
                    cs.CitizenId,
                    CitizenNationalId = c.NationalId,
                    CitizenFullName = c.FullName,
                    cs.RelatedCitizenId,
                    RelatedNationalId = related != null ? related.NationalId : null,
                    RelatedFullName = related != null ? related.FullName : null,
                    cs.AddressId,
                    AddressText = address != null ? address.AddressText : null,
                    cs.EventDate,
                    cs.RegistrationDate,
                    cs.DocumentNumber,
                    cs.Status,
                    cs.Notes,
                    cs.CancelReason,
                    cs.CreatedAt,
                    CreatedByUsername = creator != null ? creator.Username : "System",
                    cs.CancelledAt,
                    CancelledByUsername = canceller != null ? canceller.Username : null
                };

            if (!string.IsNullOrWhiteSpace(criteria.SearchText)) {
                var term = criteria.SearchText.Trim();
                query = query.Where(x =>
                    x.CitizenNationalId.Contains(term) ||
                    x.CitizenFullName.Contains(term) ||
                    (x.RelatedNationalId != null && x.RelatedNationalId.Contains(term)) ||
                    (x.RelatedFullName != null && x.RelatedFullName.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(criteria.RecordType) &&
                !string.Equals(criteria.RecordType, "All", StringComparison.OrdinalIgnoreCase)) {
                var type = criteria.RecordType.Trim().ToUpperInvariant();
                query = query.Where(x => x.RecordType == type);
            }

            if (!string.IsNullOrWhiteSpace(criteria.Status) &&
                !string.Equals(criteria.Status, "All", StringComparison.OrdinalIgnoreCase)) {
                var status = criteria.Status.Trim().ToUpperInvariant();
                query = query.Where(x => x.Status == status);
            }

            if (criteria.FromDate is DateOnly from) {
                query = query.Where(x => x.EventDate >= from);
            }

            if (criteria.ToDate is DateOnly to) {
                query = query.Where(x => x.EventDate <= to);
            }

            var rows = await query
                .OrderByDescending(x => x.EventDate)
                .ThenByDescending(x => x.CivilStatusId)
                .Take(MaxSearchResults)
                .ToListAsync(ct);

            var dtos = rows.Select(x => new CivilStatusRecordDto(
                x.CivilStatusId,
                x.RecordType,
                x.CitizenId,
                x.CitizenNationalId,
                x.CitizenFullName,
                x.RelatedCitizenId,
                x.RelatedNationalId,
                x.RelatedFullName,
                x.AddressId,
                x.AddressText,
                x.EventDate,
                x.RegistrationDate,
                x.DocumentNumber,
                x.Status,
                x.Notes,
                x.CancelReason,
                x.CreatedAt,
                x.CreatedByUsername,
                x.CancelledAt,
                x.CancelledByUsername));

            return Result<IEnumerable<CivilStatusRecordDto>>.Success(dtos);
        } catch (Exception ex) {
            return Result<IEnumerable<CivilStatusRecordDto>>.Failure(
                $"Failed to search civil status records: {ex.Message}");
        }
    }

    public async Task<Result<CivilStatusRecordDto?>> GetByIdAsync(
        int civilStatusId,
        CancellationToken ct = default) {
        try {
            var row = await (
                from cs in _context.CivilStatusRecords.AsNoTracking()
                where cs.CivilStatusId == civilStatusId
                join c in _context.Citizens.AsNoTracking()
                    on cs.CitizenId equals c.CitizenId
                join rc in _context.Citizens.AsNoTracking()
                    on cs.RelatedCitizenId equals rc.CitizenId into rcJoin
                from related in rcJoin.DefaultIfEmpty()
                join a in _context.Addresses.AsNoTracking()
                    on cs.AddressId equals a.AddressId into aJoin
                from address in aJoin.DefaultIfEmpty()
                join u in _context.UserAccounts.AsNoTracking()
                    on cs.CreatedBy equals u.UserId into uJoin
                from creator in uJoin.DefaultIfEmpty()
                join cu in _context.UserAccounts.AsNoTracking()
                    on cs.CancelledBy equals cu.UserId into cuJoin
                from canceller in cuJoin.DefaultIfEmpty()
                select new {
                    cs.CivilStatusId,
                    cs.RecordType,
                    cs.CitizenId,
                    CitizenNationalId = c.NationalId,
                    CitizenFullName = c.FullName,
                    cs.RelatedCitizenId,
                    RelatedNationalId = related != null ? related.NationalId : null,
                    RelatedFullName = related != null ? related.FullName : null,
                    cs.AddressId,
                    AddressText = address != null ? address.AddressText : null,
                    cs.EventDate,
                    cs.RegistrationDate,
                    cs.DocumentNumber,
                    cs.Status,
                    cs.Notes,
                    cs.CancelReason,
                    cs.CreatedAt,
                    CreatedByUsername = creator != null ? creator.Username : "System",
                    cs.CancelledAt,
                    CancelledByUsername = canceller != null ? canceller.Username : null
                }).FirstOrDefaultAsync(ct);

            if (row is null) {
                return Result<CivilStatusRecordDto?>.Success(null);
            }

            return Result<CivilStatusRecordDto?>.Success(new CivilStatusRecordDto(
                row.CivilStatusId,
                row.RecordType,
                row.CitizenId,
                row.CitizenNationalId,
                row.CitizenFullName,
                row.RelatedCitizenId,
                row.RelatedNationalId,
                row.RelatedFullName,
                row.AddressId,
                row.AddressText,
                row.EventDate,
                row.RegistrationDate,
                row.DocumentNumber,
                row.Status,
                row.Notes,
                row.CancelReason,
                row.CreatedAt,
                row.CreatedByUsername,
                row.CancelledAt,
                row.CancelledByUsername));
        } catch (Exception ex) {
            return Result<CivilStatusRecordDto?>.Failure(
                $"Failed to load civil status record: {ex.Message}");
        }
    }

    public async Task<Result<int>> RegisterAsync(
        RecordCivilStatusDto dto,
        int currentUserId,
        CancellationToken ct = default) {
        var validation = await _registerValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) {
            return Result<int>.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        if (!await IsActiveActorAsync(currentUserId, ct)) {
            return Result<int>.Failure(
                "The authenticated user was not found or is not active.");
        }

        var recordType = dto.RecordType.Trim().ToUpperInvariant();
        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try {
            var citizen = await _context.Citizens
                .FirstOrDefaultAsync(c => c.CitizenId == dto.CitizenId, ct);

            if (citizen is null) {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure("Citizen not found.");
            }

            if (citizen.Status == "INACTIVE") {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "A civil status record cannot be registered for an inactive citizen.");
            }

            Citizen? related = null;

            if (recordType == "MARRIAGE") {
                if (dto.RelatedCitizenId is not int relatedId) {
                    await TryRollbackAsync(transaction, ct);
                    return Result<int>.Failure(
                        "A spouse must be selected for a marriage record.");
                }

                related = await _context.Citizens
                    .FirstOrDefaultAsync(c => c.CitizenId == relatedId, ct);

                if (related is null) {
                    await TryRollbackAsync(transaction, ct);
                    return Result<int>.Failure(
                        "The related citizen (spouse) was not found.");
                }

                if (related.Status == "INACTIVE") {
                    await TryRollbackAsync(transaction, ct);
                    return Result<int>.Failure(
                        "The related citizen (spouse) is inactive.");
                }

                if (related.CitizenId == citizen.CitizenId) {
                    await TryRollbackAsync(transaction, ct);
                    return Result<int>.Failure(
                        "A citizen cannot be married to themselves.");
                }

                var first = Math.Min(citizen.CitizenId, related.CitizenId);
                var second = Math.Max(citizen.CitizenId, related.CitizenId);

                var alreadyMarried = await _context.CivilStatusRecords.AnyAsync(x =>
                    x.RecordType == "MARRIAGE" &&
                    x.Status == "ACTIVE" &&
                    (x.CitizenId == first || x.RelatedCitizenId == first ||
                     x.CitizenId == second || x.RelatedCitizenId == second), ct);

                if (alreadyMarried) {
                    await TryRollbackAsync(transaction, ct);
                    return Result<int>.Failure(
                        "One of the citizens already has an active marriage record.");
                }
            }

            var relatedCitizenId = related?.CitizenId;
            var hasActiveDeath = await _context.CivilStatusRecords.AnyAsync(x =>
                x.RecordType == "DEATH" &&
                x.Status == "ACTIVE" &&
                (x.CitizenId == citizen.CitizenId ||
                 (relatedCitizenId != null && x.CitizenId == relatedCitizenId)), ct);

            if (hasActiveDeath && recordType != "DEATH") {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "A citizen with an active death record cannot have another civil status event.");
            }

            if (recordType == "DEATH") {
                var hasDeath = await _context.CivilStatusRecords.AnyAsync(x =>
                    x.RecordType == "DEATH" &&
                    x.Status == "ACTIVE" &&
                    x.CitizenId == citizen.CitizenId, ct);

                if (hasDeath) {
                    await TryRollbackAsync(transaction, ct);
                    return Result<int>.Failure(
                        "An active death record already exists for this citizen.");
                }
            }

            if (recordType == "BIRTH") {
                var hasBirth = await _context.CivilStatusRecords.AnyAsync(x =>
                    x.RecordType == "BIRTH" &&
                    x.Status == "ACTIVE" &&
                    x.CitizenId == citizen.CitizenId, ct);

                if (hasBirth) {
                    await TryRollbackAsync(transaction, ct);
                    return Result<int>.Failure(
                        "An active birth record already exists for this citizen.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.DocumentNumber)) {
                var documentNumber = dto.DocumentNumber.Trim();
                var duplicateDocument = await _context.CivilStatusRecords
                    .AnyAsync(x => x.DocumentNumber == documentNumber, ct);

                if (duplicateDocument) {
                    await TryRollbackAsync(transaction, ct);
                    return Result<int>.Failure(
                        "The document number is already used by another civil status record.");
                }
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == dto.AddressId, ct);

            if (address is null || address.Status != "ACTIVE") {
                await TryRollbackAsync(transaction, ct);
                return Result<int>.Failure(
                    "The selected address is not found or is no longer active.");
            }

            var entity = new CivilStatusRecord {
                RecordType = recordType,
                CitizenId = recordType == "MARRIAGE"
                    ? Math.Min(citizen.CitizenId, related!.CitizenId)
                    : citizen.CitizenId,
                RelatedCitizenId = recordType == "MARRIAGE"
                    ? Math.Max(citizen.CitizenId, related!.CitizenId)
                    : null,
                AddressId = dto.AddressId,
                EventDate = dto.EventDate,
                RegistrationDate = dto.RegistrationDate,
                DocumentNumber = string.IsNullOrWhiteSpace(dto.DocumentNumber)
                    ? null
                    : dto.DocumentNumber.Trim(),
                Status = "ACTIVE",
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                CreatedBy = currentUserId,
                CreatedAt = now
            };

            _context.CivilStatusRecords.Add(entity);
            await _context.SaveChangesAsync(ct);

            await _audit.LogAsync(new AuditEntry {
                ActorUserId = currentUserId,
                Action = RegisterAction,
                EntityType = "CivilStatusRecord",
                EntityId = entity.CivilStatusId,
                DetailsJson = JsonSerializer.Serialize(new {
                    entity.CivilStatusId,
                    entity.RecordType,
                    entity.CitizenId,
                    entity.RelatedCitizenId,
                    entity.AddressId,
                    entity.EventDate,
                    entity.RegistrationDate,
                    entity.DocumentNumber
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(entity.CivilStatusId);
        } catch (DbUpdateConcurrencyException) {
            await TryRollbackAsync(transaction, ct);
            return Result<int>.Failure(
                "This record was changed by another user. Reload it and try again.");
        } catch (Exception ex) {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, RegisterAction, null, correlationId, ex, ct);
            return Result<int>.Failure($"Failed to register civil status: {ex.Message}");
        }
    }

    public async Task<Result> CancelAsync(
        int civilStatusId,
        CancelCivilStatusDto dto,
        int currentUserId,
        CancellationToken ct = default) {
        var validation = await _cancelValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) {
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        if (!await IsActiveActorAsync(currentUserId, ct)) {
            return Result.Failure(
                "The authenticated user was not found or is not active.");
        }

        var record = await _context.CivilStatusRecords
            .FirstOrDefaultAsync(x => x.CivilStatusId == civilStatusId, ct);

        if (record is null) {
            return Result.Failure("Civil status record not found.");
        }

        if (record.Status != "ACTIVE") {
            return Result.Failure(
                $"Only an active record can be cancelled. Current status: {record.Status}.");
        }

        if (record.RecordType == "DEATH") {
            return Result.Failure(
                "A death registration cannot be cancelled. Correct it through the " +
                "administrative correction procedure.");
        }

        var correlationId = Guid.NewGuid();

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try {
            record.Status = "CANCELLED";
            record.CancelledBy = currentUserId;
            record.CancelledAt = DateTime.UtcNow;
            record.CancelReason = dto.Reason.Trim();

            await _context.SaveChangesAsync(ct);

            await _audit.LogAsync(new AuditEntry {
                ActorUserId = currentUserId,
                Action = CancelAction,
                EntityType = "CivilStatusRecord",
                EntityId = record.CivilStatusId,
                DetailsJson = JsonSerializer.Serialize(new {
                    record.CivilStatusId,
                    record.RecordType,
                    record.CitizenId,
                    record.RelatedCitizenId,
                    reason = record.CancelReason
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result.Success();
        } catch (DbUpdateConcurrencyException) {
            await TryRollbackAsync(transaction, ct);
            return Result.Failure(
                "This record was changed by another user. Reload it and try again.");
        } catch (Exception ex) {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(
                currentUserId, CancelAction, civilStatusId, correlationId, ex, ct);
            return Result.Failure($"Failed to cancel civil status: {ex.Message}");
        }
    }

    private async Task<bool> IsActiveActorAsync(int userId, CancellationToken ct) {
        return await _context.UserAccounts
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId && u.Status == "ACTIVE", ct);
    }

    private static async Task TryRollbackAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken ct) {
        try {
            await transaction.RollbackAsync(ct);
        } catch {
            // A trigger may already have rolled the transaction back;
            // keep the original error.
        }
    }

    private async Task LogFailureAsync(
        int actorUserId,
        string action,
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
            EntityType = "CivilStatusRecord",
            EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(new {
                reason = exception.Message,
                exception = exception.GetType().Name
            }),
            CorrelationId = correlationId
        }, ct);
    }
}