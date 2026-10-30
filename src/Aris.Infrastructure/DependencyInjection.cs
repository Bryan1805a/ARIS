using Aris.Application.Accounts;
using Aris.Application.Addresses;
using Aris.Application.Auditing;
using Aris.Application.Citizens;
using Aris.Application.Households;
using Aris.Application.Transfers;
using Aris.Infrastructure.Accounts;
using Aris.Infrastructure.Addresses;
using Aris.Infrastructure.Auditing;
using Aris.Infrastructure.Citizens;
using Aris.Infrastructure.Households;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Aris.Application.Interfaces;
using Aris.Infrastructure.Repositories;

namespace Aris.Infrastructure;

/// <summary>
/// Composition entry point for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ArisDb");

        // Resolve the connection string lazily:
        // the UI can start without a database connection.
        services.AddDbContext<ArisDbContext>((_, options) =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:ArisDb is not configured. " +
                    "Set it with 'dotnet user-secrets set \"ConnectionStrings:ArisDb\" \"...\"' " +
                    "from src/Aris.UI (see docs/07-deployment-view.md).");
            }

            // Do NOT enable EF's retrying execution strategy here. Every mutating
            // service opens an explicit transaction, and SqlServerRetryingExecutionStrategy
            // rejects user-initiated transactions. Transient connection faults on the
            // independent audit path are still handled by SqlResilience. If EF-level retry
            // is needed later, wrap each transaction in
            // Database.CreateExecutionStrategy().ExecuteAsync(...).
            options.UseSqlServer(connectionString);
        });
        services.AddDbContextFactory<ArisDbContext>(
    (_, options) =>
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:ArisDb is not configured.");
        }

        options.UseSqlServer(connectionString);
    },
    ServiceLifetime.Scoped);

        // Register repository for Authentication.
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<
            ISystemConfigurationRepository,
            SystemConfigurationRepository>();
        // Scoped: AuditService shares the caller's ArisDbContext.
        services.AddScoped<IAuditService, AuditService>();

        // Fresh-database setup and demo data (R15/T6/T11).
        services.AddScoped<IBootstrapService, BootstrapService>();
        services.AddScoped<IDemoDataService, DemoDataService>();

        // Citizen slice (CIT-01..CIT-04, SRCH-01).
        services.AddScoped<ICitizenService, CitizenService>();

        // Address lookup/creation and household lifecycle (ADR-01, HH-01/HH-02/HH-04/HH-06).
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IHouseholdService, HouseholdService>();

        // Maker-checker transfer engine (UC-06).
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();
        services.AddScoped<IReportRepository, ReportRepository>();

        return services;
    }
}
