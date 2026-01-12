# ===========================================================================
# CREAR MSI CON WIX v6.0 - GESTIONTIME DESKTOP
# VERSION: 4.0 - ENERO 2026
# DESCRIPCION: Usa WiX v6.0 para generar MSI automáticamente
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  CREAR MSI CON WIX v6.0" -ForegroundColor Cyan
Write-Host "  GestionTime Desktop v1.2.0" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$wixExe = "C:\Program Files\WiX Toolset v6.0\bin\wix.exe"
$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$outputDir = "$projectDir\Installer\Output"
$msiPath = "$outputDir\GestionTime-Desktop-1.2.0-Setup.msi"

Write-Host "[1/4] Verificando archivos..." -ForegroundColor Yellow

if (-not (Test-Path "$binDir\GestionTime.Desktop.exe")) {
    Write-Host "ERROR: Ejecutable no encontrado" -ForegroundColor Red
    Write-Host "Compilar con: dotnet build -c Debug -r win-x64" -ForegroundColor Yellow
    exit 1
}

Write-Host "   Ejecutable: OK" -ForegroundColor Green

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host ""
Write-Host "[2/4] Creando archivo WiX simplificado..." -ForegroundColor Yellow

$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  
  <Package Name="GestionTime Desktop"
           Language="1034"
           Version="1.2.0.0"
           Manufacturer="Global Retail Solutions"
           UpgradeCode="F1E2D3C4-B5A6-9780-ABCD-123456789012"
           Scope="perMachine"
           Compressed="yes">

    <MajorUpgrade DowngradeErrorMessage="Ya existe una version mas reciente instalada." />
    <MediaTemplate EmbedCab="yes" />

    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLFOLDER" Name="GestionTime Desktop" />
    </StandardDirectory>

    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ProgramMenuDir" Name="GestionTime Desktop" />
    </StandardDirectory>

    <Feature Id="Main" Title="GestionTime Desktop" Level="1">
      <ComponentGroupRef Id="AppFiles" />
      <ComponentRef Id="StartMenuShortcut" />
    </Feature>

  </Package>

  <Fragment>
    <ComponentGroup Id="AppFiles" Directory="INSTALLFOLDER">
      <Component>
        <File Source="$binDir\GestionTime.Desktop.exe" KeyPath="yes" />
      </Component>
      <Component>
        <File Source="$binDir\GestionTime.Desktop.dll" />
      </Component>
      <Component>
        <File Source="$binDir\appsettings.json" />
      </Component>
      <Component>
        <File Source="$binDir\GestionTime.Desktop.deps.json" />
      </Component>
      <Component>
        <File Source="$binDir\GestionTime.Desktop.runtimeconfig.json" />
      </Component>
    </ComponentGroup>
  </Fragment>

  <Fragment>
    <Component Id="StartMenuShortcut" Directory="ProgramMenuDir">
      <Shortcut Id="AppShortcut"
                Name="GestionTime Desktop"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER" />
      <RemoveFolder Id="RemoveProgramMenuDir" On="uninstall" />
      <RegistryValue Root="HKCU" 
                     Key="Software\GestionTime" 
                     Name="Installed" 
                     Value="1" 
                     Type="integer" 
                     KeyPath="yes" />
    </Component>
  </Fragment>

</Wix>
"@

$wxsTemp = Join-Path $env:TEMP "GestionTime-Temp.wxs"
$wxsContent | Out-File -FilePath $wxsTemp -Encoding UTF8

Write-Host "   Archivo WiX temporal creado" -ForegroundColor Green

Write-Host ""
Write-Host "[3/4] Compilando MSI con WiX v6.0..." -ForegroundColor Yellow

Set-Location $projectDir

try {
    & $wixExe build `
        $wxsTemp `
        -arch x64 `
        -out $msiPath `
        -bindpath $binDir
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error en compilacion (codigo: $LASTEXITCODE)"
    }
    
    Write-Host "   Compilacion exitosa" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "ERROR:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
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
    Write-Host "  MSI CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "ARCHIVO:" -ForegroundColor Cyan
    Write-Host "  $($msiFile.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "TAMAÑO:" -ForegroundColor Cyan
    Write-Host "  $([math]::Round($msiFile.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "INSTALACION:" -ForegroundColor Yellow
    Write-Host "  1. Doble-clic en el archivo MSI" -ForegroundColor White
    Write-Host "  2. Seguir asistente" -ForegroundColor White
    Write-Host "  3. Buscar 'GestionTime Desktop' en Menu Inicio" -ForegroundColor White
    Write-Host ""
    Write-Host "INSTALACION SILENCIOSA:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiFile.Name)`" /qn /norestart" -ForegroundColor White
    Write-Host ""
    
    Start-Process explorer.exe -ArgumentList "/select,`"$($msiFile.FullName)`""
    
} else {
    Write-Host "ERROR: No se creo el MSI" -ForegroundColor Red
    exit 1
}

Write-Host "Completado!" -ForegroundColor Green
Write-Host ""
