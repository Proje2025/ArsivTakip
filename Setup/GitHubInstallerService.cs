using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace ArsivTakip.Setup;

public sealed class GitHubInstallerService
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/Proje2025/ArsivTakip/releases/latest";

    private static readonly string[] PreferredAssetNames =
    [
        "ArsivTakipKurulum.exe",
        "BirtanaArsivKurulum.exe",
        "BirtanaArsivSetup.exe"
    ];

    public string LatestVersion { get; private set; } = "";
    public string ReleaseNotes { get; private set; } = "";
    public string InstallerFileName { get; private set; } = "";
    public string DownloadUrl { get; private set; } = "";
    public string LastError { get; private set; } = "";

    public async Task<bool> ResolveLatestInstallerAsync(CancellationToken cancellationToken = default)
    {
        LastError = "";
        DownloadUrl = "";
        InstallerFileName = "";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ArsivTakip-Setup");
        client.Timeout = TimeSpan.FromSeconds(30);

        using var response = await client.GetAsync(ReleasesApiUrl, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            LastError = "GitHub'da henüz yayınlanmış bir sürüm bulunamadı.";
            return false;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.TryGetProperty("tag_name", out var tagElement))
        {
            LatestVersion = tagElement.GetString()?.TrimStart('v') ?? "";
        }

        if (document.RootElement.TryGetProperty("body", out var bodyElement))
        {
            ReleaseNotes = bodyElement.GetString() ?? "";
        }

        if (!document.RootElement.TryGetProperty("assets", out var assets))
        {
            LastError = "Yayında kurulum dosyası bulunamadı.";
            return false;
        }

        var candidates = new List<(string Name, string Url)>();
        foreach (var asset in assets.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var nameElement)
                || !asset.TryGetProperty("browser_download_url", out var urlElement))
            {
                continue;
            }

            var name = nameElement.GetString() ?? "";
            var url = urlElement.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (name.Equals("setup.exe", StringComparison.OrdinalIgnoreCase)
                || name.Equals("ArsivTakip.exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            candidates.Add((name, url));
        }

        if (candidates.Count == 0)
        {
            LastError = "Yayında kurulum dosyası (Kurulum.exe) bulunamadı.";
            return false;
        }

        foreach (var preferred in PreferredAssetNames)
        {
            var match = candidates.FirstOrDefault(c => c.Name.Equals(preferred, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(match.Name))
            {
                Select(match.Name, match.Url);
                return true;
            }
        }

        var kurulumAsset = candidates.FirstOrDefault(c =>
            c.Name.Contains("Kurulum", StringComparison.OrdinalIgnoreCase)
            || c.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(kurulumAsset.Name))
        {
            Select(kurulumAsset.Name, kurulumAsset.Url);
            return true;
        }

        LastError = "Kurulum paketi bulunamadı. GitHub release'e ArsivTakipKurulum.exe ekleyin.";
        return false;
    }

    public async Task<string> DownloadInstallerAsync(
        IProgress<int>? progress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(DownloadUrl))
        {
            throw new InvalidOperationException("İndirme adresi hazır değil.");
        }

        var targetPath = Path.Combine(
            Path.GetTempPath(),
            $"ArsivTakip_{LatestVersion}_{InstallerFileName}");

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ArsivTakip-Setup");
        client.Timeout = TimeSpan.FromMinutes(30);

        using var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var downloadedBytes = 0L;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            downloadedBytes += bytesRead;

            if (totalBytes > 0)
            {
                progress?.Report((int)(downloadedBytes * 100 / totalBytes));
            }
        }

        await fileStream.FlushAsync(cancellationToken);
        return targetPath;
    }

    private void Select(string name, string url)
    {
        InstallerFileName = name;
        DownloadUrl = url;
    }
}
