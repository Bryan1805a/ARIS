using Aris.Application.Interfaces;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Repositories;

public class SystemConfigurationRepository
    : ISystemConfigurationRepository
{
    private readonly ArisDbContext _context;

    public SystemConfigurationRepository(ArisDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetValueAsync(string configKey)
    {
        return await _context.SystemConfigurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == configKey)
            .Select(c => c.ConfigValue)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Aris.Domain.Entities.SystemConfiguration>> GetAllAsync()
    {
        return await _context.SystemConfigurations
            .AsNoTracking()
            .OrderBy(c => c.ConfigKey)
            .ToListAsync();
    }

    public async Task<bool> UpdateAsync(
        string configKey,
        string configValue,
        int updatedBy)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.ConfigKey == configKey);

        if (config == null)
            return false;

        config.ConfigValue = configValue;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();

        return true;
    }
}