using System;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos.Catalog;

/// <summary>Respuesta GET /api/v2/clientes/{id}/notas.</summary>
public sealed class ClienteNotasResponse
{
    [JsonPropertyName("clienteId")]
    public int ClienteId { get; set; }

    [JsonPropertyName("global")]
    public ClienteNotaItem? Global { get; set; }

    [JsonPropertyName("personal")]
    public ClienteNotaItem? Personal { get; set; }
}

/// <summary>Detalle de una nota (global o personal).</summary>
public sealed class ClienteNotaItem
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("updatedByName")]
    public string? UpdatedByName { get; set; }
}

/// <summary>Request PUT /api/v2/clientes/{id}/notas/global o /personal.</summary>
public sealed class ClienteNotaUpdateRequest
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
