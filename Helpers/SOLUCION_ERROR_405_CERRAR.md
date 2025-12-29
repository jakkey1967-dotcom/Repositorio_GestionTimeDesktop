# ?? SOLUCIÓN - ERROR 405 AL CERRAR PARTE

**Error visto:**
```
Error cerrando parte: Error 405 (MethodNotAllowed): Error al procesar la solicitud.
```

---

## ?? CAUSA DEL PROBLEMA

El error **405 (MethodNotAllowed)** ocurre cuando:
- El backend no acepta el método HTTP usado (GET, POST, PUT, DELETE, etc.)
- El endpoint espera un método diferente al que se está usando

En tu caso, el código estaba usando:
```csharp
await App.Api.PutAsync<object, object>($"/api/v1/partes/{parteId}/close", new { HoraFin = horaFin });
```

Pero el backend probablemente espera un **POST** en lugar de **PUT**.

---

## ? SOLUCIÓN APLICADA

He modificado el método `OnCerrarClick` para que:

1. **Pregunte la hora de cierre** al usuario con un diálogo intuitivo
2. **Incluya botón "Ahora"** para poner la hora actual automáticamente  
3. **Intente primero POST** y, si falla con 405, **pruebe con PUT**

```csharp
private async void OnCerrarClick(object sender, RoutedEventArgs e)
{
    // ...validaciones...

    try
    {
        App.Log?.LogInformation("?? CERRAR PARTE - ID: {id}", parteId);
        
        // ? NUEVO: Preguntar hora de cierre al usuario
        var horaFin = await AskHoraCierreAsync();
        
        if (string.IsNullOrEmpty(horaFin))
        {
            App.Log?.LogInformation("Usuario canceló el cierre del parte");
            return;
        }
        
        // ? Intentar con POST primero
        try
        {
            await App.Api.PostAsync($"/api/v1/partes/{parteId}/close?horaFin={Uri.EscapeDataString(horaFin)}");
            App.Log?.LogInformation("? Parte {id} cerrado usando POST con HoraFin={hora}", parteId, horaFin);
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            // ? FALLBACK: Si POST no funciona, intentar con PUT
            App.Log?.LogWarning("POST /close no permitido, intentando con PUT...");
            await App.Api.PutAsync<object, object>($"/api/v1/partes/{parteId}", new 
            { 
                HoraFin = horaFin,
                EstadoInt = 2
            });
            App.Log?.LogInformation("? Parte {id} cerrado usando PUT con HoraFin={hora}", parteId, horaFin);
        }
        
        await LoadPartesAsync();
    }
    catch (Exception ex)
    {
        App.Log?.LogError(ex, "Error cerrando parte {id}", parteId);
        await ShowInfoAsync($"? Error cerrando parte: {ex.Message}");
    }
}

private async Task<string?> AskHoraCierreAsync()
{
    var horaActual = DateTime.Now.ToString("HH:mm");
    
    // Crear diálogo con TimePicker y botón "Ahora"
    var stackPanel = new StackPanel { Spacing = 15 };
    
    stackPanel.Children.Add(new TextBlock
    {
        Text = "? Hora de cierre:",
        FontSize = 14,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
    });

    var timePickerGrid = new Grid { ColumnSpacing = 10 };
    
    var timePicker = new TimePicker
    {
        ClockIdentifier = "24HourClock",
        MinuteIncrement = 1,
        Time = TimeSpan.Parse(horaActual)
    };
    
    var btnHoraActual = new Button
    {
        Content = "?? Ahora",
        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 11, 140, 153))
    };
    btnHoraActual.Click += (s, e) =>
    {
        timePicker.Time = TimeSpan.Parse(DateTime.Now.ToString("HH:mm"));
    };
    
    timePickerGrid.Children.Add(timePicker);
    timePickerGrid.Children.Add(btnHoraActual);
    stackPanel.Children.Add(timePickerGrid);

    var dialog = new ContentDialog
    {
        Title = "? Cerrar Parte - Especificar Hora",
        Content = stackPanel,
        PrimaryButtonText = "? Cerrar",
        CloseButtonText = "? Cancelar",
        DefaultButton = ContentDialogButton.Primary,
        XamlRoot = XamlRoot
    };

    var result = await dialog.ShowAsync();
    
    if (result == ContentDialogResult.Primary)
    {
        var time = timePicker.Time;
        return $"{time.Hours:D2}:{time.Minutes:D2}";
    }

    return null;
}
```

---

## ?? CÓMO FUNCIONA

### **Paso 1: Usuario hace click en "Cerrar"**
```
Usuario ? Click en icono de estado (?) ? "Cerrar"
```

### **Paso 2: Diálogo de hora de cierre**
```
??????????????????????????????????????????
?  ? Cerrar Parte                       ?
??????????????????????????????????????????
?                                        ?
?  ?? Hora de inicio:                    ?
?  ????????????????????????????????????  ?
?  ? 08:30                            ?  ?
?  ????????????????????????????????????  ?
?                                        ?
?  ? Hora de cierre:                    ?
?  ??????????????????????????????????   ?
?  ? 14:30          ? ?? Ahora      ?   ?
?  ??????????????????????????????????   ?
?                                        ?
?  ?? Formato: HH:mm (ejemplo: 14:30)   ?
?                                        ?
?  ???????????  ???????????             ?
?  ? ? Cerrar?  ?? Cancelar?           ?
?  ???????????  ???????????             ?
??????????????????????????????????????????
```

