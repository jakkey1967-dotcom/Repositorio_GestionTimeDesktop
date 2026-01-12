# ================================================================================
# SCRIPT: Crear Instalador MSI Profesional para GestionTime Desktop
# VERSION: 2.0
# FECHA: Enero 2026
# DESCRIPCION: Genera un instalador MSI completo con WiX Toolset
# ================================================================================

param(
    [string]$SourceDir = "C:\GestionTime\GestionTimeDesktop\bin\x64\Debug\net8.0-windows10.0.19041.0",
    [string]$OutputDir = "C:\GestionTime\GestionTimeDesktop\Installer\Output",
    [string]$Version = "1.2.0"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CREAR INSTALADOR MSI - GESTIONTIME  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ================================================================================
# PASO 1: VERIFICAR REQUISITOS
# ================================================================================

Write-Host "[1/8] Verificando requisitos..." -ForegroundColor Yellow

# Verificar directorio de origen
if (-not (Test-Path $SourceDir)) {
    Write-Host "ERROR: No existe el directorio de origen: $SourceDir" -ForegroundColor Red
    Write-Host "SOLUCION: Compilar el proyecto primero con 'dotnet build'" -ForegroundColor Yellow
    exit 1
}

# Verificar que existe GestionTime.Desktop.exe
$exePath = Join-Path $SourceDir "GestionTime.Desktop.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: No se encontro GestionTime.Desktop.exe en $SourceDir" -ForegroundColor Red
    exit 1
}

Write-Host "   Directorio fuente: OK" -ForegroundColor Green
Write-Host "   Ejecutable encontrado: OK" -ForegroundColor Green

# Verificar WiX Toolset
$wixPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin"
if (-not (Test-Path $wixPath)) {
    Write-Host "ADVERTENCIA: WiX Toolset no encontrado en la ruta predeterminada" -ForegroundColor Yellow
    Write-Host "Intentando buscar en PATH..." -ForegroundColor Yellow
    
    $candle = Get-Command "candle.exe" -ErrorAction SilentlyContinue
    $light = Get-Command "light.exe" -ErrorAction SilentlyContinue
    
    if (-not $candle -or -not $light) {
        Write-Host ""
        Write-Host "ERROR: WiX Toolset NO esta instalado" -ForegroundColor Red
        Write-Host ""
        Write-Host "INSTALAR WiX Toolset:" -ForegroundColor Cyan
        Write-Host "  1. Descargar desde: https://wixtoolset.org/releases/" -ForegroundColor White
        Write-Host "  2. O ejecutar: winget install WiXToolset.WiX" -ForegroundColor White
        Write-Host "  3. Reiniciar PowerShell despues de instalar" -ForegroundColor White
        Write-Host ""
        exit 1
    }
    
    $wixPath = Split-Path $candle.Source -Parent
}

Write-Host "   WiX Toolset: OK ($wixPath)" -ForegroundColor Green

# ================================================================================
# PASO 2: CREAR ESTRUCTURA DE DIRECTORIOS
# ================================================================================

Write-Host ""
Write-Host "[2/8] Creando estructura de directorios..." -ForegroundColor Yellow

$installerDir = "C:\GestionTime\GestionTimeDesktop\Installer\MSI"
$outputMsiDir = $OutputDir

# Crear directorios
@($installerDir, $outputMsiDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
        Write-Host "   Creado: $_" -ForegroundColor Green
    } else {
        Write-Host "   Existe: $_" -ForegroundColor Gray
    }
}

# ================================================================================
# PASO 3: GENERAR LISTA DE ARCHIVOS Y CARPETAS
# ================================================================================

Write-Host ""
Write-Host "[3/8] Escaneando archivos del proyecto..." -ForegroundColor Yellow

# Obtener todos los archivos (excluyendo carpetas temporales y archivos innecesarios)
$allFiles = Get-ChildItem -Path $SourceDir -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\obj\\' -and
    $_.FullName -notmatch '\\tmp\\' -and
    $_.FullName -notmatch '\\.vs\\' -and
    $_.FullName -notmatch '\\.git\\' -and
    $_.FullName -notmatch '\\DISTRIBUIR\\' -and
    $_.Extension -notin @('.pdb', '.xml', '.cache', '.log')
}

