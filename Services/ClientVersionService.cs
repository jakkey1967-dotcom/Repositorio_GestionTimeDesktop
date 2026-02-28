using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.Models.Dtos;

namespace GestionTime.Desktop.Services;

/// <summary>Servicio para registrar la versión del cliente en el backend tras el login.</summary>
public sealed class ClientVersionService
{
    private static ClientVersionService? _instance;
    public static ClientVersionService Instance => _instance ??= new ClientVersionService();

    private readonly ILogger? _log;

    private ClientVersionService()
    {
        _log = App.Log;
    }

    /// <summary>Registra la versión actual del cliente en el backend.</summary>
    public async Task<ClientVersionResponse?> RegisterVersionAsync(CancellationToken ct = default)
    {
        try
        {
            var payload = new ClientVersionDto
            {
                AppVersion = VersionInfo.Version,
                Platform = "Desktop",
                OsVersion = Environment.OSVersion.ToString(),
                MachineName = Environment.MachineName
            };

            _log?.LogInformation("═══════════════════════════════════════════════════════════════");
            _log?.LogInformation("📦 REGISTRO DE VERSIÓN DEL CLIENTE");
            _log?.LogInformation("   • AppVersion: {version}", payload.AppVersion);
            _log?.LogInformation("   • Platform: {platform}", payload.Platform);
            _log?.LogInformation("   • OS: {os}", payload.OsVersion);
            _log?.LogInformation("   • Machine: {machine}", payload.MachineName);
            _log?.LogInformation("   • Endpoint: POST /api/v2/client-version");

            var response = await App.Api.PostAsync<ClientVersionDto, ClientVersionResponse>(
                "/api/v2/client-version", payload, ct);

            if (response != null)
            {
                _log?.LogInformation("✅ Versión registrada correctamente");

                if (response.UpdateRequired)
                {
                    _log?.LogWarning("⚠️ ACTUALIZACIÓN REQUERIDA:");
                    _log?.LogWarning("   • Versión actual: {current}", payload.AppVersion);
                    _log?.LogWarning("   • Versión más reciente: {latest}", response.LatestVersion);
                    _log?.LogWarning("   • URL de descarga: {url}", response.UpdateUrl);
                    _log?.LogWarning("   • Mensaje: {msg}", response.Message);
                }
            }
            else
            {
                _log?.LogDebug("ℹ️ Backend no devolvió respuesta de versión (endpoint puede no existir aún)");
            }

            _log?.LogInformation("═══════════════════════════════════════════════════════════════");
            return response;
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _log?.LogDebug("ℹ️ Endpoint /api/v2/client-version no existe aún en el backend (404) - Ignorando");
            return null;
        }
        catch (Exception ex)
        {
            _log?.LogWarning(ex, "⚠️ Error registrando versión del cliente (no bloqueante)");
            return null;
        }
    }
}
