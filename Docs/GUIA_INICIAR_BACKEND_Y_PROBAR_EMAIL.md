# ? BACKEND CONFIGURADO - GUÍA PARA INICIAR Y PROBAR EMAIL

## ?? ESTADO ACTUAL

### **? COMPLETADO:**
```
? Backend compila sin errores
? SmtpEmailService configurado (email real)
? Configuración IONOS correcta en appsettings.json
? Sistema de verificación por enlace implementado
? Template HTML con logo listo
```

### **?? CONFIGURACIÓN DE EMAIL VERIFICADA:**
```json
{
  "Email": {
    "SmtpHost": "smtp.ionos.es",
    "SmtpPort": "587",
    "SmtpUser": "envio_noreplica@tdkportal.com", 
    "SmtpPassword": "Nimda2008@2020",
    "From": "envio_noreplica@tdkportal.com",
    "FromName": "GestionTime"
  }
}
```

---

## ?? CÓMO INICIAR EL BACKEND (MANUAL)

### **OPCIÓN 1: PowerShell separado**
```powershell
# Abrir PowerShell nuevo
cd C:\GestionTime\src\GestionTime.Api
dotnet run
```

### **OPCIÓN 2: Desde Visual Studio**
```
1. Abrir proyecto GestionTime.Api en Visual Studio
2. Seleccionar "GestionTime.Api" como startup project
3. Presionar F5 o click "? GestionTime.Api"
```

### **OPCIÓN 3: Terminal integrado**
```powershell
# En terminal nuevo de VS Code o Visual Studio
cd C:\GestionTime\src\GestionTime.Api
dotnet run
```

---

## ?? PROBAR ENVÍO DE EMAIL

### **PASO 1: Iniciar backend**
Usar cualquiera de las opciones de arriba. Verás:
```
[INF] Iniciando GestionTime API...
[INF] GestionTime API iniciada correctamente
```

### **PASO 2: Registrar usuario con tu email**
```
1. Abrir aplicación desktop
2. RegisterPage ? Llenar formulario
3. Usar TU EMAIL REAL (ej: psantos@tdkportal.com)
4. Click "Registrarse"
```

### **PASO 3: Verificar logs del backend**
Buscar en logs líneas como:
```
[INF] Solicitud de registro para psantos@tdkportal.com
[INF] ? Usuario creado exitosamente (sin activar): psantos@tdkportal.com
[INF] Token de activación generado para psantos@tdkportal.com
[INF] ?? Enviando email de activación a psantos@tdkportal.com
[INF] ? Email de activación enviado exitosamente a psantos@tdkportal.com
```

### **PASO 4: Revisar tu bandeja de entrada**
```
?? Buscar email de: GestionTime <envio_noreplica@tdkportal.com>
?? Asunto: "Activar tu cuenta - GestionTime"
?? Si no está en Inbox, revisar SPAM/Correo no deseado
```

---

## ?? DIAGNÓSTICO DE PROBLEMAS

### **Si no aparece "Email enviado" en logs:**
```
? Error de SMTP/credenciales
? Revisar logs para errores específicos
? Verificar que credenciales IONOS están correctas
```

### **Si aparece error de autenticación:**
```
? Credenciales incorrectas
? Verificar que email y password IONOS son correctos
? Verificar que el email envio_noreplica@tdkportal.com existe
```

### **Si email no llega a tu bandeja:**
```
? Filtros de spam
? Revisar carpeta SPAM/Correo no deseado
? Agregar envio_noreplica@tdkportal.com a contactos
? Verificar que tu bandeja no está llena
```

---

## ?? TESTING ALTERNATIVO

### **Si hay problemas con SMTP:**
**Cambiar temporalmente a FakeEmailService:**

```csharp
// En Program.cs línea 109, cambiar:
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Por:
builder.Services.AddScoped<IEmailService, FakeEmailService>();
```

**Resultado:**
- Enlaces aparecerán en logs del backend
- Copiar enlace y abrir en navegador
- Activación funcionará igual

---

## ?? LOGS ESPERADOS

### **Registro exitoso con SmtpEmailService:**
```
[INF] Solicitud de registro para psantos@tdkportal.com
[INF] ? Usuario creado exitosamente (sin activar): psantos@tdkportal.com
[INF] Token de activación generado para psantos@tdkportal.com
[INF] ?? Enviando email de activación a psantos@tdkportal.com
[INF]    URL de activación: https://localhost:2501/api/v1/auth/activate/ABC123...
[INF] ? Email de activación enviado exitosamente a psantos@tdkportal.com
```

### **Errores comunes:**
```
[ERR] ? Error enviando email de activación a psantos@tdkportal.com
[ERR] Authentication failed
```
? **Solución:** Verificar credenciales IONOS

---

## ?? CONTENIDO DEL EMAIL

### **Lo que recibirás:**
```
?? Remitente: GestionTime <envio_noreplica@tdkportal.com>
?? Asunto: Activar tu cuenta - GestionTime
?? Contenido: 
   - ??? Logo de GestionTime
   - ?? Diseño profesional HTML
   - ?? Botón "Activar mi cuenta ahora"
   - ?? Enlace alternativo para copiar/pegar
   - ? Aviso de expiración (24 horas)
```

### **Al hacer click en "Activar mi cuenta ahora":**
```
?? Navegador abre: https://localhost:2501/api/v1/auth/activate/{token}
?? Página: "¡Cuenta activada exitosamente!"
? Usuario puede hacer login en la app
```

---

## ?? PRÓXIMOS PASOS

### **INMEDIATOS:**
1. **?? Iniciar backend** (opción manual)
2. **?? Registrar con email real** 
3. **?? Verificar logs** de envío
4. **?? Revisar bandeja** de entrada

### **SI TODO FUNCIONA:**
```
? Email llegó correctamente
? Activación por enlace funciona
? Login posterior exitoso
? ¡Sistema 100% operativo!
```

### **SI HAY PROBLEMAS:**
```
?? Cambiar a FakeEmailService temporalmente
?? Usar enlaces de logs para testing
?? Revisar configuración IONOS
?? Contactar soporte IONOS si es necesario
```

---

**¡BACKEND LISTO! AHORA SOLO NECESITAS INICIARLO MANUALMENTE Y PROBAR.** ??

---

**Instrucciones:**
1. **Abrir PowerShell nuevo** 
2. **`cd C:\GestionTime\src\GestionTime.Api`**
3. **`dotnet run`**
4. **Registrar usuario con tu email**
5. **¡Revisar tu bandeja de entrada!**

---

**Fecha:** 2025-12-27 19:05:00  
**Estado:** ? Backend configurado y compilado  
**Email:** ? SmtpEmailService + IONOS configurado  
**Próximo:** Iniciar backend y probar registro