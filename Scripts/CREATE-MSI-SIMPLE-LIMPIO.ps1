# ===========================================================================
# CREAR MSI DESDE CERO - GESTIONTIME DESKTOP
# VERSION: 1.0 LIMPIA - ENERO 2026
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CREAR MSI DESDE CERO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Rutas
$wixExe = "C:\Program Files\WiX Toolset v6.0\bin\wix.exe"
$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$outputDir = "$projectDir\Installer\Output"
$msiPath = "$outputDir\GestionTime-Desktop-Setup.msi"

# Verificar WiX
if (-not (Test-Path $wixExe)) {
    Write-Host "ERROR: WiX Toolset v6.0 no encontrado" -ForegroundColor Red
    exit 1
}

# Verificar ejecutable
if (-not (Test-Path "$binDir\GestionTime.Desktop.exe")) {
    Write-Host "ERROR: Ejecutable no encontrado. Compilar con: dotnet build" -ForegroundColor Red
    exit 1
}

# Crear directorio de salida
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "[1/3] Copiando window-config.ini personalizado..." -ForegroundColor Yellow
$customConfig = "$projectDir\Installer\window-config.ini"
if (Test-Path $customConfig) {
    Copy-Item -Path $customConfig -Destination "$binDir\window-config.ini" -Force
    Write-Host "   ✓ Copiado" -ForegroundColor Green
} else {
    Write-Host "   ⚠ No encontrado, se usará el existente" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[2/3] Generando archivo WiX..." -ForegroundColor Yellow

# Crear WXS simple
$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui">
  
  <Package Name="GestionTime Desktop"
           Version="1.2.0.0"
           Manufacturer="Global Retail Solutions"
           UpgradeCode="F1E2D3C4-B5A6-9780-ABCD-123456789012"
           Language="1034">

    <MajorUpgrade DowngradeErrorMessage="Ya existe una version mas reciente." />
    <MediaTemplate EmbedCab="yes" />

    <Icon Id="AppIcon" SourceFile="$projectDir\Assets\app_logo.ico" />
    <Property Id="ARPPRODUCTICON" Value="AppIcon" />

    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLFOLDER" Name="GestionTime Desktop" />
    </StandardDirectory>

    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ProgramMenuDir" Name="GestionTime Desktop" />
    </StandardDirectory>

    <Feature Id="Main" Title="GestionTime Desktop" Level="1">
      <ComponentGroupRef Id="AppFiles" />
      <ComponentRef Id="MenuShortcut" />
    </Feature>

    <ui:WixUI Id="WixUI_InstallDir" InstallDirectory="INSTALLFOLDER" />
    <WixVariable Id="WixUILicenseRtf" Value="$projectDir\Installer\MSI\License.rtf" />

  </Package>

  <Fragment>
    <ComponentGroup Id="AppFiles" Directory="INSTALLFOLDER">
      <Component>
        <Files Include="$binDir\**" />
      </Component>
    </ComponentGroup>
  </Fragment>

  <Fragment>
    <Component Id="MenuShortcut" Directory="ProgramMenuDir">
      <Shortcut Name="GestionTime Desktop"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="AppIcon" />
      <RemoveFolder Id="RemoveMenu" On="uninstall" />
      <RegistryValue Root="HKCU" Key="Software\GestionTime" Name="Installed" Value="1" Type="integer" KeyPath="yes" />
    </Component>
  </Fragment>

</Wix>
"@

$wxsFile = Join-Path $env:TEMP "GestionTime-Simple.wxs"
$wxsContent | Out-File -FilePath $wxsFile -Encoding UTF8
Write-Host "   ✓ Generado" -ForegroundColor Green

Write-Host ""
Write-Host "[3/3] Compilando MSI..." -ForegroundColor Yellow
Write-Host "   (Esto puede tardar 1-2 minutos...)" -ForegroundColor Gray

try {
    & $wixExe build $wxsFile -arch x64 -out $msiPath -bindpath $binDir -ext WixToolset.UI.wixext 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error en compilacion"
    }
    
    if (Test-Path $msiPath) {
        $msiFile = Get-Item $msiPath
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  MSI CREADO EXITOSAMENTE" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "ARCHIVO: $($msiFile.FullName)" -ForegroundColor Cyan
        Write-Host "TAMAÑO: $([math]::Round($msiFile.Length / 1MB, 2)) MB" -ForegroundColor Cyan
        Write-Host ""
        Start-Process explorer.exe -ArgumentList "/select,`"$($msiFile.FullName)`""
    } else {
        throw "MSI no se creo"
    }
    
} catch {
    Write-Host ""
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Intentando con metodo alternativo..." -ForegroundColor Yellow
    
    # Intentar sin Files Include (WiX v6 puede no soportarlo)
    Write-Host "Generando componentes manualmente..." -ForegroundColor Cyan
    
    # Este script alternativo se ejecutará en el siguiente paso
    exit 1
}

Remove-Item $wxsFile -Force -ErrorAction SilentlyContinue
Write-Host "Completado!" -ForegroundColor Green
Write-Host ""
