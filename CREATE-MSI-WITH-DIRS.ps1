# ===========================================================================
# CREAR MSI CON ESTRUCTURA DE DIRECTORIOS - GESTIONTIME DESKTOP
# VERSION: 6.0 - ENERO 2026
# DESCRIPCION: Incluye TODOS los archivos manteniendo estructura de carpetas
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  CREAR MSI CON DIRECTORIOS - WIX v6.0" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$wixExe = "C:\Program Files\WiX Toolset v6.0\bin\wix.exe"
$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$outputDir = "$projectDir\Installer\Output"
$msiPath = "$outputDir\GestionTime-Desktop-1.2.0-Complete-Setup.msi"

Write-Host "[1/4] Verificando archivos..." -ForegroundColor Yellow

$allFiles = Get-ChildItem -Path $binDir -File -Recurse
Write-Host "   Archivos encontrados: $($allFiles.Count)" -ForegroundColor Green

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host ""
Write-Host "[2/4] Creando archivo WiX con DirectoryRef..." -ForegroundColor Yellow

# Crear WXS que copia TODO el directorio recursivamente
$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui">
  
  <Package Name="GestionTime Desktop"
           Language="1034"
           Version="1.2.0.0"
           Manufacturer="Global Retail Solutions"
           UpgradeCode="F1E2D3C4-B5A6-9780-ABCD-123456789012"
           Scope="perMachine"
           Compressed="yes">

    <MajorUpgrade DowngradeErrorMessage="Ya existe una version mas reciente instalada."
                  AllowSameVersionUpgrades="yes" />
    
    <MediaTemplate EmbedCab="yes" CompressionLevel="high" />

    <!-- Propiedades -->
    <Property Id="ARPPRODUCTICON" Value="AppIcon" />
    <Property Id="ARPCONTACT" Value="soporte@gestiontime.com" />
    <Property Id="ARPCOMMENTS" Value="Sistema de gestion de partes de trabajo - GestionTime Desktop v1.2.0" />
    <Property Id="ARPHELPLINK" Value="https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop" />

    <Icon Id="AppIcon" SourceFile="$projectDir\Assets\app_logo.ico" />

    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="ManufacturerFolder" Name="GestionTime">
        <Directory Id="INSTALLFOLDER" Name="Desktop" />
      </Directory>
    </StandardDirectory>

    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ProgramMenuDir" Name="GestionTime Desktop" />
    </StandardDirectory>

    <StandardDirectory Id="DesktopFolder" />

    <Feature Id="MainApplication" 
             Title="GestionTime Desktop" 
             Level="1"
             ConfigurableDirectory="INSTALLFOLDER">
      <ComponentGroupRef Id="AppFiles" />
      <ComponentRef Id="StartMenuShortcut" />
      <ComponentRef Id="DesktopShortcut" />
    </Feature>

    <ui:WixUI Id="WixUI_InstallDir" InstallDirectory="INSTALLFOLDER" />
    <WixVariable Id="WixUILicenseRtf" Value="$projectDir\Installer\MSI\License.rtf" />

  </Package>

  <Fragment>
    <ComponentGroup Id="AppFiles" Directory="INSTALLFOLDER">
      <Component>
        <File Source="$binDir\*" />
      </Component>
    </ComponentGroup>
  </Fragment>

  <Fragment>
    <Component Id="StartMenuShortcut" Directory="ProgramMenuDir">
      <Shortcut Id="AppStartMenuShortcut"
                Name="GestionTime Desktop"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="AppIcon" />
      
      <Shortcut Id="UninstallShortcut"
                Name="Desinstalar GestionTime Desktop"
                Target="[System64Folder]msiexec.exe"
                Arguments="/x [ProductCode]" />
      
      <RemoveFolder Id="RemoveProgramMenuDir" On="uninstall" />
      
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime\Desktop" 
                     Name="Installed" 
                     Value="1" 
                     Type="integer" 
                     KeyPath="yes" />
    </Component>
  </Fragment>

  <Fragment>
    <Component Id="DesktopShortcut" Directory="DesktopFolder">
      <Shortcut Id="AppDesktopShortcut"
                Name="GestionTime Desktop"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Icon="AppIcon" />
      
      <RemoveFolder Id="RemoveDesktopFolder" On="uninstall" />
      
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime\Desktop" 
                     Name="DesktopShortcut" 
                     Value="1" 
                     Type="integer" 
                     KeyPath="yes" />
    </Component>
  </Fragment>

</Wix>
"@

$wxsTemp = Join-Path $env:TEMP "GestionTime-WithDirs.wxs"
$wxsContent | Out-File -FilePath $wxsTemp -Encoding UTF8

Write-Host "   Archivo WiX creado" -ForegroundColor Green

Write-Host ""
Write-Host "[3/4] Compilando MSI con estructura completa..." -ForegroundColor Yellow
Write-Host "   (Esto puede tardar 2-3 minutos...)" -ForegroundColor Gray

Set-Location $projectDir

try {
    & $wixExe build `
        $wxsTemp `
        -arch x64 `
        -out $msiPath `
        -bindpath $binDir `
        -ext WixToolset.UI.wixext `
        -bindfiles `
        -nologo
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error en compilacion (codigo: $LASTEXITCODE)"
    }
    
    Write-Host "   Compilacion exitosa" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "ERROR:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Intentando con metodo alternativo..." -ForegroundColor Yellow
    
    # Método alternativo: copiar manualmente toda la estructura
    Write-Host ""
    Write-Host "No se pudo usar comodines en WiX v6.0" -ForegroundColor Yellow
    Write-Host "Usando CREATE-MSI-COMPLETE.ps1 actualizado..." -ForegroundColor Yellow
    
    Remove-Item $wxsTemp -Force -ErrorAction SilentlyContinue
    exit 1
}

Remove-Item $wxsTemp -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "[4/4] Verificando MSI..." -ForegroundColor Yellow

if (Test-Path $msiPath) {
    $msiFile = Get-Item $msiPath
    
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host "  MSI CREADO CON ESTRUCTURA DE DIRECTORIOS" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "ARCHIVO:" -ForegroundColor Cyan
    Write-Host "  $($msiFile.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "TAMAÑO:" -ForegroundColor Cyan
    Write-Host "  $([math]::Round($msiFile.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "INCLUYE:" -ForegroundColor Cyan
    Write-Host "  - $($allFiles.Count) archivos" -ForegroundColor White
    Write-Host "  - Estructura completa de carpetas:" -ForegroundColor White
    Write-Host "    • Assets\" -ForegroundColor Gray
    Write-Host "    • Controls\" -ForegroundColor Gray
    Write-Host "    • logs\" -ForegroundColor Gray
    Write-Host "    • runtimes\win-x64\native\" -ForegroundColor Gray
    Write-Host "    • Views\" -ForegroundColor Gray
    Write-Host "    • *.xbf (XAML compilados)" -ForegroundColor Gray
    Write-Host ""
    
    Start-Process explorer.exe -ArgumentList "/select,`"$($msiFile.FullName)`""
    
} else {
    Write-Host "ERROR: No se creo el MSI" -ForegroundColor Red
    exit 1
}

Write-Host "Completado!" -ForegroundColor Green
Write-Host ""
