using System.Text.Json;

using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Interfaces;
using Aris.Application.SystemConfiguration.Dtos;

namespace Aris.Application.SystemConfiguration;

/// <summary>
/// Key/value operational parameters (<c>SYS-01</c>, <c>D11</c>).
/// </summary>
/// <remarks>
/// Reads and updates are admin-only. Each update runs inside <see cref="IUnitOfWork"/>
/// together with its success audit row (<c>CON-ARCH-07</c>); expected failures return
/// <c>Result</c> instead of throwing.
/// </remarks>
public class SystemConfigurationService {
    private const string UpdateAction = "UPDATE_SYSTEM_CONFIGURATION";

    private readonly ISystemConfigurationRepository _repository;
    private readonly IUserSession _userSession;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public SystemConfigurationService(
        ISystemConfigurationRepository repository,
        IUserSession userSession,
        IAuditService auditService,
        IUnitOfWork unitOfWork) {
        _repository = repository;
        _userSession = userSession;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<SystemConfigurationDto>>> GetAllAsync(
        CancellationToken ct = default) {
        // Keep configuration values out of the application unless the current
        // session belongs to an authenticated administrator.
        if (!TryGetAdminUserId(out _)) {
            return Result<List<SystemConfigurationDto>>.Failure(
                "Only administrators can manage system configuration.");
        }

        var configurations = await _repository.GetAllAsync();
        // Project entities to DTOs so callers receive only the configuration
        // data needed by the management screen.
        var dtos = configurations.Select(c => new SystemConfigurationDto {
            ConfigKey = c.ConfigKey,
            ConfigValue = c.ConfigValue,
            Description = c.Description,
            UpdatedAt = c.UpdatedAt,
            UpdatedBy = c.UpdatedBy
        }).ToList();

        return Result<List<SystemConfigurationDto>>.Success(dtos);
    }

    public async Task<Result> UpdateAsync(
        string configKey,
        string configValue,
        CancellationToken ct = default) {
        // Capture the actor from the session once so the update and its audit
        // entry use the same authenticated administrator.
        if (!TryGetAdminUserId(out int currentUserId)) {
            return Result.Failure(
                "Only administrators can manage system configuration.");
        }

        configKey = (configKey ?? string.Empty).Trim();
        configValue = (configValue ?? string.Empty).Trim();

        // Normalize values before validation and repository access so harmless
        // surrounding whitespace cannot produce inconsistent keys or values.
        if (string.IsNullOrWhiteSpace(configKey)) {
            return Result.Failure("Configuration key is required.");
        }

        if (!int.TryParse(configValue, out int value) || value <= 0) {
            return Result.Failure(
                "Configuration value must be a positive integer.");
        }

        var before = await _repository.GetValueAsync(configKey);
        if (before is null) {
            return Result.Failure(
                $"Configuration key '{configKey}' was not found.");
        }

        var correlationId = Guid.NewGuid();
        var updated = false;

        try {
            await _unitOfWork.ExecuteAsync(async token => {
                // The repository update and success audit share one unit of
                // work, preventing an untraceable configuration change.
                updated = await _repository.UpdateAsync(
                    configKey,
                    configValue,
                    currentUserId);

                if (!updated) {
                    // A concurrent deletion or missing key is reported after
                    // the unit of work without creating a false success audit.
                    return;
                }

                await _auditService.LogAsync(new AuditEntry {
                    ActorUserId = currentUserId,
                    Action = UpdateAction,
                    EntityType = "SystemConfiguration",
                    DetailsJson = JsonSerializer.Serialize(new {
                        configKey,
                        before,
                        after = configValue,
                        updatedBy = currentUserId
                    }),
                    CorrelationId = correlationId
                }, token);
            }, ct);

            if (!updated) {
                return Result.Failure(
                    $"Configuration key '{configKey}' was not found.");
            }

            return Result.Success();
        } catch (Exception ex) {
            // Failure auditing is independent of the update transaction so
            // operational errors remain visible even when the update rolls back.
            await _auditService.LogFailureIndependentAsync(new AuditFailureEntry {
                ActorUserId = currentUserId,
                Action = UpdateAction,
                EntityType = "SystemConfiguration",
                DetailsJson = JsonSerializer.Serialize(new {
                    configKey,
                    reason = ex.Message,
                    exception = ex.GetType().Name
                }),
                CorrelationId = correlationId
            }, ct);

            return Result.Failure(
                $"The configuration could not be updated: {ex.Message}");
        }
    }

    private bool TryGetAdminUserId(out int userId) {
        // Authorization depends on the live session, not only on a supplied
        // user ID, so unauthenticated or non-admin callers cannot self-assert
        // administrative access.
        if (_userSession.IsAuthenticated
            && _userSession.UserId is int id
            && string.Equals(
                _userSession.Role,
                "ADMIN",
                StringComparison.OrdinalIgnoreCase)) {
            userId = id;
            return true;
        }

        userId = 0;
        return false;
    }
}