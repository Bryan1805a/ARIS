using Aris.Domain.Entities;

namespace Aris.Application.Interfaces;

public interface ISystemConfigurationRepository
{
    // Get a configuration value by key
    Task<string?> GetValueAsync(string configKey);

    // Get all configurations
    Task<List<Aris.Domain.Entities.SystemConfiguration>> GetAllAsync();
    // Update a configuration
    Task<bool> UpdateAsync(
        string configKey,
        string configValue,
        int updatedBy
    );
}