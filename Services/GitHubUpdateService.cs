using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IPS.Services
{
    /// <summary>
    /// Service for checking and downloading updates from GitHub Releases
    /// </summary>
    public class GitHubUpdateService
    {
        // Hardcoded GitHub repository - from git remote origin
        private const string GITHUB_OWNER = "yagami8yte";
        private const string GITHUB_REPO = "IPS";

        private readonly HttpClient _httpClient;
        private readonly string _currentVersion;
        private readonly string _appDirectory;

        public GitHubUpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "IPS-Updater");
            // Prevent caching - GitHub API responses should always be fresh
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store");
            _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");

            // Get current version from assembly
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            _currentVersion = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";

            Console.WriteLine($"[UpdateService] Initialized. Assembly version: {version}, CurrentVersion: {_currentVersion}");

            _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Current application version
        /// </summary>
        public string CurrentVersion => _currentVersion;

        /// <summary>
        /// Check GitHub for the latest release
        /// </summary>
        public async Task<GitHubReleaseInfo?> CheckForUpdatesAsync()
        {
            try
            {
                var url = $"https://api.github.com/repos/{GITHUB_OWNER}/{GITHUB_REPO}/releases/latest";
                Console.WriteLine($"[UpdateService] Checking for updates at: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[UpdateService] GitHub API returned: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubReleaseResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (release == null)
                {
                    Console.WriteLine("[UpdateService] Failed to parse GitHub response");
                    return null;
                }

                // Log raw response for debugging
                Console.WriteLine($"[UpdateService] GitHub API Response - TagName: '{release.TagName}', Name: '{release.Name}'");

                // Parse version from tag (e.g., "v1.2.0" -> "1.2.0")
                var latestVersion = release.TagName?.TrimStart('v', 'V') ?? "0.0.0";
                Console.WriteLine($"[UpdateService] Parsed latestVersion: '{latestVersion}'");

                // Find the ZIP asset
                string? downloadUrl = null;
                string? assetName = null;
                long assetSize = 0;

                if (release.Assets != null)
                {
                    foreach (var asset in release.Assets)
                    {
                        if (asset.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            downloadUrl = asset.BrowserDownloadUrl;
                            assetName = asset.Name;
                            assetSize = asset.Size;
                            break;
                        }
                    }
                }

                var compareResult = CompareVersions(latestVersion, _currentVersion);
                Console.WriteLine($"[UpdateService] CompareVersions('{latestVersion}', '{_currentVersion}') = {compareResult}");

                var info = new GitHubReleaseInfo
                {
                    CurrentVersion = _currentVersion,
                    LatestVersion = latestVersion,
                    IsUpdateAvailable = compareResult > 0,
                    ReleaseNotes = release.Body ?? "",
                    PublishedAt = release.PublishedAt,
                    DownloadUrl = downloadUrl,
                    AssetName = assetName,
                    AssetSize = assetSize,
                    HtmlUrl = release.HtmlUrl
                };

                Console.WriteLine($"[UpdateService] Current: {_currentVersion}, Latest: {latestVersion}, Update available: {info.IsUpdateAvailable}");

                return info;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateService] Error checking for updates: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Download and apply update
        /// </summary>
        public async Task<bool> DownloadAndApplyUpdateAsync(GitHubReleaseInfo releaseInfo, IProgress<(string message, int percent)>? progress = null)
        {
            if (string.IsNullOrEmpty(releaseInfo.DownloadUrl))
            {
                Console.WriteLine("[UpdateService] No download URL available");
                return false;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "IPS_Update_" + Guid.NewGuid().ToString("N")[..8]);
            string zipPath = Path.Combine(tempDir, releaseInfo.AssetName ?? "update.zip");
            string extractPath = Path.Combine(tempDir, "extracted");

            try
            {
                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(extractPath);

                // Step 1: Download ZIP
                progress?.Report(("Downloading update...", 10));
                Console.WriteLine($"[UpdateService] Downloading from: {releaseInfo.DownloadUrl}");

                using (var response = await _httpClient.GetAsync(releaseInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? releaseInfo.AssetSize;

                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);

                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            int percent = (int)(10 + (totalRead * 50 / totalBytes));
                            progress?.Report(($"Downloading... {totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB", percent));
                        }
                    }
                }

                Console.WriteLine($"[UpdateService] Download complete: {zipPath}");

                // Step 2: Extract ZIP
                progress?.Report(("Extracting update...", 65));
                Console.WriteLine($"[UpdateService] Extracting to: {extractPath}");

                ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

                Console.WriteLine("[UpdateService] Extraction complete");

                // Step 3: Create updater script
                progress?.Report(("Preparing update...", 80));

                string updaterScript = CreateUpdaterScript(extractPath, _appDirectory);
                string scriptPath = Path.Combine(tempDir, "update.bat");
                await File.WriteAllTextAsync(scriptPath, updaterScript);

                Console.WriteLine($"[UpdateService] Updater script created: {scriptPath}");

                // Step 4: Launch updater and exit
                progress?.Report(("Launching updater...", 90));

                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = tempDir
                };

                Process.Start(startInfo);

                Console.WriteLine("[UpdateService] Updater launched, application will now exit");
                progress?.Report(("Restarting application...", 100));

                // Give the script a moment to start
                await Task.Delay(500);

                // Exit the application
                Environment.Exit(0);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateService] Update failed: {ex.Message}");

                // Cleanup on failure
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch { }

                return false;
            }
        }

        /// <summary>
        /// Create a batch script that copies files and restarts the app
        /// </summary>
        private string CreateUpdaterScript(string sourcePath, string targetPath)
        {
            // Find the actual files - they might be in a subdirectory
            string actualSource = sourcePath;

            // Check if there's a single subdirectory (common with ZIP extraction)
            var dirs = Directory.GetDirectories(sourcePath);
            if (dirs.Length == 1 && Directory.GetFiles(sourcePath).Length == 0)
            {
                actualSource = dirs[0];
            }

            string exePath = Path.Combine(targetPath, "IPS.exe");

            return $@"@echo off
echo ========================================
echo IPS Auto-Updater
echo ========================================
echo.
echo Waiting for application to close...
timeout /t 3 /nobreak > nul

echo.
echo Copying new files...
echo Source: {actualSource}
echo Target: {targetPath}
echo.

REM Copy all files EXCEPT appSettings.json
for %%f in (""{actualSource}\*.*"") do (
    if /I not ""%%~nxf""==""appSettings.json"" (
        echo Copying: %%~nxf
        copy /Y ""%%f"" ""{targetPath}\"" > nul
    ) else (
        echo Skipping: %%~nxf (keeping existing settings)
    )
)

REM Copy subdirectories if any (like runtimes, etc.)
if exist ""{actualSource}\runtimes"" (
    echo Copying: runtimes folder
    xcopy /E /Y /I ""{actualSource}\runtimes"" ""{targetPath}\runtimes"" > nul
)

echo.
echo ========================================
echo Update complete!
echo ========================================
echo.
echo Starting application...
timeout /t 2 /nobreak > nul

start """" ""{exePath}""

REM Cleanup temp files after a delay
timeout /t 5 /nobreak > nul
rd /s /q ""{Path.GetDirectoryName(actualSource)}""

exit
";
        }

        /// <summary>
        /// Compare two version strings
        /// Returns: positive if v1 > v2, negative if v1 < v2, 0 if equal
        /// </summary>
        private int CompareVersions(string v1, string v2)
        {
            try
            {
                var parts1 = v1.Split('.');
                var parts2 = v2.Split('.');

                int maxLen = Math.Max(parts1.Length, parts2.Length);

                for (int i = 0; i < maxLen; i++)
                {
                    int num1 = i < parts1.Length && int.TryParse(parts1[i], out int n1) ? n1 : 0;
                    int num2 = i < parts2.Length && int.TryParse(parts2[i], out int n2) ? n2 : 0;

                    if (num1 != num2)
                        return num1 - num2;
                }

                return 0;
            }
            catch
            {
                return string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// Information about a GitHub release
    /// </summary>
    public class GitHubReleaseInfo
    {
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public bool IsUpdateAvailable { get; set; }
        public string ReleaseNotes { get; set; } = "";
        public DateTime? PublishedAt { get; set; }
        public string? DownloadUrl { get; set; }
        public string? AssetName { get; set; }
        public long AssetSize { get; set; }
        public string? HtmlUrl { get; set; }

        public string AssetSizeFormatted => AssetSize > 0
            ? $"{AssetSize / 1024.0 / 1024.0:F1} MB"
            : "Unknown size";
    }

    /// <summary>
    /// GitHub API response model - uses JsonPropertyName for snake_case mapping
    /// </summary>
    internal class GitHubReleaseResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("body")]
        public string? Body { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("assets")]
        public GitHubAsset[]? Assets { get; set; }
    }

    internal class GitHubAsset
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
