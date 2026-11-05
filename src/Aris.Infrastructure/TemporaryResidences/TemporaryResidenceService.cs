using System.Text.Json;
using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.TemporaryResidences;
using Aris.Application.TemporaryResidences.Dtos;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.TemporaryResidences;

public sealed class TemporaryResidenceService : ITemporaryResidenceService
{
    private const string RegisterAction = "REGISTER_TEMP_RESIDENCE";
    private const string UpdateAction = "UPDATE_TEMP_RESIDENCE";
    private const string CancelAction = "CANCEL_TEMP_RESIDENCE";

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;
    private readonly IValidator<RegisterTemporaryResidenceDto> _registerValidator;
    private readonly IValidator<UpdateTemporaryResidenceDto> _updateValidator;

    public TemporaryResidenceService(
        ArisDbContext context,
        IAuditService audit,
        IValidator<RegisterTemporaryResidenceDto> registerValidator,
        IValidator<UpdateTemporaryResidenceDto> updateValidator)
    {
        _context = context;
        _audit = audit;
        _registerValidator = registerValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<IEnumerable<TemporaryResidenceDto>>> SearchAsync(
        TemporaryResidenceSearchCriteria criteria,
        CancellationToken ct = default)
    {
        try
        {
            var query = from tr in _context.TemporaryResidences.AsNoTracking()
                        join c in _context.Citizens.AsNoTracking() on tr.CitizenId equals c.CitizenId
                        join a in _context.Addresses.AsNoTracking() on tr.AddressId equals a.AddressId
                        join u in _context.UserAccounts.AsNoTracking() on tr.CreatedBy equals u.UserId into uc
                        from creator in uc.DefaultIfEmpty()
                        select new
                        {
                            tr.TempResId,
                            tr.CitizenId,
                            c.NationalId,
                            c.FullName,
                            tr.AddressId,
                            AddressText = a.AddressText ?? a.AddressLine + ", " + a.Ward + ", " + a.Province,
                            tr.StartDate,
                            tr.EndDate,
                            tr.StayReason,
                            tr.HostInfo,
                            tr.Status,
                            tr.Notes,
                            tr.CreatedAt,
                            CreatedByUsername = creator != null ? creator.Username : "System"
                        };

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var term = criteria.SearchText.Trim();
                query = query.Where(x => x.NationalId.Contains(term) || x.FullName.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(criteria.Status) && !string.Equals(criteria.Status, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Status == criteria.Status);
            }

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .ToListAsync(ct);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dtos = items.Select(x =>
            {
                // Auto compute expired if active but passed end date
                var status = x.Status;
                if (status == "ACTIVE" && x.EndDate < today)
                {
                    status = "EXPIRED";
                }

                return new TemporaryResidenceDto(
                    x.TempResId,
                    x.CitizenId,
                    x.NationalId,
                    x.FullName,
                    x.AddressId,
                    x.AddressText,
                    x.StartDate,
                    x.EndDate,
                    x.StayReason,
                    x.HostInfo,
                    status,
                    x.Notes,
                    x.CreatedAt,
                    x.CreatedByUsername
                );
            });

            return Result<IEnumerable<TemporaryResidenceDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<TemporaryResidenceDto>>.Failure($"Failed to search temporary residences: {ex.Message}");
        }
    }

    public async Task<Result<TemporaryResidenceDto?>> GetByIdAsync(
        int tempResId,
        CancellationToken ct = default)
    {
        try
        {
            var item = await (from tr in _context.TemporaryResidences.AsNoTracking()
                              where tr.TempResId == tempResId
                              join c in _context.Citizens.AsNoTracking() on tr.CitizenId equals c.CitizenId
                              join a in _context.Addresses.AsNoTracking() on tr.AddressId equals a.AddressId
                              join u in _context.UserAccounts.AsNoTracking() on tr.CreatedBy equals u.UserId into uc
                              from creator in uc.DefaultIfEmpty()
                              select new
                              {
                                  tr.TempResId,
                                  tr.CitizenId,
                                  c.NationalId,
                                  c.FullName,
                                  tr.AddressId,
                                  AddressText = a.AddressText ?? a.AddressLine + ", " + a.Ward + ", " + a.Province,
                                  tr.StartDate,
                                  tr.EndDate,
                                  tr.StayReason,
                                  tr.HostInfo,
                                  tr.Status,
                                  tr.Notes,
                                  tr.CreatedAt,
                                  CreatedByUsername = creator != null ? creator.Username : "System"
                              }).FirstOrDefaultAsync(ct);

            if (item == null)
            {
                return Result<TemporaryResidenceDto?>.Success(null);
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var status = item.Status;
            if (status == "ACTIVE" && item.EndDate < today)
            {
                status = "EXPIRED";
            }

            var dto = new TemporaryResidenceDto(
                item.TempResId,
                item.CitizenId,
                item.NationalId,
                item.FullName,
                item.AddressId,
                item.AddressText,
                item.StartDate,
                item.EndDate,
                item.StayReason,
                item.HostInfo,
                status,
                item.Notes,
                item.CreatedAt,
                item.CreatedByUsername
            );

            return Result<TemporaryResidenceDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<TemporaryResidenceDto?>.Failure($"Failed to get temporary residence details: {ex.Message}");
        }
    }

    public async Task<Result<int>> RegisterAsync(
        RegisterTemporaryResidenceDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        var validation = await _registerValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result<int>.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var citizen = await _context.Citizens.FirstOrDefaultAsync(c => c.NationalId == dto.NationalId.Trim(), ct);
        if (citizen == null)
        {
            return Result<int>.Failure($"Citizen with National ID {dto.NationalId} is not found in the system. Please register citizen first.");
        }

        var address = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == dto.AddressId, ct);
        if (address == null || address.Status != "ACTIVE")
        {
            return Result<int>.Failure("The selected temporary residence address is not found or is no longer active.");
        }

        // Check if there is an existing active temporary residence at this address
        var hasActiveOverlap = await _context.TemporaryResidences.AnyAsync(
            tr => tr.CitizenId == citizen.CitizenId &&
                  tr.AddressId == dto.AddressId &&
                  tr.Status == "ACTIVE" &&
                  tr.EndDate >= dto.StartDate,
            ct);

        if (hasActiveOverlap)
        {
            return Result<int>.Failure("Citizen already has an active temporary residence registration at this address.");
        }

        var correlationId = Guid.NewGuid();
        var tempRes = new TemporaryResidence
        {
            CitizenId = citizen.CitizenId,
            AddressId = dto.AddressId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            StayReason = dto.StayReason.Trim(),
            HostInfo = string.IsNullOrWhiteSpace(dto.HostInfo) ? null : dto.HostInfo.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            Status = "ACTIVE",
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            _context.TemporaryResidences.Add(tempRes);
            await _context.SaveChangesAsync(ct);

            var auditEntry = new AuditEntry
            {
                ActorUserId = currentUserId,
                Action = RegisterAction,
                EntityType = nameof(TemporaryResidence),
                EntityId = tempRes.TempResId,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    tempRes.TempResId,
                    tempRes.CitizenId,
                    citizen.NationalId,
                    citizen.FullName,
                    tempRes.AddressId,
                    tempRes.StartDate,
                    tempRes.EndDate,
                    tempRes.StayReason,
                    tempRes.HostInfo
                }),
                CorrelationId = correlationId
            };

            await _audit.LogAsync(auditEntry, ct);
            await tx.CommitAsync(ct);

            return Result<int>.Success(tempRes.TempResId);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return Result<int>.Failure($"Failed to register temporary residence: {ex.Message}");
        }
    }

    public async Task<Result> UpdateAsync(
        int tempResId,
        UpdateTemporaryResidenceDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var tempRes = await _context.TemporaryResidences.FirstOrDefaultAsync(tr => tr.TempResId == tempResId, ct);
        if (tempRes == null)
        {
            return Result.Failure("Temporary residence record not found.");
        }

        if (tempRes.Status != "ACTIVE")
        {
            return Result.Failure($"Only active temporary residence records can be updated or extended. Current status: {tempRes.Status}.");
        }

        if (dto.EndDate < tempRes.StartDate)
        {
            return Result.Failure("Extended end date cannot be earlier than start date.");
        }

        var correlationId = Guid.NewGuid();
        var oldEndDate = tempRes.EndDate;
        tempRes.EndDate = dto.EndDate;
        tempRes.StayReason = dto.StayReason.Trim();
        tempRes.HostInfo = string.IsNullOrWhiteSpace(dto.HostInfo) ? null : dto.HostInfo.Trim();
        tempRes.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await _context.SaveChangesAsync(ct);

            var auditEntry = new AuditEntry
            {
                ActorUserId = currentUserId,
                Action = UpdateAction,
                EntityType = nameof(TemporaryResidence),
                EntityId = tempRes.TempResId,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    tempRes.TempResId,
                    OldEndDate = oldEndDate,
                    NewEndDate = tempRes.EndDate,
                    tempRes.StayReason,
                    tempRes.HostInfo
                }),
                CorrelationId = correlationId
            };

            await _audit.LogAsync(auditEntry, ct);
            await tx.CommitAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return Result.Failure($"Failed to update temporary residence: {ex.Message}");
        }
    }

    public async Task<Result> CancelAsync(
        int tempResId,
        string? cancelReason,
        int currentUserId,
        CancellationToken ct = default)
    {
        var tempRes = await _context.TemporaryResidences.FirstOrDefaultAsync(tr => tr.TempResId == tempResId, ct);
        if (tempRes == null)
        {
            return Result.Failure("Temporary residence record not found.");
        }

        if (tempRes.Status != "ACTIVE")
        {
            return Result.Failure($"Record is already {tempRes.Status}. Only active records can be cancelled.");
        }

        var correlationId = Guid.NewGuid();
        tempRes.Status = "CANCELLED";
        tempRes.EndedBy = currentUserId;
        tempRes.EndedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(cancelReason))
        {
            tempRes.Notes = string.IsNullOrWhiteSpace(tempRes.Notes)
                ? $"[Cancelled: {cancelReason.Trim()}]"
                : $"{tempRes.Notes} [Cancelled: {cancelReason.Trim()}]";
        }

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await _context.SaveChangesAsync(ct);

            var auditEntry = new AuditEntry
            {
                ActorUserId = currentUserId,
                Action = CancelAction,
                EntityType = nameof(TemporaryResidence),
                EntityId = tempRes.TempResId,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    tempRes.TempResId,
                    CancelledBy = currentUserId,
                    CancelReason = cancelReason
                }),
                CorrelationId = correlationId
            };

            await _audit.LogAsync(auditEntry, ct);
            await tx.CommitAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return Result.Failure($"Failed to cancel temporary residence: {ex.Message}");
        }
    }
}
