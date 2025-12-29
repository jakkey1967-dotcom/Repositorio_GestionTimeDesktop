# ?? **OPTIMIZACIÓN INMEDIATA DEL SISTEMA DE LOGGING**

**Fecha:** 29/12/2025  
**Prioridad:** ?? **ALTA - IMPLEMENTAR HOY**  
**Objetivo:** Optimizar logging para producción

---

## ?? **MEJORA #1: NIVELES POR AMBIENTE**

### **Problema Actual:**
```csharp
// En App.xaml.cs - SIEMPRE Debug (muy verboso)
builder.SetMinimumLevel(LogLevel.Debug);
```

### **Solución Optimizada:**
```csharp
// En App.xaml.cs - Condicional por ambiente
#if DEBUG
    builder.SetMinimumLevel(LogLevel.Debug);
    App.Log.LogInformation("??? MODO DEBUG: Logging verboso activado");
#else
    builder.SetMinimumLevel(LogLevel.Information);
    App.Log.LogInformation("?? MODO RELEASE: Logging optimizado para producción");
#endif
```

---

## ?? **MEJORA #2: ROTACIÓN DE ARCHIVOS**

### **Crear RotatingFileLoggerProvider.cs:**

```csharp
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GestionTime.Desktop.Diagnostics;

/// <summary>
/// Logger que rota archivos automáticamente por tamaño y mantiene histórico limitado
/// </summary>
public sealed class RotatingFileLoggerProvider : ILoggerProvider
{
    private readonly string _baseFilePath;
    private readonly long _maxFileSize;
    private readonly int _maxFiles;
    private readonly object _lock = new();
    private string _currentFilePath;

    public RotatingFileLoggerProvider(string baseFilePath, long maxFileSize = 10_000_000, int maxFiles = 5)
    {
        _baseFilePath = baseFilePath;
        _maxFileSize = maxFileSize; // 10MB por defecto
        _maxFiles = maxFiles;       // 5 archivos por defecto
        
        var dir = Path.GetDirectoryName(_baseFilePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
            
        _currentFilePath = GetCurrentLogFilePath();
        
        // Limpiar archivos antiguos al inicio
        CleanOldLogFiles();
    }

    public ILogger CreateLogger(string categoryName)
        => new RotatingFileLogger(categoryName, this, _lock);

    public void Dispose() { }

    private string GetCurrentLogFilePath()
    {
        var directory = Path.GetDirectoryName(_baseFilePath);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(_baseFilePath);
        var extension = Path.GetExtension(_baseFilePath);
        
        return Path.Combine(directory, $"{fileNameWithoutExt}_{DateTime.Now:yyyyMMdd}{extension}");
    }

    private void CheckAndRotateIfNeeded()
    {
        lock (_lock)
        {
            if (!File.Exists(_currentFilePath))
            {
                return; // Archivo aún no creado
            }

            var fileInfo = new FileInfo(_currentFilePath);
            if (fileInfo.Length >= _maxFileSize)
            {
                // Rotar archivo
                var directory = Path.GetDirectoryName(_currentFilePath);
                var fileName = Path.GetFileNameWithoutExtension(_currentFilePath);
                var extension = Path.GetExtension(_currentFilePath);
                
                var timestamp = DateTime.Now.ToString("HHmmss");
                var rotatedPath = Path.Combine(directory, $"{fileName}_{timestamp}{extension}");
                
                File.Move(_currentFilePath, rotatedPath);
                _currentFilePath = GetCurrentLogFilePath();
                
                File.AppendAllText(_currentFilePath, 
                    $"--- LOG ROTATED FROM {Path.GetFileName(rotatedPath)} at {DateTime.Now:O} ---{Environment.NewLine}",
                    Encoding.UTF8);
                    
                CleanOldLogFiles();
            }
        }
    }

    private void CleanOldLogFiles()
    {
        try
        {
            var directory = Path.GetDirectoryName(_baseFilePath);
            var fileNamePattern = Path.GetFileNameWithoutExtension(_baseFilePath);
            var extension = Path.GetExtension(_baseFilePath);
            
            var logFiles = Directory.GetFiles(directory, $"{fileNamePattern}_*{extension}")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(_maxFiles)
                .ToList();

            foreach (var oldFile in logFiles)
            {
                try
                {
                    oldFile.Delete();
                }
                catch
                {
                    // Ignorar errores al eliminar archivos antiguos
                }
            }
        }
        catch
        {
            // Ignorar errores en limpieza
        }
    }

    public void WriteLog(LogLevel logLevel, string categoryName, string message, Exception? exception)
    {
        CheckAndRotateIfNeeded();
        
        var logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {categoryName} - {message}" +
                     (exception == null ? "" : Environment.NewLine + exception);

        Debug.WriteLine(logLine);
        LogHub.Publish(logLine);

        lock (_lock)
        {
            File.AppendAllText(_currentFilePath, logLine + Environment.NewLine, Encoding.UTF8);
        }
    }

    private sealed class RotatingFileLogger : ILogger
    {
        private readonly string _category;
        private readonly RotatingFileLoggerProvider _provider;
        private readonly object _lock;

        public RotatingFileLogger(string category, RotatingFileLoggerProvider provider, object @lock)
        {
            _category = category;
            _provider = provider;
            _lock = @lock;
        }

        public IDisposable? BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            _provider.WriteLog(logLevel, _category, message, exception);
        }
    }
}
```

