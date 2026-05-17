using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ArsivTakip.Helpers;

public class UpdateChecker
{
    private const string GitHubApiUrl = "https://api.github.com/repos/Proje2025/ArsivTakip/releases/latest";
    public const string UpdateLogFileName = "ArsivTakip_update.log";

    private readonly string _assemblyVersion;
    private readonly string _appPath;
    private string? _downloadUrl;
    private bool _downloadIsInstaller;

    public string CurrentVersion => _assemblyVersion;
    /// <summary>Eski uyumluluk; her zaman kurulu dosya sürümü.</summary>
    public string EffectiveVersion => _assemblyVersion;
    public string LatestVersion { get; private set; } = "";
    public string ReleaseNotes { get; private set; } = "";
    public bool UpdateAvailable { get; private set; } = false;
    public bool HasDownloadAsset => !string.IsNullOrEmpty(_downloadUrl);
    public string LastError { get; private set; } = "";

    public static string UpdateLogPath => Path.Combine(Path.GetTempPath(), UpdateLogFileName);

    public UpdateChecker()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        _assemblyVersion = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        _appPath = ResolveAppPath();
        MigrateAppliedVersionFromLog(_appPath);
        ClearStaleAppliedVersion(_assemblyVersion, _appPath);
    }

    private static void MigrateAppliedVersionFromLog(string appPath)
    {
        if (string.IsNullOrEmpty(appPath) || ReadAppliedVersion(appPath) != null)
        {
            return;
        }

        if (!File.Exists(UpdateLogPath))
        {
            return;
        }

        try
        {
            var log = File.ReadAllText(UpdateLogPath);
            if (!log.Contains("UPDATE_SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var match = Regex.Match(log, @"version=(\d+\.\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return;
            }

            var appDir = Path.GetDirectoryName(appPath);
            if (string.IsNullOrEmpty(appDir))
            {
                return;
            }

            File.WriteAllText(Path.Combine(appDir, ".applied-version"), match.Groups[1].Value);
            File.Delete(UpdateLogPath);
        }
        catch
        {
            // Geçiş başarısız olursa normal akış devam eder
        }
    }

    /// <summary>
    /// Önceki güncelleme etiketi (.applied-version) dosyayı değiştirmeden kaldıysa
    /// yanlış "güncelsiniz" sonucunu önlemek için temizlenir.
    /// </summary>
    private static void ClearStaleAppliedVersion(string assemblyVersion, string appPath)
    {
        var applied = ReadAppliedVersion(appPath);
        if (string.IsNullOrEmpty(applied))
        {
            return;
        }

        if (CompareVersions(assemblyVersion, applied) < 0)
        {
            TryDeleteAppliedVersion(appPath);
        }
    }

    private static void TryDeleteAppliedVersion(string appPath)
    {
        try
        {
            var appDir = Path.GetDirectoryName(appPath);
            if (string.IsNullOrEmpty(appDir))
            {
                return;
            }

            var appliedPath = Path.Combine(appDir, ".applied-version");
            if (File.Exists(appliedPath))
            {
                File.Delete(appliedPath);
            }
        }
        catch
        {
            // Sessizce devam et
        }
    }

    private static string? ReadAppliedVersion(string appPath)
    {
        try
        {
            var appDir = Path.GetDirectoryName(appPath);
            if (string.IsNullOrEmpty(appDir))
            {
                return null;
            }

            var appliedPath = Path.Combine(appDir, ".applied-version");
            if (!File.Exists(appliedPath))
            {
                return null;
            }

            var text = File.ReadAllText(appliedPath).Trim().TrimStart('v');
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveAppPath()
    {
        if (!string.IsNullOrEmpty(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
        {
            return Path.GetFullPath(Environment.ProcessPath);
        }

        var modulePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(modulePath) && File.Exists(modulePath))
        {
            return Path.GetFullPath(modulePath);
        }

        var baseDirExe = Path.Combine(AppContext.BaseDirectory, "ArsivTakip.exe");
        if (File.Exists(baseDirExe))
        {
            return Path.GetFullPath(baseDirExe);
        }

        return "";
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

        _downloadUrl = null;
        _downloadIsInstaller = false;

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
            string? portableUrl = null;
            string? installerUrl = null;
            string? installerName = null;

            foreach (var asset in assets.EnumerateArray())
            {
                if (!asset.TryGetProperty("name", out var nameElement)
                    || !asset.TryGetProperty("browser_download_url", out var urlElement))
                {
                    continue;
                }

                var name = nameElement.GetString() ?? "";
                var url = urlElement.GetString() ?? "";
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
                {
                    continue;
                }

                if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (name.Equals("setup.exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (name.Equals("ArsivTakip.exe", StringComparison.OrdinalIgnoreCase))
                {
                    portableUrl = url;
                    continue;
                }

                if (name.Contains("Kurulum", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("BirtanaArsivKurulum.exe", StringComparison.OrdinalIgnoreCase))
                {
                    installerUrl = url;
                    installerName = name;
                }
            }

            if (!string.IsNullOrEmpty(portableUrl))
            {
                _downloadUrl = portableUrl;
                _downloadIsInstaller = false;
            }
            else if (!string.IsNullOrEmpty(installerUrl))
            {
                _downloadUrl = installerUrl;
                _downloadIsInstaller = true;
            }
        }

        if (string.IsNullOrEmpty(LatestVersion))
        {
            throw new Exception("Yayınlanan sürüm bilgisi okunamadı.");
        }

        var versionIsNewer = CompareVersions(_assemblyVersion, LatestVersion) < 0;
        UpdateAvailable = versionIsNewer && HasDownloadAsset;
        return UpdateAvailable;
    }

    private static int CompareVersions(string v1, string v2)
    {
        var parts1 = ParseVersionParts(v1);
        var parts2 = ParseVersionParts(v2);

        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            int p1 = i < parts1.Length ? parts1[i] : 0;
            int p2 = i < parts2.Length ? parts2[i] : 0;

            if (p1 < p2) return -1;
            if (p1 > p2) return 1;
        }
        return 0;
    }

    private static int[] ParseVersionParts(string version)
    {
        var cleaned = version.Trim().TrimStart('v');
        var segments = cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var parts = new int[Math.Max(segments.Length, 3)];

        for (var i = 0; i < segments.Length && i < parts.Length; i++)
        {
            var segment = segments[i];
            var digits = new string(segment.TakeWhile(char.IsDigit).ToArray());
            parts[i] = int.TryParse(digits, out var value) ? value : 0;
        }

        return parts;
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(IProgress<int>? progress = null)
    {
        LastError = "";

        if (string.IsNullOrEmpty(_downloadUrl))
        {
            LastError = "İndirilebilir güncelleme dosyası bulunamadı.";
            return false;
        }

        if (string.IsNullOrEmpty(_appPath) || !File.Exists(_appPath))
        {
            LastError = "Uygulama yolu belirlenemedi.";
            return false;
        }

        try
        {
            if (File.Exists(UpdateLogPath))
            {
                File.Delete(UpdateLogPath);
            }

            var fileName = _downloadIsInstaller
                ? $"ArsivTakip_Kurulum_{LatestVersion}.exe"
                : "ArsivTakip_Update.exe";
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "ArsivTakip");
            client.Timeout = TimeSpan.FromMinutes(30);

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

            await fileStream.FlushAsync();

            if (_downloadIsInstaller)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });
                return true;
            }

            var pid = Environment.ProcessId;
            var scriptPath = Path.Combine(Path.GetTempPath(), "ArsivTakip_update.ps1");
            var appDir = Path.GetDirectoryName(_appPath) ?? AppContext.BaseDirectory;
            var scriptContent = BuildUpdateScript(pid, tempPath, _appPath, appDir, UpdateLogPath, LatestVersion);
            await File.WriteAllTextAsync(scriptPath, scriptContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            var batchProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (batchProcess == null)
            {
                LastError = "Güncelleme betiği başlatılamadı.";
                await WriteLogAsync(LastError);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            await WriteLogAsync($"DOWNLOAD_FAILED: {ex.Message}");
            return false;
        }
    }

    private static string BuildUpdateScript(int pid, string sourcePath, string destPath, string workingDir, string logPath, string appliedVersion)
    {
        var src = EscapePsString(sourcePath);
        var dest = EscapePsString(destPath);
        var dir = EscapePsString(workingDir);
        var log = EscapePsString(logPath);
        var version = EscapePsString(appliedVersion.TrimStart('v'));

        return $$"""
            $ErrorActionPreference = 'Continue'
            $log = '{{log}}'
            $pidToWait = {{pid}}
            $src = '{{src}}'
            $dest = '{{dest}}'
            $workDir = '{{dir}}'
            $appliedVersion = '{{version}}'
            "UPDATE_STARTED $(Get-Date -Format o)" | Out-File -LiteralPath $log -Encoding utf8

            while (Get-Process -Id $pidToWait -ErrorAction SilentlyContinue) {
                Start-Sleep -Seconds 1
            }

            $copied = $false
            $lastError = ''
            for ($attempt = 1; $attempt -le 30; $attempt++) {
                try {
                    Copy-Item -LiteralPath $src -Destination $dest -Force -ErrorAction Stop
                    $copied = $true
                    break
                } catch {
                    $lastError = $_.Exception.Message
                    Start-Sleep -Seconds 2
                }
            }

            if (-not $copied) {
                "COPY_FAILED $lastError" | Add-Content -LiteralPath $log -Encoding utf8
                exit 1
            }

            Set-Content -LiteralPath (Join-Path $workDir '.applied-version') -Value $appliedVersion -Encoding utf8 -NoNewline
            "UPDATE_SUCCESS $(Get-Date -Format o) dest=$dest version=$appliedVersion" | Add-Content -LiteralPath $log -Encoding utf8
            Start-Process -FilePath $dest -WorkingDirectory $workDir
            exit 0
            """;
    }

    private static string EscapePsString(string value) => value.Replace("'", "''");

    public static bool TryGetFailedUpdateMessage(out string message)
    {
        message = "";
        if (!File.Exists(UpdateLogPath))
        {
            return false;
        }

        try
        {
            var log = File.ReadAllText(UpdateLogPath);
            if (!log.Contains("COPY_FAILED", StringComparison.OrdinalIgnoreCase)
                && !log.Contains("DOWNLOAD_FAILED", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            message =
                "Son güncelleme uygulanamadı. Uygulamayı kapatıp tekrar deneyin veya GitHub'dan manuel indirin.\n\n" +
                $"Ayrıntılar:\n{log.Trim()}\n\nDosya: {UpdateLogPath}";
            File.Delete(UpdateLogPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WriteLogAsync(string line)
    {
        try
        {
            await File.AppendAllTextAsync(UpdateLogPath, line + Environment.NewLine);
        }
        catch
        {
            // Log yazılamazsa sessizce devam et
        }
    }
}
