using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

using Microsoft.UI.Xaml.Controls;

using GestionTime.Desktop.Diagnostics;
using GestionTime.Desktop.Services;

namespace GestionTime.Desktop;

public partial class App : Application
{
    public static ILoggerFactory LogFactory { get; private set; } = null!;
    public static ILogger Log { get; private set; } = null!;
    public static MainWindow MainWindowInstance { get; private set; } = null!;
    public static ConfiguracionService ConfiguracionService => Services.ConfiguracionService.Instance;
    public static ApiClient Api { get; private set; } = null!;
    public static string PartesPath { get; private set; } = "/api/v1/partes";
    public static void ApplyThemeFromSettings()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            var theme = settings.Values["AppTheme"] as string ?? "System";

            if (MainWindowInstance?.Content is FrameworkElement root)
            {
                root.RequestedTheme = theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }

            Log?.LogInformation("Theme aplicado: {Theme}", theme);
        }
        catch (Exception ex)
        {
            Log?.LogWarning(ex, "No se pudo aplicar theme");
        }
    }

    public App()
    {
        InitializeComponent();

        // Lee appsettings.json si existe (sin paquetes extra)
        var settings = LoadAppSettings();

        var logPath = !string.IsNullOrWhiteSpace(settings.LogPath)
            ? Path.IsPathRooted(settings.LogPath) 
                ? settings.LogPath! 
                : Path.Combine(AppContext.BaseDirectory, settings.LogPath!)
            : ResolveLogPath();

        // Crear directorio ANTES de inicializar logger
        try
        {
            var logDir = Path.GetDirectoryName(logPath)!;
            Directory.CreateDirectory(logDir);
            
            // Test básico de escritura
            File.WriteAllText(Path.Combine(logDir, "test_startup.txt"), $"Startup test at {DateTime.Now}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creando directorio log: {ex.Message}");
            // Usar fallback
            logPath = ResolveLogPath();
        }

        // ===== SISTEMA DE LOGGING OPTIMIZADO =====
        try
        {
            LogFactory = LoggerFactory.Create(builder =>
            {
                #if DEBUG
                    builder.SetMinimumLevel(LogLevel.Debug);
                #else
                    builder.SetMinimumLevel(LogLevel.Information);
                #endif
                
                // Logger unificado con rotación automática
                builder.AddProvider(new RotatingFileLoggerProvider(
                    logPath,
                    maxFileSize: 10_000_000,  // 10MB
                    maxFiles: 5               // 5 archivos históricos
                ));
            });

            Log = LogFactory.CreateLogger("GestionTime");

            // Inicializar loggers especializados
            SpecializedLoggers.Initialize(LogFactory);

            #if DEBUG
                Log.LogInformation("🛠️ MODO DEBUG: Logging verboso activado");
            #else
                Log.LogInformation("🏭 MODO RELEASE: Logging optimizado para producción");
            #endif

            Log.LogInformation("📊 Sistema de logging inicializado - Rotación: 10MB/5 archivos");
            Log.LogInformation("APP START - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            #if DEBUG
            // Ejecutar pruebas automáticas del sistema de logging
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // Esperar a que el sistema esté completamente inicializado
                
                try
                {
                    Log.LogInformation("🧪 INICIANDO PRUEBAS AUTOMÁTICAS DEL SISTEMA DE LOGGING");
                    
                    var testResult = await LoggingTestUtilities.RunLoggingTestAsync(Log);
                    var fileResult = LoggingTestUtilities.VerifyLogFiles(Log);
                    
                    Log.LogInformation("📊 RESULTADO DE PRUEBAS: {PassedTests}/{TotalTests} pruebas pasadas ({SuccessRate:F1}%)",
                        testResult.PassedTests, testResult.TotalTests, testResult.SuccessRate);
                    
                    if (fileResult.Success)
                    {
                        Log.LogInformation("📁 ARCHIVOS DE LOG: {LogFiles} encontrados en {Directory}, {Recent} recientes",
                            fileResult.LogFilesFound, fileResult.LogDirectory, fileResult.RecentLogFiles);
                    }
                    
                    if (testResult.OverallSuccess && fileResult.Success)
                    {
                        Log.LogInformation("✅ SISTEMA DE LOGGING: FUNCIONANDO CORRECTAMENTE");
                    }
                    else
                    {
                        Log.LogWarning("⚠️ SISTEMA DE LOGGING: Algunos tests fallaron");
                        if (!string.IsNullOrEmpty(testResult.ErrorMessage))
                        {
                            Log.LogError("Error en pruebas: {Error}", testResult.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, "❌ Error ejecutando pruebas automáticas del sistema de logging");
                }
            });
            #endif

            var baseUrl = settings.BaseUrl ?? "https://localhost:2501";
            var loginPath = settings.LoginPath ?? "/api/v1/auth/login";
            PartesPath = settings.PartesPath ?? PartesPath;

            Api = new ApiClient(baseUrl, loginPath, Log);

            HookGlobalExceptions();

            Debug.WriteLine("LOG PATH = " + logPath);
            Log.LogInformation("App() inicializada. Log en: {path}", logPath);
            Log.LogInformation("API BaseUrl={baseUrl} LoginPath={loginPath} PartesPath={partesPath}", baseUrl, loginPath, PartesPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error inicializando logging: {ex.Message}");
            // Crear logger básico como fallback
            LogFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug));
            Log = LogFactory.CreateLogger("GestionTime");
        }
    }

    private sealed record AppSettings(string? BaseUrl, string? LoginPath, string? PartesPath, string? LogPath);

    private static AppSettings LoadAppSettings()
    {
        // Buscamos appsettings.json junto al exe, y si no, en el working dir.
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(Environment.CurrentDirectory, "appsettings.json"),
        };

        foreach (var file in candidates)
        {
            try
            {
                if (!File.Exists(file))
                    continue;

                var json = File.ReadAllText(file);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                    continue;

                static string? GetString(System.Text.Json.JsonElement el, string name)
                    => el.TryGetProperty(name, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
                        ? v.GetString()
                        : null;

                // Soporta 2 formatos:
                // 1) {"BaseUrl":"...","LoginPath":"..."}
                // 2) {"Api":{"BaseUrl":"...","LoginPath":"..."},"Logging":{"LogPath":"..."}}

                var apiEl = doc.RootElement;
                if (doc.RootElement.TryGetProperty("Api", out var apiObj) && apiObj.ValueKind == System.Text.Json.JsonValueKind.Object)
                    apiEl = apiObj;

                string? logPath = null;
                if (doc.RootElement.TryGetProperty("Logging", out var logObj) && logObj.ValueKind == System.Text.Json.JsonValueKind.Object)
                    logPath = GetString(logObj, "LogPath");

                // compat: LogPath también en raíz
                logPath ??= GetString(doc.RootElement, "LogPath");

                return new AppSettings(
                    BaseUrl: GetString(apiEl, "BaseUrl") ?? GetString(doc.RootElement, "BaseUrl"),
                    LoginPath: GetString(apiEl, "LoginPath") ?? GetString(doc.RootElement, "LoginPath"),
                    PartesPath: GetString(apiEl, "PartesPath") ?? GetString(doc.RootElement, "PartesPath"),
                    LogPath: logPath
                );
            }
            catch
            {
                // ignora y prueba siguiente
            }
        }

        return new AppSettings(null, null, null, null);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log.LogInformation("OnLaunched()");
        MainWindowInstance = new MainWindow();
        MainWindowInstance.Activate();
        ApplyThemeFromSettings();

    }

    private static string ResolveLogPath()
    {
        // 1) Intentar junto al EXE (ideal en Debug/Unpackaged)
        var exePath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");

        // 2) Fallback seguro (Packaged): LocalState\logs\app.log
        var localStatePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs", "app.log");

        // 3) Último recurso: TEMP
        var tempPath = Path.Combine(Path.GetTempPath(), "GestionTime", "logs", "app.log");

        foreach (var path in new[] { exePath, localStatePath, tempPath })
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                // Test escritura
                File.AppendAllText(path, $"--- log start {DateTime.Now:O} ---{Environment.NewLine}");
                return path;
            }
            catch
            {
                // probar siguiente
            }
        }

        // Por si todo falla (muy raro)
        return tempPath;
    }

    private void HookGlobalExceptions()
    {
        // WinUI UI thread exceptions
        this.UnhandledException += (s, e) =>
        {
            Log.LogError(e.Exception, "UnhandledException (UI Thread): {msg}", e.Message);
            // Si quieres evitar cierre en algunos casos:
            // e.Handled = true;
        };

        // Background thread exceptions
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Log.LogError(e.ExceptionObject as Exception,
                "AppDomain UnhandledException. IsTerminating={t}", e.IsTerminating);
        };

        // Task exceptions no observadas
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log.LogError(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        };
    }
}
