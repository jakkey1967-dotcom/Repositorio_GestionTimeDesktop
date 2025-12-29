using System;

namespace GestionTime.Desktop.Models.Requests;

public class ParteRequest
{
    // Swagger: "fecha_trabajo": "2025-12-21T15:19:34.954Z"
    public DateTime Fecha_Trabajo { get; set; }

    // Swagger: "hora_inicio": "string"  (ej: "08:30")
    public string Hora_Inicio { get; set; } = "";

    // Swagger: "hora_fin": "string"     (ej: "13:30")
    public string Hora_Fin { get; set; } = "";

    // Swagger: "id_cliente": 0
    public int Id_Cliente { get; set; }

    // Swagger: "tienda": "string"       (ej: "4")
    public string Tienda { get; set; } = "";

    // Swagger: "id_grupo": 0
    public int Id_Grupo { get; set; }

    // Swagger: "id_tipo": 0
    public int Id_Tipo { get; set; }

    // Swagger: "accion": "string"
    public string Accion { get; set; } = "";

    // Swagger: "ticket": "string"
    public string? Ticket { get; set; }
}
