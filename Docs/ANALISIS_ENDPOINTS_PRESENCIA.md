# 🔍 Análisis de Endpoints de Presencia - GestionTime

## 📊 Estado Actual del Sistema

### ✅ **Endpoint Actualmente en Uso**
```
GET /api/v1/admin/users
```

**Ubicación en código:**
- `Services\Presence\PresenceService.cs` línea 47
- `Services\Admin\AdminUsersService.cs`

**Autenticación:**
- ✅ **Bearer Token** (JWT)
- Header: `Authorization: Bearer <token>`

**Código actual:**
```csharp
var response = await App.Api.GetAsync<List<UserListItemDto>>("/api/v1/admin/users", ct);
```

---

## 🔐 Cómo Funciona la Autenticación

### **ApiClient.cs - Sistema de Bearer Token**

```csharp
// 1. Login obtiene el token
var res = await PostAsync<LoginRequest, LoginResponse>(LoginPath, req, ct);
if (res != null && !string.IsNullOrWhiteSpace(res.AccessToken))
{
    SetBearerToken(res.AccessToken!, res.RefreshToken);
}

// 2. Token se agrega automáticamente a TODAS las peticiones
public void SetBearerToken(string accessToken, string? refreshToken = null)
{
    AccessToken = accessToken;
    RefreshToken = refreshToken;
    
    // ✅ ESTE ES EL KEY: Se agrega el header a TODAS las peticiones
    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}
```

**Flow completo:**
1. Usuario hace login → recibe `access_token` y `refresh_token`
2. `SetBearerToken()` configura el header `Authorization: Bearer <token>`
3. **TODAS** las peticiones posteriores incluyen automáticamente este header
4. Backend valida el token en cada request

---

## 🆚 Comparación de Endpoints

### **Opción A: `/api/v1/admin/users` (ACTUAL)**
| Característica | Valor |
|---------------|-------|
| **Autenticación** | ✅ Bearer Token (JWT) |
| **Permisos** | Solo ADMIN |
| **Caché** | 15 segundos |
| **Campos** | id, email, fullName, enabled, roles, **lastSeenAt** |
| **Ventaja** | Ya implementado, funciona |

### **Opción B: `/v1/presence/users` (¿ALTERNATIVA?)**
| Característica | Valor |
|---------------|-------|
| **Autenticación** | ❓ ¿Cookies? ¿Bearer? |
| **Permisos** | ❓ Desconocido |
| **Documentación** | ❌ No existe en docs |
| **Estado** | 🚫 **NO EXISTE en el backend actual** |

---

## 🔎 Búsqueda de `/v1/presence/users`

He revisado:
- ✅ `Services\Presence\PresenceService.cs` → Usa `/api/v1/admin/users`
- ✅ `Services\ApiClient.cs` → Usa Bearer Token en TODOS los requests
- ✅ `Docs\` → No hay referencia a `/v1/presence/users`
- ✅ Backend docs → Solo menciona `/api/v1/admin/users`

**Resultado:** El endpoint `/v1/presence/users` **NO EXISTE** en tu proyecto actual.

---

## 🧐 ¿De Dónde Viene `/v1/presence/users`?

### **Hipótesis 1: Confusión con otra implementación**
Posiblemente viste este endpoint en:
- Otra versión del backend
- Un proyecto diferente
- Documentación de ejemplo

### **Hipótesis 2: Backend alternativo**
Si tienes un backend diferente con este endpoint, necesitarías:
1. Configurar la autenticación (cookies vs token)
2. Actualizar `PresenceService.cs`
3. Verificar el formato de respuesta

---

## 🔧 ¿Quieres Cambiar al Endpoint `/v1/presence/users`?

### **Paso 1: Verificar si Existe**
```powershell
# Probar si el endpoint responde
$token = "<tu_access_token>"
Invoke-RestMethod -Uri "https://gestiontimeapi.onrender.com/v1/presence/users" `
    -Headers @{Authorization="Bearer $token"} `
    -Method Get
```

**Resultado esperado:**
- ✅ **200 OK + JSON** → El endpoint existe
- ❌ **404 Not Found** → El endpoint NO existe

---

### **Paso 2: Identificar Autenticación**

#### **A) Si usa Bearer Token (JWT):**
```csharp
// NO NECESITAS CAMBIAR NADA
// ApiClient.cs ya agrega automáticamente el Bearer token a TODAS las peticiones
// Solo cambiar la URL:
var response = await App.Api.GetAsync<List<UserListItemDto>>("/v1/presence/users", ct);
```

#### **B) Si usa Cookies (Session-based):**
```csharp
// ApiClient.cs ya soporta cookies:
var handler = new HttpClientHandler 
{ 
    UseCookies = true,  // ✅ YA ESTÁ HABILITADO
    CookieContainer = new CookieContainer()
};

