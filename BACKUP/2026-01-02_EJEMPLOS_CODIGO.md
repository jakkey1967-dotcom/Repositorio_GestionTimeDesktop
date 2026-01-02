# 💻 EJEMPLOS DE CÓDIGO - BACKUP 02 ENERO 2026

**Fecha:** 2026-01-02 20:09  
**Propósito:** Ejemplos de uso de las funcionalidades implementadas

---

## 📋 ÍNDICE

1. [IntervalMerger - Algoritmo de Merge](#intervalmerger)
2. [Tooltip de Cobertura](#tooltip-cobertura)
3. [Endpoint de Rango](#endpoint-rango)
4. [Invalidación de Caché](#invalidacion-cache)

---

## 🔀 IntervalMerger - Algoritmo de Merge de Intervalos {#intervalmerger}

**Archivo:** `Helpers/IntervalMerger.cs`

### **Características:**
- ✅ Clase `Interval`: Representa un intervalo de tiempo con inicio y fin
- ✅ Método `MergeIntervals()`: Une intervalos solapados
- ✅ Método `ComputeCoverage()`: Calcula tiempo cubierto y solapado
- ✅ Método `FormatDuration()`: Formato amigable (ej: "2h 15min")
- ✅ Método `FormatInterval()`: Formato de rango (ej: "08:10–09:05")

### **Ejemplo 1: Cálculo básico de cobertura**

```csharp
using GestionTime.Desktop.Helpers;

// Crear intervalos de tiempo
var intervals = new List<IntervalMerger.Interval>
{
    new(DateTime.Parse("2026-01-02 08:00"), DateTime.Parse("2026-01-02 10:00")),
    new(DateTime.Parse("2026-01-02 09:30"), DateTime.Parse("2026-01-02 11:00")),
    new(DateTime.Parse("2026-01-02 14:00"), DateTime.Parse("2026-01-02 16:00"))
};

// Calcular cobertura
var coverage = IntervalMerger.ComputeCoverage(intervals);

// Resultados:
// coverage.TotalCovered = 5 horas (sin solape: 08:00-11:00 + 14:00-16:00)
// coverage.TotalOverlap = 30 minutos (09:30-10:00)
// coverage.MergedIntervals.Count = 2 intervalos unidos

Console.WriteLine($"Tiempo cubierto: {IntervalMerger.FormatDuration(coverage.TotalCovered)}");
// Salida: "Tiempo cubierto: 5h 0min"

Console.WriteLine($"Tiempo solapado: {IntervalMerger.FormatDuration(coverage.TotalOverlap)}");
// Salida: "Tiempo solapado: 30min"
```

### **Ejemplo 2: Formateo de intervalos**

```csharp
var interval = new IntervalMerger.Interval(
    DateTime.Parse("2026-01-02 08:10"), 
    DateTime.Parse("2026-01-02 09:05")
);

// Formatear intervalo
var formatted = IntervalMerger.FormatInterval(interval);
// Resultado: "08:10–09:05"

// Formatear duración
var duration = IntervalMerger.FormatDuration(interval.Duration);
// Resultado: "55min"
```

### **Ejemplo 3: Caso con múltiples solapamientos**

```csharp
var complexIntervals = new List<IntervalMerger.Interval>
{
    new(DateTime.Parse("2026-01-02 08:00"), DateTime.Parse("2026-01-02 10:00")), // 2h
    new(DateTime.Parse("2026-01-02 09:00"), DateTime.Parse("2026-01-02 11:00")), // 2h (solapa 1h)
    new(DateTime.Parse("2026-01-02 10:30"), DateTime.Parse("2026-01-02 12:00")), // 1.5h (solapa 30min)
    new(DateTime.Parse("2026-01-02 14:00"), DateTime.Parse("2026-01-02 15:30"))  // 1.5h (sin solape)
};

var coverage = IntervalMerger.ComputeCoverage(complexIntervals);

// Resultados:
// - TotalCovered: 5.5h (08:00-12:00 + 14:00-15:30)
// - TotalOverlap: 1.5h (09:00-10:00 + 10:30-11:00)
// - MergedIntervals: 2 intervalos
//   1. 08:00–12:00 (4h)
//   2. 14:00–15:30 (1.5h)
```

---

## 🏷️ Tooltip de Cobertura en DiarioPage {#tooltip-cobertura}

**Archivo:** `Views/DiarioPage.xaml.cs`

### **Métodos implementados:**

```csharp
/// <summary>
/// Calcula y actualiza el tooltip de cobertura de tiempo sin solapamiento
/// </summary>
private void UpdateTimeCoverageTooltip()
{
    try
    {
        // Extraer intervalos de los partes visibles
        var intervals = Partes
            .Where(p => !string.IsNullOrWhiteSpace(p.HoraInicio))
            .Select(p =>
            {
                if (!TimeSpan.TryParse(p.HoraInicio, out var inicio))
                    return null;
                
                var startTime = p.Fecha.Date.Add(inicio);
                
                DateTime endTime;
                if (!string.IsNullOrWhiteSpace(p.HoraFin) && TimeSpan.TryParse(p.HoraFin, out var fin))
                {
                    endTime = p.Fecha.Date.Add(fin);
                }
                else
                {
                    endTime = DateTime.Now; // Parte abierto
                }
                
                if (endTime <= startTime)
                    return null;
                
                return new IntervalMerger.Interval(startTime, endTime);
            })
            .Where(i => i != null)
            .Cast<IntervalMerger.Interval>()
            .ToList();
        
        if (!intervals.Any())
        {
            UpdateDuracionHeaderTooltip(null);
            return;
        }
        
        // Calcular cobertura
        var coverage = IntervalMerger.ComputeCoverage(intervals);
        UpdateDuracionHeaderTooltip(coverage);
    }
    catch (Exception ex)
    {
        App.Log?.LogError(ex, "Error calculando cobertura");
        UpdateDuracionHeaderTooltip(null);
    }
}

/// <summary>
/// Actualiza el tooltip del header "Dur."
/// </summary>
private void UpdateDuracionHeaderTooltip(IntervalMerger.CoverageResult? coverage)
{
    try
    {
        if (DuracionHeader == null)
            return;
        
        if (coverage == null || !coverage.MergedIntervals.Any())
        {
            ToolTipService.SetToolTip(DuracionHeader, "No hay datos de tiempo disponibles");
            return;
        }
        
        var tooltipText = BuildCoverageTooltipText(coverage);
        ToolTipService.SetToolTip(DuracionHeader, tooltipText);
    }
    catch (Exception ex)
    {
        App.Log?.LogError(ex, "Error actualizando tooltip");
    }
}

/// <summary>
/// Construye el texto formateado del tooltip
/// </summary>
private static string BuildCoverageTooltipText(IntervalMerger.CoverageResult coverage)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("⏱️ TIEMPO REAL OCUPADO (SIN SOLAPAMIENTO)");
    sb.AppendLine();
    sb.AppendLine($"📊 Cubierto: {IntervalMerger.FormatDuration(coverage.TotalCovered)}");
    
    if (coverage.TotalOverlap.TotalMinutes > 0)
        sb.AppendLine($"⚠️ Solapado: {IntervalMerger.FormatDuration(coverage.TotalOverlap)}");
    
    sb.AppendLine();
    sb.AppendLine($"🕐 Intervalos cubiertos ({coverage.MergedIntervals.Count}):");
    
    foreach (var interval in coverage.MergedIntervals)
    {
        var formatted = IntervalMerger.FormatInterval(interval);
        var duration = IntervalMerger.FormatDuration(interval.Duration);
        sb.AppendLine($"   • {formatted} ({duration})");
    }
    
    return sb.ToString().TrimEnd();
}
```

### **Integración:**

```csharp
private void ApplyFilterToListView()
{
    // ...código existente de filtrado...
    
    Partes.Clear();
    foreach (var p in query)
        Partes.Add(p);
    
    // 🆕 NUEVO: Actualizar tooltip automáticamente
    UpdateTimeCoverageTooltip();
}
```

### **Formato del Tooltip:**

```
⏱️ TIEMPO REAL OCUPADO (SIN SOLAPAMIENTO)

📊 Cubierto: 5h 30min
⚠️ Solapado: 45min

🕐 Intervalos cubiertos (3):
   • 08:10–10:35 (2h 25min)
   • 11:00–12:45 (1h 45min)
   • 14:00–15:20 (1h 20min)
```

---

## 🌐 Endpoint de Rango con fechaInicio/fechaFin {#endpoint-rango}

**Archivo:** `Views/DiarioPage.xaml.cs`

### **Uso del endpoint optimizado:**

```csharp
private async Task<bool> TryLoadWithRangeEndpointAsync(DateTime fromDate, DateTime toDate, CancellationToken ct)
{
    // ✅ Construir URL con nuevos parámetros
    var path = $"/api/v1/partes?fechaInicio={fromDate:yyyy-MM-dd}&fechaFin={toDate:yyyy-MM-dd}";
    
    // Ejemplo: GET /api/v1/partes?fechaInicio=2025-12-26&fechaFin=2026-01-02
    
    var result = await App.Api.GetAsync<List<ParteDto>>(path, ct);
    
    if (result != null && result.Count > 0)
    {
        _cache30dias = result;
        return true; // Éxito
    }
    
    return false; // Necesita fallback
}
```

### **Comparativa de endpoints:**

```csharp
// ❌ ANTIGUO: Múltiples peticiones
for (var d = fromDate; d <= toDate; d = d.AddDays(1))
{
    var path = $"/api/v1/partes?fecha={d:yyyy-MM-dd}";
    var result = await App.Api.GetAsync<List<ParteDto>>(path, ct);
    // 31 peticiones para 31 días
}

// ✅ NUEVO: Una sola petición
var path = $"/api/v1/partes?fechaInicio={fromDate:yyyy-MM-dd}&fechaFin={toDate:yyyy-MM-dd}";
var result = await App.Api.GetAsync<List<ParteDto>>(path, ct);
// 1 petición para 31 días = 97% menos tráfico
```

---

## 🗑️ Invalidación de Caché {#invalidacion-cache}

**Archivo:** `Views/DiarioPage.xaml.cs`

### **Método de invalidación:**

```csharp
private void InvalidatePartesCache(DateTime fecha)
{
    try
    {
        // Invalidar endpoint de rango (±30 días)
        var fromDate = fecha.AddDays(-30).ToString("yyyy-MM-dd");
        var toDate = fecha.AddDays(30).ToString("yyyy-MM-dd");
        
        var rangePath = $"/api/v1/partes?created_from={fromDate}&created_to={toDate}";
        App.Api.InvalidateCacheEntry(rangePath);
        
        // Invalidar fecha específica
        var dayPath = $"/api/v1/partes?fecha={fecha:yyyy-MM-dd}";
        App.Api.InvalidateCacheEntry(dayPath);
        
        // Invalidar fecha actual
        if (fecha.Date != DateTime.Today)
        {
            var todayPath = $"/api/v1/partes?fecha={DateTime.Today:yyyy-MM-dd}";
            App.Api.InvalidateCacheEntry(todayPath);
        }
        
        App.Log?.LogInformation("✅ Caché invalidado correctamente");
    }
    catch (Exception ex)
    {
        App.Log?.LogWarning(ex, "Error invalidando caché");
    }
}
```

### **Uso después de operaciones:**

```csharp
// Después de crear un parte
await App.Api.PostAsync<ParteDto, ParteDto>("/api/v1/partes", nuevoParte);
InvalidatePartesCache(nuevoParte.Fecha);

// Después de editar un parte
await App.Api.PutAsync<ParteDto, ParteDto>($"/api/v1/partes/{id}", parteEditado);
InvalidatePartesCache(parteEditado.Fecha);

// Después de eliminar un parte
await App.Api.DeleteAsync($"/api/v1/partes/{id}");
InvalidatePartesCache(parte.Fecha);

// Después de cerrar un parte
await App.Api.PostAsync($"/api/v1/partes/{id}/close?horaFin={horaFin}");
InvalidatePartesCache(parte.Fecha);
```

---

## 📚 Referencias

- **Archivo principal:** `Views/DiarioPage.xaml.cs`
- **Helper:** `Helpers/IntervalMerger.cs`
- **Documentación completa:** `BACKUP/2026-01-02_NUEVO_BACKUP_INDEX.md`

---

**Última actualización:** 2026-01-02 20:15
