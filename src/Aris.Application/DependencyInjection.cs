
using Microsoft.Extensions.DependencyInjection;

using Aris.Application.Authentication;

namespace Aris.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
