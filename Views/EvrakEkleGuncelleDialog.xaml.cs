using System.IO;
using System.Windows;
using Microsoft.Win32;
using ArsivTakip.Data;
using ArsivTakip.Models;

namespace ArsivTakip.Views;

public partial class EvrakEkleGuncelleDialog : Window
{
    private readonly ArsivDbContext _context;
    private readonly Evrak? _mevcutEvrak;
    private readonly int _klasorId;
    private readonly string _pdfFolderPath;
    private string? _secilenPdfDosyasi;

    public EvrakEkleGuncelleDialog(ArsivDbContext context, Evrak? evrak, int klasorId, string pdfFolderPath)
    {
        InitializeComponent();
        _context = context;
        _mevcutEvrak = evrak;
        _klasorId = klasorId;
        _pdfFolderPath = pdfFolderPath;

        if (evrak != null)
        {
            BaslikText.Text = "Evrak Güncelle";
            Title = "Evrak Güncelle";
            KonuTextBox.Text = evrak.Konu;
            SayiTextBox.Text = evrak.Sayi;
            TarihDatePicker.SelectedDate = evrak.Tarih;
            AciklamaTextBox.Text = evrak.Aciklama;
            _secilenPdfDosyasi = evrak.PdfDosyaAdi;
            if (!string.IsNullOrEmpty(_secilenPdfDosyasi))
            {
                PdfYoluTextBox.Text = _secilenPdfDosyasi;
                PdfDurumText.Text = "Mevcut dosya: " + _secilenPdfDosyasi;
            }
        }
        else
        {
            TarihDatePicker.SelectedDate = DateTime.Now;
        }

        var klasorler = _context.Klasorler.ToList();
        KlasorComboBox.ItemsSource = klasorler;

        if (evrak != null)
        {
            KlasorComboBox.SelectedItem = klasorler.FirstOrDefault(k => k.Id == evrak.KlasorId);
        }
        else
        {
            KlasorComboBox.SelectedItem = klasorler.FirstOrDefault(k => k.Id == _klasorId);
        }
    }

    private void PdfSec_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF Dosyaları|*.pdf",
            Title = "PDF Seç"
        };

        if (dialog.ShowDialog() == true)
        {
            var dosyaAdi = Path.GetFileName(dialog.FileName);

            // Güvenlik: Dosya uzantısı kontrolü
            if (!dosyaAdi.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Yalnızca PDF dosyaları seçilebilir.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Güvenlik: Dosya adındaki tehlikeli karakterleri temizle
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                dosyaAdi = dosyaAdi.Replace(c, '_');
            }

            _secilenPdfDosyasi = dialog.FileName;
            PdfYoluTextBox.Text = dosyaAdi;
            PdfDurumText.Text = "Seçili dosya: " + dosyaAdi;
        }
    }

    private void PdfKaldir_Click(object sender, RoutedEventArgs e)
    {
        _secilenPdfDosyasi = null;
        PdfYoluTextBox.Text = "";
        PdfDurumText.Text = "";
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(KonuTextBox.Text))
        {
            MessageBox.Show("Konu giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(SayiTextBox.Text))
        {
            MessageBox.Show("Sayı giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TarihDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Tarih seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (KlasorComboBox.SelectedItem is not Klasor secilenKlasor)
        {
            MessageBox.Show("Klasör seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string? pdfDosyaAdi = null;

        if (!string.IsNullOrEmpty(_secilenPdfDosyasi))
        {
            try
            {
                if (!Directory.Exists(_pdfFolderPath))
                {
                    Directory.CreateDirectory(_pdfFolderPath);
                }

                pdfDosyaAdi = Path.GetFileName(_secilenPdfDosyasi);
                var hedefYol = Path.Combine(_pdfFolderPath, pdfDosyaAdi);

                if (File.Exists(_secilenPdfDosyasi))
                {
                    File.Copy(_secilenPdfDosyasi, hedefYol, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF kopyalanırken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        if (_mevcutEvrak != null)
        {
            _mevcutEvrak.Konu = KonuTextBox.Text.Trim();
            _mevcutEvrak.Sayi = SayiTextBox.Text.Trim();
            _mevcutEvrak.Tarih = TarihDatePicker.SelectedDate.Value;
            _mevcutEvrak.Aciklama = string.IsNullOrWhiteSpace(AciklamaTextBox.Text) ? null : AciklamaTextBox.Text.Trim();
            _mevcutEvrak.KlasorId = secilenKlasor.Id;

            if (pdfDosyaAdi != null)
            {
                _mevcutEvrak.PdfDosyaAdi = pdfDosyaAdi;
            }

            _context.SaveChanges();
        }
        else
        {
            var yeniEvrak = new Evrak
            {
                Konu = KonuTextBox.Text.Trim(),
                Sayi = SayiTextBox.Text.Trim(),
                Tarih = TarihDatePicker.SelectedDate.Value,
                Aciklama = string.IsNullOrWhiteSpace(AciklamaTextBox.Text) ? null : AciklamaTextBox.Text.Trim(),
                KlasorId = secilenKlasor.Id,
                PdfDosyaAdi = pdfDosyaAdi,
                EklenmeTarihi = DateTime.Now
            };

            _context.Evraklar.Add(yeniEvrak);
            _context.SaveChanges();
        }

        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}