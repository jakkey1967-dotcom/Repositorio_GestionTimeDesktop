using System;
using System.Text.Json.Serialization;
using GestionTime.Desktop.Helpers;  // 🆕 NUEVO: Para DateOnlyJsonConverter

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>
/// Estados posibles de un parte de trabajo (valores int según la API)
/// </summary>
public enum ParteEstado
{
    Abierto = 0,    // En curso activo (▶️ verde)
    Pausado = 1,    // Temporalmente detenido (⏸️ amarillo)
    Cerrado = 2,    // Finalizado (✅)
    Enviado = 3,    // Enviado al sistema destino
    Anulado = 9     // Cancelado (⛔ gris)
}

public sealed class ParteDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("fecha")]
    [JsonConverter(typeof(DateOnlyJsonConverter))]  // 🆕 NUEVO: Converter para fechas
    public DateTime Fecha { get; set; }

    [JsonIgnore]
    public string FechaText => Fecha == default
        ? string.Empty
        : Fecha.ToString("dd/MM/yyyy");

    [JsonPropertyName("cliente")]
    public string Cliente { get; set; } = "";

    /// <summary>
    /// ID del cliente (necesario para PUT /api/v1/partes/{id})
    /// </summary>
    [JsonPropertyName("id_cliente")]
    public int IdCliente { get; set; }

    [JsonPropertyName("tienda")]
    public string Tienda { get; set; } = "";

    [JsonPropertyName("accion")]
    public string Accion { get; set; } = "";

    [JsonPropertyName("horainicio")]
    public string HoraInicio { get; set; } = "";

    [JsonPropertyName("horafin")]
    public string HoraFin { get; set; } = "";

    [JsonIgnore]
    public string HoraText
    {
        get
        {
            var hi = (HoraInicio ?? string.Empty).Trim();
            var hf = (HoraFin ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(hi) && string.IsNullOrWhiteSpace(hf)) return string.Empty;
            if (string.IsNullOrWhiteSpace(hf)) return hi;
            if (string.IsNullOrWhiteSpace(hi)) return hf;
            return $"{hi}-{hf}";
        }
    }

    [JsonPropertyName("duracion_min")]
    public int DuracionMin { get; set; }

    [JsonIgnore]
    public string DuracionText
    {
        get
        {
            if (DuracionMin <= 0) return string.Empty;
            
            var hours = DuracionMin / 60;
            var minutes = DuracionMin % 60;
            return $"{hours:D2}:{minutes:D2}";
        }
    }

    [JsonPropertyName("ticket")]
    public string Ticket { get; set; } = "";

    [JsonPropertyName("grupo")]
    public string Grupo { get; set; } = "";

    /// <summary>
    /// ID del grupo (necesario para PUT)
    /// </summary>
    [JsonPropertyName("id_grupo")]
    public int? IdGrupo { get; set; }

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "";

    /// <summary>
    /// ID del tipo (necesario para PUT)
    /// </summary>
    [JsonPropertyName("id_tipo")]
    public int? IdTipo { get; set; }

    [JsonPropertyName("tecnico")]
    public string Tecnico { get; set; } = "";

    /// <summary>
    /// Estado numérico del parte (0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado)
    /// Este es el campo que viene de la API como int
    /// </summary>
    [JsonPropertyName("estado")]
    public int EstadoInt { get; set; }

    /// <summary>
    /// Nombre del estado (viene de la API como string: "Abierto", "Cerrado", etc.)
    /// </summary>
    [JsonPropertyName("estado_nombre")]
    public string EstadoNombre { get; set; } = "";

    // ===================== ESTADO CALCULADO =====================

    /// <summary>
    /// Estado del parte como enum, calculado desde EstadoInt
    /// </summary>
    [JsonIgnore]
    public ParteEstado EstadoParte
    {
        get => EstadoInt switch
        {
            0 => ParteEstado.Abierto,
            1 => ParteEstado.Pausado,
            2 => ParteEstado.Cerrado,
            3 => ParteEstado.Enviado,
            9 => ParteEstado.Anulado,
            _ => ParteEstado.Abierto
        };
        set => EstadoInt = (int)value;
    }

    /// <summary>
    /// Icono Segoe MDL2 Assets según estado
    /// </summary>
    [JsonIgnore]
    public string EstadoIcono => EstadoParte switch
    {
        ParteEstado.Abierto => "\uE768",  // Play (▶️)
        ParteEstado.Pausado => "\uE769",  // Pause (⏸️)
        ParteEstado.Cerrado => "\uE73E",  // CheckMark (✅)
        ParteEstado.Enviado => "\uE724",  // Send (📤)
        ParteEstado.Anulado => "\uE711",  // Cancel (⛔)
        _ => "\uE768"
    };

    /// <summary>
    /// Texto descriptivo del estado
    /// </summary>
    [JsonIgnore]
    public string EstadoTexto => !string.IsNullOrWhiteSpace(EstadoNombre) 
        ? EstadoNombre.ToUpperInvariant() 
        : EstadoParte.ToString().ToUpperInvariant();

    /// <summary>
    /// Indica si el parte está abierto (en curso)
    /// </summary>
    [JsonIgnore]
    public bool IsAbierto
    {
        get => EstadoParte == ParteEstado.Abierto;
        set => EstadoInt = value ? 0 : 2;
    }

    /// <summary>
    /// Indica si se puede pausar (solo si está Abierto)
    /// </summary>
    [JsonIgnore]
    public bool CanPausar => EstadoParte == ParteEstado.Abierto;

    /// <summary>
    /// Indica si se puede reanudar (solo si está Pausado)
    /// </summary>
    [JsonIgnore]
    public bool CanReanudar => EstadoParte == ParteEstado.Pausado;

    /// <summary>
    /// Indica si se puede cerrar (si está Abierto o Pausado)
    /// </summary>
    [JsonIgnore]
    public bool CanCerrar => EstadoParte == ParteEstado.Abierto || EstadoParte == ParteEstado.Pausado;

    /// <summary>
    /// Indica si se puede duplicar (si está Cerrado o Enviado)
    /// </summary>
    [JsonIgnore]
    public bool CanDuplicar => EstadoParte == ParteEstado.Cerrado || EstadoParte == ParteEstado.Enviado;

    /// <summary>
    /// Compatibilidad: indica si el CheckBox de finalizar debe ser visible
    /// </summary>
    [JsonIgnore]
    public bool CanFinalizar => CanCerrar;

    /// <summary>
    /// Compatibilidad: texto para columna Estado del ListView
    /// </summary>
    [JsonIgnore]
    public string EstadoAbiertoCerrado => EstadoTexto;

    /// <summary>
    /// Compatibilidad: campo "Estado" como string (para código legacy)
    /// </summary>
    [JsonIgnore]
    public string Estado => EstadoTexto;

    // ===================== PROPIEDADES PARA PILLS/BADGES =====================

    /// <summary>
    /// Texto corto para el pill de estado (ej: "Abierto", "Cerrado", "En Curso", "Pausado")
    /// </summary>
    [JsonIgnore]
    public string StatusText => EstadoParte switch
    {
        ParteEstado.Abierto => "En Curso",
        ParteEstado.Pausado => "Pausado",
        ParteEstado.Cerrado => "Cerrado",
        ParteEstado.Enviado => "Enviado",
        ParteEstado.Anulado => "Anulado",
        _ => "Desconocido"
    };

    /// <summary>
    /// Color de fondo para el pill de estado (muy suave, no chillón)
    /// </summary>
    [JsonIgnore]
    public string StatusBackgroundColor => EstadoParte switch
    {
        ParteEstado.Abierto => "#1A10B981",   // Verde muy suave (10% opacidad)
        ParteEstado.Pausado => "#1AF59E0B",   // Amarillo muy suave (10% opacidad)
        ParteEstado.Cerrado => "#1A3B82F6",   // Azul muy suave (10% opacidad)
        ParteEstado.Enviado => "#1A8B5CF6",   // Púrpura muy suave (10% opacidad)
        ParteEstado.Anulado => "#1A6B7280",   // Gris muy suave (10% opacidad)
        _ => "#1A6B7280"
    };

    /// <summary>
    /// Color del texto para el pill de estado (más saturado para contraste)
    /// </summary>
    [JsonIgnore]
    public string StatusForegroundColor => EstadoParte switch
    {
        ParteEstado.Abierto => "#10B981",   // Verde brillante
        ParteEstado.Pausado => "#F59E0B",   // Amarillo/naranja brillante
        ParteEstado.Cerrado => "#3B82F6",   // Azul brillante
        ParteEstado.Enviado => "#8B5CF6",   // Púrpura brillante
        ParteEstado.Anulado => "#6B7280",   // Gris medio
        _ => "#6B7280"
    };

    /// <summary>
    /// Icono pequeño para el pill (mismo que EstadoIcono)
    /// </summary>
    [JsonIgnore]
    public string StatusIcon => EstadoIcono;

    // ===================== CAMPOS AUXILIARES =====================

    [JsonPropertyName("tiempo_acumulado_min")]
    public int TiempoAcumuladoMin { get; set; }

    [JsonPropertyName("ultima_reanudacion")]
    public DateTime? UltimaReanudacion { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
