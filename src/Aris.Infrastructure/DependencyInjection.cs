using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aris.Infrastructure;

/// <summary>
/// Composition entry point for the Infrastructure layer.
/// The Database Designer registers the EF Core <c>ArisDbContext</c> and
/// other infrastructure concerns here.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO(Database Designer): register the DbContext with the SQL Server provider, e.g.
        // services.AddDbContext<ArisDbContext>(options =>
        //     options.UseSqlServer(configuration.GetConnectionString("ArisDb")));
        return services;
    }
}
