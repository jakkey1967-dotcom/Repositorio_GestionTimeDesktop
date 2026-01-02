# 📋 CÓMO USAR EL NUEVO DIÁLOGO EN DIARIOPAGE

## 🔧 MODIFICACIÓN EN `DiarioPage.xaml.cs`

Reemplaza el método `AskHoraCierreAsync()` actual con este código mejorado:

```csharp
using GestionTime.Desktop.Dialogs;

// ... existing code ...

/// <summary>
/// Muestra el diálogo mejorado para cerrar un parte
/// </summary>
private async Task<string?> AskHoraCierreAsync(ParteDto parte)
{
    try
    {
        // Crear instancia del diálogo mejorado
        var dialog = new CerrarParteDialog(parte)
        {
            XamlRoot = this.XamlRoot
        };
        
        App.Log?.LogInformation("🔒 Abriendo diálogo de cierre para parte ID: {id}", parte.Id);
        
        // Mostrar diálogo
        var result = await dialog.ShowAsync();
        
        // Verificar resultado
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(dialog.HoraCierreConfirmada))
        {
            App.Log?.LogInformation("✅ Hora de cierre confirmada: {hora}", dialog.HoraCierreConfirmada);
            return dialog.HoraCierreConfirmada;
        }
        else
        {
            App.Log?.LogInformation("❌ Usuario canceló el cierre del parte");
            return null;
        }
    }
    catch (Exception ex)
    {
        App.Log?.LogError(ex, "❌ Error mostrando diálogo de cierre");
        await ShowInfoAsync("Error mostrando diálogo. Intenta nuevamente.");
        return null;
    }
}
```

## 📝 ACTUALIZACIÓN DEL MÉTODO `OnCerrarClick`

El método `OnCerrarClick` ya existente solo necesita cambiar la llamada:

```csharp
private async void OnCerrarClick(object sender, RoutedEventArgs e)
{
    if (sender is not MenuFlyoutItem menuItem || menuItem.Tag is not int parteId)
    {
        return;
    }

    var parte = Partes.FirstOrDefault(p => p.Id == parteId);
    if (parte == null || !parte.CanCerrar)
    {
        return;
    }

    try
    {
        App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        App.Log?.LogInformation("🔒 CERRAR PARTE - ID: {id}", parteId);
        App.Log?.LogInformation("   Estado ANTES: {estado} (EstadoInt={int}, IsAbierto={abierto})", 
            parte.EstadoTexto, parte.EstadoInt, parte.IsAbierto);
        App.Log?.LogInformation("   HoraInicio: {inicio}, HoraFin: {fin}", parte.HoraInicio, parte.HoraFin);
        
        // 🆕 USAR EL NUEVO DIÁLOGO (pasa el objeto parte completo)
        var horaFin = await AskHoraCierreAsync(parte);
        
        if (string.IsNullOrEmpty(horaFin))
        {
            App.Log?.LogInformation("Usuario canceló el cierre del parte");
            return;
        }
        
        App.Log?.LogInformation("   Hora de cierre confirmada por usuario: {hora}", horaFin);
        
        // ... resto del código existente (PUT/POST) ...
        
        // 🆕 NUEVO: Intentar con PUT completo primero (más confiable)
        try
        {
            // Payload completo para PUT /api/v1/partes/{id}
            var putPayload = new 
            {
                fecha_trabajo = parte.Fecha.ToString("yyyy-MM-dd"),
                hora_inicio = parte.HoraInicio,
                hora_fin = horaFin,
                id_cliente = parte.IdCliente,
                tienda = parte.Tienda ?? "",
                id_grupo = parte.IdGrupo,
                id_tipo = parte.IdTipo,
                accion = parte.Accion ?? "",
                ticket = parte.Ticket ?? "",
                estado = 2  // Cerrado
            };
            
            App.Log?.LogInformation("   🔄 Intentando PUT completo a: /api/v1/partes/{id}", parteId);
            App.Log?.LogDebug("   Payload: {@payload}", putPayload);
            
            await App.Api.PutAsync<object, object>($"/api/v1/partes/{parteId}", putPayload);
            
            App.Log?.LogInformation("✅ Parte {id} cerrado correctamente usando PUT con HoraFin={hora}", parteId, horaFin);
        }
        catch (Exception putEx)
        {
            App.Log?.LogWarning(putEx, "PUT completo falló, intentando POST /close...");
            
            try
            {
                var endpoint = $"/api/v1/partes/{parteId}/close?horaFin={Uri.EscapeDataString(horaFin)}";
                App.Log?.LogInformation("   Enviando POST a: {endpoint}", endpoint);
                
                await App.Api.PostAsync(endpoint);
                
                App.Log?.LogInformation("✅ Parte {id} cerrado correctamente usando POST con HoraFin={hora}", parteId, horaFin);
            }
            catch (Exception postEx)
            {
                App.Log?.LogError(postEx, "❌ Ambos métodos fallaron (PUT y POST)");
                throw;
            }
        }
        
        // 🆕 NUEVO: Invalidar caché después de cerrar el parte
        App.Log?.LogInformation("🗑️ Invalidando caché de partes...");
        InvalidatePartesCache(parte.Fecha);
        
        // CRUCIAL: Esperar un momento antes de recargar para asegurar que el backend procesó el cambio
        App.Log?.LogInformation("⏳ Esperando 500ms antes de recargar...");
        await Task.Delay(500);
        
        App.Log?.LogInformation("🔄 Recargando lista de partes...");
        await LoadPartesAsync();
        
        App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
    }
    catch (Exception ex)
    {
        App.Log?.LogError(ex, "Error cerrando parte {id}", parteId);
        await ShowInfoAsync($"❌ Error cerrando parte: {ex.Message}");
    }
}
```

