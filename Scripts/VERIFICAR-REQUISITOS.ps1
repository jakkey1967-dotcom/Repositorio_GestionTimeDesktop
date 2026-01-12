# ═══════════════════════════════════════════════════════════════════════
# SCRIPT: Verificar Requisitos - Sistema de Instaladores
# VERSION: 1.0
# ═══════════════════════════════════════════════════════════════════════

$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  VERIFICADOR DE REQUISITOS  " -ForegroundColor Cyan
Write-Host "  GestionTime Desktop v1.2.0  " -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$allGood = $true

# ═══════════════════════════════════════════════════════════════════════
# VERIFICAR .NET SDK
# ═══════════════════════════════════════════════════════════════════════

Write-Host "🔍 Verificando .NET SDK 8..." -ForegroundColor Yellow

try {
    $dotnetVersion = & dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        $major = [int]($dotnetVersion.Split('.')[0])
        if ($major -eq 8) {
            Write-Host "   ✅ .NET SDK 8 instalado: $dotnetVersion" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  .NET SDK instalado pero es versión $major" -ForegroundColor Yellow
            Write-Host "      Se requiere .NET SDK 8.x" -ForegroundColor Yellow
            $allGood = $false
        }
    }
} catch {
    Write-Host "   ❌ .NET SDK no está instalado" -ForegroundColor Red
    Write-Host "      Descargar de: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    $allGood = $false
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════════════
# VERIFICAR VISUAL STUDIO
# ═══════════════════════════════════════════════════════════════════════

Write-Host "🔍 Verificando Visual Studio..." -ForegroundColor Yellow

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if (Test-Path $vswhere) {
    try {
        $vsPath = & $vswhere -latest -property productPath 2>$null
        $vsVersion = & $vswhere -latest -property catalog_productLineVersion 2>$null
        
        if ($vsPath) {
            Write-Host "   ✅ Visual Studio $vsVersion instalado" -ForegroundColor Green
            Write-Host "      Ruta: $vsPath" -ForegroundColor Gray
            
            # Verificar MSBuild
            $msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
            if ($msbuildPath) {
                Write-Host "   ✅ MSBuild disponible" -ForegroundColor Green
            } else {
                Write-Host "   ⚠️  MSBuild no encontrado" -ForegroundColor Yellow
            }
        } else {
            Write-Host "   ⚠️  Visual Studio no instalado" -ForegroundColor Yellow
            Write-Host "      Nota: Solo necesario para método MSIX" -ForegroundColor Gray
        }
    } catch {
        Write-Host "   ⚠️  Error al verificar Visual Studio" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ⚠️  Visual Studio no instalado" -ForegroundColor Yellow
    Write-Host "      Nota: Solo necesario para método MSIX" -ForegroundColor Gray
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════════════
# VERIFICAR PROYECTO
# ═══════════════════════════════════════════════════════════════════════

Write-Host "🔍 Verificando proyecto..." -ForegroundColor Yellow

$projectDir = "C:\GestionTime\GestionTimeDesktop"
$projectFile = Join-Path $projectDir "GestionTime.Desktop.csproj"
$solutionFile = Join-Path $projectDir "GestionTime.Desktop.sln"

if (Test-Path $projectFile) {
    Write-Host "   ✅ Archivo de proyecto encontrado" -ForegroundColor Green
} else {
    Write-Host "   ❌ Archivo de proyecto no encontrado" -ForegroundColor Red
    Write-Host "      Buscado en: $projectFile" -ForegroundColor Yellow
    $allGood = $false
}

if (Test-Path $solutionFile) {
    Write-Host "   ✅ Solución encontrada" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Archivo de solución no encontrado" -ForegroundColor Yellow
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════════════
# VERIFICAR WINDOWS SDK
# ═══════════════════════════════════════════════════════════════════════

Write-Host "🔍 Verificando Windows SDK..." -ForegroundColor Yellow

$sdkPaths = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64"
)

$sdkFound = $false
foreach ($sdkPath in $sdkPaths) {
    $makeappxPath = Join-Path $sdkPath "makeappx.exe"
    if (Test-Path $makeappxPath) {
        Write-Host "   ✅ Windows SDK encontrado" -ForegroundColor Green
        Write-Host "      Versión: $(Split-Path (Split-Path $sdkPath -Parent) -Leaf)" -ForegroundColor Gray
        $sdkFound = $true
        break
    }
}

if (-not $sdkFound) {
    Write-Host "   ⚠️  Windows SDK no encontrado" -ForegroundColor Yellow
    Write-Host "      Nota: Solo necesario para método MSIX avanzado" -ForegroundColor Gray
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════════════
# VERIFICAR ESPACIO EN DISCO
# ═══════════════════════════════════════════════════════════════════════

Write-Host "🔍 Verificando espacio en disco..." -ForegroundColor Yellow

try {
    $drive = (Get-Item $projectDir).PSDrive
    $freeSpace = [math]::Round($drive.Free / 1GB, 2)
    
    if ($freeSpace -gt 5) {
        Write-Host "   ✅ Espacio disponible: $freeSpace GB" -ForegroundColor Green
    } elseif ($freeSpace -gt 2) {
        Write-Host "   ⚠️  Espacio disponible: $freeSpace GB" -ForegroundColor Yellow
        Write-Host "      Recomendado: Al menos 5 GB libres" -ForegroundColor Gray
    } else {
        Write-Host "   ❌ Espacio insuficiente: $freeSpace GB" -ForegroundColor Red
        Write-Host "      Se requiere al menos 2 GB libres" -ForegroundColor Yellow
        $allGood = $false
    }
} catch {
    Write-Host "   ⚠️  No se pudo verificar espacio en disco" -ForegroundColor Yellow
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════════════
# VERIFICAR PERMISOS
# ═══════════════════════════════════════════════════════════════════════

Write-Host "🔍 Verificando permisos..." -ForegroundColor Yellow

try {
    $testFile = Join-Path $projectDir "test_permisos_temp.txt"
    "test" | Out-File $testFile -ErrorAction Stop
    Remove-Item $testFile -ErrorAction Stop
    Write-Host "   ✅ Permisos de escritura OK" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Sin permisos de escritura en el directorio" -ForegroundColor Red
    Write-Host "      Intenta ejecutar PowerShell como Administrador" -ForegroundColor Yellow
    $allGood = $false
}

Write-Host ""

# ═══════════════════════════════════════════════════════════════════════
# RESUMEN
# ═══════════════════════════════════════════════════════════════════════

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  RESUMEN  " -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($allGood) {
    Write-Host "✅ TODOS LOS REQUISITOS CUMPLIDOS" -ForegroundColor Green
    Write-Host ""
    Write-Host "Puedes generar instaladores usando:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  • Método Portable (Recomendado):" -ForegroundColor White
    Write-Host "    Doble clic en: GENERAR-INSTALADOR-PORTABLE.bat" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  • Método MSIX:" -ForegroundColor White
    Write-Host "    Doble clic en: GENERAR-INSTALADOR-MSIX.bat" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  • Menú con todas las opciones:" -ForegroundColor White
    Write-Host "    Doble clic en: GENERAR-INSTALADOR-MENU.bat" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "⚠️  ALGUNOS REQUISITOS NO SE CUMPLEN" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Revisa los mensajes arriba marcados con ❌" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "REQUISITO MÍNIMO OBLIGATORIO:" -ForegroundColor Cyan
    Write-Host "  • .NET SDK 8" -ForegroundColor White
    Write-Host "    Descargar: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Gray
    Write-Host ""
    Write-Host "REQUISITOS OPCIONALES (solo para MSIX):" -ForegroundColor Cyan
    Write-Host "  • Visual Studio 2022" -ForegroundColor White
    Write-Host "  • Windows SDK" -ForegroundColor White
    Write-Host ""
}

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Para más información, lee: INICIO-RAPIDO.md" -ForegroundColor Gray
Write-Host ""

Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
