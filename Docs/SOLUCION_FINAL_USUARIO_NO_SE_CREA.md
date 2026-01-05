# ?? SOLUCIÓN: USUARIO NO SE CREA EN LA BASE DE DATOS

## ? PROBLEMA IDENTIFICADO

**El usuario NO se está creando durante el registro.**

**Causa:** El backend tiene un flujo de registro en 2 pasos:
1. `/register` ? Solo guarda datos temporalmente
2. `/verify-email` ? **Crea el usuario** (pero la app no tiene esta página)

**Resultado:** Usuario no existe ? Login falla con "Credenciales inválidas"

---

## ? SOLUCIÓN (5 MINUTOS)

### **PASO 1: Modificar método `/register` en el backend**

**Archivo:** `C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs`

**Acción:**
1. Abrir el archivo en Visual Studio
2. Buscar: `[HttpPost("register")]` (línea ~135)
3. **SELECCIONAR TODO EL MÉTODO** (desde `[HttpPost("register")]` hasta el `}` antes de `[HttpPost("verify-email")]`)
4. **ELIMINAR**
5. **PEGAR** el código que está en: `Helpers/REGISTER_METHOD_FIXED.cs`
6. **Guardar** (Ctrl+S)

**Código a pegar:** Ver archivo `REGISTER_METHOD_FIXED.cs`

**Cambios clave:**
```csharp
// ? ANTES (INCORRECTO):
var tempData = new { Email, FullName, Password };
tokenSvc.SaveTokenWithData($"register:{token}", jsonData); // Solo guarda temp
return Ok("Código enviado");

// ? DESPUÉS (CORRECTO):
var newUser = new User { Email, PasswordHash, FullName, Enabled = true };
db.Users.Add(newUser);
await db.SaveChangesAsync(); // ? Usuario creado AQUÍ
return Ok("Registro exitoso. Ya puedes iniciar sesión.");
```

---

### **PASO 2: Reiniciar backend**

**Opción A - Terminal:**
```sh
cd C:\GestionTime\src\GestionTime.Api
dotnet run
```

**Opción B - Visual Studio:**
- Detener el backend (Shift+F5)
- Iniciar de nuevo (F5)

---

### **PASO 3: Probar registro**

**En la aplicación desktop:**

1. **Ir a RegisterPage**
2. **Llenar formulario:**
   - Nombre: `Test User`
   - Email: `test@example.com`
   - Contraseña: `test123`
3. **Click "Registrarse"**

**Resultado esperado:**
```
? "Registro exitoso. Ya puedes iniciar sesión."
? Redirige a LoginPage
```

---

### **PASO 4: Verificar en base de datos**

**Abrir pgAdmin ? Query Tool:**

```sql
SELECT id, email, full_name, email_confirmed, enabled 
FROM users 
WHERE email = 'test@example.com';
```

**Resultado esperado:**
```
id         | email             | full_name | email_confirmed | enabled
-----------|-------------------|-----------|-----------------|--------
[UUID]     | test@example.com | Test User | false           | true
```

? **Usuario existe** ? Login funcionará

---

### **PASO 5: Probar login**

**En LoginPage:**
1. Email: `test@example.com`
2. Contraseña: `test123`
3. Click "Iniciar sesión"

**Resultado esperado:**
```
? Login exitoso
? Navega a DiarioPage
```

---

## ?? TESTING COMPLETO

### **Test 1: Registro**
```
RegisterPage ? Llenar formulario ? Click "Registrarse"
Esperado: ? "Registro exitoso"
```

### **Test 2: Usuario en BD**
```sql
SELECT * FROM users WHERE email = 'test@example.com';
Esperado: ? Usuario existe
```

### **Test 3: Login**
```
LoginPage ? Ingresar credenciales ? Click "Iniciar sesión"
Esperado: ? Login exitoso ? DiarioPage
```

### **Test 4: Tu cuenta**
```
LoginPage ? Email: psantos@tdkportal.com
           ? Password: Nimda2008@2020
Esperado: ? Login exitoso (si la cuenta fue activada con SQL)
```

---

## ?? COMPARACIÓN: ANTES vs DESPUÉS

| Flujo | ANTES (INCORRECTO) | DESPUÉS (CORRECTO) |
|-------|-------------------|-------------------|
| **Registro** | Solo guarda temp | ? **Crea usuario** |
| **Verificación** | Crea usuario | ? Opcional |
| **Login** | ? Falla (usuario no existe) | ? **Funciona** |
| **Base de datos** | ? Vacía | ? **Usuario existe** |

---

## ?? IMPORTANTE

### **EmailConfirmed = false**
- Usuario se crea con `email_confirmed = false`
- **NO IMPORTA** porque ya configuraste `RequireConfirmedEmail = false`
- Login funciona sin verificar

### **Email se envía (opcional)**
- El código sigue enviándose por email
- **NO es bloqueante** - Si falla el email, el usuario ya está creado
- En el futuro, si quieres verificación, solo cambias `RequireConfirmedEmail = true`

---

## ?? FLUJO FINAL

```
1. Usuario llena formulario en RegisterPage
2. Click "Registrarse"
3. Backend CREA usuario en BD (email_confirmed = false)
4. Backend envía email con código (opcional)
5. App redirige a LoginPage
6. Usuario puede hacer login inmediatamente ?
```

---

## ? CHECKLIST

- [ ] 1. Modificar `AuthController.cs` método `/register`
- [ ] 2. Guardar archivo
- [ ] 3. Reiniciar backend
- [ ] 4. Probar registro nuevo
- [ ] 5. Verificar usuario en BD
- [ ] 6. Probar login
- [ ] 7. ¡Listo! ??

---

## ?? ARCHIVOS RELACIONADOS

- ? `PROBLEMA_USUARIO_NO_SE_CREA.md` - Explicación del problema
- ? `REGISTER_METHOD_FIXED.cs` - Código corregido del método
- ? Este documento - Instrucciones paso a paso

---

**¿Necesitas ayuda modificando el archivo? Te guío paso a paso.** ??

---

**Fecha:** 2025-12-26 21:15:00  
**Problema:** Usuario no se crea en registro  
**Solución:** Modificar método `/register`  
**Tiempo:** 5 minutos  
**Estado:** ? Pendiente de implementar
