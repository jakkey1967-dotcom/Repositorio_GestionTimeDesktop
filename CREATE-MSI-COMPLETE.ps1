# ===========================================================================
# CREAR MSI COMPLETO CON WIX v6.0 - GESTIONTIME DESKTOP
# VERSION: 5.0 - ENERO 2026
# DESCRIPCION: Incluye TODOS los archivos (DLLs, Assets, Runtimes, etc.)
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  CREAR MSI COMPLETO - WIX v6.0" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$wixExe = "C:\Program Files\WiX Toolset v6.0\bin\wix.exe"
$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$outputDir = "$projectDir\Installer\Output"
$msiPath = "$outputDir\GestionTime-Desktop-1.2.0-Complete-Setup.msi"

Write-Host "[1/5] Recopilando archivos..." -ForegroundColor Yellow

# Obtener todos los archivos
$allFiles = Get-ChildItem -Path $binDir -File -Recurse | Where-Object { 
    $_.Extension -in @('.exe', '.dll', '.json', '.config', '.xml', '.ico', '.png', '.jpg', '.jpeg', '.md') 
}

Write-Host "   Archivos encontrados: $($allFiles.Count)" -ForegroundColor Green

Write-Host ""
Write-Host "[2/5] Generando componentes WiX..." -ForegroundColor Yellow

$componentsXml = New-Object System.Text.StringBuilder
[void]$componentsXml.AppendLine("  <Fragment>")
[void]$componentsXml.AppendLine("    <ComponentGroup Id=`"AllAppFiles`" Directory=`"INSTALLFOLDER`">")

$componentId = 1
$fileId = 1
foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Replace("$binDir\", "")
    $guid = [System.Guid]::NewGuid().ToString().ToUpper()
    
    # Generar ID unico para el archivo
    $uniqueFileId = "File$fileId"
    
    [void]$componentsXml.AppendLine("      <Component Id=`"Cmp$componentId`" Guid=`"$guid`">")
    [void]$componentsXml.AppendLine("        <File Id=`"$uniqueFileId`" Source=`"$($file.FullName)`" />")
    [void]$componentsXml.AppendLine("      </Component>")
    
    $componentId++
    $fileId++
}

[void]$componentsXml.AppendLine("    </ComponentGroup>")
[void]$componentsXml.AppendLine("  </Fragment>")

Write-Host "   Componentes generados: $componentId" -ForegroundColor Green

Write-Host ""
Write-Host "[3/5] Creando archivo WiX completo..." -ForegroundColor Yellow

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

    <MajorUpgrade DowngradeErrorMessage="Ya existe una version mas reciente instalada."
                  AllowSameVersionUpgrades="yes" />
    
    <MediaTemplate EmbedCab="yes" CompressionLevel="high" />

    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLFOLDER" Name="GestionTime Desktop" />
    </StandardDirectory>

    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ProgramMenuDir" Name="GestionTime Desktop" />
    </StandardDirectory>

    <StandardDirectory Id="DesktopFolder" />

    <Feature Id="MainApplication" Title="GestionTime Desktop" Level="1">
      <ComponentGroupRef Id="AllAppFiles" />
      <ComponentRef Id="StartMenuShortcut" />
      <ComponentRef Id="DesktopShortcut" />
    </Feature>

  </Package>

$($componentsXml.ToString())

  <Fragment>
    <Component Id="StartMenuShortcut" Directory="ProgramMenuDir">
      <Shortcut Id="AppStartMenuShortcut"
                Name="GestionTime Desktop"
                Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                WorkingDirectory="INSTALLFOLDER"
                Description="Sistema de gestion de partes de trabajo" />
      
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
                Description="Sistema de gestion de partes de trabajo" />
      
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

$wxsTemp = Join-Path $env:TEMP "GestionTime-Complete.wxs"
$wxsContent | Out-File -FilePath $wxsTemp -Encoding UTF8

Write-Host "   Archivo WiX creado (tamaño: $([math]::Round((Get-Item $wxsTemp).Length / 1KB, 2)) KB)" -ForegroundColor Green

Write-Host ""
Write-Host "[4/5] Compilando MSI..." -ForegroundColor Yellow
Write-Host "   (Esto puede tardar 1-2 minutos...)" -ForegroundColor Gray

Set-Location $projectDir

try {
    & $wixExe build `
        $wxsTemp `
        -arch x64 `
        -out $msiPath `
        -bindpath $binDir `
        -nologo
    
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
Write-Host "[5/5] Verificando MSI..." -ForegroundColor Yellow

if (Test-Path $msiPath) {
    $msiFile = Get-Item $msiPath
    
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host "  MSI COMPLETO CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "ARCHIVO:" -ForegroundColor Cyan
    Write-Host "  $($msiFile.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "TAMAÑO:" -ForegroundColor Cyan
    Write-Host "  $([math]::Round($msiFile.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "ARCHIVOS INCLUIDOS:" -ForegroundColor Cyan
    Write-Host "  $($allFiles.Count) archivos" -ForegroundColor White
    Write-Host "  - Ejecutable principal" -ForegroundColor Gray
    Write-Host "  - Todas las DLLs ($($allFiles.Where({$_.Extension -eq '.dll'}).Count) DLLs)" -ForegroundColor Gray
    Write-Host "  - Assets y configuracion" -ForegroundColor Gray
    Write-Host "  - Runtimes nativos" -ForegroundColor Gray
    Write-Host ""
    Write-Host "INSTALACION:" -ForegroundColor Yellow
    Write-Host "  1. Doble-clic en el archivo MSI" -ForegroundColor White
    Write-Host "  2. Seguir asistente de instalacion" -ForegroundColor White
    Write-Host "  3. Buscar 'GestionTime Desktop' en Menu Inicio o Escritorio" -ForegroundColor White
    Write-Host ""
    Write-Host "INSTALACION SILENCIOSA:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiFile.Name)`" /qn /norestart" -ForegroundColor White
    Write-Host ""
    Write-Host "DESINSTALACION:" -ForegroundColor Yellow
    Write-Host "  Panel de Control -> Programas y caracteristicas -> GestionTime Desktop" -ForegroundColor White
    Write-Host ""
    
    Start-Process explorer.exe -ArgumentList "/select,`"$($msiFile.FullName)`""
    
} else {
    Write-Host "ERROR: No se creo el MSI" -ForegroundColor Red
    exit 1
}

Write-Host "Completado!" -ForegroundColor Green
Write-Host ""
