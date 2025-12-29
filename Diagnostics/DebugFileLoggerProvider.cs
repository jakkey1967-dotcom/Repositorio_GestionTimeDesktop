using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GestionTime.Desktop.Diagnostics;

public sealed class DebugFileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public DebugFileLoggerProvider(string filePath)
    {
        _filePath = filePath;

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }

    public ILogger CreateLogger(string categoryName)
        => new DebugFileLogger(categoryName, _filePath, _lock);

    public void Dispose() { }

    private sealed class DebugFileLogger : ILogger
    {
        private readonly string _category;
        private readonly string _filePath;
        private readonly object _lock;

        public DebugFileLogger(string category, string filePath, object @lock)
        {
            _category = category;
            _filePath = filePath;
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
            var msg = formatter(state, exception);

            var line =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_category} - {msg}" +
                (exception is null ? "" : Environment.NewLine + exception);

            Debug.WriteLine(line);
            LogHub.Publish(line);

            // fichero
            lock (_lock)
            {
                File.AppendAllText(_filePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}

