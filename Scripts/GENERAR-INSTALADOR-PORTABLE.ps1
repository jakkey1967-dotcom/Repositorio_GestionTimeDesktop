# ═══════════════════════════════════════════════════════════════════════
# SCRIPT: Generar Instalador MSIX - Método Automatizado
# VERSION: 1.0 - SIMPLIFICADO
# ═══════════════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"
$projectDir = "C:\GestionTime\GestionTimeDesktop"
$version = "1.2.0"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  GENERADOR AUTOMÁTICO DE MSIX  " -ForegroundColor Cyan
Write-Host "  GestionTime Desktop v$version  " -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ===========================================================================
# PASO 1: VERIFICAR REQUISITOS
# ===========================================================================

Write-Host "🔍 Verificando requisitos..." -ForegroundColor Yellow

# Verificar .NET SDK
try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK no instalado" -ForegroundColor Red
    Write-Host "   Descargar de: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Verificar proyecto
$projectFile = Join-Path $projectDir "GestionTime.Desktop.csproj"
if (-not (Test-Path $projectFile)) {
    Write-Host "❌ Proyecto no encontrado: $projectFile" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Proyecto encontrado" -ForegroundColor Green

Write-Host ""

# ===========================================================================
# PASO 2: PUBLICAR APLICACIÓN
# ===========================================================================

Write-Host "📦 Publicando aplicación..." -ForegroundColor Yellow
Write-Host ""

Set-Location $projectDir

# Limpiar
Write-Host "   Limpiando salida anterior..." -ForegroundColor Gray
& dotnet clean -c Release -v quiet 2>&1 | Out-Null

# Restaurar
Write-Host "   Restaurando paquetes NuGet..." -ForegroundColor Gray
& dotnet restore -v quiet

# Publicar
Write-Host "   Compilando y publicando..." -ForegroundColor Gray
Write-Host ""

$publishOutput = & dotnet publish -c Release -r win-x64 --self-contained true -p:Platform=x64 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error al compilar:" -ForegroundColor Red
    Write-Host $publishOutput -ForegroundColor Gray
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host "  MÉTODO ALTERNATIVO: USAR VISUAL STUDIO  " -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "PASOS MANUALES:" -ForegroundColor Cyan
    Write-Host "  1. Abrir Visual Studio 2022" -ForegroundColor White
    Write-Host "  2. Abrir: GestionTime.Desktop.sln" -ForegroundColor White
    Write-Host "  3. Click derecho en proyecto > Publish" -ForegroundColor White
    Write-Host "  4. Create App Packages" -ForegroundColor White
    Write-Host "  5. Sideloading > x64 > Create" -ForegroundColor White
    Write-Host ""
    
    # Preguntar si abrir VS
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -property productPath 2>$null
        if ($vsPath) {
            Write-Host "¿Abrir Visual Studio ahora? (S/N): " -ForegroundColor Yellow -NoNewline
            $response = Read-Host
            if ($response -eq "S" -or $response -eq "s") {
                Start-Process $vsPath -ArgumentList "`"$projectDir\GestionTime.Desktop.sln`""
                Write-Host "✅ Visual Studio abierto" -ForegroundColor Green
            }
        }
    }
    
    exit 1
}

Write-Host "✅ Aplicación publicada exitosamente" -ForegroundColor Green
Write-Host ""

# ===========================================================================
# PASO 3: LOCALIZAR SALIDA
# ===========================================================================

$publishPath = Join-Path $projectDir "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"

if (-not (Test-Path $publishPath)) {
    Write-Host "⚠️  Directorio de publicación no encontrado" -ForegroundColor Yellow
    Write-Host "   Buscando en rutas alternativas..." -ForegroundColor Gray
    
    # Buscar en otras ubicaciones posibles
    $possiblePaths = @(
        "bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish",
        "bin\Release\net8.0-windows10.0.19041.0\publish",
        "bin\x64\Release\net8.0-windows10.0.19041.0\publish"
    )
    
    foreach ($path in $possiblePaths) {
        $testPath = Join-Path $projectDir $path
        if (Test-Path $testPath) {
            $publishPath = $testPath
            Write-Host "   ✅ Encontrado en: $publishPath" -ForegroundColor Green
            break
        }
    }
}

if (-not (Test-Path $publishPath)) {
    Write-Host "❌ No se encontró la salida de publicación" -ForegroundColor Red
    Write-Host "   Revisa los errores de compilación arriba" -ForegroundColor Yellow
    exit 1
}

# ===========================================================================
# PASO 4: COMPRIMIR EN ZIP (DISTRIBUCIÓN PORTABLE)
# ===========================================================================

Write-Host "📦 Creando paquete de distribución..." -ForegroundColor Yellow

$outputDir = Join-Path $projectDir "Installer\Output"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$zipPath = Join-Path $outputDir "GestionTime-Desktop-$version-Portable.zip"

# Eliminar ZIP anterior si existe
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# Crear ZIP
Write-Host "   Comprimiendo archivos..." -ForegroundColor Gray
Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -CompressionLevel Optimal

$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ INSTALADOR GENERADO EXITOSAMENTE  " -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "📦 PAQUETE PORTABLE (ZIP):" -ForegroundColor Cyan
Write-Host "   $zipPath" -ForegroundColor White
Write-Host "   Tamaño: $zipSize MB" -ForegroundColor Gray
Write-Host ""
Write-Host "📋 CONTENIDO:" -ForegroundColor Cyan
Write-Host "   ✓ Ejecutable: GestionTime.Desktop.exe" -ForegroundColor White
Write-Host "   ✓ Todas las dependencias incluidas" -ForegroundColor White
Write-Host "   ✓ Runtime .NET 8 embebido" -ForegroundColor White
Write-Host "   ✓ Archivos de configuración" -ForegroundColor White
Write-Host ""
Write-Host "🚀 USO:" -ForegroundColor Yellow
Write-Host "   1. Extraer el ZIP donde quieras" -ForegroundColor White
Write-Host "   2. Ejecutar: GestionTime.Desktop.exe" -ForegroundColor White
Write-Host "   3. No requiere instalación" -ForegroundColor White
Write-Host ""
Write-Host "💡 PARA CREAR MSIX (INSTALADOR MSI MODERNO):" -ForegroundColor Yellow
Write-Host "   Ejecuta: .\GENERAR-MSIX-VISUAL-STUDIO.ps1" -ForegroundColor White
Write-Host "   O usa Visual Studio directamente" -ForegroundColor White
Write-Host ""

# Abrir explorador
Start-Process explorer.exe -ArgumentList "/select,`"$zipPath`""

Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
