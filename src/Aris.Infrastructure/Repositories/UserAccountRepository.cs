using Aris.Application.Interfaces;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly ArisDbContext _context;

    public UserAccountRepository(ArisDbContext context)
    {
        _context = context;
    }

    public async Task<UserAccount?> GetByUsernameAsync(string username)
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);
    }
    public async Task<List<UserAccount>> GetAllAsync()
    {
        return await _context.UserAccounts
            .AsNoTracking()
            .OrderBy(u => u.UserId)
            .ToListAsync();
    }

    public async Task AddAsync(UserAccount account)
    {
        await _context.UserAccounts.AddAsync(account);
        await _context.SaveChangesAsync();
    }

    public async Task<UserAccount?> GetByIdAsync(int userId)
    {
        return await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }
    public async Task UpdateAsync(UserAccount account)
    {
        await _context.SaveChangesAsync();
    }
}