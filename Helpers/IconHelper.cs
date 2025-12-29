using System.Collections.Generic;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Helper centralizado para gestionar iconos de la aplicación.
/// Contiene todos los glyphs de Segoe MDL2 Assets utilizados en la UI.
/// </summary>
public static class IconHelper
{
    #region Glyphs Generales
    
    /// <summary>Icono de calendario (&#xE787;)</summary>
    public const string Calendar = "\uE787";
    
    /// <summary>Icono de reloj/tiempo (&#xE823;)</summary>
    public const string Clock = "\uE823";
    
    /// <summary>Icono de documento/ticket (&#xE8A1;)</summary>
    public const string Document = "\uE8A1";
    
    /// <summary>Icono de persona/usuario (&#xE77B;)</summary>
    public const string Person = "\uE77B";
    
    /// <summary>Icono de añadir/más (&#xE710;)</summary>
    public const string Add = "\uE710";
    
    /// <summary>Icono de herramienta/configuración (&#xE90F;)</summary>
    public const string Settings = "\uE90F";
    
    /// <summary>Icono de parte/formulario (&#xE8F1;)</summary>
    public const string Form = "\uE8F1";
    
    /// <summary>Icono de tema/paleta (&#xE700;)</summary>
    public const string Theme = "\uE700";
    
    #endregion

    #region Acciones de Toolbar
    
    /// <summary>Copiar (&#xE8C8;)</summary>
    public const string Copy = "\uE8C8";
    
    /// <summary>Pegar (&#xE77F;)</summary>
    public const string Paste = "\uE77F";
    
    /// <summary>Guardar/Grabar (&#xE74E;)</summary>
    public const string Save = "\uE74E";
    
    /// <summary>Cancelar/Anular (&#xE711;)</summary>
    public const string Cancel = "\uE711";
    
    /// <summary>Salir/Cerrar sesión (&#xE7E8;)</summary>
    public const string Exit = "\uE7E8";
    
    /// <summary>Nuevo (&#xE710;)</summary>
    public const string New = "\uE710";
    
    /// <summary>Editar (&#xE70F;)</summary>
    public const string Edit = "\uE70F";
    
    /// <summary>Eliminar/Borrar (&#xE74D;)</summary>
    public const string Delete = "\uE74D";
    
    /// <summary>Refrescar (&#xE72C;)</summary>
    public const string Refresh = "\uE72C";
    
    #endregion

    #region Navegación
    
    /// <summary>Atrás (&#xE72B;)</summary>
    public const string Back = "\uE72B";
    
    /// <summary>Adelante (&#xE72A;)</summary>
    public const string Forward = "\uE72A";
    
    /// <summary>Inicio/Home (&#xE80F;)</summary>
    public const string Home = "\uE80F";
    
    #endregion

    #region Estados
    
    /// <summary>Éxito/Checkmark (&#xE73E;)</summary>
    public const string Success = "\uE73E";
    
    /// <summary>Error/Advertencia (&#xE783;)</summary>
    public const string Error = "\uE783";
    
    /// <summary>Info/Información (&#xE946;)</summary>
    public const string Info = "\uE946";
    
    #endregion

    /// <summary>
    /// Obtiene un diccionario con todos los iconos disponibles.
    /// Útil para debugging o listados.
    /// </summary>
    public static Dictionary<string, string> GetAllIcons()
    {
        return new Dictionary<string, string>
        {
            // Generales
            { nameof(Calendar), Calendar },
            { nameof(Clock), Clock },
            { nameof(Document), Document },
            { nameof(Person), Person },
            { nameof(Add), Add },
            { nameof(Settings), Settings },
            { nameof(Form), Form },
            { nameof(Theme), Theme },
            
            // Acciones
            { nameof(Copy), Copy },
            { nameof(Paste), Paste },
            { nameof(Save), Save },
            { nameof(Cancel), Cancel },
            { nameof(Exit), Exit },
            { nameof(New), New },
            { nameof(Edit), Edit },
            { nameof(Delete), Delete },
            { nameof(Refresh), Refresh },
            
            // Navegación
            { nameof(Back), Back },
            { nameof(Forward), Forward },
            { nameof(Home), Home },
            
            // Estados
            { nameof(Success), Success },
            { nameof(Error), Error },
            { nameof(Info), Info }
        };
    }

    /// <summary>
    /// Obtiene el código hexadecimal del glyph (para uso en XAML).
    /// Ejemplo: GetGlyphHex(IconHelper.Copy) devuelve "E8C8"
    /// </summary>
    public static string GetGlyphHex(string glyph)
    {
        if (string.IsNullOrEmpty(glyph) || glyph.Length == 0)
            return string.Empty;
        
        return ((int)glyph[0]).ToString("X4");
    }

    /// <summary>
    /// Convierte un código hexadecimal a glyph.
    /// Ejemplo: FromHex("E8C8") devuelve el carácter para Copiar
    /// </summary>
    public static string FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return string.Empty;

        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int value))
            return ((char)value).ToString();

        return string.Empty;
    }
}
