# FIX: Duplicar Parte - Copiar Ticket y Tags correctamente

**Fecha**: 2026-01-31  
**Estado**: ✅ CORREGIDO  
**Compilación**: ✅ EXITOSA

---

## 🔴 PROBLEMA

Al pulsar "Duplicar" sobre un parte en DiarioPage, **NO se copiaban correctamente**:
1. ❌ **Ticket** quedaba vacío (se perdía el valor)
2. ❌ **Tags** no se copiaban (lista vacía)
3. ⚠️ Riesgo de referencia compartida en Tags

---

## ✅ SOLUCIÓN IMPLEMENTADA

### Cambios en `OnDuplicarClick()`:

**Archivo**: `Views/DiarioPage.xaml.cs`

#### ANTES ❌:
```csharp
var nuevoParte = new ParteDto
{
    Id = 0,
    Fecha = DateTime.Today,
    HoraInicio = DateTime.Now.ToString("HH:mm"),
    HoraFin = "",
    Cliente = parte.Cliente,
    Tienda = parte.Tienda,
    Accion = parte.Accion,
    Ticket = "", // ❌ NO copiaba Ticket
    Grupo = parte.Grupo,
    Tipo = parte.Tipo,
    EstadoParte = ParteEstado.Abierto,
    IdCliente = parte.IdCliente,
    IdGrupo = parte.IdGrupo,
    IdTipo = parte.IdTipo
    // ❌ NO copiaba Tags
};
```

#### DESPUÉS ✅:
```csharp
var nuevoParte = new ParteDto
{
    Id = 0, // Nuevo registro
    Fecha = DateTime.Today, // ⚠️ SIEMPRE HOY (no copiar fecha original)
    HoraInicio = DateTime.Now.ToString("HH:mm"),
    HoraFin = "",
    Cliente = parte.Cliente,
    Tienda = parte.Tienda,
    Accion = parte.Accion,
    Ticket = parte.Ticket, // ✅ COPIAR TICKET
    Grupo = parte.Grupo,
    Tipo = parte.Tipo,
    EstadoParte = ParteEstado.Abierto, // Estado inicial: Abierto
    IdCliente = parte.IdCliente,
    IdGrupo = parte.IdGrupo,
    IdTipo = parte.IdTipo,
    // ✅ COPIAR TAGS con deep copy (nueva lista)
    Tags = parte.Tags != null ? new List<string>(parte.Tags) : new List<string>()
};

// Logs para verificación
App.Log?.LogInformation("📋 Parte duplicado creado:");
App.Log?.LogInformation("   • Cliente: {cliente}", nuevoParte.Cliente);
App.Log?.LogInformation("   • Tienda: {tienda}", nuevoParte.Tienda);
App.Log?.LogInformation("   • Ticket: {ticket}", nuevoParte.Ticket ?? "(vacío)");
App.Log?.LogInformation("   • Tags: {tags}", nuevoParte.Tags != null ? string.Join(", ", nuevoParte.Tags) : "(sin tags)");
App.Log?.LogInformation("   • Fecha: {fecha} (HOY)", nuevoParte.Fecha.ToString("yyyy-MM-dd"));
```

---

## 📊 COMPORTAMIENTO CORREGIDO

### Flujo completo de "Duplicar":

1. Usuario selecciona un parte en DiarioPage
2. Click derecho → "Duplicar"
3. ✅ Se crea un **nuevo ParteDto** con:
   - **Id = 0** (nuevo registro)
   - **Fecha = HOY** (no copia fecha original)
   - **Estado = Abierto** (siempre)
   - **Ticket = copiado** del parte original
   - **Tags = nueva lista** con los mismos valores (deep copy)
   - **Cliente, Tienda, Acción, Grupo, Tipo** = copiados
4. Se abre **ParteItemEdit** en modo CREAR
5. Título: "📋 Duplicar Parte #123"
6. Usuario puede editar y guardar (POST)

---

## 🔧 DETALLES TÉCNICOS

### Deep Copy de Tags:

