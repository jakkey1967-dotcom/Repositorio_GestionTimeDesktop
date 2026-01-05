# Converters para WinUI 3

## Descripción
Converters IValueConverter para usar en bindings XAML.

## Ubicación
`Helpers/Converters.cs`

## Converters Disponibles

### BoolToVisibilityConverter
Convierte `bool` a `Visibility`:
- `true` ? `Visibility.Visible`
- `false` ? `Visibility.Collapsed`

**Uso XAML:**
```xml
<TextBlock Visibility="{Binding IsVisible, Converter={StaticResource BoolToVisibilityConverter}}"/>
```

### InverseBoolConverter
Invierte un valor booleano:
- `true` ? `false`
- `false` ? `true`

**Uso XAML:**
```xml
<CheckBox IsChecked="{Binding IsAbierto, Converter={StaticResource InverseBoolConverter}}"/>
```

### BoolToColorConverter
Convierte `IsAbierto` a color de badge:
- `true` (ABIERTO) ? Verde (#10B981)
- `false` (CERRADO) ? Gris (#6B7280)

**Uso XAML:**
```xml
<Border Background="{Binding IsAbierto, Converter={StaticResource BoolToColorConverter}}">
    <TextBlock Text="{Binding EstadoAbiertoCerrado}"/>
</Border>
```

## Registro en Page.Resources

```xml
<Page.Resources>
    <ResourceDictionary>
        <helpers:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
        <helpers:InverseBoolConverter x:Key="InverseBoolConverter"/>
        <helpers:BoolToColorConverter x:Key="BoolToColorConverter"/>
        <!-- ... -->
    </ResourceDictionary>
</Page.Resources>
```

Asegúrate de tener el namespace `helpers` declarado:
```xml
xmlns:helpers="using:GestionTime.Desktop.Helpers"
```
