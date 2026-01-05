# ?? DIAGNÓSTICO: LOGIN FUNCIONA EN SWAGGER PERO NO EN DESKTOP

## ?? ANÁLISIS DEL PROBLEMA

**SÍNTOMA:** 
- ? Login funciona desde Swagger
- ? Login falla desde aplicación desktop

**Esto indica que:**
- ? Backend está funcionando correctamente
- ? Usuario existe en la base de datos
- ? Hay un problema en la aplicación desktop o comunicación

---

## ?? POSIBLES CAUSAS

### **1. DIFERENCIA EN LA URL**
**Swagger:** `https://localhost:2501/api/v1/auth/login`
**Desktop:** `https://localhost:2501/api/v1/auth/login` (según appsettings.json)

**Verificación:** URLs coinciden ?

### **2. DIFERENCIA EN EL FORMATO DE DATOS**
**Swagger envía:**
```json
{
  "email": "psantos@tdkportal.com",
  "password": "Nimda2008@2020"
}
```

**Desktop envía:** (según LoginPage.xaml.cs)
```json
{
  "email": "psantos@tdkportal.com",
  "password": "Nimda2008@2020"
}
```

**Verificación:** Formato coincide ?

### **3. HEADERS HTTP**
**Swagger:** Envía headers estándar del navegador
**Desktop:** Puede estar enviando headers diferentes o faltantes

**Posible problema:** ? Content-Type, User-Agent, Accept headers

### **4. CERTIFICADOS SSL**
**Problema común:** Desktop no acepta certificado self-signed de `localhost:2501`

**Error típico:** `The SSL connection could not be established`

### **5. CORS**
**Backend configurado para:**
```json
"Origins": [
  "https://localhost:5173",
  "http://localhost:5173", 
  "https://localhost:2501",
  "http://localhost:2500"
]
```

**Desktop:** Viene desde aplicación nativa (no navegador)
**Posible problema:** ? CORS puede estar bloqueando

### **6. COOKIES vs TOKENS**
**Swagger:** Puede estar usando cookies para autenticación
**Desktop:** Espera JWT tokens

**Incompatibilidad:** ? Diferentes métodos de autenticación

---

## ?? PASOS DE DIAGNÓSTICO

### **PASO 1: Ver logs específicos del error**
¿Qué error exacto ves en la aplicación desktop?
- ¿Timeout?
- ¿Conexión rechazada?
- ¿Error 401/400?
- ¿Error SSL?

### **PASO 2: Verificar logs del backend**
Cuando intentas login desde desktop:
- ¿Llega la petición al backend?
- ¿Qué respuesta envía el backend?
- ¿Hay errores en los logs del backend?

### **PASO 3: Verificar conectividad básica**
¿Puedes acceder a `https://localhost:2501` desde un navegador?

---

## ? SOLUCIONES SEGÚN EL PROBLEMA

### **SOLUCIÓN 1: Problema SSL (MÁS PROBABLE)**

**SÍNTOMA:** Error de certificado SSL con localhost:2501

**SOLUCIÓN:** Deshabilitar validación SSL temporalmente

**Archivo:** `Services/ApiClient.cs` (constructor)
```csharp
// Agregar esto ANTES de crear HttpClient
var handler = new HttpClientHandler { 
    UseCookies = true, 
    CookieContainer = new CookieContainer(),
    // ? DESHABILITAR VALIDACIÓN SSL (SOLO DESARROLLO)
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
};
```

### **SOLUCIÓN 2: Cambiar a HTTP en lugar de HTTPS**

**Archivo:** `appsettings.json`
```json
{
  "Api": {
    "BaseUrl": "http://localhost:2500",  // ? HTTP en lugar de HTTPS
    "LoginPath": "/api/v1/auth/login"
  }
}
```

### **SOLUCIÓN 3: Agregar headers específicos**

**Archivo:** `Services/ApiClient.cs` (constructor)
```csharp
_http.DefaultRequestHeaders.Add("User-Agent", "GestionTime-Desktop/1.0");
_http.DefaultRequestHeaders.Add("Accept", "application/json");
```

### **SOLUCIÓN 4: CORS - Agregar origen para desktop**

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Program.cs`
```csharp
"Origins": [
  "https://localhost:5173",
  "http://localhost:5173",
  "https://localhost:2501",
  "http://localhost:2500",
  "app://localhost"  // ? Para apps desktop
]
```

---

## ?? TESTING ESPECÍFICO

### **Test 1: Verificar URL del backend**
Abre navegador ? `https://localhost:2501/api/v1/health`
**Esperado:** ? Respuesta del backend

### **Test 2: Verificar endpoint de login desde navegador**
Abre navegador ? `https://localhost:2501/swagger`
Probar login desde Swagger de nuevo
**Esperado:** ? Login exitoso

### **Test 3: Ver logs de la desktop**
Revisar logs en: `C:\GestionTime\GestionTime.Desktop\logs\app.log`
Buscar líneas que contengan:
- "LoginAsync iniciado"
- "HTTP POST /api/v1/auth/login"
- "ERROR" o "Exception"

### **Test 4: Capturar error específico**
En `LoginPage.xaml.cs`, buscar este bloque:
```csharp
catch (HttpRequestException httpEx)
{
    // ¿Qué mensaje exacto muestra?
}
```

---

## ?? INFORMACIÓN NECESARIA

Para diagnosticar exactamente, necesito:

1. **¿Qué mensaje de error exacto ves en la desktop?**
2. **¿Los logs de app.log muestran algo específico?**
3. **¿El backend está corriendo en HTTPS (2501) o HTTP (2500)?**
4. **¿Puedes acceder a https://localhost:2501 desde el navegador?**

---

## ?? DIAGNÓSTICO RÁPIDO

**Ejecuta esto y dime el resultado:**

1. **Verificar backend:**
```sh
curl https://localhost:2501/api/v1/health
```

2. **Verificar login desde curl:**
```sh
curl -X POST https://localhost:2501/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"psantos@tdkportal.com","password":"Nimda2008@2020"}'
```

3. **Ver logs de desktop:**
```sh
tail -n 50 "C:\GestionTime\GestionTime.Desktop\logs\app.log"
```

---

**Con esta información podré darte la solución exacta.** ??

---

**Fecha:** 2025-12-26 21:55:00  
**Problema:** Login funciona en Swagger, falla en desktop  
**Diagnóstico:** Múltiples causas posibles (SSL, CORS, headers)  
**Estado:** ? Esperando información específica del error