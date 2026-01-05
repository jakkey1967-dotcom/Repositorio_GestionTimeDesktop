# ?? ACTIVAR CUENTA MANUALMENTE EN POSTGRESQL

## ?? PROBLEMA

El endpoint `/api/v1/auth/verify-email` requiere **todos los datos del registro**, no solo email + código.

Tu backend funciona así:
1. POST `/register` ? Envía código al email
2. POST `/verify-email` ? Creas la cuenta con todos los datos + código

**Pero tú ya registraste la cuenta**, solo necesitas activarla.

---

## ? SOLUCIÓN RÁPIDA: ACTIVAR EN LA BASE DE DATOS

### **Opción 1: pgAdmin (GUI)**

1. **Abrir pgAdmin**
2. **Conectar a tu base de datos:** `localhost:5432` ? `gestiontime`
3. **Abrir Query Tool** (Tools ? Query Tool)
4. **Ejecutar:**

```sql
UPDATE "AspNetUsers" 
SET "EmailConfirmed" = true 
WHERE "Email" = 'psantos@tdkportal.com';
```

5. **Verificar:**

```sql
SELECT "Id", "Email", "EmailConfirmed" 
FROM "AspNetUsers" 
WHERE "Email" = 'psantos@tdkportal.com';
```

**Resultado esperado:**
```
Id  | Email                    | EmailConfirmed
----|--------------------------|--------------
1   | psantos@tdkportal.com   | true
```

---

### **Opción 2: psql (Command Line)**

```sh
psql -U postgres -d gestiontime -c "UPDATE \"AspNetUsers\" SET \"EmailConfirmed\" = true WHERE \"Email\" = 'psantos@tdkportal.com';"
```

---

### **Opción 3: DBeaver (Si tienes instalado)**

1. Conectar a `localhost:5432` ? `gestiontime`
2. Abrir SQL Editor
3. Ejecutar el UPDATE de arriba

---

## ? SOLUCIÓN PERMANENTE: DESHABILITAR VERIFICACIÓN

Si estás en **desarrollo** y no quieres verificar email cada vez:

### **Archivo:** `C:\GestionTime\src\GestionTime.Api\Program.cs`

**Buscar línea ~90-110** (después de `AddAuthentication`):

**Agregar:**
```csharp
// Configurar Identity Options
builder.Services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(options =>
{
    // ? DESHABILITAR verificación de email (SOLO DESARROLLO)
    options.SignIn.RequireConfirmedEmail = false;
    
    // Opcional: Relajar requisitos de contraseña
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
});
```

**Reiniciar backend**

**Resultado:**
- ? Registro funciona sin verificar email
- ? Login funciona inmediatamente
- ? No necesitas activar manualmente cada vez

---

## ?? COMPARACIÓN

| Método | Velocidad | Permanente | Recomendado |
|--------|-----------|------------|-------------|
| **SQL UPDATE** | ? 30 seg | No (solo esta cuenta) | Para esta vez |
| **Deshabilitar verificación** | ? 1 min | Sí (todas las cuentas) | ? Para desarrollo |

---

## ?? MI RECOMENDACIÓN

### **Para AHORA (activar tu cuenta actual):**
```sql
UPDATE "AspNetUsers" 
SET "EmailConfirmed" = true 
WHERE "Email" = 'psantos@tdkportal.com';
```

### **Para DESARROLLO (no verificar más):**
```csharp
// En Program.cs
options.SignIn.RequireConfirmedEmail = false;
```

---

## ?? TESTING

### **1. Activar cuenta con SQL**
```sql
UPDATE "AspNetUsers" SET "EmailConfirmed" = true WHERE "Email" = 'psantos@tdkportal.com';
```

### **2. Verificar que está activada**
```sql
SELECT "Email", "EmailConfirmed" FROM "AspNetUsers" WHERE "Email" = 'psantos@tdkportal.com';
```

Debería mostrar: `EmailConfirmed = true`

### **3. Probar login en la app desktop**
- Email: `psantos@tdkportal.com`
- Password: tu contraseña

**Resultado esperado:** ? Login exitoso

---

## ?? NOTA SOBRE EL FLUJO DE REGISTRO

Tu backend tiene un flujo **diferente** al estándar:

### **Backend Actual:**
```
1. POST /register ? Envía código (NO crea usuario aún)
2. POST /verify-email ? Crea usuario + verifica email
```

### **Backend Estándar:**
```
1. POST /register ? Crea usuario (EmailConfirmed = false)
2. POST /verify-email ? Solo marca EmailConfirmed = true
```

Por eso tu endpoint `/verify-email` pide todos los datos.

---

## ?? RESUMEN

**Para activar tu cuenta AHORA:**
1. Abrir pgAdmin
2. Ejecutar: `UPDATE "AspNetUsers" SET "EmailConfirmed" = true WHERE "Email" = 'psantos@tdkportal.com';`
3. Probar login en la app

**Para NO tener que verificar en desarrollo:**
1. Editar `Program.cs` del backend
2. Agregar: `options.SignIn.RequireConfirmedEmail = false;`
3. Reiniciar backend

---

**¿Tienes pgAdmin instalado? Si no, te ayudo a deshabilitarlo en el backend directamente.** ??

---

**Fecha:** 2025-12-26 20:15:00  
**Problema:** verify-email pide todos los datos del registro  
**Causa:** Flujo de registro diferente en el backend  
**Solución:** SQL UPDATE o deshabilitar verificación  
**Estado:** Esperando tu elección
