# ?? Implementación de Envío de Emails para Reset Password

**Fecha:** 2024-12-24  
**Estado:** ?? Guía de Implementación  

---

## ?? ¿Cómo Enviar Emails desde el Backend?

Para implementar el flujo seguro de reset password (Opción 2), necesitas un servicio de envío de emails en tu backend.

---

## ?? Opciones para Enviar Emails

### Opción 1: SMTP (Simple Mail Transfer Protocol) ? Recomendado

Usa un servidor SMTP para enviar correos. Puedes usar:
- **Gmail** (gratis, con límites)
- **SendGrid** (gratis hasta 100 emails/día)
- **Mailgun** (gratis hasta 5,000 emails/mes)
- **Amazon SES** (muy barato, $0.10 por 1,000 emails)
- **Office 365 / Outlook SMTP**
- **Tu propio servidor SMTP**

### Opción 2: APIs de Servicios de Email

Servicios especializados con APIs REST:
- **SendGrid API**
- **Mailgun API**
- **Postmark**
- **Mailjet**
- **Amazon SES API**

---

## ?? Implementación en ASP.NET Core

### 1?? Instalación de Paquetes NuGet

```bash
# Para SMTP básico (incluido en .NET)
# No necesitas instalar nada adicional

# O para servicios avanzados:
dotnet add package SendGrid
# O
dotnet add package Mailgun.Api
```

---

### 2?? Configuración en appsettings.json

```json
{
  "EmailSettings": {
    // Opción A: SMTP (Gmail ejemplo)
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "tu-email@gmail.com",
    "SmtpPassword": "tu-app-password",
    "FromEmail": "noreply@gestiontime.com",
    "FromName": "GestionTime",
    
    // Opción B: SendGrid
    "SendGridApiKey": "SG.xxxxxxxxxxxxxxxxxxxxx",
    
    // Opción C: Mailgun
    "MailgunApiKey": "key-xxxxxxxxxxxxxxxxxxxxx",
    "MailgunDomain": "mg.tudominio.com"
  }
}
```

**?? IMPORTANTE:** Nunca subas las contraseñas/keys al repositorio. Usa:
- **User Secrets** para desarrollo
- **Variables de entorno** para producción
- **Azure Key Vault** o **AWS Secrets Manager** para mayor seguridad

---

### 3?? Servicio de Email - IEmailService

```csharp
// Services/IEmailService.cs
public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}
```

---

### 4?? Implementación con SMTP (Opción más simple)

```csharp
// Services/SmtpEmailService.cs
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
        
        _smtpServer = _config["EmailSettings:SmtpServer"];
        _smtpPort = int.Parse(_config["EmailSettings:SmtpPort"]);
        _smtpUsername = _config["EmailSettings:SmtpUsername"];
        _smtpPassword = _config["EmailSettings:SmtpPassword"];
        _fromEmail = _config["EmailSettings:FromEmail"];
        _fromName = _config["EmailSettings:FromName"];
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Recuperación de Contraseña - GestionTime";
        
        var htmlBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #098ca3; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .button {{ 
                        display: inline-block; 
                        padding: 12px 24px; 
                        background-color: #098ca3; 
                        color: white; 
                        text-decoration: none; 
                        border-radius: 5px; 
                        margin: 20px 0;
                    }}
                    .token {{ 
                        background-color: #e9ecef; 
                        padding: 10px; 
                        border-left: 4px solid #098ca3; 
                        margin: 15px 0;
                        word-break: break-all;
                    }}
                    .footer {{ 
                        text-align: center; 
                        padding: 20px; 
                        font-size: 12px; 
                        color: #666; 
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>?? Recuperación de Contraseña</h1>
                    </div>
                    <div class='content'>
                        <p>Hola,</p>
                        <p>Recibimos una solicitud para restablecer tu contraseña en <strong>GestionTime</strong>.</p>
                        
                        <p>Tu código de verificación es:</p>
                        <div class='token'>
                            <strong>{resetToken}</strong>
                        </div>
                        
                        <p>Este código expira en <strong>1 hora</strong>.</p>
                        
                        <p><strong>?? Si no solicitaste este cambio, ignora este correo.</strong></p>
                        
                        <p>Saludos,<br/>El equipo de GestionTime</p>
                    </div>
                    <div class='footer'>
                        <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
                        <p>&copy; 2024 GestionTime. Todos los derechos reservados.</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "¡Bienvenido a GestionTime!";
        
        var htmlBody = $@"
            <!DOCTYPE html>
            <html>
            <body>
                <h2>¡Bienvenido, {userName}!</h2>
                <p>Tu cuenta en GestionTime ha sido creada exitosamente.</p>
                <p>Ya puedes comenzar a gestionar tu tiempo de manera eficiente.</p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            _logger.LogInformation("?? Enviando email a {email} - Asunto: {subject}", toEmail, subject);

            using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("? Email enviado exitosamente a {email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error enviando email a {email}", toEmail);
            throw;
        }
    }
}
```

