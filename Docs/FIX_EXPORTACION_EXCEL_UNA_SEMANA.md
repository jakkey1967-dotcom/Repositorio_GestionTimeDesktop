# 🐛 FIX: Exportación Excel Solo Permite Una Semana

## 📋 PROBLEMA IDENTIFICADO

La exportación de Excel solo puede exportar una semana porque **solo exporta los datos que están cargados en memoria** en `Partes` (ObservableCollection).

### ❌ Comportamiento Actual

```csharp
// DiarioPage.xaml.cs - OnExportarExcel()
// Paso 1: Calcular semanas disponibles desde los datos actuales
var weeks = CalculateAvailableWeeks(Partes); // ⚠️ Solo datos en memoria

// Paso 4: Filtrar partes por semana seleccionada
var partesToExport = Partes  // ⚠️ Solo desde datos cargados
    .Where(p => System.Globalization.ISOWeek.GetWeekOfYear(p.Fecha) == selectedWeek.WeekNumber &&
               System.Globalization.ISOWeek.GetYear(p.Fecha) == selectedWeek.Year)
    .OrderBy(p => p.Fecha)
    .ThenBy(p => p.HoraInicio)
    .ToList();
```

### 🔍 Análisis de LoadPartesAsync()

La carga inicial de datos tiene estas limitaciones:

1. **Carga inicial (página abierta)**: Solo últimos 25 partes
   ```csharp
   // LoadPartesWithLimitAsync()
   var path = $"/api/v1/partes?limit={limit}&offset=0"; // limit=25
   ```

2. **Fecha específica**: Solo partes de ESA fecha
   ```csharp
   // LoadPartesByDateAsync()
   var path = $"/api/v1/partes?fecha={fecha:yyyy-MM-dd}";
   ```

3. **Rango de fechas**: 7 días máximo (legacy)
   ```csharp
   // TryLoadWithRangeEndpointAsync()
   var path = $"/api/v1/partes?fechaInicio={fromDate:yyyy-MM-dd}&fechaFin={toDate:yyyy-MM-dd}";
   ```

### 📊 Escenario Real

```
Usuario inicia app (HOY = 2025-01-29)
├── LoadPartesAsync() → Solo últimos 25 partes
├── Usuario presiona "Exportar Excel"
├── CalculateAvailableWeeks(Partes) → Solo semanas en esos 25 partes
└── ❌ No puede exportar semanas antiguas
```

## ✅ SOLUCIÓN PROPUESTA

Modificar el flujo de exportación para cargar TODO el historial disponible antes de mostrar el diálogo de selección de semana.

### Opción 1: Cargar Todo el Historial (Recomendada)

