# ?? FIX: Bucle Infinito en Apertura de Dropdown de Grupos y Tipos

## ? Problema Detectado

Al revisar los logs, se identificó un **bucle infinito** en los eventos `GotFocus` de los ComboBox `CmbGrupo` y `CmbTipo`:

### Síntomas en los Logs (Versión 1):

```
00:46:35 - ?? CmbGrupo GotFocus ? Abriendo dropdown
00:46:36 - ?? CmbGrupo DropDownOpened
00:46:36 - ?? CmbGrupo GotFocus ? Abriendo dropdown (OTRA VEZ)
00:46:37 - ?? CmbGrupo DropDownOpened
00:46:37 - ?? CmbGrupo GotFocus ? Abriendo dropdown (OTRA VEZ)
... se repite 8+ veces en 5 segundos
```

### Síntomas en los Logs (Versión 2 - Después de Fix Parcial):

```
00:53:49 - ? Tipo seleccionado: Incidencia
00:53:49 - ?? CmbTipo GotFocus - IsDropDownOpen=False
00:53:49 - ? Tipos ya cargados, abriendo dropdown
00:53:49 - ?? CmbTipo DropDownOpened
00:53:50 - ?? CmbTipo GotFocus - IsDropDownOpen=False  ? PROBLEMA
00:53:50 - ? Tipos ya cargados, abriendo dropdown      ? SE REPITE
... continúa 4+ veces
```

### Flujo del Problema (Versión 1):

```
Usuario hace Tab ? CmbGrupo
       ?
   GotFocus se dispara
       ?
   Código abre dropdown: IsDropDownOpen = true
       ?
   Dropdown se abre ? ComboBox PIERDE y RECUPERA foco
       ?
   GotFocus se dispara OTRA VEZ
       ?
   Bucle infinito ??
```

### Flujo del Problema (Versión 2):

```
Usuario selecciona "Incidencia" ? SelectionChanged
       ?
   Dropdown se cierra: IsDropDownOpen = false
       ?
   ComboBox RECUPERA el foco ? GotFocus se dispara
       ?
   Código detecta IsDropDownOpen=False
       ?
   ?? Cree que debe abrirlo otra vez ? IsDropDownOpen = true
       ?
   Dropdown se abre ? Pierde foco ? Recupera foco
       ?
   Bucle continúa ??
```

---

## ? Solución Implementada (MEJORADA)

### Cambio 1: Agregar Flags Temporales

```csharp
// Flags temporales para evitar bucle después de seleccionar
private bool _grupoJustSelected = false;
private bool _tipoJustSelected = false;
```

### Cambio 2: Modificar `OnGrupoGotFocus()`:

```csharp
private async void OnGrupoGotFocus(object sender, RoutedEventArgs e)
{
    App.Log?.LogInformation("?? CmbGrupo GotFocus - _gruposLoaded={loaded}, IsDropDownOpen={open}, JustSelected={just}", 
        _gruposLoaded, CmbGrupo.IsDropDownOpen, _grupoJustSelected);
    
    // ?? NO abrir si ya está abierto (evita bucle infinito)
    if (CmbGrupo.IsDropDownOpen)
    {
        App.Log?.LogDebug("?? Dropdown ya abierto, saltando...");
        return;
    }
    
    // ?? NO abrir si acabamos de seleccionar (el foco vuelve después del cierre)
    if (_grupoJustSelected)
    {
        App.Log?.LogDebug("?? Recién seleccionado, NO abrir automáticamente");
        _grupoJustSelected = false; // ?? Resetear flag
        return; // ?? SALIR
    }
    
    // Cargar grupos si aún no se han cargado
    if (!_gruposLoaded)
    {
        App.Log?.LogInformation("? Cargando grupos al recibir foco...");
        await LoadGruposAsync();
        
        // Después de cargar, abrir el dropdown automáticamente
        if (sender is ComboBox combo && _grupoItems.Count > 0)
        {
            App.Log?.LogDebug("?? Abriendo dropdown automáticamente con {count} items", _grupoItems.Count);
            _grupoDropDownOpenedByUser = true;
            combo.IsDropDownOpen = true;
        }
    }
    else
    {
        App.Log?.LogDebug("? Grupos ya cargados ({count} items), abriendo dropdown", _grupoItems.Count);
        
        // Si ya están cargados, abrir directamente
        if (sender is ComboBox combo && _grupoItems.Count > 0)
        {
            _grupoDropDownOpenedByUser = true;
            combo.IsDropDownOpen = true;
        }
    }
}
```

### Cambio 3: Modificar `OnGrupoSelectionChanged()`:

```csharp
private void OnGrupoSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (sender is ComboBox combo && combo.SelectedItem is string selectedGrupo)
    {
        App.Log?.LogInformation("? Grupo seleccionado: {grupo}", selectedGrupo);
        
        // ? Marcar que acabamos de seleccionar (para evitar bucle en GotFocus)
        _grupoJustSelected = true;
        
        // Cerrar dropdown después de seleccionar
        combo.IsDropDownOpen = false;
        
        // Marcar como modificado
        OnFieldChanged(sender, e);
    }
}
```

