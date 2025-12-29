# ?? CONFIGURACIÓN DE EMAIL EN EL BACKEND

## ?? ESTADO ACTUAL

**Proyecto Backend:** `C:\GestionTime\src\GestionTime.Api`

**Archivos encontrados:**
- ? `Services/IEmailService.cs` - Interfaz del servicio
- ? `Services/FakeEmailService.cs` - Implementación fake (solo logs)
- ? `Program.cs` - Línea 97: `builder.Services.AddScoped<IEmailService, FakeEmailService>();`

**Configuración actual:**
```csharp
// Program.cs línea 97
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, 
                          GestionTime.Api.Services.FakeEmailService>();
```

**Problema:** FakeEmailService solo muestra el código en logs, no envía emails reales.

---

## ? OPCIONES DE CONFIGURACIÓN

### ?? COMPARACIÓN RÁPIDA

| Opción | Tiempo | Complejidad | Recomendada |
|--------|--------|-------------|-------------|
| **1. Deshabituji verificación** | 1 min | ?? Muy fácil | Para desarrollo |
| **2. Mailtrap (testing)** | 10 min | ?? Fácil | Para testing |
| **3. Gmail SMTP** | 15 min | ?? Media | Para pruebas personales |
| **4. SendGrid** | 15 min | ?? Media | Para producción |

---

## ?? OPCIÓN 1: DESHABILITAR VERIFICACIÓN (MÁS RÁPIDO - 1 MIN)

**Para:** Desarrollo rápido, no necesitas verificación de email ahora

### **Paso 1: Modificar `Program.cs`**

**Buscar** (línea ~90-110):
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        // ... configuración JWT ...
    });
```

**Agregar DESPUÉS** de `AddAuthentication`:
```csharp
// Configurar Identity Options
builder.Services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(options =>
{
    // ? DESHABILITAR verificación de email (SOLO DESARROLLO)
    options.SignIn.RequireConfirmedEmail = false;
    
    // Configuración de contraseña (opcional)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
});
```

### **Resultado:**
- ? Registro funciona sin verificar email
- ? Login funciona inmediatamente
- ? No necesitas activar manualmente en la BD
- ?? Solo para desarrollo

---

## ?? OPCIÓN 2: MAILTRAP (PARA TESTING - 10 MIN)

**Para:** Ver los emails en un inbox fake, perfecto para desarrollo

### **Paso 1: Crear cuenta en Mailtrap**
1. Ir a: https://mailtrap.io/
2. Registrarse (gratis)
3. Ir a "Email Testing" ? "My Inbox"
4. Copiar credenciales SMTP:
   ```
   Host: smtp.mailtrap.io
   Port: 587
   Username: xxxxxxxxxxxxx
   Password: xxxxxxxxxxxxx
   ```

### **Paso 2: Crear `Services/SmtpEmailService.cs`**

```csharp
using System.Net;
using System.Net.Mail;

namespace GestionTime.Api.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Recuperación de Contraseña - GestionTime";
        var htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Recuperación de Contraseña</h2>
                <p>Tu código de verificación es:</p>
                <div style='background: #f0f0f0; padding: 15px; border-left: 4px solid #0B8C99;'>
                    <strong style='font-size: 18px;'>{resetToken}</strong>
                </div>
                <p><small>Este código expira en 1 hora.</small></p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendRegistrationEmailAsync(string toEmail, string verificationToken)
    {
        var subject = "Verificación de Email - GestionTime";
        var htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Verificación de Registro</h2>
                <p>Tu código de verificación es:</p>
                <div style='background: #f0f0f0; padding: 15px; border-left: 4px solid #0B8C99;'>
                    <strong style='font-size: 18px;'>{verificationToken}</strong>
                </div>
                <p><small>Este código expira en 24 horas.</small></p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.mailtrap.io";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:SmtpUser"];
            var smtpPass = _config["Email:SmtpPassword"];
            var fromEmail = _config["Email:From"] ?? "noreply@gestiontime.com";

            _logger.LogInformation("?? Enviando email a {Email}", toEmail);

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "GestionTime"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("? Email enviado exitosamente a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error enviando email a {Email}", toEmail);
            throw;
        }
    }
}
```

### **Paso 3: Actualizar `appsettings.json`**

Agregar al final:
```json
{
  // ... configuración existente ...
  
  "Email": {
    "SmtpHost": "smtp.mailtrap.io",
    "SmtpPort": "587",
    "SmtpUser": "TU_USERNAME_MAILTRAP",
    "SmtpPassword": "TU_PASSWORD_MAILTRAP",
    "From": "noreply@gestiontime.com"
  }
}
```

### **Paso 4: Cambiar en `Program.cs` (línea 97)**

**ANTES:**
```csharp
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, 
                          GestionTime.Api.Services.FakeEmailService>();
```

**DESPUÉS:**
```csharp
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, 
                          GestionTime.Api.Services.SmtpEmailService>();
