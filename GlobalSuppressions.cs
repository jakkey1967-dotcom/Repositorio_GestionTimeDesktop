// ========================================
// 🔇 Supresión Global de Advertencias 
// GestionTime.Desktop
// ========================================

using System.Diagnostics.CodeAnalysis;

// Suprimir advertencias MVVM Toolkit AOT en toda la aplicación
// Estas advertencias aparecen porque usamos [ObservableProperty] en lugar de partial properties
// pero son seguras para nuestro caso de uso con WinUI 3
[assembly: SuppressMessage("Mvvm.Toolkit", "MVVMTK0045:The field using [ObservableProperty] will generate code that is not AOT compatible", 
    Justification = "La aplicación no usa AOT compilation. [ObservableProperty] fields son compatibles con nuestro escenario WinUI 3.")]

// Suprimir advertencias de nullable en código generado
[assembly: SuppressMessage("Style", "CS8618:Non-nullable field must contain a non-null value when exiting constructor", 
    Scope = "type", Target = "~T:GestionTime.Desktop.Views.ConfiguracionWindow",
    Justification = "Los campos se inicializan en CreateContent() llamado desde ShowWindow().")]

// Suprimir advertencias de publicación NETSDK1198 (perfiles de publicación faltantes)
[assembly: SuppressMessage("Build", "NETSDK1198:A publish profile with the name 'win-ARM64.pubxml' was not found in the project", 
    Justification = "Solo publicamos para x64. ARM64 no es necesario para esta aplicación.")]

// Suprimir advertencias CS8633 en ILogger implementations (restricciones de nulabilidad)
[assembly: SuppressMessage("Style", "CS8633:Nullability of 'TState' type parameter constraints doesn't match the constraints for type parameter 'TState' from interface method", 
    Scope = "member", Target = "~M:GestionTime.Desktop.Diagnostics.DebugFileLoggerProvider.DebugFileLogger.BeginScope``1(``0)",
    Justification = "Las restricciones de nullability de ILogger son compatibles con nuestro uso.")]

[assembly: SuppressMessage("Style", "CS8633:Nullability of 'TState' type parameter constraints doesn't match the constraints for type parameter 'TState' from interface method", 
    Scope = "member", Target = "~M:GestionTime.Desktop.Diagnostics.RotatingFileLoggerProvider.RotatingFileLogger.BeginScope``1(``0)",
    Justification = "Las restricciones de nullability de ILogger son compatibles con nuestro uso.")]