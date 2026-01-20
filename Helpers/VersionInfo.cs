using System.Reflection;

namespace GestionTime.Desktop.Helpers;

/// <summary>Proporciona información de versión centralizada de la aplicación.</summary>
/// <remarks>
/// La versión se define en Directory.Build.props y se propaga automáticamente
/// a través de las propiedades del ensamblado (AssemblyInformationalVersion).
/// 
/// Para cambiar la versión, modificar SOLO en Directory.Build.props:
/// - AppVersionMajor
/// - AppVersionMinor  
/// - AppVersionPatch
/// - AppVersionSuffix
/// </remarks>
public static class VersionInfo
{
    private static string? _version;
    private static string? _versionNumeric;

    /// <summary>Obtiene la versión completa de la aplicación (ej: "1.4.1-beta").</summary>
    public static string Version
    {
        get
        {
            if (_version == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                
                var infoVersionAttr = assembly
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                    .FirstOrDefault() as AssemblyInformationalVersionAttribute;
                
                if (infoVersionAttr != null && !string.IsNullOrWhiteSpace(infoVersionAttr.InformationalVersion))
                {
                    _version = infoVersionAttr.InformationalVersion;
                }
                else
                {
                    var version = assembly.GetName().Version;
                    _version = version != null 
                        ? $"{version.Major}.{version.Minor}.{version.Build}" 
                        : "1.4.1-beta";
                }
            }
            
            return _version;
        }
    }

    /// <summary>Obtiene la versión numérica (ej: "1.4.1.0").</summary>
    public static string VersionNumeric
    {
        get
        {
            if (_versionNumeric == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                _versionNumeric = version != null 
                    ? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}" 
                    : "1.4.1.0";
            }
            
            return _versionNumeric;
        }
    }

    /// <summary>Obtiene la versión con prefijo 'v' (ej: "v1.4.1-beta").</summary>
    public static string VersionWithPrefix => $"v{Version}";
}
