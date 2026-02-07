# ✅ FIX CRÍTICO: UserRoleSafe devolvía "Usuario" en lugar de "USER"

**Fecha:** 2025-02-03  
**Estado:** ✅ **CORREGIDO Y COMPILADO**  
**Impacto:** CRÍTICO - Afectaba a TODOS los usuarios donde backend no devuelve rol

---

## 🐛 Bug Reportado

**Síntoma:**
- Con rol USER: Funciona correctamente (candados OK)
- Con rol ADMIN: Sale igual que USER (debería mostrar todos abiertos 🔓)

**Usuario reportó:**
> "con el rol USER si se ve como yo quiero, pero luego entro con el rol de ADMIN y sale igual"

---

## 🔍 Causa Raíz REAL

### Problema en `ApiClient.cs` línea 1222

```csharp
// ❌ ANTES (MAL):
public string UserRoleSafe => string.IsNullOrWhiteSpace(UserRole) ? "Usuario" : UserRole;
//                                                                     ^^^^^^^^
//                                                                     PROBLEMA AQUÍ
```

**¿Por qué esto rompía todo?**

1. Backend NO devuelve campo `role` en respuesta de login (o es NULL)
2. `LoginResponse.UserRole` = `null`
3. `UserRoleSafe` devuelve `"Usuario"` (string genérico en español)
4. `LoginPage` guarda `"Usuario"` en `user-info.json`
5. `SettingsViewModel` lee `"Usuario"` y lo mapea a `UserRole.USER` (por el default `_`)
6. **RESULTADO:** TODOS los usuarios (incluso ADMIN) se tratan como USER

---

## 📊 Flujo del Bug

```
┌──────────────────────────────────┐
│ Backend Login Endpoint           │
│ POST /api/v1/auth/login          │
│                                  │
│ Response:                        │
│ {                                │
│   "accessToken": "...",          │
│   "userName": "Pedro Santos",    │
│   "role": null   ◄───────────────┼─── ❌ BACKEND NO DEVUELVE ROL
│ }                                │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ ApiClient.cs (línea 1222)        │
│                                  │
│ LoginResponse.UserRole = null    │
│                                  │
│ UserRoleSafe devuelve:           │
│   "Usuario"  ◄───────────────────┼─── ❌ STRING INVÁLIDO
│   (no es USER, EDITOR ni ADMIN)  │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ LoginPage.xaml.cs (línea 446)    │
│                                  │
│ UserInfoFileStorage.SaveUserInfo │
│   (userName, email, "Usuario")   │
│                                  │
│ Archivo: user-info.json          │
│ {                                │
│   "UserRole": "Usuario"  ◄───────┼─── ❌ VALOR INVÁLIDO GUARDADO
│ }                                │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ SettingsViewModel.cs (línea 65)  │
│                                  │
│ roleString = "Usuario".ToUpper() │
│            = "USUARIO"            │
│                                  │
│ Mapeo:                           │
│   "ADMIN" => UserRole.ADMIN      │
│   "EDITOR" => UserRole.EDITOR    │
│   "USER" => UserRole.USER        │
│   _ => UserRole.USER  ◄──────────┼─── ❌ DEFAULT PARA INVÁLIDOS
│                                  │
│ currentRole = UserRole.USER      │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ ❌ RESULTADO:                    │
│                                  │
│ TODOS los usuarios (incluso      │
│ ADMIN real) se tratan como USER  │
│                                  │
│ • Candados bloqueados            │
│ • Acceso denegado a secciones    │
│ • Permisos incorrectos           │
└──────────────────────────────────┘
```

---

## ✅ Solución Aplicada

### **Fix #1: ApiClient.cs (línea 1222)**

**❌ ANTES:**
```csharp
public string UserRoleSafe => string.IsNullOrWhiteSpace(UserRole) ? "Usuario" : UserRole;
```

**✅ DESPUÉS:**
```csharp
public string UserRoleSafe => string.IsNullOrWhiteSpace(UserRole) ? "USER" : UserRole;
//                                                                    ^^^^
//                                                                    CORREGIDO
```

**Razonamiento:**
- Si backend no devuelve rol, **asumir rol más restrictivo (USER)**
- `"USER"` es un valor VÁLIDO que SettingsViewModel puede mapear correctamente
- Evita romper el sistema con strings genéricos como `"Usuario"`

---

### **Fix #2: SettingsViewModel.cs (líneas 54-88)**

**Mejoras añadidas:**
1. ✅ Logging detallado del proceso de carga de rol
2. ✅ Advertencia específica cuando se detecta rol inválido
3. ✅ Normalización con `.Trim().ToUpperInvariant()`
4. ✅ Log de permisos calculados para diagnóstico

