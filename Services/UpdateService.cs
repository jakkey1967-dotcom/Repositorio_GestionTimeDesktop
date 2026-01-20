using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.Models;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Services;

public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateService> _logger;
    
    // Cambiado: Usar /releases en lugar de /releases/latest para incluir pre-releases
    private const string GitHubApiUrl = "https://api.github.com/repos/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases";
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
            // ✅ Usar VersionInfo centralizado
            var version = VersionInfo.Version;
            
            _logger.LogInformation("=== GetCurrentVersion ===");
            _logger.LogInformation("Versión actual (desde VersionInfo): {Version}", version);
            
            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la versión actual");
            return "1.4.1-beta"; // Fallback actualizado
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
            _logger.LogInformation("Verificando actualizaciones en GitHub (incluyendo pre-releases)...");

            var response = await _httpClient.GetAsync(GitHubApiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("No se pudo obtener información de actualizaciones. Status: {StatusCode}", response.StatusCode);
                return updateInfo;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // El endpoint /releases devuelve un array, tomamos el primer elemento (más reciente)
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                _logger.LogWarning("No se encontraron releases en GitHub");
                return updateInfo;
            }

            var latestRelease = root[0]; // Primer release (más reciente, incluye pre-releases)

            // Obtener información del release
            if (latestRelease.TryGetProperty("tag_name", out var tagName))
            {
                updateInfo.LatestVersion = tagName.GetString()?.TrimStart('v') ?? string.Empty;
            }

            if (latestRelease.TryGetProperty("name", out var name))
            {
                updateInfo.ReleaseName = name.GetString() ?? string.Empty;
            }

            if (latestRelease.TryGetProperty("body", out var body))
            {
                updateInfo.ReleaseNotes = body.GetString() ?? string.Empty;
            }

            if (latestRelease.TryGetProperty("published_at", out var publishedAt))
            {
                if (DateTime.TryParse(publishedAt.GetString(), out var date))
                {
                    updateInfo.PublishedAt = date;
                }
            }

            // Buscar el archivo .msi o .zip en los assets
            if (latestRelease.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var assetName))
                    {
                        var fileName = assetName.GetString() ?? string.Empty;
                        if ((fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) || 
                             fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) && 
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
            _logger.LogInformation("=== Iniciando comparación de versiones ===");
            _logger.LogInformation("CurrentVersion antes de comparar: '{CurrentVersion}'", updateInfo.CurrentVersion);
            _logger.LogInformation("LatestVersion antes de comparar: '{LatestVersion}'", updateInfo.LatestVersion);
            
            updateInfo.UpdateAvailable = IsNewerVersion(updateInfo.CurrentVersion, updateInfo.LatestVersion);

            if (updateInfo.UpdateAvailable)
            {
                _logger.LogInformation("✅ NUEVA VERSIÓN DISPONIBLE: {LatestVersion} (actual: {CurrentVersion})", 
                    updateInfo.LatestVersion, updateInfo.CurrentVersion);
            }
            else
            {
                _logger.LogInformation("ℹ️ La aplicación está actualizada. Versión: {CurrentVersion} (GitHub: {LatestVersion})", 
                    updateInfo.CurrentVersion, updateInfo.LatestVersion);
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
            _logger.LogInformation("=== Comparando versiones ===");
            _logger.LogInformation("Versión actual: '{CurrentVersion}'", currentVersion);
            _logger.LogInformation("Versión última: '{LatestVersion}'", latestVersion);
            
            if (string.IsNullOrEmpty(latestVersion))
            {
                _logger.LogWarning("latestVersion está vacío");
                return false;
            }

            // Limpiar prefijos como 'v'
            currentVersion = currentVersion.TrimStart('v');
            latestVersion = latestVersion.TrimStart('v');
            
            _logger.LogInformation("Después de limpiar 'v' - Actual: '{Current}', Última: '{Latest}'", currentVersion, latestVersion);

            var current = ParseVersion(currentVersion);
            var latest = ParseVersion(latestVersion);
            
            _logger.LogInformation("Parseado - Actual: {CurrentMajor}.{CurrentMinor}.{CurrentPatch}", current.Major, current.Minor, current.Patch);
            _logger.LogInformation("Parseado - Última: {LatestMajor}.{LatestMinor}.{LatestPatch}", latest.Major, latest.Minor, latest.Patch);

            // Comparar major, minor, patch
            if (latest.Major > current.Major)
            {
                _logger.LogInformation("✅ Nueva versión disponible (Major: {LatestMajor} > {CurrentMajor})", latest.Major, current.Major);
                return true;
            }
            if (latest.Major < current.Major)
            {
                _logger.LogInformation("❌ Versión actual es más nueva (Major: {CurrentMajor} > {LatestMajor})", current.Major, latest.Major);
                return false;
            }

            if (latest.Minor > current.Minor)
            {
                _logger.LogInformation("✅ Nueva versión disponible (Minor: {LatestMinor} > {CurrentMinor})", latest.Minor, current.Minor);
                return true;
            }
            if (latest.Minor < current.Minor)
            {
                _logger.LogInformation("❌ Versión actual es más nueva (Minor: {CurrentMinor} > {LatestMinor})", current.Minor, latest.Minor);
                return false;
            }

            if (latest.Patch > current.Patch)
            {
                _logger.LogInformation("✅ Nueva versión disponible (Patch: {LatestPatch} > {CurrentPatch})", latest.Patch, current.Patch);
                return true;
            }
            
            _logger.LogInformation("❌ Versiones son iguales o actual es más nueva");
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
