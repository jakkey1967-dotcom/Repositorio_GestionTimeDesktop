# ?? PROBLEMA CRÍTICO: USUARIO NO SE CREA EN LA BASE DE DATOS

## ? PROBLEMA DETECTADO

**El usuario NO se está creando en la base de datos durante el registro.**

### **Flujo actual del backend:**

```
1. POST /register ? Solo guarda datos temporalmente + envía código
2. POST /verify-email ? CREA el usuario en la BD

? El usuario NO existe hasta que verifica el email
? Por eso el login falla con "Credenciales inválidas"
```

---

## ?? EVIDENCIA

### **En `AuthController.cs` método `/register`:**

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register(...)
{
    // ? NO CREA EL USUARIO
    var tempData = new { Email, FullName, Password, Empresa };
    tokenSvc.SaveTokenWithData($"register:{token}", jsonData); // Solo guarda temp
    
    await emailSvc.SendRegistrationEmailAsync(email, token);
    return Ok(new RegisterResponse(true, "Código enviado a tu correo.", null));
}
```

### **En `AuthController.cs` método `/verify-email`:**

```csharp
[HttpPost("verify-email")]
public async Task<IActionResult> VerifyEmail(...)
{
    // ? AQUÍ SÍ CREA EL USUARIO
    var newUser = new User { ... };
    db.Users.Add(newUser);
    await db.SaveChangesAsync(); // ? Usuario creado AQUÍ
    
    return Ok(new RegisterResponse(true, "Registro exitoso. Ya puedes iniciar sesión.", null));
}
```

---

## ? SOLUCIONES

### **OPCIÓN 1: Crear usuario inmediatamente (RECOMENDADA)**

**Modificar `/register` para crear el usuario con `EmailConfirmed = false`:**

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs`

**Reemplazar método `/register` completo:**

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

    logger.LogInformation("Solicitud de registro para {Email}", email);

    var existingUser = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

    if (existingUser != null)
    {
        logger.LogWarning("Email ya registrado: {Email}", email);
        return BadRequest(new RegisterResponse(false, null, "El email ya está registrado."));
    }

    try
    {
        // ? CREAR USUARIO INMEDIATAMENTE
        var newUser = new GestionTime.Domain.Auth.User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            Enabled = true,
            EmailConfirmed = false  // ? Sin verificar inicialmente
        };

        db.Users.Add(newUser);

        // Asignar rol
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

        // Generar token para verificación (opcional si ya deshabilitaste RequireConfirmedEmail)
        var token = tokenSvc.GenerateToken();
        tokenSvc.SaveToken($"verify:{email}", token);

        logger.LogInformation("Usuario creado: {Email}, Código: {Token}", email, token);

        // Enviar email (no crítico)
        try
        {
            await emailSvc.SendRegistrationEmailAsync(email, token);
            logger.LogInformation("Email de verificación enviado");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enviando email (usuario ya creado)");
        }

        return Ok(new RegisterResponse(true, "Registro exitoso. Ya puedes iniciar sesión.", null));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creando usuario");
        return StatusCode(500, new RegisterResponse(false, null, "Error al crear el usuario."));
    }
}
```

**Resultado:**
- ? Usuario creado inmediatamente
- ? Login funciona sin verificar (porque deshabilitaste `RequireConfirmedEmail`)
- ? Email se envía pero no es bloqueante

---

### **OPCIÓN 2: Usar el flujo actual (NO RECOMENDADA)**

**Requiere implementar `VerifyEmailPage` en la desktop app** para que el usuario pueda ingresar el código.

**Problema:** Más complejo y ya deshabilitaste la verificación.

---

## ?? RECOMENDACIÓN

**Implementar OPCIÓN 1:**
1. Modificar `/register` para crear usuario inmediatamente
2. Mantener `RequireConfirmedEmail = false` (ya configurado)
3. Usuario puede hacer login inmediatamente

---

## ?? PASOS PARA IMPLEMENTAR

### **PASO 1: Modificar `AuthController.cs`**

1. Abrir `C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs`
2. Buscar método `[HttpPost("register")]`
3. Reemplazar TODO el método con el código de arriba

### **PASO 2: Reiniciar backend**

```sh
cd C:\GestionTime\src\GestionTime.Api
dotnet run
```

### **PASO 3: Probar registro**

**En RegisterPage:**
1. Nombre: `Usuario Test`
2. Email: `test@example.com`
3. Password: `test123`
4. Click "Registrarse"

**Esperado:**
- ? "Registro exitoso. Ya puedes iniciar sesión."
- ? Usuario creado en BD

### **PASO 4: Probar login**

**En LoginPage:**
1. Email: `test@example.com`
2. Password: `test123`
3. Click "Iniciar sesión"

**Esperado:**
- ? Login exitoso
- ? Navega a DiarioPage

---

## ?? TESTING EN POSTGRESQL

### **Verificar que el usuario se creó:**

```sql
SELECT id, email, full_name, email_confirmed, enabled 
FROM users 
WHERE email = 'test@example.com';
```

**Resultado esperado:**
```
id                                   | email             | full_name    | email_confirmed | enabled
-------------------------------------|-------------------|--------------|-----------------|--------
xxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx   | test@example.com | Usuario Test | false           | true
```

**Notas:**
- `email_confirmed = false` ? Normal, porque no verificó
- `enabled = true` ? Usuario activo
- **Login funciona** ? Porque `RequireConfirmedEmail = false`

---

## ? RESUMEN

**Problema:**
- ? Usuario no se crea hasta `/verify-email`
- ? Login falla porque usuario no existe

**Solución:**
- ? Modificar `/register` para crear usuario inmediatamente
- ? Login funciona sin verificar (ya configurado)
- ? Flujo simple y directo

---

**¿Quieres que modifique el `AuthController.cs` automáticamente?** ??

---

**Fecha:** 2025-12-26 21:10:00  
**Problema:** Usuario no se crea en registro  
**Causa:** Flujo de registro en 2 pasos  
**Solución:** Crear usuario en `/register`  
**Estado:** ? Pendiente implementación
