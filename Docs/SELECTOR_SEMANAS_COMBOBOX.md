# 🎯 Feature: Selector de Semanas (ComboBox)

**Fecha:** 2026-02-14  
**Versión:** v1.9.5-alpha  
**Estado:** ✅ Implementado  
**Archivos:** `ViewModels/Reports/ReportsViewModel.cs`, `Views/Reports/ReportsWindow.xaml`

---

## ✨ Funcionalidad Implementada

Se reemplazó el **TextBox manual** para introducir `weekIso` por un **ComboBox amigable** que muestra las últimas 52 semanas en formato legible.

### ANTES ❌
- Usuario tenía que escribir manualmente: **"2026-W06"**
- Propenso a errores de escritura
- No sabía qué semana buscar sin calcularlo manualmente

### AHORA ✅
- Usuario selecciona de una lista: **"Semana 07 (09/02/2026 - 15/02/2026)"**
- El sistema convierte automáticamente a **"2026-W07"** para el backend
- Muestra últimas 52 semanas (1 año hacia atrás)
- Selección automática de la semana actual al abrir

---

## 📊 Estructura del Selector

### 1. Modelo `WeekOption`

```csharp
/// <summary>Opción de semana para el ComboBox (formato amigable + ISO).</summary>
public class WeekOption
{
    /// <summary>Texto visible: "Semana 07 (09/02/2026 - 15/02/2026)"</summary>
    public string Display { get; set; } = string.Empty;
    
    /// <summary>Valor ISO para el backend: "2026-W07"</summary>
    public string Value { get; set; } = string.Empty;
}
```

**Ejemplo de datos:**
- `Display`: "Semana 07 (09/02/2026 - 15/02/2026)"
- `Value`: "2026-W07"

---

### 2. Generación de Semanas (ViewModel)

```csharp
/// <summary>Genera lista de las últimas 52 semanas para el ComboBox.</summary>
private void GenerateAvailableWeeks()
{
    var weeks = new List<WeekOption>();
    var today = DateTime.Now;
    
    // Generar últimas 52 semanas (1 año hacia atrás)
    for (int i = 0; i < 52; i++)
    {
        var targetDate = today.AddDays(-i * 7);
        var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(targetDate);
        var year = System.Globalization.ISOWeek.GetYear(targetDate);
        
        // Calcular lunes y domingo de esa semana
        var monday = System.Globalization.ISOWeek.ToDateTime(year, weekNum, DayOfWeek.Monday);
        var sunday = monday.AddDays(6);
        
        var weekOption = new WeekOption
        {
            Value = $"{year}-W{weekNum:D2}",
            Display = $"Semana {weekNum:D2} ({monday:dd/MM/yyyy} - {sunday:dd/MM/yyyy})"
        };
        
        weeks.Add(weekOption);
    }
    
    AvailableWeeks = new ObservableCollection<WeekOption>(weeks);
    
    // Seleccionar semana actual por defecto
    var currentWeekIso = GetCurrentWeekIso();
    SelectedWeek = AvailableWeeks.FirstOrDefault(w => w.Value == currentWeekIso);
}
```

**Notas:**
- `System.Globalization.ISOWeek.GetWeekOfYear()`: Obtiene número de semana ISO
- `System.Globalization.ISOWeek.ToDateTime()`: Calcula lunes de una semana ISO
- Se usa formato `dd/MM/yyyy` para las fechas (formato europeo)

---

### 3. Propiedades en ViewModel

```csharp
// Selector de semanas (ComboBox)
private ObservableCollection<WeekOption> _availableWeeks = new();
public ObservableCollection<WeekOption> AvailableWeeks
{
    get => _availableWeeks;
    set => SetProperty(ref _availableWeeks, value);
}

[ObservableProperty] private WeekOption? _selectedWeek;

partial void OnSelectedWeekChanged(WeekOption? value)
{
    if (value != null)
    {
        WeekIso = value.Value; // Actualizar WeekIso con formato ISO "2026-W07"
    }
}
```