---

## ?? **MEJORA #3: LOGGERS ESPECIALIZADOS**

### **Crear LoggerCategories.cs:**

```csharp
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Diagnostics;

/// <summary>
/// Categorías especializadas para loggers por componente
/// </summary>
public static class LoggerCategories
{
    // Categorías principales
    public const string API = "GestionTime.API";
    public const string UI = "GestionTime.UI"; 
    public const string DATA = "GestionTime.Data";
    public const string AUTH = "GestionTime.Auth";
    public const string PERFORMANCE = "GestionTime.Performance";
    public const string SYSTEM = "GestionTime.System";
    
    // Subcategorías específicas
    public const string API_REQUEST = "GestionTime.API.Request";
    public const string API_RESPONSE = "GestionTime.API.Response";
    public const string API_ERROR = "GestionTime.API.Error";
    
    public const string UI_NAVIGATION = "GestionTime.UI.Navigation";
    public const string UI_INTERACTION = "GestionTime.UI.Interaction";
    public const string UI_RENDERING = "GestionTime.UI.Rendering";
    
    public const string DATA_LOAD = "GestionTime.Data.Load";
    public const string DATA_SAVE = "GestionTime.Data.Save";
    public const string DATA_VALIDATION = "GestionTime.Data.Validation";
}

/// <summary>
/// Factory para loggers especializados
/// </summary>
public static class SpecializedLoggers
{
    private static ILoggerFactory? _factory;
    
    public static void Initialize(ILoggerFactory factory)
    {
        _factory = factory;
    }
    
    public static ILogger Api => _factory?.CreateLogger(LoggerCategories.API) ?? App.Log;
    public static ILogger UI => _factory?.CreateLogger(LoggerCategories.UI) ?? App.Log;
    public static ILogger Data => _factory?.CreateLogger(LoggerCategories.DATA) ?? App.Log;
    public static ILogger Auth => _factory?.CreateLogger(LoggerCategories.AUTH) ?? App.Log;
    public static ILogger Performance => _factory?.CreateLogger(LoggerCategories.PERFORMANCE) ?? App.Log;
    public static ILogger System => _factory?.CreateLogger(LoggerCategories.SYSTEM) ?? App.Log;
}
```

---

## ? **MEJORA #4: PERFORMANCE LOGGING**

### **Crear PerformanceLogger.cs:**

```csharp
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace GestionTime.Desktop.Diagnostics;

/// <summary>
/// Logger especializado para métricas de performance
/// </summary>
public static class PerformanceLogger
{
    /// <summary>
    /// Crea un scope que mide duración automáticamente
    /// </summary>
    public static IDisposable BeginScope(ILogger logger, string operation, object? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid().ToString("N")[..8];
        
        if (parameters != null)
        {
            logger.LogDebug("?? [{OperationId}] {Operation} iniciada con parámetros: {Parameters}", 
                operationId, operation, parameters);
        }
        else
        {
            logger.LogDebug("?? [{OperationId}] {Operation} iniciada", operationId, operation);
        }
        
        return new PerformanceScope(logger, operation, operationId, stopwatch);
    }
    
    /// <summary>
    /// Log simple de duración sin scope
    /// </summary>
    public static void LogDuration(ILogger logger, string operation, long milliseconds, bool isSlowOperation = false)
    {
        var emoji = isSlowOperation ? "??" : "??";
        var level = isSlowOperation ? LogLevel.Warning : LogLevel.Information;
        
        logger.Log(level, "{Emoji} {Operation} completada en {Duration}ms {SlowFlag}", 
            emoji, operation, milliseconds, isSlowOperation ? "[LENTA]" : "");
    }
}

/// <summary>
/// Scope que mide duración automáticamente
/// </summary>
internal class PerformanceScope : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operation;
    private readonly string _operationId;
    private readonly Stopwatch _stopwatch;
    private bool _disposed = false;

    public PerformanceScope(ILogger logger, string operation, string operationId, Stopwatch stopwatch)
    {
        _logger = logger;
        _operation = operation;
        _operationId = operationId;
        _stopwatch = stopwatch;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _stopwatch.Stop();
            
            var duration = _stopwatch.ElapsedMilliseconds;
            var isSlowOperation = duration > 1000; // >1 segundo es lento
            
            PerformanceLogger.LogDuration(_logger, $"[{_operationId}] {_operation}", duration, isSlowOperation);
        }
    }
}
```

