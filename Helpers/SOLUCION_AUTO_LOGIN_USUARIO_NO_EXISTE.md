# ?? PROBLEMA: AUTO-LOGIN FALLA - USUARIO NO EXISTE

## ?? PROBLEMA IDENTIFICADO

**Error en logs:**
```
BCrypt.Net.SaltParseException: Invalid salt version
```

**Causa real:**
- Auto-login intenta login con `psantos@global-retail.com`
- ? **Este usuario NO existe en la base de datos**
- Base de datos está vacía (0 usuarios)

---

## ? SOLUCIONES

### **OPCIÓN 1: Crear el usuario faltante (RECOMENDADA)**

**Registrar usuario usando la aplicación desktop:**

1. **Deshabilitar auto-login temporalmente**
2. **Ir a RegisterPage** 
3. **Registrar usuario:**
   - Nombre: `Pedro Santos`
   - Email: `psantos@global-retail.com`
   - Password: `Nimda2008@2020`
4. **Volver a habilitar auto-login**

### **OPCIÓN 2: Cambiar email del auto-login**

**Archivo:** `MainWindow.xaml.cs` (línea ~12)

**CAMBIAR:**
```csharp
private const string AutoLoginEmail = "psantos@global-retail.com";
```

**POR:**
```csharp
private const string AutoLoginEmail = "psantos@tdkportal.com";  // ? Email que usaste en tests
```

### **OPCIÓN 3: Deshabilitar auto-login temporalmente**

**Archivo:** `MainWindow.xaml.cs` (línea ~11)

**CAMBIAR:**
```csharp
private const bool AutoLoginEnabled = true;
```

**POR:**
```csharp
private const bool AutoLoginEnabled = false;  // ? Deshabilitar temporalmente
```

---

## ?? PASOS INMEDIATOS

### **SOLUCIÓN RÁPIDA (30 segundos):**

**1. Deshabilitar auto-login:**

Editar `MainWindow.xaml.cs`:
```csharp
private const bool AutoLoginEnabled = false;  // ? Cambiar a false
```

**2. Reiniciar aplicación**

**3. Registrar usuario manualmente:**
- Abrir app ? "Registrarse como nuevo usuario"
- Nombre: `Pedro Santos`
- Email: `psantos@global-retail.com` 
- Password: `Nimda2008@2020`

**4. Habilitar auto-login de nuevo:**
```csharp
private const bool AutoLoginEnabled = true;   // ? Volver a true
```

---

## ?? VERIFICACIÓN

### **Comprobar que el usuario se creó:**
```sql
SELECT email, full_name FROM users WHERE email = 'psantos@global-retail.com';
```

**Esperado:**
```
email                      | full_name
---------------------------|------------
psantos@global-retail.com | Pedro Santos
```

### **Probar auto-login:**
1. Reiniciar aplicación
2. Auto-login debería funcionar
3. Navegación automática a DiarioPage

---

## ?? ALTERNATIVAS AVANZADAS

### **Si prefieres SQL directo:**

**1. Generar hash de contraseña:**
```csharp
// En C#, por ejemplo en una app console temporal:
var hash = BCrypt.Net.BCrypt.HashPassword("Nimda2008@2020");
Console.WriteLine(hash);
```

**2. Insertar usuario:**
```sql
INSERT INTO users (id, email, password_hash, full_name, enabled) 
VALUES (gen_random_uuid(), 'psantos@global-retail.com', '[HASH_GENERADO]', 'Pedro Santos', true);
```

---

## ?? ESTADO ACTUAL

```
? Base de datos: 0 usuarios
? Auto-login busca: psantos@global-retail.com (NO EXISTE)
? Resultado: BCrypt error (usuario no encontrado)
```

**Después de la solución:**
```
? Base de datos: Usuario psantos@global-retail.com creado
? Auto-login busca: psantos@global-retail.com (EXISTE)
? Resultado: Login exitoso ? DiarioPage
```

---

## ? RECOMENDACIÓN

**Para resolver AHORA:**

1. **Deshabilitar auto-login** (1 línea de código)
2. **Registrar usuario** desde la app (2 minutos)
3. **Habilitar auto-login** (1 línea de código)

**Total:** 3 minutos

---

**¿Qué opción prefieres?** ??

---

**Fecha:** 2025-12-27 15:15:00  
**Error:** BCrypt.Net.SaltParseException  
**Causa real:** Usuario no existe en BD  
**Solución:** Crear usuario psantos@global-retail.com  
**Estado:** ? Esperando implementación