# Sistema de Temas en LoginPage - Implementación

## ?? Resumen

Se ha implementado un sistema completo de gestión de temas (oscuro/claro) en el `LoginPage` con las siguientes características:

1. ? Selector de tema con botón visible
2. ? Persistencia del tema seleccionado
3. ? Cambio dinámico de fondo según el tema
4. ? Cambio de colores del formulario y controles
5. ? Tema oscuro por defecto

---

## ?? Temas Disponibles

### Tema Oscuro (Default)
```yaml
Fondo: login_fondoOscuro.png
PageBackground: #1a1d21 (gris muy oscuro)
CardBackground: #2c3e50 (azul grisáceo oscuro)
InputBackground: #1f2937 (gris oscuro)
InputForeground: #f3f4f6 (texto blanco grisáceo)
TitleForeground: #e5e7eb (blanco humo)
LabelForeground: #9ca3af (gris claro)
```

### Tema Claro
```yaml
Fondo: login_fondoClaro.png
PageBackground: #f3f4f6 (gris muy claro)
CardBackground: #ffffff (blanco puro)
InputBackground: #ffffff (blanco)
InputForeground: #1f2937 (texto oscuro)
TitleForeground: #1a3b47 (azul oscuro)
LabelForeground: #6b7280 (gris medio)
```

### Colores Compartidos (Teal)
```yaml
TealPrimary: #098ca3
TealDark: #076b7d
TealLight: #0bb3cf
```

---

## ?? Implementación Técnica

### 1. XAML - Recursos con ThemeDictionaries

```xaml
<Page.Resources>
    <ResourceDictionary>
        <ResourceDictionary.ThemeDictionaries>
            
            <!-- TEMA OSCURO -->
            <ResourceDictionary x:Key="Dark">
                <SolidColorBrush x:Key="PageBackground" Color="#1a1d21"/>
                <SolidColorBrush x:Key="CardBackground" Color="#2c3e50"/>
                <SolidColorBrush x:Key="InputBackground" Color="#1f2937"/>
                <SolidColorBrush x:Key="InputForeground" Color="#f3f4f6"/>
                <ImageBrush x:Key="BackgroundImage" 
                            ImageSource="ms-appx:///Assets/login_fondoOscuro.png" 
                            Stretch="UniformToFill"/>
            </ResourceDictionary>
            
            <!-- TEMA CLARO -->
            <ResourceDictionary x:Key="Light">
                <SolidColorBrush x:Key="PageBackground" Color="#f3f4f6"/>
                <SolidColorBrush x:Key="CardBackground" Color="#ffffff"/>
                <SolidColorBrush x:Key="InputBackground" Color="#ffffff"/>
                <SolidColorBrush x:Key="InputForeground" Color="#1f2937"/>
                <ImageBrush x:Key="BackgroundImage" 
                            ImageSource="ms-appx:///Assets/login_fondoClaro.png" 
                            Stretch="UniformToFill"/>
            </ResourceDictionary>
            
        </ResourceDictionary.ThemeDictionaries>
    </ResourceDictionary>
</Page.Resources>
```

### 2. Selector de Tema (Botón)

```xaml
<StackPanel Orientation="Horizontal"
            HorizontalAlignment="Left"
            VerticalAlignment="Top"
            Margin="20"
            Spacing="8">
    <Button x:Name="BtnTheme"
            Width="40"
            Height="40"
            CornerRadius="20"
            Background="{ThemeResource CardBackground}"
            BorderThickness="1"
            BorderBrush="{ThemeResource TealPrimary}"
            Click="OnThemeClick"
            ToolTipService.ToolTip="Cambiar tema">
        <FontIcon Glyph="&#xE706;"
                  FontSize="18"
                  Foreground="{ThemeResource TealPrimary}"/>
    </Button>
</StackPanel>
```

**Ubicación:** Esquina superior izquierda (20px de margen)  
**Icono:** `E706` (paleta de colores)  
**Comportamiento:** Alterna entre tema oscuro y claro

### 3. Fondo Dinámico

```xaml
<Grid x:Name="PageRootGrid" Opacity="0">
    <!-- Fondo con imagen dinámica según tema -->
    <Grid.Background>
        <ThemeResource ResourceKey="BackgroundImage"/>
    </Grid.Background>
    
    <!-- Contenido... -->
</Grid>
```

**Clave:** Usar `ThemeResource` en lugar de recurso estático para que cambie dinámicamente.

### 4. Card del Formulario Adaptativo

```xaml
<Border x:Name="LoginCard"
        Width="290" 
        CornerRadius="20"
        Background="{ThemeResource CardBackground}"
        ...>
```

**Colores que cambian:**
- Fondo del card (`CardBackground`)
- Título (`TitleForeground`)
- Labels (`LabelForeground`)
- Inputs (`InputBackground`, `InputForeground`)

---

## ?? Código C# - Gestión de Temas

### 1. Cargar Tema al Iniciar

```csharp
public LoginPage()
{
    InitializeComponent();
    
    // Cargar tema guardado
    LoadSavedTheme();
    
    LoadRememberedEmail();
    this.Loaded += OnPageLoaded;
}
```

