# ⚠️ PROBLEMA: Cancelación Prematura de Peticiones HTTP

**Fecha:** 2026-01-02 15:47  
**Estado:** 🔍 **IDENTIFICADO - Solución disponible**

---

## 🔍 PROBLEMA IDENTIFICADO

El sistema está haciendo **2 llamadas simultáneas** a `LoadPartesAsync()` cuando la página se carga, causando que la primera se cancele prematuramente.

### **Evidencia del Log:**

```log
// Primera llamada (CANCELADA):
2026-01-02 15:47:26.879 [Error] - timeout tras 140ms
⚠️ Error inesperado con endpoint de rango - Usando fallback

// Segunda llamada (ÉXITO):
2026-01-02 15:47:27.295 [Information] ✅ Petición exitosa en 494ms
```

### **Causa Raíz:**

En `DiarioPage.xaml.cs` línea 247:

```csharp
private async Task LoadPartesAsync()
{
    // ❌ PROBLEMA: Cancela cualquier carga previa inmediatamente
    _loadCts?.Cancel();
    _loadCts?.Dispose();
    _loadCts = new CancellationTokenSource();
    // ...
}
```

**Escenario problemático:**

1. ✅ `OnPageLoaded()` llama a `LoadPartesAsync()` → Inicia petición HTTP
2. ⚠️ Algún evento (probablemente `OnFiltroFechaChanged`) llama a `LoadPartesAsync()` de nuevo
3. ❌ La segunda llamada CANCELA la primera con `_loadCts.Cancel()`
4. ❌ La primera petición falla con `TaskCanceledException` (140ms)
5. ✅ La segunda petición completa exitosamente (494ms)

---

## ✅ SOLUCIÓN

Hay varias opciones para resolver esto:

### **Opción 1: Evitar Llamadas Duplicadas en la Carga Inicial** ⭐ (RECOMENDADA)

Asegurarse de que `OnFiltroFechaChanged` NO se dispare durante la inicialización.

**Modificar línea 35-36 en el constructor:**

```csharp
public DiarioPage()
{
    this.InitializeComponent();
    this.DataContext = ViewModel;

    LvPartes.ItemsSource = Partes;
    
    ThemeService.Instance.ApplyTheme(this);
    
    // ✅ SOLUCIÓN: Establecer la fecha SIN disparar el evento
    DpFiltroFecha.DateChanged -= OnFiltroFechaChanged;  // 🆕 Desuscribir temporalmente
    DpFiltroFecha.Date = DateTimeOffset.Now;
    DpFiltroFecha.DateChanged += OnFiltroFechaChanged;  // 🆕 Re-suscribir

    _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
    _debounce.Tick += (_, __) =>
    {
        _debounce!.Stop();
        ApplyFilterToListView();
    };
    
    // ... resto del código
}
```

### **Opción 2: No Cancelar Si Ya Hay Una Carga En Proceso**

Modificar `LoadPartesAsync()` para NO cancelar si ya está cargando:

```csharp
private bool _isLoading = false;  // 🆕 Flag de carga

private async Task LoadPartesAsync()
{
    // ✅ SOLUCIÓN: No iniciar nueva carga si ya está en proceso
    if (_isLoading)
    {
        App.Log?.LogDebug("⚠️ Carga ya en proceso, ignorando nueva petición");
        return;
    }

    _isLoading = true;
    
    try
    {
        // Cancelar solo si realmente queremos reemplazar
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        // ... resto del código de carga ...
    }
    finally
    {
        _isLoading = false;
    }
}
```

### **Opción 3: Debounce en LoadPartesAsync**

Agregar un pequeño delay para evitar llamadas duplicadas rápidas:

```csharp
private DateTime _lastLoadTime = DateTime.MinValue;

private async Task LoadPartesAsync()
{
    // ✅ SOLUCIÓN: Ignorar llamadas duplicadas dentro de 500ms
    var now = DateTime.Now;
    if ((now - _lastLoadTime).TotalMilliseconds < 500)
    {
        App.Log?.LogDebug("⚠️ Llamada duplicada ignorada (dentro de 500ms)");
        return;
    }
    _lastLoadTime = now;
    
    // ... resto del código ...
}
```

---

## 📊 COMPARATIVA DE SOLUCIONES

| Solución | Pros | Contras | Recomendación |
|----------|------|---------|---------------|
| **Opción 1** | ✅ Solución en origen<br>✅ Sin lógica extra | ⚠️ Requiere identificar todos los eventos | ⭐⭐⭐⭐⭐ |
| **Opción 2** | ✅ Robusto<br>✅ Protege de cualquier origen | ⚠️ Agrega complejidad | ⭐⭐⭐⭐ |
| **Opción 3** | ✅ Simple<br>✅ Funciona para todos los casos | ⚠️ Retrasa legítimas | ⭐⭐⭐ |

---

