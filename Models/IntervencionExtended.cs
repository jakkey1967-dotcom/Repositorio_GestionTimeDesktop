using GestionTime.Desktop.Models.Dtos;
using Windows.UI;
using System;

namespace GestionTime.Desktop.Models;

/// <summary>
/// Extensión de ParteDto con datos calculados para visualización en gráfica
/// </summary>
public class IntervencionExtended
{
    public ParteDto ParteOriginal { get; set; } = null!;
    
    // Tiempos normalizados (minutos desde medianoche)
    public int InicioMinuto { get; set; }
    public int FinMinuto { get; set; }
    
    // Métricas calculadas
    public int DuracionEfectiva { get; set; } // Excluyendo comida
    public int MinutosSolapados { get; set; }
    public bool TieneSolape => MinutosSolapados > 0;
    public bool DebeExplotar { get; set; }
    
    // Horas en formato texto
    public string HoraInicioTexto { get; set; } = string.Empty;
    public string HoraFinTexto { get; set; } = string.Empty;
    
    // Visualización
    public Color ColorSegmento { get; set; }
    public double AnguloInicio { get; set; } // Grados
    public double AnguloFin { get; set; }
    public double PorcentajeDelDia => DuracionEfectiva > 0 ? (DuracionEfectiva / 540.0) * 100 : 0;
    
    public string EtiquetaCorta
    {
        get
        {
            var cliente = ParteOriginal.Cliente ?? "Sin cliente";
            var accion = ParteOriginal.Accion ?? "";
            var maxLen = Math.Min(20, accion.Length);
            var accionCorta = accion.Length > 0 ? accion.Substring(0, maxLen) : "";
            return $"{cliente} - {accionCorta}";
        }
    }
}
