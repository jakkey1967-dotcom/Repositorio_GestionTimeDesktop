# Contributing to GestionTime Desktop

¡Gracias por tu interés en contribuir a GestionTime Desktop! 🎉

## 📋 Tabla de Contenidos

- [Código de Conducta](#código-de-conducta)
- [¿Cómo puedo contribuir?](#cómo-puedo-contribuir)
- [Proceso de Pull Request](#proceso-de-pull-request)
- [Guía de Estilo](#guía-de-estilo)
- [Estructura del Proyecto](#estructura-del-proyecto)

## Código de Conducta

Este proyecto se adhiere a un Código de Conducta. Al participar, se espera que mantengas este código.

## ¿Cómo puedo contribuir?

### 🐛 Reportar Bugs

Si encuentras un bug:

1. **Busca primero** en los [Issues](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/issues) existentes
2. Si no existe, crea un **nuevo issue** con:
   - Título descriptivo
   - Pasos para reproducir
   - Comportamiento esperado vs actual
   - Capturas de pantalla (si aplica)
   - Versión de Windows y .NET
   - Logs relevantes

**Template de Bug:**
```markdown
**Descripción:**
[Breve descripción del bug]

**Pasos para reproducir:**
1. Ir a '...'
2. Click en '...'
3. Ver error

**Comportamiento esperado:**
[Qué debería pasar]

**Capturas:**
[Adjuntar imágenes]

**Entorno:**
- Windows: [e.g. Windows 11 22H2]
- .NET: [e.g. .NET 8.0.1]
- Aplicación: [e.g. v1.0.0]
```

### ✨ Proponer Nuevas Características

Para proponer nuevas funcionalidades:

1. Abre un **issue** con la etiqueta `enhancement`
2. Describe:
   - **Problema** que resuelve
   - **Solución propuesta**
   - **Alternativas** consideradas
   - **Mockups** (si aplica)

### 🔧 Contribuir con Código

1. **Fork** el repositorio
2. **Crea una rama** desde `main`:
   ```bash
   git checkout -b feature/mi-nueva-funcionalidad
   ```
3. **Realiza tus cambios**
4. **Commit** siguiendo las convenciones:
   ```bash
   git commit -m "✨ feat: Agregar exportación a PDF"
   ```
5. **Push** a tu fork:
   ```bash
   git push origin feature/mi-nueva-funcionalidad
   ```
6. Abre un **Pull Request**

## Proceso de Pull Request

### Checklist antes de enviar

- [ ] El código compila sin errores
- [ ] He probado los cambios localmente
- [ ] He agregado comentarios XML donde es necesario
- [ ] He actualizado la documentación (si aplica)
- [ ] El código sigue la guía de estilo
- [ ] No hay warnings de compilación

### Revisión de PR

El PR será revisado considerando:

- ✅ **Funcionalidad**: ¿Hace lo que dice?
- ✅ **Calidad**: ¿El código es limpio y mantenible?
- ✅ **Rendimiento**: ¿Hay impacto en el rendimiento?
- ✅ **Seguridad**: ¿Introduce vulnerabilidades?
- ✅ **UX**: ¿Mejora la experiencia del usuario?

## Guía de Estilo

### C# Code Style

```csharp
// ✅ CORRECTO
/// <summary>Carga los partes de trabajo desde la API con caché de 30 minutos.</summary>
public async Task<List<ParteDto>> LoadPartesAsync(DateTime fecha)
{
    // Usar var para tipos obvios
    var path = $"/api/v1/partes?fecha={fecha:yyyy-MM-dd}";
    
    // Logs informativos
    App.Log?.LogInformation("Cargando partes para fecha: {fecha}", fecha);
    
    try
    {
        var result = await App.Api.GetAsync<List<ParteDto>>(path);
        return result ?? new List<ParteDto>();
    }
    catch (ApiException ex)
    {
        App.Log?.LogError(ex, "Error cargando partes");
        throw;
    }
}

// ❌ INCORRECTO
public async Task<List<ParteDto>> LoadPartesAsync(DateTime fecha) // Sin comentario XML
{
    string path = "/api/v1/partes?fecha=" + fecha.ToString("yyyy-MM-dd"); // No usar var ni interpolación
    
    var result = await App.Api.GetAsync<List<ParteDto>>(path); // Sin try-catch
    return result; // Puede retornar null
}
```

### Comentarios XML (Una sola línea)

```csharp
// ✅ CORRECTO
/// <summary>Valida el formato de hora (HH:mm) y retorna true si es válido.</summary>
public bool ValidateHoraFormat(string hora) { }

// ❌ INCORRECTO (multilínea innecesaria)
/// <summary>
/// Valida el formato de hora
/// </summary>
public bool ValidateHoraFormat(string hora) { }
```

### Naming Conventions

```csharp
// Clases, Métodos, Propiedades: PascalCase
public class ParteDto { }
public void LoadPartes() { }
public string NombreCliente { get; set; }

// Variables locales, parámetros: camelCase
var partesTotales = 10;
public void Process(int parteId) { }

// Constantes: PascalCase
public const int MaxRetries = 3;

// Campos privados: _camelCase
private List<ParteDto> _cache;
```

### XAML Style

```xml
<!-- Indentación: 4 espacios -->
<Grid>
    <StackPanel Spacing="8">
        <TextBlock Text="Título" 
                   FontSize="16" 
                   FontWeight="SemiBold" />
                   
        <Button Content="Guardar" 
                Click="OnGuardar" />
    </StackPanel>
</Grid>

<!-- Usar recursos para colores -->
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" />

<!-- NO hardcodear colores -->
<!-- ❌ <Border Background="#FF0000" /> -->
```

### Logs

```csharp
// Usar niveles apropiados
App.Log?.LogDebug("Valor de variable: {valor}", valor);          // Solo en Debug
App.Log?.LogInformation("Operación completada: {count} items", count);  // Info general
App.Log?.LogWarning("Cache expirado, recargando...");            // Advertencias
App.Log?.LogError(ex, "Error procesando parte {id}", parteId);   // Errores con excepción

// Usar structured logging (placeholders)
// ✅ CORRECTO
App.Log?.LogInformation("Usuario {user} creó parte {id}", userName, parteId);

// ❌ INCORRECTO
App.Log?.LogInformation($"Usuario {userName} creó parte {parteId}");
```

## Estructura del Proyecto

### Organización de Archivos

```
Views/
├── DiarioPage.xaml          # Vista principal
├── DiarioPage.xaml.cs       # Code-behind
└── ParteItemEdit.xaml       # Editor

ViewModels/
└── DiarioViewModel.cs       # ViewModel MVVM

Services/
├── ApiClient.cs             # Cliente HTTP
├── ProfileService.cs        # Lógica de negocio
└── Notifications/
    └── NotificationService.cs

Models/Dtos/
└── ParteDto.cs              # DTOs del dominio

Helpers/
├── Converters.cs            # Converters XAML
└── DiarioPageHelpers.cs     # Métodos helper
```

### Agregar Nueva Página

1. Crear `MiPagina.xaml` y `MiPagina.xaml.cs` en `Views/`
2. Crear `MiPaginaViewModel.cs` (si aplica) en `ViewModels/`
3. Registrar navegación en `MainWindow.xaml.cs`
4. Actualizar documentación

### Agregar Nuevo Servicio

1. Crear interfaz `IMyService.cs` en `Services/`
2. Implementar `MyService.cs`
3. Registrar en `App.xaml.cs` (si usa DI)
4. Agregar comentarios XML
5. Actualizar README.md

## Convenciones de Commits

Usamos [Conventional Commits](https://www.conventionalcommits.org/):

```
✨ feat: Nueva característica
🐛 fix: Corrección de bug
📝 docs: Documentación
🎨 style: Formato (sin cambios funcionales)
♻️ refactor: Refactorización
⚡ perf: Mejora de rendimiento
✅ test: Tests
🔧 chore: Mantenimiento
```

**Ejemplos:**

```bash
git commit -m "✨ feat: Agregar exportación a PDF"
git commit -m "🐛 fix: Corregir timeout en importación Excel"
git commit -m "📝 docs: Actualizar README con instrucciones MSI"
git commit -m "♻️ refactor: Extraer lógica de validación a helper"
```

## Testing

### Manual Testing

Antes de enviar un PR, probar:

1. **Happy path**: Flujo normal esperado
2. **Edge cases**: Valores límite, vacíos, null
3. **Errores**: Conexión perdida, timeout, datos inválidos
4. **UX**: Navegación, responsividad, accesibilidad

### Checklist de Testing

- [ ] Login/Logout funciona
- [ ] Crear nuevo parte funciona
- [ ] Editar parte funciona
- [ ] Eliminar parte funciona
- [ ] Importación Excel funciona
- [ ] Filtros funcionan
- [ ] Notificaciones aparecen
- [ ] Cambio de tema funciona
- [ ] No hay memory leaks

## Recursos Útiles

- [WinUI 3 Docs](https://microsoft.github.io/microsoft-ui-xaml/)
- [.NET 8 Docs](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [C# Coding Conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [MVVM Pattern](https://learn.microsoft.com/windows/communitytoolkit/mvvm/introduction)

## ❓ Preguntas

¿Tienes dudas? Abre un [Discussion](https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/discussions) en GitHub.

---

¡Gracias por contribuir! 🙏
