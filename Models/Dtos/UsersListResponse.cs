using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>Respuesta del endpoint GET /api/v1/admin/users (lista de usuarios del sistema).</summary>
public sealed class UserListItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = Array.Empty<string>();

    [JsonPropertyName("lastSeenAt")]
    public DateTime? LastSeenAt { get; set; }

    /// <summary>Rol principal del usuario (el primero de la lista o "USER" por defecto).</summary>
    [JsonIgnore]
    public string Role => Roles?.FirstOrDefault() ?? "USER";

    /// <summary>Primer nombre (extraído de FullName).</summary>
    [JsonIgnore]
    public string? FirstName
    {
        get
        {
            var parts = FullName?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts?.Length > 0 ? parts[0] : null;
        }
    }

    /// <summary>Apellido (extraído de FullName).</summary>
    [JsonIgnore]
    public string? LastName
    {
        get
        {
            var parts = FullName?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts?.Length > 1 ? parts[1] : null;
        }
    }

    /// <summary>Indica si el usuario está activo.</summary>
    [JsonIgnore]
    public bool IsActive => Enabled;

    /// <summary>Indica si el usuario está online (last_seen_at menor a 2 minutos).</summary>
    [JsonIgnore]
    public bool IsOnline
    {
        get
        {
            if (!Enabled || LastSeenAt == null)
                return false;

            var threshold = DateTime.UtcNow.AddMinutes(-2);
            return LastSeenAt.Value >= threshold;
        }
    }

    /// <summary>Orden de prioridad del rol (para ordenamiento).</summary>
    [JsonIgnore]
    public int RolePriority
    {
        get
        {
            return Role?.ToUpperInvariant() switch
            {
                "ADMIN" => 1,
                "EDITOR" => 2,
                "USER" => 3,
                _ => 4
            };
        }
    }
}