**Por qué es necesario**:
```csharp
// ❌ MAL - Referencia compartida
Tags = parte.Tags

// ✅ BIEN - Nueva lista independiente
Tags = parte.Tags != null ? new List<string>(parte.Tags) : new List<string>()
```

Si usas la referencia directa, modificar los tags en el duplicado **afectaría al original**.

### Fecha siempre HOY:

```csharp
Fecha = DateTime.Today // ✅ Usa fecha actual, NO la del parte original
```

Esto es correcto porque al duplicar, normalmente quieres crear un parte **para hoy**, no copiar la fecha histórica.

---

## 🧪 CASOS DE PRUEBA

### ✅ Duplicar parte con Ticket y Tags:

**Parte original**:
- Fecha: 2026-01-28
- Cliente: "Cliente ABC"
- Ticket: "INC123456"
- Tags: ["tpv", "hardware", "urgente"]

**Parte duplicado** (después del fix):
- ✅ Fecha: **2026-01-31** (HOY)
- ✅ Cliente: "Cliente ABC" (copiado)
- ✅ Ticket: **"INC123456"** (copiado)
- ✅ Tags: **["tpv", "hardware", "urgente"]** (deep copy)
- ✅ Estado: **Abierto**
- ✅ Id: **0** (nuevo)

### ✅ Duplicar parte sin Tags:

**Parte original**:
- Tags: null / vacío

**Parte duplicado**:
- ✅ Tags: **[]** (lista vacía, no null)

### ✅ Guardar parte duplicado:

1. Se abre ParteItemEdit con los campos rellenados
2. Usuario pulsa "Guardar"
3. ✅ Se hace **POST** (no PUT) porque Id=0
4. ✅ Backend asigna nuevo ID
5. ✅ Tags se envían correctamente en el payload

---

## 📁 ARCHIVOS MODIFICADOS

1. ✅ `Views/DiarioPage.xaml.cs`
   - Método `OnDuplicarClick()`
   - Agregado: Copia de Ticket
   - Agregado: Deep copy de Tags
   - Agregado: Logs de verificación

---

## ✅ VERIFICACIÓN

### Logs esperados (app.log):

```
📋 DUPLICAR PARTE - ID: 123
📋 Parte duplicado creado:
   • Cliente: Cliente ABC
   • Tienda: T01
   • Ticket: INC123456
   • Tags: tpv, hardware, urgente
   • Fecha: 2026-01-31 (HOY)
📝 Abriendo editor con parte duplicado (ID=0 indica NUEVO)...
```

### Checklist de pruebas:

- [ ] Duplicar parte **con Ticket** → ✅ Ticket copiado
- [ ] Duplicar parte **con Tags** → ✅ Tags copiados (deep copy)
- [ ] Duplicar parte **sin Tags** → ✅ Lista vacía (no null)
- [ ] Fecha del duplicado = **HOY** (no fecha original)
- [ ] Estado del duplicado = **Abierto**
- [ ] Al guardar → **POST** (no PUT)
- [ ] Modificar tags en duplicado → **NO afecta al original** (deep copy verificado)

---

## 🔗 NOTAS ADICIONALES

### Sin cambios en:

✅ ListView de DiarioPage - Sin cambios  
✅ ParteItemEdit - Sin cambios (ya maneja Id=0 como nuevo)  
✅ Backend/API - Sin cambios  
✅ Otros componentes - Sin cambios  

### Comportamiento esperado:

- **Ticket siempre copiado** (antes se perdía)
- **Tags siempre copiados** con nueva colección (antes vacío)
- **Fecha siempre HOY** (correcto para duplicados)
- **Estado siempre Abierto** (lógica de negocio correcta)

---

## ✅ RESULTADO FINAL

**Duplicar Parte - CORREGIDO** ✅

- Ticket se copia correctamente
- Tags se copian con deep copy
- Fecha siempre HOY (no copia original)
- Sin cambios en otros componentes
- Compilación exitosa

---

**Fin del documento**
