# ✅ SOLUCIÓN: Sistema de Heartbeat para Usuarios Online

## 🔍 PROBLEMA DETECTADO

Los usuarios NO aparecían como "online" porque:
- ❌ El frontend **nunca enviaba pings** al backend
- ❌ El campo `lastSeenAt` del usuario actual **nunca se actualizaba**
- ❌ El método `PingAsync()` existía pero **no se llamaba**

**Resultado:** Todos los usuarios aparecían como offline, incluso el que está usando la aplicación.

---

## 💡 SOLUCIÓN IMPLEMENTADA

### **1. Nuevo Servicio: `PresenceHeartbeatService`**

Creado servicio singleton que envía pings automáticos cada 60 segundos.

**Archivo:** `Services/Presence/PresenceHeartbeatService.cs`

```csharp
public sealed class PresenceHeartbeatService : IDisposable
{
    private DispatcherQueueTimer? _heartbeatTimer;
    
    public void Start(DispatcherQueue dispatcher)
    {
        _heartbeatTimer = dispatcher.CreateTimer();
        _heartbeatTimer.Interval = TimeSpan.FromSeconds(60);
        _heartbeatTimer.Tick += async (s, e) => await SendHeartbeatAsync();
        _heartbeatTimer.Start();
        
        // Ping inicial inmediato
        _ = SendHeartbeatAsync();
    }
    
    private async Task SendHeartbeatAsync()
    {
        await PresenceService.Instance.PingAsync(_cts!.Token);
    }
}
```

**Características:**
- ✅ Ping cada **60 segundos**
- ✅ Ping **inmediato** al iniciar
- ✅ Usa `DispatcherQueueTimer` (UI thread-safe)
- ✅ Singleton pattern
- ✅ Manejo de errores robusto

---

### **2. Integración en `App.xaml.cs`**

Agregada propiedad estática para acceso global:

```csharp
// App.xaml.cs - Línea ~51
public static Services.Presence.PresenceHeartbeatService PresenceHeartbeat 
    => Services.Presence.PresenceHeartbeatService.Instance;
```

---

### **3. Inicio Automático en Login**

Modificado `LoginPage.xaml.cs` para iniciar heartbeat después del login:

```csharp
// LoginPage.xaml.cs - Línea ~536
App.MainWindowInstance.Navigator.Navigate(typeof(DiarioPage));

// 🆕 NUEVO: Iniciar heartbeat
App.PresenceHeartbeat.Start(DispatcherQueue);
App.Log?.LogInformation("💓 Heartbeat de presencia iniciado");
```

**Flow:**
1. Usuario hace login exitosamente
2. Se navega a DiarioPage
3. **Se inicia heartbeat** (ping cada 60s)
4. Usuario aparece como "online" en la ventana de usuarios

---

### **4. Detención Automática en Logout**

Modificado `MainWindow.xaml.cs` para detener heartbeat en logout:

```csharp
// MainWindow.xaml.cs - PerformLogoutAsync() - Línea ~224
private async Task PerformLogoutAsync()
{
    // Detener docking y cerrar ventanas
    DetachUsersOnlineWindow();
    CloseUsersOnlineWindow();
    
    // 🆕 NUEVO: Detener heartbeat
    App.PresenceHeartbeat.Stop();
    App.Log?.LogInformation("✅ Heartbeat de presencia detenido");
    
    // Continuar con limpieza de sesión...
}
```

**Flow:**
1. Usuario hace logout (botón Salir o X)
2. Se confirma el logout
3. **Se detiene heartbeat** (deja de enviar pings)
4. Se limpia sesión y navega a LoginPage
5. Usuario aparece como "offline" después de 2 minutos

---

## 🔄 FLUJO COMPLETO

### **A) Login → Online**
```
1. Usuario ingresa credenciales
2. LoginAsync() obtiene token
3. Navegación a DiarioPage
4. ✅ Start() del heartbeat (ping cada 60s)
5. Backend actualiza last_seen_at
6. Usuario aparece ONLINE en ventana de usuarios
```

### **B) Durante la Sesión**
```
Cada 60 segundos:
1. Heartbeat envía GET /api/v1/admin/ping
2. Backend actualiza last_seen_at del usuario actual
3. Otros usuarios ven el estado actualizado
```

### **C) Logout → Offline**
```
1. Usuario hace clic en "Salir" o presiona X
2. Confirmación de cierre de sesión
3. ✅ Stop() del heartbeat (detiene pings)
4. Limpieza de sesión (token, caché, perfil)
5. Navegación a LoginPage
6. Después de 2 minutos, usuario aparece OFFLINE
```

---

## 📊 TIEMPOS CONFIGURADOS

| Componente | Intervalo | Descripción |
|-----------|-----------|-------------|
| **Heartbeat Ping** | 60 segundos | Actualiza `last_seen_at` del usuario actual |
| **Refresh Usuarios** | 15 segundos | Recarga lista de usuarios desde API |
| **Umbral Online** | 2 minutos | Si `last_seen_at` < 2 min → ONLINE |

**Resultado:** Usuario aparece como online mientras está usando la app.

---

## 🎯 ENDPOINT BACKEND REQUERIDO

### **GET /api/v1/admin/ping**

**Headers:**
```
Authorization: Bearer <token>
```

**Backend debe:**
1. Extraer email del token JWT
2. Buscar usuario en BD por email
3. Actualizar `last_seen_at = DateTime.UtcNow`
4. Guardar cambios en BD

**Respuesta:**
```json
{
  "message": "Ping registrado",
  "lastSeenAt": "2026-01-25T23:15:00Z"
}
```

---

## ✅ ARCHIVOS MODIFICADOS

### 1. **NUEVO: `Services/Presence/PresenceHeartbeatService.cs`**
- Servicio singleton de heartbeat
- Timer cada 60 segundos
- Manejo de errores robusto

