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

# Obtener TODOS los archivos (sin filtrar por extensión)
$allFiles = Get-ChildItem -Path $binDir -File -Recurse

Write-Host "   Archivos encontrados: $($allFiles.Count)" -ForegroundColor Green

# Verificar archivos críticos
$criticalFiles = @(
    "GestionTime.Desktop.exe",
    "resources.pri",
    "window-config.ini",
    "appsettings.json"
)

foreach ($criticalFile in $criticalFiles) {
    $found = $allFiles | Where-Object { $_.Name -eq $criticalFile }
    if ($found) {
        Write-Host "   ✓ $criticalFile" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $criticalFile - FALTA" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "[2/5] Generando componentes WiX con estructura de directorios..." -ForegroundColor Yellow

# Archivo de configuración personalizado para reemplazar
$customWindowConfig = "$projectDir\Installer\window-config.ini"
$hasCustomConfig = Test-Path $customWindowConfig

# Crear estructura de directorios en WiX
$directoriesXml = New-Object System.Text.StringBuilder
$componentsXml = New-Object System.Text.StringBuilder

# Obtener todos los subdirectorios únicos
$allDirs = $allFiles | ForEach-Object { Split-Path $_.FullName -Parent } | Select-Object -Unique | Sort-Object

# Crear definiciones de directorios
$dirMap = @{}
$dirId = 1

[void]$directoriesXml.AppendLine("  <Fragment>")
[void]$directoriesXml.AppendLine("    <DirectoryRef Id=`"INSTALLFOLDER`">")

foreach ($dir in $allDirs) {
    if ($dir -eq $binDir) {
        $dirMap[$dir] = "INSTALLFOLDER"
        continue
    }
    
    $relativePath = $dir.Replace("$binDir\", "").Replace("$binDir", "")
    if (-not $relativePath) { continue }
    
    $pathParts = $relativePath.Split('\')
    $currentPath = $binDir
    $parentDirId = "INSTALLFOLDER"
    
    foreach ($part in $pathParts) {
        $currentPath = Join-Path $currentPath $part
        
        if (-not $dirMap.ContainsKey($currentPath)) {
            $safeDirName = $part -replace '[^a-zA-Z0-9]', '_'
            $newDirId = "Dir_$safeDirName`_$dirId"
            $dirMap[$currentPath] = $newDirId
            
            [void]$directoriesXml.AppendLine("      <Directory Id=`"$newDirId`" Name=`"$part`">")
            [void]$directoriesXml.AppendLine("      </Directory>")
            
            $dirId++
        }
        
        $parentDirId = $dirMap[$currentPath]
    }
}

[void]$directoriesXml.AppendLine("    </DirectoryRef>")
[void]$directoriesXml.AppendLine("  </Fragment>")

# Generar componentes agrupados por directorio
[void]$componentsXml.AppendLine("  <Fragment>")
[void]$componentsXml.AppendLine("    <ComponentGroup Id=`"AllAppFiles`">")

$componentId = 1
$fileId = 1

foreach ($file in $allFiles) {
    # Si es window-config.ini del bin, lo saltamos porque usaremos el personalizado
    if ($file.Name -eq "window-config.ini" -and $hasCustomConfig) {
        Write-Host "   ⚠ Omitiendo window-config.ini de bin (se usará versión personalizada)" -ForegroundColor Yellow
        continue
    }
    
    $fileDir = Split-Path $file.FullName -Parent
    $targetDirId = $dirMap[$fileDir]
    
    $guid = [System.Guid]::NewGuid().ToString().ToUpper()
    $uniqueFileId = "File$fileId"
    
    [void]$componentsXml.AppendLine("      <Component Id=`"Cmp$componentId`" Directory=`"$targetDirId`" Guid=`"$guid`">")
    [void]$componentsXml.AppendLine("        <File Id=`"$uniqueFileId`" Source=`"$($file.FullName)`" Name=`"$($file.Name)`" KeyPath=`"yes`" />")
    [void]$componentsXml.AppendLine("      </Component>")
    
    $componentId++
    $fileId++
}

# Agregar window-config.ini personalizado si existe
if ($hasCustomConfig) {
    $guid = [System.Guid]::NewGuid().ToString().ToUpper()
    $uniqueFileId = "File$fileId"
    
    [void]$componentsXml.AppendLine("      <Component Id=`"CmpCustomWindowConfig`" Directory=`"INSTALLFOLDER`" Guid=`"$guid`">")
    [void]$componentsXml.AppendLine("        <File Id=`"$uniqueFileId`" Source=`"$customWindowConfig`" Name=`"window-config.ini`" KeyPath=`"yes`" NeverOverwrite=`"yes`" />")
    [void]$componentsXml.AppendLine("      </Component>")
    
    Write-Host "   ✓ window-config.ini personalizado agregado (NeverOverwrite)" -ForegroundColor Green
    
    $componentId++
    $fileId++
}

[void]$componentsXml.AppendLine("    </ComponentGroup>")
[void]$componentsXml.AppendLine("  </Fragment>")

Write-Host "   Directorios: $($dirMap.Count)" -ForegroundColor Green
Write-Host "   Componentes: $componentId" -ForegroundColor Green

Write-Host ""
Write-Host "[3/5] Creando archivo WiX completo..." -ForegroundColor Yellow

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
             Description="Aplicacion completa con estructura de directorios"
             ConfigurableDirectory="INSTALLFOLDER">
      <ComponentGroupRef Id="AllAppFiles" />
      <ComponentRef Id="StartMenuShortcut" />
      <ComponentRef Id="DesktopShortcut" />
    </Feature>

    <ui:WixUI Id="WixUI_InstallDir" InstallDirectory="INSTALLFOLDER" />
    <WixVariable Id="WixUILicenseRtf" Value="$projectDir\Installer\MSI\License.rtf" />

  </Package>

$($directoriesXml.ToString())

$($componentsXml.ToString())

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
        -ext WixToolset.UI.wixext `
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