```

### **Paso 5: Reiniciar backend**

### **Resultado:**
- ? Emails aparecen en tu inbox de Mailtrap
- ? Puedes ver el código de verificación
- ? Perfecto para testing

---

## ?? OPCIÓN 3: GMAIL SMTP (15 MIN)

**Para:** Usar tu email personal de Gmail

### **Paso 1: Configurar Gmail**
1. Ir a: https://myaccount.google.com/security
2. Habilitar "Verificación en 2 pasos"
3. Ir a "Contraseñas de aplicaciones"
4. Generar contraseña para "Correo"
5. Copiar la contraseña de 16 caracteres

### **Paso 2: Usar el mismo `SmtpEmailService.cs` de la Opción 2**

### **Paso 3: Actualizar `appsettings.json`**

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUser": "tu-email@gmail.com",
    "SmtpPassword": "xxxx xxxx xxxx xxxx",
    "From": "tu-email@gmail.com"
  }
}
```

### **Paso 4: Cambiar en `Program.cs` (igual que Opción 2)**

### **Resultado:**
- ? Emails reales enviados desde tu Gmail
- ? Límite: ~500 emails/día
- ?? Puede caer en spam

---

## ?? OPCIÓN 4: SENDGRID (15 MIN)

**Para:** Producción profesional

### **Paso 1: Crear cuenta**
1. Ir a: https://sendgrid.com/
2. Registrarse (gratis: 100 emails/día)
3. Settings ? API Keys
4. Crear API Key
5. Copiar la key (empieza con `SG.`)

### **Paso 2: Instalar NuGet package**

```sh
cd C:\GestionTime\src\GestionTime.Api
dotnet add package SendGrid
```

### **Paso 3: Crear `Services/SendGridEmailService.cs`**

```csharp
using SendGrid;
using SendGrid.Helpers.Mail;

namespace GestionTime.Api.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Recuperación de Contraseña - GestionTime";
        var htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Recuperación de Contraseña</h2>
                <p>Tu código: <strong>{resetToken}</strong></p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendRegistrationEmailAsync(string toEmail, string verificationToken)
    {
        var subject = "Verificación de Email - GestionTime";
        var htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Verificación de Registro</h2>
                <p>Tu código: <strong>{verificationToken}</strong></p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var apiKey = _config["Email:SendGridApiKey"];
            var fromEmail = _config["Email:From"] ?? "noreply@gestiontime.com";

            _logger.LogInformation("?? Enviando email vía SendGrid a {Email}", toEmail);

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, "GestionTime");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("? Email enviado exitosamente");
            }
            else
            {
                _logger.LogError("? SendGrid error: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error enviando email");
            throw;
        }
    }
}
```

### **Paso 4: Actualizar `appsettings.json`**

```json
{
  "Email": {
    "SendGridApiKey": "SG.xxxxxxxxxxxxxxxxxxxxx",
    "From": "noreply@gestiontime.com"
  }
}
```

### **Paso 5: Cambiar en `Program.cs`**

```csharp
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, 
                          GestionTime.Api.Services.SendGridEmailService>();
```

### **Resultado:**
- ? 100 emails/día gratis
- ? Profesional
- ? Alta deliverability

---

## ?? RESUMEN DE PASOS

### **Para AHORA (más rápido):**

1. **Abrir:** `C:\GestionTime\src\GestionTime.Api\Program.cs`
2. **Buscar línea ~90-110** (después de `AddAuthentication`)
3. **Agregar:**
   ```csharp
   builder.Services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(options =>
   {
       options.SignIn.RequireConfirmedEmail = false; // ? DESHABILITAR verificación
   });
   ```
4. **Reiniciar backend**
5. **Probar registro + login**

---

### **Para DESPUÉS (email real):**

1. **Crear cuenta en Mailtrap** (o Gmail/SendGrid)
2. **Crear `SmtpEmailService.cs`** en `Services/`
3. **Agregar configuración** en `appsettings.json`
4. **Cambiar línea 97** de `Program.cs`:
   ```csharp
   builder.Services.AddScoped<IEmailService, SmtpEmailService>();
   ```
5. **Reiniciar backend**

---

## ? RECOMENDACIÓN FINAL

**PARA AHORA:**
? **Opción 1** (deshabilitar verificación) - 1 minuto

**PARA TESTING:**
? **Opción 2** (Mailtrap) - 10 minutos

**PARA PRODUCCIÓN:**
? **Opción 4** (SendGrid) - 15 minutos

---

**¿Qué opción quieres que te ayude a implementar?** ??

---

**Fecha:** 2025-12-26 19:45:00  
**Backend:** C:\GestionTime\src\GestionTime.Api  
**Estado Actual:** FakeEmailService (solo logs)  
**Opciones:** 4 opciones documentadas  
**Tiempo mínimo:** 1 minuto (deshabilitar verificación)  
**Archivos clave:** Program.cs (línea 97), appsettings.json, Services/
