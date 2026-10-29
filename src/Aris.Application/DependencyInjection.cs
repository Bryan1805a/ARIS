using Aris.Application.Accounts;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Addresses.Validators;
using Aris.Application.Auditing;
using Aris.Application.Authentication;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Citizens.Validators;
using Aris.Application.Households.Dtos;
using Aris.Application.Households.Validators;
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

        services.AddScoped<IValidator<CreateAddressDto>, CreateAddressValidator>();
        services.AddScoped<IValidator<RegisterHouseholdDto>, RegisterHouseholdValidator>();
        services.AddScoped<IValidator<AddMemberDto>, AddMemberValidator>();
        services.AddScoped<IValidator<ChangeHeadDto>, ChangeHeadValidator>();

        return services;
    }
}