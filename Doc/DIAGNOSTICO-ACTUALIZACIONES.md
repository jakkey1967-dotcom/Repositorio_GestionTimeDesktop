# 🔍 GUÍA DE DIAGNÓSTICO - Sistema de Actualizaciones

## ✅ LO QUE SE HA HECHO:

1. ✅ Cambiado de `UpdateServiceMock` a `UpdateService` real
2. ✅ Agregado logging detallado en `GetCurrentVersion()`
3. ✅ Agregado logging detallado en `IsNewerVersion()`
4. ✅ Agregado logging detallado en `CheckForUpdatesAsync()`
5. ✅ Proyecto compilado con versión 1.2.0-beta

## 🎯 PRÓXIMOS PASOS PARA DIAGNOSTICAR:

### Opción 1: Ejecutar desde Visual Studio (RECOMENDADO)

1. **Abre el proyecto en Visual Studio**
2. **Asegúrate de estar en modo Debug**
3. **Presiona F5 para ejecutar con debugger**
4. **Mira la ventana "Output" o "Debug"**
5. **Espera 10 segundos después del login**
6. **Los logs aparecerán en la ventana**

Busca estas líneas:
```
=== DEBUG GetCurrentVersion ===
InformationalVersion encontrado: ...
=== Comparando versiones ===
Versión actual: ...
Versión última: ...
```

### Opción 2: Buscar el archivo de logs

Los logs se guardan en uno de estos lugares:

```powershell
# Buscar logs:
Get-ChildItem -Path "C:\App\GestionTime-Desktop" -Filter "*.log" -Recurse -ErrorAction SilentlyContinue
Get-ChildItem -Path "$env:LOCALAPPDATA\GestionTime" -Filter "*.log" -Recurse -ErrorAction SilentlyContinue
Get-ChildItem -Path "bin\x64\Debug" -Filter "*.log" -Recurse -ErrorAction SilentlyContinue
```

### Opción 3: Test manual simplificado

Ejecuta este PowerShell script:

```powershell
$ErrorActionPreference = "Continue"

Write-Host "=== TEST UPDATE SERVICE ===" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar versión del ejecutable
$exe = "bin\x64\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe"
if (Test-Path $exe) {
    $version = (Get-Item $exe).VersionInfo
    Write-Host "✅ Versión del ejecutable:" -ForegroundColor Green
    Write-Host "   FileVersion: $($version.FileVersion)" -ForegroundColor Cyan
    Write-Host "   ProductVersion: $($version.ProductVersion)" -ForegroundColor Cyan
} else {
    Write-Host "❌ Ejecutable no encontrado" -ForegroundColor Red
    exit
}

Write-Host ""

# 2. Verificar qué hay en GitHub
Write-Host "🔍 Verificando GitHub API..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://api.github.com/repos/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases" -Method Get
    
    if ($response -and $response.Count -gt 0) {
        $latest = $response[0]
        Write-Host "✅ Release más reciente en GitHub:" -ForegroundColor Green
        Write-Host "   Tag: $($latest.tag_name)" -ForegroundColor Cyan
        Write-Host "   Name: $($latest.name)" -ForegroundColor Cyan
        Write-Host "   PreRelease: $($latest.prerelease)" -ForegroundColor Cyan
        Write-Host "   Assets: $($latest.assets.Count)" -ForegroundColor Cyan
        
        if ($latest.assets.Count -gt 0) {
            Write-Host ""
            Write-Host "   Archivos disponibles:" -ForegroundColor Yellow
            foreach ($asset in $latest.assets) {
                Write-Host "     - $($asset.name)" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "⚠️ No se encontraron releases" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error al consultar GitHub API:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 3. Comparación manual
Write-Host "🔢 Comparación de versiones:" -ForegroundColor Yellow
$current = "1.2.0-beta"
$latest = if ($response -and $response.Count -gt 0) { $response[0].tag_name.TrimStart('v') } else { "desconocida" }

Write-Host "   Actual:  $current" -ForegroundColor Cyan
Write-Host "   GitHub:  $latest" -ForegroundColor Cyan

if ($latest -ne "desconocida") {
    # Parsear versiones
    $currentParts = $current -replace '-.*', '' -split '\.'
    $latestParts = $latest -replace '-.*', '' -split '\.'
    
    $currentMajor = [int]$currentParts[0]
    $currentMinor = [int]$currentParts[1]
    $currentPatch = [int]$currentParts[2]
    
    $latestMajor = [int]$latestParts[0]
    $latestMinor = [int]$latestParts[1]
    $latestPatch = [int]$latestParts[2]
    
    Write-Host ""
    Write-Host "   Parseado actual: $currentMajor.$currentMinor.$currentPatch" -ForegroundColor Gray
    Write-Host "   Parseado GitHub: $latestMajor.$latestMinor.$latestPatch" -ForegroundColor Gray
    Write-Host ""
    
    $shouldUpdate = ($latestMajor -gt $currentMajor) -or 
                    (($latestMajor -eq $currentMajor) -and ($latestMinor -gt $currentMinor)) -or
                    (($latestMajor -eq $currentMajor) -and ($latestMinor -eq $currentMinor) -and ($latestPatch -gt $currentPatch))
    
    if ($shouldUpdate) {
        Write-Host "✅ DEBERÍA APARECER EL DIÁLOGO DE ACTUALIZACIÓN" -ForegroundColor Green
    } else {
        Write-Host "ℹ️ NO debería aparecer el diálogo (versiones iguales o actual más nueva)" -ForegroundColor Cyan
    }
} else {
    Write-Host "⚠️ No se puede comparar" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== FIN DEL TEST ===" -ForegroundColor Cyan
```

Guarda esto como `test-update.ps1` y ejecútalo.

## 📊 RESULTADOS ESPERADOS:

Si todo está bien, deberías ver:
- ✅ FileVersion: 1.2.0.0
- ✅ ProductVersion: 1.2.0-beta+...
- ✅ GitHub Tag: v1.3.0-beta
- ✅ DEBERÍA APARECER EL DIÁLOGO

Si algo falla:
- ❌ GitHub no devuelve v1.3.0-beta → El release no está publicado correctamente
- ❌ ProductVersion es 1.3.0-beta → Estás ejecutando la versión equivocada
- ❌ Comparación dice "NO debería aparecer" → Hay un bug en la lógica

## 🐛 POSIBLES PROBLEMAS:

1. **UpdateService.CheckForUpdatesAsync() no se está ejecutando**
   - Verificar que se llama en App.xaml.cs línea 319
   - Verificar que no hay excepciones silenciosas

2. **La comparación está fallando**
   - El método ParseVersion() no maneja bien los sufijos "-beta"
   - Necesita limpiarlos antes de parsear

3. **XamlRoot no está disponible**
   - El diálogo necesita XamlRoot para mostrarse
   - Si es null, el diálogo no aparece

4. **GitHub API devuelve datos incorrectos**
   - El release no está marcado correctamente
   - El tag name no coincide

## ✅ SOLUCIÓN RÁPIDA:

**Ejecuta la app desde Visual Studio** y mira los logs en la ventana Output. Ahí verás exactamente qué está pasando.
