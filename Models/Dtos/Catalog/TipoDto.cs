using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos.Catalog;

/// <summary>Tipo - Respuesta del endpoint /api/v1/tipos</summary>
public sealed class TipoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }
}

/// <summary>Request para crear un tipo</summary>
public sealed class TipoCreateRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }
}

/// <summary>Request para actualizar un tipo</summary>
public sealed class TipoUpdateRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }
}
