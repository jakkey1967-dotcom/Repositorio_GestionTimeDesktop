# ?? DIAGNÓSTICO: ENDPOINT verify-email NO DISPONIBLE

## ?? PROBLEMA IDENTIFICADO

**Síntoma:** No aparece opción para verificar email en `/api/v1/auth/verify-email`
**Estado del backend:** ? Ejecutándose (proceso ID 25840)
**Código verificado:** ? Endpoint existe en AuthController.cs

---

## ?? ANÁLISIS

### **El endpoint SÍ EXISTE en el código:**

```csharp
[HttpPost("verify-email")]
[AllowAnonymous]
public async Task<IActionResult> VerifyEmail(
    [FromBody] VerifyEmailRequest req,
    [FromServices] Services.ResetTokenService tokenSvc)
{
    // ... implementación completa ...
}
```

### **Posibles causas:**

1. **?? Backend no reflejó cambios** (necesita reinicio)
2. **?? Contrato faltante** (`VerifyEmailRequest` no definido)
3. **?? Swagger no muestra el endpoint** (error de documentación)
4. **??? Servicio ResetTokenService** no registrado

---

## ?? VERIFICACIONES NECESARIAS

### **PASO 1: Verificar contratos (DTOs)**

Buscar si existe `VerifyEmailRequest`:
```csharp
// ¿Existe en Contracts/Auth/?
public record VerifyEmailRequest(string Email, string Token);
```

### **PASO 2: Verificar servicios registrados**

En `Program.cs`, debe estar:
```csharp
builder.Services.AddScoped<Services.ResetTokenService>();
```

### **PASO 3: Reiniciar backend**

Para asegurar que los cambios se reflejen.

---

## ?? TESTING PLAN

### **Test 1: Verificar Swagger**
1. Abrir: `https://localhost:2501/swagger`
2. Buscar: `POST /api/v1/auth/verify-email`
3. **Esperado:** ? Debe aparecer en la lista

### **Test 2: Prueba directa**
```bash
curl -X POST https://localhost:2501/api/v1/auth/verify-email \
  -k -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","token":"123456"}'
```

### **Test 3: Logs del backend**
Revisar logs para ver si el endpoint se registra al iniciar.

---

## ?? PASOS DE SOLUCIÓN

### **INMEDIATOS:**
1. ?? **Verificar contratos** faltantes
2. ?? **Reiniciar backend** completamente
3. ?? **Probar en Swagger** para confirmar

### **SI AÚN FALLA:**
1. ?? **Crear contratos** faltantes
2. ?? **Registrar servicios** en Program.cs
3. ?? **Compilar y reiniciar** de nuevo

---

**Comenzamos verificando los contratos...**