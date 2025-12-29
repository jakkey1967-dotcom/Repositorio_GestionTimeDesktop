# IconHelper - Guía de Uso

## Descripción
`IconHelper` es una clase estática que centraliza todos los glyphs de iconos utilizados en la aplicación. Facilita el mantenimiento y reutilización de iconos en toda la UI.

## Ubicación
`Helpers/IconHelper.cs`

## Ventajas

? **Centralizado**: Todos los iconos en un solo lugar  
? **Tipado**: Evita errores al escribir códigos hexadecimales  
? **Documentado**: Cada icono tiene su descripción  
? **Reutilizable**: Fácil de usar en XAML y C#  
? **Mantenible**: Cambiar un icono en toda la app editando un solo lugar  

## Uso en XAML

### Forma 1: Usar constantes directamente (recomendado)

```xaml
<!-- Agregar namespace al inicio del archivo XAML -->
xmlns:helpers="using:GestionTime.Desktop.Helpers"

<!-- Usar en FontIcon -->
<FontIcon Glyph="{x:Static helpers:IconHelper.Copy}" FontSize="20"/>
```

### Forma 2: Usar códigos hexadecimales tradicionales

```xaml
<!-- Esto sigue funcionando -->
<FontIcon Glyph="&#xE8C8;" FontSize="20"/>
```

## Uso en C#

### Obtener un glyph

```csharp
using GestionTime.Desktop.Helpers;

// Asignar un icono a un FontIcon
myFontIcon.Glyph = IconHelper.Copy;

// Mostrar el código hexadecimal
string hex = IconHelper.GetGlyphHex(IconHelper.Copy); // Devuelve "E8C8"

// Convertir de hexadecimal a glyph
string glyph = IconHelper.FromHex("E8C8"); // Devuelve el carácter
```

### Listar todos los iconos

```csharp
var allIcons = IconHelper.GetAllIcons();
foreach (var icon in allIcons)
{
    Console.WriteLine($"{icon.Key}: {icon.Value}");
}
```

## Iconos Disponibles

### Generales

| Constante | Glyph | Descripción |
|-----------|-------|-------------|
| `Calendar` | &#xE787; | Calendario |
| `Clock` | &#xE823; | Reloj/Tiempo |
| `Document` | &#xE8A1; | Documento/Ticket |
| `Person` | &#xE77B; | Persona/Usuario |
| `Add` | &#xE710; | Añadir/Más |
| `Settings` | &#xE90F; | Herramienta/Configuración |
| `Form` | &#xE8F1; | Formulario/Parte |
| `Theme` | &#xE700; | Tema/Paleta |

### Acciones de Toolbar

| Constante | Glyph | Descripción |
|-----------|-------|-------------|
| `Copy` | &#xE8C8; | Copiar |
| `Paste` | &#xE77F; | Pegar |
| `Save` | &#xE74E; | Guardar/Grabar |
| `Cancel` | &#xE711; | Cancelar/Anular |
| `Exit` | &#xE7E8; | Salir/Cerrar sesión |
| `New` | &#xE710; | Nuevo |
| `Edit` | &#xE70F; | Editar |
| `Delete` | &#xE74D; | Eliminar/Borrar |
| `Refresh` | &#xE72C; | Refrescar |

### Navegación

| Constante | Glyph | Descripción |
|-----------|-------|-------------|
| `Back` | &#xE72B; | Atrás |
| `Forward` | &#xE72A; | Adelante |
| `Home` | &#xE80F; | Inicio/Home |

### Estados

| Constante | Glyph | Descripción |
|-----------|-------|-------------|
| `Success` | &#xE73E; | Éxito/Checkmark |
| `Error` | &#xE783; | Error/Advertencia |
| `Info` | &#xE946; | Info/Información |

## Ejemplos Completos

### Ejemplo 1: Botón con icono en XAML

```xaml
<Page
    xmlns:helpers="using:GestionTime.Desktop.Helpers">
    
    <Button>
        <StackPanel Orientation="Horizontal" Spacing="8">
            <FontIcon Glyph="{x:Static helpers:IconHelper.Save}" 
                      FontSize="16"/>
            <TextBlock Text="Guardar"/>
        </StackPanel>
    </Button>
</Page>
```

### Ejemplo 2: Cambiar icono dinámicamente en C#

```csharp
public sealed partial class MyPage : Page
{
    private void UpdateIcon(bool isSuccess)
    {
        MyIcon.Glyph = isSuccess 
            ? IconHelper.Success 
            : IconHelper.Error;
    }
}
```

### Ejemplo 3: AppBarButton con icono

```xaml
<AppBarButton Label="Copiar">
    <AppBarButton.Icon>
        <FontIcon Glyph="{x:Static helpers:IconHelper.Copy}"/>
    </AppBarButton.Icon>
</AppBarButton>
```

### Ejemplo 4: MenuFlyoutItem con icono

```xaml
<MenuFlyoutItem Text="Editar">
    <MenuFlyoutItem.Icon>
        <FontIcon Glyph="{x:Static helpers:IconHelper.Edit}"/>
    </MenuFlyoutItem.Icon>
</MenuFlyoutItem>
```

## Agregar Nuevos Iconos

Para agregar un nuevo icono al sistema:

1. **Encontrar el glyph**: Busca el código en [Segoe MDL2 Assets](https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font)

2. **Agregar constante en IconHelper.cs**:

```csharp
/// <summary>Descripción del icono (&#xEXXX;)</summary>
public const string MiIcono = "\uEXXX";
```

3. **Agregar al diccionario** en `GetAllIcons()`:

```csharp
{ nameof(MiIcono), MiIcono },
```

4. **Documentar** en este README.

## Migración de Código Existente

### Antes

```xaml
<FontIcon Glyph="&#xE8C8;" FontSize="20"/>
```

### Después

```xaml
xmlns:helpers="using:GestionTime.Desktop.Helpers"

<FontIcon Glyph="{x:Static helpers:IconHelper.Copy}" FontSize="20"/>
```

## Notas Importantes

?? **WinUI 3 Limitation**: `x:Static` requiere que la constante sea `public` y `static`

? **IntelliSense**: Al usar `IconHelper.`, verás todos los iconos disponibles con autocompletado

?? **Búsqueda**: Si no sabes qué icono usar, revisa la tabla de iconos disponibles arriba

## Referencias

- [Segoe MDL2 Assets - Microsoft Docs](https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font)
- [FontIcon Class - WinUI 3](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.fonticon)

## Soporte

Si necesitas un icono que no está en la lista, puedes:
1. Buscarlo en la documentación de Segoe MDL2 Assets
2. Agregarlo siguiendo la sección "Agregar Nuevos Iconos"
3. Documentarlo en este README para futuros usos
