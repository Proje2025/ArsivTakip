using System.Windows;
using BirtanaArsivTakip.Helpers;

namespace BirtanaArsivTakip.Views;

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
    }

    private async void Guncelle_Click(object sender, RoutedEventArgs e)
    {
        GuncelleButton.IsEnabled = false;
        SonraButton.IsEnabled = false;
        IndirmeBar.Visibility = Visibility.Visible;
        IndirmeBar.IsIndeterminate = false;

        var success = await _updateChecker.DownloadAndInstallUpdateAsync(new Progress<int>(p =>
        {
            IndirmeBar.Value = p;
        }));

        if (success)
        {
            Application.Current.Shutdown();
        }
        else
        {
            MessageBox.Show("Güncelleme indirilirken hata oluştu. Lütfen manuel olarak indirin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            GuncelleButton.IsEnabled = true;
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