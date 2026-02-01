using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos.Catalog;

/// <summary>Grupo - Respuesta del endpoint /api/v1/grupos</summary>
public sealed class GrupoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }
}

/// <summary>Request para crear un grupo</summary>
public sealed class GrupoCreateRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }
}

/// <summary>Request para actualizar un grupo</summary>
public sealed class GrupoUpdateRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }
}
