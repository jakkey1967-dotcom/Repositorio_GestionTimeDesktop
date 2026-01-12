# ===========================================================================
# GENERAR MSI CON WIX v3.14 - SCRIPT LIMPIO QUE FUNCIONA
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  GENERAR MSI CON WIX v3.14" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0\TEMP_MSI"
$outDir = "$projectDir\Installer\Output"
$msiFile = "$outDir\GestionTime-Desktop-Setup.msi"

$heat = "C:\Program Files (x86)\WiX Toolset v3.14\bin\heat.exe"
$candle = "C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe"
$light = "C:\Program Files (x86)\WiX Toolset v3.14\bin\light.exe"

Set-Location $projectDir

Write-Host "[1/4] Usando carpeta portable limpia..." -ForegroundColor Yellow
Write-Host "   Carpeta: TEMP_MSI" -ForegroundColor Cyan

Write-Host "[2/4] Generando componentes con heat.exe..." -ForegroundColor Yellow
& $heat dir $binDir `
    -cg AppFiles `
    -dr INSTALLFOLDER `
    -gg `
    -sfrag `
    -srd `
    -sreg `
    -out "$env:TEMP\AppFiles.wxs"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: heat.exe fallo" -ForegroundColor Red
    exit 1
}
Write-Host "   OK - Componentes generados" -ForegroundColor Green

Write-Host "[3/4] Compilando con candle.exe..." -ForegroundColor Yellow
& $candle -nologo `
    "$env:TEMP\AppFiles.wxs" `
    "Installer\MSI\Product_WiX3.wxs" `
    -out "$env:TEMP\"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: candle.exe fallo" -ForegroundColor Red
    Remove-Item "$env:TEMP\AppFiles.wxs" -ErrorAction SilentlyContinue
    exit 1
}
Write-Host "   OK - Compilado" -ForegroundColor Green

Write-Host "[4/4] Enlazando con light.exe..." -ForegroundColor Yellow
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

& $light -nologo -ext WixUIExtension `
    "$env:TEMP\AppFiles.wixobj" `
    "$env:TEMP\Product_WiX3.wixobj" `
    -out $msiFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: light.exe fallo" -ForegroundColor Red
    Remove-Item "$env:TEMP\AppFiles.wxs" -ErrorAction SilentlyContinue
    Remove-Item "$env:TEMP\*.wixobj" -ErrorAction SilentlyContinue
    exit 1
}
Write-Host "   OK - MSI enlazado" -ForegroundColor Green

# Limpiar
Remove-Item "$env:TEMP\AppFiles.wxs" -ErrorAction SilentlyContinue
Remove-Item "$env:TEMP\*.wixobj" -ErrorAction SilentlyContinue
Remove-Item "$env:TEMP\*.wixpdb" -ErrorAction SilentlyContinue

if (Test-Path $msiFile) {
    $msi = Get-Item $msiFile
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host "  MSI CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Archivo: $($msi.FullName)" -ForegroundColor Cyan
    Write-Host "Tamano: $([math]::Round($msi.Length / 1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    Start-Process explorer.exe -ArgumentList "/select,`"$($msi.FullName)`""
} else {
    Write-Host "ERROR: MSI no se creo" -ForegroundColor Red
    exit 1
}

Write-Host "Completado!" -ForegroundColor Green
