# ? IMPLEMENTACIÓN COMPLETA - REGISTRO Y VERIFICACIÓN DE EMAIL

## ?? CAMBIOS REALIZADOS AUTOMÁTICAMENTE

### ? **1. `VerifyEmailRequest.cs` - MODIFICADO**
- Eliminados campos innecesarios
- Solo requiere: `Email` + `Token`

### ? **2. `User.cs` - MODIFICADO**
- Agregado campo: `EmailConfirmed`

---

## ?? CAMBIOS MANUALES REQUERIDOS

### **1. Crear Migración para `EmailConfirmed`**

**Abrir terminal en:** `C:\GestionTime\src\GestionTime.Api`

```sh
dotnet ef migrations add AddEmailConfirmed --project ..\GestionTime.Infrastructure

dotnet ef database update
```

Esto agregará la columna `EmailConfirmed` a la tabla `Users`.

---

### **2. Modificar `AuthController.cs`**

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs`

#### **A. Método `/register` - REEMPLAZAR COMPLETO**

**Buscar:**
```csharp
[HttpPost("register")]
[AllowAnonymous]
public async Task<IActionResult> Register(
```

**Reemplazar TODO el método por:**

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

#### **B. Método `/verify-email` - REEMPLAZAR COMPLETO**

**Buscar:**
```csharp
[HttpPost("verify-email")]
[AllowAnonymous]
public async Task<IActionResult> VerifyEmail(
```

**Reemplazar TODO el método por:**

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

### **3. Modificar método `/login` para validar EmailConfirmed**

**Buscar en `AuthController.cs`:**

```csharp
if (user is null || !user.Enabled)
{
    logger.LogWarning("Login fallido para {Email}: usuario no encontrado o deshabilitado", email);
    return Unauthorized(new { message = "Credenciales inválidas" });
}
```

**CAMBIAR POR:**

```csharp
if (user is null || !user.Enabled)
{
    logger.LogWarning("Login fallido para {Email}: usuario no encontrado o deshabilitado", email);
    return Unauthorized(new { message = "Credenciales inválidas" });
}

// ? VALIDAR EMAIL CONFIRMADO (solo si RequireConfirmedEmail = true)
if (!user.EmailConfirmed)
{
    logger.LogWarning("Login fallido para {Email}: email no verificado", email);
    return Unauthorized(new { message = "Por favor, verifica tu email antes de iniciar sesión." });
}
```

---

## ?? CHECKLIST DE IMPLEMENTACIÓN

- [x] 1. Modificar `VerifyEmailRequest.cs` - ? Hecho
- [x] 2. Agregar `EmailConfirmed` a `User.cs` - ? Hecho
- [ ] 3. Crear migración y actualizar BD
- [ ] 4. Modificar método `/register` en `AuthController.cs`
- [ ] 5. Modificar método `/verify-email` en `AuthController.cs`
- [ ] 6. Modificar método `/login` para validar email
- [ ] 7. Reiniciar backend
- [ ] 8. Probar flujo completo

---

## ?? TESTING DESPUÉS DE LOS CAMBIOS

### **Test 1: Registro**

```json
POST https://localhost:2501/api/v1/auth/register
{
  "email": "nuevo@example.com",
  "fullName": "Usuario Nuevo",
  "password": "Test1234",
  "empresa": ""
}
```

**Esperado:**
```json
{
  "success": true,
  "message": "Registro exitoso. Revisa tu email para verificar tu cuenta."
}
```

? Usuario creado en BD con `EmailConfirmed = false`
? Email enviado con código

---

### **Test 2: Login sin verificar (debería fallar)**

```json
POST https://localhost:2501/api/v1/auth/login
{
  "email": "nuevo@example.com",
  "password": "Test1234"
}
```

**Esperado:**
```json
{
  "message": "Por favor, verifica tu email antes de iniciar sesión."
}
```

---

### **Test 3: Verificación de email**

```json
POST https://localhost:2501/api/v1/auth/verify-email
{
  "email": "nuevo@example.com",
  "token": "123456"
}
```

**Esperado:**
```json
{
  "success": true,
  "message": "Email verificado exitosamente. Ya puedes iniciar sesión."
}
```

? `EmailConfirmed = true` en BD

---

### **Test 4: Login después de verificar (debería funcionar)**

```json
POST https://localhost:2501/api/v1/auth/login
{
  "email": "nuevo@example.com",
  "password": "Test1234"
}
```

**Esperado:**
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "...",
  ...
}
```

---

## ?? IMPORTANTE: MIGRACIÓN

**Después de agregar `EmailConfirmed` al modelo, DEBES crear una migración:**

```sh
cd C:\GestionTime\src\GestionTime.Api
dotnet ef migrations add AddEmailConfirmed --project ..\GestionTime.Infrastructure
dotnet ef database update
```

Esto agregará la columna a la BD. Los usuarios existentes tendrán `EmailConfirmed = false` por defecto.

**Para activar usuarios existentes:**
```sql
UPDATE "AspNetUsers" SET "EmailConfirmed" = true WHERE "Email" = 'psantos@tdkportal.com';
```

---

## ?? RESULTADO FINAL

**Flujo CORRECTO:**
1. ? Usuario se registra ? Cuenta creada (EmailConfirmed = false) + email enviado
2. ? Usuario recibe código ? Ingresa en la app
3. ? Usuario verifica ? `EmailConfirmed = true`
4. ? Usuario hace login ? Token JWT

**Swagger:**
- ? `/register` ? Solo fullName, email, password, empresa
- ? `/verify-email` ? Solo email + token (código de 6 dígitos)

---

**¿Necesitas ayuda con algún paso específico?** ??
