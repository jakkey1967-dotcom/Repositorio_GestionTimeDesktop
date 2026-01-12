# ===========================================================================
# CREAR MSI LIMPIO Y FUNCIONAL - GESTIONTIME DESKTOP
# GENERA COMPONENTES MANUALMENTE PARA TODAS LAS CARPETAS
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  MSI LIMPIO CON TODAS LAS CARPETAS" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$wixExe = "C:\Program Files\WiX Toolset v6.0\bin\wix.exe"
$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$outputDir = "$projectDir\Installer\Output"
$msiPath = "$outputDir\GestionTime-Desktop-Setup.msi"

# PASO 1: Copiar window-config.ini
Write-Host "[1/4] Preparando archivos..." -ForegroundColor Yellow
$customConfig = "$projectDir\Installer\window-config.ini"
if (Test-Path $customConfig) {
    Copy-Item -Path $customConfig -Destination "$binDir\window-config.ini" -Force
    Write-Host "   ✓ window-config.ini copiado" -ForegroundColor Green
}

# PASO 2: Obtener todos los archivos
Write-Host "[2/4] Escaneando archivos..." -ForegroundColor Yellow
$allFiles = Get-ChildItem -Path $binDir -File -Recurse
Write-Host "   Archivos: $($allFiles.Count)" -ForegroundColor Green

# PASO 3: Generar XML
Write-Host "[3/4] Generando XML de WiX..." -ForegroundColor Yellow

$componentsXml = ""
$fileId = 1

foreach ($file in $allFiles) {
    $relPath = $file.FullName.Replace("$binDir\", "").Replace("$binDir", "")
    $guid = [System.Guid]::NewGuid().ToString().ToUpper()
    
    $neverOverwrite = if ($file.Name -eq "window-config.ini") { " NeverOverwrite=`"yes`"" } else { "" }
    
    $componentsXml += @"
      <Component Directory="INSTALLFOLDER" Guid="$guid">
        <File Source="$($file.FullName)"$neverOverwrite />
      </Component>

"@
    $fileId++
}

$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs" xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui">
  <Package Name="GestionTime Desktop" Version="1.2.0.0" Manufacturer="Global Retail Solutions" UpgradeCode="F1E2D3C4-B5A6-9780-ABCD-123456789012" Language="1034">
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
      <ComponentGroupRef Id="AllFiles" />
      <ComponentRef Id="MenuShortcut" />
    </Feature>
    
    <ui:WixUI Id="WixUI_InstallDir" InstallDirectory="INSTALLFOLDER" />
    <WixVariable Id="WixUILicenseRtf" Value="$projectDir\Installer\MSI\License.rtf" />
  </Package>
  
  <Fragment>
    <ComponentGroup Id="AllFiles" Directory="INSTALLFOLDER">
$componentsXml
    </ComponentGroup>
  </Fragment>
  
  <Fragment>
    <Component Id="MenuShortcut" Directory="ProgramMenuDir">
      <Shortcut Name="GestionTime Desktop" Target="[INSTALLFOLDER]GestionTime.Desktop.exe" WorkingDirectory="INSTALLFOLDER" Icon="AppIcon" />
      <RemoveFolder Id="RemoveMenu" On="uninstall" />
      <RegistryValue Root="HKCU" Key="Software\GestionTime" Name="Installed" Value="1" Type="integer" KeyPath="yes" />
    </Component>
  </Fragment>
</Wix>
"@

$wxsFile = Join-Path $env:TEMP "GestionTime-Clean.wxs"
$wxsContent | Out-File -FilePath $wxsFile -Encoding UTF8
Write-Host "   ✓ XML generado ($fileId archivos)" -ForegroundColor Green

# PASO 4: Compilar
Write-Host "[4/4] Compilando MSI..." -ForegroundColor Yellow
if (-not (Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir -Force | Out-Null }

try {
    & $wixExe build $wxsFile -arch x64 -out $msiPath -bindpath $binDir -ext WixToolset.UI.wixext -nologo
    
    if ($LASTEXITCODE -eq 0 -and (Test-Path $msiPath)) {
        $msi = Get-Item $msiPath
        Write-Host ""
        Write-Host "==========================================" -ForegroundColor Green
        Write-Host "  ✓ MSI CREADO CORRECTAMENTE" -ForegroundColor Green
        Write-Host "==========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Archivo: $($msi.FullName)" -ForegroundColor Cyan
        Write-Host "Tamaño: $([math]::Round($msi.Length / 1MB, 2)) MB" -ForegroundColor Cyan
        Write-Host "Archivos: $($allFiles.Count)" -ForegroundColor Cyan
        Write-Host ""
        Start-Process explorer.exe -ArgumentList "/select,`"$($msi.FullName)`""
    } else {
        throw "MSI no creado"
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    Remove-Item $wxsFile -Force -ErrorAction SilentlyContinue
}

Write-Host "Completado!" -ForegroundColor Green
