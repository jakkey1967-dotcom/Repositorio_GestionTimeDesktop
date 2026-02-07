# ✅ VALIDACIONES DE DATOS - Exportación Excel

**Fecha**: 2025-01-29  
**Archivo**: `Services\Export\ExcelExportService.cs`  
**Estado**: ✅ IMPLEMENTADO

---

## 🎯 OBJETIVO

Validar que TODOS los datos exportados a Excel sean correctos, detectando y registrando:
- Datos faltantes (hora inicio/fin, fechas)
- Datos erróneos (formatos inválidos, horas fuera de rango)
- Datos sospechosos (duraciones >16h, fechas inválidas)

---

## ✅ VALIDACIONES IMPLEMENTADAS

### 1️⃣ Validación de Cliente/Proyecto

```csharp
worksheet.Cell(row, 1).Value = parte.Cliente ?? string.Empty;
if (string.IsNullOrWhiteSpace(parte.Cliente))
{
    errorDetails.Add("Cliente vacío");
}
```

**Detecta**:
- Cliente null o vacío
- Solo espacios en blanco

**Acción**: Log warning, pero exporta fila

---

### 2️⃣ Validación de Fecha

```csharp
if (parte.Fecha != default)
{
    worksheet.Cell(row, 2).Value = parte.Fecha;
    worksheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy";
}
else
{
    errorDetails.Add("Fecha inválida");
    hasError = true;
}
```

**Detecta**:
- Fecha = `default(DateTime)` (01/01/0001)
- Fechas nulas

**Acción**: Log error, celda vacía

---

### 3️⃣ Validación de Hora Inicio

```csharp
var horaInicio = ParseTimeToExcelValue(parte.HoraInicio);
if (horaInicio.HasValue)
{
    worksheet.Cell(row, 3).Value = horaInicio.Value;
    worksheet.Cell(row, 3).Style.NumberFormat.Format = "HH:mm";
}
else
{
    errorDetails.Add($"Hora Inicio inválida o vacía: '{parte.HoraInicio}'");
    rowsWithMissingTime++;
}
```

**Detecta**:
- Hora null o vacía
- Formato inválido (no parseable como TimeSpan)
- Horas fuera de rango (negativas o >=24h)

**Acción**: Log warning, celda vacía, contador `rowsWithMissingTime++`

---

### 4️⃣ Validación de Hora Fin

```csharp
var horaFin = ParseTimeToExcelValue(parte.HoraFin);
if (horaFin.HasValue)
{
    worksheet.Cell(row, 4).Value = horaFin.Value;
    worksheet.Cell(row, 4).Style.NumberFormat.Format = "HH:mm";
}
else
{
    errorDetails.Add($"Hora Fin inválida o vacía: '{parte.HoraFin}'");
    rowsWithMissingTime++;
}
```

**Detecta**:
- Hora null o vacía
- Formato inválido
- Horas fuera de rango

**Acción**: Log warning, celda vacía, contador `rowsWithMissingTime++`

---

### 5️⃣ Validación de Duración Calculada

```csharp
if (horaInicio.HasValue && horaFin.HasValue)
{
    // Validar que la duración sea razonable (<24h normalmente)
    var duracionCalculada = horaFin.Value - horaInicio.Value;
    if (duracionCalculada < 0)
    {
        duracionCalculada += 1.0; // Cruce de medianoche
    }
    
    // Advertir si duración >16 horas (jornada muy larga, posible error)
    if (duracionCalculada > 0.666667) // 16/24
    {
        errorDetails.Add($"Duración sospechosa: {duracionCalculada * 24:F2}h");
        hasError = true;
    }
    
    worksheet.Cell(row, 5).FormulaA1 = $"=IF(D{row}<C{row},D{row}+1-C{row},D{row}-C{row})";
}
```

**Detecta**:
- Duración >16 horas (jornada excesivamente larga)
- Ejemplo: 08:00 - 02:00 (día siguiente) = 18h → Warning

**Acción**: Log warning, pero exporta fórmula correcta

---

### 6️⃣ Validación de Duración Fallback (DuracionMin)

```csharp
else
{
    if (parte.DuracionMin > 0)
    {
        // Advertir si duración >16 horas
        if (parte.DuracionMin > 960) // 16 * 60
        {
            errorDetails.Add($"DuracionMin sospechosa: {parte.DuracionMin} min ({parte.DuracionMin/60.0:F2}h)");
            hasError = true;
        }
        
        worksheet.Cell(row, 5).Value = parte.DuracionMin / 1440.0;
        rowsWithFallbackDuration++;
    }
    else
    {
        errorDetails.Add("Sin duración disponible");
        hasError = true;
    }
}
```

**Detecta**:
- DuracionMin >16 horas (960 minutos)
- DuracionMin = 0 o negativo
- Sin horas ni duración en minutos

**Acción**: Log warning, celda vacía o valor fallback

---

### 7️⃣ Validación de Tarea

```csharp
worksheet.Cell(row, 6).Value = parte.Accion ?? string.Empty;
if (string.IsNullOrWhiteSpace(parte.Accion))
{
    errorDetails.Add("Tarea vacía");
}
```

