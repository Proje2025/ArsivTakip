using System.Windows;
using System.Windows.Threading;
using ArsivTakip.Helpers;

namespace ArsivTakip.Views;

public partial class GuncellemeDialog : Window
{
    private readonly UpdateChecker _updateChecker;
    private readonly Action? _onGuncelle;
    private readonly Action? _onSonra;

    public GuncellemeDialog(UpdateChecker updateChecker, Action? onGuncelle = null, Action? onSonra = null)
    {
        InitializeComponent();
        _updateChecker = updateChecker;
        _onGuncelle = onGuncelle;
        _onSonra = onSonra;

        MevcutVersiyonText.Text = updateChecker.CurrentVersion;
        YeniVersiyonText.Text = updateChecker.LatestVersion;
        DegisikliklerText.Text = string.IsNullOrWhiteSpace(updateChecker.ReleaseNotes)
            ? "Yeni özellikler ve iyileştirmeler mevcut."
            : updateChecker.ReleaseNotes;

        if (!updateChecker.HasDownloadAsset)
        {
            GuncelleButton.IsEnabled = false;
            GuncelleButton.ToolTip = "Bu sürüm için indirilebilir dosya bulunamadı.";
        }
    }

    private async void Guncelle_Click(object sender, RoutedEventArgs e)
    {
        GuncelleButton.IsEnabled = false;
        SonraButton.IsEnabled = false;
        IndirmeBar.Visibility = Visibility.Visible;
        IndirmeBar.IsIndeterminate = false;
        IndirmeBar.Value = 0;

        var success = await _updateChecker.DownloadAndInstallUpdateAsync(new Progress<int>(p =>
        {
            Dispatcher.Invoke(() => IndirmeBar.Value = p, DispatcherPriority.Background);
        }));

        if (success)
        {
            _onGuncelle?.Invoke();
            Environment.Exit(0);
        }
        else
        {
            var detail = string.IsNullOrWhiteSpace(_updateChecker.LastError)
                ? ""
                : $"\n\n{_updateChecker.LastError}";
            MessageBox.Show(
                $"Güncelleme indirilirken veya uygulanırken hata oluştu. Lütfen manuel olarak indirin.{detail}",
                "Hata",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            GuncelleButton.IsEnabled = _updateChecker.HasDownloadAsset;
            SonraButton.IsEnabled = true;
            IndirmeBar.Visibility = Visibility.Collapsed;
        }
    }

    private void Sonra_Click(object sender, RoutedEventArgs e)
    {
        _onSonra?.Invoke();
        Close();
    }
}
