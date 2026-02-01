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

    /// <summary>Cambia el rol de un usuario.</summary>
    /// <param name="userId">ID del usuario a modificar (GUID).</param>
    /// <param name="newRole">Nuevo rol (ADMIN, EDITOR, USER).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si se actualizó correctamente, False en caso contrario.</returns>
    public async Task<bool> UpdateUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default)
    {
        try
        {
            _log?.LogInformation("🔄 Actualizando rol del usuario {userId} a {role}...", userId, newRole);

            var request = new UpdateUserRoleRequest { Role = newRole };
            
            var response = await App.Api.PutAsync<UpdateUserRoleRequest, UpdateUserRoleResponse>(
                $"/api/v1/admin/users/{userId}/roles",
                request,
                ct
            );

            if (response?.Success == true)
            {
                _log?.LogInformation("✅ Rol actualizado correctamente: {message}", response.Message);
                
                // Limpiar caché de usuarios para forzar recarga
                Services.Presence.PresenceService.Instance.ClearCache();
                
                return true;
            }
            else
            {
                _log?.LogWarning("⚠️ No se pudo actualizar el rol: {message}", response?.Message ?? "Sin respuesta");
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _log?.LogError(httpEx, "❌ Error HTTP al actualizar rol del usuario {userId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error inesperado al actualizar rol del usuario {userId}", userId);
            return false;
        }
    }

    /// <summary>Obtiene la lista de roles disponibles en el sistema.</summary>
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
