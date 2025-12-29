# ?? Problema: Endpoint de Reset Password

**Fecha:** 2024-12-24  
**Estado:** ?? Pendiente de implementación en Backend  

---

## ?? Problema Identificado

### Error Actual
```
HTTP POST /api/v1/admin/users/{userId}/reset-password ? 401 Unauthorized
```

### Causa
El endpoint que intentaste usar en Swagger es un **endpoint de administrador** que:
1. ? Requiere autenticación (token de admin)
2. ? Requiere el `userId` en la ruta
3. ? **NO es público** - no se puede usar sin login

### ¿Qué falta?
Tu API backend **NO tiene un endpoint público** para reset de contraseña. El flujo de "Olvidé mi contraseña" necesita un endpoint que:
- ? **NO requiera autenticación** (porque el usuario no puede hacer login)
- ? Reciba email + nueva contraseña
- ? Valide el email y actualice la contraseña

---

## ?? Solución: Crear Endpoint Público en Backend

### Opción 1: Endpoint Público Directo (?? Inseguro pero simple)

**Endpoint a crear:**
```
POST /api/v1/auth/reset-password
```

**Request:**
```json
{
  "email": "usuario@dominio.com",
  "newPassword": "nueva_contraseña"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Contraseña actualizada correctamente",
  "error": null
}
```

**?? Advertencia de Seguridad:**
Este enfoque es **MUY INSEGURO** porque:
- Cualquiera puede cambiar la contraseña de cualquier usuario solo con el email
- No hay verificación de identidad
- Es vulnerable a ataques de fuerza bruta

### Opción 2: Endpoint con Token de Verificación (? Recomendado)

**Flujo correcto:**

1. **Paso 1: Solicitar reset**
```
POST /api/v1/auth/forgot-password
Body: { "email": "usuario@dominio.com" }
? Genera token único y envía email
```

2. **Paso 2: Confirmar con token**
```
POST /api/v1/auth/reset-password
Body: { "token": "abc123...", "newPassword": "nueva_contraseña" }
? Valida token y actualiza contraseña
```

**Ventajas:**
- ? Seguro: requiere acceso al email del usuario
- ? Token expirable (ej: 1 hora)
- ? Token de un solo uso
- ? Previene ataques

---

## ?? Implementación Backend Necesaria

### C# / ASP.NET Core Ejemplo (Opción 1 - Simple pero insegura)

```csharp
[HttpPost("reset-password")]
[AllowAnonymous] // Permite acceso sin autenticación
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    
    if (user == null)
    {
        // Por seguridad, no revelar si el email existe
        return Ok(new { success = true, message = "Si el email existe, la contraseña ha sido actualizada." });
    }
    
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
    
    if (result.Succeeded)
    {
        return Ok(new { success = true, message = "Contraseña actualizada correctamente." });
    }
    
    return BadRequest(new { success = false, error = "Error al actualizar contraseña." });
}
```

### C# / ASP.NET Core Ejemplo (Opción 2 - Segura con token)

```csharp
[HttpPost("forgot-password")]
[AllowAnonymous]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    
    if (user == null)
    {
        // Por seguridad, no revelar si el email existe
        return Ok(new { success = true, message = "Si el email existe, recibirás instrucciones." });
    }
    
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    
    // Enviar email con link: https://app.com/reset-password?token={token}
    await _emailService.SendPasswordResetEmail(user.Email, token);
    
    return Ok(new { success = true, message = "Instrucciones enviadas al correo." });
}

[HttpPost("reset-password")]
[AllowAnonymous]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithTokenRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    
    if (user == null)
    {
        return BadRequest(new { success = false, error = "Token inválido o expirado." });
    }
    
    var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
    
    if (result.Succeeded)
    {
        return Ok(new { success = true, message = "Contraseña actualizada correctamente." });
    }
    
    return BadRequest(new { success = false, error = "Token inválido o contraseña no válida." });
}
```

---

## ?? Estado Actual del Cliente Desktop

### ? Ya implementado:
- Formulario de recuperación de contraseña
- Validaciones de email y contraseña
- UI con temas oscuro/claro
- ScrollViewer para pantallas pequeñas

### ? Pendiente (Backend):
- **Crear endpoint público** `/api/v1/auth/reset-password`
- Implementar lógica de validación
- (Opcional pero recomendado) Sistema de tokens de verificación

### ?? Cambio necesario en el cliente:
Una vez que el backend tenga el endpoint correcto, actualizar:

**Archivo:** `Views/ForgotPasswordPage.xaml.cs`  
**Línea:** ~221

```csharp
// Cambiar de:
var result = await App.Api.PostAsync<ForgotPasswordRequest, ForgotPasswordResponse>("/api/v1/auth/forgot-password", payload);

// A (si usas opción 1):
var result = await App.Api.PostAsync<ForgotPasswordRequest, ForgotPasswordResponse>("/api/v1/auth/reset-password", payload);

// O (si usas opción 2 con token):
// Implementar flujo de dos pasos
```

---

## ?? Resumen

| Aspecto | Estado | Acción |
|---------|--------|--------|
| Cliente Desktop | ? Listo | Esperando backend |
| Endpoint Público | ? Falta | Crear en backend |
| Seguridad | ?? Débil | Implementar tokens |
| Funcionalidad | ?? Parcial | Completar backend |

---

## ?? Próximos Pasos

1. **Implementar endpoint en backend** (elegir Opción 1 o 2)
2. **Probar con Swagger** que el endpoint funciona sin autenticación
3. **Actualizar el cliente** si es necesario (cambiar la ruta)
4. **Validar flujo completo** desde la aplicación desktop

---

## ?? Diferencia entre Endpoints

| Endpoint | Requiere Auth | Para qué sirve |
|----------|---------------|----------------|
| `/api/v1/admin/users/{id}/reset-password` | ? Sí (Admin) | Administrador resetea contraseña de cualquier usuario |
| `/api/v1/auth/reset-password` | ? No | Usuario resetea su propia contraseña (sin login) |
| `/api/v1/auth/forgot-password` | ? No | Solicita token de reset (envía email) |

---

**?? IMPORTANTE:** El enfoque actual (cambiar contraseña sin verificación) es **inseguro**. Para producción, debes implementar el flujo con tokens de verificación por email (Opción 2).
