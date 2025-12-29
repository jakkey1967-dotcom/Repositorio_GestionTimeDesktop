using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestionTime.Desktop.Models;

public class ConfiguracionModel : INotifyPropertyChanged
{
    #region Connection Settings
    private string _apiUrl = "https://localhost:2501";
    private int _timeoutSeconds = 30;
    private int _maxRetries = 3;
    private bool _ignoreSSL = true;

    public string ApiUrl
    {
        get => _apiUrl;
        set => SetProperty(ref _apiUrl, value);
    }

    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => SetProperty(ref _timeoutSeconds, value);
    }

    public int MaxRetries
    {
        get => _maxRetries;
        set => SetProperty(ref _maxRetries, value);
    }

    public bool IgnoreSSL
    {
        get => _ignoreSSL;
        set => SetProperty(ref _ignoreSSL, value);
    }
    #endregion

    #region Logging Settings
    private bool _enableLogging = true;
    private LogLevel _logLevel = LogLevel.Warning;
    private string _logPath = @"C:\Logs\GestionTime";
    private bool _logToFile = true;
    private bool _logHttp = false;
    private bool _logErrors = true;
    private bool _logDebug = false;
    private LogRotation _logRotation = LogRotation.Daily;
    private int _logRetentionDays = 30;

    public bool EnableLogging
    {
        get => _enableLogging;
        set => SetProperty(ref _enableLogging, value);
    }

    public LogLevel LogLevel
    {
        get => _logLevel;
        set => SetProperty(ref _logLevel, value);
    }

    public string LogPath
    {
        get => _logPath;
        set => SetProperty(ref _logPath, value);
    }

    public bool LogToFile
    {
        get => _logToFile;
        set => SetProperty(ref _logToFile, value);
    }

    public bool LogHttp
    {
        get => _logHttp;
        set => SetProperty(ref _logHttp, value);
    }

    public bool LogErrors
    {
        get => _logErrors;
        set => SetProperty(ref _logErrors, value);
    }

    public bool LogDebug
    {
        get => _logDebug;
        set => SetProperty(ref _logDebug, value);
    }

    public LogRotation LogRotation
    {
        get => _logRotation;
        set => SetProperty(ref _logRotation, value);
    }

    public int LogRetentionDays
    {
        get => _logRetentionDays;
        set => SetProperty(ref _logRetentionDays, value);
    }
    #endregion

    #region Application Settings
    private bool _autoLogin = false;
    private bool _startMinimized = false;
    private bool _minimizeToTray = false;
    private int _autoRefreshSeconds = 60;
    private AppTheme _theme = AppTheme.Auto;

    public bool AutoLogin
    {
        get => _autoLogin;
        set => SetProperty(ref _autoLogin, value);
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetProperty(ref _startMinimized, value);
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    public int AutoRefreshSeconds
    {
        get => _autoRefreshSeconds;
        set => SetProperty(ref _autoRefreshSeconds, value);
    }

    public AppTheme Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }
    #endregion

    #region Debug Settings
    private bool _debugMode = false;
    private bool _showConsole = false;
    private bool _detailedErrors = true;

    public bool DebugMode
    {
        get => _debugMode;
        set => SetProperty(ref _debugMode, value);
    }

    public bool ShowConsole
    {
        get => _showConsole;
        set => SetProperty(ref _showConsole, value);
    }

    public bool DetailedErrors
    {
        get => _detailedErrors;
        set => SetProperty(ref _detailedErrors, value);
    }
    #endregion

    #region Cache Settings
    private int _cacheTTLMinutes = 5;
    private int _maxCacheItems = 100;

    public int CacheTTLMinutes
    {
        get => _cacheTTLMinutes;
        set => SetProperty(ref _cacheTTLMinutes, value);
    }

    public int MaxCacheItems
    {
        get => _maxCacheItems;
        set => SetProperty(ref _maxCacheItems, value);
    }
    #endregion

    #region INotifyPropertyChanged Implementation

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

public enum LogLevel
{
    Error = 0,
    Warning = 1,
    Info = 2,
    Debug = 3,
    Trace = 4
}

public enum LogRotation
{
    Daily = 0,
    BySize = 1,
    Weekly = 2,
    Monthly = 3
}

public enum AppTheme
{
    Auto = 0,
    Light = 1,
    Dark = 2
}