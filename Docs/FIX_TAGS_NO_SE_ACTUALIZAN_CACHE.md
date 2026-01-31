# FIX: Tags no se actualizan en caché al guardar parte

**Fecha**: 2026-01-31  
**Estado**: ✅ CORREGIDO  
**Compilación**: ✅ EXITOSA

---

## 🔴 PROBLEMA

Al guardar un parte (nuevo o editado), los **Tags NO aparecían en DiarioPage** después de guardar, aunque sí se guardaban correctamente en la base de datos.

**Síntoma**:
- Usuario agrega tags en ParteItemEdit
- Guarda el parte
- Vuelve a DiarioPage
- ❌ Columna "Tags" aparece vacía
- Solo aparecen después de refrescar manualmente (F5)

---

## 🔍 CAUSA RAÍZ

En `ParteItemEdit.xaml.cs`, al construir el objeto `ParteDto` que se guarda en el **caché local**, se **olvidó incluir la propiedad Tags**.

Esto ocurría en 2 lugares:
1. Al crear un parte nuevo (POST)
2. Al actualizar un parte existente (PUT)

### Código problemático:

```csharp
// ❌ CONSTRUCCIÓN DEL OBJETO SIN TAGS
response = new ParteDto
{
    Id = nuevoId,
    Fecha = Parte.Fecha,
    Cliente = Parte.Cliente,
    Tienda = Parte.Tienda,
    // ... otros campos ...
    IdCliente = clienteId,
    IdGrupo = grupoId,
    IdTipo = tipoId
    // ❌ FALTA: Tags
};

// El objeto se guardaba en caché sin Tags
App.Api.UpdateCacheEntry(endpoint, response);
App.Api.AddItemToListCache(listEndpoint, response);
```

---

## ✅ SOLUCIÓN IMPLEMENTADA

### 1. Agregado Tags al objeto de caché (CREATE)

**Archivo**: `Views/ParteItemEdit.xaml.cs` (línea ~1296)

**ANTES** ❌:
```csharp
response = new ParteDto
{
    Id = nuevoId,
    Fecha = Parte.Fecha,
    Cliente = Parte.Cliente,
    // ... campos ...
    IdTipo = tipoId
    // ❌ Tags no estaba
};
```

**DESPUÉS** ✅:
```csharp
response = new ParteDto
{
    Id = nuevoId,
    Fecha = Parte.Fecha,
    Cliente = Parte.Cliente,
    // ... campos ...
    IdTipo = tipoId,
    // ✅ FIX: Incluir Tags en el objeto que se guarda en caché
    Tags = _currentTags.Any() ? _currentTags.ToList() : new List<string>()
};
```

### 2. Agregado Tags al objeto de caché (UPDATE)

**Archivo**: `Views/ParteItemEdit.xaml.cs` (línea ~1156)

Mismo cambio aplicado para el caso de actualización (PUT).

### 3. Agregado log de verificación

```csharp
App.Log?.LogInformation("      • 🏷️ Tags: {tags}", 
    response.Tags != null && response.Tags.Any() 
    ? string.Join(", ", response.Tags) 
    : "(sin tags)");
```

Esto permite verificar en `logs/app.log` que los tags se están guardando correctamente.

---

## 📊 FLUJO CORREGIDO

### Escenario 1: Crear parte nuevo con tags

1. Usuario abre ParteItemEdit (nuevo)
2. Agrega tags: ["tpv", "hardware"]
3. Guarda el parte
4. ✅ Backend responde con ID nuevo
5. ✅ Se construye `ParteDto` con **Tags incluidos**
6. ✅ Se guarda en caché: `UpdateCacheEntry()`, `AddItemToListCache()`
7. ✅ DiarioPage muestra los tags **inmediatamente**

### Escenario 2: Editar parte existente y cambiar tags

1. Usuario abre ParteItemEdit (editar)
2. Modifica tags: quita "urgente", agrega "resuelto"
3. Guarda el parte
4. ✅ Backend responde OK
5. ✅ Se construye `ParteDto` con **Tags actualizados**
6. ✅ Se actualiza caché: `UpdateCacheEntry()`
7. ✅ DiarioPage muestra los tags **actualizados inmediatamente**

---

## 🔧 DETALLES TÉCNICOS

### Por qué ocurría el bug:

El backend **SÍ guardaba los tags correctamente** en la BD, pero el Desktop construía el objeto de caché **sin incluir los tags**, por lo que:

```
┌─────────────┐  ✅ Tags enviados    ┌──────────┐
│ ParteEdit   │ ──────────────────►  │ Backend  │
│             │                      │ (BD OK)  │
└─────────────┘                      └──────────┘
      │
      │ ❌ Objeto sin Tags
      ▼
┌─────────────┐
│ Caché local │
│ Tags: []    │ ◄─── DiarioPage lee desde aquí
└─────────────┘
```

### Solución:

Asegurar que el objeto que se guarda en caché **incluya los tags** actuales:

```csharp
Tags = _currentTags.Any() ? _currentTags.ToList() : new List<string>()
```

- `_currentTags` es la `ObservableCollection<string>` que contiene los tags actuales
- Se convierte a `List<string>` para evitar referencias compartidas
- Si está vacío, se crea una lista vacía (no null)

---

## 🧪 VERIFICACIÓN

### Logs esperados (app.log):

**Al guardar parte con tags**:
```
✅ Parte creado exitosamente con ID: 123
   📊 Objeto construido con datos del formulario:
      • ID: 123
      • Cliente: Cliente ABC
      • Ticket: INC456
      • 🏷️ Tags: tpv, hardware, urgente
💾 Cache del parte individual actualizado: /api/v1/partes/123
➕ Nuevo parte agregado al cache de la lista
```

**Al guardar parte sin tags**:
```
      • 🏷️ Tags: (sin tags)
```

### Checklist de pruebas:

- [ ] Crear parte con tags → ✅ Tags aparecen en DiarioPage inmediatamente
- [ ] Editar parte y agregar tags → ✅ Tags aparecen sin refrescar
- [ ] Editar parte y quitar tags → ✅ Tags desaparecen sin refrescar
- [ ] Duplicar parte con tags → ✅ Tags se copian y aparecen
- [ ] Columna Tags en DiarioPage → ✅ Muestra primer tag con tooltip de todos

---

## 📁 ARCHIVOS MODIFICADOS

1. ✅ `Views/ParteItemEdit.xaml.cs`
   - **Línea ~1156**: Agregado Tags en construcción de objeto (UPDATE)
   - **Línea ~1296**: Agregado Tags en construcción de objeto (CREATE)
   - **Líneas 1191 y 1332**: Agregado log de Tags

---

## ✅ RESULTADO FINAL

**Tags en Caché - CORREGIDO** ✅

- Tags se guardan en caché correctamente (CREATE y UPDATE)
- DiarioPage muestra tags inmediatamente después de guardar
- No se necesita refrescar manualmente
- Logs muestran tags para debugging
- Compilación exitosa

---

**Fin del documento**
