using Microsoft.Extensions.Logging;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel; // Alias para evitar conflicto
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

using Microsoft.UI.Xaml.Controls;

using GestionTime.Desktop.Diagnostics;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Services.Notifications;
using GestionTime.Desktop.Models.Dtos; // 🆕 NUEVO: Para UserProfileResponse
using GestionTime.Desktop.Models; // 🆕 NUEVO: Para UpdateInfo

namespace GestionTime.Desktop;

public partial class App : Application
{
    public static ILoggerFactory LogFactory { get; private set; } = null!;
    public static ILogger Log { get; private set; } = null!;
    public static MainWindow MainWindowInstance { get; private set; } = null!;
    public static ConfiguracionService ConfiguracionService => Services.ConfiguracionService.Instance;
    public static ApiClient Api { get; private set; } = null!;
    public static string PartesPath { get; private set; } = "/api/v1/partes";
    
    // 🆕 NUEVO: ProfileService como singleton compartido
    public static ProfileService ProfileService { get; private set; } = null!;
    
    // 🆕 NUEVO: NotificationService como singleton compartido
    public static INotificationService? Notifications { get; private set; }
    
    // 🆕 NUEVO: UpdateService para verificar actualizaciones
    public static IUpdateService UpdateService { get; private set; } = null!;

    // 🆕 NUEVO: Session Store Global para perfil de usuario
    public static UserProfileResponse? CurrentUserProfile { get; set; }
    
    // 🆕 NUEVO: Email del login (guardado cuando el usuario hace login)
    public static string? CurrentLoginEmail { get; set; }

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
        // ===== LOG CRÍTICO DE INICIO =====
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Diagnostics.Debug.WriteLine($"🚀 GESTIONTIME DESKTOP - INICIO DE APLICACIÓN");
        System.Diagnostics.Debug.WriteLine($"📅 Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        System.Diagnostics.Debug.WriteLine($"💻 OS: {Environment.OSVersion}");
        System.Diagnostics.Debug.WriteLine($"🔢 .NET: {Environment.Version}");
        System.Diagnostics.Debug.WriteLine($"📁 BaseDirectory: {AppContext.BaseDirectory}");
        System.Diagnostics.Debug.WriteLine($"📂 CurrentDirectory: {Environment.CurrentDirectory}");
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════════");

        try
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("✅ InitializeComponent() completado");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ERROR CRÍTICO en InitializeComponent: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
            // Escribir en un archivo de emergencia
            WriteEmergencyLog($"InitializeComponent failed: {ex}");
            throw;
        }

        // Lee appsettings.json si existe (sin paquetes extra)
        var settings = LoadAppSettings();
        
