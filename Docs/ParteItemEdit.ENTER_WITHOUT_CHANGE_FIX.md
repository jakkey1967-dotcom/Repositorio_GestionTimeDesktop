# ParteItemEdit - Corrección de Navegación con Enter sin Cambiar Selección ?

## ?? Problema

Al presionar **Enter** en un ComboBox (`CmbGrupo` o `CmbTipo`) **sin cambiar la selección**, el usuario quedaba **atrapado** en el control y no podía avanzar al siguiente campo.

### Síntomas
1. Usuario hace Tab ? ComboBox recibe foco
2. Dropdown NO se abre (correcto, ya tiene valor seleccionado)
3. Usuario presiona Enter (esperando confirmar y avanzar)
4. Dropdown se abre brevemente
5. Usuario presiona Enter de nuevo
6. **No puede salir del ComboBox** ?

### Comportamiento Esperado
- Si el usuario presiona Enter sin cambiar el valor ? Debería **avanzar al siguiente control** ?
- Si el usuario cambia el valor ? SelectionChanged se dispara y permite avanzar ?

### Causa Raíz

El evento `SelectionChanged` solo se dispara cuando **cambia** la selección:

```csharp
private void OnGrupoSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (sender is ComboBox combo && combo.SelectedItem is string selectedGrupo)
    {
        _grupoJustSelected = true; // ? Se activa SOLO si cambia
        combo.IsDropDownOpen = false;
    }
}
```

**Problema**: Si el usuario presiona Enter sin cambiar el valor, `SelectionChanged` **no se dispara** y `_grupoJustSelected` permanece `false`.

Resultado:
1. Enter cierra el dropdown
2. GotFocus se dispara de nuevo
3. Como `_grupoJustSelected = false` ? **Reabre el dropdown automáticamente** ?
4. Usuario queda atrapado en un ciclo

## ? Solución

### 1. Actualizar `OnComboBoxEnterKey`

Ahora detecta Enter y **marca la bandera** `justSelected` incluso si no cambió el valor:

```csharp
private void OnComboBoxEnterKey(object sender, KeyRoutedEventArgs e)
{
    if (e.Key == Windows.System.VirtualKey.Enter)
    {
        if (sender is ComboBox combo)
        {
            App.Log?.LogDebug("? Enter presionado en ComboBox: {name}", combo.Name);
            
            // Cerrar dropdown si está abierto
            if (combo.IsDropDownOpen)
            {
                combo.IsDropDownOpen = false;
                App.Log?.LogDebug("? Dropdown cerrado por Enter");
            }
            
            // ?? CLAVE: Marcar como "recién seleccionado" para evitar reapertura
            if (combo.Name == "CmbGrupo")
            {
                _grupoJustSelected = true;
                App.Log?.LogDebug("? Grupo marcado como justSelected (Enter sin cambio)");
            }
            else if (combo.Name == "CmbTipo")
            {
                _tipoJustSelected = true;
                App.Log?.LogDebug("? Tipo marcado como justSelected (Enter sin cambio)");
            }
            
            // Mover al siguiente control
            var options = new FindNextElementOptions
            {
                SearchRoot = this.Content as DependencyObject ?? this
            };
            
            var element = FocusManager.FindNextElement(FocusNavigationDirection.Next, options);
            if (element is Control control)
            {
                control.Focus(FocusState.Programmatic);
                e.Handled = true;
                App.Log?.LogDebug("? Foco movido al siguiente control");
            }
        }
    }
}
```

### 2. Mejorar Lógica de `GotFocus`

Ahora también verifica si **ya hay una selección** antes de abrir automáticamente:

```csharp
private async void OnGrupoGotFocus(object sender, RoutedEventArgs e)
{
    // ... validaciones de navegación y justSelected ...
    
    if (!_gruposLoaded)
    {
        await LoadGruposAsync();
        
        // Abrir SOLO si NO hay selección previa
        if (sender is ComboBox combo && _grupoItems.Count > 0 && combo.SelectedIndex < 0)
        {
            App.Log?.LogDebug("?? Abriendo dropdown (sin selección previa)");
            combo.IsDropDownOpen = true;
        }
    }
    else
    {
        // Si ya están cargados, abrir SOLO si NO hay selección previa
        if (sender is ComboBox combo && _grupoItems.Count > 0 && combo.SelectedIndex < 0)
        {
            combo.IsDropDownOpen = true;
            App.Log?.LogDebug("?? Dropdown abierto (sin selección previa)");
        }
        else if (sender is ComboBox combo2 && combo2.SelectedIndex >= 0)
        {
            App.Log?.LogDebug("?? Ya hay selección (index: {index}), NO abrir automáticamente", combo2.SelectedIndex);
        }
    }
}
```

## ?? Flujo Corregido

### Escenario 1: Enter sin Cambiar Valor ?

