using System.Windows;
using ArsivTakip.Data;
using ArsivTakip.Models;

namespace ArsivTakip.Views;

public partial class KlasorEkleDialog : Window
{
    private readonly ArsivDbContext _context;
    public Klasor? SecilenKlasor { get; private set; }

    public KlasorEkleDialog(ArsivDbContext context)
    {
        InitializeComponent();
        _context = context;

        var klasorler = _context.Klasorler.ToList();
        klasorler.Insert(0, new Klasor { Id = 0, KlasorAdi = "(Üst Klasör Yok)" });
        UstKlasorComboBox.ItemsSource = klasorler;
        UstKlasorComboBox.SelectedIndex = 0;
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(KlasorAdiTextBox.Text))
        {
            MessageBox.Show("Klasör adı giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var yeniKlasor = new Klasor
        {
            KlasorAdi = KlasorAdiTextBox.Text.Trim(),
            Tarih = string.IsNullOrWhiteSpace(TarihTextBox.Text) ? null : TarihTextBox.Text.Trim(),
            Aciklama = string.IsNullOrWhiteSpace(AciklamaTextBox.Text) ? null : AciklamaTextBox.Text.Trim()
        };

        if (UstKlasorComboBox.SelectedIndex > 0)
        {
            yeniKlasor.UstKlasorId = ((Klasor)UstKlasorComboBox.SelectedItem).Id;
        }

        _context.Klasorler.Add(yeniKlasor);
        _context.SaveChanges();

        SecilenKlasor = yeniKlasor;
        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}