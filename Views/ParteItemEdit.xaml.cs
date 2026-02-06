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
using GestionTime.Desktop.Models.Dtos.Catalog;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Services.Catalog;

namespace GestionTime.Desktop.Views;

public sealed partial class ParteItemEdit : Page
{
    public ParteDto? Parte { get; private set; }
    public bool Guardado { get; private set; }
    
    /// <summary>Parte actualizado recibido del servidor después de guardar (CREATE o UPDATE).</summary>
    public ParteDto? ParteActualizado { get; private set; }
    
    private Microsoft.UI.Xaml.Window? _parentWindow;
    
    // 🆕 NUEVO: Cache del nombre del técnico (para evitar acceso a ApplicationData desde thread worker)
    private string _currentUserName = "Usuario";
    
    // 🆕 NUEVO: Gestor centralizado de catálogos
    private readonly CatalogManager _catalogManager = new();
    
    // 🆕 NOTA CLIENTE: Cache de nota del cliente actual
    private string? _clienteNotaActual;
    private int _clienteIdActual;
    private CancellationTokenSource? _clienteNotaCts;
    
    // 🆕 NUEVO: Gestores de eventos para ComboBox
    private ComboBoxEventManager? _grupoEventManager;
    private ComboBoxEventManager? _tipoEventManager;
    
    // Cache local de clientes (todavía usado para compatibilidad)
    private static List<ClienteResponse>? _clientesCache;
    private static DateTime? _cacheLoadedAt;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    
    // ✅ ELIMINADO: Campos no usados (_gruposCache, _gruposCacheLoadedAt, _tiposCache, _tiposCacheLoadedAt)
    // Los catálogos de Grupo y Tipo se gestionan ahora en CatalogManager
    
    // Items de Cliente para AutoSuggestBox
    private readonly ObservableCollection<string> _clienteSuggestions = new();
    private DispatcherTimer? _clienteSearchTimer;
    private CancellationTokenSource? _clienteSearchCts;
    private string _lastClienteQuery = string.Empty;
    
    // Items de Cliente originales
    private readonly ObservableCollection<string> _clienteItems = new();
    private CancellationTokenSource? _clienteLoadCts;
    private bool _clientesLoaded;
    
    // Items de Grupo (usados por ComboBoxEventManager)
    private readonly ObservableCollection<string> _grupoItems = new();
    
    // Items de Tipo (usados por ComboBoxEventManager)
    private readonly ObservableCollection<string> _tipoItems = new();
    
    // 🆕 TAGS: Sistema de gestión de tags
    private readonly ObservableCollection<string> _currentTags = new();
    private readonly ObservableCollection<string> _tagSuggestions = new(); // 🔧 FIX: Cambiar a ObservableCollection para binding
    private DispatcherTimer? _tagSearchTimer;
    private CancellationTokenSource? _tagSearchCts;
    private const int MAX_TAGS = 5;
    
    // 🆕 UX: Control manual de selección de tags (sin auto-commit)
    private string _tagInputBeforeNavigation = string.Empty; // Texto original antes de navegar con flechas
    private bool _isNavigatingTagSuggestions = false; // Flag para detectar navegación con flechas
    
    // Sistema de tracking de foco
    private string _lastFocusedControl = "";
    private int _focusChangeCounter;
    private DateTime _lastFocusChangeTime = DateTime.Now;

    private bool _suppressHoraFormatting;
    
    // Flags para detectar si es la primera tecla después de recibir foco
    private bool _horaInicioFirstKey;
    private bool _horaFinFirstKey;
    
    // Sistema de timestamp automático para TxtAccion
    private bool _suppressAccionTimestamp;

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
        
        App.Log?.LogDebug("✅ AutoSuggestBox Cliente configurado con búsqueda dinámica");
        
        // 🆕 TAGS: Configurar timer de búsqueda de tags (debounce 300ms)
        _tagSearchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _tagSearchTimer.Tick += async (s, e) =>
        {
            _tagSearchTimer.Stop();
            await SearchTagSuggestionsAsync();
        };
        
        // 🆕 TAGS: Configurar ItemsControl
        TagsItemsControl.ItemsSource = _currentTags;
        
        // ⚠️ IMPORTANTE: NO asignar ItemsSource aquí (constructor)
        // Se asignará en OnPageLoaded después de que el control esté en el árbol visual
        App.Log?.LogDebug("✅ Tags ItemsControl configurado (AutoSuggestBox se configurará en Loaded)");
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
            
            // ✅ FIX TAGS: Configurar ItemsSource DESPUÉS de que el control esté en el árbol visual
            if (TxtTagInput != null && _tagSuggestions != null)
            {
                TxtTagInput.ItemsSource = _tagSuggestions;
                
                // 🆕 UX: Configurar handler de teclado para navegación manual
                // Usar KeyDown en lugar de PreviewKeyDown (WinUI 3 AutoSuggestBox no soporta PreviewKeyDown correctamente)
                TxtTagInput.KeyDown += OnTagInputKeyDown;
                
                App.Log?.LogInformation("✅ AutoSuggestBox de tags configurado con ItemsSource ({count} items) + KeyDown handler", _tagSuggestions.Count);
            }
            else
            {
                App.Log?.LogWarning("⚠️ TxtTagInput o _tagSuggestions es null en OnPageLoaded");
            }
            
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
                OnFieldChanged(combo, EventArgs.Empty);
                
