
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Aris.Application.Interfaces;
using Aris.Infrastructure.Repositories;

namespace Aris.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO(Database Designer): Register ArisDbContext here.

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();

        return services;
    }
}
