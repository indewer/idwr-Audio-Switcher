using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AudioSwitcherWidget;

public class UpdateCheckResult
{
    public bool IsUpdateAvailable { get; set; }
    public string LatestVersionTag { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public static class UpdateHelper
{
    public const string CurrentVersion = "0.1 b8";
    public const string CurrentVersionTag = "v0.1-b8";
    private const string GitHubApiUrl = "https://api.github.com/repos/indewer/idwr-Audio-Switcher/releases/latest";

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    static UpdateHelper()
    {
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("idwr-AudioSwitcher", "1.0"));
    }

    public static async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        var result = new UpdateCheckResult();
        try
        {
            using var response = await _http.GetAsync(GitHubApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"GitHub ответил со статусом {(int)response.StatusCode}";
                return result;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("tag_name", out var tagElem))
            {
                result.LatestVersionTag = tagElem.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("body", out var bodyElem))
            {
                result.ReleaseNotes = bodyElem.GetString() ?? string.Empty;
            }

            // Find executable asset (AudioSwitcherWidget.exe)
            if (root.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsElem.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var nameProp))
                    {
                        string name = nameProp.GetString() ?? "";
                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            if (asset.TryGetProperty("browser_download_url", out var urlProp))
                            {
                                result.DownloadUrl = urlProp.GetString() ?? string.Empty;
                                break;
                            }
                        }
                    }
                }
            }

            result.IsUpdateAvailable = IsNewer(result.LatestVersionTag, CurrentVersionTag);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public static async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, Action<string>? statusCallback = null)
    {
        try
        {
            statusCallback?.Invoke("Загрузка обновления...");
            byte[] exeBytes = await _http.GetByteArrayAsync(downloadUrl);

            string tempFile = Path.Combine(Path.GetTempPath(), $"idwr_update_{Guid.NewGuid():N}.exe");
            await File.WriteAllBytesAsync(tempFile, exeBytes);

            string? currentExe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExe))
            {
                currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            }

            if (string.IsNullOrEmpty(currentExe) || !File.Exists(tempFile))
            {
                return false;
            }

            statusCallback?.Invoke("Перезапуск...");

            // Command: wait 1 second for current app to exit, overwrite exe, restart it, and delete temp file
            string cmdArgs = $"/c timeout /t 1 /nobreak >nul & move /y \"{tempFile}\" \"{currentExe}\" & start \"\" \"{currentExe}\"";

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmdArgs,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(psi);

            // Cleanly shutdown current app
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.Shutdown();
            });

            return true;
        }
        catch (Exception ex)
        {
            statusCallback?.Invoke($"Ошибка: {ex.Message}");
            return false;
        }
    }

    private static bool IsNewer(string latestTag, string currentTag)
    {
        if (string.IsNullOrWhiteSpace(latestTag)) return false;
        if (string.Equals(latestTag.Trim(), currentTag.Trim(), StringComparison.OrdinalIgnoreCase)) return false;

        // Compare beta versions like v0.1-b8 vs v0.1-b7
        try
        {
            int latestNum = ExtractBuildNumber(latestTag);
            int currentNum = ExtractBuildNumber(currentTag);
            if (latestNum > 0 && currentNum > 0)
            {
                return latestNum > currentNum;
            }
        }
        catch { }

        // Fallback: if tags differ and latest is not empty
        return !string.Equals(latestTag, currentTag, StringComparison.OrdinalIgnoreCase);
    }

    private static int ExtractBuildNumber(string tag)
    {
        // Extracts '8' from 'v0.1-b8' or '0.1 b8'
        var lower = tag.ToLowerInvariant();
        int idx = lower.IndexOf('b');
        if (idx >= 0 && idx + 1 < lower.Length)
        {
            string numPart = new string(lower.Substring(idx + 1).TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(numPart, out int num))
            {
                return num;
            }
        }
        return 0;
    }
}
