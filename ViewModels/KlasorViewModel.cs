using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using ArsivTakip.Data;
using ArsivTakip.Models;

namespace ArsivTakip.ViewModels;

public class KlasorTreeItem : ViewModelBase
{
    private bool _isExpanded = true;
    private bool _isSelected;

    public Klasor Klasor { get; }

    public string KlasorAdi => Klasor.KlasorAdi;
    public int Id => Klasor.Id;

    public ObservableCollection<KlasorTreeItem> Children { get; } = new();

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public KlasorTreeItem(Klasor klasor)
    {
        Klasor = klasor;
    }
}

public class MainViewModel : ViewModelBase
{
    private readonly ArsivDbContext _context;
    private string _pdfFolderPath;

    private ObservableCollection<KlasorTreeItem> _klasorTree = new();
    private ObservableCollection<Evrak> _evraklar = new();
    private KlasorTreeItem? _selectedTreeItem;
    private Evrak? _selectedEvrak;

    private string _aramaMetni = string.Empty;
    private string _aramaSayi = string.Empty;
    private DateTime? _aramaBaslangicTarih;
    private DateTime? _aramaBitisTarih;
    private string _statusText = "Hazır";

    public ObservableCollection<KlasorTreeItem> KlasorTree
    {
        get => _klasorTree;
        set => SetProperty(ref _klasorTree, value);
    }

    public ObservableCollection<Evrak> Evraklar
    {
        get => _evraklar;
        set => SetProperty(ref _evraklar, value);
    }

    public KlasorTreeItem? SelectedTreeItem
    {
        get => _selectedTreeItem;
        set
        {
            if (SetProperty(ref _selectedTreeItem, value))
                EvraklariYukle();
        }
    }

