using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using Windows.Storage;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Helpers;

namespace GestionTime.Desktop.Views
{
    public sealed partial class LoginPage : Page
    {
        private bool _isPasswordVisible = false;
        
        // 🆕 NUEVO: Path alternativo para guardar el correo (no usa ApplicationData)
        private static string GetEmailSettingsPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var gestionTimePath = Path.Combine(appDataPath, "GestionTime");
            Directory.CreateDirectory(gestionTimePath); // Asegurar que existe
            return Path.Combine(gestionTimePath, "login-settings.json");
        }

        public LoginPage()
        {
            InitializeComponent();
            
            // 🆕 NUEVO: Cargar y aplicar tema global
            ThemeService.Instance.ApplyTheme(this);
            UpdateThemeCheckmarks();
            
            // 🆕 NUEVO: Suscribirse a cambios de tema globales
            ThemeService.Instance.ThemeChanged += OnGlobalThemeChanged;
            
            // 🔥 NUEVO: Cargar correo desde archivo JSON
            LoadRememberedEmailFromFile();
            
            // Iniciar fade in cuando se carga la página
            this.Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).Loaded -= OnPageLoaded;

            App.Log?.LogInformation("LoginPage cargado");

            // Animación de entrada
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeIn, PageRootGrid);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();

            await Task.Delay(100);
            
            // 🆕 FOCUS INICIAL INTELIGENTE
            if (string.IsNullOrWhiteSpace(TxtUser.Text))
            {
                TxtUser.Focus(FocusState.Programmatic);
                App.Log?.LogDebug("🎯 Focus inicial: Correo (vacío)");
            }
            else
            {
                TxtPass.Focus(FocusState.Programmatic);
                App.Log?.LogDebug("🎯 Focus inicial: Contraseña (correo pre-rellenado: {email})", TxtUser.Text);
            }
        }

        // 🆕 MÉTODO PARA MANEJAR ENTER EN CONTRASEÑA
        private void OnPasswordKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                App.Log?.LogDebug("⌨️ Enter presionado en contraseña, iniciando login...");
                e.Handled = true;
                OnLoginClick(sender, new RoutedEventArgs());
            }
        }

        // 🔥 NUEVO: Cargar correo desde archivo JSON (no usa ApplicationData)
        private void LoadRememberedEmailFromFile()
        {
            try
            {
                var settingsPath = GetEmailSettingsPath();
                
                if (!File.Exists(settingsPath))
                {
                    App.Log?.LogDebug("📧 No existe archivo de settings: {path}", settingsPath);
                    return;
                }
                
                var json = File.ReadAllText(settingsPath);
                
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("RememberSession", out var remProp) && 
                    remProp.ValueKind == System.Text.Json.JsonValueKind.True)
                {
                    ChkRemember.IsChecked = true;
                    
                    if (root.TryGetProperty("Email", out var emailProp) && 
                        emailProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var email = emailProp.GetString();
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            TxtUser.Text = email;
                            App.Log?.LogInformation("📧 Correo cargado desde archivo: {email}", email);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error cargando correo desde archivo JSON");
            }
        }

        // 🔥 NUEVO: Guardar correo en archivo JSON (no usa ApplicationData)
        private void SaveRememberedEmailToFile()
        {
            try
            {
                var remember = ChkRemember.IsChecked == true;
                var email = TxtUser.Text?.Trim() ?? "";
                
                var settingsPath = GetEmailSettingsPath();
                
                if (remember && !string.IsNullOrWhiteSpace(email))
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        RememberSession = true,
                        Email = email,
                        LastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    
                    File.WriteAllText(settingsPath, json);
                    App.Log?.LogInformation("✅ Correo guardado en archivo: {email}", email);
                }
                else
                {
                    // Si NO está marcado o email vacío, eliminar archivo
                    if (File.Exists(settingsPath))
                    {
                        File.Delete(settingsPath);
                        App.Log?.LogInformation("🗑️ Archivo de settings eliminado (Recordar sesión desactivado)");
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error guardando correo en archivo JSON");
            }
        }

        // OBSOLETO: Ya no usamos ApplicationData
        private void LoadRememberedEmail()
        {
            LoadRememberedEmailFromFile();
        }

        // OBSOLETO: Ya no usamos ApplicationData
        private void SaveRememberedEmail()
        {
            SaveRememberedEmailToFile();
        }

        private void OnTogglePasswordClick(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            
            if (_isPasswordVisible)
            {
                // Mostrar contraseña
                TxtPassVisible.Text = TxtPass.Password;
                TxtPass.Visibility = Visibility.Collapsed;
                TxtPassVisible.Visibility = Visibility.Visible;
                IconPassword.Glyph = "\uED1A"; // EyeHide
                ToolTipService.SetToolTip(BtnTogglePassword, "Ocultar contraseña");
                
                // Mover foco al TextBox visible
                TxtPassVisible.Focus(FocusState.Programmatic);
                TxtPassVisible.SelectionStart = TxtPassVisible.Text.Length;
            }
            else
            {
                // Ocultar contraseña
                TxtPass.Password = TxtPassVisible.Text;
                TxtPassVisible.Visibility = Visibility.Collapsed;
                TxtPass.Visibility = Visibility.Visible;
                IconPassword.Glyph = "\uE7B3"; // Eye
                ToolTipService.SetToolTip(BtnTogglePassword, "Mostrar contraseña");
                
                // Mover foco al PasswordBox
                TxtPass.Focus(FocusState.Programmatic);
            }
            
            App.Log?.LogDebug("Visibilidad de contraseña alternada: {visible}", _isPasswordVisible);
        }

        private async void OnLoginClick(object sender, RoutedEventArgs e)
        {
            var email = TxtUser.Text?.Trim() ?? "";
            
            // Obtener contraseña del control visible
            var pass = _isPasswordVisible 
                ? TxtPassVisible.Text ?? "" 
                : TxtPass.Password ?? "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            {
                // 🔔 NOTIFICACIÓN: Campos vacíos
                App.Notifications?.ShowWarning(
                    "Por favor, rellena correo y contraseña",
                    title: "⚠️ Campos Requeridos");
                return;
            }

            SetBusy(true, "Conectando con el servidor...");
            
            await Task.Delay(100);

            try
            {
                var sw = Stopwatch.StartNew();

                App.Log?.LogInformation("Intentando login para: {email}", email);

                // MODO DESARROLLO
                if (email.Equals("dev", StringComparison.OrdinalIgnoreCase))
                {
                    App.Log?.LogWarning("⚠️ MODO DESARROLLO activado - Navegando sin validación");
                    
                    // 🔔 NOTIFICACIÓN: Modo desarrollo
                    App.Notifications?.ShowWarning(
                        "Acceso directo sin validación",
                        title: "🛠️ MODO DESARROLLO");
                    
                    await Task.Delay(500);
                    
                    if (App.MainWindowInstance?.Navigator != null)
                    {
                        App.MainWindowInstance.Navigator.Navigate(typeof(DiarioPage));
                        App.Log?.LogInformation("Navegación a DiarioPage en modo DEV ✅");
                    }
                    return;
                }

                SetBusy(true, "Validando credenciales...");

                // Llamada real al API
                ApiClient.LoginResponse? res = null;
                
                try
                {
                    res = await App.Api.LoginAsync(email, pass);
                }
                catch (ApiException apiEx)
                {
                    sw.Stop();
                    
                    App.Log?.LogError(apiEx, "Error de API: {statusCode} - {message}", apiEx.StatusCode, apiEx.Message);
                    
                    // 🔔 NOTIFICACIÓN: Error de API
                    App.Notifications?.ShowError(
                        apiEx.Message,
                        title: $"❌ Error de API ({apiEx.StatusCode})");
                    
                    SetBusy(false, "");
                    return;
                }
                catch (HttpRequestException httpEx)
                {
                    sw.Stop();
                    
                    var errorMsg = GetHttpErrorMessage(httpEx);
                    App.Log?.LogError(httpEx, "Error de conexión HTTP: {msg}", errorMsg);
                    
                    // 🔔 NOTIFICACIÓN: Error de conexión HTTP
                    App.Notifications?.ShowError(
                        errorMsg,
                        title: "🌐 Error de Conexión");
                    
                    SetBusy(false, "");
                    return;
                }
                catch (TaskCanceledException)
                {
                    sw.Stop();
                    
                    App.Log?.LogError("Timeout al conectar con el servidor");
                    
                    // 🔔 NOTIFICACIÓN: Timeout
                    App.Notifications?.ShowError(
                        "El servidor no responde. Verifica tu conexión.",
                        title: "⏳ Tiempo de Espera Agotado");
                    
                    SetBusy(false, "");
                    return;
                }
               
                sw.Stop();

                if (sw.ElapsedMilliseconds < 300)
                {
                    var remainingTime = 300 - (int)sw.ElapsedMilliseconds;
                    App.Log?.LogDebug("Login muy rápido ({ms}ms), agregando delay de {delay}ms para UX", 
                        sw.ElapsedMilliseconds, remainingTime);
                    await Task.Delay(remainingTime);
                }

                App.Log?.LogInformation("Respuesta de login recibida en {ms}ms. Res: {res}, Token: {hasToken}", 
                    sw.ElapsedMilliseconds, 
                    res != null, 
                    !string.IsNullOrEmpty(App.Api.AccessToken));

                if (res == null)
                {
                    // 🔔 NOTIFICACIÓN: Login fallido
                    App.Notifications?.ShowError(
                        "Verifica tus credenciales",
                        title: "❌ Login Fallido");
                    
                    SetBusy(false, "");
                    return;
                }

                // Verificar si requiere cambio de contraseña
                if (res.Message != null && res.Message.Equals("password_change_required", StringComparison.OrdinalIgnoreCase))
                {
                    App.Log?.LogInformation("Usuario {email} debe cambiar contraseña - Expired: {expired}, Days: {days}", 
                        email, res.PasswordExpired, res.DaysUntilExpiration);
                    
                    SetBusy(false, "");
                    
                    // 🔔 NOTIFICACIÓN: Contraseña debe cambiarse
                    App.Notifications?.ShowWarning(
                        "Tu contraseña ha expirado o debe ser cambiada por seguridad",
                        title: "⚠️ Cambio de Contraseña Requerido");
                    
                    await ShowChangePasswordDialog(email, res.PasswordExpired, res.DaysUntilExpiration);
                    return;
                }

                if (res.Message != null && !res.Message.Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    // 🔔 NOTIFICACIÓN: Error en login
                    App.Notifications?.ShowError(res.Message, title: "❌ Error de Autenticación");
                    
                    SetBusy(false, "");
                    return;
                }

                SetBusy(true, "Guardando sesión...");

                // 🔥 CRÍTICO: Guardar ANTES de cualquier operación de navegación
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                App.Log?.LogInformation("💾 GUARDANDO CORREO - Antes de navegar (NUEVO MÉTODO)");
                App.Log?.LogInformation("   • Correo: {email}", email);
                App.Log?.LogInformation("   • ChkRemember.IsChecked: {checked}", ChkRemember.IsChecked);
                App.Log?.LogInformation("   • Método: Archivo JSON (no usa ApplicationData)");
                
                SaveRememberedEmailToFile();
                
                App.Log?.LogInformation("✅ Correo guardado exitosamente");
                App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
                
                // ✅ PASO 1: Guardar información BÁSICA del usuario con el EMAIL DEL LOGIN
                try
                {
                    var userName = res.UserNameSafe;
                    var userEmail = email; // 🔥 USAR EMAIL DEL LOGIN, NO DEL RESPONSE
                    var userRole = res.UserRoleSafe;
                    
                    App.Log?.LogInformation("💾 PASO 1: Guardando información básica del usuario...");
                    App.Log?.LogInformation("   • UserName (de login): {name}", userName);
                    App.Log?.LogInformation("   • UserEmail (del input): {email}", userEmail);
                    App.Log?.LogInformation("   • UserRole (de login): {role}", userRole);
                    
                    // 🔥 CRÍTICO: Guardar SIEMPRE el email del login
                    UserInfoFileStorage.SaveUserInfo(userName, userEmail, userRole, null, App.Log);
                    
                    App.Log?.LogInformation("✅ Información básica guardada correctamente");
                    
                    // ✅ PASO 2: Intentar cargar perfil completo (OPCIONAL - sin sobrescribir email)
                    if (string.IsNullOrEmpty(res.UserName) || string.IsNullOrEmpty(res.UserRole))
                    {
                        App.Log?.LogInformation("🔄 LoginResponse incompleto, intentando cargar perfil desde /api/v1/profiles/me...");
                        
                        SetBusy(true, "Cargando perfil completo...");
                        
                        try
                        {
                            var profileLoaded = await ProfileService.LoadProfileAfterLoginAsync(App.Log, userEmail);
                            
                            if (profileLoaded)
                            {
                                App.Log?.LogInformation("✅ Perfil completo cargado correctamente");
                            }
                            else
                            {
                                App.Log?.LogWarning("⚠️ No se pudo cargar el perfil completo, usando datos básicos del login");
                            }
                        }
                        catch (Exception profileEx)
                        {
                            App.Log?.LogWarning(profileEx, "⚠️ Error cargando perfil completo, usando datos básicos del login");
                        }
                    }
                    else
                    {
                        App.Log?.LogInformation("✅ LoginResponse completo, NO es necesario cargar perfil adicional");
                    }
                    
                    // 🆕 Verificar qué datos finales tenemos
                    var finalUserInfo = UserInfoFileStorage.LoadUserInfo(App.Log);
                    App.Log?.LogInformation("📝 Información de usuario final:");
                    App.Log?.LogInformation("   • UserName: {name}", finalUserInfo?.UserName ?? "NO DISPONIBLE");
                    App.Log?.LogInformation("   • UserEmail: {email}", finalUserInfo?.UserEmail ?? "NO DISPONIBLE");
                    App.Log?.LogInformation("   • UserRole: {role}", finalUserInfo?.UserRole ?? "NO DISPONIBLE");
                    
                    // 🔥 VALIDACIÓN CRÍTICA: Verificar que el email sea correcto
                    if (finalUserInfo?.UserEmail != email)
                    {
                        App.Log?.LogError("❌ ERROR CRÍTICO: Email guardado NO coincide con email del login");
                        App.Log?.LogError("   • Email del login: {loginEmail}", email);
                        App.Log?.LogError("   • Email guardado: {savedEmail}", finalUserInfo?.UserEmail);
                        App.Log?.LogError("   • RE-GUARDANDO con email correcto...");
                        
                        // Forzar guardado con email correcto
                        UserInfoFileStorage.SaveUserInfo(userName, email, userRole, null, App.Log);
                        
                        App.Log?.LogInformation("✅ Email corregido exitosamente");
                    }
                }
                catch (Exception ex)
                {
                    App.Log?.LogWarning(ex, "Error guardando información de usuario");
                }

                // 🔔 NOTIFICACIÓN: Login exitoso
                App.Notifications?.ShowSuccess(
                    $"Bienvenido de vuelta, {res.UserNameSafe}",
                    title: "✅ Inicio de Sesión Exitoso");

                SetBusy(true, "Preparando...");

                // Pausa para mostrar el mensaje de éxito
                await Task.Delay(800);

                App.Log?.LogInformation("Navegando a DiarioPage...");

                // Hacer fade out antes de navegar
                try
                {
                    var fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    
                    Storyboard.SetTarget(fadeOut, PageRootGrid);
                    Storyboard.SetTargetProperty(fadeOut, "Opacity");
                    
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeOut);
                    
                    var tcs = new TaskCompletionSource<bool>();
                    
                    storyboard.Completed += (s, args) =>
                    {
                        tcs.SetResult(true);
                    };
                    
                    storyboard.Begin();
                    
                    await tcs.Task;
                }
                catch (Exception animEx)
                {
                    App.Log?.LogWarning(animEx, "Error en animación de fade out");
                }

                // Navega a Diario
                if (App.MainWindowInstance?.Navigator != null)
                {
                    App.MainWindowInstance.Navigator.Navigate(typeof(DiarioPage));
                    App.Log?.LogInformation("Navegación a DiarioPage completada ✅");
                }
                else
                {
                    App.Log?.LogError("MainWindowInstance o Navigator es null. No se puede navegar.");
                    
                    // 🔔 NOTIFICACIÓN: Error de navegación
                    App.Notifications?.ShowError(
                        "No se puede navegar a la página principal",
                        title: "❌ Error Interno");
                    
                    SetBusy(false, "");
                }
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Login error inesperado");
                
                // 🔔 NOTIFICACIÓN: Error inesperado
                var errorMsg = GetFriendlyErrorMessage(ex);
                App.Notifications?.ShowError(errorMsg, title: "❌ Error Inesperado");
            }
            finally
            {
                SetBusy(false, "");
            }
        }

        /// <summary>
        /// Obtiene un mensaje de error amigable para errores HTTP
        /// </summary>
        private static string GetHttpErrorMessage(HttpRequestException ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            
            // 🆕 MEJORADO: Detectar errores HTML (respuestas no JSON) - Más robusto
            if (innerMsg.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || 
                innerMsg.Contains("<html", StringComparison.OrdinalIgnoreCase) || 
                innerMsg.Contains("<HTML", StringComparison.OrdinalIgnoreCase) ||
                innerMsg.Contains("<head>", StringComparison.OrdinalIgnoreCase) ||
                innerMsg.Contains("<meta", StringComparison.OrdinalIgnoreCase) ||
                innerMsg.Contains("ServiceUnavailable", StringComparison.OrdinalIgnoreCase))
            {
                // El servidor devolvió HTML en lugar de JSON
                if (ex.StatusCode != null)
                {
                    return ex.StatusCode switch
                    {
                        System.Net.HttpStatusCode.ServiceUnavailable => 
                            "⚠️ Servicio no disponible: El servidor está temporalmente fuera de línea o en mantenimiento. Por favor, intenta más tarde.",
                        System.Net.HttpStatusCode.TooManyRequests => 
                            "⏱️ Servidor saturado: Demasiadas peticiones. Espera un momento e intenta nuevamente.",
                        System.Net.HttpStatusCode.BadGateway => 
                            "🚫 Error de conexión: No se puede acceder al servidor. Verifica que el servidor esté funcionando.",
                        System.Net.HttpStatusCode.InternalServerError => 
                            "❌ Error interno del servidor: Problema en el servicio. Contacta al administrador.",
                        System.Net.HttpStatusCode.GatewayTimeout => 
                            "⏳ Tiempo de espera agotado: El servidor tardó demasiado en responder.",
                        _ => $"⚠️ Error del servidor ({(int)ex.StatusCode}): El servicio no está respondiendo correctamente. Intenta más tarde."
                    };
                }
                
                return "⚠️ Servicio no disponible: El servidor no está respondiendo correctamente. Verifica que el servidor esté funcionando o intenta más tarde.";
            }
            
            // Detectar tipos comunes de errores de conexión
            if (innerMsg.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) || 
                innerMsg.Contains("nodename nor servname provided", StringComparison.OrdinalIgnoreCase))
            {
                return "🌐 Servidor no encontrado: Verifica la URL del servidor en la configuración.";
            }
            
            if (innerMsg.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                innerMsg.Contains("actively refused", StringComparison.OrdinalIgnoreCase))
            {
                return "🚫 Conexión rechazada: El servidor no está disponible o no acepta conexiones.";
            }
            
            if (innerMsg.Contains("Connection timed out", StringComparison.OrdinalIgnoreCase) ||
                innerMsg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return "⏳ Tiempo de espera agotado: El servidor no responde a tiempo. Verifica tu conexión.";
            }
            
            // 🆕 MEJORADO: Detectar errores HTTP por código de estado
            if (ex.StatusCode != null)
            {
                return ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => 
                        "🔒 Credenciales incorrectas: Usuario o contraseña incorrectos.",
                    System.Net.HttpStatusCode.Forbidden => 
                        "⛔ Acceso denegado: No tienes permisos para acceder.",
                    System.Net.HttpStatusCode.NotFound => 
                        "🔍 Endpoint no encontrado: Verifica la configuración del servidor.",
                    System.Net.HttpStatusCode.InternalServerError => 
                        "❌ Error interno del servidor: Problema en el servicio. Contacta al administrador.",
                    System.Net.HttpStatusCode.BadGateway => 
                        "🚫 Error de gateway: El servidor no está accesible.",
                    System.Net.HttpStatusCode.ServiceUnavailable => 
                        "⚠️ Servicio no disponible: El servidor está temporalmente fuera de línea o en mantenimiento.",
                    System.Net.HttpStatusCode.GatewayTimeout => 
                        "⏳ Tiempo de espera agotado: El servidor tardó demasiado en responder.",
                    System.Net.HttpStatusCode.TooManyRequests => 
                        "⏱️ Servidor saturado: Demasiadas peticiones. Espera un momento e intenta nuevamente.",
                    _ => $"⚠️ Error del servidor ({(int)ex.StatusCode}): {GetShortErrorMessage(innerMsg)}"
                };
            }
            
            // Detectar errores HTTP por contenido del mensaje (método antiguo - fallback)
            if (innerMsg.Contains("401", StringComparison.OrdinalIgnoreCase))
            {
                return "🔒 Credenciales incorrectas (401): Usuario o contraseña incorrectos.";
            }
            
            if (innerMsg.Contains("403", StringComparison.OrdinalIgnoreCase))
            {
                return "⛔ Acceso denegado (403): No tienes permisos.";
            }
            
            if (innerMsg.Contains("404", StringComparison.OrdinalIgnoreCase))
            {
                return "🔍 Endpoint no encontrado (404): Verifica la configuración del servidor.";
            }
            
            if (innerMsg.Contains("500", StringComparison.OrdinalIgnoreCase) || 
                innerMsg.Contains("Internal Server Error", StringComparison.OrdinalIgnoreCase))
            {
                return "❌ Error interno del servidor (500): Contacta al administrador.";
            }
            
            if (innerMsg.Contains("502", StringComparison.OrdinalIgnoreCase) || 
                innerMsg.Contains("Bad Gateway", StringComparison.OrdinalIgnoreCase))
            {
                return "🚫 Error de gateway (502): El servidor no está accesible.";
            }
            
            if (innerMsg.Contains("503", StringComparison.OrdinalIgnoreCase) || 
                innerMsg.Contains("Service Unavailable", StringComparison.OrdinalIgnoreCase))
            {
                return "⚠️ Servicio no disponible (503): El servidor está temporalmente fuera de línea.";
            }
            
            if (innerMsg.Contains("504", StringComparison.OrdinalIgnoreCase) || 
                innerMsg.Contains("Gateway Timeout", StringComparison.OrdinalIgnoreCase))
            {
                return "⏳ Tiempo de espera agotado (504): El servidor tardó demasiado en responder.";
            }
            
            if (innerMsg.Contains("429", StringComparison.OrdinalIgnoreCase) || 
                innerMsg.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
            {
                return "⏱️ Servidor saturado (429): Demasiadas peticiones. Espera un momento.";
            }
            
            // Error genérico
            return $"⚠️ Error de conexión: {GetShortErrorMessage(innerMsg)}";
        }

        /// <summary>
        /// Obtiene una versión corta y amigable de un mensaje de error técnico
        /// </summary>
        private static string GetShortErrorMessage(string message)
        {
            // Si el mensaje es muy largo o contiene HTML, devolver un mensaje genérico
            if (message.Length > 100 || 
                message.Contains("<", StringComparison.OrdinalIgnoreCase) || 
                message.Contains(">", StringComparison.OrdinalIgnoreCase))
            {
                return "El servidor no está respondiendo correctamente.";
            }
            
            return message;
        }

        /// <summary>
        /// Obtiene un mensaje de error amigable para cualquier excepción
        /// </summary>
        private static string GetFriendlyErrorMessage(Exception ex)
        {
            if (ex is HttpRequestException httpEx)
            {
                return GetHttpErrorMessage(httpEx);
            }
            
            if (ex is TaskCanceledException)
            {
                return "⏳ Operación cancelada o timeout: El servidor tardó demasiado en responder.";
            }
            
            if (ex is System.Net.Sockets.SocketException)
            {
                return "🌐 Error de red: No se puede establecer conexión con el servidor.";
            }
            
            if (ex is ApiException apiEx)
            {
                return $"❌ Error del servidor: {apiEx.Message}";
            }
            
            return $"⚠️ Error inesperado: {GetShortErrorMessage(ex.Message)}";
        }

        private void SetBusy(bool busy, string status)
        {
            Prg.IsActive = busy;
            Prg.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            BtnLogin.IsEnabled = !busy;
            TxtUser.IsEnabled = !busy;
            TxtPass.IsEnabled = !busy;
            TxtPassVisible.IsEnabled = !busy;
            BtnTogglePassword.IsEnabled = !busy;
            
            TxtStatus.Text = status;
            TxtStatus.Visibility = string.IsNullOrEmpty(status) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Cargar el tema guardado en configuración
        /// </summary>
        private void LoadSavedTheme()
        {
            // Ya no necesitamos cargar aquí, ThemeService lo hace
            // Solo aplicamos el tema actual
            ThemeService.Instance.ApplyTheme(this);
            UpdateThemeCheckmarks();
        }

        /// <summary>
        /// 🆕 MODIFICADO: Usar ThemeService para guardar tema
        /// </summary>
        private void SaveTheme(ElementTheme theme)
        {
            // Delegar al servicio centralizado
            ThemeService.Instance.SetTheme(theme);
        }

        /// <summary>
        /// 🆕 MODIFICADO: Usar ThemeService para aplicar tema
        /// </summary>
        private void SetTheme(ElementTheme theme)
        {
            // Delegar al servicio centralizado (notificará a todos los componentes)
            ThemeService.Instance.SetTheme(theme);
            
            // Actualizar checkmarks localmente
            UpdateThemeCheckmarks();
        }
        
        /// <summary>
        /// 🆕 NUEVO: Actualiza los checkmarks del menú de tema
        /// </summary>
        private void UpdateThemeCheckmarks()
        {
            var currentTheme = ThemeService.Instance.CurrentTheme;
            ThemeSystemItem.IsChecked = currentTheme == ElementTheme.Default;
            ThemeLightItem.IsChecked = currentTheme == ElementTheme.Light;
            ThemeDarkItem.IsChecked = currentTheme == ElementTheme.Dark;
        }

        /// <summary>
        /// Eventos del menú de tema
        /// </summary>
        private void OnThemeSystem(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Default);
        private void OnThemeLight(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Light);
        private void OnThemeDark(object sender, RoutedEventArgs e) => SetTheme(ElementTheme.Dark);

        /// <summary>
        /// Navegar a la página de registro
        /// </summary>
        private async void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                
                Storyboard.SetTarget(fadeOut, PageRootGrid);
                Storyboard.SetTargetProperty(fadeOut, "Opacity");
                
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeOut);
                
                var tcs = new TaskCompletionSource<bool>();
                storyboard.Completed += (s, args) =>
                {
                    tcs.SetResult(true);
                };
                
                storyboard.Begin();
                await tcs.Task;
                
                App.MainWindowInstance?.Navigator?.Navigate(typeof(RegisterPage));
                App.Log?.LogInformation("Navegando a RegisterPage");
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Error navegando a RegisterPage");
                App.MainWindowInstance?.Navigator?.Navigate(typeof(RegisterPage));
            }
        }

        /// <summary>
        /// Navegar a la página de recuperación de contraseña
        /// </summary>
        private async void OnForgotPasswordClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                
                Storyboard.SetTarget(fadeOut, PageRootGrid);
                Storyboard.SetTargetProperty(fadeOut, "Opacity");
                
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeOut);
                
                var tcs = new TaskCompletionSource<bool>();
                storyboard.Completed += (s, args) =>
                {
                    tcs.SetResult(true);
                };
                
                storyboard.Begin();
                await tcs.Task;
                
                App.MainWindowInstance?.Navigator?.Navigate(typeof(ForgotPasswordPage));
                App.Log?.LogInformation("Navegando a ForgotPasswordPage");
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Error navegando a ForgotPasswordPage");
                App.MainWindowInstance?.Navigator?.Navigate(typeof(ForgotPasswordPage));
            }
        }
        
        /// <summary>
        ///Mostrar diálogo para cambio de contraseña obligatorio
        /// </summary>
        private async Task ShowChangePasswordDialog(string email, bool passwordExpired, int daysUntilExpiration)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Cambio de Contraseña Requerido",
                    PrimaryButtonText = "Cambiar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary
                };

                // Crear el contenido del diálogo
                var stackPanel = new StackPanel { Spacing = 15 };

                // Mensaje informativo
                var messageText = passwordExpired 
                    ? "Tu contraseña ha expirado. Debes cambiarla para continuar."
                    : daysUntilExpiration <= 7
                        ? $"Tu contraseña expira en {daysUntilExpiration} días. Se recomienda cambiarla ahora."
                        : "Por seguridad, debes cambiar tu contraseña antes de continuar.";

                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = messageText, 
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                // Campo contraseña actual
                stackPanel.Children.Add(new TextBlock { Text = "Contraseña actual:" });
                var currentPasswordBox = new PasswordBox { PlaceholderText = "Ingresa tu contraseña actual" };
                stackPanel.Children.Add(currentPasswordBox);

                // Campo nueva contraseña
                stackPanel.Children.Add(new TextBlock { Text = "Nueva contraseña:" });
                var newPasswordBox = new PasswordBox { PlaceholderText = "Mínimo 6 caracteres" };
                stackPanel.Children.Add(newPasswordBox);

                // Campo confirmar contraseña
                stackPanel.Children.Add(new TextBlock { Text = "Confirmar nueva contraseña:" });
                var confirmPasswordBox = new PasswordBox { PlaceholderText = "Repite la nueva contraseña" };
                stackPanel.Children.Add(confirmPasswordBox);

                dialog.Content = stackPanel;

                // Mostrar el diálogo
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var currentPassword = currentPasswordBox.Password?.Trim() ?? "";
                    var newPassword = newPasswordBox.Password?.Trim() ?? "";
                    var confirmPassword = confirmPasswordBox.Password?.Trim() ?? "";

                    // Validaciones
                    if (string.IsNullOrWhiteSpace(currentPassword))
                    {
                        App.Notifications?.ShowWarning(
                            "Por favor, ingresa tu contraseña actual",
                            title: "⚠️ Campo Requerido");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    {
                        App.Notifications?.ShowWarning(
                            "La nueva contraseña debe tener al menos 6 caracteres",
                            title: "⚠️ Contraseña Inválida");
                        return;
                    }

                    if (newPassword != confirmPassword)
                    {
                        App.Notifications?.ShowWarning(
                            "Las contraseñas no coinciden",
                            title: "⚠️ Error de Validación");
                        return;
                    }

                    if (currentPassword == newPassword)
                    {
                        App.Notifications?.ShowWarning(
                            "La nueva contraseña debe ser diferente a la actual",
                            title: "⚠️ Contraseña Duplicada");
                        return;
                    }

                    // Intentar cambiar la contraseña
                    await PerformPasswordChange(email, currentPassword, newPassword);
                }
                else
                {
                    App.Log?.LogInformation("Usuario canceló el cambio de contraseña");
                }
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Error mostrando diálogo de cambio de contraseña");
                
                // 🔔 NOTIFICACIÓN: Error mostrando diálogo
                App.Notifications?.ShowError(
                    "Error interno. Intenta nuevamente",
                    title: "❌ Error al Mostrar Diálogo");
            }
        }

        /// <summary>
        /// Realizar el cambio de contraseña
        /// </summary>
        private async Task PerformPasswordChange(string email, string currentPassword, string newPassword)
        {
            SetBusy(true, "Cambiando contraseña...");
            
            try
            {
                App.Log?.LogInformation("Cambiando contraseña para usuario: {email}", email);

                var response = await App.Api.ChangePasswordAsync(email, currentPassword, newPassword);

                if (response?.Success == true)
                {
                    App.Log?.LogInformation("Contraseña cambiada exitosamente para: {email}", email);
                    
                    // 🔔 NOTIFICACIÓN: Contraseña cambiada exitosamente
                    App.Notifications?.ShowSuccess(
                        "Ahora puedes iniciar sesión con tu nueva contraseña",
                        title: "✅ Contraseña Actualizada");
                    
                    // Limpiar campos
                    TxtUser.Text = email;
                    TxtPass.Password = "";
                    TxtPassVisible.Text = "";
                }
                else
                {
                    var errorMessage = response?.Error ?? "Error desconocido al cambiar la contraseña";
                    App.Log?.LogWarning("Error al cambiar contraseña: {error}", errorMessage);
                    
                    // 🔔 NOTIFICACIÓN: Error al cambiar contraseña
                    App.Notifications?.ShowError(
                        errorMessage,
                        title: "❌ Error al Cambiar Contraseña");
                    
                    // Volver a mostrar el diálogo si hubo error
                    await Task.Delay(2000);
                    await ShowChangePasswordDialog(email, false, 0);
                }
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Excepción al cambiar contraseña");
                
                // 🔔 NOTIFICACIÓN: Error de conexión al cambiar contraseña
                App.Notifications?.ShowError(
                    "Error de conexión. Verifica tu conexión a internet",
                    title: "🌐 Sin Conexión");
                
                // Volver a mostrar el diálogo si hubo error de conexión
                await Task.Delay(2000);
                await ShowChangePasswordDialog(email, false, 0);
            }
            finally
            {
                SetBusy(false, "");
            }
        }
        
        /// <summary>
        /// 🆕 NUEVO: Manejador de cambios de tema globales
        /// </summary>
        private void OnGlobalThemeChanged(object? sender, ElementTheme theme)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                this.RequestedTheme = theme;
                UpdateThemeCheckmarks();
                App.Log?.LogDebug("🎨 LoginPage: Tema actualizado por cambio global a {theme}", theme);
            });
        }
    }
    
    /// <summary>
    /// Respuesta del endpoint /api/v1/users/me
    /// </summary>
    internal sealed class UserInfoResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}
