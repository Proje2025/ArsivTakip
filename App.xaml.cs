using System.Windows;
using ArsivTakip.Helpers;

namespace ArsivTakip;

public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        if (e.Args.Contains("--show-version", StringComparer.OrdinalIgnoreCase))
        {
            var checker = new UpdateChecker();
            if (e.Args.Contains("--silent", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"VERSION={checker.CurrentVersion}");
                Console.WriteLine($"PATH={Environment.ProcessPath}");
                Shutdown();
                return;
            }

            MessageBox.Show(
                $"Sürüm: v{checker.CurrentVersion}\nYol: {Environment.ProcessPath}",
                "Arşiv Takip",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        if (e.Args.Contains("--check-update", StringComparer.OrdinalIgnoreCase))
        {
            await RunCheckUpdateAsync(e.Args.Contains("--silent", StringComparer.OrdinalIgnoreCase));
            return;
        }

        if (e.Args.Contains("--apply-update", StringComparer.OrdinalIgnoreCase))
        {
            var silent = e.Args.Contains("--silent", StringComparer.OrdinalIgnoreCase);
            await RunApplyUpdateAsync(silent);
            return;
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private static async Task RunCheckUpdateAsync(bool silent)
    {
        try
        {
            var checker = new UpdateChecker();
            var hasUpdate = await checker.CheckForUpdateAsync();
            if (silent)
            {
                Console.WriteLine($"HAS_UPDATE={hasUpdate}");
                Console.WriteLine($"EFFECTIVE={checker.EffectiveVersion}");
                Console.WriteLine($"LATEST={checker.LatestVersion}");
            }
            else
            {
                MessageBox.Show(
                    hasUpdate
                        ? $"Güncelleme var: v{checker.LatestVersion}"
                        : $"Güncelsiniz: v{checker.CurrentVersion} (yayın: v{checker.LatestVersion})",
                    "Güncelleme",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            if (silent) Console.WriteLine($"ERROR={ex.Message}");
            else MessageBox.Show(ex.Message, "Güncelleme", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        Current.Shutdown();
    }

    private static async Task RunApplyUpdateAsync(bool silent)
    {
        try
        {
            var checker = new UpdateChecker();
            var hasUpdate = await checker.CheckForUpdateAsync();
            if (!hasUpdate)
            {
                Report(silent, $"NO_UPDATE current={checker.CurrentVersion} latest={checker.LatestVersion}");
                Current.Shutdown();
                return;
            }

            Report(silent, $"DOWNLOADING to {checker.LatestVersion}...");
            var success = await checker.DownloadAndInstallUpdateAsync();
            if (!success)
            {
                Report(silent, $"FAILED {checker.LastError}");
                Current.Shutdown();
                return;
            }

            Report(silent, "INSTALLER_STARTED");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Report(silent, $"ERROR {ex.Message}");
            Current.Shutdown();
        }
    }

    private static void Report(bool silent, string message)
    {
        if (silent)
        {
            Console.WriteLine(message);
            return;
        }

        MessageBox.Show(message, "Güncelleme", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
