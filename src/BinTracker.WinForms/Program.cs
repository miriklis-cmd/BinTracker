using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BinTracker.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();

        using var splash = new SplashForm();
        splash.Show();
        splash.Refresh();
        Application.DoEvents();

        try
        {
            DeveloperDatabaseStartup.ApplyPendingOperation();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Developer database switch failed.\n\n{ex.Message}",
                "BinTracker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBinTrackerData();
                services.AddBinTrackerServices();
                services.AddTransient<MainForm>();
            })
            .Build();

        try
        {
            // Keep startup on this STA thread. Using async Main here can resume on a
            // thread-pool (MTA) thread before the WinForms message loop starts,
            // which breaks OLE-backed dialogs such as SaveFileDialog.
            DatabaseSetup.InitializeAsync(host.Services).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Database setup failed.\n\n{ex.Message}",
                "BinTracker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        splash.Close();

        using var scope = host.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        if (!auth.HasUsersAsync().GetAwaiter().GetResult())
        {
            using var setup = new FirstRunAdminForm(auth);
            if (setup.ShowDialog() != DialogResult.OK)
                return;
        }

        while (true)
        {
            using (var login = new LoginForm(auth))
            {
                if (login.ShowDialog() != DialogResult.OK)
                    break;
            }

            var session = scope.ServiceProvider.GetRequiredService<UserSession>();

            if (session.MustChangePassword)
            {
                using var change = new ChangePasswordForm(auth, required: true);
                if (change.ShowDialog() != DialogResult.OK)
                {
                    auth.LogoutAsync().GetAwaiter().GetResult();
                    break;
                }
            }

            using var main = scope.ServiceProvider.GetRequiredService<MainForm>();
            Application.Run(main);

            if (main.RestartRequested)
            {
                Application.Restart();
                return;
            }

            // A normal window close exits BinTracker. An explicit Logout clears
            // the authenticated session and loops back to the login dialog.
            if (!main.LogoutRequested)
                break;

            auth.LogoutAsync().GetAwaiter().GetResult();
        }

        auth.LogoutAsync().GetAwaiter().GetResult();
    }
}
