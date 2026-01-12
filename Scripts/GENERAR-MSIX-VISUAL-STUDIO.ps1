# ═══════════════════════════════════════════════════════════════════════
# SCRIPT: Generar MSIX usando Visual Studio MSBuild
# VERSION: 1.0
# DESCRIPCION: Método más confiable para generar MSIX
# ═══════════════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  GENERAR MSIX - MÉTODO VISUAL STUDIO  " -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar Visual Studio
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path $vswhere)) {
    Write-Host "❌ ERROR: Visual Studio no está instalado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Este método requiere Visual Studio 2022" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Buscar Visual Studio
$vsPath = & $vswhere -latest -property productPath
$msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1

if (-not $vsPath -or -not $msbuildPath) {
    Write-Host "❌ ERROR: No se encontró Visual Studio o MSBuild" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Visual Studio encontrado" -ForegroundColor Green
Write-Host "✅ MSBuild: $msbuildPath" -ForegroundColor Green
Write-Host ""

# Abrir Visual Studio con la solución
$solutionPath = "C:\GestionTime\GestionTimeDesktop\GestionTime.Desktop.sln"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host "  INSTRUCCIONES PARA GENERAR MSIX  " -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""
Write-Host "Visual Studio se abrirá ahora." -ForegroundColor Cyan
Write-Host ""
Write-Host "Sigue estos pasos:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1️⃣  Espera a que Visual Studio termine de cargar" -ForegroundColor White
Write-Host ""
Write-Host "  2️⃣  Click derecho en el proyecto:" -ForegroundColor White
Write-Host "      'GestionTime.Desktop'" -ForegroundColor Gray
Write-Host ""
Write-Host "  3️⃣  Seleccionar:" -ForegroundColor White
Write-Host "      Publish > Create App Packages" -ForegroundColor Gray
Write-Host ""
Write-Host "  4️⃣  En el asistente:" -ForegroundColor White
Write-Host "      • Seleccionar: Sideloading" -ForegroundColor Gray
Write-Host "      • Click: Next" -ForegroundColor Gray
Write-Host ""
Write-Host "  5️⃣  Configuración:" -ForegroundColor White
Write-Host "      • Marcar: x64" -ForegroundColor Gray
Write-Host "      • Version: 1.2.0.0 (o superior)" -ForegroundColor Gray
Write-Host "      • Click: Create" -ForegroundColor Gray
Write-Host ""
Write-Host "  6️⃣  Esperar 2-3 minutos" -ForegroundColor White
Write-Host ""
Write-Host "  7️⃣  El instalador se generará en:" -ForegroundColor White
Write-Host "      C:\GestionTime\GestionTimeDesktop\AppPackages\" -ForegroundColor Gray
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""
Write-Host "Presiona cualquier tecla para abrir Visual Studio..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Abrir Visual Studio
Write-Host ""
Write-Host "Abriendo Visual Studio..." -ForegroundColor Cyan
Start-Process $vsPath -ArgumentList "`"$solutionPath`""

Write-Host "✅ Visual Studio abierto" -ForegroundColor Green
Write-Host ""
Write-Host "Sigue las instrucciones de arriba para crear el paquete MSIX" -ForegroundColor Yellow
Write-Host ""
