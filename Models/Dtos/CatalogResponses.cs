using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>
/// DTO para respuesta de clientes desde la API
/// </summary>
public sealed class ClienteResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>
/// DTO para respuesta de grupos desde la API
/// </summary>
public sealed class GrupoResponse
{
    [JsonPropertyName("id_grupo")]
    public int Id_grupo { get; set; }
    
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>
/// DTO para respuesta de tipos desde la API
/// </summary>
public sealed class TipoResponse
{
    [JsonPropertyName("id_tipo")]
    public int Id_tipo { get; set; }
    
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>
/// DTO para request de creación/actualización de parte
/// POST /api/v1/partes (creación) - usa CreateParteRequest
/// PUT /api/v1/partes/{id} (actualización) - usa UpdateParteRequest
/// 
/// Campos requeridos: fecha_trabajo, hora_inicio, hora_fin, id_cliente, accion
/// Estado es INT: 0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado
/// </summary>
public sealed class ParteRequest
{
    [JsonPropertyName("fecha_trabajo")]
    public DateTime FechaTrabajo { get; set; }

    [JsonPropertyName("hora_inicio")]
    public string HoraInicio { get; set; } = string.Empty;

    [JsonPropertyName("hora_fin")]
    public string HoraFin { get; set; } = string.Empty;

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

    /// <summary>
    /// Estado del parte (int): 0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado
    /// Solo se envía en PUT (actualización), no en POST (creación)
    /// </summary>
    [JsonPropertyName("estado")]
    public int? Estado { get; set; }
}
