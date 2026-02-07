# ✅ FIX: Nombre de banner no se actualiza al cambiar de usuario

**Fecha:** 2025-02-05  
**Problema:** Email se actualiza, nombre NO  
**Solución:** Forzar recarga de perfil SIEMPRE (sin caché) + **FIX BACKEND PENDIENTE**

---

## 🎯 CAUSA RAÍZ

```csharp
// ❌ PROBLEMA (DiarioPage.xaml.cs línea 353):
if (App.CurrentUserProfile == null)  // ← Solo recarga si NULL
{
    App.CurrentUserProfile = await ProfileService.GetCurrentUserProfileAsync();
}
```

**¿Por qué fallaba?**
- Email usa `App.CurrentLoginEmail` (guardado en cada login) ✅
- Nombre usa `App.CurrentUserProfile` (cacheado, solo recarga si NULL) ❌
- Si el perfil no se limpia correctamente → usa datos del usuario anterior

---

## ✅ SOLUCIÓN FRONTEND (COMPLETADA)

```csharp
// ✅ NUEVO: SIEMPRE recargar perfil (sin if)
App.CurrentUserProfile = await ProfileService.GetCurrentUserProfileAsync();
```

**Beneficios:**
- ✅ Siempre muestra datos del usuario actual
- ✅ No depende de que el cleanup funcione perfectamente
- ✅ Logging mejorado para diagnóstico

**Costo:**
- +1 API request al abrir DiarioPage (~50-100ms)

---

## ❌ PROBLEMA BACKEND DESCUBIERTO

### 🔍 Evidencia en los logs:

```
💾 PASO 1: Guardando información básica del usuario...
   • UserName (de login): Wilson Sánchez  ✅
   • UserEmail (del input): wsanchez@global-retail.com  ✅

📥 Cargando perfil del usuario actual desde API...
   • CurrentLoginEmail: wsanchez@global-retail.com  ✅
   • CurrentUserProfile (antes): NULL  ✅
✅ Perfil cargado: Francisco Santos | Francisco Santos | 626751367  ❌ BACKEND ERROR
🎨 Banner actualizado:
   • DisplayName: Francisco Santos  ❌ NOMBRE INCORRECTO (viene del backend)
   • DisplayEmail: wsanchez@global-retail.com  ✅ EMAIL CORRECTO (viene del login)
```

### 🎯 Diagnóstico:

**El fix del frontend FUNCIONÓ PERFECTAMENTE:**
- ✅ Se eliminó el check `if (App.CurrentUserProfile == null)`
- ✅ Se forzó la recarga SIEMPRE desde la API
- ✅ El logging muestra que se ejecutó correctamente

**El problema está en el BACKEND:**
- ❌ El endpoint `/api/v1/profiles/me` devuelve el perfil de **"Francisco Santos"** cuando debería devolver **"Wilson Sánchez"**
- ❌ El email `wsanchez@global-retail.com` está asociado al usuario incorrecto en la base de datos
- ❌ O la tabla `user_profiles` NO tiene relación con `users` (falta columna `user_id`)
- ❌ O el endpoint NO filtra por el `user_id` del token JWT

---

## 📋 ARCHIVOS MODIFICADOS (FRONTEND)

- **`Views/DiarioPage.xaml.cs`**:
  - Eliminado `if (App.CurrentUserProfile == null)`
  - SIEMPRE recarga perfil desde API
  - Añadido logging de diagnóstico (email actual, perfil anterior/nuevo)

---

## 🔧 DIAGNÓSTICO BACKEND PENDIENTE

### Scripts de diagnóstico creados:

```powershell
# 1. Script SQL para verificar la base de datos
..\GestionTimeApi\scripts\Diagnose-ProfileMismatch.sql

# 2. Script PowerShell para probar el endpoint
.\Scripts\Diagnose-ProfileMismatch.ps1 -Email "wsanchez@global-retail.com" -Password "tu_password"
```

### Verificaciones necesarias:

1. **¿Existe columna `user_id` en `user_profiles`?**
   ```sql
   SELECT column_name 
   FROM information_schema.columns 
   WHERE table_name = 'user_profiles' AND column_name = 'user_id';
   ```

2. **¿Qué perfil está asociado al email `wsanchez@global-retail.com`?**
   ```sql
   SELECT u.id, u.email, u.name, p.first_name, p.last_name
   FROM users u
   LEFT JOIN user_profiles p ON p.user_id = u.id
   WHERE u.email = 'wsanchez@global-retail.com';
   ```

3. **¿El endpoint `/profiles/me` filtra correctamente por `user_id` del token?**
   - Verificar código en `ProfilesController.cs`
   - Debe usar: `var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);`
   - Y filtrar: `WHERE user_id = @userId`

---

## 🧪 VERIFICACIÓN

### Plan de pruebas:

1. ✅ **Frontend funcionando correctamente** (recarga siempre desde API)
2. ⏳ **Ejecutar diagnóstico SQL** (verificar estructura de BBDD)
3. ⏳ **Ejecutar diagnóstico PowerShell** (verificar endpoint)
4. ⏳ **Corregir backend** según hallazgos
5. ⏳ **Verificar visual** (Login Usuario 1 → Logout → Login Usuario 2)

### Logs esperados (Output > Debug):

```
📥 Cargando perfil del usuario actual desde API...
   • CurrentLoginEmail: maria@empresa.com
   • CurrentUserProfile (antes): Pedro Santos  ← Usuario anterior detectado
✅ Perfil cargado: María | López | ...  ← ✅ DEBE COINCIDIR CON USUARIO ACTUAL
🎨 Banner actualizado:
   • DisplayName: María López  ← ✅ ACTUALIZADO CORRECTAMENTE
```

---

## 📝 ESTADO ACTUAL

| Componente | Estado | Notas |
|------------|--------|-------|
| **Frontend Fix** | ✅ Completado | Recarga siempre desde API, logging mejorado |
| **Diagnóstico Backend** | ⏳ Pendiente | Scripts creados, falta ejecutar |
| **Fix Backend** | ⏳ Pendiente | Depende de hallazgos del diagnóstico |
| **Prueba Visual** | ⏳ Pendiente | Requiere fix backend completado |

---

**Doc completa:** `Docs/FIX_NOMBRE_BANNER_NO_ACTUALIZA.md`  
**Scripts diagnóstico:** `Scripts/Diagnose-ProfileMismatch.ps1`, `../GestionTimeApi/scripts/Diagnose-ProfileMismatch.sql`  
**Estado:** ✅ Frontend OK | ❌ Backend con problema | ⏳ Diagnóstico pendiente
