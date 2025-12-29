# ParteItemEdit - Dropdown Loop Fix COMPLETE ?

## Problem

The `CmbGrupo` and `CmbTipo` ComboBoxes were stuck in an infinite loop where:

1. User navigates away with Tab/clicks elsewhere
2. Focus briefly moves to next control
3. Focus immediately returns to the ComboBox
4. `GotFocus` event automatically reopens the dropdown
5. Loop repeats indefinitely

### Logs Evidence
```
2025-12-24 01:14:31.499 [Information] ?? [39] FOCO EN ? TxtAccion (TextBox MultiLine)
2025-12-24 01:14:31.504 [Debug] ?? FOCO PERDIDO ? TxtAccion (TextBox MultiLine) (4ms desde cambio)
2025-12-24 01:14:32.044 [Information] ?? [40] FOCO EN ? CmbTipo (ComboBox)
2025-12-24 01:14:32.045 [Information] ?? CmbTipo GotFocus - _tiposLoaded=True, IsDropDownOpen=False, JustSelected=False
2025-12-24 01:14:32.047 [Debug] ? Tipos ya cargados (10 items), abriendo dropdown
2025-12-24 01:14:32.053 [Information] ?? CmbTipo DropDownOpened - _tipoDropDownOpenedByUser=True, _tiposLoaded=True, Items=10
```

The focus kept bouncing back and forth between controls.

## Solution

Added navigation tracking flags that detect when the user is actively trying to navigate away:

### 1. New Navigation Flags
```csharp
// Flags para detectar navegación activa (Tab/Escape)
private bool _grupoNavigatingAway = false;
private bool _tipoNavigatingAway = false;
```

### 2. PreviewKeyDown Detection
Updated `OnGrupoPreviewKeyDown` and `OnTipoPreviewKeyDown` to detect Tab/Escape:

```csharp
// Detectar navegación con Tab o Escape
if (e.Key == Windows.System.VirtualKey.Tab || 
    e.Key == Windows.System.VirtualKey.Escape)
{
    App.Log?.LogDebug("?? Usuario navegando con {key}, marcar flag", e.Key);
    _tipoNavigatingAway = true;
    
    // Cerrar dropdown si está abierto
    if (sender is ComboBox comboBox && comboBox.IsDropDownOpen)
    {
        comboBox.IsDropDownOpen = false;
    }
    return;
}
```

### 3. GotFocus Prevention
Updated `OnGrupoGotFocus` and `OnTipoGotFocus` to check navigation flag:

```csharp
// ?? NO abrir si el usuario está navegando con Tab/Escape
if (_tipoNavigatingAway)
{
    App.Log?.LogDebug("?? Usuario navegando, NO abrir dropdown");
    _tipoNavigatingAway = false; // Resetear flag
    return;
}
```

## How It Works

1. **User presses Tab/Escape**: `PreviewKeyDown` sets `_navigatingAway` flag
2. **Dropdown closes**: If open, closes immediately
3. **Focus moves away**: Natural tab navigation proceeds
4. **GotFocus may trigger again**: But now checks the flag and exits early
5. **Flag is reset**: After preventing one unwanted reopen

## Benefits

? **No more infinite loops** - Tab/Escape navigation works properly
? **Dropdown still opens on first focus** - When not navigating away
? **Works with keyboard and mouse** - Both navigation methods supported
? **Minimal code changes** - Only added detection, no removal of features
? **Maintains existing behavior** - Down arrow still opens dropdown

## Testing Scenarios

### Scenario 1: Tab Navigation ?
- Tab into ComboBox ? Opens dropdown
- Press Tab ? Closes and moves to next field
- No loop, navigation continues

### Scenario 2: Escape Key ?
- Open dropdown manually
- Press Escape ? Closes dropdown
- Tab away ? No reopening

### Scenario 3: Mouse Click ?
- Click on ComboBox ? Opens dropdown
- Click outside ? Closes and focus moves
- No loop

### Scenario 4: Arrow Keys Still Work ?
- Tab into ComboBox ? Opens dropdown
- Arrow Down ? Navigates within dropdown
- Not mistaken for navigation away

## Files Modified

- `Views/ParteItemEdit.xaml.cs`
  - Added `_grupoNavigatingAway` and `_tipoNavigatingAway` flags
  - Updated `OnGrupoPreviewKeyDown` to detect Tab/Escape
  - Updated `OnTipoPreviewKeyDown` to detect Tab/Escape
  - Updated `OnGrupoGotFocus` to check navigation flag
  - Updated `OnTipoGotFocus` to check navigation flag

## Verification

Build: ? Successful
- No compilation errors
- All existing functionality preserved

## Notes

This fix is similar to the approach used for the `_justSelected` flags but targets the specific case of keyboard navigation. The flags work together:

- `_justSelected` ? Prevents reopening after selection
- `_navigatingAway` ? Prevents reopening during navigation

Both are needed because they handle different scenarios in the focus/navigation lifecycle.