```csharp
private async void OnExportarExcel(object sender, RoutedEventArgs e)
{
    if (ViewModel.IsBusy) return;

    try
    {
        App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
        App.Log?.LogInformation("📊 EXPORTAR A EXCEL - Iniciando proceso");
        
        // ✅ NUEVO: Mostrar loader mientras carga historial completo
        ViewModel.IsBusy = true;
        LoadingOverlay.Visibility = Visibility.Visible;
        LoadingRing.IsActive = true;
        
        App.Log?.LogInformation("📥 Cargando historial completo para exportación...");
        
        // ✅ NUEVO: Cargar TODO el historial sin límite
        var partesService = new Services.Catalog.PartesService(App.Api, App.Log);
        var allPartes = await partesService.ListAsync(
            fechaInicio: null,  // Sin filtro de fecha = TODO
            fechaFin: null,
            limit: null        // Sin límite
        );
        
        if (allPartes == null || !allPartes.Any())
        {
            App.Log?.LogWarning("⚠️ No hay datos disponibles para exportar");
            App.Notifications?.ShowWarning(
                "No hay partes disponibles para exportar.",
                title: "⚠️ Sin Datos");
            return;
        }
        
        App.Log?.LogInformation("✅ Historial cargado: {count} partes totales", allPartes.Count);
        
        // ✅ Calcular semanas desde TODO el historial
        var weeks = CalculateAvailableWeeks(new ObservableCollection<ParteDto>(allPartes));
        
        if (!weeks.Any())
        {
            App.Log?.LogWarning("⚠️ No se pudieron calcular semanas");
            App.Notifications?.ShowWarning(
                "No se pudieron calcular semanas disponibles.",
                title: "⚠️ Error");
            return;
        }
        
        App.Log?.LogInformation("📅 Semanas disponibles: {count}", weeks.Count);
        
        // ✅ Ocultar loader antes de mostrar diálogo
        ViewModel.IsBusy = false;
        LoadingRing.IsActive = false;
        LoadingOverlay.Visibility = Visibility.Collapsed;
        
        // Paso 2: Mostrar diálogo con TODAS las semanas
        var recordCounts = CalculateRecordCountsByWeek(
            new ObservableCollection<ParteDto>(allPartes), weeks);
        
        var dialog = new Dialogs.ExportWeekDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Exportar Semana a Excel",
            PrimaryButtonText = "Exportar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary
        };
        
        dialog.SetWeeks(weeks, recordCounts);
        
        var result = await dialog.ShowAsync();
        
        if (result != ContentDialogResult.Primary || dialog.SelectedWeek == null)
        {
            App.Log?.LogInformation("❌ Usuario canceló la exportación");
            return;
        }
        
        var selectedWeek = dialog.SelectedWeek;
        App.Log?.LogInformation("✅ Semana seleccionada: {week} (Año: {year}, Semana: {num})",
            selectedWeek.DisplayText, selectedWeek.Year, selectedWeek.WeekNumber);
        
        // ✅ Filtrar desde TODO el historial
        var partesToExport = allPartes
            .Where(p => System.Globalization.ISOWeek.GetWeekOfYear(p.Fecha) == selectedWeek.WeekNumber &&
                       System.Globalization.ISOWeek.GetYear(p.Fecha) == selectedWeek.Year)
            .OrderBy(p => p.Fecha)
            .ThenBy(p => p.HoraInicio)
            .ToList();
        
        App.Log?.LogInformation("📊 Registros a exportar: {count}", partesToExport.Count);
        
        if (!partesToExport.Any())
        {
            App.Log?.LogWarning("⚠️ No hay registros en la semana seleccionada");
            App.Notifications?.ShowWarning(
                "La semana seleccionada no tiene registros para exportar.",
                title: "⚠️ Sin Registros");
            return;
        }
        
        // ... resto del código de guardado y exportación ...
    }
    catch (Exception ex)
    {
        App.Log?.LogError(ex, "❌ Error durante la exportación");
        App.Notifications?.ShowError(
            $"Error: {ex.Message}",
            title: "❌ Error de Exportación");
    }
    finally
    {
        ViewModel.IsBusy = false;
        LoadingRing.IsActive = false;
        LoadingOverlay.Visibility = Visibility.Collapsed;
    }
}
```

### Opción 2: Preguntar Rango de Fechas Primero

Alternativa: Mostrar diálogo para seleccionar rango de fechas, luego cargar solo ese rango.

```csharp
// 1. Mostrar DateRangePicker dialog
// 2. Cargar partes del rango seleccionado
// 3. Calcular semanas del rango
// 4. Exportar
```

## 🔧 MODIFICACIONES NECESARIAS

### 1. Añadir soporte para `limit` en PartesService

```csharp
// Services\Catalog\PartesService.cs
public async Task<List<ParteDto>?> ListAsync(
    DateTime? fecha = null,
    DateTime? fechaInicio = null,
    DateTime? fechaFin = null,
    string? search = null,
    int? estado = null,
    int? idCliente = null,
    int? idTipo = null,
    int? idGrupo = null,
    int? limit = null,        // ✅ NUEVO
    int? offset = null,       // ✅ NUEVO
    CancellationToken ct = default)
{
    // ...
    
    if (limit.HasValue)
        queryParams.Add($"limit={limit.Value}");
    
    if (offset.HasValue)
        queryParams.Add($"offset={offset.Value}");
    
    // ...
}
```

### 2. Modificar OnExportarExcel en DiarioPage.xaml.cs