**Flujo:**
1. Usuario selecciona en ComboBox: "Semana 07 (09/02/2026 - 15/02/2026)"
2. `SelectedWeek` se actualiza con el objeto `WeekOption`
3. `OnSelectedWeekChanged` ejecuta y actualiza `WeekIso = "2026-W07"`
4. Búsqueda usa `WeekIso` con formato ISO correcto

---

### 4. XAML (ComboBox)

```xaml
<StackPanel Spacing="8" Visibility="{x:Bind ViewModel.WeekScopeVisibility, Mode=OneWay}" VerticalAlignment="Center">
    <TextBlock Text="Semana:" FontSize="13" Foreground="White" FontWeight="SemiBold"/>
    <ComboBox ItemsSource="{x:Bind ViewModel.AvailableWeeks, Mode=OneWay}"
              SelectedItem="{x:Bind ViewModel.SelectedWeek, Mode=TwoWay}"
              DisplayMemberPath="Display"
              Width="280"
              PlaceholderText="Selecciona una semana"/>
</StackPanel>
```

**Propiedades clave:**
- `ItemsSource`: Lista de `WeekOption` generadas
- `SelectedItem`: Binding bidireccional con `SelectedWeek`
- `DisplayMemberPath="Display"`: Muestra la propiedad `Display` (texto amigable)
- `Width="280"`: Suficiente para "Semana 07 (09/02/2026 - 15/02/2026)"

---

## 📋 Ejemplos de Uso

### Caso 1: Seleccionar Semana Actual

```
1. Abrir Informes
2. Seleccionar Scope=Semana
3. ComboBox muestra: "Semana 07 (09/02/2026 - 15/02/2026)" (preseleccionada)
4. Click en Buscar
5. Backend recibe: scope=week&weekIso=2026-W07
```

---

### Caso 2: Buscar Semana Anterior

```
1. Abrir ComboBox de semanas
2. Ver lista:
   - Semana 07 (09/02/2026 - 15/02/2026)  ← Actual
   - Semana 06 (02/02/2026 - 08/02/2026)
   - Semana 05 (26/01/2026 - 01/02/2026)
   - ...
3. Seleccionar: "Semana 06 (02/02/2026 - 08/02/2026)"
4. Click en Buscar
5. Backend recibe: scope=week&weekIso=2026-W06
```

---

### Caso 3: Buscar Semana de Hace 6 Meses

```
1. Abrir ComboBox de semanas
2. Hacer scroll hacia abajo (últimas 52 semanas disponibles)
3. Seleccionar: "Semana 33 (11/08/2025 - 17/08/2025)"
4. Click en Buscar
5. Backend recibe: scope=week&weekIso=2025-W33
```

---

## 🎯 Beneficios

### ✅ Beneficio 1: UX mejorada
**Antes:** Usuario tenía que buscar el número de semana en un calendario  
**Ahora:** Selecciona directamente de la lista con fechas visibles

### ✅ Beneficio 2: Sin errores de escritura
**Antes:** "2026-W7" (sin cero), "2026-07" (sin W), "W07-2026" (orden incorrecto)  
**Ahora:** Imposible error porque el sistema genera el formato correcto

### ✅ Beneficio 3: Contexto visual
**Antes:** "2026-W06" no dice mucho (¿qué fechas incluye?)  
**Ahora:** "Semana 06 (02/02/2026 - 08/02/2026)" clarísimo

### ✅ Beneficio 4: Inicialización automática
**Antes:** Campo vacío, usuario tenía que escribir algo  
**Ahora:** Semana actual preseleccionada al abrir