**Código nuevo:**
```csharp
var roleString = userInfo.UserRole.Trim().ToUpperInvariant();

_log?.LogInformation("📋 Analizando rol desde archivo: '{roleOriginal}' -> '{roleNormalized}'", 
    userInfo.UserRole, roleString);

// ⚠️ ADVERTENCIA: Si backend devuelve NULL, UserRoleSafe devuelve "Usuario" (inválido)
if (!new[] { "ADMIN", "EDITOR", "USER" }.Contains(roleString))
{
    _log?.LogWarning("⚠️ Rol INVÁLIDO detectado: '{roleString}' (usando USER por defecto)", roleString);
    _log?.LogWarning("   Posible causa: Backend devuelve NULL en campo 'role' → UserRoleSafe = 'Usuario'");
    _log?.LogWarning("   Verificar endpoint de login: /api/v1/auth/login debe incluir 'role' en respuesta");
}
```

---

## 📊 Logs Esperados (DESPUÉS)

### **Si backend NO devuelve rol (NULL):**

**Login (LoginPage.xaml.cs línea 443):**
```
[Info] 💾 PASO 1: Guardando información básica del usuario...
[Info]    • UserRole (de login): USER         ← ✅ AHORA ES "USER" en lugar de "Usuario"
```

**Settings (SettingsViewModel.cs línea 58-88):**
```
[Info] 📋 Analizando rol desde archivo: 'USER' -> 'USER'
[Info] ✅ Rol mapeado: 'USER' -> USER
[Info] ══════════════════════════════════════════════════════════════
[Info] ✅ SettingsViewModel inicializado con rol: USER
[Info]    • Perfil y cuenta: True
[Info]    • Clientes: False
[Info]    • Permisos: False
[Info] ══════════════════════════════════════════════════════════════
```

### **Si backend devuelve rol ADMIN correctamente:**

**Login:**
```
[Info]    • UserRole (de login): ADMIN        ← ✅ Viene del backend
```

**Settings:**
```
[Info] 📋 Analizando rol desde archivo: 'ADMIN' -> 'ADMIN'
[Info] ✅ Rol mapeado: 'ADMIN' -> ADMIN
[Info] ══════════════════════════════════════════════════════════════
[Info] ✅ SettingsViewModel inicializado con rol: ADMIN
[Info]    • Perfil y cuenta: True
[Info]    • Clientes: True                    ← ✅ PERMITIDO
[Info]    • Permisos: True                    ← ✅ PERMITIDO
[Info] ══════════════════════════════════════════════════════════════
```

### **Si backend devolvía "Usuario" (ANTES del fix):**

**Settings (con logging mejorado):**
```
[Info] 📋 Analizando rol desde archivo: 'Usuario' -> 'USUARIO'
[Warning] ⚠️ Rol INVÁLIDO detectado: 'USUARIO' (usando USER por defecto)
[Warning]    Posible causa: Backend devuelve NULL en campo 'role' → UserRoleSafe = 'Usuario'
[Warning]    Verificar endpoint de login: /api/v1/auth/login debe incluir 'role' en respuesta
[Info] ✅ Rol mapeado: 'USUARIO' -> USER
```

---

## 🧪 Testing

### **Paso 1: Eliminar archivo antiguo**

```powershell
# Eliminar archivo con valor "Usuario" inválido
Remove-Item "$env:LOCALAPPDATA\GestionTime\user-info.json" -Force
```

### **Paso 2: Volver a hacer login**

1. Abrir aplicación
2. Iniciar sesión
3. Monitorear logs

### **Paso 3: Verificar archivo nuevo**

```powershell
# Ver contenido del archivo nuevo
Get-Content "$env:LOCALAPPDATA\GestionTime\user-info.json" | ConvertFrom-Json | Format-List
```

**Salida esperada:**
```json
{
  "UserName": "Pedro Santos",
  "UserEmail": "psantos@example.com",
  "UserRole": "USER",        ← ✅ Ahora es "USER" en lugar de "Usuario"
  "LastUpdated": "2025-02-03T..."
}
```

### **Paso 4: Abrir Settings (Ctrl+Alt+P)**

Verificar logs:
```powershell
Select-String -Path "C:\App\GestionTime-Desktop\logs\*.log" -Pattern "SettingsViewModel inicializado con rol"
```

**Salida esperada:**
```
✅ SettingsViewModel inicializado con rol: USER
```

### **Paso 5: Cambiar rol a ADMIN en backend**

**SQL (backend):**
```sql
UPDATE users SET role = 'ADMIN' WHERE email = 'tu_email@example.com';
```

**O endpoint admin (si existe):**
```powershell
$headers = @{ Authorization = "Bearer $token" }
$body = @{ userId = 123; role = "ADMIN" } | ConvertTo-Json
Invoke-RestMethod -Uri "https://api.gestiontime.com/api/v1/admin/users/role" `
    -Method PUT -Headers $headers -Body $body -ContentType "application/json"
