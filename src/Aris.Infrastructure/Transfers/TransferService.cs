using System.Data;
using System.Text.Json;

using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Transfers;
using Aris.Application.Transfers.Dtos;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Transfers;

/// <summary>
/// Maker-checker relocation engine (<c>UC-06</c>, <c>D1</c>, <c>D5</c>).
/// </summary>
/// <remarks>
/// Phase 1 creates a PENDING request and its items; no business state changes. Phase 2
/// executes a checker's decision in one <see cref="IsolationLevel.Serializable"/>
/// transaction: close old tenures, handle the source head/household, then open the
/// destination tenures. Partial approval is supported; rejected items remain in the
/// source household.
/// </remarks>
public sealed class TransferService : ITransferService
{
    private const string CreateAction = "CREATE_TRANSFER_REQUEST";
    private const string ExecuteAction = "EXECUTE_TRANSFER";
    private const string CancelAction = "CANCEL_TRANSFER_REQUEST";
    private const int MaxSearchResults = 500;

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;
    private readonly IValidator<CreateTransferRequestDto> _createValidator;
    private readonly IValidator<TransferDecisionDto> _decisionValidator;

    public TransferService(
        ArisDbContext context,
        IAuditService audit,
        IValidator<CreateTransferRequestDto> createValidator,
        IValidator<TransferDecisionDto> decisionValidator)
    {
        _context = context;
        _audit = audit;
        _createValidator = createValidator;
        _decisionValidator = decisionValidator;
    }

    public async Task<Result<int>> CreateRequestAsync(
        CreateTransferRequestDto dto,
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

        var type = dto.RequestType;
        var citizenIds = dto.CitizenIds.Distinct().ToList();

        if (citizenIds.Count != dto.CitizenIds.Count)
        {
            return Result<int>.Failure("Each citizen may appear only once in a request.");
        }

        var sourceHousehold = await _context.Households
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HouseholdId == dto.SourceHouseholdId, ct);

        if (sourceHousehold is null || sourceHousehold.Status != "ACTIVE")
        {
            return Result<int>.Failure("Select an active source household.");
        }

        var memberships = await _context.HouseholdMemberships
            .AsNoTracking()
            .Where(m => m.HouseholdId == dto.SourceHouseholdId && m.Status == "ACTIVE")
            .ToListAsync(ct);

        if (memberships.Count == 0)
        {
            return Result<int>.Failure("The source household has no active members.");
        }

        var sourceHead = memberships.FirstOrDefault(m => m.Role == "HEAD");
        if (sourceHead is null)
        {
            return Result<int>.Failure(
                "The source household has no active HEAD. Run the Golden Invariant check.");
        }

        var citizens = await _context.Citizens
            .AsNoTracking()
            .Where(c => citizenIds.Contains(c.CitizenId))
            .ToListAsync(ct);

        var selectedMemberships = new List<HouseholdMembership>();
        var selectedResidences = new List<Residence>();

        foreach (var citizenId in citizenIds)
        {
            var membership = memberships.FirstOrDefault(m => m.CitizenId == citizenId);
            if (membership is null)
            {
                return Result<int>.Failure(
                    $"Citizen {citizenId} is not an active member of the source household.");
            }

            var residence = await _context.Residences
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.CitizenId == citizenId && r.Status == "ACTIVE", ct);

            if (residence is null)
            {
                return Result<int>.Failure(
                    $"Citizen {citizenId} has no active residence. Run the Golden Invariant check.");
            }

            var citizen = citizens.FirstOrDefault(c => c.CitizenId == citizenId);
            if (citizen is null || citizen.Status != "ACTIVE")
            {
                return Result<int>.Failure($"Citizen {citizenId} is not ACTIVE.");
            }

            if (dto.EffectiveDate <= membership.StartDate
                || dto.EffectiveDate <= residence.StartDate)
            {
                return Result<int>.Failure(
                    $"The effective date must be later than citizen {citizenId}'s active tenure start.");
            }

            if (await _context.TransferRequestItems.AnyAsync(
                    i => i.CitizenId == citizenId && i.Status == "PENDING", ct))
            {
                return Result<int>.Failure(
                    $"Citizen {citizenId} already has a pending transfer item.");
            }