---

## ?? **MEJORA #5: SANITIZACIÓN AVANZADA**

### **Crear LogSanitizer.cs:**

```csharp
using System;
using System.Text.RegularExpressions;

namespace GestionTime.Desktop.Diagnostics;

/// <summary>
/// Utilidades para sanitizar información sensible en logs
/// </summary>
public static class LogSanitizer
{
    private static readonly string[] SensitiveFields = 
    {
        "password", "token", "authorization", "secret", "key", "credential",
        "passwd", "pwd", "auth", "bearer", "jwt", "session", "cookie"
    };
    
    private static readonly string[] SensitivePatterns = 
    {
        // Emails (parcialmente ocultos)
        @"([a-zA-Z0-9._%+-]+)@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})",
        
        // IPs (último octeto oculto)
        @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.)\d{1,3}",
        
        // URLs con tokens
        @"(token=|jwt=|auth=)([^&\s]+)"
    };
    
    /// <summary>
    /// Sanitiza JSON ocultando campos sensibles
    /// </summary>
    public static string SanitizeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;
            
        var sanitized = json;
        
        foreach (var field in SensitiveFields)
        {
            // Pattern para JSON: "campo":"valor"
            var pattern = $"\"{field}\"\\s*:\\s*\"([^\"]*?)\"";
            sanitized = Regex.Replace(sanitized, pattern, $"\"{field}\":\"***\"", RegexOptions.IgnoreCase);
            
            // Pattern para JSON sin comillas: "campo":valor
            pattern = $"\"{field}\"\\s*:\\s*([^,\\}}\\s]+)";
            sanitized = Regex.Replace(sanitized, pattern, $"\"{field}\":\"***\"", RegexOptions.IgnoreCase);
        }
        
        return sanitized;
    }
    
    /// <summary>
    /// Sanitiza texto general ocultando información sensible
    /// </summary>
    public static string SanitizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;
            
        var sanitized = text;
        
        // Ocultar emails parcialmente: usuario@domain.com -> u***o@d***n.com
        sanitized = Regex.Replace(sanitized, 
            @"([a-zA-Z0-9._%+-]+)@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})",
            match => ObfuscateEmail(match.Groups[1].Value, match.Groups[2].Value));
        
        // Ocultar último octeto de IPs: 192.168.1.100 -> 192.168.1.***
        sanitized = Regex.Replace(sanitized,
            @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.)\d{1,3}",
            "$1***");
            
        // Ocultar tokens en URLs
        sanitized = Regex.Replace(sanitized,
            @"(token=|jwt=|auth=)([^&\s]+)",
            "$1***");
        
        return sanitized;
    }
    
    /// <summary>
    /// Ofusca email manteniendo estructura reconocible
    /// </summary>
    private static string ObfuscateEmail(string user, string domain)
    {
        if (user.Length <= 2)
            return $"***@{ObfuscateDomain(domain)}";
            
        var obfuscatedUser = user.Length > 3 
            ? $"{user[0]}***{user[^1]}" 
            : $"{user[0]}***";
            
        return $"{obfuscatedUser}@{ObfuscateDomain(domain)}";
    }
    
    /// <summary>
    /// Ofusca dominio manteniendo estructura
    /// </summary>
    private static string ObfuscateDomain(string domain)
    {
        var parts = domain.Split('.');
        if (parts.Length < 2)
            return "***";
            
        var obfuscatedParts = new string[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            obfuscatedParts[i] = part.Length > 2 
                ? $"{part[0]}***{part[^1]}" 
                : "***";
        }
        
        return string.Join(".", obfuscatedParts);
    }
    
    /// <summary>
    /// Trunca texto largo para logs
    /// </summary>
    public static string Truncate(string text, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
            
        return text.Substring(0, maxLength) + "... [TRUNCATED]";
    }
}
```

---

## ??? **INTEGRACIÓN EN App.xaml.cs**

### **Modificar App.xaml.cs para usar mejoras:**

