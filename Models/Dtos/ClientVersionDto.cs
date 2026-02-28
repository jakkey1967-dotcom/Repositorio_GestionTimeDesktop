using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>DTO para registrar la versión del cliente en el backend.</summary>
public sealed class ClientVersionDto
{
    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = "";

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "Desktop";

    [JsonPropertyName("osVersion")]
    public string? OsVersion { get; set; }

    [JsonPropertyName("machineName")]
    public string? MachineName { get; set; }
}

/// <summary>Respuesta del backend al registrar la versión del cliente.</summary>
public sealed class ClientVersionResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("updateRequired")]
    public bool UpdateRequired { get; set; }

    [JsonPropertyName("latestVersion")]
    public string? LatestVersion { get; set; }

    [JsonPropertyName("updateUrl")]
    public string? UpdateUrl { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