            selectedMemberships.Add(membership);
            selectedResidences.Add(residence);
        }

        if (type == "HOUSEHOLD_MOVE" && memberships.Count != citizenIds.Count)
        {
            return Result<int>.Failure(
                "A household move must include every active member of the source household.");
        }

        var remainingMembers = memberships
            .Where(m => !citizenIds.Contains(m.CitizenId))
            .ToList();

        var headLeaves = citizenIds.Contains(sourceHead.CitizenId);
        var successorId = dto.SuccessorHeadCitizenId;

        if (type != "HOUSEHOLD_MOVE" && headLeaves)
        {
            // When other members remain, the successor is required and can be validated
            // now. When every member is selected, a successor may still be supplied so
            // partial approval can reject one of them and keep the source household
            // headed; that candidate is validated at decision time.
            if (remainingMembers.Count > 0)
            {
                if (successorId is null)
                {
                    return Result<int>.Failure(
                        "The source head is leaving while other members remain. Choose a successor head.");
                }

                var successor = remainingMembers
                    .FirstOrDefault(m => m.CitizenId == successorId && m.Role == "MEMBER");

                if (successor is null)
                {
                    return Result<int>.Failure(
                        "The successor must be an active MEMBER who remains in the source household.");
                }

                if (dto.EffectiveDate <= successor.StartDate)
                {
                    return Result<int>.Failure(
                        "The effective date must be later than the successor's membership start.");
                }
            }
        }
        else if (successorId is not null)
        {
            return Result<int>.Failure(
                "A successor head is only valid when the source head leaves and the request is not a household move.");
        }

        if (type == "JOIN_EXISTING")
        {
            var destination = await _context.Households
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HouseholdId == dto.DestinationHouseholdId, ct);

            if (destination is null
                || destination.Status != "ACTIVE"
                || destination.HouseholdId == dto.SourceHouseholdId)
            {
                return Result<int>.Failure("Select a different active destination household.");
            }

            var destinationAddress = await _context.HouseholdAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.HouseholdId == destination.HouseholdId
                    && a.Status == "ACTIVE", ct);

            if (destinationAddress is null)
            {
                return Result<int>.Failure("The destination household has no active address.");
            }
        }
        else
        {
            var destinationAddress = await _context.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AddressId == dto.DestinationAddressId, ct);

            if (destinationAddress is null || destinationAddress.Status != "ACTIVE")
            {
                return Result<int>.Failure("Select an active destination address.");
            }
        }

        if (type == "NEW_HOUSEHOLD"
            && (dto.NewHeadCitizenId is not int newHeadId || !citizenIds.Contains(newHeadId)))
        {
            return Result<int>.Failure(
                "The new household head must be one of the moving citizens.");
        }

        var now = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var request = new TransferRequest
            {
                RequestType = type,
                SourceHouseholdId = dto.SourceHouseholdId,
                DestinationHouseholdId = type == "JOIN_EXISTING"
                    ? dto.DestinationHouseholdId
                    : null,
                DestinationAddressId = type is "NEW_HOUSEHOLD" or "HOUSEHOLD_MOVE"
                    ? dto.DestinationAddressId
                    : null,
                EffectiveDate = dto.EffectiveDate,
                NewHeadCitizenId = type == "NEW_HOUSEHOLD" ? dto.NewHeadCitizenId : null,
                SuccessorHeadCitizenId = headLeaves && type != "HOUSEHOLD_MOVE"
                    ? successorId
                    : null,
                Status = "PENDING",
                RequestedBy = currentUserId,
                RequestedAt = now
            };

            _context.TransferRequests.Add(request);
            await _context.SaveChangesAsync(ct);

            foreach (var membership in selectedMemberships)
            {
                var residence = selectedResidences.First(r => r.CitizenId == membership.CitizenId);

                _context.TransferRequestItems.Add(new TransferRequestItem
                {
                    RequestId = request.RequestId,
                    CitizenId = membership.CitizenId,
                    SourceMembershipId = membership.MembershipId,
                    SourceResidenceId = residence.ResidenceId,
                    Status = "PENDING"
                });
            }

            await _context.SaveChangesAsync(ct);

            await WriteAuditAsync(
                CreateAction,
                request.RequestId,
                currentUserId,
                correlationId,
                new
                {
                    requestId = request.RequestId,
                    requestType = type,
                    sourceHouseholdId = dto.SourceHouseholdId,
                    destinationHouseholdId = request.DestinationHouseholdId,
                    destinationAddressId = request.DestinationAddressId,
                    effectiveDate = dto.EffectiveDate,
                    citizenIds
                },
                ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(request.RequestId);
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, CreateAction, null, correlationId, ex, ct);
            return Result<int>.Failure($"The transfer request could not be created: {ex.Message}");
        }
    }

    public async Task<Result> DecideAsync(
        TransferDecisionDto dto,
        int currentUserId,
        CancellationToken ct = default)
    {
        if (!await IsActiveActorAsync(currentUserId, ct))
        {
            return Result.Failure("The authenticated user was not found or is not active.");
        }

        var validation = await _decisionValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));
        }

        if (dto.Items.GroupBy(i => i.ItemId).Any(g => g.Count() > 1))
        {
            return Result.Failure("Each item may be decided only once.");
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var effective = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var request = await _context.TransferRequests
                .FirstOrDefaultAsync(r => r.RequestId == dto.RequestId, ct);

            if (request is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("Transfer request not found.");
            }

            if (request.Status != "PENDING")
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("This transfer request has already been decided.");
            }

            if (request.RequestedBy == currentUserId)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("A different officer must decide this request.");
            }

            effective = request.EffectiveDate;

            var items = await _context.TransferRequestItems
                .Where(i => i.RequestId == request.RequestId)
                .ToListAsync(ct);

            if (items.Count == 0)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("The request has no items.");
            }

            var decisions = dto.Items.ToDictionary(i => i.ItemId);

            if (items.Any(i => !decisions.ContainsKey(i.ItemId)))
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("Every item must receive an approve or reject decision.");
            }

            if (decisions.Keys.Any(id => items.All(i => i.ItemId != id)))
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("A decision refers to an item outside this request.");
            }

            if (dto.Items.Any(i => !i.Approve && string.IsNullOrWhiteSpace(i.RejectReason)))
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("A rejection reason is required.");
            }

            var approvedItems = items.Where(i => decisions[i.ItemId].Approve).ToList();
            var rejectedItems = items.Where(i => !decisions[i.ItemId].Approve).ToList();

            if (approvedItems.Count == 0)
            {
                foreach (var item in items)
                {
                    item.Status = "REJECTED";
                    item.RejectReason = decisions[item.ItemId].RejectReason;
                }

                request.Status = "REJECTED";
                request.DecidedBy = currentUserId;
                request.DecidedAt = now;
                request.DecisionNote = dto.DecisionNote;

                await _context.SaveChangesAsync(ct);
                await WriteAuditAsync(
                    ExecuteAction,
                    request.RequestId,
                    currentUserId,
                    correlationId,
                    new { requestId = request.RequestId, approved = 0, rejected = rejectedItems.Count },
                    ct);

                await transaction.CommitAsync(ct);
                return Result.Success();
            }

            if (request.RequestType == "HOUSEHOLD_MOVE" && rejectedItems.Count > 0)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "A household move must be approved for every member. Reject the whole request instead.");
            }

            var approvedCitizenIds = approvedItems.Select(i => i.CitizenId).ToList();
            var isHouseholdMove = request.RequestType == "HOUSEHOLD_MOVE";

            var sourceMemberships = await _context.HouseholdMemberships
                .Where(m => approvedCitizenIds.Contains(m.CitizenId) && m.Status == "ACTIVE")
                .ToListAsync(ct);

            var sourceResidences = await _context.Residences
                .Where(r => approvedCitizenIds.Contains(r.CitizenId) && r.Status == "ACTIVE")
                .ToListAsync(ct);

            foreach (var item in approvedItems)
            {
                var membership = sourceMemberships.FirstOrDefault(
                    m => m.MembershipId == item.SourceMembershipId && m.CitizenId == item.CitizenId);
                var residence = sourceResidences.FirstOrDefault(
                    r => r.ResidenceId == item.SourceResidenceId && r.CitizenId == item.CitizenId);

                if (membership is null
                    || residence is null
                    || membership.HouseholdId != request.SourceHouseholdId)
                {
                    await TryRollbackAsync(transaction, ct);
                    return Result.Failure(
                        "A moving citizen no longer has an active membership/residence in the source household. Reload the request.");
                }

                if (effective <= membership.StartDate || effective <= residence.StartDate)
                {
                    await TryRollbackAsync(transaction, ct);
                    return Result.Failure(
                        "The effective date is not later than a moving citizen's tenure start.");
                }
            }

            var sourceHead = await _context.HouseholdMemberships
                .FirstOrDefaultAsync(m => m.HouseholdId == request.SourceHouseholdId
                    && m.Status == "ACTIVE"
                    && m.Role == "HEAD", ct);

            if (sourceHead is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("The source household has no active head.");
            }

            var remainingMembers = await _context.HouseholdMemberships
                .Where(m => m.HouseholdId == request.SourceHouseholdId
                    && m.Status == "ACTIVE"
                    && !approvedCitizenIds.Contains(m.CitizenId))
                .ToListAsync(ct);

            var headLeaves = approvedCitizenIds.Contains(sourceHead.CitizenId);
            HouseholdMembership? successor = null;

            if (!isHouseholdMove && headLeaves && remainingMembers.Count > 0)
            {
                if (request.SuccessorHeadCitizenId is not int successorId)
                {
                    await TryRollbackAsync(transaction, ct);
                    return Result.Failure("The source head is leaving; a successor head is required.");
                }

                successor = remainingMembers
                    .FirstOrDefault(m => m.CitizenId == successorId && m.Role == "MEMBER");

                if (successor is null)
                {
                    await TryRollbackAsync(transaction, ct);
                    return Result.Failure(
                        "The successor must be an active MEMBER remaining in the source household.");
                }
            }

            if (request.RequestType == "NEW_HOUSEHOLD"
                && (request.NewHeadCitizenId is not int newHead || !approvedCitizenIds.Contains(newHead)))
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure(
                    "The new household head was rejected; the request cannot be approved partially.");
            }

            // Close approved tenures first; the filtered unique indexes must see no active
            // rows before the destination rows are inserted. A HOUSEHOLD_MOVE keeps the
            // memberships: only the household address and the residences change.
            if (!isHouseholdMove)
            {
                foreach (var membership in sourceMemberships)
                {
                    membership.EndDate = effective.AddDays(-1);
                    membership.Status = "ENDED";
                    membership.EndedBy = currentUserId;
                    membership.EndedAt = now;
                }
            }

            foreach (var residence in sourceResidences)
            {
                residence.EndDate = effective.AddDays(-1);
                residence.Status = "ENDED";
                residence.EndedBy = currentUserId;
                residence.EndedAt = now;
            }

            if (successor is not null)
            {
                successor.EndDate = effective.AddDays(-1);
                successor.Status = "ENDED";
                successor.EndedBy = currentUserId;
                successor.EndedAt = now;
            }

            await _context.SaveChangesAsync(ct);

            if (successor is not null)
            {
                _context.HouseholdMemberships.Add(new HouseholdMembership
                {
                    CitizenId = successor.CitizenId,
                    HouseholdId = request.SourceHouseholdId,
                    Role = "HEAD",
                    StartDate = effective,
                    Status = "ACTIVE",
                    CreatedBy = currentUserId,
                    CreatedAt = now
                });

                await _context.SaveChangesAsync(ct);
            }

            var sourceBecomesEmpty = !isHouseholdMove
                && headLeaves
                && remainingMembers.Count == 0;

            if (sourceBecomesEmpty)
            {
                var household = await _context.Households
                    .FirstOrDefaultAsync(h => h.HouseholdId == request.SourceHouseholdId, ct);

                if (household is not null)
                {
                    household.Status = "INACTIVE";
                    household.InactivatedAt = now;
                }

                var sourceAddress = await _context.HouseholdAddresses
                    .FirstOrDefaultAsync(a => a.HouseholdId == request.SourceHouseholdId
                        && a.Status == "ACTIVE", ct);

                if (sourceAddress is not null)
                {
                    sourceAddress.EndDate = effective.AddDays(-1);
                    sourceAddress.Status = "ENDED";
                    sourceAddress.EndedBy = currentUserId;
                    sourceAddress.EndedAt = now;
                }

                await _context.SaveChangesAsync(ct);
            }

            int resultHouseholdId;

            if (request.RequestType == "JOIN_EXISTING")
            {
                var destinationHouseholdId = request.DestinationHouseholdId
                    ?? throw new InvalidOperationException("JOIN_EXISTING has no destination household.");

                var destinationAddress = await _context.HouseholdAddresses
                    .FirstOrDefaultAsync(a => a.HouseholdId == destinationHouseholdId
                        && a.Status == "ACTIVE", ct);

                if (destinationAddress is null)
                {
                    await TryRollbackAsync(transaction, ct);
                    return Result.Failure("The destination household has no active address.");
                }

                foreach (var item in approvedItems)
                {
                    _context.HouseholdMemberships.Add(new HouseholdMembership
                    {
                        CitizenId = item.CitizenId,
                        HouseholdId = destinationHouseholdId,
                        Role = "MEMBER",
                        StartDate = effective,
                        Status = "ACTIVE",
                        CreatedBy = currentUserId,
                        CreatedAt = now
                    });

                    _context.Residences.Add(new Residence
                    {
                        CitizenId = item.CitizenId,
                        AddressId = destinationAddress.AddressId,
                        StartDate = effective,
                        Status = "ACTIVE",
                        CreatedBy = currentUserId,
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync(ct);
                resultHouseholdId = destinationHouseholdId;
            }
            else if (request.RequestType == "NEW_HOUSEHOLD")
            {
                var destinationAddressId = request.DestinationAddressId
                    ?? throw new InvalidOperationException("NEW_HOUSEHOLD has no destination address.");

                var newHeadCitizenId = request.NewHeadCitizenId
                    ?? throw new InvalidOperationException("NEW_HOUSEHOLD has no new head.");

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
                    AddressId = destinationAddressId,
                    StartDate = effective,
                    Status = "ACTIVE",
                    CreatedBy = currentUserId,
                    CreatedAt = now
                });

                foreach (var item in approvedItems)
                {
                    _context.HouseholdMemberships.Add(new HouseholdMembership
                    {
                        CitizenId = item.CitizenId,
                        HouseholdId = household.HouseholdId,
                        Role = item.CitizenId == newHeadCitizenId ? "HEAD" : "MEMBER",
                        StartDate = effective,
                        Status = "ACTIVE",
                        CreatedBy = currentUserId,
                        CreatedAt = now
                    });

                    _context.Residences.Add(new Residence
                    {
                        CitizenId = item.CitizenId,
                        AddressId = destinationAddressId,
                        StartDate = effective,
                        Status = "ACTIVE",
                        CreatedBy = currentUserId,
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync(ct);
                resultHouseholdId = household.HouseholdId;
            }
            else
            {
                var destinationAddressId = request.DestinationAddressId
                    ?? throw new InvalidOperationException("HOUSEHOLD_MOVE has no destination address.");

                var sourceAddress = await _context.HouseholdAddresses
                    .FirstOrDefaultAsync(a => a.HouseholdId == request.SourceHouseholdId
                        && a.Status == "ACTIVE", ct);

                if (sourceAddress is null)
                {
                    await TryRollbackAsync(transaction, ct);
                    return Result.Failure("The source household has no active address to move.");
                }

                sourceAddress.EndDate = effective.AddDays(-1);
                sourceAddress.Status = "ENDED";
                sourceAddress.EndedBy = currentUserId;
                sourceAddress.EndedAt = now;

                await _context.SaveChangesAsync(ct);

                _context.HouseholdAddresses.Add(new HouseholdAddress
                {
                    HouseholdId = request.SourceHouseholdId,
                    AddressId = destinationAddressId,
                    StartDate = effective,
                    Status = "ACTIVE",
                    CreatedBy = currentUserId,
                    CreatedAt = now
                });

                foreach (var item in approvedItems)
                {
                    _context.Residences.Add(new Residence
                    {
                        CitizenId = item.CitizenId,
                        AddressId = destinationAddressId,
                        StartDate = effective,
                        Status = "ACTIVE",
                        CreatedBy = currentUserId,
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync(ct);
                resultHouseholdId = request.SourceHouseholdId;
            }

            foreach (var item in items)
            {
                var decision = decisions[item.ItemId];

                item.Status = decision.Approve ? "APPROVED" : "REJECTED";
                item.RejectReason = decision.Approve ? null : decision.RejectReason;
            }

            request.Status = "APPROVED";
            request.DecidedBy = currentUserId;
            request.DecidedAt = now;
            request.DecisionNote = dto.DecisionNote;
            request.ResultHouseholdId = resultHouseholdId;

            await _context.SaveChangesAsync(ct);

            await WriteAuditAsync(
                ExecuteAction,
                request.RequestId,
                currentUserId,
                correlationId,
                new
                {
                    requestId = request.RequestId,
                    requestType = request.RequestType,
                    approvedItemIds = approvedItems.Select(i => i.ItemId),
                    rejectedItemIds = rejectedItems.Select(i => i.ItemId),
                    resultHouseholdId,
                    effectiveDate = effective
                },
                ct);

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await TryRollbackAsync(transaction, ct);
            return Result.Failure(
                "The request or a tenure changed while it was being executed. Reload and try again.");
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, ExecuteAction, dto.RequestId, correlationId, ex, ct);
            return Result.Failure($"The transfer could not be executed: {ex.Message}");
        }
    }

    public async Task<Result> CancelAsync(
        int requestId,
        int currentUserId,
        string? note = null,
        CancellationToken ct = default)
    {
        if (!await IsActiveActorAsync(currentUserId, ct))
        {
            return Result.Failure("The authenticated user was not found or is not active.");
        }

        var correlationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var request = await _context.TransferRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId, ct);

            if (request is null)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("Transfer request not found.");
            }

            if (request.Status != "PENDING")
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("Only a pending request can be cancelled.");
            }

            if (request.RequestedBy != currentUserId)
            {
                await TryRollbackAsync(transaction, ct);
                return Result.Failure("Only the maker can cancel this request.");
            }

            var items = await _context.TransferRequestItems
                .Where(i => i.RequestId == requestId)
                .ToListAsync(ct);

            foreach (var item in items)
            {
                item.Status = "CANCELLED";
                item.RejectReason = null;
            }

            request.Status = "CANCELLED";
            request.DecidedBy = currentUserId;
            request.DecidedAt = now;
            request.DecisionNote = note;

            await _context.SaveChangesAsync(ct);

            await WriteAuditAsync(
                CancelAction,
                request.RequestId,
                currentUserId,
                correlationId,
                new { requestId = request.RequestId },
                ct);

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, ct);
            await LogFailureAsync(currentUserId, CancelAction, requestId, correlationId, ex, ct);
            return Result.Failure($"The transfer request could not be cancelled: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<TransferRequestSummaryDto>>> GetRequestsAsync(
        TransferSearchCriteria criteria,
        CancellationToken ct = default)
    {
        var query = _context.TransferRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            var status = criteria.Status.Trim().ToUpperInvariant();
            if (status is not ("PENDING" or "APPROVED" or "REJECTED" or "CANCELLED"))
            {
                return Result<IEnumerable<TransferRequestSummaryDto>>.Failure(
                    "Status must be PENDING, APPROVED, REJECTED, or CANCELLED.");
            }

            query = query.Where(r => r.Status == status);
        }

        if (criteria.RequestedBy is int requestedBy)
        {
            query = query.Where(r => r.RequestedBy == requestedBy);
        }

        var requests = await query
            .OrderByDescending(r => r.RequestedAt)
            .ThenByDescending(r => r.RequestId)
            .Take(MaxSearchResults)
            .Select(r => new TransferRequestSummaryDto
            {
                RequestId = r.RequestId,
                RequestType = r.RequestType,
                Status = r.Status,
                SourceHouseholdId = r.SourceHouseholdId,
                SourceAddressText = (
                    from ha in _context.HouseholdAddresses
                    join a in _context.Addresses on ha.AddressId equals a.AddressId
                    where ha.HouseholdId == r.SourceHouseholdId && ha.Status == "ACTIVE"
                    select a.AddressText).FirstOrDefault(),
                DestinationHouseholdId = r.DestinationHouseholdId,
                DestinationAddressId = r.DestinationAddressId,
                DestinationAddressText = (
                    from a in _context.Addresses
                    where a.AddressId == r.DestinationAddressId
                    select a.AddressText).FirstOrDefault(),
                EffectiveDate = r.EffectiveDate,
                RequestedBy = r.RequestedBy,
                RequestedByName = (
                    from u in _context.UserAccounts
                    where u.UserId == r.RequestedBy
                    select u.FullName).FirstOrDefault(),
                RequestedAt = r.RequestedAt,
                ItemCount = _context.TransferRequestItems.Count(i => i.RequestId == r.RequestId),
                ApprovedItemCount = _context.TransferRequestItems.Count(
                    i => i.RequestId == r.RequestId && i.Status == "APPROVED"),
                RejectedItemCount = _context.TransferRequestItems.Count(
                    i => i.RequestId == r.RequestId && i.Status == "REJECTED")
            })
            .ToListAsync(ct);

        return Result<IEnumerable<TransferRequestSummaryDto>>.Success(requests);
    }

    public async Task<Result<TransferRequestDetailDto?>> GetByIdAsync(
        int requestId,
        CancellationToken ct = default)
    {
        var request = await _context.TransferRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RequestId == requestId, ct);

        if (request is null)
        {
            return Result<TransferRequestDetailDto?>.Success(null);
        }

        var sourceAddressText = await (
            from ha in _context.HouseholdAddresses.AsNoTracking()
            join a in _context.Addresses.AsNoTracking() on ha.AddressId equals a.AddressId
            where ha.HouseholdId == request.SourceHouseholdId && ha.Status == "ACTIVE"
            select a.AddressText).FirstOrDefaultAsync(ct);

        string? destinationAddressText = null;

        if (request.DestinationHouseholdId is int destinationHouseholdId)
        {
            destinationAddressText = await (
                from ha in _context.HouseholdAddresses.AsNoTracking()
                join a in _context.Addresses.AsNoTracking() on ha.AddressId equals a.AddressId
                where ha.HouseholdId == destinationHouseholdId && ha.Status == "ACTIVE"
                select a.AddressText).FirstOrDefaultAsync(ct);
        }
        else if (request.DestinationAddressId is int destinationAddressId)
        {
            destinationAddressText = await _context.Addresses
                .AsNoTracking()
                .Where(a => a.AddressId == destinationAddressId)
                .Select(a => a.AddressText)
                .FirstOrDefaultAsync(ct);
        }

        var requestedByName = await _context.UserAccounts
            .AsNoTracking()
            .Where(u => u.UserId == request.RequestedBy)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct);

        string? decidedByName = null;
        if (request.DecidedBy is int decidedBy)
        {
            decidedByName = await _context.UserAccounts
                .AsNoTracking()
                .Where(u => u.UserId == decidedBy)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }

        var items = await (
            from i in _context.TransferRequestItems.AsNoTracking()
            join c in _context.Citizens.AsNoTracking() on i.CitizenId equals c.CitizenId
            where i.RequestId == requestId
            orderby i.ItemId
            select new TransferItemDto
            {
                ItemId = i.ItemId,
                CitizenId = i.CitizenId,
                NationalId = c.NationalId,
                FullName = c.FullName,
                SourceMembershipId = i.SourceMembershipId,
                SourceResidenceId = i.SourceResidenceId,
                Status = i.Status,
                RejectReason = i.RejectReason
            }).ToListAsync(ct);

        return Result<TransferRequestDetailDto?>.Success(new TransferRequestDetailDto
        {
            RequestId = request.RequestId,
            RequestType = request.RequestType,
            SourceHouseholdId = request.SourceHouseholdId,
            SourceAddressText = sourceAddressText,
            DestinationHouseholdId = request.DestinationHouseholdId,
            DestinationAddressId = request.DestinationAddressId,
            DestinationAddressText = destinationAddressText,
            EffectiveDate = request.EffectiveDate,
            NewHeadCitizenId = request.NewHeadCitizenId,
            SuccessorHeadCitizenId = request.SuccessorHeadCitizenId,
            ResultHouseholdId = request.ResultHouseholdId,
            Status = request.Status,
            RequestedBy = request.RequestedBy,
            RequestedByName = requestedByName,
            RequestedAt = request.RequestedAt,
            DecidedBy = request.DecidedBy,
            DecidedByName = decidedByName,
            DecidedAt = request.DecidedAt,
            DecisionNote = request.DecisionNote,
            Items = items
        });
    }

    private async Task<bool> IsActiveActorAsync(int userId, CancellationToken ct)
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId && u.Status == "ACTIVE", ct);
    }

    private async Task WriteAuditAsync(
        string action,
        int requestId,
        int actorUserId,
        Guid correlationId,
        object details,
        CancellationToken ct)
    {
        await _audit.LogAsync(new AuditEntry
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = "TransferRequest",
            EntityId = requestId,
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
            EntityType = "TransferRequest",
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
