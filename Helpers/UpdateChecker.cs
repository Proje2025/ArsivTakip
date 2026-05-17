using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace ArsivTakip.Helpers;

public class UpdateChecker
{
    private const string GitHubApiUrl = "https://api.github.com/repos/ahiska03/ArsivTakip/releases/latest";
    private readonly string _currentVersion;
    private readonly string _appPath;
    private string? _downloadUrl;

    public string CurrentVersion => _currentVersion;
    public string LatestVersion { get; private set; } = "";
    public string ReleaseNotes { get; private set; } = "";
    public bool UpdateAvailable { get; private set; } = false;

    public UpdateChecker()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        _currentVersion = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        _appPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
    }

    public async Task<bool> CheckForUpdateAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ArsivTakip");
        client.Timeout = TimeSpan.FromSeconds(10);

        var responseMessage = await client.GetAsync(GitHubApiUrl);
        
        if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new Exception("Güncelleme sunucusuna ulaşılamadı (Henüz bir sürüm yayınlanmamış olabilir veya depo gizli).");
        }
        
        responseMessage.EnsureSuccessStatusCode();

        var response = await responseMessage.Content.ReadAsStringAsync();
        var release = JsonDocument.Parse(response);

        if (release.RootElement.TryGetProperty("tag_name", out var tagElement))
        {
            LatestVersion = tagElement.GetString()?.TrimStart('v') ?? "";
        }

        if (release.RootElement.TryGetProperty("body", out var bodyElement))
        {
            ReleaseNotes = bodyElement.GetString() ?? "";
        }

        if (release.RootElement.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.TryGetProperty("name", out var name) && name.GetString()?.EndsWith(".exe") == true)
                {
                    if (asset.TryGetProperty("browser_download_url", out var url))
                    {
                        _downloadUrl = url.GetString() ?? "";
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(LatestVersion))
        {
            throw new Exception("Yayınlanan sürüm bilgisi okunamadı.");
        }

        UpdateAvailable = CompareVersions(_currentVersion, LatestVersion) < 0;
        return UpdateAvailable;
    }

    private int CompareVersions(string v1, string v2)
    {
        var parts1 = v1.Split('.').Select(int.Parse).ToArray();
        var parts2 = v2.Split('.').Select(int.Parse).ToArray();

        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            int p1 = i < parts1.Length ? parts1[i] : 0;
            int p2 = i < parts2.Length ? parts2[i] : 0;

            if (p1 < p2) return -1;
            if (p1 > p2) return 1;
        }
        return 0;
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(IProgress<int>? progress = null)
    {
        if (string.IsNullOrEmpty(_downloadUrl)) return false;

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "ArsivTakip_Update.exe");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "ArsivTakip");

            using var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    var progressPercent = (int)((downloadedBytes * 100) / totalBytes);
                    progress?.Report(progressPercent);
                }
            }

            var batchPath = Path.Combine(Path.GetTempPath(), "update.bat");
            var batchContent = $@"
@echo off
timeout /t 2 /nobreak >nul
copy /y ""{tempPath}"" ""{_appPath}""
start """" ""{_appPath}""
del ""%~f0""
";
            await File.WriteAllTextAsync(batchPath, batchContent);

            Process.Start(new ProcessStartInfo
            {
                FileName = batchPath,
                UseShellExecute = true,
                CreateNoWindow = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}