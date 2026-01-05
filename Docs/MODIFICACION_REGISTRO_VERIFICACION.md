# ?? MODIFICACIÓN DE REGISTRO Y VERIFICACIÓN DE EMAIL

## ? CAMBIOS REALIZADOS

### **1. `VerifyEmailRequest.cs` - MODIFICADO**

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Contracts\Auth\VerifyEmailRequest.cs`

**ANTES:**
```csharp
public record VerifyEmailRequest(
    string Email,
    string Token,
    string FullName,
    string Password,
    string? Empresa
);
```

**AHORA:**
```csharp
public record VerifyEmailRequest(
    string Email,
    string Token
);
```

? **Ya modificado automáticamente**

---

### **2. `AuthController.cs` - CAMBIOS NECESARIOS**

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs`

---

#### **MÉTODO `/register` - MODIFICAR COMPLETAMENTE**

**Buscar este método completo y REEMPLAZARLO:**

```csharp
[HttpPost("register")]
[AllowAnonymous]
public async Task<IActionResult> Register(
    [FromBody] RegisterRequest req,
    [FromServices] Services.ResetTokenService tokenSvc,
    [FromServices] Services.IEmailService emailSvc)
{
    var email = (req.Email ?? "").Trim().ToLowerInvariant();
    var fullName = (req.FullName ?? "").Trim();
    var password = req.Password ?? "";
    var empresa = req.Empresa;

    logger.LogInformation("Solicitud de registro para {Email}", email);

    // Validaciones
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(password))
    {
        return BadRequest(new RegisterResponse(false, null, "Todos los campos son requeridos."));
    }

    var existingUser = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

    if (existingUser != null)
    {
        logger.LogWarning("Email ya registrado: {Email}", email);
        return BadRequest(new RegisterResponse(false, null, "El email ya está registrado."));
    }

    try
    {
        // ? CREAR USUARIO INMEDIATAMENTE (EmailConfirmed = false)
        var newUser = new GestionTime.Domain.Auth.User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            Enabled = true,
            EmailConfirmed = false  // ? Usuario creado pero no verificado
        };

        db.Users.Add(newUser);

        // Asignar rol de usuario
        var userRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == "User");
        if (userRole != null)
        {
            db.UserRoles.Add(new GestionTime.Domain.Auth.UserRole
            {
                UserId = newUser.Id,
                RoleId = userRole.Id
            });
        }

        await db.SaveChangesAsync();

        // ? GENERAR TOKEN DE VERIFICACIÓN
        var token = tokenSvc.GenerateToken();
        tokenSvc.SaveToken($"verify:{email}", token);

        logger.LogInformation("Usuario creado (sin verificar): {Email}, código: {Token}", email, token);

        // Enviar email
        try
        {
            await emailSvc.SendRegistrationEmailAsync(email, token);
            logger.LogInformation("Email de verificación enviado");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enviando email (usuario ya creado)");
            // No falla el registro si el email falla
        }

        return Ok(new RegisterResponse(true, "Registro exitoso. Revisa tu email para verificar tu cuenta.", null));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creando usuario");
        return StatusCode(500, new RegisterResponse(false, null, "Error al crear el usuario."));
    }
}
```

---

#### **MÉTODO `/verify-email` - MODIFICAR COMPLETAMENTE**

**Buscar este método completo y REEMPLAZARLO:**

