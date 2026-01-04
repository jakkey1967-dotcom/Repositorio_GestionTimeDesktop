using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.Models.Dtos;
using GestionTime.Desktop.Services;

namespace GestionTime.Desktop.Views;

public sealed partial class ParteItemEdit : Page
{
    public ParteDto? Parte { get; private set; }
    public bool Guardado { get; private set; }
    
    private Microsoft.UI.Xaml.Window? _parentWindow;
    
    // 🆕 NUEVO: Gestor centralizado de catálogos
    private readonly CatalogManager _catalogManager = new();
    
    // 🆕 NUEVO: Gestores de eventos para ComboBox
    private ComboBoxEventManager? _grupoEventManager;
    private ComboBoxEventManager? _tipoEventManager;
    
    // Cache local de clientes (todavía usado para compatibilidad)
    private static List<ClienteResponse>? _clientesCache = null;
    private static DateTime? _cacheLoadedAt = null;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    
    // Cache local de grupos (usado por ComboBoxEventManager)
    private static List<GrupoResponse>? _gruposCache = null;
    private static DateTime? _gruposCacheLoadedAt = null;
    
    // Cache local de tipos (usado por ComboBoxEventManager)
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
    
    // Items de Grupo (usados por ComboBoxEventManager)
    private ObservableCollection<string> _grupoItems = new();
    
    // Items de Tipo (usados por ComboBoxEventManager)
    private ObservableCollection<string> _tipoItems = new();
    
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
        
        App.Log?.LogDebug("✅ AutoSuggestBox Cliente configurado com búsqueda dinámica");
        
        // Configurar ComboBox de Grupo (solo lectura)
        CmbGrupo.ItemsSource = _grupoItems;
        App.Log?.LogDebug("✅ CmbGrupo.ItemsSource configurado con ObservableCollection vacía");
        
        // Configurar ComboBox de Tipo (solo lectura)
        CmbTipo.ItemsSource = _tipoItems;
        App.Log?.LogDebug("✅ CmbTipo.ItemsSource configurado con ObservableCollection vacía");
        
        // 🆕 NUEVO: Configurar gestores de eventos para ComboBox
        _grupoEventManager = new ComboBoxEventManager(
            CmbGrupo, _grupoItems, _catalogManager, 
            MoveToNextControl, OnFieldChanged, "Grupo");
        
        _tipoEventManager = new ComboBoxEventManager(
            CmbTipo, _tipoItems, _catalogManager, 
            MoveToNextControl, OnFieldChanged, "Tipo");
        
        App.Log?.LogDebug("✅ Gestores de eventos ComboBox configurados");
        
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
    
    /// <summary>Handler ejecutado cuando un control recibe foco.</summary>
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
    
    // ⚠️ Métodos movidos a ComboBoxEventManager
    // OnGrupoGotFocus, OnGrupoPreviewKeyDown, OnGrupoDropDownOpened, OnGrupoSelectionChanged
    // LoadGruposAsync, IsGruposCacheValid, InvalidateGruposCache
    
    // ===================== TIPO =====================
    
    // ⚠️ Métodos movidos a ComboBoxEventManager
    // OnTipoGotFocus, OnTipoPreviewKeyDown, OnTipoDropDownOpened, OnTipoSelectionChanged
    // LoadTiposAsync, IsTiposCacheValid, InvalidateTiposCache
    
    /// <summary>Método público para invalidar el cache de clientes manualmente.</summary>
    public static void InvalidateClientesCache()
    {
        _clientesCache = null;
        _cacheLoadedAt = null;
        App.Log?.LogInformation("Cache de clientes invalidado");
    }

    // ===================== CLIENTES =====================