Write-Host "   Total de archivos encontrados: $($allFiles.Count)" -ForegroundColor Green

# Agrupar por directorio
$filesByDir = $allFiles | Group-Object { $_.DirectoryName }

Write-Host "   Total de carpetas: $($filesByDir.Count)" -ForegroundColor Green

# ================================================================================
# PASO 4: GENERAR Product.wxs
# ================================================================================

Write-Host ""
Write-Host "[4/8] Generando Product.wxs..." -ForegroundColor Yellow

$productWxs = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  
  <!-- ============================================================ -->
  <!-- DEFINICION DEL PRODUCTO                                      -->
  <!-- ============================================================ -->
  
  <Product Id="*" 
           Name="GestionTime Desktop" 
           Language="3082" 
           Version="$Version.0" 
           Manufacturer="Global Retail Solutions" 
           UpgradeCode="A1B2C3D4-E5F6-7890-ABCD-EF1234567890">
    
    <Package InstallerVersion="500" 
             Compressed="yes" 
             InstallScope="perMachine"
             Description="Sistema de gestion de partes de trabajo"
             Comments="Instalador MSI v$Version" 
             Manufacturer="Global Retail Solutions" />
    
    <!-- Permitir actualizaciones -->
    <MajorUpgrade DowngradeErrorMessage="Ya tienes una version mas reciente instalada."
                  AllowSameVersionUpgrades="yes" />
    
    <!-- Medio de instalacion -->
    <MediaTemplate EmbedCab="yes" />
    
    <!-- ============================================================ -->
    <!-- ICONOS Y APARIENCIA                                          -->
    <!-- ============================================================ -->
    
    <Icon Id="AppIcon.ico" SourceFile="$SourceDir\Assets\app.ico" />
    <Property Id="ARPPRODUCTICON" Value="AppIcon.ico" />
    <Property Id="ARPURLINFOABOUT" Value="https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop" />
    
    <!-- ============================================================ -->
    <!-- ESTRUCTURA DE DIRECTORIOS                                    -->
    <!-- ============================================================ -->
    
    <Directory Id="TARGETDIR" Name="SourceDir">
      
      <!-- Archivos de programa -->
      <Directory Id="ProgramFiles64Folder">
        <Directory Id="ManufacturerFolder" Name="GestionTime">
          <Directory Id="INSTALLFOLDER" Name="Desktop" />
        </Directory>
      </Directory>
      
      <!-- Menu Inicio -->
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="GestionTime Desktop" />
      </Directory>
      
      <!-- Escritorio -->
      <Directory Id="DesktopFolder" Name="Desktop" />
      
    </Directory>
    
    <!-- ============================================================ -->
    <!-- FEATURES Y COMPONENTES                                       -->
    <!-- ============================================================ -->
    
    <Feature Id="MainApplication" 
             Title="GestionTime Desktop" 
             Description="Aplicacion principal de gestion de partes de trabajo"
             Level="1" 
             ConfigurableDirectory="INSTALLFOLDER"
             AllowAdvertise="no"
             Display="expand">
      
      <!-- Componente principal -->
      <ComponentGroupRef Id="ProductComponents" />
      
      <!-- Accesos directos -->
      <ComponentRef Id="ApplicationShortcut" />
      <ComponentRef Id="DesktopShortcut" />
      
    </Feature>
    
    <!-- ============================================================ -->
    <!-- INTERFAZ DE USUARIO                                          -->
    <!-- ============================================================ -->
    
    <UIRef Id="WixUI_InstallDir" />
    <Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER" />
    
    <!-- Personalizar textos -->
    <WixVariable Id="WixUILicenseRtf" Value="$installerDir\License.rtf" />
    <WixVariable Id="WixUIBannerBmp" Value="$installerDir\Banner.bmp" />
    <WixVariable Id="WixUIDialogBmp" Value="$installerDir\Dialog.bmp" />
    
  </Product>
  
  <!-- ============================================================ -->
  <!-- FRAGMENTOS - ACCESOS DIRECTOS                                -->
  <!-- ============================================================ -->
  
  <Fragment>
    <DirectoryRef Id="ApplicationProgramsFolder">
      <Component Id="ApplicationShortcut" Guid="B2C3D4E5-F6A7-8901-BCDE-F12345678901">
        
        <!-- Shortcut en Menu Inicio -->
        <Shortcut Id="ApplicationStartMenuShortcut"
                  Name="GestionTime Desktop"
                  Description="Sistema de gestion de partes de trabajo"
                  Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                  WorkingDirectory="INSTALLFOLDER"
                  Icon="AppIcon.ico" />
        
        <!-- Shortcut de desinstalacion -->
        <Shortcut Id="UninstallProduct"
                  Name="Desinstalar GestionTime Desktop"
                  Description="Eliminar GestionTime Desktop del sistema"
                  Target="[SystemFolder]msiexec.exe"
                  Arguments="/x [ProductCode]" />
        
        <RemoveFolder Id="CleanUpShortCut" Directory="ApplicationProgramsFolder" On="uninstall" />
        <RegistryValue Root="HKCU" 
                       Key="Software\GestionTime\Desktop" 
                       Name="installed" 
                       Type="integer" 
                       Value="1" 
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>
  </Fragment>
  
  <Fragment>
    <DirectoryRef Id="DesktopFolder">
      <Component Id="DesktopShortcut" Guid="C3D4E5F6-A7B8-9012-CDEF-123456789012">
        
        <!-- Shortcut en Escritorio -->
        <Shortcut Id="ApplicationDesktopShortcut"
                  Name="GestionTime Desktop"
                  Description="Sistema de gestion de partes de trabajo"
                  Target="[INSTALLFOLDER]GestionTime.Desktop.exe"
                  WorkingDirectory="INSTALLFOLDER"
                  Icon="AppIcon.ico" />
        
        <RemoveFolder Id="CleanUpDesktopShortcut" Directory="DesktopFolder" On="uninstall" />
        <RegistryValue Root="HKCU" 
                       Key="Software\GestionTime\Desktop" 
                       Name="desktopShortcut" 
                       Type="integer" 
                       Value="1" 
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>
  </Fragment>
  