---

### 5?? Implementación con SendGrid (Opción profesional)

```csharp
// Services/SendGridEmailService.cs
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger)
    {
        _config = config;
        _logger = logger;
        _apiKey = _config["EmailSettings:SendGridApiKey"];
        _fromEmail = _config["EmailSettings:FromEmail"];
        _fromName = _config["EmailSettings:FromName"];
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Recuperación de Contraseña - GestionTime";
        var htmlBody = GenerateResetPasswordHtml(resetToken);
        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "¡Bienvenido a GestionTime!";
        var htmlBody = GenerateWelcomeHtml(userName);
        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            _logger.LogInformation("?? Enviando email via SendGrid a {email}", toEmail);

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("? Email enviado exitosamente a {email}", toEmail);
            }
            else
            {
                _logger.LogError("? Error SendGrid: {status}", response.StatusCode);
                throw new Exception($"SendGrid error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error enviando email a {email}", toEmail);
            throw;
        }
    }

    private string GenerateResetPasswordHtml(string token)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #098ca3;'>?? Recuperación de Contraseña</h2>
                    <p>Tu código de verificación es:</p>
                    <div style='background: #f0f0f0; padding: 15px; border-left: 4px solid #098ca3;'>
                        <strong style='font-size: 18px;'>{token}</strong>
                    </div>
                    <p><small>Este código expira en 1 hora.</small></p>
                </div>
            </body>
            </html>
        ";
    }

    private string GenerateWelcomeHtml(string userName)
    {
        return $@"
            <html>
            <body>
                <h2>¡Bienvenido, {userName}!</h2>
                <p>Tu cuenta ha sido creada exitosamente.</p>
            </body>
            </html>
        ";
    }
}
```

---

### 6?? Registrar el Servicio en Program.cs

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Opción A: SMTP
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Opción B: SendGrid
// builder.Services.AddScoped<IEmailService, SendGridEmailService>();

var app = builder.Build();
```

---

### 7?? Uso en el Controller

```csharp
// Controllers/AuthController.cs
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        
        if (user == null)
        {
            // Por seguridad, siempre devolver éxito aunque el usuario no exista
            return Ok(new { 
                success = true, 
                message = "Si el email existe, recibirás un código de verificación." 
            });
        }

        // Generar token de 6 dígitos (más simple que un GUID largo)
        var token = GenerateSimpleToken();
        
        // Guardar token en caché o base de datos con expiración
        await SaveResetTokenAsync(user.Id, token, TimeSpan.FromHours(1));
        
        // Enviar email
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, token);
            
            _logger.LogInformation("? Token de reset enviado a {email}", request.Email);
            
            return Ok(new { 
                success = true, 
                message = "Código de verificación enviado a tu correo." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error enviando email a {email}", request.Email);
            
            return StatusCode(500, new { 
                success = false, 
                error = "Error al enviar el correo. Intenta nuevamente." 
            });
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        // Validar token
        var userId = await ValidateResetTokenAsync(request.Token);
        
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { 
                success = false, 
                error = "Código inválido o expirado." 
            });
        }

        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return BadRequest(new { 
                success = false, 
                error = "Usuario no encontrado." 
            });
        }

        // Resetear contraseña
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (result.Succeeded)
        {
            // Eliminar token usado
            await DeleteResetTokenAsync(request.Token);
            
            _logger.LogInformation("? Contraseña reseteada para {email}", user.Email);
            
            return Ok(new { 
                success = true, 
                message = "Contraseña actualizada correctamente." 
            });
        }

        return BadRequest(new { 
            success = false, 
            error = "Error al actualizar la contraseña." 
        });
    }

    // Helper: Generar token simple de 6 dígitos
    private string GenerateSimpleToken()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    // Helper: Guardar token (puedes usar Redis, caché de memoria, o base de datos)
    private async Task SaveResetTokenAsync(string userId, string token, TimeSpan expiration)
    {
        // Implementación con caché de memoria o Redis
        // Ejemplo simplificado:
        // await _cache.SetStringAsync($"reset:{token}", userId, new DistributedCacheEntryOptions 
        // { 
        //     AbsoluteExpirationRelativeToNow = expiration 
        // });
    }

    private async Task<string?> ValidateResetTokenAsync(string token)
    {
        // Validar y obtener userId del token
        // return await _cache.GetStringAsync($"reset:{token}");
        return null; // Implementar según tu solución de caché
    }

    private async Task DeleteResetTokenAsync(string token)
    {
        // Eliminar token usado
        // await _cache.RemoveAsync($"reset:{token}");
    }
}
```

---

## ?? Configuración de Gmail (Para pruebas)

### Paso 1: Habilitar "Contraseñas de aplicación"

1. Ve a tu cuenta de Google: https://myaccount.google.com/
2. Ve a **Seguridad**
3. Habilita **Verificación en 2 pasos**
4. Luego ve a **Contraseñas de aplicaciones**
5. Genera una contraseña para "Correo" ? "Windows Computer"
6. **Usa esa contraseña en tu appsettings.json**, NO tu contraseña de Gmail

### Paso 2: appsettings.json

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "tu-email@gmail.com",
    "SmtpPassword": "xxxx xxxx xxxx xxxx",  // ? Contraseña de aplicación generada
    "FromEmail": "tu-email@gmail.com",
    "FromName": "GestionTime"
  }
}
```