    /// <summary>Carga clientes desde cache o API según sea necesario.</summary>
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
                    // Ya tiene 4 dígitos, assurance formato HH:mm
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
                    combo.IsDropDownOpen = false;
                    App.Log?.LogDebug("📥 Dropdown cerrado, item ya seleccionado");
                }
                else if (combo.IsDropDownOpen)
                {
                    // Dropdown abierto pero sin selección específica
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
                
                // Marcar como modificado
                OnFieldChanged(combo, null!);
                
                // Navegar al siguiente campo
                MoveToNextControl(combo);
                e.Handled = true;
            }
        }
    }

    /// <summary>Mueve el foco al siguiente control según el orden de TabIndex.</summary>
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
    
    /// <summary>Encuentra el siguiente control navegable según su TabIndex.</summary>
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
        App.Log?.LogDebug("📝 TxtAccion recibió foco");
        
        // 🔧 CORREGIDO: Solo insertar timestamp si está COMPLETAMENTE vacío
        // NO insertar si ya tiene contenido (evita duplicación)
        if (string.IsNullOrWhiteSpace(TxtAccion.Text))
        {
            App.Log?.LogDebug("📝 Campo vacío - Insertando timestamp inicial");
            InsertTimestampAtCursor();
        }
        else
        {
            App.Log?.LogDebug("📝 Campo tiene contenido - NO insertar timestamp");
        }
    }
    
    // ===================== TIMESTAMP AUTOMÁTICO EN ACCIÓN =====================
    
    /// <summary>
    /// Intercepta teclas antes de procesarlas para manejar Enter y detectar inicio de línea.
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
    /// Se dispara cuando el texto está cambiando (antes de TextChanged).
    /// </summary>
    /// <remarks>Deshabilitado para evitar inserciones continuas de timestamp.</remarks>
    private void OnAccionTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        if (_suppressAccionTimestamp) return;
        
        // ❌ DESHABILITADO: Este método causaba inserciones continuas de timestamp
        // Solo OnAccionPreviewKeyDown y OnAccionGotFocus deben insertar timestamps
        return;
    }
    
    /// <summary>Inserta timestamp en formato HH:mm en la posición actual del cursor.</summary>
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
    
    /// <summary>Obtiene el timestamp actual en formato "HH:mm " con espacio final.</summary>
    private string GetCurrentTimestamp()
    {
        return DateTime.Now.ToString("HH:mm") + " ";
    }
    
    /// <summary>Verifica si el cursor está al inicio de una línea sin timestamp.</summary>
    private bool IsAtStartOfLineWithoutTimestamp(string text, int cursorPos)
    {
        return ParteItemEditValidation.IsAtStartOfLineWithoutTimestamp(text, cursorPos);
    }
    
    private int GetLineStartPosition(string text, int cursorPos)
    {
        return ParteItemEditValidation.GetLineStartPosition(text, cursorPos);
    }
    
    private bool HasTimestampAt(string text, int position)
    {
        return ParteItemEditValidation.HasTimestampAt(text, position);
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
        
        // Actualizar badge de estado según el estado actual del parte
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
        
        // ✅ CORREGIDO: Cargar catálogos Y poblar ObservableCollections ANTES de seleccionar
        
        // 1. Cargar clientes
        if (!_clientesLoaded || !IsCacheValid())
        {
            await LoadClientesAsync();
        }
        
        // 2. Cargar grupos usando CatalogManager
        await _catalogManager.LoadGruposAsync();
        
        // 3. Poblar _grupoItems desde CatalogManager
        _grupoItems.Clear();
        var grupos = _catalogManager.GetAllGrupos();
        foreach (var grupo in grupos.OrderBy(g => g.Nombre))
        {
            _grupoItems.Add(grupo.Nombre);
        }
        App.Log?.LogInformation("📊 _grupoItems poblado con {count} items", _grupoItems.Count);
        
        // 4. Cargar tipos usando CatalogManager
        await _catalogManager.LoadTiposAsync();
        
        // 5. Poblar _tipoItems desde CatalogManager
        _tipoItems.Clear();
        var tipos = _catalogManager.GetAllTipos();
        foreach (var tipo in tipos.OrderBy(t => t.Nombre))
        {
            _tipoItems.Add(tipo.Nombre);
        }
        App.Log?.LogInformation("📊 _tipoItems poblado con {count} items", _tipoItems.Count);
        
        // ✅ AHORA SÍ: Seleccionar valores en los ComboBox (las colecciones ya están pobladas)
        
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
                TxtCliente.Text = parte.Cliente;
                App.Log?.LogWarning("⚠️ Cliente '{cliente}' no encontrado en catálogo, usando texto libre", parte.Cliente);
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
        
        App.Log?.LogInformation("✅ LoadParte completado - Cliente: {cliente}, Grupo: {grupo} ({grupoIdx}), Tipo: {tipo} ({tipoIdx}), Estado: {estado}", 
            parte.Cliente, parte.Grupo, CmbGrupo.SelectedIndex, parte.Tipo, CmbTipo.SelectedIndex, parte.EstadoTexto);
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
        return ParteItemEditValidation.NormalizeHora(value);
    }

    /// <summary>
    /// Request DTO para crear o actualizar un parte en la API.
    /// </summary>
    /// <remarks>POST /api/v1/partes (creación) o PUT /api/v1/partes/{id} (actualización).</remarks>
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

        /// <summary>Estado del parte como entero (0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado).</summary>
        /// <remarks>Solo se envía en PUT (actualización), no en POST (creación).</remarks>
        [JsonPropertyName("estado")]
        public int? Estado { get; set; }
    }

    private async void OnGuardarClick(object? sender, RoutedEventArgs e)
    {
        await GuardarAsync();
    }
    
    /// <summary>Guarda el parte y cierra la ventana automáticamente.</summary>
    private async void OnGuardarYCerrarClick(object? sender, RoutedEventArgs e)
    {
        await GuardarAsync();
    }
    
    /// <summary>Lógica centralizada de guardado del parte.</summary>
    private async Task GuardarAsync()
    {
        if (Parte == null) return;

        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("💾 INICIAR GUARDADO DE PARTE");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            
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
                App.Log?.LogWarning("❌ Validación fallida: Hora inicio inválida");
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
                    App.Log?.LogWarning("❌ Validación fallida: Hora fin inválida");
                    await ShowErrorAsync("Hora fin inválida (usa HH:mm)");
                    return;
                }
                horaFin = normalizedHoraFin;
            }

            Parte.HoraInicio = horaInicio;
            Parte.HoraFin = horaFin;

            Parte.Ticket = TxtTicket.Text?.Trim() ?? string.Empty;

            // ✅ CORREGIDO: Obtener valor de ComboBox desde .Text (soporta texto libre)
            Parte.Grupo = CmbGrupo.Text?.Trim() ?? string.Empty;
            Parte.Tipo = CmbTipo.Text?.Trim() ?? string.Empty;
            
            App.Log?.LogInformation("---------------------------------------------------------------");
            App.Log?.LogInformation("🔧 VALORES AL GUARDAR:");
            App.Log?.LogInformation("   ID Parte = {id} (0 = nuevo)", Parte.Id);
            App.Log?.LogInformation("   Fecha = {fecha}", Parte.Fecha.ToString("yyyy-MM-dd"));
            App.Log?.LogInformation("   Cliente = '{cliente}'", Parte.Cliente);
            App.Log?.LogInformation("   Tienda = '{tienda}'", Parte.Tienda);
            App.Log?.LogInformation("   HoraInicio = '{inicio}'", Parte.HoraInicio);
            App.Log?.LogInformation("   HoraFin = '{fin}'", Parte.HoraFin);
            App.Log?.LogInformation("   Ticket = '{ticket}'", Parte.Ticket);
            App.Log?.LogInformation("   Grupo = '{grupo}' (Text='{text}', SelectedItem='{selected}')", 
                Parte.Grupo, CmbGrupo.Text ?? "(null)", CmbGrupo.SelectedItem as string ?? "(null)");
            App.Log?.LogInformation("   Tipo = '{tipo}' (Text='{text}', SelectedItem='{selected}')", 
                Parte.Tipo, CmbTipo.Text ?? "(null)", CmbTipo.SelectedItem as string ?? "(null)");
            App.Log?.LogInformation("   Acción = '{accion}'", Trim(Parte.Accion, 100));
            App.Log?.LogInformation("---------------------------------------------------------------");

            // ✅ ASEGURAR catálogos cargados para mapear IDs
            App.Log?.LogInformation("📚 PASO 1: Cargar catálogos para mapeo de IDs...");
            await LoadClientesAsync();
            await _catalogManager.LoadGruposAsync();
            await _catalogManager.LoadTiposAsync();
            App.Log?.LogInformation("✅ Catálogos cargados correctamente");

            var clienteId = _clientesCache?.FirstOrDefault(c => string.Equals(c.Nombre, Parte.Cliente, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
            var grupoId = _catalogManager.GetGrupoId(Parte.Grupo);
            var tipoId = _catalogManager.GetTipoId(Parte.Tipo);
            
            App.Log?.LogInformation("📊 PASO 2: Mapeo de catálogos:");
            App.Log?.LogInformation("   Cliente: '{nombre}' → ID={id}", Parte.Cliente, clienteId);
            App.Log?.LogInformation("   Grupo: '{nombre}' → ID={id}", Parte.Grupo, grupoId?.ToString() ?? "null");
            App.Log?.LogInformation("   Tipo: '{nombre}' → ID={id}", Parte.Tipo, tipoId?.ToString() ?? "null");

            // ✅ VALIDAR que el clienteId sea válido
            if (clienteId == 0)
            {
                App.Log?.LogError("❌ ERROR: Cliente '{cliente}' no encontrado o ID=0", Parte.Cliente);
                await ShowErrorAsync($"Cliente '{Parte.Cliente}' no encontrado en el catálogo.");
                return;
            }

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
            App.Log?.LogInformation("📦 PASO 3: Preparar payload para API:");
            App.Log?.LogInformation("   • fecha_trabajo: {fecha}", payload.FechaTrabajo.ToString("yyyy-MM-dd"));
            App.Log?.LogInformation("   • hora_inicio: '{inicio}'", payload.HoraInicio);
            App.Log?.LogInformation("   • hora_fin: '{fin}'", payload.HoraFin);
            App.Log?.LogInformation("   • id_cliente: {id}", payload.IdCliente);
            App.Log?.LogInformation("   • tienda: '{tienda}'", payload.Tienda ?? "(null)");
            App.Log?.LogInformation("   • id_grupo: {id}", payload.IdGrupo?.ToString() ?? "null");
            App.Log?.LogInformation("   • id_tipo: {id}", payload.IdTipo?.ToString() ?? "null");
            App.Log?.LogInformation("   • accion: '{accion}'", Trim(payload.Accion, 50));
            App.Log?.LogInformation("   • ticket: '{ticket}'", payload.Ticket ?? "(null)");
            App.Log?.LogInformation("---------------------------------------------------------------");

            if (Parte.Id > 0)
            {
                // ✅ EDITAR parte existente
                var endpoint = $"/api/v1/partes/{Parte.Id}";
                var fullUrl = $"{App.Api.BaseUrl}{endpoint}";
                
                App.Log?.LogInformation("🔄 PASO 4: Actualizar parte existente");
                App.Log?.LogInformation("   📡 Endpoint: PUT {endpoint}", endpoint);
                App.Log?.LogInformation("   🌐 URL completa: {url}", fullUrl);
                App.Log?.LogInformation("   ⏳ Enviando petición...");
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await App.Api.PutAsync<ParteRequest, ParteDto>(endpoint, payload);
                sw.Stop();
                
                App.Log?.LogInformation("✅ Parte {id} actualizado correctamente en {ms}ms", Parte.Id, sw.ElapsedMilliseconds);
            }
            else
            {
                // ✅ CREAR parte nuevo
                var endpoint = "/api/v1/partes";
                var fullUrl = $"{App.Api.BaseUrl}{endpoint}";
                
                App.Log?.LogInformation("🔄 PASO 4: Crear parte nuevo");
                App.Log?.LogInformation("   📡 Endpoint: POST {endpoint}", endpoint);
                App.Log?.LogInformation("   🌐 URL completa: {url}", fullUrl);
                App.Log?.LogInformation("   ⏳ Enviando petición...");
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var resultado = await App.Api.PostAsync<ParteRequest, ParteDto>(endpoint, payload);
                sw.Stop();
                
                if (resultado != null)
                {
                    App.Log?.LogInformation("✅ Parte creado exitosamente con ID: {id} en {ms}ms", resultado.Id, sw.ElapsedMilliseconds);
                    Parte.Id = resultado.Id; // Actualizar ID del parte recién creado
                }
                else
                {
                    App.Log?.LogWarning("⚠️ Parte creado pero no se recibió confirmación del servidor");
                }
            }

            // ✅ PASO 5: Invalidar el caché de partes después de guardar
            App.Log?.LogInformation("🗑️ PASO 5: Invalidando caché de partes...");
            InvalidatePartesCache(Parte.Fecha);
            App.Log?.LogInformation("✅ Caché invalidado correctamente");
            
            Guardado = true;
            
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("✅ GUARDADO COMPLETADO EXITOSAMENTE");
            App.Log?.LogInformation("   • Parte ID: {id}", Parte.Id);
            App.Log?.LogInformation("   • Cliente: {cliente}", Parte.Cliente);
            App.Log?.LogInformation("   • Fecha: {fecha}", Parte.Fecha.ToString("yyyy-MM-dd"));
            App.Log?.LogInformation("   • Guardado = true");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            
            _parentWindow?.Close();
        }
        catch (ApiException apiEx)
        {
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            App.Log?.LogError("❌ ERROR API AL GUARDAR PARTE");
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            App.Log?.LogError("🔴 DETALLES DEL ERROR:");
            App.Log?.LogError("   • StatusCode: {status} ({statusInt})", apiEx.StatusCode, (int)apiEx.StatusCode);
            App.Log?.LogError("   • Path: {path}", apiEx.Path);
            App.Log?.LogError("   • Mensaje: {message}", apiEx.Message);
            App.Log?.LogError("   • Mensaje del servidor: {serverMsg}", apiEx.ServerMessage ?? "(sin mensaje)");
            App.Log?.LogError("   • Error del servidor: {serverError}", apiEx.ServerError ?? "(sin error)");
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            
            await ShowErrorAsync($"Error guardando parte:\n\n{apiEx.Message}\n\nCódigo: {apiEx.StatusCode}");
        }
        catch (Exception ex)
        {
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            App.Log?.LogError("❌ ERROR INESPERADO AL GUARDAR PARTE");
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            App.Log?.LogError("🔴 DETALLES DEL ERROR:");
            App.Log?.LogError("   • Tipo: {type}", ex.GetType().Name);
            App.Log?.LogError("   • Mensaje: {message}", ex.Message);
            App.Log?.LogError("   • Stack trace: {stack}", ex.StackTrace);
            if (ex.InnerException != null)
            {
                App.Log?.LogError("   • Inner exception: {inner}", ex.InnerException.Message);
            }
            App.Log?.LogError("═══════════════════════════════════════════════════════════════");
            
            await ShowErrorAsync($"Error guardando parte: {ex.Message}");
        }
    }
    
    /// <summary>Invalida las entradas de caché relacionadas con un parte en rango de ±30 días.</summary>
    private void InvalidatePartesCache(DateTime fecha)
    {
        try
        {
            // Invalidar el endpoint de rango que cubre ±30 días (usando fechaInicio/fechaFin)
            var fromDate = fecha.AddDays(-30).ToString("yyyy-MM-dd");
            var toDate = fecha.AddDays(30).ToString("yyyy-MM-dd");
            
            var rangePath = $"/api/v1/partes?fechaInicio={fromDate}&fechaFin={toDate}";
            App.Api.InvalidateCacheEntry(rangePath);
            App.Log?.LogDebug("🗑️ Caché invalidado (rango): {path}", rangePath);
            
            // También invalidar la fecha específica (para el método legacy)
            var dayPath = $"/api/v1/partes?fecha={fecha:yyyy-MM-dd}";
            App.Api.InvalidateCacheEntry(dayPath);
            App.Log?.LogDebug("🗑️ Caché invalidado (día): {path}", dayPath);
            
            // También invalidar la fecha actual (por si estamos trabajando con hoy)
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
                txt.Text = digits[^1].ToString();
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

        // Usar helper para formateo
        var (formatted, cursorPosition) = ParteItemEditValidation.FormatHoraWhileTyping(original);

        if (!formatted.Equals(original, StringComparison.Ordinal))
        {
            _suppressHoraFormatting = true;
            txt.Text = formatted;
            txt.SelectionStart = cursorPosition;
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

    /// <summary>Carga la información del usuario desde LocalSettings y actualiza el banner.</summary>
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
    
    /// <summary>Actualiza el logo del banner según el tema actual (claro/oscuro).</summary>
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
    /// Actualiza el badge de estado visual según el ParteEstado actual.
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
    
    /// <summary>Busca clientes en la API según el texto ingresado (case-insensitive).</summary>
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
    
    /// <summary>Handler ejecutado cuando el usuario presiona Enter o confirma la selección.</summary>
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
    
    /// <summary>Helper para truncar strings en logs con un máximo de caracteres.</summary>
    private static string Trim(string? s, int maxLen)
    {
        return ParteItemEditValidation.TruncateForLog(s, maxLen);
    }
}
