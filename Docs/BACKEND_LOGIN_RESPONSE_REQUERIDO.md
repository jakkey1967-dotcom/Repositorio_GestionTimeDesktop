# ?? Backend: LoginResponse Requerido para Desktop

**Fecha:** 2025-01-26  
**Estado:** ?? URGENTE - Requerido para funcionamiento correcto  
**Prioridad:** ?? **CRÍTICA**

---

## ? **Problema Actual**

El backend solo está devolviendo:

```json
{
  "message": "ok"
}
```

Esto hace que el cliente desktop **NO pueda mostrar**:
- ? Nombre del usuario en el banner
- ? Email del usuario
- ? Rol del usuario (Admin, Técnico, Usuario)

## ? **Solución Requerida**

El endpoint de login **DEBE devolver** la información básica del usuario:

```json
{
  "message": "ok",
  "userName": "Pedro Santos",
  "userEmail": "psantos@global-retail.com",
  "userRole": "Admin"
}
```

---

## ?? **Endpoint a Modificar**

### **POST** `/api/v1/auth/login`

**Request Body:**
```json
{
  "email": "psantos@global-retail.com",
  "password": "contraseña"
}
```

**Response Actual (? INCOMPLETO):**
```json
{
  "message": "ok"
}
```

**Response Requerido (? COMPLETO):**
```json
{
  "message": "ok",
  "userName": "Pedro Santos",          // ?? AGREGAR
  "userEmail": "psantos@global-retail.com",  // ?? AGREGAR
  "userRole": "Admin"                  // ?? AGREGAR
}
```

---

## ?? **Código Backend (C# / ASP.NET Core)**

### Antes (? Incompleto):

```csharp
[HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    
    if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
    {
        return Unauthorized(new { message = "Credenciales inválidas" });
    }
    
    // Crear sesión con cookies
    await _signInManager.SignInAsync(user, isPersistent: true);
    
    // ? RESPUESTA INCOMPLETA
    return Ok(new { message = "ok" });
}
```

### Después (? Completo):

```csharp
[HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    
    if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
    {
        return Unauthorized(new { message = "Credenciales inválidas" });
    }
    
    // Crear sesión con cookies
    await _signInManager.SignInAsync(user, isPersistent: true);
    
    // Obtener roles del usuario
    var roles = await _userManager.GetRolesAsync(user);
    var role = roles.FirstOrDefault() ?? "Usuario";
    
    // ? RESPUESTA COMPLETA con datos del usuario
    return Ok(new
    {
        message = "ok",
        userName = user.Name ?? user.UserName ?? user.Email?.Split('@')[0],  // ??
        userEmail = user.Email,                                                // ??
        userRole = role                                                        // ??
    });
}
```

---

## ?? **Explicación de los Campos**

### 1. `userName` (Nombre del Usuario)

**Fuentes posibles (en orden de prioridad):**

1. **`user.Name`** - Si tu tabla de usuarios tiene un campo `Name`
2. **`user.UserName`** - Si usas el UserName de ASP.NET Identity
3. **`user.Email.Split('@')[0]`** - Extraer del email como fallback

**Ejemplo:**
```csharp
// Si user.Name = "Pedro Santos" ? usar "Pedro Santos"
// Si user.Name = null y user.UserName = "psantos" ? usar "psantos"
// Si ambos null y user.Email = "psantos@global-retail.com" ? usar "psantos"
userName = user.Name ?? user.UserName ?? user.Email?.Split('@')[0]
```

### 2. `userEmail` (Email del Usuario)

**Fuente:**
- **`user.Email`** - Email registrado en la base de datos

**Ejemplo:**
```csharp
userEmail = user.Email  // "psantos@global-retail.com"
```

### 3. `userRole` (Rol del Usuario)

**Fuente:**
- **`_userManager.GetRolesAsync(user)`** - Roles de ASP.NET Identity

**Valores posibles:**
- `"Admin"` - Administrador
- `"Técnico"` - Usuario técnico
- `"Usuario"` - Usuario estándar
- Cualquier otro rol que tengas configurado

**Ejemplo:**
```csharp
var roles = await _userManager.GetRolesAsync(user);
var role = roles.FirstOrDefault() ?? "Usuario";  // Si no tiene rol, usar "Usuario"
```

---

## ?? **Testing del Backend**

### 1. Probar con Swagger

1. Abrir Swagger: `https://localhost:2501/swagger`
2. Buscar endpoint: `POST /api/v1/auth/login`
3. Hacer login con credenciales válidas:
   ```json
   {
     "email": "psantos@global-retail.com",
     "password": "tu_contraseña"
   }
   ```
4. **Verificar respuesta:**
   ```json
   {
     "message": "ok",
     "userName": "Pedro Santos",    // ? Debe aparecer
     "userEmail": "psantos@global-retail.com",  // ? Debe aparecer
     "userRole": "Admin"            // ? Debe aparecer
   }
   ```

### 2. Probar con Postman

```http
POST https://localhost:2501/api/v1/auth/login
Content-Type: application/json

{
  "email": "psantos@global-retail.com",
  "password": "tu_contraseña"
}
```

**Respuesta esperada:**
```json
HTTP 200 OK

{
  "message": "ok",
  "userName": "Pedro Santos",
  "userEmail": "psantos@global-retail.com",
  "userRole": "Admin"
}
```

---

## ?? **Logs del Cliente (Después de la Modificación)**

