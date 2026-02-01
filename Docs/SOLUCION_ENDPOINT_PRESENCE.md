# 🔍 SOLUCIÓN: Endpoint /api/v1/presence/users

## 📊 RESULTADO DEL TEST

### ✅ **ENDPOINTS QUE FUNCIONAN:**

1. **Login:** `/api/v1/auth/login-desktop` (actual)
   - Devuelve JWT token correctamente
   - Usado en: `App.xaml.cs` línea ~165

2. **Usuarios Admin:** `/api/v1/admin/users` (actual)
   - Campos completos: id, email, fullName, enabled, roles
   - **PROBLEMA:** lastSeenAt = null (NO detecta online/offline)

3. **Usuarios Presence:** `/api/v1/presence/users` (NUEVO DESCUBRIMIENTO)
   - **FUNCIONA:** lastSeenAt tiene valor real
   - **PROBLEMA:** Faltan campos: id, enabled, roles están vacíos

---

## 🎯 RECOMENDACIÓN: Backend debe Actualizar `/api/v1/admin/users`

### **Estado Actual:**
```csharp
// Controllers\AdminUsersController.cs - Línea ~77
var result = users.Select(u => new
{
    u.Id,
    u.Email,
    u.FullName,
    u.Enabled,
    // ❌ FALTA: u.LastSeenAt
    roles = rolesByUser.TryGetValue(u.Id, out var rr) ? rr : Array.Empty<string>()
});
```

### **Estado Deseado:**
```csharp
// Controllers\AdminUsersController.cs - Línea ~77
var result = users.Select(u => new
{
    u.Id,
    u.Email,
    u.FullName,
    u.Enabled,
    u.LastSeenAt,  // ✅ AGREGAR ESTE CAMPO
    roles = rolesByUser.TryGetValue(u.Id, out var rr) ? rr : Array.Empty<string>()
});
```

### **Documentación ya creada:**
```
C:\GestionTime\GestionTimeApi\docs\IMPLEMENTAR-PRESENCIA-BACKEND.md
```

**Pasos:**
1. Ejecutar migración SQL (agregar columna `last_seen_at`)
2. Actualizar `User.cs` (agregar propiedad `LastSeenAt`)
3. Actualizar `AdminUsersController.cs` (agregar campo en respuesta)
4. Implementar endpoint `/api/v1/admin/ping` (actualizar last_seen_at)

---

## 🔧 OPCIÓN TEMPORAL: Cambiar a `/api/v1/presence/users`

### **⚠️ ADVERTENCIA:**
Este cambio **ROMPERÁ** las siguientes funcionalidades:
- ❌ Agrupación por roles (roles está vacío)
- ❌ Identificación por ID (id está vacío)
- ❌ Filtrado por enabled (enabled está vacío)
- ✅ Detección online/offline (lastSeenAt funciona)

### **Cambio en PresenceService.cs:**

```csharp
// Services\Presence\PresenceService.cs - Línea 47

// ANTES:
var response = await App.Api.GetAsync<List<UserListItemDto>>("/api/v1/admin/users", ct);

// DESPUÉS (NO RECOMENDADO - Rompe otras funciones):
var response = await App.Api.GetAsync<List<UserListItemDto>>("/api/v1/presence/users", ct);
```

### **Resultado:**
- ✅ Verás usuarios online/offline correctamente
- ❌ Todos estarán en el mismo grupo (sin roles)
- ❌ No podrás cambiar roles (sin ID)

---

## 🚀 SOLUCIÓN COMPLETA (DUAL ENDPOINT)

Si no puedes actualizar el backend ahora, usa AMBOS endpoints:

### **Nuevo PresenceService.cs:**

```csharp
public async Task<List<UserListItemDto>> GetUsersAsync(CancellationToken ct = default)
{
    await _lock.WaitAsync(ct);
    try
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastFetch;

        if (_cachedUsers.Any() && elapsed < _cacheDuration)
        {
            _log?.LogDebug("📦 Usuarios desde caché");
            return _cachedUsers;
        }

        _log?.LogInformation("🌐 Cargando usuarios desde API...");

        try
        {
            // 1️⃣ Obtener info completa de usuarios (sin lastSeenAt)
            var usersAdmin = await App.Api.GetAsync<List<UserListItemDto>>("/api/v1/admin/users", ct);

            // 2️⃣ Obtener presencia (con lastSeenAt)
            var usersPresence = await App.Api.GetAsync<List<UserListItemDto>>("/api/v1/presence/users", ct);

            if (usersAdmin == null || !usersAdmin.Any())
            {
                _log?.LogWarning("⚠️ API devolvió lista vacía");
                return _cachedUsers;
            }

            // 3️⃣ MERGE: Combinar ambos endpoints por email
            foreach (var user in usersAdmin)
            {
                var presence = usersPresence?.FirstOrDefault(p => 
                    p.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));
                
                if (presence != null && presence.LastSeenAt.HasValue)
                {
                    user.LastSeenAt = presence.LastSeenAt;
                }
            }

            _cachedUsers = usersAdmin;
            _lastFetch = now;

            _log?.LogInformation("✅ Usuarios cargados y mergeados: {count} usuarios", _cachedUsers.Count);

            return _cachedUsers;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "❌ Error cargando usuarios");
            return _cachedUsers;
        }
    }
    finally
    {
        _lock.Release();
    }
}
```

### **Ventajas de esta solución:**
- ✅ Mantiene TODOS los campos (id, roles, enabled)
- ✅ Obtiene lastSeenAt actualizado
- ✅ No rompe funcionalidades existentes
- ⚠️ Desventaja: 2 llamadas al API (más lento)

---

## 📋 COMPARACIÓN DE OPCIONES

| Opción | Ventajas | Desventajas | Recomendación |
|--------|---------|-------------|---------------|
| **A) Actualizar Backend** | ✅ Solución definitiva<br>✅ 1 solo endpoint<br>✅ Más rápido | ⚠️ Requiere cambios en backend | ⭐⭐⭐⭐⭐ **MEJOR** |
| **B) Usar solo /presence/users** | ✅ Funciona para online/offline | ❌ Rompe roles<br>❌ Rompe ID<br>❌ Rompe enabled | ⭐ **NO RECOMENDADO** |
| **C) Dual Endpoint (Merge)** | ✅ Funciona todo<br>✅ No requiere cambios backend | ⚠️ 2 llamadas al API<br>⚠️ Más lento | ⭐⭐⭐ **TEMPORAL** |

---

## 🎯 DECISIÓN RECOMENDADA

### **Para el FIN DE SEMANA:**
1. ✅ Implementar backend siguiendo:
   ```
   C:\GestionTime\GestionTimeApi\docs\CHECKLIST-IMPLEMENTACION-PRESENCIA.md
   ```

2. ✅ Esto agregará `lastSeenAt` a `/api/v1/admin/users`

3. ✅ Tu frontend seguirá funcionando SIN CAMBIOS

### **Mientras tanto (opcional):**
Si necesitas ver online/offline YA, implementa la **Opción C (Dual Endpoint)** temporalmente.

---

## 📞 SIGUIENTE PASO

**¿Qué prefieres?**

1. **Implementar backend ahora** → Te guío paso a paso
2. **Usar solución temporal dual endpoint** → Actualizo `PresenceService.cs`
3. **Esperar al fin de semana** → Mantener sistema actual

---

**Creado:** 2025-01-25  
**Proyecto:** GestionTime Desktop v1.5.0-beta