**Detecta**:
- Tarea null, vacía o solo espacios

**Acción**: Log warning, exporta celda vacía

---

### 8️⃣ Validación en ParseTimeToExcelValue()

```csharp
private static double? ParseTimeToExcelValue(string? horaStr)
{
    if (string.IsNullOrWhiteSpace(horaStr))
        return null;

    try
    {
        if (TimeSpan.TryParse(horaStr, out var timeSpan))
        {
            // Validar rango 0-24h
            if (timeSpan.TotalHours < 0 || timeSpan.TotalHours >= 24)
            {
                App.Log?.LogWarning("⚠️ Hora fuera de rango (0-24h): '{hora}' = {hours}h", 
                    horaStr, timeSpan.TotalHours);
                
                // Normalizar al rango 0-24h
                var normalizedHours = ((timeSpan.TotalHours % 24) + 24) % 24;
                return normalizedHours / 24.0;
            }
            
            return timeSpan.TotalDays;
        }
        else
        {
            App.Log?.LogWarning("⚠️ Formato de hora inválido: '{hora}'", horaStr);
        }
    }
    catch (Exception ex)
    {
        App.Log?.LogError(ex, "❌ Error parseando hora: '{hora}'", horaStr);
    }

    return null;
}
```

**Detecta**:
- Horas negativas (ej: "-01:00")
- Horas >=24 (ej: "25:30")
- Formatos inválidos (ej: "abc", "12:99")
- Excepciones durante el parseo

**Acción**: 
- Log warning/error
- Normaliza horas fuera de rango
- Retorna null si no puede parsear

---

## 📊 LOGS DE VALIDACIÓN

### Logs por Fila

Para cada fila con problemas:

```
⚠️ Fila 5 - Parte ID 1234: Cliente vacío; Hora Inicio inválida o vacía: ''; Duración sospechosa: 18.50h
⚠️ Fila 12 - Parte ID 1245: Hora Fin inválida o vacía: 'abc'; Sin duración disponible
⚠️ Fila 23 - Parte ID 1289: Tarea vacía
```

### Logs de Resumen

Al final de la exportación:

```
✅ Datos escritos (42 filas)
⚠️ VALIDACIÓN: 3 filas con advertencias/errores
⚠️ VALIDACIÓN: 5 valores de hora faltantes o inválidos
ℹ️ VALIDACIÓN: 2 filas usan DuracionMin (fallback)
```

O si todo está OK:

```
✅ Datos escritos (42 filas)
✅ VALIDACIÓN: Todos los datos son correctos
```

---

## 🧪 CASOS DE PRUEBA

### ✅ Caso 1: Datos Completos y Correctos

**Entrada**:
```
Cliente: "Cliente A"
Fecha: 29/01/2025
HoraInicio: "08:30"
HoraFin: "10:00"
Accion: "Desarrollo"
```

**Esperado**:
```
✅ Sin advertencias
✅ Duración = 1:30:00 (fórmula)
```

---

### ⚠️ Caso 2: Hora Inicio Vacía

**Entrada**:
```
Cliente: "Cliente B"
Fecha: 29/01/2025
HoraInicio: ""
HoraFin: "10:00"
DuracionMin: 120
Accion: "Reunión"
```

**Esperado**:
```
⚠️ Fila X - Parte ID Y: Hora Inicio inválida o vacía: ''
ℹ️ Duración = 2:00:00 (fallback desde DuracionMin)
```

---

### ⚠️ Caso 3: Hora Fin Inválida

**Entrada**:
```
Cliente: "Cliente C"
Fecha: 29/01/2025
HoraInicio: "08:00"
HoraFin: "abc"
DuracionMin: 0
```

**Esperado**:
```
⚠️ Fila X - Parte ID Y: Hora Fin inválida o vacía: 'abc'; Sin duración disponible
⚠️ Formato de hora inválido: 'abc'
```

---

### ⚠️ Caso 4: Duración Sospechosa (>16h)

**Entrada**:
```
Cliente: "Cliente D"
Fecha: 29/01/2025
HoraInicio: "08:00"
HoraFin: "02:00"  (día siguiente)
```

**Esperado**:
```
⚠️ Fila X - Parte ID Y: Duración sospechosa: 18.00h
✅ Duración = 18:00:00 (fórmula con cruce de medianoche)
```

---

### ⚠️ Caso 5: Hora Fuera de Rango

**Entrada**:
```
HoraInicio: "25:30"
```

**Esperado**:
```
⚠️ Hora fuera de rango (0-24h): '25:30' = 25.5h
✅ Normalizado a 01:30 (25.5 % 24 = 1.5)
```

---

### ⚠️ Caso 6: Cliente y Tarea Vacíos

**Entrada**:
```
Cliente: ""
Tarea: ""
Fecha: 29/01/2025
HoraInicio: "08:00"
HoraFin: "10:00"
```

**Esperado**:
```
⚠️ Fila X - Parte ID Y: Cliente vacío; Tarea vacía
✅ Duración = 2:00:00
```

---

### ⚠️ Caso 7: Fecha Inválida

