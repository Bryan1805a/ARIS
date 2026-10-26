using Aris.Application.Accounts;
using Aris.Application.Auditing;
using Aris.Application.Authentication;
using Aris.Application.Reports;

using Microsoft.Extensions.DependencyInjection;

namespace Aris.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();

        return services;
    }
}