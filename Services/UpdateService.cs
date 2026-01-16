using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using GestionTime.Desktop.Models;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Services;

public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateService> _logger;
    private const string GitHubApiUrl = "https://api.github.com/repos/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/latest";
    private const string GitHubReleasesUrl = "https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases";

    public UpdateService(ILogger<UpdateService> logger)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GestionTime-Desktop");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    public string GetCurrentVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.1.0";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la versión actual");
            return "1.1.0";
        }
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var updateInfo = new UpdateInfo
        {
            CurrentVersion = GetCurrentVersion()
        };

        try
        {
            _logger.LogInformation("Verificando actualizaciones en GitHub...");

            var response = await _httpClient.GetAsync(GitHubApiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("No se pudo obtener información de actualizaciones. Status: {StatusCode}", response.StatusCode);
                return updateInfo;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Obtener información del release
            if (root.TryGetProperty("tag_name", out var tagName))
            {
                updateInfo.LatestVersion = tagName.GetString()?.TrimStart('v') ?? string.Empty;
            }

            if (root.TryGetProperty("name", out var name))
            {
                updateInfo.ReleaseName = name.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("body", out var body))
            {
                updateInfo.ReleaseNotes = body.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("published_at", out var publishedAt))
            {
                if (DateTime.TryParse(publishedAt.GetString(), out var date))
                {
                    updateInfo.PublishedAt = date;
                }
            }

            // Buscar el archivo .zip en los assets
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var assetName))
                    {
                        var fileName = assetName.GetString() ?? string.Empty;
                        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && 
                            fileName.Contains("GestionTime", StringComparison.OrdinalIgnoreCase))
                        {
                            if (asset.TryGetProperty("browser_download_url", out var downloadUrl))
                            {
                                updateInfo.DownloadUrl = downloadUrl.GetString() ?? string.Empty;
                            }
                            break;
                        }
                    }
                }
            }

            // Comparar versiones
            updateInfo.UpdateAvailable = IsNewerVersion(updateInfo.CurrentVersion, updateInfo.LatestVersion);

            if (updateInfo.UpdateAvailable)
            {
                _logger.LogInformation("Nueva versión disponible: {LatestVersion} (actual: {CurrentVersion})", 
                    updateInfo.LatestVersion, updateInfo.CurrentVersion);
            }
            else
            {
                _logger.LogInformation("La aplicación está actualizada. Versión: {CurrentVersion}", 
                    updateInfo.CurrentVersion);
            }

            return updateInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar actualizaciones");
            return updateInfo;
        }
    }

    public void OpenReleasesPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubReleasesUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al abrir la página de releases");
        }
    }

    public async Task<bool> DownloadUpdateAsync(string downloadUrl, string destinationPath)
    {
        try
        {
            _logger.LogInformation("Descargando actualización desde: {DownloadUrl}", downloadUrl);

            var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(destinationPath);
            await stream.CopyToAsync(fileStream);

            _logger.LogInformation("Actualización descargada exitosamente en: {DestinationPath}", destinationPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar la actualización");
            return false;
        }
    }

    private bool IsNewerVersion(string currentVersion, string latestVersion)
    {
        try
        {
            if (string.IsNullOrEmpty(latestVersion))
                return false;

            // Limpiar prefijos como 'v'
            currentVersion = currentVersion.TrimStart('v');
            latestVersion = latestVersion.TrimStart('v');

            var current = ParseVersion(currentVersion);
            var latest = ParseVersion(latestVersion);

            // Comparar major, minor, patch
            if (latest.Major > current.Major) return true;
            if (latest.Major < current.Major) return false;

            if (latest.Minor > current.Minor) return true;
            if (latest.Minor < current.Minor) return false;

            if (latest.Patch > current.Patch) return true;

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al comparar versiones: {Current} vs {Latest}", currentVersion, latestVersion);
            return false;
        }
    }

    private (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;
        return (major, minor, patch);
    }
}
