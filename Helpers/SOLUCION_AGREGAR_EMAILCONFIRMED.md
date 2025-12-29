# ?? SOLUCIÓN: AGREGAR EmailConfirmed MANUALMENTE

## ? PROBLEMA

1. Backend no arranca (línea 96 de Program.cs)
2. Entity Framework detecta cambios en el modelo (EmailConfirmed agregado)
3. No puede validar contra la BD porque falta la columna

---

## ? SOLUCIÓN: AGREGAR COLUMNA MANUALMENTE

### **PASO 1: Ejecutar SQL en PostgreSQL**

**Abrir pgAdmin** y ejecutar:

```sql
-- Agregar columna EmailConfirmed a la tabla users
ALTER TABLE "users" 
ADD COLUMN IF NOT EXISTS "email_confirmed" boolean NOT NULL DEFAULT false;

-- Verificar que se agregó
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'users' 
AND column_name = 'email_confirmed';
```

---

### **PASO 2: Activar tu cuenta**

```sql
UPDATE "users" 
SET "email_confirmed" = true 
WHERE "email" = 'psantos@tdkportal.com';
```

---

### **PASO 3: Crear migración (después de que el backend arranque)**

```sh
cd C:\GestionTime\src\GestionTime.Api
dotnet ef migrations add AddEmailConfirmed --project ..\GestionTime.Infrastructure
```

Esto creará una migración que **YA está aplicada** (la columna ya existe), pero quedará registrada.

---

### **PASO 4: Marcar migración como aplicada**

```sh
dotnet ef database update --project ..\GestionTime.Infrastructure
```

---

## ?? ALTERNATIVA: REVERTIR CAMBIO EN User.cs

Si no quieres lidiar con esto ahora, **quita temporalmente** `EmailConfirmed` del modelo:

**Archivo:** `C:\GestionTime\src\GestionTime.Domain\Auth\User.cs`

**QUITAR esta línea:**
```csharp
public bool EmailConfirmed { get; set; } = false;  // ? QUITAR TEMPORALMENTE
```

**Reiniciar backend** ? Debería arrancar sin problemas

---

## ?? RESUMEN

| Opción | Tiempo | Recomendada |
|--------|--------|-------------|
| **SQL Manual** | ? 2 min | ? Sí (más rápido) |
| **Revertir cambio** | ? 1 min | Para arrancar el backend |

---

**¿Qué prefieres hacer?** ??
