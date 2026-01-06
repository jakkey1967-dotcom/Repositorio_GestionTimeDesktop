using System;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>DTO para crear un nuevo parte (sin ID generado).</summary>
public sealed class ParteCreateRequest
{
    public string FechaTrabajo { get; set; } = string.Empty; // yyyy-MM-dd
    public string HoraInicio { get; set; } = string.Empty;   // HH:mm
    public string? HoraFin { get; set; }                     // HH:mm (opcional)
    public int? DuracionMin { get; set; }                    // minutos
    public int IdCliente { get; set; }
    public string? Tienda { get; set; }
    public int? IdGrupo { get; set; }
    public int? IdTipo { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Ticket { get; set; }
    public string? Tecnico { get; set; }
    public int Estado { get; set; } = 2; // 2=Cerrado por defecto
}
