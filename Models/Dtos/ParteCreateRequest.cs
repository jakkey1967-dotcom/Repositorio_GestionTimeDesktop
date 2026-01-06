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
    
    /// <summary>Estado del parte: 1=Abierto, 2=Cerrado, 3=Pausado. Solo se envía si se especifica explícitamente (importación).</summary>
    [JsonPropertyName("estado")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Estado { get; set; } = null; // ✅ MODIFICADO: null por defecto para que no se envíe
}
