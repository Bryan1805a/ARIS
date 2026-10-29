using Aris.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Persistence;

public class DatabaseConnectionService : IDatabaseConnectionService
{
    private readonly ArisDbContext _dbContext;

    public DatabaseConnectionService(ArisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync();
        }
        catch (Exception)
        {
            return false;
        }
    }
}