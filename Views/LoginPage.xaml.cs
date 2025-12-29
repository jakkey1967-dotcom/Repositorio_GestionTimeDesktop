using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Extensions.Logging;
using Windows.Storage;
using GestionTime.Desktop.Services;

namespace GestionTime.Desktop.Views
{
    public sealed partial class LoginPage : Page
    {
        private bool _isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();
            
            // Cargar tema guardado
            LoadSavedTheme();
            
            LoadRememberedEmail();
            
            // Iniciar fade in cuando se carga la página
            this.Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnPageLoaded;
            
            try
            {
                // Crear animación de fade in
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
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error en animación de fade in");
                // Fallback: mostrar página inmediatamente
                PageRootGrid.Opacity = 1;
            }
        }

        private void LoadRememberedEmail()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;

                if (settings.TryGetValue("RememberSession", out var remObj) && remObj is bool rem && rem)
                {
                    ChkRemember.IsChecked = true;

                    if (settings.TryGetValue("RememberedEmail", out var emailObj) && emailObj is string email && !string.IsNullOrWhiteSpace(email))
                    {
                        TxtUser.Text = email;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "LoadRememberedEmail() falló");
            }
        }

        private void SaveRememberedEmail()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;

                var remember = ChkRemember.IsChecked == true;
                settings["RememberSession"] = remember;

                if (remember)
                    settings["RememberedEmail"] = TxtUser.Text?.Trim() ?? "";
                else
                    settings.Remove("RememberedEmail");
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "SaveRememberedEmail() falló");
            }
        }

        /// <summary>
        /// Alternar visibilidad de la contraseña
        /// </summary>
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
                ShowMessage("Por favor, rellena correo y contraseña.", MessageType.Warning);
                return;
            }

            SetBusy(true, "Conectando con el servidor...");
            HideMessage();

            try
            {
                var sw = Stopwatch.StartNew();

                App.Log?.LogInformation("Intentando login para: {email}", email);

                // MODO DESARROLLO: Si el email es "dev" permite entrar sin API
                if (email.Equals("dev", StringComparison.OrdinalIgnoreCase))
                {
                    App.Log?.LogWarning("⚠️ MODO DESARROLLO activado - Navegando sin validación");
                    ShowMessage("MODO DESARROLLO - Acceso directo", MessageType.Warning);
                    await Task.Delay(500);
                    
                    if (App.MainWindowInstance?.Navigator != null)
                    {
                        App.MainWindowInstance.Navigator.Navigate(typeof(DiarioPage));
                        App.Log?.LogInformation("Navegación a DiarioPage en modo DEV ✅");
                    }
                    return;
                }

                // Llamada real al API
                ApiClient.LoginResponse? res = null;
                
                try
                {
                    res = await App.Api.LoginAsync(email, pass);
                }
                catch (ApiException apiEx)
                {
                    sw.Stop();
                    
                    // Error de API con mensaje del servidor
                    App.Log?.LogError(apiEx, "Error de API: {statusCode} - {message}", apiEx.StatusCode, apiEx.Message);
                    ShowMessage(apiEx.Message, MessageType.Error);
                    SetBusy(false, "");
                    return;
                }
                catch (HttpRequestException httpEx)
                {
                    sw.Stop();
                    
                    // Error de conexión HTTP específico
                    var errorMsg = GetHttpErrorMessage(httpEx);
                    App.Log?.LogError(httpEx, "Error de conexión HTTP: {msg}", errorMsg);
                    ShowMessage(errorMsg, MessageType.Error);
                    SetBusy(false, "");
                    return;
                }
                catch (TaskCanceledException)
                {
                    sw.Stop();
                    
                    // Timeout
                    App.Log?.LogError("Timeout al conectar con el servidor");
                    ShowMessage("Timeout: El servidor no responde. Verifica tu conexión.", MessageType.Error);
                    SetBusy(false, "");
                    return;
                }
               
                sw.Stop();

                App.Log?.LogInformation("Respuesta de login recibida en {ms}ms. Res: {res}, Token: {hasToken}", 
                    sw.ElapsedMilliseconds, 
                    res != null, 
                    !string.IsNullOrEmpty(App.Api.AccessToken));

                // Validar que el login fue exitoso (ya sea con token o con cookies)
                if (res == null)
                {
                    ShowMessage("Login fallido. Verifica tus credenciales.", MessageType.Error);
                    SetBusy(false, "");
                    return;
                }

                // ✅ VERIFICAR SI REQUIERE CAMBIO DE CONTRASEÑA
                if (res.Message != null && res.Message.Equals("password_change_required", StringComparison.OrdinalIgnoreCase))
                {
                    App.Log?.LogInformation("Usuario {email} debe cambiar contraseña - Expired: {expired}, Days: {days}", 
                        email, res.PasswordExpired, res.DaysUntilExpiration);
                    
                    SetBusy(false, "");
                    
                    // Mostrar diálogo de cambio de contraseña
                    await ShowChangePasswordDialog(email, res.PasswordExpired, res.DaysUntilExpiration);
                    return;
                }

                // Si la respuesta tiene un mensaje de error explícito
                if (res.Message != null && !res.Message.Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    ShowMessage($"Error: {res.Message}", MessageType.Error);
                    SetBusy(false, "");
                    return;
                }

                SaveRememberedEmail();
                
                // Guardar información del usuario
                try
                {
                    var settings = ApplicationData.Current.LocalSettings.Values;
                    
                    // Usar propiedades seguras de LoginResponse
                    var userName = res.UserNameSafe;
                    var userEmail = res.UserEmailSafe;
                    var userRole = res.UserRoleSafe;
                    

                    // Si el backend NO devuelve los datos en el login, intentar obtenerlos de /api/v1/users/me
                    if (string.IsNullOrEmpty(res.UserName) || string.IsNullOrEmpty(res.UserRole))
                    {
                        App.Log?.LogInformation("🔄 LoginResponse no incluye userName/userRole, intentando obtener de /api/v1/users/me...");
                        
                        try
                        {
                            var userInfo = await App.Api.GetAsync<UserInfoResponse>("/api/v1/users/me");
                            
                            if (userInfo != null)
                            {
                                userName = userInfo.Name ?? userName;
                                userEmail = userInfo.Email ?? email;
                                userRole = userInfo.Role ?? userRole;
                                
                                App.Log?.LogInformation("✅ Información de usuario obtenida de /api/v1/users/me");
                                App.Log?.LogInformation("   • Name: {name}", userInfo.Name);
                                App.Log?.LogInformation("   • Email: {email}", userInfo.Email);
                                App.Log?.LogInformation("   • Role: {role}", userInfo.Role);
                            }
                        }
                        catch (Exception userInfoEx)
                        {
                            App.Log?.LogWarning(userInfoEx, "⚠️ No se pudo obtener info de usuario desde /api/v1/users/me, usando defaults");
                        }
                    }
                    
                    settings["UserName"] = userName;
                    settings["UserEmail"] = userEmail;
                    settings["UserRole"] = userRole;
                    
                    App.Log?.LogInformation("📝 Guardando información de usuario:");
                    App.Log?.LogInformation("   • UserName (API): {apiName} → Guardado: {name}", res.UserName ?? "(null)", userName);
                    App.Log?.LogInformation("   • UserEmail (API): {apiEmail} → Guardado: {email}", res.UserEmail ?? "(null)", userEmail);
                    App.Log?.LogInformation("   • UserRole (API): {apiRole} → Guardado: {role}", res.UserRole ?? "(null)", userRole);
                }
                catch (Exception ex)
                {
                    App.Log?.LogWarning(ex, "Error guardando información de usuario");
                }

                ShowMessage($"Inicio de sesión exitoso ({sw.ElapsedMilliseconds}ms)", MessageType.Success);

                // Pequeña pausa para mostrar el mensaje de éxito
                await Task.Delay(500);

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
                    
                    // Esperar a que termine la animación
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
                    ShowMessage("Error interno: No se puede navegar.", MessageType.Error);
                    SetBusy(false, "");
                }
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Login error inesperado");
                
                var errorMsg = GetFriendlyErrorMessage(ex);
                ShowMessage(errorMsg, MessageType.Error);
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
            
            // Detectar tipos comunes de errores de conexión
            if (innerMsg.Contains("No such host is known") || 
                innerMsg.Contains("nodename nor servname provided"))
            {
                return "No se puede conectar: Servidor no encontrado. Verifica la URL del API.";
            }
            
            if (innerMsg.Contains("Connection refused") ||
                innerMsg.Contains("actively refused"))
            {
                return "Conexión rechazada: El servidor no está disponible.";
            }
            
            if (innerMsg.Contains("Connection timed out") ||
                innerMsg.Contains("timeout"))
            {
                return "Timeout: El servidor no responde a tiempo.";
            }
            
            if (innerMsg.Contains("401"))
            {
                return "Credenciales incorrectas (401 No autorizado).";
            }
            
            if (innerMsg.Contains("403"))
            {
                return "Acceso denegado (403 Prohibido).";
            }
            
            if (innerMsg.Contains("404"))
            {
                return "Endpoint no encontrado (404). Verifica la configuración del API.";
            }
            
            if (innerMsg.Contains("500") || innerMsg.Contains("Internal Server Error"))
            {
                return "Error del servidor (500). Contacta al administrador.";
            }
            
            if (innerMsg.Contains("502") || innerMsg.Contains("Bad Gateway"))
            {
                return "Error de gateway (502). El servidor no está accesible.";
            }
            
            if (innerMsg.Contains("503") || innerMsg.Contains("Service Unavailable"))
            {
                return "Servicio no disponible (503). El servidor está temporalmente fuera de línea.";
            }
            
            // Error genérico
            return $"Error de conexión: {innerMsg}";
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
                return "Operación cancelada o timeout.";
            }
            
            if (ex is System.Net.Sockets.SocketException)
            {
                return "Error de red: No se puede establecer conexión.";
            }
            
            return $"Error: {ex.Message}";
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

        private enum MessageType
        {
            Success,
            Error,
            Warning,
            Info
        }

        private void ShowMessage(string text, MessageType type)
        {
            MsgBox.Visibility = Visibility.Visible;
            LblMsg.Text = text;

            // Configurar colores e iconos según el tipo de mensaje
            switch (type)
            {
                case MessageType.Success:
                    MsgBox.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 252, 231)); // Verde claro
                    MsgBox.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94)); // Verde
                    LblMsg.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 21, 128, 61)); // Verde oscuro
                    MsgIcon.Glyph = "\uE73E"; // CheckMark
                    MsgIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94));
                    break;
                
                case MessageType.Error:
                    MsgBox.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 226, 226)); // Rojo claro
                    MsgBox.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38)); // Rojo
                    LblMsg.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 127, 29, 29)); // Rojo oscuro
                    MsgIcon.Glyph = "\uE783"; // Error/Warning
                    MsgIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38));
                    break;
                
                case MessageType.Warning:
                    MsgBox.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 243, 199)); // Amarillo claro
                    MsgBox.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amarillo
                    LblMsg.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 146, 64, 14)); // Marrón
                    MsgIcon.Glyph = "\uE7BA"; // Info
                    MsgIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11));
                    break;
                
                case MessageType.Info:
                    MsgBox.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 224, 242, 254)); // Azul claro
                    MsgBox.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)); // Azul
                    LblMsg.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 64, 175)); // Azul oscuro
                    MsgIcon.Glyph = "\uE946"; // Info icon
                    MsgIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246));
                    break;
            }
        }

        private void HideMessage()
        {
            MsgBox.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Cargar el tema guardado en configuración
        /// </summary>
        private void LoadSavedTheme()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                
                ElementTheme theme = ElementTheme.Dark; // Por defecto oscuro
                
                if (settings.TryGetValue("AppTheme", out var themeObj) && themeObj is string themeName)
                {
                    App.Log?.LogInformation("Cargando tema guardado: {theme}", themeName);
                    
                    theme = themeName switch
                    {
                        "Light" => ElementTheme.Light,
                        "Dark" => ElementTheme.Dark,
                        "Default" => ElementTheme.Default,
                        _ => ElementTheme.Dark
                    };
                }
                else
                {
                    App.Log?.LogInformation("No hay tema guardado, usando tema oscuro por defecto");
                    SaveTheme(ElementTheme.Dark);
                }
                
                SetTheme(theme);
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error cargando tema guardado");
                SetTheme(ElementTheme.Dark);
            }
        }

        /// <summary>
        /// Guardar el tema seleccionado
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
                    _ => "Dark"
                };
                
                settings["AppTheme"] = themeName;
                App.Log?.LogInformation("Tema guardado: {theme}", themeName);
            }
            catch (Exception ex)
            {
                App.Log?.LogWarning(ex, "Error guardando tema");
            }
        }

        /// <summary>
        /// Aplicar tema y actualizar checkmarks del menú
        /// </summary>
        private void SetTheme(ElementTheme theme)
        {
            this.RequestedTheme = theme;
            
            // Actualizar checkmarks del menú
            ThemeSystemItem.IsChecked = theme == ElementTheme.Default;
            ThemeLightItem.IsChecked = theme == ElementTheme.Light;
            ThemeDarkItem.IsChecked = theme == ElementTheme.Dark;
            
            SaveTheme(theme);
            
            var themeName = theme switch
            {
                ElementTheme.Default => "sistema",
                ElementTheme.Light => "claro",
                ElementTheme.Dark => "oscuro",
                _ => "desconocido"
            };
            
            App.Log?.LogInformation("Tema aplicado: {theme}", themeName);
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
        /// Mostrar diálogo para cambio de contraseña obligatorio
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
                        ShowMessage("Por favor, ingresa tu contraseña actual.", MessageType.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    {
                        ShowMessage("La nueva contraseña debe tener al menos 6 caracteres.", MessageType.Warning);
                        return;
                    }

                    if (newPassword != confirmPassword)
                    {
                        ShowMessage("Las contraseñas no coinciden.", MessageType.Warning);
                        return;
                    }

                    if (currentPassword == newPassword)
                    {
                        ShowMessage("La nueva contraseña debe ser diferente a la actual.", MessageType.Warning);
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
                ShowMessage("Error interno. Intenta nuevamente.", MessageType.Error);
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
                    ShowMessage("Contraseña cambiada exitosamente. Puedes hacer login con la nueva contraseña.", MessageType.Success);
                    
                    // Limpiar campos
                    TxtUser.Text = email;
                    TxtPass.Password = "";
                    TxtPassVisible.Text = "";
                }
                else
                {
                    var errorMessage = response?.Error ?? "Error desconocido al cambiar la contraseña";
                    App.Log?.LogWarning("Error al cambiar contraseña: {error}", errorMessage);
                    ShowMessage(errorMessage, MessageType.Error);
                    
                    // Volver a mostrar el diálogo si hubo error
                    await Task.Delay(2000);
                    await ShowChangePasswordDialog(email, false, 0);
                }
            }
            catch (Exception ex)
            {
                App.Log?.LogError(ex, "Excepción al cambiar contraseña");
                ShowMessage("Error de conexión. Verifica tu conexión a internet.", MessageType.Error);
                
                // Volver a mostrar el diálogo si hubo error de conexión
                await Task.Delay(2000);
                await ShowChangePasswordDialog(email, false, 0);
            }
            finally
            {
                SetBusy(false, "");
            }
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
