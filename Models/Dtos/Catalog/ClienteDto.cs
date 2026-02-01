using System;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos.Catalog;

/// <summary>Cliente - Respuesta del endpoint /api/v1/clientes</summary>
public sealed class ClienteDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("idPuntoop")]
    public int? IdPuntoop { get; set; }

    [JsonPropertyName("localNum")]
    public int? LocalNum { get; set; }

    [JsonPropertyName("nombreComercial")]
    public string? NombreComercial { get; set; }

    [JsonPropertyName("provincia")]
    public string? Provincia { get; set; }

    [JsonPropertyName("dataUpdate")]
    public DateTimeOffset? DataUpdate { get; set; }

    [JsonPropertyName("dataHtml")]
    public string? DataHtml { get; set; }

    [JsonPropertyName("nota")]
    public string? Nota { get; set; }
}

/// <summary>Request para crear un cliente</summary>
public sealed class ClienteCreateRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("idPuntoop")]
    public int? IdPuntoop { get; set; }

    [JsonPropertyName("localNum")]
    public int? LocalNum { get; set; }

    [JsonPropertyName("nombreComercial")]
    public string? NombreComercial { get; set; }

    [JsonPropertyName("provincia")]
    public string? Provincia { get; set; }

    [JsonPropertyName("nota")]
    public string? Nota { get; set; }
}

/// <summary>Request para actualizar un cliente (PUT completo)</summary>
public sealed class ClienteUpdateRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("idPuntoop")]
    public int? IdPuntoop { get; set; }

    [JsonPropertyName("localNum")]
    public int? LocalNum { get; set; }

    [JsonPropertyName("nombreComercial")]
    public string? NombreComercial { get; set; }

    [JsonPropertyName("provincia")]
    public string? Provincia { get; set; }

    [JsonPropertyName("nota")]
    public string? Nota { get; set; }
}

/// <summary>Request para actualizar solo la nota de un cliente (PATCH)</summary>
public sealed class ClienteUpdateNotaRequest
{
    [JsonPropertyName("nota")]
    public string? Nota { get; set; }
}