    public Evrak? SelectedEvrak
    {
        get => _selectedEvrak;
        set
        {
            if (SetProperty(ref _selectedEvrak, value))
            {
                (EvrakGuncelleCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (EvrakSilCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string AramaMetni
    {
        get => _aramaMetni;
        set
        {
            if (SetProperty(ref _aramaMetni, value))
                AramaYap();
        }
    }

    public string AramaSayi
    {
        get => _aramaSayi;
        set
        {
            if (SetProperty(ref _aramaSayi, value))
                AramaYap();
        }
    }

    public DateTime? AramaBaslangicTarih
    {
        get => _aramaBaslangicTarih;
        set
        {
            if (SetProperty(ref _aramaBaslangicTarih, value))
                AramaYap();
        }
    }

    public DateTime? AramaBitisTarih
    {
        get => _aramaBitisTarih;
        set
        {
            if (SetProperty(ref _aramaBitisTarih, value))
                AramaYap();
        }
    }

    public ICommand KlasorEkleCommand { get; }
    public ICommand KlasorSilCommand { get; }
    public ICommand EvrakEkleCommand { get; }
    public ICommand EvrakGuncelleCommand { get; }
    public ICommand EvrakSilCommand { get; }
    public ICommand TemizleCommand { get; }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public MainViewModel(ArsivDbContext context, string pdfFolderPath)
    {
        _context = context;
        _pdfFolderPath = pdfFolderPath;

        KlasorEkleCommand = new RelayCommand(KlasorEkle);
        KlasorSilCommand = new RelayCommand(KlasorSil, () => SelectedTreeItem != null);
        EvrakEkleCommand = new RelayCommand(EvrakEkle);
        EvrakGuncelleCommand = new RelayCommand(EvrakGuncelle, () => SelectedEvrak != null);
        EvrakSilCommand = new RelayCommand(EvrakSil, () => SelectedEvrak != null);
        TemizleCommand = new RelayCommand(Temizle);

        KlasorAgaciniYukle();
    }

    private void KlasorAgaciniYukle()
    {
        var rootKlasorler = _context.Klasorler
            .Include(k => k.AltKlasorler)
            .Where(k => k.UstKlasorId == null)
            .ToList();

        KlasorTree.Clear();
        foreach (var k in rootKlasorler)
        {
            var item = new KlasorTreeItem(k);
            CocuklariDoldur(item);
            KlasorTree.Add(item);
        }
    }

    private void CocuklariDoldur(KlasorTreeItem parent)
    {
        var altKlasorler = _context.Klasorler
            .Include(k => k.AltKlasorler)
            .Where(k => k.UstKlasorId == parent.Id)
            .ToList();

        parent.Children.Clear();
        foreach (var k in altKlasorler)
        {
            var item = new KlasorTreeItem(k);
            CocuklariDoldur(item);
            parent.Children.Add(item);
        }
    }

    private void EvraklariYukle()
    {
        if (SelectedTreeItem == null)
        {
            Evraklar.Clear();
            return;
        }

        var evraklar = _context.Evraklar
            .Include(e => e.Klasor)
            .Where(e => e.KlasorId == SelectedTreeItem.Id && !e.Silindi)
            .OrderByDescending(e => e.Tarih)
            .ToList();

        Evraklar.Clear();
        foreach (var e in evraklar)
            Evraklar.Add(e);

        StatusText = $"{SelectedTreeItem.KlasorAdi} - {Evraklar.Count} evrak";
    }

    private void KlasorEkle(object? param)
    {
        var dialog = new Views.KlasorEkleDialog(_context);
        if (dialog.ShowDialog() == true && dialog.SecilenKlasor != null)
        {
            KlasorAgaciniYukle();
        }
    }

    private void KlasorSil()
    {
        if (SelectedTreeItem == null) return;

        var evrakSayisi = _context.Evraklar.Count(e => e.KlasorId == SelectedTreeItem.Id && !e.Silindi);
        var altKlasorSayisi = _context.Klasorler.Count(k => k.UstKlasorId == SelectedTreeItem.Id);

        if (evrakSayisi > 0 || altKlasorSayisi > 0)
        {
            System.Windows.MessageBox.Show(
                $"Bu klasörde {evrakSayisi} evrak ve {altKlasorSayisi} alt klasör var. Önce bunları silin.",
                "Uyarı",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"'{SelectedTreeItem.KlasorAdi}' klasörünü silmek istediğinize emin misiniz?",
            "Klasör Sil",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _context.Klasorler.Remove(SelectedTreeItem.Klasor);
            _context.SaveChanges();
            KlasorAgaciniYukle();
        }
    }

    private void EvrakEkle(object? param)
    {
        if (SelectedTreeItem == null)
        {
            System.Windows.MessageBox.Show("Lütfen önce bir klasör seçin.", "Uyarı", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var dialog = new Views.EvrakEkleGuncelleDialog(_context, null, SelectedTreeItem.Id, _pdfFolderPath);
        if (dialog.ShowDialog() == true)
        {
            EvraklariYukle();
        }
    }

    private void EvrakGuncelle()
    {
        if (SelectedEvrak == null) return;

        var dialog = new Views.EvrakEkleGuncelleDialog(_context, SelectedEvrak, SelectedEvrak.KlasorId, _pdfFolderPath);
        if (dialog.ShowDialog() == true)
        {
            _context.Entry(SelectedEvrak).Reload();
            EvraklariYukle();
        }
    }

    private void EvrakSil()
    {
        if (SelectedEvrak == null) return;

        var result = System.Windows.MessageBox.Show(
            $"'{SelectedEvrak.Konu}' evrağını silmek istediğinize emin misiniz?\n\nNot: Evrak geri alınabilir şekilde 'silindi' olarak işaretlenecektir.",
            "Evrak Sil",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            SelectedEvrak.Silindi = true;
            SelectedEvrak.SilmeTarihi = DateTime.Now;
            _context.SaveChanges();
            EvraklariYukle();
        }
    }

    public void PdfAc()
    {
        if (SelectedEvrak == null || string.IsNullOrEmpty(SelectedEvrak.PdfDosyaAdi)) return;

        var tamYol = Path.Combine(_pdfFolderPath, SelectedEvrak.PdfDosyaAdi);

        if (!File.Exists(tamYol))
        {
            System.Windows.MessageBox.Show(
                $"PDF dosyası bulunamadı: {tamYol}\n\nDosya taşınmış veya silinmiş olabilir.",
                "Hata",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = tamYol,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"PDF açılamadı: {ex.Message}",
                "Hata",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void AramaYap()
    {
        if (string.IsNullOrWhiteSpace(AramaMetni) && string.IsNullOrWhiteSpace(AramaSayi) &&
            AramaBaslangicTarih == null && AramaBitisTarih == null)
        {
            EvraklariYukle();
            return;
        }

        var query = _context.Evraklar
            .Include(e => e.Klasor)
            .Where(e => !e.Silindi);

        if (!string.IsNullOrWhiteSpace(AramaMetni))
            query = query.Where(e => e.Konu.Contains(AramaMetni) || (e.Aciklama != null && e.Aciklama.Contains(AramaMetni)));

        if (!string.IsNullOrWhiteSpace(AramaSayi))
            query = query.Where(e => e.Sayi.Contains(AramaSayi));

        if (AramaBaslangicTarih != null)
            query = query.Where(e => e.Tarih >= AramaBaslangicTarih);

        if (AramaBitisTarih != null)
            query = query.Where(e => e.Tarih <= AramaBitisTarih);

        var sonuclar = query.OrderByDescending(e => e.Tarih).ToList();

        Evraklar.Clear();
        foreach (var e in sonuclar)
            Evraklar.Add(e);
    }

    private void Temizle()
    {
        AramaMetni = string.Empty;
        AramaSayi = string.Empty;
        AramaBaslangicTarih = null;
        AramaBitisTarih = null;
        EvraklariYukle();
    }
}