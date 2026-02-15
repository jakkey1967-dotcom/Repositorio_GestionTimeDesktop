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

            // GT-BEGIN: Buscar release más reciente que tenga assets descargables
            // Iterar releases (ordenados por fecha desc) hasta encontrar uno con MSI/ZIP
            JsonElement? bestRelease = null;
            string bestDownloadUrl = "";

            foreach (var release in root.EnumerateArray())
            {
                // Saltar drafts
                if (release.TryGetProperty("draft", out var draft) && draft.GetBoolean())
                    continue;

                // Buscar asset MSI o ZIP
                if (release.TryGetProperty("assets", out var releaseAssets) && releaseAssets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in releaseAssets.EnumerateArray())
                    {
                        if (asset.TryGetProperty("name", out var assetName))
                        {
                            var fileName = assetName.GetString() ?? string.Empty;
                            if ((fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ||
                                 fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) &&
                                fileName.Contains("GestionTime", StringComparison.OrdinalIgnoreCase))
                            {
                                if (asset.TryGetProperty("browser_download_url", out var dlUrl))
                                {
                                    bestDownloadUrl = dlUrl.GetString() ?? string.Empty;
                                }
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(bestDownloadUrl))
                {
                    bestRelease = release;
                    break;
                }
            }

            if (bestRelease == null)
            {
                _logger.LogWarning("No se encontró ningún release con assets descargables");
                return updateInfo;
            }

            var latestRelease = bestRelease.Value;
            // GT-END

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

            // URL de descarga ya obtenida en la búsqueda de release
            updateInfo.DownloadUrl = bestDownloadUrl;

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

    // GT-BEGIN: Comparación de versiones semver
    private bool IsNewerVersion(string currentVersion, string latestVersion)
    {
        try
        {
            _logger.LogInformation("=== Comparando versiones ===");
            _logger.LogInformation("Versión actual (raw): '{CurrentVersion}'", currentVersion);
            _logger.LogInformation("Versión última (raw): '{LatestVersion}'", latestVersion);

            if (string.IsNullOrEmpty(latestVersion))
            {
                _logger.LogWarning("latestVersion está vacío");
                return false;
            }

            var current = ParseSemVer(currentVersion);
            var latest = ParseSemVer(latestVersion);

            _logger.LogInformation("Parseado - Actual: {Major}.{Minor}.{Patch} suffix='{Suffix}'", 
                current.Major, current.Minor, current.Patch, current.Suffix);
            _logger.LogInformation("Parseado - Última: {Major}.{Minor}.{Patch} suffix='{Suffix}'", 
                latest.Major, latest.Minor, latest.Patch, latest.Suffix);

            // Comparar major
            if (latest.Major != current.Major)
            {
                var newer = latest.Major > current.Major;
                _logger.LogInformation(newer ? "Nueva versión (Major)" : "Versión actual es más nueva (Major)");
                return newer;
            }

            // Comparar minor
            if (latest.Minor != current.Minor)
            {
                var newer = latest.Minor > current.Minor;
                _logger.LogInformation(newer ? "Nueva versión (Minor)" : "Versión actual es más nueva (Minor)");
                return newer;
            }

            // Comparar patch
            if (latest.Patch != current.Patch)
            {
                var newer = latest.Patch > current.Patch;
                _logger.LogInformation(newer ? "Nueva versión (Patch)" : "Versión actual es más nueva (Patch)");
                return newer;
            }

            // Mismo major.minor.patch — comparar sufijo (release > rc > beta > alpha)
            var currentWeight = GetSuffixWeight(current.Suffix);
            var latestWeight = GetSuffixWeight(latest.Suffix);

            if (latestWeight != currentWeight)
            {
                var newer = latestWeight > currentWeight;
                _logger.LogInformation(newer 
                    ? "Nueva versión (suffix '{LatestSuffix}' > '{CurrentSuffix}')" 
                    : "Versión actual es más nueva (suffix)", latest.Suffix, current.Suffix);
                return newer;
            }

            _logger.LogInformation("Versiones son iguales: {Current} == {Latest}", currentVersion, latestVersion);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al comparar versiones: {Current} vs {Latest}", currentVersion, latestVersion);
            return false;
        }
    }

    /// <summary>Parsea versión semver: "1.9.5-beta+hash" → (1, 9, 5, "beta").</summary>
    private static (int Major, int Minor, int Patch, string Suffix) ParseSemVer(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return (0, 0, 0, "");

        // Limpiar prefijo 'v' y metadata '+hash'
        version = version.TrimStart('v');
        var plusIndex = version.IndexOf('+');
        if (plusIndex >= 0)
            version = version[..plusIndex];

        // Separar sufijo: "1.9.5-beta" → "1.9.5" + "beta"
        var suffix = "";
        var dashIndex = version.IndexOf('-');
        if (dashIndex >= 0)
        {
            suffix = version[(dashIndex + 1)..];
            version = version[..dashIndex];
        }

        // Parsear major.minor.patch
        var parts = version.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;

        return (major, minor, patch, suffix.ToLowerInvariant());
    }

    /// <summary>Peso del sufijo para comparación: release(100) > rc(30) > beta(20) > alpha(10).</summary>
    private static int GetSuffixWeight(string suffix)
    {
        if (string.IsNullOrEmpty(suffix)) return 100; // release (sin sufijo) es la más alta
        if (suffix.StartsWith("rc")) return 30;
        if (suffix.StartsWith("beta")) return 20;
        if (suffix.StartsWith("alpha")) return 10;
        return 15; // sufijo desconocido
    }
    // GT-END
}
