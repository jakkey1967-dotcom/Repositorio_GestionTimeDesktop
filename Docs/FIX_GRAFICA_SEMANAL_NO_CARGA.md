# 🔧 Fix: Gráfica Semanal No Se Carga

**Fecha:** 2026-02-14  
**Versión:** v1.9.5-alpha  
**Estado:** ✅ Fix implementado

---

## 🐛 Problema Reportado

La gráfica semanal en ReportsWindow no se muestra después de realizar una búsqueda.

---

## 🔍 Causas Identificadas

### 1. **Catch Silencioso Sin Logging**
```csharp
catch
{
    WeekChartMessage = "Error al cargar gráfica semanal";  // ❌ No loggea el error real
}
```
**Problema:** No se puede diagnosticar qué está fallando realmente.

### 2. **Modificación de UI desde Thread Incorrecto**
```csharp
// ❌ INCORRECTO: Modifica ObservableCollection desde thread de API
BuildWeekChartFromByDay(Resumen.ByDay);
ShowWeekChart = true;
```
**Problema:** WinUI 3 requiere que las colecciones observables se modifiquen desde el UI thread.

### 3. **Falta de Mensaje Cuando No Hay Datos**
Si `weekData.ByDay` es null o vacío, no se muestra ningún feedback al usuario.

---

## ✅ Soluciones Implementadas

### 1. Logging de Excepciones
```csharp
catch (Exception ex)
{
    WeekChartMessage = $"Error al cargar gráfica: {ex.Message}";
    System.Diagnostics.Debug.WriteLine($"[WeekChart] Error: {ex}");
}
```
**Beneficio:** Ahora el usuario ve el mensaje de error exacto y aparece en el Output de Visual Studio.

### 2. Dispatcher para Modificaciones de UI
```csharp
if (weekData?.ByDay != null && weekData.ByDay.Count > 0)
{
    _dispatcher.TryEnqueue(() =>
    {
        BuildWeekChartFromByDay(weekData.ByDay);
        ShowWeekChart = true;
    });
}
```
**Beneficio:** Garantiza que la UI se actualiza desde el thread correcto.

### 3. Mensaje Cuando No Hay Datos
```csharp
else
{
    WeekChartMessage = "No hay datos disponibles para esta semana";
}
```
**Beneficio:** Usuario sabe que no hubo error, simplemente no hay datos.

---

## 📦 Archivos Modificados

### `ViewModels/Reports/ReportsViewModel.cs`

**Método:** `LoadWeekChartIfNeededAsync()`

**Cambios:**
1. ✅ Catch con logging de excepción completa
2. ✅ Uso de `_dispatcher.TryEnqueue()` para modificar UI
3. ✅ Mensaje cuando no hay datos
4. ✅ Debug.WriteLine para troubleshooting

**Código actualizado:**
```csharp
// GT-BEGIN: Carga de gráfica semanal
private async Task LoadWeekChartIfNeededAsync()
{
    try
    {
        WeekChartItems.Clear();
        ShowWeekChart = false;
        WeekChartMessage = string.Empty;

        if (Scope == "range" || Resumen == null)
        {
            WeekChartMessage = "Gráfica semanal disponible en Día/Semana";
            return;
        }

        string? weekIsoToLoad = null;

        if (Scope == "week")
            weekIsoToLoad = WeekIso;
        else if (Scope == "day")
        {
            var date = SelectedDate.DateTime;
            var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(date);
            weekIsoToLoad = $"{date.Year}-W{weekNum:D2}";
        }

        if (string.IsNullOrWhiteSpace(weekIsoToLoad))
            return;

        // Si ya tenemos byDay en el resumen actual, úsalo
        if (Scope == "week" && Resumen.ByDay != null && Resumen.ByDay.Count > 0)
        {
            _dispatcher.TryEnqueue(() =>
            {
                BuildWeekChartFromByDay(Resumen.ByDay);
                ShowWeekChart = true;
            });
            return;
        }

        // Si no, hacer llamada adicional
        string? agentIdToSend = CurrentUserRole == UserRole.USER ? CurrentUserId : SelectedAgentId;

        var weekData = await _informesService.GetResumenAsync(
            scope: "week",
            date: null,
            weekIso: weekIsoToLoad,
            from: null,
            to: null,
            agentId: agentIdToSend,
            cancellationToken: _cts?.Token ?? CancellationToken.None);

        if (weekData?.ByDay != null && weekData.ByDay.Count > 0)
        {
            _dispatcher.TryEnqueue(() =>
            {
                BuildWeekChartFromByDay(weekData.ByDay);
                ShowWeekChart = true;
            });
        }
        else
        {
            WeekChartMessage = "No hay datos disponibles para esta semana";
        }
    }
    catch (OperationCanceledException)
    {
        // Ignorar cancelación
    }
    catch (Exception ex)
    {
        WeekChartMessage = $"Error al cargar gráfica: {ex.Message}";
        System.Diagnostics.Debug.WriteLine($"[WeekChart] Error: {ex}");
    }
}
// GT-END
```

---

## 🧪 Cómo Probar el Fix

### Prueba 1: Scope = Día (con datos)
1. Abrir ReportsWindow
2. Seleccionar scope "Día"
3. Elegir fecha: 2026-02-09
4. Click en Buscar
5. **Resultado esperado:**
   - Se muestra gráfica semanal con barras para Lun-Dom
   - Días con < 8h tienen color ámbar
   - Días con >= 8h tienen color verde

