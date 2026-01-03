# ====================================================================
# Script Definitivo para crear instalador MSI de GestionTime Desktop
# Fecha: 2025-01-27
# Version: 1.1.0.0
# ====================================================================

param(
    [string]$Version = "1.1.0.0",
    [switch]$SkipPublish = $false,
    [switch]$OpenAfter = $true
)

$ErrorActionPreference = "Stop"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  CREACION DE INSTALADOR MSI               " -ForegroundColor Cyan
Write-Host "  GestionTime Desktop v$Version            " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# ====================================================================
# 1. VERIFICAR HERRAMIENTAS NECESARIAS
# ====================================================================

Write-Host "1. Verificando herramientas necesarias..." -ForegroundColor Yellow

# Verificar dotnet
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "   [ERROR] .NET SDK no encontrado" -ForegroundColor Red
    Write-Host "   Por favor instala .NET 8 SDK desde: https://dot.net" -ForegroundColor Yellow
    exit 1
}
Write-Host "   [OK] .NET SDK encontrado" -ForegroundColor Green

# Verificar/Instalar WiX Toolset
Write-Host "   Verificando WiX Toolset..." -ForegroundColor Gray

$wixInstalled = $false
try {
    $wixVersion = dotnet tool list --global | Select-String "wix"
    if ($wixVersion) {
        Write-Host "   [OK] WiX Toolset encontrado (global)" -ForegroundColor Green
        $wixInstalled = $true
    }
} catch {
    # Ignorar
}

