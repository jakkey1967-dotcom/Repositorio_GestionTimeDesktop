# 🔍 Diagnóstico: Aplicación No Arranca Después de Instalarse

## 📋 Problema

La aplicación **GestionTime Desktop** se instala correctamente, crea la carpeta, pero no arranca cuando se ejecuta el `.exe`.

---

## 🎯 Causas Más Comunes

### 1. ⚠️ **Windows App Runtime No Instalado** (CAUSA #1 - MÁS PROBABLE)

WinUI 3 requiere el **Windows App Runtime** instalado en el sistema. Si no está presente, la aplicación no inicia y puede fallar silenciosamente.

**Síntomas:**
- El ejecutable no abre ninguna ventana
- No hay mensaje de error visible
- Puede aparecer brevemente en el Administrador de Tareas y desaparecer

**Solución:**
```powershell
# Verificar si está instalado
winget list --id Microsoft.WindowsAppRuntime.1.8

# Si no está, instalarlo
winget install Microsoft.WindowsAppRuntime.1.8 --source winget
```

**Alternativa Manual:**
1. Descargar desde: https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
2. Ejecutar el instalador como Administrador
3. Reintentar abrir la aplicación

---

### 2. 📁 **Archivo appsettings.json Faltante o Mal Ubicado**

La aplicación busca `appsettings.json` en estas ubicaciones (en orden):
1. `AppContext.BaseDirectory` (junto al `.exe`)
2. `Environment.CurrentDirectory` (directorio de trabajo actual)

**Verificación:**
```powershell
# En la carpeta de instalación, buscar:
Get-ChildItem -Path "C:\Path\To\GestionTime" -Filter "appsettings.json"
```

**Solución:**
- Asegurar que `appsettings.json` está en la misma carpeta que `GestionTime.Desktop.exe`
- Verificar que el archivo no tiene extensión oculta (ej: `.json.txt`)

---

### 3. 🛡️ **Archivos Bloqueados por Windows (Zone.Identifier)**

Windows puede bloquear archivos descargados de Internet, especialmente DLLs y ejecutables.

**Síntomas:**
- No inicia sin mensaje
- En propiedades del archivo aparece "Este archivo proviene de otro equipo..."

**Solución:**
```powershell
# Desbloquear todos los archivos
Get-ChildItem -Path "C:\Path\To\GestionTime" -Recurse | Unblock-File
```

**Alternativa Manual:**
1. Click derecho en `GestionTime.Desktop.exe` → Propiedades
2. En la pestaña **General**, marcar **"Desbloquear"**
3. Aplicar y repetir para archivos `.dll` si es necesario

---

### 4. 🚫 **Dependencias DLL Faltantes**

Aunque compilas como `self-contained`, algunas DLLs nativas pueden faltar.

**Verificación:**
```powershell
# Listar DLLs en la carpeta
Get-ChildItem -Path "C:\Path\To\GestionTime" -Filter "*.dll" | Select-Object Name

# Verificar que existen al menos estas:
# - Microsoft.WindowsAppRuntime.dll
# - Microsoft.Windows.ApplicationModel.dll
# - Microsoft.UI.Xaml.dll
# - WinRT.Runtime.dll
```

**Solución:**
- Recompilar con `--self-contained true` (ya lo haces)
- Verificar que todas las DLLs del `publish` se copiaron

---

### 5. 📝 **Error en Inicialización de Logs**

El código intenta crear logs en `AppContext.BaseDirectory\logs\app.log`. Si falla, puede crashear.

**Verificación:**
```powershell
# Crear carpeta de logs manualmente
New-Item -Path "C:\Path\To\GestionTime\logs" -ItemType Directory -Force
```

**Solución Temporal en Código:**
Ver sección "Soluciones" más abajo.

---

### 6. 🔐 **Permisos Insuficientes**

La carpeta de instalación puede tener permisos restrictivos.

