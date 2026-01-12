# ═══════════════════════════════════════════════════════════════════════
# SCRIPT: Crear Instalador MSIX (MSI Moderno) - GestionTime Desktop
# VERSION: 4.0 - ENERO 2026 - SIMPLIFICADO Y FUNCIONAL
# DESCRIPCION: Crea un instalador .msix usando MSBuild directamente
# ═══════════════════════════════════════════════════════════════════════

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$Version = "1.2.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  INSTALADOR MSIX - GESTIONTIME DESKTOP  " -ForegroundColor Cyan
Write-Host "  Método Simplificado con MSBuild        " -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ===========================================================================
# VERIFICAR REQUISITOS
# ===========================================================================

Write-Host "🔍 Verificando requisitos..." -ForegroundColor Yellow
Write-Host ""

# Verificar proyecto
$projectDir = "C:\GestionTime\GestionTimeDesktop"
$projectPath = Join-Path $projectDir "GestionTime.Desktop.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Host "❌ ERROR: No se encontró el proyecto en $projectPath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Proyecto encontrado" -ForegroundColor Green

# Buscar MSBuild de Visual Studio
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    if ($msbuildPath) {
        Write-Host "✅ MSBuild encontrado: $msbuildPath" -ForegroundColor Green
    }
} else {
    Write-Host "⚠️  vswhere no encontrado, intentando con dotnet..." -ForegroundColor Yellow
    $msbuildPath = $null
}

Write-Host ""

# =========================================================================
# MÉTODO 1: PUBLICAR CON DOTNET (MÁS SIMPLE)
# ===========================================================================

Write-Host "🔨 Método 1: Publicando con dotnet..." -ForegroundColor Yellow
Write-Host ""

try {
    Set-Location $projectDir
    
    # Limpiar salida anterior
    Write-Host "   Limpiando salida anterior..." -ForegroundColor Gray
    if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue }
    
    # Restaurar dependencias
    Write-Host "   Restaurando dependencias..." -ForegroundColor Gray
    & dotnet restore -v quiet
    
    # Publicar aplicación self-contained
    Write-Host "   Publicando aplicación..." -ForegroundColor Gray
    $publishPath = Join-Path $projectDir "bin\$Configuration\net8.0-windows10.0.19041.0\win-x64\publish"
    
    & dotnet publish -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -v minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error al publicar (código: $LASTEXITCODE)"
    }
    
    Write-Host ""
    Write-Host "✅ Publicación exitosa" -ForegroundColor Green
    Write-Host "   Ubicación: $publishPath" -ForegroundColor Gray
    
} catch {
    Write-Host ""
    Write-Host "❌ ERROR al publicar:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Intentando método alternativo con Visual Studio..." -ForegroundColor Yellow
    Write-Host ""
}

# =========================================================================
# MÉTODO 2: CREAR PAQUETE MSIX USANDO MAKEAPPX
# ===========================================================================

Write-Host ""
Write-Host "🔨 Método 2: Creando paquete MSIX..." -ForegroundColor Yellow
Write-Host ""

# Buscar Windows SDK
$sdkPaths = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64"
)

$makeappxPath = $null
foreach ($sdkPath in $sdkPaths) {
    $testPath = Join-Path $sdkPath "makeappx.exe"
    if (Test-Path $testPath) {
        $makeappxPath = $testPath
        break
    }
}