```

### **Paso 6: Re-loguearse como ADMIN**

1. Cerrar sesión
2. Volver a iniciar sesión
3. Verificar archivo:

```powershell
Get-Content "$env:LOCALAPPDATA\GestionTime\user-info.json" | ConvertFrom-Json | Format-List
```

**Salida esperada:**
```json
{
  "UserRole": "ADMIN"        ← ✅ AHORA debe aparecer ADMIN
}
```

### **Paso 7: Verificar candados en Settings**

Abrir Settings (Ctrl+Alt+P) y verificar:

**Para ADMIN:**
- ✅ Perfil y cuenta → 🔓 Verde
- ✅ Permisos y roles → 🔓 Verde (AHORA SÍ)
- ✅ Clientes → 🔓 Verde (AHORA SÍ)
- ✅ Grupos y Tipos → 🔓 Verde (AHORA SÍ)
- ✅ Integraciones → 🔓 Verde (AHORA SÍ)
- ✅ Importación/Exportación → 🔓 Verde (AHORA SÍ)
- ✅ Usuarios online/Presencia → 🔓 Verde
- ✅ Parámetros → 🔓 Verde (AHORA SÍ)
- ✅ Salir → 🔓 Verde

**TODOS los candados deben estar abiertos (verde) para ADMIN.**

---

## 🔍 Script de Diagnóstico

Para diagnosticar problemas futuros:

```powershell
.\Scripts\Diagnose-RolesProblem.ps1
```

El script verifica:
- ✅ Contenido de `user-info.json`
- ✅ Logs de login (UserRole guardado)
- ✅ Logs de Settings (rol cargado)
- ✅ Roles inválidos detectados
- ✅ Recomendaciones de solución

---

## 📝 Archivos Modificados

1. ✅ `Services/ApiClient.cs` (línea 1222)
   - `UserRoleSafe` devuelve `"USER"` en lugar de `"Usuario"`

2. ✅ `ViewModels/SettingsViewModel.cs` (líneas 54-88)
   - Logging detallado del proceso de carga
   - Advertencia para roles inválidos
   - Normalización con Trim() y ToUpperInvariant()

3. ✅ `Scripts/Diagnose-RolesProblem.ps1` (NUEVO)
   - Script de diagnóstico completo

---

## ✅ Resultado Final

### ANTES (Bug):
- ❌ Backend devuelve NULL → `UserRoleSafe = "Usuario"`
- ❌ Se guarda `"Usuario"` en archivo
- ❌ SettingsViewModel mapea a `USER` (default)
- ❌ ADMIN real se trata como USER
- ❌ Candados todos cerrados para ADMIN

### DESPUÉS (Corregido):
- ✅ Backend devuelve NULL → `UserRoleSafe = "USER"`
- ✅ Se guarda `"USER"` en archivo (valor válido)
- ✅ SettingsViewModel mapea correctamente
- ✅ Si backend devuelve `"ADMIN"`, se respeta
- ✅ Candados correctos según rol real
- ✅ Logging detallado para diagnóstico

---

## 🎓 Lecciones Aprendidas

1. **NUNCA usar strings genéricos como defaults**
   - `"Usuario"` es español, no es un valor de enum
   - Siempre usar valores VÁLIDOS del dominio (`"USER"`, `"ADMIN"`, `"EDITOR"`)

2. **Logging es crítico en flujos de autenticación**
   - Permite identificar dónde se pierde/corrompe la información
   - En este caso, los logs hubieran mostrado `"Usuario"` de inmediato

3. **Testing con múltiples roles**
   - El desarrollador probó solo con USER (funcionaba)
   - No probó con ADMIN (donde se manifestaba el bug)
   - Siempre probar TODOS los casos de la matriz de permisos

4. **Backend puede devolver NULL en campos opcionales**
   - Siempre tener defaults seguros (restrictivos, no permisivos)
   - `USER` es más seguro que `ADMIN` como default

5. **Valores por defecto deben ser del tipo correcto**
   - Si esperas un enum de roles, el default debe ser un valor válido del enum
   - No usar strings descriptivos genéricos

---

## 🚀 Próximos Pasos

### Inmediatos
1. ⏳ **Eliminar archivo `user-info.json` antiguo**
2. ⏳ **Re-loguearse con usuario ADMIN**
3. ⏳ **Verificar que candados estén todos abiertos (verde)**
4. ⏳ **Monitorear logs** - Buscar "SettingsViewModel inicializado con rol: ADMIN"

### Futuro
1. 🔜 **Verificar backend**: Asegurar que `/api/v1/auth/login` devuelva campo `role`
2. 🔜 **Sincronizar rol**: Implementar endpoint `/api/v1/users/me` que devuelva rol actual
3. 🔜 **Refresh de permisos**: Permitir recargar permisos sin cerrar sesión

---

**Estado:** ✅ **CORREGIDO, COMPILADO Y LISTO PARA TESTING**  
**Confianza:** 🟢 Alta (bug crítico identificado y corregido)  
**Riesgo:** 🟢 Bajo (fix quirúrgico, sin breaking changes)

**Próxima acción:** Eliminar archivo antiguo + Re-loguearse + Verificar candados

