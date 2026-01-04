using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Services;

/// <summary>
/// Servicio para guardar/cargar configuración de tamaños de ventana desde window-config.ini
/// </summary>
public sealed class WindowConfigService
{
    private static WindowConfigService? _instance;
    private readonly string _configFilePath;
    private readonly Dictionary<string, (int Width, int Height)> _windowSizes;
    private readonly ILogger? _logger;

    public static WindowConfigService Instance => _instance ??= new WindowConfigService();

    private WindowConfigService()
    {
        _logger = App.Log;
        
        // Archivo de configuración en el directorio de la aplicación
        var appDir = AppContext.BaseDirectory;
        _configFilePath = Path.Combine(appDir, "window-config.ini");
        
        _windowSizes = new Dictionary<string, (int Width, int Height)>();
        
        LoadConfiguration();
    }

    /// <summary>
    /// Obtiene el tamaño guardado para una página, o null si no existe
    /// </summary>
    public (int Width, int Height)? GetSizeForPage(string pageName)
    {
        if (_windowSizes.TryGetValue(pageName, out var size))
        {
            _logger?.LogDebug("Tamaño cargado para {pageName}: {width}x{height}", pageName, size.Width, size.Height);
            return size;
        }
        
        return null;
    }

    /// <summary>
    /// Guarda el tamaño actual de una página
    /// </summary>
    public void SaveSizeForPage(string pageName, int width, int height)
    {
        _windowSizes[pageName] = (width, height);
        SaveConfiguration();
        
        _logger?.LogInformation("💾 Tamaño guardado para {pageName}: {width}x{height}", pageName, width, height);
    }

    /// <summary>
    /// Carga la configuración desde window-config.ini
    /// </summary>
    private void LoadConfiguration()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger?.LogInformation("📄 Archivo de configuración no existe: {path}", _configFilePath);
                return;
            }

            var lines = File.ReadAllLines(_configFilePath);
            
            foreach (var line in lines)
            {
                // Ignorar comentarios y líneas vacías
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                // Formato: PageName=Width,Height
                var parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                
                if (parts.Length != 2)
                    continue;

                var pageName = parts[0];
                var sizeParts = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                
                if (sizeParts.Length != 2)
                    continue;

                if (int.TryParse(sizeParts[0], out var width) && int.TryParse(sizeParts[1], out var height))
                {
                    _windowSizes[pageName] = (width, height);
                    _logger?.LogDebug("Cargado {pageName}: {width}x{height}", pageName, width, height);
                }
            }

            _logger?.LogInformation("✅ Configuración de ventanas cargada: {count} páginas", _windowSizes.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error cargando configuración de ventanas");
        }
    }

    /// <summary>
    /// Guarda la configuración en window-config.ini
    /// </summary>
    private void SaveConfiguration()
    {
        try
        {
            // 🔍 LOG DETALLADO: Verificar ruta y permisos
            App.Log?.LogInformation("💾 Iniciando guardado de window-config.ini...");
            App.Log?.LogInformation("   📍 Ruta completa: {path}", _configFilePath);
            App.Log?.LogInformation("   📁 Directorio: {dir}", Path.GetDirectoryName(_configFilePath));
            
            // Verificar que el directorio existe
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                App.Log?.LogInformation("   📁 Directorio creado: {dir}", directory);
            }
            
            // Verificar permisos de escritura
            try
            {
                var testFile = Path.Combine(directory!, "test_write.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                App.Log?.LogInformation("   ✅ Permisos de escritura verificados");
            }
            catch (Exception permEx)
            {
                App.Log?.LogError(permEx, "   ❌ Sin permisos de escritura en {dir}", directory);
                throw;
            }
            
            var lines = new List<string>
            {
                "# ============================================",
                "# CONFIGURACIÓN DE TAMAÑOS DE VENTANA",
                "# GestionTime Desktop",
                "# ============================================",
                "#",
                "# Formato: PageName=Width,Height",
                "# ",
                "# Páginas disponibles:",
                "#   - LoginPage",
                "#   - DiarioPage",
                "#   - ParteItemEdit",
                "#   - GraficaDiaPage",
                "#   - RegisterPage",
                "#   - ForgotPasswordPage",
                "#",
                $"# Última actualización: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "# ============================================",
                ""
            };

            // Ordenar por nombre de página
            foreach (var kvp in _windowSizes.OrderBy(x => x.Key))
            {
                lines.Add($"{kvp.Key}={kvp.Value.Width},{kvp.Value.Height}");
            }
            
            // 🔍 LOG: Mostrar qué se va a guardar
            App.Log?.LogInformation("   📄 Contenido a guardar:");
            foreach (var line in lines.Where(l => !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l)))
            {
                App.Log?.LogInformation("      {line}", line);
            }

            File.WriteAllLines(_configFilePath, lines);
            
            // 🔍 VERIFICACIÓN INMEDIATA: Leer el archivo recién guardado
            if (File.Exists(_configFilePath))
            {
                var savedContent = File.ReadAllLines(_configFilePath);
                var dataLines = savedContent.Where(l => !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l)).ToArray();
                
                App.Log?.LogInformation("   ✅ Archivo guardado exitosamente");
                App.Log?.LogInformation("   📊 Líneas de datos guardadas: {count}", dataLines.Length);
                
                // Mostrar el contenido real guardado
                foreach (var line in dataLines)
                {
                    App.Log?.LogInformation("      ✓ {line}", line);
                }
            }
            else
            {
                App.Log?.LogWarning("   ⚠️ Archivo no existe después de guardar!");
            }
            
            App.Log?.LogInformation("💾 window-config.ini guardado exitosamente");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error guardando window-config.ini");
        }
    }

    /// <summary>
    /// Muestra información de debug de la configuración actual
    /// </summary>
    public string GetDebugInfo()
    {
        var info = $"📄 window-config.ini path: {_configFilePath}\n";
        info += $"📊 Páginas configuradas: {_windowSizes.Count}\n\n";
        
        foreach (var kvp in _windowSizes.OrderBy(x => x.Key))
        {
            info += $"  • {kvp.Key,-20} = {kvp.Value.Width} x {kvp.Value.Height}\n";
        }
        
        return info;
    }
}
