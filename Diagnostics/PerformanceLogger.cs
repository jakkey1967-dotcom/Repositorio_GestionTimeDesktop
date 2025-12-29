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