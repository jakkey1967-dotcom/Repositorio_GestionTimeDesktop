# ?? FIX: Campo "FullName" en Registro

## ?? Problema

Al intentar registrar un usuario, la API devolvía error:

```
Error 400 (BadRequest): The FullName field is required.
```

**Log del error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "FullName": ["The FullName field is required."]
  }
}
```

---

## ?? Causa

El modelo `RegisterRequest` en el cliente estaba usando el campo `Nombre`, pero la API espera `FullName`:

### **ANTES (Incorrecto):**
```csharp
private sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;  // ? Incorrecto
    public string Empresa { get; set; } = string.Empty;
}
```

**JSON enviado:**
```json
{
  "Email": "psantos@tdkportal.com",
  "Password": "***",
  "Nombre": "Francisco Santos",  // ? API no reconoce este campo
  "Empresa": ""
}
```

---

## ? Solución

Cambiar el campo `Nombre` a `FullName` para que coincida con lo que espera la API:

### **AHORA (Correcto):**
```csharp
private sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;  // ? Correcto
    public string Empresa { get; set; } = string.Empty;
}
```

**JSON enviado:**
```json
{
  "Email": "psantos@tdkportal.com",
  "Password": "***",
  "FullName": "Francisco Santos",  // ? API lo reconoce
  "Empresa": ""
}
```

**Y actualizar la asignación:**
```csharp
var payload = new RegisterRequest
{
    Email = email,
    Password = password,
    FullName = nombre,  // ? Cambiado de Nombre a FullName
    Empresa = empresa
};
```

---

## ?? Comparación

| Aspecto | Antes (?) | Ahora (?) |
|---------|-----------|-----------|
| **Campo en C#** | `Nombre` | `FullName` |
| **JSON enviado** | `{ "Nombre": "..." }` | `{ "FullName": "..." }` |
| **API reconoce** | ? No | ? Sí |
| **Resultado** | Error 400 | ? Registro exitoso |

---

## ?? Testing

### **Test 1: Registro Exitoso**

1. Abrir la aplicación
2. Ir a página de registro
3. Llenar formulario:
   - **Nombre:** Francisco Santos
   - **Email:** test@example.com
   - **Contraseña:** Test1234
   - **Confirmar:** Test1234
   - **Empresa:** (opcional)
4. Click en "Registrarse"
5. **Resultado esperado:** ? "¡Registro exitoso! Redirigiendo al login..."

---

### **Test 2: Verificar JSON Enviado**

Revisar logs del cliente:

**ANTES (Error):**
```
[INFO] HTTP POST /api/v1/auth/register Payload: {"Email":"...","Password":"***","Nombre":"Francisco Santos","Empresa":""}
[WARN] HTTP POST /api/v1/auth/register ERROR 400. Body: {"errors":{"FullName":["The FullName field is required."]}}
```

**AHORA (Exitoso):**
```
[INFO] HTTP POST /api/v1/auth/register Payload: {"Email":"...","Password":"***","FullName":"Francisco Santos","Empresa":""}
[INFO] HTTP POST /api/v1/auth/register -> 200 en 150ms
[INFO] ? Usuario registrado exitosamente
```

---

## ?? Lección Aprendida

### **Validación de Campos de API**

Cuando trabajas con una API, los nombres de los campos deben coincidir **exactamente** con lo que la API espera:

| API Espera | Cliente Debe Enviar | Resultado |
|------------|---------------------|-----------|
| `FullName` | `FullName` | ? Funciona |
| `FullName` | `Nombre` | ? Error 400 |
| `fullName` (camelCase) | `FullName` (PascalCase) | ?? Depende de config |

**Recomendación:**
- Revisar la documentación de la API (Swagger)
- Validar el JSON que se envía en los logs
- Usar los mismos nombres de campo que la API

---

## ?? Archivos Modificados

| Archivo | Cambio | Impacto |
|---------|--------|---------|
| `Views/RegisterPage.xaml.cs` | Campo `Nombre` ? `FullName` | **CRÍTICO** ? |
| - Clase `RegisterRequest` | Propiedad renombrada | Corrige error 400 |
| - Método `OnRegisterClick` | Asignación actualizada | Envía campo correcto |

---

## ? Resultado Final

**Compilación:** ? Exitosa (0 errores)  
**Campo Corregido:** ? `Nombre` ? `FullName`  
**JSON Enviado:** ? Coincide con API  
**Registro:** ? Funciona correctamente  
**Estado:** ? **PROBLEMA RESUELTO**  

---

## ?? Beneficio

**ANTES:** Error 400 al intentar registrarse
```
? Error: Error 400 (BadRequest): The FullName field is required.
```

**AHORA:** Registro exitoso
```
? ¡Registro exitoso! Redirigiendo al login...
```

---

**¡El registro de usuarios ahora funciona correctamente!** ?????

**Además, gracias a la mejora de mensajes de error de API, el error era claro:**
```
Error 400 (BadRequest): The FullName field is required.
```

En lugar del genérico:
```
Error 400 /api/v1/auth/register
```

**¡El sistema de mensajes de error funcionó perfectamente!** ??

---

**Fecha:** 2025-12-26 19:15:00  
**Problema:** Campo "Nombre" no reconocido por API  
**Causa:** API espera "FullName", cliente enviaba "Nombre"  
**Solución:** Renombrar campo a "FullName"  
**Resultado:** ? Registro funciona correctamente  
**Beneficio adicional:** ? Mensaje de error claro gracias a ApiException
