using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.ViewModels;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Diagnostics;
using GestionTime.Desktop.Dialogs;  // 🆕 NUEVO: Agregar para usar CerrarParteDialog
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GestionTime.Desktop.Views;

public sealed partial class DiarioPage : Page
{
    public ObservableCollection<ParteDto> Partes { get; } = new();

    private List<ParteDto> _cache30dias = new();
    private DispatcherTimer? _debounce;
    private CancellationTokenSource? _loadCts;
    private bool _isLoading = false; // 🆕 NUEVO: Flag para evitar llamadas concurrentes
    private bool _isInitialLoad = true; // 🆕 NUEVO: Flag para evitar carga automática en constructor

    public DiarioViewModel ViewModel { get; } = new();

    public DiarioPage()
    {
        this.InitializeComponent();
        this.DataContext = ViewModel;

        LvPartes.ItemsSource = Partes;

        // 🆕 NUEVO: Aplicar tema global
        ThemeService.Instance.ApplyTheme(this);

        // 🆕 CORREGIDO: Establecer fecha SIN disparar el evento DateChanged
        DpFiltroFecha.Date = DateTimeOffset.Now;

        // 🆕 NUEVO: Suscribir el evento DESPUÉS de establecer la fecha inicial
        DpFiltroFecha.DateChanged += OnFiltroFechaChanged;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _debounce.Tick += (_, __) =>
        {
            _debounce!.Stop();
            ApplyFilterToListView();
        };

        InitializeIcons();
        InitializeKeyboardAccelerators();

        // 🆕 NUEVO: Suscribirse a cambios de tema globales
        ThemeService.Instance.ThemeChanged += OnGlobalThemeChanged;

        this.Unloaded += OnPageUnloaded;
    }