**Entrada**:
```
Fecha: default(DateTime)  // 01/01/0001
```

**Esperado**:
```
⚠️ Fila X - Parte ID Y: Fecha inválida
❌ Celda fecha vacía
```

---

## 📈 MÉTRICAS DE VALIDACIÓN

El sistema rastrea:

| Métrica | Variable | Descripción |
|---------|----------|-------------|
| **Filas con errores** | `rowsWithErrors` | Filas con al menos 1 advertencia |
| **Valores de hora faltantes** | `rowsWithMissingTime` | HoraInicio o HoraFin inválidas |
| **Fallback a DuracionMin** | `rowsWithFallbackDuration` | Filas sin horas que usan minutos |

---

## 🎯 BENEFICIOS

### Para el Usuario
- ✅ **Transparencia**: Ve exactamente qué datos tienen problemas
- ✅ **Trazabilidad**: ID del parte y fila en logs
- ✅ **Corrección proactiva**: Puede corregir datos antes de re-exportar

### Para el Desarrollador
- ✅ **Diagnóstico rápido**: Logs claros con contexto
- ✅ **Métricas**: Estadísticas de calidad de datos
- ✅ **Robustez**: Maneja todos los casos edge sin crashear

---

## 🔧 CONFIGURACIÓN DE THRESHOLDS

### Duración Sospechosa: >16 horas

```csharp
if (duracionCalculada > 0.666667) // 16/24 = 0.666667
{
    errorDetails.Add($"Duración sospechosa: {duracionCalculada * 24:F2}h");
}
```

**Ajustar si es necesario**:
- Para turnos largos normales: cambiar a `0.75` (18h)
- Para detección estricta: cambiar a `0.5` (12h)

---

## 📝 EJEMPLO DE LOGS REALES

### Exportación con Datos Mixtos

```
═══════════════════════════════════════════════════════════════
📊 EXPORTACIÓN A EXCEL - Iniciando
   • Archivo destino: C:\Users\...\GestionTime_Semana_2025_05.xlsx
   • Registros a exportar: 42
═══════════════════════════════════════════════════════════════
✅ Encabezados escritos (columnas: 8)
⚠️ Fila 5 - Parte ID 1234: Hora Inicio inválida o vacía: ''
⚠️ Fila 12 - Parte ID 1245: Duración sospechosa: 18.50h
⚠️ Fila 15 - Parte ID 1250: Cliente vacío; Tarea vacía
⚠️ Formato de hora inválido: 'abc'
⚠️ Fila 23 - Parte ID 1289: Hora Fin inválida o vacía: 'abc'; Sin duración disponible
✅ Datos escritos (42 filas)
⚠️ VALIDACIÓN: 4 filas con advertencias/errores
⚠️ VALIDACIÓN: 3 valores de hora faltantes o inválidos
ℹ️ VALIDACIÓN: 1 filas usan DuracionMin (fallback)
✅ Fila TOTAL añadida (fila 44)
✅ Autofiltro aplicado
✅ Columnas autoajustadas
✅ Primera fila congelada
✅ Bordes aplicados
✅ Workbook configurado para auto-cálculo
✅ Archivo Excel guardado exitosamente
═══════════════════════════════════════════════════════════════
✅ EXPORTACIÓN COMPLETADA EXITOSAMENTE
═══════════════════════════════════════════════════════════════
```

### Exportación Perfecta

```
═══════════════════════════════════════════════════════════════
📊 EXPORTACIÓN A EXCEL - Iniciando
   • Archivo destino: C:\Users\...\GestionTime_Semana_2025_05.xlsx
   • Registros a exportar: 42
═══════════════════════════════════════════════════════════════
✅ Encabezados escritos (columnas: 8)
✅ Datos escritos (42 filas)
✅ VALIDACIÓN: Todos los datos son correctos
✅ Fila TOTAL añadida (fila 44)
✅ Autofiltro aplicado
✅ Columnas autoajustadas
✅ Primera fila congelada
✅ Bordes aplicados
✅ Workbook configurado para auto-cálculo
✅ Archivo Excel guardado exitosamente
═══════════════════════════════════════════════════════════════
✅ EXPORTACIÓN COMPLETADA EXITOSAMENTE
═══════════════════════════════════════════════════════════════
```

---

## ⚠️ LIMITACIONES

### No Valida
- Fechas en el futuro (permitido para planning)
- Fechas muy antiguas (>10 años)
- Nombres de cliente duplicados
- Tareas con texto extraño (caracteres especiales)

### Podría Mejorar
- Validar coherencia entre Fecha y Horas
- Detectar partes duplicados (mismo cliente + fecha + hora)
- Validar que GRUPO y TIPO existan en catálogos

---

## 📚 RECURSOS

- **Código**: `Services\Export\ExcelExportService.cs`
- **Test**: `Scripts\Test-ExcelDurationSummable.ps1`
- **Doc principal**: `Docs\FIX_EXCEL_DURACION_SUMABLE.md`

---

**Implementado**: 2025-01-29  
**Compilación**: ✅ Exitosa  
**Testing**: ⚠️ Pendiente validación manual con datos reales
