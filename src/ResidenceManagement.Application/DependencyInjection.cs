using Microsoft.Extensions.DependencyInjection;

namespace ResidenceManagement.Application;

/// <summary>
/// Composition entry point for the Application layer.
/// Feature owners register their services and validators here.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // TODO(Module owners): register use cases, e.g.
        // services.AddScoped<ICitizenService, CitizenService>();
        // services.AddScoped<IHouseholdService, HouseholdService>();
        // services.AddScoped<ITransferService, TransferService>();
        return services;
    }
}
