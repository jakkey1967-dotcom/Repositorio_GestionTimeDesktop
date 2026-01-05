# 🔔 ACTIVAR NOTIFICACIONES EN LOGIN Y REGISTRO

**Fecha:** 2025-01-20  
**Versión:** 1.0  
**Archivos:** LoginPage.xaml.cs, RegisterPage.xaml.cs (futuro)

---

## 📋 **ÍNDICE**

1. [LoginPage - Notificaciones](#loginpage-notificaciones)
2. [RegisterPage - Notificaciones](#registerpage-notificaciones)
3. [Escenarios Cubiertos](#escenarios-cubiertos)
4. [Código Completo](#código-completo)

---

## 🔐 **LOGINPAGE - NOTIFICACIONES**

### **Escenario 1: Validación - Campos Vacíos**

**Ubicación:** `OnLoginClick()`, línea ~180

**REEMPLAZAR:**
```csharp
if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
{
    ShowMessage("Por favor, rellena correo y contraseña.", MessageType.Warning);
    return;
}
```

**POR:**
```csharp
if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
{
    // 🔔 Notificación: Campos vacíos
    App.Notifications?.ShowWarning(
        "Por favor, rellena correo y contraseña.",
        title: "Campos Requeridos"
    );
    return;
}
```

---

### **Escenario 2: Error de API (Credenciales Incorrectas)**

**Ubicación:** `OnLoginClick()`, línea ~210

**REEMPLAZAR:**
```csharp
catch (ApiException apiEx)
{
    sw.Stop();
    
    App.Log?.LogError(apiEx, "Error de API: {statusCode} - {message}", apiEx.StatusCode, apiEx.Message);
    ShowMessage(apiEx.Message, MessageType.Error);
    SetBusy(false, "");
    return;
}
```

**POR:**
```csharp
catch (ApiException apiEx)
{
    sw.Stop();
    
    App.Log?.LogError(apiEx, "Error de API: {statusCode} - {message}", apiEx.StatusCode, apiEx.Message);
    
    // 🔔 Notificación: Error de API con detalles
    App.Notifications?.ShowError(
        apiEx.Message,
        title: $"Error {(int)apiEx.StatusCode}",
        exception: apiEx
    );
    
    SetBusy(false, "");
    return;
}
```

---

### **Escenario 3: Error de Conexión HTTP**

**Ubicación:** `OnLoginClick()`, línea ~220

**REEMPLAZAR:**
```csharp
catch (HttpRequestException httpEx)
{
    sw.Stop();
    
    var errorMsg = GetHttpErrorMessage(httpEx);
    App.Log?.LogError(httpEx, "Error de conexión HTTP: {msg}", errorMsg);
    ShowMessage(errorMsg, MessageType.Error);
    SetBusy(false, "");
    return;
}
```

**POR:**
```csharp
catch (HttpRequestException httpEx)
{
    sw.Stop();
    
    var errorMsg = GetHttpErrorMessage(httpEx);
    App.Log?.LogError(httpEx, "Error de conexión HTTP: {msg}", errorMsg);
    
    // 🔔 Notificación: Error de conexión
    App.Notifications?.ShowError(
        errorMsg,
        title: "Error de Conexión",
        exception: httpEx
    );
    
    SetBusy(false, "");
    return;
}
```

---

### **Escenario 4: Timeout**

**Ubicación:** `OnLoginClick()`, línea ~230

**REEMPLAZAR:**
```csharp
catch (TaskCanceledException)
{
    sw.Stop();
    
    App.Log?.LogError("Timeout al conectar con el servidor");
    ShowMessage("Timeout: El servidor no responde. Verifica tu conexión.", MessageType.Error);
    SetBusy(false, "");
    return;
}
```

**POR:**
```csharp
catch (TaskCanceledException)
{
    sw.Stop();
    
    App.Log?.LogError("Timeout al conectar con el servidor");
    
    // 🔔 Notificación: Timeout
    App.Notifications?.ShowWarning(
        "El servidor no responde. Verifica tu conexión a internet.",
        title: "Tiempo de Espera Agotado",
        options: new NotificationOptions
        {
            DurationMs = 6000  // 6 segundos para timeout
        }
    );
    
    SetBusy(false, "");
    return;
}
```

---

### **Escenario 5: Login Fallido (Respuesta Null)**

**Ubicación:** `OnLoginClick()`, línea ~250

**REEMPLAZAR:**
```csharp
if (res == null)
{
    ShowMessage("Login fallido. Verifica tus credenciales.", MessageType.Error);
    SetBusy(false, "");
    return;
}
```

**POR:**
```csharp
if (res == null)
{
    // 🔔 Notificación: Login fallido
    App.Notifications?.ShowError(
        "Verifica tu email y contraseña.",
        title: "Login Fallido"
    );
    
    SetBusy(false, "");
    return;
}
```

---

### **Escenario 6: Cambio de Contraseña Requerido**

**Ubicación:** `OnLoginClick()`, línea ~260

**AGREGAR DESPUÉS DEL LOG:**
```csharp
if (res.Message != null && res.Message.Equals("password_change_required", StringComparison.OrdinalIgnoreCase))
{
    App.Log?.LogInformation("Usuario {email} debe cambiar contraseña...", email);
    
    // 🔔 Notificación: Cambio de contraseña requerido
    App.Notifications?.ShowWarning(
        res.PasswordExpired 
            ? "Tu contraseña ha expirado. Debes cambiarla para continuar."
            : $"Tu contraseña expira en {res.DaysUntilExpiration} días.",
        title: "Cambio de Contraseña Requerido",
        options: new NotificationOptions
        {
            DurationMs = 0  // Persistente hasta que cierre el diálogo
        }
    );
    
    SetBusy(false, "");
    await ShowChangePasswordDialog(email, res.PasswordExpired, res.DaysUntilExpiration);
    return;
}
```

---

### **Escenario 7: Error del Servidor**

**Ubicación:** `OnLoginClick()`, línea ~270

**REEMPLAZAR:**
```csharp
if (res.Message != null && !res.Message.Equals("ok", StringComparison.OrdinalIgnoreCase))
{
    ShowMessage($"Error: {res.Message}", MessageType.Error);
    SetBusy(false, "");
    return;
}
```

**POR:**
```csharp
if (res.Message != null && !res.Message.Equals("ok", StringComparison.OrdinalIgnoreCase))
{
    // 🔔 Notificación: Error del servidor
    App.Notifications?.ShowError(
        res.Message,
        title: "Error del Servidor"
    );
    
    SetBusy(false, "");
    return;
}
```

---

### **Escenario 8: Login Exitoso** ✅

**Ubicación:** `OnLoginClick()`, línea ~350

**REEMPLAZAR:**
```csharp
ShowMessage($"Inicio de sesión exitoso ({sw.ElapsedMilliseconds}ms)", MessageType.Success);
```

**POR:**
```csharp
// 🔔 Notificación: Login exitoso
App.Notifications?.ShowSuccess(
    $"Bienvenido, {userName}",
    title: "Login Exitoso",
    options: new NotificationOptions
    {
        DurationMs = 2000  // 2 segundos
    }
);
```

---

### **Escenario 9: Error Inesperado**

**Ubicación:** `OnLoginClick()`, catch final, línea ~390

**REEMPLAZAR:**
```csharp
catch (Exception ex)
{
    App.Log?.LogError(ex, "Login error inesperado");
    
    var errorMsg = GetFriendlyErrorMessage(ex);
    ShowMessage(errorMsg, MessageType.Error);
}
```

**POR:**
```csharp
catch (Exception ex)
{
    App.Log?.LogError(ex, "Login error inesperado");
    
    var errorMsg = GetFriendlyErrorMessage(ex);
    
    // 🔔 Notificación: Error inesperado con opción de ver detalles
    App.Notifications?.ShowError(
        errorMsg,
        title: "Error Inesperado",
        exception: ex,
        options: new NotificationOptions
        {
            Actions = new List<NotificationAction>
            {
                new()
                {
                    Label = "Ver Log",
                    OnClick = async () =>
                    {
                        try
                        {
                            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
                            if (File.Exists(logPath))
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = logPath,
                                    UseShellExecute = true
                                });
                            }
                        }
                        catch { }
                    },
                    CloseOnClick = true
                }
            }
        }
    );
}
```

---

### **Escenario 10: Cambio de Contraseña Exitoso**

**Ubicación:** `PerformPasswordChange()`, línea ~700

**REEMPLAZAR:**
```csharp
if (response?.Success == true)
{
    App.Log?.LogInformation("Contraseña cambiada exitosamente para: {email}", email);
    ShowMessage("Contraseña cambiada exitosamente. Puedes hacer login con la nueva contraseña.", MessageType.Success);
    
    // Limpiar campos
    TxtUser.Text = email;
    TxtPass.Password = "";
    TxtPassVisible.Text = "";
}
```

**POR:**
```csharp
if (response?.Success == true)
{
    App.Log?.LogInformation("Contraseña cambiada exitosamente para: {email}", email);
    
    // 🔔 Notificación: Cambio de contraseña exitoso
    App.Notifications?.ShowSuccess(
        "Puedes hacer login con tu nueva contraseña.",
        title: "Contraseña Actualizada",
        options: new NotificationOptions
        {
            DurationMs = 5000  // 5 segundos
        }
    );
    
    // Limpiar campos
    TxtUser.Text = email;
    TxtPass.Password = "";
    TxtPassVisible.Text = "";
}
```

---

### **Escenario 11: Error al Cambiar Contraseña**

**Ubicación:** `PerformPasswordChange()`, línea ~715

**REEMPLAZAR:**
```csharp
else
{
    var errorMessage = response?.Error ?? "Error desconocido al cambiar la contraseña";
    App.Log?.LogWarning("Error al cambiar contraseña: {error}", errorMessage);
    ShowMessage(errorMessage, MessageType.Error);
    
    // Volver a mostrar el diálogo si hubo error
    await Task.Delay(2000);
    await ShowChangePasswordDialog(email, false, 0);
}
```

**POR:**
```csharp
else
{
    var errorMessage = response?.Error ?? "Error desconocido al cambiar la contraseña";
    App.Log?.LogWarning("Error al cambiar contraseña: {error}", errorMessage);
    
    // 🔔 Notificación: Error al cambiar contraseña
    App.Notifications?.ShowError(
        errorMessage,
        title: "Error al Cambiar Contraseña",
        options: new NotificationOptions
        {
            DurationMs = 0,  // Persistente
            Actions = new List<NotificationAction>
            {
                new()
                {
                    Label = "Reintentar",
                    OnClick = async () =>
                    {
                        await ShowChangePasswordDialog(email, false, 0);
                    },
                    CloseOnClick = true
                }
            }
        }
    );
}
```

---

## 📝 **REGISTERPAGE - NOTIFICACIONES** (Futuro)

### **Escenario 1: Validación - Email Inválido**

```csharp
if (!IsValidEmail(email))
{
    // 🔔 Notificación: Email inválido
    App.Notifications?.ShowWarning(
        "Por favor, ingresa un email válido.",
        title: "Email Inválido"
    );
    return;
}
```

---

### **Escenario 2: Validación - Contraseña Débil**

```csharp
if (password.Length < 6)
{
    // 🔔 Notificación: Contraseña débil
    App.Notifications?.ShowWarning(
        "La contraseña debe tener al menos 6 caracteres.",
        title: "Contraseña Débil"
    );
    return;
}
```

---

### **Escenario 3: Registro Exitoso**

```csharp
if (registerResponse?.Success == true)
{
    // 🔔 Notificación: Registro exitoso
    App.Notifications?.ShowSuccess(
        "Revisa tu email para activar tu cuenta.",
        title: "Registro Exitoso",
        options: new NotificationOptions
        {
            DurationMs = 6000  // 6 segundos
        }
    );
    
    // Navegar a verificación
    NavigateToVerificationPage(email);
}
```

---

### **Escenario 4: Email Ya Registrado**

```csharp
catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.Conflict)
{
    // 🔔 Notificación: Email ya existe
    App.Notifications?.ShowWarning(
        "Este email ya está registrado. ¿Olvidaste tu contraseña?",
        title: "Email Ya Registrado",
        options: new NotificationOptions
        {
            Actions = new List<NotificationAction>
            {
                new()
                {
                    Label = "Recuperar Contraseña",
                    OnClick = async () =>
                    {
                        NavigateToForgotPassword();
                    },
                    CloseOnClick = true
                }
            }
        }
    );
}
```

---

## 📊 **ESCENARIOS CUBIERTOS**

### **LoginPage:**

| # | Escenario | Tipo | Duración | Acciones |
|---|-----------|------|----------|----------|
| 1 | Campos vacíos | ⚠️ Warning | 4s | - |
| 2 | Error API | ❌ Error | 4s | - |
| 3 | Error conexión | ❌ Error | 4s | - |
| 4 | Timeout | ⚠️ Warning | 6s | - |
| 5 | Login fallido | ❌ Error | 4s | - |
| 6 | Cambio contraseña requerido | ⚠️ Warning | Persistente | - |
| 7 | Error del servidor | ❌ Error | 4s | - |
| 8 | **Login exitoso** ✅ | ✅ Success | 2s | - |
| 9 | Error inesperado | ❌ Error | 4s | Ver Log |
| 10 | Contraseña cambiada | ✅ Success | 5s | - |
| 11 | Error cambio contraseña | ❌ Error | Persistente | Reintentar |

### **RegisterPage (Futuro):**

| # | Escenario | Tipo | Duración | Acciones |
|---|-----------|------|----------|----------|
| 1 | Email inválido | ⚠️ Warning | 4s | - |
| 2 | Contraseña débil | ⚠️ Warning | 4s | - |
| 3 | **Registro exitoso** ✅ | ✅ Success | 6s | - |
| 4 | Email ya registrado | ⚠️ Warning | 0s | Recuperar Contraseña |

---

## 🎨 **CONSIDERACIONES DE UX**

### **1. Duración de Notificaciones:**

- ✅ **Success:** 2-3 segundos (rápido, usuario ya está navegando)
- ⚠️ **Warning:** 4-6 segundos (dar tiempo a leer)
- ❌ **Error:** 4 segundos o persistente (si requiere acción)
- ℹ️ **Info:** 4 segundos

### **2. Acciones en Notificaciones:**

Solo agregar acciones cuando:
- ✅ **Haya algo útil que el usuario pueda hacer** (Reintentar, Ver Log, etc.)
- ✅ **No sea obvio qué hacer después** (ej: email ya registrado → ofrecer recuperar contraseña)
- ❌ **NO agregar acciones innecesarias** (ej: en un login exitoso)

### **3. Compatibilidad con ShowMessage:**

Si quieres **mantener** el sistema de mensajes actual (`ShowMessage`) **Y** agregar notificaciones:

```csharp
// Opción 1: Solo notificaciones (recomendado)
App.Notifications?.ShowSuccess("Login exitoso");

// Opción 2: Ambos sistemas (transición gradual)
ShowMessage("Login exitoso", MessageType.Success);  // Banner interno
App.Notifications?.ShowSuccess("Login exitoso");     // Notificación flotante

// Opción 3: Usar notificaciones solo en errores críticos
if (esErrorCritico)
{
    App.Notifications?.ShowError(...);  // Para errores que requieren atención
}
else
{
    ShowMessage(...);  // Para feedback rápido
}
```

---

## ✅ **CHECKLIST DE IMPLEMENTACIÓN**

### **Paso 1: Agregar using**

Al inicio de `LoginPage.xaml.cs`:

```csharp
using GestionTime.Desktop.Services.Notifications;
```

### **Paso 2: Reemplazar llamadas ShowMessage**

- [ ] Escenario 1: Campos vacíos (línea ~180)
- [ ] Escenario 2: Error API (línea ~210)
- [ ] Escenario 3: Error HTTP (línea ~220)
- [ ] Escenario 4: Timeout (línea ~230)
- [ ] Escenario 5: Login fallido (línea ~250)
- [ ] Escenario 6: Cambio contraseña (línea ~260)
- [ ] Escenario 7: Error servidor (línea ~270)
- [ ] Escenario 8: Login exitoso (línea ~350)
- [ ] Escenario 9: Error inesperado (línea ~390)
- [ ] Escenario 10: Contraseña cambiada (línea ~700)
- [ ] Escenario 11: Error cambio (línea ~715)

### **Paso 3: Compilar y Probar**

```bash
Build > Rebuild Solution
```

### **Paso 4: Test de Usuario**

1. **Login exitoso:** Ver notificación verde ✅
2. **Credenciales incorrectas:** Ver notificación roja ❌
3. **Campos vacíos:** Ver notificación naranja ⚠️
4. **Cambio de contraseña:** Ver notificación naranja persistente ⚠️

---

## 📝 **NOTAS FINALES**

### **¿Qué pasa con ShowMessage?**

Puedes:

1. **Eliminar completamente** `ShowMessage()` y usar solo notificaciones
2. **Mantener ambos** durante transición
3. **Ocultar el banner** interno y mostrar solo notificaciones:

```csharp
private void ShowMessage(string text, MessageType type)
{
    // 🔥 NUEVO: Delegar a notificaciones
    switch (type)
    {
        case MessageType.Success:
            App.Notifications?.ShowSuccess(text);
            break;
        case MessageType.Error:
            App.Notifications?.ShowError(text);
            break;
        case MessageType.Warning:
            App.Notifications?.ShowWarning(text);
            break;
        case MessageType.Info:
            App.Notifications?.ShowInfo(text);
            break;
    }
    
    // Opcional: Mantener banner interno también
    // MsgBox.Visibility = Visibility.Visible;
    // LblMsg.Text = text;
    // ...
}
```

---

**Documento creado:** 2025-01-20 23:55 UTC  
**Última actualización:** 2025-01-20 23:55 UTC  
**Versión:** 1.0.0  
**Estado:** ✅ Guía completa lista para implementar

