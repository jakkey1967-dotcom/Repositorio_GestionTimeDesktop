using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.ViewModels;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Services.Catalog;  // 🆕 NUEVO: Usar PartesService
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
    private OnlineUsersPanelViewModel? _usersPanelViewModel; // 🆕 NUEVO: ViewModel del panel de usuarios
    
    // 🆕 NUEVO: Servicio de partes (lazy loading)
    private PartesService? _partesService;
    private PartesService PartesService => _partesService ??= new PartesService(App.Api, App.Log!);
    
    // 🆕 PAGINACIÓN: Variables para paginar ListView
    private const int ITEMS_PER_PAGE = 30;
    private int _currentPage = 1;
    private int _totalPages = 1;
    private List<ParteDto> _allFilteredPartes = new();

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

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("🧹 Iniciando limpieza de DiarioPage...");
            
            // Detener el monitoreo del servicio
            ViewModel.StopServiceMonitoring();

            // 🔧 FIX: Limpiar y destruir timer de debounce completamente
            if (_debounce != null)
            {
                _debounce.Stop();
                // No desuscribir Tick manualmente (se limpia con null)
                _debounce = null;
            }

            // 🔧 FIX: Cancelar cualquier carga en progreso
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;

            // 🆕 NUEVO: Limpiar panel de usuarios online
            try
            {
                if (_usersPanelViewModel != null)
                {
                    UsersPanel.Cleanup();
                    _usersPanelViewModel.Dispose();
                    _usersPanelViewModel = null;
                    App.Log?.LogInformation("✅ Panel de usuarios online limpiado");
                }
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Error limpiando panel de usuarios");
            }

            // 🔧 FIX: Desuscribir eventos de tema para evitar memory leaks
            ThemeService.Instance.ThemeChanged -= OnGlobalThemeChanged;
            
            // 🔧 FIX: Desuscribir evento de fecha
            DpFiltroFecha.DateChanged -= OnFiltroFechaChanged;
            
            // 🔧 FIX: Limpiar ListView para liberar virtualización
            if (LvPartes != null)
            {
                LvPartes.ContainerContentChanging -= OnContainerContentChanging;
                LvPartes.SelectionChanged -= OnPartesSelectionChanged;
                LvPartes.ItemsSource = null;
            }
            
            // 🔧 FIX: Limpiar colecciones
            Partes.Clear();
            _cache30dias.Clear();
            _allFilteredPartes.Clear();
            
            // 🔧 FIX: Limpiar servicio de partes
            _partesService = null;

            App.Log?.LogInformation("✅ DiarioPage Unloaded - Recursos limpiados completamente");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error durante limpieza de DiarioPage");
        }
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

        // Ctrl+I - Importar Excel
        var accelImportar = new KeyboardAccelerator { Key = Windows.System.VirtualKey.I };
        accelImportar.Modifiers = Windows.System.VirtualKeyModifiers.Control;
        accelImportar.Invoked += (s, e) => { OnImportarExcel(this, new RoutedEventArgs()); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelImportar);

        // Ctrl+X - Exportar Excel
        var accelExportar = new KeyboardAccelerator { Key = Windows.System.VirtualKey.X };
        accelExportar.Modifiers = Windows.System.VirtualKeyModifiers.Control;
        accelExportar.Invoked += (s, e) => { OnExportarExcel(this, new RoutedEventArgs()); e.Handled = true; };
        this.KeyboardAccelerators.Add(accelExportar);

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

        // ❌ ELIMINADO: F12 - Configuración (botón removido del UI)

        App.Log?.LogDebug("Atajos de teclado configurados: Ctrl+T, Ctrl+N, Ctrl+E, Ctrl+I, Ctrl+X, Delete, Ctrl+Q, F5");
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

            // 🧪 TEST TEMPORAL: Mostrar notificación al cargar
            App.Notifications?.ShowSuccess("Sistema funcionando correctamente", title: "✅ DiarioPage Cargado");

            // Inicializar tema y assets
            UpdateThemeAssets(this.RequestedTheme);

            // 🆕 NUEVO: Cargar perfil dinámicamente desde API (solo si no está cacheado)
            try
            {
                // Intentar cargar perfil desde cache global primero
                if (App.CurrentUserProfile == null)
                {
                    App.Log?.LogInformation("📥 Cargando perfil del usuario desde API...");
                    
                    try
                    {
                        App.CurrentUserProfile = await App.ProfileService.GetCurrentUserProfileAsync();
                        
                        if (App.CurrentUserProfile != null)
                        {
                            App.Log?.LogInformation("✅ Perfil cargado: {firstName} {lastName} | {phone}", 
                                App.CurrentUserProfile.FirstName, 
                                App.CurrentUserProfile.LastName,
                                App.CurrentUserProfile.Phone);
                        }
                        else
                        {
                            App.Log?.LogWarning("⚠️ Perfil no encontrado en backend, usando datos del login");
                        }
                    }
                    catch (Exception profileEx)
                    {
                        App.Log?.LogWarning(profileEx, "⚠️ Error cargando perfil, usando fallback");
                    }
                }
                
                // Construir información para mostrar en el banner
                string displayName;
                string displayEmail;
                string displayPhone;
                
                if (App.CurrentUserProfile != null)
                {
                    // 📊 Usar datos del perfil completo
                    displayName = $"{App.CurrentUserProfile.FirstName} {App.CurrentUserProfile.LastName}".Trim();
                    displayEmail = App.CurrentLoginEmail ?? App.CurrentUserProfile.FullName ?? "usuario@empresa.com";
                    displayPhone = App.CurrentUserProfile.Phone ?? "";
                    
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = displayEmail.Split('@')[0]; // Fallback: parte local del email
                    }
                }
                else
                {
                    // 📧 Fallback: Usar email del login
                    var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
                    
                    var userName = settings.TryGetValue("UserName", out var nameObj) && nameObj is string name 
                        ? name 
                        : "Usuario";
                        
                    displayName = userName;
                    displayEmail = App.CurrentLoginEmail ?? "usuario@empresa.com";
                    displayPhone = ""; // Sin perfil, no hay teléfono
                }
                
                // Actualizar ViewModel con los datos dinámicos
                ViewModel.DisplayName = displayName;
                ViewModel.DisplayEmail = displayEmail;
                ViewModel.DisplayPhone = displayPhone;
                
                App.Log?.LogInformation("🎨 Banner actualizado: {name} | {email} | {phone}", 
                    displayName, displayEmail, 
                    string.IsNullOrEmpty(displayPhone) ? "(sin teléfono)" : displayPhone);
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error cargando perfil del usuario");
                
                // Fallback seguro
                ViewModel.DisplayName = "Usuario";
                ViewModel.DisplayEmail = App.CurrentLoginEmail ?? "usuario@empresa.com";
                ViewModel.DisplayPhone = "";
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

            // 🆕 MODIFICADO: Determinar si es carga inicial (HOY sin cambios) o filtro específico
            var isToday = selectedDate.Date == DateTime.Today;

            using var loadScope = PerformanceLogger.BeginScope(SpecializedLoggers.Data, "LoadPartes",
                new { IsInitialLoad = _isInitialLoad, SelectedDate = selectedDate });

            SpecializedLoggers.Data.LogInformation("══════════════════════════════════════════════════════════════─");
            SpecializedLoggers.Data.LogInformation("📥 CARGA DE PARTES");

            if (_isInitialLoad && isToday)
            {
                // 🆕 NUEVO: Carga inicial - Últimos 25 partes sin filtro de fecha
                SpecializedLoggers.Data.LogInformation("   • Tipo: CARGA INICIAL - Últimos 25 partes (sin filtro de fecha)");
                SpecializedLoggers.Data.LogInformation("   • Orden: Fecha descendente (más recientes primero)");
                
                await LoadPartesWithLimitAsync(limit: 25, ct);
            }
            else
            {
                // 🆕 CORREGIDO: Fecha específica (incluyendo HOY cuando se selecciona manualmente)
                SpecializedLoggers.Data.LogInformation("   • Tipo: FECHA ESPECÍFICA - {date}", selectedDate.ToString("yyyy-MM-dd"));
                
                await LoadPartesByDateAsync(selectedDate, ct);
            }
        }
        catch (OperationCanceledException)
        {
            SpecializedLoggers.Data.LogInformation("Carga de partes cancelada por el usuario.");
        }
        catch (Exception ex)
        {
            SpecializedLoggers.Data.LogError(ex, "Error cargando partes");
            SpecializedLoggers.Data.LogWarning("La lista quedará vacía. El usuario puede intentar refrescar (F5).");
        }
        finally
        {
            _isLoading = false; // 🆕 NUEVO: Liberar flag
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Carga los últimos N partes ordenados por fecha descendente (sin filtro de fecha)
    /// </summary>
    private async Task LoadPartesWithLimitAsync(int limit, CancellationToken ct)
    {
        try
        {
            // Usar parámetros limit y offset para paginación
            // El backend debe ordenar por fecha_trabajo DESC por defecto
            var path = $"/api/v1/partes?limit={limit}&offset=0";
            
            SpecializedLoggers.Data.LogInformation("📡 Endpoint: GET {path}", path);
            SpecializedLoggers.Data.LogInformation("   • Limit: {limit} registros", limit);
            SpecializedLoggers.Data.LogInformation("   • Offset: 0 (primeros registros)");
            SpecializedLoggers.Data.LogInformation("   • Orden esperado: fecha_trabajo DESC");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await App.Api.GetAsync<List<ParteDto>>(path, ct);
            sw.Stop();

            if (result == null)
            {
                SpecializedLoggers.Data.LogWarning("⚠️ Endpoint devolvió null - Lista vacía");
                _cache30dias = new List<ParteDto>();
            }
            else
            {
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

                // Log de rango de fechas cargadas
                if (_cache30dias.Any())
                {
                    var minFecha = _cache30dias.Min(p => p.Fecha);
                    var maxFecha = _cache30dias.Max(p => p.Fecha);
                    SpecializedLoggers.Data.LogInformation("📅 Rango de fechas: {min} a {max}", 
                        minFecha.ToString("yyyy-MM-dd"), maxFecha.ToString("yyyy-MM-dd"));
                }
            }

            ApplyFilterToListView();
        }
        catch (Exception ex)
        {
            SpecializedLoggers.Data.LogError(ex, "Error cargando partes con limit");
            _cache30dias = new List<ParteDto>();
            ApplyFilterToListView();
            throw;
        }
    }

    /// <summary>
    /// 🆕 NUEVO: Carga partes de una fecha específica
    /// </summary>
    private async Task LoadPartesByDateAsync(DateTime fecha, CancellationToken ct)
    {
        try
        {
            var path = $"/api/v1/partes?fecha={fecha:yyyy-MM-dd}";
            
            SpecializedLoggers.Data.LogInformation("📡 Endpoint: GET {path}", path);
            SpecializedLoggers.Data.LogInformation("   • Fecha específica: {fecha}", fecha.ToString("yyyy-MM-dd"));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await App.Api.GetAsync<List<ParteDto>>(path, ct);
            sw.Stop();

            if (result == null)
            {
                SpecializedLoggers.Data.LogWarning("⚠️ Endpoint devolvió null - Lista vacía");
                _cache30dias = new List<ParteDto>();
            }
            else
            {
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
            }

            ApplyFilterToListView();
        }
        catch (Exception ex)
        {
            SpecializedLoggers.Data.LogError(ex, "Error cargando partes por fecha");
            _cache30dias = new List<ParteDto>();
            ApplyFilterToListView();
            throw;
        }
    }

    // 🔄 MÉTODO LEGACY - Mantener por compatibilidad pero ya no se usa en carga inicial
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
        
        // 🐛 DEBUG TEMPORAL
        System.Diagnostics.Debug.WriteLine($"═══ ApplyFilterToListView ═══");
        System.Diagnostics.Debug.WriteLine($"_cache30dias Count: {_cache30dias.Count}");
        System.Diagnostics.Debug.WriteLine($"Filtro 'q': '{q}'");

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p =>
                DiarioPageHelpers.Has(p.Cliente, q) ||
                DiarioPageHelpers.Has(p.Tienda, q) ||
                DiarioPageHelpers.Has(p.Accion, q) ||
                DiarioPageHelpers.Has(p.Ticket, q) ||
                DiarioPageHelpers.Has(p.Grupo, q) ||
                DiarioPageHelpers.Has(p.Tipo, q) ||
                DiarioPageHelpers.Has(p.Tecnico, q) ||
                DiarioPageHelpers.Has(p.Estado, q)
            );
        }

        query = query
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => DiarioPageHelpers.ParseTime(p.HoraInicio));

        // 🆕 PAGINACIÓN: Guardar todos los resultados filtrados
        _allFilteredPartes = query.ToList();
        
        // 🆕 PAGINACIÓN: Calcular total de páginas
        _totalPages = (_allFilteredPartes.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;
        if (_totalPages < 1) _totalPages = 1;
        
        // 🆕 PAGINACIÓN: Ajustar página actual si está fuera de rango
        if (_currentPage > _totalPages) _currentPage = _totalPages;
        if (_currentPage < 1) _currentPage = 1;
        
        // 🆕 PAGINACIÓN: Obtener solo los items de la página actual
        var pagedItems = _allFilteredPartes
            .Skip((_currentPage - 1) * ITEMS_PER_PAGE)
            .Take(ITEMS_PER_PAGE);

        Partes.Clear();
        foreach (var p in pagedItems)
            Partes.Add(p);
        
        // 🐛 DEBUG TEMPORAL
        System.Diagnostics.Debug.WriteLine($"Total filtrados: {_allFilteredPartes.Count}");
        System.Diagnostics.Debug.WriteLine($"Página actual: {_currentPage}/{_totalPages}");
        System.Diagnostics.Debug.WriteLine($"Mostrando: {Partes.Count} items");
        System.Diagnostics.Debug.WriteLine($"═══════════════════════════");

        App.Log?.LogInformation("Filtro aplicado q='{q}'. Total: {total}, Página: {page}/{totalPages}, Mostrando: {count}",
            q, _allFilteredPartes.Count, _currentPage, _totalPages, Partes.Count);

        // Log de estados en la lista final
        var estadosEnLista = Partes.GroupBy(p => p.EstadoTexto).Select(g => $"{g.Key}:{g.Count()}");
        App.Log?.LogInformation("📊 Estados en ListView: {estados}", string.Join(", ", estadosEnLista));

        // 🆕 NUEVO: Actualizar tooltip de cobertura de tiempo
        UpdateTimeCoverageTooltip();
        
        // 🆕 PAGINACIÓN: Actualizar UI de paginación (si existe)
        UpdatePaginationUI();
    }
    
    /// <summary>
    /// 🆕 PAGINACIÓN: Actualiza la UI de paginación (botones, labels, etc.)
    /// </summary>
    private void UpdatePaginationUI()
    {
        // TODO: Implementar botones Previous/Next/Page numbers en el XAML
        // Por ahora solo logueamos
        App.Log?.LogDebug("📄 Paginación: {current}/{total} páginas", _currentPage, _totalPages);
    }
    
    /// <summary>
    /// 🆕 PAGINACIÓN: Navegar a una página específica
    /// </summary>
    private void GoToPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > _totalPages) return;
        
        _currentPage = pageNumber;
        ApplyFilterToListView();
    }
    
    /// <summary>
    /// 🆕 PAGINACIÓN: Ir a la página siguiente
    /// </summary>
    private void NextPage()
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            ApplyFilterToListView();
        }
    }
    
    /// <summary>
    /// 🆕 PAGINACIÓN: Ir a la página anterior
    /// </summary>
    private void PreviousPage()
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            ApplyFilterToListView();
        }
    }

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

        // 🆕 NUEVO: Capturar el ID ORIGINAL antes de cargar/guardar
        var idOriginal = parte?.Id ?? 0;

        if (parte == null)
            editPage.NewParte();
        else
            editPage.LoadParte(parte);

        var tcs = new TaskCompletionSource<bool>();
        window.Closed += (_, __) => tcs.TrySetResult(editPage.Guardado);
        window.Activate();

        var saved = await tcs.Task;
        
        // 🆕 NUEVO: Log detallado del resultado
        App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        App.Log?.LogInformation("📝 VENTANA CERRADA - Resultado del guardado:");
        App.Log?.LogInformation("   • Guardado: {saved}", saved);
        App.Log?.LogInformation("   • ParteActualizado != null: {hasUpdated}", editPage.ParteActualizado != null);
        if (editPage.ParteActualizado != null)
        {
            App.Log?.LogInformation("   • ParteActualizado.Id: {id}", editPage.ParteActualizado.Id);
        }
        App.Log?.LogInformation("   • ID ORIGINAL (capturado antes): {idOriginal}", idOriginal);
        App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        
        if (saved && editPage.ParteActualizado != null)
        {
            // ✅ OPTIMIZACIÓN: Actualización local sin recargar desde servidor
            var parteActualizado = editPage.ParteActualizado;
            
            App.Log?.LogInformation("💾 Parte guardado - Actualizando lista local SIN recargar desde servidor...");
            App.Log?.LogInformation("   • Parte ID: {id}", parteActualizado.Id);
            App.Log?.LogInformation("   • Cliente: {cliente}", parteActualizado.Cliente);
            App.Log?.LogInformation("   • Grupo: {grupo}", parteActualizado.Grupo);
            App.Log?.LogInformation("   • Tipo: {tipo}", parteActualizado.Tipo);
            
            // ✅ CORREGIDO: Detectar si es creación o edición usando el ID ORIGINAL capturado
            var esNuevo = idOriginal == 0;
            
            App.Log?.LogInformation("🔍 DETECCIÓN DE OPERACIÓN:");
            App.Log?.LogInformation("   • ID ORIGINAL (antes del guardado): {idOriginal}", idOriginal);
            App.Log?.LogInformation("   • ID ACTUAL (después del guardado): {idActual}", parteActualizado.Id);
            App.Log?.LogInformation("   • Es NUEVO: {esNuevo}", esNuevo);
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            
            if (esNuevo)
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
                        (parteActualizado.Fecha == p.Fecha && DiarioPageHelpers.ParseTime(parteActualizado.HoraInicio) > DiarioPageHelpers.ParseTime(p.HoraInicio)))
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
        else
        {
            // 🆕 NUEVO: Log si no se guardó o no hay ParteActualizado
            if (!saved)
            {
                App.Log?.LogInformation("ℹ️ Ventana cerrada sin guardar (Guardado=false)");
            }
            else
            {
                App.Log?.LogWarning("⚠️ Guardado=true pero ParteActualizado es NULL - esto NO debería ocurrir");
            }
        }
    }

    private void ConfigureChildWindow(Microsoft.UI.Xaml.Window window)
    {
        // ✅ Usar WindowSizeManager para ParteItemEdit con REDIMENSIONAMIENTO HABILITADO
        WindowSizeManager.SetChildWindowSize(window,
            typeof(ParteItemEdit),
            WindowSizeManager.ParteEditSize.Width,
            WindowSizeManager.ParteEditSize.Height,
            resizable: true,
            maximizable: true);
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

    private async void OnImportarExcel(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("📊 IMPORTAR EXCEL - Iniciando selector de archivo");

            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".xls");
            picker.FileTypeFilter.Add(".xlsx");

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                App.Log?.LogInformation("Usuario canceló selección de archivo");
                return;
            }

            App.Log?.LogInformation("Archivo seleccionado: {file}", file.Path);

            var dialog = new ImportExcelDialog
            {
                XamlRoot = this.XamlRoot
            };

            await dialog.LoadFileAsync(file.Path);
            await dialog.ShowAsync();

            if (dialog.ImportCompleted)
            {
                App.Log?.LogInformation("Importación completada - Recargando lista de partes...");
                
                App.Notifications?.ShowSuccess(
                    "Los nuevos partes ya están disponibles en la lista",
                    title: "✅ Importación Exitosa");
                
                await ShowLoadingAndReloadAsync();
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en proceso de importación");
            
            App.Notifications?.ShowError(
                $"Error: {ex.Message}",
                title: "❌ Error de Importación");
        }
    }
    
    private async Task ShowLoadingAndReloadAsync()
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
            
            App.Log?.LogInformation("🔄 Mostrando spinner de carga...");
            
            App.Log?.LogInformation("🗑️ Invalidando caché completo de partes...");
            App.Api.ClearGetCache();
            
            _cache30dias.Clear();
            Partes.Clear();
            
            await LoadPartesAsync();
            
            App.Log?.LogInformation("✅ Recarga completada exitosamente");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error durante la recarga");
            
            App.Notifications?.ShowError(
                "Error al recargar los datos",
                title: "❌ Error");
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingOverlay.Visibility = Visibility.Collapsed;
            
            App.Log?.LogInformation("🔄 Spinner de carga ocultado");
        }
    }

    private async void OnExportarExcel(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsBusy)
        {
            App.Log?.LogWarning("⚠️ Exportación ya en proceso, ignorando nueva petición");
            return;
        }

        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("📊 EXPORTAR A EXCEL - Iniciando proceso");
            
            // Paso 1: Calcular semanas disponibles desde los datos actuales
            var weeks = CalculateAvailableWeeks(Partes);
            
            if (!weeks.Any())
            {
                App.Log?.LogWarning("⚠️ No hay datos disponibles para exportar");
                App.Notifications?.ShowWarning(
                    "No hay partes cargados para exportar. Carga datos primero.",
                    title: "⚠️ Sin Datos");
                return;
            }
            
            App.Log?.LogInformation("📅 Semanas disponibles: {count}", weeks.Count);
            
            // Paso 2: Calcular conteo de registros por semana
            var recordCounts = CalculateRecordCountsByWeek(Partes, weeks);
            
            // Paso 3: Mostrar diálogo de selección de semana
            var dialog = new ExportWeekDialog
            {
                XamlRoot = this.XamlRoot
            };
            
            dialog.SetWeeks(weeks, recordCounts);
            
            var result = await dialog.ShowAsync();
            
            if (result != ContentDialogResult.Primary || dialog.SelectedWeek == null)
            {
                App.Log?.LogInformation("❌ Usuario canceló la exportación");
                return;
            }
            
            var selectedWeek = dialog.SelectedWeek;
            App.Log?.LogInformation("✅ Semana seleccionada: {week} (Año: {year}, Semana: {num})",
                selectedWeek.DisplayText, selectedWeek.Year, selectedWeek.WeekNumber);
            
            // Paso 4: Filtrar partes por semana seleccionada
            var partesToExport = Partes
                .Where(p => System.Globalization.ISOWeek.GetWeekOfYear(p.Fecha) == selectedWeek.WeekNumber &&
                           System.Globalization.ISOWeek.GetYear(p.Fecha) == selectedWeek.Year)
                .OrderBy(p => p.Fecha)
                .ThenBy(p => p.HoraInicio)
                .ToList();
            
            App.Log?.LogInformation("📊 Registros a exportar: {count}", partesToExport.Count);
            
            if (!partesToExport.Any())
            {
                App.Log?.LogWarning("⚠️ No hay registros en la semana seleccionada");
                App.Notifications?.ShowWarning(
                    "La semana seleccionada no tiene registros para exportar.",
                    title: "⚠️ Sin Registros");
                return;
            }
            
            // Paso 5: Solicitar ubicación de guardado
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"GestionTime_Semana_{selectedWeek.Year}_{selectedWeek.WeekNumber:D2}"
            };
            
            savePicker.FileTypeChoices.Add("Excel Workbook", new List<string> { ".xlsx" });
            
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
            
            var file = await savePicker.PickSaveFileAsync();
            
            if (file == null)
            {
                App.Log?.LogInformation("❌ Usuario canceló la selección de archivo");
                return;
            }
            
            App.Log?.LogInformation("📁 Archivo destino: {path}", file.Path);
            
            // Paso 6: Exportar (con loader)
            ViewModel.IsBusy = true;
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
            
            App.Log?.LogInformation("📤 Iniciando exportación...");
            
            var exportService = new Services.Export.ExcelExportService();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            await exportService.ExportAsync(partesToExport, file.Path, cts.Token);
            
            App.Log?.LogInformation("✅ Exportación completada exitosamente");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            
            // Notificar éxito
            App.Notifications?.ShowSuccess(
                $"Se exportaron {partesToExport.Count} registros de la {selectedWeek.DisplayText}",
                title: "✅ Exportación Exitosa");
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogWarning("⚠️ Exportación cancelada por timeout o usuario");
            App.Notifications?.ShowWarning(
                "La exportación fue cancelada.",
                title: "⚠️ Cancelado");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error durante la exportación");
            App.Notifications?.ShowError(
                $"Error: {ex.Message}",
                title: "❌ Error de Exportación");
        }
        finally
        {
            ViewModel.IsBusy = false;
            LoadingRing.IsActive = false;
            LoadingOverlay.Visibility = Visibility.Collapsed;
            App.Log?.LogInformation("🔄 Exportación finalizada - Loader ocultado");
        }
    }

    /// <summary>Calcula las semanas disponibles desde los partes cargados.</summary>
    private List<Models.Export.WeekOption> CalculateAvailableWeeks(ObservableCollection<ParteDto> partes)
    {
        if (!partes.Any())
            return new List<Models.Export.WeekOption>();
        
        var weekGroups = partes
            .GroupBy(p => new
            {
                Year = System.Globalization.ISOWeek.GetYear(p.Fecha),
                Week = System.Globalization.ISOWeek.GetWeekOfYear(p.Fecha)
            })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Week)
            .ToList();
        
        var weeks = new List<Models.Export.WeekOption>();
        
        foreach (var group in weekGroups)
        {
            var startDate = System.Globalization.ISOWeek.ToDateTime(group.Key.Year, group.Key.Week, DayOfWeek.Monday);
            var endDate = startDate.AddDays(6);
            
            weeks.Add(new Models.Export.WeekOption(
                group.Key.Year,
                group.Key.Week,
                startDate,
                endDate
            ));
        }
        
        return weeks;
    }

    /// <summary>Calcula el conteo de registros por cada semana.</summary>
    private Dictionary<Models.Export.WeekOption, int> CalculateRecordCountsByWeek(
        ObservableCollection<ParteDto> partes,
        List<Models.Export.WeekOption> weeks)
    {
        var counts = new Dictionary<Models.Export.WeekOption, int>();
        
        foreach (var week in weeks)
        {
            var count = partes.Count(p =>
                System.Globalization.ISOWeek.GetWeekOfYear(p.Fecha) == week.WeekNumber &&
                System.Globalization.ISOWeek.GetYear(p.Fecha) == week.Year);
            
            counts[week] = count;
        }
        
        return counts;
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

            App.Log?.LogInformation("🗑️ Invalidando caché de partes...");
            InvalidatePartesCache(parte.Fecha);

            var removedFromCache = _cache30dias.RemoveAll(p => p.Id == parte.Id);
            App.Log?.LogInformation("🗑️ Eliminados {count} registros de la caché local", removedFromCache);

            var parteEnLista = Partes.FirstOrDefault(p => p.Id == parte.Id);
            if (parteEnLista != null)
            {
                Partes.Remove(parteEnLista);
                App.Log?.LogInformation("🗑️ Parte eliminado de la lista visible");
            }

            await ShowInfoAsync($"✅ Parte {parte.Id} eliminado definitivamente.");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error eliminando parte {id}", parte.Id);
            await ShowInfoAsync($"❌ Error eliminando parte: {ex.Message}");
        }
    }

    private async void OnLogout(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("Usuario solicitó logout desde botón Salir");

            // 🆕 NUEVO: Usar método centralizado de MainWindow
            if (App.MainWindowInstance != null)
            {
                await App.MainWindowInstance.RequestLogoutAsync();
            }
            else
            {
                App.Log?.LogError("❌ MainWindowInstance es null - No se puede ejecutar logout");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error crítico en logout");
        }
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
                         "¿Qué deseas hacer?",
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
    
    private void InvalidatePartesCache(DateTime fecha)
    {
        try
        {
            var fromDate = fecha.AddDays(-30).ToString("yyyy-MM-dd");
            var toDate = fecha.AddDays(30).ToString("yyyy-MM-dd");
            
            var rangePath = $"/api/v1/partes?fechaInicio={fromDate}&fechaFin={toDate}";
            App.Api.InvalidateCacheEntry(rangePath);
            App.Log?.LogDebug("🗑️ Caché invalidado (rango fechaInicio/fechaFin): {path}", rangePath);
            
            var dayPath = $"/api/v1/partes?fecha={fecha:yyyy-MM-dd}";
            App.Api.InvalidateCacheEntry(dayPath);
            App.Log?.LogDebug("🗑️ Caché invalidado (día específico): {path}", dayPath);
            
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
    
    private void UpdateTimeCoverageTooltip()
    {
        try
        {
            var partesConTiempo = Partes
                .Where(p => !string.IsNullOrWhiteSpace(p.HoraInicio))
                .ToList();
            
            var intervals = partesConTiempo
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
                UpdateDuracionHeaderTooltip(null, 0);
                return;
            }
            
            var coverage = IntervalMerger.ComputeCoverage(intervals);
            UpdateDuracionHeaderTooltip(coverage, partesConTiempo.Count);
            
            App.Log?.LogInformation("⏱️ Cobertura calculada - Partes: {count}, Intervalos: {intervals}, Cubierto: {covered}, Solapado: {overlap}",
                partesConTiempo.Count,
                coverage.MergedIntervals.Count,
                IntervalMerger.FormatDuration(coverage.TotalCovered),
                IntervalMerger.FormatDuration(coverage.TotalOverlap));
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error calculando cobertura");
            UpdateDuracionHeaderTooltip(null, 0);
        }
    }
    
    private void UpdateDuracionHeaderTooltip(IntervalMerger.CoverageResult? coverage, int totalPartes)
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
            
            var tooltipText = DiarioPageHelpers.BuildCoverageTooltipText(coverage, totalPartes);
            ToolTipService.SetToolTip(DuracionHeader, tooltipText);
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error actualizando tooltip");
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

            await App.Api.PostAsync($"/api/v1/partes/{parteId}/pause");

            App.Log?.LogInformation("✅ Parte {id} pausado correctamente", parteId);
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");

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
            App.Log?.LogInformation("📋 Estrategia: Confirmar hora cierre → Cerrar parte pausado → Crear nuevo duplicado");
            
            // 1️⃣ NUEVO: Mostrar diálogo para confirmar hora de cierre
            App.Log?.LogInformation("🕐 Solicitando confirmación de hora de cierre...");
            var horaFin = await AskHoraCierreAsync(parte);
            
            if (string.IsNullOrEmpty(horaFin))
            {
                App.Log?.LogInformation("❌ Usuario canceló la reanudación del parte");
                return;
            }
            
            App.Log?.LogInformation("✅ Hora de cierre confirmada: {hora}", horaFin);
            
            // 2️⃣ Cerrar el parte pausado actual con la hora confirmada
            var updatePayload = new Models.Dtos.ParteCreateRequest
            {
                FechaTrabajo = parte.Fecha.ToString("yyyy-MM-dd"),
                HoraInicio = parte.HoraInicio,
                HoraFin = horaFin,
                DuracionMin = CalcularDuracionMinutos(parte.HoraInicio, horaFin),
                IdCliente = parte.IdCliente,
                Tienda = parte.Tienda,
                IdGrupo = parte.IdGrupo,
                IdTipo = parte.IdTipo,
                Accion = parte.Accion,
                Ticket = parte.Ticket,
                Tecnico = parte.Tecnico,
                Estado = 2  // 2 = Cerrado
            };
            
            App.Log?.LogInformation("🔒 Cerrando parte pausado (ID={id}) con HoraFin={hora}...", parteId, horaFin);
            await App.Api.PutAsync<Models.Dtos.ParteCreateRequest, object>($"/api/v1/partes/{parteId}", updatePayload);
            App.Log?.LogInformation("✅ Parte {id} cerrado correctamente", parteId);
            
            // 3️⃣ Crear nuevo parte con los mismos datos
            var nuevoParte = new ParteDto
            {
                Id = 0,
                Fecha = DateTime.Today,
                HoraInicio = horaFin,  // ✅ Usar la hora de cierre confirmada como hora inicio del nuevo
                HoraFin = "",
                Cliente = parte.Cliente,
                Tienda = parte.Tienda,
                Accion = parte.Accion,
                Ticket = parte.Ticket,  // ✅ Mantener ticket
                Grupo = parte.Grupo,
                Tipo = parte.Tipo,
                Tecnico = parte.Tecnico,
                EstadoParte = ParteEstado.Abierto,
                IdCliente = parte.IdCliente,
                IdGrupo = parte.IdGrupo,
                IdTipo = parte.IdTipo
            };
            
            App.Log?.LogInformation("📝 Abriendo editor con nuevo parte (duplicado del {id})...", parteId);
            App.Log?.LogInformation("   • Hora inicio del nuevo parte: {hora}", horaFin);
            await OpenParteEditorAsync(nuevoParte, $"▶️ Reanudar Parte #{parte.Id}");

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
            App.Log?.LogInformation("   • Acción: {accion}", DiarioPageHelpers.TrimForLog(parte.Accion, 50));
            App.Log?.LogInformation("───────────────────────────────────────────────────────────────");

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

            var cierreCorrecto = false;
            var metodoUsado = "";
            var requestStart = System.Diagnostics.Stopwatch.StartNew();

            try
            {
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
                    DiarioPageHelpers.TrimForLog(postEx.ServerMessage ?? postEx.ServerError ?? "(sin respuesta)", 200));
                
                if (postEx.StatusCode == System.Net.HttpStatusCode.BadRequest && 
                    (postEx.Message?.Contains("cerrado", StringComparison.OrdinalIgnoreCase) == true ||
                     postEx.ServerMessage?.Contains("cerrado", StringComparison.OrdinalIgnoreCase) == true))
                {
                    App.Log?.LogInformation("❌ Parte ya está cerrado - Notificando al usuario");
                    
                    App.Notifications?.ShowInfo(
                        "Este parte ya está cerrado. Si necesitas trabajar en él de nuevo, usa la opción 'Duplicar' del menú contextual.",
                        title: "⚠️ Parte Ya Cerrado");
                        
                    App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                    return;
                }
                
                try
                {
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
                        estado = 2
                    };

                    App.Log?.LogInformation("   📡 Endpoint: PUT {endpoint}", putEndpoint);
                    App.Log?.LogInformation("   🌐 URL completa: {url}", fullPutUrl);
                    App.Log?.LogInformation("   ⏳ Enviando petición...");

                    var putStart = System.Diagnostics.Stopwatch.StartNew();

                    await App.Api.PutAsync<object, object>(putEndpoint, putPayload);

                    putStart.Stop();

                    App.Log?.LogInformation("✅ PUT EXITOSO");
                    App.Log?.LogInformation("   ⏱️ Tiempo de respuesta: {ms}ms", putStart.ElapsedMilliseconds);

                    cierreCorrecto = true;
                    metodoUsado = "PUT /partes/{id}";
                }
                catch (ApiException putEx)
                {
                    App.Log?.LogError("❌ PUT TAMBIÉN FALLÓ - Código: {status}", putEx.StatusCode);
                    throw;
                }
                catch (Exception putGenEx)
                {
                    App.Log?.LogError(putGenEx, "❌ PUT falló con error inesperado");
                    throw;
                }
            }
            finally
            {
                requestStart.Stop();
                App.Log?.LogInformation("   ⏱️ Tiempo total de peticiones HTTP: {ms}ms", requestStart.ElapsedMilliseconds);
            }

            if (!cierreCorrecto)
            {
                App.Log?.LogError("❌ CIERRE FALLIDO: No se pudo cerrar el parte {id}", parteId);
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                await ShowInfoAsync($"❌ Error: No se pudo cerrar el parte.\n\nRevisa los logs para más detalles.");
                return;
            }

            App.Log?.LogInformation("───────────────────────────────────────────────────────────────");
            App.Log?.LogInformation("✅ CIERRE EXITOSO usando: {metodo}", metodoUsado);
            App.Log?.LogInformation("───────────────────────────────────────────────────────────────");

            var cacheUpdateStart = System.Diagnostics.Stopwatch.StartNew();
            
            var indexCache = _cache30dias.FindIndex(p => p.Id == parteId);
            if (indexCache >= 0)
            {
                var parteCache = _cache30dias[indexCache];
                parteCache.HoraFin = horaFin;
                parteCache.EstadoInt = 2;
                parteCache.EstadoNombre = "Cerrado";
                parteCache.DuracionMin = CalcularDuracionMinutos(parteCache.HoraInicio, horaFin);
                
                _cache30dias[indexCache] = parteCache;
                App.Log?.LogInformation("   ✅ Parte actualizado en _cache30dias (index: {index})", indexCache);
            }
            else
            {
                App.Log?.LogWarning("   ⚠️ Parte ID {id} no encontrado en _cache30dias", parteId);
            }
            
            var parteEnLista = Partes.FirstOrDefault(p => p.Id == parteId);
            if (parteEnLista != null)
            {
                var indexLista = Partes.IndexOf(parteEnLista);
                
                var parteActualizado = new ParteDto
                {
                    Id = parteEnLista.Id,
                    Fecha = parteEnLista.Fecha,
                    Cliente = parteEnLista.Cliente,
                    Tienda = parteEnLista.Tienda,
                    HoraInicio = parteEnLista.HoraInicio,
                    HoraFin = horaFin,
                    Ticket = parteEnLista.Ticket,
                    Grupo = parteEnLista.Grupo,
                    Tipo = parteEnLista.Tipo,
                    Accion = parteEnLista.Accion,
                    DuracionMin = CalcularDuracionMinutos(parteEnLista.HoraInicio, horaFin),
                    Tecnico = parteEnLista.Tecnico,
                    EstadoInt = 2,
                    EstadoNombre = "Cerrado",
                    IdCliente = parteEnLista.IdCliente,
                    IdGrupo = parteEnLista.IdGrupo,
                    IdTipo = parteEnLista.IdTipo
                };
                
                Partes[indexLista] = parteActualizado;
                App.Log?.LogInformation("   ✅ Parte actualizado en Partes (ObservableCollection, index: {index})", indexLista);
            }
            else
            {
                App.Log?.LogWarning("   ⚠️ Parte ID {id} no encontrado en Partes (ObservableCollection)", parteId);
            }
            
            cacheUpdateStart.Stop();
            App.Log?.LogInformation("   ⏱️ Cache local actualizado en {ms}ms", cacheUpdateStart.ElapsedMilliseconds);

            var parteEndpoint = $"/api/v1/partes/{parteId}";
            if (parteEnLista != null)
            {
                var parteCacheDto = new ParteDto
                {
                    Id = parteEnLista.Id,
                    Fecha = parteEnLista.Fecha,
                    Cliente = parteEnLista.Cliente,
                    Tienda = parteEnLista.Tienda,
                    HoraInicio = parteEnLista.HoraInicio,
                    HoraFin = horaFin,
                    Ticket = parteEnLista.Ticket,
                    Grupo = parteEnLista.Grupo,
                    Tipo = parteEnLista.Tipo,
                    Accion = parteEnLista.Accion,
                    DuracionMin = CalcularDuracionMinutos(parteEnLista.HoraInicio, horaFin),
                    Tecnico = parteEnLista.Tecnico,
                    EstadoInt = 2,
                    EstadoNombre = "Cerrado",
                    IdCliente = parteEnLista.IdCliente,
                    IdGrupo = parteEnLista.IdGrupo,
                    IdTipo = parteEnLista.IdTipo
                };
                
                App.Api.UpdateCacheEntry(parteEndpoint, parteCacheDto);
                App.Log?.LogInformation("   ✅ Cache del ApiClient actualizado: {endpoint}", parteEndpoint);
            }

            stopwatch.Stop();

            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("✅ PROCESO COMPLETADO EXITOSAMENTE");
            App.Log?.LogInformation("   ⏱️ Tiempo total: {ms}ms ({seconds:F2}s)",
                stopwatch.ElapsedMilliseconds, stopwatch.Elapsed.TotalSeconds);
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
                DiarioPageHelpers.TrimForLog(apiEx.ServerError ?? "(sin error)", 1000));
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

    private async Task<string?> AskHoraCierreAsync(ParteDto parte)
    {
        try
        {
            var dialog = new CerrarParteDialog(parte)
            {
                XamlRoot = this.XamlRoot
            };

            App.Log?.LogInformation("🔒 Abriendo diálogo de cierre para parte ID: {id}", parte.Id);

            var result = await dialog.ShowAsync();

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

            // 🆕 MODIFICADO: Copiar TODOS los campos incluyendo Ticket y Tags
            var nuevoParte = new ParteDto
            {
                Id = 0, // Nuevo registro
                Fecha = DateTime.Today, // ⚠️ SIEMPRE HOY (no copiar fecha original)
                HoraInicio = DateTime.Now.ToString("HH:mm"),
                HoraFin = "",
                Cliente = parte.Cliente,
                Tienda = parte.Tienda,
                Accion = parte.Accion,
                Ticket = parte.Ticket, // ✅ COPIAR TICKET
                Grupo = parte.Grupo,
                Tipo = parte.Tipo,
                EstadoParte = ParteEstado.Abierto, // Estado inicial: Abierto
                IdCliente = parte.IdCliente,
                IdGrupo = parte.IdGrupo,
                IdTipo = parte.IdTipo,
                // ✅ COPIAR TAGS con deep copy (nueva lista)
                Tags = parte.Tags != null ? new List<string>(parte.Tags) : new List<string>()
            };

            App.Log?.LogInformation("📋 Parte duplicado creado:");
            App.Log?.LogInformation("   • Cliente: {cliente}", nuevoParte.Cliente);
            App.Log?.LogInformation("   • Tienda: {tienda}", nuevoParte.Tienda);
            App.Log?.LogInformation("   • Ticket: {ticket}", nuevoParte.Ticket ?? "(vacío)");
            App.Log?.LogInformation("   • Tags: {tags}", nuevoParte.Tags != null ? string.Join(", ", nuevoParte.Tags) : "(sin tags)");
            App.Log?.LogInformation("   • Fecha: {fecha} (HOY)", nuevoParte.Fecha.ToString("yyyy-MM-dd"));

            App.Log?.LogInformation("📝 Abriendo editor con parte duplicado (ID=0 indica NUEVO)...");
            await OpenParteEditorAsync(nuevoParte, $"📋 Duplicar Parte #{parte.Id}");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error duplicando parte {id}", parteId);
            await ShowInfoAsync($"❌ Error duplicando parte: {ex.Message}");
        }
    }

    private void OnMiPerfilClick(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("👤 MI PERFIL - Navegando a UserProfilePage");
            App.MainWindowInstance?.Navigator?.Navigate(typeof(UserProfilePage));
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error navegando a UserProfilePage");
        }
    }

    private static int CalcularDuracionMinutos(string? horaInicio, string? horaFin)
    {
        if (string.IsNullOrWhiteSpace(horaInicio) || string.IsNullOrWhiteSpace(horaFin))
            return 0;
        
        if (!TimeSpan.TryParse(horaInicio, out var inicio))
            return 0;
        
        if (!TimeSpan.TryParse(horaFin, out var fin))
            return 0;
        
        var duracion = fin - inicio;
        
        if (duracion.TotalMinutes < 0)
            duracion = duracion.Add(TimeSpan.FromDays(1));
        
        return (int)Math.Round(duracion.TotalMinutes);
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

    private void OnSalir(object sender, RoutedEventArgs e)
    {
        OnLogout(sender, e);
    }

    // ===================== AYUDA Y NOTAS DE VERSIÓN =====================

    private async void OnNotasVersionClick(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("📋 Mostrando notas de versión...");

            var dialog = new ContentDialog
            {
                Title = $"📋 Notas de Versión - GestionTime Desktop {VersionInfo.VersionWithPrefix}",
                Content = CreateChangelogContent(),
                PrimaryButtonText = "Ver en GitHub",
                CloseButtonText = "Cerrar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Usuario hizo clic en "Ver en GitHub"
                App.Log?.LogInformation("🔗 Abriendo GitHub releases en navegador...");
                
                var uri = new Uri("https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases");
                _ = Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error mostrando notas de versión");
            await ShowInfoAsync("Error mostrando notas de versión. Revisa app.log.");
        }
    }

    /// <summary>Abre o cierra el panel lateral de usuarios online.</summary>
    private void OnToggleUsersPanel(object sender, RoutedEventArgs e)
    {
        try
        {
            var isOpen = MainSplitView.IsPaneOpen;

            if (!isOpen)
            {
                // Abrir panel
                App.Log?.LogInformation("📂 Abriendo panel de usuarios online integrado");

                // Inicializar ViewModel si es primera vez
                if (_usersPanelViewModel == null)
                {
                    _usersPanelViewModel = new OnlineUsersPanelViewModel(DispatcherQueue);
                    UsersPanel.Initialize(_usersPanelViewModel);
                    App.Log?.LogInformation("✅ Panel de usuarios inicializado");
                }

                MainSplitView.IsPaneOpen = true;
            }
            else
            {
                // Cerrar panel
                App.Log?.LogInformation("🔒 Cerrando panel de usuarios online integrado");
                MainSplitView.IsPaneOpen = false;
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error toggling panel de usuarios");
        }
    }

    private ScrollViewer CreateChangelogContent()
    {
        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 500
        };

        var stackPanel = new StackPanel
        {
            Padding = new Thickness(20),
            Spacing = 16
        };

        // Header
        var headerText = new TextBlock
        {
            Text = $"🎉 Novedades de la Versión {VersionInfo.Version}",
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        stackPanel.Children.Add(headerText);

        var subtitleText = new TextBlock
        {
            Text = "En desarrollo • Próximo lanzamiento",
            FontSize = 12,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            Margin = new Thickness(0, 4, 0, 0)
        };
        stackPanel.Children.Add(subtitleText);

        // Importación Excel Mejorada
        var importBorder = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 26, 26, 26)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 51, 51, 51)),
            BorderThickness = new Thickness(1)
        };

        var importStack = new StackPanel { Spacing = 12 };
        
        var importTitle = new TextBlock
        {
            Text = "✨ Importación Excel Mejorada",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        importStack.Children.Add(importTitle);

        importStack.Children.Add(CreateFeatureText("• Detección automática de duplicados", "Valida por fecha + hora + cliente + acción"));
        importStack.Children.Add(CreateFeatureText("• Actualización inteligente", "Los duplicados se actualizan en lugar de duplicarse"));
        importStack.Children.Add(CreateFeatureText("• Soporte para columna INCIDENCIA", "Ahora acepta INCIDENCIA como alias de Ticket"));
        importStack.Children.Add(CreateFeatureText("• Grupo y Tipo opcionales", "No genera error si no se encuentran en el catálogo"));
        importStack.Children.Add(CreateFeatureText("• Estadísticas detalladas", "Muestra: X nuevos, Y actualizados, Z errores"));

        importBorder.Child = importStack;
        stackPanel.Children.Add(importBorder);

        // Reanudar Parte Mejorado
        var resumeBorder = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 26, 26, 26)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 51, 51, 51)),
            BorderThickness = new Thickness(1)
        };

        var resumeStack = new StackPanel { Spacing = 12 };
        
        var resumeTitle = new TextBlock
        {
            Text = "▶️ Reanudar Parte Mejorado",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        resumeStack.Children.Add(resumeTitle);

        resumeStack.Children.Add(CreateFeatureText("• Confirmación de hora de cierre", "Solicita la hora antes de cerrar el parte pausado"));
        resumeStack.Children.Add(CreateFeatureText("• Crea nuevo parte duplicado", "Mantiene todos los datos (ticket, cliente, acción, etc.)"));
        resumeStack.Children.Add(CreateFeatureText("• Hora inicio = Hora cierre anterior", "Continuidad perfecta entre sesiones de trabajo"));

        resumeBorder.Child = resumeStack;
        stackPanel.Children.Add(resumeBorder);

        // Link a GitHub
        var githubBorder = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 37, 37, 37)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 59, 130, 246)),
            BorderThickness = new Thickness(2)
        };

        var githubStack = new StackPanel { Spacing = 8 };
        
        var githubTitle = new TextBlock
        {
            Text = "🔗 Más Información",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        githubStack.Children.Add(githubTitle);

        var githubDesc = new TextBlock
        {
            Text = "Consulta el historial completo de cambios en GitHub",
            FontSize = 12,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
        };
        githubStack.Children.Add(githubDesc);

        githubBorder.Child = githubStack;
        stackPanel.Children.Add(githubBorder);

        // Versión actual
        var versionText = new TextBlock
        {
            Text = $"Versión actual: {VersionInfo.Version}",
            FontSize = 12,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        };
        stackPanel.Children.Add(versionText);

        scrollViewer.Content = stackPanel;
        return scrollViewer;
    }

    private StackPanel CreateFeatureText(string title, string description)
    {
        var stack = new StackPanel();
        
        var titleText = new TextBlock
        {
            Text = title,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        
        var descText = new TextBlock
        {
            Text = $"  {description}",
            FontSize = 12,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };
        
        stack.Children.Add(titleText);
        stack.Children.Add(descText);
        
        return stack;
    }
}