</Wix>
"@

$productWxsPath = Join-Path $installerDir "Product.wxs"
$productWxs | Out-File -FilePath $productWxsPath -Encoding UTF8
Write-Host "   Product.wxs generado: OK" -ForegroundColor Green

# ================================================================================
# PASO 5: GENERAR Features.wxs (COMPONENTES)
# ================================================================================

Write-Host ""
Write-Host "[5/8] Generando Features.wxs con todos los componentes..." -ForegroundColor Yellow

# Funcion para generar un GUID unico
function New-ComponentGuid {
    param([string]$seed)
    $guid = [System.Guid]::NewGuid().ToString().ToUpper()
    return $guid
}

# Funcion para escapar caracteres especiales en XML
function Escape-XmlString {
    param([string]$text)
    return $text -replace '&', '&amp;' -replace '<', '&lt;' -replace '>', '&gt;' -replace '"', '&quot;' -replace "'", '&apos;'
}

# Generar estructura XML
$featuresXml = New-Object System.Text.StringBuilder
[void]$featuresXml.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
[void]$featuresXml.AppendLine('<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">')
[void]$featuresXml.AppendLine('')
[void]$featuresXml.AppendLine('  <!-- ============================================================ -->')
[void]$featuresXml.AppendLine('  <!-- COMPONENTES DE ARCHIVOS                                       -->')
[void]$featuresXml.AppendLine('  <!-- ============================================================ -->')
[void]$featuresXml.AppendLine('')
[void]$featuresXml.AppendLine('  <Fragment>')
[void]$featuresXml.AppendLine('    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">')
[void]$featuresXml.AppendLine('')

