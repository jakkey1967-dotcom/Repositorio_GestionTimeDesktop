using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>Respuesta de GET /api/v1/roles</summary>
public sealed class RolesResponse
{
    [JsonPropertyName("roles")]
    public List<RoleDto> Roles { get; set; } = new();
}

/// <summary>Rol individual del sistema</summary>
public sealed class RoleDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
