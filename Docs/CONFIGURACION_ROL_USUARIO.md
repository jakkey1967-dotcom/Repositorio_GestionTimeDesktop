# ?? Configuración del Rol de Usuario

**Fecha:** 2025-01-XX  
**Estado:** ? Cliente implementado, esperando backend  
**Problema:** El rol del usuario no se muestra en el banner

---

## ?? **Diagnóstico del Problema**

### Síntomas
- ? El banner se muestra correctamente
- ? El nombre del usuario aparece
- ? El email aparece
- ? El **rol** NO aparece (se muestra "Usuario" por defecto)

### Causa
El backend **NO está devolviendo** la información del usuario (`userName`, `userEmail`, `userRole`) en la respuesta del login.

---

## ??? **Solución Implementada (Cliente)**

El cliente ahora tiene **2 estrategias** para obtener la información del usuario:

### Estrategia 1: Desde el LoginResponse (Preferida)
```json
POST /api/v1/auth/login
Response:
{
  "accessToken": "eyJhbGc...",
  "userName": "Francisco",
  "userEmail": "francisco@empresa.com",
  "userRole": "Admin",
  "message": "ok"
}
```

### Estrategia 2: Desde /api/v1/users/me (Fallback)
Si el login NO devuelve `userName` o `userRole`, el cliente automáticamente hace una llamada adicional:

```json
GET /api/v1/users/me
Response:
{
  "id": 123,
  "name": "Francisco",
  "email": "francisco@empresa.com",
  "role": "Admin"
}
```

---

## ?? **Configuración del Backend**

### Opción 1: Modificar el LoginResponse (Recomendado)

**Ventaja:** Una sola llamada HTTP, más eficiente

#### C# / ASP.NET Core Ejemplo

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
    
    // Generar token JWT
    var token = GenerateJwtToken(user);
    
    // Obtener rol del usuario
    var roles = await _userManager.GetRolesAsync(user);
    var role = roles.FirstOrDefault() ?? "Usuario";
    
    return Ok(new
    {
        accessToken = token,
        userName = user.Name ?? user.UserName,  // ?? NUEVO
        userEmail = user.Email,                  // ?? NUEVO
        userRole = role,                         // ?? NUEVO
        message = "ok"
    });
}
```

---

### Opción 2: Crear endpoint /api/v1/users/me

**Ventaja:** Separación de responsabilidades, más flexible

#### C# / ASP.NET Core Ejemplo

```csharp
[HttpGet("me")]
[Authorize] // Requiere autenticación
public async Task<IActionResult> GetCurrentUser()
{
    // Obtener userId del token JWT
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userId))
    {
        return Unauthorized();
    }
    
    var user = await _userManager.FindByIdAsync(userId);
    
    if (user == null)
    {
        return NotFound();
    }
    
    // Obtener roles
    var roles = await _userManager.GetRolesAsync(user);
    var role = roles.FirstOrDefault() ?? "Usuario";
    
    return Ok(new
    {
        id = user.Id,
        name = user.Name ?? user.UserName,
        email = user.Email,
        role = role
    });
}
```

**Nota:** Este endpoint debe estar en el controlador `/api/v1/users` (UsersController)

---

## ?? **Logs del Cliente**

### Si el Login devuelve los datos:
```
[INFO] LoginAsync iniciado para francisco@empresa.com
[INFO] ?? Guardando información de usuario:
[INFO]    • UserName (API): Francisco ? Guardado: Francisco
[INFO]    • UserEmail (API): francisco@empresa.com ? Guardado: francisco@empresa.com
[INFO]    • UserRole (API): Admin ? Guardado: Admin
[INFO] Navegación a DiarioPage completada ?
```

### Si el Login NO devuelve los datos (Fallback):
```
[INFO] LoginAsync iniciado para francisco@empresa.com
[INFO] ?? LoginResponse no incluye userName/userRole, intentando obtener de /api/v1/users/me...
[INFO] HTTP GET /api/v1/users/me
[INFO] ? Información de usuario obtenida de /api/v1/users/me
[INFO]    • Name: Francisco
[INFO]    • Email: francisco@empresa.com
[INFO]    • Role: Admin
[INFO] ?? Guardando información de usuario:
[INFO]    • UserName (API): (null) ? Guardado: Francisco
[INFO]    • UserEmail (API): (null) ? Guardado: francisco@empresa.com
[INFO]    • UserRole (API): (null) ? Guardado: Admin
```

### Si ambos fallan:
```
[INFO] LoginAsync iniciado para francisco@empresa.com
[INFO] ?? LoginResponse no incluye userName/userRole, intentando obtener de /api/v1/users/me...
[WARN] ?? No se pudo obtener info de usuario desde /api/v1/users/me, usando defaults
[INFO] ?? Guardando información de usuario:
[INFO]    • UserName (API): (null) ? Guardado: francisco@empresa.com
[INFO]    • UserEmail (API): (null) ? Guardado: francisco@empresa.com
[INFO]    • UserRole (API): (null) ? Guardado: Usuario
```

---

## ?? **Testing**

### Test 1: Verificar LoginResponse actual

1. Hacer login en la app
2. Revisar los logs
3. Buscar la línea: `UserName (API): ... ? Guardado: ...`

**Si ves:**
```
UserName (API): (null) ? Guardado: francisco@empresa.com
UserRole (API): (null) ? Guardado: Usuario
```

Entonces el backend **NO** está devolviendo los campos en el login.

---

### Test 2: Verificar endpoint /api/v1/users/me

**Opción A: Usar Swagger**
1. Abrir Swagger del backend
2. Buscar endpoint `GET /api/v1/users/me`
3. Hacer login para obtener el token
4. Usar el token en "Authorize"
5. Ejecutar `GET /api/v1/users/me`

**Respuesta esperada:**
```json
{
  "id": 123,
  "name": "Francisco",
  "email": "francisco@empresa.com",
  "role": "Admin"
}
```

**Opción B: Usar la aplicación**
1. Hacer login
2. Revisar los logs
3. Buscar: `? Información de usuario obtenida de /api/v1/users/me`

Si ves esto, el endpoint existe y funciona.

---

### Test 3: Verificar que el rol se muestra en el banner

1. Hacer login
2. Ir a DiarioPage
3. Revisar el banner superior
4. Debería mostrar: **"Francisco • Admin"**

Si solo muestra **"Francisco • Usuario"**, entonces el backend no está devolviendo el rol.

---

## ?? **Solución Rápida (Sin modificar backend)**

Si **NO puedes modificar el backend** ahora, puedes hardcodear el rol temporalmente:

**Archivo:** `Views/LoginPage.xaml.cs`

```csharp
// En lugar de:
var userRole = res.UserRole ?? "Usuario";

