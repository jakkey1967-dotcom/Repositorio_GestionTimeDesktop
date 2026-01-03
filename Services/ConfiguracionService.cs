using GestionTime.Desktop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace GestionTime.Desktop.Services;

public class ConfiguracionService
{
    private readonly ApplicationDataContainer _localSettings;
    private ConfiguracionModel _configuracion = null!; // CS8618 Fix: Será inicializado en el constructor
    private static readonly Lazy<ConfiguracionService> _instance = new(() => new ConfiguracionService());
    
    // 🔧 CONFIGURACIÓN DE LOGS
    private const long MaxLogFileSizeBytes = 10 * 1024 * 1024; // 10 MB por defecto
    private const int MaxLogFiles = 5; // Mantener máximo 5 archivos rotados
    
    // 📝 Nombres de archivos de log
    public const string MainLogFileName = "app.log";
    public const string DebugLogFileName = "debug.log";
    public const string HttpLogFileName = "http.log";
    public const string ErrorLogFileName = "errors.log";
    public const string PerformanceLogFileName = "performance.log";

    public static ConfiguracionService Instance => _instance.Value;

    public ConfiguracionModel Configuracion 
    { 
        get => _configuracion ??= LoadConfiguration(); 
    }

    public event EventHandler<ConfiguracionChangedEventArgs>? ConfiguracionChanged;

