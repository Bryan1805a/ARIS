using Microsoft.Extensions.DependencyInjection;

using Aris.Application.Authentication;
using Aris.Application.Accounts;

namespace Aris.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IAccountService, AccountService>();

        return services;
    }
}