# 🔍 DEBUGGING: Gráfica Semanal - Instrucciones Detalladas

**Fecha:** 2026-02-14  
**Versión:** v1.9.5-alpha  
**Estado:** 🔧 Debugging habilitado

---

## 🎯 Objetivo

Identificar exactamente qué está fallando en la carga de la gráfica semanal mediante logs detallados en la ventana Output de Visual Studio.

---

## ✅ Cambios Implementados

### 1. **Propiedad Visibility Correcta**
**Problema anterior:** Binding de `bool` a `Visibility` en XAML no funciona automáticamente.

**Solución:**
```csharp
// Antes ❌
[ObservableProperty] private bool _showWeekChart;
// XAML: Visibility="{x:Bind ViewModel.ShowWeekChart}"

// Ahora ✅
[ObservableProperty] private bool _showWeekChart;
public Visibility WeekChartVisibility => _showWeekChart ? Visibility.Visible : Visibility.Collapsed;
// XAML: Visibility="{x:Bind ViewModel.WeekChartVisibility}"
```

### 2. **Logging Exhaustivo**
Añadidos logs en cada paso del proceso:
- Inicio del método
- Scope y estado del resumen
- Cálculo de weekIso
- Decisión de usar byDay existente o hacer llamada
- Respuesta del endpoint
- Construcción de items de gráfica
- Estado final

### 3. **BuildWeekChartFromByDay con Logs**
Cada item añadido a la colección ahora se loggea con:
- Día de la semana
- Horas y minutos
- Ancho de barra
- Si está por debajo del target de 8h

---

## 📋 Pasos para Debugging

### PASO 1: Ejecutar en Modo Debug
1. Abrir Visual Studio
2. Presionar **F5** o Click en "Start Debugging"
3. Esperar a que la app se inicie

### PASO 2: Abrir Ventana Output
1. En Visual Studio: **View → Output** (o `Ctrl+Alt+O`)
2. En el dropdown de la ventana Output, seleccionar: **Debug**

### PASO 3: Abrir ReportsWindow
1. En la app: Navegar a **Informes de Partes** (ícono 📊)
2. La ventana se abre con filtros

### PASO 4: Realizar Búsqueda
1. Seleccionar scope: **Día** o **Semana**
2. Si Día: Elegir fecha: **2026-02-09**
3. Si Semana: Ingresar: **2026-W06**
4. Click en **Buscar**

### PASO 5: Ver Logs en Output
En la ventana Output deberías ver algo como:

#### Logs Esperados (ÉXITO) ✅
```
[InformesService] Iniciando GetResumenAsync...
[InformesService] Endpoint construido: https://gestiontimeapi.onrender.com/api/v2/informes/resumen?scope=day&date=2026-02-09&agentId=...
[InformesService] Respuesta - Partes: 5, Cubierto: 510min...
[WeekChart] ===== INICIO LoadWeekChartIfNeededAsync =====
[WeekChart] Scope: day, Resumen != null: True
[WeekChart] Scope=day, calculado weekIsoToLoad: 2026-W06
[WeekChart] Haciendo llamada adicional con agentId: b455821b-e481-4969-825d-817ee4e85184
[InformesService] Iniciando GetResumenAsync...
[InformesService] Endpoint construido: https://gestiontimeapi.onrender.com/api/v2/informes/resumen?scope=week&weekIso=2026-W06&agentId=...
[InformesService] Respuesta - Partes: 5, Cubierto: 510min...
[WeekChart] Respuesta recibida: weekData != null: True, ByDay != null: True, Count: 7
[WeekChart] Construyendo gráfica con 7 días
[WeekChart] BuildWeekChartFromByDay iniciado con 7 días
[WeekChart] maxMinutes: 510
[WeekChart] Añadido: Lun = 0h 00m (barWidth: 10px, isUnder: True)
[WeekChart] Añadido: Mar = 0h 00m (barWidth: 10px, isUnder: True)
[WeekChart] Añadido: Mié = 0h 00m (barWidth: 10px, isUnder: True)
[WeekChart] Añadido: Jue = 0h 00m (barWidth: 10px, isUnder: True)
[WeekChart] Añadido: Vie = 0h 00m (barWidth: 10px, isUnder: True)
[WeekChart] Añadido: Sáb = 0h 00m (barWidth: 10px, isUnder: True)
[WeekChart] Añadido: Dom = 8h 30m (barWidth: 200px, isUnder: False)
[WeekChart] BuildWeekChartFromByDay completado. Total items: 7
[WeekChart] ShowWeekChart=true, WeekChartItems.Count=7
[WeekChart] ===== FIN LoadWeekChartIfNeededAsync =====
```