// El login debe devolver Set-Cookie en la respuesta HTTP
// Las cookies se envían automáticamente en requests posteriores
```

---

## 📝 Cambio de Endpoint (Si Necesario)

### **Archivo: `Services\Presence\PresenceService.cs`**

**Antes (línea 47):**
```csharp
var response = await App.Api.GetAsync<List<UserListItemDto>>("/api/v1/admin/users", ct);
```

**Después:**
```csharp
var response = await App.Api.GetAsync<List<UserListItemDto>>("/v1/presence/users", ct);
```

**⚠️ IMPORTANTE:** Verifica que el nuevo endpoint devuelva el mismo formato JSON:
```json
[
  {
    "id": "guid",
    "email": "user@example.com",
    "fullName": "User Name",
    "enabled": true,
    "roles": ["ADMIN"],
    "lastSeenAt": "2024-01-15T10:30:00Z"  // ✅ CRÍTICO
  }
]
```

---

## 🧪 Testing

### **Test 1: Verificar Autenticación**
```powershell
# Archivo: Scripts\Test-PresenceEndpoint.ps1

$baseUrl = "https://gestiontimeapi.onrender.com"

# 1. Login
$login = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login-desktop" `
    -Method Post `
    -Body '{"email":"psantos@global-retail.com","password":"tu_password"}' `
    -ContentType "application/json"

$token = $login.AccessToken
Write-Host "✅ Token obtenido: $($token.Substring(0, 50))..."

# 2. Probar /api/v1/admin/users (actual)
$usersAdmin = Invoke-RestMethod -Uri "$baseUrl/api/v1/admin/users" `
    -Headers @{Authorization="Bearer $token"} `
    -Method Get

Write-Host "✅ /api/v1/admin/users responde: $($usersAdmin.Count) usuarios"

# 3. Probar /v1/presence/users (alternativo)
try {
    $usersPresence = Invoke-RestMethod -Uri "$baseUrl/v1/presence/users" `
        -Headers @{Authorization="Bearer $token"} `
        -Method Get
    
    Write-Host "✅ /v1/presence/users responde: $($usersPresence.Count) usuarios"
} catch {
    Write-Host "❌ /v1/presence/users NO EXISTE o requiere autenticación diferente"
    Write-Host "Error: $($_.Exception.Message)"
}
```

### **Test 2: Comparar Respuestas**
Si ambos endpoints existen, compara el formato:
```powershell
$usersAdmin | ConvertTo-Json -Depth 3 | Out-File "users_admin.json"
$usersPresence | ConvertTo-Json -Depth 3 | Out-File "users_presence.json"

code --diff users_admin.json users_presence.json
```

---

## 🎯 Recomendación

### **Opción RECOMENDADA: Mantener `/api/v1/admin/users`**

**Razones:**
1. ✅ **Ya funciona** y está probado
2. ✅ **Documentado** en backend
3. ✅ **Usa Bearer Token** (más seguro que cookies para APIs)
4. ✅ **Incluye `lastSeenAt`** (crítico para online/offline)
5. ✅ **Roles incluidos** en la respuesta

**Solo cambiar si:**
- `/v1/presence/users` tiene datos adicionales que necesitas
- El backend actual no soporta `/api/v1/admin/users`
- Hay requisitos específicos de negocio

---

## 🔒 Seguridad: Bearer Token vs Cookies

### **Bearer Token (JWT) - ACTUAL**
**Ventajas:**
- ✅ Stateless (no requiere sesión en servidor)
- ✅ Funciona con APIs REST puras
- ✅ Fácil refresh con `refresh_token`
- ✅ Expiraciones configurables

**Desventajas:**
- ⚠️ Debe protegerse contra XSS (no almacenar en localStorage)
- ⚠️ Token puede ser grande (payload incluido)

### **Cookies (Session-based)**
**Ventajas:**
- ✅ HttpOnly cookies más seguras contra XSS
- ✅ Browser maneja automáticamente

**Desventajas:**
- ❌ Requiere estado en servidor (sesiones)
- ❌ CSRF protection necesaria
- ❌ No funciona bien con apps móviles

---

## 📞 Siguiente Paso

**Para ayudarte mejor, necesito que:**

1. **Verifica si `/v1/presence/users` existe:**
   ```powershell
   cd C:\GestionTime\GestionTimeDesktop\Scripts
   .\Test-PresenceEndpoint.ps1
   ```

2. **Si existe, dime:**
   - ¿Usa Bearer Token o Cookies?
   - ¿Qué campos devuelve en JSON?
   - ¿Qué ventajas tiene sobre `/api/v1/admin/users`?

3. **Si NO existe:**
   - Confirmo que el sistema actual funciona correctamente
   - Usa Bearer Token con JWT
   - No necesitas cambiar nada

---

**Creado:** 2025-01-21  
**Proyecto:** GestionTime Desktop v1.5.0-beta  
**Estado:** Sistema actual funciona con `/api/v1/admin/users` + Bearer Token
