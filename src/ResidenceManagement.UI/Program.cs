using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResidenceManagement.Application;
using ResidenceManagement.Infrastructure;

namespace ResidenceManagement.UI;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Composition root: UI -> Application -> Domain; Infrastructure wired here.
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);

                // UI forms and view models.
                services.AddTransient<MainForm>();
            })
            .Build();

        System.Windows.Forms.Application.Run(host.Services.GetRequiredService<MainForm>());
    }
}
