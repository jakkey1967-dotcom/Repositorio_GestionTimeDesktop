# 🔧 FIX: Logout debe actualizar presencia en el backend

**Fecha**: 2026-02-02  
**Versión Desktop**: v1.1.0  
**Backend**: GestionTimeApi  

---

## 📋 **PROBLEMA IDENTIFICADO**

El endpoint `/api/v1/auth/logout` del backend **NO actualiza la presencia del usuario**.

### ❌ **Comportamiento actual (INCORRECTO)**:

1. Usuario hace logout desde el Desktop
2. Se borran cookies y se revoca el refresh token
3. **PERO el usuario sigue apareciendo online** en `/presence/users`
4. Solo se marca offline después de 30 segundos (timeout)

### ✅ **Comportamiento esperado (CORRECTO)**:

1. Usuario hace logout desde el Desktop
2. Se borran cookies y se revoca el refresh token
3. **Se revocan todas las sesiones activas (UserSessions)**
4. **Usuario se marca offline INMEDIATAMENTE**

---

## 🔍 **DIAGNÓSTICO**

**Archivo afectado**: `C:\GestionTime\GestionTimeApi\Controllers\AuthController.cs`

**Método**: `Logout()`

**Línea aproximada**: ~150-180 (buscar `[HttpPost("logout")]`)

---

## 🛠️ **SOLUCIÓN: 3 CAMBIOS EN EL MÉTODO `Logout()`**

### ✅ **CAMBIO 1: Log mejorado del refresh token**

**ANTES**:
```csharp
logger.LogDebug("Refresh token revocado");
```

**DESPUÉS**:
```csharp
logger.LogDebug("Refresh token revocado: {TokenId}", token.Id);
```

---

### ✅ **CAMBIO 2: NUEVO - Revocar sesiones UserSessions** ⭐ **CRÍTICO**

**AÑADIR ESTE BLOQUE COMPLETO** después de revocar el refresh token y **ANTES** de borrar las cookies:

```csharp
// 2. Revocar TODAS las sesiones activas del usuario (marcar offline inmediatamente)
if (userId != null && Guid.TryParse(userId, out var userIdGuid))
{
    var activeSessions = await db.UserSessions
        .Where(s => s.UserId == userIdGuid && s.RevokedAt == null)
        .ToListAsync();
    
    foreach (var session in activeSessions)
    {
        session.RevokedAt = DateTime.UtcNow;
    }
    
    if (activeSessions.Any())
    {
        await db.SaveChangesAsync();
        logger.LogInformation("Revocadas {Count} sesiones activas del usuario {UserId}", activeSessions.Count, userIdGuid);
    }
}
```

**¿Qué hace este código?**
- Busca todas las sesiones activas del usuario (`RevokedAt == null`)
- Las marca como revocadas (`RevokedAt = DateTime.UtcNow`)
- Guarda los cambios en la base de datos
- **Resultado**: El usuario se marca offline inmediatamente

---

### ✅ **CAMBIO 3: Log mejorado final**

**ANTES**:
```csharp
logger.LogInformation("Logout completado");
```

**DESPUÉS**:
```csharp
logger.LogInformation("Logout completado para UserId: {UserId}", userId);
```

---

## 📄 **CÓDIGO COMPLETO DEL MÉTODO CORREGIDO**

Reemplaza el método `Logout()` completo en `AuthController.cs` con este código:

```csharp
[HttpPost("logout")]
public async Task<IActionResult> Logout()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    logger.LogInformation("Logout solicitado{UserInfo}", 
        userId != null ? $" por UserId: {userId}" : "");

    // 1. Revoca el refresh actual (si existe)
    if (Request.Cookies.TryGetValue("refresh_token", out var rawRefresh) && !string.IsNullOrWhiteSpace(rawRefresh))
    {
        var hash = RefreshTokenService.Hash(rawRefresh);

        var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash);
        if (token is not null && token.RevokedAt == null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            logger.LogDebug("Refresh token revocado: {TokenId}", token.Id);
        }
    }

    // 2. Revocar TODAS las sesiones activas del usuario (marcar offline inmediatamente)
    if (userId != null && Guid.TryParse(userId, out var userIdGuid))
    {
        var activeSessions = await db.UserSessions
            .Where(s => s.UserId == userIdGuid && s.RevokedAt == null)
            .ToListAsync();
        
        foreach (var session in activeSessions)
        {
            session.RevokedAt = DateTime.UtcNow;
        }
        
        if (activeSessions.Any())
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Revocadas {Count} sesiones activas del usuario {UserId}", activeSessions.Count, userIdGuid);
        }
    }

    // 3. Borrar cookies
    Response.Cookies.Delete("access_token");
    Response.Cookies.Delete("refresh_token", new CookieOptions
    {
        Path = "/api/v1/auth/refresh"
    });

    logger.LogInformation("Logout completado para UserId: {UserId}", userId);

    return Ok(new { message = "bye" });
}
```

