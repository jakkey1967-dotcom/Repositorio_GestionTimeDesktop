using System.Collections.Generic;
using Windows.UI;

namespace GestionTime.Desktop.Models;

/// <summary>
/// Representa un segmento de la gráfica donut agrupado según criterio
/// </summary>
public class SegmentoGrafica
{
    public string Etiqueta { get; set; } = string.Empty;
    public int MinutosTotales { get; set; }
    public int MinutosSolapados { get; set; }
    public Color Color { get; set; }
    public double AnguloInicio { get; set; }
    public double AnguloBarrido { get; set; } // Sweep angle
    public List<IntervencionExtended> Intervenciones { get; set; } = new();
    
    // Propiedades para modo reloj
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
    public string TooltipText { get; set; } = string.Empty;
    
    public bool DebeExplotar { get; set; }
    public double PorcentajeDelDia => MinutosTotales > 0 ? (MinutosTotales / 540.0) * 100 : 0;
}
