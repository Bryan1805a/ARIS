using Aris.Application.Accounts;
using Aris.Application.Addresses.Dtos;
using Aris.Application.Addresses.Validators;
using Aris.Application.Auditing;
using Aris.Application.Authentication;
using Aris.Application.Citizens.Dtos;
using Aris.Application.Citizens.Validators;
using Aris.Application.Households.Dtos;
using Aris.Application.Households.Validators;
using Aris.Application.Rectifications.Dtos;
using Aris.Application.Rectifications.Validators;
using Aris.Application.Reports;
using Aris.Application.SystemConfiguration;
using Aris.Application.TemporaryResidences.Dtos;
using Aris.Application.TemporaryResidences.Validators;
using Aris.Application.Transfers.Dtos;
using Aris.Application.Transfers.Validators;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Aris.Application;

public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
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

        services.AddScoped<IValidator<CreateTransferRequestDto>, CreateTransferRequestValidator>();
        services.AddScoped<IValidator<TransferDecisionDto>, TransferDecisionValidator>();

        services.AddScoped<IValidator<RetireAddressDto>, RetireAddressValidator>();

        // HIST-01: void-and-correct validators. The service implementation lives in
        // Infrastructure and is registered by AddInfrastructure.
        services.AddScoped<IValidator<RectifyResidenceDto>, RectifyResidenceValidator>();
        services.AddScoped<IValidator<RectifyMembershipDto>, RectifyMembershipValidator>();
        services.AddScoped<IValidator<RectifyHouseholdAddressDto>, RectifyHouseholdAddressValidator>();

        services.AddScoped<IValidator<RegisterTemporaryResidenceDto>, RegisterTemporaryResidenceValidator>();
        services.AddScoped<IValidator<UpdateTemporaryResidenceDto>, UpdateTemporaryResidenceValidator>();

        return services;
    }
}