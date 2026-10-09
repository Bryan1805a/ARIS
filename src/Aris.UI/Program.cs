using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Aris.Application;
using Aris.Infrastructure;

namespace Aris.UI;

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
            .ConfigureAppConfiguration((context, config) =>
            {
                // Load user-secrets explicitly. CreateDefaultBuilder only does this
                // when the environment is Development, which a WinForms launch does
                // not set by default - so without this the documented secret path
                // would silently not apply. Secrets stay outside the repository
                // (CON-SEC-02); see README > Configuration & Secrets.
                config.AddUserSecrets(typeof(Program).Assembly, optional: true);
            })
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
