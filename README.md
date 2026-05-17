# Arşiv Takip Programı

Evrak ve belge arşivleme sistemi.

## Kurulum

### 1. SQL Server Express Kurulumu
1. Microsoft SQL Server Express'i bir bilgisayara kurun
2. Kurulum sırasında **SQL Server Authentication** veya **Windows Authentication** seçin
3. Sunucu adını not edin (örn: `SUNUCU\SQLEXPRESS`)

### 2. Network Share Klasörü
1. PDF dosyaları için ağ paylaşımı oluşturun (örn: `\\SUNUCU\Arsiv`)
2. Tüm kullanıcıların okuma/yazma yetkisi olduğundan emin olun

### 3. Uygulama Yapılandırması
`appsettings.json` dosyasını açın ve sunucu adresini güncelleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SUNUCU\\SQLEXPRESS;Database=ArsivDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "PdfFolderPath": "\\\\SUNUCU\\Arsiv"
}
```

- `SUNUCU` yerine SQL Server'ın kurulu olduğu bilgisayarın adını yazın
- `SQLEXPRESS` instance adı farklıysa değiştirin

### 4. İlk Çalıştırma
Uygulamayı çalıştırdığınızda veritabanı otomatik olarak oluşturulacaktır.

## Kullanım

### Klasör İşlemleri
- **Klasör Ekle**: Sol paneldeki "+ Klasör Ekle" butonu ile yeni klasör ekleyin
- **Alt Klasör**: Klasör ekleme penceresinde üst klasör seçebilirsiniz
- **Klasör Sil**: Seçili klasörü silmek için butona tıklayın (içinde evrak varsa silinemez)

### Evrak İşlemleri
- **Evrak Ekle**: Bir klasör seçtikten sonra "+ Evrak Ekle" butonu ile evrak ekleyin
- **Evrak Güncelle**: Listeden bir evrak seçip "Evrak Güncelle" butonuna tıklayın
- **Evrak Sil**: Evraklar "silindi" olarak işaretlenir (geri alınabilir)
- **PDF Aç**: Evrakın PDF'ini görüntülemek için "PDF Aç" butonuna tıklayın

### Arama
Sağ paneldeki arama alanından:
- Konu veya açıklama arama
- Evrak sayısına göre arama
- Tarih aralığına göre arama

## Teknik Detaylar

| Bileşen | Açıklama |
|---------|-----------|
| Platform | C# WPF (.NET 8) |
| Veritabanı | SQL Server Express |
| ORM | Entity Framework Core 8.0 |
| PDF Depolama | Network Share Klasörü |

## Departman Bazlı Kullanım

Her departman kendi SQL Server Express kurulumunu kullanır:
- Her departman farklı veritabanı kullanır
- Veriler birbirinden bağımsızdır
- Her departman kendi network share klasörünü kullanır