#### Logs Esperados (SIN DATOS) ⚠️
```
[WeekChart] ===== INICIO LoadWeekChartIfNeededAsync =====
[WeekChart] Scope: day, Resumen != null: True
[WeekChart] Scope=day, calculado weekIsoToLoad: 2026-W06
[WeekChart] Haciendo llamada adicional con agentId: b455821b-e481-4969-825d-817ee4e85184
[InformesService] Iniciando GetResumenAsync...
[WeekChart] Respuesta recibida: weekData != null: True, ByDay != null: False, Count: 0
[WeekChart] No hay datos, mensaje establecido
[WeekChart] ===== FIN LoadWeekChartIfNeededAsync =====
```

#### Logs Esperados (ERROR) ❌
```
[WeekChart] ===== INICIO LoadWeekChartIfNeededAsync =====
[WeekChart] Scope: day, Resumen != null: True
[WeekChart] ERROR: System.Net.Http.HttpRequestException: No connection could be made...
   at GestionTime.Desktop.Services.Reports.InformesService.GetResumenAsync(...)
   at GestionTime.Desktop.ViewModels.Reports.ReportsViewModel.LoadWeekChartIfNeededAsync()
[WeekChart] ===== FIN LoadWeekChartIfNeededAsync =====
```

---

## 🔍 Interpretación de Logs

### ✅ Caso 1: "ShowWeekChart=true" pero NO aparece gráfica
**Logs:**
```
[WeekChart] ShowWeekChart=true, WeekChartItems.Count=7
```

**Problema:** El binding en XAML no está funcionando.

**Verificar:**
1. ¿El XAML usa `WeekChartVisibility` (no `ShowWeekChart`)?
2. ¿El binding tiene `Mode=OneWay`?
3. ¿La propiedad `WeekChartVisibility` devuelve `Visibility.Visible`?

**Solución:**
- Poner breakpoint en la propiedad `WeekChartVisibility`
- Verificar que se devuelve `Visibility.Visible`

---

### ❌ Caso 2: "Respuesta recibida: ByDay != null: False"
**Logs:**
```
[WeekChart] Respuesta recibida: weekData != null: True, ByDay != null: False, Count: 0
[WeekChart] No hay datos, mensaje establecido
```

**Problema:** El backend devuelve un resumen pero sin `byDay`.

**Causas posibles:**
- El endpoint `/api/v2/informes/resumen?scope=week` no devuelve `byDay`
- El usuario no tiene partes registrados en esa semana
- El backend tiene un bug

**Verificar con PowerShell:**
```powershell
.\Scripts\Test-InformesEndpoint-Auto.ps1
```

Buscar en el JSON:
```json
{
  "partsCount": 5,
  "byDay": {           // ← ¿Este objeto existe?
    "2026-02-09": {
      "coveredMinutes": 510
    }
  }
}
```

---

### ⚠️ Caso 3: "Saliendo: scope=range o Resumen=null"
**Logs:**
```
[WeekChart] Scope: range, Resumen != null: True
[WeekChart] Saliendo: scope=range o Resumen=null
```

**Problema:** El scope es "range" y la gráfica solo funciona en "day" o "week".

**Solución:** Esto es **comportamiento esperado**. Cambiar a scope "Día" o "Semana".

---

### 🔄 Caso 4: "Operación cancelada"
**Logs:**
```
[WeekChart] Operación cancelada
```

**Problema:** El usuario cambió filtros antes de que terminara la carga anterior.

**Solución:** Esto es **comportamiento esperado**. El nuevo filtro reemplaza al anterior.

---

### 💥 Caso 5: "ERROR: System..."
**Logs:**
```
[WeekChart] ERROR: System.Net.Http.HttpRequestException: No connection could be made...
```

**Problema:** Error de red o backend caído.

**Verificar:**
1. ¿El backend está corriendo?
   ```powershell
   curl https://gestiontimeapi.onrender.com/api/v1/health
   ```
2. ¿La app tiene conexión a internet?
3. ¿Hay firewall bloqueando?

---

## 🔧 Breakpoints Recomendados

Si los logs no son suficientes, poner breakpoints en:

### 1. Propiedad `WeekChartVisibility`
```csharp
// ViewModels/Reports/ReportsViewModel.cs línea ~75
public Visibility WeekChartVisibility => _showWeekChart ? Visibility.Visible : Visibility.Collapsed;
```
**Verificar:** ¿Devuelve `Visibility.Visible` cuando `_showWeekChart = true`?

