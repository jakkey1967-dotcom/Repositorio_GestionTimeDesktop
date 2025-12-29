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
        
        return Path.Combine(directory!, $"{fileNameWithoutExt}_{DateTime.Now:yyyyMMdd}{extension}");
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
                var rotatedPath = Path.Combine(directory!, $"{fileName}_{timestamp}{extension}");
                
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
            
            var logFiles = Directory.GetFiles(directory!, $"{fileNamePattern}_*{extension}")
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