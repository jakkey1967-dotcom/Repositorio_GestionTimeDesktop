# 🔧 FIX: Endpoint /health debe actualizar presencia automáticamente

**Fecha**: 2026-02-02  
**Versión Desktop**: v1.1.0  
**Backend**: GestionTimeApi  

---

## 📋 **PROBLEMA IDENTIFICADO**

El sistema de presencia requiere que usuarios ADMIN llamen periódicamente a `/api/v1/admin/ping` para mantenerse online.

### ❌ **Situación actual**:

1. DiarioPage llama a `/api/v1/health` cada X segundos (health check)
2. `/health` solo devuelve `{"status":"ok"}` sin hacer nada más
3. Usuarios USER/EDITOR NO pueden llamar a `/admin/ping` (403)
4. **Presencia no se actualiza automáticamente para todos los usuarios**

### ✅ **Solución propuesta**:

1. DiarioPage sigue llamando a `/api/v1/health` (sin cambios)
2. `/health` actualiza automáticamente `LastSeenAt` del usuario autenticado
3. **Todos los roles** se benefician (USER, EDITOR, ADMIN)
4. **Cero cambios en el Desktop** (backward compatible)

---

## 💡 **VENTAJAS DE ESTA SOLUCIÓN**

| Ventaja | Descripción |
|---------|-------------|
| ✅ **Cero cambios en Desktop** | Ya llama a `/health`, no requiere modificaciones |
| ✅ **Universal** | Funciona para USER, EDITOR y ADMIN |
| ✅ **Backward compatible** | Respuesta sigue siendo `{"status":"ok"}` |
| ✅ **Eficiente** | Una sola petición actualiza presencia y health check |
| ✅ **Transparente** | Side effect interno, no afecta API pública |

---

## 🔍 **DIAGNÓSTICO**

**Archivo afectado**: `C:\GestionTime\GestionTimeApi\Controllers\HealthController.cs`

**Método**: `Get()`

**Línea aproximada**: ~20-30 (buscar `[HttpGet]` dentro de `HealthController`)

---

## 🛠️ **SOLUCIÓN: MODIFICAR EL ENDPOINT `/health`**

### 📝 **ANTES** (solo health check):

```csharp
[HttpGet]
public IActionResult Get()
{
    return Ok(new { status = "ok" });
}
```

### ✅ **DESPUÉS** (health check + actualización de presencia):

```csharp
[HttpGet]
public async Task<IActionResult> Get()
{
    // 1. Si hay usuario autenticado, actualizar presencia automáticamente
    var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId != null && Guid.TryParse(userId, out var userIdGuid))
    {
        try
        {
            var session = await _db.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.RevokedAt == null);
            
            if (session != null)
            {
                session.LastSeenAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogDebug("Presencia actualizada para UserId: {UserId} via /health", userIdGuid);
            }
        }
        catch (Exception ex)
        {
            // No fallar el health check si la actualización de presencia falla
            _logger.LogWarning(ex, "Error actualizando presencia en /health para UserId: {UserId}", userIdGuid);
        }
    }
    
    // 2. Devolver siempre la misma respuesta (backward compatible)
    return Ok(new { status = "ok" });
}
```

---

## 📄 **CÓDIGO COMPLETO DEL CONTROLADOR**

Necesitas asegurarte de que el `HealthController` tenga acceso a `_db` y `_logger`:

```csharp
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionTime.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly GestionTimeDbContext _db;
    private readonly ILogger<HealthController> _logger;

    public HealthController(GestionTimeDbContext db, ILogger<HealthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Health check del backend + actualización automática de presencia
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // 1. Si hay usuario autenticado, actualizar presencia automáticamente
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null && Guid.TryParse(userId, out var userIdGuid))
        {
            try
            {
                var session = await _db.UserSessions
                    .FirstOrDefaultAsync(s => s.UserId == userIdGuid && s.RevokedAt == null);
                
                if (session != null)
                {
                    session.LastSeenAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    _logger.LogDebug("Presencia actualizada para UserId: {UserId} via /health", userIdGuid);
                }
            }
            catch (Exception ex)
            {
                // No fallar el health check si la actualización de presencia falla
                _logger.LogWarning(ex, "Error actualizando presencia en /health para UserId: {UserId}", userIdGuid);
            }
        }
        
        // 2. Devolver siempre la misma respuesta (backward compatible)
        return Ok(new { status = "ok" });
    }
}
```

---

## 🎯 **¿QUÉ CAMBIA EN LA RESPUESTA?**

### 🔍 **NADA** - La respuesta sigue siendo:

```json
{
  "status": "ok"
}
```

### ✅ **Pero INTERNAMENTE**:

1. Se actualiza `session.LastSeenAt = DateTime.UtcNow` en la base de datos
2. Usuario aparece online en `/presence/users`
3. **El Desktop no se entera** (backward compatible)

---

## 🚀 **INSTRUCCIONES DE IMPLEMENTACIÓN**

### **PASO 1: Abrir el archivo**

```powershell
code C:\GestionTime\GestionTimeApi\Controllers\HealthController.cs
```

### **PASO 2: Verificar dependencias**

Asegúrate de que el archivo tenga estos `using`:

```csharp
using GestionTime.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
```

### **PASO 3: Añadir inyección de dependencias**

Si el constructor NO tiene `_db` y `_logger`, añadirlos:

```csharp
private readonly GestionTimeDbContext _db;
private readonly ILogger<HealthController> _logger;

public HealthController(GestionTimeDbContext db, ILogger<HealthController> logger)
{
    _db = db;
    _logger = logger;
}
```

