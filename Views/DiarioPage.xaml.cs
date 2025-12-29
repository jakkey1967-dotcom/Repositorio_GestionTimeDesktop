using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.ViewModels;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Diagnostics;
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

    public DiarioViewModel ViewModel { get; } = new();

    public DiarioPage()
    {
        this.InitializeComponent();
        this.DataContext = ViewModel;

        LvPartes.ItemsSource = Partes;
        SetTheme(ElementTheme.Default);
        DpFiltroFecha.Date = DateTimeOffset.Now;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _debounce.Tick += (_, __) =>
        {
            _debounce!.Stop();
            ApplyFilterToListView();
        };
        
        InitializeIcons();
        InitializeKeyboardAccelerators();
        
        LvPartes.ContainerContentChanging += OnListViewContainerContentChanging;
        
        this.Unloaded += OnPageUnloaded;
    }
    
    /// <summary>
    /// Implementa zebra rows (filas alternadas) aplicando fondo basado en el índice del item
    /// </summary>
    private void OnListViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer is ListViewItem container)
        {
            // Remover handler anterior si existe para evitar duplicados
            container.Loaded -= OnContainerLoaded;
            
            // Aplicar inmediatamente
            ApplyZebraRowBackground(sender, container);
            
            // También aplicar cuando el container se cargue completamente
            container.Loaded += OnContainerLoaded;
        }
    }
    
    /// <summary>
    /// Se ejecuta cuando un container individual se ha cargado completamente
    /// </summary>
    private void OnContainerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListViewItem container && container.Parent is ListViewBase listView)
        {
            ApplyZebraRowBackground(listView, container);
        }
    }
    
    /// <summary>
    /// Aplica el fondo de zebra row basado en el índice del container
    /// </summary>
    private void ApplyZebraRowBackground(ListViewBase listView, ListViewItem container)
    {
        var index = listView.IndexFromContainer(container);
        
        if (index < 0) return; // Container no encontrado en la lista
        
        // Aplicar fondo alterno: par = transparente, impar = TURQUESA 40% (MUY VISIBLE)
        if (index % 2 == 0)
        {
            // Fila par - Transparente
            container.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
        }
        else
        {
            // Fila impar - TURQUESA 40% VISIBLE (#66 = 102 = 40% opacidad)
            // Aumentado de 20% a 40% para máxima visibilidad sobre fondo oscuro
            container.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(102, 11, 140, 153));
        }
    }
    
    /// <summary>
    /// Refresca las zebra rows de todos los containers visibles en el ListView
    /// </summary>
    private void RefreshAllZebraRows()
    {
        if (LvPartes?.Items == null) return;
        
        // Iterar por todos los items visibles y aplicar zebra rows
        for (int i = 0; i < LvPartes.Items.Count; i++)
        {
            var container = LvPartes.ContainerFromIndex(i) as ListViewItem;
            if (container != null)
            {
                ApplyZebraRowBackground(LvPartes, container);
            }
        }
    }
    
    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        // Detener el monitoreo del servicio
        ViewModel.StopServiceMonitoring();
        
        // Limpiar timer de debounce
        _debounce?.Stop();
        
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
            
            await LoadPartesAsync();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnPageLoaded()");
            await ShowInfoAsync("Error cargando Diario. Revisa app.log.");
        }
    }

    private async Task LoadPartesAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            var toDate = DpFiltroFecha.Date?.DateTime.Date ?? DateTime.Today;
            var fromDate = toDate.AddDays(-30);

            using var loadScope = PerformanceLogger.BeginScope(SpecializedLoggers.Data, "LoadPartes", 
                new { FromDate = fromDate, ToDate = toDate });

            SpecializedLoggers.Data.LogInformation("══════════════════════════════════════════════════════════════─");
            SpecializedLoggers.Data.LogInformation("📥 CARGA DE PARTES - Iniciando rango {DateRange}", 
                $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");

            using var sem = new SemaphoreSlim(6);

            var tasks = new List<Task<List<ParteDto>>>();
            for (var d = fromDate; d <= toDate; d = d.AddDays(1))
            {
                var day = d;
                tasks.Add(FetchDayLimitedAsync(day, sem, ct));
            }

            var results = await Task.WhenAll(tasks);

            _cache30dias = results.SelectMany(x => x ?? new List<ParteDto>()).ToList();

            SpecializedLoggers.Data.LogInformation("───────────────────────────────────────────────────────────────");
            SpecializedLoggers.Data.LogInformation("🔍 ANÁLISIS DE DATOS RECIBIDOS DE LA API");
            SpecializedLoggers.Data.LogInformation("───────────────────────────────────────────────────────────────");
            SpecializedLoggers.Data.LogInformation("📊 Total partes recibidos: {count}", _cache30dias.Count);
            
            // Contar por estado
            var porEstado = _cache30dias.GroupBy(p => p.EstadoParte).ToList();
            SpecializedLoggers.Data.LogInformation("📈 Distribución por EstadoParte:");
            foreach (var grupo in porEstado)
            {
                SpecializedLoggers.Data.LogInformation("   • {estado}: {count} partes", grupo.Key, grupo.Count());
            }
            
            // Contar por IsAbierto
            var abiertos = _cache30dias.Count(p => p.IsAbierto);
            var cerrados = _cache30dias.Count(p => !p.IsAbierto);
            SpecializedLoggers.Data.LogInformation("📈 Distribución por IsAbierto:");
            SpecializedLoggers.Data.LogInformation("   • IsAbierto=true: {count}", abiertos);
            SpecializedLoggers.Data.LogInformation("   • IsAbierto=false: {count}", cerrados);
            
            // Contar por HoraFin vacío/no vacío
            var sinHoraFin = _cache30dias.Count(p => string.IsNullOrWhiteSpace(p.HoraFin));
            var conHoraFin = _cache30dias.Count(p => !string.IsNullOrWhiteSpace(p.HoraFin));
            SpecializedLoggers.Data.LogInformation("📈 Distribución por HoraFin:");
            SpecializedLoggers.Data.LogInformation("   • HoraFin vacío: {count}", sinHoraFin);
            SpecializedLoggers.Data.LogInformation("   • HoraFin con valor: {count}", conHoraFin);
            
            SpecializedLoggers.Data.LogInformation("───────────────────────────────────────────────────────────────");
            SpecializedLoggers.Data.LogInformation("📋 DETALLE DE PRIMEROS 10 REGISTROS:");
            SpecializedLoggers.Data.LogInformation("───────────────────────────────────────────────────────────────");
            
            // Ordenar primero para ver los más recientes
            var primeros = _cache30dias
                .OrderByDescending(p => p.Fecha)
                .ThenByDescending(p => ParseTime(p.HoraInicio))
                .Take(10)
                .ToList();
                
            foreach (var p in primeros)
            {
                SpecializedLoggers.Data.LogInformation(
                    "   ID:{id} | {fecha} | Cliente:{cliente} | Grupo:'{grupo}' | Tipo:'{tipo}' | Estado:{estado}",
                    p.Id,
                    p.FechaText,
                    Trim(p.Cliente ?? "", 12),
                    p.Grupo ?? "(null)",
                    p.Tipo ?? "(null)",
                    p.EstadoInt
                );
            }
            
            SpecializedLoggers.Data.LogInformation("═══════════════════════════════════════════════════════════════");

            ApplyFilterToListView();
        }
        catch (OperationCanceledException)
        {
            SpecializedLoggers.Data.LogInformation("Carga de partes cancelada.");
        }
        catch (Exception ex)
        {
            SpecializedLoggers.Data.LogError(ex, "Error cargando partes (30 días)");
            await ShowInfoAsync("Error cargando partes. Revisa app.log.");
        }
    }

    private async Task<List<ParteDto>> FetchDayLimitedAsync(DateTime day, SemaphoreSlim sem, CancellationToken ct)
    {
        await sem.WaitAsync(ct);
        try
        {
            var path = "/api/v1/partes?fecha=" + Uri.EscapeDataString(day.ToString("yyyy-MM-dd"));
            App.Log?.LogDebug("GET {path}", path);

            var result = await App.Api.GetAsync<List<ParteDto>>(path, ct) ?? new List<ParteDto>();
            
            // Log detallado por día si hay resultados
            if (result.Count > 0)
            {
                App.Log?.LogDebug("📅 {fecha}: {count} partes recibidos", day.ToString("yyyy-MM-dd"), result.Count);
                
                // Log del primer registro de este día para ver datos raw
                var first = result.First();
                App.Log?.LogDebug("   └─ Primer registro: ID={id}, EstadoParte={ep}, IsAbierto={ia}, HoraFin='{hf}'",
                    first.Id, first.EstadoParte, first.IsAbierto, first.HoraFin ?? "(vacío)");
            }
            
            return result;
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
        
        // ✅ REFRESH ESTRATÉGICO de zebra rows con delays aumentados
        // El ListView virtualizado tarda en crear containers, usar delays más largos
        
        // 1. Con DispatcherQueue Low (después del layout inicial)
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            RefreshAllZebraRows();
        });
        
        // 2. Con delay de 250ms (garantía para containers completos)
        _ = Task.Delay(250).ContinueWith(_ =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                RefreshAllZebraRows();
            });
        });
        
        // 3. Con delay de 500ms (garantía final para scroll y virtualización)
        _ = Task.Delay(500).ContinueWith(_ =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                RefreshAllZebraRows();
            });
        });
    }

    private static bool Has(string? s, string q)
        => !string.IsNullOrWhiteSpace(s) && s.Contains(q, StringComparison.OrdinalIgnoreCase);

    private static TimeSpan ParseTime(string? hhmm)
        => TimeSpan.TryParse(hhmm, out var ts) ? ts : TimeSpan.Zero;

    // ===================== Filtros =====================

    private async void OnFiltroFechaChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        => await LoadPartesAsync();

    private void OnFiltroQChanged(object sender, TextChangedEventArgs e)
    {
        _debounce?.Stop();
        _debounce?.Start();
    }

    private async void OnRefrescar(object sender, RoutedEventArgs e)
        => await LoadPartesAsync();

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
        RequestedTheme = theme;
        ThemeSystemItem.IsChecked = theme == ElementTheme.Default;
        ThemeLightItem.IsChecked = theme == ElementTheme.Light;
        ThemeDarkItem.IsChecked = theme == ElementTheme.Dark;
        
        // Actualizar logo y fondo según el tema
        UpdateThemeAssets(theme);
        
        // ✅ Refrescar zebra rows después de cambiar tema
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            RefreshAllZebraRows();
        });
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

        // Actualizar logo
        if (effectiveTheme == ElementTheme.Dark)
        {
            LogoImageBanner.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/LogoOscuro.png"));
            BackgroundImageBrush.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/diario_bg_dark.png"));
        }
        else
        {
            LogoImageBanner.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/LogoClaro.png"));
            BackgroundImageBrush.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/Diario_bg_claro.png"));
        }
        
        App.Log?.LogDebug("Tema actualizado: {theme} (efectivo: {effective})", theme, effectiveTheme);
    }

    // ===================== Botones principales =====================

    private async Task OpenParteEditorAsync(ParteDto? parte, string title)
    {
        var window = new Microsoft.UI.Xaml.Window { Title = title };
        var editPage = new ParteItemEdit();
        editPage.RequestedTheme = this.RequestedTheme;
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
        if (saved)
            await LoadPartesAsync();
    }

    private void ConfigureChildWindow(Microsoft.UI.Xaml.Window window)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        if (appWindow != null)
        {
            var displayArea = Microsoft.UI.Windowing.DisplayArea.Primary;
            var workArea = displayArea.WorkArea;

            int width = 1650;
            int height = 750;
            int x = workArea.X + (workArea.Width - width) / 2;
            int y = workArea.Y + (workArea.Height - height) / 2;

            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));

            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }
        }
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
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
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
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        if (appWindow != null)
        {
            var displayArea = Microsoft.UI.Windowing.DisplayArea.Primary;
            var workArea = displayArea.WorkArea;

            int width = 1200;
            int height = 800;
            int x = workArea.X + (workArea.Width - width) / 2;
            int y = workArea.Y + (workArea.Height - height) / 2;

            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));

            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
            }
        }
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
            await ShowInfoAsync($"✅ Parte {parte.Id} eliminado definitivamente.");
            
            await LoadPartesAsync();
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
            return;
        }

        var parte = Partes.FirstOrDefault(p => p.Id == parteId);
        if (parte == null || !parte.CanCerrar)
        {
            return;
        }

        try
        {
            App.Log?.LogInformation("🔒 CERRAR PARTE - ID: {id}", parteId);
            
            // Preguntar hora de cierre al usuario
            var horaFin = await AskHoraCierreAsync(parte.HoraInicio);
            
            if (string.IsNullOrEmpty(horaFin))
            {
                App.Log?.LogInformation("Usuario canceló el cierre del parte");
                return;
            }
            
            try
            {
                await App.Api.PostAsync($"/api/v1/partes/{parteId}/close?horaFin={Uri.EscapeDataString(horaFin)}");
                App.Log?.LogInformation("✅ Parte {id} cerrado correctamente usando POST con HoraFin={hora}", parteId, horaFin);
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                App.Log?.LogWarning("POST /close no permitido, intentando con PUT...");
                await App.Api.PutAsync<object, object>($"/api/v1/partes/{parteId}", new 
                { 
                    HoraFin = horaFin,
                    EstadoInt = 2
                });
                App.Log?.LogInformation("✅ Parte {id} cerrado correctamente usando PUT con HoraFin={hora}", parteId, horaFin);
            }
            
            await LoadPartesAsync();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error cerrando parte {id}", parteId);
            await ShowInfoAsync($"❌ Error cerrando parte: {ex.Message}");
        }
    }

    private async Task<string?> AskHoraCierreAsync(string horaInicio)
    {
        var horaActual = DateTime.Now.ToString("HH:mm");
        
        var stackPanel = new StackPanel { Spacing = 15 };
        
        // Hora de inicio (solo lectura)
        stackPanel.Children.Add(new TextBlock
        {
            Text = "🕐 Hora de inicio:",
            FontSize = 13,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184))
        });
        
        var txtHoraInicio = new TextBox
        {
            Text = horaInicio,
            IsReadOnly = true,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 40)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 8, 12, 8),
            FontSize = 14
        };
        stackPanel.Children.Add(txtHoraInicio);
        
        // Hora de fin (editable)
        stackPanel.Children.Add(new TextBlock
        {
            Text = "⏰ Hora de cierre:",
            FontSize = 13,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 239, 242)),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 0)
        });

        var horaFinGrid = new Grid { ColumnSpacing = 10 };
        horaFinGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        horaFinGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var txtHoraFin = new TextBox
        {
            Text = horaActual,
            PlaceholderText = "HH:mm",
            MaxLength = 5,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12, 8, 12, 8),
            FontSize = 14
        };
        Grid.SetColumn(txtHoraFin, 0);
        horaFinGrid.Children.Add(txtHoraFin);

        var btnHoraActual = new Button
        {
            Content = "🕐 Ahora",
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 11, 140, 153)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            Padding = new Thickness(20, 8, 20, 8),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        btnHoraActual.Click += (s, e) =>
        {
            txtHoraFin.Text = DateTime.Now.ToString("HH:mm");
        };
        Grid.SetColumn(btnHoraActual, 1);
        horaFinGrid.Children.Add(btnHoraActual);

        stackPanel.Children.Add(horaFinGrid);

        stackPanel.Children.Add(new TextBlock
        {
            Text = "💡 Formato: HH:mm (ejemplo: 14:30)",
            FontSize = 11,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 176, 184)),
            Opacity = 0.7,
            Margin = new Thickness(0, 5, 0, 0)
        });

        var dialog = new ContentDialog
        {
            Title = "⏰ Cerrar Parte",
            Content = stackPanel,
            PrimaryButtonText = "✅ Cerrar",
            CloseButtonText = "❌ Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            var horaFin = txtHoraFin.Text?.Trim();
            
            // Validar formato HH:mm
            if (!string.IsNullOrEmpty(horaFin) && TimeSpan.TryParse(horaFin, out _))
            {
                return horaFin;
            }
            else
            {
                await ShowInfoAsync("⚠️ Formato de hora inválido. Use HH:mm (ejemplo: 14:30)");
                return null;
            }
        }

        return null;
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
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                settings.Values.Remove("UserToken");
                settings.Values.Remove("UserName");
                settings.Values.Remove("UserEmail");
                settings.Values.Remove("UserRole");
                
                if (App.MainWindowInstance?.Navigator != null)
                {
                    App.MainWindowInstance.Navigator.Navigate(typeof(LoginPage));
                }
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en logout");
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
                await App.Api.PutAsync<object, object>($"/api/v1/partes/{parte.Id}/close", new { HoraFin = horaFin });
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

    private static string Trim(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Length <= max) return s;
        return s.Substring(0, max) + "…";
    }

    private async void OnSalir(object sender, RoutedEventArgs e)
    {
        OnLogout(sender, e);
    }
}