    private ConfiguracionService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
        _configuracion = LoadConfiguration();
    }

    public ConfiguracionModel LoadConfiguration()
    {
        var config = new ConfiguracionModel();

        try
        {
            // 1. CARGAR DESDE APPSETTINGS.JSON PRIMERO
            LoadFromAppSettings(config);

            // 2. SOBRESCRIBIR CON CONFIGURACIÓN DE USUARIO (si existe)
            LoadFromUserSettings(config);

            // 3. VALIDAR Y NORMALIZAR CONFIGURACIÓN
            NormalizeConfiguration(config);

            // Subscribe to property changes
            config.PropertyChanged += OnConfigPropertyChanged;

            return config;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error loading configuration: {ex.Message}");
            return config;
        }
    }

    private void LoadFromAppSettings(ConfiguracionModel config)
    {
        try
        {
            var appPath = System.AppDomain.CurrentDomain.BaseDirectory;
            var settingsPath = Path.Combine(appPath, "appsettings.json");

            if (!File.Exists(settingsPath))
            {
                System.Diagnostics.Debug.WriteLine("?? appsettings.json no encontrado - usando valores por defecto");
                return;
            }

            var jsonContent = File.ReadAllText(settingsPath);
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            // ?? Configuración de Logging
            if (root.TryGetProperty("Logging", out var loggingSection))
            {
                if (loggingSection.TryGetProperty("LogPath", out var logPathProp))
                {
                    var logPath = logPathProp.GetString();
                    if (!string.IsNullOrEmpty(logPath))
                    {
                        // Si es una ruta completa con archivo, extraer solo el directorio
                        config.LogPath = logPath.EndsWith(".log") 
                            ? Path.GetDirectoryName(logPath) ?? config.LogPath
                            : logPath;
                    }
                }
            }

            // ?? Configuración de GestionTime
            if (root.TryGetProperty("GestionTime", out var gestionTimeSection))
            {
                if (gestionTimeSection.TryGetProperty("EnableDebugLogs", out var enableDebug))
                {
                    config.LogDebug = enableDebug.GetBoolean();
                }

                if (gestionTimeSection.TryGetProperty("EnableHttpLogs", out var enableHttp))
                {
                    config.LogHttp = enableHttp.GetBoolean();
                }

                if (gestionTimeSection.TryGetProperty("EnableErrorLogs", out var enableErrors))
                {
                    config.LogErrors = enableErrors.GetBoolean();
                }

                if (gestionTimeSection.TryGetProperty("LogDirectory", out var logDir))
                {
                    var logDirectory = logDir.GetString();
                    if (!string.IsNullOrEmpty(logDirectory))
                    {
                        config.LogPath = logDirectory.TrimEnd('\\', '/');
                    }
                }

                if (gestionTimeSection.TryGetProperty("MaxFileSizeMB", out var maxSize))
                {
                    config.LogRotation = LogRotation.BySize;
                }

                if (gestionTimeSection.TryGetProperty("RetainedFileCountLimit", out var retainCount))
                {
                    config.LogRetentionDays = retainCount.GetInt32();
                }
            }

            // ?? Configuración de API
            if (root.TryGetProperty("Api", out var apiSection))
            {
                if (apiSection.TryGetProperty("BaseUrl", out var baseUrl))
                {
                    config.ApiUrl = baseUrl.GetString() ?? config.ApiUrl;
                }
            }

            System.Diagnostics.Debug.WriteLine($"?? Configuración cargada desde appsettings.json");
            System.Diagnostics.Debug.WriteLine($"   ?? LogPath: {config.LogPath}");
            System.Diagnostics.Debug.WriteLine($"   ?? LogDebug: {config.LogDebug}");
            System.Diagnostics.Debug.WriteLine($"   ?? LogHttp: {config.LogHttp}");
            System.Diagnostics.Debug.WriteLine($"   ?? ApiUrl: {config.ApiUrl}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error leyendo appsettings.json: {ex.Message}");
        }
    }

    private void LoadFromUserSettings(ConfiguracionModel config)
    {
        try
        {
            // ?? Connection Settings
            if (_localSettings.Values.ContainsKey("ApiUrl"))
                config.ApiUrl = GetSetting("ApiUrl", config.ApiUrl);
            
            config.TimeoutSeconds = GetSetting("TimeoutSeconds", 30);
            config.MaxRetries = GetSetting("MaxRetries", 3);
            config.IgnoreSSL = GetSetting("IgnoreSSL", true);

            // ?? Logging Settings
            if (_localSettings.Values.ContainsKey("EnableLogging"))
                config.EnableLogging = GetSetting("EnableLogging", config.EnableLogging);
            if (_localSettings.Values.ContainsKey("LogLevel"))
                config.LogLevel = (LogLevel)GetSetting("LogLevel", (int)config.LogLevel);
            if (_localSettings.Values.ContainsKey("LogPath"))
                config.LogPath = GetSetting("LogPath", config.LogPath);
            
            // ?? Control granular de logs
            config.LogToFile = GetSetting("LogToFile", true);
            config.LogHttp = GetSetting("LogHttp", false); // HTTP OFF por defecto
            config.LogErrors = GetSetting("LogErrors", true);
            config.LogDebug = GetSetting("LogDebug", false); // Debug OFF por defecto
            config.LogRotation = (LogRotation)GetSetting("LogRotation", (int)LogRotation.BySize);
            config.LogRetentionDays = GetSetting("LogRetentionDays", 7);

            // ?? Application Settings
            config.AutoLogin = GetSetting("AutoLogin", false);
            config.StartMinimized = GetSetting("StartMinimized", false);
            config.MinimizeToTray = GetSetting("MinimizeToTray", false);
            config.AutoRefreshSeconds = GetSetting("AutoRefreshSeconds", 60);
            config.Theme = (AppTheme)GetSetting("Theme", (int)AppTheme.Auto);

            // ?? Debug Settings
            config.DebugMode = GetSetting("DebugMode", false);
            config.ShowConsole = GetSetting("ShowConsole", false);
            config.DetailedErrors = GetSetting("DetailedErrors", true);

            // ?? Cache Settings
            config.CacheTTLMinutes = GetSetting("CacheTTLMinutes", 5);
            config.MaxCacheItems = GetSetting("MaxCacheItems", 100);

            System.Diagnostics.Debug.WriteLine($"? Configuración de usuario aplicada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error cargando configuración de usuario: {ex.Message}");
        }
    }

    /// <summary>
    /// Normaliza y valida la configuración
    /// </summary>
    private void NormalizeConfiguration(ConfiguracionModel config)
    {
        // Asegurar que LogPath no esté vacío
        if (string.IsNullOrWhiteSpace(config.LogPath))
        {
            config.LogPath = GetDefaultLogPath();
            System.Diagnostics.Debug.WriteLine($"?? LogPath vacío - usando por defecto: {config.LogPath}");
        }

        // Normalizar path (quitar barras al final)
        config.LogPath = config.LogPath.TrimEnd('\\', '/');

        // Validar que sea una ruta absoluta
        if (!Path.IsPathRooted(config.LogPath))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            config.LogPath = Path.Combine(localAppData, "GestionTime", config.LogPath);
            System.Diagnostics.Debug.WriteLine($"?? Convertido a ruta absoluta: {config.LogPath}");
        }
    }

    public async Task SaveConfigurationAsync()
    {
        try
        {
            var config = _configuracion;

            System.Diagnostics.Debug.WriteLine("?? Iniciando guardado de configuración...");

            // ?? Connection Settings
            SetSetting("ApiUrl", config.ApiUrl);
            SetSetting("TimeoutSeconds", config.TimeoutSeconds);
            SetSetting("MaxRetries", config.MaxRetries);
            SetSetting("IgnoreSSL", config.IgnoreSSL);

            // ?? Logging Settings
            SetSetting("EnableLogging", config.EnableLogging);
            SetSetting("LogLevel", (int)config.LogLevel);
            SetSetting("LogPath", config.LogPath);
            SetSetting("LogToFile", config.LogToFile);
            SetSetting("LogHttp", config.LogHttp);
            SetSetting("LogErrors", config.LogErrors);
            SetSetting("LogDebug", config.LogDebug);
            SetSetting("LogRotation", (int)config.LogRotation);
            SetSetting("LogRetentionDays", config.LogRetentionDays);

            // ?? Application Settings
            SetSetting("AutoLogin", config.AutoLogin);
            SetSetting("StartMinimized", config.StartMinimized);
            SetSetting("MinimizeToTray", config.MinimizeToTray);
            SetSetting("AutoRefreshSeconds", config.AutoRefreshSeconds);
            SetSetting("Theme", (int)config.Theme);

            // ?? Debug Settings
            SetSetting("DebugMode", config.DebugMode);
            SetSetting("ShowConsole", config.ShowConsole);
            SetSetting("DetailedErrors", config.DetailedErrors);

            // ?? Cache Settings
            SetSetting("CacheTTLMinutes", config.CacheTTLMinutes);
            SetSetting("MaxCacheItems", config.MaxCacheItems);

            // ?? Apply changes
            await ApplyConfigurationChanges();

            // ?? Sincronizar con appsettings.json
            await SaveToAppSettingsAsync();

            // ?? Reconfigurar el logger con el nuevo path
            var newLogPath = GetLogFilePath(MainLogFileName);
            System.Diagnostics.Debug.WriteLine($"?? Reconfigurando logger: {newLogPath}");
            App.ReconfigureLogger(newLogPath);

            // ?? Notify listeners
            ConfiguracionChanged?.Invoke(this, new ConfiguracionChangedEventArgs(config));
            
            System.Diagnostics.Debug.WriteLine("? Configuración guardada exitosamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error guardando configuración: {ex.Message}");
            throw new InvalidOperationException($"Error saving configuration: {ex.Message}", ex);
        }
    }

    private async Task ApplyConfigurationChanges()
    {
        try
        {
            if (string.IsNullOrEmpty(_configuracion.LogPath))
            {
                _configuracion.LogPath = GetDefaultLogPath();
            }

            // ?? Crear directorio de logs
            Directory.CreateDirectory(_configuracion.LogPath);
            System.Diagnostics.Debug.WriteLine($"? Directorio de logs: {_configuracion.LogPath}");

            // ?? Limpiar logs antiguos si es necesario
            await CleanupOldLogsAsync();

            // ?? Rotar logs grandes
            await RotateLogFilesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error aplicando cambios: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene la ruta completa de un archivo de log
    /// </summary>
    public string GetLogFilePath(string logFileName)
    {
        return Path.Combine(_configuracion.LogPath, logFileName);
    }

    /// <summary>
    /// Verifica si un archivo de log debe rotarse por tamaño
    /// </summary>
    public bool ShouldRotateLogFile(string logFileName)
    {
        if (_configuracion.LogRotation != LogRotation.BySize)
            return false;

        var filePath = GetLogFilePath(logFileName);
        if (!File.Exists(filePath))
            return false;

        var fileInfo = new FileInfo(filePath);
        var shouldRotate = fileInfo.Length >= MaxLogFileSizeBytes;
        
        if (shouldRotate)
        {
            System.Diagnostics.Debug.WriteLine($"?? {logFileName}: {fileInfo.Length / 1024 / 1024:F2} MB (límite: {MaxLogFileSizeBytes / 1024 / 1024} MB)");
        }
        
        return shouldRotate;
    }

    /// <summary>
    /// Rota un archivo de log cuando excede el tamaño máximo
    /// </summary>
    public async Task RotateLogFileAsync(string logFileName)
    {
        try
        {
            var filePath = GetLogFilePath(logFileName);
            if (!File.Exists(filePath))
                return;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < MaxLogFileSizeBytes)
                return;

            System.Diagnostics.Debug.WriteLine($"?? Rotando archivo: {logFileName} ({fileInfo.Length / 1024 / 1024:F2} MB)");

            // Renombrar archivo actual
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{Path.GetFileNameWithoutExtension(logFileName)}_{timestamp}.log";
            var backupPath = GetLogFilePath(backupFileName);

            File.Move(filePath, backupPath);
            System.Diagnostics.Debug.WriteLine($"? Archivo rotado: {backupFileName}");

            // Limpiar archivos antiguos
            await CleanupRotatedFilesAsync(logFileName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error rotando {logFileName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Rota todos los archivos de log que excedan el tamaño
    /// </summary>
    private async Task RotateLogFilesAsync()
    {
        var logFiles = new[] { MainLogFileName, DebugLogFileName, HttpLogFileName, ErrorLogFileName, PerformanceLogFileName };
        
        foreach (var logFile in logFiles)
        {
            if (ShouldRotateLogFile(logFile))
            {
                await RotateLogFileAsync(logFile);
            }
        }
    }

    /// <summary>
    /// Limpia archivos rotados antiguos, manteniendo solo los últimos N
    /// </summary>
    private async Task CleanupRotatedFilesAsync(string baseFileName)
    {
        try
        {
            var baseName = Path.GetFileNameWithoutExtension(baseFileName);
            var directory = new DirectoryInfo(_configuracion.LogPath);
            
            if (!directory.Exists)
                return;
            
            var rotatedFiles = directory.GetFiles($"{baseName}_*.log")
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            if (rotatedFiles.Count > MaxLogFiles)
            {
                var filesToDelete = rotatedFiles.Skip(MaxLogFiles);
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        System.Diagnostics.Debug.WriteLine($"??? Eliminado archivo antiguo: {file.Name}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"?? No se pudo eliminar {file.Name}: {ex.Message}");
                    }
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error limpiando archivos rotados: {ex.Message}");
        }
    }

    /// <summary>
    /// Limpia logs antiguos basándose en la configuración de retención
    /// </summary>
    private async Task CleanupOldLogsAsync()
    {
        try
        {
            if (_configuracion.LogRetentionDays <= 0)
                return;

            var directory = new DirectoryInfo(_configuracion.LogPath);
            if (!directory.Exists)
                return;

            var cutoffDate = DateTime.Now.AddDays(-_configuracion.LogRetentionDays);
            var oldFiles = directory.GetFiles("*.log")
                .Where(f => f.LastWriteTime < cutoffDate)
                .ToList();

            if (oldFiles.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"?? Limpiando {oldFiles.Count} archivos con más de {_configuracion.LogRetentionDays} días");
            }

            foreach (var file in oldFiles)
            {
                try
                {
                    file.Delete();
                    System.Diagnostics.Debug.WriteLine($"??? Eliminado log antiguo: {file.Name}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"?? No se pudo eliminar {file.Name}: {ex.Message}");
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error limpiando logs antiguos: {ex.Message}");
        }
    }

    public async Task SaveToAppSettingsAsync()
    {
        try
        {
            var appPath = System.AppDomain.CurrentDomain.BaseDirectory;
            var settingsPath = Path.Combine(appPath, "appsettings.json");

            // ?? LEER configuración existente primero
            JsonDocument? existingDoc = null;
            Dictionary<string, JsonElement>? apiSection = null;

            if (File.Exists(settingsPath))
            {
                try
                {
                    var existingJson = await File.ReadAllTextAsync(settingsPath);
                    existingDoc = JsonDocument.Parse(existingJson);
                    
                    // Preservar la sección Api completa si existe
                    if (existingDoc.RootElement.TryGetProperty("Api", out var apiElement))
                    {
                        apiSection = new Dictionary<string, JsonElement>();
                        foreach (var prop in apiElement.EnumerateObject())
                        {
                            apiSection[prop.Name] = prop.Value.Clone();
                        }
                        System.Diagnostics.Debug.WriteLine($"?? Preservando configuración de API existente con {apiSection.Count} propiedades");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"?? No se pudo leer appsettings.json existente: {ex.Message}");
                }
            }

            // ?? Construir nueva configuración preservando Api
            object appSettings;
            
            if (apiSection != null && apiSection.Count > 0)
            {
                // Reconstruir sección Api desde el JSON existente
                var apiConfig = new Dictionary<string, object?>();
                foreach (var kvp in apiSection)
                {
                    apiConfig[kvp.Key] = kvp.Value.ValueKind switch
                    {
                        JsonValueKind.String => kvp.Value.GetString(),
                        JsonValueKind.Number => kvp.Value.GetInt32(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => kvp.Value.GetRawText()
                    };
                }

                appSettings = new
                {
                    Logging = new
                    {
                        LogPath = _configuracion.LogPath
                    },
                    GestionTime = new
                    {
                        EnableDebugLogs = _configuracion.LogDebug,
                        EnableHttpLogs = _configuracion.LogHttp,
                        EnableErrorLogs = _configuracion.LogErrors,
                        LogDirectory = _configuracion.LogPath,
                        MaxFileSizeMB = (int)(MaxLogFileSizeBytes / 1024 / 1024),
                        MaxLogFiles = MaxLogFiles,
                        RetainedFileCountLimit = _configuracion.LogRetentionDays
                    },
                    Api = apiConfig
                };
            }
            else
            {
                // Si no existe Api, usar solo BaseUrl como fallback
                appSettings = new
                {
                    Logging = new
                    {
                        LogPath = _configuracion.LogPath
                    },
                    GestionTime = new
                    {
                        EnableDebugLogs = _configuracion.LogDebug,
                        EnableHttpLogs = _configuracion.LogHttp,
                        EnableErrorLogs = _configuracion.LogErrors,
                        LogDirectory = _configuracion.LogPath,
                        MaxFileSizeMB = (int)(MaxLogFileSizeBytes / 1024 / 1024),
                        MaxLogFiles = MaxLogFiles,
                        RetainedFileCountLimit = _configuracion.LogRetentionDays
                    },
                    Api = new
                    {
                        BaseUrl = _configuracion.ApiUrl
                    }
                };
            }

            var json = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(settingsPath, json, System.Text.Encoding.UTF8);
            
            existingDoc?.Dispose();
            
            System.Diagnostics.Debug.WriteLine($"? appsettings.json actualizado (API preservado)");
            System.Diagnostics.Debug.WriteLine($"   ?? Debug Logs: {_configuracion.LogDebug}");
            System.Diagnostics.Debug.WriteLine($"   ?? HTTP Logs: {_configuracion.LogHttp}");
            System.Diagnostics.Debug.WriteLine($"   ?? Error Logs: {_configuracion.LogErrors}");
            System.Diagnostics.Debug.WriteLine($"   ?? Max Size: {MaxLogFileSizeBytes / 1024 / 1024} MB");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error guardando appsettings.json: {ex.Message}");
        }
    }

    private T GetSetting<T>(string key, T defaultValue)
    {
        try
        {
            if (_localSettings.Values.ContainsKey(key))
            {
                var value = _localSettings.Values[key];
                
                if (typeof(T) == typeof(bool) && value is bool boolValue)
                    return (T)(object)boolValue;
                
                if (typeof(T) == typeof(int) && value is int intValue)
                    return (T)(object)intValue;
                
                if (typeof(T) == typeof(string) && value is string strValue)
                    return (T)(object)strValue;
                
                if (value is T directValue)
                    return directValue;
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        catch { }
        
        return defaultValue;
    }

    private void SetSetting<T>(string key, T value)
    {
        _localSettings.Values[key] = value;
    }

    private string GetDefaultLogPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "GestionTime", "Logs");
    }

    private void OnConfigPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Auto-save deshabilitado - solo guardar cuando el usuario lo solicite
    }

    public void ResetToDefaults()
    {
        try
        {
            _localSettings.Values.Clear();
            _configuracion = LoadConfiguration();
            System.Diagnostics.Debug.WriteLine("? Configuración restablecida a valores por defecto");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error resetting configuration: {ex.Message}", ex);
        }
    }

    // ========== MÉTODOS PARA EXPORTAR/IMPORTAR ==========
    public async Task<string> ExportConfigurationAsync()
    {
        try
        {
            var exportData = new
            {
                ExportDate = DateTime.Now,
                Version = "1.0",
                Application = "GestionTime Desktop",
                Configuration = new
                {
                    // Connection
                    ApiUrl = _configuracion.ApiUrl,
                    TimeoutSeconds = _configuracion.TimeoutSeconds,
                    MaxRetries = _configuracion.MaxRetries,
                    IgnoreSSL = _configuracion.IgnoreSSL,

                    // Logging
                    EnableLogging = _configuracion.EnableLogging,
                    LogLevel = _configuracion.LogLevel,
                    LogPath = _configuracion.LogPath,
                    LogToFile = _configuracion.LogToFile,
                    LogHttp = _configuracion.LogHttp,
                    LogErrors = _configuracion.LogErrors,
                    LogDebug = _configuracion.LogDebug,
                    LogRotation = _configuracion.LogRotation,
                    LogRetentionDays = _configuracion.LogRetentionDays,

                    // Application
                    StartMinimized = _configuracion.StartMinimized,
                    MinimizeToTray = _configuracion.MinimizeToTray,
                    AutoRefreshSeconds = _configuracion.AutoRefreshSeconds,
                    Theme = _configuracion.Theme,

                    // Debug
                    DebugMode = _configuracion.DebugMode,
                    DetailedErrors = _configuracion.DetailedErrors,

                    // Cache
                    CacheTTLMinutes = _configuracion.CacheTTLMinutes,
                    MaxCacheItems = _configuracion.MaxCacheItems
                }
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return json;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error exporting configuration: {ex.Message}", ex);
        }
    }

    public async Task ImportConfigurationAsync(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("configuration", out var configElement))
            {
                throw new InvalidOperationException("Invalid configuration file format");
            }

            var config = _configuracion;

            // Connection
            if (configElement.TryGetProperty("apiUrl", out var apiUrl))
                config.ApiUrl = apiUrl.GetString() ?? config.ApiUrl;
            if (configElement.TryGetProperty("timeoutSeconds", out var timeout))
                config.TimeoutSeconds = timeout.GetInt32();
            if (configElement.TryGetProperty("maxRetries", out var retries))
                config.MaxRetries = retries.GetInt32();
            if (configElement.TryGetProperty("ignoreSSL", out var ignoreSSL))
                config.IgnoreSSL = ignoreSSL.GetBoolean();

            // Logging
            if (configElement.TryGetProperty("enableLogging", out var enableLogging))
                config.EnableLogging = enableLogging.GetBoolean();
            if (configElement.TryGetProperty("logLevel", out var logLevel))
                config.LogLevel = (LogLevel)logLevel.GetInt32();
            if (configElement.TryGetProperty("logPath", out var logPath))
                config.LogPath = logPath.GetString() ?? config.LogPath;
            if (configElement.TryGetProperty("logToFile", out var logToFile))
                config.LogToFile = logToFile.GetBoolean();
            if (configElement.TryGetProperty("logHttp", out var logHttp))
                config.LogHttp = logHttp.GetBoolean();
            if (configElement.TryGetProperty("logErrors", out var logErrors))
                config.LogErrors = logErrors.GetBoolean();
            if (configElement.TryGetProperty("logDebug", out var logDebug))
                config.LogDebug = logDebug.GetBoolean();
            if (configElement.TryGetProperty("logRotation", out var logRotation))
                config.LogRotation = (LogRotation)logRotation.GetInt32();
            if (configElement.TryGetProperty("logRetentionDays", out var logRetention))
                config.LogRetentionDays = logRetention.GetInt32();

            // Application
            if (configElement.TryGetProperty("startMinimized", out var startMin))
                config.StartMinimized = startMin.GetBoolean();
            if (configElement.TryGetProperty("minimizeToTray", out var minToTray))
                config.MinimizeToTray = minToTray.GetBoolean();
            if (configElement.TryGetProperty("autoRefreshSeconds", out var autoRefresh))
                config.AutoRefreshSeconds = autoRefresh.GetInt32();
            if (configElement.TryGetProperty("theme", out var theme))
                config.Theme = (AppTheme)theme.GetInt32();

            // Debug
            if (configElement.TryGetProperty("debugMode", out var debugMode))
                config.DebugMode = debugMode.GetBoolean();
            if (configElement.TryGetProperty("detailedErrors", out var detailedErrors))
                config.DetailedErrors = detailedErrors.GetBoolean();

            // Cache
            if (configElement.TryGetProperty("cacheTTLMinutes", out var cacheTTL))
                config.CacheTTLMinutes = cacheTTL.GetInt32();
            if (configElement.TryGetProperty("maxCacheItems", out var maxCache))
                config.MaxCacheItems = maxCache.GetInt32();

            await SaveConfigurationAsync();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON format: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error importing configuration: {ex.Message}", ex);
        }
    }

    public async Task CreateLogDirectoryAsync(string logPath)
    {
        try
        {
            if (string.IsNullOrEmpty(logPath))
            {
                throw new ArgumentException("La ruta del directorio no puede estar vacía", nameof(logPath));
            }

            System.Diagnostics.Debug.WriteLine($"?? Iniciando creación de directorio: {logPath}");

            // Crear directorio principal
            Directory.CreateDirectory(logPath);
            System.Diagnostics.Debug.WriteLine($"? Directorio creado/verificado: {logPath}");
            
            // Verificar que el directorio realmente existe
            if (!Directory.Exists(logPath))
            {
                throw new DirectoryNotFoundException($"El directorio no se pudo crear: {logPath}");
            }

            // Verificar permisos de escritura
            var testFile = Path.Combine(logPath, $"test_permissions_{DateTime.Now:yyyyMMddHHmmss}.tmp");
            
            try
            {
                await File.WriteAllTextAsync(testFile, $"Test - {DateTime.Now}");
                System.Diagnostics.Debug.WriteLine($"? Archivo de prueba escrito exitosamente");
                
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                    System.Diagnostics.Debug.WriteLine($"??? Archivo de prueba eliminado");
                }
            }
            catch (Exception fileEx)
            {
                System.Diagnostics.Debug.WriteLine($"? Error en archivo de prueba: {fileEx.Message}");
                throw new UnauthorizedAccessException($"Error de permisos en el directorio: {fileEx.Message}", fileEx);
            }
            
            // Actualizar configuración
            _configuracion.LogPath = logPath;
            await SaveConfigurationAsync();
            
            System.Diagnostics.Debug.WriteLine($"? Directorio de logs configurado exitosamente: {logPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error configurando directorio de logs: {ex.Message}");
            throw new InvalidOperationException($"Error configurando directorio de logs: {ex.Message}", ex);
        }
    }
}

public class ConfiguracionChangedEventArgs : EventArgs
{
    public ConfiguracionModel Configuracion { get; }

    public ConfiguracionChangedEventArgs(ConfiguracionModel configuracion)
    {
        Configuracion = configuracion;
    }
}