// Usar (temporal):
var userRole = email.Contains("admin") ? "Admin" : "Usuario";
```

**Nota:** Esto es solo una solución temporal para desarrollo.

---

## ?? **Comparación de Opciones**

| Opción | Ventajas | Desventajas | Llamadas HTTP |
|--------|----------|-------------|---------------|
| Opción 1: LoginResponse | ? Más eficiente<br>? Una sola llamada<br>? Más rápido | ? Requiere modificar login | 1 |
| Opción 2: /api/v1/users/me | ? Separación de responsabilidades<br>? Más flexible<br>? No modifica login | ? Dos llamadas HTTP<br>? Más lento | 2 |
| Hardcodeado (temporal) | ? No requiere backend<br>? Inmediato | ? No es real<br>? Solo para desarrollo | 1 |

---

## ? **Checklist**

### Cliente Desktop ?
- [x] LoginResponse con campos opcionales (userName, userEmail, userRole)
- [x] Fallback a /api/v1/users/me si login no devuelve datos
- [x] Logs detallados de qué datos se reciben
- [x] Guardado en LocalSettings
- [x] Carga desde LocalSettings en DiarioPage
- [x] Mostrar en el banner: "Nombre • Rol"

### Backend ? (Pendiente)
- [ ] **Opción 1:** Modificar LoginResponse para incluir userName, userEmail, userRole
- [ ] **Opción 2:** Crear endpoint GET /api/v1/users/me
- [ ] Verificar que el rol del usuario esté disponible
- [ ] Probar con Swagger
- [ ] Probar desde la aplicación

---

## ?? **Endpoints Requeridos**

### Endpoint 1: Login (Modificado)
```
POST /api/v1/auth/login
Body: { "email": "...", "password": "..." }
Response: {
  "accessToken": "...",
  "userName": "Francisco",      // ?? AGREGAR
  "userEmail": "francisco@...",  // ?? AGREGAR
  "userRole": "Admin",           // ?? AGREGAR
  "message": "ok"
}
```

### Endpoint 2: Información del Usuario (Nuevo)
```
GET /api/v1/users/me
Authorization: Bearer {token}
Response: {
  "id": 123,
  "name": "Francisco",
  "email": "francisco@empresa.com",
  "role": "Admin"
}
```

---

## ?? **Siguiente Paso**

1. **Ejecutar la aplicación** y revisar los logs
2. **Identificar** cuál es el problema:
   - ¿El login no devuelve los campos?
   - ¿El endpoint /api/v1/users/me no existe?
   - ¿Ambos?
3. **Implementar** la solución correspondiente en el backend
4. **Probar** de nuevo

---

## ?? **Notas Importantes**

- El cliente está **100% preparado** para recibir los datos del backend
- Los **logs te dirán exactamente** qué está pasando
- Si el backend no está listo, el cliente usará **valores por defecto** ("Usuario")
- La aplicación **funcionará** incluso sin el rol, solo no se mostrará

---

**Estado:** ? Cliente listo, esperando backend  
**Prioridad:** ?? Alta (afecta UX del banner)  
**Esfuerzo:** ?? Bajo (5-15 minutos de backend)