$componentCount = 0

# Generar componentes por directorio
foreach ($dirGroup in $filesByDir) {
    $dirPath = $dirGroup.Name
    $relativePath = $dirPath.Replace($SourceDir, '').TrimStart('\')
    
    # Determinar ID del directorio (escapar caracteres especiales)
    $dirId = if ($relativePath -eq '') { 
        'INSTALLFOLDER' 
    } else {
        'Dir_' + ($relativePath -replace '[\\\/\.\-\s]', '_')
    }
    
    # Comentario de seccion
    [void]$featuresXml.AppendLine("      <!-- Directorio: $relativePath -->")
    
    foreach ($file in $dirGroup.Group) {
        $componentCount++
        $fileName = $file.Name
        $fileRelativePath = $file.FullName.Replace($SourceDir + '\', '')
        
        # Generar GUID unico
        $componentGuid = New-ComponentGuid -seed $fileRelativePath
        
        # Generar ID del componente (sin caracteres especiales)
        $componentId = "Cmp_$componentCount"
        
        [void]$featuresXml.AppendLine("      <Component Id=`"$componentId`" Guid=`"$componentGuid`">")
        [void]$featuresXml.AppendLine("        <File Id=`"File_$componentCount`" Source=`"$($file.FullName)`" KeyPath=`"yes`" />")
        [void]$featuresXml.AppendLine("      </Component>")
    }
    
    [void]$featuresXml.AppendLine('')
}

[void]$featuresXml.AppendLine('    </ComponentGroup>')
[void]$featuresXml.AppendLine('  </Fragment>')
[void]$featuresXml.AppendLine('')
[void]$featuresXml.AppendLine('</Wix>')

$featuresWxsPath = Join-Path $installerDir "Features.wxs"
$featuresXml.ToString() | Out-File -FilePath $featuresWxsPath -Encoding UTF8

Write-Host "   Features.wxs generado: $componentCount componentes" -ForegroundColor Green

# ================================================================================
# PASO 6: CREAR ARCHIVOS AUXILIARES
# ================================================================================

Write-Host ""
Write-Host "[6/8] Creando archivos auxiliares..." -ForegroundColor Yellow

# License.rtf
$licenseRtf = @"
{\rtf1\ansi\deff0
{\fonttbl{\f0 Arial;}}
{\colortbl;\red0\green0\blue0;\red0\green0\blue255;}
\f0\fs24
\par \b\fs28 LICENCIA DE USO - GESTIONTIME DESKTOP\b0\fs24
\par
\par \b VERSION:\b0 $Version
\par \b FECHA:\b0 $(Get-Date -Format "dd/MM/yyyy")
\par \b EMPRESA:\b0 Global Retail Solutions
\par
\par \b\fs26 TERMINOS Y CONDICIONES\b0\fs24
\par
\par Este software es propiedad de Global Retail Solutions y esta protegido por leyes de derechos de autor internacionales.
\par
\par \b 1. LICENCIA DE USO\b0
\par Se concede al usuario final una licencia no exclusiva para usar este software en equipos de su organizacion.
\par
\par \b 2. RESTRICCIONES\b0
\par - No se permite la redistribucion del software sin autorizacion escrita.
\par - No se permite la ingenieria inversa o descompilacion.
\par - El software se proporciona "tal cual" sin garantias de ningun tipo.
\par
\par \b 3. SOPORTE TECNICO\b0
\par Email: soporte@gestiontime.com
\par Tel: +34 900 123 456
\par
\par \copyright 2026 Global Retail Solutions. Todos los derechos reservados.
}
"@

$licenseRtfPath = Join-Path $installerDir "License.rtf"
$licenseRtf | Out-File -FilePath $licenseRtfPath -Encoding ASCII

Write-Host "   License.rtf: OK" -ForegroundColor Green

# Copiar icono si existe
$iconSource = Join-Path $SourceDir "Assets\app.ico"
if (-not (Test-Path $iconSource)) {
    # Buscar en otras ubicaciones
    $altIconPath = Join-Path (Split-Path $SourceDir -Parent) "Assets\app.ico"
    if (Test-Path $altIconPath) {
        $iconSource = $altIconPath
    }
}

if (Test-Path $iconSource) {
    Write-Host "   Icono encontrado: OK" -ForegroundColor Green
} else {
    Write-Host "   ADVERTENCIA: No se encontro app.ico" -ForegroundColor Yellow
}

# ================================================================================
# PASO 7: COMPILAR CON WiX
# ================================================================================

Write-Host ""
Write-Host "[7/8] Compilando instalador MSI con WiX..." -ForegroundColor Yellow

# Cambiar al directorio de WiX
Push-Location $wixPath

try {
    # Compilar Product.wxs
    Write-Host "   Compilando Product.wxs..." -ForegroundColor Gray
    & .\candle.exe -nologo -arch x64 -ext WixUIExtension -out "$installerDir\Product.wixobj" "$productWxsPath"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error al compilar Product.wxs (codigo: $LASTEXITCODE)"
    }
    
    # Compilar Features.wxs
    Write-Host "   Compilando Features.wxs..." -ForegroundColor Gray
    & .\candle.exe -nologo -arch x64 -ext WixUIExtension -out "$installerDir\Features.wixobj" "$featuresWxsPath"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error al compilar Features.wxs (codigo: $LASTEXITCODE)"
    }
    
    # Enlazar (link)
    Write-Host "   Enlazando objetos..." -ForegroundColor Gray
    $msiOutput = Join-Path $outputMsiDir "GestionTime-Desktop-$Version-Setup.msi"
    
    & .\light.exe -nologo -ext WixUIExtension -ext WixUtilExtension `
        -out $msiOutput `
        "$installerDir\Product.wixobj" `
        "$installerDir\Features.wixobj" `
        -sval
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error al enlazar MSI (codigo: $LASTEXITCODE)"
    }
    
    Write-Host "   MSI compilado exitosamente" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "ERROR al compilar MSI:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Pop-Location
    exit 1
    
} finally {
    Pop-Location
}

