# WindowHelper - Guía de Uso Actualizada

## Descripción
`WindowHelper` es una clase helper que facilita la configuración de tamaño, posición y escala DPI de ventanas en WinUI 3.

## ?? IMPORTANTE - Cambios Recientes

### Problema Resuelto
El sistema ahora usa **píxeles físicos directamente** en `ResizeAndCenter()` para evitar problemas con el escalado DPI automático.

### ¿Qué significa esto?
- **Antes**: `ResizeAndCenter(window, 1600, 900)` se escalaba según el DPI (ej: 2000x1125 en 125% DPI)
- **Ahora**: `ResizeAndCenter(window, 1600, 900)` usa exactamente 1600x900 píxeles físicos

## Ubicación
`Helpers/WindowHelper.cs`

## Métodos Disponibles

### 1. `ResizeAndCenter(Window window, int width, int height)` ? RECOMENDADO
Redimensiona y centra una ventana usando **píxeles físicos** (sin escala DPI).

```csharp
// Ventana principal de login: 1500x550 píxeles físicos
WindowHelper.ResizeAndCenter(this, 1500, 550);

// Ventana de edición: 1600x900 píxeles físicos
WindowHelper.ResizeAndCenter(editWindow, 1600, 900);
```

### 2. `ResizePhysical(Window window, int width, int height)`
Solo redimensiona la ventana (píxeles físicos, sin centrar).

```csharp
WindowHelper.ResizePhysical(myWindow, 1024, 768);
```

### 3. `Resize(Window window, int width, int height)`
Redimensiona considerando el factor de escala DPI (usa píxeles lógicos).

```csharp
// Si el DPI es 125%, esto se convertirá a 1280x960 píxeles físicos
WindowHelper.Resize(myWindow, 1024, 768);
```

### 4. `CenterWindow(Window window)`
Solo centra la ventana en la pantalla.

```csharp
WindowHelper.CenterWindow(myWindow);
```

### 5. `GetDpiScalingFactor(Window window)`
Obtiene el factor de escala DPI de la pantalla.

```csharp
var scaleFactor = WindowHelper.GetDpiScalingFactor(this);
// scaleFactor = 1.0  (100% - 96 DPI)
// scaleFactor = 1.25 (125% - 120 DPI)
// scaleFactor = 1.5  (150% - 144 DPI)
```

### 6. `SetResizable(Window window, bool isResizable)` ??
Controla si el usuario puede redimensionar la ventana.

```csharp
// Deshabilitar redimensionado (tamaño fijo)
WindowHelper.SetResizable(myWindow, false);
```

### 7. `SetMaximizable(Window window, bool isMaximizable)`
Controla si la ventana puede maximizarse.

```csharp
// Deshabilitar botón maximizar
WindowHelper.SetMaximizable(myWindow, false);
```

### 8. `SetMinimizable(Window window, bool isMinimizable)` ??
Controla si la ventana puede minimizarse.

```csharp
// Deshabilitar botón minimizar
WindowHelper.SetMinimizable(myWindow, false);
```

## Ejemplos de Uso Actualizados

### Ejemplo 1: Ventana Principal (MainWindow) ?

```csharp
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Configurar DESPUÉS de que la ventana se active
        this.Activated += OnFirstActivated;
        
        RootFrame.Navigate(typeof(Views.LoginPage));
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
    {
        // Solo ejecutar una vez
        this.Activated -= OnFirstActivated;
        
        try
        {
            App.Log?.LogInformation("Configurando ventana principal...");
            
            // Configurar tamaño 1500x550 píxeles físicos
            WindowHelper.ResizeAndCenter(this, 1500, 550);
            
            // Opcional: Evitar que se redimensione
            // WindowHelper.SetResizable(this, false);
            
            App.Log?.LogInformation("Ventana configurada correctamente");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error configurando ventana");
        }
    }
}
```

### Ejemplo 2: Ventana Modal de Edición ?

```csharp
private async void OnEditar(object sender, RoutedEventArgs e)
{
    try
    {
        var editWindow = new Window
        {
            Title = "Editar Parte"
        };

        var editPage = new ParteItemEdit();
        editWindow.Content = editPage;

        // 1. Activar la ventana primero
        editWindow.Activate();

        // 2. Esperar un momento para que se renderice
        await Task.Delay(100);

        // 3. Configurar tamaño 1600x900 píxeles físicos
        WindowHelper.ResizeAndCenter(editWindow, 1600, 900);
        
        App.Log?.LogInformation("Ventana de edición configurada");
    }
    catch (Exception ex)
    {
        App.Log?.LogError(ex, "Error abriendo ventana de edición");
    }
}
```

