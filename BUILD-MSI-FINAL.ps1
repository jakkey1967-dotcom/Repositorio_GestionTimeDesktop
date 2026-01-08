# ===========================================================================
# COMPILAR MSI CON WIX TOOLSET - GESTIONTIME DESKTOP
# VERSION: 2.0 - ENERO 2026
# DESCRIPCION: Compila el instalador MSI usando WiX Toolset
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  COMPILAR MSI - GESTIONTIME DESKTOP" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# ===========================================================================
# VERIFICAR WIX TOOLSET
# ===========================================================================

Write-Host "[1/6] Verificando WiX Toolset..." -ForegroundColor Yellow

$wixPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin"
$candleExe = Join-Path $wixPath "candle.exe"
$lightExe = Join-Path $wixPath "light.exe"

if (-not (Test-Path $candleExe)) {
    Write-Host ""
    Write-Host "ERROR: WiX Toolset NO esta instalado" -ForegroundColor Red
    Write-Host ""
    Write-Host "INSTALAR WiX Toolset:" -ForegroundColor Yellow
    Write-Host "  1. Visitar: https://wixtoolset.org/releases/" -ForegroundColor White
    Write-Host "  2. Descargar: wix314.exe" -ForegroundColor White
    Write-Host "  3. Ejecutar instalador" -ForegroundColor White
    Write-Host "  4. Reiniciar PowerShell" -ForegroundColor White
    Write-Host "  5. Volver a ejecutar este script" -ForegroundColor White
    Write-Host ""
    
    $response = Read-Host "Abrir pagina de descarga ahora? (S/N)"
    if ($response -match '^[Ss]$') {
        Start-Process "https://wixtoolset.org/releases/"
    }
    exit 1
}

Write-Host "   WiX Toolset encontrado: OK" -ForegroundColor Green

# ===========================================================================
# VERIFICAR ARCHIVOS FUENTE
# ===========================================================================

Write-Host ""
Write-Host "[2/6] Verificando archivos fuente..." -ForegroundColor Yellow

$projectDir = "C:\GestionTime\GestionTimeDesktop"
$binDir = "$projectDir\bin\x64\Debug\net8.0-windows10.0.19041.0"
$msiDir = "$projectDir\Installer\MSI"
$outputDir = "$projectDir\Installer\Output"

# Verificar ejecutable
$exePath = Join-Path $binDir "GestionTime.Desktop.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "   ERROR: No se encuentra el ejecutable compilado" -ForegroundColor Red
    Write-Host "   Compilar primero con: dotnet build -c Debug -r win-x64" -ForegroundColor Yellow
    exit 1
}

Write-Host "   Ejecutable: OK" -ForegroundColor Green

# Verificar archivos WiX
$productWxs = Join-Path $msiDir "Product.wxs"
$featuresWxs = Join-Path $msiDir "Features_Simple.wxs"

if (-not (Test-Path $productWxs)) {
    Write-Host "   ERROR: No se encuentra Product.wxs" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $featuresWxs)) {
    Write-Host "   ERROR: No se encuentra Features_Simple.wxs" -ForegroundColor Red
    exit 1
}

Write-Host "   Archivos WiX: OK" -ForegroundColor Green

# Crear directorio de salida
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# ===========================================================================
# COMPILAR PRODUCT.WXS
# ===========================================================================

Write-Host ""
Write-Host "[3/6] Compilando Product.wxs..." -ForegroundColor Yellow

Push-Location $projectDir

try {
    $productObj = Join-Path $msiDir "Product.wixobj"
    
    & $candleExe `
        -nologo `
        -arch x64 `
        -ext WixUIExtension `
        -out $productObj `
        $productWxs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error al compilar Product.wxs (codigo: $LASTEXITCODE)"
    }
    
    Write-Host "   Product.wixobj: OK" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "ERROR al compilar Product.wxs:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Pop-Location
    exit 1
}

# ===========================================================================
# COMPILAR FEATURES_SIMPLE.WXS
# ===========================================================================

Write-Host ""
Write-Host "[4/6] Compilando Features_Simple.wxs..." -ForegroundColor Yellow

try {
    $featuresObj = Join-Path $msiDir "Features_Simple.wixobj"
    
    & $candleExe `
        -nologo `
        -arch x64 `
        -ext WixUIExtension `
        -out $featuresObj `
        $featuresWxs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error al compilar Features_Simple.wxs (codigo: $LASTEXITCODE)"
    }
    
    Write-Host "   Features_Simple.wixobj: OK" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "ERROR al compilar Features_Simple.wxs:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Pop-Location
    exit 1
}

# ===========================================================================
# ENLAZAR (LINK) MSI
# ===========================================================================

Write-Host ""
Write-Host "[5/6] Enlazando MSI..." -ForegroundColor Yellow

try {
    $msiPath = Join-Path $outputDir "GestionTime-Desktop-1.2.0-Setup.msi"
    
    & $lightExe `
        -nologo `
        -ext WixUIExtension `
        -ext WixUtilExtension `
        -out $msiPath `
        "$msiDir\Product.wixobj" `
        "$msiDir\Features_Simple.wixobj" `
        -sval
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error al enlazar MSI (codigo: $LASTEXITCODE)"
    }
    
    Write-Host "   MSI enlazado: OK" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "ERROR al enlazar MSI:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Pop-Location
    exit 1
    
} finally {
    Pop-Location
}

# ===========================================================================
# VERIFICAR RESULTADO
# ===========================================================================

Write-Host ""
Write-Host "[6/6] Verificando instalador MSI..." -ForegroundColor Yellow

$msiFile = Get-Item $msiPath -ErrorAction SilentlyContinue

if ($msiFile) {
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host "  INSTALADOR MSI CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "ARCHIVO MSI:" -ForegroundColor Cyan
    Write-Host "  $($msiFile.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "TAMAÑO:" -ForegroundColor Cyan
    Write-Host "  $([math]::Round($msiFile.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "VERSION:" -ForegroundColor Cyan
    Write-Host "  1.2.0" -ForegroundColor White
    Write-Host ""
    Write-Host "INSTRUCCIONES:" -ForegroundColor Yellow
    Write-Host "  1. Hacer doble-clic en el archivo MSI" -ForegroundColor White
    Write-Host "  2. Seguir asistente de instalacion" -ForegroundColor White
    Write-Host "  3. Buscar 'GestionTime Desktop' en Menu Inicio" -ForegroundColor White
    Write-Host ""
    Write-Host "INSTALACION SILENCIOSA:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiFile.Name)`" /qn /norestart" -ForegroundColor White
    Write-Host ""
    
    # Abrir explorador
    Start-Process explorer.exe -ArgumentList "/select,`"$($msiFile.FullName)`""
    
} else {
    Write-Host ""
    Write-Host "ERROR: No se pudo crear el MSI" -ForegroundColor Red
    exit 1
}

Write-Host "Listo!" -ForegroundColor Green
Write-Host ""