if (!$wixInstalled) {
    Write-Host "   [WARN] WiX Toolset no encontrado, instalando..." -ForegroundColor Yellow
    dotnet tool install --global wix --version 5.0.2
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   [OK] WiX Toolset instalado correctamente" -ForegroundColor Green
        $wixInstalled = $true
    } else {
        Write-Host "   [ERROR] Error instalando WiX Toolset" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# ====================================================================
# 2. LIMPIAR COMPILACIONES ANTERIORES
# ====================================================================

Write-Host "2. Limpiando compilaciones anteriores..." -ForegroundColor Yellow

$publishPath = "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
$installerPath = "bin\Release\Installer"
$msiOutputPath = "bin\Release\MSI"

$foldersToClean = @($publishPath, $installerPath, $msiOutputPath)

foreach ($folder in $foldersToClean) {
    if (Test-Path $folder) {
        Write-Host "   Limpiando: $folder" -ForegroundColor Gray
        Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "   [OK] Carpetas limpiadas" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 3. PUBLICAR APLICACION EN RELEASE
# ====================================================================

if (!$SkipPublish) {
    Write-Host "3. Publicando aplicacion en modo Release..." -ForegroundColor Yellow
    Write-Host "   Esto puede tardar unos minutos..." -ForegroundColor Gray
    
    $publishArgs = @(
        "publish"
        "GestionTime.Desktop.csproj"
        "-c", "Release"
        "-r", "win-x64"
        "--self-contained", "true"
        "-p:PublishSingleFile=false"
        "-p:PublishReadyToRun=true"
        "-p:IncludeNativeLibrariesForSelfExtract=true"
        "-p:EnableCompressionInSingleFile=false"
        "-o", $publishPath
        "-v", "minimal"
    )
    
    & dotnet @publishArgs | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   [ERROR] Error en la publicacion" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   [OK] Aplicacion publicada exitosamente" -ForegroundColor Green
} else {
    Write-Host "3. Saltando publicacion (usando archivos existentes)..." -ForegroundColor Yellow
    
    if (!(Test-Path $publishPath)) {
        Write-Host "   [ERROR] No se encontraron archivos publicados" -ForegroundColor Red
        Write-Host "   Ejecuta sin -SkipPublish para generar los archivos" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""

# ====================================================================
# 4. COPIAR ARCHIVOS AL DIRECTORIO DE INSTALADOR
# ====================================================================

Write-Host "4. Preparando archivos para el instalador..." -ForegroundColor Yellow

$appFolder = Join-Path $installerPath "App"
New-Item -ItemType Directory -Path $appFolder -Force | Out-Null

# Copiar todos los archivos publicados
Write-Host "   Copiando archivos de aplicacion..." -ForegroundColor Gray
Copy-Item -Path "$publishPath\*" -Destination $appFolder -Recurse -Force

# Contar archivos copiados
$fileCount = (Get-ChildItem -Path $appFolder -Recurse -File).Count
Write-Host "   Total de archivos copiados: $fileCount" -ForegroundColor Gray

# Verificar archivos criticos
$criticalFiles = @(
    "GestionTime.Desktop.exe",
    "appsettings.json",
    "Microsoft.WindowsAppRuntime.Bootstrap.dll",
    "Microsoft.WindowsAppRuntime.dll"
)

$missingFiles = @()
foreach ($file in $criticalFiles) {
    $filePath = Join-Path $appFolder $file
    if (!(Test-Path $filePath)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host "   [ERROR] Archivos criticos faltantes:" -ForegroundColor Red
    foreach ($file in $missingFiles) {
        Write-Host "      - $file" -ForegroundColor Gray
    }
    exit 1
}

Write-Host "   [OK] Todos los archivos criticos presentes" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 5. GENERAR COMPONENTES WIX AUTOMATICAMENTE
# ====================================================================

Write-Host "5. Generando componentes WiX..." -ForegroundColor Yellow

# Asegurar que existe el directorio MSI
if (!(Test-Path "Installer\MSI")) {
    New-Item -ItemType Directory -Path "Installer\MSI" -Force | Out-Null
}

# Crear archivo Features.wxs con todos los archivos
$featuresWxs = @'
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  
  <Fragment>
    <Feature Id="MainApplication" 
             Title="GestionTime Desktop" 
             Description="Aplicacion principal de gestion de tiempo"
             Level="1"
             ConfigurableDirectory="INSTALLFOLDER"
             AllowAdvertise="no"
             Display="expand">
      
      <ComponentGroupRef Id="ApplicationFiles" />
      <ComponentRef Id="StartMenuShortcut" />
      
    </Feature>

    <Feature Id="DesktopShortcut"
             Title="Acceso directo en Escritorio"
             Description="Crear acceso directo en el Escritorio"
             Level="1"
             AllowAdvertise="no">
      
      <ComponentRef Id="DesktopShortcutComponent" />
      
    </Feature>
  </Fragment>

  <Fragment>
    <ComponentGroup Id="ApplicationFiles" Directory="INSTALLFOLDER">
'@

# Obtener todos los archivos y generar componentes
$files = Get-ChildItem -Path $appFolder -Recurse -File
$componentId = 1

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($appFolder.Length + 1).Replace('\', '/')
    $fileId = "File_" + $componentId
    $compId = "Comp_" + $componentId
    
    # Determinar si es el archivo principal ejecutable
    $isMainExe = $file.Name -eq "GestionTime.Desktop.exe"
    $keyPath = if ($isMainExe) { ' KeyPath="yes"' } else { '' }
    
    $featuresWxs += "`n      <Component Id=`"$compId`" Guid=`"*`">"
    $featuresWxs += "`n        <File Id=`"$fileId`" Source=`"`$(var.AppPath)/$relativePath`"$keyPath />"
    $featuresWxs += "`n      </Component>"
    
    $componentId++
}

$featuresWxs += @'

    </ComponentGroup>
  </Fragment>

  <Fragment>
    <Component Id="StartMenuShortcut" Directory="ApplicationProgramsFolder" Guid="*">
      <Shortcut Id="StartMenuShortcut"
                Name="GestionTime Desktop"
                Description="Aplicacion de gestion de tiempo y partes de trabajo"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="app_logo.ico" />
      <RemoveFolder Id="ApplicationProgramsFolder" On="uninstall" />
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime Solutions\GestionTime Desktop" 
                     Name="installed" 
                     Type="integer" 
                     Value="1" 
                     KeyPath="yes" />
    </Component>
  </Fragment>

  <Fragment>
    <Component Id="DesktopShortcutComponent" Directory="DesktopFolder" Guid="*">
      <Shortcut Id="DesktopShortcut"
                Name="GestionTime Desktop"
                Description="Aplicacion de gestion de tiempo y partes de trabajo"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="app_logo.ico" />
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime Solutions\GestionTime Desktop" 
                     Name="desktopShortcut" 
                     Type="integer" 
                     Value="1" 
                     KeyPath="yes" />
    </Component>
  </Fragment>

</Wix>
'@

$featuresWxs | Out-File -FilePath "Installer\MSI\Features.wxs" -Encoding UTF8 -Force

$totalComponents = $componentId - 1
Write-Host "   [OK] Componentes generados: $totalComponents archivos" -ForegroundColor Green
Write-Host ""

# ====================================================================
# 6. COMPILAR INSTALADOR MSI
# ====================================================================

Write-Host "6. Compilando instalador MSI..." -ForegroundColor Yellow

# Asegurar que exista el directorio de salida
New-Item -ItemType Directory -Path $msiOutputPath -Force | Out-Null

# Cambiar a directorio MSI
Push-Location "Installer\MSI"

try {
    Write-Host "   Ejecutando WiX build..." -ForegroundColor Gray
    
    # Usar wix directamente
    wix build GestionTimeDesktop.wixproj -arch x64 -o "..\..\$msiOutputPath\GestionTimeDesktop-$Version.msi" 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   [WARN] Intentando con dotnet build..." -ForegroundColor Yellow
        dotnet build GestionTimeDesktop.wixproj -c Release 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "   [ERROR] Error compilando instalador" -ForegroundColor Red
            Pop-Location
            exit 1
        }
    }
    
    Write-Host "   [OK] Instalador compilado exitosamente" -ForegroundColor Green
    
} finally {
    Pop-Location
}

Write-Host ""

# ====================================================================
# 7. VERIFICAR INSTALADOR GENERADO
# ====================================================================

Write-Host "7. Verificando instalador generado..." -ForegroundColor Yellow

$msiFiles = Get-ChildItem -Path $msiOutputPath -Filter "*.msi" -ErrorAction SilentlyContinue

if ($msiFiles) {
    $msiFile = $msiFiles | Select-Object -First 1
    $msiSize = [math]::Round($msiFile.Length / 1MB, 2)
    
    Write-Host "   [OK] Instalador MSI generado:" -ForegroundColor Green
    Write-Host "      Archivo: $($msiFile.Name)" -ForegroundColor Gray
    Write-Host "      Tamano: $msiSize MB" -ForegroundColor Gray
    Write-Host "      Ruta: $($msiFile.FullName)" -ForegroundColor Gray
} else {
    Write-Host "   [ERROR] No se encontro el archivo MSI generado" -ForegroundColor Red
    Write-Host "   Verifica los errores de compilacion arriba" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# ====================================================================
# 8. RESUMEN FINAL
# ====================================================================

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  [OK] INSTALADOR MSI CREADO EXITOSAMENTE  " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Informacion del instalador:" -ForegroundColor White
Write-Host "   * Producto: GestionTime Desktop" -ForegroundColor Gray
Write-Host "   * Version: $Version" -ForegroundColor Gray
Write-Host "   * Plataforma: x64" -ForegroundColor Gray
Write-Host "   * Tamano: $msiSize MB" -ForegroundColor Gray
Write-Host "   * Archivos incluidos: $totalComponents" -ForegroundColor Gray
Write-Host ""
Write-Host "Ubicacion:" -ForegroundColor White
Write-Host "   $($msiFile.FullName)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para instalar:" -ForegroundColor White
Write-Host "   msiexec /i `"$($msiFile.FullName)`"" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para instalar silenciosamente:" -ForegroundColor White
Write-Host "   msiexec /i `"$($msiFile.FullName)`" /quiet /norestart" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para desinstalar:" -ForegroundColor White
Write-Host "   msiexec /x `"$($msiFile.FullName)`"" -ForegroundColor Cyan
Write-Host ""

# Abrir carpeta del instalador si se solicito
if ($OpenAfter) {
    Write-Host "Abriendo carpeta del instalador..." -ForegroundColor Yellow
    Start-Process explorer.exe -ArgumentList "/select,`"$($msiFile.FullName)`""
}

Write-Host ""
Write-Host "[OK] Proceso completado exitosamente" -ForegroundColor Green
Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