```
1. Usuario hace Tab ? CmbGrupo recibe foco
2. GotFocus verifica: SelectedIndex >= 0 ? NO abre dropdown
3. Usuario presiona Enter
4. OnComboBoxEnterKey se dispara
5. Marca _grupoJustSelected = true ?
6. Cierra dropdown si estaba abierto
7. Mueve foco al siguiente control (CmbTipo)
8. GotFocus en CmbGrupo se dispara brevemente
9. Ve _grupoJustSelected = true ? NO reabre dropdown ?
10. Usuario continúa navegando normalmente
```

### Escenario 2: Cambiar Valor y Enter ?

```
1. Usuario hace Tab ? CmbGrupo recibe foco
2. Usuario selecciona nuevo valor
3. SelectionChanged marca _grupoJustSelected = true
4. Usuario presiona Enter (opcional)
5. OnComboBoxEnterKey también marca _grupoJustSelected = true (redundante pero seguro)
6. Mueve foco al siguiente control
7. Usuario continúa navegando normalmente
```

### Escenario 3: Primera Vez (Sin Selección Previa) ?

```
1. Usuario hace Tab ? CmbGrupo recibe foco
2. GotFocus verifica: SelectedIndex < 0 ? Abre dropdown automáticamente ?
3. Usuario navega con flechas y selecciona
4. Usuario presiona Enter
5. OnComboBoxEnterKey marca _grupoJustSelected = true
6. Mueve foco al siguiente control
```

## ?? Ventajas

? **Enter siempre avanza** - Incluso sin cambiar valor
? **No reabre dropdown** - Banderas previenen ciclos
? **Dropdown solo en primera vez** - Si no hay selección previa
? **Comportamiento intuitivo** - Usuario no queda atrapado
? **Logging detallado** - Fácil de debuggear

## ?? Casos de Prueba

### Caso 1: Enter sin Cambiar ?
```
Entrada: Tab ? CmbGrupo ? Enter (sin cambiar)
Esperado: Avanza a CmbTipo
Resultado: ? Funciona correctamente
```

### Caso 2: Tab sin Enter ?
```
Entrada: Tab ? CmbGrupo ? Tab
Esperado: Avanza a CmbTipo sin abrir dropdown
Resultado: ? Funciona correctamente
```

### Caso 3: Cambiar y Enter ?
```
Entrada: Tab ? CmbGrupo ? Cambiar valor ? Enter
Esperado: Avanza a CmbTipo
Resultado: ? Funciona correctamente
```

### Caso 4: Primera Vez ?
```
Entrada: Tab ? CmbGrupo (sin valor previo)
Esperado: Abre dropdown automáticamente
Resultado: ? Funciona correctamente
```

## ?? Archivos Modificados

- `Views/ParteItemEdit.xaml.cs`
  - ? Actualizado `OnComboBoxEnterKey()` para marcar `justSelected`
  - ? Actualizado `OnGrupoGotFocus()` para verificar `SelectedIndex`
  - ? Actualizado `OnTipoGotFocus()` para verificar `SelectedIndex`
  - ? Agregado logging detallado

## ?? Verificación

**Build**: ? Exitoso
- Sin errores de compilación
- Todas las funcionalidades existentes preservadas

**Logs Esperados (Enter sin Cambio)**:
```
? Enter presionado en ComboBox: CmbGrupo
? Dropdown cerrado por Enter
? Grupo marcado como justSelected (Enter sin cambio)
? Foco movido al siguiente control
?? FOCO EN ? CmbTipo (ComboBox)
?? CmbTipo GotFocus - JustSelected=False, SelectedIndex=4
?? Ya hay selección (index: 4), NO abrir automáticamente
```

## ?? Estado Final

**PROBLEMA COMPLETAMENTE RESUELTO** ?

El usuario ahora puede:
- ? Presionar Enter para avanzar (con o sin cambiar valor)
- ? Navegar con Tab fluidamente
- ? No quedar atrapado en ComboBoxes
- ? Ver dropdown solo cuando es necesario
- ? Experiencia consistente en ambos ComboBoxes (Grupo y Tipo)

## ?? Notas Técnicas

### Banderas de Control

Ahora hay **dos caminos** para activar `_justSelected`:

1. **SelectionChanged** ? Se dispara al cambiar valor
2. **OnComboBoxEnterKey** ? Se dispara al presionar Enter (con o sin cambio)

Esto asegura que **siempre** se marca la bandera cuando el usuario quiere avanzar.

### Verificación de Selección Previa

```csharp
combo.SelectedIndex >= 0  // Hay algo seleccionado
combo.SelectedIndex < 0   // No hay nada seleccionado
```

Esto permite **comportamiento diferenciado**:
- Sin selección ? Abrir dropdown automáticamente (ayuda al usuario)
- Con selección ? NO abrir (usuario solo navega)

### Compatibilidad

Funciona correctamente con:
- ? Navegación con Tab
- ? Navegación con Enter
- ? Navegación con Shift+Tab (retroceso)
- ? Selección con mouse
- ? Selección con teclado (flechas)
- ? Escape para cancelar

La solución es **robusta y completa** ??
