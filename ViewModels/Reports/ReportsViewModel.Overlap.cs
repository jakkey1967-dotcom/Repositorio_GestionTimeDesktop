using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Models.Enums;
using GestionTime.Desktop.Services.Catalog;
using Microsoft.UI.Xaml;

namespace GestionTime.Desktop.ViewModels.Reports;

// GT-BEGIN: Modelo de parte con solapamiento para edición inline
/// <summary>Item editable que representa un parte con info de solapamiento.</summary>
public partial class OverlapPartItem : ObservableObject
{
    public int Id { get; set; }
    public string Fecha { get; set; } = string.Empty;
    public string FechaTrabajo { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Ticket { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;

    private string _horaInicio = string.Empty;
    /// <summary>Hora inicio con auto-formato HH:mm.</summary>
    public string HoraInicio
    {
        get => _horaInicio;
        set => SetProperty(ref _horaInicio, AutoFormatTime(value));
    }

    private string _horaFin = string.Empty;
    /// <summary>Hora fin con auto-formato HH:mm.</summary>
    public string HoraFin
    {
        get => _horaFin;
        set => SetProperty(ref _horaFin, AutoFormatTime(value));
    }

    public string DuracionText { get; set; } = string.Empty;

    /// <summary>Auto-formatea 4 dígitos consecutivos a HH:mm.</summary>
    private static string AutoFormatTime(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        if (input.Contains(':') && input.Length <= 5) return input;
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 4)
            return $"{digits[..2]}:{digits[2..]}";
        return input;
    }

    /// <summary>Indica si este parte se solapa con otro.</summary>
    [ObservableProperty] private bool _isOverlapping;

    /// <summary>Texto descriptivo del solape (ej: "Solapa con parte #45").</summary>
    [ObservableProperty] private string _overlapInfo = string.Empty;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _validationError;

    /// <summary>Valores originales para cancelar edición.</summary>
    public string OriginalHoraInicio { get; set; } = string.Empty;
    public string OriginalHoraFin { get; set; } = string.Empty;

    // Campos necesarios para ParteUpdateRequest
    public int IdCliente { get; set; }
    public int? IdGrupo { get; set; }
    public int? IdTipo { get; set; }
    public string? Tienda { get; set; }
    public string? Tecnico { get; set; }
    public int? DuracionMin { get; set; }
    public int EstadoInt { get; set; }

    partial void OnIsEditingChanged(bool value)
    {
        OnPropertyChanged(nameof(EditingVisibility));
        OnPropertyChanged(nameof(DisplayVisibility));
    }

    partial void OnIsOverlappingChanged(bool value)
    {
        OnPropertyChanged(nameof(OverlapBadgeVisibility));
    }

    public Visibility EditingVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DisplayVisibility => IsEditing ? Visibility.Collapsed : Visibility.Visible;
    public Visibility OverlapBadgeVisibility => IsOverlapping ? Visibility.Visible : Visibility.Collapsed;
}
// GT-END

// GT-BEGIN: Lógica de solapamiento en partial class
/// <summary>Extensión de ReportsViewModel para detección y edición de solapamientos.</summary>
public partial class ReportsViewModel
{
    private PartesService? _partesService;
    private List<ParteDto>? _allPartesForOverlap;

    /// <summary>Obtiene o crea el servicio de partes (lazy).</summary>
    private PartesService GetPartesService()
    {
        return _partesService ??= new PartesService(App.Api, App.Log!);
    }

    private ObservableCollection<OverlapPartItem> _overlappingParts = new();
    /// <summary>Lista de partes con detalle de solapamiento.</summary>
    public ObservableCollection<OverlapPartItem> OverlappingParts
    {
        get => _overlappingParts;
        set => SetProperty(ref _overlappingParts, value);
    }

    [ObservableProperty] private bool _showOverlapDetail;
    [ObservableProperty] private bool _isLoadingOverlap;

    partial void OnShowOverlapDetailChanged(bool value)
    {
        OnPropertyChanged(nameof(OverlapDetailVisibility));
    }

    partial void OnIsLoadingOverlapChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLoadingOverlapVisibility));
    }

    public Visibility OverlapDetailVisibility => ShowOverlapDetail ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsLoadingOverlapVisibility => IsLoadingOverlap ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Carga los partes del periodo y detecta solapamientos.</summary>
    public async Task LoadOverlappingPartsAsync()
    {
        if (Resumen == null || Resumen.OverlapMinutes <= 0)
        {
            DispatchUI(() =>
            {
                OverlappingParts.Clear();
                ShowOverlapDetail = false;
            });
            return;
        }

        DispatchUI(() => IsLoadingOverlap = true);

        try
        {
            var svc = GetPartesService();
            List<ParteDto>? partes = null;

            // Obtener partes según el scope actual
            if (Scope == "day")
            {
                var fecha = SelectedDate.DateTime;
                partes = await svc.ListAsync(fecha: fecha, ct: _cts?.Token ?? CancellationToken.None);
            }
            else if (Scope == "week" && !string.IsNullOrWhiteSpace(WeekIso))
            {
                var parts = WeekIso.Split('-');
                if (parts.Length == 2 && parts[1].StartsWith("W"))
                {
                    var year = int.Parse(parts[0]);
                    var weekNum = int.Parse(parts[1].Substring(1));
                    var monday = System.Globalization.ISOWeek.ToDateTime(year, weekNum, DayOfWeek.Monday);
                    var saturday = monday.AddDays(5);
                    partes = await svc.ListAsync(fechaInicio: monday, fechaFin: saturday, ct: _cts?.Token ?? CancellationToken.None);
                }
            }
            else if (Scope == "range" && RangeFrom.HasValue && RangeTo.HasValue)
            {
                partes = await svc.ListAsync(
                    fechaInicio: RangeFrom.Value.DateTime,
                    fechaFin: RangeTo.Value.DateTime,
                    ct: _cts?.Token ?? CancellationToken.None);
            }

            if (partes == null || partes.Count == 0)
            {
                DispatchUI(() =>
                {
                    OverlappingParts.Clear();
                    ShowOverlapDetail = false;
                });
                return;
            }

            // Guardar referencia a todos los partes (para auto-fix)
            _allPartesForOverlap = partes;

            // Detectar solapamientos localmente
            var items = BuildOverlapItems(partes);

            DispatchUI(() =>
            {
                OverlappingParts.Clear();
                foreach (var item in items)
                    OverlappingParts.Add(item);
                ShowOverlapDetail = items.Count > 0;
            });
        }
        catch (OperationCanceledException) { /* ignorar */ }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Overlap] ERROR: {ex.Message}");
            DispatchUI(() => ShowOverlapDetail = false);
        }
        finally
        {
            DispatchUI(() => IsLoadingOverlap = false);
        }
    }

    /// <summary>Detecta solapamientos entre partes y devuelve solo los que se solapan.</summary>
    private static List<OverlapPartItem> BuildOverlapItems(List<ParteDto> partes)
    {
        // Agrupar por fecha para detectar solapamientos dentro del mismo día
        var overlapIds = new HashSet<int>();
        var overlapInfoMap = new Dictionary<int, List<string>>();

        var byDate = partes
            .Where(p => !string.IsNullOrWhiteSpace(p.HoraInicio) && !string.IsNullOrWhiteSpace(p.HoraFin))
            .GroupBy(p => p.Fecha.Date);

        foreach (var group in byDate)
        {
            var sorted = group.OrderBy(p => ParseTime(p.HoraInicio)).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                for (int j = i + 1; j < sorted.Count; j++)
                {
                    var a = sorted[i];
                    var b = sorted[j];
                    var aEnd = ParseTime(a.HoraFin);
                    var bStart = ParseTime(b.HoraInicio);

                    if (aEnd > bStart)
                    {
                        // Solapamiento detectado
                        overlapIds.Add(a.Id);
                        overlapIds.Add(b.Id);

                        if (!overlapInfoMap.ContainsKey(a.Id))
                            overlapInfoMap[a.Id] = new List<string>();
                        overlapInfoMap[a.Id].Add($"#{b.Id} ({b.Cliente})");

                        if (!overlapInfoMap.ContainsKey(b.Id))
                            overlapInfoMap[b.Id] = new List<string>();
                        overlapInfoMap[b.Id].Add($"#{a.Id} ({a.Cliente})");
                    }
                }
            }
        }

        // Construir items solo para partes que solapan
        return partes
            .Where(p => overlapIds.Contains(p.Id))
            .OrderBy(p => p.Fecha).ThenBy(p => ParseTime(p.HoraInicio))
            .Select(p => new OverlapPartItem
            {
                Id = p.Id,
                Fecha = p.Fecha == default ? "" : p.Fecha.ToString("dd/MM"),
                FechaTrabajo = p.Fecha == default ? "" : p.Fecha.ToString("yyyy-MM-dd"),
                Cliente = p.Cliente,
                Ticket = string.IsNullOrWhiteSpace(p.Ticket) ? "-" : p.Ticket,
                Accion = string.IsNullOrWhiteSpace(p.Accion) ? "-" : TruncateText(p.Accion, 30),
                HoraInicio = p.HoraInicio,
                HoraFin = p.HoraFin,
                OriginalHoraInicio = p.HoraInicio,
                OriginalHoraFin = p.HoraFin,
                DuracionText = p.DuracionText,
                IsOverlapping = true,
                OverlapInfo = overlapInfoMap.ContainsKey(p.Id)
                    ? $"Solapa con: {string.Join(", ", overlapInfoMap[p.Id])}"
                    : string.Empty,
                IdCliente = p.IdCliente,
                IdGrupo = p.IdGrupo,
                IdTipo = p.IdTipo,
                Tienda = p.Tienda,
                Tecnico = p.Tecnico,
                DuracionMin = p.DuracionMin,
                EstadoInt = p.EstadoInt
            })
            .ToList();
    }

    /// <summary>Inicia edición de un parte (muestra campos editables).</summary>
    public void BeginEditPart(OverlapPartItem item)
    {
        // Cancelar cualquier otra edición activa
        foreach (var p in OverlappingParts)
        {
            if (p != item && p.IsEditing)
            {
                p.HoraInicio = p.OriginalHoraInicio;
                p.HoraFin = p.OriginalHoraFin;
                p.IsEditing = false;
                p.ValidationError = null;
            }
        }

        item.IsEditing = true;
        item.ValidationError = null;
    }

    /// <summary>Cancela edición y restaura valores originales.</summary>
    public void CancelEditPart(OverlapPartItem item)
    {
        item.HoraInicio = item.OriginalHoraInicio;
        item.HoraFin = item.OriginalHoraFin;
        item.IsEditing = false;
        item.ValidationError = null;
    }

    /// <summary>Valida y guarda cambios de tiempo en un parte.</summary>
    public async Task SavePartTimeChangeAsync(OverlapPartItem item)
    {
        item.ValidationError = null;

        // Validar formato HH:mm
        var newStart = ParseTime(item.HoraInicio);
        var newEnd = ParseTime(item.HoraFin);

        if (newStart == TimeSpan.MinValue)
        {
            item.ValidationError = "Hora inicio no válida (HH:mm)";
            return;
        }
        if (newEnd == TimeSpan.MinValue)
        {
            item.ValidationError = "Hora fin no válida (HH:mm)";
            return;
        }
        if (newStart >= newEnd)
        {
            item.ValidationError = "Inicio debe ser anterior a Fin";
            return;
        }

        // Validar que no solapa con otros partes de la misma fecha
        var sameDateParts = OverlappingParts
            .Where(p => p.FechaTrabajo == item.FechaTrabajo && p.Id != item.Id)
            .ToList();

        // También necesitamos verificar contra partes NO mostrados (los que no solapan)
        // Para eso recargamos todos los partes del día
        try
        {
            item.IsSaving = true;
            var svc = GetPartesService();

            DateTime fecha;
            if (!DateTime.TryParse(item.FechaTrabajo, out fecha))
            {
                item.ValidationError = "Fecha no válida";
                return;
            }

            var allPartes = await svc.ListAsync(fecha: fecha, ct: _cts?.Token ?? CancellationToken.None);
            if (allPartes != null)
            {
                foreach (var other in allPartes.Where(p => p.Id != item.Id
                    && !string.IsNullOrWhiteSpace(p.HoraInicio) && !string.IsNullOrWhiteSpace(p.HoraFin)))
                {
                    var otherStart = ParseTime(other.HoraInicio);
                    var otherEnd = ParseTime(other.HoraFin);

                    if (newStart < otherEnd && newEnd > otherStart)
                    {
                        item.ValidationError = $"Solapa con #{other.Id} ({other.Cliente}: {other.HoraInicio}-{other.HoraFin})";
                        return;
                    }
                }
            }

            // Calcular nueva duración
            var newDuration = (int)(newEnd - newStart).TotalMinutes;

            // Guardar con UpdateAsync
            var request = new ParteUpdateRequest
            {
                FechaTrabajo = item.FechaTrabajo,
                HoraInicio = item.HoraInicio,
                HoraFin = item.HoraFin,
                DuracionMin = newDuration,
                IdCliente = item.IdCliente,
                Tienda = item.Tienda,
                IdGrupo = item.IdGrupo,
                IdTipo = item.IdTipo,
                Accion = item.Accion == "-" ? "" : item.Accion,
                Ticket = item.Ticket == "-" ? null : item.Ticket,
                Tecnico = item.Tecnico
            };

            var updated = await svc.UpdateAsync(item.Id, request, _cts?.Token ?? CancellationToken.None);

            if (updated != null)
            {
                // Actualizar item local
                item.OriginalHoraInicio = item.HoraInicio;
                item.OriginalHoraFin = item.HoraFin;
                item.DuracionMin = newDuration;
                item.IsEditing = false;

                // Invalidar caché de partes e informes
                App.Api.InvalidateCacheEntry("/api/v1/partes");
                App.Api.InvalidateCacheEntry("/api/v2/informes/resumen");

                // Re-buscar para actualizar totales y detectar si el solape se resolvió
                DispatchUI(() =>
                {
                    NotificationMessage = $"✅ Parte #{item.Id} actualizado ({item.HoraInicio}-{item.HoraFin})";
                    NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                    ShowNotification = true;
                });

                // Refrescar búsqueda completa (actualiza totales + gráfica + solapamientos)
                await SearchAsync();
            }
            else
            {
                item.ValidationError = "Error: el servidor no devolvió datos";
            }
        }
        catch (Exception ex)
        {
            item.ValidationError = $"Error al guardar: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[Overlap] Error guardando parte #{item.Id}: {ex}");
        }
        finally
        {
            item.IsSaving = false;
        }
    }

    /// <summary>Parsea hora en formato HH:mm a TimeSpan.</summary>
    private static TimeSpan ParseTime(string time)
    {
        if (string.IsNullOrWhiteSpace(time)) return TimeSpan.MinValue;
        var clean = time.Trim();
        if (TimeSpan.TryParse(clean, out var ts)) return ts;
        if (TimeSpan.TryParseExact(clean, @"hh\:mm", null, out ts)) return ts;
        return TimeSpan.MinValue;
    }

    // GT-BEGIN: Solución automática de solapamientos
    [ObservableProperty] private bool _isAutoFixing;

    partial void OnIsAutoFixingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAutoFixingVisibility));
    }

    public Visibility IsAutoFixingVisibility => IsAutoFixing ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Resuelve solapamientos automáticamente (prioridad por duración).</summary>
    public async Task AutoFixOverlapsAsync()
    {
        if (OverlappingParts.Count == 0) return;

        DispatchUI(() => IsAutoFixing = true);

        try
        {
            var svc = GetPartesService();

            // Agrupar solapados por fecha
            var byDate = OverlappingParts.GroupBy(p => p.FechaTrabajo).ToList();

            foreach (var group in byDate)
            {
                // Obtener TODOS los partes del día (no solo solapados)
                DateTime fecha;
                if (!DateTime.TryParse(group.Key, out fecha)) continue;

                var allPartes = _allPartesForOverlap?
                    .Where(p => p.Fecha.Date == fecha.Date
                        && !string.IsNullOrWhiteSpace(p.HoraInicio)
                        && !string.IsNullOrWhiteSpace(p.HoraFin))
                    .ToList();

                if (allPartes == null || allPartes.Count == 0) continue;

                // Rangos ocupados por partes NO solapados (no se tocan)
                var overlapIds = new HashSet<int>(group.Select(p => p.Id));
                var fixedRanges = allPartes
                    .Where(p => !overlapIds.Contains(p.Id))
                    .Select(p => (Start: ParseTime(p.HoraInicio), End: ParseTime(p.HoraFin)))
                    .Where(r => r.Start != TimeSpan.MinValue && r.End != TimeSpan.MinValue)
                    .OrderBy(r => r.Start)
                    .ToList();

                // Ordenar solapados por duración DESC (el más largo tiene prioridad)
                var overlapping = group
                    .Select(p => {
                        var s = ParseTime(p.HoraInicio);
                        var e = ParseTime(p.HoraFin);
                        var dur = (e > s) ? e - s : TimeSpan.FromMinutes(1);
                        return (Item: p, Start: s, End: e, Duration: dur);
                    })
                    .OrderByDescending(x => x.Duration)
                    .ToList();

                // Colocar cada parte sin solapar (greedy por duración)
                var occupied = new List<(TimeSpan Start, TimeSpan End)>(fixedRanges);

                foreach (var entry in overlapping)
                {
                    var start = entry.Start;
                    var end = entry.End;
                    var originalDur = entry.Duration;

                    // Intentar ajustar para evitar conflictos
                    int passes = 0;
                    bool conflict = true;
                    while (conflict && passes < 20)
                    {
                        conflict = false;
                        foreach (var r in occupied.OrderBy(x => x.Start))
                        {
                            if (start < r.End && end > r.Start)
                            {
                                // Conflicto: si nuestro inicio está dentro del rango, empujar después
                                if (start >= r.Start)
                                    start = r.End;
                                else
                                    end = r.Start;
                                conflict = true;
                            }
                        }
                        passes++;
                    }

                    // Si el rango quedó inválido, colocar después de todo lo ocupado
                    if (start >= end)
                    {
                        var lastEnd = occupied.Count > 0 ? occupied.Max(r => r.End) : entry.Start;
                        start = lastEnd;
                        end = start + originalDur;
                    }

                    entry.Item.HoraInicio = start.ToString(@"hh\:mm");
                    entry.Item.HoraFin = end.ToString(@"hh\:mm");
                    occupied.Add((start, end));
                }
            }

            // Guardar todos los cambios vía API
            int saved = 0;
            int errors = 0;

            foreach (var item in OverlappingParts)
            {
                if (item.HoraInicio == item.OriginalHoraInicio && item.HoraFin == item.OriginalHoraFin)
                    continue; // Sin cambios

                try
                {
                    var newStart = ParseTime(item.HoraInicio);
                    var newEnd = ParseTime(item.HoraFin);
                    var newDuration = (int)(newEnd - newStart).TotalMinutes;

                    var request = new ParteUpdateRequest
                    {
                        FechaTrabajo = item.FechaTrabajo,
                        HoraInicio = item.HoraInicio,
                        HoraFin = item.HoraFin,
                        DuracionMin = newDuration,
                        IdCliente = item.IdCliente,
                        Tienda = item.Tienda,
                        IdGrupo = item.IdGrupo,
                        IdTipo = item.IdTipo,
                        Accion = item.Accion == "-" ? "" : item.Accion,
                        Ticket = item.Ticket == "-" ? null : item.Ticket,
                        Tecnico = item.Tecnico
                    };

                    var updated = await svc.UpdateAsync(item.Id, request, _cts?.Token ?? CancellationToken.None);
                    if (updated != null)
                    {
                        item.OriginalHoraInicio = item.HoraInicio;
                        item.OriginalHoraFin = item.HoraFin;
                        item.DuracionMin = newDuration;
                        saved++;
                    }
                    else errors++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AutoFix] Error guardando #{item.Id}: {ex.Message}");
                    errors++;
                }
            }

            // Invalidar caché y refrescar
            App.Api.InvalidateCacheEntry("/api/v1/partes");
            App.Api.InvalidateCacheEntry("/api/v2/informes/resumen");

            DispatchUI(() =>
            {
                var msg = errors == 0
                    ? $"✅ Solapamientos resueltos ({saved} partes ajustados)"
                    : $"⚠️ {saved} ajustados, {errors} con error";
                NotificationMessage = msg;
                NotificationSeverity = errors == 0
                    ? Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success
                    : Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                ShowNotification = true;
            });

            // Refrescar búsqueda completa
            await SearchAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoFix] ERROR: {ex}");
            DispatchUI(() =>
            {
                NotificationMessage = $"❌ Error en solución automática: {ex.Message}";
                NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                ShowNotification = true;
            });
        }
        finally
        {
            DispatchUI(() => IsAutoFixing = false);
        }
    }
    // GT-END

    /// <summary>Trunca texto a máximo N caracteres.</summary>
    private static string TruncateText(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLen) return text;
        return text[..(maxLen - 1)] + "…";
    }
}
// GT-END
