using System;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>DTO para crear un nuevo parte (sin ID generado).</summary>
public sealed class ParteCreateRequest
{
    [JsonPropertyName("fecha_trabajo")]
    public string FechaTrabajo { get; set; } = string.Empty; // yyyy-MM-dd
    
    [JsonPropertyName("hora_inicio")]
    public string HoraInicio { get; set; } = string.Empty;   // HH:mm
    
    [JsonPropertyName("hora_fin")]
    public string? HoraFin { get; set; }                     // HH:mm (opcional)
    
    [JsonPropertyName("duracion_min")]
    public int? DuracionMin { get; set; }                    // minutos
    
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
    public int Estado { get; set; } = 2; // 2=Cerrado por defecto
}
