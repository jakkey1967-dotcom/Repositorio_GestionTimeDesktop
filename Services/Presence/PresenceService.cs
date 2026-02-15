using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GestionTime.Desktop.Models.Dtos;

namespace GestionTime.Desktop.Services.Presence;

/// <summary>Servicio para gestionar presencia de usuarios (online/offline) con polling periódico.</summary>
public sealed class PresenceService
{
    private static PresenceService? _instance;
    public static PresenceService Instance => _instance ??= new PresenceService();

    private readonly ILogger? _log;
    private DateTime _lastFetch = DateTime.MinValue;
    private List<PresenceUserDto> _cachedUsers = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(15);
    private readonly SemaphoreSlim _lock = new(1, 1);

    private PresenceService()
    {
        _log = App.Log;
    }

    /// <summary>Obtiene la lista de usuarios desde la API (con caché de 15 segundos).</summary>
    public async Task<List<PresenceUserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastFetch;

            if (_cachedUsers.Any() && elapsed < _cacheDuration)
            {
                _log?.LogDebug("📦 Usuarios desde caché ({count} usuarios, caché válido por {remaining:F1}s)", 
                    _cachedUsers.Count, (_cacheDuration - elapsed).TotalSeconds);
                return _cachedUsers;
            }

            _log?.LogInformation("🌐 Cargando usuarios desde API GET /api/v1/presence/users...");

            try
            {
                var response = await App.Api.GetAsync<List<PresenceUserDto>>("/api/v1/presence/users", ct);

                if (response == null || !response.Any())
                {
                    _log?.LogWarning("⚠️ API devolvió lista vacía o null");
                    return _cachedUsers;
                }

                _cachedUsers = response;
                _lastFetch = now;

                _log?.LogInformation("✅ Usuarios cargados: {count} usuarios", _cachedUsers.Count);

                return _cachedUsers;
            }
            catch (HttpRequestException httpEx)
            {
                _log?.LogError(httpEx, "❌ Error HTTP al cargar usuarios desde API");
                return _cachedUsers;
            }
            catch (Exception ex)
            {
                _log?.LogError(ex, "❌ Error inesperado al cargar usuarios");
                return _cachedUsers;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Envía un ping al backend para actualizar last_seen_at del usuario actual.</summary>
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            _log?.LogDebug("📡 Enviando heartbeat a GET /api/v1/health...");

            var response = await App.Api.GetAsync<object>("/api/v1/health", ct);

            _log?.LogDebug("✅ Heartbeat enviado correctamente");
            return true;
        }
        catch (HttpRequestException httpEx)
        {
            _log?.LogWarning(httpEx, "⚠️ Error HTTP al enviar heartbeat");
            return false;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error al enviar heartbeat");
            return false;
        }
    }

    /// <summary>Limpia el caché de usuarios.</summary>
    public void ClearCache()
    {
        _lock.Wait();
        try
        {
            _cachedUsers.Clear();
            _lastFetch = DateTime.MinValue;
            _log?.LogDebug("🗑️ Caché de usuarios limpiado");
        }
        finally
        {
            _lock.Release();
        }
    }
}
