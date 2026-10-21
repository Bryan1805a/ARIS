using Aris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aris.Infrastructure;

/// <summary>
/// Composition entry point for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ArisDb");

        // Resolve the connection string lazily: the shell can start without a database
        // (useful while the UI is being built), but the first feature that needs data
        // fails with an actionable message instead of an opaque provider error.
        // The real value belongs in user-secrets, outside the repository
        // (CON-SEC-02; docs/07-deployment-view.md, step 4).
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

        return services;
    }
}
