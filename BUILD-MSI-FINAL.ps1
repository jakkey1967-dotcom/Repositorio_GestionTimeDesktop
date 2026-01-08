# ===========================================================================
# COMPILAR MSI CON WIX TOOLSET v6.0 - GESTIONTIME DESKTOP
# VERSION: 3.0 - ENERO 2026
# DESCRIPCION: Compila el instalador MSI usando WiX Toolset v6.0
# ===========================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  COMPILAR MSI - GESTIONTIME DESKTOP" -ForegroundColor Cyan
Write-Host "  WiX Toolset v6.0" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# ===========================================================================
# VERIFICAR WIX TOOLSET v6.0
# ===========================================================================

Write-Host "[1/5] Verificando WiX Toolset..." -ForegroundColor Yellow

$wixPath = "C:\Program Files\WiX Toolset v6.0\bin"
$wixExe = Join-Path $wixPath "wix.exe"

if (-not (Test-Path $wixExe)) {
    Write-Host ""
    Write-Host "ERROR: WiX Toolset v6.0 NO esta instalado" -ForegroundColor Red
    Write-Host ""
    Write-Host "INSTALAR WiX Toolset:" -ForegroundColor Yellow
    Write-Host "  1. Visitar: https://wixtoolset.org/releases/" -ForegroundColor White
    Write-Host "  2. Descargar instalador de WiX v6.0" -ForegroundColor White
    Write-Host "  3. Ejecutar instalador" -ForegroundColor White
    Write-Host "  4. Volver a ejecutar este script" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "   WiX Toolset v6.0 encontrado: OK" -ForegroundColor Green

# ===========================================================================
# VERIFICAR ARCHIVOS FUENTE
# ===========================================================================

Write-Host ""
Write-Host "[2/5] Verificando archivos fuente..." -ForegroundColor Yellow

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

# Crear directorio de salida
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# ===========================================================================
# COMPILAR Y ENLAZAR MSI CON WIX v6.0
# ===========================================================================

Write-Host ""
Write-Host "[3/5] Compilando MSI con WiX v6.0..." -ForegroundColor Yellow

# Cambiar al directorio del proyecto para que las rutas relativas funcionen
Set-Location $projectDir

try {
    $msiPath = Join-Path $outputDir "GestionTime-Desktop-1.2.0-Setup.msi"
    $productWxs = Join-Path $msiDir "Product_Simple.wxs"
    
    # WiX v6.0 usa un comando unificado
    & $wixExe build `
        -arch x64 `
        -out $msiPath `
        $productWxs `
        -ext WixToolset.UI.wixext `
        -culture es-ES `
        -d "ProjectDir=$projectDir" `
        -bindpath "$projectDir"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Error al compilar MSI con WiX v6.0 (codigo: $LASTEXITCODE)"
    }
    
    Write-Host "   MSI compilado: OK" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "ERROR al compilar MSI:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "NOTA: WiX v6.0 tiene sintaxis diferente a v3.x" -ForegroundColor Yellow
    Write-Host "Es posible que los archivos .wxs necesiten actualizarse" -ForegroundColor Yellow
    exit 1
}

# ===========================================================================
# VERIFICAR RESULTADO
# ===========================================================================

Write-Host ""
Write-Host "[4/5] Verificando instalador MSI..." -ForegroundColor Yellow

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
    Write-Host ""
    Write-Host "ALTERNATIVA: Usar instalador ZIP portable ya creado:" -ForegroundColor Yellow
    Write-Host "  $outputDir\GestionTime-Desktop-1.2.0-Portable.zip" -ForegroundColor White
    exit 1
}

Write-Host "[5/5] Proceso completado!" -ForegroundColor Green
Write-Host ""
