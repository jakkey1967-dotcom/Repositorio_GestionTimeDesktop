using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionTime.Desktop.Models;
using GestionTime.Desktop.Models.Dtos.Reports;
using GestionTime.Desktop.Models.Enums;
using GestionTime.Desktop.Services.Reports;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace GestionTime.Desktop.ViewModels.Reports;

/// <summary>Item de gráfica semanal (día con minutos).</summary>
public class WeekChartItem
{
    public string DayLabel { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public string HoursText { get; set; } = string.Empty;
    public double BarWidth { get; set; }
    public bool IsUnderTarget { get; set; }
    public bool IsSelectedDay { get; set; }  // ← Marca el día seleccionado (solo en scope=day)
    public int Percentage { get; set; }  // ← Porcentaje del día respecto al total de la semana
    public int Percent8h { get; set; }  // ← Porcentaje respecto a objetivo de 8h (480 min)

    public SolidColorBrush BarBrush
    {
        get
        {
            // Si es el día seleccionado (scope=day), usar azul/cian
            if (IsSelectedDay)
                return new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)); // #3B82F6 (azul)

            // Si no, verde (>=8h) o ámbar (<8h)
            return IsUnderTarget 
                ? new SolidColorBrush(Color.FromArgb(255, 245, 158, 11))  // #F59E0B (ámbar)
                : new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)); // #10B981 (verde)
        }
    }
}

/// <summary>Opción de semana para el ComboBox (formato amigable + ISO).</summary>
public class WeekOption
{
    /// <summary>Texto visible: "Semana 07 (09/02/2026 - 15/02/2026)"</summary>
    public string Display { get; set; } = string.Empty;

    /// <summary>Valor ISO para el backend: "2026-W07"</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>Opción de agente para ComboBox (selección única).</summary>
public class AgentOption
{
    /// <summary>ID del agente (GUID)</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre completo del agente</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email del agente</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Texto visible: "Nombre (email)"</summary>
    public string Display => $"{FullName} ({Email})";
}

/// <summary>ViewModel para la ventana de Informes.</summary>
public partial class ReportsViewModel : ObservableObject
{
    private readonly InformesService _informesService;
    private DispatcherQueue? _uiDispatcher;
    private CancellationTokenSource? _cts;
    private DateTime _lastSearchTime = DateTime.MinValue;
    private const int DebounceMs = 500;

    /// <summary>Ejecuta acción en UI thread (seguro desde cualquier thread).</summary>
    private void DispatchUI(Action action)
    {
        if (_uiDispatcher?.HasThreadAccess == true)
        {
            try { action(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DispatchUI] ERROR (inline): {ex}"); }
        }
        else
        {
            _uiDispatcher?.TryEnqueue(() =>
            {
                try { action(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DispatchUI] ERROR (enqueued): {ex}"); }
            });
        }
    }

    [ObservableProperty] private string _scope = "day";
    [ObservableProperty] private DateTimeOffset _selectedDate = DateTimeOffset.Now.AddDays(-1);
    [ObservableProperty] private string _weekIso = GetCurrentWeekIso();
    [ObservableProperty] private DateTimeOffset? _rangeFrom;
    [ObservableProperty] private DateTimeOffset? _rangeTo;

    partial void OnScopeChanged(string value)
    {
        OnPropertyChanged(nameof(IsDayScope));
        OnPropertyChanged(nameof(IsWeekScope));
        OnPropertyChanged(nameof(IsRangeScope));
        OnPropertyChanged(nameof(DayScopeVisibility));
        OnPropertyChanged(nameof(WeekScopeVisibility));
        OnPropertyChanged(nameof(RangeScopeVisibility));
        OnPropertyChanged(nameof(CanSearch));
        OnPropertyChanged(nameof(BannerContextText));
        OnPropertyChanged(nameof(TotalHeaderText));
    }
    // Selección única de agente para EDITOR/ADMIN (ComboBox)
    private ObservableCollection<AgentOption> _availableAgents = new();
    public ObservableCollection<AgentOption> AvailableAgents
    {
        get => _availableAgents;
        set => SetProperty(ref _availableAgents, value);
    }

    private AgentOption? _selectedAgent;
    /// <summary>Agente seleccionado en el ComboBox (solo EDITOR/ADMIN).</summary>
    public AgentOption? SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (SetProperty(ref _selectedAgent, value))
            {
                OnPropertyChanged(nameof(AgentDisplayText));
                System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] Agente seleccionado: {value?.Display ?? "(ninguno)"}");
            }
        }
    }

