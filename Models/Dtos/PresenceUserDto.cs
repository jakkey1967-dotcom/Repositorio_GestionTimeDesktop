using System;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>Respuesta del endpoint GET /api/v1/presence/users (usuarios con información de presencia).</summary>
public sealed class PresenceUserDto
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("lastSeenAt")]
    public DateTime? LastSeenAt { get; set; }

    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }
}
