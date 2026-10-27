using Aris.Application.Common;
using Aris.Application.Interfaces;
using Aris.Domain.Entities;

namespace Aris.Application.SystemConfiguration;

public class SystemConfigurationService
{
    private readonly ISystemConfigurationRepository _repository;
    private readonly IUserSession _userSession;

    public SystemConfigurationService(
        ISystemConfigurationRepository repository,
        IUserSession userSession)
    {
        _repository = repository;
        _userSession = userSession;
    }

    public async Task<List<Aris.Domain.Entities.SystemConfiguration>> GetAllAsync()
    {
        EnsureAdmin();

        return await _repository.GetAllAsync();
    }

    public async Task<bool> UpdateAsync(
        string configKey,
        string configValue)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(configKey))
            throw new ArgumentException("Configuration key is required.");

        if (!int.TryParse(configValue, out int value) || value <= 0)
        {
            throw new ArgumentException(
                "Configuration value must be a positive integer.");
        }

        return await _repository.UpdateAsync(
            configKey,
            configValue,
            _userSession.UserId!.Value
        );
    }

    private void EnsureAdmin()
    {
        if (!_userSession.IsAuthenticated ||
            !_userSession.UserId.HasValue ||
            !string.Equals(
                _userSession.Role,
                "ADMIN",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Only administrators can manage system configuration.");
        }
    }
}