# Contribución a GestionTime Desktop

¡Gracias por tu interés en contribuir a GestionTime Desktop! Este documento proporciona guías y mejores prácticas para contribuir al proyecto.

## 📋 Tabla de Contenidos

- [Código de Conducta](#código-de-conducta)
- [Cómo Contribuir](#cómo-contribuir)
- [Reportar Bugs](#reportar-bugs)
- [Sugerir Características](#sugerir-características)
- [Pull Requests](#pull-requests)
- [Estándares de Código](#estándares-de-código)
- [Configuración del Entorno](#configuración-del-entorno)

## 🤝 Código de Conducta

Este proyecto adhiere a un código de conducta profesional. Al participar, se espera que mantengas este código.

### Comportamiento Esperado

- ✅ Usa lenguaje acogedor e inclusivo
- ✅ Respeta diferentes puntos de vista
- ✅ Acepta críticas constructivas
- ✅ Enfócate en lo mejor para el proyecto

### Comportamiento Inaceptable

- ❌ Lenguaje ofensivo o comentarios despectivos
- ❌ Ataques personales o políticos
- ❌ Acoso público o privado
- ❌ Publicar información privada de otros

## 🚀 Cómo Contribuir

### 1. Fork del Repositorio

```bash
# Fork en GitHub, luego clona tu fork
git clone https://github.com/TU-USUARIO/GestionTime.Desktop.git
cd GestionTime.Desktop

# Agrega el repositorio original como remote
git remote add upstream https://github.com/jakkey1967-dotcom/GestionTime.Desktop.git
```

### 2. Crea una Rama

```bash
# Actualiza tu fork
git fetch upstream
git checkout main
git merge upstream/main

# Crea tu rama de característica
git checkout -b feature/mi-nueva-caracteristica
# O para corrección de bugs
git checkout -b fix/correccion-de-bug
```

### 3. Realiza tus Cambios

- Escribe código siguiendo las [guías de estilo](#estándares-de-código)
- Agrega tests si es necesario
- Actualiza documentación si es aplicable
- Verifica que el código compila sin errores

### 4. Commit

```bash
git add .
git commit -m "feat: descripción breve del cambio"
```

#### Formato de Mensajes de Commit

Usamos [Conventional Commits](https://www.conventionalcommits.org/):

```
tipo(alcance): descripción breve

[cuerpo opcional con más detalles]

[footer opcional con referencias a issues]
```

**Tipos válidos:**
- `feat`: Nueva característica
- `fix`: Corrección de bug
- `docs`: Cambios en documentación
- `style`: Formato de código (espacios, punto y coma, etc.)
- `refactor`: Refactorización de código
- `test`: Agregar o modificar tests
- `chore`: Tareas de mantenimiento

**Ejemplos:**
```bash
feat(partes): agregar botón de duplicación de partes
fix(login): corregir error al guardar token
docs(readme): actualizar guía de instalación
refactor(api): simplificar lógica de refresh tokens
```

### 5. Push y Pull Request

```bash
# Push a tu fork
git push origin feature/mi-nueva-caracteristica

# Crea Pull Request en GitHub
# Incluye descripción detallada y referencias a issues
```

## 🐛 Reportar Bugs

### Antes de Reportar

1. Verifica que estás usando la última versión
2. Busca en issues existentes
3. Comprueba la documentación en `Helpers/`

### Cómo Reportar

Crea un issue con:

```markdown
**Descripción del Bug**
Descripción clara y concisa del problema.

**Pasos para Reproducir**
1. Ir a '...'
2. Click en '....'
3. Scroll hasta '....'
4. Ver error

**Comportamiento Esperado**
Qué esperabas que sucediera.

**Comportamiento Actual**
Qué sucedió realmente.

**Screenshots**
Si aplica, agrega capturas de pantalla.

**Entorno:**
- OS: [ej. Windows 11]
- Versión App: [ej. 1.0.0]
- .NET: [ej. 8.0.1]

**Logs**
```
[Pega aquí el contenido de los logs]
```

**Información Adicional**
Cualquier otro contexto relevante.
```

## 💡 Sugerir Características

### Antes de Sugerir

1. Verifica el roadmap en CHANGELOG.md
2. Busca sugerencias similares en issues
3. Considera si es realmente necesario

### Cómo Sugerir

Crea un issue con:

```markdown
**¿El problema está relacionado con una frustración? Describe.**
Descripción clara del problema que esta característica resolvería.

**Describe la solución que te gustaría**
Descripción clara y concisa de lo que quieres que suceda.

**Describe alternativas consideradas**
Descripción de soluciones o características alternativas.

**Información adicional**
Screenshots, mockups, o contexto adicional.
```

## 🔄 Pull Requests

### Checklist de Pull Request

Antes de enviar tu PR, asegúrate de:

- [ ] El código compila sin errores
- [ ] Has seguido las guías de estilo del proyecto
- [ ] Has comentado el código en áreas complejas
- [ ] Has actualizado la documentación si es necesario
- [ ] Tus cambios no generan nuevas advertencias
- [ ] Has agregado tests que prueban tu fix o característica
- [ ] Los tests existentes pasan
- [ ] Has actualizado CHANGELOG.md

### Proceso de Revisión

1. Un maintainer revisará tu PR
2. Pueden solicitar cambios
3. Realiza los cambios solicitados
4. Una vez aprobado, se hará merge

## 📝 Estándares de Código

### C# / .NET

```csharp
// Usa PascalCase para nombres de clases, métodos y propiedades
public class ParteService
{
    public async Task<Parte> GetParteAsync(int id)
    {
        // Usa camelCase para variables locales y parámetros
        var parte = await _repository.GetByIdAsync(id);
        return parte;
    }
}

// Usa prefijo _ para campos privados
private readonly IParteRepository _repository;
private readonly ILogger<ParteService> _logger;

// Usa async/await para operaciones asíncronas
public async Task<List<Parte>> GetPartesAsync(DateTime fecha)
{
    return await _apiClient.GetPartesAsync(fecha);
}

// Documentación XML para APIs públicas
/// <summary>
/// Obtiene un parte por su ID.
/// </summary>
/// <param name="id">ID del parte</param>
/// <returns>Parte encontrado o null</returns>
public async Task<Parte?> GetParteAsync(int id)
{
    // ...
}
```

### XAML

```xml
<!-- Usa espaciado consistente -->
<Page
    x:Class="GestionTime.Desktop.Views.DiarioPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- Agrupa elementos relacionados -->
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- Usa nombres descriptivos para elementos con x:Name -->
        <StackPanel x:Name="HeaderPanel" Grid.Row="0">
            <!-- Contenido -->
        </StackPanel>
    </Grid>
</Page>
```

### Logging

```csharp
// Usa niveles apropiados
_logger.LogDebug("Detalles técnicos útiles para debugging");
_logger.LogInformation("Evento importante en el flujo normal");
_logger.LogWarning("Situación inusual pero manejable");
_logger.LogError(ex, "Error que requiere atención");
_logger.LogCritical(ex, "Fallo crítico del sistema");

// Usa structured logging
_logger.LogInformation("Usuario {UserId} creó parte {ParteId}", userId, parteId);
```

## 🛠️ Configuración del Entorno

### Requisitos

- Visual Studio 2022 (17.8+)
- .NET 8 SDK
- Windows App SDK 1.8
- Git

### Setup

```bash
# 1. Clonar repositorio
git clone https://github.com/jakkey1967-dotcom/GestionTime.Desktop.git
cd GestionTime.Desktop

# 2. Restaurar dependencias
dotnet restore

# 3. Compilar
dotnet build -c Debug

# 4. Ejecutar
dotnet run
```

### Configuración de appsettings.json

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5000",  // URL local para desarrollo
    "LoginPath": "/api/v1/auth/login-desktop",
    "PartesPath": "/api/v1/partes"
  },
  "Logging": {
    "LogPath": "logs"
  }
}
```

### Tests

```bash
# Ejecutar todos los tests
dotnet test

# Con verbosidad detallada
dotnet test --logger "console;verbosity=detailed"

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## 📚 Recursos

- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [WinUI 3 Documentation](https://docs.microsoft.com/windows/apps/winui/)
- [MVVM Toolkit](https://docs.microsoft.com/windows/communitytoolkit/mvvm/introduction)

## ❓ Preguntas

Si tienes preguntas, puedes:

1. Revisar la documentación en `Helpers/`
2. Buscar en issues cerrados
3. Crear un nuevo issue con la etiqueta `question`

---

¡Gracias por contribuir a GestionTime Desktop! 🎉