### 2. Método `OnShowWeekChartChanged`
```csharp
// ViewModels/Reports/ReportsViewModel.cs línea ~84
partial void OnShowWeekChartChanged(bool value)
{
    OnPropertyChanged(nameof(WeekChartVisibility));
}
```
**Verificar:** ¿Se llama cuando se establece `ShowWeekChart = true`?

### 3. Dispatcher Callback
```csharp
// ViewModels/Reports/ReportsViewModel.cs línea ~327
_dispatcher.TryEnqueue(() =>
{
    BuildWeekChartFromByDay(weekData.ByDay);
    ShowWeekChart = true;  // ← Breakpoint aquí
});
```
**Verificar:** ¿Se ejecuta este código?

### 4. XAML Binding (no directamente, pero inspeccionar)
```xaml
<!-- Views/Reports/ReportsWindow.xaml línea ~155 -->
<Border Visibility="{x:Bind ViewModel.WeekChartVisibility, Mode=OneWay}">
```
**Verificar en Live Visual Tree:** ¿El Border tiene `Visibility="Visible"` cuando debería?

---

## 📊 Escenarios de Prueba

### ✅ Prueba 1: Scope = Día (con datos conocidos)
```
Scope: day
Fecha: 2026-02-09
Usuario: psantos@global-retail.com

Resultado esperado:
- [WeekChart] calculado weekIsoToLoad: 2026-W06
- [WeekChart] Construyendo gráfica con 7 días
- [WeekChart] ShowWeekChart=true
```

### ✅ Prueba 2: Scope = Semana (con byDay en resumen)
```
Scope: week
WeekIso: 2026-W06

Resultado esperado:
- [WeekChart] Usando byDay del resumen actual (7 días)
- [WeekChart] Gráfica construida y ShowWeekChart=true
- NO debería hacer llamada adicional
```

### ✅ Prueba 3: Scope = Rango (debe mostrar mensaje)
```
Scope: range
From: 2026-02-01
To: 2026-02-28

Resultado esperado:
- [WeekChart] Saliendo: scope=range o Resumen=null
- Mensaje: "Gráfica semanal disponible en Día/Semana"
```

---

## 🎯 Checklist de Verificación

Cuando veas los logs, verifica:

- [ ] ¿`LoadWeekChartIfNeededAsync` se ejecuta?
- [ ] ¿`weekIsoToLoad` se calcula correctamente?
- [ ] ¿Se recibe respuesta del backend (`weekData != null`)?
- [ ] ¿`weekData.ByDay` tiene datos (`Count > 0`)?
- [ ] ¿`BuildWeekChartFromByDay` se ejecuta?
- [ ] ¿Se añaden items a `WeekChartItems` (`Total items: X`)?
- [ ] ¿Se establece `ShowWeekChart=true`?
- [ ] ¿`OnShowWeekChartChanged` notifica a `WeekChartVisibility`?
- [ ] ¿El Border en XAML tiene `Visibility="Visible"`?

Si todos los checks son ✅ pero la gráfica NO aparece:
→ El problema está en el binding XAML o en la propiedad `WeekChartVisibility`

---

## 📝 Reportar el Problema

Una vez ejecutados los pasos, copia y pega:

**1. Todos los logs que empiecen con `[WeekChart]` o `[InformesService]`**

**2. Resultado del checklist anterior (✅ o ❌ cada item)**

**3. Screenshot de la ventana Output con los logs**

**4. Screenshot de la ventana ReportsWindow mostrando si aparece o no la gráfica**

Con esta información podremos identificar exactamente dónde está el problema.

---

## 🔧 Fixes Aplicados

### Fix #1: Propiedad Visibility
```csharp
// Añadido
public Visibility WeekChartVisibility => _showWeekChart ? Visibility.Visible : Visibility.Collapsed;

partial void OnShowWeekChartChanged(bool value)
{
    OnPropertyChanged(nameof(WeekChartVisibility));
}
```

### Fix #2: Logging Exhaustivo
```csharp
System.Diagnostics.Debug.WriteLine($"[WeekChart] ...");
```

### Fix #3: XAML Actualizado
```xaml
<!-- Antes -->
<Border Visibility="{x:Bind ViewModel.ShowWeekChart, Mode=OneWay}">

<!-- Después -->
<Border Visibility="{x:Bind ViewModel.WeekChartVisibility, Mode=OneWay}">
```

---

**Versión:** v1.9.5-alpha  
**Fecha:** 2026-02-14  
**Estado:** 🔧 Debugging habilitado - Logging exhaustivo añadido  
**Compilación:** ✅ Exitosa

**EJECUTAR LA APP EN DEBUG Y COPIAR LOS LOGS DE OUTPUT**
