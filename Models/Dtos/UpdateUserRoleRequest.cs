using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>Request para actualizar el rol de un usuario (PUT /api/v1/admin/users/{id}/roles).</summary>
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
