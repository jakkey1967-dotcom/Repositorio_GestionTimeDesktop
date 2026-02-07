using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>Request para actualizar roles de un usuario (PUT /api/v1/users/{id}/roles) - NUEVO ENDPOINT.</summary>
public sealed class UpdateUserRolesRequest
{
    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = Array.Empty<string>();
}

/// <summary>Request para actualizar el estado enabled de un usuario (PUT /api/v1/users/{id}/enabled).</summary>
public sealed class UpdateUserEnabledRequest
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

/// <summary>Respuesta de actualización de roles (PUT /api/v1/users/{id}/roles).</summary>
public sealed class UpdateUserRolesResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = Array.Empty<string>();
}

/// <summary>Request LEGACY para actualizar el rol de un usuario (PUT /api/v1/admin/users/{id}/roles) - DEPRECADO.</summary>
[Obsolete("Usar UpdateUserRolesRequest con endpoint /api/v1/users/{id}/roles")]
public sealed class UpdateUserRoleRequest
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

/// <summary>Response de actualización de rol de usuario.</summary>
public sealed class UpdateUserRoleResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("user")]
    public UserListItemDto? User { get; set; }
}


