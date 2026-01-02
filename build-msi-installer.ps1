# ========================================
# 📦 Script de Compilación con Instalador MSI
# GestionTime Desktop - Versión 1.0.0
# WiX Toolset v4 - VERSIÓN SIMPLIFICADA
# ========================================

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📦 GESTIONTIME DESKTOP - BUILD MSI INSTALLER" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Configuración
$version = "1.0.0"
$manufacturer = "Global Retail Group"
$productName = "GestionTime Desktop"
$outputFolder = "publish"
$msiName = "GestionTime-Setup-v$version.msi"

# Verificar WiX Toolset
Write-Host "🔍 Verificando WiX Toolset..." -ForegroundColor Yellow
try {
    $wixVersion = & wix --version 2>&1
    Write-Host "✅ WiX Toolset encontrado: $wixVersion" -ForegroundColor Green
}
catch {
    Write-Host "❌ ERROR: WiX Toolset no está instalado" -ForegroundColor Red
    Write-Host "" -ForegroundColor Yellow
    Write-Host "Para instalar WiX Toolset v4:" -ForegroundColor Yellow
    Write-Host "   dotnet tool install --global wix --version 4.0.5" -ForegroundColor White
    Write-Host "" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Limpiar builds anteriores
Write-Host "🧹 Limpiando builds anteriores..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path $outputFolder) {
    Remove-Item -Path $outputFolder -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path $msiName) {
    Remove-Item -Path $msiName -Force -ErrorAction SilentlyContinue
}
if (Test-Path "*.wixobj") {
    Remove-Item -Path "*.wixobj" -Force -ErrorAction SilentlyContinue
}
if (Test-Path "*.wixpdb") {
    Remove-Item -Path "*.wixpdb" -Force -ErrorAction SilentlyContinue
}
Write-Host "✅ Limpieza completada" -ForegroundColor Green
Write-Host ""

# Crear carpeta de salida
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null

# Verificar appsettings.json
Write-Host "🔍 Verificando configuración..." -ForegroundColor Yellow
if (-not (Test-Path "appsettings.json")) {
    Write-Host "❌ ERROR: No se encuentra appsettings.json" -ForegroundColor Red
    exit 1
}

$config = Get-Content "appsettings.json" -Raw | ConvertFrom-Json
Write-Host "📋 Configuración:" -ForegroundColor Cyan
Write-Host "   • API URL: $($config.Api.BaseUrl)" -ForegroundColor White
Write-Host "   • Login Path: $($config.Api.LoginPath)" -ForegroundColor White
Write-Host ""

# Compilar aplicación
Write-Host "🔨 Compilando aplicación..." -ForegroundColor Yellow
Write-Host "   • Configuración: Release" -ForegroundColor White
Write-Host "   • Plataforma: win-x64" -ForegroundColor White
Write-Host "   • Modo: Self-contained (.NET 8 incluido)" -ForegroundColor White
Write-Host ""

$publishArgs = @(
    "publish",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "/p:PublishSingleFile=false",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:PublishTrimmed=false",
    "/p:PublishReadyToRun=true"
)

$buildProcess = Start-Process -FilePath "dotnet" -ArgumentList $publishArgs -NoNewWindow -Wait -PassThru

