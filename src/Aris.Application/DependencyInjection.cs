using Aris.Application.Accounts;
using Aris.Application.Auditing;
using Aris.Application.Authentication;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Citizens.Validators;
using Aris.Application.Reports;
using Aris.Application.SystemConfiguration;

using FluentValidation;

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
        services.AddScoped<SystemConfigurationService>();

        services.AddScoped<IValidator<CreateCitizenDto>, CreateCitizenValidator>();
        services.AddScoped<IValidator<UpdateCitizenDto>, UpdateCitizenValidator>();

        return services;
    }
}