Una vez que modifiques el backend, los logs del cliente mostrarán:

### Antes (?):
```
[INFO] ?? Guardando información de usuario:
[INFO]    • UserName (API): (null) ? Guardado: Psantos
[INFO]    • UserEmail (API): (null) ? Guardado: psantos@global-retail.com
[INFO]    • UserRole (API): (null) ? Guardado: Admin
[WARN] ?? TEMPORAL: Rol hardcodeado basado en email: Admin
```

### Después (?):
```
[INFO] ?? Guardando información de usuario:
[INFO]    • UserName (API): Pedro Santos ? Guardado: Pedro Santos
[INFO]    • UserEmail (API): psantos@global-retail.com ? Guardado: psantos@global-retail.com
[INFO]    • UserRole (API): Admin ? Guardado: Admin
```

---

## ?? **Banner del Desktop (Resultado Final)**

### Antes (con hardcode temporal):
```
?? Psantos • Admin
   psantos@global-retail.com
```

### Después (con datos reales del backend):
```
?? Pedro Santos • Admin
   psantos@global-retail.com
```

---

## ?? **Casos Especiales**

### Si el usuario NO tiene un campo `Name`:

```csharp
// Opción 1: Usar UserName
userName = user.UserName

// Opción 2: Extraer del email
userName = user.Email?.Split('@')[0]

// Opción 3: Capitalizar el username
userName = CapitalizeFirst(user.UserName)

// Helper para capitalizar
private string CapitalizeFirst(string text)
{
    if (string.IsNullOrEmpty(text)) return text;
    return char.ToUpper(text[0]) + text.Substring(1);
}
```

### Si el usuario NO tiene rol asignado:

```csharp
var roles = await _userManager.GetRolesAsync(user);
var role = roles.FirstOrDefault() ?? "Usuario";  // Fallback a "Usuario"
```

### Si hay múltiples roles:

```csharp
// Opción 1: Tomar el primero
var role = roles.FirstOrDefault() ?? "Usuario";

// Opción 2: Concatenar todos
var role = string.Join(", ", roles);  // "Admin, Técnico"

// Opción 3: Priorizar por jerarquía
var role = roles.Contains("Admin") ? "Admin" 
         : roles.Contains("Técnico") ? "Técnico" 
         : "Usuario";
```

---

## ?? **Seguridad**

### ? Datos seguros de enviar en el login:
- ? `userName` - Nombre del usuario (visible en la UI)
- ? `userEmail` - Email (ya lo sabe el usuario)
- ? `userRole` - Rol (necesario para la UI)

### ? Datos que NO debes enviar:
- ? `passwordHash` - Contraseña hasheada
- ? `securityStamp` - Token de seguridad interno
- ? Datos sensibles de otros usuarios

---

## ?? **Estructura Completa del LoginResponse**

```csharp
public class LoginResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = "ok";
    
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }
    
    [JsonPropertyName("userEmail")]
    public string? UserEmail { get; set; }
    
    [JsonPropertyName("userRole")]
    public string? UserRole { get; set; }
    
    // Opcional: Si quieres devolver el token JWT en vez de cookies
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }
}
```

**Uso:**
```csharp
return Ok(new LoginResponse
{
    Message = "ok",
    UserName = user.Name ?? user.UserName,
    UserEmail = user.Email,
    UserRole = role
});
```

---

## ?? **Checklist de Implementación**

### Backend
- [ ] Modificar endpoint `POST /api/v1/auth/login`
- [ ] Agregar campo `userName` a la respuesta
- [ ] Agregar campo `userEmail` a la respuesta
- [ ] Agregar campo `userRole` a la respuesta
- [ ] Obtener roles con `_userManager.GetRolesAsync(user)`
- [ ] Manejar caso de usuario sin rol (fallback a "Usuario")
- [ ] Probar con Swagger/Postman
- [ ] Verificar que los datos son correctos

### Cliente Desktop (Ya está listo ?)
- [x] `LoginResponse` con campos opcionales
- [x] Extracción de datos del login
- [x] Fallback a `/api/v1/users/me` si no vienen
- [x] Fallback a valores por defecto si ambos fallan
- [x] Guardado en `LocalSettings`
- [x] Mostrar en el banner

---

## ?? **Soporte**

Si tienes dudas sobre:
- **Campo `Name`**: ¿Tu tabla de usuarios tiene un campo `Name`?
- **Roles**: ¿Cómo están configurados los roles en tu aplicación?
- **Estructura**: ¿Usas ASP.NET Identity o una estructura personalizada?

Revisa tu modelo de usuario:
```csharp
// Ejemplo típico
public class ApplicationUser : IdentityUser
{
    public string? Name { get; set; }  // ¿Existe este campo?
    // ... otros campos
}
```

---

## ? **Estado Actual**

| Componente | Estado | Acción Requerida |
|------------|--------|------------------|
| Cliente Desktop | ? Listo | Ninguna |
| Backend Login | ? Incompleto | **Modificar response** |
| Swagger/Testing | ? Pendiente | Probar después de modificar |

---

## ?? **Prioridad**

**?? CRÍTICA** - Sin estos datos, el cliente desktop:
- ? No muestra el nombre real del usuario
- ? Usa el email como nombre (poco profesional)
- ? Usa rol hardcodeado (inseguro)

**Tiempo estimado de implementación:** 5-10 minutos

---

**Última actualización:** 2025-01-26  
**Estado:** ?? Pendiente de implementación en backend
