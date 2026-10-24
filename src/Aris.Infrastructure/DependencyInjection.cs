using Aris.Application.Auditing;
using Aris.Infrastructure.Auditing;
using Aris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Aris.Application.Interfaces;
using Aris.Infrastructure.Repositories;

namespace Aris.Infrastructure;

/// <summary>
/// Composition entry point for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ArisDb");

        // Resolve the connection string lazily:
        // the UI can start without a database connection.
        services.AddDbContext<ArisDbContext>((_, options) =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:ArisDb is not configured. " +
                    "Set it with 'dotnet user-secrets set \"ConnectionStrings:ArisDb\" \"...\"' " +
                    "from src/Aris.UI (see docs/07-deployment-view.md).");
            }

            options.UseSqlServer(
                connectionString,
                sql => sql.EnableRetryOnFailure());
        });

        // Register repository for Authentication.
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        // Scoped: AuditService shares the caller's ArisDbContext so LogAsync joins the
        // business transaction (D7). Coder 1-3 code against IAuditService only.
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