### ✅ Beneficio 5: Colores diferenciados en gráfica
**Scope=Día:**
- Día seleccionado: **Azul (#3B82F6)** 🔵
- Días cumpliendo 8h: Verde (#10B981) 🟢
- Días no cumpliendo 8h: Ámbar (#F59E0B) 🟡

**Scope=Semana:**
- Todos los días: Verde (>=8h) o Ámbar (<8h) 🟢🟡
- **NO** se marca ningún día en azul

---

## 🎨 Sistema de Colores en Gráfica Semanal

### Colores Definidos

| Color | Código HEX | RGB | Cuándo se usa |
|-------|-----------|-----|---------------|
| **Azul** | `#3B82F6` | `59, 130, 246` | Día seleccionado (solo scope=day) |
| **Verde** | `#10B981` | `16, 185, 129` | Día con >=480 minutos (8h) |
| **Ámbar** | `#F59E0B` | `245, 158, 11` | Día con <480 minutos (<8h) |

### Lógica de Color (BarBrush)

```csharp
public SolidColorBrush BarBrush
{
    get
    {
        // Si es el día seleccionado (scope=day), usar azul/cian
        if (IsSelectedDay)
            return new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)); // #3B82F6 (azul)

        // Si no, verde (>=8h) o ámbar (<8h)
        return IsUnderTarget 
            ? new SolidColorBrush(Color.FromArgb(255, 245, 158, 11))  // #F59E0B (ámbar)
            : new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)); // #10B981 (verde)
    }
}
```

### Ejemplos Visuales

**Caso 1: Búsqueda por Día (2026-01-27 - Martes)**
```
Lun: 🟢 8h 00m (verde)
Mar: 🔵 8h 40m (azul - día seleccionado)
Mié: 🟢 8h 00m (verde)
Jue: 🟢 8h 00m (verde)
Vie: 🟢 8h 30m (verde)
Sáb: 🟡 0h 00m (ámbar)
Dom: 🟡 0h 00m (ámbar)
```

**Caso 2: Búsqueda por Semana (W05)**
```
Lun: 🟢 8h 00m (verde)
Mar: 🟢 8h 40m (verde - SIN azul)
Mié: 🟢 8h 00m (verde)
Jue: 🟢 8h 00m (verde)
Vie: 🟢 8h 30m (verde)
Sáb: 🟡 0h 00m (ámbar)
Dom: 🟡 0h 00m (ámbar)
```

**Caso 3: Búsqueda por Día con <8h (2026-01-29 - Jueves con 6h)**
```
Lun: 🟢 8h 00m (verde)
Mar: 🟢 8h 40m (verde)
Mié: 🟢 8h 00m (verde)
Jue: 🔵 6h 00m (azul - día seleccionado, aunque <8h)
Vie: 🟢 8h 30m (verde)
Sáb: 🟡 0h 00m (ámbar)
Dom: 🟡 0h 00m (ámbar)
```

---

## 📝 Formato ISO de Semanas

### Estándar ISO 8601
- **Formato:** `YYYY-Www` (ejemplo: `2026-W07`)
- **Inicio de semana:** Lunes (no domingo)
- **Semana 01:** Primera semana del año que contiene al menos 4 días del nuevo año
- **Año ISO:** Puede diferir del año calendario (Ej: 2025-12-29 es `2026-W01`)

### Ejemplos Reales (2026)

| Semana ISO | Lunes | Domingo | Observaciones |
|------------|-------|---------|---------------|
| 2026-W01 | 29/12/2025 | 04/01/2026 | Incluye días de 2025 |
| 2026-W05 | 26/01/2026 | 01/02/2026 | |
| 2026-W06 | 02/02/2026 | 08/02/2026 | |
| 2026-W07 | 09/02/2026 | 15/02/2026 | Semana actual (ejemplo) |

---

## 🔄 Integración con Backend

### Endpoint: `GET /api/v2/informes/resumen`

**Parámetro:**
```
?scope=week&weekIso=2026-W07
```

**Response (byDay incluye 7 días):**
```json
{
  "weekIso": "2026-W07",
  "byDay": [
    { "date": "2026-02-09T00:00:00Z", "coveredMinutes": 480 },  // Lunes
    { "date": "2026-02-10T00:00:00Z", "coveredMinutes": 520 },  // Martes
    { "date": "2026-02-11T00:00:00Z", "coveredMinutes": 480 },  // Miércoles
    { "date": "2026-02-12T00:00:00Z", "coveredMinutes": 480 },  // Jueves
    { "date": "2026-02-13T00:00:00Z", "coveredMinutes": 510 },  // Viernes
    { "date": "2026-02-14T00:00:00Z", "coveredMinutes": 0 },    // Sábado
    { "date": "2026-02-15T00:00:00Z", "coveredMinutes": 0 }     // Domingo
  ]
}
```

**Frontend:**
- Gráfica semanal muestra 7 barras (Lun-Dom)
- Usa `byDay` para construir `WeekChartItems`

---

## ✅ Testing

### Test 1: Selección de Semana Actual
```
1. F5 para ejecutar app
2. Ir a Informes
3. Click en RadioButton "Semana"
4. Verificar: ComboBox muestra semana actual preseleccionada
5. Formato: "Semana XX (dd/MM/yyyy - dd/MM/yyyy)"
6. Click en Buscar
7. Verificar: Gráfica semanal aparece con 7 barras
```

**Resultado esperado:** ✅ Semana actual preseleccionada y búsqueda funciona

---

### Test 2: Cambio de Semana
```
1. Abrir ComboBox
2. Verificar: Lista de 52 semanas (scroll disponible)
3. Seleccionar semana diferente (Ej: W05)
4. Verificar: ComboBox muestra "Semana 05 (26/01/2026 - 01/02/2026)"
5. Click en Buscar
6. Verificar logs: agentId enviado correctamente
7. Verificar: Gráfica actualizada con datos de W05
```

**Resultado esperado:** ✅ Cambio de semana funciona correctamente

---

### Test 3: Semanas de Años Pasados
```
1. Scroll en ComboBox hasta final (semana 52 hacia atrás)
2. Seleccionar semana de 2025 (Ej: "Semana 40 (29/09/2025 - 05/10/2025)")
3. Click en Buscar
4. Verificar: Backend recibe weekIso=2025-W40
5. Si hay datos: Gráfica muestra barras
6. Si no hay datos: Mensaje "No hay datos disponibles para esta semana"
```

**Resultado esperado:** ✅ Semanas de años anteriores funcionan

---

### Test 4: Logs del Proceso
```
1. Seleccionar semana
2. Output window → Debug
3. Buscar líneas:
   [InformesService] Endpoint construido: ...&weekIso=2026-W07...
   [WeekChart] Scope=week, weekIsoToLoad: 2026-W07
   [WeekChart] BuildWeekChartFromByDay iniciado con 7 días
```

**Resultado esperado:** ✅ Logs muestran formato ISO correcto

---

## 📚 Archivos Modificados

### Frontend (GestionTimeDesktop)
- ✅ `ViewModels/Reports/ReportsViewModel.cs`
  - Añadida clase `WeekOption` (Display + Value)
  - Propiedades `AvailableWeeks`, `SelectedWeek`
  - Método `GenerateAvailableWeeks()` (últimas 52 semanas)
  - Handler `OnSelectedWeekChanged()` (actualiza WeekIso)
  
- ✅ `Views/Reports/ReportsWindow.xaml`
  - Reemplazado `TextBox` por `ComboBox`
  - Binding con `AvailableWeeks` y `SelectedWeek`
  - `DisplayMemberPath="Display"` para mostrar texto amigable

### Backend (GestionTimeApi)
- ℹ️ **NO requiere cambios** (ya acepta formato ISO)

---

## ✅ Conclusión

### Problema Original
- Usuario tenía que escribir manualmente `weekIso` en formato ISO
- Propenso a errores de escritura
- Sin contexto visual de las fechas de la semana

### Solución Implementada
- ✅ ComboBox con lista de 52 semanas generadas automáticamente
- ✅ Formato amigable: "Semana 07 (09/02/2026 - 15/02/2026)"
- ✅ Conversión automática a formato ISO: "2026-W07"
- ✅ Semana actual preseleccionada al abrir
- ✅ Bindings bidireccionales (selección → WeekIso → búsqueda)

### Resultado
- ✅ UX mejorada significativamente
- ✅ Sin errores de escritura
- ✅ Contexto visual claro
- ✅ Integración transparente con backend (formato ISO preservado)

---

**Versión:** v1.9.5-alpha  
**Fecha:** 2026-02-14  
**Estado:** ✅ Implementado y compilado  
**Testing:** ⏳ Pendiente (testing manual en runtime)

**FIN DEL DOCUMENTO**