```csharp
[HttpPost("verify-email")]
[AllowAnonymous]
public async Task<IActionResult> VerifyEmail(
    [FromBody] VerifyEmailRequest req,
    [FromServices] Services.ResetTokenService tokenSvc)
{
    var token = (req.Token ?? "").Trim();
    var email = (req.Email ?? "").Trim().ToLowerInvariant();

    logger.LogInformation("Verificación de email: {Email}", email);

    // ? VALIDAR TOKEN
    var storedToken = tokenSvc.GetToken($"verify:{email}");

    if (storedToken == null || storedToken != token)
    {
        logger.LogWarning("Token inválido o expirado para {Email}", email);
        return BadRequest(new { success = false, message = "Código inválido o expirado." });
    }

    // ? BUSCAR USUARIO
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

    if (user == null)
    {
        logger.LogWarning("Usuario no encontrado: {Email}", email);
        return NotFound(new { success = false, message = "Usuario no encontrado." });
    }

    if (user.EmailConfirmed)
    {
        logger.LogInformation("Email ya verificado: {Email}", email);
        return Ok(new { success = true, message = "Email ya verificado." });
    }

    try
    {
        // ? MARCAR EMAIL COMO VERIFICADO
        user.EmailConfirmed = true;
        await db.SaveChangesAsync();

        // Eliminar token usado
        tokenSvc.RemoveToken($"verify:{email}");

        logger.LogInformation("Email verificado exitosamente: {Email}", email);

        return Ok(new { success = true, message = "Email verificado exitosamente. Ya puedes iniciar sesión." });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error verificando email");
        return StatusCode(500, new { success = false, message = "Error al verificar email." });
    }
}
```

---

## ?? RESUMEN DE CAMBIOS

### **Flujo ANTES (incorrecto):**
```
1. POST /register ? Solo guarda datos temporales + envía código
2. POST /verify-email ? Crea usuario + verifica (requiere todos los datos)
```

### **Flujo AHORA (correcto):**
```
1. POST /register ? Crea usuario (EmailConfirmed = false) + envía código
2. POST /verify-email ? Solo marca EmailConfirmed = true (requiere email + código)
```

---

## ? CAMBIOS EN ARCHIVOS

| Archivo | Cambio | Estado |
|---------|--------|--------|
| `Contracts/Auth/VerifyEmailRequest.cs` | Solo email + token | ? Modificado |
| `Controllers/AuthController.cs` | `/register` ? Crea usuario | ? Pendiente |
| `Controllers/AuthController.cs` | `/verify-email` ? Solo verifica | ? Pendiente |

---

## ?? INSTRUCCIONES

### **PASO 1: Abrir `AuthController.cs`**
```
C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs
```

### **PASO 2: Buscar método `[HttpPost("register")]`**

Reemplazar TODO el método con el código de arriba

### **PASO 3: Buscar método `[HttpPost("verify-email")]`**

Reemplazar TODO el método con el código de arriba

### **PASO 4: Verificar que el modelo User tiene `EmailConfirmed`**

Si no existe, agregar:
```csharp
public bool EmailConfirmed { get; set; }
```

### **PASO 5: Reiniciar backend**

### **PASO 6: Probar**

1. **Registro:**
```json
POST /api/v1/auth/register
{
  "email": "test@example.com",
  "fullName": "Test User",
  "password": "Test1234",
  "empresa": ""
}
```

**Respuesta:** `"Registro exitoso. Revisa tu email para verificar tu cuenta."`

2. **Verificación:**
```json
POST /api/v1/auth/verify-email
{
  "email": "test@example.com",
  "token": "123456"
}
```

**Respuesta:** `"Email verificado exitosamente. Ya puedes iniciar sesión."`

3. **Login:**
```json
POST /api/v1/auth/login
{
  "email": "test@example.com",
  "password": "Test1234"
}
```

**Respuesta:** Token JWT

---

## ?? IMPORTANTE

**Si `RequireConfirmedEmail` está en `false`:**
- El usuario puede hacer login sin verificar
- Para producción, debe estar en `true`

**Si `RequireConfirmedEmail` está en `true`:**
- El usuario NO puede hacer login sin verificar
- Debe verificar el email primero

---

## ?? TESTING

### **Test 1: Registro**
```sh
# Debería crear usuario + enviar email
POST /register con fullName, email, password
```

### **Test 2: Verificación**
```sh
# Debería marcar EmailConfirmed = true
POST /verify-email con email + token
```

### **Test 3: Login sin verificar (si RequireConfirmedEmail = true)**
```sh
# Debería rechazar
POST /login ? "Email no verificado"
```

### **Test 4: Login después de verificar**
```sh
# Debería funcionar
POST /login ? Token JWT
```

---

**¿Necesitas ayuda aplicando estos cambios?** ??
