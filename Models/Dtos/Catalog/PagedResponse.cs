using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos.Catalog;

/// <summary>Respuesta paginada genérica del backend</summary>
public sealed class PagedResponse<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();

    /// <summary>Total de elementos (totalItems en backend, totalCount alias por compatibilidad)</summary>
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }
    
    /// <summary>Alias de TotalItems para compatibilidad con spec original</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount 
    { 
        get => TotalItems; 
        set => TotalItems = value; 
    }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }
}