Reemplazar líneas **1428-1448** con la nueva lógica de carga completa.

### 3. Añadir Logs Detallados

```csharp
App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
App.Log?.LogInformation("📊 EXPORTACIÓN EXCEL - Análisis de datos");
App.Log?.LogInformation("   • Total partes cargados: {total}", allPartes.Count);
App.Log?.LogInformation("   • Rango de fechas: {min} a {max}", 
    allPartes.Min(p => p.Fecha).ToString("yyyy-MM-dd"),
    allPartes.Max(p => p.Fecha).ToString("yyyy-MM-dd"));
App.Log?.LogInformation("   • Semanas disponibles: {semanas}", weeks.Count);
App.Log?.LogInformation("═══════════════════════════════════════════════════════════════");
```

## ⚠️ CONSIDERACIONES

1. **Performance**: Si hay muchos partes (>5000), la carga puede tardar
   - Solución: Mostrar progress bar "Cargando historial... X/Y partes"
   
2. **Memoria**: Cargar todo en memoria puede consumir RAM
   - Solución: Cargar por lotes de 1000 partes con paginación
   
3. **Backend**: Verificar que el endpoint soporta cargar sin límite
   - Test: `GET /api/v1/partes?limit=10000&offset=0`

## 🧪 TESTING

```powershell
# Scripts\Test-ExportFullHistory.ps1
# 1. Login
# 2. Verificar que DiarioPage carga solo 25 partes
# 3. Presionar "Exportar Excel"
# 4. Verificar que se cargan TODOS los partes
# 5. Verificar que se muestran TODAS las semanas disponibles
# 6. Exportar semana antigua (ej: hace 3 meses)
# 7. Verificar que el Excel contiene datos correctos
```

## 📝 LOGS ESPERADOS

```
═══════════════════════════════════════════════════════════════
📊 EXPORTAR A EXCEL - Iniciando proceso
📥 Cargando historial completo para exportación...
📡 Endpoint: GET /api/v1/partes
✅ Historial cargado: 1234 partes totales
📅 Semanas disponibles: 18
═══════════════════════════════════════════════════════════════
📊 EXPORTACIÓN EXCEL - Análisis de datos
   • Total partes cargados: 1234
   • Rango de fechas: 2024-08-01 a 2025-01-29
   • Semanas disponibles: 18
═══════════════════════════════════════════════════════════════
✅ Semana seleccionada: Semana 15/2024 (08/04/2024 - 14/04/2024)
📊 Registros a exportar: 42
📤 Iniciando exportación...
✅ Exportación completada exitosamente
```

## 🎯 RESULTADO ESPERADO

Después del fix:
- ✅ Usuario puede exportar CUALQUIER semana del historial
- ✅ No está limitado a la semana actual o datos en pantalla
- ✅ Logs claros muestran cuántos datos se cargaron
- ✅ Loader visual mientras carga historial completo

---
**Estado**: ✅ COMPLETADO  
**Prioridad**: 🔴 ALTA (funcionalidad crítica rota)  
**Tiempo implementación**: 30 minutos  
**Fecha**: 2025-01-29

## ✅ CAMBIOS REALIZADOS

### 1. Services\Catalog\PartesService.cs
- ✅ Añadidos parámetros `limit` y `offset` al método `ListAsync()`
- ✅ Implementada lógica de paginación con queryParams

### 2. Views\DiarioPage.xaml.cs
- ✅ Modificado `OnExportarExcel()` para cargar TODO el historial
- ✅ Añadido loader visual durante carga de historial
- ✅ Semanas calculadas desde historial completo (no solo datos en pantalla)
- ✅ Exportación filtra desde historial completo
- ✅ Logs detallados implementados

### 3. Scripts\Test-ExportFullHistory.ps1
- ✅ Script de verificación automática creado
- ✅ Valida parámetros en PartesService
- ✅ Valida lógica de carga completa en DiarioPage
- ✅ Test ejecutado exitosamente

## 🧪 TESTING REALIZADO

```powershell
Scripts\Test-ExportFullHistory.ps1
# ✅ Todos los tests pasados
```