### Ejemplo 3: Ventana de Diálogo con Tamaño Fijo

```csharp
private void ShowDialog()
{
    var dialogWindow = new Window
    {
        Title = "Confirmación"
    };

    dialogWindow.Content = new MyDialogPage();

    // Activar primero
    dialogWindow.Activate();

    // Esperar renderizado
    await Task.Delay(50);

    // Configurar tamaño pequeño 500x400 píxeles físicos
    WindowHelper.ResizeAndCenter(dialogWindow, 500, 400);
    
    // Evitar maximizar y redimensionar
    WindowHelper.SetMaximizable(dialogWindow, false);
    WindowHelper.SetResizable(dialogWindow, false);
}
```

## ?? Tamaños Recomendados (Píxeles Físicos)

### Ventana Principal
```csharp
WindowHelper.ResizeAndCenter(this, 1500, 550);  // Pequeña, para login
```

### Ventana de Edición/Formulario
```csharp
WindowHelper.ResizeAndCenter(editWindow, 1600, 900);  // Mediana, para editar datos
```

### Ventana de Diálogo
```csharp
WindowHelper.ResizeAndCenter(dialogWindow, 500, 400);  // Pequeña, para confirmaciones
```

### Ventana de Configuración
```csharp
WindowHelper.ResizeAndCenter(settingsWindow, 800, 600);  // Mediana, para settings
```

### Ventana Grande (Pantalla Completa de Trabajo)
```csharp
WindowHelper.ResizeAndCenter(mainWindow, 2100, 950);  // Grande, para pantalla completa de trabajo
```

## ?? Logs de Diagnóstico

El `WindowHelper` ahora incluye logs detallados para diagnóstico:

```
[INFO] ResizeAndCenter llamado con 1600x900
[INFO] Ventana redimensionada a 1600x900 píxeles físicos
[INFO] Ventana centrada en (160, 90) - WorkArea: 1920x1080
```

Para ver estos logs, revisa el archivo `app.log` en tu directorio de logs.

## ?? Notas Importantes

### 1. Orden de Llamada Correcto

```csharp
// ? CORRECTO - Para ventana principal
InitializeComponent();
this.Activated += OnFirstActivated; // Configurar en evento Activated

// ? CORRECTO - Para ventanas modales
editWindow.Activate();              // 1. Activar primero
await Task.Delay(100);              // 2. Esperar renderizado
WindowHelper.ResizeAndCenter(editWindow, 1600, 900); // 3. Configurar tamaño

// ? INCORRECTO - No funciona bien
InitializeComponent();
WindowHelper.ResizeAndCenter(this, 1500, 550); // Muy pronto, puede no aplicarse
Activate();
```

### 2. Píxeles Físicos vs Lógicos

- **`ResizeAndCenter()`** y **`ResizePhysical()`**: Usan píxeles físicos directamente
- **`Resize()`**: Convierte píxeles lógicos a físicos usando el DPI

### 3. Compatibilidad con Monitores de Alta DPI

El sistema funciona correctamente en:
- Monitores 100% DPI (96 DPI) - Estándar
- Monitores 125% DPI (120 DPI) - Común en laptops
- Monitores 150% DPI (144 DPI) - Alta resolución
- Monitores 200% DPI (192 DPI) - 4K

### 4. Thread-Safety

Todos los métodos manejan excepciones internamente y registran errores en el log sin romper la aplicación.

## ?? Solución de Problemas

### Problema: La ventana no cambia de tamaño
**Solución**: Asegúrate de llamar a `ResizeAndCenter()` DESPUÉS de `Activate()` y con un pequeño delay:

```csharp
window.Activate();
await Task.Delay(100);
WindowHelper.ResizeAndCenter(window, 1600, 900);
```

### Problema: La ventana no se centra correctamente
**Solución**: Verifica que el tamaño se haya aplicado antes de centrar. El método `ResizeAndCenter()` ya incluye un delay interno.

### Problema: Los logs no aparecen
**Solución**: Verifica que `App.Log` esté inicializado. Los logs se guardan en `app.log`.

## ?? Migración de Código Existente

### Antes
```csharp
var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
var appWindow = AppWindow.GetFromWindowId(windowId);
appWindow?.Resize(new SizeInt32 { Width = 1500, Height = 550 });
```

### Después
```csharp
// En el constructor
this.Activated += OnFirstActivated;

// En el evento
WindowHelper.ResizeAndCenter(this, 1500, 550);
```

¡Mucho más simple y con logs de diagnóstico! ??
