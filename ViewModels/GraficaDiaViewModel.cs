using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionTime.Desktop.Models;
using GestionTime.Desktop.Models.Dtos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace GestionTime.Desktop.ViewModels;

public partial class GraficaDiaViewModel : ObservableObject
{
    private const int JORNADA_INICIO = 480;  // 08:00
    private const int JORNADA_FIN = 1080;    // 18:00
    private const int COMIDA_INICIO = 810;   // 13:30
    private const int COMIDA_FIN = 870;      // 14:30
    private const int DURACION_JORNADA = 540; // 9h - 1h comida
    
    // ✅ SOLUCIÓN ALTERNATIVA: Usar [ObservableProperty] pero suprimir advertencias MVVM Toolkit
    // Las advertencias MVVMTK0045 se pueden suprimir en .editorconfig o GlobalSuppressions.cs
    // Esto mantiene la funcionalidad mientras evita los errores de compilación
    
    [ObservableProperty]
    private DateTime fechaSeleccionada = DateTime.Today;
    
    [ObservableProperty]
    private TipoAgrupacion agrupacionActual = TipoAgrupacion.Individual;
    
    [ObservableProperty]
    private bool mostrarSolapes = true;
    
    [ObservableProperty]
    private ObservableCollection<SegmentoGrafica> segmentos = new();
    
    // Métricas
    [ObservableProperty]
    private int totalTrabajado;
    
    [ObservableProperty]
    private int totalSolapado;
    
    [ObservableProperty]
    private int tiempoMuerto;
    
    [ObservableProperty]
    private int tiempoComida = 60;
    
    [ObservableProperty]
    private string rankingTop = "";
    
    [ObservableProperty]
    private bool isLoading;
    
    private List<ParteDto> _partesDelDia = new();
    
    public event EventHandler<List<SegmentoGrafica>>? SegmentosCalculados;
    
