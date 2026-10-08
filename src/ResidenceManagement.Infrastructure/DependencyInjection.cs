using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ResidenceManagement.Infrastructure;

/// <summary>
/// Composition entry point for the Infrastructure layer.
/// The Database Designer registers the EF Core <c>ResidenceDbContext</c> and
/// other infrastructure concerns here.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO(Database Designer): register the DbContext with the SQL Server provider, e.g.
        // services.AddDbContext<ResidenceDbContext>(options =>
        //     options.UseSqlServer(configuration.GetConnectionString("ResidenceDb")));
        return services;
    }
}