### **PASO 4: Reemplazar el método `Get()`**

Reemplazar desde `[HttpGet]` hasta el cierre de llaves con el código nuevo.

### **PASO 5: Guardar y compilar**

```powershell
cd C:\GestionTime\GestionTimeApi
dotnet build
```

### **PASO 6: Reiniciar el backend**

```powershell
dotnet run
```

---

## ✅ **VERIFICACIÓN**

### **Prueba 1: Health check funciona igual**

```powershell
curl https://localhost:2502/api/v1/health
```

**Resultado esperado**:
```json
{"status":"ok"}
```

### **Prueba 2: Presencia se actualiza automáticamente**

1. Inicia sesión en Desktop con `wsanchez@global-retail.com`
2. **Espera 10 segundos** (DiarioPage llama a `/health` periódicamente)
3. Consulta presencia desde otro terminal:

```powershell
curl https://localhost:2502/api/v1/presence/users -H "Authorization: Bearer TOKEN"
```

**Resultado esperado**:
```json
[
  {
    "userId": "3e90c352-...",
    "fullName": "Wilson Sánchez",
    "lastSeenAt": "2026-02-02T21:05:33Z",  // ← Actualizado hace < 10 segundos
    "isOnline": true
  }
]
```

### **Prueba 3: Verificar logs del backend**

Buscar en los logs del backend:

```
[Debug] Presencia actualizada para UserId: 3e90c352-... via /health
```

Si ves este mensaje periódicamente (cada X segundos), **el fix funciona correctamente**.

---

## 📊 **FLUJO COMPLETO**

```
┌─────────────┐
│   Desktop   │
│  DiarioPage │
└──────┬──────┘
       │
       │ GET /api/v1/health (cada 10s)
       │ Authorization: Bearer TOKEN
       │
       ▼
┌─────────────────┐
│    Backend      │
│ HealthController│
└──────┬──────────┘
       │
       ├─► 1. Verificar si usuario está autenticado
       │   (extraer UserId del token JWT)
       │
       ├─► 2. Buscar sesión activa del usuario
       │   (UserSessions WHERE RevokedAt IS NULL)
       │
       ├─► 3. Actualizar LastSeenAt = DateTime.UtcNow
       │   (guardar en base de datos)
       │
       └─► 4. Devolver {"status":"ok"}
           (backward compatible)
```

---

## 📊 **COMPARACIÓN: ANTES vs DESPUÉS**

### ❌ **ANTES**:

| Usuario | Puede actualizar presencia | Cómo |
|---------|---------------------------|------|
| ADMIN | ✅ Sí | Llamando a `/admin/ping` |
| EDITOR | ❌ No | Sin solución |
| USER | ❌ No | Sin solución |

### ✅ **DESPUÉS**:

| Usuario | Puede actualizar presencia | Cómo |
|---------|---------------------------|------|
| ADMIN | ✅ Sí | Automático con `/health` |
| EDITOR | ✅ Sí | Automático con `/health` |
| USER | ✅ Sí | Automático con `/health` |

---

## 🔗 **ARCHIVOS RELACIONADOS**

### **Backend (GestionTimeApi)**:
- `Controllers/HealthController.cs` → Método `Get()` **(ESTE ARCHIVO)**
- `Controllers/AuthController.cs` → Método `Logout()` (ya corregido) ✅
- `Controllers/PresenceController.cs` → Ya corregido (timeout 30s) ✅

### **Desktop (GestionTimeDesktop)**:
- `Views/DiarioPage.xaml.cs` → Llama a `/health` periódicamente
- `Services/ApiClient.cs` → Gestiona peticiones HTTP
- `Services/Presence/PresenceService.cs` → Consulta `/presence/users`

---

## 📚 **DOCUMENTACIÓN ADICIONAL**

- [FIX_LOGOUT_PRESENCIA_BACKEND.md](FIX_LOGOUT_PRESENCIA_BACKEND.md) - Fix del logout
- [SISTEMA_ROLES_USUARIOS.md](SISTEMA_ROLES_USUARIOS.md)
- [GESTION_USUARIOS_INLINE_SETTINGS.md](GESTION_USUARIOS_INLINE_SETTINGS.md)

---

## ⚠️ **CONSIDERACIONES**

### **¿Qué pasa si el usuario NO está autenticado?**

El endpoint funciona igual:
- No se actualiza presencia (porque no hay usuario)
- Devuelve `{"status":"ok"}` normalmente
- **No falla** ✅

### **¿Qué pasa si falla la actualización de presencia?**

El health check **NO falla**:
```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "Error actualizando presencia...");
    // Continúa y devuelve {"status":"ok"} de todos modos
}
```

### **¿Afecta el rendimiento?**

**Impacto mínimo**:
- 1 query adicional: `SELECT * FROM UserSessions WHERE UserId = X AND RevokedAt IS NULL`
- 1 query adicional: `UPDATE UserSessions SET LastSeenAt = NOW() WHERE Id = Y`
- **Total**: ~2-5ms adicionales

---

## 🎯 **ESTADO**

- [x] Problema identificado
- [x] Solución documentada
- [ ] Fix aplicado en backend
- [ ] Backend reiniciado
- [ ] Pruebas de verificación completadas

---

**Autor**: GitHub Copilot  
**Ticket**: Actualización automática de presencia con /health  
**Prioridad**: 🟡 MEDIA (mejora de experiencia)  
**Relacionado con**: [FIX_LOGOUT_PRESENCIA_BACKEND.md](FIX_LOGOUT_PRESENCIA_BACKEND.md)