if ($buildProcess.ExitCode -ne 0) {
    Write-Host "❌ ERROR: La compilación falló" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Compilación exitosa" -ForegroundColor Green
Write-Host ""

# Localizar carpeta de publicación
$publishPath = Get-ChildItem -Path "bin\Release" -Recurse -Directory -Filter "publish" | Select-Object -First 1

if (-not $publishPath) {
    Write-Host "❌ ERROR: No se encontró la carpeta de publicación" -ForegroundColor Red
    exit 1
}

Write-Host "📂 Carpeta de publicación: $($publishPath.FullName)" -ForegroundColor Cyan
Write-Host ""

# Copiar archivos
Write-Host "📦 Copiando archivos..." -ForegroundColor Yellow
Copy-Item -Path "$($publishPath.FullName)\*" -Destination $outputFolder -Recurse -Force

# Calcular tamaño
$totalSize = (Get-ChildItem -Path $outputFolder -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "✅ $("{0:N2}" -f $totalSize) MB copiados" -ForegroundColor Green
Write-Host ""

# Generar GUID para el producto (consistente para upgrades)
$upgradeGuid = "ABCDEF01-2345-6789-ABCD-EF0123456789"

# Usar heat.exe para generar automáticamente los componentes
Write-Host "📝 Generando componentes con heat.exe..." -ForegroundColor Yellow
$heatArgs = @(
    "harvest", "dir", $outputFolder,
    "-cg", "ProductComponents",
    "-dr", "INSTALLFOLDER",
    "-srd",
    "-sfrag",
    "-gg",
    "-out", "components.wxs"
)

$heatProcess = Start-Process -FilePath "wix" -ArgumentList $heatArgs -NoNewWindow -Wait -PassThru

if ($heatProcess.ExitCode -ne 0) {
    Write-Host "❌ ERROR: Heat falló generando componentes" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Componentes generados" -ForegroundColor Green
Write-Host ""

# Crear archivo WiX principal simplificado
Write-Host "📝 Generando archivo WiX principal..." -ForegroundColor Yellow

$wixContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package Name="$productName" 
           Version="$version" 
           Manufacturer="$manufacturer"
           UpgradeCode="$upgradeGuid"
           Language="1033">
    
    <MajorUpgrade DowngradeErrorMessage="Una versión más nueva ya está instalada." />
    
    <MediaTemplate EmbedCab="yes" />
    
    <Feature Id="ProductFeature" Title="$productName" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
      <ComponentRef Id="ApplicationShortcut" />
    </Feature>
  </Package>

  <Fragment>
    <StandardDirectory Id="ProgramFiles6432Folder">
      <Directory Id="INSTALLFOLDER" Name="GestionTime" />
    </StandardDirectory>
    
    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ApplicationProgramsFolder" Name="$productName"/>
    </StandardDirectory>
  </Fragment>

  <Fragment>
    <Component Id="ApplicationShortcut" Directory="ApplicationProgramsFolder" Guid="66666666-6666-6666-6666-666666666666">
      <Shortcut Id="ApplicationStartMenuShortcut"
                Name="$productName"
                Description="Gestor de partes de trabajo"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER" />
      <RemoveFolder Id="CleanUpShortCut" Directory="ApplicationProgramsFolder" On="uninstall"/>
      <RegistryValue Root="HKCU" Key="Software\$manufacturer\$productName" Name="installed" Type="integer" Value="1" KeyPath="yes"/>
    </Component>
  </Fragment>
  
  <Fragment>
    <Icon Id="ProductIcon.exe" SourceFile="$outputFolder\GestionTime.Desktop.exe" />
    <Property Id="ARPPRODUCTICON" Value="ProductIcon.exe" />
  </Fragment>
</Wix>
"@

$wixFile = "GestionTime.wxs"
$wixContent | Out-File -FilePath $wixFile -Encoding UTF8
Write-Host "✅ Archivo WiX generado: $wixFile" -ForegroundColor Green
Write-Host ""

# Compilar MSI con WiX (sin extensiones de UI)
Write-Host "🔨 Compilando instalador MSI..." -ForegroundColor Yellow
Write-Host "   Este proceso puede tardar varios minutos..." -ForegroundColor Gray
Write-Host ""

try {
    # Compilar sin UI extension
    $wixArgs = @(
        "build",
        $wixFile,
        "components.wxs",
        "-out", $msiName,
        "-arch", "x64"
    )
    
    $wixProcess = Start-Process -FilePath "wix" -ArgumentList $wixArgs -NoNewWindow -Wait -PassThru
    
    if ($wixProcess.ExitCode -ne 0) {
        Write-Host "❌ ERROR: La compilación de WiX falló (código: $($wixProcess.ExitCode))" -ForegroundColor Red
        Write-Host "" -ForegroundColor Yellow
        Write-Host "Revisando archivos WiX generados..." -ForegroundColor Yellow
        if (Test-Path "GestionTime.wxs") {
            Write-Host "✅ GestionTime.wxs existe" -ForegroundColor Green
        }
        if (Test-Path "components.wxs") {
            Write-Host "✅ components.wxs existe" -ForegroundColor Green
        }
        Write-Host "" -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host "❌ ERROR ejecutando WiX: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Verificar que se creó el MSI
if (-not (Test-Path $msiName)) {
    Write-Host "❌ ERROR: El archivo MSI no se creó" -ForegroundColor Red
    exit 1
}

$msiSize = (Get-Item $msiName).Length / 1MB
Write-Host "✅ Instalador MSI creado exitosamente" -ForegroundColor Green
Write-Host ""

# Limpiar archivos temporales
Write-Host "🧹 Limpiando archivos temporales..." -ForegroundColor Yellow
Remove-Item -Path "*.wixobj" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "*.wixpdb" -Force -ErrorAction SilentlyContinue
Remove-Item -Path $wixFile -Force -ErrorAction SilentlyContinue
Remove-Item -Path "components.wxs" -Force -ErrorAction SilentlyContinue
Write-Host "✅ Limpieza completada" -ForegroundColor Green
Write-Host ""

# Resumen final
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ BUILD MSI COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📦 INSTALADOR CREADO:" -ForegroundColor Yellow
Write-Host "   • Archivo: $msiName" -ForegroundColor White
Write-Host "   • Tamaño: $("{0:N2}" -f $msiSize) MB" -ForegroundColor White
Write-Host "   • Versión: $version" -ForegroundColor White
Write-Host ""
Write-Host "✨ CARACTERÍSTICAS:" -ForegroundColor Yellow
Write-Host "   ✅ Instalación con Windows Installer" -ForegroundColor White
Write-Host "   ✅ Acceso directo en Menú Inicio" -ForegroundColor White
Write-Host "   ✅ Desinstalación desde Panel de Control" -ForegroundColor White
Write-Host "   ✅ Actualizaciones automáticas (upgrade)" -ForegroundColor White
Write-Host "   ✅ .NET 8 Runtime incluido" -ForegroundColor White
Write-Host ""
Write-Host "🚀 INSTALACIÓN:" -ForegroundColor Yellow
Write-Host "   1. Doble clic en $msiName" -ForegroundColor White
Write-Host "   2. Seguir instalación (UI básica de Windows)" -ForegroundColor White
Write-Host "   3. La aplicación se instalará en:" -ForegroundColor White
Write-Host "      C:\Program Files\GestionTime\" -ForegroundColor Gray
Write-Host ""
Write-Host "📝 DESINSTALACIÓN:" -ForegroundColor Yellow
Write-Host "   • Panel de Control → Programas → Desinstalar" -ForegroundColor White
Write-Host "   • O: msiexec /x $msiName" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ LISTO PARA DISTRIBUCIÓN" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
