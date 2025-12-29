# ?? SOLUCIÓN AL ERROR DE INICIO DEL BACKEND

## ? PROBLEMA

El backend falla al iniciar con error:
```
[FTL] : La aplicación falló al iniciar
Microsoft.Extensions.Hosting.HostAbortedException: The host was aborted.
at Program.<Main>$(String[] args) in C:\GestionTime\src\GestionTime.Api\Program.cs:line 96
```

---

## ?? CAUSA

El error ocurre en la línea 96 de `Program.cs` (donde se hace `builder.Build()`), lo que indica que hay un problema al inicializar los servicios.

**Posibles causas:**
1. `SmtpEmailService` requiere configuración que no está disponible
2. Error en el constructor de `SmtpEmailService`
3. Dependencia faltante

---

## ? SOLUCIÓN INMEDIATA

### **Opción 1: Volver a `FakeEmailService` temporalmente**

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Program.cs`

**Línea 94, cambiar:**

```csharp
// ANTES (causa error)
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.SmtpEmailService>();

// DESPUÉS (funciona)
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();
```

**Reiniciar backend** ? Debería arrancar sin problemas

---

### **Opción 2: Hacer `SmtpEmailService` más robusto**

El problema puede ser que `SmtpEmailService` falla si la configuración no está completa.

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Services\SmtpEmailService.cs`

**Reemplazar el constructor:**

```csharp
public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
{
    _config = config ?? throw new ArgumentNullException(nameof(config));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    
    // ? VALIDAR que la configuración existe (no que sea válida)
    _logger.LogInformation("SmtpEmailService inicializado");
}
```

**Y modificar el método `SendEmailAsync` para NO fallar si falta config:**

```csharp
private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
{
    var smtpHost = _config["Email:SmtpHost"];
    var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
    var smtpUser = _config["Email:SmtpUser"];
    var smtpPass = _config["Email:SmtpPassword"];

    // ? SI FALTA CONFIGURACIÓN, SIMULAR ENVÍO
    if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
    {
        _logger.LogWarning("?? [SIMULADO] Email a {Email} - Falta configuración SMTP", toEmail);
        _logger.LogWarning("Código de verificación: {Code}", htmlBody.Contains("</strong>") 
            ? System.Text.RegularExpressions.Regex.Match(htmlBody, @"<strong[^>]*>([^<]+)</strong>").Groups[1].Value 
            : "N/A");
        return; // ? NO FALLA, solo loguea
    }

    try
    {
        _logger.LogInformation("?? Enviando email a {Email}", toEmail);

        using var smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            Timeout = 10000
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_config["Email:From"] ?? "noreply@gestiontime.com", 
                                  _config["Email:FromName"] ?? "GestionTime"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);

        _logger.LogInformation("? Email enviado exitosamente");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "? Error enviando email");
        throw;
    }
}
```

---

## ?? PASOS PARA RESOLVER

### **PASO 1: Volver a FakeEmailService**

```csharp
// Program.cs línea 94
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, 
                          GestionTime.Api.Services.FakeEmailService>();
```

### **PASO 2: Reiniciar backend**

Debería arrancar sin problemas.

### **PASO 3: Verificar que funciona**

- Registrar un usuario
- Deberías ver el código en los logs

### **PASO 4: (Opcional) Probar SmtpEmailService después**

Una vez que el backend arranque correctamente con `FakeEmailService`, podemos depurar por qué falla con `SmtpEmailService`.

---

## ?? DEBUG AVANZADO

Si quieres usar `SmtpEmailService`, agrega más logs:

```csharp
public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
{
    _logger = logger;
    _config = config;
    
    _logger.LogInformation("=== SmtpEmailService Constructor ===");
    _logger.LogInformation("SmtpHost: {Host}", config["Email:SmtpHost"] ?? "NULL");
    _logger.LogInformation("SmtpPort: {Port}", config["Email:SmtpPort"] ?? "NULL");
    _logger.LogInformation("SmtpUser: {User}", config["Email:SmtpUser"] ?? "NULL");
    _logger.LogInformation("SmtpPassword: {HasPass}", string.IsNullOrEmpty(config["Email:SmtpPassword"]) ? "NO" : "YES");
    _logger.LogInformation("=====================================");
}
```

Esto te dirá si la configuración se está leyendo correctamente.

---

## ? RESUMEN

**PARA ARRANCAR EL BACKEND AHORA:**
1. Cambiar `SmtpEmailService` por `FakeEmailService` (línea 94 de Program.cs)
2. Reiniciar backend
3. Debería funcionar

**PARA DEPURAR SmtpEmailService:**
1. Agregar logs en el constructor
2. Ver qué configuración se está leyendo
3. Identificar el problema específico

---

**¿El backend arranca ahora con `FakeEmailService`?** ??
