using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using GestionTime.Desktop.Models.Dtos;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Gestor de eventos para ComboBox (Grupo y Tipo) en ParteItemEdit
/// Maneja la carga lazy, apertura automática, navegación y selección
/// </summary>
public sealed class ComboBoxEventManager
{
    private readonly ComboBox _comboBox;
    private readonly ObservableCollection<string> _items;
    private readonly CatalogManager _catalogManager;
    private readonly Action<Control> _moveToNextControlCallback;
    private readonly Action<object?, object> _fieldChangedCallback;
    private readonly string _comboBoxType; // "Grupo" o "Tipo"
    
    // Flags de estado
    private bool _isLoaded;
    private bool _justSelected;
    private bool _navigatingAway;
    private bool _dropDownOpenedByUser;
    
    public ComboBoxEventManager(
        ComboBox comboBox,
        ObservableCollection<string> items,
        CatalogManager catalogManager,
        Action<Control> moveToNextControlCallback,
        Action<object?, object> fieldChangedCallback,
        string comboBoxType)
    {
        _comboBox = comboBox ?? throw new ArgumentNullException(nameof(comboBox));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _catalogManager = catalogManager ?? throw new ArgumentNullException(nameof(catalogManager));
        _moveToNextControlCallback = moveToNextControlCallback ?? throw new ArgumentNullException(nameof(moveToNextControlCallback));
        _fieldChangedCallback = fieldChangedCallback ?? throw new ArgumentNullException(nameof(fieldChangedCallback));
        _comboBoxType = comboBoxType;
        
        // Configurar eventos
        _comboBox.GotFocus += OnGotFocus;
        _comboBox.DropDownOpened += OnDropDownOpened;
        _comboBox.PreviewKeyDown += OnPreviewKeyDown;
        _comboBox.SelectionChanged += OnSelectionChanged;
    }
    
    /// <summary>
    /// Maneja el evento GotFocus del ComboBox
    /// </summary>
    private async void OnGotFocus(object sender, RoutedEventArgs e)
    {
        App.Log?.LogInformation("🔧 Cmb{type} GotFocus - Loaded={loaded}, IsDropDownOpen={open}, JustSelected={just}, NavigatingAway={nav}", 
            _comboBoxType, _isLoaded, _comboBox.IsDropDownOpen, _justSelected, _navigatingAway);
        
        // NO abrir si el usuario está navegando con Tab/Escape
        if (_navigatingAway)
        {
            App.Log?.LogDebug("🔧 Usuario navegando, NO abrir dropdown");
            _navigatingAway = false;
            return;
        }
        
        // NO abrir si ya está abierto
        if (_comboBox.IsDropDownOpen)
        {
            App.Log?.LogDebug("🔧 Dropdown ya abierto, saltando...");
            return;
        }
        
        // NO abrir si acabamos de seleccionar
        if (_justSelected)
        {
            App.Log?.LogDebug("🔧 Recién seleccionado/confirmado, NO abrir automáticamente");
            _justSelected = false;
            return;
        }
        
        // Cargar items si aún no se han cargado
        if (!_isLoaded)
        {
            App.Log?.LogInformation("📊 Cargando {type} al recibir foco...", _comboBoxType);
            await LoadItemsAsync();
            
            // Después de cargar, abrir el dropdown automáticamente SOLO si es la primera vez
            if (_items.Count > 0 && _comboBox.SelectedIndex < 0)
            {
                App.Log?.LogDebug("🔧 Abriendo dropdown automáticamente con {count} items (sin selección previa)", _items.Count);
                _dropDownOpenedByUser = true;
                _comboBox.IsDropDownOpen = true;
            }
        }
        else
        {
            App.Log?.LogDebug("✅ {type} ya cargados ({count} items), abriendo dropdown", _comboBoxType, _items.Count);
            
            // Si ya están cargados, abrir directamente SOLO si no hay selección previa
            if (_items.Count > 0 && _comboBox.SelectedIndex < 0)
            {
                _dropDownOpenedByUser = true;
                _comboBox.IsDropDownOpen = true;
                App.Log?.LogDebug("🔧 Dropdown abierto (sin selección previa)");
            }
            else if (_comboBox.SelectedIndex >= 0)
            {
                App.Log?.LogDebug("🔧 Ya hay selección (index: {index}), NO abrir automáticamente", _comboBox.SelectedIndex);
            }
        }
    }
    
    /// <summary>
    /// Maneja el evento PreviewKeyDown del ComboBox
    /// </summary>
    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        App.Log?.LogDebug("🔧 Cmb{type} PreviewKeyDown - Key={key}", _comboBoxType, e.Key);
        
        // INTERCEPTAR ENTER para confirmar y avanzar
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            App.Log?.LogInformation("📥 Enter en {type} - Cerrando y avanzando", _comboBoxType);
            