    [ObservableProperty] private bool _isLoadingAgents;

    /// <summary>Indica si el selector de agente debe estar habilitado (solo EDITOR/ADMIN).</summary>
    public bool IsAgentSelectorEnabled => CanSelectAgent;

    partial void OnIsLoadingAgentsChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLoadingAgentsVisibility));
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private InformeResumenDto? _resumen;
    [ObservableProperty] private UserRole _currentUserRole;
    [ObservableProperty] private bool _canSelectAgent;
    [ObservableProperty] private string? _currentUserId;  // ← ID del agente del usuario actual

    // Sistema de notificaciones temporales
    [ObservableProperty] private bool _showNotification;
    [ObservableProperty] private string _notificationMessage = string.Empty;
    [ObservableProperty] private Microsoft.UI.Xaml.Controls.InfoBarSeverity _notificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational;

    // Propiedades para gráfica semanal
    private ObservableCollection<WeekChartItem> _weekChartItems = new();
    public ObservableCollection<WeekChartItem> WeekChartItems
    {
        get => _weekChartItems;
        set => SetProperty(ref _weekChartItems, value);
    }

    [ObservableProperty] private bool _showWeekChart;
    [ObservableProperty] private string _weekChartMessage = string.Empty;
    [ObservableProperty] private string _weekTotalHours = string.Empty;  // ← Total de horas trabajadas en la semana

    public Visibility WeekChartVisibility => ShowWeekChart ? Visibility.Visible : Visibility.Collapsed;
    public Visibility WeekChartMessageVisibility => !string.IsNullOrEmpty(WeekChartMessage) ? Visibility.Visible : Visibility.Collapsed;

    // Selector de semanas (ComboBox)
    private ObservableCollection<WeekOption> _availableWeeks = new();
    public ObservableCollection<WeekOption> AvailableWeeks
    {
        get => _availableWeeks;
        set => SetProperty(ref _availableWeeks, value);
    }

    [ObservableProperty] private WeekOption? _selectedWeek;

    partial void OnSelectedWeekChanged(WeekOption? value)
    {
        if (value != null)
        {
            WeekIso = value.Value;
            OnPropertyChanged(nameof(BannerContextText));
        }
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(LoadingVisibility));
        OnPropertyChanged(nameof(CanSearch));
    }

    partial void OnShowWeekChartChanged(bool value)
    {
        OnPropertyChanged(nameof(WeekChartVisibility));
    }

    partial void OnWeekChartMessageChanged(string value)
    {
        OnPropertyChanged(nameof(WeekChartMessageVisibility));
    }

    partial void OnResumenChanged(InformeResumenDto? value)
    {
        OnPropertyChanged(nameof(HasResumen));
        OnPropertyChanged(nameof(ResumenVisibility));
        OnPropertyChanged(nameof(Is8HoursComplete));
        OnPropertyChanged(nameof(MinutesMissing));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(RecordedTime));
        OnPropertyChanged(nameof(CoveredTime));
        OnPropertyChanged(nameof(OverlapTime));
        OnPropertyChanged(nameof(FirstStartFormatted));
        OnPropertyChanged(nameof(LastEndFormatted));
        OnPropertyChanged(nameof(TotalHeaderText));
        OnPropertyChanged(nameof(AgentDisplayText));

        // NO cargar gráfica aquí - se hace explícitamente desde SearchAsync después del await
    }

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    public ReportsViewModel(InformesService informesService, UserRole userRole, string? currentUserId = null)
    {
        _informesService = informesService;
        _currentUserRole = userRole;
        _currentUserId = currentUserId;
        _canSelectAgent = userRole is UserRole.EDITOR or UserRole.ADMIN;

        // Generar lista de semanas (últimas 52)
        GenerateAvailableWeeks();

        // NO cargar agentes aquí - se hace en LoadInitialDataAsync()
    }

    /// <summary>Genera lista de las últimas 52 semanas para el ComboBox.</summary>
    private void GenerateAvailableWeeks()
    {
        var weeks = new List<WeekOption>();
        var today = DateTime.Now;

        // Generar últimas 52 semanas (1 año hacia atrás)
        for (int i = 0; i < 52; i++)
        {
            var targetDate = today.AddDays(-i * 7);
            var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(targetDate);
            var year = System.Globalization.ISOWeek.GetYear(targetDate);

            // Calcular lunes y domingo de esa semana
            var monday = System.Globalization.ISOWeek.ToDateTime(year, weekNum, DayOfWeek.Monday);
            var sunday = monday.AddDays(6);

            var weekOption = new WeekOption
            {
                Value = $"{year}-W{weekNum:D2}",
                Display = $"Semana {weekNum:D2} ({monday:dd/MM/yyyy} - {sunday:dd/MM/yyyy})"
            };

            weeks.Add(weekOption);
        }

        AvailableWeeks = new ObservableCollection<WeekOption>(weeks);

        // Seleccionar semana actual por defecto
        var currentWeekIso = GetCurrentWeekIso();
        SelectedWeek = AvailableWeeks.FirstOrDefault(w => w.Value == currentWeekIso);
    }

    /// <summary>Ejecuta búsqueda con validaciones y cancelación.</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        DispatchUI(() =>
        {
            ErrorMessage = null;
            IsLoading = true;
            Resumen = null;
        });

        try
        {
            // Validaciones (lectura de propiedades - seguro desde cualquier thread)
            if (Scope == "day" && SelectedDate == default)
            {
                DispatchUI(() => ErrorMessage = "Fecha no válida.");
                return;
            }

            if (Scope == "week" && string.IsNullOrWhiteSpace(WeekIso))
            {
                DispatchUI(() => ErrorMessage = "Semana no válida (formato: YYYY-Www).");
                return;
            }

            if (Scope == "range")
            {
                if (!RangeFrom.HasValue || !RangeTo.HasValue)
                {
                    DispatchUI(() => ErrorMessage = "Debes seleccionar fecha de inicio y fin.");
                    return;
                }

                if (RangeFrom.Value > RangeTo.Value)
                {
                    DispatchUI(() => ErrorMessage = "La fecha de inicio debe ser anterior a la fecha de fin.");
                    return;
                }
            }

            // Llamada API
            // GT-BEGIN: Determinar agentId según rol
            // USER: NO enviar agentId → el backend usa el JWT token para identificar al usuario
            // EDITOR/ADMIN: solo enviar agentId si seleccionaron un agente explícitamente en el ComboBox
            string? agentIdToSend = null;

            if (CurrentUserRole == UserRole.USER)
            {
                // No enviar agentId: el backend identifica al usuario por su JWT token
                agentIdToSend = null;
                System.Diagnostics.Debug.WriteLine($"👤 [ReportsViewModel] USER → Sin agentId (backend usa JWT)");
            }
            else
            {
                // EDITOR/ADMIN: enviar agentId solo si seleccionaron uno en el ComboBox
                if (SelectedAgent != null)
                {
                    agentIdToSend = SelectedAgent.Id;
                    System.Diagnostics.Debug.WriteLine($"👥 [ReportsViewModel] EDITOR/ADMIN → Agente seleccionado: {SelectedAgent.Display} (ID: {agentIdToSend})");
                }
                else
                {
                    // Sin selección: backend usa JWT para devolver datos del usuario autenticado
                    agentIdToSend = null;
                    System.Diagnostics.Debug.WriteLine($"👤 [ReportsViewModel] EDITOR/ADMIN (sin selección) → Sin agentId (backend usa JWT)");
                }
            }
            // GT-END

            var result = await _informesService.GetResumenAsync(
                scope: Scope,
                date: Scope == "day" ? SelectedDate.ToString("yyyy-MM-dd") : null,
                weekIso: Scope == "week" ? WeekIso : null,
                from: Scope == "range" && RangeFrom.HasValue ? RangeFrom.Value.ToString("yyyy-MM-dd") : null,
                to: Scope == "range" && RangeTo.HasValue ? RangeTo.Value.ToString("yyyy-MM-dd") : null,
                agentId: agentIdToSend,
                cancellationToken: _cts.Token);

            // Después del await: puede estar en thread background → usar dispatcher
            DispatchUI(() =>
            {
                if (result != null)
                {
                    Resumen = result;
                    // Fire-and-forget seguro: captura excepciones
                    _ = SafeLoadWeekChartAsync();
                    // GT-BEGIN: Cargar detalle de solapamientos si hay overlap
                    _ = SafeLoadOverlappingPartsAsync();
                    // GT-END
                }
                else
                {
                    ErrorMessage = "No se encontraron datos para los filtros seleccionados.";
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Búsqueda cancelada, ignorar
        }
        catch (Exception ex)
        {
            DispatchUI(() => ErrorMessage = $"Error al cargar informe: {ex.Message}");
        }
        finally
        {
            DispatchUI(() => IsLoading = false);
        }
    }

    /// <summary>Carga inicial al abrir la ventana.</summary>
    public async Task LoadInitialDataAsync()
    {
        try
        {
            // CRÍTICO: Capturar dispatcher desde UI thread (se llama desde Window constructor)
            _uiDispatcher = DispatcherQueue.GetForCurrentThread();

            // Cargar agentes disponibles si es EDITOR/ADMIN
            if (CanSelectAgent)
            {
                await LoadAvailableAgentsAsync();
            }

            // Buscar último día con partes para usarlo como fecha por defecto
            var defaultDate = await FindLastDateWithDataAsync();

            DispatchUI(() =>
            {
                Scope = "day";
                SelectedDate = defaultDate;
            });

            await SearchAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] ERROR en LoadInitialDataAsync: {ex}");
            DispatchUI(() => ErrorMessage = $"Error al inicializar informes: {ex.Message}");
        }
    }

    /// <summary>Busca el último día con partes consultando la semana actual y la anterior.</summary>
    private async Task<DateTimeOffset> FindLastDateWithDataAsync()
    {
        try
        {
            // USER: no enviar agentId (backend usa JWT). EDITOR/ADMIN: solo si hay agente seleccionado.
            string? agentId = CurrentUserRole == UserRole.USER
                ? null
                : SelectedAgent?.Id;

            // Intentar semana actual primero
            var currentWeek = GetCurrentWeekIso();
            var weekData = await _informesService.GetResumenAsync(
                scope: "week", weekIso: currentWeek, agentId: agentId,
                cancellationToken: _cts?.Token ?? CancellationToken.None);

            if (weekData?.ByDay != null && weekData.ByDay.Count > 0)
            {
                var lastDay = weekData.ByDay
                    .Where(d => d.PartsCount > 0)
                    .OrderByDescending(d => d.Date)
                    .FirstOrDefault();

                if (lastDay != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] Último día con datos: {lastDay.Date:yyyy-MM-dd} ({lastDay.PartsCount} partes)");
                    return new DateTimeOffset(lastDay.Date);
                }
            }

            // Si no hay datos esta semana, intentar semana anterior
            var prevWeekDate = DateTime.Now.AddDays(-7);
            var prevWeekNum = System.Globalization.ISOWeek.GetWeekOfYear(prevWeekDate);
            var prevYear = System.Globalization.ISOWeek.GetYear(prevWeekDate);
            var prevWeekIso = $"{prevYear}-W{prevWeekNum:D2}";

            weekData = await _informesService.GetResumenAsync(
                scope: "week", weekIso: prevWeekIso, agentId: agentId,
                cancellationToken: _cts?.Token ?? CancellationToken.None);

            if (weekData?.ByDay != null && weekData.ByDay.Count > 0)
            {
                var lastDay = weekData.ByDay
                    .Where(d => d.PartsCount > 0)
                    .OrderByDescending(d => d.Date)
                    .FirstOrDefault();

                if (lastDay != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] Último día con datos (semana anterior): {lastDay.Date:yyyy-MM-dd}");
                    return new DateTimeOffset(lastDay.Date);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] Error buscando último día con datos: {ex.Message}");
        }

        // Fallback: ayer
        System.Diagnostics.Debug.WriteLine("[ReportsViewModel] No se encontró día con datos, usando ayer como fallback");
        return DateTimeOffset.Now.AddDays(-1);
    }

    /// <summary>Cancela búsqueda actual.</summary>
    public void CancelSearch()
    {
        _cts?.Cancel();
    }

    /// <summary>Calcula semana ISO actual (YYYY-Www).</summary>
    private static string GetCurrentWeekIso()
    {
        var today = DateTime.Now;
        var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(today);
        return $"{today.Year}-W{weekNum:D2}";
    }

    // Propiedades computadas
    public bool IsDayScope => Scope == "day";
    public bool IsWeekScope => Scope == "week";
    public bool IsRangeScope => Scope == "range";
    public bool HasResumen => Resumen != null;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // Propiedades de Visibility (para x:Bind sin converter)
    public Visibility DayScopeVisibility => IsDayScope ? Visibility.Visible : Visibility.Collapsed;
    public Visibility WeekScopeVisibility => IsWeekScope ? Visibility.Visible : Visibility.Collapsed;
    public Visibility RangeScopeVisibility => IsRangeScope ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ResumenVisibility => HasResumen ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CanSelectAgentVisibility => CanSelectAgent ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsLoadingAgentsVisibility => IsLoadingAgents ? Visibility.Visible : Visibility.Collapsed;  // ← Nueva propiedad

    // Validación 8 horas
    public bool Is8HoursComplete => Resumen?.CoveredMinutes >= 480;
    public int MinutesMissing => Math.Max(0, 480 - (Resumen?.CoveredMinutes ?? 0));
    public string StatusMessage => Is8HoursComplete 
        ? "✅ Jornada completa (>=8h)" 
        : $"⚠️ Faltan {MinutesMissing} min para 8h";

    // Formateo de minutos a HH:mm
    public string RecordedTime => FormatMinutes(Resumen?.RecordedMinutes ?? 0);
    public string CoveredTime => FormatMinutes(Resumen?.CoveredMinutes ?? 0);
    public string OverlapTime => FormatMinutes(Resumen?.OverlapMinutes ?? 0);

    // Formato legible de fechas (dd/MM/yyyy HH:mm)
    public string FirstStartFormatted
    {
        get
        {
            if (Resumen?.FirstStart == null) return "-";
            if (DateTime.TryParse(Resumen.FirstStart, out var dt))
                return dt.ToString("dd/MM/yyyy HH:mm");
            return Resumen.FirstStart;
        }
    }

    public string LastEndFormatted
    {
        get
        {
            if (Resumen?.LastEnd == null) return "-";
            if (DateTime.TryParse(Resumen.LastEnd, out var dt))
                return dt.ToString("dd/MM/yyyy HH:mm");
            return Resumen.LastEnd;
        }
    }

    // Header contextual del Total según alcance
    public string TotalHeaderText
    {
        get
        {
            if (Resumen == null) return "Total:";

            return Scope switch
            {
                "day" => $"Total (Día: {SelectedDate:dd/MM/yyyy}):",
                "week" => GetWeekRangeText(),
                "range" => GetRangeText(),
                _ => "Total:"
            };
        }
    }

    private string GetWeekRangeText()
    {
        if (SelectedWeek == null) return "Total (Semana):";

        // Extraer año y número de semana del formato ISO "2026-W07"
        var parts = WeekIso.Split('-');
        if (parts.Length == 2 && parts[1].StartsWith("W"))
        {
            var year = int.Parse(parts[0]);
            var weekNum = int.Parse(parts[1].Substring(1));
            var monday = System.Globalization.ISOWeek.ToDateTime(year, weekNum, DayOfWeek.Monday);
            var sunday = monday.AddDays(6);
            return $"Total (Semana: {monday:dd/MM/yyyy} – {sunday:dd/MM/yyyy}):";
        }

        return "Total (Semana):";
    }

    private string GetRangeText()
    {
        if (RangeFrom.HasValue && RangeTo.HasValue)
            return $"Total (Rango: {RangeFrom.Value:dd/MM/yyyy} – {RangeTo.Value:dd/MM/yyyy}):";

        return "Total (Rango):";
    }

    // Agente de las estadísticas mostradas (banner + gráfica)
    public string AgentDisplayText
    {
        get
        {
            // Si no hay resumen, no mostrar nada
            if (Resumen == null) return string.Empty;

            // USER: Siempre es el usuario actual
            if (CurrentUserRole == UserRole.USER)
                return "Agente: Tú";

            // EDITOR/ADMIN: Verificar selección
            if (SelectedAgent != null)
            {
                return $"Agente: {SelectedAgent.FullName}";
            }

            // Sin selección: solo el usuario actual
            return "Agente: Tú";
        }
    }

    // Texto contextual para banner según scope
    public string BannerContextText
    {
        get
        {
            return Scope switch
            {
                "day" => $"Día: {SelectedDate:dd/MM/yyyy}",
                "week" => GetWeekBannerText(),
                "range" => GetRangeBannerText(),
                _ => "Selecciona un alcance"
            };
        }
    }

    private string GetWeekBannerText()
    {
        if (SelectedWeek == null) return "Semana: (selecciona una)";

        // Extraer año y número de semana del formato ISO "2026-W07"
        var parts = WeekIso.Split('-');
        if (parts.Length == 2 && parts[1].StartsWith("W"))
        {
            var year = int.Parse(parts[0]);
            var weekNum = int.Parse(parts[1].Substring(1));
            var monday = System.Globalization.ISOWeek.ToDateTime(year, weekNum, DayOfWeek.Monday);
            var sunday = monday.AddDays(6);
            return $"Semana {weekNum:D2} ({monday:dd/MM/yyyy} – {sunday:dd/MM/yyyy})";
        }

        return "Semana: (sin datos)";
    }

    private string GetRangeBannerText()
    {
        if (RangeFrom.HasValue && RangeTo.HasValue)
            return $"Rango: {RangeFrom.Value:dd/MM/yyyy} – {RangeTo.Value:dd/MM/yyyy}";

        return "Rango: (sin fechas)";
    }

    // Método para disparar notificaciones tras búsqueda
    public void ShowSearchResultNotifications()
    {
        if (Resumen == null) return;

        var messages = new List<string>();
        var severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;

        // 1. Validación 8 horas
        if (Resumen.CoveredMinutes >= 480)
        {
            messages.Add("✅ Jornada completa (>=8h)");
        }
        else
        {
            var minutesMissing = 480 - Resumen.CoveredMinutes;
            var hours = minutesMissing / 60;
            var mins = minutesMissing % 60;
            messages.Add($"⚠️ Faltan {hours}h {mins:D2}m para completar 8h");
            severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
        }

        // 2. Solapes
        if (Resumen.OverlapMinutes > 0)
        {
            var overlapHours = Resumen.OverlapMinutes / 60;
            var overlapMins = Resumen.OverlapMinutes % 60;
            messages.Add($"⚠️ Solape detectado: {overlapHours}h {overlapMins:D2}m");
            severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
        }

        // Mostrar notificación
        NotificationMessage = string.Join(" | ", messages);
        NotificationSeverity = severity;
        ShowNotification = true;
    }

    // GT-BEGIN: Validación para habilitar botón Buscar
    public bool CanSearch
    {
        get
        {
            if (IsLoading) return false;

            return Scope switch
            {
                "day" => SelectedDate != default,
                "week" => !string.IsNullOrWhiteSpace(WeekIso),
                "range" => RangeFrom.HasValue && RangeTo.HasValue && RangeFrom.Value <= RangeTo.Value,
                _ => false
            };
        }
    }
    // GT-END

    private static string FormatMinutes(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        return $"{hours}h {mins:D2}m";
    }

    /// <summary>Wrapper seguro para LoadWeekChartIfNeededAsync (evita crash en fire-and-forget).</summary>
    private async Task SafeLoadWeekChartAsync()
    {
        try
        {
            await LoadWeekChartIfNeededAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WeekChart] ERROR no capturado: {ex}");
            DispatchUI(() => WeekChartMessage = "Error al cargar gráfica semanal");
        }
    }

    /// <summary>Wrapper seguro para LoadOverlappingPartsAsync (evita crash en fire-and-forget).</summary>
    private async Task SafeLoadOverlappingPartsAsync()
    {
        try
        {
            await LoadOverlappingPartsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Overlap] ERROR no capturado: {ex}");
        }
    }

    // Carga de gráfica semanal
    /// <summary>Carga gráfica semanal si scope es Día o Semana.</summary>
    private async Task LoadWeekChartIfNeededAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[WeekChart] ===== INICIO LoadWeekChartIfNeededAsync =====");
        System.Diagnostics.Debug.WriteLine($"[WeekChart] Scope: {Scope}, Resumen != null: {Resumen != null}");

        try
        {
            DispatchUI(() =>
            {
                WeekChartItems.Clear();
                ShowWeekChart = false;
                WeekChartMessage = string.Empty;
            });

            if (Scope == "range" || Resumen == null)
            {
                DispatchUI(() => WeekChartMessage = "Gráfica semanal disponible en Día/Semana");
                System.Diagnostics.Debug.WriteLine($"[WeekChart] Saliendo: scope=range o Resumen=null");
                return;
            }

            string? weekIsoToLoad = null;

            if (Scope == "week")
            {
                weekIsoToLoad = WeekIso;
            }
            else if (Scope == "day")
            {
                var date = SelectedDate.DateTime;
                var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(date);
                weekIsoToLoad = $"{date.Year}-W{weekNum:D2}";
            }

            System.Diagnostics.Debug.WriteLine($"[WeekChart] weekIsoToLoad: {weekIsoToLoad}");

            if (string.IsNullOrWhiteSpace(weekIsoToLoad)) return;

            // Verificar si podemos reutilizar byDay del resumen actual
            bool canReuseByDay = Resumen.ByDay != null && Resumen.ByDay.Count > 0
                                 && Scope == "week" && WeekIso == weekIsoToLoad;

            if (canReuseByDay)
            {
                System.Diagnostics.Debug.WriteLine($"[WeekChart] REUTILIZANDO byDay del resumen actual ({Resumen.ByDay!.Count} días)");
                DateTime? selectedDateForHighlight = Scope == "day" ? SelectedDate.Date : null;

                DispatchUI(() =>
                {
                    BuildWeekChartFromByDay(Resumen.ByDay!, selectedDateForHighlight);
                    ShowWeekChart = true;
                });
                return;
            }

            // Hacer llamada adicional para obtener datos semanales
            // USER: no enviar agentId (backend usa JWT). EDITOR/ADMIN: solo si hay agente seleccionado.
            string? agentIdToSend = CurrentUserRole == UserRole.USER
                ? null
                : SelectedAgent?.Id;

            System.Diagnostics.Debug.WriteLine($"[WeekChart] Llamada adicional con agentId: {agentIdToSend ?? "(null, backend usa JWT)"}");

            var weekData = await _informesService.GetResumenAsync(
                scope: "week",
                date: null,
                weekIso: weekIsoToLoad,
                from: null,
                to: null,
                agentId: agentIdToSend,
                cancellationToken: _cts?.Token ?? CancellationToken.None);

            // DESPUÉS del await: puede estar en thread background → usar DispatchUI
            System.Diagnostics.Debug.WriteLine($"[WeekChart] Respuesta: weekData={weekData != null}, ByDay={weekData?.ByDay?.Count ?? 0}");

            if (weekData?.ByDay != null && weekData.ByDay.Count > 0)
            {
                DateTime? selectedDateForHighlight = Scope == "day" ? SelectedDate.Date : null;

                DispatchUI(() =>
                {
                    BuildWeekChartFromByDay(weekData.ByDay, selectedDateForHighlight);
                    ShowWeekChart = true;
                });
            }
            else
            {
                DispatchUI(() => WeekChartMessage = "No hay datos disponibles para esta semana");
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[WeekChart] Operación cancelada");
        }
        catch (Exception ex)
        {
            DispatchUI(() => WeekChartMessage = $"Error al cargar gráfica: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[WeekChart] ERROR: {ex}");
        }
    }

    /// <summary>Construye items de gráfica desde ByDay del DTO.</summary>
    /// <param name="byDay">Lista de estadísticas por día</param>
    /// <param name="selectedDate">Fecha seleccionada (solo si scope=day) para marcar con color diferente</param>
    private void BuildWeekChartFromByDay(List<DayStatsDto> byDay, DateTime? selectedDate = null)
    {
        System.Diagnostics.Debug.WriteLine($"[WeekChart] BuildWeekChartFromByDay iniciado con {byDay.Count} días (selectedDate: {selectedDate?.ToString("yyyy-MM-dd") ?? "null"})");

        // Filtrar solo Lunes-Sábado (excluir domingo)
        var workDays = byDay.Where(d => d.Date.DayOfWeek != DayOfWeek.Sunday).OrderBy(d => d.Date).ToList();
        System.Diagnostics.Debug.WriteLine($"[WeekChart] Días laborables (Lun-Sáb): {workDays.Count}");

        // Calcular total de minutos trabajados en la semana (Lun-Sáb)
        var totalWeekMinutes = workDays.Sum(d => d.CoveredMinutes);
        var totalWeekHours = totalWeekMinutes / 60;
        var totalWeekMins = totalWeekMinutes % 60;
        WeekTotalHours = $"{totalWeekHours}h {totalWeekMins:D2}m";

        System.Diagnostics.Debug.WriteLine($"[WeekChart] Total semana (Lun-Sáb): {totalWeekMinutes} min = {WeekTotalHours}");

        var daysOfWeek = new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };

        for (int i = 0; i < workDays.Count; i++)
        {
            var day = workDays[i];
            var dayLabel = i < daysOfWeek.Length ? daysOfWeek[i] : day.Date.ToString("dd");
            var minutes = day.CoveredMinutes;
            var hours = minutes / 60;
            var mins = minutes % 60;

            // Cálculo de porcentajes (8h objetivo + distribución semanal)
            // Porcentaje respecto a 8h objetivo (480 min)
            var percent8h = minutes > 0 ? (int)Math.Round((minutes / 480.0) * 100) : 0;

            // Porcentaje respecto al total de la semana (distribución)
            var percentage = totalWeekMinutes > 0 ? (int)((minutes / (double)totalWeekMinutes) * 100) : 0;

            // Ancho de barra normalizado a 8h (100% = 500px, máximo 600px para overflow)
            var barWidth = Math.Min((percent8h / 100.0) * 500, 600);

            // Verificar si es el día seleccionado (solo si selectedDate tiene valor y coincide con day.Date)
            bool isSelectedDay = selectedDate.HasValue && day.Date.Date == selectedDate.Value.Date;

            var item = new WeekChartItem
            {
                DayLabel = dayLabel,
                Minutes = minutes,
                HoursText = $"{hours}h {mins:D2}m",
                BarWidth = Math.Max(barWidth, 10), // Mínimo 10px para visibilidad
                IsUnderTarget = minutes < 480,
                IsSelectedDay = isSelectedDay,  // ← Marca el día seleccionado con color azul
                Percentage = percentage,  // ← Porcentaje del día respecto al total semanal
                Percent8h = percent8h  // ← Porcentaje respecto a 8h objetivo
            };

            WeekChartItems.Add(item);

            var colorInfo = isSelectedDay ? "(AZUL - día seleccionado)" : (item.IsUnderTarget ? "(ámbar)" : "(verde)");
            System.Diagnostics.Debug.WriteLine($"[WeekChart] Añadido: {dayLabel} = {hours}h {mins}m (8h: {percent8h}%, semanal: {percentage}%, barWidth: {item.BarWidth}px, {colorInfo})");
        }

        System.Diagnostics.Debug.WriteLine($"[WeekChart] BuildWeekChartFromByDay completado. Total items: {WeekChartItems.Count}, Total semana: {WeekTotalHours}");
    }

    /// <summary>Carga la lista de agentes disponibles desde el backend para EDITOR/ADMIN.</summary>
    private async Task LoadAvailableAgentsAsync()
    {
        if (!CanSelectAgent || IsLoadingAgents) return;

        try
        {
            DispatchUI(() => IsLoadingAgents = true);
            System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] 🔄 Cargando agentes disponibles...");

            var users = await App.Api.GetAsync<List<UserViewModel>>("/api/v1/admin/users");

            // DESPUÉS del await: puede estar en thread background → usar DispatchUI
            if (users != null && users.Count > 0)
            {
                var agents = users
                    .Where(u => u.Enabled)
                    .Select(u => new AgentOption
                    {
                        Id = u.Id.ToString(),
                        FullName = u.FullName ?? u.Email,
                        Email = u.Email
                    })
                    .OrderBy(a => a.FullName)
                    .ToList();

                DispatchUI(() =>
                {
                    AvailableAgents.Clear();
                    foreach (var agent in agents)
                    {
                        AvailableAgents.Add(agent);
                    }

                    if (!string.IsNullOrEmpty(CurrentUserId))
                    {
                        SelectedAgent = AvailableAgents.FirstOrDefault(a => a.Id == CurrentUserId);
                        if (SelectedAgent != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] 👤 Agente actual preseleccionado: {SelectedAgent.Display}");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] ✅ {agents.Count} agentes cargados");
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] ⚠️ No se encontraron agentes disponibles");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] ❌ Error cargando agentes: {ex.Message}");
        }
        finally
        {
            DispatchUI(() => IsLoadingAgents = false);
        }
    }
}
