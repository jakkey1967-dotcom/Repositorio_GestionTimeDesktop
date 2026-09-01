using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Models.Export;
using GestionTime.Desktop.Models.Enums;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.ViewModels;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Services.Catalog;  // 🆕 NUEVO: Usar PartesService
using GestionTime.Desktop.Services.Export;
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
    private int _containerLogCounter;
    private const bool DiagSkipInitialListRender = true;
    private const bool DiagDisableContainerContentChanging = true;

    private List<ParteDto> _cache30dias = new();
    private DispatcherTimer? _debounce;
    private CancellationTokenSource? _loadCts;
    private bool _isLoading = false; // 🆕 NUEVO: Flag para evitar llamadas concurrentes
    private bool _isInitialLoad = true; // 🆕 NUEVO: Flag para evitar carga automática en constructor
    private OnlineUsersPanelViewModel? _usersPanelViewModel; // 🆕 NUEVO: ViewModel del panel de usuarios
    
    // 🆕 NUEVO: Servicio de partes (lazy loading)
    private PartesService? _partesService;
    private PartesService PartesService => _partesService ??= new PartesService(App.Api, App.Log!);
    
    private const int HistoryPageSize = 30;
    private int _historyOffset;
    private int _loadedBatchCount;
    private bool _hasMoreItems;
    private bool _isLoadingMore;
    private DateTime _anchorDate = DateTime.Today;
    private string _activeSearch = string.Empty;
    private long _activeQueryVersion;
    private readonly List<int> _batchSizes = new();
    private readonly HashSet<int> _loadedParteIds = new();
    private bool _isUpdatingAgentCombo;
    private AgentFilterItem? _selectedAgentFilter;
    private readonly List<AgentFilterItem> _availableAgents = new();

    // GT-BEGIN: Filtros avanzados con pills
    private readonly List<(string Category, string Value)> _activeFilters = new();
    // GT-END

    public DiarioViewModel ViewModel { get; } = new();
    
    /// <summary>Indica si el usuario actual tiene rol USER.</summary>
    public bool IsUserRole
    {
        get
        {
            // TODO: Obtener rol real desde backend cuando esté disponible
            // Por ahora, siempre devolver true para mostrar "Configuración"
            return true;
        }
    }

    private bool IsAdminSession => App.CurrentAuthenticatedUser?.IsAdmin == true;
    private Guid? AuthenticatedUserId => App.CurrentAuthenticatedUser?.UserId;
    private bool IsViewingOwnAgent =>
        !IsAdminSession
        || _selectedAgentFilter == null
        || (_selectedAgentFilter.IsSelf && AuthenticatedUserId.HasValue);
    private bool IsReadOnlyAgentView => IsAdminSession && !IsViewingOwnAgent;
    private bool CanMutateCurrentView => !IsReadOnlyAgentView;

    public DiarioPage()
    {
        this.InitializeComponent();
        this.DataContext = ViewModel;

        LvPartes.ItemsSource = Partes;

        // 🆕 NUEVO: Aplicar tema global
        ThemeService.Instance.ApplyTheme(this);
        UpdateThemeAssets(ThemeService.Instance.CurrentTheme);
        UpdateThemeToggleIcon();

        // 🆕 CORREGIDO: Establecer fecha SIN disparar el evento DateChanged
        DpFiltroFecha.Date = DateTimeOffset.Now;

        // GT-BEGIN: Calendario semana desde lunes
        CalendarFirstDayHelper.Attach(DpFiltroFecha);
        // GT-END

        // 🆕 NUEVO: Suscribir el evento DESPUÉS de establecer la fecha inicial
        DpFiltroFecha.DateChanged += OnFiltroFechaChanged;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _debounce.Tick += async (_, __) =>
        {
            _debounce!.Stop();
            await LoadPartesAsync(reset: true);
        };

        InitializeIcons();
        InitializeKeyboardAccelerators();

        if (!DiagDisableContainerContentChanging)
        {
            LvPartes.ContainerContentChanging += OnContainerContentChanging;
        }
        else
        {
            App.Log?.LogWarning("🧪 DIAG: ContainerContentChanging desactivado temporalmente");
        }

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
            UpdateThemeToggleIcon();
            App.Log?.LogDebug("🎨 DiarioPage: Tema actualizado por cambio global a {theme}", theme);
        });
    }

    /// <summary>
    /// 🆕 NUEVO: Aplica zebra rows dinámicamente usando e.ItemIndex
    /// Se ejecuta en cada render/reciclado para mantener el patrón correcto con virtualización
    /// </summary>
    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        try
        {
            if (args.ItemContainer is ListViewItem container)
            {
                var isEvenRow = args.ItemIndex % 2 == 0;
                var brushKey = isEvenRow ? "EvenRowBrush" : "OddRowBrush";
                var rowBrush = Resources[brushKey] as SolidColorBrush;

                if (rowBrush is null)
                {
                    App.Log?.LogError("❌ OnContainerContentChanging: recurso {brushKey} no encontrado (ItemIndex={index}, InRecycleQueue={recycle})",
                        brushKey, args.ItemIndex, args.InRecycleQueue);
                    return;
                }

                container.Background = rowBrush;

                var currentCount = Interlocked.Increment(ref _containerLogCounter);
                if (currentCount <= 40)
                {
                    var itemType = args.Item?.GetType().FullName ?? "<null>";
                    App.Log?.LogDebug("🧩 ContainerContentChanging #{count}: ItemIndex={index}, InRecycleQueue={recycle}, ItemType={itemType}, Phase={phase}",
                        currentCount, args.ItemIndex, args.InRecycleQueue, itemType, args.Phase);
                }
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Excepción en OnContainerContentChanging (ItemIndex={index}, InRecycleQueue={recycle}, Phase={phase})",
                args.ItemIndex, args.InRecycleQueue, args.Phase);
            throw;
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
            ResetHistoryState();
            
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
        UpdateThemeToggleIcon();
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
        accelRefrescar.Invoked += async (s, e) => { await LoadPartesAsync(reset: true); e.Handled = true; };
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

            // Inicializar tema y assets
            UpdateThemeAssets(this.RequestedTheme);

            // 🔥 FIX DEFINITIVO: Usar mismo dato que la notificación (UserInfo del login)
            try
            {
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                App.Log?.LogInformation("🔄 BANNER: Cargando desde UserInfoFileStorage (login response)");
                
                // ✅ Cargar datos guardados en el login (MISMOS que usa la notificación)
                var userInfo = UserInfoFileStorage.LoadUserInfo(App.Log);
                
                if (userInfo != null)
                {
                    ViewModel.DisplayName = userInfo.UserName ?? "";
                    ViewModel.DisplayEmail = userInfo.UserEmail ?? App.CurrentLoginEmail ?? "usuario@empresa.com";
                    ViewModel.DisplayPhone = ""; // Sin teléfono (no viene en login response)
                    
                    App.Log?.LogInformation("✅ Banner configurado desde login response:");
                    App.Log?.LogInformation("   • DisplayName: {name}", ViewModel.DisplayName);
                    App.Log?.LogInformation("   • DisplayEmail: {email}", ViewModel.DisplayEmail);
                }
                else
                {
                    App.Log?.LogWarning("⚠️ UserInfo no disponible - Usando fallback");
                    ViewModel.DisplayName = "";
                    ViewModel.DisplayEmail = App.CurrentLoginEmail ?? "usuario@empresa.com";
                    ViewModel.DisplayPhone = "";
                }
                
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error configurando banner");
                ViewModel.DisplayName = "";
                ViewModel.DisplayEmail = App.CurrentLoginEmail ?? "usuario@empresa.com";
                ViewModel.DisplayPhone = "";
            }

            // GT-BEGIN: Aislamiento de transición inicial para evitar crash WinUI nativo
            if (RootGrid != null)
                RootGrid.Opacity = 1;
            // GT-END

            UpdateWeekLabel();
            await InitializeAgentFilterAsync();

            // 🆕 NUEVO: Cargar datos y DESPUÉS habilitar el evento de fecha
            await LoadPartesAsync();

            // Habilitar el evento de cambio de fecha DESPUÉS de la carga inicial
            _isInitialLoad = false;
            App.Log?.LogDebug("✅ Carga inicial completada - Evento de fecha habilitado");

            // 🧪 DIAG: Al haber omitido el render inicial, forzar repintado cuando la UI ya está estable
            DispatcherQueue.TryEnqueue(() =>
            {
                App.Log?.LogDebug("🧪 DIAG: Reaplicando filtro tras fin de carga inicial para poblar ListView");
                ApplyFilterToListView();
            });

            DispatcherQueue.TryEnqueue(() =>
            {
                App.Log?.LogDebug("✅ DiarioPage post-load en DispatcherQueue (UI estable)");
            });

            _ = Task.Delay(150).ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    App.Log?.LogDebug("✅ DiarioPage post-load +150ms en hilo UI");
                });
            });
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnPageLoaded()");
        }
    }

    private async Task LoadPartesAsync(bool reset = true)
    {
        if (reset)
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();
            ResetHistoryState();
            LvPartes.SelectedItem = null;
            Partes.Clear();
            _cache30dias.Clear();
        }
        else if (_isLoadingMore || !_hasMoreItems)
        {
            return;
        }

        var ct = _loadCts?.Token ?? CancellationToken.None;
        var queryVersion = ++_activeQueryVersion;
        _anchorDate = DpFiltroFecha.Date?.DateTime.Date ?? DateTime.Today;
        _activeSearch = (TxtFiltroQ.Text ?? string.Empty).Trim();
        UpdateWeekLabel();

        if (reset)
            _isLoading = true;
        else
            _isLoadingMore = true;

        UpdatePaginationUI();

        try
        {
            using var loadScope = PerformanceLogger.BeginScope(SpecializedLoggers.Data, "LoadPartes",
                new { AnchorDate = _anchorDate, Offset = _historyOffset, Search = _activeSearch, ReadOnly = IsReadOnlyAgentView });

            Guid? requestedAgentId = null;
            if (IsAdminSession &&
                _selectedAgentFilter != null &&
                !_selectedAgentFilter.IsSelf &&
                _selectedAgentFilter.Id != Guid.Empty &&
                _selectedAgentFilter.Id != AuthenticatedUserId)
            {
                requestedAgentId = _selectedAgentFilter.Id;
            }

            var searchPreview = _activeSearch.Length <= 40 ? _activeSearch : _activeSearch[..40] + "…";
            SpecializedLoggers.Data.LogInformation(
                "📥 Carga de partes: scope={scope}, targetAgentId={agent}, fechaFin={date}, limit={limit}, offset={offset}, q='{q}'",
                requestedAgentId.HasValue ? "other-agent" : "self",
                requestedAgentId,
                _anchorDate.ToString("yyyy-MM-dd"),
                HistoryPageSize,
                _historyOffset,
                searchPreview);

            var page = await PartesService.ListAsync(
                fechaFin: _anchorDate,
                search: string.IsNullOrWhiteSpace(_activeSearch) ? null : _activeSearch,
                limit: HistoryPageSize,
                offset: _historyOffset,
                agentId: requestedAgentId,
                ct: ct) ?? new List<ParteDto>();

            if (ct.IsCancellationRequested || queryVersion != _activeQueryVersion)
                return;

            var ordered = page
                .Where(p => p.Fecha.Date <= _anchorDate.Date)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderByDescending(p => p.Fecha)
                .ThenByDescending(p => DiarioPageHelpers.ParseTime(p.HoraInicio))
                .ThenByDescending(p => p.Id)
                .ToList();

            var added = 0;
            foreach (var parte in ordered)
            {
                if (!_loadedParteIds.Add(parte.Id))
                    continue;
                _cache30dias.Add(parte);
                added++;
            }

            _batchSizes.Add(added);
            _loadedBatchCount = _batchSizes.Count;
            _historyOffset += page.Count;
            _hasMoreItems = page.Count >= HistoryPageSize;

            if (!reset && page.Count == 0)
            {
                _hasMoreItems = false;
                App.Notifications?.ShowInfo("No hay más registros anteriores.", title: "Historial");
            }

            ApplyFilterToListView();
            UpdateMutationUiState();
        }
        catch (OperationCanceledException)
        {
            SpecializedLoggers.Data.LogInformation("Carga de partes cancelada por el usuario.");
        }
        catch (ApiException apiEx)
        {
            SpecializedLoggers.Data.LogWarning("HTTP {code} cargando partes (sin fallback al usuario autenticado)", (int)apiEx.StatusCode);
            _hasMoreItems = false;
            UpdateMutationUiState();

            if (apiEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                App.Notifications?.ShowWarning(
                    "No tienes autorización para consultar los partes de este agente",
                    title: "Acceso denegado");
            }
            else if (apiEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                App.Notifications?.ShowWarning(
                    "El agente seleccionado no es válido",
                    title: "Consulta inválida");
            }
            else if (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw;
            }
            else
            {
                App.Notifications?.ShowError(
                    "No se pudieron cargar los partes. Inténtalo de nuevo.",
                    title: "Error de carga");
            }
        }
        catch (Exception ex)
        {
            SpecializedLoggers.Data.LogError(ex, "Error cargando partes");
            SpecializedLoggers.Data.LogWarning("La lista quedará vacía. El usuario puede intentar refrescar (F5).");
        }
        finally
        {
            _isLoading = false;
            _isLoadingMore = false;
            UpdatePaginationUI();
        }
    }

    private void ResetHistoryState()
    {
        _historyOffset = 0;
        _loadedBatchCount = 0;
        _hasMoreItems = false;
        _isLoadingMore = false;
        _batchSizes.Clear();
        _loadedParteIds.Clear();
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
        System.Diagnostics.Debug.WriteLine($"Filtro texto libre 'q': '{q}'");
        System.Diagnostics.Debug.WriteLine($"Pills activos: {_activeFilters.Count}");
        foreach (var (cat, val) in _activeFilters)
            System.Diagnostics.Debug.WriteLine($"  🏷️ {cat}: {val}");
        App.Log?.LogDebug("📋 ApplyFilterToListView - q='{q}', pills={pills}, cache={cache}",
            q, _activeFilters.Count, _cache30dias.Count);

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
                DiarioPageHelpers.Has(p.Estado, q) ||
                p.Tags.Any(t => DiarioPageHelpers.Has(t, q))
            );
        }

        // GT-BEGIN: Filtros por pills activos (AND)
        foreach (var (category, value) in _activeFilters)
        {
            var v = value;
            query = category switch
            {
                "Cliente" => query.Where(p => DiarioPageHelpers.Has(p.Cliente, v)),
                "Grupo" => query.Where(p => DiarioPageHelpers.Has(p.Grupo, v)),
                "Tipo" => query.Where(p => DiarioPageHelpers.Has(p.Tipo, v)),
                "Ticket" => query.Where(p => DiarioPageHelpers.Has(p.Ticket, v)),
                "Tags" => query.Where(p => p.Tags.Any(t => DiarioPageHelpers.Has(t, v))),
                _ => query
            };
        }
        // GT-END

        query = query
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => DiarioPageHelpers.ParseTime(p.HoraInicio))
            .ThenByDescending(p => p.Id);

        var visibleItems = query.ToList();
        SyncVisiblePartes(visibleItems);

        App.Log?.LogInformation("Historial visible q='{q}'. Mostrando: {count}, bloques={batches}, hasMore={hasMore}",
            q, Partes.Count, _loadedBatchCount, _hasMoreItems);

        UpdateTimeCoverageTooltip();
        UpdatePaginationUI();
    }

    private void SyncVisiblePartes(List<ParteDto> visibleItems)
    {
        var visibleIds = visibleItems.Select(p => p.Id).ToHashSet();
        for (var i = Partes.Count - 1; i >= 0; i--)
        {
            if (!visibleIds.Contains(Partes[i].Id))
                Partes.RemoveAt(i);
        }

        var currentIds = Partes.Select(p => p.Id).ToHashSet();
        foreach (var parte in visibleItems)
        {
            if (currentIds.Add(parte.Id))
                Partes.Add(parte);
        }
    }

    /// <summary>Actualiza la visibilidad de ChevronUp/ChevronDown sin números de página.</summary>
    private void UpdatePaginationUI()
    {
        var showLess = _loadedBatchCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        var showMore = _hasMoreItems ? Visibility.Visible : Visibility.Collapsed;

        if (PnlShowLessHistory != null)
            PnlShowLessHistory.Visibility = showLess;
        if (BtnShowLessHistory != null)
            BtnShowLessHistory.Visibility = showLess;

        if (PnlLoadMoreHistory != null)
            PnlLoadMoreHistory.Visibility = showMore;
        if (BtnLoadMoreHistory != null)
        {
            BtnLoadMoreHistory.Visibility = showMore;
            BtnLoadMoreHistory.IsEnabled = !_isLoadingMore;
        }

        if (IconLoadMoreHistory != null)
            IconLoadMoreHistory.Visibility = _isLoadingMore ? Visibility.Collapsed : Visibility.Visible;

        if (RingLoadMoreHistory != null)
        {
            RingLoadMoreHistory.Visibility = _isLoadingMore ? Visibility.Visible : Visibility.Collapsed;
            RingLoadMoreHistory.IsActive = _isLoadingMore;
        }
    }

    private async void OnLoadMoreHistoryClick(object sender, RoutedEventArgs e)
    {
        if (_isLoadingMore || !_hasMoreItems)
            return;

        await LoadPartesAsync(reset: false);
    }

    private void OnShowLessHistoryClick(object sender, RoutedEventArgs e)
    {
        if (_loadedBatchCount <= 1 || _batchSizes.Count <= 1)
            return;

        var lastBatchSize = _batchSizes[^1];
        _batchSizes.RemoveAt(_batchSizes.Count - 1);
        _loadedBatchCount = _batchSizes.Count;
        _historyOffset = Math.Max(0, _historyOffset - HistoryPageSize);
        _hasMoreItems = true;

        if (lastBatchSize > 0 && _cache30dias.Count >= lastBatchSize)
        {
            var removed = _cache30dias.GetRange(_cache30dias.Count - lastBatchSize, lastBatchSize);
            _cache30dias.RemoveRange(_cache30dias.Count - lastBatchSize, lastBatchSize);
            foreach (var parte in removed)
                _loadedParteIds.Remove(parte.Id);
        }

        ApplyFilterToListView();
        UpdatePaginationUI();
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

        App.Log?.LogInformation("📅 Usuario cambió fecha de anclaje - Recargando historial...");
        UpdateWeekLabel();
        await LoadPartesAsync(reset: true);
    }

    // GT-BEGIN: Búsqueda avanzada con sugerencias y pills
    private void OnFiltroQChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        App.Log?.LogDebug("🔍 OnFiltroQChanged - Reason: {reason}, Text: '{text}'", args.Reason, sender.Text);

        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            UpdateFilterSuggestions(sender.Text);
            _debounce?.Stop();
            _debounce?.Start();
        }
    }

    /// <summary>Genera sugerencias categorizadas desde el caché local.</summary>
    private void UpdateFilterSuggestions(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
        {
            TxtFiltroQ.ItemsSource = null;
            return;
        }

        var q = text.Trim();
        var suggestions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCategory(string cat, IEnumerable<string?> values)
        {
            foreach (var v in values
                .Where(v => !string.IsNullOrWhiteSpace(v) && v!.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3))
            {
                var key = $"{cat}: {v}";
                if (seen.Add(key)) suggestions.Add(key);
            }
        }

        AddCategory("Cliente", _cache30dias.Select(p => p.Cliente));
        AddCategory("Grupo", _cache30dias.Select(p => p.Grupo));
        AddCategory("Tipo", _cache30dias.Select(p => p.Tipo));
        AddCategory("Ticket", _cache30dias.Select(p => p.Ticket));
        AddCategory("Tags", _cache30dias.SelectMany(p => p.Tags));

        TxtFiltroQ.ItemsSource = suggestions.Count > 0 ? suggestions : null;
    }

    /// <summary>Cuando el usuario selecciona una sugerencia o pulsa Enter.</summary>
    private void OnFilterQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _debounce?.Stop();

        App.Log?.LogDebug("🎯 OnFilterQuerySubmitted - ChosenSuggestion: '{chosen}', QueryText: '{query}'",
            args.ChosenSuggestion, args.QueryText);

        if (args.ChosenSuggestion is string suggestion)
        {
            // ✅ Caso 1: Usuario seleccionó una sugerencia del dropdown (clic o Enter en item)
            App.Log?.LogDebug("🏷️ Sugerencia seleccionada: '{suggestion}'", suggestion);

            var colonIdx = suggestion.IndexOf(':');
            if (colonIdx > 0)
            {
                var category = suggestion[..colonIdx].Trim();
                var value = suggestion[(colonIdx + 1)..].Trim();

                App.Log?.LogDebug("🏷️ Pill → Categoría: '{cat}', Valor: '{val}'", category, value);

                if (!_activeFilters.Any(f => f.Category == category && f.Value.Equals(value, StringComparison.OrdinalIgnoreCase)))
                {
                    _activeFilters.Add((category, value));
                    RebuildFilterPillsUI();
                }

                sender.Text = "";
                sender.ItemsSource = null;
                _ = LoadPartesAsync(reset: true);
            }
        }
        else
        {
            // ✅ Caso 2: Usuario pulsó Enter sin seleccionar sugerencia → filtro libre
            var freeText = (args.QueryText ?? sender.Text ?? "").Trim();
            App.Log?.LogDebug("⌨️ Enter sin sugerencia - texto libre: '{text}'", freeText);

            sender.ItemsSource = null;
            _ = LoadPartesAsync(reset: true);
        }
    }

    /// <summary>Reconstruye los pills visuales desde _activeFilters.</summary>
    private void RebuildFilterPillsUI()
    {
        PnlFilterPills.Children.Clear();

        foreach (var (category, value) in _activeFilters)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            sp.Children.Add(new TextBlock
            {
                Text = $"{category}: {value}",
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            });

            var closeBtn = new Button
            {
                Content = "✕",
                FontSize = 10,
                Padding = new Thickness(4, 2, 4, 2),
                MinWidth = 0,
                MinHeight = 0,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                BorderThickness = new Thickness(0),
                Tag = $"{category}:{value}"
            };
            closeBtn.Click += OnRemoveFilterPill;
            sp.Children.Add(closeBtn);

            var pill = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 15, 167, 182)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 4, 4),
                Child = sp
            };

            PnlFilterPills.Children.Add(pill);
        }
    }

    /// <summary>Elimina un pill de filtro activo.</summary>
    private void OnRemoveFilterPill(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            var colonIdx = tag.IndexOf(':');
            if (colonIdx > 0)
            {
                var category = tag[..colonIdx];
                var value = tag[(colonIdx + 1)..];
                _activeFilters.RemoveAll(f => f.Category == category && f.Value == value);
                RebuildFilterPillsUI();
                _ = LoadPartesAsync(reset: true);
            }
        }
    }
    // GT-END: Búsqueda avanzada con sugerencias y pills

    private async void OnRefrescar(object sender, RoutedEventArgs e)
    {
        App.Log?.LogInformation("🔄 Botón REFRESCAR presionado - Restaurando vista inicial");

        // 🆕 NUEVO: Invalidar TODO el caché antes de recargar
        App.Log?.LogInformation("🗑️ Invalidando caché completo de partes...");
        App.Api.ClearGetCache(); // Limpia TODA la caché de GET (es más seguro que invalidar solo un rango)
        App.Log?.LogInformation("✅ Caché de API limpiado completamente");
        
        // Limpiar caché local y filtros activos
        _cache30dias.Clear();
        Partes.Clear();
        _activeFilters.Clear();
        RebuildFilterPillsUI();
        TxtFiltroQ.Text = "";
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
        UpdateMutationUiState();
    }

    private void UpdateWeekLabel()
    {
        var selectedDate = DpFiltroFecha.Date?.DateTime.Date ?? DateTime.Today;
        TxtSemanaIso.Text = IsoWeekRangeHelper.GetWeekLabel(selectedDate);
        ToolTipService.SetToolTip(TxtSemanaIso, IsoWeekRangeHelper.GetWeekTooltip(selectedDate));
    }

    private void UpdateMutationUiState()
    {
        var canMutate = CanMutateCurrentView;
        var hasSelection = canMutate && LvPartes.SelectedItem != null;
        BtnTelefono.IsEnabled = canMutate;
        BtnNuevo.IsEnabled = canMutate;
        BtnEditar.IsEnabled = hasSelection;
        BtnBorrar.IsEnabled = hasSelection;
        BtnImportar.IsEnabled = canMutate;
        BtnExportar.IsEnabled = canMutate;

        BannerReadOnlyAgent.Visibility = IsReadOnlyAgentView ? Visibility.Visible : Visibility.Collapsed;
        if (IsReadOnlyAgentView)
            TxtReadOnlyAgent.Text = $"Modo solo lectura: viendo los partes de {_selectedAgentFilter?.FullName}";
    }

    private bool EnsureCanMutate()
    {
        if (CanMutateCurrentView)
            return true;

        App.Notifications?.ShowWarning(
            "Estás viendo los datos de otro agente en modo solo lectura",
            title: "👁️ Solo lectura");
        return false;
    }

    private async Task InitializeAgentFilterAsync()
    {
        var session = App.CurrentAuthenticatedUser;
        if (session?.IsAdmin != true)
        {
            PnlAgenteFiltro.Visibility = Visibility.Collapsed;
            _selectedAgentFilter = null;
            UpdateMutationUiState();
            return;
        }

        PnlAgenteFiltro.Visibility = Visibility.Visible;
        _availableAgents.Clear();

        try
        {
            var page = 1;
            var pageSize = 200;
            var totalPages = 1;
            do
            {
                var response = await App.Api.GetAsync<UsersPagedResponse>($"/api/v1/users?page={page}&pageSize={pageSize}");
                if (response?.Users == null || response.Users.Count == 0)
                    break;

                foreach (var user in response.Users.Where(u => u.Enabled))
                {
                    _availableAgents.Add(new AgentFilterItem
                    {
                        Id = user.Id,
                        FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName,
                        Email = user.Email,
                        IsSelf = session.UserId == user.Id
                    });
                }

                totalPages = Math.Max(response.TotalPages, 1);
                page++;
            } while (page <= totalPages);
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando agentes para DiarioPage");
        }

        _availableAgents.Sort((a, b) =>
        {
            var byName = string.Compare(a.FullName, b.FullName, StringComparison.CurrentCultureIgnoreCase);
            return byName != 0 ? byName : string.Compare(a.Email, b.Email, StringComparison.OrdinalIgnoreCase);
        });

        if (session.UserId != Guid.Empty && _availableAgents.All(a => a.Id != session.UserId))
        {
            _availableAgents.Insert(0, new AgentFilterItem
            {
                Id = session.UserId,
                FullName = session.FullName,
                Email = session.Email,
                IsSelf = true
            });
        }

        _isUpdatingAgentCombo = true;
        CmbAgenteFiltro.ItemsSource = _availableAgents;
        _selectedAgentFilter = _availableAgents.FirstOrDefault(a => a.IsSelf) ?? _availableAgents.FirstOrDefault();
        CmbAgenteFiltro.SelectedItem = _selectedAgentFilter;
        ToolTipService.SetToolTip(CmbAgenteFiltro, _selectedAgentFilter?.Display);
        _isUpdatingAgentCombo = false;
        UpdateMutationUiState();
    }

    private async void OnAgenteFiltroChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingAgentCombo || _isInitialLoad)
            return;

        _selectedAgentFilter = CmbAgenteFiltro.SelectedItem as AgentFilterItem;
        ToolTipService.SetToolTip(CmbAgenteFiltro, _selectedAgentFilter?.Display);
        UpdateMutationUiState();
        await LoadPartesAsync();
    }

    private sealed class AgentFilterItem
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool IsSelf { get; init; }
        public string Display => $"{FullName} ({Email})";
    }

    // ===================== Theme =====================

    private void OnThemeSystem(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Default);
    private void OnThemeLight(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Light);
    private void OnThemeDark(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Dark);

    private void SetTheme(ElementTheme theme)
    {
        // 🆕 NUEVO: Usar ThemeService para cambiar el tema globalmente
        ThemeService.Instance.SetTheme(theme);

        // Actualizar logo y fondo según el tema
        UpdateThemeAssets(theme);
        UpdateThemeToggleIcon();

        App.Log?.LogInformation("🎨 DiarioPage - Tema cambiado a: {theme} (guardado en configuración)", theme);
    }

    /// <summary>
    /// 🆕 NUEVO: Actualiza los checkmarks del menú de tema
    /// </summary>
    private void UpdateThemeCheckmarks()
    {
        var currentTheme = ThemeService.Instance.CurrentTheme;
        // Nota: Los items de tema fueron eliminados del menú
    }

    private void OnToggleThemeClick(object sender, RoutedEventArgs e)
    {
        var nextTheme = ThemeService.Instance.GetEffectiveTheme() == ElementTheme.Dark
            ? ElementTheme.Light
            : ElementTheme.Dark;

        SetTheme(nextTheme);
    }

    private void UpdateThemeToggleIcon()
    {
        var isDark = ThemeService.Instance.GetEffectiveTheme() == ElementTheme.Dark;
        IconThemeDiario.Glyph = isDark ? "\uE708" : "\uE706";
        ToolTipService.SetToolTip(BtnThemeDiario, isDark ? "Cambiar a tema claro" : "Cambiar a tema oscuro");
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

            // Fondo claro: la textura clara cubre completamente el fondo visual.
            BackgroundImageBrush.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/diario_bg_claro.png"));
            BackgroundImageBrush.Opacity = 0.95;
        }

        App.Log?.LogDebug("Tema actualizado: {theme} (efectivo: {effective})", theme, effectiveTheme);
    }

    // ===================== Botones principales =====================
    
    /// <summary>Calcula la hora de inicio para un nuevo parte basándose en el último parte del día actual.</summary>
    /// <returns>Hora de inicio en formato HH:mm. Si hay partes del día actual, retorna la hora de FIN del más reciente (o su hora de inicio si no está cerrado). Si no, retorna la hora actual.</returns>
    private string CalcularHoraInicioParaNuevoParte()
    {
        try
        {
            var hoy = DateTime.Today;
            
            // Buscar partes del día actual en el cache (ordenados por hora inicio DESC)
            var partesHoy = _cache30dias
                .Where(p => p.Fecha.Date == hoy)
                .OrderByDescending(p => DiarioPageHelpers.ParseTime(p.HoraInicio ?? "00:00"))
                .ToList();
            
            if (partesHoy.Any())
            {
                var ultimoParte = partesHoy.First();
                
                // ✅ CORREGIDO: Usar HoraFin si existe (continuidad), sino HoraInicio como fallback
                string horaCalculada;
                if (!string.IsNullOrWhiteSpace(ultimoParte.HoraFin))
                {
                    horaCalculada = ultimoParte.HoraFin;
                    App.Log?.LogInformation("📌 Nuevo parte - Usando hora FIN del último parte: {hora} (Parte ID: {id}, Cliente: {cliente})",
                        horaCalculada, ultimoParte.Id, ultimoParte.Cliente ?? "(sin cliente)");
                }
                else
                {
                    horaCalculada = ultimoParte.HoraInicio ?? DateTime.Now.ToString("HH:mm");
                    App.Log?.LogInformation("📌 Nuevo parte - Último parte SIN hora fin, usando hora INICIO: {hora} (Parte ID: {id}, Cliente: {cliente})",
                        horaCalculada, ultimoParte.Id, ultimoParte.Cliente ?? "(sin cliente)");
                }
                
                return horaCalculada;
            }
            else
            {
                var horaActual = DateTime.Now.ToString("HH:mm");
                
                App.Log?.LogInformation("📌 Nuevo parte - No hay partes previos hoy, usando hora actual: {hora}", horaActual);
                
                return horaActual;
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error calculando hora de inicio para nuevo parte, usando hora actual como fallback");
            return DateTime.Now.ToString("HH:mm");
        }
    }

    private async Task OpenParteEditorAsync(ParteDto? parte, string title)
    {
        if (!EnsureCanMutate())
            return;

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
        {
            // 🆕 NUEVO: Calcular hora de inicio para nuevo parte
            var horaInicio = CalcularHoraInicioParaNuevoParte();
            editPage.NewParte(horaInicio);
        }
        else
        {
            editPage.LoadParte(parte);
        }

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
        if (!EnsureCanMutate())
            return;

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
        if (!EnsureCanMutate())
            return;

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
        if (!EnsureCanMutate())
            return;

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
        if (!EnsureCanMutate())
            return;

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
        if (!EnsureCanMutate())
            return;

        if (ViewModel.IsBusy)
        {
            App.Log?.LogWarning("⚠️ Exportación ya en proceso, ignorando nueva petición");
            return;
        }

        CancellationTokenSource? cts = null;
        try
        {
            App.Log?.LogInformation("📊 EXPORTAR A EXCEL - Iniciando proceso");

            var dialog = new ExportWeekDialog
            {
                XamlRoot = this.XamlRoot,
                RequestedTheme = ThemeService.Instance.CurrentTheme
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary || !dialog.IsRangeValid)
            {
                App.Log?.LogInformation("❌ Usuario canceló la exportación");
                return;
            }

            var request = dialog.ToRequest();
            var destination = await PickExportDestinationAsync(request);
            if (string.IsNullOrWhiteSpace(destination))
            {
                App.Log?.LogInformation("❌ Usuario canceló la selección de destino");
                return;
            }

            ViewModel.IsBusy = true;
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
            cts = new CancellationTokenSource();

            App.Log?.LogInformation("📥 Cargando partes {from} a {to}",
                request.EffectiveMonday.ToString("yyyy-MM-dd"),
                request.EffectiveSunday.ToString("yyyy-MM-dd"));

            var partes = await PartesService.ListRangePagedAsync(
                request.EffectiveMonday,
                request.EffectiveSunday,
                pageSize: 500,
                ct: cts.Token);

            if (partes.Count == 0)
            {
                App.Notifications?.ShowWarning(
                    "No hay partes en el rango seleccionado.",
                    title: "⚠️ Sin Datos");
                return;
            }

            var rangeService = new ExcelRangeExportService(new ExcelExportService());
            var exportResult = await rangeService.ExportRangeAsync(partes, request, destination, cts.Token);
            NotifyExportResult(exportResult);
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogWarning("⚠️ Exportación cancelada");
            App.Notifications?.ShowWarning("La exportación fue cancelada.", title: "⚠️ Cancelado");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error durante la exportación");
            App.Notifications?.ShowError($"Error: {ex.Message}", title: "❌ Error de Exportación");
        }
        finally
        {
            cts?.Dispose();
            ViewModel.IsBusy = false;
            LoadingRing.IsActive = false;
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Solicita archivo o carpeta de destino según el modo de exportación.</summary>
    private async Task<string?> PickExportDestinationAsync(ExportRangeRequest request)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);

        if (request.Mode == ExportMode.Unified)
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"GestionTime_Desde_{request.EffectiveMonday:yyyy-MM-dd}_Hasta_{request.EffectiveSunday:yyyy-MM-dd}"
            };
            savePicker.FileTypeChoices.Add("Excel Workbook", new List<string> { ".xlsx" });
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
            var file = await savePicker.PickSaveFileAsync();
            return file?.Path;
        }

        var folderPicker = new Windows.Storage.Pickers.FolderPicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            ViewMode = Windows.Storage.Pickers.PickerViewMode.List
        };
        folderPicker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    /// <summary>Muestra el resumen de éxito o el fallo parcial de la exportación.</summary>
    private static void NotifyExportResult(ExportRangeResult exportResult)
    {
        var files = string.Join(", ", exportResult.GeneratedFiles.Select(System.IO.Path.GetFileName));
        var emptyWeeks = exportResult.WeeksWithoutRecords > 0
            ? $" Semanas sin registros: {exportResult.WeeksWithoutRecords}."
            : string.Empty;

        if (!exportResult.IsSuccess)
        {
            App.Log?.LogWarning("⚠️ Exportación parcial: creados {count}, falló {failed}",
                exportResult.GeneratedFiles.Count, exportResult.FailedFile);
            App.Notifications?.ShowError(
                $"Se crearon {exportResult.GeneratedFiles.Count} archivo(s) ({files}). Falló: {System.IO.Path.GetFileName(exportResult.FailedFile)}. {exportResult.ErrorMessage}",
                title: "❌ Exportación incompleta");
            return;
        }

        App.Log?.LogInformation("✅ Exportación: {records} registros, {files} archivos, rango {from}-{to}",
            exportResult.TotalRecords, exportResult.GeneratedFiles.Count,
            exportResult.EffectiveMonday.ToString("yyyy-MM-dd"),
            exportResult.EffectiveSunday.ToString("yyyy-MM-dd"));
        App.Notifications?.ShowSuccess(
            $"{exportResult.TotalRecords} registros. Rango {exportResult.EffectiveMonday:dd/MM/yyyy} a {exportResult.EffectiveSunday:dd/MM/yyyy}. Semanas procesadas: {exportResult.WeeksProcessed}. Archivos: {exportResult.GeneratedFiles.Count} ({files}). Destino: {exportResult.Destination}.{emptyWeeks}",
            title: "✅ Exportación Exitosa");
    }

    private async void OnBorrar(object sender, RoutedEventArgs e)
    {
        if (!EnsureCanMutate())
            return;

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
            XamlRoot = XamlRoot,
            RequestedTheme = ThemeService.Instance.CurrentTheme
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
        if (!EnsureCanMutate())
            return;

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
            if (!IsLoaded || XamlRoot == null)
                return;

            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(UpdateTimeCoverageTooltip);
                return;
            }

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
            if (!IsLoaded || XamlRoot == null)
                return;

            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(() => UpdateDuracionHeaderTooltip(coverage, totalPartes));
                return;
            }

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
        if (!EnsureCanMutate())
            return;

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
        if (!EnsureCanMutate())
            return;

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
        if (!EnsureCanMutate())
            return;

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
                XamlRoot = this.XamlRoot,
                RequestedTheme = ThemeService.Instance.CurrentTheme
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
        if (!EnsureCanMutate())
            return;

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
                XamlRoot = this.XamlRoot,
                RequestedTheme = ActualTheme
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
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = GetThemeBrush("AppPrimaryBrush")
        };
        stackPanel.Children.Add(headerText);

        var subtitleText = new TextBlock
        {
            Text = "En desarrollo • Próximo lanzamiento",
            FontSize = 12,
            Foreground = GetThemeBrush("AppTextSecondaryBrush"),
            Margin = new Thickness(0, 4, 0, 0)
        };
        stackPanel.Children.Add(subtitleText);

        // Importación Excel Mejorada
        var importBorder = new Border
        {
            Background = GetThemeBrush("AppSurfaceBrush"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            BorderBrush = GetThemeBrush("AppBorderBrush"),
            BorderThickness = new Thickness(1)
        };

        var importStack = new StackPanel { Spacing = 12 };
        
        var importTitle = new TextBlock
        {
            Text = "✨ Importación Excel Mejorada",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = GetThemeBrush("AppTextPrimaryBrush")
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
            Background = GetThemeBrush("AppSurfaceBrush"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            BorderBrush = GetThemeBrush("AppBorderBrush"),
            BorderThickness = new Thickness(1)
        };

        var resumeStack = new StackPanel { Spacing = 12 };
        
        var resumeTitle = new TextBlock
        {
            Text = "▶️ Reanudar Parte Mejorado",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = GetThemeBrush("AppTextPrimaryBrush")
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
            Background = GetThemeBrush("AppSurfaceVariantBrush"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            BorderBrush = GetThemeBrush("AppInfoBrush"),
            BorderThickness = new Thickness(2)
        };

        var githubStack = new StackPanel { Spacing = 8 };
        
        var githubTitle = new TextBlock
        {
            Text = "🔗 Más Información",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = GetThemeBrush("AppTextPrimaryBrush")
        };
        githubStack.Children.Add(githubTitle);

        var githubDesc = new TextBlock
        {
            Text = "Consulta el historial completo de cambios en GitHub",
            FontSize = 12,
            Foreground = GetThemeBrush("AppTextSecondaryBrush")
        };
        githubStack.Children.Add(githubDesc);

        githubBorder.Child = githubStack;
        stackPanel.Children.Add(githubBorder);

        // Versión actual
        var versionText = new TextBlock
        {
            Text = $"Versión actual: {VersionInfo.Version}",
            FontSize = 12,
            Foreground = GetThemeBrush("AppTextSecondaryBrush"),
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
            Foreground = GetThemeBrush("AppTextPrimaryBrush"),
            TextWrapping = TextWrapping.Wrap
        };
        
        var descText = new TextBlock
        {
            Text = $"  {description}",
            FontSize = 12,
            Foreground = GetThemeBrush("AppTextSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };
        
        stack.Children.Add(titleText);
        stack.Children.Add(descText);
        
        return stack;
    }

    private Brush GetThemeBrush(string key)
    {
        var themeKey = ActualTheme == ElementTheme.Dark ? "Dark" : "Light";

        foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
        {
            if (dictionary.ThemeDictionaries.TryGetValue(themeKey, out var themeResources) &&
                themeResources is ResourceDictionary resources &&
                resources.TryGetValue(key, out var resource) &&
                resource is Brush brush)
            {
                return brush;
            }
        }

        return (Brush)Application.Current.Resources[key];
    }
    
    /// <summary>Abre la ventana de Settings y navega a Perfil y cuenta.</summary>
    private void OnOpenUserSettings(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("⚙️ Usuario abriendo Configuración (Perfil y cuenta)");
            App.ShowSettingsWindow();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error abriendo Configuración");
        }
    }

    /// <summary>Abre la ventana de Informes y oculta DiarioPage.</summary>
    private void OnOpenInformes(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Log?.LogInformation("📊 Usuario abriendo ventana de Informes");
            var currentWindow = App.MainWindowInstance;
            if (currentWindow != null)
            {
                App.ShowReportsWindow(currentWindow);
            }
            else
            {
                App.Log?.LogError("❌ No se pudo obtener la ventana actual para abrir Informes");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error abriendo ventana de Informes");
        }
    }
}