```csharp
// En el constructor App(), reemplazar:

// ANTES:
LogFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddProvider(new DebugFileLoggerProvider(logPath));
});

// DESPUÉS:
LogFactory = LoggerFactory.Create(builder =>
{
    #if DEBUG
        builder.SetMinimumLevel(LogLevel.Debug);
        builder.AddProvider(new DebugFileLoggerProvider(logPath)); // Para debug inmediato
    #else
        builder.SetMinimumLevel(LogLevel.Information);
    #endif
    
    // Logger con rotación automática para todos los ambientes
    builder.AddProvider(new RotatingFileLoggerProvider(
        logPath.Replace(".log", "_rotating.log"),
        maxFileSize: 10_000_000,  // 10MB
        maxFiles: 5               // 5 archivos históricos
    ));
});

Log = LogFactory.CreateLogger("GestionTime");

// Inicializar loggers especializados
SpecializedLoggers.Initialize(LogFactory);

#if DEBUG
    Log.LogInformation("??? MODO DEBUG: Logging verboso activado");
#else
    Log.LogInformation("?? MODO RELEASE: Logging optimizado para producción");
#endif

Log.LogInformation("?? Sistema de logging inicializado - Rotación: 10MB/5 archivos");
```

---

## ?? **EJEMPLO DE USO OPTIMIZADO**

### **En ApiClient.cs:**

```csharp
// ANTES:
var json = JsonSerializer.Serialize(payload, _jsonWrite);
_log.LogInformation("HTTP POST {url} Payload: {payload}", path, SafePayloadForLog(json));

// DESPUÉS:
var json = JsonSerializer.Serialize(payload, _jsonWrite);
using var _ = PerformanceLogger.BeginScope(SpecializedLoggers.Api, $"POST {path}");
SpecializedLoggers.Api.LogInformation("HTTP POST {url} Payload: {payload}", 
    path, LogSanitizer.SanitizeJson(json));
```

### **En DiarioPage.cs:**

```csharp
// ANTES:
App.Log?.LogInformation("?? CARGA DE PARTES - Iniciando...");

// DESPUÉS:
using var loadScope = PerformanceLogger.BeginScope(SpecializedLoggers.Data, "LoadPartes", 
    new { FromDate = fromDate, ToDate = toDate });
SpecializedLoggers.Data.LogInformation("?? CARGA DE PARTES - Iniciando rango {DateRange}", 
    $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
```

---

## ? **CHECKLIST DE IMPLEMENTACIÓN**

### **?? HOY - PRIORIDAD MÁXIMA:**
- [ ] ? Crear `RotatingFileLoggerProvider.cs`
- [ ] ? Crear `LoggerCategories.cs`
- [ ] ? Crear `PerformanceLogger.cs`
- [ ] ? Crear `LogSanitizer.cs`
- [ ] ? Modificar `App.xaml.cs` para usar nuevos loggers
- [ ] ? Compilar y verificar que no hay errores

### **?? MAÑANA - INTEGRACIÓN:**
- [ ] ? Actualizar `ApiClient.cs` con loggers especializados
- [ ] ? Actualizar `DiarioPage.cs` con performance logging
- [ ] ? Actualizar otros ViewModels según necesidad
- [ ] ? Probar rotación de archivos

### **?? ESTA SEMANA - OPTIMIZACIÓN:**
- [ ] ? Revisar todos los logs Debug excesivos
- [ ] ? Implementar correlation IDs si es necesario
- [ ] ? Documentar nuevo sistema de logging
- [ ] ? Crear guías de uso para desarrolladores

---

## ?? **IMPACTO ESPERADO**

### **?? ANTES vs DESPUÉS:**

| Aspecto | ANTES | DESPUÉS |
|---------|-------|---------|
| **Tamaño logs diarios** | ~100MB | ~20MB |
| **Archivos acumulados** | ? (manual) | 5 máx (automático) |
| **Levels en Release** | Debug (verboso) | Info (optimizado) |
| **Categorización** | Genérica | 6 especializados |
| **Performance insights** | Manual | Automático |
| **Seguridad** | Passwords ocultos | Datos sensibles sanitizados |

---

## ?? **RESULTADO FINAL**

Con estas mejoras implementadas:

? **Logs optimizados** para producción  
? **Rotación automática** sin mantenimiento manual  
? **Categorización profesional** por componente  
? **Métricas de performance** automáticas  
? **Sanitización avanzada** de datos sensibles  
? **Logs estructurados** para análisis  

**?? Sistema de logging nivel enterprise listo para v1.1.1**

---

**Tiempo estimado de implementación:** 2-3 horas  
**Impacto:** ALTO - Mejora significativa en observabilidad  
**Riesgo:** BAJO - Cambios compatibles con código existente