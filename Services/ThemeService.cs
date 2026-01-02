using Microsoft.UI.Xaml;
using Windows.Storage;
using System;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Services;

/// <summary>
/// Servicio centralizado para gestionar el tema de la aplicación de forma global
/// Asegura que todos los componentes (Login, Diario, Partes, etc.) usen el mismo tema
/// </summary>
public sealed class ThemeService
{
    private static ThemeService? _instance;
    private static readonly object _lock = new();

    private ElementTheme _currentTheme = ElementTheme.Default;
    private const string ThemeKey = "AppTheme";

    /// <summary>
    /// Instancia única del servicio (Singleton)
    /// </summary>
    public static ThemeService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ThemeService();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Evento que se dispara cuando el tema cambia
    /// </summary>
    public event EventHandler<ElementTheme>? ThemeChanged;

    /// <summary>
    /// Tema actualmente configurado
    /// </summary>
    public ElementTheme CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                App.Log?.LogInformation("🎨 Tema global cambiado a: {theme}", value);
                ThemeChanged?.Invoke(this, value);
            }
        }
    }

    private ThemeService()
    {
        // Cargar tema guardado al inicializar
        LoadSavedTheme();
    }

    /// <summary>
    /// Carga el tema guardado desde LocalSettings
    /// </summary>
    private void LoadSavedTheme()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            
            if (settings.TryGetValue(ThemeKey, out var themeObj) && themeObj is string themeName)
            {
                _currentTheme = themeName switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    "Default" => ElementTheme.Default,
                    _ => ElementTheme.Default
                };

                App.Log?.LogInformation("🎨 Tema cargado desde configuración: {theme}", _currentTheme);
            }
            else
            {
                // Por defecto: tema del sistema
                _currentTheme = ElementTheme.Default;
                SaveTheme(_currentTheme);
                App.Log?.LogInformation("🎨 No hay tema guardado, usando: {theme}", _currentTheme);
            }
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Error cargando tema guardado, usando Default");
            _currentTheme = ElementTheme.Default;
        }
    }

    /// <summary>
    /// Cambia el tema de la aplicación y lo guarda
    /// </summary>
    public void SetTheme(ElementTheme theme)
    {
        CurrentTheme = theme;
        SaveTheme(theme);
        
        var themeName = theme switch
        {
            ElementTheme.Light => "claro",
            ElementTheme.Dark => "oscuro",
            ElementTheme.Default => "sistema",
            _ => "desconocido"
        };
        
        App.Log?.LogInformation("✅ Tema aplicado globalmente: {name}", themeName);
    }

    /// <summary>
    /// Guarda el tema en LocalSettings
    /// </summary>
    private void SaveTheme(ElementTheme theme)
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            var themeName = theme switch
            {
                ElementTheme.Light => "Light",
                ElementTheme.Dark => "Dark",
                ElementTheme.Default => "Default",
                _ => "Default"
            };
            
            settings[ThemeKey] = themeName;
            App.Log?.LogDebug("💾 Tema guardado: {theme}", themeName);
        }
        catch (Exception ex)
        {
            App.Log?.LogWarning(ex, "Error guardando tema");
        }
    }

    /// <summary>
    /// Aplica el tema actual a un elemento de UI (Page, Window, etc.)
    /// </summary>
    public void ApplyTheme(FrameworkElement element)
    {
        if (element == null)
        {
            App.Log?.LogWarning("Elemento null en ApplyTheme");
            return;
        }

        element.RequestedTheme = CurrentTheme;
        App.Log?.LogDebug("🎨 Tema aplicado a {element}: {theme}", element.GetType().Name, CurrentTheme);
    }

    /// <summary>
    /// Obtiene el tema efectivo (resuelve Default al tema del sistema)
    /// </summary>
    public ElementTheme GetEffectiveTheme()
    {
        if (CurrentTheme != ElementTheme.Default)
            return CurrentTheme;

        // Detectar tema del sistema
        try
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            var foreground = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
            
            // Si el foreground es blanco, el tema es oscuro
            return (foreground.R == 255 && foreground.G == 255 && foreground.B == 255) 
                ? ElementTheme.Dark 
                : ElementTheme.Light;
        }
        catch
        {
            return ElementTheme.Dark; // Fallback
        }
    }

    /// <summary>
    /// Determina si el tema efectivo es oscuro
    /// </summary>
    public bool IsDarkTheme()
    {
        return GetEffectiveTheme() == ElementTheme.Dark;
    }
}
