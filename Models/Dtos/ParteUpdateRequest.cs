using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>DTO para actualizar un parte existente (PUT)</summary>
public sealed class ParteUpdateRequest
{
    [JsonPropertyName("fecha_trabajo")]
    public string FechaTrabajo { get; set; } = string.Empty;
    
    [JsonPropertyName("hora_inicio")]
    public string HoraInicio { get; set; } = string.Empty;
    
    [JsonPropertyName("hora_fin")]
    public string? HoraFin { get; set; }
    
    [JsonPropertyName("duracion_min")]
    public int? DuracionMin { get; set; }
    
    [JsonPropertyName("id_cliente")]
    public int IdCliente { get; set; }
    
    [JsonPropertyName("tienda")]
    public string? Tienda { get; set; }
    
    [JsonPropertyName("id_grupo")]
    public int? IdGrupo { get; set; }
    
    [JsonPropertyName("id_tipo")]
    public int? IdTipo { get; set; }
    
    [JsonPropertyName("accion")]
    public string Accion { get; set; } = string.Empty;
    
    [JsonPropertyName("ticket")]
    public string? Ticket { get; set; }
    
    [JsonPropertyName("tecnico")]
    public string? Tecnico { get; set; }
    
    [JsonPropertyName("estado")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Estado { get; set; } = null;
}
