using System;
using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Views;

public sealed partial class ParteItemEdit : Page
{
    public ParteDto? Parte { get; private set; }
    public bool Guardado { get; private set; }
    
    private Microsoft.UI.Xaml.Window? _parentWindow;
    
    // Cache local de clientes
    private static List<ClienteResponse>? _clientesCache = null;
    private static DateTime? _cacheLoadedAt = null;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30); // 30 minutos de validez
    
    // Cache local de grupos
    private static List<GrupoResponse>? _gruposCache = null;
    private static DateTime? _gruposCacheLoadedAt = null;
    
    // Cache local de tipos
    private static List<TipoResponse>? _tiposCache = null;
    private static DateTime? _tiposCacheLoadedAt = null;
    
    // Items de Cliente para AutoSuggestBox
    private ObservableCollection<string> _clienteSuggestions = new();
    private DispatcherTimer? _clienteSearchTimer;
    private CancellationTokenSource? _clienteSearchCts;
    private string _lastClienteQuery = string.Empty;
    
    // Items de Cliente originales
    private ObservableCollection<string> _clienteItems = new();
    private CancellationTokenSource? _clienteLoadCts;
    private bool _clientesLoaded = false;
    
    // Items de Grupo
    private ObservableCollection<string> _grupoItems = new();
    private CancellationTokenSource? _grupoLoadCts;
    
    // Items de Tipo
    private ObservableCollection<string> _tipoItems = new();
    private CancellationTokenSource? _tipoLoadCts;
    
    // Flag para evitar abrir dropdown al recibir foco por Tab
    private bool _grupoDropDownOpenedByUser = false;
    private bool _tipoDropDownOpenedByUser = false;
    private bool _gruposLoaded = false;
    private bool _tiposLoaded = false;
    
    // Flags temporales para evitar bucle después de seleccionar
    private bool _grupoJustSelected = false;
    private bool _tipoJustSelected = false;
    
    // Flags para detectar navegación activa (Tab/Escape)
    private bool _grupoNavigatingAway = false;
    private bool _tipoNavigatingAway = false;
    
    // Sistema de tracking de foco
    private string _lastFocusedControl = "";
    private int _focusChangeCounter = 0;
    private DateTime _lastFocusChangeTime = DateTime.Now;

    private bool _suppressHoraFormatting;
    
    // Flags para detectar si es la primera tecla después de recibir foco
    private bool _horaInicioFirstKey = false;
    private bool _horaFinFirstKey = false;
    
    // Sistema de timestamp automático para TxtAccion
    private bool _suppressAccionTimestamp = false;

    public ParteItemEdit()
    {
        InitializeComponent();
        
        App.Log?.LogInformation("📝 ParteItemEdit constructor iniciado");
        
        // 🆕 NUEVO: Aplicar tema global
        ThemeService.Instance.ApplyTheme(this);
        
        // 🆕 NUEVO: Suscribirse a cambios de tema globales
        ThemeService.Instance.ThemeChanged += OnGlobalThemeChanged;
        
        // Cargar información del usuario desde LocalSettings
        LoadUserInfo();
        
        // Configurar AutoSuggestBox de Cliente
        TxtCliente.ItemsSource = _clienteSuggestions;
        
        // Configurar timer de búsqueda (debounce de 350ms)
        _clienteSearchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        _clienteSearchTimer.Tick += async (s, e) =>
        {
            _clienteSearchTimer.Stop();
            await SearchClientesAsync();
        };
        
        App.Log?.LogDebug("✅ AutoSuggestBox Cliente configurado com búsqueda dinâmica");
        
        // Configurar ComboBox de Grupo (solo lectura)
        CmbGrupo.ItemsSource = _grupoItems;
        App.Log?.LogDebug("✅ CmbGrupo.ItemsSource configurado con ObservableCollection vacía");
        
        // Configurar ComboBox de Tipo (solo lectura)
        CmbTipo.ItemsSource = _tipoItems;
        App.Log?.LogDebug("✅ CmbTipo.ItemsSource configurado con ObservableCollection vacía");
        
        // Configurar navegación por Enter en fields de texto
        ConfigureKeyboardNavigation();
        
        // Agregar evento Loaded para fade in
        this.Loaded += OnPageLoaded;
        
        // Eventos de foco para fields de hora
        TxtHoraInicio.GotFocus += OnHoraGotFocus;
        TxtHoraFin.GotFocus += OnHoraGotFocus;
        
        App.Log?.LogInformation("✅ ParteItemEdit constructor completado");
    }

    private void OnPageLoaded(object? sender, RoutedEventArgs e)
    {
        // Remover evento para evitar doble ejecución
        this.Loaded -= OnPageLoaded;
        
        try
        {
            App.Log?.LogInformation("ParteItemEdit Loaded ✅");
            
            // Actualizar logo según tema
            UpdateBannerLogo();
            
            // Iniciar animación de fade in
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            Storyboard.SetTarget(fadeIn, RootGrid);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");
            
            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnPageLoaded() de ParteItemEdit");
        }
    }

    private void ConfigureKeyboardNavigation()
    {
        App.Log?.LogDebug("Configurando navegación de teclado...");
        
        // Enter para navegar entre TextBox
        TxtTienda.KeyDown += OnTextBoxEnterKey;
        TxtHoraInicio.KeyDown += OnTextBoxEnterKey;
		TxtHoraFin.KeyDown += OnTextBoxEnterKey;
		TxtTicket.KeyDown += OnTextBoxEnterKey;
        
        // ComboBox: Enter para confirmar selección y avanzar
        CmbGrupo.KeyDown += OnComboBoxEnterKey;
        CmbTipo.KeyDown += OnComboBoxEnterKey;
        
        // Grupo: cargar todos al recibir foco
        CmbGrupo.GotFocus += OnGrupoGotFocus;
        CmbGrupo.DropDownOpened += OnGrupoDropDownOpened;
        CmbGrupo.PreviewKeyDown += OnGrupoPreviewKeyDown;
        CmbGrupo.SelectionChanged += OnGrupoSelectionChanged;
        
        App.Log?.LogDebug("✅ Eventos de Grupo configurados");
        
        // Tipo: cargar todos al recibir foco
        CmbTipo.GotFocus += OnTipoGotFocus;
        CmbTipo.DropDownOpened += OnTipoDropDownOpened;
        CmbTipo.PreviewKeyDown += OnTipoPreviewKeyDown;
        CmbTipo.SelectionChanged += OnTipoSelectionChanged;
        
        App.Log?.LogDebug("✅ Eventos de Tipo configurados");
        
        // Acción: Ctrl+Enter para guardar desde el campo
        TxtAccion.KeyDown += OnAccionKeyDown;
        
        App.Log?.LogDebug("✅ Navegación de teclado configurada");
    }
    
    // ===================== Animaciones Hover =====================
    
    private void OnButtonPointerEntered(object? sender, PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.IsEnabled)
        {
            AnimateButtonScale(button, 1.08, 150);
        }
    }

    private void OnButtonPointerExited(object? sender, PointerRoutedEventArgs e)
    {
        if (sender is Button button)
        {
            AnimateButtonScale(button, 1.0, 150);
        }
    }

    private void AnimateButtonScale(Button button, double targetScale, int durationMs)
    {
        // Asegurar que cada botón tenga su propio ScaleTransform
        Microsoft.UI.Xaml.Media.ScaleTransform scaleTransform;
        
        if (button.RenderTransform is Microsoft.UI.Xaml.Media.ScaleTransform existingTransform)
        {
            scaleTransform = existingTransform;
        }
        else
        {
            // Crear un nuevo ScaleTransform único para este botón
            scaleTransform = new Microsoft.UI.Xaml.Media.ScaleTransform 
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

    // ===================== Focus Tracking =====================
    
    /// <summary>
    /// Registra eventos de foco en todos los controles interactivos del formulario
    /// para poder ver el recorrido completo del foco en los logs
    /// </summary>
    private void RegisterFocusTracking()
    {
        App.Log?.LogInformation("🔍 Iniciando sistema de tracking de foco...");
        
        // Lista de controles a monitorear
        var controls = new Dictionary<Control, string>
        {
            { DpFecha, "DpFecha (CalendarDatePicker)" },
            { TxtCliente, "TxtCliente (ComboBox)" },
            { TxtTienda, "TxtTienda (TextBox)" },
            { TxtHoraInicio, "TxtHoraInicio (TextBox)" },
            { TxtHoraFin, "TxtHoraFin (TextBox)" },
            { TxtTicket, "TxtTicket (TextBox)" },
            { CmbGrupo, "CmbGrupo (ComboBox)" },
            { CmbTipo, "CmbTipo (ComboBox)" },
            { TxtDuracion, "TxtDuracion (TextBox ReadOnly)" },
            { TxtAccion, "TxtAccion (TextBox MultiLine)" },
            { BtnGuardar, "BtnGuardar (Button)" },
            { BtnCancelar, "BtnCancelar (Button)" },
            { BtnSalir, "BtnSalir (Button)" }
        };
        
        foreach (var kvp in controls)
        {
            var control = kvp.Key;
            var name = kvp.Value;
            
            // GotFocus: cuando el control recibe foco
            control.GotFocus += (s, e) => OnControlGotFocus(name, e);
            
            // LostFocus: cuando el control pierde foco
            control.LostFocus += (s, e) => OnControlLostFocus(name, e);
        }
        
        App.Log?.LogInformation("✅ Sistema de tracking configurado para {count} controles", controls.Count);
    }
    
    /// <summary>
    /// Handler para cuando un control RECIBE foco
    /// </summary>
    private void OnControlGotFocus(string controlName, RoutedEventArgs e)
    {
        _focusChangeCounter++;
        var elapsed = (DateTime.Now - _lastFocusChangeTime).TotalMilliseconds;
        
        App.Log?.LogInformation(
            "🔍 [{counter}] FOCO EN ➡ {control} (desde: {from}, {ms:F0}ms)", 
            _focusChangeCounter,
            controlName,
            string.IsNullOrEmpty(_lastFocusedControl) ? "Inicio" : _lastFocusedControl,
            elapsed
        );
        
        _lastFocusedControl = controlName;
        _lastFocusChangeTime = DateTime.Now;
    }
    
    /// <summary>
    /// Handler para cuando un control PIERDE foco
    /// </summary>
    private void OnControlLostFocus(string controlName, RoutedEventArgs e)
    {
        var elapsed = (DateTime.Now - _lastFocusChangeTime).TotalMilliseconds;
        
        App.Log?.LogDebug(
            "🔍 FOCO PERDIDO ⬅ {control} ({ms:F0}ms desde cambio)", 
            controlName,
            elapsed
        );
    }
    
    // ===================== GRUPO =====================
    
    /// <summary>
    /// Método para OnGrupoGotFocus
    /// </summary>
    private async void OnGrupoGotFocus(object sender, RoutedEventArgs e)
    {
        App.Log?.LogInformation("🔧 CmbGrupo GotFocus - _gruposLoaded={loaded}, IsDropDownOpen={open}, JustSelected={just}, NavigatingAway={nav}", 
            _gruposLoaded, CmbGrupo.IsDropDownOpen, _grupoJustSelected, _grupoNavigatingAway);
        
        // ⚠️ NO abrir si el usuario está navegando con Tab/Escape
        if (_grupoNavigatingAway)
        {
            App.Log?.LogDebug("🔧 Usuario navegando, NO abrir dropdown");
            _grupoNavigatingAway = false; // Resetear flag
            return;
        }
        
        // ⚠️ NO abrir si ya está abierto (evita bucle infinito)
        if (CmbGrupo.IsDropDownOpen)
        {
            App.Log?.LogDebug("🔧 Dropdown ya abierto, saltando...");
            return;
        }
        
        // ⚠️ NO abrir si acabamos de selecionar o confirmar con Enter (el foco vuelve después del cierre)
        if (_grupoJustSelected)
        {
            App.Log?.LogDebug("🔧 Recién seleccionado/confirmado, NO abrir automáticamente");
            _grupoJustSelected = false; // Resetear flag
            return;
        }
        
        // Cargar grupos si aún no se han cargado
        if (!_gruposLoaded)
        {
            App.Log?.LogInformation("📊 Cargando grupos al recibir foco...");
            await LoadGruposAsync();
            
            // Después de cargar, abrir el dropdown automáticamente SOLO si es la primera vez
            if (sender is ComboBox combo && _grupoItems.Count > 0 && combo.SelectedIndex < 0)
            {
                App.Log?.LogDebug("🔧 Abriendo dropdown automáticamente con {count} items (sin selección previa)", _grupoItems.Count);
                _grupoDropDownOpenedByUser = true; // Marcar como apertura válida
                combo.IsDropDownOpen = true;
            }
        }
        else
        {
            App.Log?.LogDebug("✅ Grupos ya cargados ({count} items), abriendo dropdown", _grupoItems.Count);
            
            // Si ya están cargados, abrir directamente SOLO si no hay selección previa
            if (sender is ComboBox combo && _grupoItems.Count > 0 && combo.SelectedIndex < 0)
            {
                _grupoDropDownOpenedByUser = true; // Marcar como apertura válida
                combo.IsDropDownOpen = true;
                App.Log?.LogDebug("🔧 Dropdown abierto (sin selección previa)");
            }
            else if (sender is ComboBox combo2 && combo2.SelectedIndex >= 0)
            {
                App.Log?.LogDebug("🔧 Ya hay selección (index: {index}), NO abrir automáticamente", combo2.SelectedIndex);
            }
        }
    }
    
    private void OnGrupoPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        App.Log?.LogDebug("🔧 CmbGrupo PreviewKeyDown - Key={key}", e.Key);
        
        // ? INTERCEPTAR ENTER AQUÍ para confirmar y avanzar
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (sender is ComboBox comboGrupo)
            {
                App.Log?.LogInformation("📥 Enter en Grupo - Cerrando y avanzando");
                
                // Cerrar dropdown si está abierto
                if (comboGrupo.IsDropDownOpen)
                {
                    comboGrupo.IsDropDownOpen = false;
                }
                
                // Marcar como recién seleccionado
                _grupoJustSelected = true;
                
                // Marcar como modificado
                OnFieldChanged(comboGrupo, null!);
                
                // Navegar al siguiente control
                MoveToNextControl(comboGrupo);
                
                e.Handled = true;
                return;
            }
        }
        
        // Detectar navegación con Tab o Escape
        if (e.Key == Windows.System.VirtualKey.Tab || 
            e.Key == Windows.System.VirtualKey.Escape)
        {
            App.Log?.LogDebug("🔧 Usuario navegando con {key}, marcar flag", e.Key);
            _grupoNavigatingAway = true;
            
            // Cerrar dropdown si está abierto
            if (sender is ComboBox comboBox && comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = false;
            }
            return;
        }
        
        // Si presiona flecha abajo o Alt+Down, es apertura manual
        if (e.Key == Windows.System.VirtualKey.Down && sender is ComboBox combo)
        {
            var altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
            
            // Alt+Down o solo Down cuando está cerrado = apertura manual
            if ((altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down ||
                !combo.IsDropDownOpen)
            {
                _grupoDropDownOpenedByUser = true;
                App.Log?.LogDebug("🔧 Dropdown de grupo marcado como apertura manual");
            }
        }
    }

    private async void OnGrupoDropDownOpened(object? sender, object e)
    {
        App.Log?.LogInformation("🔧 CmbGrupo DropDownOpened - _grupoDropDownOpenedByUser={manual}, _gruposLoaded={loaded}, Items={items}", 
            _grupoDropDownOpenedByUser, _gruposLoaded, _grupoItems.Count);
        
        // Si no hay items y no están cargados, cargar ahora
        if (!_gruposLoaded)
        {
            App.Log?.LogInformation("📊 Cargando grupos desde dropdown...");
            await LoadGruposAsync();
        }
        
        // Resetear flag después de abrir
        _grupoDropDownOpenedByUser = false;
    }

    private void OnGrupoSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Cuando el usuario selecciona un grupo
        if (sender is ComboBox combo && combo.SelectedItem is string selectedGrupo)
        {
            App.Log?.LogInformation("✅ Grupo seleccionado: {grupo}", selectedGrupo);
            
            // Marcar que acabamos de seleccionar (para evitar bucle en GotFocus)
            _grupoJustSelected = true;
            
            // NO cerrar dropdown automáticamente aquí
            // Dejar que el usuario presione Enter o haga click para confirmar
            // combo.IsDropDownOpen = false;  // <-- REMOVIDO
            
            // Marcar como modificado
            OnFieldChanged(sender, e);
        }
    }

    private async Task LoadGruposAsync()
    {
        App.Log?.LogInformation("🔄 LoadGruposAsync iniciado - Cache válido: {valid}", IsGruposCacheValid());
        
        // Si ya está cargado o el cache es válido, no recargar
        if (_gruposLoaded && IsGruposCacheValid())
        {
            App.Log?.LogDebug("✅ Usando cache de grupos ({count} items, cargado hace {age})",
                _gruposCache!.Count,
                DateTime.Now - _gruposCacheLoadedAt);
            return;
        }
        
        try
        {
            // Cancelar carga anterior
            _grupoLoadCts?.Cancel();
            _grupoLoadCts = new CancellationTokenSource();
            var ct = _grupoLoadCts.Token;
            
            // Llamar a la API
            var path = "/api/v1/catalog/grupos";
            App.Log?.LogInformation("🔄 Llamando a API: {path}", path);
            
            var response = await App.Api.GetAsync<GrupoResponse[]>(path, ct);
            
            App.Log?.LogInformation("✅ Respuesta recibida: {isNull}, Cancelado: {cancelled}", 
                response == null ? "NULL" : $"{response.Length} items", 
                ct.IsCancellationRequested);
            
            if (response != null && !ct.IsCancellationRequested)
            {
                // ?? LOG DETALLADO: Ver qué viene en cada item del JSON
                App.Log?.LogInformation("---------------------------------------------------------------");
                App.Log?.LogInformation("🔍 ANÁLISIS DETALLADO DE GRUPOS RECIBIDOS:");
                foreach (var g in response.Take(10))
                {
                    App.Log?.LogInformation("   - Id_grupo={id}, Nombre='" + 
                        g.Id_grupo + "");

                }
                App.Log?.LogInformation("----------------------------------------------------------------");
                
                // Guardar en cache estático (compartido entre instancias)
                _gruposCache = response.ToList();
                _gruposCacheLoadedAt = DateTime.Now;
                
                App.Log?.LogInformation("✅ Cache de grupos guardado: {count} items", _gruposCache.Count);
                
                // Actualizar items de la UI
                _grupoItems.Clear();
                
                // Filtrar grupos con nombre no vacío y ordenar alfabéticamente
                var gruposValidos = _gruposCache
                    .Where(g => !string.IsNullOrWhiteSpace(g.Nombre))
                    .OrderBy(g => g.Nombre)
                    .ToList();
                
                App.Log?.LogInformation("✅ Grupos válidos (con nombre): {count} de {total}", 
                    gruposValidos.Count, _gruposCache.Count);
                
                foreach (var grupo in gruposValidos)
                {
                    _grupoItems.Add(grupo.Nombre);
                    App.Log?.LogDebug("  + [{id}] {nombre}", grupo.Id_grupo, grupo.Nombre);
                }
                
                _gruposLoaded = true;
                
                App.Log?.LogInformation("📊 Cache de grupos actualizado: {count} registros en UI", _grupoItems.Count);
            }
            else
            {
                App.Log?.LogWarning("⚠️ No se pudieron cargar grupos (response null o cancelado)");
            }
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogDebug("🚫 Carga de grupos cancelada");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando catálogo de grupos");
        }
    }

    private bool IsGruposCacheValid()
    {
        if (_gruposCache == null || _gruposCacheLoadedAt == null)
            return false;
        
        var age = DateTime.Now - _gruposCacheLoadedAt.Value;
        return age < CacheDuration;
    }

    /// <summary>
    /// Método público para refrescar el cache de grupos manualmente
    /// </summary>
    public static void InvalidateGruposCache()
    {
        _gruposCache = null;
        _gruposCacheLoadedAt = null;
        App.Log?.LogInformation("Cache de grupos invalidado");
    }

    // ===================== TIPO =====================
    
    private async void OnTipoGotFocus(object sender, RoutedEventArgs e)
    {
        App.Log?.LogInformation("🔧 CmbTipo GotFocus - _tiposLoaded={loaded}, IsDropDownOpen={open}, JustSelected={just}, NavigatingAway={nav}", 
            _tiposLoaded, CmbTipo.IsDropDownOpen, _tipoJustSelected, _tipoNavigatingAway);
        
        // ⚠️ NO abrir si el usuario está navegando con Tab/Escape
        if (_tipoNavigatingAway)
        {
            App.Log?.LogDebug("🔧 Usuario navegando, NO abrir dropdown");
            _tipoNavigatingAway = false; // Resetear flag
            return;
        }
        
        // ⚠️ NO abrir si ya está abierto (evita bucle infinito)
        if (CmbTipo.IsDropDownOpen)
        {
            App.Log?.LogDebug("🔧 Dropdown ya abierto, saltando...");
            return;
        }
        
        // ⚠️ NO abrir si acabamos de seleccionar o confirmar con Enter (el foco vuelve después del cierre)
        if (_tipoJustSelected)
        {
            App.Log?.LogDebug("🔧 Recién seleccionado/confirmado, NO abrir automáticamente");
            _tipoJustSelected = false; // Resetear flag
            return;
        }
        
        // Cargar tipos si aún no se han cargado
        if (!_tiposLoaded)
        {
            App.Log?.LogInformation("📊 Cargando tipos al recibir foco...");
            await LoadTiposAsync();
            
            // Después de cargar, abrir el dropdown automáticamente SOLO si es la primera vez
            if (sender is ComboBox combo && _tipoItems.Count > 0 && combo.SelectedIndex < 0)
            {
                App.Log?.LogDebug("🔧 Abriendo dropdown automáticamente con {count} items (sin selección previa)", _tipoItems.Count);
                _tipoDropDownOpenedByUser = true; // Marcar como apertura válida
                combo.IsDropDownOpen = true;
            }
        }
        else
        {
            App.Log?.LogDebug("✅ Tipos ya cargados ({count} items), abriendo dropdown", _tipoItems.Count);
            
            // Si ya están cargados, abrir directamente SOLO si no hay selección previa
            if (sender is ComboBox combo && _tipoItems.Count > 0 && combo.SelectedIndex < 0)
            {
                _tipoDropDownOpenedByUser = true; // Marcar como apertura válida
                combo.IsDropDownOpen = true;
                App.Log?.LogDebug("🔧 Dropdown abierto (sin selección previa)");
            }
            else if (sender is ComboBox combo2 && combo2.SelectedIndex >= 0)
            {
                App.Log?.LogDebug("🔧 Ya hay selección (index: {index}), NO abrir automáticamente", combo2.SelectedIndex);
            }
        }
    }

    private void OnTipoPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        App.Log?.LogDebug("🔧 CmbTipo PreviewKeyDown - Key={key}", e.Key);
        
        // ? INTERCEPTAR ENTER AQUÍ para confirmar y avanzar
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (sender is ComboBox comboTipo)
            {
                App.Log?.LogInformation("📥 Enter en Tipo - Cerrando y avanzando");
                
                // Cerrar dropdown si está abierto
                if (comboTipo.IsDropDownOpen)
                {
                    comboTipo.IsDropDownOpen = false;
                }
                
                // Marcar como recién seleccionado
                _tipoJustSelected = true;
                
                // Marcar como modificado
                OnFieldChanged(comboTipo, null!);
                
                // Navegar al siguiente control
                MoveToNextControl(comboTipo);
                
                e.Handled = true;
                return;
            }
        }
        
        // Detectar navegación con Tab o Escape
        if (e.Key == Windows.System.VirtualKey.Tab || 
            e.Key == Windows.System.VirtualKey.Escape)
        {
            App.Log?.LogDebug("🔧 Usuario navegando con {key}, marcar flag", e.Key);
            _tipoNavigatingAway = true;
            
            // Cerrar dropdown si está abierto
            if (sender is ComboBox comboBox && comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = false;
            }
            return;
        }
        
        // Si presiona flecha abajo o Alt+Down, es apertura manual
        if (e.Key == Windows.System.VirtualKey.Down && sender is ComboBox combo)
        {
            var altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
            
            // Alt+Down o solo Down cuando está cerrado = apertura manual
            if ((altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down ||
                !combo.IsDropDownOpen)
            {
                _tipoDropDownOpenedByUser = true;
                App.Log?.LogDebug("🔧 Dropdown de tipo marcado como apertura manual");
            }
        }
    }

    private async void OnTipoDropDownOpened(object? sender, object e)
    {
        App.Log?.LogInformation("🔧 CmbTipo DropDownOpened - _tipoDropDownOpenedByUser={manual}, _tiposLoaded={loaded}, Items={items}", 
            _tipoDropDownOpenedByUser, _tiposLoaded, _tipoItems.Count);
        
        // Si no hay items y no están cargados, cargar ahora
        if (!_tiposLoaded)
        {
            App.Log?.LogInformation("📊 Cargando tipos desde dropdown...");
            await LoadTiposAsync();
        }
        
        // Resetear flag después de abrir
        _tipoDropDownOpenedByUser = false;
    }

    private void OnTipoSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Cuando el usuario selecciona un tipo
        if (sender is ComboBox combo && combo.SelectedItem is string selectedTipo)
        {
            App.Log?.LogInformation("✅ Tipo seleccionado: {tipo}", selectedTipo);
            
            // Marcar que acabamos de seleccionar (para evitar bucle en GotFocus)
            _tipoJustSelected = true;
            
            // NO cerrar dropdown automáticamente aquí
            // Dejar que el usuario presione Enter o haga click para confirmar
            // combo.IsDropDownOpen = false;  // <-- REMOVIDO
            
            // Marcar como modificado
            OnFieldChanged(sender, e);
        }
    }

    private async Task LoadTiposAsync()
    {
        App.Log?.LogInformation("🔄 LoadTiposAsync iniciado - Cache válido: {valid}", IsTiposCacheValid());
        
        // Si ya está cargado o el cache es válido, no recargar
        if (_tiposLoaded && IsTiposCacheValid())
        {
            App.Log?.LogDebug("✅ Usando cache de tipos ({count} items, cargado hace {age})",
                _tiposCache!.Count,
                DateTime.Now - _tiposCacheLoadedAt);
            return;
        }
        
        try
        {
            // Cancelar carga anterior
            _tipoLoadCts?.Cancel();
            _tipoLoadCts = new CancellationTokenSource();
            var ct = _tipoLoadCts.Token;
            
            // Llamar a la API
            var path = "/api/v1/catalog/tipos";
            App.Log?.LogInformation("🔄 Llamando a API: {path}", path);
            
            var response = await App.Api.GetAsync<TipoResponse[]>(path, ct);
            
            App.Log?.LogInformation("✅ Respuesta recibida: {isNull}, Cancelado: {cancelled}", 
                response == null ? "NULL" : $"{response.Length} items", 
                ct.IsCancellationRequested);
            
            if (response != null && !ct.IsCancellationRequested)
            {
                // ?? LOG DETALLADO: Ver qué viene en cada item del JSON
                App.Log?.LogInformation("---------------------------------------------------------------");
                App.Log?.LogInformation("🔍 ANÁLISIS DETALLADO DE TIPOS RECIBIDOS:");
                foreach (var t in response.Take(10))
                {
                    App.Log?.LogInformation("   - Id_tipo={id}, Nombre='{nombre}'", 
                        t.Id_tipo, 
                        t.Nombre ?? "(null)");
                }
                App.Log?.LogInformation("----------------------------------------------------------------");
                
                // Guardar en cache estático (compartido entre instancias)
                _tiposCache = response.ToList();
                _tiposCacheLoadedAt = DateTime.Now;
                
                App.Log?.LogInformation("✅ Cache de tipos guardado: {count} items", _tiposCache.Count);
                
                // Actualizar items de la UI
                _tipoItems.Clear();
                
                // Filtrar tipos con nombre no vacío y ordenar alfabéticamente
                var tiposValidos = _tiposCache
                    .Where(t => !string.IsNullOrWhiteSpace(t.Nombre))
                    .OrderBy(t => t.Nombre)
                    .ToList();
                
                App.Log?.LogInformation("✅ Tipos válidos (con nombre): {count} de {total}", 
                    tiposValidos.Count, _tiposCache.Count);
                
                foreach (var tipo in tiposValidos)
                {
                    _tipoItems.Add(tipo.Nombre);
                    App.Log?.LogDebug("  + [{id}] {nombre}", tipo.Id_tipo, tipo.Nombre);
                }
                
                _tiposLoaded = true;
                
                App.Log?.LogInformation("📊 Cache de tipos actualizado: {count} registros en UI", _tipoItems.Count);
            }
            else
            {
                App.Log?.LogWarning("⚠️ No se pudieron cargar tipos (response null o cancelado)");
            }
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogDebug("🚫 Carga de tipos cancelada");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando catálogo de tipos");
        }
    }

    private bool IsTiposCacheValid()
    {
        if (_tiposCache == null || _tiposCacheLoadedAt == null)
            return false;
        
        var age = DateTime.Now - _tiposCacheLoadedAt.Value;
        return age < CacheDuration;
    }

    /// <summary>
    /// Método público para refrescar el cache de tipos manualmente
    /// </summary>
    public static void InvalidateTiposCache()
    {
        _tiposCache = null;
        _tiposCacheLoadedAt = null;
        App.Log?.LogInformation("Cache de tipos invalidado");
    }

    // ===================== CLIENTES =====================

    /// <summary>
    /// Carga clientes desde cache o API según sea necesario
    /// </summary>
    private async Task LoadClientesAsync()
    {
        App.Log?.LogInformation("🔄 LoadClientesAsync iniciado - Cache válido: {valid}", IsCacheValid());
        
        if (_clientesLoaded && IsCacheValid())
        {
            App.Log?.LogDebug("✅ Usando cache de clientes ({count} items)",
                _clientesCache!.Count);
            return;
        }
        
        try
        {
            _clienteLoadCts?.Cancel();
            _clienteLoadCts = new CancellationTokenSource();
            var ct = _clienteLoadCts.Token;
            
            var path = "/api/v1/catalog/clientes?limit=200&offset=0";
            App.Log?.LogInformation("🔄 Llamando a API: {path}", path);
            
            var response = await App.Api.GetAsync<ClienteResponse[]>(path, ct);
            
            if (response != null && !ct.IsCancellationRequested)
            {
                _clientesCache = response.ToList();
                _cacheLoadedAt = DateTime.Now;
                
                App.Log?.LogInformation("✅ Cache de clientes guardado: {count} items", _clientesCache.Count);
                
                _clienteItems.Clear();
                
                var clientesValidos = _clientesCache
                    .Where(c => !string.IsNullOrWhiteSpace(c.Nombre))
                    .OrderBy(c => c.Nombre)
                    .ToList();
                
                foreach (var cliente in clientesValidos)
                {
                    _clienteItems.Add(cliente.Nombre);
                }
                
                _clientesLoaded = true;
                
                App.Log?.LogInformation("📊 Cache de clientes actualizado: {count} registros en UI", _clienteItems.Count);
            }
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogDebug("🚫 Carga de clientes cancelada");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando catálogo de clientes");
        }
    }

    /// <summary>
    /// Verifica si el cache es válido (no expirado)
    /// </summary>
    private bool IsCacheValid()
    {
        if (_clientesCache == null || _cacheLoadedAt == null)
            return false;
        
        var age = DateTime.Now - _cacheLoadedAt.Value;
        return age < CacheDuration;
    }

    /// <summary>
    /// Método público para refrescar el cache manualmente
    /// </summary>
    public static void InvalidateClientesCache()
    {
        _clientesCache = null;
        _cacheLoadedAt = null;
        App.Log?.LogInformation("Cache de clientes invalidado");
    }

    private async void OnTextBoxEnterKey(object? sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            // Si es un campo de hora, validar y formatear antes de mover el foco
            if (sender is TextBox textBox && 
                (textBox.Name == "TxtHoraInicio" || textBox.Name == "TxtHoraFin"))
            {
                // Obtener solo dígitos del texto actual
                var text = textBox.Text ?? string.Empty;
                var digits = new string(text.Where(char.IsDigit).ToArray());
                
                if (digits.Length >= 4)
                {
                    // Ya tiene 4 dígitos, asegurar formato HH:mm
                    var hh = digits[..2];
                    var mm = digits[2..4];
                    
                    // Validar rangos
                    if (int.TryParse(hh, out var h) && int.TryParse(mm, out var m) &&
                        h >= 0 && h <= 23 && m >= 0 && m <= 59)
                    {
                        textBox.Text = $"{hh}:{mm}";
                    }
                    else
                    {
                        // Hora inválida, no mover el foco
                        e.Handled = true;
                        return;
                    }
                }
                else if (digits.Length > 0 && digits.Length < 4)
                {
                    // Hora incompleta, no mover el foco
                    e.Handled = true;
                    return;
                }
                // Si está vacío (digits.Length == 0), permitir navegación
            }
            
            // Navegar al siguiente campo usando Tab simulado
            MoveToNextControl(sender as Control);
            e.Handled = true;
        }
    }

    private async void OnComboBoxEnterKey(object? sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (sender is ComboBox combo)
            {
                App.Log?.LogDebug("📥 Enter presionado en ComboBox: {name}", combo.Name);
                
                // Si el dropdown está abierto y hay un item seleccionado en la lista, usarlo
                if (combo.IsDropDownOpen && combo.SelectedItem != null)
                {
                    // Ya hay un item seleccionado, cerrarlo
                    combo.IsDropDownOpen = false;
                    App.Log?.LogDebug("📥 Dropdown cerrado, item ya seleccionado");
                }
                else if (combo.IsDropDownOpen)
                {
                    // Dropdown abierto pero sin selección específica
                    // Intentar buscar coincidencia con el texto escrito
                    var text = combo.Text?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(text))
                    {
                        // Buscar item que coincida (case-insensitive)
                        var matchingItem = combo.Items.Cast<string>()
                            .FirstOrDefault(item => item.Equals(text, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchingItem != null)
                        {
                            combo.SelectedItem = matchingItem;
                            App.Log?.LogDebug("📥 Item encontrado por texto: {item}", matchingItem);
                        }
                    }
                    combo.IsDropDownOpen = false;
                }
                else
                {
                    // Dropdown cerrado, simplemente validar que hay un valor
                    App.Log?.LogDebug("📥 Dropdown cerrado, valor actual: {value}", combo.SelectedItem ?? combo.Text);
                }
                
                // 🔧 CLAVE: Marcar como "recién seleccionado" para evitar reapertura
                if (combo.Name == "CmbGrupo")
                {
                    _grupoJustSelected = true;
                    App.Log?.LogDebug("📥 Grupo marcado como justSelected");
                }
                else if (combo.Name == "CmbTipo")
                {
                    _tipoJustSelected = true;
                    App.Log?.LogDebug("📥 Tipo marcado como justSelected");
                }
                
                // Marcar como modificado si es necesario
                OnFieldChanged(combo, null!);
                
                // Navegar al siguiente campo usando Tab simulado
                MoveToNextControl(combo);
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Mueve el foco al siguiente control según el orden de TabIndex
    /// </summary>
    private void MoveToNextControl(Control? currentControl)
    {
        if (currentControl == null) return;
        
        var currentTabIndex = currentControl.TabIndex;
        App.Log?.LogDebug("Moviendo desde {name} (TabIndex={index})", currentControl.Name, currentTabIndex);
        
        // Buscar el siguiente control con TabIndex mayor
        var nextControl = FindNextTabControl(currentTabIndex);
        
        if (nextControl != null)
        {
            App.Log?.LogDebug("Siguiente control: {name} (TabIndex={index})", nextControl.Name, nextControl.TabIndex);
            nextControl.Focus(FocusState.Keyboard);
        }
        else
        {
            App.Log?.LogDebug("No se encontró siguiente control");
        }
    }
    
    /// <summary>
    /// Encuentra el siguiente control según TabIndex
    /// </summary>
    private Control? FindNextTabControl(int currentTabIndex)
    {
        // Lista de controles en orden de TabIndex
        var controls = new List<(Control control, int tabIndex)>
        {
            (DpFecha, DpFecha.TabIndex),
            (TxtCliente, TxtCliente.TabIndex),
            (TxtTienda, TxtTienda.TabIndex),
            (TxtHoraInicio, TxtHoraInicio.TabIndex),
            (TxtHoraFin, TxtHoraFin.TabIndex),
            (TxtTicket, TxtTicket.TabIndex),
            (CmbGrupo, CmbGrupo.TabIndex),
            (CmbTipo, CmbTipo.TabIndex),
            (TxtAccion, TxtAccion.TabIndex),
            (BtnGuardar, BtnGuardar.TabIndex),
            (BtnCancelar, BtnCancelar.TabIndex),
            (BtnSalir, BtnSalir.TabIndex)
        };

        // Filtrar controles con TabIndex mayor al atual, ordenar y tomar el primero
        var nextControl = controls
            .Where(c => c.tabIndex > currentTabIndex && c.control.IsTabStop)
            .OrderBy(c => c.tabIndex)
            .FirstOrDefault();
        
        return nextControl.control;
    }

    private void OnAccionKeyDown(object? sender, KeyRoutedEventArgs e)
    {
        // Ctrl+Enter para guardar
        if (e.Key == Windows.System.VirtualKey.Enter && 
            (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control) & 
             Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
        {
            if (BtnGuardar.IsEnabled)
            {
                OnGuardarClick(sender, null!);
                e.Handled = true;
            }
        }
    }

    private void OnAccionGotFocus(object? sender, RoutedEventArgs e)
    {
        // Cuando TxtAccion recibe foco, simplemente posicionar el cursor
        // No es necesario hacer nada especial aquí por ahora
        App.Log?.LogDebug("📝 TxtAccion recibió foco");
        
        // Si el TextBox está vacío, insertar timestamp inicial
        if (string.IsNullOrWhiteSpace(TxtAccion.Text))
        {
            InsertTimestampAtCursor();
        }
    }
    
    // ===================== TIMESTAMP AUTOMÁTICO EN ACCIÓN =====================
    
    /// <summary>
    /// Intercepta teclas antes de que se procesen para manejar Enter y detectar inicio de línea
    /// </summary>
    private void OnAccionPreviewKeyDown(object? sender, KeyRoutedEventArgs e)
    {
        if (_suppressAccionTimestamp) return;
        
        var textBox = sender as TextBox;
        if (textBox == null) return;
        
        // Interceptar Enter para añadir timestamp en nueva línea
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true; // Prevenir comportamiento por defecto
            
            _suppressAccionTimestamp = true;
            
            var cursorPos = textBox.SelectionStart;
            var text = textBox.Text ?? string.Empty;
            
            // Insertar salto de línea + timestamp
            var timestamp = GetCurrentTimestamp();
            var newText = text.Insert(cursorPos, "\r\n" + timestamp);
            
            textBox.Text = newText;
            textBox.SelectionStart = cursorPos + 2 + timestamp.Length; // Posicionar después de "\r\nHH:mm "
            
            _suppressAccionTimestamp = false;
            
            App.Log?.LogDebug("📝 Enter en Acción - Timestamp insertado: {timestamp}", timestamp);
            return;
        }
        
        // Ctrl+Enter para guardar (comportamiento existente)
        if (e.Key == Windows.System.VirtualKey.Enter && 
            (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control) & 
             Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
        {
            if (BtnGuardar.IsEnabled)
            {
                OnGuardarClick(sender, null!);
                e.Handled = true;
            }
        }
    }
    
    /// <summary>
    /// Se dispara cuando el texto está cambiando (antes de TextChanged)
    /// Usado para detectar cuando el usuario empieza a escribir al inicio de una línea
    /// </summary>
    private void OnAccionTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        if (_suppressAccionTimestamp) return;
        
        // Solo procesar si el cambio es por input del usuario (no programático)
        if (!args.IsContentChanging) return;
        
        var cursorPos = sender.SelectionStart;
        var text = sender.Text ?? string.Empty;
        
        // Verificar si estamos al inicio de una línea sin timestamp
        if (IsAtStartOfLineWithoutTimestamp(text, cursorPos))
        {
            _suppressAccionTimestamp = true;
            
            // Insertar timestamp en la posición actual
            var timestamp = GetCurrentTimestamp();
            var lineStart = GetLineStartPosition(text, cursorPos);
            
            sender.Text = text.Insert(lineStart, timestamp);
            sender.SelectionStart = lineStart + timestamp.Length;
            
            _suppressAccionTimestamp = false;
            
            App.Log?.LogDebug("? Inicio de línea sin timestamp - Insertado: {timestamp}", timestamp);
        }
    }
    
    /// <summary>
    /// Inserta timestamp en la posición actual del cursor
    /// </summary>
    private void InsertTimestampAtCursor()
    {
        if (_suppressAccionTimestamp) return;
        
        _suppressAccionTimestamp = true;
        
        var timestamp = GetCurrentTimestamp();
        var cursorPos = TxtAccion.SelectionStart;
        var text = TxtAccion.Text ?? string.Empty;
        
        TxtAccion.Text = text.Insert(cursorPos, timestamp);
        TxtAccion.SelectionStart = cursorPos + timestamp.Length;
        
        _suppressAccionTimestamp = false;
        
        App.Log?.LogDebug("? Timestamp insertado manualmente: {timestamp}", timestamp);
    }
    
    /// <summary>
    /// Obtiene el timestamp actual en formato "HH:mm "
    /// </summary>
    private string GetCurrentTimestamp()
    {
        return DateTime.Now.ToString("HH:mm") + " ";
    }
    
    /// <summary>
    /// Verifica si el cursor está al inicio de una línea sin timestamp
    /// </summary>
    private bool IsAtStartOfLineWithoutTimestamp(string text, int cursorPos)
    {
        if (string.IsNullOrEmpty(text)) return true;
        
        // Obtener el inicio de la línea actual
        var lineStart = GetLineStartPosition(text, cursorPos);
        
        // Si estamos al inicio del texto
        if (lineStart == 0 && cursorPos <= 6)
        {
            // Verificar si NO empieza con timestamp (patrón HH:mm)
            return !HasTimestampAt(text, 0);
        }
        
        // Si estamos al inicio de una línea después de un salto
        if (lineStart > 0 && cursorPos == lineStart)
        {
            return !HasTimestampAt(text, lineStart);
        }
        
        return false;
    }
    
    /// <summary>
    /// Obtiene la posición de inicio de la línea actual
    /// </summary>
    private int GetLineStartPosition(string text, int cursorPos)
    {
        if (string.IsNullOrEmpty(text) || cursorPos == 0) return 0;
        
        // Buscar hacia atrás desde cursorPos hasta encontrar \n
        for (int i = cursorPos - 1; i >= 0; i--)
        {
            if (text[i] == '\n')
            {
                return i + 1; // Retornar posición después del \n
            }
        }
        
        return 0; // Estamos en la primera línea
    }
    
    /// <summary>
    /// Verifica si hay un timestamp en la posición especificada
    /// Formato esperado: "HH:mm " (5 caracteres + espacio)
    /// </summary>
    private bool HasTimestampAt(string text, int position)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (position + 6 > text.Length) return false;
        
        var substring = text.Substring(position, 6);
        
        // Verificar patrón: DD:DD + espacio
        if (substring.Length < 6) return false;
        if (substring[2] != ':') return false;
        if (substring[5] != ' ') return false;
        
        // Verificar que sean dígitos
        return char.IsDigit(substring[0]) && 
               char.IsDigit(substring[1]) && 
               char.IsDigit(substring[3]) && 
               char.IsDigit(substring[4]);
    }

    public void SetParentWindow(Microsoft.UI.Xaml.Window window)
    {
        _parentWindow = window;
        
        // NO redimensionar aquí - se hace desde DiarioPage después de Activate()
    }

    public async void NewParte()
    {
        var horaInicioNow = DateTime.Now.ToString("HH:mm");
        
        // Actualizar título del banner
        TxtTituloParte.Text = "Nuevo Parte";
        
        // ? Actualizar badge de estado para nuevo parte
        TxtEstadoParte.Text = "Abierto";
        BadgeEstado.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Verde #10B981
        
        Parte = new ParteDto
        {
            Fecha = DateTime.Today,
            DuracionMin = 0,
            Cliente = string.Empty,
            Tienda = string.Empty,
            Accion = string.Empty,
            HoraInicio = horaInicioNow,
            HoraFin = string.Empty,
            Ticket = string.Empty,
            Grupo = string.Empty,
            Tipo = string.Empty,
            Tecnico = string.Empty,
            EstadoNombre = string.Empty,
            EstadoInt = 0
        };

        App.Log?.LogInformation("PARTE_CREATE_ABIERTO: Nuevo parte con hora_inicio={horaInicio}, estado=0 (Abierto)", horaInicioNow);

        DpFecha.Date = DateTime.Today;
        TxtCliente.Text = "";  // AutoSuggestBox vacío
        TxtTienda.Text = "";
        TxtAccion.Text = "";
        TxtHoraInicio.Text = horaInicioNow;
        TxtHoraFin.Text = "";
        TxtDuracion.Text = "0";
        TxtTicket.Text = "";
        CmbGrupo.SelectedIndex = -1;
        CmbTipo.SelectedIndex = -1;
        TxtTecnico.Text = "";
        TxtEstado.Text = "";

        // Asegurar renderizado inicial y colocar foco
        await Task.Delay(50);
        TxtCliente.Focus(FocusState.Programmatic);
    }

    public async void LoadParte(ParteDto parte)
    {
        if (parte == null) return;

        Parte = parte;

        // Actualizar título del banner
        TxtTituloParte.Text = "Editar Parte";
        
        // ? Actualizar badge de estado según el estado actual del parte
        UpdateEstadoBadge(parte.EstadoParte);

        DpFecha.Date = parte.Fecha;
        TxtTienda.Text = parte.Tienda ?? "";
        TxtAccion.Text = parte.Accion ?? "";
        TxtHoraInicio.Text = parte.HoraInicio ?? "";
        TxtHoraFin.Text = parte.HoraFin ?? "";
        TxtDuracion.Text = parte.DuracionMin.ToString();
        TxtTicket.Text = parte.Ticket ?? "";
        
        TxtTecnico.Text = parte.Tecnico ?? "";
        TxtEstado.Text = parte.Estado ?? "";
        
        App.Log?.LogInformation("🔄 Cargando catálogos para selección inicial...");
        
        // Cargar clientes si no están cargados
        if (!_clientesLoaded || !IsCacheValid())
        {
            await LoadClientesAsync();
        }
        
        // Cargar grupos si no están cargados
        if (!_gruposLoaded || !IsGruposCacheValid())
        {
            await LoadGruposAsync();
        }
        
        // Cargar tipos si no están cargados
        if (!_tiposLoaded || !IsTiposCacheValid())
        {
            await LoadTiposAsync();
        }
        
        // Seleccionar el cliente correcto
        if (!string.IsNullOrWhiteSpace(parte.Cliente))
        {
            var clienteIndex = _clienteItems.IndexOf(parte.Cliente);
            if (clienteIndex >= 0)
            {
                TxtCliente.Text = parte.Cliente;
                App.Log?.LogInformation("✅ Cliente seleccionado al cargar: {cliente} (index: {index})", parte.Cliente, clienteIndex);
            }
            else
            {
                App.Log?.LogWarning("⚠️ Cliente '{cliente}' no encontrado en catálogo", parte.Cliente);
            }
        }
        
        // Seleccionar el grupo correcto
        if (!string.IsNullOrWhiteSpace(parte.Grupo))
        {
            var grupoIndex = _grupoItems.IndexOf(parte.Grupo);
            if (grupoIndex >= 0)
            {
                CmbGrupo.SelectedIndex = grupoIndex;
                App.Log?.LogInformation("✅ Grupo seleccionado al cargar: {grupo} (index: {index})", parte.Grupo, grupoIndex);
            }
            else
            {
                CmbGrupo.Text = parte.Grupo;
                App.Log?.LogWarning("⚠️ Grupo '{grupo}' no encontrado en catálogo, usando texto libre", parte.Grupo);
            }
        }
        
        // Seleccionar el tipo correcto
        if (!string.IsNullOrWhiteSpace(parte.Tipo))
        {
            var tipoIndex = _tipoItems.IndexOf(parte.Tipo);
            if (tipoIndex >= 0)
            {
                CmbTipo.SelectedIndex = tipoIndex;
                App.Log?.LogInformation("✅ Tipo seleccionado al cargar: {tipo} (index: {index})", parte.Tipo, tipoIndex);
            }
            else
            {
                CmbTipo.Text = parte.Tipo;
                App.Log?.LogWarning("⚠️ Tipo '{tipo}' no encontrado en catálogo, usando texto libre", parte.Tipo);
            }
        }
        
        await Task.Delay(50);
        TxtCliente.Focus(FocusState.Programmatic);
        
        App.Log?.LogInformation("? LoadParte completado - Cliente: {cliente}, Grupo: {grupo}, Tipo: {tipo}, Estado: {estado}", 
            parte.Cliente, parte.Grupo, parte.Tipo, parte.EstadoTexto);
    }

    private async Task ShowErrorAsync(string message)
    {
        try
        {
            var dlg = new ContentDialog
            {
                Title = "GestionTime",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dlg.ShowAsync();
        }
        catch
        {
            // no romper UI
        }
    }

    private string? NormalizeHora(string? value)
    {
        var txt = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(txt))
            return string.Empty;

        // Mantener solo dígitos y limitar a 4
        var digits = new string(txt.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return string.Empty;
        if (digits.Length < 4)
            return null; // incompleto
        if (digits.Length > 4)
            digits = digits[..4];

        var hh = digits[..2];
        var mm = digits[2..];
        if (!int.TryParse(hh, out var h) || h < 0 || h > 23)
            return null;
        if (!int.TryParse(mm, out var m) || m < 0 || m > 59)
            return null;

        return $"{h:00}:{m:00}";
    }

    /// <summary>
    /// Request para crear/actualizar un parte según la API real.
    /// POST /api/v1/partes (creación) - usa CreateParteRequest
    /// PUT /api/v1/partes/{id} (actualización) - usa UpdateParteRequest
    /// 
    /// Campos requeridos: fecha_trabajo, hora_inicio, hora_fin, id_cliente, accion
    /// Estado es INT: 0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado
    /// </summary>
    private sealed class ParteRequest
    {
        [JsonPropertyName("fecha_trabajo")]
        public DateTime FechaTrabajo { get; set; }

        [JsonPropertyName("hora_inicio")]
        public string HoraInicio { get; set; } = string.Empty;

        [JsonPropertyName("hora_fin")]
        public string HoraFin { get; set; } = string.Empty;

        [JsonPropertyName("id_cliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("tienda")]
        public string? Tienda { get; set; }

        [JsonPropertyName("id_grupo")]
        public int? IdGrupo { get; set; }

        [JsonPropertyName("id_tipo")]
        public int? IdTipo { get; set; }

        [JsonPropertyName("accion")]
        public string Accion { get; set; } = string.Empty;

        [JsonPropertyName("ticket")]
        public string? Ticket { get; set; }

        /// <summary>
        /// Estado del parte (int): 0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado
        /// Solo se envía en PUT (actualización), no en POST (creación)
        /// </summary>
        [JsonPropertyName("estado")]
        public int? Estado { get; set; }
    }

    private async void OnGuardarClick(object? sender, RoutedEventArgs e)
    {
        if (Parte == null) return;

        try
        {
            Parte.Fecha = DpFecha.Date?.DateTime ?? DateTime.Today;

            // Obtener cliente del texto del AutoSuggestBox
            Parte.Cliente = TxtCliente.Text?.Trim() ?? string.Empty;

            Parte.Tienda = TxtTienda.Text?.Trim() ?? string.Empty;
            Parte.Accion = TxtAccion.Text?.Trim() ?? string.Empty;
            Parte.HoraInicio = TxtHoraInicio.Text?.Trim() ?? string.Empty;
            Parte.HoraFin = TxtHoraFin.Text?.Trim() ?? string.Empty;

            // Validar hora inicio (obligatoria)
            var horaInicio = NormalizeHora(Parte.HoraInicio);
            if (horaInicio == null)
            {
                await ShowErrorAsync("Hora inicio inválida (usa HH:mm)");
                return;
            }

            // Validar hora fin (obligatoria para la API)
            string horaFin;
            if (string.IsNullOrWhiteSpace(Parte.HoraFin))
            {
                // HoraFin vacío - usar hora actual como valor por defecto para partes nuevos
                horaFin = Parte.Id > 0 ? "00:00" : DateTime.Now.ToString("HH:mm");
                App.Log?.LogDebug("Parte sin hora_fin → usando: {horaFin}", horaFin);
            }
            else
            {
                var normalizedHoraFin = NormalizeHora(Parte.HoraFin);
                if (normalizedHoraFin == null)
                {
                    await ShowErrorAsync("Hora fin inválida (usa HH:mm)");
                    return;
                }
                horaFin = normalizedHoraFin;
            }

            Parte.HoraInicio = horaInicio;
            Parte.HoraFin = horaFin;

            Parte.Ticket = TxtTicket.Text?.Trim() ?? string.Empty;

            // Obtener valor de ComboBox (usar SelectedItem)
            Parte.Grupo = CmbGrupo.SelectedItem as string ?? string.Empty;
            Parte.Tipo = CmbTipo.SelectedItem as string ?? string.Empty;
            
            App.Log?.LogInformation("---------------------------------------------------------------");
            App.Log?.LogInformation("🔧 VALORES AL GUARDAR:");
            App.Log?.LogInformation("   Cliente = '{cliente}'", Parte.Cliente);
            App.Log?.LogInformation("   Grupo = '{grupo}'", Parte.Grupo);
            App.Log?.LogInformation("   Tipo = '{tipo}'", Parte.Tipo);
            App.Log?.LogInformation("---------------------------------------------------------------");

            // Asegurar catálogos cargados para mapear IDs
            await LoadClientesAsync();
            await LoadGruposAsync();
            await LoadTiposAsync();

            var clienteId = _clientesCache?.FirstOrDefault(c => string.Equals(c.Nombre, Parte.Cliente, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
            var grupoMatch = _gruposCache?.FirstOrDefault(g => string.Equals(g.Nombre, Parte.Grupo, StringComparison.OrdinalIgnoreCase));
            var tipoMatch = _tiposCache?.FirstOrDefault(t => string.Equals(t.Nombre, Parte.Tipo, StringComparison.OrdinalIgnoreCase));
            
            var grupoId = grupoMatch?.Id_grupo;
            var tipoId = tipoMatch?.Id_tipo;
            
            App.Log?.LogInformation("📊 Mapeo de catálogos:");
            App.Log?.LogInformation("   Cliente: '{nombre}' → ID={id}", Parte.Cliente, clienteId);
            App.Log?.LogInformation("   Grupo: '{nombre}' → ID={id}", Parte.Grupo, grupoId?.ToString() ?? "null");
            App.Log?.LogInformation("   Tipo: '{nombre}' → ID={id}", Parte.Tipo, tipoId?.ToString() ?? "null");

            // IMPORTANTE: Para partes NUEVOS, el backend debe asignar automáticamente estado=0 (Abierto)
            // Para partes EXISTENTES, NO modificar el estado (el backend lo gestiona)
            var payload = new ParteRequest
            {
                FechaTrabajo = Parte.Fecha.Date,
                HoraInicio = Parte.HoraInicio,
                HoraFin = Parte.HoraFin,
                IdCliente = clienteId,
                Tienda = Parte.Tienda,
                IdGrupo = grupoId.HasValue && grupoId.Value > 0 ? grupoId : null,
                IdTipo = tipoId.HasValue && tipoId.Value > 0 ? tipoId : null,
                Accion = Parte.Accion,
                Ticket = Parte.Ticket
                // ⚠️ NO enviar Estado - el backend lo gestiona automáticamente
            };

            App.Log?.LogInformation("---------------------------------------------------------------");
            App.Log?.LogInformation("💾 GUARDANDO PARTE:");
            App.Log?.LogInformation("   • Es nuevo: {isNew}", Parte.Id == 0);
            App.Log?.LogInformation("   • Fecha: {fecha}", Parte.Fecha.ToString("yyyy-MM-dd"));
            App.Log?.LogInformation("   • Cliente: '{cliente}' (ID: {id})", Parte.Cliente, clienteId);
            App.Log?.LogInformation("   • Tienda: '{tienda}'", Parte.Tienda);
            App.Log?.LogInformation("   • HoraInicio: {inicio}", Parte.HoraInicio);
            App.Log?.LogInformation("   • HoraFin: {fin}", Parte.HoraFin);
            App.Log?.LogInformation("   • Acción: '{accion}'", Trim(Parte.Accion, 50));
            App.Log?.LogInformation("   • Ticket: '{ticket}'", Parte.Ticket);
            App.Log?.LogInformation("   • Grupo: '{grupo}' (ID: {id})", Parte.Grupo, grupoId?.ToString() ?? "null");
            App.Log?.LogInformation("   • Tipo: '{tipo}' (ID: {id})", Parte.Tipo, tipoId?.ToString() ?? "null");
            App.Log?.LogInformation("---------------------------------------------------------------");

            if (Parte.Id > 0)
            {
                // Editar parte existente
                App.Log?.LogInformation("PUT /api/v1/partes/{id} (edición)", Parte.Id);
                await App.Api.PutAsync<ParteRequest, ParteDto>($"/api/v1/partes/{Parte.Id}", payload);
                App.Log?.LogInformation("✅ Parte {id} actualizado correctamente", Parte.Id);
            }
            else
            {
                // Crear parte nuevo
                App.Log?.LogInformation("POST /api/v1/partes (creación)");
                var resultado = await App.Api.PostAsync<ParteRequest, ParteDto>("/api/v1/partes", payload);
                
                if (resultado != null)
                {
                    App.Log?.LogInformation("✅ Parte creado exitosamente con ID: {id}", resultado.Id);
                }
                else
                {
                    App.Log?.LogWarning("⚠️ Parte creado pero no se recibió confirmación del servidor");
                }
            }

            // 🆕 NUEVO: Invalidar el caché de partes después de guardar
            App.Log?.LogInformation("🗑️ Invalidando caché de partes...");
            InvalidatePartesCache(Parte.Fecha);
            
            Guardado = true;
            
            App.Log?.LogInformation("---------------------------------------------------------------");
            App.Log?.LogInformation("✅ GUARDADO COMPLETADO - Cerrando editor");
            App.Log?.LogInformation("---------------------------------------------------------------");
            
            _parentWindow?.Close();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ ERROR guardando parte");
            await ShowErrorAsync($"Error guardando parte: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 🆕 NUEVO: Invalida las entradas de caché relacionadas con un parte
    /// </summary>
    private void InvalidatePartesCache(DateTime fecha)
    {
        try
        {
            // Invalidar el endpoint de rango que cubre ±30 días
            var fromDate = fecha.AddDays(-30).ToString("yyyy-MM-dd");
            var toDate = fecha.AddDays(30).ToString("yyyy-MM-dd");
            
            var rangePath = $"/api/v1/partes?created_from={fromDate}&created_to={toDate}";
            App.Api.InvalidateCacheEntry(rangePath);
            App.Log?.LogDebug("🗑️ Caché invalidado: {path}", rangePath);
            
            // También invalidar la fecha específica (para el método legacy)
            var dayPath = $"/api/v1/partes?fecha={fecha:yyyy-MM-dd}";
            App.Api.InvalidateCacheEntry(dayPath);
            App.Log?.LogDebug("🗑️ Caché invalidado: {path}", dayPath);
            
            App.Log?.LogInformation("✅ Caché de partes invalidado correctamente");
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Error invalidando caché de partes");
        }
    }
    
    // ===================== GLOBAL =====================
    
    /// <summary>
    /// Se dispara cuando el tema global cambia desde otra ventana
    /// </summary>
    private void OnGlobalThemeChanged(object? sender, ElementTheme newTheme)
    {
        // Aplicar el nuevo tema a esta página
        this.RequestedTheme = newTheme;
        
        // Actualizar logo del banner
        UpdateBannerLogo();
        
        App.Log?.LogInformation("🎨 ParteItemEdit - Tema global cambiado a: {theme}", newTheme);
    }
    
    // ===================== MÉTODOS AUXILIARES =====================
    
    private void OnFieldChanged(object? sender, object e)
    {
        BtnGuardar.IsEnabled = true;
        if (BtnAccionGrabar != null)
            BtnAccionGrabar.IsEnabled = true;
    }

    private void OnHoraGotFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Marcar que la próxima tecla debe borrar el contenido
            if (textBox.Name == "TxtHoraInicio")
            {
                _horaInicioFirstKey = true;
                App.Log?.LogDebug("⌨️ HoraInicio recibió foco - próxima tecla borrará contenido");
            }
            else if (textBox.Name == "TxtHoraFin")
            {
                _horaFinFirstKey = true;
                App.Log?.LogDebug("⌨️ HoraFin recibió foco - próxima tecla borrará contenido");
            }
            
            // Seleccionar todo el texto para visualizar que se va a reemplazar
            textBox.SelectAll();
        }
    }

    private void OnHoraTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressHoraFormatting)
            return;

        if (sender is not TextBox txt)
            return;

        // Si es la primera tecla después de recibir foco, borrar y empezar de nuevo
        if ((txt.Name == "TxtHoraInicio" && _horaInicioFirstKey) ||
            (txt.Name == "TxtHoraFin" && _horaFinFirstKey))
        {
            // Obtener solo el último carácter escrito (el nuevo)
            var text = txt.Text ?? string.Empty;
            var digits = new string(text.Where(char.IsDigit).ToArray());
            
            // Si hay dígitos, tomar solo el último
            if (digits.Length > 0)
            {
                _suppressHoraFormatting = true;
                txt.Text = digits[^1].ToString(); // Tomar último dígito
                txt.SelectionStart = txt.Text.Length;
                _suppressHoraFormatting = false;
                
                App.Log?.LogDebug("⌨️ Campo de hora reiniciado con: {digit}", digits[^1]);
            }
            
            // Resetear flags
            if (txt.Name == "TxtHoraInicio")
                _horaInicioFirstKey = false;
            else if (txt.Name == "TxtHoraFin")
                _horaFinFirstKey = false;
            
            return;
        }

        var original = txt.Text ?? string.Empty;

        // Mantener solo dígitos y limitar a 4
        var allDigits = new string(original.Where(char.IsDigit).ToArray());
        if (allDigits.Length > 4)
            allDigits = allDigits[..4];

        string formatted;
        if (allDigits.Length == 0)
        {
            formatted = string.Empty;
        }
        else if (allDigits.Length <= 2)
        {
            formatted = allDigits;
        }
        else
        {
            // A partir de 3 dígitos, insertar dos puntos: HH:mm
            formatted = allDigits[..2] + ":" + allDigits[2..];
        }

        if (formatted.Length > 5)
            formatted = formatted[..5];

        if (!formatted.Equals(original, StringComparison.Ordinal))
        {
            _suppressHoraFormatting = true;

            txt.Text = formatted;
            // Colocar el cursor al final
            txt.SelectionStart = formatted.Length;

            _suppressHoraFormatting = false;
        }

        OnFieldChanged(sender, e);
    }

    private void OnCopiarClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implementar funcionalidad de copiar
    }

    private void OnPegarClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implementar funcionalidad de pegar
    }

    private void OnCancelarClick(object? sender, RoutedEventArgs e)
    {
        Guardado = false;
        _parentWindow?.Close();
    }

    private void OnSalirClick(object? sender, RoutedEventArgs e)
    {
        _parentWindow?.Close();
    }

    /// <summary>
    /// Carga la información del usuario desde LocalSettings y actualiza el banner
    /// </summary>
    private void LoadUserInfo()
    {
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
            
            App.Log?.LogInformation("📋 Cargando información de usuario en ParteItemEdit:");
            App.Log?.LogInformation("   • UserName: {name}", userName);
            App.Log?.LogInformation("   • UserEmail: {email}", userEmail);
            App.Log?.LogInformation("   • UserRole: {role}", userRole);
            
            // Actualizar banner
            TxtUserName.Text = userName;
            TxtUserEmail.Text = userEmail;
            TxtUserRole.Text = userRole;
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Error cargando información del usuario en ParteItemEdit");
            TxtUserName.Text = "Usuario";
            TxtUserEmail.Text = "usuario@empresa.com";
            TxtUserRole.Text = "Usuario";
        }
    }
    
    /// <summary>
    /// Actualiza el logo del banner según el tema actual
    /// </summary>
    private void UpdateBannerLogo()
    {
        var theme = this.RequestedTheme;
        
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
        }
        else
        {
            LogoImageBanner.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/LogoClaro.png"));
        }
        
        App.Log?.LogDebug("Logo actualizado para tema: {theme}", effectiveTheme);
    }
    
    /// <summary>
    /// Actualiza el badge de estado según el estado del parte
    /// </summary>
    private void UpdateEstadoBadge(ParteEstado estado)
    {
        string textoEstado;
        Windows.UI.Color colorBadge;
        
        switch (estado)
        {
            case ParteEstado.Abierto:
                textoEstado = "Abierto";
                colorBadge = Windows.UI.Color.FromArgb(255, 16, 185, 129); // Verde #10B981
                break;
                
            case ParteEstado.Pausado:
                textoEstado = "Pausado";
                colorBadge = Windows.UI.Color.FromArgb(255, 245, 158, 11); // Amarillo #F59E0B
                break;
                
            case ParteEstado.Cerrado:
                textoEstado = "Cerrado";
                colorBadge = Windows.UI.Color.FromArgb(255, 59, 130, 246); // Azul #3B82F6
                break;
                
            case ParteEstado.Enviado:
                textoEstado = "Enviado";
                colorBadge = Windows.UI.Color.FromArgb(255, 139, 92, 246); // Púrpura #8B5CF6
                break;
                
            case ParteEstado.Anulado:
                textoEstado = "Anulado";
                colorBadge = Windows.UI.Color.FromArgb(255, 239, 68, 68); // Rojo #EF4444
                break;
            
            default:
                textoEstado = "Desconocido";
                colorBadge = Windows.UI.Color.FromArgb(255, 107, 114, 128); // Gris #6B7280
                break;
        }
        
        TxtEstadoParte.Text = textoEstado;
        BadgeEstado.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(colorBadge);
        
 App.Log?.LogDebug("Badge de estado actualizado: {estado} (color: {color})", textoEstado, colorBadge);
    }
    
    /// <summary>
    /// Busca clientes en la API según el texto ingresado (búsqueda case-insensitive)
    /// </summary>
    private async Task SearchClientesAsync()
    {
        var query = TxtCliente.Text?.Trim() ?? string.Empty;
        
        // Si el texto está vacío, limpiar sugerencias
        if (string.IsNullOrWhiteSpace(query))
        {
            _clienteSuggestions.Clear();
            App.Log?.LogDebug("🔍 Búsqueda vacía - sugerencias limpiadas");
            return;
        }
        
        // Evitar búsquedas duplicadas
        if (query.Equals(_lastClienteQuery, StringComparison.OrdinalIgnoreCase))
        {
            App.Log?.LogDebug("🔍 Query igual a la anterior, saltando búsqueda");
            return;
        }
        
        _lastClienteQuery = query;
        
        try
        {
            // Cancelar búsqueda anterior
            _clienteSearchCts?.Cancel();
            _clienteSearchCts = new CancellationTokenSource();
            var ct = _clienteSearchCts.Token;
            
            App.Log?.LogInformation("🔍 Buscando clientes: '{query}'", query);
            
            // Llamar a la API con el parámetro de búsqueda
            var path = $"/api/v1/catalog/clientes?q={Uri.EscapeDataString(query)}&limit=20&offset=0";
            var response = await App.Api.GetAsync<ClienteResponse[]>(path, ct);
            
            if (response != null && !ct.IsCancellationRequested)
            {
                _clienteSuggestions.Clear();
                
                foreach (var cliente in response)
                {
                    if (!string.IsNullOrWhiteSpace(cliente.Nombre))
                    {
                        _clienteSuggestions.Add(cliente.Nombre);
                    }
                }
                
                App.Log?.LogInformation("✅ Encontrados {count} clientes para '{query}'", _clienteSuggestions.Count, query);
                
                // Si hay una sola sugerencia o el texto coincide exactamente, actualizar automáticamente
                if (_clienteSuggestions.Count == 1)
                {
                    var onlySuggestion = _clienteSuggestions[0];
                    App.Log?.LogDebug("💡 Una sola sugerencia encontrada: '{suggestion}'", onlySuggestion);
                    
                    // Si el usuario escribió texto que coincide parcialmente, completar
                    if (onlySuggestion.StartsWith(query, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(query, onlySuggestion, StringComparison.OrdinalIgnoreCase))
                    {
                        // Actualizar el texto con la sugerencia completa
                        TxtCliente.Text = onlySuggestion;
                        App.Log?.LogDebug("✨ Auto-completado: '{query}' → '{suggestion}'", query, onlySuggestion);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogDebug("🚫 Búsqueda de clientes cancelada");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error buscando clientes");
            _clienteSuggestions.Clear();
        }
    }
    
    // ===================== AUTOCOMPLETE CLIENTE =====================
    
    /// <summary>
    /// Se dispara cuando el usuario escribe en el campo Cliente
    /// </summary>
    private void OnClienteTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Solo buscar si el usuario está escribiendo (no si selecciona una sugerencia)
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text?.Trim() ?? string.Empty;
            
            App.Log?.LogDebug("📝 Cliente texto cambiado: '{query}' (Reason: UserInput)", query);
            
            // Reiniciar timer de búsqueda (debounce)
            _clienteSearchTimer?.Stop();
            _clienteSearchTimer?.Start();
        }
    }
    
    /// <summary>
    /// Se dispara cuando el usuario selecciona una sugerencia de la lista
    /// </summary>
    private void OnClienteSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string selectedCliente)
        {
            App.Log?.LogInformation("✅ Cliente seleccionado: {cliente}", selectedCliente);
            sender.Text = selectedCliente;
            OnFieldChanged(sender, null!);
        }
    }
    
    /// <summary>
    /// Se dispara cuando el usuario presiona Enter o selecciona definitivamente
    /// </summary>
    private void OnClienteQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        string selectedCliente;
        
        if (args.ChosenSuggestion != null)
        {
            // Usuario seleccionó de la lista con Enter
            selectedCliente = args.ChosenSuggestion.ToString() ?? string.Empty;
            App.Log?.LogInformation("✅ Cliente confirmado desde lista: '{cliente}'", selectedCliente);
        }
        else
        {
            // Usuario escribió y presionó Enter
            var queryText = args.QueryText?.Trim() ?? string.Empty;
            
            // Si hay sugerencias disponibles, usar la primera automáticamente
            if (_clienteSuggestions.Count > 0)
            {
                selectedCliente = _clienteSuggestions[0];
                App.Log?.LogInformation("✨ Auto-seleccionada primera sugerencia: '{cliente}'", selectedCliente);
            }
            else
            {
                // No hay sugerencias, usar texto libre
                selectedCliente = queryText;
                App.Log?.LogInformation("📝 Cliente texto libre: '{cliente}'", selectedCliente);
            }
        }
        
        sender.Text = selectedCliente;
        OnFieldChanged(sender, null!);
        
        // Mover foco al siguiente campo (Tienda)
        TxtTienda.Focus(FocusState.Keyboard);
    }
    
    /// <summary>
    /// Helper para truncar strings en logs
    /// </summary>
    private static string Trim(string? s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "(vacío)";
        if (s.Length <= maxLen) return s;
        return s.Substring(0, maxLen) + "...";
    }
}

// ==================== DTOs DE RESPUESTA DEL API ====================

// Clase DTO para respuesta de clientes
public class ClienteResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}   

// Clase DTO para respuesta de grupos
public class GrupoResponse
{
    [JsonPropertyName("id_grupo")]
    public int Id_grupo { get; set; }
    
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

// Clase DTO para respuesta de tipos
public class TipoResponse
{
    [JsonPropertyName("id_tipo")]
    public int Id_tipo { get; set; }
    
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}