    /// <summary>
    /// 🆕 NUEVO: Manejador de cambios de tema globales
    /// </summary>
    private void OnGlobalThemeChanged(object? sender, ElementTheme theme)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            this.RequestedTheme = theme;
            UpdateThemeAssets(theme);
            UpdateThemeCheckmarks();
            App.Log?.LogDebug("🎨 DiarioPage: Tema actualizado por cambio global a {theme}", theme);
        });
    }

    /// <summary>
    /// 🆕 NUEVO: Aplica zebra rows dinámicamente usando e.ItemIndex
    /// Se ejecuta en cada render/reciclado para mantener el patrón correcto con virtualización
    /// </summary>
    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer is ListViewItem container)
        {
            // Aplicar Background según el índice (par/impar)
            var isEvenRow = args.ItemIndex % 2 == 0;

            if (isEvenRow)
            {
                // Fila par: Transparente
                container.Background = Resources["EvenRowBrush"] as SolidColorBrush;
            }
            else
            {
                // Fila impar: Turquesa 40%
                container.Background = Resources["OddRowBrush"] as SolidColorBrush;
            }

            // Log para debug (solo en modo Debug)
#if DEBUG
            if (args.ItemIndex < 10)
            {
                App.Log?.LogDebug("🎨 Zebra: ItemIndex={index}, IsEven={isEven}, Background={bg}", 
                    args.ItemIndex, isEvenRow, isEvenRow ? "Transparent" : "Turquesa");
            }
#endif
        }
    }

    /// <summary>
    /// OBSOLETO: Ya no usamos OnListViewContainerContentChanging (se reemplaza por OnContainerContentChanging)
    /// </summary>
    private void OnListViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        // Ya no se usa - el nuevo método OnContainerContentChanging lo reemplaza
    }

    /// <summary>
    /// OBSOLETO: Ya no necesitamos OnContainerLoaded
    /// </summary>
    private void OnContainerLoaded(object sender, RoutedEventArgs e)
    {
        // Ya no se usa - zebra rows se aplican en OnContainerContentChanging
    }

    /// <summary>
    /// OBSOLETO: Ya no necesitamos ApplyZebraRowBackground
    /// </summary>
    private void ApplyZebraRowBackground(ListViewBase listView, ListViewItem container)
    {
        // Ya no se usa - zebra rows se aplican en OnContainerContentChanging
    }

    /// <summary>
    /// OBSOLETO: Ya no necesitamos RefreshAllZebraRows
    /// </summary>
    private void RefreshAllZebraRows()
    {
        // Ya no se usa - zebra rows se aplican automáticamente con ItemIndex
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        // Detener el monitoreo del servicio
        ViewModel.StopServiceMonitoring();

        // Limpiar timer de debounce
        _debounce?.Stop();

        // 🆕 NUEVO: Desuscribir eventos de tema para evitar memory leaks
        ThemeService.Instance.ThemeChanged -= OnGlobalThemeChanged;

        App.Log?.LogInformation("DiarioPage Unloaded - Recursos limpiados");
    }

    private void InitializeIcons()
    {
        App.Log?.LogDebug("Iconos de DiarioPage inicializados (referenciando IconHelper)");
    }

    private void InitializeKeyboardAccelerators()
    {
        // Ctrl+N - Nuevo
        var accelNuevo = new KeyboardAccelerator { Key = Windows.System.VirtualKey.N };
        accelNuevo.Modifiers = Windows.System.VirtualKeyModifiers.Control;
        accelNuevo.Invoked += (s, e) => { OnNuevo(this, new RoutedEventArgs()); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelNuevo);

        // Ctrl+T - Nueva llamada telefónica
        var accelTelefono = new KeyboardAccelerator { Key = Windows.System.VirtualKey.T };
        accelTelefono.Modifiers = Windows.System.VirtualKeyModifiers.Control;
        accelTelefono.Invoked += (s, e) => { OnNuevaLlamada(this, new RoutedEventArgs()); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelTelefono);

        // Ctrl+E - Editar
        var accelEditar = new KeyboardAccelerator { Key = Windows.System.VirtualKey.E };
        accelEditar.Modifiers = Windows.System.VirtualKeyModifiers.Control;
        accelEditar.Invoked += (s, e) => { if (BtnEditar.IsEnabled) OnEditar(this, new RoutedEventArgs()); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelEditar);

        // F8 - Gráfica
        var accelGrafica = new KeyboardAccelerator { Key = Windows.System.VirtualKey.F8 };
        accelGrafica.Invoked += (s, e) => { OnAbrirGrafica(this, new RoutedEventArgs()); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelGrafica);

        // Delete - Borrar
        var accelBorrar = new KeyboardAccelerator { Key = Windows.System.VirtualKey.Delete };
        accelBorrar.Invoked += (s, e) => { OnBorrar(this, new RoutedEventArgs()); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelBorrar);

        // Ctrl+Q - Salir
        var accelSalir = new KeyboardAccelerator { Key = Windows.System.VirtualKey.Q };
        accelSalir.Modifiers = Windows.System.VirtualKeyModifiers.Control;
        accelSalir.Invoked += (s, e) => { OnLogout(this, new RoutedEventArgs()); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelSalir);

        // F5 - Refrescar
        var accelRefrescar = new KeyboardAccelerator { Key = Windows.System.VirtualKey.F5 };
        accelRefrescar.Invoked += async (s, e) => { await LoadPartesAsync(); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelRefrescar);

        // F12 - Configuración
        var accelConfig = new KeyboardAccelerator { Key = Windows.System.VirtualKey.F12 };
        accelConfig.Invoked += (s, e) => { OnConfiguracion(this, new RoutedEventArgs()); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelConfig);

        App.Log?.LogDebug("Atajos de teclado configurados: Ctrl+T, Ctrl+N, Ctrl+E, F8, Delete, Ctrl+Q, F5, F12");
    }

    // ===================== ANIMACIONES HOVER =====================

    private void OnButtonPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.IsEnabled)
        {
            AnimateButtonScale(button, 1.08, 150);
        }
    }

    private void OnButtonPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Button button)
        {
            AnimateButtonScale(button, 1.0, 150);
        }
    }

    private void AnimateButtonScale(Button button, double targetScale, int durationMs)
    {
        // Asegurar que cada botón tenga su propio ScaleTransform
        ScaleTransform scaleTransform;

        if (button.RenderTransform is ScaleTransform existingTransform)
        {
            scaleTransform = existingTransform;
        }
        else
        {
            // Crear un nuevo ScaleTransform único para este botón
            scaleTransform = new ScaleTransform
            {
                ScaleX = 1.0,
                ScaleY = 1.0,
                CenterX = 0.5,
                CenterY = 0.5
            };
            button.RenderTransform = scaleTransform;
            button.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        }

        // Crear animaciones para ScaleX y ScaleY
        var animX = new DoubleAnimation
        {
            To = targetScale,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var animY = new DoubleAnimation
        {
            To = targetScale,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        // Aplicar las animaciones directamente al ScaleTransform de este botón
        Storyboard.SetTarget(animX, scaleTransform);
        Storyboard.SetTargetProperty(animX, "ScaleX");

        Storyboard.SetTarget(animY, scaleTransform);
        Storyboard.SetTargetProperty(animY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(animX);
        storyboard.Children.Add(animY);
        storyboard.Begin();
    }

    // ===================== PAGE LIFECYCLE =====================

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ((FrameworkElement)sender).Loaded -= OnPageLoaded;

        try
        {
            App.Log?.LogInformation("DiarioPage Loaded ✅");

            // Inicializar tema y assets
            UpdateThemeAssets(this.RequestedTheme);

            // Cargar información del usuario desde LocalSettings
            try
            {
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;

                var userName = settings.TryGetValue("UserName", out var nameObj) && nameObj is string name
                    ? name
                    : "Usuario";

                var userEmail = settings.TryGetValue("UserEmail", out var emailObj) && emailObj is string email
                    ? email
                    : "usuario@empresa.com";

                var userRole = settings.TryGetValue("UserRole", out var roleObj) && roleObj is string role
                    ? role
                    : "Usuario";

                App.Log?.LogInformation("📋 Cargando información de usuario desde LocalSettings:");
                App.Log?.LogInformation("   • UserName: {name} (default: {isDefault})", userName, nameObj == null);
                App.Log?.LogInformation("   • UserEmail: {email} (default: {isDefault})", userEmail, emailObj == null);
                App.Log?.LogInformation("   • UserRole: {role} (default: {isDefault})", userRole, roleObj == null);

                ViewModel.SetUserInfo(userName, userEmail, userRole);
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error cargando información del usuario");
                ViewModel.SetUserInfo("Usuario", "usuario@empresa.com", "Usuario");
            }

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeIn, RootGrid);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();

            // 🆕 NUEVO: Cargar datos y DESPUÉS habilitar el evento de fecha
            await LoadPartesAsync();

            // Habilitar el evento de cambio de fecha DESPUÉS de la carga inicial
            _isInitialLoad = false;
            App.Log?.LogDebug("✅ Carga inicial completada - Evento de fecha habilitado");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnPageLoaded()");
        }
    }

    private async Task LoadPartesAsync()
    {
        // 🆕 NUEVO: Protección contra llamadas concurrentes
        if (_isLoading)
        {
            App.Log?.LogDebug("⚠️ Carga ya en proceso, ignorando nueva petición");
            return;
        }

        _isLoading = true;

        try
        {
            // 🔒 Cancelar cualquier carga previa
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();
            var ct = _loadCts.Token;

            var selectedDate = DpFiltroFecha.Date?.DateTime.Date ?? DateTime.Today;

            // 🆕 OPTIMIZACIÓN: Determinar si el usuario seleccionó HOY o una fecha específica
            var isToday = selectedDate.Date == DateTime.Today;

            DateTime fromDate;
            DateTime toDate = selectedDate;

            if (isToday)
            {
                // Vista por defecto: Últimos 7 días (no 30)
                fromDate = selectedDate.AddDays(-7);
                App.Log?.LogInformation("📅 Carga INICIAL: Últimos 7 días (desde {from} hasta HOY)", fromDate.ToString("yyyy-MM-dd"));
            }
            else
            {
                // Fecha específica: SOLO ese día
                fromDate = selectedDate;
                App.Log?.LogInformation("📅 Carga FILTRADA: Solo día {date}", selectedDate.ToString("yyyy-MM-dd"));
            }

            using var loadScope = PerformanceLogger.BeginScope(SpecializedLoggers.Data, "LoadPartes",
                new { FromDate = fromDate, ToDate = toDate, IsFiltered = !isToday });

            SpecializedLoggers.Data.LogInformation("══════════════════════════════════════════════════════════════─");
            SpecializedLoggers.Data.LogInformation("📥 CARGA DE PARTES");
            SpecializedLoggers.Data.LogInformation("   • Fecha inicio: {from}", fromDate.ToString("yyyy-MM-dd"));
            SpecializedLoggers.Data.LogInformation("   • Fecha fin: {to}", toDate.ToString("yyyy-MM-dd"));

            // 🆕 CORREGIDO: Cálculo preciso de días
            var totalDays = isToday ? 7 : 1;  // Simplificado: 7 días para HOY, 1 para fecha específica
            SpecializedLoggers.Data.LogInformation("   • Días a cargar: {days}", totalDays);
            SpecializedLoggers.Data.LogInformation("   • Tipo: {type}", isToday ? "Vista inicial (últimos 7 días)" : "Fecha específica");

            // 🆕 Usar método con estrategia dual (rango + fallback)
            await LoadPartesAsync_Legacy();
        }
        catch (OperationCanceledException)
        {
            SpecializedLoggers.Data.LogInformation("Carga de partes cancelada por el usuario.");
        }
        catch (Exception ex)
        {
            SpecializedLoggers.Data.LogError(ex, "Error cargando partes");

            // NO mostrar diálogo de error, solo loguear
            SpecializedLoggers.Data.LogWarning("La lista quedará vacía. El usuario puede intentar refrescar (F5).");
        }
        finally
        {
            _isLoading = false; // 🆕 NUEVO: Liberar flag
        }
    }

    // 🔄 MÉTODO CON ESTRATEGIA DUAL
    private async Task LoadPartesAsync_Legacy()
    {
        var ct = _loadCts?.Token ?? CancellationToken.None;

        try
        {
            // 🆕 CORREGIDO: Usar las fechas que ya calculamos en LoadPartesAsync()
            var selectedDate = DpFiltroFecha.Date?.DateTime.Date ?? DateTime.Today;
            var isToday = selectedDate.Date == DateTime.Today;

            DateTime fromDate;
            DateTime toDate = selectedDate;

            if (isToday)
            {
                // Vista por defecto: Últimos 7 días
                fromDate = selectedDate.AddDays(-7);
            }
            else
            {
                // Fecha específica: SOLO ese día
                fromDate = selectedDate;
            }

            // ✅ ESTRATEGIA DUAL: Intentar endpoint de rango primero, fallback a peticiones individuales
            SpecializedLoggers.Data.LogInformation("🔄 Intentando carga con endpoint de rango (1 petición)...");

            var usedRangeEndpoint = await TryLoadWithRangeEndpointAsync(fromDate, toDate, ct);

            if (usedRangeEndpoint)
            {
                SpecializedLoggers.Data.LogInformation("✅ Endpoint de rango exitoso - {count} partes cargados", _cache30dias.Count);
                ApplyFilterToListView();
                return;
            }

            // Si el endpoint de rango falló, usar método de peticiones individuales
            SpecializedLoggers.Data.LogWarning("⚠️ Endpoint de rango no disponible - Usando fallback a peticiones individuales");
            await LoadWithIndividualRequestsAsync(fromDate, toDate, ct);

            ApplyFilterToListView();
        }
        catch (OperationCanceledException)
        {
            SpecializedLoggers.Data.LogInformation("Carga de partes cancelada");
            throw;
        }
        catch (Exception ex)
        {
            SpecializedLoggers.Data.LogError(ex, "Error en método de carga");

            // Asegurar que al menos haya una lista vacía
            _cache30dias = new List<ParteDto>();
            ApplyFilterToListView();

            throw;
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Intenta cargar con endpoint de rango (1 sola petición)
    /// Retorna true si fue exitoso, false si necesita fallback
    /// </summary>
    private async Task<bool> TryLoadWithRangeEndpointAsync(DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        try
        {
            // ✅ USAR NUEVOS PARÁMETROS: fechaInicio y fechaFin
            // El backend ahora soporta filtrado por fecha_trabajo (NO por created_at)
            var path = $"/api/v1/partes?fechaInicio={fromDate:yyyy-MM-dd}&fechaFin={toDate:yyyy-MM-dd}";

            SpecializedLoggers.Data.LogInformation("📡 Endpoint: GET {path}", path);
            SpecializedLoggers.Data.LogInformation("   • Fecha inicio: {from}", fromDate.ToString("yyyy-MM-dd"));
            SpecializedLoggers.Data.LogInformation("   • Fecha fin: {to}", toDate.ToString("yyyy-MM-dd"));
            SpecializedLoggers.Data.LogInformation("   ℹ️ Usando endpoint de rango por fecha_trabajo (fechaInicio/fechaFin)");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await App.Api.GetAsync<List<ParteDto>>(path, ct);
            sw.Stop();

            if (result == null)
            {
                SpecializedLoggers.Data.LogWarning("⚠️ Endpoint de rango devolvió null - Necesita fallback");
                return false;
            }

            if (result.Count == 0)
            {
                // Verificar si realmente no hay datos o si el endpoint no está implementado
                SpecializedLoggers.Data.LogInformation("ℹ️ Endpoint de rango devolvió 0 registros - Verificando si es correcto...");

                var testPath = $"/api/v1/partes?fecha={toDate:yyyy-MM-dd}";
                var testResult = await App.Api.GetAsync<List<ParteDto>>(testPath, ct);

                if (testResult != null && testResult.Count > 0)
                {
                    SpecializedLoggers.Data.LogWarning("⚠️ El endpoint de un día SÍ tiene datos, pero el de rango devolvió vacío");
                    SpecializedLoggers.Data.LogWarning("   → Endpoint de rango probablemente NO implementado correctamente");
                    return false; // Necesita fallback
                }
                else
                {
                    SpecializedLoggers.Data.LogInformation("✅ Endpoint de rango correcto - Realmente no hay datos en este periodo");
                    _cache30dias = new List<ParteDto>();
                    return true; // No hay datos, pero el endpoint funciona
                }
            }

            _cache30dias = result;
            SpecializedLoggers.Data.LogInformation("✅ Petición exitosa en {ms}ms - {count} partes cargados",
                sw.ElapsedMilliseconds, _cache30dias.Count);

            // Log de estadísticas por estado
            var estadoStats = _cache30dias
                .GroupBy(p => p.EstadoTexto)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();

            if (estadoStats.Any())
            {
                SpecializedLoggers.Data.LogInformation("📊 Estados: {estados}", string.Join(", ", estadoStats));
            }

            return true; // Éxito
        }
        catch (ApiException apiEx)
        {
            SpecializedLoggers.Data.LogWarning("⚠️ Endpoint de rango falló - StatusCode: {status}, Message: {msg}",
                apiEx.StatusCode, apiEx.Message);

            // Si es 404 o 400, el endpoint probablemente no existe
            if (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound ||
                apiEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                SpecializedLoggers.Data.LogWarning("   → Endpoint probablemente no implementado en backend");
                return false; // Necesita fallback
            }

            // Para otros errores, re-lanzar
            throw;
        }
        catch (Exception ex)
        {
            SpecializedLoggers.Data.LogWarning(ex, "⚠️ Error inesperado con endpoint de rango - Usando fallback");
            return false; // Necesita fallback
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Carga con 31 peticiones individuales (fallback)
    /// </summary>
    private async Task LoadWithIndividualRequestsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        SpecializedLoggers.Data.LogInformation("🔄 Cargando partes día por día ({days} peticiones)", (toDate - fromDate).Days + 1);

        using var sem = new SemaphoreSlim(6); // 6 peticiones concurrentes
        var tasks = new List<Task<List<ParteDto>>>();

        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            var day = d;
            tasks.Add(FetchDayLimitedAsync(day, sem, ct));
        }

        var results = await Task.WhenAll(tasks);
        _cache30dias = results
            .Where(x => x != null)
            .SelectMany(x => x)
            .ToList();

        SpecializedLoggers.Data.LogInformation("✅ {count} partes cargados correctamente (método individual)", _cache30dias.Count);
    }

    /// <summary>
    /// Helper para cargar un día específico con semáforo y retry
    /// </summary>
    private async Task<List<ParteDto>> FetchDayLimitedAsync(DateTime day, SemaphoreSlim sem, CancellationToken ct)
    {
        var waitSuccessful = await sem.WaitAsync(TimeSpan.FromSeconds(30), ct);

        if (!waitSuccessful)
        {
            App.Log?.LogWarning("⚠️ Timeout esperando slot del semáforo para {fecha} - Saltando...",
                day.ToString("yyyy-MM-dd"));
            return new List<ParteDto>();
        }

        try
        {
            var path = "/api/v1/partes?fecha=" + Uri.EscapeDataString(day.ToString("yyyy-MM-dd"));

            var maxRetries = 3;
            var retryDelay = 500;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        App.Log?.LogDebug("🔄 Reintento {attempt}/{max} - GET {path}",
                            attempt, maxRetries, path);
                    }

                    var result = await App.Api.GetAsync<List<ParteDto>>(path, ct) ?? new List<ParteDto>();

                    if (result.Count > 0 && attempt == 1)
                    {
                        App.Log?.LogDebug("📅 {fecha}: {count} partes", day.ToString("yyyy-MM-dd"), result.Count);
                    }

                    if (attempt > 1)
                    {
                        App.Log?.LogInformation("✅ Exitoso en intento {attempt} para {fecha}", attempt, day.ToString("yyyy-MM-dd"));
                    }

                    return result;
                }
                catch (Exception ex) when (attempt < maxRetries && !ct.IsCancellationRequested)
                {
                    lastException = ex;

                    App.Log?.LogWarning("⚠️ Intento {attempt}/{max} fallido para {fecha} - {error}",
                        attempt, maxRetries, day.ToString("yyyy-MM-dd"), ex.Message);

                    await Task.Delay(retryDelay, ct);
                    retryDelay *= 2;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }

            App.Log?.LogWarning("❌ Todos los intentos ({max}) fallaron para {fecha}",
                maxRetries, day.ToString("yyyy-MM-dd"));

            return new List<ParteDto>();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Fallo cargando partes del día {day}", day.ToString("yyyy-MM-dd"));
            return new List<ParteDto>();
        }
        finally { sem.Release(); }
    }

    private void ApplyFilterToListView()
    {
        var q = (TxtFiltroQ.Text ?? string.Empty).Trim();

        IEnumerable<ParteDto> query = _cache30dias;

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p =>
                Has(p.Cliente, q) ||
                Has(p.Tienda, q) ||
                Has(p.Accion, q) ||
                Has(p.Ticket, q) ||
                Has(p.Grupo, q) ||
                Has(p.Tipo, q) ||
                Has(p.Tecnico, q) ||
                Has(p.Estado, q)
            );
        }

        query = query
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => ParseTime(p.HoraInicio));

        Partes.Clear();
        foreach (var p in query)
            Partes.Add(p);

        App.Log?.LogInformation("Filtro aplicado q='{q}'. Mostrando {count} registros.", q, Partes.Count);

        // Log de estados en la lista final
        var estadosEnLista = Partes.GroupBy(p => p.EstadoTexto).Select(g => $"{g.Key}:{g.Count()}");
        App.Log?.LogInformation("📊 Estados en ListView: {estados}", string.Join(", ", estadosEnLista));

        // 🆕 NUEVO: Actualizar tooltip de cobertura de tiempo
        UpdateTimeCoverageTooltip();
    }

    private static bool Has(string? s, string q)
        => !string.IsNullOrWhiteSpace(s) && s.Contains(q, StringComparison.OrdinalIgnoreCase);

    private static TimeSpan ParseTime(string? hhmm)
        => TimeSpan.TryParse(hhmm, out var ts) ? ts : TimeSpan.Zero;

    // ===================== Filtros =====================

    private async void OnFiltroFechaChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        // 🆕 NUEVO: NO cargar si es la inicialización automática
        if (_isInitialLoad)
        {
            App.Log?.LogDebug("🚫 OnFiltroFechaChanged - Ignorando carga inicial automática");
            return;
        }

        App.Log?.LogInformation("📅 Usuario cambió fecha manualmente - Recargando...");
        await LoadPartesAsync();
    }

    private void OnFiltroQChanged(object sender, TextChangedEventArgs e)
    {
        _debounce?.Stop();
        _debounce?.Start();
    }

    private async void OnRefrescar(object sender, RoutedEventArgs e)
    {
        App.Log?.LogInformation("🔄 Botón REFRESCAR presionado - Restaurando vista inicial");

        // 🆕 NUEVO: Invalidar TODO el caché antes de recargar
        App.Log?.LogInformation("🗑️ Invalidando caché completo de partes...");
        App.Api.ClearGetCache(); // Limpia TODA la caché de GET (es más seguro que invalidar solo un rango)
        App.Log?.LogInformation("✅ Caché de API limpiado completamente");
        
        // Limpiar caché local también
        _cache30dias.Clear();
        Partes.Clear();
        App.Log?.LogInformation("✅ Caché local limpiado");

        // Deshabilitar temporalmente el evento de fecha
        _isInitialLoad = true;

        // Restaurar fecha a HOY
        DpFiltroFecha.Date = DateTimeOffset.Now;

        // Recargar partes (se cargará últimos 7 días automáticamente desde el servidor)
        await LoadPartesAsync();

        // Rehabilitar el evento de fecha
        _isInitialLoad = false;
        
        App.Log?.LogInformation("✅ Refrescar completado - Datos actualizados desde el servidor");
    }

    private void OnPartesSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var hasSelection = LvPartes.SelectedItem != null;
        BtnEditar.IsEnabled = hasSelection;
        BtnBorrar.IsEnabled = hasSelection;
    }

    // ===================== Theme =====================

    private void OnThemeSystem(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Default);
    private void OnThemeLight(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Light);
    private void OnThemeDark(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Dark);

    private void SetTheme(ElementTheme theme)
    {
        // 🆕 NUEVO: Usar ThemeService para cambiar el tema globalmente
        ThemeService.Instance.SetTheme(theme);

        // Actualizar checks del menú
        ThemeSystemItem.IsChecked = theme == ElementTheme.Default;
        ThemeLightItem.IsChecked = theme == ElementTheme.Light;
        ThemeDarkItem.IsChecked = theme == ElementTheme.Dark;

        // Actualizar logo y fondo según el tema
        UpdateThemeAssets(theme);

        App.Log?.LogInformation("🎨 DiarioPage - Tema cambiado a: {theme} (guardado en configuración)", theme);
    }

    /// <summary>
    /// 🆕 NUEVO: Actualiza los checkmarks del menú de tema
    /// </summary>
    private void UpdateThemeCheckmarks()
    {
        var currentTheme = ThemeService.Instance.CurrentTheme;
        ThemeSystemItem.IsChecked = currentTheme == ElementTheme.Default;
        ThemeLightItem.IsChecked = currentTheme == ElementTheme.Light;
        ThemeDarkItem.IsChecked = currentTheme == ElementTheme.Dark;
    }

    private void UpdateThemeAssets(ElementTheme theme)
    {
        // Determinar el tema efectivo
        var effectiveTheme = theme;
        if (theme == ElementTheme.Default)
        {
            // Obtener el tema del sistema
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            var foreground = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
            // Si el foreground es blanco, el tema es oscuro
            effectiveTheme = foreground.R == 255 && foreground.G == 255 && foreground.B == 255
                ? ElementTheme.Dark
                : ElementTheme.Light;
        }

        // Actualizar logo del banner
        if (effectiveTheme == ElementTheme.Dark)
        {
            LogoImageBanner.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/LogoOscuro.png"));

            // Fondo oscuro: imagen visible
            BackgroundImageBrush.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/diario_bg_dark.png"));
            BackgroundImageBrush.Opacity = 1.0;
        }
        else
        {
            LogoImageBanner.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/LogoClaro.png"));

            // Fondo claro: imagen muy sutil (casi transparente)
            BackgroundImageBrush.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/Diario_bg_claro.png"));
            BackgroundImageBrush.Opacity = 0.15;
        }

        App.Log?.LogDebug("Tema actualizado: {theme} (efectivo: {effective})", theme, effectiveTheme);
    }

    // ===================== Botones principales =====================

    private async Task OpenParteEditorAsync(ParteDto? parte, string title)
    {
        var window = new Microsoft.UI.Xaml.Window { Title = title };
        var editPage = new ParteItemEdit();

        // 🆕 NUEVO: Aplicar tema global a la ventana de edición
        ThemeService.Instance.ApplyTheme(editPage);

        editPage.SetParentWindow(window);
        window.Content = editPage;
        ConfigureChildWindow(window);

        if (parte == null)
            editPage.NewParte();
        else
            editPage.LoadParte(parte);

        var tcs = new TaskCompletionSource<bool>();
        window.Closed += (_, __) => tcs.TrySetResult(editPage.Guardado);
        window.Activate();

        var saved = await tcs.Task;
        if (saved && editPage.ParteActualizado != null)
        {
            // ✅ OPTIMIZACIÓN: Actualización local sin recargar desde servidor
            var parteActualizado = editPage.ParteActualizado;
            
            App.Log?.LogInformation("💾 Parte guardado - Actualizando lista local SIN recargar desde servidor...");
            App.Log?.LogInformation("   • Parte ID: {id}", parteActualizado.Id);
            App.Log?.LogInformation("   • Cliente: {cliente}", parteActualizado.Cliente);
            App.Log?.LogInformation("   • Grupo: {grupo}", parteActualizado.Grupo);
            App.Log?.LogInformation("   • Tipo: {tipo}", parteActualizado.Tipo);
            
            if (parte == null)
            {
                // ✅ CREAR: Agregar a la lista local
                App.Log?.LogInformation("🆕 Nuevo parte - Agregando a lista local...");
                
                // Agregar al caché
                _cache30dias.Add(parteActualizado);
                
                // Insertar en la posición correcta en la ObservableCollection (ordenado por fecha DESC, hora DESC)
                var insertIndex = 0;
                for (int i = 0; i < Partes.Count; i++)
                {
                    var p = Partes[i];
                    // Si el parte actual tiene fecha más reciente, o misma fecha pero hora más reciente
                    if (parteActualizado.Fecha > p.Fecha ||
                        (parteActualizado.Fecha == p.Fecha && ParseTime(parteActualizado.HoraInicio) > ParseTime(p.HoraInicio)))
                    {
                        insertIndex = i;
                        break;
                    }
                    insertIndex = i + 1;
                }
                
                Partes.Insert(insertIndex, parteActualizado);
                
                App.Log?.LogInformation("✅ Nuevo parte agregado en posición {index} (ID: {id})", insertIndex, parteActualizado.Id);
            }
            else
            {
                // ✅ EDITAR: Actualizar en ambas listas
                App.Log?.LogInformation("✏️ Editando parte existente - Actualizando en lista local...");
                
                // Actualizar en _cache30dias
                var indexCache = _cache30dias.FindIndex(p => p.Id == parteActualizado.Id);
                if (indexCache >= 0)
                {
                    _cache30dias[indexCache] = parteActualizado;
                    App.Log?.LogInformation("✅ Parte actualizado en _cache30dias (index: {index})", indexCache);
                }
                else
                {
                    App.Log?.LogWarning("⚠️ Parte ID {id} no encontrado en _cache30dias", parteActualizado.Id);
                }
                
                // Actualizar en Partes (ObservableCollection)
                var parteEnLista = Partes.FirstOrDefault(p => p.Id == parteActualizado.Id);
                if (parteEnLista != null)
                {
                    var indexLista = Partes.IndexOf(parteEnLista);
                    Partes[indexLista] = parteActualizado;
                    App.Log?.LogInformation("✅ Parte actualizado en Partes (ObservableCollection, index: {index})", indexLista);
                }
                else
                {
                    App.Log?.LogWarning("⚠️ Parte ID {id} no encontrado en Partes (ObservableCollection)", parteActualizado.Id);
                }
            }
            
            // ✅ OPCIONAL: Invalidar solo el endpoint específico (para futuras consultas)
            InvalidatePartesCache(parteActualizado.Fecha);
            
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("✅ ACTUALIZACIÓN LOCAL COMPLETADA");
            App.Log?.LogInformation("   📊 Estadísticas:");
            App.Log?.LogInformation("      • Peticiones HTTP: 0 (actualización local)");
            App.Log?.LogInformation("      • Tiempo: <10ms (instantáneo)");
            App.Log?.LogInformation("      • Items en _cache30dias: {count}", _cache30dias.Count);
            App.Log?.LogInformation("      • Items en Partes: {count}", Partes.Count);
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        }
    }

    private void ConfigureChildWindow(Microsoft.UI.Xaml.Window window)
    {
        // ✅ Usar WindowSizeManager para ParteItemEdit con REDIMENSIONAMIENTO HABILITADO
        WindowSizeManager.SetChildWindowSize(window,
            typeof(ParteItemEdit),
            WindowSizeManager.ParteEditSize.Width,
            WindowSizeManager.ParteEditSize.Height,
            resizable: true,       // ✅ HABILITADO: Ahora se puede redimensionar
            maximizable: true);    // ✅ HABILITADO: Ahora se puede maximizar
    }

    private async void OnNuevo(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("🆕 NUEVO PARTE - Iniciando proceso");

            var fechaNuevo = DpFiltroFecha.Date?.DateTime ?? DateTime.Today;
            App.Log?.LogInformation("📅 Fecha seleccionada: {fecha}", fechaNuevo.ToString("yyyy-MM-dd"));

            var decision = await CheckSolapeAndAskAsync(fechaNuevo);
            App.Log?.LogInformation("🎯 Decisión del usuario: {decision}", decision);

            if (decision == "cancel")
            {
                App.Log?.LogInformation("SOLAPE_CANCEL: Usuario canceló creación de nuevo parte");
                return;
            }

            if (decision == "close")
            {
                var horaInicioNuevo = DateTime.Now.ToString("HH:mm");
                var abiertos = _cache30dias
                    .Where(p => p.CanCerrar && p.Fecha.Date == fechaNuevo.Date)
                    .ToList();

                App.Log?.LogInformation("SOLAPE_CLOSE_PREV: Cerrando {count} partes abiertos con hora_fin={hora}",
                    abiertos.Count, horaInicioNuevo);

                await ClosePartesAbiertosAsync(abiertos, horaInicioNuevo);
            }
            else if (decision == "keep")
            {
                App.Log?.LogInformation("SOLAPE_KEEP_OPEN: Manteniendo partes abiertos (solape permitido)");
            }

            App.Log?.LogInformation("📝 Abriendo editor de nuevo parte...");
            await OpenParteEditorAsync(null, "Nuevo Parte");
            App.Log?.LogInformation("════════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error abriendo ventana de nuevo parte");
            await ShowInfoAsync("❌ Error creando parte nuevo. Revisa app.log.");
        }
    }

    private async void OnNuevaLlamada(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("📞 NUEVA LLAMADA TELEFÓNICA - Creación rápida");

            var fechaLlamada = DateTime.Today;
            var horaActual = DateTime.Now.ToString("HH:mm");

            App.Log?.LogInformation("📅 Fecha: {fecha} | Hora: {hora}", fechaLlamada.ToString("yyyy-MM-dd"), horaActual);

            var parteLlamada = new ParteDto
            {
                Fecha = fechaLlamada,
                HoraInicio = horaActual,
                HoraFin = "",
                Ticket = "TELEFONO",
                Accion = "Llamada telefónica",
                Cliente = "",
                Tienda = "",
                Grupo = "",
                Tipo = "",
                EstadoParte = ParteEstado.Abierto
            };

            App.Log?.LogInformation("📝 Abriendo editor con parte de llamada pre-configurado...");
            await OpenParteEditorAsync(parteLlamada, "📞 Nueva Llamada Telefónica");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error creando parte de llamada telefónica");
            await ShowInfoAsync("❌ Error creando llamada. Revisa app.log.");
        }
    }

    private async void OnEditar(object sender, RoutedEventArgs e)
    {
        if (LvPartes.SelectedItem is not ParteDto parte)
        {
            await ShowInfoAsync("⚠️ Selecciona un parte para editar.");
            return;
        }

        try
        {
            App.Log?.LogInformation("Abriendo ventana de edición para parte ID: {id}", parte.Id);
            await OpenParteEditorAsync(parte, "Editar Parte");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error abriendo ventana de edición");
            await ShowInfoAsync("❌ Error abriendo ventana de edición. Revisa app.log.");
        }
    }

    private async void OnGrabar(object sender, RoutedEventArgs e)
        => await ShowInfoAsync("Grabar: pendiente de implementar 💾");

    private void OnAbrirGrafica(object sender, RoutedEventArgs e)
    {
        try
        {
            var fecha = DpFiltroFecha.Date?.DateTime ?? DateTime.Today;

            var window = new Microsoft.UI.Xaml.Window
            {
                Title = $"📊 Gráfica del Día - {fecha:dd/MM/yyyy}"
            };

            var graficaPage = new GraficaDiaPage();

            // 🆕 NUEVO: Aplicar tema global a la ventana de gráfica
            ThemeService.Instance.ApplyTheme(graficaPage);

            graficaPage.ViewModel.FechaSeleccionada = fecha;

            window.Content = graficaPage;
            ConfigureGraficaWindow(window);
            window.Activate();

            App.Log?.LogInformation("Ventana de gráfica abierta para fecha {fecha}", fecha.ToString("yyyy-MM-dd"));
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error abriendo ventana de gráfica");
        }
    }

    private void ConfigureGraficaWindow(Microsoft.UI.Xaml.Window window)
    {
        // ✅ Usar WindowSizeManager para GraficaPage
        WindowSizeManager.SetChildWindowSize(window,
            typeof(GraficaDiaPage),
            WindowSizeManager.GraficaSize.Width,
            WindowSizeManager.GraficaSize.Height,
            resizable: true,
            maximizable: true);
    }

    private async void OnBorrar(object sender, RoutedEventArgs e)
    {
        if (LvPartes.SelectedItem is not ParteDto parte)
        {
            await ShowInfoAsync("⚠️ Selecciona un parte para borrar.");
            return;
        }

        var confirmDialog = new ContentDialog
        {
            Title = "⚠️ Confirmar eliminación DEFINITIVA",
            Content = $"¿Estás seguro de que deseas ELIMINAR DEFINITIVAMENTE el parte ID {parte.Id}?\n\nCliente: {parte.Cliente}\nFecha: {parte.FechaText}\nAcción: {parte.Accion}\n\n⚠️ ATENCIÓN: Esta acción NO se puede deshacer. El registro se borrará permanentemente de la base de datos.",
            PrimaryButtonText = "Eliminar definitivamente",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        try
        {
            App.Log?.LogWarning("DELETE /api/v1/partes/{id} (borrado físico definitivo)", parte.Id);
            await App.Api.DeleteAsync($"/api/v1/partes/{parte.Id}");

            App.Log?.LogWarning("✅ Parte {id} ELIMINADO FÍSICAMENTE de la base de datos", parte.Id);

            // 🆕 NUEVO: Invalidar caché después de eliminar
            App.Log?.LogInformation("🗑️ Invalidando caché de partes...");
            InvalidatePartesCache(parte.Fecha);

            // 🆕 MEJORADO: Eliminar inmediatamente de la caché local
            var removedFromCache = _cache30dias.RemoveAll(p => p.Id == parte.Id);
            App.Log?.LogInformation("🗑️ Eliminados {count} registros de la caché local", removedFromCache);

            // 🆕 MEJORADO: Eliminar de la ObservableCollection inmediatamente
            var parteEnLista = Partes.FirstOrDefault(p => p.Id == parte.Id);
            if (parteEnLista != null)
            {
                Partes.Remove(parteEnLista);
                App.Log?.LogInformation("🗑️ Parte eliminado de la lista visible");
            }

            await ShowInfoAsync($"✅ Parte {parte.Id} eliminado definitivamente.");

            // Opcional: Recargar desde el servidor para asegurar sincronización
            // await LoadPartesAsync();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error eliminando parte {id}", parte.Id);
            await ShowInfoAsync($"❌ Error eliminando parte: {ex.Message}");
        }
    }

    // ===================== ACCIONES DE ESTADO =====================

    private async void OnPausarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuItem || menuItem.Tag is not int parteId)
        {
            App.Log?.LogWarning("OnPausarClick: Tag no es int, es {type}", (sender as MenuFlyoutItem)?.Tag?.GetType()?.Name ?? "null");
            return;
        }

        var parte = Partes.FirstOrDefault(p => p.Id == parteId);
        if (parte == null || !parte.CanPausar)
        {
            App.Log?.LogWarning("OnPausarClick: Parte {id} no encontrado o CanPausar=false", parteId);
            return;
        }

        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("⏸️ PAUSAR PARTE - ID: {id}", parteId);
            App.Log?.LogInformation("   Estado actual: {estado}", parte.Estado);

            // Usar endpoint específico POST /api/v1/partes/{id}/pause
            await App.Api.PostAsync($"/api/v1/partes/{parteId}/pause");

            App.Log?.LogInformation("✅ Parte {id} pausado correctamente", parteId);
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

            // 🆕 NUEVO: Invalidar caché después de pausar
            App.Log?.LogInformation("🗑️ Invalidando caché de partes...");
            InvalidatePartesCache(parte.Fecha);

            await LoadPartesAsync();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error pausando parte {id}", parteId);
            await ShowInfoAsync($"❌ Error pausando parte: {ex.Message}");
        }
    }

    private async void OnReanudarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuItem || menuItem.Tag is not int parteId)
        {
            return;
        }

        var parte = Partes.FirstOrDefault(p => p.Id == parteId);
        if (parte == null || !parte.CanReanudar)
        {
            return;
        }

        try
        {
            App.Log?.LogInformation("▶️ REANUDAR PARTE - ID: {id}", parteId);
            await App.Api.PostAsync($"/api/v1/partes/{parteId}/resume");
            App.Log?.LogInformation("✅ Parte {id} reanudado correctamente", parteId);

            // 🆕 NUEVO: Invalidar caché después de reanudar
            App.Log?.LogInformation("🗑️ Invalidando caché de partes...");
            InvalidatePartesCache(parte.Fecha);

            await LoadPartesAsync();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error reanudando parte {id}", parteId);
            await ShowInfoAsync($"❌ Error reanudando parte: {ex.Message}");
        }
    }

    private async void OnCerrarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuItem || menuItem.Tag is not int parteId)
        {
            App.Log?.LogWarning("⚠️ OnCerrarClick: Tag inválido - Type={type}",
                (sender as MenuFlyoutItem)?.Tag?.GetType()?.Name ?? "null");
            return;
        }

        var parte = Partes.FirstOrDefault(p => p.Id == parteId);
        if (parte == null || !parte.CanCerrar)
        {
            App.Log?.LogWarning("⚠️ OnCerrarClick: Parte {id} no encontrado o no se puede cerrar (CanCerrar={can})",
                parteId, parte?.CanCerrar ?? false);
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("🔒 CERRAR PARTE - INICIO DEL PROCESO");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("📋 DATOS DEL PARTE A CERRAR:");
            App.Log?.LogInformation("   • ID: {id}", parteId);
            App.Log?.LogInformation("   • Cliente: {cliente}", parte.Cliente ?? "(sin cliente)");
            App.Log?.LogInformation("   • Fecha: {fecha}", parte.Fecha.ToString("yyyy-MM-dd"));
            App.Log?.LogInformation("   • Estado ACTUAL: {estado} (EstadoInt={int}, IsAbierto={abierto})",
                parte.EstadoTexto, parte.EstadoInt, parte.IsAbierto);
            App.Log?.LogInformation("   • HoraInicio: {inicio}", parte.HoraInicio ?? "(vacío)");
            App.Log?.LogInformation("   • HoraFin ANTES: '{fin}'", string.IsNullOrEmpty(parte.HoraFin) ? "(vacío)" : parte.HoraFin);
            App.Log?.LogInformation("   • Ticket: {ticket}", parte.Ticket ?? "(sin ticket)");
            App.Log?.LogInformation("   • Acción: {accion}", TrimForLog(parte.Accion, 50));
            App.Log?.LogInformation("───────────────────────────────────────────────────────────────");

            // 🆕 NUEVO: Pasar el objeto parte completo al diálogo
            App.Log?.LogInformation("🎯 PASO 1: Abrir diálogo para solicitar hora de cierre...");
            var dialogStart = System.Diagnostics.Stopwatch.StartNew();

            var horaFin = await AskHoraCierreAsync(parte);

            dialogStart.Stop();
            App.Log?.LogInformation("   ⏱️ Diálogo completado en {ms}ms", dialogStart.ElapsedMilliseconds);

            if (string.IsNullOrEmpty(horaFin))
            {
                App.Log?.LogInformation("❌ Usuario CANCELÓ el cierre del parte");
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                return;
            }

            App.Log?.LogInformation("✅ Hora de cierre capturada del usuario: '{hora}'", horaFin);
            App.Log?.LogInformation("───────────────────────────────────────────────────────────────");
            App.Log?.LogInformation("🎯 PASO 2: Enviar petición de cierre al backend...");
            App.Log?.LogInformation("   📤 PARÁMETROS DE CIERRE:");
            App.Log?.LogInformation("      • Parte ID: {id}", parteId);
            App.Log?.LogInformation("      • Hora Fin: '{horaFin}'", horaFin);
            App.Log?.LogInformation("      • Estado objetivo: 2 (Cerrado)");
            App.Log?.LogInformation("───────────────────────────────────────────────────────────────");

            // 🆕 CORREGIDO: Intentar POST /close PRIMERO (más confiable)
            var cierreCorrecto = false;
            var metodoUsado = "";
            var requestStart = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Método 1: POST /api/v1/partes/{id}/close?horaFin=HH:mm
                var endpoint = $"/api/v1/partes/{parteId}/close?horaFin={Uri.EscapeDataString(horaFin)}";
                var fullUrl = $"{App.Api.BaseUrl}{endpoint}";

                App.Log?.LogInformation("🔄 MÉTODO 1: Intentando POST /close");
                App.Log?.LogInformation("   📡 Endpoint: POST {endpoint}", endpoint);
                App.Log?.LogInformation("   🌐 URL completa: {url}", fullUrl);
                App.Log?.LogInformation("   📦 Query params:");
                App.Log?.LogInformation("      - horaFin={hora} (URL encoded: {encoded})",
                    horaFin, Uri.EscapeDataString(horaFin));
                App.Log?.LogInformation("   ⏳ Enviando petición...");

                var postStart = System.Diagnostics.Stopwatch.StartNew();

                await App.Api.PostAsync(endpoint);

                postStart.Stop();

                App.Log?.LogInformation("✅ POST /close EXITOSO");
                App.Log?.LogInformation("   ⏱️ Tiempo de respuesta: {ms}ms", postStart.ElapsedMilliseconds);
                App.Log?.LogInformation("   📥 Parte {id} cerrado correctamente", parteId);
                App.Log?.LogInformation("   🕐 Hora de fin aplicada: {hora}", horaFin);

                cierreCorrecto = true;
                metodoUsado = "POST /close";
            }
            catch (ApiException postEx)
            {
                App.Log?.LogWarning("⚠️ POST /close FALLÓ - Código: {status}", postEx.StatusCode);
                App.Log?.LogWarning("   💬 Mensaje: {message}", postEx.Message);
                App.Log?.LogWarning("   📄 Mensaje del servidor: {serverMsg}",
                    TrimForLog(postEx.ServerMessage ?? postEx.ServerError ?? "(sin respuesta)", 200));
                App.Log?.LogInformation("───────────────────────────────────────────────────────────────");
                App.Log?.LogInformation("🔄 MÉTODO 2 (FALLBACK): Intentando PUT completo...");

                try
                {
                    // Método 2 (fallback): PUT /api/v1/partes/{id} con payload completo
                    var putEndpoint = $"/api/v1/partes/{parteId}";
                    var fullPutUrl = $"{App.Api.BaseUrl}{putEndpoint}";

                    var putPayload = new
                    {
                        fecha_trabajo = parte.Fecha.ToString("yyyy-MM-dd"),
                        hora_inicio = parte.HoraInicio,
                        hora_fin = horaFin,
                        id_cliente = parte.IdCliente,
                        tienda = parte.Tienda ?? "",
                        id_grupo = parte.IdGrupo,
                        id_tipo = parte.IdTipo,
                        accion = parte.Accion ?? "",
                        ticket = parte.Ticket ?? "",
                        estado = 2  // Cerrado
                    };

                    App.Log?.LogInformation("   📡 Endpoint: PUT {endpoint}", putEndpoint);
                    App.Log?.LogInformation("   🌐 URL completa: {url}", fullPutUrl);
                    App.Log?.LogInformation("   📦 Payload JSON:");
                    App.Log?.LogInformation("      - fecha_trabajo: {fecha}", putPayload.fecha_trabajo);
                    App.Log?.LogInformation("      - hora_inicio: {inicio}", putPayload.hora_inicio);
                    App.Log?.LogInformation("      - hora_fin: {fin}", putPayload.hora_fin);
                    App.Log?.LogInformation("      - id_cliente: {id}", putPayload.id_cliente);
                    App.Log?.LogInformation("      - tienda: '{tienda}'", putPayload.tienda);
                    App.Log?.LogInformation("      - id_grupo: {id}", putPayload.id_grupo?.ToString() ?? "null");
                    App.Log?.LogInformation("      - id_tipo: {id}", putPayload.id_tipo?.ToString() ?? "null");
                    App.Log?.LogInformation("      - accion: '{accion}'", TrimForLog(putPayload.accion, 50));
                    App.Log?.LogInformation("      - ticket: '{ticket}'", putPayload.ticket);
                    App.Log?.LogInformation("      - estado: {estado} (Cerrado)", putPayload.estado);
                    App.Log?.LogDebug("   📋 Payload completo: {@payload}", putPayload);
                    App.Log?.LogInformation("   ⏳ Enviando petición...");

                    var putStart = System.Diagnostics.Stopwatch.StartNew();

                    await App.Api.PutAsync<object, object>(putEndpoint, putPayload);

                    putStart.Stop();

                    App.Log?.LogInformation("✅ PUT EXITOSO");
                    App.Log?.LogInformation("   ⏱️ Tiempo de respuesta: {ms}ms", putStart.ElapsedMilliseconds);
                    App.Log?.LogInformation("   📥 Parte {id} cerrado correctamente", parteId);
                    App.Log?.LogInformation("   🕐 Hora de fin aplicada: {hora}", horaFin);

                    cierreCorrecto = true;
                    metodoUsado = "PUT /partes/{id}";
                }
                catch (ApiException putEx)
                {
                    App.Log?.LogError("❌ PUT TAMBIÉN FALLÓ - Código: {status}", putEx.StatusCode);
                    App.Log?.LogError("   💬 Mensaje: {message}", putEx.Message);
                    App.Log?.LogError("   📄 Mensaje del servidor: {serverMsg}",
                        TrimForLog(putEx.ServerMessage ?? putEx.ServerError ?? "(sin respuesta)", 500));
                    App.Log?.LogError("   🔍 Stack trace: {stack}", putEx.StackTrace);
                    throw;
                }
                catch (Exception putGenEx)
                {
                    App.Log?.LogError(putGenEx, "❌ PUT falló con error inesperado");
                    throw;
                }
            }
            catch (Exception postGenEx)
            {
                App.Log?.LogError(postGenEx, "❌ POST /close falló con error inesperado");
                throw;
            }
            finally
            {
                requestStart.Stop();
                App.Log?.LogInformation("   ⏱️ Tiempo total de peticiones HTTP: {ms}ms", requestStart.ElapsedMilliseconds);
            }

            // Verificar que el cierre fue exitoso
            if (!cierreCorrecto)
            {
                App.Log?.LogError("❌ CIERRE FALLIDO: No se pudo cerrar el parte {id}", parteId);
                App.Log?.LogError("   ⚠️ Ambos métodos (POST y PUT) fallaron");
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                await ShowInfoAsync($"❌ Error: No se pudo cerrar el parte.\n\nRevisa los logs para más detalles.");
                return;
            }

            App.Log?.LogInformation("───────────────────────────────────────────────────────────────");
            App.Log?.LogInformation("✅ CIERRE EXITOSO usando: {metodo}", metodoUsado);
            App.Log?.LogInformation("───────────────────────────────────────────────────────────────");
            App.Log?.LogInformation("🎯 PASO 3: Post-procesamiento...");

            // 🆕 NUEVO: Invalidar caché después de cerrar el parte
            App.Log?.LogInformation("   🗑️ Invalidando caché de partes...");
            var cacheStart = System.Diagnostics.Stopwatch.StartNew();

            InvalidatePartesCache(parte.Fecha);

            cacheStart.Stop();
            App.Log?.LogInformation("   ✅ Caché invalidado en {ms}ms", cacheStart.ElapsedMilliseconds);

            // CRUCIAL: Esperar un momento antes de recargar para asegurar que el backend procesó el cambio
            App.Log?.LogInformation("   ⏳ Esperando 500ms para sincronización del backend...");
            await Task.Delay(500);

            App.Log?.LogInformation("   🔄 Recargando lista de partes desde el servidor...");
            var reloadStart = System.Diagnostics.Stopwatch.StartNew();

            await LoadPartesAsync();

            reloadStart.Stop();
            App.Log?.LogInformation("   ✅ Lista recargada en {ms}ms", reloadStart.ElapsedMilliseconds);

            stopwatch.Stop();

            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("✅ PROCESO COMPLETADO EXITOSAMENTE");
            App.Log?.LogInformation("   ⏱️ Tiempo total: {ms}ms ({seconds:F2}s)",
                stopwatch.ElapsedMilliseconds, stopwatch.Elapsed.TotalSeconds);
            App.Log?.LogInformation("   📊 Resumen:");
            App.Log?.LogInformation("      • Método usado: {metodo}", metodoUsado);
            App.Log?.LogInformation("      • Parte ID: {id}", parteId);
            App.Log?.LogInformation("      • Hora de cierre: {hora}", horaFin);
            App.Log?.LogInformation("      • Estado final: Cerrado (2)");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        }
        catch (ApiException apiEx)
        {
            stopwatch.Stop();

            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            App.Log?.LogError("❌ ERROR API AL CERRAR PARTE {id}", parteId);
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            App.Log?.LogError("🔴 DETALLES DEL ERROR:");
            App.Log?.LogError("   • Tipo: ApiException");
            App.Log?.LogError("   • StatusCode: {status} ({statusInt})", apiEx.StatusCode, (int)apiEx.StatusCode);
            App.Log?.LogError("   • Mensaje: {message}", apiEx.Message);
            App.Log?.LogError("   • Path: {path}", apiEx.Path);
            App.Log?.LogError("   • Mensaje del servidor: {serverMsg}", apiEx.ServerMessage ?? "(sin mensaje)");
            App.Log?.LogError("   • Error del servidor: {serverError}",
                TrimForLog(apiEx.ServerError ?? "(sin error)", 1000));
            App.Log?.LogError("   • Stack trace: {stack}", apiEx.StackTrace);
            App.Log?.LogError("   ⏱️ Tiempo transcurrido: {ms}ms", stopwatch.ElapsedMilliseconds);
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");

            await ShowInfoAsync($"❌ Error cerrando parte:\n\n{apiEx.Message}\n\nCódigo: {apiEx.StatusCode}\n\nRevisa los logs para más detalles.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            App.Log?.LogError("❌ ERROR INESPERADO AL CERRAR PARTE {id}", parteId);
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            App.Log?.LogError("🔴 DETALLES DEL ERROR:");
            App.Log?.LogError("   • Tipo: {type}", ex.GetType().Name);
            App.Log?.LogError("   • Mensaje: {message}", ex.Message);
            App.Log?.LogError("   • Stack trace: {stack}", ex.StackTrace);
            if (ex.InnerException != null)
            {
                App.Log?.LogError("   • Inner exception: {inner}", ex.InnerException.Message);
                App.Log?.LogError("   • Inner stack: {stack}", ex.InnerException.StackTrace);
            }
            App.Log?.LogError("   ⏱️ Tiempo transcurrido: {ms}ms", stopwatch.ElapsedMilliseconds);
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");

            await ShowInfoAsync($"❌ Error inesperado cerrando parte:\n\n{ex.Message}\n\nRevisa los logs para más detalles.");
        }
    }

    /// <summary>
    /// Muestra el diálogo mejorado para cerrar un parte
    /// </summary>
    private async Task<string?> AskHoraCierreAsync(ParteDto parte)
    {
        try
        {
            // Crear instancia del diálogo mejorado
            var dialog = new CerrarParteDialog(parte)
            {
                XamlRoot = this.XamlRoot
            };

            App.Log?.LogInformation("🔒 Abriendo diálogo de cierre para parte ID: {id}", parte.Id);

            // Mostrar diálogo
            var result = await dialog.ShowAsync();

            // Verificar resultado
            if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(dialog.HoraCierreConfirmada))
            {
                App.Log?.LogInformation("✅ Hora de cierre confirmada: {hora}", dialog.HoraCierreConfirmada);
                return dialog.HoraCierreConfirmada;
            }
            else
            {
                App.Log?.LogInformation("❌ Usuario canceló el cierre del parte");
                return null;
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error mostrando diálogo de cierre");
            await ShowInfoAsync("Error mostrando diálogo. Intenta nuevamente.");
            return null;
        }
    }

    private async void OnDuplicarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuItem || menuItem.Tag is not int parteId)
        {
            return;
        }

        var parte = Partes.FirstOrDefault(p => p.Id == parteId);
        if (parte == null)
        {
            return;
        }

        try
        {
            App.Log?.LogInformation("📋 DUPLICAR PARTE - ID: {id}", parteId);

            var nuevoParte = new ParteDto
            {
                Fecha = DateTime.Today,
                HoraInicio = DateTime.Now.ToString("HH:mm"),
                HoraFin = "",
                Cliente = parte.Cliente,
                Tienda = parte.Tienda,
                Accion = parte.Accion,
                Ticket = "",
                Grupo = parte.Grupo,
                Tipo = parte.Tipo,
                EstadoParte = ParteEstado.Abierto
            };

            await OpenParteEditorAsync(nuevoParte, $"📋 Duplicar Parte #{parte.Id}");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error duplicando parte {id}", parteId);
            await ShowInfoAsync($"❌ Error duplicando parte: {ex.Message}");
        }
    }

    // ===================== CONFIGURACIÓN =====================

    private async void OnConfiguracion(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("🎛️ Abriendo ventana de configuración del sistema...");

            var configWindow = new ConfiguracionWindow();
            configWindow.ShowWindow(this);

            App.Log?.LogInformation("✅ Ventana de configuración abierta");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error abriendo ventana de configuración");
            await ShowInfoAsync("Error abriendo configuración. Revisa app.log.");
        }
    }
    
    /// <summary>Navega a la página de perfil de usuario.</summary>
    private void OnMiPerfilClick(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("👤 MI PERFIL - Navegando a UserProfilePage");
            App.MainWindowInstance?.Navigator?.Navigate(typeof(UserProfilePage));
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error navegando a UserProfilePage");
        }
    }

    private string GetCurrentUserRole()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            return settings.TryGetValue("UserRole", out var roleObj) && roleObj is string role
                ? role
                : "Usuario";
        }
        catch
        {
            return "Usuario";
        }
    }

    // ===================== MÉTODOS HELPER =====================

    private async void OnLogout(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("Usuario solicitó logout");

            var confirmDialog = new ContentDialog
            {
                Title = "Cerrar sesión",
                Content = "¿Estás seguro de que deseas cerrar la sesión?",
                PrimaryButtonText = "Cerrar sesión",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                // 🆕 LIMPIEZA COMPLETA al hacer logout
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                App.Log?.LogInformation("🚪 LOGOUT - Limpiando sesión y datos");
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

                // 🔥 CORRECCIÓN: Acceso seguro a ApplicationData con try-catch robusto
                bool rememberSession = false;
                string? savedEmail = null;
                
                try
                {
                    var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    
                    // Verificar si el usuario tenía "Recordar sesión" activado ANTES de limpiar
                    if (settings.Values.TryGetValue("RememberSession", out var remObj) && remObj is bool rem)
                    {
                        rememberSession = rem;
                        App.Log?.LogInformation("📧 RememberSession estaba en: {value}", rememberSession);
                    }
                    
                    if (rememberSession && settings.Values.TryGetValue("RememberedEmail", out var emailObj) && emailObj is string email)
                    {
                        savedEmail = email;
                        App.Log?.LogInformation("📧 Correo guardado encontrado: {email}", savedEmail);
                    }

                    // 1. Limpiar LocalSettings (sesión de usuario)
                    settings.Values.Remove("UserToken");
                    settings.Values.Remove("UserName");
                    settings.Values.Remove("UserEmail");
                    settings.Values.Remove("UserRole");
                    App.Log?.LogInformation("✅ LocalSettings de sesión limpiados");
                    
                    // 🔥 IMPORTANTE: NO eliminar RememberSession, RememberedEmail ni SavedEmail
                    // si el usuario tenía activado "Recordar sesión"
                    if (rememberSession && !string.IsNullOrEmpty(savedEmail))
                    {
                        App.Log?.LogInformation("✅ Preservando preferencias de 'Recordar sesión':");
                        App.Log?.LogInformation("   • RememberSession: {value}", rememberSession);
                        App.Log?.LogInformation("   • RememberedEmail: {email}", savedEmail);
                        App.Log?.LogInformation("   • SavedEmail: {email}", savedEmail);
                        // No eliminar estas claves
                    }
                    else
                    {
                        // Si NO tenía activado "Recordar sesión", limpiar todo
                        settings.Values.Remove("RememberSession");
                        settings.Values.Remove("RememberedEmail");
                        settings.Values.Remove("SavedEmail");
                        App.Log?.LogInformation("🗑️ Preferencias de 'Recordar sesión' eliminadas (no estaba activado)");
                    }
                }
                catch (InvalidOperationException invOpEx)
                {
                    // Este error ocurre cuando ApplicationData.Current no está disponible
                    App.Log?.LogError(invOpEx, "⚠️ ApplicationData.Current no disponible durante logout");
                    App.Log?.LogWarning("Se continuará con la limpieza de otros datos");
                }
                catch (Exception settingsEx)
                {
                    App.Log?.LogError(settingsEx, "Error accediendo a ApplicationData.Current.LocalSettings");
                }

                // 2. Limpiar token del ApiClient
                App.Api.ClearToken();
                App.Log?.LogInformation("✅ Token de autenticación eliminado");

                // 3. Limpiar caché de GET requests
                App.Api.ClearGetCache();
                App.Log?.LogInformation("✅ Caché de peticiones limpiado");

                // 4. Limpiar caché local de partes
                _cache30dias.Clear();
                Partes.Clear();
                App.Log?.LogInformation("✅ Caché local de partes limpiado");

                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                App.Log?.LogInformation("✅ LOGOUT COMPLETADO - Navegando al login");
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

                // Navegar al LoginPage con animación
                try
                {
                    var fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    
                    Storyboard.SetTarget(fadeOut, RootGrid);
                    Storyboard.SetTargetProperty(fadeOut, "Opacity");
                    
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeOut);
                    storyboard.Completed += (s, args) => App.MainWindowInstance?.Navigator?.Navigate(typeof(LoginPage));
                    
                    storyboard.Begin();
                }
                catch (Exception animEx)
                {
                    App.Log?.LogWarning(animEx, "Error en animación de fade out, continuando con navegación");
                    App.MainWindowInstance?.Navigator?.Navigate(typeof(LoginPage));
                }
            }
            else
            {
                App.Log?.LogInformation("Usuario canceló el logout");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error crítico en logout");
            
            // FALLBACK DE EMERGENCIA: Navegar sin animación
            try
            {
                App.Log?.LogWarning("Intentando navegación de emergencia al LoginPage...");
                App.MainWindowInstance?.Navigator?.Navigate(typeof(LoginPage));
                App.Log?.LogInformation("✅ Navegación de emergencia exitosa");
            }
            catch (Exception fallbackEx)
            {
                App.Log?.LogError(fallbackEx, "❌ Navegación de emergencia también falló");
            }
        }
    }
    
    private void OnSalir(object sender, RoutedEventArgs e)
    {
        // Llamar directamente a OnLogout
        OnLogout(sender, e);
    }
    
    private async Task<string> CheckSolapeAndAskAsync(DateTime fecha)
    {
        try
        {
            var abiertos = _cache30dias
                .Where(p => p.CanCerrar && p.Fecha.Date == fecha.Date)
                .ToList();

            if (!abiertos.Any())
            {
                return "continue";
            }

            var dialog = new ContentDialog
            {
                Title = "⚠️ Hay partes abiertos",
                Content = $"Hay {abiertos.Count} parte(s) abierto(s) en la fecha {fecha:dd/MM/yyyy}.\n\n" +
                         "¿Qué deseos hacer?",
                PrimaryButtonText = "Cerrar anteriores",
                SecondaryButtonText = "Mantener abiertos",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result switch
            {
                ContentDialogResult.Primary => "close",
                ContentDialogResult.Secondary => "keep",
                _ => "cancel"
            };
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error verificando solape");
            return "continue";
        }
    }

    private async Task ClosePartesAbiertosAsync(List<ParteDto> abiertos, string horaFin)
    {
        try
        {
            foreach (var parte in abiertos)
            {
                await App.Api.PostAsync($"/api/v1/partes/{parte.Id}/close?horaFin={Uri.EscapeDataString(horaFin)}");
                App.Log?.LogInformation("Parte {id} cerrado automáticamente con HoraFin={hora}", parte.Id, horaFin);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error cerrando partes abiertos");
        }
    }

    private async Task ShowInfoAsync(string message)
    {
        try
        {
            var dlg = new ContentDialog
            {
                Title = "GestionTime",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await dlg.ShowAsync();
        }
        catch { }
    }

    private static string TrimForLog(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Length <= max) return s;
        return s.Substring(0, max) + "…";
    }
    
    /// <summary>
    /// 🆕 NUEVO: Invalida las entradas de caché relacionadas con un parte en una fecha específica
    /// </summary>
    private void InvalidatePartesCache(DateTime fecha)
    {
        try
        {
            // ✅ CORREGIDO: Invalidar usando fechaInicio/fechaFin (NO created_from/created_to)
            var fromDate = fecha.AddDays(-30).ToString("yyyy-MM-dd");
            var toDate = fecha.AddDays(30).ToString("yyyy-MM-dd");
            
            // Endpoint de rango (usando fechaInicio/fechaFin)
            var rangePath = $"/api/v1/partes?fechaInicio={fromDate}&fechaFin={toDate}";
            App.Api.InvalidateCacheEntry(rangePath);
            App.Log?.LogDebug("🗑️ Caché invalidado (rango fechaInicio/fechaFin): {path}", rangePath);
            
            // También invalidar la fecha específica (método legacy)
            var dayPath = $"/api/v1/partes?fecha={fecha:yyyy-MM-dd}";
            App.Api.InvalidateCacheEntry(dayPath);
            App.Log?.LogDebug("🗑️ Caché invalidado (día específico): {path}", dayPath);
            
            // También invalidar la fecha actual (por si estamos trabajando con la fecha de hoy)
            if (fecha.Date != DateTime.Today)
            {
                var todayPath = $"/api/v1/partes?fecha={DateTime.Today:yyyy-MM-dd}";
                App.Api.InvalidateCacheEntry(todayPath);
                App.Log?.LogDebug("🗑️ Caché invalidado (hoy): {path}", todayPath);
            }
            
            App.Log?.LogInformation("✅ Caché de partes invalidado correctamente");
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Error invalidando caché de partes");
        }
    }
    
    /// <summary>
    /// 🆕 NUEVO: Calcula y actualiza el tooltip de cobertura de tiempo sin solapamiento
    /// </summary>
    private void UpdateTimeCoverageTooltip()
    {
        try
        {
            var intervals = Partes
                .Where(p => !string.IsNullOrWhiteSpace(p.HoraInicio))
                .Select(p =>
                {
                    if (!TimeSpan.TryParse(p.HoraInicio, out var inicio))
                        return null;
                    
                    var startTime = p.Fecha.Date.Add(inicio);
                    
                    DateTime endTime;
                    if (!string.IsNullOrWhiteSpace(p.HoraFin) && TimeSpan.TryParse(p.HoraFin, out var fin))
                    {
                        endTime = p.Fecha.Date.Add(fin);
                    }
                    else
                    {
                        endTime = DateTime.Now;
                    }
                    
                    if (endTime <= startTime)
                        return null;
                    
                    return new IntervalMerger.Interval(startTime, endTime);
                })
                .Where(i => i != null)
                .Cast<IntervalMerger.Interval>()
                .ToList();
            
            if (!intervals.Any())
            {
                UpdateDuracionHeaderTooltip(null);
                return;
            }
            
            var coverage = IntervalMerger.ComputeCoverage(intervals);
            UpdateDuracionHeaderTooltip(coverage);
            
            App.Log?.LogInformation("⏱️ Cobertura: {covered}, Solapado: {overlap}",
                IntervalMerger.FormatDuration(coverage.TotalCovered),
                IntervalMerger.FormatDuration(coverage.TotalOverlap));
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error calculando cobertura");
            UpdateDuracionHeaderTooltip(null);
        }
    }
    
    /// <summary>
    /// 🆕 NUEVO: Actualiza el tooltip del header "Dur."
    /// </summary>
    private void UpdateDuracionHeaderTooltip(IntervalMerger.CoverageResult? coverage)
    {
        try
        {
            if (DuracionHeader == null)
                return;
            
            if (coverage == null || !coverage.MergedIntervals.Any())
            {
                ToolTipService.SetToolTip(DuracionHeader, "No hay datos de tiempo disponibles");
                return;
            }
            
            var tooltipText = BuildCoverageTooltipText(coverage);
            ToolTipService.SetToolTip(DuracionHeader, tooltipText);
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error actualizando tooltip");
        }
    }
    
    /// <summary>
    /// 🆕 NUEVO: Construye el texto del tooltip
    /// </summary>
    private static string BuildCoverageTooltipText(IntervalMerger.CoverageResult coverage)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("⏱️ TIEMPO REAL OCUPADO (SIN SOLAPAMIENTO)");
        sb.AppendLine();
        sb.AppendLine($"📊 Cubierto: {IntervalMerger.FormatDuration(coverage.TotalCovered)}");
        
        if (coverage.TotalOverlap.TotalMinutes > 0)
            sb.AppendLine($"⚠️ Solapado: {IntervalMerger.FormatDuration(coverage.TotalOverlap)}");
        
        sb.AppendLine();
        sb.AppendLine($"🕐 Intervalos cubiertos ({coverage.MergedIntervals.Count}):");
        
        foreach (var interval in coverage.MergedIntervals)
        {
            var formatted = IntervalMerger.FormatInterval(interval);
            var duration = IntervalMerger.FormatDuration(interval.Duration);
            sb.AppendLine($"   • {formatted} ({duration})");
        }
        
        return sb.ToString().TrimEnd();
    }
}
