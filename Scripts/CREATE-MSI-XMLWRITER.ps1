# ===========================================================================
# CREAR MSI FUNCIONAL - SIN PROBLEMAS DE PARSING
# Genera XML en archivo separado para evitar problemas con < y >
# ===========================================================================

param(
    [string]$ProjectDir = "C:\GestionTime\GestionTimeDesktop",
    [string]$OutputName = "GestionTime-Desktop-1.2.0-Setup.msi"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CREAR MSI - VERSION FUNCIONAL" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Rutas
$wixExe = "C:\Program Files\WiX Toolset v6.0\bin\wix.exe"
$binDir = "$ProjectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$outputDir = "$ProjectDir\Installer\Output"
$msiPath = "$outputDir\$OutputName"

# Verificaciones
if (-not (Test-Path $wixExe)) {
    Write-Host "ERROR: WiX Toolset v6.0 no encontrado" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "$binDir\GestionTime.Desktop.exe")) {
    Write-Host "ERROR: Ejecutable no encontrado en:" -ForegroundColor Red
    Write-Host "  $binDir" -ForegroundColor Yellow
    Write-Host "Compilar con: dotnet build -c Debug -r win-x64" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# PASO 1: Copiar window-config.ini
Write-Host "[1/5] Preparando archivos..." -ForegroundColor Yellow
$customConfig = "$ProjectDir\Installer\window-config.ini"
if (Test-Path $customConfig) {
    Copy-Item -Path $customConfig -Destination "$binDir\window-config.ini" -Force
    Write-Host "   [OK] window-config.ini copiado" -ForegroundColor Green
}

# PASO 2: Obtener archivos
Write-Host "[2/5] Escaneando archivos..." -ForegroundColor Yellow
$allFiles = Get-ChildItem -Path $binDir -File -Recurse
$allDirs = $allFiles | ForEach-Object { Split-Path $_.FullName -Parent } | Select-Object -Unique | Sort-Object
Write-Host "   Archivos: $($allFiles.Count)" -ForegroundColor Green
Write-Host "   Carpetas: $($allDirs.Count)" -ForegroundColor Green

# PASO 3: Generar XML en archivo
Write-Host "[3/5] Generando XML..." -ForegroundColor Yellow

$wxsFile = Join-Path $env:TEMP "GestionTime-Build.wxs"
$xmlWriter = [System.Xml.XmlWriter]::Create($wxsFile, (New-Object System.Xml.XmlWriterSettings -Property @{ Indent = $true }))

try {
    # Comenzar documento
    $xmlWriter.WriteStartDocument()
    $xmlWriter.WriteStartElement("Wix", "http://wixtoolset.org/schemas/v4/wxs")
    $xmlWriter.WriteAttributeString("xmlns", "ui", $null, "http://wixtoolset.org/schemas/v4/wxs/ui")
    
    # Package
    $xmlWriter.WriteStartElement("Package")
    $xmlWriter.WriteAttributeString("Name", "GestionTime Desktop")
    $xmlWriter.WriteAttributeString("Version", "1.2.0.0")
    $xmlWriter.WriteAttributeString("Manufacturer", "Global Retail Solutions")
    $xmlWriter.WriteAttributeString("UpgradeCode", "F1E2D3C4-B5A6-9780-ABCD-123456789012")
    $xmlWriter.WriteAttributeString("Language", "1034")
    
    # MajorUpgrade
    $xmlWriter.WriteStartElement("MajorUpgrade")
    $xmlWriter.WriteAttributeString("DowngradeErrorMessage", "Ya existe una version mas reciente.")
    $xmlWriter.WriteEndElement()
    
    # MediaTemplate
    $xmlWriter.WriteStartElement("MediaTemplate")
    $xmlWriter.WriteAttributeString("EmbedCab", "yes")
    $xmlWriter.WriteEndElement()
    
    # Icon
    $xmlWriter.WriteStartElement("Icon")
    $xmlWriter.WriteAttributeString("Id", "AppIcon")
    $xmlWriter.WriteAttributeString("SourceFile", "$ProjectDir\Assets\app_logo.ico")
    $xmlWriter.WriteEndElement()
    
    # Property
    $xmlWriter.WriteStartElement("Property")
    $xmlWriter.WriteAttributeString("Id", "ARPPRODUCTICON")
    $xmlWriter.WriteAttributeString("Value", "AppIcon")
    $xmlWriter.WriteEndElement()
    
    # StandardDirectory
    $xmlWriter.WriteStartElement("StandardDirectory")
    $xmlWriter.WriteAttributeString("Id", "ProgramFiles64Folder")
    $xmlWriter.WriteStartElement("Directory")
    $xmlWriter.WriteAttributeString("Id", "INSTALLFOLDER")
    $xmlWriter.WriteAttributeString("Name", "GestionTime Desktop")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndElement()
    
    # ProgramMenuFolder
    $xmlWriter.WriteStartElement("StandardDirectory")
    $xmlWriter.WriteAttributeString("Id", "ProgramMenuFolder")
    $xmlWriter.WriteStartElement("Directory")
    $xmlWriter.WriteAttributeString("Id", "ProgramMenuDir")
    $xmlWriter.WriteAttributeString("Name", "GestionTime Desktop")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndElement()
    
    # Feature
    $xmlWriter.WriteStartElement("Feature")
    $xmlWriter.WriteAttributeString("Id", "Main")
    $xmlWriter.WriteAttributeString("Title", "GestionTime Desktop")
    $xmlWriter.WriteAttributeString("Level", "1")
    $xmlWriter.WriteStartElement("ComponentGroupRef")
    $xmlWriter.WriteAttributeString("Id", "AllFiles")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteStartElement("ComponentRef")
    $xmlWriter.WriteAttributeString("Id", "MenuShortcut")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndElement()
    
    # UI
    $xmlWriter.WriteStartElement("WixUI", "http://wixtoolset.org/schemas/v4/wxs/ui")
    $xmlWriter.WriteAttributeString("Id", "WixUI_InstallDir")
    $xmlWriter.WriteAttributeString("InstallDirectory", "INSTALLFOLDER")
    $xmlWriter.WriteEndElement()
    
    # WixVariable
    $xmlWriter.WriteStartElement("WixVariable")
    $xmlWriter.WriteAttributeString("Id", "WixUILicenseRtf")
    $xmlWriter.WriteAttributeString("Value", "$ProjectDir\Installer\MSI\License.rtf")
    $xmlWriter.WriteEndElement()
    
    $xmlWriter.WriteEndElement() # Package
    
    # Fragment con componentes
    $xmlWriter.WriteStartElement("Fragment")
    $xmlWriter.WriteStartElement("ComponentGroup")
    $xmlWriter.WriteAttributeString("Id", "AllFiles")
    $xmlWriter.WriteAttributeString("Directory", "INSTALLFOLDER")
    
    $cmpId = 1
    foreach ($file in $allFiles) {
        $guid = [System.Guid]::NewGuid().ToString().ToUpper()
        $xmlWriter.WriteStartElement("Component")
        $xmlWriter.WriteAttributeString("Id", "C$cmpId")
        $xmlWriter.WriteAttributeString("Guid", $guid)
        $xmlWriter.WriteStartElement("File")
        $xmlWriter.WriteAttributeString("Source", $file.FullName)
        # No usar NeverOverwrite en WiX v6 - simplemente no marcar nada especial
        # El comportamiento por defecto es actualizar solo si la version es mayor
        $xmlWriter.WriteEndElement()
        $xmlWriter.WriteEndElement()
        $cmpId++
    }
    
    $xmlWriter.WriteEndElement() # ComponentGroup
    $xmlWriter.WriteEndElement() # Fragment
    
    # Fragment con shortcut
    $xmlWriter.WriteStartElement("Fragment")
    $xmlWriter.WriteStartElement("Component")
    $xmlWriter.WriteAttributeString("Id", "MenuShortcut")
    $xmlWriter.WriteAttributeString("Directory", "ProgramMenuDir")
    $xmlWriter.WriteStartElement("Shortcut")
    $xmlWriter.WriteAttributeString("Name", "GestionTime Desktop")
    $xmlWriter.WriteAttributeString("Target", "[INSTALLFOLDER]GestionTime.Desktop.exe")
    $xmlWriter.WriteAttributeString("WorkingDirectory", "INSTALLFOLDER")
    $xmlWriter.WriteAttributeString("Icon", "AppIcon")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteStartElement("RemoveFolder")
    $xmlWriter.WriteAttributeString("Id", "RemoveMenu")
    $xmlWriter.WriteAttributeString("On", "uninstall")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteStartElement("RegistryValue")
    $xmlWriter.WriteAttributeString("Root", "HKCU")
    $xmlWriter.WriteAttributeString("Key", "Software\GestionTime")
    $xmlWriter.WriteAttributeString("Name", "Installed")
    $xmlWriter.WriteAttributeString("Value", "1")
    $xmlWriter.WriteAttributeString("Type", "integer")
    $xmlWriter.WriteAttributeString("KeyPath", "yes")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndElement() # Component
    $xmlWriter.WriteEndElement() # Fragment
    
    $xmlWriter.WriteEndElement() # Wix
    $xmlWriter.WriteEndDocument()
    
} finally {
    $xmlWriter.Close()
}

Write-Host "   [OK] XML generado" -ForegroundColor Green

# PASO 4: Compilar
Write-Host "[4/5] Compilando MSI..." -ForegroundColor Yellow

try {
    & $wixExe build $wxsFile -arch x64 -out $msiPath -bindpath $binDir -ext WixToolset.UI.wixext -nologo
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error de compilacion: codigo $LASTEXITCODE"
    }
    
    Write-Host "   [OK] Compilacion exitosa" -ForegroundColor Green
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Remove-Item $wxsFile -Force -ErrorAction SilentlyContinue
    exit 1
}

Remove-Item $wxsFile -Force -ErrorAction SilentlyContinue

# PASO 5: Verificar
Write-Host "[5/5] Verificando..." -ForegroundColor Yellow

if (Test-Path $msiPath) {
    $msi = Get-Item $msiPath
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  [OK] MSI CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Archivo: $($msi.FullName)" -ForegroundColor Cyan
    Write-Host "Tamano: $([math]::Round($msi.Length / 1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host "Archivos: $($allFiles.Count)" -ForegroundColor Cyan
    Write-Host ""
    Start-Process explorer.exe -ArgumentList "/select,`"$($msi.FullName)`""
} else {
    Write-Host "ERROR: MSI no se creo" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Completado!" -ForegroundColor Green