### 2. Método LoadSavedTheme()

```csharp
private void LoadSavedTheme()
{
    try
    {
        var settings = ApplicationData.Current.LocalSettings.Values;
        
        if (settings.TryGetValue("AppTheme", out var themeObj) && themeObj is string themeName)
        {
            var theme = themeName switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
            
            this.RequestedTheme = theme;
        }
        else
        {
            // Por defecto usar tema oscuro
            this.RequestedTheme = ElementTheme.Dark;
            SaveTheme(ElementTheme.Dark);
        }
    }
    catch (Exception ex)
    {
        App.Log?.LogWarning(ex, "Error cargando tema guardado");
        this.RequestedTheme = ElementTheme.Dark;
    }
}
```

### 3. Método SaveTheme()

```csharp
private void SaveTheme(ElementTheme theme)
{
    try
    {
        var settings = ApplicationData.Current.LocalSettings.Values;
        var themeName = theme switch
        {
            ElementTheme.Light => "Light",
            ElementTheme.Dark => "Dark",
            _ => "Default"
        };
        
        settings["AppTheme"] = themeName;
        App.Log?.LogInformation("Tema guardado: {theme}", themeName);
    }
    catch (Exception ex)
    {
        App.Log?.LogWarning(ex, "Error guardando tema");
    }
}
```

**Persistencia:** Se guarda en `ApplicationData.Current.LocalSettings`

### 4. Método OnThemeClick()

```csharp
private void OnThemeClick(object sender, RoutedEventArgs e)
{
    // Alternar entre temas
    var newTheme = this.RequestedTheme == ElementTheme.Dark 
        ? ElementTheme.Light 
        : ElementTheme.Dark;
    
    // Aplicar el nuevo tema
    this.RequestedTheme = newTheme;
    
    // Guardar preferencia
    SaveTheme(newTheme);
    
    // Mostrar notificación
    var themeName = newTheme == ElementTheme.Dark ? "oscuro" : "claro";
    ShowMessage($"Tema {themeName} activado", MessageType.Info);
    
    // Ocultar mensaje después de 2 segundos
    _ = Task.Delay(2000).ContinueWith(_ =>
    {
        DispatcherQueue.TryEnqueue(() => HideMessage());
    });
}
```

**Comportamiento:**
1. Detecta el tema actual
2. Cambia al tema opuesto
3. Guarda la preferencia
4. Muestra notificación al usuario
5. Oculta la notificación automáticamente

---

## ?? Archivos de Imagen Necesarios

### Ubicación
```
C:\GestionTime\GestionTime.Desktop\Assets\
```

### Archivos Requeridos

