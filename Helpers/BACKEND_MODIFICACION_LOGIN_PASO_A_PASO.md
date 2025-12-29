# 🔧 Guía de Modificación del Backend - LoginResponse

**Archivo a modificar:** `AuthController.cs` (o similar)  
**Método:** `Login` o `Authenticate`  
**Endpoint:** `POST /api/v1/auth/login`

---

## 📍 **ANTES - Código Actual (❌ Incompleto)**

```csharp
[HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // Buscar usuario
    var user = await _userManager.FindByEmailAsync(request.Email);
    
    if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
    {
        return Unauthorized(new { message = "Credenciales inválidas" });
    }
    
    // Crear sesión con cookies
    await _signInManager.SignInAsync(user, isPersistent: true);
    
    // ❌ RESPUESTA INCOMPLETA - SOLO DEVUELVE "ok"
    return Ok(new { message = "ok" });
}
```

---

## ✅ **DESPUÉS - Código Modificado (Completo)**

```csharp
[HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // Buscar usuario
    var user = await _userManager.FindByEmailAsync(request.Email);
    
    if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
    {
        return Unauthorized(new { message = "Credenciales inválidas" });
    }
    
    // Crear sesión con cookies
    await _signInManager.SignInAsync(user, isPersistent: true);
    
    // Obtener roles
    var roles = await _userManager.GetRolesAsync(user);
    var role = roles.FirstOrDefault() ?? "Usuario";
    
    // Obtener nombre
    var userName = user.Name ?? user.UserName ?? user.Email?.Split('@')[0] ?? "Usuario";
    
    // Respuesta completa
    return Ok(new
    {
        message = "ok",
        userName = userName,
        userEmail = user.Email,
        userRole = role
    });
}
```

---

## 🔍 **Explicación Detallada**

### 1. Obtener Roles del Usuario

```csharp
// Obtener todos los roles asignados al usuario
var roles = await _userManager.GetRolesAsync(user);

// Tomar el primero, o "Usuario" si no tiene ninguno
var role = roles.FirstOrDefault() ?? "Usuario";
```

**¿Qué hace?**
- `GetRolesAsync(user)` → Devuelve un array de roles: `["Admin"]`, `["Técnico"]`, etc.
- `FirstOrDefault()` → Toma el primer rol
- `?? "Usuario"` → Si no tiene rol, usa "Usuario" por defecto

### 2. Extraer Nombre del Usuario

```csharp
var userName = user.Name ?? user.UserName ?? user.Email?.Split('@')[0] ?? "Usuario";
```

**¿Qué hace?**
- Intenta usar `user.Name` si existe
- Si no, usa `user.UserName`
- Como último recurso, extrae la parte antes del @ del email

### 3. Devolver Respuesta Completa

```csharp
return Ok(new
{
    message = "ok",
    userName = userName,      // "Pedro Santos" o "psantos"
    userEmail = user.Email,   // "psantos@global-retail.com"
    userRole = role           // "Admin", "Técnico", "Usuario"
});
```

---

## 📊 **Resultado Esperado**

### Antes (❌):
```json
{
  "message": "ok"
}
```

### Después (✅):
```json
{
  "message": "ok",
  "userName": "Pedro Santos",
  "userEmail": "psantos@global-retail.com",
  "userRole": "Admin"
}
```

---

## 🧪 **Testing**

### 1. Probar en Swagger

1. Abrir: `https://localhost:2501/swagger`
2. Buscar: `POST /api/v1/auth/login`
3. Click en "Try it out"
4. Ingresar:
   ```json
   {
     "email": "psantos@global-retail.com",
     "password": "tu_contraseña"
   }
   ```
5. Click en "Execute"
6. **Verificar respuesta:**
   ```json
   {
     "message": "ok",
     "userName": "Pedro Santos",    ✅ Debe aparecer
     "userEmail": "psantos@global-retail.com",  ✅ Debe aparecer
     "userRole": "Admin"            ✅ Debe aparecer
   }
   ```