---

## ? Configuración de SendGrid (Recomendado para producción)

### Paso 1: Crear cuenta gratuita

1. Ve a https://sendgrid.com/
2. Crea una cuenta (gratis: 100 emails/día)
3. Verifica tu email
4. Ve a **Settings ? API Keys**
5. Crea una API Key con permisos de envío

### Paso 2: Verificar dominio (opcional pero recomendado)

1. Ve a **Settings ? Sender Authentication**
2. Verifica tu dominio o email individual

### Paso 3: appsettings.json

```json
{
  "EmailSettings": {
    "SendGridApiKey": "SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "FromEmail": "noreply@tudominio.com",
    "FromName": "GestionTime"
  }
}
```

---

## ?? Testing / Desarrollo

Para pruebas sin enviar emails reales:

```csharp
// Services/FakeEmailService.cs (para testing)
public class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _logger;

    public FakeEmailService(ILogger<FakeEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        _logger.LogWarning("?? [FAKE EMAIL] Reset token para {email}: {token}", toEmail, resetToken);
        Console.WriteLine($"===== EMAIL DE PRUEBA =====");
        Console.WriteLine($"Para: {toEmail}");
        Console.WriteLine($"Asunto: Recuperación de Contraseña");
        Console.WriteLine($"Token: {resetToken}");
        Console.WriteLine($"===========================");
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        _logger.LogWarning("?? [FAKE EMAIL] Bienvenida para {email}", toEmail);
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        _logger.LogWarning("?? [FAKE EMAIL] a {email} - {subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
```

En `Program.cs` (desarrollo):

```csharp
#if DEBUG
    builder.Services.AddScoped<IEmailService, FakeEmailService>();
#else
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
#endif
```

---

## ?? Comparación de Opciones

| Servicio | Costo | Emails Gratis | Facilidad | Recomendación |
|----------|-------|---------------|-----------|---------------|
| **Gmail SMTP** | Gratis | ~500/día | ???? Fácil | ? Desarrollo/Pruebas |
| **SendGrid** | Gratis/Pago | 100/día | ????? Muy fácil | ? Producción pequeña |
| **Mailgun** | Gratis/Pago | 5,000/mes | ???? Fácil | ? Producción mediana |
| **Amazon SES** | Muy barato | 62,000/mes* | ??? Medio | ? Producción grande |
| **Office 365** | $5-12/mes | Ilimitado | ??? Medio | ? Empresas con O365 |

*Si usas EC2

---

## ?? Checklist de Implementación

- [ ] Elegir servicio de email (Gmail/SendGrid/otro)
- [ ] Crear cuenta y obtener credenciales
- [ ] Instalar paquetes NuGet si es necesario
- [ ] Crear `IEmailService` y su implementación
- [ ] Configurar `appsettings.json` (usar User Secrets)
- [ ] Registrar servicio en `Program.cs`
- [ ] Crear template HTML del email
- [ ] Implementar generación de tokens
- [ ] Implementar almacenamiento de tokens (caché/BD)
- [ ] Implementar validación de tokens
- [ ] Agregar logging
- [ ] Probar envío de emails
- [ ] Implementar manejo de errores
- [ ] Configurar variables de entorno para producción

---

## ?? Seguridad

1. **Nunca envíes contraseñas por email** (solo tokens)
2. **Tokens con expiración** (1 hora recomendado)
3. **Tokens de un solo uso** (eliminar después de usar)
4. **No reveles si el email existe** (siempre responder éxito)
5. **Rate limiting** (limitar intentos por IP)
6. **HTTPS obligatorio** en producción
7. **User Secrets** o variables de entorno para credenciales

---

**? Resultado Final:**

El usuario recibe un email como este:

```
????????????????????????????????
?? Recuperación de Contraseña
????????????????????????????????

Hola,

Tu código de verificación es:

??????????????????
?   456789       ?
??????????????????

Este código expira en 1 hora.

?? Si no solicitaste este cambio,
   ignora este correo.

????????????????????????????????
© 2024 GestionTime
```