### 2. **`App.xaml.cs`**
```csharp
// Línea ~51
public static Services.Presence.PresenceHeartbeatService PresenceHeartbeat 
    => Services.Presence.PresenceHeartbeatService.Instance;
```

### 3. **`Views/LoginPage.xaml.cs`**
```csharp
// Línea ~536 - Después de navegar a DiarioPage
App.PresenceHeartbeat.Start(DispatcherQueue);
```

### 4. **`MainWindow.xaml.cs`**
```csharp
// Línea ~224 - En PerformLogoutAsync()
App.PresenceHeartbeat.Stop();
```

---

## 🧪 VERIFICACIÓN

### **Paso 1: Compilar**
```powershell
dotnet build
```
✅ Compilación exitosa

### **Paso 2: Ejecutar**
```powershell
dotnet run
```

### **Paso 3: Hacer Login**
1. Ingresar credenciales
2. **Verificar en logs:**
   ```
   💓 Heartbeat de presencia iniciado
   📡 Enviando ping a GET /api/v1/admin/ping...
   💓 Heartbeat enviado correctamente
   ```

### **Paso 4: Abrir Ventana de Usuarios**
1. Ir a Diario → Botón "Usuarios Online"
2. **Verificar:** Tu propio usuario debe aparecer con badge verde "[ONLINE]"

### **Paso 5: Esperar 60 segundos**
1. Revisar logs cada minuto
2. **Verificar:** Debe aparecer `💓 Heartbeat enviado correctamente`

### **Paso 6: Hacer Logout**
1. Clic en "Salir"
2. **Verificar en logs:**
   ```
   💔 Heartbeat detenido
   ✅ Heartbeat de presencia detenido
   ```

---

## ⚠️ REQUISITO BACKEND

### **¿Endpoint `/api/v1/admin/ping` existe?**

**Probar manualmente:**
```powershell
$token = "<tu_token>"
Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/api/v1/admin/ping" `
    -Headers @{Authorization="Bearer $token"} `
    -Method Get
```

**Si devuelve 404:**
- ❌ El endpoint NO está implementado en backend
- ⚠️ Necesitas implementarlo siguiendo:
  ```
  C:\GestionTime\GestionTimeApi\docs\IMPLEMENTAR-PRESENCIA-BACKEND.md
  ```

**Si devuelve 200 OK:**
- ✅ El endpoint existe y funciona
- ✅ El sistema ya debería funcionar correctamente

---

## 🎯 RESULTADO ESPERADO

### **Cuando el usuario está logueado:**
- ✅ Heartbeat envía ping cada 60 segundos
- ✅ Backend actualiza `last_seen_at`
- ✅ Usuario aparece con badge verde "[ONLINE]"
- ✅ Otros usuarios ven el estado actualizado

### **Cuando el usuario hace logout:**
- ✅ Heartbeat se detiene
- ✅ `last_seen_at` deja de actualizarse
- ✅ Después de 2 minutos → Badge gris "[OFFLINE]"

---

## 📝 LOGS ESPERADOS

### **Login:**
```
[INFO] Usuario solicitó login para: psantos@global-retail.com
[INFO] Navegación a DiarioPage completada ✅
[INFO] 💓 Heartbeat de presencia iniciado
[DEBUG] 📡 Enviando ping a GET /api/v1/admin/ping...
[DEBUG] ✅ Ping enviado correctamente
```

### **Durante Sesión (cada 60s):**
```
[DEBUG] 💓 Heartbeat enviado correctamente
```

### **Logout:**
```
[INFO] Usuario solicitó logout desde botón Salir
[INFO] 🚪 LOGOUT - Limpiando sesión y datos
[INFO] ✅ Heartbeat de presencia detenido
[INFO] ✅ Token de autenticación eliminado
[INFO] ✅ LOGOUT COMPLETADO - Navegando al login
```

---

## 🐛 TROUBLESHOOTING

### **Problema: Usuario NO aparece como online**

**Verificar:**
1. ¿El heartbeat está corriendo?
   ```
   Buscar en logs: "💓 Heartbeat de presencia iniciado"
   ```

2. ¿Los pings se envían correctamente?
   ```
   Buscar en logs cada 60s: "💓 Heartbeat enviado correctamente"
   ```

3. ¿El endpoint existe en backend?
   ```powershell
   # Probar manualmente
   curl GET /api/v1/admin/ping -H "Authorization: Bearer <token>"
   ```

4. ¿El backend actualiza last_seen_at?
   ```sql
   SELECT email, last_seen_at FROM users WHERE email = 'tu_email';
   ```

---

### **Problema: Heartbeat se detiene solo**

**Verificar:**
1. ¿Hay errores en logs?
   ```
   Buscar: "❌ Error enviando heartbeat"
   ```

2. ¿El token expiró?
   ```
   Buscar: "⚠️ Token inválido o expirado"
   ```

3. ¿La app entró en suspensión?
   ```
   Windows puede pausar timers en apps inactivas
   ```

---

## 🚀 PRÓXIMO PASO

**Ahora debes:**
1. ✅ Ejecutar la aplicación
2. ✅ Hacer login
3. ✅ Verificar logs (ping cada 60s)
4. ✅ Abrir ventana de usuarios online
5. ✅ Confirmar que apareces con badge verde "[ONLINE]"

**Si no apareces como online:**
- Verificar si el endpoint `/api/v1/admin/ping` existe
- Revisar logs del backend para ver si se actualiza `last_seen_at`
- Implementar endpoint siguiendo documentación de backend

---

**Creado:** 2025-01-25  
**Proyecto:** GestionTime Desktop v1.5.0-beta  
**Estado:** ✅ Implementado y Compilado Exitosamente
