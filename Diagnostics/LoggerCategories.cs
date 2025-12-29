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