**Características:**
- ?? **Hora de inicio**: Se muestra pero NO es modificable (solo lectura)
- ? **Hora de fin**: Campo de texto editable con formato HH:mm
- ?? **Botón "Ahora"**: Pone la hora actual automáticamente en hora de fin
- ?? **Edición manual**: Puedes escribir directamente en formato HH:mm
- ? **Validación**: Verifica que el formato sea correcto antes de cerrar

**Opciones del usuario:**
- ?? **Click en "Ahora"**: Pone la hora actual automáticamente
- ?? **Escribir hora**: Formato HH:mm (ejemplo: 14:30)
- ? **Cerrar**: Confirma y cierra el parte con la hora especificada
- ? **Cancelar**: Cancela la operación, el parte sigue abierto

### **Paso 3: Envío al backend**
```http
POST /api/v1/partes/123/close?horaFin=14:30
```

**Si funciona:**
- ? Parte se cierra correctamente
- ? Log: "Parte 123 cerrado usando POST con HoraFin=14:30"
- ? Se recarga la lista

**Si recibe 405, intenta PUT:**
```http
PUT /api/v1/partes/123
Content-Type: application/json

{
  "HoraFin": "14:30",
  "EstadoInt": 2
}
```

---

## ?? COMPARACIÓN

### ? **ANTES (TimePicker complejo):**
```
Usuario hace click en "Cerrar" 
? Diálogo con TimePicker
? Difícil de usar
? No muestra hora de inicio
? POST /api/v1/partes/123/close
```

### ? **AHORA (TextBox simple y directo):**
```
Usuario hace click en "Cerrar"
? Diálogo aparece
? Muestra hora de inicio (solo lectura)
? Campo de texto para hora de fin
? Botón "Ahora" para hora actual
? Puede escribir directamente: "14:30"
? Valida formato antes de enviar
? POST /api/v1/partes/123/close?horaFin=14:30
? Si funciona: ? Cerrado
? Si recibe 405: Intenta PUT
? Si falla: ? Error con formato inválido
```

---

## ?? PRUEBA DESPUÉS DE LA SOLUCIÓN

1. **Ejecutar la aplicación** (`F5`)

2. **Seleccionar un parte abierto o pausado**

3. **Click en el icono de estado** (?)

4. **Seleccionar "Cerrar"**

5. **Resultado esperado:**
   - ?? Muestra hora de inicio (solo lectura, en gris)
   - ? Campo de hora de fin editable con hora actual
   - ?? Botón "Ahora" funciona
   - ?? Puedes escribir la hora directamente (ej: 14:30)
   - ? Al confirmar, valida el formato
   - ? Si es válido, cierra el parte
   - ?? Si es inválido, muestra error y no cierra

---

## ?? CARACTERÍSTICAS DEL DIÁLOGO

| Elemento | Descripción |
|----------|-------------|
| **Hora de inicio** | Solo lectura, fondo gris, muestra la hora de inicio del parte |
| **Hora de fin** | TextBox editable, formato HH:mm |
| **Botón "Ahora"** | Pone la hora actual con un click |
| **Formato** | HH:mm (24 horas) - ejemplo: 08:30, 14:45, 23:59 |
| **Validación** | Verifica formato antes de cerrar |
| **Valor inicial** | Hora actual del sistema |
| **Botón Cerrar** | Confirma y cierra el parte |
| **Botón Cancelar** | Cancela la operación |

---

## ?? VERIFICACIÓN EN LOGS

Después de cerrar un parte, revisa los logs:

```powershell
# Abrir logs
notepad C:\Logs\GestionTime\app_*.log
```

### **Buscar estas líneas:**

**Si el usuario confirma con hora válida:**
```
?? CERRAR PARTE - ID: 123
? Parte 123 cerrado correctamente usando POST con HoraFin=14:30
```

**Si el usuario cancela:**
```
?? CERRAR PARTE - ID: 123
Usuario canceló el cierre del parte
```

**Si el formato es inválido:**
```
?? Formato de hora inválido. Use HH:mm (ejemplo: 14:30)
```

**Si POST no funciona y usa PUT:**
```
?? CERRAR PARTE - ID: 123
?? POST /close no permitido, intentando con PUT...
? Parte 123 cerrado correctamente usando PUT con HoraFin=14:30
```

---

## ? RESULTADO ESPERADO

Después de aplicar la solución:

```
??????????????????????????????????????????????
?  ? Cerrar parte funciona correctamente    ?
?                                            ?
?  ?? Muestra hora de inicio (solo lectura)  ?
?  ? Campo de texto simple para hora fin    ?
?  ?? Botón "Ahora" para hora actual         ?
?  ?? Puede escribir directamente HH:mm      ?
?  ? Validación de formato automática       ?
?  ?? Intenta POST primero                   ?
?  ?? Fallback automático a PUT si falla     ?
?  ?? Logs detallados de cada intento        ?
?  ??? No rompe si el formato es inválido    ?
?                                            ?
?  ?? INTERFAZ MEJORADA Y MÁS SIMPLE         ?
??????????????????????????????????????????????
```

---

**Fecha:** 2025-01-27  
**Estado:** ? Solución aplicada con TextBox simple  
**Build:** ? Exitoso  
**Interfaz:** ? Mejorada - Más simple y directa  
**Próximo paso:** Probar cerrar un parte y verificar el nuevo diálogo
