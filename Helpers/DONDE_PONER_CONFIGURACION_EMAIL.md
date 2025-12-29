# ?? CONFIGURACIÓN FINAL - DÓNDE PONER TUS CREDENCIALES

## ? ARCHIVOS YA CREADOS/MODIFICADOS

He creado automáticamente:
- ? `C:\GestionTime\src\GestionTime.Api\Services\SmtpEmailService.cs`

---

## ?? PASO 1: CONFIGURAR TU CORREO

### **Abrir archivo:**
```
C:\GestionTime\src\GestionTime.Api\appsettings.json
```

### **Agregar al final (antes del último `}`):**

```json
{
  "Jwt": {
    // ... configuración existente ...
  },
  "Cors": {
    // ... configuración existente ...
  },
  "Logging": {
    // ... configuración existente ...
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    // ... configuración existente ...
  },
  
  // ? AGREGAR ESTA SECCIÓN AQUÍ ???
  "Email": {
    "SmtpHost": "AQUI_TU_SERVIDOR_SMTP",
    "SmtpPort": "587",
    "SmtpUser": "AQUI_TU_EMAIL",
    "SmtpPassword": "AQUI_TU_CONTRASEÑA",
    "From": "AQUI_TU_EMAIL",
    "FromName": "GestionTime"
  }
}
```

---

## ?? EJEMPLOS SEGÚN TU PROVEEDOR

### **OPCIÓN A: GMAIL**

Si tienes Gmail, primero debes:
1. Ir a: https://myaccount.google.com/security
2. Habilitar "Verificación en 2 pasos"
3. Ir a "Contraseñas de aplicaciones"
4. Generar contraseña para "Correo"
5. Copiar la contraseña de 16 caracteres

Luego configurar así:

```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "tu-email@gmail.com",
  "SmtpPassword": "xxxx xxxx xxxx xxxx",
  "From": "tu-email@gmail.com",
  "FromName": "GestionTime"
}
```

---

### **OPCIÓN B: OUTLOOK/HOTMAIL**

```json
"Email": {
  "SmtpHost": "smtp-mail.outlook.com",
  "SmtpPort": "587",
  "SmtpUser": "tu-email@outlook.com",
  "SmtpPassword": "TU_CONTRASEÑA",
  "From": "tu-email@outlook.com",
  "FromName": "GestionTime"
}
```

---

### **OPCIÓN C: MAILTRAP (PARA TESTING)**

Crear cuenta gratis en: https://mailtrap.io/

```json
"Email": {
  "SmtpHost": "smtp.mailtrap.io",
  "SmtpPort": "587",
  "SmtpUser": "TU_USERNAME_MAILTRAP",
  "SmtpPassword": "TU_PASSWORD_MAILTRAP",
  "From": "noreply@gestiontime.com",
  "FromName": "GestionTime"
}
```

---

### **OPCIÓN D: OTRO PROVEEDOR**

Si tienes otro servidor SMTP (tu empresa, otro proveedor):

```json
"Email": {
  "SmtpHost": "smtp.tuservidor.com",
  "SmtpPort": "587",
  "SmtpUser": "tu-usuario",
  "SmtpPassword": "tu-contraseña",
  "From": "noreply@tudominio.com",
  "FromName": "GestionTime"
}
```

---

## ?? PASO 2: MODIFICAR PROGRAM.CS

### **Abrir archivo:**
```
C:\GestionTime\src\GestionTime.Api\Program.cs
```

### **Buscar la línea (aproximadamente línea 97):**

```csharp
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.FakeEmailService>();
```

### **CAMBIAR POR:**

```csharp
builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.SmtpEmailService>();
```

**Es decir, cambiar `FakeEmailService` por `SmtpEmailService`**

---

## ?? PASO 3: REINICIAR EL BACKEND

1. Detener el backend (si está corriendo)
2. Iniciar de nuevo
3. ¡Listo!

---

## ?? PASO 4: PROBAR

1. **Abrir tu aplicación desktop**
2. **Ir a Registro**
3. **Registrar un nuevo usuario**
4. **Revisar:**
   - **Gmail/Outlook:** Revisa tu bandeja de entrada
   - **Mailtrap:** Revisa tu inbox en https://mailtrap.io/

Deberías recibir un email con el código de verificación.

---

## ? ¿QUÉ OPCIÓN USAR?

| Situación | Opción Recomendada |
|-----------|-------------------|
| **Solo quiero testear rápido** | Mailtrap |
| **Tengo Gmail personal** | Gmail |
| **Tengo Outlook personal** | Outlook |
| **Mi empresa tiene servidor SMTP** | Configuración personalizada |

---

## ?? IMPORTANTE: SEGURIDAD

**NO subas `appsettings.json` al repositorio con tus credenciales reales.**

Para desarrollo local está bien, pero para producción usa:
- **Variables de entorno**
- **Azure Key Vault**
- **User Secrets** (desarrollo)

---

## ?? RESUMEN DE CAMBIOS

| Archivo | Acción | Estado |
|---------|--------|--------|
| `Services/SmtpEmailService.cs` | Creado | ? Listo |
| `appsettings.json` | **TÚ EDITAS** | ? Pendiente |
| `Program.cs` (línea 97) | **TÚ CAMBIAS** | ? Pendiente |

---

## ? CHECKLIST

- [ ] 1. Editar `appsettings.json` con tus credenciales
- [ ] 2. Cambiar `FakeEmailService` por `SmtpEmailService` en `Program.cs`
- [ ] 3. Reiniciar backend
- [ ] 4. Probar registro
- [ ] 5. Revisar email recibido

---

## ?? SI TIENES PROBLEMAS

**Error: "Configuración de email incompleta"**
? Revisa que `appsettings.json` tenga todos los campos: `SmtpHost`, `SmtpUser`, `SmtpPassword`

**Error: "Authentication failed"**
? Gmail: Asegúrate de usar la contraseña de aplicación, NO tu contraseña normal
? Otros: Verifica usuario y contraseña

**No recibo el email**
? Revisa spam
? Revisa logs del backend para ver errores
? Si usas Mailtrap, revisa el inbox en su web

---

**¡Ya tienes todo listo! Solo falta que agregues tus credenciales en `appsettings.json` y cambies una línea en `Program.cs`!** ??