                // Navegar al siguiente campo
                MoveToNextControl(combo);
                e.Handled = true;
            }
        }
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
                OnGuardarClick(sender, new RoutedEventArgs());
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
                OnGuardarClick(sender, new RoutedEventArgs());
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

        // 🆕 NUEVO: Habilitar botón Guardar desde el inicio (parte nuevo puede guardarse inmediatamente)
        BtnGuardar.IsEnabled = true;
        if (BtnAccionGrabar != null)
            BtnAccionGrabar.IsEnabled = true;
        
        // 🆕 NOTA CLIENTE: Deshabilitar botón de nota (no hay cliente seleccionado)
        _clienteIdActual = 0;
        _clienteNotaActual = null;
        BtnClienteNota.IsEnabled = false;
        UpdateNotaTooltip();
        
        App.Log?.LogDebug("✅ Botón Guardar habilitado para nuevo parte");

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
        
        // 🆕 TAGS: Cargar tags del parte
        LoadParteTags(parte);
        
        // 🆕 NOTA CLIENTE: Cargar nota del cliente seleccionado
        _ = LoadClienteNotaAsync();
        
        App.Log?.LogInformation("✅ LoadParte completado - Cliente: {cliente}, Grupo: {grupo} ({grupoIdx}), Tipo: {tipo} ({tipoIdx}), Estado: {estado}", 
            parte.Cliente, parte.Grupo, CmbGrupo.SelectedIndex, parte.Tipo, CmbTipo.SelectedIndex, parte.EstadoTexto);
    }
    
    /// <summary>Carga los tags del parte en la colección de UI.</summary>
    private void LoadParteTags(ParteDto parte)
    {
        try
        {
            _currentTags.Clear();
            
            if (parte.Tags != null && parte.Tags.Any())
            {
                foreach (var tag in parte.Tags.Take(MAX_TAGS))
                {
                    _currentTags.Add(tag);
                }
                
                App.Log?.LogInformation("✅ {count} tags cargados del parte", _currentTags.Count);
            }
            
            UpdateTagCounter();
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error cargando tags del parte");
        }
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

        // ✅ CORREGIDO: Incluir SIEMPRE en JSON, incluso si es null
        [JsonPropertyName("id_grupo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? IdGrupo { get; set; }

        // ✅ CORREGIDO: Incluir SIEMPRE en JSON, incluso si es null
        [JsonPropertyName("id_tipo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? IdTipo { get; set; }

        [JsonPropertyName("accion")]
        public string Accion { get; set; } = string.Empty;

        [JsonPropertyName("ticket")]
        public string? Ticket { get; set; }

        /// <summary>Estado del parte como entero (0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado).</summary>
        /// <remarks>Solo se envía en PUT (actualización), no en POST (creación).</remarks>
        [JsonPropertyName("estado")]
        public int? Estado { get; set; }
        
        /// <summary>Tags/etiquetas del parte (máximo 5).</summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
    }

    private async void OnGuardarClick(object? sender, RoutedEventArgs e)
    {
        App.Log?.LogInformation("🔘 BOTÓN GUARDAR PRESIONADO - Iniciando guardado...");
        await GuardarAsync(cerrarParte: false);
    }
    
    /// <summary>Guarda el parte y cierra la ventana automáticamente.</summary>
    private async void OnGuardarYCerrarClick(object? sender, RoutedEventArgs e)
    {
        App.Log?.LogInformation("🔘 BOTÓN GUARDAR Y CERRAR PRESIONADO - Iniciando guardado...");
        await GuardarAsync(cerrarParte: true);
    }
    
    /// <summary>Lógica centralizada de guardado del parte.</summary>
    /// <param name="cerrarParte">Si true, cambia el estado a Cerrado (2). Si false, aplica lógica según estado original.</param>
    private async Task GuardarAsync(bool cerrarParte = false)
    {
        if (Parte == null) return;

        try
        {
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("💾 INICIAR GUARDADO DE PARTE");
            App.Log?.LogInformation("   • Cerrar parte: {cerrar}", cerrarParte ? "SÍ" : "NO");
            App.Log?.LogInformation("   • Estado original: {estado} ({estadoInt})", Parte.EstadoNombre, Parte.EstadoInt);
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            
            // 🆕 NUEVO: Guardar estado original para determinar el nuevo estado
            var estadoOriginal = Parte.EstadoInt;
            int nuevoEstado;
            
            if (Parte.Id == 0)
            {
                // ✅ PARTE NUEVO: Siempre Abierto (0)
                nuevoEstado = cerrarParte ? 2 : 0;
                App.Log?.LogInformation("📝 Parte NUEVO → Estado: {estado} ({nombre})", 
                    nuevoEstado, nuevoEstado == 0 ? "Abierto" : "Cerrado");
            }
            else
            {
                // ✅ PARTE EXISTENTE (EDICIÓN)
                if (cerrarParte)
                {
                    // Botón "Guardar y Cerrar" → Siempre Cerrado (2)
                    nuevoEstado = 2;
                    App.Log?.LogInformation("📝 EDICIÓN + Guardar y Cerrar → Estado: Cerrado (2)");
                }
                else
                {
                    // Botón "Guardar" → Lógica según estado original
                    if (estadoOriginal == 2)
                    {
                        // Si estaba Cerrado, mantener Cerrado
                        nuevoEstado = 2;
                        App.Log?.LogInformation("📝 EDICIÓN + Estado original Cerrado → Mantener Cerrado (2)");
                    }
                    else
                    {
                        // Si NO estaba Cerrado, cambiar a Abierto
                        nuevoEstado = 0;
                        App.Log?.LogInformation("📝 EDICIÓN + Estado original {estado} → Cambiar a Abierto (0)", 
                            estadoOriginal);
                    }
                }
            }
            
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

            // ✅ CORREGIDO: Obtener valor desde SelectedItem (si hay selección) o Text (si es texto libre)
            Parte.Grupo = (CmbGrupo.SelectedItem as string) ?? CmbGrupo.Text?.Trim() ?? string.Empty;
            Parte.Tipo = (CmbTipo.SelectedItem as string) ?? CmbTipo.Text?.Trim() ?? string.Empty;
            
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
            App.Log?.LogInformation("   🆕 Estado a guardar = {estado} ({nombre})", 
                nuevoEstado, nuevoEstado == 0 ? "Abierto" : nuevoEstado == 2 ? "Cerrado" : "Otro");
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

            // 🆕 MODIFICADO: Incluir estado en el payload para partes existentes
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
                Ticket = Parte.Ticket,
                // 🆕 NUEVO: Enviar estado calculado (solo para UPDATE, null para CREATE)
                Estado = Parte.Id > 0 ? nuevoEstado : (int?)null,
                // 🆕 TAGS: Incluir tags actuales (max 5)
                Tags = _currentTags.Any() ? _currentTags.ToList() : null
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
            App.Log?.LogInformation("   • 🆕 estado: {estado}", payload.Estado?.ToString() ?? "(null - CREATE)");
            App.Log?.LogInformation("---------------------------------------------------------------");
            
            // Variables para determinar el tipo de operación y mostrar notificación apropiada
            bool esCreacion = Parte.Id == 0;
            string tipoOperacion = esCreacion ? "creado" : "actualizado";

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
                var response = await App.Api.PutAsync<ParteRequest, ParteDto>(endpoint, payload);
                sw.Stop();
                
                // ✅ SOLUCIÓN CORRECTA: Construir objeto con datos del formulario
                if (response == null || response.Id == 0)
                {
                    App.Log?.LogInformation("🔧 PUT exitoso - Construyendo objeto con datos del formulario");
                    
                    // ⚠️ CRÍTICO: CALCULAR DURACIÓN con las horas actualizadas
                    int duracionCalculada = CalcularDuracionMinutos(Parte.HoraInicio, Parte.HoraFin);
                    
                    // 🆕 MODIFICADO: Usar el nuevo estado calculado
                    var nuevoEstadoNombre = nuevoEstado switch
                    {
                        0 => "Abierto",
                        1 => "Pausado",
                        2 => "Cerrado",
                        3 => "Enviado",
                        9 => "Anulado",
                        _ => "Desconocido"
                    };
                    
                    // Construir ParteActualizado con los datos actualizados
                    response = new ParteDto
                    {
                        Id = Parte.Id,
                        Fecha = Parte.Fecha,
                        Cliente = Parte.Cliente,
                        Tienda = Parte.Tienda,
                        HoraInicio = Parte.HoraInicio,
                        HoraFin = Parte.HoraFin,
                        Ticket = Parte.Ticket,
                        Grupo = Parte.Grupo,
                        Tipo = Parte.Tipo,
                        Accion = Parte.Accion,
                        DuracionMin = duracionCalculada,
                        Tecnico = Parte.Tecnico,
                        // 🆕 MODIFICADO: Usar el nuevo estado calculado
                        EstadoInt = nuevoEstado,
                        EstadoNombre = nuevoEstadoNombre,
                        IdCliente = clienteId,
                        IdGrupo = grupoId,
                        IdTipo = tipoId,
                        // ✅ FIX: Incluir Tags en el objeto que se guarda en caché
                        Tags = _currentTags.Any() ? _currentTags.ToList() : new List<string>()
                    };
                    
                    App.Log?.LogInformation("✅ Objeto ParteDto construido manualmente con datos actualizados");
                    App.Log?.LogInformation("   ⏱️ Duración recalculada: {duracion} minutos ({inicio} - {fin})", 
                        duracionCalculada, Parte.HoraInicio, Parte.HoraFin);
                    App.Log?.LogInformation("   🆕 Estado actualizado: {estado} ({nombre})", 
                        nuevoEstado, nuevoEstadoNombre);
                    
                    // ⚠️ CRÍTICO: ACTUALIZAR CACHE en lugar de invalidarlo
                    App.Api.UpdateCacheEntry(endpoint, response);
                    App.Log?.LogInformation("💾 Cache actualizado directamente con datos modificados (sin recargar desde servidor)");
                }
                
                if (response != null && response.Id > 0)
                {
                    ParteActualizado = response;
                    App.Log?.LogInformation("✅ Parte {id} actualizado correctamente en {ms}ms", Parte.Id, sw.ElapsedMilliseconds);
                    App.Log?.LogInformation("   📊 Datos completos del parte:");
                    App.Log?.LogInformation("      • ID: {id}", response.Id);
                    App.Log?.LogInformation("      • Fecha: {fecha}", response.Fecha.ToString("yyyy-MM-dd"));
                    App.Log?.LogInformation("      • Cliente: {cliente}", response.Cliente);
                    App.Log?.LogInformation("      • Tienda: {tienda}", response.Tienda ?? "(vacío)");
                    App.Log?.LogInformation("      • HoraInicio: {inicio}", response.HoraInicio ?? "(vacío)");
                    App.Log?.LogInformation("      • HoraFin: {fin}", response.HoraFin ?? "(vacío)");
                    App.Log?.LogInformation("      • DuraciónMin: {duracion}", response.DuracionMin);
                    App.Log?.LogInformation("      • Grupo: {grupo}", response.Grupo ?? "(vacío)");
                    App.Log?.LogInformation("      • Tipo: {tipo}", response.Tipo ?? "(vacío)");
                    App.Log?.LogInformation("      • Ticket: {ticket}", response.Ticket ?? "(vacío)");
                    App.Log?.LogInformation("      • Accion: {accion}", Trim(response.Accion, 80) ?? "(vacío)");
                    App.Log?.LogInformation("      • Estado: {estado} (int={estadoInt})", response.EstadoTexto, response.EstadoInt);
                    App.Log?.LogInformation("      • Tecnico: {tecnico}", response.Tecnico ?? "(vacío)");
                    App.Log?.LogInformation("      • 🏷️ Tags: {tags}", response.Tags != null && response.Tags.Any() ? string.Join(", ", response.Tags) : "(sin tags)");
                }
                else
                {
                    App.Log?.LogError("❌ No se pudo construir el objeto actualizado");
                }
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
                var response = await App.Api.PostAsync<ParteRequest, ParteDto>(endpoint, payload);
                sw.Stop();
                
                int nuevoId = 0;
                
                if (response != null && response.Id > 0)
                {
                    nuevoId = response.Id;
                    App.Log?.LogInformation("✅ Servidor devolvió ID: {id}", nuevoId);
                }
                else
                {
                    App.Log?.LogError("❌ El servidor no devolvió un ID válido");
                    await ShowErrorAsync("El servidor no devolvió el ID del parte creado.\n\nContacta con el administrador.");
                    return;
                }
                
                // 🆕 NUEVO: Si cerrarParte=true, llamar al endpoint /close
                if (cerrarParte && nuevoEstado == 2)
                {
                    App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                    App.Log?.LogInformation("🔒 PASO 4.5: Cerrar parte recién creado");
                    App.Log?.LogInformation("   • Parte ID: {id}", nuevoId);
                    App.Log?.LogInformation("   • HoraFin: {hora}", horaFin);
                    
                    try
                    {
                        var closeEndpoint = $"/api/v1/partes/{nuevoId}/close?horaFin={Uri.EscapeDataString(horaFin)}";
                        var closeFullUrl = $"{App.Api.BaseUrl}{closeEndpoint}";
                        
                        App.Log?.LogInformation("   📡 Endpoint: POST {endpoint}", closeEndpoint);
                        App.Log?.LogInformation("   🌐 URL completa: {url}", closeFullUrl);
                        App.Log?.LogInformation("   ⏳ Enviando petición de cierre...");
                        
                        var closeStart = System.Diagnostics.Stopwatch.StartNew();
                        await App.Api.PostAsync(closeEndpoint);
                        closeStart.Stop();
                        
                        App.Log?.LogInformation("✅ Parte {id} cerrado correctamente en {ms}ms", nuevoId, closeStart.ElapsedMilliseconds);
                        App.Log?.LogInformation("   🕐 Hora de fin aplicada en servidor: {hora}", horaFin);
                        App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                    }
                    catch (ApiException closeEx)
                    {
                        App.Log?.LogError("❌ Error cerrando parte - StatusCode: {status}", closeEx.StatusCode);
                        App.Log?.LogError("   💬 Mensaje: {message}", closeEx.Message);
                        App.Log?.LogError("   📄 Mensaje del servidor: {serverMsg}", closeEx.ServerMessage ?? "(sin respuesta)");
                        
                        // No fallar el guardado completo, pero mostrar warning
                        App.Notifications?.ShowWarning(
                            $"El parte #{nuevoId} fue creado pero no se pudo cerrar automáticamente. Ciérralo manualmente desde la lista.",
                            title: "⚠️ Cierre Parcial");
                        
                        // Continuar con estado Abierto en lugar de Cerrado
                        nuevoEstado = 0;
                    }
                }
                
                int duracionCalculada = CalcularDuracionMinutos(Parte.HoraInicio, Parte.HoraFin);
                var tecnicoNombre = _currentUserName;
                
                // 🆕 MODIFICADO: Usar el estado REAL según si se cerró o no
                var nuevoEstadoNombre = nuevoEstado switch
                {
                    0 => "Abierto",
                    2 => "Cerrado",
                    _ => "Abierto"
                };
                
                response = new ParteDto
                {
                    Id = nuevoId,
                    Fecha = Parte.Fecha,
                    Cliente = Parte.Cliente,
                    Tienda = Parte.Tienda,
                    HoraInicio = Parte.HoraInicio,
                    HoraFin = Parte.HoraFin,
                    Ticket = Parte.Ticket,
                    Grupo = Parte.Grupo,
                    Tipo = Parte.Tipo,
                    Accion = Parte.Accion,
                    DuracionMin = duracionCalculada,
                    Tecnico = tecnicoNombre,
                    // 🆕 MODIFICADO: Usar el nuevo estado REAL (puede ser 0 si el cierre falló)
                    EstadoInt = nuevoEstado,
                    EstadoNombre = nuevoEstadoNombre,
                    IdCliente = clienteId,
                    IdGrupo = grupoId,
                    IdTipo = tipoId,
                    // ✅ FIX: Incluir Tags en el objeto que se guarda en caché
                    Tags = _currentTags.Any() ? _currentTags.ToList() : new List<string>()
                };
                
                Parte.Id = nuevoId;
                ParteActualizado = response;
                
                App.Log?.LogInformation("✅ Parte creado exitosamente con ID: {id} en {ms}ms", nuevoId, sw.ElapsedMilliseconds);
                App.Log?.LogInformation("   📊 Objeto construido con datos del formulario:");
                App.Log?.LogInformation("      • ID: {id}", response.Id);
                App.Log?.LogInformation("      • Fecha: {fecha}", response.Fecha.ToString("yyyy-MM-dd"));
                App.Log?.LogInformation("      • Cliente: {cliente}", response.Cliente);
                App.Log?.LogInformation("      • Tienda: {tienda}", response.Tienda ?? "(vacío)");
                App.Log?.LogInformation("      • HoraInicio: {inicio}", response.HoraInicio ?? "(vacío)");
                App.Log?.LogInformation("      • HoraFin: {fin}", response.HoraFin ?? "(vacío)");
                App.Log?.LogInformation("      • DuraciónMin: {duracion}", response.DuracionMin);
                App.Log?.LogInformation("      • Grupo: {grupo}", response.Grupo ?? "(vacío)");
                App.Log?.LogInformation("      • Tipo: {tipo}", response.Tipo ?? "(vacío)");
                App.Log?.LogInformation("      • Ticket: {ticket}", response.Ticket ?? "(vacío)");
                App.Log?.LogInformation("      • Accion: {accion}", Trim(response.Accion, 80) ?? "(vacío)");
                App.Log?.LogInformation("      • 🆕 Estado: {estado} (int={estadoInt})", response.EstadoTexto, response.EstadoInt);
                App.Log?.LogInformation("      • Tecnico: {tecnico}", response.Tecnico ?? "(vacío)");
                App.Log?.LogInformation("      • 🏷️ Tags: {tags}", response.Tags != null && response.Tags.Any() ? string.Join(", ", response.Tags) : "(sin tags)");
                
                
                // ⚠️ CRÍTICO: ACTUALIZAR CACHE de la LISTA
                var parteEndpoint = $"/api/v1/partes/{response.Id}";
                App.Api.UpdateCacheEntry(parteEndpoint, response);
                App.Log?.LogInformation("💾 Cache del parte individual actualizado: {endpoint}", parteEndpoint);
                
                var fromDate = Parte.Fecha.AddDays(-30).ToString("yyyy-MM-dd");
                var toDate = Parte.Fecha.AddDays(30).ToString("yyyy-MM-dd");
                var listEndpoint = $"/api/v1/partes?fechaInicio={fromDate}&fechaFin={toDate}";
                
                App.Api.AddItemToListCache(listEndpoint, response);
                App.Log?.LogInformation("➕ Nuevo parte agregado al cache de la lista: {endpoint}", listEndpoint);
                
                var dayEndpoint = $"/api/v1/partes?fecha={Parte.Fecha:yyyy-MM-dd}";
                App.Api.AddItemToListCache(dayEndpoint, response);
                App.Log?.LogInformation("➕ Nuevo parte agregado al cache del día: {endpoint}", dayEndpoint);
            }

            App.Log?.LogInformation("✅ PASO 5: Cache sincronizado correctamente (sin invalidación)");
            
            Guardado = true;
            
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            App.Log?.LogInformation("✅ GUARDADO COMPLETADO EXITOSAMENTE");
            App.Log?.LogInformation("   • Parte ID: {id}", Parte.Id);
            App.Log?.LogInformation("   • Cliente: {cliente}", Parte.Cliente);
            App.Log?.LogInformation("   • Fecha: {fecha}", Parte.Fecha.ToString("yyyy-MM-dd"));
            App.Log?.LogInformation("   • 🆕 Estado final: {estado} ({nombre})", 
                nuevoEstado, nuevoEstado == 0 ? "Abierto" : nuevoEstado == 2 ? "Cerrado" : "Otro");
            App.Log?.LogInformation("   • Guardado = true");
            App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
            
            // 🆕 NUEVO: Mostrar notificación de éxito
            string mensajeNotificacion;
            if (cerrarParte)
            {
                mensajeNotificacion = esCreacion 
                    ? $"Parte #{Parte.Id} creado y cerrado correctamente" 
                    : $"Parte #{Parte.Id} actualizado y cerrado correctamente";
            }
            else
            {
                mensajeNotificacion = esCreacion 
                    ? $"Parte #{Parte.Id} creado correctamente" 
                    : $"Parte #{Parte.Id} actualizado correctamente";
            }
            
            App.Notifications?.ShowSuccess(
                mensajeNotificacion,
                title: "✅ Guardado Exitoso");
            
            App.Log?.LogInformation("🔔 Notificación mostrada: {mensaje}", mensajeNotificacion);
            
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
            
            // 🆕 NUEVO: Mostrar notificación de error
            App.Notifications?.ShowError(
                $"Error: {apiEx.Message}\n\nCódigo: {apiEx.StatusCode}",
                title: "❌ Error al Guardar");
            
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
            
            // 🆕 NUEVO: Mostrar notificación de error
            App.Notifications?.ShowError(
                $"Error: {ex.Message}",
                title: "❌ Error Inesperado");
            
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
                App.Log?.LogDebug("⌨️ HoraFin recibido foco - próxima tecla borrará contenido");
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

    /// <summary>Carga la información del usuario desde el perfil API y actualiza el banner.</summary>
    private async void LoadUserInfo()
    {
        try
        {
            // Intentar cargar perfil desde cache global primero
            if (App.CurrentUserProfile == null)
            {
                App.Log?.LogInformation("📥 Cargando perfil del usuario desde API (ParteItemEdit)...");
                
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
                        App.Log?.LogWarning("⚠️ Perfil no encontrado en backend, usando fallback");
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
                // Usar datos del perfil completo
                displayName = $"{App.CurrentUserProfile.FirstName} {App.CurrentUserProfile.LastName}".Trim();
                displayEmail = App.CurrentLoginEmail ?? App.CurrentUserProfile.FullName ?? "usuario@empresa.com";
                displayPhone = App.CurrentUserProfile.Phone ?? "";
                
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = displayEmail.Split('@')[0];
                }
                
                // Para el técnico, usar FirstName + LastName
                _currentUserName = displayName;
            }
            else
            {
                // Fallback: Usar email del login
                var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
                
                var userName = settings.TryGetValue("UserName", out var nameObj) && nameObj is string name 
                    ? name 
                    : "Usuario";
                    
                displayName = userName;
                displayEmail = App.CurrentLoginEmail ?? "usuario@empresa.com";
                displayPhone = "";
                
                _currentUserName = displayName;
            }
            
            // Actualizar banner
            TxtUserName.Text = displayName;
            TxtUserEmail.Text = displayEmail;
            
            // Actualizar teléfono (solo si tiene valor)
            if (!string.IsNullOrWhiteSpace(displayPhone))
            {
                TxtUserPhone.Text = displayPhone;
                TxtUserPhone.Visibility = Visibility.Visible;
            }
            else
            {
                TxtUserPhone.Visibility = Visibility.Collapsed;
            }
            
            App.Log?.LogInformation("🎨 Banner ParteItemEdit actualizado: {name} | {email} | {phone}", 
                displayName, displayEmail, 
                string.IsNullOrEmpty(displayPhone) ? "(sin teléfono)" : displayPhone);
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Error cargando perfil del usuario en ParteItemEdit");
            
            // Fallback seguro
            _currentUserName = "Usuario";
            TxtUserName.Text = "Usuario";
            TxtUserEmail.Text = App.CurrentLoginEmail ?? "usuario@empresa.com";
            TxtUserPhone.Visibility = Visibility.Collapsed;
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
            App.Log?.LogDebug("🚫 Carga de clientes cancelada");
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
            OnFieldChanged(sender, EventArgs.Empty);
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
        OnFieldChanged(sender, EventArgs.Empty);
        
        // Mover foco al siguiente campo (Tienda)
        TxtTienda.Focus(FocusState.Keyboard);
        
        // 🆕 NOTA CLIENTE: Cargar nota del cliente seleccionado
        _ = LoadClienteNotaAsync();
    }
    
    // ===================== NOTA CLIENTE =====================
    
    /// <summary>Carga la nota del cliente actual desde la API.</summary>
    private async Task LoadClienteNotaAsync()
    {
        try
        {
            // Obtener ID del cliente actual
            var clienteNombre = TxtCliente.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(clienteNombre))
            {
                _clienteIdActual = 0;
                _clienteNotaActual = null;
                BtnClienteNota.IsEnabled = false;
                UpdateNotaTooltip();
                return;
            }
            
            // Buscar cliente en cache
            var cliente = _clientesCache?.FirstOrDefault(
                c => string.Equals(c.Nombre, clienteNombre, StringComparison.OrdinalIgnoreCase));
            
            if (cliente == null || cliente.Id == 0)
            {
                App.Log?.LogDebug("⚠️ Cliente '{nombre}' no encontrado en cache", clienteNombre);
                _clienteIdActual = 0;
                _clienteNotaActual = null;
                BtnClienteNota.IsEnabled = false;
                UpdateNotaTooltip();
                return;
            }
            
            var clienteId = cliente.Id;
            
            // Si ya tenemos la nota de este cliente, no recargar
            if (clienteId == _clienteIdActual && _clienteNotaActual != null)
            {
                App.Log?.LogDebug("💾 Usando nota en cache para cliente ID {id}", clienteId);
                BtnClienteNota.IsEnabled = true;
                UpdateNotaTooltip();
                return;
            }
            
            // Cancelar carga anterior
            _clienteNotaCts?.Cancel();
            _clienteNotaCts?.Dispose();
            _clienteNotaCts = new CancellationTokenSource();
            var ct = _clienteNotaCts.Token;
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            App.Log?.LogInformation("🔍 LoadClienteNota start - ClienteId: {id}", clienteId);
            
            // Cargar cliente completo desde API
            var clienteDto = await App.Api.GetAsync<ClienteDto>(
                $"/api/v1/clientes/{clienteId}", ct);
            
            sw.Stop();
            
            if (clienteDto != null && !ct.IsCancellationRequested)
            {
                _clienteIdActual = clienteId;
                _clienteNotaActual = clienteDto.Nota;
                BtnClienteNota.IsEnabled = true;
                
                var notaLength = _clienteNotaActual?.Length ?? 0;
                App.Log?.LogInformation("✅ LoadClienteNota end - ClienteId: {id}, NotaLength: {length}, Duration: {ms}ms",
                    clienteId, notaLength, sw.ElapsedMilliseconds);
                
                UpdateNotaTooltip();
            }
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogDebug("🚫 Carga de nota de cliente cancelada");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando nota del cliente - StatusCode: {status}",
                ex is ApiException apiEx ? apiEx.StatusCode.ToString() : "N/A");
            
            _clienteNotaActual = null;
            BtnClienteNota.IsEnabled = false;
            UpdateNotaTooltip();
        }
    }
    
    /// <summary>Actualiza el tooltip del botón de nota según el contenido actual.</summary>
    private void UpdateNotaTooltip()
    {
        if (string.IsNullOrWhiteSpace(_clienteNotaActual))
        {
            ClienteNotaTooltip.Content = "Sin nota";
        }
        else
        {
            // Mostrar primeras 100 caracteres con "..." si es más largo
            var preview = _clienteNotaActual.Length <= 100 
                ? _clienteNotaActual 
                : _clienteNotaActual.Substring(0, 100) + "...";
            
            ClienteNotaTooltip.Content = preview;
        }
    }
    
    /// <summary>Handler cuando se hace click en el botón de nota del cliente.</summary>
    private async void OnClienteNotaClick(object sender, RoutedEventArgs e)
    {
        if (_clienteIdActual == 0)
        {
            App.Log?.LogWarning("⚠️ Click en nota sin cliente válido");
            return;
        }
        
        try
        {
            // Cargar texto actual en el editor
            TxtNotaEditor.Text = _clienteNotaActual ?? string.Empty;
            
            // Mostrar dialog
            ClienteNotaDialog.XamlRoot = this.XamlRoot;
            var result = await ClienteNotaDialog.ShowAsync();
            
            // El guardado se maneja en OnNotaDialogSave
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error abriendo dialog de nota");
        }
    }
    
    /// <summary>Handler cuando se presiona "Guardar" en el dialog de nota.</summary>
    private async void OnNotaDialogSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Evitar que el dialog se cierre automáticamente mientras guardamos
        args.Cancel = true;
        
        try
        {
            var nuevaNota = TxtNotaEditor.Text?.Trim();
            
            // Si no cambió, no hacer nada
            if (nuevaNota == _clienteNotaActual)
            {
                App.Log?.LogDebug("📝 Nota sin cambios, cerrando dialog");
                sender.Hide();
                return;
            }
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var notaLength = nuevaNota?.Length ?? 0;
            
            App.Log?.LogInformation("💾 SaveClienteNota start - ClienteId: {id}, NotaLength: {length}",
                _clienteIdActual, notaLength);
            
            // Crear servicio de clientes
            var clientesService = new ClientesService(App.Api, App.Log!);
            
            // Guardar nota
            var result = await clientesService.UpdateNotaAsync(_clienteIdActual, nuevaNota);
            
            sw.Stop();
            
            if (result != null)
            {
                _clienteNotaActual = result.Nota;
                
                App.Log?.LogInformation("✅ SaveClienteNota end - ClienteId: {id}, Duration: {ms}ms",
                    _clienteIdActual, sw.ElapsedMilliseconds);
                
                UpdateNotaTooltip();
                
                // Cerrar dialog
                sender.Hide();
                
                // Notificación de éxito
                App.Notifications?.ShowSuccess(
                    "Nota del cliente actualizada",
                    title: "✅ Guardado");
            }
        }
        catch (ApiException apiEx)
        {
            App.Log?.LogError("❌ Error guardando nota - StatusCode: {status}, Path: {path}, Message: {msg}",
                apiEx.StatusCode, apiEx.Path, apiEx.Message);
            
            // Mostrar error al usuario
            await ShowErrorAsync($"Error guardando nota:\n\n{apiEx.Message}\n\nCódigo: {apiEx.StatusCode}");
            
            // Permitir que el dialog se cierre
            args.Cancel = false;
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error inesperado guardando nota");
            
            await ShowErrorAsync($"Error guardando nota: {ex.Message}");
            
            args.Cancel = false;
        }
    }
    
    /// <summary>Handler cuando se presiona "Cancelar" en el dialog de nota.</summary>
    private void OnNotaDialogCancel(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        App.Log?.LogDebug("🚫 Dialog de nota cancelado");
        // No hacer nada, el dialog se cierra automáticamente
    }

    /// <summary>Helper para truncar strings en logs con un máximo de caracteres.</summary>
    private static string Trim(string? s, int maxLen)
    {
        return ParteItemEditValidation.TruncateForLog(s, maxLen);
    }
    
    /// <summary>Calcula la duración en minutos entre dos horas en formato HH:mm.</summary>
    private static int CalcularDuracionMinutos(string? horaInicio, string? horaFin)
    {
        if (string.IsNullOrWhiteSpace(horaInicio) || string.IsNullOrWhiteSpace(horaFin))
            return 0;
        
        if (!TimeSpan.TryParse(horaInicio, out var inicio))
            return 0;
        
        if (!TimeSpan.TryParse(horaFin, out var fin))
            return 0;
        
        var duracion = fin - inicio;
        
        // Si la duración es negativa, probablemente cruzó medianoche
        if (duracion.TotalMinutes < 0)
            duracion = duracion.Add(TimeSpan.FromDays(1));
        
        return (int)Math.Round(duracion.TotalMinutes);
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

        // Filtrar controles con TabIndex mayor al actual, ordenar y tomar el primero
        var nextControl = controls
            .Where(c => c.tabIndex > currentTabIndex && c.control.IsTabStop)
            .OrderBy(c => c.tabIndex)
            .FirstOrDefault();
        
        return nextControl.control;
    }
    
    // ===================== TAGS MANAGEMENT =====================
    
    /// <summary>Actualiza el contador de tags (n/5).</summary>
    private void UpdateTagCounter()
    {
        TxtTagCounter.Text = $"({_currentTags.Count}/{MAX_TAGS})";
    }
    
    /// <summary>Agrega un tag a la lista si es válido y no duplicado.</summary>
    private void AddTag(string tagText)
    {
        try
        {
            // Normalizar
            var tag = tagText.Trim();
            
            // Validar vacío
            if (string.IsNullOrWhiteSpace(tag))
            {
                App.Log?.LogDebug("⚠️ Tag vacío, ignorando");
                return;
            }
            
            // Validar máximo
            if (_currentTags.Count >= MAX_TAGS)
            {
                App.Log?.LogWarning("⚠️ Máximo de {max} tags alcanzado", MAX_TAGS);
                ShowMaxTagsWarning();
                return;
            }
            
            // Validar duplicados (case-insensitive)
            if (_currentTags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                App.Log?.LogDebug("⚠️ Tag duplicado: {tag}", tag);
                return;
            }
            
            // Agregar
            _currentTags.Add(tag);
            UpdateTagCounter();
            
            App.Log?.LogInformation("✅ Tag agregado: {tag} ({count}/{max})", tag, _currentTags.Count, MAX_TAGS);
            
            // Limpiar input
            TxtTagInput.Text = string.Empty;
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error agregando tag");
        }
    }
    
    /// <summary>Elimina un tag de la lista.</summary>
    private void RemoveTag(string tag)
    {
        try
        {
            if (_currentTags.Remove(tag))
            {
                UpdateTagCounter();
                App.Log?.LogInformation("🗑️ Tag eliminado: {tag} ({count}/{max})", tag, _currentTags.Count, MAX_TAGS);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error eliminando tag");
        }
    }
    
    /// <summary>Muestra advertencia cuando se intenta agregar más de 5 tags.</summary>
    private async void ShowMaxTagsWarning()
    {
        try
        {
            var teachingTip = new TeachingTip
            {
                Title = "Máximo de tags alcanzado",
                Subtitle = $"Solo puedes agregar hasta {MAX_TAGS} tags por parte.",
                PreferredPlacement = TeachingTipPlacementMode.Bottom,
                IsLightDismissEnabled = true,
                Target = TxtTagInput
            };
            
            // Agregar al Grid principal temporalmente
            var grid = (Grid)this.Content;
            grid.Children.Add(teachingTip);
            
            teachingTip.IsOpen = true;
            
            // Auto-cerrar después de 3 segundos
            await Task.Delay(3000);
            teachingTip.IsOpen = false;
            
            await Task.Delay(500);
            grid.Children.Remove(teachingTip);
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error mostrando teaching tip");
        }
    }
    
    /// <summary>Busca sugerencias de tags en el backend.</summary>
    private async Task SearchTagSuggestionsAsync()
    {
        try
        {
            var query = TxtTagInput.Text?.Trim() ?? string.Empty;
            
            App.Log?.LogDebug("═══════════════════════════════════════════════════════════════");
            App.Log?.LogDebug("🔍 BÚSQUEDA DE TAGS - INICIO");
            App.Log?.LogDebug("   • Query: '{query}'", query);
            App.Log?.LogDebug("   • Longitud: {len} caracteres", query.Length);
            
            // 🔧 MODIFICADO: Buscar desde 1 carácter (antes requería 2)
            if (query.Length < 1)
            {
                _tagSuggestions.Clear();
                App.Log?.LogDebug("⚠️ Query vacía - Limpiando sugerencias");
                App.Log?.LogDebug("═══════════════════════════════════════════════════════════════");
                return;
            }
            
            // Cancelar búsqueda anterior
            _tagSearchCts?.Cancel();
            _tagSearchCts?.Dispose();
            _tagSearchCts = new CancellationTokenSource();
            var ct = _tagSearchCts.Token;
            
            App.Log?.LogDebug("🔄 Preparando petición HTTP...");
            
            // ✅ FIX: Usar parámetro 'source' para filtrado en backend (no en cliente)
            // Backend filtra en BD por eficiencia, límite de 20 es suficiente
            var endpoint = $"/api/v1/tags?source={Uri.EscapeDataString(query)}&limit=20";
            App.Log?.LogDebug("📡 Endpoint: {endpoint}", endpoint);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // ✅ El endpoint /api/v1/tags devuelve List<string> directamente
            var allTags = await App.Api.GetAsync<List<string>>(endpoint, ct);
            
            // ✅ Backend ya filtró, solo ordenar alfabéticamente (sin .Where)
            var filteredTags = allTags?
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();
            
            sw.Stop();

            App.Log?.LogDebug("⏱️ Petición completada en {ms}ms", sw.ElapsedMilliseconds);
            
            if (filteredTags == null)
            {
                _tagSuggestions.Clear();
                App.Log?.LogWarning("⚠️ Response es NULL");
                App.Log?.LogDebug("═══════════════════════════════════════════════════════════════");
                return;
            }
            
            App.Log?.LogDebug("📦 Response recibida:");
            App.Log?.LogDebug("   • Tags.Count: {tagCount}", filteredTags.Count);
            
            if (filteredTags.Any())
            {
                App.Log?.LogDebug("🧹 Limpiando sugerencias anteriores...");
                _tagSuggestions.Clear();
                
                App.Log?.LogDebug("✅ Agregando {count} sugerencias a la colección:", filteredTags.Count);
                
                // Agregar uno por uno para actualizar ObservableCollection
                foreach (var tag in filteredTags)
                {
                    _tagSuggestions.Add(tag);
                    App.Log?.LogDebug("   + '{tag}'", tag);
                }
                
                App.Log?.LogDebug("✅ {count} sugerencias agregadas correctamente", _tagSuggestions.Count);
                App.Log?.LogDebug("📊 ItemsSource actual del AutoSuggestBox: {count} items", TxtTagInput.ItemsSource is System.Collections.ICollection col ? col.Count : -1);
            }
            else
            {
                _tagSuggestions.Clear();
                App.Log?.LogDebug("⚠️ No se encontraron sugerencias para '{query}'", query);
            }
            
            App.Log?.LogDebug("═══════════════════════════════════════════════════════════════");
        }
        catch (OperationCanceledException)
        {
            App.Log?.LogDebug("🚫 Búsqueda de tags cancelada");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error buscando sugerencias de tags");
            _tagSuggestions.Clear();
        }
    }
    
    // ===================== EVENT HANDLERS: TAGS =====================
    
    /// <summary>Handler de teclado para navegación en sugerencias (solo Escape para cancelar).</summary>
    private void OnTagInputKeyDown(object? sender, KeyRoutedEventArgs e)
    {
        try
        {
            var key = e.Key;
            
            // ✅ ESCAPE: Cerrar popup sin cambios
            if (key == Windows.System.VirtualKey.Escape)
            {
                if (TxtTagInput.IsSuggestionListOpen)
                {
                    TxtTagInput.IsSuggestionListOpen = false;
                    TxtTagInput.Text = string.Empty;
                    
                    App.Log?.LogDebug("⌨️ ESCAPE - Popup cerrado, texto limpiado");
                    e.Handled = true;
                }
                return;
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnTagInputKeyDown");
        }
    }
    
    /// <summary>Evento cuando el texto del AutoSuggestBox de tags cambia.</summary>
    private void OnTagTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        try
        {
            // Solo procesar cambios del usuario (escribiendo)
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                _tagSearchTimer?.Stop();
                _tagSearchTimer?.Start();
                
                App.Log?.LogDebug("📝 Tag TextChanged - Iniciando búsqueda para: '{text}'", sender.Text ?? "(vacío)");
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnTagTextChanged");
        }
    }
    
    /// <summary>Evento cuando se selecciona una sugerencia de tag.</summary>
    /// <remarks>Este evento se dispara ANTES de QuerySubmitted, usamos solo para actualizar el texto, NO para agregar.</remarks>
    private void OnTagSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        try
        {
            var selectedTag = args.SelectedItem as string;
            
            // ✅ SOLUCIÓN DEFINITIVA: NO agregar aquí, solo actualizar el texto
            // El tag se agregará en QuerySubmitted (que se dispara DESPUÉS)
            if (!string.IsNullOrWhiteSpace(selectedTag))
            {
                sender.Text = selectedTag;
                App.Log?.LogDebug("🖱️ SuggestionChosen - Texto actualizado a: '{tag}' (agregado en QuerySubmitted)", selectedTag);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnTagSuggestionChosen");
        }
    }
    
    /// <summary>Evento cuando se presiona Enter o se confirma una sugerencia con click.</summary>
    /// <remarks>Este es el ÚNICO lugar donde se agregan tags (confirmación explícita del usuario).</remarks>
    private void OnTagQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        try
        {
            string tagToAdd = string.Empty;
            
            // Caso 1: Usuario seleccionó de la lista (click o Enter en item resaltado)
            if (args.ChosenSuggestion != null)
            {
                tagToAdd = args.ChosenSuggestion.ToString() ?? string.Empty;
                App.Log?.LogDebug("📥 QuerySubmitted - Tag de lista: '{tag}'", tagToAdd);
            }
            // Caso 2: Usuario escribió y presionó Enter (sin seleccionar de lista)
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                var query = args.QueryText.Trim();
                
                // Buscar coincidencia exacta en sugerencias
                var matchingTag = _tagSuggestions.FirstOrDefault(t => 
                    t.Equals(query, StringComparison.OrdinalIgnoreCase));
                
                if (matchingTag != null)
                {
                    tagToAdd = matchingTag;
                    App.Log?.LogDebug("📥 QuerySubmitted - Tag encontrado por texto: '{tag}'", tagToAdd);
                }
                else
                {
                    // Permitir texto libre si no hay coincidencia
                    tagToAdd = query;
                    App.Log?.LogDebug("📥 QuerySubmitted - Tag texto libre: '{tag}'", tagToAdd);
                }
            }
            
            // Agregar el tag si no está vacío
            if (!string.IsNullOrWhiteSpace(tagToAdd))
            {
                AddTag(tagToAdd);
                sender.Text = string.Empty;
                sender.IsSuggestionListOpen = false;
                
                App.Log?.LogInformation("✅ Tag agregado: '{tag}'", tagToAdd);
            }
            else
            {
                App.Log?.LogDebug("⚠️ QuerySubmitted sin tag válido - ignorado");
                sender.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnTagQuerySubmitted");
        }
    }
    
    /// <summary>Evento cuando se hace click en el botón X de un chip de tag.</summary>
    private void OnRemoveTagClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is string tag)
            {
                RemoveTag(tag);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error en OnRemoveTagClick");
        }
    }
}

