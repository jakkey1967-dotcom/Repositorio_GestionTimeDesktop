using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GestionTime.Desktop.Models.Dtos;

namespace GestionTime.Desktop.Services.Admin;

/// <summary>Servicio para gestionar usuarios (CRUD, cambio de roles, etc.).</summary>
public sealed class AdminUsersService
{
    private static AdminUsersService? _instance;
    public static AdminUsersService Instance => _instance ??= new AdminUsersService();

    private readonly ILogger? _log;

    private AdminUsersService()
    {
        _log = App.Log;
    }

    /// <summary>Obtiene la lista de roles disponibles desde el backend (GET /api/v1/roles).</summary>
    public async Task<string[]> GetRolesAsync(CancellationToken ct = default)
    {
        try
        {
            _log?.LogInformation("📋 Cargando roles disponibles desde backend...");
            
            var response = await App.Api.GetAsync<GestionTime.Desktop.Models.Dtos.RolesResponse>("/api/v1/roles", ct);
            
            if (response != null && response.Roles != null && response.Roles.Count > 0)
            {
                var roles = response.Roles.Select(r => r.Name).ToArray();
                _log?.LogInformation("✅ Roles cargados: {roles}", string.Join(", ", roles));
                return roles;
            }
            
            _log?.LogWarning("⚠️ No se obtuvieron roles del backend, usando fallback");
            return GetAvailableRoles();
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error cargando roles desde backend, usando fallback");
            return GetAvailableRoles();
        }
    }
    
    /// <summary>Actualiza los roles de un usuario (PUT /api/v1/users/{id}/roles) - ENDPOINT NUEVO.</summary>
    public async Task<bool> UpdateUserRolesAsync(Guid userId, string[] roles, CancellationToken ct = default)
    {
        try
        {
            _log?.LogInformation("🔄 Actualizando roles del usuario {userId} a [{roles}]...", userId, string.Join(", ", roles));

            var request = new UpdateUserRolesRequest { Roles = roles };
            
            var response = await App.Api.PutAsync<UpdateUserRolesRequest, UpdateUserRolesResponse>(
                $"/api/v1/users/{userId}/roles",
                request,
                ct
            );

            if (response != null && !string.IsNullOrEmpty(response.Message))
            {
                _log?.LogInformation("✅ Roles actualizados correctamente: {message}", response.Message);
                
                // Limpiar caché de usuarios para forzar recarga
                Services.Presence.PresenceService.Instance.ClearCache();
                
                return true;
            }
            else
            {
                _log?.LogWarning("⚠️ No se pudieron actualizar los roles: Sin respuesta válida del backend");
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _log?.LogError(httpEx, "❌ Error HTTP al actualizar roles del usuario {userId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error inesperado al actualizar roles del usuario {userId}", userId);
            return false;
        }
    }
    
    /// <summary>Actualiza el estado enabled de un usuario (PUT /api/v1/users/{id}/enabled).</summary>
    public async Task<bool> UpdateUserEnabledAsync(Guid userId, bool enabled, CancellationToken ct = default)
    {
        try
        {
            _log?.LogInformation("🔄 Actualizando estado enabled del usuario {userId} a {enabled}...", userId, enabled);

            var request = new UpdateUserEnabledRequest { Enabled = enabled };
            
            var response = await App.Api.PutAsync<UpdateUserEnabledRequest, UpdateUserRoleResponse>(
                $"/api/v1/users/{userId}/enabled",
                request,
                ct
            );

            if (response?.Success == true)
            {
                _log?.LogInformation("✅ Estado enabled actualizado correctamente: {message}", response.Message);
                
                // Limpiar caché de usuarios para forzar recarga
                Services.Presence.PresenceService.Instance.ClearCache();
                
                return true;
            }
            else
            {
                _log?.LogWarning("⚠️ No se pudo actualizar el estado enabled: {message}", response?.Message ?? "Sin respuesta");
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _log?.LogError(httpEx, "❌ Error HTTP al actualizar enabled del usuario {userId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error inesperado al actualizar enabled del usuario {userId}", userId);
            return false;
        }
    }
    
    /// <summary>Echa a un usuario online revocando todas sus sesiones activas (POST /api/v1/admin/presence/users/{userId}/kick).</summary>
    public async Task<bool> KickUserAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
        _log?.LogInformation("🚪 Echando usuario {userId}...", userId);

            var response = await App.Api.PostAsync<object, KickUserResponse>(
                $"/api/v1/admin/presence/users/{userId}/kick",
                new { }, // Objeto vacío en lugar de null
                ct
            );

            if (response?.Ok == true)
            {
                _log?.LogInformation("✅ Usuario echado correctamente: {message} ({sessionsRevoked} sesiones revocadas)", 
                    response.Message, response.SessionsRevoked);
                
                // Limpiar caché de presencia para refrescar inmediatamente
                Services.Presence.PresenceService.Instance.ClearCache();
                
                return true;
            }
            else
            {
                _log?.LogWarning("⚠️ No se pudo echar al usuario: {message}", response?.Message ?? "Sin respuesta");
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _log?.LogError(httpEx, "❌ Error HTTP al echar usuario {userId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error inesperado al echar usuario {userId}", userId);
            return false;
        }
    }
    
    /// <summary>Obtiene la lista de roles disponibles (hardcoded fallback).</summary>
    public string[] GetAvailableRoles()
    {
        return new[] { "ADMIN", "EDITOR", "USER" };
    }

    /// <summary>Valida si un rol es válido.</summary>
    public bool IsValidRole(string role)
    {
        var validRoles = GetAvailableRoles();
        return Array.Exists(validRoles, r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }
}
