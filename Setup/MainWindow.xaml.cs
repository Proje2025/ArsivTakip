using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ArsivTakip.Setup;

public partial class MainWindow : Window
{
    private readonly GitHubInstallerService _installerService = new();
    private CancellationTokenSource? _cts;
    private string? _downloadedInstallerPath;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await PrepareInstallAsync();
    }

    private async Task PrepareInstallAsync()
    {
        SetUi(busy: true, status: "Son sürüm kontrol ediliyor...", progress: 0);

        try
        {
            _cts = new CancellationTokenSource();
            var resolved = await _installerService.ResolveLatestInstallerAsync(_cts.Token);
            if (!resolved)
            {
                SetUi(busy: false, status: _installerService.LastError, progress: 0);
                InstallButton.Content = "Kapat";
                InstallButton.IsEnabled = true;
                InstallButton.Click -= Install_Click;
                InstallButton.Click += (_, _) => Close();
                return;
            }

            VersionText.Text = $"Sürüm: v{_installerService.LatestVersion}  •  {_installerService.InstallerFileName}";
            SetUi(busy: false, status: "Kur'a tıklayın; en güncel sürüm indirilip kurulacak.", progress: 0);
            InstallButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            SetUi(busy: false, status: $"Bağlantı hatası: {ex.Message}", progress: 0);
            InstallButton.Content = "Kapat";
            InstallButton.IsEnabled = true;
            InstallButton.Click -= Install_Click;
            InstallButton.Click += (_, _) => Close();
        }
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_downloadedInstallerPath) && File.Exists(_downloadedInstallerPath))
        {
            LaunchInstaller(_downloadedInstallerPath);
            return;
        }

        InstallButton.IsEnabled = false;
        CancelButton.IsEnabled = false;

        try
        {
            _cts = new CancellationTokenSource();
            SetUi(busy: true, status: "Kurulum dosyası indiriliyor...", progress: 0);

            _downloadedInstallerPath = await _installerService.DownloadInstallerAsync(
                new Progress<int>(p =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = p;
                        StatusText.Text = $"İndiriliyor... %{p}";
                    });
                }),
                _cts.Token);

            SetUi(busy: false, status: "Kurulum başlatılıyor...", progress: 100);
            LaunchInstaller(_downloadedInstallerPath);
        }
        catch (OperationCanceledException)
        {
            SetUi(busy: false, status: "İndirme iptal edildi.", progress: 0);
            InstallButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            SetUi(busy: false, status: $"Hata: {ex.Message}", progress: 0);
            InstallButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
        }
    }

    private static void LaunchInstaller(string installerPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true
        });

        Application.Current.Shutdown();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        Close();
    }

    private void SetUi(bool busy, string status, int progress)
    {
        StatusText.Text = status;
        ProgressBar.Value = progress;
        ProgressBar.IsIndeterminate = busy && progress == 0;
        CancelButton.IsEnabled = !busy;
    }
}