## 🔧 IMPLEMENTACIÓN RECOMENDADA (Opción 1 + Opción 2)

Combinar ambas soluciones para máxima robustez:

### **Cambio 1: Constructor** (Evitar evento inicial)

```csharp
public DiarioPage()
{
    this.InitializeComponent();
    this.DataContext = ViewModel;

    LvPartes.ItemsSource = Partes;
    
    ThemeService.Instance.ApplyTheme(this);
    
    // ✅ NUEVO: Desuscribir temporalmente para evitar evento al setear fecha
    DpFiltroFecha.DateChanged -= OnFiltroFechaChanged;
    DpFiltroFecha.Date = DateTimeOffset.Now;
    DpFiltroFecha.DateChanged += OnFiltroFechaChanged;

    // ... resto sin cambios ...
}
```

### **Cambio 2: LoadPartesAsync** (Protección contra llamadas concurrentes)

```csharp
private bool _isLoading = false;

private async Task LoadPartesAsync()
{
    // ✅ NUEVO: Protección contra llamadas concurrentes
    if (_isLoading)
    {
        App.Log?.LogDebug("⚠️ Carga ya en proceso, ignorando nueva petición");
        return;
    }

    _isLoading = true;

    try
    {
        // 🔒 Cancelar cualquier carga previa (si existe)
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        var toDate = DpFiltroFecha.Date?.DateTime.Date ?? DateTime.Today;
        var fromDate = toDate.AddDays(-30);

        using var loadScope = PerformanceLogger.BeginScope(SpecializedLoggers.Data, "LoadPartes", 
            new { FromDate = fromDate, ToDate = toDate });

        SpecializedLoggers.Data.LogInformation("══════════════════════════════════════════════════════════════─");
        SpecializedLoggers.Data.LogInformation("📥 CARGA DE PARTES");
        SpecializedLoggers.Data.LogInformation("   • Fecha inicio: {from}", fromDate.ToString("yyyy-MM-dd"));
        SpecializedLoggers.Data.LogInformation("   • Fecha fin: {to}", toDate.ToString("yyyy-MM-dd"));
        SpecializedLoggers.Data.LogInformation("   • Días solicitados: {days}", (toDate - fromDate).Days + 1);

        await LoadPartesAsync_Legacy();
    }
    catch (OperationCanceledException)
    {
        SpecializedLoggers.Data.LogInformation("Carga de partes cancelada por el usuario.");
    }
    catch (Exception ex)
    {
        SpecializedLoggers.Data.LogError(ex, "Error cargando partes");
        SpecializedLoggers.Data.LogWarning("La lista quedará vacía. El usuario puede intentar refrescar (F5).");
    }
    finally
    {
        _isLoading = false;  // ✅ NUEVO: Liberar flag
    }
}
```

---

## 🧪 TESTING

Después de aplicar los cambios, verificar en el log que:

1. ✅ Solo hay **1 petición** HTTP al cargar la página
2. ✅ No hay mensajes de `"TaskCanceledException"`  en los primeros segundos
3. ✅ No hay mensajes de `"⚠️ Carga ya en proceso"`

**Log esperado:**

```log
2026-01-02 XX:XX:XX [Information] DiarioPage Loaded ✅
2026-01-02 XX:XX:XX [Information] 📥 CARGA DE PARTES
2026-01-02 XX:XX:XX [Information] 🔄 Intentando carga con endpoint de rango...
2026-01-02 XX:XX:XX [Information] ✅ Petición exitosa en 450ms - 14 partes cargados
```

**Sin duplicados ni errores** ✅

---

## 📝 ARCHIVOS A MODIFICAR

| Archivo | Líneas | Cambio |
|---------|--------|--------|
| `Views/DiarioPage.xaml.cs` | 35-36 | Desuscribir/resuscribir `DateChanged` |
| `Views/DiarioPage.xaml.cs` | 29 | Agregar campo `private bool _isLoading = false;` |
| `Views/DiarioPage.xaml.cs` | 247-290 | Agregar `if (_isLoading) return;` y `finally` |

---

## ✅ RESULTADO ESPERADO

```
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║  ✅ 1 SOLA PETICIÓN al cargar la página                   ║
║  ✅ Sin TaskCanceledException                              ║
║  ✅ Tiempo de carga estable (~500ms)                      ║
║  ✅ Sin warnings de duplicados                             ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
```

---

**Prioridad:** ⚠️ **MEDIA** (funciona con fallback, pero genera errores en log)  
**Impacto:** 🎯 **BAJO** (solo afecta log, no funcionalidad)  
**Complejidad:** ⭐ **BAJA** (2 cambios simples)

---

**Autor:** GitHub Copilot  
**Fecha:** 2026-01-02 15:50  
**Estado:** 🔍 **Identificado, solución disponible**  
**Archivos relacionados:** `Views/DiarioPage.xaml.cs`
