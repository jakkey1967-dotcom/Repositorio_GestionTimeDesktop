using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>Respuesta del endpoint POST /api/v1/admin/presence/users/{userId}/kick.</summary>
public sealed class KickUserResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("sessionsRevoked")]
    public int SessionsRevoked { get; set; }
}