**Solución:**
```powershell
# Ejecutar como Administrador (una vez)
Right-click GestionTime.Desktop.exe → "Ejecutar como administrador"
```

**Alternativa:**
- Instalar en carpeta de usuario: `C:\Users\<username>\GestionTime`
- Evitar `C:\Program Files` para apps portables

---

### 7. 🔴 **Certificado No Confiado (Solo MSIX)**

Si usas instalador MSIX, el certificado debe ser de confianza.

**Solución:**
```powershell
# Instalar certificado (ejecutar como Admin)
certutil -addstore "TrustedPeople" "path\to\certificate.cer"
```

---

## 🛠️ **Herramientas de Diagnóstico**

### Usar Event Viewer de Windows

1. Presionar `Win + R` → escribir `eventvwr.msc`
2. Navegar a: **Windows Logs → Application**
3. Buscar errores recientes relacionados con:
   - `.NET Runtime`
   - `Application Error`
   - `WinRT/WinUI`

### Ver Salida de Debug

Ejecutar desde PowerShell para ver errores:

```powershell
cd "C:\Path\To\GestionTime"
.\GestionTime.Desktop.exe
# Esperar y observar errores en consola
```

### Dependency Walker (Avanzado)

Descargar: https://www.dependencywalker.com/

Abrir `GestionTime.Desktop.exe` y verificar que todas las DLLs se encuentran.

---

## ✅ **Soluciones Rápidas**

### Checklist Rápido

```powershell
# 1. Verificar Windows App Runtime
winget list --id Microsoft.WindowsAppRuntime.1.8

# 2. Desbloquear archivos
Get-ChildItem -Path "." -Recurse | Unblock-File

# 3. Verificar appsettings.json
Test-Path ".\appsettings.json"

# 4. Crear carpeta logs
New-Item -Path ".\logs" -ItemType Directory -Force

# 5. Ejecutar como Admin
Start-Process ".\GestionTime.Desktop.exe" -Verb RunAs
```

---

## 🔧 **Solución: Script de Verificación**

Crea un archivo `Verificar-Instalacion.ps1` junto al ejecutable:

```powershell
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 VERIFICADOR DE INSTALACIÓN - GestionTime Desktop" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar Windows App Runtime
Write-Host "1️⃣ Verificando Windows App Runtime..." -ForegroundColor Yellow
$runtime = winget list --id Microsoft.WindowsAppRuntime.1.8 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Windows App Runtime 1.8 instalado" -ForegroundColor Green
} else {
    Write-Host "   ❌ Windows App Runtime 1.8 NO encontrado" -ForegroundColor Red
    Write-Host "      Ejecutar: winget install Microsoft.WindowsAppRuntime.1.8" -ForegroundColor Yellow
}
Write-Host ""

# 2. Verificar archivos críticos
Write-Host "2️⃣ Verificando archivos críticos..." -ForegroundColor Yellow
$files = @(
    "GestionTime.Desktop.exe",
    "appsettings.json",
    "Microsoft.UI.Xaml.dll",
    "WinRT.Runtime.dll"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "   ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $file FALTANTE" -ForegroundColor Red
    }
}
Write-Host ""

# 3. Verificar archivos bloqueados
Write-Host "3️⃣ Verificando archivos bloqueados..." -ForegroundColor Yellow
$blocked = Get-ChildItem -Recurse -File | Get-Item -Stream Zone.Identifier -ErrorAction SilentlyContinue
if ($blocked) {
    Write-Host "   ⚠️ Archivos bloqueados encontrados. Desbloqueando..." -ForegroundColor Yellow
    Get-ChildItem -Recurse | Unblock-File
    Write-Host "   ✅ Archivos desbloqueados" -ForegroundColor Green
} else {
    Write-Host "   ✅ Sin archivos bloqueados" -ForegroundColor Green
}
Write-Host ""

# 4. Crear carpeta logs
Write-Host "4️⃣ Verificando carpeta de logs..." -ForegroundColor Yellow
if (-not (Test-Path "logs")) {
    New-Item -Path "logs" -ItemType Directory -Force | Out-Null
    Write-Host "   ✅ Carpeta 'logs' creada" -ForegroundColor Green
} else {
    Write-Host "   ✅ Carpeta 'logs' existe" -ForegroundColor Green
}
Write-Host ""

# 5. Verificar permisos
Write-Host "5️⃣ Verificando permisos de escritura..." -ForegroundColor Yellow
try {
    $testFile = "logs\test_$(Get-Date -Format 'yyyyMMddHHmmss').txt"
    "Test" | Out-File $testFile
    Remove-Item $testFile
    Write-Host "   ✅ Permisos de escritura OK" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Sin permisos de escritura en 'logs'" -ForegroundColor Red
    Write-Host "      Ejecutar como Administrador o mover a carpeta de usuario" -ForegroundColor Yellow
}
Write-Host ""

# Resumen
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ VERIFICACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Si todo está ✅, intenta ejecutar:" -ForegroundColor Yellow
Write-Host "   .\GestionTime.Desktop.exe" -ForegroundColor White
Write-Host ""
Write-Host "Si sigue sin iniciar, ejecuta como Administrador o revisa Event Viewer." -ForegroundColor Yellow
Write-Host ""

Pause
```