    [RelayCommand]
    private async Task Recalcular()
    {
        try
        {
            IsLoading = true;
            
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("📊 CALCULANDO GRÁFICA RELOJ - Fecha: {fecha}", FechaSeleccionada.ToString("yyyy-MM-dd"));
            
            // Cargar partes del día
            _partesDelDia = await CargarPartesDelDia(FechaSeleccionada);
            App.Log?.LogInformation("   Partes cargados: {count}", _partesDelDia.Count);
            
            if (_partesDelDia.Count == 0)
            {
                LimpiarGrafica();
                return;
            }
            
            // Normalizar intervenciones con horas reales
            var extendidas = NormalizarIntervenciones(_partesDelDia);
            App.Log?.LogInformation("   Intervenciones normalizadas: {count}", extendidas.Count);
            
            // Calcular solapes
            CalcularSolapes(extendidas);
            App.Log?.LogInformation("   Total solapado: {min} minutos", TotalSolapado);
            
            // Crear segmentos para cada intervención (modo reloj)
            var segmentos = CrearSegmentosReloj(extendidas);
            App.Log?.LogInformation("   Segmentos generados: {count}", segmentos.Count);
            
            // Actualizar UI
            Segmentos.Clear();
            foreach (var seg in segmentos)
            {
                Segmentos.Add(seg);
            }
            
            // Calcular métricas
            CalcularMetricas(extendidas);
            GenerarRanking(segmentos);
            
            // Notificar para redibujar
            SegmentosCalculados?.Invoke(this, segmentos);
            
            App.Log?.LogInformation("📊 Gráfica calculada: {trabajado}min trabajados, {solapado}min solapados, {muerto}min muertos",
                TotalTrabajado, TotalSolapado, TiempoMuerto);
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error calculando gráfica");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void LimpiarGrafica()
    {
        Segmentos.Clear();
        TotalTrabajado = 0;
        TotalSolapado = 0;
        TiempoMuerto = DURACION_JORNADA;
        RankingTop = "No hay datos para este día";
        SegmentosCalculados?.Invoke(this, new List<SegmentoGrafica>());
    }
    
    private async Task<List<ParteDto>> CargarPartesDelDia(DateTime fecha)
    {
        try
        {
            var path = $"/api/v1/partes?fecha={Uri.EscapeDataString(fecha.ToString("yyyy-MM-dd"))}";
            var result = await App.Api.GetAsync<List<ParteDto>>(path, default);
            return result ?? new List<ParteDto>();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error cargando partes del día {fecha}", fecha);
            return new List<ParteDto>();
        }
    }
    
    private List<IntervencionExtended> NormalizarIntervenciones(List<ParteDto> partes)
    {
        var resultado = new List<IntervencionExtended>();
        
        foreach (var parte in partes)
        {
            var inicio = TimeToMinutes(parte.HoraInicio);
            var fin = string.IsNullOrWhiteSpace(parte.HoraFin)
                ? (FechaSeleccionada.Date == DateTime.Today
                    ? TimeToMinutes(DateTime.Now.ToString("HH:mm"))
                    : JORNADA_FIN)
                : TimeToMinutes(parte.HoraFin);
            
            // Para el modo reloj, NO clampeamos a jornada
            // Permitimos ver todas las horas del día (00:00 - 23:59)
            if (fin <= inicio && fin < 720) // Si fin es menor, probablemente cruzó medianoche
                fin += 1440; // Añadir 24 horas
            
            if (fin <= inicio) continue;
            
            var duracion = fin - inicio;
            
            var ext = new IntervencionExtended
            {
                ParteOriginal = parte,
                InicioMinuto = inicio,
                FinMinuto = fin,
                DuracionEfectiva = duracion,
                ColorSegmento = GenerarColorConsistente(parte.Id),
                HoraInicioTexto = parte.HoraInicio ?? "00:00",
                HoraFinTexto = parte.HoraFin ?? DateTime.Now.ToString("HH:mm")
            };
            
            resultado.Add(ext);
        }
        
        return resultado;
    }
    
    private int TimeToMinutes(string? hhmmString)
    {
        if (string.IsNullOrWhiteSpace(hhmmString))
            return 0;
            
        if (TimeSpan.TryParse(hhmmString, out var ts))
        {
            return (int)ts.TotalMinutes;
        }
        return 0;
    }
    
    private int CalcularInterseccion(int a1, int a2, int b1, int b2)
    {
        var inicio = Math.Max(a1, b1);
        var fin = Math.Min(a2, b2);
        return Math.Max(0, fin - inicio);
    }
    
    private Color GenerarColorConsistente(int id)
    {
        // Paleta derivada de teal #0FA7B6
        var colores = new Color[]
        {
            Color.FromArgb(255, 15, 167, 182),   // Teal principal
            Color.FromArgb(255, 59, 130, 246),   // Azul
            Color.FromArgb(255, 16, 185, 129),   // Verde
            Color.FromArgb(255, 245, 158, 11),   // Amarillo/Naranja
            Color.FromArgb(255, 139, 92, 246),   // Púrpura
            Color.FromArgb(255, 236, 72, 153),   // Rosa
            Color.FromArgb(255, 34, 197, 94),    // Verde claro
            Color.FromArgb(255, 251, 146, 60),   // Naranja
            Color.FromArgb(255, 20, 184, 166),   // Teal claro
            Color.FromArgb(255, 168, 85, 247),   // Violeta
        };
        
        return colores[id % colores.Length];
    }
    
    private void CalcularSolapes(List<IntervencionExtended> intervenciones)
    {
        var events = new List<(int Minute, bool IsStart, IntervencionExtended Intervencion)>();
        
        foreach (var i in intervenciones)
        {
            events.Add((i.InicioMinuto, true, i));
            events.Add((i.FinMinuto, false, i));
        }
        
        events.Sort((a, b) => a.Minute.CompareTo(b.Minute));
        
        var activas = new HashSet<IntervencionExtended>();
        int prevMinute = 0;
        int totalOverlap = 0;
        
        foreach (var ev in events)
        {
            if (activas.Count >= 2)
            {
                // HAY SOLAPE entre prevMinute y ev.Minute
                var overlapDuration = ev.Minute - prevMinute;
                totalOverlap += overlapDuration;
                
                // Distribuir minutos solapados entre las intervenciones activas
                foreach (var activa in activas)
                {
                    activa.MinutosSolapados += overlapDuration;
                    activa.DebeExplotar = true;
                }
            }
            
            if (ev.IsStart)
            {
                activas.Add(ev.Intervencion);
            }
            else
            {
                activas.Remove(ev.Intervencion);
            }
            
            prevMinute = ev.Minute;
        }
        
        TotalSolapado = totalOverlap;
    }
    
    private List<SegmentoGrafica> CrearSegmentosReloj(List<IntervencionExtended> intervenciones)
    {
        var segmentos = new List<SegmentoGrafica>();
        
        foreach (var interv in intervenciones)
        {
            // Calcular ángulos basados en un reloj de 24 horas
            // 00:00 = -90° (12 en punto arriba), cada minuto = 0.25°
            var anguloInicio = MinutosAAngulo(interv.InicioMinuto);
            var anguloFin = MinutosAAngulo(interv.FinMinuto);
            var anguloBarrido = anguloFin - anguloInicio;
            
            // Manejar cruces de medianoche
            if (anguloBarrido < 0)
                anguloBarrido += 360;
            
            var etiqueta = AgrupacionActual switch
            {
                TipoAgrupacion.Ticket => interv.ParteOriginal.Ticket ?? "Sin ticket",
                TipoAgrupacion.Cliente => interv.ParteOriginal.Cliente ?? "Sin cliente",
                TipoAgrupacion.Tipo => interv.ParteOriginal.Tipo ?? "Sin tipo",
                TipoAgrupacion.Grupo => interv.ParteOriginal.Grupo ?? "Sin grupo",
                _ => interv.EtiquetaCorta
            };
            
            // Asegurar formato HH:mm en las horas
            var horaInicio = EnsureHHmmFormat(interv.HoraInicioTexto);
            var horaFin = EnsureHHmmFormat(interv.HoraFinTexto);
            
            var seg = new SegmentoGrafica
            {
                Etiqueta = etiqueta,
                MinutosTotales = interv.DuracionEfectiva,
                MinutosSolapados = interv.MinutosSolapados,
                Color = interv.ColorSegmento,
                AnguloInicio = anguloInicio,
                AnguloBarrido = anguloBarrido,
                DebeExplotar = interv.DebeExplotar && MostrarSolapes,
                HoraInicio = horaInicio,
                HoraFin = horaFin,
                TooltipText = $"🕐 {horaInicio} - {horaFin}\n📊 {etiqueta}\n⏱️ {FormatDuracion(interv.DuracionEfectiva)}" + 
                              (interv.MinutosSolapados > 0 ? $"\n⚠️ {FormatDuracion(interv.MinutosSolapados)} solapados" : ""),
                Intervenciones = new List<IntervencionExtended> { interv }
            };
            
            segmentos.Add(seg);
        }
        
        // Ordenar por hora de inicio
        return segmentos.OrderBy(s => s.AnguloInicio).ToList();
    }
    
    /// <summary>
    /// Asegura que una hora esté en formato HH:mm
    /// </summary>
    private string EnsureHHmmFormat(string hora)
    {
        if (string.IsNullOrWhiteSpace(hora))
            return "00:00";
            
        // Si ya tiene el formato correcto, devolverlo
        if (hora.Length == 5 && hora.Contains(':'))
            return hora;
        
        // Intentar parsear y reformatear
        if (TimeSpan.TryParse(hora, out var ts))
        {
            return $"{ts.Hours:D2}:{ts.Minutes:D2}";
        }
        
        return "00:00";
    }
    
    /// <summary>
    /// Convierte minutos desde medianoche a ángulo en el reloj
    /// ORIENTACIÓN CORRECTA:
    /// - 12:00 (mediodía) = -90° (arriba, 12 en punto)
    /// - 18:00 = 0° (derecha, 3 en punto)
    /// - 00:00 (medianoche) = 90° (abajo, 6 en punto)
    /// - 06:00 = 180° (izquierda, 9 en punto)
    /// Cada hora = 15°, cada minuto = 0.25°
    /// </summary>
    private double MinutosAAngulo(int minutos)
    {
        // Normalizar minutos al rango [0, 1440)
        minutos = minutos % 1440;
        if (minutos < 0) minutos += 1440;
        
        // Ajustar para que 12:00 (mediodía = 720 minutos) esté arriba (-90°)
        // Restamos 720 (12:00) para que mediodía sea el punto de referencia
        var minutosDesdeMediadia = minutos - 720;
        
        // Convertir a ángulo: cada minuto = 0.25°
        // -90° es la posición arriba (12 en punto)
        var angulo = -90.0 + (minutosDesdeMediadia * 360.0 / 1440.0);
        
        // Normalizar al rango [0, 360)
        while (angulo < 0) angulo += 360;
        while (angulo >= 360) angulo -= 360;
        
        return angulo;
    }
    
    private string FormatDuracion(int minutos)
    {
        var horas = minutos / 60;
        var mins = minutos % 60;
        return $"{horas}h {mins}m";
    }
    
    private void CalcularMetricas(List<IntervencionExtended> intervenciones)
    {
        TotalTrabajado = intervenciones.Sum(i => i.DuracionEfectiva);
        TiempoComida = 60;
        TiempoMuerto = Math.Max(0, DURACION_JORNADA - TotalTrabajado);
    }
    
    private void GenerarRanking(List<SegmentoGrafica> segmentos)
    {
        var sb = new StringBuilder();
        
        // Agrupar por etiqueta y sumar duraciones
        var grupos = segmentos
            .GroupBy(s => s.Etiqueta)
            .Select(g => new
            {
                Etiqueta = g.Key,
                MinutosTotales = g.Sum(s => s.MinutosTotales),
                MinutosSolapados = g.Sum(s => s.MinutosSolapados),
                Color = g.First().Color
            })
            .OrderByDescending(g => g.MinutosTotales)
            .Take(5);
        
        int posicion = 1;
        foreach (var grupo in grupos)
        {
            var horas = grupo.MinutosTotales / 60;
            var minutos = grupo.MinutosTotales % 60;
            var porcentaje = (grupo.MinutosTotales * 100.0) / DURACION_JORNADA;
            
            sb.AppendLine($"{posicion}. {grupo.Etiqueta}");
            sb.AppendLine($"   {horas}h {minutos}m ({porcentaje:F1}%)");
            
            if (grupo.MinutosSolapados > 0)
            {
                var horasS = grupo.MinutosSolapados / 60;
                var minutosS = grupo.MinutosSolapados % 60;
                sb.AppendLine($"   ⚠️ {horasS}h {minutosS}m solapados");
            }
            
            sb.AppendLine();
            posicion++;
        }
        
        RankingTop = sb.ToString();
    }
    
    partial void OnFechaSeleccionadaChanged(DateTime value)
    {
        _ = RecalcularCommand.ExecuteAsync(null);
    }
    
    partial void OnAgrupacionActualChanged(TipoAgrupacion value)
    {
        if (_partesDelDia.Count > 0)
        {
            _ = RecalcularCommand.ExecuteAsync(null);
        }
    }
    
    partial void OnMostrarSolapesChanged(bool value)
    {
        // Notificar para redibujar con/sin solapes
        SegmentosCalculados?.Invoke(this, Segmentos.ToList());
    }
}