---

## 🚀 **INSTRUCCIONES DE IMPLEMENTACIÓN**

### **PASO 1: Abrir el archivo**

```powershell
code C:\GestionTime\GestionTimeApi\Controllers\AuthController.cs
```

### **PASO 2: Localizar el método**

Buscar (Ctrl+F): `[HttpPost("logout")]`

### **PASO 3: Reemplazar el método completo**

1. Seleccionar desde `[HttpPost("logout")]` hasta el cierre de llaves `}`
2. Pegar el código corregido de arriba

### **PASO 4: Guardar y compilar**

```powershell
cd C:\GestionTime\GestionTimeApi
dotnet build
```

### **PASO 5: Reiniciar el backend**

```powershell
dotnet run
```

---

## ✅ **VERIFICACIÓN**

### **Prueba 1: Logout desde Desktop**

1. Inicia sesión en el Desktop con `wsanchez@global-retail.com`
2. Verifica que aparece online en Settings → Permisos y roles
3. **Haz logout** (Menú → Salir)
4. **Refresca inmediatamente** (botón 🔄)
5. **Resultado esperado**: Wilson Sánchez debe aparecer **OFFLINE inmediatamente**

### **Prueba 2: Diagnóstico con script**

```powershell
cd C:\GestionTime\GestionTimeDesktop
.\Scripts\Debug-PresenceSystem.ps1
```

**Resultado esperado**:
```
PASO 7: Verificando usuarios online DESPUES de 35s...
   Total usuarios: 5
   Usuarios ONLINE:
     - Francisco Santos: ADMIN (hace 0s)

===================================================================
SISTEMA FUNCIONANDO CORRECTAMENTE ✅
===================================================================

wsanchez se marco como OFFLINE correctamente
despues del timeout.
```

### **Prueba 3: Verificar logs del backend**

Buscar en los logs del backend:

```
[Information] Revocadas 1 sesiones activas del usuario 3e90c352-...
[Information] Logout completado para UserId: 3e90c352-...
```

Si ves estos mensajes, **el fix funciona correctamente**.

---

## 📊 **RESUMEN DE CAMBIOS**

| Cambio | Ubicación | Importancia | Qué hace |
|--------|-----------|-------------|----------|
| 1 | Log refresh | Menor | Añade ID del token revocado (debugging) |
| 2 | **Bloque nuevo** | **🔴 CRÍTICO** | **Revoca sesiones UserSessions → marca offline** |
| 3 | Log final | Menor | Añade UserId en el log (debugging) |

---

## 🔗 **ARCHIVOS RELACIONADOS**

### **Backend (GestionTimeApi)**:
- `Controllers/AuthController.cs` → Método `Logout()` **(ESTE ARCHIVO)**
- `Controllers/PresenceController.cs` → Ya corregido (timeout 30s) ✅
- `Middleware/PresenceMiddleware.cs` → Gestiona pings automáticos

### **Desktop (GestionTimeDesktop)**:
- `Views/SettingsWindow.xaml.cs` → Gestión de usuarios inline
- `Services/Presence/PresenceHeartbeatService.cs` → Envía pings cada 5s
- `Services/Presence/PresenceService.cs` → Consulta `/presence/users`

---

## 📚 **DOCUMENTACIÓN ADICIONAL**

- [SISTEMA_ROLES_USUARIOS.md](SISTEMA_ROLES_USUARIOS.md)
- [GESTION_USUARIOS_INLINE_SETTINGS.md](GESTION_USUARIOS_INLINE_SETTINGS.md)
- [Debug-PresenceSystem.ps1](../Scripts/Debug-PresenceSystem.ps1)

---

## 🎯 **ESTADO**

- [x] Problema identificado
- [x] Solución documentada
- [ ] Fix aplicado en backend
- [ ] Backend reiniciado
- [ ] Pruebas de verificación completadas

---

**Autor**: GitHub Copilot  
**Ticket**: Sistema de presencia no se actualiza al hacer logout  
**Prioridad**: 🔴 ALTA (afecta experiencia de usuario)