---

## 🚀 **Recomendación: Mejorar el Instalador**

Para evitar estos problemas, mejora tu proceso de build:

### Opción 1: Incluir Windows App Runtime en el Instalador

Modifica `build-installer.ps1`:

```powershell
# Después de crear el ZIP, añadir:
Write-Host "📥 Descargando Windows App Runtime..." -ForegroundColor Yellow
$runtimeUrl = "https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe"
$runtimeFile = "WindowsAppRuntime-1.8-x64.exe"
Invoke-WebRequest -Uri $runtimeUrl -OutFile $runtimeFile

# Añadir al ZIP o carpeta
Copy-Item $runtimeFile -Destination $outputFolder
```

### Opción 2: Crear un Instalador con Detección Automática

Crea un `Instalar.bat` que verifique dependencias:

```batch
@echo off
echo ========================================
echo   GestionTime Desktop - Instalador
echo ========================================
echo.

REM Verificar Windows App Runtime
echo Verificando dependencias...
winget list --id Microsoft.WindowsAppRuntime.1.8 >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [!] Windows App Runtime 1.8 no encontrado
    echo [*] Instalando automaticamente...
    winget install Microsoft.WindowsAppRuntime.1.8 --silent --accept-package-agreements
    if %ERRORLEVEL% NEQ 0 (
        echo.
        echo [X] Error instalando dependencias
        echo [*] Instalar manualmente desde:
        echo     https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
        pause
        exit /b 1
    )
)

echo [OK] Dependencias verificadas
echo.

REM Desbloquear archivos
echo Desbloqueando archivos...
powershell -Command "Get-ChildItem -Recurse | Unblock-File"

REM Crear carpeta logs
if not exist "logs" mkdir logs

echo.
echo ========================================
echo   Instalacion completada
echo ========================================
echo.
echo Ejecuta: GestionTime.Desktop.exe
echo.
pause
```

---

## 📞 **Siguiente Paso**

1. **Ejecuta el Script de Verificación** creado arriba
2. **Revisa Event Viewer** para errores específicos
3. **Prueba ejecutar como Administrador** una vez
4. **Si nada funciona**, comparte:
   - Output de Event Viewer
   - Resultado del script de verificación
   - Versión de Windows (`winver`)

---

## 💡 **Nota Importante**

Si tu aplicación usa **MSIX** en lugar de ZIP portable, el problema puede ser diferente (certificado, manifiesto, etc.). En ese caso, necesitamos revisar el Package.appxmanifest y proceso de empaquetado MSIX.

¿Estás usando el instalador portable (ZIP) o MSIX?
