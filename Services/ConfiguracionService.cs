using GestionTime.Desktop.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace GestionTime.Desktop.Services;

public class ConfiguracionService
{
    private readonly ApplicationDataContainer _localSettings;
    private ConfiguracionModel _configuracion;
    private static readonly Lazy<ConfiguracionService> _instance = new(() => new ConfiguracionService());

    public static ConfiguracionService Instance => _instance.Value;

    public ConfiguracionModel Configuracion 
    { 
        get => _configuracion ??= LoadConfiguration(); 
    }

    public event EventHandler<ConfiguracionChangedEventArgs> ConfiguracionChanged;

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

            // Subscribe to property changes
            config.PropertyChanged += OnConfigPropertyChanged;

            return config;
        }
        catch (Exception ex)
        {
            // Log error and return default configuration
            System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            return config;
        }
    }

    private void LoadFromAppSettings(ConfiguracionModel config)
    {
        try
        {
            // Leer appsettings.json desde la carpeta de la aplicación
            var appPath = System.AppDomain.CurrentDomain.BaseDirectory;
            var settingsPath = Path.Combine(appPath, "appsettings.json");

            if (File.Exists(settingsPath))
            {
                var jsonContent = File.ReadAllText(settingsPath);
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                // Configuración de Logging
                if (root.TryGetProperty("Logging", out var loggingSection))
                {
                    if (loggingSection.TryGetProperty("logPath", out var logPathProp))
                    {
                        var fullPath = logPathProp.GetString();
                        if (!string.IsNullOrEmpty(fullPath))
                        {
                            // Extraer directorio de la ruta completa
                            config.LogPath = Path.GetDirectoryName(fullPath) ?? config.LogPath;
                        }
                    }
                }

                // Configuración de GestionTime
                if (root.TryGetProperty("GestionTime", out var gestionTimeSection))
                {
                    if (gestionTimeSection.TryGetProperty("EnableDebugLogs", out var enableDebug))
                    {
                        config.EnableLogging = enableDebug.GetBoolean();
                        config.LogLevel = enableDebug.GetBoolean() ? LogLevel.Debug : LogLevel.Warning;
                    }

                    if (gestionTimeSection.TryGetProperty("LogDirectory", out var logDir))
                    {
                        var logDirectory = logDir.GetString();
                        if (!string.IsNullOrEmpty(logDirectory))
                        {
                            config.LogPath = logDirectory.TrimEnd('\\');
                        }
                    }

                    if (gestionTimeSection.TryGetProperty("MaxFileSizeMB", out var maxSize))
                    {
                        // Usar para configuración de rotación
                        config.LogRotation = LogRotation.BySize;
                    }

                    if (gestionTimeSection.TryGetProperty("RetainedFileCountLimit", out var retainCount))
                    {
                        // Convertir a días aproximados (5 archivos ? 5 días)
                        config.LogRetentionDays = retainCount.GetInt32();
                    }
                }

                // Configuración de API
                if (root.TryGetProperty("Api", out var apiSection))
                {
                    if (apiSection.TryGetProperty("BaseUrl", out var baseUrl))
                    {
                        var url = baseUrl.GetString();
                        if (!string.IsNullOrEmpty(url))
                        {
                            config.ApiUrl = url;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"? Configuración cargada desde appsettings.json: LogPath={config.LogPath}, ApiUrl={config.ApiUrl}");
            }
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
            // Connection Settings (solo sobrescribir si el usuario ha configurado algo)
            if (_localSettings.Values.ContainsKey("ApiUrl"))
                config.ApiUrl = GetSetting("ApiUrl", config.ApiUrl);
            
            config.TimeoutSeconds = GetSetting("TimeoutSeconds", 30);
            config.MaxRetries = GetSetting("MaxRetries", 3);
            config.IgnoreSSL = GetSetting("IgnoreSSL", true);

            // Logging Settings (combinar con appsettings.json)
            if (_localSettings.Values.ContainsKey("EnableLogging"))
                config.EnableLogging = GetSetting("EnableLogging", config.EnableLogging);
            if (_localSettings.Values.ContainsKey("LogLevel"))
                config.LogLevel = (LogLevel)GetSetting("LogLevel", (int)config.LogLevel);
            if (_localSettings.Values.ContainsKey("LogPath"))
                config.LogPath = GetSetting("LogPath", config.LogPath);
            
            config.LogToFile = GetSetting("LogToFile", true);
            config.LogHttp = GetSetting("LogHttp", false);
            config.LogErrors = GetSetting("LogErrors", true);
            config.LogDebug = GetSetting("LogDebug", config.LogLevel >= LogLevel.Debug);
            config.LogRotation = (LogRotation)GetSetting("LogRotation", (int)config.LogRotation);
            config.LogRetentionDays = GetSetting("LogRetentionDays", config.LogRetentionDays);

            // Application Settings
            config.AutoLogin = GetSetting("AutoLogin", false);
            config.StartMinimized = GetSetting("StartMinimized", false);
            config.MinimizeToTray = GetSetting("MinimizeToTray", false);
            config.AutoRefreshSeconds = GetSetting("AutoRefreshSeconds", 60);
            config.Theme = (AppTheme)GetSetting("Theme", (int)AppTheme.Auto);

            // Debug Settings
            config.DebugMode = GetSetting("DebugMode", false);
            config.ShowConsole = GetSetting("ShowConsole", false);
            config.DetailedErrors = GetSetting("DetailedErrors", true);

            // Cache Settings
            config.CacheTTLMinutes = GetSetting("CacheTTLMinutes", 5);
            config.MaxCacheItems = GetSetting("MaxCacheItems", 100);

            System.Diagnostics.Debug.WriteLine($"? Configuración de usuario aplicada sobre configuración base");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error cargando configuración de usuario: {ex.Message}");
        }
    }

    public async Task SaveConfigurationAsync()
    {
        try
        {
            var config = _configuracion;

            // Connection Settings
            SetSetting("ApiUrl", config.ApiUrl);
            SetSetting("TimeoutSeconds", config.TimeoutSeconds);
            SetSetting("MaxRetries", config.MaxRetries);
            SetSetting("IgnoreSSL", config.IgnoreSSL);

            // Logging Settings
            SetSetting("EnableLogging", config.EnableLogging);
            SetSetting("LogLevel", (int)config.LogLevel);
            SetSetting("LogPath", config.LogPath);
            SetSetting("LogToFile", config.LogToFile);
            SetSetting("LogHttp", config.LogHttp);
            SetSetting("LogErrors", config.LogErrors);
            SetSetting("LogDebug", config.LogDebug);
            SetSetting("LogRotation", (int)config.LogRotation);
            SetSetting("LogRetentionDays", config.LogRetentionDays);

            // Application Settings
            SetSetting("AutoLogin", config.AutoLogin);
            SetSetting("StartMinimized", config.StartMinimized);
            SetSetting("MinimizeToTray", config.MinimizeToTray);
            SetSetting("AutoRefreshSeconds", config.AutoRefreshSeconds);
            SetSetting("Theme", (int)config.Theme);

            // Debug Settings
            SetSetting("DebugMode", config.DebugMode);
            SetSetting("ShowConsole", config.ShowConsole);
            SetSetting("DetailedErrors", config.DetailedErrors);

            // Cache Settings
            SetSetting("CacheTTLMinutes", config.CacheTTLMinutes);
            SetSetting("MaxCacheItems", config.MaxCacheItems);

            // Apply changes
            await ApplyConfigurationChanges();

            // Sincronizar con appsettings.json
            await SaveToAppSettingsAsync();

            // Notify listeners
            ConfiguracionChanged?.Invoke(this, new ConfiguracionChangedEventArgs(config));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error saving configuration: {ex.Message}", ex);
        }
    }

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

    public void ResetToDefaults()
    {
        try
        {
            // Clear all settings
            _localSettings.Values.Clear();
            
            // Reload with defaults
            _configuracion = LoadConfiguration();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error resetting configuration: {ex.Message}", ex);
        }
    }

    private T GetSetting<T>(string key, T defaultValue)
    {
        try
        {
            if (_localSettings.Values.ContainsKey(key))
            {
                var value = _localSettings.Values[key];
                if (value is T directValue)
                    return directValue;
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        catch
        {
            // Return default on any conversion error
        }
        
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
        // Auto-save on property change (optional)
        // await SaveConfigurationAsync();
    }

    private async Task ApplyConfigurationChanges()
    {
        try
        {
            // Apply API client settings
            if (App.Api != null)
            {
                // Update ApiClient with new settings
                // This would require modifying your ApiClient to accept configuration
            }

            // Apply logging settings - CREAR DIRECTORIO INMEDIATAMENTE
            if (!string.IsNullOrEmpty(_configuracion.LogPath))
            {
                try
                {
                    // Crear directorio si no existe
                    Directory.CreateDirectory(_configuracion.LogPath);
                    
                    // Crear directorio para subdirectorios de logs si es necesario
                    var testFile = Path.Combine(_configuracion.LogPath, "test_write_permissions.tmp");
                    await File.WriteAllTextAsync(testFile, "test");
                    File.Delete(testFile);
                    
                    System.Diagnostics.Debug.WriteLine($"?? Directorio de logs creado/verificado: {_configuracion.LogPath}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Error creando directorio de logs: {ex.Message}");
                    // Si falla, usar directorio por defecto
                    _configuracion.LogPath = GetDefaultLogPath();
                    Directory.CreateDirectory(_configuracion.LogPath);
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying configuration changes: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"?? Directorio creado/verificado: {logPath}");
            
            // Verificar que el directorio realmente existe
            if (!Directory.Exists(logPath))
            {
                throw new DirectoryNotFoundException($"El directorio no se pudo crear: {logPath}");
            }

            // Verificar permisos de escritura con un archivo de prueba más detallado
            var testFile = Path.Combine(logPath, $"test_permissions_{DateTime.Now:yyyyMMddHHmmss}_{Environment.TickCount}.tmp");
            var testContent = $"PRUEBA DE PERMISOS - {DateTime.Now}\n" +
                             $"Usuario: {Environment.UserName}\n" +
                             $"Directorio: {logPath}\n" +
                             $"Timestamp: {Environment.TickCount}\n";
            
            System.Diagnostics.Debug.WriteLine($"?? Creando archivo de prueba: {testFile}");
            
            try
            {
                await File.WriteAllTextAsync(testFile, testContent);
                System.Diagnostics.Debug.WriteLine($"? Archivo de prueba escrito exitosamente");
                
                // Verificar que el archivo se creó
                if (!File.Exists(testFile))
                {
                    throw new UnauthorizedAccessException("El archivo de prueba no se pudo crear (verificación falló)");
                }
                
                // Leer el archivo para verificar permisos de lectura
                var readContent = await File.ReadAllTextAsync(testFile);
                if (string.IsNullOrEmpty(readContent))
                {
                    throw new UnauthorizedAccessException("No se pudo leer el archivo de prueba");
                }
                
                System.Diagnostics.Debug.WriteLine($"? Archivo de prueba leído exitosamente ({readContent.Length} caracteres)");
                
                // Limpiar archivo de prueba
                File.Delete(testFile);
                System.Diagnostics.Debug.WriteLine($"??? Archivo de prueba eliminado");
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

    public async Task SaveToAppSettingsAsync()
    {
        try
        {
            var appPath = System.AppDomain.CurrentDomain.BaseDirectory;
            var settingsPath = Path.Combine(appPath, "appsettings.json");

            // Crear estructura de configuración para appsettings.json
            var appSettings = new
            {
                Logging = new
                {
                    logPath = Path.Combine(_configuracion.LogPath, "app.log")
                },
                GestionTime = new
                {
                    EnableDebugLogs = _configuracion.LogLevel >= LogLevel.Debug,
                    LogDirectory = _configuracion.LogPath.EndsWith("\\") ? _configuracion.LogPath : _configuracion.LogPath + "\\",
                    MaxFileSizeMB = _configuracion.LogRotation == LogRotation.BySize ? 10 : 50,
                    RetainedFileCountLimit = _configuracion.LogRetentionDays
                },
                Api = new
                {
                    BaseUrl = _configuracion.ApiUrl,
                    LoginPath = "/api/v1/auth/login",
                    PartesPath = "/api/v1/partes", 
                    ClientesPath = "/api/v1/catalog/clientes",
                    GruposPath = "/api/v1/catalog/grupos",
                    TiposPath = "/api/v1/catalog/tipos",
                    MePath = "/api/v1/auth/me"
                }
            };

            var json = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(settingsPath, json);
            System.Diagnostics.Debug.WriteLine($"? Configuración sincronizada con appsettings.json");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"?? Error sincronizando appsettings.json: {ex.Message}");
            // No lanzar excepción, solo registrar el error
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