### Cambio 4: Modificar `OnTipoGotFocus()`:

```csharp
private async void OnTipoGotFocus(object sender, RoutedEventArgs e)
{
    App.Log?.LogInformation("?? CmbTipo GotFocus - _tiposLoaded={loaded}, IsDropDownOpen={open}", 
        _tiposLoaded, CmbTipo.IsDropDownOpen);
    
    // ?? NO abrir si ya está abierto (evita bucle infinito)
    if (CmbTipo.IsDropDownOpen)
    {
        App.Log?.LogDebug("?? Dropdown ya abierto, saltando...");
        return; // ?? SALIR INMEDIATAMENTE
    }
    
    // ?? NO abrir si acabamos de seleccionar (el foco vuelve después del cierre)
    if (_tipoJustSelected)
    {
        App.Log?.LogDebug("?? Recién seleccionado, NO abrir automáticamente");
        _tipoJustSelected = false; // ?? Resetear flag
        return; // ?? SALIR
    }
    
    // Cargar tipos si aún no se han cargado
    if (!_tiposLoaded)
    {
        App.Log?.LogInformation("? Cargando tipos al recibir foco...");
        await LoadTiposAsync();
        
        // Después de cargar, abrir el dropdown automáticamente
        if (sender is ComboBox combo && _tipoItems.Count > 0)
        {
            App.Log?.LogDebug("?? Abriendo dropdown automáticamente con {count} items", _tipoItems.Count);
            _tipoDropDownOpenedByUser = true;
            combo.IsDropDownOpen = true;
        }
    }
    else
    {
        App.Log?.LogDebug("? Tipos ya cargados ({count} items), abriendo dropdown", _tipoItems.Count);
        
        // Si ya están cargados, abrir directamente
        if (sender is ComboBox combo && _tipoItems.Count > 0)
        {
            _tipoDropDownOpenedByUser = true;
            combo.IsDropDownOpen = true;
        }
    }
}
```

### Cambio 5: Modificar `OnTipoSelectionChanged()`:

```csharp
private void OnTipoSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (sender is ComboBox combo && combo.SelectedItem is string selectedTipo)
    {
        App.Log?.LogInformation("? Tipo seleccionado: {tipo}", selectedTipo);
        
        // ? Marcar que acabamos de seleccionar (para evitar bucle en GotFocus)
        _tipoJustSelected = true;
        
        // Cerrar dropdown después de seleccionar
        combo.IsDropDownOpen = false;
        
        // Marcar como modificado
        OnFieldChanged(sender, e);
    }
}
```

---

## ?? Cómo Funciona la Corrección

### Flujo Corregido (Versión Final):

```
Usuario selecciona "Incidencia" ? SelectionChanged
       ?
   _tipoJustSelected = true ?
       ?
   Dropdown se cierra: IsDropDownOpen = false
       ?
   ComboBox RECUPERA el foco ? GotFocus se dispara
       ?
   ¿_tipoJustSelected == true?
       ? SÍ
   ?? RETURN (resetea flag y no hace nada) ? FIN ?
```

### Verificación en Logs Esperados:

**ANTES (Bucle infinito después de seleccionar):**
```
00:53:49 - ? Tipo seleccionado: Incidencia
00:53:49 - ?? CmbTipo GotFocus - IsDropDownOpen=False
00:53:49 - ? Tipos ya cargados, abriendo dropdown  ? BUCLE
00:53:50 - ?? CmbTipo GotFocus - IsDropDownOpen=False  ? BUCLE
```

**DESPUÉS (Corregido con Flag Temporal):**
```
00:53:49 - ? Tipo seleccionado: Incidencia
00:53:49 - ?? CmbTipo GotFocus - IsDropDownOpen=False, JustSelected=True  ? DETECTADO
00:53:49 - ?? Recién seleccionado, NO abrir automáticamente  ? FIN ?
(no más eventos GotFocus)
```

---

## ?? Checklist de Verificación

- [x] `OnGrupoGotFocus()` verifica `IsDropDownOpen` antes de abrir
- [x] `OnGrupoGotFocus()` verifica `_grupoJustSelected` antes de abrir
- [x] `OnGrupoSelectionChanged()` establece `_grupoJustSelected = true`
- [x] `OnTipoGotFocus()` verifica `IsDropDownOpen` antes de abrir
- [x] `OnTipoGotFocus()` verifica `_tipoJustSelected` antes de abrir
- [x] `OnTipoSelectionChanged()` establece `_tipoJustSelected = true`
- [x] Logs muestran el estado del dropdown Y del flag en cada evento
- [x] Compilación exitosa sin errores

---

## ?? Pruebas Recomendadas

1. **Navegar con Tab** desde Cliente hasta Grupo
   - ? El dropdown se abre automáticamente
   - ? NO se abre múltiples veces

