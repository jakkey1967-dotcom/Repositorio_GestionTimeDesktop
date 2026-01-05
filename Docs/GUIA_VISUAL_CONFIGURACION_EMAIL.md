# ?? GUÍA VISUAL: DÓNDE PONER TU CONFIGURACIÓN DE EMAIL

## ?? ARCHIVO A EDITAR

```
C:\GestionTime\src\GestionTime.Api\appsettings.json
```

---

## ?? CONTENIDO ACTUAL (ANTES)

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
  }
}
```

---

## ? CONTENIDO NUEVO (DESPUÉS)

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
    "SmtpHost": "AQUI_TU_SERVIDOR",
    "SmtpPort": "587",
    "SmtpUser": "AQUI_TU_EMAIL",
    "SmtpPassword": "AQUI_TU_CONTRASEÑA",
    "From": "AQUI_TU_EMAIL",
    "FromName": "GestionTime"
  }
}
```

**Observa que agregué una coma (`,`) después de `ConnectionStrings` y luego la nueva sección `Email`**

---

## ?? EJEMPLO CON GMAIL

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
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUser": "miusuario@gmail.com",
    "SmtpPassword": "abcd efgh ijkl mnop",
    "From": "miusuario@gmail.com",
    "FromName": "GestionTime"
  }
}
```

---

## ?? EJEMPLO CON MAILTRAP (TESTING)

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
    "SmtpHost": "smtp.mailtrap.io",
    "SmtpPort": "587",
    "SmtpUser": "1a2b3c4d5e6f7g",
    "SmtpPassword": "9h8i7j6k5l4m3n",
    "From": "noreply@gestiontime.com",
    "FromName": "GestionTime"
  }
}
```

---

## ?? PASO 2: MODIFICAR PROGRAM.CS

### **Archivo:**
```
C:\GestionTime\src\GestionTime.Api\Program.cs
```

### **Buscar la línea (aproximadamente línea 97):**

```csharp
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();
```

### **Cambiar por:**

```csharp
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.SmtpEmailService>();
```

---

## ? RESUMEN

### **1. Editar `appsettings.json`:**
- ? Agregar coma (`,`) después de `ConnectionStrings`
- ? Agregar sección `Email` con tus credenciales

### **2. Editar `Program.cs`:**
- ? Cambiar `FakeEmailService` por `SmtpEmailService` (línea 97)

### **3. Reiniciar backend**

### **4. Probar registro**

---

## ?? ¿DÓNDE CONSIGO LAS CREDENCIALES?

### **Gmail:**
1. https://myaccount.google.com/security
2. Habilitar "Verificación en 2 pasos"
3. "Contraseñas de aplicaciones" ? Generar para "Correo"
4. Usar esa contraseña (NO tu contraseña de Gmail normal)

### **Mailtrap (para testing):**
1. https://mailtrap.io/ ? Registrarse (gratis)
2. "Email Testing" ? "My Inbox"
3. Copiar credenciales SMTP

### **Outlook:**
1. Usar tu email y contraseña normal de Outlook
2. `SmtpHost`: `smtp-mail.outlook.com`

---

**¡Listo! Solo copia y pega según tu proveedor y cambia una línea en `Program.cs`!** ??
