# ? SOLUCIÓN APLICADA: LOGIN DESKTOP vs SWAGGER

## ?? PROBLEMA IDENTIFICADO

**CAUSA MÁS PROBABLE:** Error de validación de certificado SSL

Swagger funciona porque los navegadores manejan mejor los certificados autofirmados, pero la aplicación desktop con `HttpClient` los rechaza por defecto.

---

## ? SOLUCIÓN APLICADA

### **CAMBIO 1: Deshabilitar validación SSL (DESARROLLO)**

**Archivo:** `Services/ApiClient.cs` (línea ~52)

**ANTES:**
```csharp
var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new CookieContainer() };
```

**AHORA:**
```csharp
var handler = new HttpClientHandler 
{ 
    UseCookies = true, 
    CookieContainer = new CookieContainer(),
    // ? DESHABILITAR VALIDACIÓN SSL (SOLO DESARROLLO)
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
};
```

**Efecto:** La aplicación desktop ahora aceptará el certificado SSL autofirmado de `localhost:2501`

---

## ?? SOLUCIONES ADICIONALES (SI AÚN FALLA)

### **SOLUCIÓN 2: Cambiar a HTTP**

Si el problema persiste, puedes cambiar la configuración a HTTP:

**Archivo:** `appsettings.json`
```json
{
  "Api": {
    "BaseUrl": "http://localhost:2500",  // ? HTTP en lugar de HTTPS
    "LoginPath": "/api/v1/auth/login"
  }
}
```

**Ventajas:**
- ? Sin problemas de SSL
- ? Más simple para desarrollo

**Desventajas:**
- ?? Menos seguro (pero está bien para desarrollo local)

### **SOLUCIÓN 3: Verificar configuración CORS**

Si aún hay problemas, agregar soporte para aplicaciones desktop:

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Program.cs`
```csharp
"Cors": {
  "Origins": [
    "https://localhost:5173",
    "http://localhost:5173",
    "https://localhost:2501",
    "http://localhost:2500",
    "app://localhost",      // ? Para apps desktop
    "capacitor://localhost"  // ? Para apps híbridas
  ]
}
```

---

## ?? TESTING

### **PASO 1: Reiniciar aplicación desktop**

**IMPORTANTE:** Como has estado debuggeando, necesitas **reiniciar completamente la aplicación** para que los cambios surtan efecto.

1. **Detener** la aplicación desktop (cerrar completamente)
2. **Compilar:** Ctrl+Shift+B o Build ? Build Solution
3. **Iniciar** de nuevo: F5 o Debug ? Start Debugging

### **PASO 2: Probar login**

1. **Abrir** aplicación desktop
2. **Ingresar credenciales:**
   - Email: `psantos@tdkportal.com`
   - Password: `Nimda2008@2020`
3. **Click** "Iniciar sesión"

**Resultado esperado:**
- ? Login exitoso
- ? Navegación a DiarioPage

### **PASO 3: Verificar logs**

Si aún falla, revisar logs en:
```
C:\GestionTime\GestionTime.Desktop\logs\app.log
```

Buscar líneas como:
```
[INF] LoginAsync iniciado para psantos@tdkportal.com
[INF] HTTP POST /api/v1/auth/login -> 200 en XXXms
[INF] Token extraído de JSON response ?
```

---

## ?? DIAGNÓSTICO ADICIONAL

### **Si el login aún falla:**

**1. Verificar que el backend esté corriendo:**
```sh
curl https://localhost:2501/api/v1/health
```

**2. Verificar endpoint de login:**
```sh
curl -X POST https://localhost:2501/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"psantos@tdkportal.com","password":"Nimda2008@2020"}' \
  -k
```

**3. Ver logs del backend:**
```sh
# En la consola donde corre el backend, buscar:
[INF] Intento de login para psantos@tdkportal.com
[INF] Login exitoso para psantos@tdkportal.com
```

---

## ?? NOTA IMPORTANTE

**Para PRODUCCIÓN:**
- ? NO usar `ServerCertificateCustomValidationCallback = true` 
- ? Usar certificados SSL válidos
- ? Configurar HTTPS correctamente

**Para DESARROLLO:**
- ? Esta solución está bien
- ? Simplifica el testing local
- ? No afecta la seguridad en entorno local

---

## ?? COMPARACIÓN

| Método | Swagger | Desktop (ANTES) | Desktop (AHORA) |
|--------|---------|-----------------|-----------------|
| **SSL** | ? Navegador maneja | ? HttpClient rechaza | ? **Validación deshabilitada** |
| **URL** | https://localhost:2501 | https://localhost:2501 | https://localhost:2501 |
| **Headers** | ? Estándar navegador | ? HttpClient estándar | ? HttpClient estándar |
| **Login** | ? Funciona | ? **Fallaba** | ? **Debería funcionar** |

---

## ? RESULTADO ESPERADO

Después de aplicar este cambio y reiniciar la aplicación:

```
1. Usuario abre desktop app
2. Ingresa credenciales
3. Click "Iniciar sesión"
4. ? Conexión SSL exitosa (certificado aceptado)
5. ? Login exitoso
6. ? Navegación a DiarioPage
```

---

**¡REINICIA LA APLICACIÓN Y PRUEBA EL LOGIN!** ??

---

**Fecha:** 2025-12-26 22:00:00  
**Problema:** Login funciona en Swagger, falla en desktop  
**Causa:** Validación SSL rechazaba certificado autofirmado  
**Solución:** ? Aplicada - Validación SSL deshabilitada para desarrollo  
**Estado:** ? Pendiente testing tras reinicio de app