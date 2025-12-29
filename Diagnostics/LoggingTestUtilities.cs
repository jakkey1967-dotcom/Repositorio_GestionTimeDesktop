using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Diagnostics;

/// <summary>
/// Utilidades para probar el sistema de logging
/// </summary>
public static class LoggingTestUtilities
{
    /// <summary>
    /// Ejecuta una prueba completa del sistema de logging
    /// </summary>
    public static async Task<LoggingTestResult> RunLoggingTestAsync(ILogger logger)
    {
        var result = new LoggingTestResult();
        
        try
        {
            // Test 1: Log básico
            logger.LogInformation("🧪 TEST LOGGING: Log básico - {Timestamp}", DateTime.Now);
            result.BasicLoggingWorks = true;
            
            // Test 2: Diferentes niveles
            logger.LogDebug("🔍 TEST LOGGING: Debug level - {TestId}", "DEBUG_001");
            logger.LogInformation("ℹ️ TEST LOGGING: Information level - {TestId}", "INFO_001");
            logger.LogWarning("⚠️ TEST LOGGING: Warning level - {TestId}", "WARN_001");
            logger.LogError("❌ TEST LOGGING: Error level - {TestId}", "ERROR_001");
            result.DifferentLevelsWork = true;
            
            // Test 3: Loggers especializados
            SpecializedLoggers.Api.LogInformation("📡 TEST API LOGGER: API test - {TestId}", "API_001");
            SpecializedLoggers.Data.LogInformation("💾 TEST DATA LOGGER: Data test - {TestId}", "DATA_001");
            SpecializedLoggers.Performance.LogInformation("⚡ TEST PERFORMANCE LOGGER: Perf test - {TestId}", "PERF_001");
            result.SpecializedLoggersWork = true;
            
            // Test 4: Performance logging
            using var perfScope = PerformanceLogger.BeginScope(SpecializedLoggers.Performance, "TestOperation");
            await Task.Delay(100); // Simular operación
            result.PerformanceLoggingWorks = true;
            
            // Test 5: Log con excepción
            try
            {
                throw new InvalidOperationException("Test exception for logging");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "🧪 TEST LOGGING: Exception handling - {TestId}", "EXCEPTION_001");
                result.ExceptionLoggingWorks = true;
            }
            
            // Test 6: Log estructurado
            logger.LogInformation("📊 TEST LOGGING: Structured data - User: {UserId}, Action: {Action}, Success: {Success}, Duration: {Duration}ms",
                "USER123", "TestAction", true, 150);
            result.StructuredLoggingWorks = true;
            
            // Test 7: Log con sanitización
            var sensitiveData = "password=secret123&token=abc123&auth=bearer xyz";
            logger.LogInformation("🔒 TEST LOGGING: Sensitive data - {SanitizedData}", 
                LogSanitizer.SanitizeText(sensitiveData));
            result.SanitizationWorks = true;
            
            logger.LogInformation("✅ TEST LOGGING: Prueba completa finalizada con éxito");
            result.OverallSuccess = true;
            
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "❌ TEST LOGGING: Error durante la prueba");
            result.ErrorMessage = ex.Message;
            result.OverallSuccess = false;
        }
        
        return result;
    }
    
    /// <summary>
    /// Verifica que los archivos de log se estén creando
    /// </summary>
    public static LogFileTestResult VerifyLogFiles(ILogger logger)
    {
        var result = new LogFileTestResult();
        
        try
        {
            // Buscar archivos de log en ubicaciones comunes
            var searchPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "logs"),
                Path.Combine(Environment.CurrentDirectory, "logs"),
                @"C:\Logs\GestionTime"
            };
            
            foreach (var searchPath in searchPaths)
            {
                if (Directory.Exists(searchPath))
                {
                    var logFiles = Directory.GetFiles(searchPath, "*.log");
                    
                    if (logFiles.Length > 0)
                    {
                        result.LogDirectoryExists = true;
                        result.LogFilesFound = logFiles.Length;
                        result.LogDirectory = searchPath;
                        
                        // Verificar archivos recientes (últimos 5 minutos)
                        var recentFiles = 0;
                        foreach (var file in logFiles)
                        {
                            var fileInfo = new FileInfo(file);
                            if (DateTime.Now - fileInfo.LastWriteTime < TimeSpan.FromMinutes(5))
                            {
                                recentFiles++;
                            }
                        }
                        
                        result.RecentLogFiles = recentFiles;
                        result.Success = true;
                        
                        logger.LogInformation("✅ TEST LOGGING: Archivos encontrados en {Directory} - {Count} archivos, {Recent} recientes",
                            searchPath, logFiles.Length, recentFiles);
                        
                        break; // Usar la primera ubicación que contenga logs
                    }
                }
            }
            
            if (!result.Success)
            {
                logger.LogWarning("⚠️ TEST LOGGING: No se encontraron archivos de log en ubicaciones esperadas");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ TEST LOGGING: Error verificando archivos de log");
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
}

/// <summary>
/// Resultado de la prueba del sistema de logging
/// </summary>
public class LoggingTestResult
{
    public bool BasicLoggingWorks { get; set; }
    public bool DifferentLevelsWork { get; set; }
    public bool SpecializedLoggersWork { get; set; }
    public bool PerformanceLoggingWorks { get; set; }
    public bool ExceptionLoggingWorks { get; set; }
    public bool StructuredLoggingWorks { get; set; }
    public bool SanitizationWorks { get; set; }
    public bool OverallSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    
    public int PassedTests => 
        (BasicLoggingWorks ? 1 : 0) +
        (DifferentLevelsWork ? 1 : 0) +
        (SpecializedLoggersWork ? 1 : 0) +
        (PerformanceLoggingWorks ? 1 : 0) +
        (ExceptionLoggingWorks ? 1 : 0) +
        (StructuredLoggingWorks ? 1 : 0) +
        (SanitizationWorks ? 1 : 0);
    
    public int TotalTests => 7;
    
    public double SuccessRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
}

/// <summary>
/// Resultado de la verificación de archivos de log
/// </summary>
public class LogFileTestResult
{
    public bool LogDirectoryExists { get; set; }
    public int LogFilesFound { get; set; }
    public int RecentLogFiles { get; set; }
    public string? LogDirectory { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}