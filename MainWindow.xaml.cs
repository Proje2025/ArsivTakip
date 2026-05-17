using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ArsivTakip.Data;
using ArsivTakip.ViewModels;
using ArsivTakip.Helpers;
using ArsivTakip.Views;
using System.Windows.Controls;

namespace ArsivTakip;

public partial class MainWindow : Window
{
    private readonly ArsivDbContext _context;
    private readonly MainViewModel _viewModel;
    private readonly UpdateChecker _updateChecker;

    public MainWindow()
    {
        InitializeComponent();

        _updateChecker = new UpdateChecker();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string connectionString;
        string pdfFolderPath;

        var sqlConnection = configuration.GetConnectionString("DefaultConnection");
        if (sqlConnection != null && sqlConnection.Contains("Server="))
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArsivDB.db");
            connectionString = $"Data Source={dbPath}";
        }
        else
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        pdfFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArsivPDF");
        if (!Directory.Exists(pdfFolderPath))
        {
            Directory.CreateDirectory(pdfFolderPath);
        }

        var optionsBuilder = new DbContextOptionsBuilder<ArsivDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        _context = new ArsivDbContext(optionsBuilder.Options);
        _context.Database.EnsureCreated();
        try
        {
            _context.Database.ExecuteSqlRaw("ALTER TABLE Klasorler ADD COLUMN Tarih TEXT;");
        }
        catch { }

        _viewModel = new MainViewModel(_context, pdfFolderPath);
        DataContext = _viewModel;

        KlasorTreeView.SelectedItemChanged += KlasorTreeView_SelectedItemChanged;

        // Evrak listesinde çift tıklama ile PDF açma
        EvrakDataGrid.MouseDoubleClick += EvrakDataGrid_MouseDoubleClick;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var hasUpdate = await _updateChecker.CheckForUpdateAsync();
            if (hasUpdate)
            {
                var dialog = new GuncellemeDialog(_updateChecker);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }
        catch
        {
            // Güncelleme kontrolü başarısız olursa sessizce devam et
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _context?.Dispose();
    }

    private void KlasorTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is KlasorTreeItem treeItem)
        {
            _viewModel.SelectedTreeItem = treeItem;
        }
    }

    private void EvrakDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _viewModel.PdfAc();
    }

    private async void GuncellemeKontrol_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var hasUpdate = await _updateChecker.CheckForUpdateAsync();
            if (hasUpdate)
            {
                var dialog = new GuncellemeDialog(_updateChecker);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
            else
            {
                MessageBox.Show($"Güncelsiniz! Mevcut Sürüm: v{_updateChecker.CurrentVersion}", "Güncelleme Kontrolü", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Güncelleme kontrolü sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}