using System.Text.Json;

using Aris.Application.Addresses;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Addresses;

/// <summary>Address registration and lookup (<c>ADR-01</c>).</summary>
public sealed class AddressService : IAddressService
{
    private const string CreateAction = "CREATE_ADDRESS";

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;
    private readonly IValidator<CreateAddressDto> _validator;

    public AddressService(
        ArisDbContext context,
        IAuditService audit,
        IValidator<CreateAddressDto> validator)
    {
        _context = context;
        _audit = audit;
        _validator = validator;
    }

    public async Task<Result<int>> CreateAsync(
        CreateAddressDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        if (!await IsActiveActorAsync(currentUserId, ct))
        {
            return Result<int>.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result<int>.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var addressCode = dto.AddressCode.Trim();

        if (await _context.Addresses.AnyAsync(a => a.AddressCode == addressCode, ct))
        {
            return Result<int>.Failure($"Address code {addressCode} is already registered.");
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var address = new Address
            {
                AddressCode = addressCode,
                AddressLine = dto.AddressLine.Trim(),
                Ward = dto.Ward.Trim(),
                District = string.IsNullOrWhiteSpace(dto.District) ? null : dto.District.Trim(),
                Province = dto.Province.Trim(),
                Status = "ACTIVE",
                ValidFrom = dto.ValidFrom,
                CreatedAt = now
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync(ct);

            await _audit.LogAsync(new AuditEntry
            {
                ActorUserId = currentUserId,
                Action = CreateAction,
                EntityType = "Address",
                EntityId = address.AddressId,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    address.AddressCode,
                    address.AddressLine,
                    address.Ward,
                    address.District,
                    address.Province
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(address.AddressId);
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, correlationId, ex, ct);
            return Result<int>.Failure($"The address could not be registered: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<AddressDto>>> GetActiveAsync(
        CancellationToken ct = default)
    {
        var addresses = await _context.Addresses
            .AsNoTracking()
            .Where(a => a.Status == "ACTIVE")
            .OrderBy(a => a.AddressText)
            .Select(a => new AddressDto
            {
                AddressId = a.AddressId,
                AddressCode = a.AddressCode,
                AddressText = a.AddressText,
                Ward = a.Ward,
                District = a.District,
                Province = a.Province,
                Status = a.Status,
                ValidFrom = a.ValidFrom
            })
            .ToListAsync(ct);

        return Result<IEnumerable<AddressDto>>.Success(addresses);
    }

    private async Task<bool> IsActiveActorAsync(int userId, CancellationToken ct)
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId && u.Status == "ACTIVE", ct);
    }

    private async Task LogFailureAsync(
        int actorUserId,
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
            Action = CreateAction,
            EntityType = "Address",
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