# ================================================================================
# PASO 8: VERIFICAR RESULTADO
# ================================================================================

Write-Host ""
Write-Host "[8/8] Verificando instalador generado..." -ForegroundColor Yellow

$msiFile = Get-Item (Join-Path $outputMsiDir "GestionTime-Desktop-$Version-Setup.msi")

if ($msiFile.Exists) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  INSTALADOR MSI CREADO EXITOSAMENTE  " -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Archivo MSI:" -ForegroundColor Cyan
    Write-Host "  $($msiFile.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "Tamaño:" -ForegroundColor Cyan
    Write-Host "  $([math]::Round($msiFile.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "Version:" -ForegroundColor Cyan
    Write-Host "  $Version" -ForegroundColor White
    Write-Host ""
    Write-Host "Componentes incluidos:" -ForegroundColor Cyan
    Write-Host "  $componentCount archivos" -ForegroundColor White
    Write-Host ""
    Write-Host "INSTRUCCIONES DE INSTALACION:" -ForegroundColor Yellow
    Write-Host "  1. Hacer doble-clic en el archivo MSI" -ForegroundColor White
    Write-Host "  2. Seguir el asistente de instalacion" -ForegroundColor White
    Write-Host "  3. Buscar 'GestionTime Desktop' en el menu inicio" -ForegroundColor White
    Write-Host ""
    Write-Host "INSTALACION SILENCIOSA (CMD):" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiFile.Name)`" /qn /norestart" -ForegroundColor White
    Write-Host ""
    
    # Abrir explorador
    Start-Process explorer.exe -ArgumentList "/select,`"$($msiFile.FullName)`""
    
} else {
    Write-Host ""
    Write-Host "ERROR: No se pudo generar el archivo MSI" -ForegroundColor Red
    exit 1
}

Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