1. **`login_fondoOscuro.png`**
   - Fondo para tema oscuro
   - Colores oscuros (#1a1d21, #2c3e50)
   - Dimensiones: 1050x720 píxeles (o mayor)

2. **`login_fondoClaro.png`**
   - Fondo para tema claro
   - Colores claros (#f3f4f6, #ffffff)
   - Dimensiones: 1050x720 píxeles (o mayor)

### Configuración en Visual Studio

Ambos archivos deben tener:
- **Build Action**: `Content`
- **Copy to Output Directory**: `Copy if newer`

---

## ?? Flujo de Usuario

### Primer Uso
1. Usuario abre la aplicación
2. Se carga **tema oscuro por defecto**
3. Se muestra `login_fondoOscuro.png`
4. Se guarda la preferencia

### Cambiar Tema
1. Usuario hace clic en botón de tema (esquina superior izquierda)
2. Tema cambia instantáneamente (oscuro ? claro)
3. Fondo cambia automáticamente
4. Notificación: "Tema [oscuro/claro] activado"
5. Preferencia se guarda automáticamente

### Sesiones Siguientes
1. Usuario abre la aplicación
2. Se carga el **último tema usado**
3. Fondo correspondiente se muestra
4. Usuario puede cambiar cuando quiera

---

## ?? Verificación Visual

### Tema Oscuro ?
```
Fondo: Imagen oscura
Card: Azul grisáceo oscuro (#2c3e50)
Título: Blanco humo (#e5e7eb)
Labels: Gris claro (#9ca3af)
Inputs: Fondo oscuro (#1f2937) con texto claro (#f3f4f6)
Botón: Teal (#098ca3) con texto blanco
```

### Tema Claro ?
```
Fondo: Imagen clara
Card: Blanco puro (#ffffff)
Título: Azul oscuro (#1a3b47)
Labels: Gris medio (#6b7280)
Inputs: Fondo blanco con texto oscuro (#1f2937)
Botón: Teal (#098ca3) con texto blanco
```

---

## ?? Pruebas Recomendadas

### Prueba 1: Tema por Defecto
1. Primera ejecución de la aplicación
2. **Esperado:** Tema oscuro activado
3. **Esperado:** `login_fondoOscuro.png` visible

### Prueba 2: Cambio de Tema
1. Click en botón de tema
2. **Esperado:** Cambio instantáneo a tema claro
3. **Esperado:** `login_fondoClaro.png` visible
4. **Esperado:** Notificación "Tema claro activado"

### Prueba 3: Persistencia
1. Cambiar a tema claro
2. Cerrar aplicación
3. Reabrir aplicación
4. **Esperado:** Tema claro sigue activo

### Prueba 4: Alternar Múltiples Veces
1. Click en botón de tema
2. Repetir 5 veces
3. **Esperado:** Cada click alterna el tema correctamente
4. **Esperado:** Sin errores en logs

### Prueba 5: Notificaciones
1. Cambiar tema
2. **Esperado:** Mensaje aparece durante 2 segundos
3. **Esperado:** Mensaje desaparece automáticamente

---

## ?? Comparación Visual

### Contraste de Colores

| Elemento | Tema Oscuro | Tema Claro |
|----------|-------------|------------|
| Fondo Página | `#1a1d21` ?? | `#f3f4f6` ?? |
| Card | `#2c3e50` ?? | `#ffffff` ?? |
| Título | `#e5e7eb` ?? | `#1a3b47` ?? |
| Labels | `#9ca3af` ??? | `#6b7280` ? |
| Input BG | `#1f2937` ?? | `#ffffff` ?? |
| Input Text | `#f3f4f6` ?? | `#1f2937` ? |
| Botón | `#098ca3` ?? | `#098ca3` ?? |

---

## ?? Mejoras Futuras Opcionales

### 1. Tema "Auto" (Sincronizar con Sistema)
```csharp
// Detectar tema del sistema
var uiSettings = new Windows.UI.ViewManagement.UISettings();
var foreground = uiSettings.GetColorValue(UIColorType.Foreground);
var isDark = foreground == Windows.UI.Colors.White;
```

### 2. Transiciones Animadas
```xaml
<VisualTransition From="Dark" To="Light" 
                  GeneratedDuration="0:0:0.3">
    <VisualTransition.Storyboard>
        <DoubleAnimation Duration="0:0:0.3" 
                         Storyboard.TargetProperty="Opacity"/>
    </VisualTransition.Storyboard>
</VisualTransition>
```

### 3. Más Opciones de Tema
- Agregar tema "Sistema" (Default)
- Agregar tema "Alto Contraste"
- Agregar temas personalizados

### 4. Selector de Tema Mejorado
```xaml
<ComboBox Header="Tema">
    <ComboBoxItem Content="?? Oscuro" Tag="Dark"/>
    <ComboBoxItem Content="?? Claro" Tag="Light"/>
    <ComboBoxItem Content="?? Sistema" Tag="Default"/>
</ComboBox>
```

---

## ? Checklist de Implementación

- [x] ThemeDictionaries agregados (Dark y Light)
- [x] Recursos dinámicos (`{ThemeResource}`)
- [x] Botón selector de tema en UI
- [x] Método `LoadSavedTheme()` implementado
- [x] Método `SaveTheme()` implementado
- [x] Método `OnThemeClick()` implementado
- [x] Persistencia en `LocalSettings`
- [x] Tema oscuro por defecto
- [x] Notificaciones de cambio de tema
- [x] Logs de debugging
- [x] Compilación sin errores
- [ ] Agregar `login_fondoOscuro.png` a Assets
- [ ] Agregar `login_fondoClaro.png` a Assets

---

## ?? Notas Importantes

### ?? Imágenes de Fondo
Las imágenes `login_fondoOscuro.png` y `login_fondoClaro.png` **deben ser agregadas manualmente** a la carpeta `Assets` del proyecto.

Si las imágenes no existen, el fondo será **transparente** pero el resto de colores funcionarán correctamente.

### ?? Sincronización con DiarioPage
Este sistema de temas en LoginPage es **independiente** del sistema de temas en DiarioPage.

Si deseas sincronizar ambos temas:
1. Mover la lógica de tema a un servicio compartido
2. Usar la misma clave en `LocalSettings` ("AppTheme")
3. Aplicar el tema guardado en ambas páginas

---

## ?? Archivos Modificados

1. **Views/LoginPage.xaml**
   - Agregado: `RequestedTheme="Dark"` en Page
   - Agregado: `ThemeDictionaries` (Dark y Light)
   - Modificado: Recursos para usar `{ThemeResource}`
   - Agregado: Botón selector de tema
   - Modificado: Grid fondo para usar `ThemeResource`
   - Modificado: Card y controles para colores adaptativos

2. **Views/LoginPage.xaml.cs**
   - Agregado: `LoadSavedTheme()` en constructor
   - Agregado: Método `LoadSavedTheme()`
   - Agregado: Método `SaveTheme(ElementTheme)`
   - Agregado: Método `OnThemeClick(object, RoutedEventArgs)`
   - Agregado: Logs de debugging para temas

---

**Fecha:** 2024-12-24  
**Estado:** ? Implementado y Compilado  
**Compilación:** ? Sin errores  
**Archivos:** 2 modificados (LoginPage.xaml, LoginPage.xaml.cs)  
**Imágenes:** ?? Pendientes de agregar manualmente