        // DEBUG: Log de configuración cargada
        System.Diagnostics.Debug.WriteLine($"=== CONFIGURACIÓN DEBUG ===");
        System.Diagnostics.Debug.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}");
        System.Diagnostics.Debug.WriteLine($"CurrentDirectory: {Environment.CurrentDirectory}");
        System.Diagnostics.Debug.WriteLine($"settings.BaseUrl: '{settings.BaseUrl}'");
        System.Diagnostics.Debug.WriteLine($"settings.LoginPath: '{settings.LoginPath}'");

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
            System.Diagnostics.Debug.WriteLine($"✅ Directorio de logs creado: {logDir}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error creando directorio log: {ex.Message}");
            // Usar fallback
            logPath = ResolveLogPath();
            System.Diagnostics.Debug.WriteLine($"   Usando fallback: {logPath}");
        }

        // ===== SISTEMA DE LOGGING OPTIMIZADO =====
        try
        {
            LogFactory = LoggerFactory.Create(builder =>
            {
                #if DEBUG
                    builder.SetMinimumLevel(MsLogLevel.Debug);
                #else
                    builder.SetMinimumLevel(MsLogLevel.Information);
                #endif
                
                // TEMPORAL: Usar solo DebugFileLoggerProvider para debugging
                builder.AddProvider(new DebugFileLoggerProvider(logPath));
            });

            Log = LogFactory.CreateLogger("GestionTime");

            // Inicializar loggers especializados
            SpecializedLoggers.Initialize(LogFactory);

            #if DEBUG
                Log.LogInformation("🛠️ MODO DEBUG: Logging verboso activado");
            #else
                Log.LogInformation("🏭 MODO RELEASE: Logging optimizado para producción");
            #endif

            Log.LogInformation("📊 Sistema de logging inicializado - TEMPORAL DEBUG VERSION");
            Log.LogInformation("APP START - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Log.LogInformation("OS: {OS}, .NET: {Version}", Environment.OSVersion, Environment.Version);
            Log.LogInformation("BaseDirectory: {BaseDir}", AppContext.BaseDirectory);

            // TEMPORAL: Hardcode la URL para asegurar que no use localhost
            var baseUrl = "https://gestiontimeapi.onrender.com";
            var loginPath = "/api/v1/auth/login-desktop";  // CAMBIADO: endpoint específico de desktop
            PartesPath = "/api/v1/partes";
            
            // DEBUG: Log de URLs forzadas
            System.Diagnostics.Debug.WriteLine($"=== URLs HARDCODED ===");
            System.Diagnostics.Debug.WriteLine($"baseUrl HARDCODED: '{baseUrl}'");
            System.Diagnostics.Debug.WriteLine($"loginPath HARDCODED: '{loginPath}'");

            Api = new ApiClient(baseUrl, loginPath, Log);
            
            // 🆕 NUEVO: Suscribirse al evento de token expirado
            Api.TokenExpired += OnTokenExpired;
            
            // 🆕 NUEVO: Inicializar ProfileService como singleton
            ProfileService = new ProfileService(Api, null); // El ProfileService creará su propio logger si se necesita
            
            // 🆕 NUEVO: Inicializar NotificationService como singleton
            Notifications = new NotificationService(LogFactory.CreateLogger<NotificationService>());
            Log.LogInformation("NotificationService inicializado. Enabled={enabled}", Notifications.IsEnabled);
            
            // 🆕 NUEVO: Inicializar UpdateService
            UpdateService = new UpdateService(LogFactory.CreateLogger<UpdateService>());
            Log.LogInformation("UpdateService inicializado");

            HookGlobalExceptions();

            Debug.WriteLine("LOG PATH = " + logPath);
            Log.LogInformation("App() inicializada. Log en: {path}", logPath);
            Log.LogInformation("API BaseUrl={baseUrl} LoginPath={loginPath} PartesPath={partesPath}", baseUrl, loginPath, PartesPath);
            
            System.Diagnostics.Debug.WriteLine("✅ App() constructor completado exitosamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error inicializando logging: {ex.Message}");
            WriteEmergencyLog($"Logging init failed: {ex}");
            
            // Crear logger básico como fallback
            LogFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(MsLogLevel.Debug));
            Log = LogFactory.CreateLogger("GestionTime");
            
            // TEMPORAL: URLs hardcoded en fallback también
            var baseUrl = "https://gestiontimeapi.onrender.com";
            var loginPath = "/api/v1/auth/login-desktop";  // CAMBIADO: endpoint específico de desktop
            
            System.Diagnostics.Debug.WriteLine($"=== FALLBACK HARDCODED URLS ===");
            System.Diagnostics.Debug.WriteLine($"baseUrl FALLBACK HARDCODED: '{baseUrl}'");
            System.Diagnostics.Debug.WriteLine($"loginPath FALLBACK HARDCODED: '{loginPath}'");
            
            Api = new ApiClient(baseUrl, loginPath, Log);
            
            // 🆕 NUEVO: Inicializar ProfileService incluso en fallback
            ProfileService = new ProfileService(Api, null);
            
            // 🆕 NUEVO: Inicializar NotificationService incluso en fallback
            Notifications = new NotificationService(LogFactory.CreateLogger<NotificationService>());
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

        System.Diagnostics.Debug.WriteLine($"=== CARGANDO APPSETTINGS ===");
        foreach (var candidate in candidates)
        {
            System.Diagnostics.Debug.WriteLine($"Probando: {candidate}");
            System.Diagnostics.Debug.WriteLine($"Existe: {File.Exists(candidate)}");
        }

        foreach (var file in candidates)
        {
            try
            {
                if (!File.Exists(file))
                    continue;

                var json = File.ReadAllText(file);
                System.Diagnostics.Debug.WriteLine($"JSON leído desde {file}: {json}");
                
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

                var result = new AppSettings(
                    BaseUrl: GetString(apiEl, "BaseUrl") ?? GetString(doc.RootElement, "BaseUrl"),
                    LoginPath: GetString(apiEl, "LoginPath") ?? GetString(doc.RootElement, "LoginPath"),
                    PartesPath: GetString(apiEl, "PartesPath") ?? GetString(doc.RootElement, "PartesPath"),
                    LogPath: logPath
                );
                
                System.Diagnostics.Debug.WriteLine($"=== SETTINGS CARGADOS ===");
                System.Diagnostics.Debug.WriteLine($"BaseUrl: '{result.BaseUrl}'");
                System.Diagnostics.Debug.WriteLine($"LoginPath: '{result.LoginPath}'");
                System.Diagnostics.Debug.WriteLine($"LogPath: '{result.LogPath}'");
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando {file}: {ex.Message}");
                // ignora y prueba siguiente
            }
        }

        System.Diagnostics.Debug.WriteLine("=== NO SE ENCONTRÓ APPSETTINGS ===");
        return new AppSettings(null, null, null, null);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Diagnostics.Debug.WriteLine("🚀 OnLaunched() - Iniciando ventana principal");
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════════");
        
        try
        {
            Log.LogInformation("OnLaunched() - Iniciando aplicación");
            
            System.Diagnostics.Debug.WriteLine("   Creando MainWindow...");
            MainWindowInstance = new MainWindow();
            System.Diagnostics.Debug.WriteLine("   ✅ MainWindow creada");
            
            System.Diagnostics.Debug.WriteLine("   Activando ventana...");
            MainWindowInstance.Activate();
            System.Diagnostics.Debug.WriteLine("   ✅ Ventana activada");
            
            System.Diagnostics.Debug.WriteLine("   Aplicando tema...");
            ApplyThemeFromSettings();
            System.Diagnostics.Debug.WriteLine("   ✅ Tema aplicado");
            
            // 🆕 NUEVO: Verificar actualizaciones en segundo plano
            System.Diagnostics.Debug.WriteLine("   Verificando actualizaciones...");
            _ = CheckForUpdatesAsync();
            
            Log.LogInformation("✅ Aplicación iniciada correctamente");
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("✅ OnLaunched() completado exitosamente");
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"❌ ERROR CRÍTICO en OnLaunched(): {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Tipo: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"   Inner Stack: {ex.InnerException.StackTrace}");
            }
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════════");
            
            Log?.LogCritical(ex, "ERROR CRÍTICO en OnLaunched - La aplicación no puede iniciar");
            WriteEmergencyLog($"OnLaunched failed: {ex}");
            
            // Intentar mostrar un diálogo de error al usuario
            try
            {
                ShowCriticalErrorDialog(ex);
            }
            catch
            {
                // Si ni siquiera podemos mostrar el diálogo, escribir en archivo
                WriteEmergencyLog("Failed to show error dialog");
            }
            
            throw; // Re-throw para que Windows registre el error
        }
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
    
    // ===== VERIFICACIÓN DE ACTUALIZACIONES =====
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            Log.LogInformation("Verificando actualizaciones disponibles...");
            
            // Esperar un poco para no bloquear el inicio
            await Task.Delay(2000);
            
            var updateInfo = await UpdateService.CheckForUpdatesAsync();
            
            if (updateInfo.UpdateAvailable)
            {
                Log.LogInformation("Nueva versión disponible: {LatestVersion}", updateInfo.LatestVersion);
                
                // Mostrar notificación al usuario
                MainWindowInstance.DispatcherQueue.TryEnqueue(() =>
                {
                    ShowUpdateNotification(updateInfo);
                });
            }
            else
            {
                Log.LogInformation("Aplicación actualizada. Versión actual: {CurrentVersion}", updateInfo.CurrentVersion);
            }
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "No se pudo verificar actualizaciones");
        }
    }
    
    private void ShowUpdateNotification(UpdateInfo updateInfo)
    {
        // Si hay un NotificationService, usar ese
        if (Notifications != null && Notifications.IsEnabled)
        {
            var message = $"Nueva versión {updateInfo.LatestVersion} disponible. ¡Descárgala ahora!";
            var title = "Actualización disponible";
            
            var options = new NotificationOptions
            {
                DurationMs = 10000
            };
            
            Notifications.ShowInfo(message, title, options);
            return;
        }
        
        // Fallback: Crear un InfoBar en la ventana principal
        try
        {
            if (MainWindowInstance?.Content is Microsoft.UI.Xaml.Controls.Frame frame &&
                frame.Content is FrameworkElement pageRoot)
            {
                var infoBar = new Microsoft.UI.Xaml.Controls.InfoBar
                {
                    Title = "Actualización disponible",
                    Message = $"Nueva versión {updateInfo.LatestVersion} disponible. Tu versión actual es {updateInfo.CurrentVersion}.",
                    Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational,
                    IsOpen = true,
                    IsClosable = true
                };
                
                // Agregar botón de descarga
                var button = new Microsoft.UI.Xaml.Controls.HyperlinkButton
                {
                    Content = "Ver actualizaciones",
                    NavigateUri = new Uri("https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases")
                };
                
                infoBar.ActionButton = button;
                
                // Intentar agregarlo al panel raíz de la página
                if (pageRoot is Microsoft.UI.Xaml.Controls.Panel panel)
                {
                    panel.Children.Insert(0, infoBar);
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "No se pudo mostrar notificación de actualización");
        }
    }

    // ===== RECONFIGURACIÓN DINÁMICA DEL LOGGER =====
    /// <summary>
    /// Reconfigura el sistema de logging con un nuevo path de archivo
    /// </summary>
    /// <param name="newLogPath">Ruta completa del nuevo archivo de log (incluyendo nombre de archivo)</param>
    public static void ReconfigureLogger(string newLogPath)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Reconfigurando logger con nuevo path: {newLogPath}");
            
            // Crear directorio si no existe
            var logDir = Path.GetDirectoryName(newLogPath);
            if (!string.IsNullOrWhiteSpace(logDir))
            {
                Directory.CreateDirectory(logDir);
                System.Diagnostics.Debug.WriteLine($"📁 Directorio creado/verificado: {logDir}");
            }
            
            // Disponer del LoggerFactory anterior
            try
            {
                (LogFactory as IDisposable)?.Dispose();
                System.Diagnostics.Debug.WriteLine("♻️ LoggerFactory anterior liberado");
            }
            catch (Exception disposeEx)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error liberando LoggerFactory: {disposeEx.Message}");
            }
            
            // Recrear LoggerFactory con el nuevo path
            LogFactory = LoggerFactory.Create(builder =>
            {
                #if DEBUG
                    builder.SetMinimumLevel(MsLogLevel.Debug);
                #else
                    builder.SetMinimumLevel(MsLogLevel.Information);
                #endif
                
                builder.AddProvider(new DebugFileLoggerProvider(newLogPath));
            });
            
            // Recrear logger principal
            Log = LogFactory.CreateLogger("GestionTime");
            
            // Reinicializar loggers especializados
            SpecializedLoggers.Initialize(LogFactory);
            
            System.Diagnostics.Debug.WriteLine($"✅ Logger reconfigurado exitosamente");
            Log.LogInformation("═══════════════════════════════════════════════════════════════");
            Log.LogInformation("🔄 SISTEMA DE LOGGING RECONFIGURADO");
            Log.LogInformation("📁 Nuevo path de logs: {path}", newLogPath);
            Log.LogInformation("⏰ Timestamp: {timestamp}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Log.LogInformation("═══════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error crítico reconfigurando logger: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
            
            // Intentar crear un logger de emergencia
            try
            {
                LogFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(MsLogLevel.Debug));
                Log = LogFactory.CreateLogger("GestionTime");
                Log.LogError(ex, "Error reconfigurando logger - usando logger de emergencia");
            }
            catch
            {
                // Si todo falla, al menos tenemos el Debug.WriteLine
            }
        }
    }

    /// <summary>
    /// Maneja el evento cuando el token expira completamente (refresh también expiró)
    /// </summary>
    private void OnTokenExpired(object? sender, EventArgs e)
    {
        Log?.LogWarning("⚠️ Token expirado completamente, mostrando diálogo al usuario...");
        
        // Redirigir al login en el thread de UI
        if (MainWindowInstance?.DispatcherQueue != null)
        {
            MainWindowInstance.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    // Mostrar diálogo explicativo al usuario
                    var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                    {
                        Title = "🔐 Sesión Expirada",
                        Content = "Tu sesión ha expirado por motivos de seguridad.\n\n" +
                                 "Por favor, vuelve a iniciar sesión para continuar trabajando.",
                        PrimaryButtonText = "Iniciar Sesión",
                        DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                        XamlRoot = MainWindowInstance.Content.XamlRoot
                    };
                    
                    await dialog.ShowAsync();
                    
                    // Limpiar sesión del usuario
                    var settings = ApplicationData.Current.LocalSettings;
                    settings.Values.Remove("UserToken");
                    settings.Values.Remove("UserName");
                    settings.Values.Remove("UserEmail");
                    settings.Values.Remove("UserRole");
                    
                    Log?.LogInformation("Sesión limpiada después de expiración de token");
                    
                    // Navegar al login
                    if (MainWindowInstance?.Navigator != null)
                    {
                        MainWindowInstance.Navigator.Navigate(typeof(Views.LoginPage));
                        Log?.LogInformation("Usuario redirigido al login por token expirado");
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError(ex, "Error redirigiendo al login después de token expirado");
                    
                    // Fallback: redirigir sin diálogo si hay error
                    try
                    {
                        var settings = ApplicationData.Current.LocalSettings;
                        settings.Values.Remove("UserToken");
                        MainWindowInstance?.Navigator?.Navigate(typeof(Views.LoginPage));
                    }
                    catch (Exception fallbackEx)
                    {
                        Log?.LogError(fallbackEx, "Error en fallback de redirect al login");
                    }
                }
            });
        }
    }

    /// <summary>
    /// Escribe un log de emergencia cuando el sistema normal de logging falla
    /// </summary>
    private static void WriteEmergencyLog(string message)
    {
        try
        {
            var emergencyLogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GestionTime",
                "emergency.log"
            );
            
            Directory.CreateDirectory(Path.GetDirectoryName(emergencyLogPath)!);
            
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] EMERGENCY: {message}\n";
            File.AppendAllText(emergencyLogPath, logEntry);
            
            System.Diagnostics.Debug.WriteLine($"📝 Emergency log escrito en: {emergencyLogPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ No se pudo escribir emergency log: {ex.Message}");
            // Último recurso: Event Log de Windows
            try
            {
                System.Diagnostics.EventLog.WriteEntry(
                    "Application",
                    $"GestionTime Desktop - Emergency: {message}",
                    System.Diagnostics.EventLogEntryType.Error
                );
            }
            catch
            {
                // Si todo falla, al menos quedó en Debug output
            }
        }
    }

    /// <summary>
    /// Muestra un diálogo crítico de error al usuario
    /// </summary>
    private static async void ShowCriticalErrorDialog(Exception ex)
    {
        try
        {
            await Task.Delay(100); // Dar tiempo para que la UI se inicialice
            
            var dialog = new ContentDialog
            {
                Title = "❌ Error Crítico de Inicio",
                Content = $"La aplicación no pudo inciarse correctamente.\n\n" +
                         $"Error: {ex.Message}\n\n" +
                         $"Por favor, verifica:\n" +
                         $"• Windows App Runtime está instalado\n" +
                         $"• Los archivos no están bloqueados\n" +
                         $"• Tienes permisos de escritura\n\n" +
                         $"Consulta emergency.log en:\n" +
                         $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GestionTime")}",
                CloseButtonText = "Cerrar"
            };
            
            // Si MainWindow no se pudo crear, no podemos mostrar el diálogo
            // así que simplemente lo ignoramos
        }
        catch
        {
            // Ignorar errores al mostrar el diálogo
        }
    }
}
