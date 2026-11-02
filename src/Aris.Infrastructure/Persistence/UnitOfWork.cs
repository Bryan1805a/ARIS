using Aris.Application.Common;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Persistence;

/// <summary>
/// Explicit transaction boundary for repository-backed application services.
/// </summary>
/// <remarks>
/// Repositories and <c>AuditService</c> share the scoped <see cref="ArisDbContext"/>, so
/// every <c>SaveChanges</c> inside the action joins the same transaction. A failed
/// success-audit write therefore rolls the business mutation back with it
/// (<c>CON-ARCH-07</c>).
/// </remarks>
public sealed class UnitOfWork : IUnitOfWork {
    private readonly ArisDbContext _context;

    public UnitOfWork(ArisDbContext context) {
        _context = context;
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(action);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try {
            await action(ct);
            await transaction.CommitAsync(ct);
        } catch {
            try {
                await transaction.RollbackAsync(ct);
            } catch {
                // A trigger may already have rolled the transaction back; keep the original error.
            }

            // The scoped context is long-lived. Detach the rolled-back mutation so a
            // later SaveChanges cannot persist a half-applied change.
            _context.ChangeTracker.Clear();

            throw;
        }
    }
}