### Prueba 2: Scope = Semana (con datos)
1. Seleccionar scope "Semana"
2. Ingresar weekIso: 2026-W06
3. Click en Buscar
4. **Resultado esperado:**
   - Se muestra gráfica semanal directamente del resumen
   - Sin llamada adicional (optimización)

### Prueba 3: Sin Datos
1. Seleccionar scope "Día"
2. Elegir fecha futura: 2026-12-31
3. Click en Buscar
4. **Resultado esperado:**
   - Mensaje: "No hay datos disponibles para esta semana"
   - NO muestra gráfica vacía

### Prueba 4: Error de API
1. Detener backend
2. Seleccionar scope "Día"
3. Click en Buscar
4. **Resultado esperado:**
   - Mensaje: "Error al cargar gráfica: [mensaje de error]"
   - El error aparece en Output de Visual Studio

---

## 🔍 Debugging

### Ver Logs en Visual Studio
1. Ejecutar en modo Debug (F5)
2. Abrir ventana **Output** (View → Output)
3. Seleccionar "Debug" en el dropdown
4. Buscar líneas con `[WeekChart]`

### Logs Esperados (Éxito)
```
[InformesService] Iniciando GetResumenAsync...
[InformesService] Endpoint construido: https://...
[InformesService] Respuesta - Partes: 5, Cubierto: 510min...
[InformesService] Intervalos cubiertos: 2
```

### Logs Esperados (Error)
```
[WeekChart] Error: System.Net.Http.HttpRequestException: No connection could be made...
```

### Breakpoints Recomendados
1. `LoadWeekChartIfNeededAsync()` línea 275
2. `BuildWeekChartFromByDay()` línea 342
3. Línea del catch de excepción

---

## 📊 Comparación Antes/Después

### Antes ❌
```csharp
// Sin dispatcher
BuildWeekChartFromByDay(Resumen.ByDay);
ShowWeekChart = true;

// Catch silencioso
catch
{
    WeekChartMessage = "Error al cargar gráfica semanal";
}
```

**Problemas:**
- ❌ Cross-thread exception al modificar ObservableCollection
- ❌ No se puede diagnosticar errores
- ❌ No hay feedback cuando no hay datos

### Después ✅
```csharp
// Con dispatcher
_dispatcher.TryEnqueue(() =>
{
    BuildWeekChartFromByDay(weekData.ByDay);
    ShowWeekChart = true;
});

// Logging de excepción
catch (Exception ex)
{
    WeekChartMessage = $"Error al cargar gráfica: {ex.Message}";
    System.Diagnostics.Debug.WriteLine($"[WeekChart] Error: {ex}");
}

// Mensaje cuando no hay datos
else
{
    WeekChartMessage = "No hay datos disponibles para esta semana";
}
```

**Beneficios:**
- ✅ Sin cross-thread exceptions
- ✅ Errores visibles para troubleshooting
- ✅ Feedback claro al usuario

---

## 🎯 Problemas Comunes y Soluciones

### Problema 1: "No hay datos disponibles" cuando SÍ hay datos
**Causa:** Backend devuelve `byDay` vacío o null.

**Solución:**
1. Verificar que el endpoint `/api/v2/informes/resumen?scope=week&weekIso=...` funciona:
   ```powershell
   .\Scripts\Test-InformesEndpoint-Auto.ps1
   ```
2. Verificar que el usuario tiene partes registrados en esa semana

### Problema 2: Error "Cannot access collection from a different thread"
**Causa:** Modificar `WeekChartItems` desde thread de API.

**Solución:** ✅ Ya implementada con `_dispatcher.TryEnqueue()`

### Problema 3: Gráfica se muestra brevemente y desaparece
**Causa:** `OnResumenChanged()` llama a `LoadWeekChartIfNeededAsync()` que limpia items al inicio.

**Solución Futura (si es necesario):**
- Verificar si ya se cargó la gráfica para esa weekIso antes de recargar
- Añadir flag `_isLoadingWeekChart` para evitar recargas duplicadas

---

## ✅ Validación

### Compilación
```
✅ Compilación exitosa sin errores
✅ Sin warnings relacionados con threads
```

### Testing Manual
- ⏳ Pendiente: Ejecutar app y probar casos de prueba
- ⏳ Pendiente: Verificar logs en Output window

---

## 📌 Próximos Pasos

1. ⏳ **Testing en entorno real:**
   - Probar con diferentes semanas (con/sin datos)
   - Verificar que los colores se muestran correctamente

2. ⏳ **Optimización (si es necesario):**
   - Añadir cache de gráficas por weekIso
   - Evitar recargas duplicadas si ya se tiene la gráfica

3. ⏳ **UX Mejorada (futuro):**
   - Añadir animación fade-in al mostrar gráfica
   - Tooltip con detalles al hacer hover en barras
   - Click en barra para navegar a ese día

---

**Versión:** v1.9.5-alpha  
**Fecha:** 2026-02-14  
**Estado:** ✅ Fix implementado + Debugging habilitado  
**Testing:** ⏳ Pendiente de validación en runtime

---

## 🔍 SIGUIENTE PASO: DEBUGGING

Si el problema persiste después de este fix, ver:
**`Docs/DEBUG_GRAFICA_SEMANAL_INSTRUCCIONES.md`**

Este documento contiene:
- ✅ Instrucciones paso a paso para debugging
- ✅ Cómo ver logs en Output window
- ✅ Interpretación de cada log
- ✅ Breakpoints recomendados
- ✅ Checklist de verificación
- ✅ Escenarios de prueba

**IMPORTANTE:** Ejecutar la app en modo Debug (F5) y copiar todos los logs que empiecen con `[WeekChart]` de la ventana Output.

**FIN DEL DOCUMENTO**
