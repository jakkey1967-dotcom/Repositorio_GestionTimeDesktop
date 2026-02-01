# 🧪 PRUEBA DE ACTUALIZACIONES - GESTIONTIME DESKTOP

## ✅ OPCIÓN 1: Prueba Simulando Versión Anterior (SIN GitHub Release)

### Paso 1: Modificar temporalmente la versión actual
```xml
<!-- En GestionTime.Desktop.csproj -->
<AssemblyVersion>1.0.0.0</AssemblyVersion>  <!-- Era 1.1.0.0 -->
<FileVersion>1.0.0.0</FileVersion>
```

### Paso 2: Compilar y ejecutar
```powershell
dotnet build -c Debug
dotnet run
```

### Paso 3: En la app, verificar actualizaciones
- La app debería detectar que existe una versión **1.1.0** publicada en GitHub
- Mostrará el diálogo de actualización disponible

### Paso 4: Restaurar la versión real
```xml
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
```

---

## ✅ OPCIÓN 2: Prueba Real Publicando Nuevo Release en GitHub

### Paso 1: Incrementar la versión
```xml
<!-- En GestionTime.Desktop.csproj -->
<AssemblyVersion>1.2.0.0</AssemblyVersion>
<FileVersion>1.2.0.0</FileVersion>
```

### Paso 2: Publicar el proyecto
```powershell
dotnet publish GestionTime.Desktop.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -p:PublishReadyToRun=true `
  -o "publish\portable"
```

### Paso 3: Crear el ZIP del instalador
```powershell
# Copiar archivos necesarios a la carpeta Installer/App
Copy-Item "publish\portable\*" -Destination "Installer\App\" -Recurse -Force

# Crear el ZIP
Compress-Archive -Path "Installer\*" -DestinationPath "GestionTime-v1.2.0.zip"
```

### Paso 4: Crear Release en GitHub
1. Ve a: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new
2. **Tag version:** `v1.2.0`
3. **Release title:** `GestionTime Desktop v1.2.0`
4. **Describe this release:**
   ```markdown
   ## ✨ Novedades
   - Mejora en el sistema de exportación
   - Optimizaciones de rendimiento
   
   ## 🐛 Correcciones
   - Corrección de errores menores
   ```
5. Adjunta el archivo `GestionTime-v1.2.0.zip`
6. Publica el release

### Paso 5: Volver a versión anterior para probar
```xml
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
```

### Paso 6: Ejecutar y verificar
- La app detectará la nueva versión 1.2.0
- Mostrará el diálogo con la opción de descargar

---

## ✅ OPCIÓN 3: Prueba con Mock del UpdateService (Para testing automatizado)

### Crear un Mock Service para pruebas unitarias:

```csharp
// Services/UpdateServiceMock.cs
public class UpdateServiceMock : IUpdateService
{
    private readonly string _mockLatestVersion;
    
    public UpdateServiceMock(string mockLatestVersion = "2.0.0")
    {
        _mockLatestVersion = mockLatestVersion;
    }
    
    public string GetCurrentVersion() => "1.1.0";
    
    public Task<UpdateInfo> CheckForUpdatesAsync()
    {
        return Task.FromResult(new UpdateInfo
        {
            CurrentVersion = "1.1.0",
            LatestVersion = _mockLatestVersion,
            UpdateAvailable = true,
            DownloadUrl = "https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/download/v2.0.0/GestionTime-v2.0.0.zip",
            ReleaseNotes = "## ✨ Nueva versión disponible\n- Mejoras importantes\n- Corrección de bugs",
            PublishedAt = DateTime.Now,
            ReleaseName = "GestionTime Desktop v2.0.0"
        });
    }
    
    public void OpenReleasesPage() { }
    
    public Task<bool> DownloadUpdateAsync(string downloadUrl, string destinationPath)
    {
        return Task.FromResult(true);
    }
}
```

### Usar el mock en App.xaml.cs temporalmente:

```csharp
// En App.xaml.cs, reemplazar temporalmente:
// UpdateService = new UpdateService(loggerFactory.CreateLogger<UpdateService>());

// Por:
UpdateService = new UpdateServiceMock("2.0.0"); // Simula versión 2.0.0 disponible
```

---

## 🔍 VERIFICACIÓN DEL FLUJO COMPLETO

### 1. Al iniciar la app:
```csharp
// En App.xaml.cs se verifica automáticamente
var updateInfo = await UpdateService.CheckForUpdatesAsync();
if (updateInfo.UpdateAvailable)
{
    // Muestra notificación
}
```

### 2. Endpoints verificados:
- ✅ API de GitHub: `https://api.github.com/repos/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/latest`
- ✅ Página de releases: `https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases`

### 3. Formato esperado del ZIP en GitHub:
- Nombre: `GestionTime-v{version}.zip`
- Ejemplo: `GestionTime-v1.2.0.zip`

### 4. Logs a revisar:
```
🔄 Verificando actualizaciones en GitHub...
✅ Nueva versión disponible: 1.2.0 (actual: 1.1.0)
```

---

## 🚀 RECOMENDACIÓN: Prueba Rápida (5 minutos)

1. Cambiar temporalmente la versión en `.csproj` a `1.0.0.0`
2. Compilar: `dotnet build`
3. Ejecutar la app
4. Debería detectar que existe la versión `1.1.0` ya publicada en GitHub
5. Restaurar la versión a `1.1.0.0`

---

## 📊 FLUJO DE ACTUALIZACIÓN ACTUAL

```
┌─────────────────────────────────────────────────────────────┐
│  1. App.xaml.cs OnLaunched()                                │
│     └─> UpdateService.CheckForUpdatesAsync()               │
│         └─> GET api.github.com/repos/.../releases/latest   │
│             └─> Compara versión actual vs tag_name         │
│                 └─> Si hay nueva versión:                  │
│                     └─> Muestra notificación               │
│                         └─> Usuario clickea "Ver detalles" │
│                             └─> OpenReleasesPage()        │
│                                 └─> Abre navegador        │
└─────────────────────────────────────────────────────────────┘
```

---

## ✅ CHECKLIST DE PRUEBA

- [ ] Modificar versión en .csproj a 1.0.0
- [ ] Compilar el proyecto
- [ ] Ejecutar la aplicación
- [ ] Verificar que muestra notificación de actualización
- [ ] Hacer clic en "Ver detalles"
- [ ] Verificar que abre la página de releases de GitHub
- [ ] Restaurar versión a 1.1.0
- [ ] (Opcional) Publicar versión 1.2.0 real en GitHub
- [ ] Probar descarga e instalación manual

---

**💡 TIP:** Para pruebas rápidas, usa la OPCIÓN 1. Para pruebas reales antes de publicar a usuarios, usa la OPCIÓN 2.
