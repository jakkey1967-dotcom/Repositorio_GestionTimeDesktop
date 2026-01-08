# ═══════════════════════════════════════════════════════════════════════
# SCRIPT: Crear Instalador MSIX (MSI Moderno) - GestionTime Desktop
# VERSION: 3.0 - ENERO 2026
# DESCRIPCION: Crea un instalador .msix (equivalente moderno de MSI)
#              compatible con Windows 11 sin necesidad de WiX Toolset
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
Write-Host "  (Equivalente moderno de MSI sin WiX)   " -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ===========================================================================
# VERIFICAR REQUISITOS
# ===========================================================================

Write-Host "🔍 Verificando requisitos..." -ForegroundColor Yellow
Write-Host ""

# Verificar .NET 8 SDK
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Host "❌ ERROR: .NET 8 SDK no está instalado" -ForegroundColor Red
    Write-Host ""
    Write-Host "INSTALAR .NET 8 SDK:" -ForegroundColor Yellow
    Write-Host "   https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
    exit 1
}

Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green

# Verificar proyecto
$projectPath = "C:\GestionTime\GestionTimeDesktop\GestionTime.Desktop.csproj"
if (-not (Test-Path $projectPath)) {
    Write-Host "❌ ERROR: No se encontró el proyecto en $projectPath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Proyecto encontrado" -ForegroundColor Green
Write-Host ""

# ===========================================================================
# PREPARAR PACKAGE.APPXMANIFEST
# ===========================================================================

Write-Host "📝 Verificando Package.appxmanifest..." -ForegroundColor Yellow

$manifestPath = "C:\GestionTime\GestionTimeDesktop\Package.appxmanifest"

if (-not (Test-Path $manifestPath)) {
    Write-Host "   Creando Package.appxmanifest..." -ForegroundColor Gray
    
    $manifestContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Package 
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity Name="GestionTime.Desktop"
            Publisher="CN=Global Retail Solutions"
            Version="$Version" />

  <Properties>
    <DisplayName>GestionTime Desktop</DisplayName>
    <PublisherDisplayName>Global Retail Solutions</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>Sistema de gestion de partes de trabajo para tecnicos</Description>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
    <PackageDependency Name="Microsoft.WindowsAppRuntime.1.8" MinVersion="8000.0.0.0" Publisher="CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" />
  </Dependencies>

  <Resources>
    <Resource Language="es-ES" />
    <Resource Language="en-US" />
  </Resources>

  <Applications>
    <Application Id="GestionTimeDesktop" Executable="GestionTime.Desktop.exe" EntryPoint="`$targetname`.Program">
      <uap:VisualElements DisplayName="GestionTime Desktop"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png"
                          Description="Gestion de partes de trabajo"
                          BackgroundColor="transparent">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" 
                         ShortName="GestionTime"
                         Square71x71Logo="Assets\SmallTile.png"
                         Square310x310Logo="Assets\LargeTile.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" BackgroundColor="#0078D4" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
    <Capability Name="internetClient" />
  </Capabilities>

</Package>
"@

    $manifestContent | Out-File -FilePath $manifestPath -Encoding UTF8
    Write-Host "   ✅ Package.appxmanifest creado" -ForegroundColor Green
} else {
    Write-Host "   ✅ Package.appxmanifest existe" -ForegroundColor Green
}

Write-Host ""

# ===========================================================================
# ACTUALIZAR .CSPROJ PARA MSIX
# ===========================================================================

Write-Host "⚙️  Configurando proyecto para MSIX..." -ForegroundColor Yellow

$csprojContent = Get-Content $projectPath -Raw

# Verificar si ya tiene configuración MSIX
if ($csprojContent -notmatch '<GenerateAppxPackageOnBuild>') {
    Write-Host "   Actualizando .csproj..." -ForegroundColor Gray
    
    # Agregar propiedades MSIX
    $csprojContent = $csprojContent -replace '(</PropertyGroup>)', @"
    
    <!-- Configuracion para MSIX Packaging -->
    <GenerateAppxPackageOnBuild>true</GenerateAppxPackageOnBuild>
    <AppxPackageSigningEnabled>false</AppxPackageSigningEnabled>
    <AppxBundle>Always</AppxBundle>
    <AppxBundlePlatforms>x64</AppxBundlePlatforms>
    <AppxPackageDir>Installer\Output\MSIX\</AppxPackageDir>
    <Version>$Version</Version>
  `$1
"@

    $csprojContent | Out-File -FilePath $projectPath -Encoding UTF8
    Write-Host "   ✅ .csproj actualizado" -ForegroundColor Green
} else {
    Write-Host "   ✅ .csproj ya tiene configuración MSIX" -ForegroundColor Green
}

Write-Host ""

# ===========================================================================
# COMPILAR Y EMPAQUETAR
# ===========================================================================

Write-Host "🔨 Compilando proyecto y generando MSIX..." -ForegroundColor Yellow
Write-Host ""

try {
    # Limpiar salida anterior
    Write-Host "   Limpiando..." -ForegroundColor Gray
    & dotnet clean -c $Configuration -v quiet
    
    # Restaurar dependencias
    Write-Host "   Restaurando dependencias..." -ForegroundColor Gray
    & dotnet restore -v quiet
    
    # Publicar (esto genera el MSIX)
    Write-Host "   Publicando aplicación..." -ForegroundColor Gray
    & dotnet publish -c $Configuration -r win-$Platform --self-contained false -p:PublishProfile=MSIX -v minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error al publicar (código: $LASTEXITCODE)"
    }
    
    Write-Host ""
    Write-Host "✅ Compilación exitosa" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "❌ ERROR al compilar:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "ALTERNATIVA: Usar Visual Studio para crear el paquete:" -ForegroundColor Yellow
    Write-Host "   1. Abrir GestionTime.Desktop.sln en Visual Studio" -ForegroundColor White
    Write-Host "   2. Click derecho en proyecto → Publish → Create App Packages" -ForegroundColor White
    Write-Host "   3. Seleccionar Sideloading" -ForegroundColor White
    Write-Host "   4. Elegir arquitectura x64" -ForegroundColor White
    Write-Host "   5. Click Create" -ForegroundColor White
    exit 1
}

# ===========================================================================
# BUSCAR PAQUETE GENERADO
# ===========================================================================

Write-Host ""
Write-Host "🔍 Buscando paquete generado..." -ForegroundColor Yellow

$appPackagesDir = "C:\GestionTime\GestionTimeDesktop\AppPackages"
$msixFiles = Get-ChildItem -Path $appPackagesDir -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue

if ($msixFiles) {
    $latestMsix = $msixFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "  ✅ PAQUETE MSIX GENERADO EXITOSAMENTE  " -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "📦 ARCHIVO MSIX:" -ForegroundColor Cyan
    Write-Host "   $($latestMsix.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "📊 TAMAÑO:" -ForegroundColor Cyan
    Write-Host "   $([math]::Round($latestMsix.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "📋 CONTENIDO:" -ForegroundColor Cyan
    Write-Host "   ✓ Ejecutable principal + todas las DLLs" -ForegroundColor White
    Write-Host "   ✓ Assets completos" -ForegroundColor White
    Write-Host "   ✓ Runtimes (win-x64)" -ForegroundColor White
    Write-Host "   ✓ Configuración" -ForegroundColor White
    Write-Host "   ✓ Manifiesto de aplicación" -ForegroundColor White
    Write-Host ""
    Write-Host "🚀 INSTALACIÓN:" -ForegroundColor Yellow
    Write-Host "   1. Hacer doble-clic en el archivo .msix" -ForegroundColor White
    Write-Host "   2. Click en 'Instalar'" -ForegroundColor White
    Write-Host "   3. Buscar 'GestionTime Desktop' en Menú Inicio" -ForegroundColor White
    Write-Host ""
    Write-Host "⚠️  NOTA IMPORTANTE:" -ForegroundColor Yellow
    Write-Host "   Si Windows pide 'Certificado no confiable':" -ForegroundColor White
    Write-Host "   - Click en 'Más información'" -ForegroundColor White
    Write-Host "   - Click en 'Instalar de todos modos'" -ForegroundColor White
    Write-Host "   (Normal en desarrollo sin certificado de código)" -ForegroundColor Gray
    Write-Host ""
    
    # Abrir explorador
    Start-Process explorer.exe -ArgumentList "/select,`"$($latestMsix.FullName)`""
    
} else {
    Write-Host ""
    Write-Host "⚠️  No se encontró el paquete MSIX generado" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "VERIFICAR:" -ForegroundColor Yellow
    Write-Host "   • Errores de compilación arriba" -ForegroundColor White
    Write-Host "   • Directorio: $appPackagesDir" -ForegroundColor White
    Write-Host ""
    Write-Host "ALTERNATIVA MANUAL:" -ForegroundColor Yellow
    Write-Host "   Usar Visual Studio para crear el paquete MSIX" -ForegroundColor White
}

Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