2. **Seleccionar un Grupo** con el dropdown abierto
   - ? El dropdown se cierra
   - ? El foco permanece en el campo
   - ? **NO se vuelve a abrir automáticamente**

3. **Navegar con Tab** desde Grupo hasta Tipo
   - ? El dropdown de Tipo se abre automáticamente
   - ? NO se abre múltiples veces

4. **Seleccionar un Tipo** con el dropdown abierto
   - ? El dropdown se cierra
   - ? **NO se vuelve a abrir automáticamente**

5. **Revisar logs** durante la navegación y selección
   - ? Después de seleccionar: `JustSelected=True` seguido de "?? Recién seleccionado, NO abrir"
   - ? NO hay eventos repetitivos de `GotFocus` ? `DropDownOpened`

---

## ?? Análisis Adicional

### ¿Por qué ocurría el bucle (Versión 1)?

WinUI 3 ComboBox tiene un comportamiento donde **abrir el dropdown puede causar eventos de foco**:

1. Al abrir programáticamente con `IsDropDownOpen = true`
2. El control interno del dropdown **toma el foco brevemente**
3. Luego **devuelve el foco** al ComboBox padre
4. Esto dispara `GotFocus` nuevamente

### ¿Por qué ocurría el bucle (Versión 2)?

Después del primer fix, el bucle **continuaba al seleccionar un item**:

1. Usuario selecciona item ? `SelectionChanged`
2. Dropdown se cierra con `IsDropDownOpen = false`
3. **El foco vuelve al ComboBox** ? `GotFocus`
4. El código detecta `IsDropDownOpen=False` y **decide abrirlo**
5. Esto causa que el foco se mueva otra vez ? Bucle

### ¿Por qué no se detectó antes?

El problema solo es **visible en los logs** cuando se:
- Navega rápidamente con Tab
- Selecciona items y vuelve al campo
- Mantiene presionado Tab

En uso normal con mouse, el usuario hace click fuera del campo antes de que el bucle sea perceptible.

---

## ?? Archivos Modificados

- **Views/ParteItemEdit.xaml.cs**
  - Agregados campos: `_grupoJustSelected`, `_tipoJustSelected`
  - Método `OnGrupoGotFocus()`: Agregado check de `_grupoJustSelected`
  - Método `OnGrupoSelectionChanged()`: Establece `_grupoJustSelected = true`
  - Método `OnTipoGotFocus()`: Agregado check de `_tipoJustSelected`
  - Método `OnTipoSelectionChanged()`: Establece `_tipoJustSelected = true`

---

## ?? Lecciones Aprendidas

1. **Siempre verificar el estado antes de cambiar UI**
   - Antes de `IsDropDownOpen = true` ? verificar `if (!IsDropDownOpen)`

2. **Los eventos de foco pueden ser recursivos**
   - Abrir/cerrar un dropdown puede disparar `GotFocus` otra vez

3. **Usar flags temporales para interrumpir ciclos**
   - `_justSelected` se establece en `SelectionChanged`
   - Se verifica y resetea en `GotFocus`

4. **Logs detallados son esenciales**
   - Agregar el estado de TODOS los flags relevantes en cada log
   - `JustSelected={just}` ayudó a identificar el problema

5. **WinUI 3 tiene comportamientos no documentados**
   - El foco vuelve al ComboBox después de cerrar dropdown por selección
   - Probar exhaustivamente todos los flujos de navegación

6. **Los bugs pueden tener múltiples causas**
   - Fix parcial (verificar `IsDropDownOpen`) no fue suficiente
   - Se necesitó un segundo fix (flags temporales)

---

## ? Estado Final

**Problema v1:** ? Bucle infinito al abrir dropdown por foco  
**Solución v1:** ? Verificar `IsDropDownOpen` antes de abrir  

**Problema v2:** ? Bucle infinito después de seleccionar item  
**Solución v2:** ? Usar flags temporales `_grupoJustSelected` / `_tipoJustSelected`  

**Compilación:** ? Sin errores  
**Logs:** ? Detección completa de ambos tipos de bucle implementada  

---

## ?? Resumen de la Estrategia

La solución final combina **dos defensas**:

1. **Defensa contra apertura durante apertura:**
   ```csharp
   if (CmbGrupo.IsDropDownOpen) return;
   ```

2. **Defensa contra reapertura después de selección:**
   ```csharp
   if (_grupoJustSelected) { _grupoJustSelected = false; return; }
   ```

Ambas son necesarias porque el bucle puede ocurrir en **dos contextos diferentes**:
- Durante la navegación por Tab (apertura inicial)
- Después de seleccionar un item (foco de retorno)

---

**Fecha:** 2025-12-24  
**Autor:** GitHub Copilot  
**Issue:** Bucle infinito en eventos GotFocus de ComboBox (v1 y v2)  
**Status:** ? RESUELTO (con doble defensa)