if (-not $makeappxPath) {
    Write-Host "⚠️  makeappx.exe no encontrado" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host "  USAR VISUAL STUDIO PARA CREAR MSIX  " -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "PASOS:" -ForegroundColor Cyan
    Write-Host "   1. Abrir Visual Studio 2022" -ForegroundColor White
    Write-Host "   2. Abrir solución: GestionTime.Desktop.sln" -ForegroundColor White
    Write-Host "   3. Click derecho en proyecto 'GestionTime.Desktop'" -ForegroundColor White
    Write-Host "   4. Seleccionar: Publish > Create App Packages" -ForegroundColor White
    Write-Host "   5. Elegir: Sideloading" -ForegroundColor White
    Write-Host "   6. Marcar: x64" -ForegroundColor White
    Write-Host "   7. Versión: $Version" -ForegroundColor White
    Write-Host "   8. Click: Create" -ForegroundColor White
    Write-Host ""
    Write-Host "El paquete MSIX se generará en:" -ForegroundColor Cyan
    Write-Host "   AppPackages\GestionTime.Desktop_$Version" + "_x64_Test\" -ForegroundColor White
    Write-Host ""
    
    # Abrir Visual Studio si está instalado
    $vsPath = & $vswhere -latest -property productPath 2>$null
    if ($vsPath -and (Test-Path $vsPath)) {
        Write-Host "¿Abrir Visual Studio ahora? (S/N): " -ForegroundColor Yellow -NoNewline
        $response = Read-Host
        if ($response -eq "S" -or $response -eq "s") {
            Write-Host ""
            Write-Host "Abriendo Visual Studio..." -ForegroundColor Green
            Start-Process $vsPath -ArgumentList "`"$projectDir\GestionTime.Desktop.sln`""
        }
    }
    
    Write-Host ""
    Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 0
}

# Si tenemos makeappx, crear el paquete manualmente
Write-Host "✅ makeappx.exe encontrado" -ForegroundColor Green
Write-Host ""

# Preparar directorio de staging
$stagingDir = Join-Path $projectDir "Installer\Staging"
$outputDir = Join-Path $projectDir "Installer\Output"

if (Test-Path $stagingDir) { Remove-Item -Path $stagingDir -Recurse -Force }
if (-not (Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir | Out-Null }

New-Item -ItemType Directory -Path $stagingDir | Out-Null

# Copiar archivos publicados
Write-Host "   Copiando archivos..." -ForegroundColor Gray
Copy-Item -Path "$publishPath\*" -Destination $stagingDir -Recurse -Force

# Crear AppxManifest.xml básico
$manifestContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
         IgnorableNamespaces="uap rescap">
  <Identity Name="GestionTime.Desktop" Publisher="CN=GestionTime" Version="$Version" />
  <Properties>
    <DisplayName>GestionTime Desktop</DisplayName>
    <PublisherDisplayName>GestionTime Solutions</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>
  <Resources>
    <Resource Language="es-ES" />
  </Resources>
  <Applications>
    <Application Id="GestionTimeDesktop" Executable="GestionTime.Desktop.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="GestionTime Desktop"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png"
                          Description="Gestión de partes de trabajo"
                          BackgroundColor="transparent">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
    <Capability Name="internetClient" />
  </Capabilities>
</Package>
"@

$manifestPath = Join-Path $stagingDir "AppxManifest.xml"
$manifestContent | Out-File -FilePath $manifestPath -Encoding UTF8

Write-Host "   Creando paquete MSIX..." -ForegroundColor Gray
$msixPath = Join-Path $outputDir "GestionTime-Desktop-$Version.msix"

& $makeappxPath pack /d $stagingDir /p $msixPath /o

if ($LASTEXITCODE -eq 0 -and (Test-Path $msixPath)) {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "  ✅ PAQUETE MSIX GENERADO EXITOSAMENTE  " -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "📦 ARCHIVO:" -ForegroundColor Cyan
    Write-Host "   $msixPath" -ForegroundColor White
    Write-Host ""
    Write-Host "📊 TAMAÑO:" -ForegroundColor Cyan
    Write-Host "   $([math]::Round((Get-Item $msixPath).Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "🚀 INSTALACIÓN:" -ForegroundColor Yellow
    Write-Host "   1. Doble-clic en el archivo .msix" -ForegroundColor White
    Write-Host "   2. Click 'Instalar'" -ForegroundColor White
    Write-Host "   3. Buscar 'GestionTime' en Menú Inicio" -ForegroundColor White
    Write-Host ""
    
    # Abrir explorador
    Start-Process explorer.exe -ArgumentList "/select,`"$msixPath`""
    
} else {
    Write-Host "❌ Error creando paquete MSIX" -ForegroundColor Red
    Write-Host ""
    Write-Host "Usa Visual Studio (ver instrucciones arriba)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
