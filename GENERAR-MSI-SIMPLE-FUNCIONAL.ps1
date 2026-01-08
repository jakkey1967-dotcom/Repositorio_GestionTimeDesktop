# ===========================================================================
# GENERAR MSI QUE SI FUNCIONA - VERSION FINAL
# Genera WXS dinamicamente solo con archivos que EXISTEN
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  GENERAR MSI - VERSION FINAL QUE FUNCIONA" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$wix = "C:\Program Files\WiX Toolset v6.0\bin\wix.exe"
$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$wxsFile = "$projectDir\Installer\MSI\Product_Generated_Clean.wxs"
$outDir = "$projectDir\Installer\Output"
$msiFile = "$outDir\GestionTime-Desktop-Setup.msi"

Write-Host "[1/4] Obteniendo archivos que EXISTEN..." -ForegroundColor Yellow

# Obtener SOLO archivos que EXISTEN (sin recursion para evitar fantasmas)
$rootFiles = Get-ChildItem -Path $binDir -File | Where-Object { $_.Exists }

Write-Host "   Archivos en raiz: $($rootFiles.Count)" -ForegroundColor Cyan

# Verificar archivo critico
if (-not ($rootFiles | Where-Object { $_.Name -eq "GestionTime.Desktop.exe" })) {
    Write-Host "ERROR: GestionTime.Desktop.exe no encontrado" -ForegroundColor Red
    exit 1
}

Write-Host "[2/4] Generando WXS..." -ForegroundColor Yellow

# Generar WXS usando XmlWriter (sin XML inline)
$xml = New-Object System.Xml.XmlTextWriter($wxsFile, [System.Text.Encoding]::UTF8)
$xml.Formatting = 'Indented'

$xml.WriteStartDocument()
$xml.WriteStartElement('Wix', 'http://wixtoolset.org/schemas/v4/wxs')
$xml.WriteStartElement('Package')
$xml.WriteAttributeString('Name', 'GestionTime Desktop')
$xml.WriteAttributeString('Version', '1.2.0.0')
$xml.WriteAttributeString('Manufacturer', 'Global Retail Solutions')
$xml.WriteAttributeString('UpgradeCode', 'F1E2D3C4-B5A6-9780-ABCD-123456789012')
$xml.WriteAttributeString('Language', '1034')

$xml.WriteStartElement('MajorUpgrade')
$xml.WriteAttributeString('DowngradeErrorMessage', 'Ya existe version mas reciente')
$xml.WriteEndElement()

$xml.WriteStartElement('MediaTemplate')
$xml.WriteAttributeString('EmbedCab', 'yes')
$xml.WriteEndElement()

$xml.WriteStartElement('StandardDirectory')
$xml.WriteAttributeString('Id', 'ProgramFiles64Folder')
$xml.WriteStartElement('Directory')
$xml.WriteAttributeString('Id', 'INSTALLFOLDER')
$xml.WriteAttributeString('Name', 'GestionTime Desktop')
$xml.WriteEndElement()
$xml.WriteEndElement()

$xml.WriteStartElement('StandardDirectory')
$xml.WriteAttributeString('Id', 'ProgramMenuFolder')
$xml.WriteStartElement('Directory')
$xml.WriteAttributeString('Id', 'MenuDir')
$xml.WriteAttributeString('Name', 'GestionTime Desktop')
$xml.WriteEndElement()
$xml.WriteEndElement()

$xml.WriteStartElement('Feature')
$xml.WriteAttributeString('Id', 'Main')
$xml.WriteAttributeString('Level', '1')
$xml.WriteStartElement('ComponentGroupRef')
$xml.WriteAttributeString('Id', 'Files')
$xml.WriteEndElement()
$xml.WriteStartElement('ComponentRef')
$xml.WriteAttributeString('Id', 'Menu')
$xml.WriteEndElement()
$xml.WriteEndElement()

$xml.WriteEndElement() # Package

# Fragment con archivos
$xml.WriteStartElement('Fragment')
$xml.WriteStartElement('ComponentGroup')
$xml.WriteAttributeString('Id', 'Files')
$xml.WriteAttributeString('Directory', 'INSTALLFOLDER')

$i = 1
foreach ($f in $rootFiles) {
    $guid = [System.Guid]::NewGuid().ToString().ToUpper()
    $xml.WriteStartElement('Component')
    $xml.WriteAttributeString('Id', "F$i")
    $xml.WriteAttributeString('Guid', $guid)
    $xml.WriteStartElement('File')
    $xml.WriteAttributeString('Source', $f.FullName)
    $xml.WriteEndElement()
    $xml.WriteEndElement()
    $i++
}

$xml.WriteEndElement() # ComponentGroup
$xml.WriteEndElement() # Fragment

# Fragment con shortcut
$xml.WriteStartElement('Fragment')
$xml.WriteStartElement('Component')
$xml.WriteAttributeString('Id', 'Menu')
$xml.WriteAttributeString('Directory', 'MenuDir')
$xml.WriteStartElement('Shortcut')
$xml.WriteAttributeString('Name', 'GestionTime Desktop')
$xml.WriteAttributeString('Target', '[INSTALLFOLDER]GestionTime.Desktop.exe')
$xml.WriteAttributeString('WorkingDirectory', 'INSTALLFOLDER')
$xml.WriteEndElement()
$xml.WriteStartElement('RemoveFolder')
$xml.WriteAttributeString('Id', 'Del')
$xml.WriteAttributeString('On', 'uninstall')
$xml.WriteEndElement()
$xml.WriteStartElement('RegistryValue')
$xml.WriteAttributeString('Root', 'HKCU')
$xml.WriteAttributeString('Key', 'Software\GestionTime')
$xml.WriteAttributeString('Name', 'Installed')
$xml.WriteAttributeString('Value', '1')
$xml.WriteAttributeString('Type', 'integer')
$xml.WriteAttributeString('KeyPath', 'yes')
$xml.WriteEndElement()
$xml.WriteEndElement() # Component
$xml.WriteEndElement() # Fragment

$xml.WriteEndElement() # Wix
$xml.WriteEndDocument()
$xml.Close()

Write-Host "   WXS generado con $($rootFiles.Count) archivos" -ForegroundColor Green

Write-Host "[3/4] Compilando MSI..." -ForegroundColor Yellow

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

& $wix build $wxsFile -arch x64 -out $msiFile -bindpath $binDir -nologo 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0 -and (Test-Path $msiFile)) {
    $msi = Get-Item $msiFile
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host "  MSI CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Archivo: $($msi.FullName)" -ForegroundColor Cyan
    Write-Host "Tamano: $([math]::Round($msi.Length / 1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host "Archivos: $($rootFiles.Count) (solo raiz)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "NOTA: Este MSI solo incluye archivos de la raiz" -ForegroundColor Yellow
    Write-Host "NO incluye subcarpetas (Assets, Views, etc.)" -ForegroundColor Yellow
    Write-Host "Para app completa usar ZIP Portable" -ForegroundColor Yellow
    Write-Host ""
    Start-Process explorer.exe -ArgumentList "/select,`"$($msi.FullName)`""
} else {
    Write-Host "ERROR: MSI no creado" -ForegroundColor Red
    Write-Host "Ver errores arriba" -ForegroundColor Yellow
    exit 1
}

Write-Host "[4/4] Completado" -ForegroundColor Green