## ✅ VENTAJAS DEL NUEVO DIÁLOGO

### **1. Información Contextual Rica:**
```
✅ Muestra fecha, cliente, tienda
✅ Hora de inicio destacada y NO editable
✅ Chips visuales para Ticket, Grupo, Tipo
```

### **2. Validación Robusta:**
```
✅ Solo permite dígitos y dos puntos
✅ Auto-formatea mientras escribes (93 → 9:3 → 9:30)
✅ Validación con Regex: ^([01]\d|2[0-3]):[0-5]\d$
✅ Botón "Cerrar" deshabilitado si hora inválida
✅ Mensajes visuales de error/éxito
```

### **3. UX Mejorada:**
```
✅ Pre-rellenado con hora actual
✅ Botón "Ahora" para rapidez
✅ Focus automático con selección
✅ Tecla Enter para confirmar (después de validar)
✅ Colores consistentes con el tema de la app
```

### **4. Logs Detallados:**
```
[INFO] 📋 Diálogo CerrarParte abierto - Parte ID: 123, HoraInicio: 09:30
[DEBUG] ✅ Hora válida: 14:30
[INFO] ✅ Diálogo cerrado - Hora de cierre confirmada: 14:30
```

## 🎨 PREVIEW VISUAL

```
╔══════════════════════════════════════════════════╗
║           🔒 Cerrar Parte                        ║
╠══════════════════════════════════════════════════╣
║                                                  ║
║  ┌────────────────────────────────────────────┐ ║
║  │ 📋 Información del Parte                   │ ║
║  │                                            │ ║
║  │ 📅 Fecha:      02/01/2026                 │ ║
║  │ 👤 Cliente:    ACME Corporation           │ ║
║  │ 🏪 Tienda:     Madrid Centro              │ ║
║  │ 🕐 Inicio:     ┌─────────┐                │ ║
║  │                │  09:30  │ (DESTACADO)    │ ║
║  │                └─────────┘                │ ║
║  │                                            │ ║
║  │ [🎫 TK-1234] [📁 Sistemas] [🏷️ Mantenim.] │ ║
║  └────────────────────────────────────────────┘ ║
║                                                  ║
║  ┌────────────────────────────────────────────┐ ║
║  │ ⏰ Hora de Cierre                          │ ║
║  │                                            │ ║
║  │ ┌──────────────────┐  ┌──────────┐        │ ║
║  │ │     14:30        │  │ 🕐 Ahora │        │ ║
║  │ └──────────────────┘  └──────────┘        │ ║
║  │                                            │ ║
║  │ 💡 Formato: HH:mm (ejemplo: 14:30)        │ ║
║  │                                            │ ║
║  │ ┌────────────────────────────────────────┐│ ║
║  │ │ ✓ Hora válida ✓                       ││ ║
║  │ └────────────────────────────────────────┘│ ║
║  └────────────────────────────────────────────┘ ║
║                                                  ║
║              [✅ Cerrar]  [❌ Cancelar]          ║
╚══════════════════════════════════════════════════╝
```

## 🚀 CÓMO COMPILAR Y PROBAR

1. **Agregar `using`:**
   ```csharp
   using GestionTime.Desktop.Dialogs;
   ```

2. **Reemplazar método:**
   ```csharp
   private async Task<string?> AskHoraCierreAsync(ParteDto parte)
   {
       var dialog = new CerrarParteDialog(parte) { XamlRoot = this.XamlRoot };
       var result = await dialog.ShowAsync();
       return result == ContentDialogResult.Primary ? dialog.HoraCierreConfirmada : null;
   }
   ```

3. **Compilar:**
   ```
   Build > Rebuild Solution (Ctrl+Shift+B)
   ```

4. **Probar:**
   - Seleccionar un parte abierto
   - Click derecho → "Cerrar"
   - Verificar que se muestra el nuevo diálogo
   - Probar validaciones:
     * Escribir "99:99" → debe marcar error
     * Escribir "14:30" → debe marcar válido
     * Click "Ahora" → debe poner hora actual
     * Click "Cancelar" → debe cerrar sin cambios
     * Click "Cerrar" → debe cerrar el parte

## 📊 LOGS ESPERADOS

```
[INFO] 🔒 CERRAR PARTE - ID: 123
[INFO] 📋 Diálogo CerrarParte abierto - Parte ID: 123, HoraInicio: 09:30
[DEBUG] 🔧 Auto-formato aplicado: '93' → '9:3'
[DEBUG] ✅ Hora válida: 14:30
[INFO] ✅ Diálogo cerrado - Hora de cierre confirmada: 14:30
[INFO] 🔄 Intentando PUT completo a: /api/v1/partes/123
[INFO] ✅ Parte 123 cerrado correctamente usando PUT con HoraFin=14:30
[INFO] 🗑️ Invalidando caché de partes...
[INFO] ✅ Caché de partes invalidado correctamente
[INFO] ⏳ Esperando 500ms antes de recargar...
[INFO] 🔄 Recargando lista de partes...
```

## ✅ CHECKLIST FINAL

- [x] XAML del diálogo creado
- [x] Code-behind con validación completa
- [x] Auto-formato mientras escribes
- [x] Validación Regex HH:mm
- [x] Botón "Ahora" funcional
- [x] Deshabilitar "Cerrar" si inválido
- [x] Mostrar datos del parte original
- [x] Chips visuales para detalles
- [x] Logs detallados
- [x] Manejo de errores robusto
- [x] Estilo consistente con la app

---

**🎉 ¡El nuevo diálogo está listo para usar!**
