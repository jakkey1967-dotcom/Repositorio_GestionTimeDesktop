# ?? CONFIGURACIÓN FINAL PARA TU SERVIDOR IONOS

## ?? DATOS DE TU SERVIDOR

- **Email:** `envio_noreplica@tdkportal.com`
- **Servidor SMTP:** `smtp.ionos.es`
- **Puerto:** `587` (TLS)
- **Autenticación:** Requerida

---

## ? PASO 1: ACTUALIZAR `appsettings.json`

### **Archivo:** `C:\GestionTime\src\GestionTime.Api\appsettings.json`

### **Contenido completo (COPIAR Y PEGAR):**

```json
{
  "Jwt": {
    "Issuer": "GestionTime",
    "Audience": "GestionTime.Web",
    "Key": "v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e",
    "AccessMinutes": 15,
    "RefreshDays": 14
  },
  "Cors": {
    "Origins": [
      "https://localhost:5173",
      "http://localhost:5173",
      "https://localhost:2501",
      "http://localhost:2500"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "GestionTime": {
      "EnableDebugLogs": true,
      "LogDirectory": "logs",
      "MaxFileSizeMB": 10,
      "RetainedFileCountLimit": 5
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=gestiontime;Username=postgres;Password=postgres"
  },
  
  "Email": {
    "SmtpHost": "smtp.ionos.es",
    "SmtpPort": "587",
    "SmtpUser": "envio_noreplica@tdkportal.com",
    "SmtpPassword": "AQUI_TU_CONTRASEÑA",
    "From": "envio_noreplica@tdkportal.com",
    "FromName": "GestionTime"
  }
}
```

**?? IMPORTANTE:** Reemplaza `AQUI_TU_CONTRASEÑA` con la contraseña real de tu email `envio_noreplica@tdkportal.com`

---

## ? PASO 2: MODIFICAR `Program.cs`

### **Archivo:** `C:\GestionTime\src\GestionTime.Api\Program.cs`

### **Buscar (línea ~97):**
```csharp
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();
```

### **Cambiar por:**
```csharp
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.SmtpEmailService>();
```

---

## ? PASO 3: REINICIAR BACKEND

1. Detener el backend
2. Iniciar de nuevo
3. Los logs deberían mostrar: `?? Enviando email a...`

---

## ?? PASO 4: PROBAR

1. Abrir aplicación desktop
2. Ir a Registro
3. Registrar usuario con un email real tuyo
4. Deberías recibir el email en tu bandeja

---

## ?? RESUMEN DE CONFIGURACIÓN

| Campo | Valor |
|-------|-------|
| **Servidor SMTP** | smtp.ionos.es |
| **Puerto** | 587 (TLS) |
| **Usuario** | envio_noreplica@tdkportal.com |
| **Contraseña** | ? **TÚ LA PONES** |
| **De (From)** | envio_noreplica@tdkportal.com |
| **Nombre** | GestionTime |

---

## ? CHECKLIST

- [ ] 1. Copiar contenido completo a `appsettings.json`
- [ ] 2. Reemplazar `AQUI_TU_CONTRASEÑA` con la contraseña real
- [ ] 3. Cambiar `FakeEmailService` por `SmtpEmailService` en `Program.cs` (línea 97)
- [ ] 4. Reiniciar backend
- [ ] 5. Probar registro con tu email personal
- [ ] 6. Revisar email recibido

---

## ?? SI HAY ERRORES

**Error: "Authentication failed"**
? Verifica que la contraseña sea correcta
? Verifica que el email `envio_noreplica@tdkportal.com` esté activo

**Error: "SMTP server requires a secure connection"**
? Ya está configurado con TLS (puerto 587), debería funcionar

**No recibo el email**
? Revisa spam/correo no deseado
? Revisa logs del backend para ver errores específicos
? Verifica que el email de destino sea válido

---

**¡Con estos datos ya casi está listo! Solo falta que pongas la contraseña en `appsettings.json` y cambies una línea en `Program.cs`!** ??

---

**Fecha:** 2025-12-26 20:00:00  
**Email:** envio_noreplica@tdkportal.com  
**Servidor:** smtp.ionos.es  
**Puerto:** 587 (TLS)  
**Estado:** ? Pendiente contraseña
