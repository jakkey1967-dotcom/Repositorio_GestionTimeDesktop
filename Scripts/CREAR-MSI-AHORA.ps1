# ===========================================================================
# CREAR MSI FINAL - VERSION QUE SI FUNCIONA
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  CREAR MSI COMPLETO - 153 ARCHIVOS" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$wix = "C:\Program Files\WiX Toolset v6.0\bin\wix.exe"
$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$wxsFile = "$projectDir\Installer\MSI\Product_Final.wxs"
$outDir = "$projectDir\Installer\Output"
$msiFile = "$outDir\GestionTime-Desktop-Setup.msi"

# Paso 1
Write-Host "[1/3] Preparando..." -ForegroundColor Yellow
if (Test-Path "$projectDir\Installer\window-config.ini") {
    Copy-Item "$projectDir\Installer\window-config.ini" "$binDir\window-config.ini" -Force
    Write-Host "   OK - window-config.ini copiado" -ForegroundColor Green
}

# Paso 2
Write-Host "[2/3] Generando WXS..." -ForegroundColor Yellow
$allFiles = Get-ChildItem -Path $binDir -File -Recurse
Write-Host "   Archivos: $($allFiles.Count)" -ForegroundColor Cyan

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
$xml.WriteEndElement()
$xml.WriteStartElement('Fragment')
$xml.WriteStartElement('ComponentGroup')
$xml.WriteAttributeString('Id', 'Files')
$xml.WriteAttributeString('Directory', 'INSTALLFOLDER')
$i = 1
foreach ($f in $allFiles) {
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
$xml.WriteEndElement()
$xml.WriteEndElement()
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
$xml.WriteEndElement()
$xml.WriteEndElement()
$xml.WriteEndElement()
$xml.WriteEndDocument()
$xml.Close()

Write-Host "   OK - WXS generado" -ForegroundColor Green

# Paso 3
Write-Host "[3/3] Compilando MSI..." -ForegroundColor Yellow
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

& $wix build $wxsFile -arch x64 -out $msiFile -bindpath $binDir -nologo

if ($LASTEXITCODE -eq 0 -and (Test-Path $msiFile)) {
    $msi = Get-Item $msiFile
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host "  MSI CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Archivo: $($msi.FullName)" -ForegroundColor Cyan
    Write-Host "Tamano: $([math]::Round($msi.Length / 1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host "Archivos: $($allFiles.Count)" -ForegroundColor Cyan
    Write-Host ""
    Start-Process explorer.exe -ArgumentList "/select,`"$($msi.FullName)`""
} else {
    Write-Host "ERROR: MSI no creado" -ForegroundColor Red
    exit 1
}

Write-Host "OK - Completado" -ForegroundColor Green