### 2. Probar desde el Desktop

1. Abrir la aplicación desktop
2. Hacer login con `psantos@global-retail.com`
3. **Revisar logs:**
   ```
   [INFO] 📝 Guardando información de usuario:
   [INFO]    • UserName (API): Pedro Santos → Guardado: Pedro Santos  ✅
   [INFO]    • UserEmail (API): psantos@global-retail.com → Guardado: psantos@...  ✅
   [INFO]    • UserRole (API): Admin → Guardado: Admin  ✅
   ```
4. **Verificar banner:**
   ```
   👤 Pedro Santos • Admin
      psantos@global-retail.com
   ```

---

## ⚠️ **Casos Especiales**

### Si tu modelo de usuario NO tiene campo `Name`:

```csharp
// Opción 1: Solo UserName
string userName = user.UserName ?? user.Email?.Split('@')[0] ?? "Usuario";

// Opción 2: Capitalizar el UserName
string userName = CapitalizeName(user.UserName ?? user.Email?.Split('@')[0] ?? "Usuario");

// Helper para capitalizar
private string CapitalizeName(string text)
{
    if (string.IsNullOrEmpty(text)) return text;
    
    var words = text.Split(' ');
    for (int i = 0; i < words.Length; i++)
    {
        if (words[i].Length > 0)
        {
            words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
        }
    }
    return string.Join(" ", words);
}
```

### Si el usuario tiene múltiples roles:

```csharp
// Opción 1: Tomar el primero
var role = roles.FirstOrDefault() ?? "Usuario";

// Opción 2: Concatenar todos
var role = roles.Any() ? string.Join(", ", roles) : "Usuario";

// Opción 3: Priorizar por jerarquía
var role = roles.Contains("Admin") ? "Admin"
         : roles.Contains("Técnico") ? "Técnico"
         : roles.Contains("Supervisor") ? "Supervisor"
         : "Usuario";
```

---

## 📝 **Checklist de Modificación**

- [ ] Localizar archivo `AuthController.cs`
- [ ] Encontrar método `Login`
- [ ] Agregar código para obtener roles: `var roles = await _userManager.GetRolesAsync(user);`
- [ ] Agregar código para extraer nombre de usuario
- [ ] Modificar el `return Ok(...)` para incluir los 3 campos nuevos
- [ ] Compilar el backend
- [ ] Probar en Swagger
- [ ] Verificar que los 3 campos aparecen en la respuesta
- [ ] Probar desde el cliente desktop
- [ ] Verificar logs del cliente
- [ ] Verificar banner del cliente

---

## 🚀 **Resultado Final en el Cliente**

Una vez que modifiques el backend, el cliente desktop automáticamente:

1. ✅ Recibirá los datos en el login
2. ✅ Los guardará en LocalSettings
3. ✅ Los mostrará en el banner correctamente:
   ```
   👤 Pedro Santos • Admin
      psantos@global-retail.com
   ```

**Sin necesidad de modificar nada en el cliente** - ya está 100% preparado para recibirlos.

---

## 📞 **¿Necesitas Ayuda?**

### Si tu modelo de usuario es diferente:

Comparte tu clase de usuario y te doy el código exacto:

```csharp
public class ApplicationUser : IdentityUser
{
    // ¿Qué propiedades tiene tu clase?
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    // ... etc
}
```

### Si no usas ASP.NET Identity:

Comparte cómo manejas usuarios y roles en tu backend.

---

## ⏱️ **Tiempo Estimado**

- **Modificación del código:** 2-3 minutos
- **Compilación:** 30 segundos
- **Testing:** 2 minutos
- **Total:** ⏱️ **5 minutos**

---

**Estado:** 📝 Guía lista para implementar  
**Prioridad:** 🔴 CRÍTICA - Necesario para funcionamiento correcto del cliente  
**Dificultad:** 🟢 Baja - Solo agregar 3 líneas y modificar el return