            // Cerrar dropdown si está abierto
            if (_comboBox.IsDropDownOpen)
            {
                _comboBox.IsDropDownOpen = false;
            }
            
            // Marcar como recién seleccionado
            _justSelected = true;
            
            // Marcar como modificado
            _fieldChangedCallback(_comboBox, null!);
            
            // Navegar al siguiente control
            _moveToNextControlCallback(_comboBox);
            
            e.Handled = true;
            return;
        }
        
        // Detectar navegación con Tab o Escape
        if (e.Key == Windows.System.VirtualKey.Tab || 
            e.Key == Windows.System.VirtualKey.Escape)
        {
            App.Log?.LogDebug("🔧 Usuario navegando con {key}, marcar flag", e.Key);
            _navigatingAway = true;
            
            // Cerrar dropdown si está abierto
            if (_comboBox.IsDropDownOpen)
            {
                _comboBox.IsDropDownOpen = false;
            }
            return;
        }
        
        // Si presiona flecha abajo o Alt+Down, es apertura manual
        if (e.Key == Windows.System.VirtualKey.Down)
        {
            var altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
            
            // Alt+Down o solo Down cuando está cerrado = apertura manual
            if ((altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down ||
                !_comboBox.IsDropDownOpen)
            {
                _dropDownOpenedByUser = true;
                App.Log?.LogDebug("🔧 Dropdown de {type} marcado como apertura manual", _comboBoxType);
            }
        }
    }
    
    /// <summary>
    /// Maneja el evento DropDownOpened del ComboBox
    /// </summary>
    private async void OnDropDownOpened(object? sender, object e)
    {
        App.Log?.LogInformation("🔧 Cmb{type} DropDownOpened - OpenedByUser={manual}, Loaded={loaded}, Items={items}", 
            _comboBoxType, _dropDownOpenedByUser, _isLoaded, _items.Count);
        
        // Si no hay items y no están cargados, cargar ahora
        if (!_isLoaded)
        {
            App.Log?.LogInformation("📊 Cargando {type} desde dropdown...", _comboBoxType);
            await LoadItemsAsync();
        }
        
        // Resetear flag después de abrir
        _dropDownOpenedByUser = false;
    }
    
    /// <summary>
    /// Maneja el evento SelectionChanged del ComboBox
    /// </summary>
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_comboBox.SelectedItem is string selectedItem)
        {
            App.Log?.LogInformation("✅ {type} seleccionado: {item}", _comboBoxType, selectedItem);
            
            _justSelected = true;
            
            // Si el dropdown está abierto, cerrarlo y avanzar automáticamente
            if (_comboBox.IsDropDownOpen)
            {
                App.Log?.LogDebug("🖱️ Click en {type} - Cerrando dropdown y avanzando", _comboBoxType);
                _comboBox.IsDropDownOpen = false;
                
                // Avanzar al siguiente campo automáticamente
                _comboBox.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    _moveToNextControlCallback(_comboBox);
                });
            }
            
            _fieldChangedCallback(sender, e);
        }
    }
    
    /// <summary>
    /// Carga los items del catálogo correspondiente
    /// </summary>
    private async Task LoadItemsAsync()
    {
        try
        {
            if (_comboBoxType == "Grupo")
            {
                await _catalogManager.LoadGruposAsync();
                
                if (_catalogManager.GrupoItems.Count > 0)
                {
                    _items.Clear();
                    foreach (var item in _catalogManager.GrupoItems)
                    {
                        _items.Add(item);
                    }
                    _isLoaded = true;
                    App.Log?.LogInformation("📊 {type} cargados: {count} items", _comboBoxType, _items.Count);
                }
            }
            else if (_comboBoxType == "Tipo")
            {
                await _catalogManager.LoadTiposAsync();
                
                if (_catalogManager.TipoItems.Count > 0)
                {
                    _items.Clear();
                    foreach (var item in _catalogManager.TipoItems)
                    {
                        _items.Add(item);
                    }
                    _isLoaded = true;
                    App.Log?.LogInformation("📊 {type} cargados: {count} items", _comboBoxType, _items.Count);
                }
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando catálogo de {type}", _comboBoxType);
        }
    }
    
    /// <summary>
    /// Invalida el caché y marca como no cargado
    /// </summary>
    public void InvalidateCache()
    {
        _isLoaded = false;
        _items.Clear();
        
        if (_comboBoxType == "Grupo")
        {
            CatalogManager.InvalidateGruposCache();
        }
        else if (_comboBoxType == "Tipo")
        {
            CatalogManager.InvalidateTiposCache();
        }
        
        App.Log?.LogInformation("🗑️ Cache de {type} invalidado", _comboBoxType);
    }
}
