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
}