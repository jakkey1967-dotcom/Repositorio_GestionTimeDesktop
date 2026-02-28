# ===============================================================
# BUILD MSI CI - GESTIONTIME DESKTOP
# Version para GitHub Actions / CI (sin paths hardcodeados).
# Lee la version de Directory.Build.props automaticamente.
# Requiere: WiX v3.14 en PATH, .NET 8 SDK.
# ===============================================================

param(
    [string]$Version        = "",   # Ej: "2.0.0-beta" (vacio = leer de props)
    [string]$VersionNumeric = "",   # Ej: "2.0.0.0"    (vacio = leer de props)
    [switch]$SkipPublish    = $false
)

$ErrorActionPreference = "Stop"

$ProjectRoot  = Split-Path $PSScriptRoot -Parent
$ProjectFile  = Join-Path $ProjectRoot "GestionTime.Desktop.csproj"
$PublishDir   = Join-Path $ProjectRoot "publish\portable"
$WixDir       = Join-Path $ProjectRoot "WiX-v3-MSI"
$InstallerDir = Join-Path $ProjectRoot "installers"

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  BUILD MSI CI - GestionTime Desktop" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# ================================================================
# PASO 1: VERSION DESDE Directory.Build.props (si no se paso)
# ================================================================

if (-not $Version -or -not $VersionNumeric) {
    Write-Host "[1/6] Leyendo version de Directory.Build.props..." -ForegroundColor Yellow
    [xml]$props    = Get-Content (Join-Path $ProjectRoot "Directory.Build.props")
    $major  = $props.SelectSingleNode("//AppVersionMajor").InnerText.Trim()
    $minor  = $props.SelectSingleNode("//AppVersionMinor").InnerText.Trim()
    $patch  = $props.SelectSingleNode("//AppVersionPatch").InnerText.Trim()
    $suffix = $props.SelectSingleNode("//AppVersionSuffix").InnerText.Trim()

    if (-not $Version)        { $Version        = "$major.$minor.$patch$suffix" }
    if (-not $VersionNumeric) { $VersionNumeric  = "$major.$minor.$patch.0"     }
} else {
    Write-Host "[1/6] Version recibida por parametro" -ForegroundColor Yellow
}

Write-Host "  OK AppVersion:        $Version"        -ForegroundColor Green
Write-Host "  OK AppVersionNumeric: $VersionNumeric" -ForegroundColor Green
Write-Host ""

# ================================================================
# PASO 2: VERIFICAR HERRAMIENTAS (WiX en PATH)
# ================================================================

Write-Host "[2/6] Verificando herramientas WiX..." -ForegroundColor Yellow

$heat   = Get-Command heat.exe   -ErrorAction SilentlyContinue
$candle = Get-Command candle.exe -ErrorAction SilentlyContinue
$light  = Get-Command light.exe  -ErrorAction SilentlyContinue

if (-not $heat -or -not $candle -or -not $light) {
    Write-Host "  ERROR: WiX Toolset v3.14 no esta en el PATH" -ForegroundColor Red
    Write-Host "  Descarga: https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe" -ForegroundColor Yellow
    exit 1
}

Write-Host "  OK heat.exe, candle.exe, light.exe encontrados" -ForegroundColor Green
Write-Host ""

# ================================================================
# PASO 3: PUBLICAR APLICACION
# ================================================================

if (-not $SkipPublish) {
    Write-Host "[3/6] Publicando aplicacion (win-x64, self-contained)..." -ForegroundColor Yellow

    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    }

    $publishArgs = @(
        "publish", $ProjectFile,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:Platform=x64",
        "-p:PublishSingleFile=false",
        "-p:PublishReadyToRun=true",
        "-o", $PublishDir
    )

    Write-Host "  Compilando (puede tardar 2-3 min)..." -ForegroundColor Gray
    Write-Host "  Comando: dotnet $($publishArgs -join ' ')" -ForegroundColor DarkGray

    # Proteger contra $ErrorActionPreference=Stop de GitHub Actions
    # que convierte stderr warnings en excepciones terminantes
    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $out = & dotnet @publishArgs 2>&1
    $publishExit = $LASTEXITCODE
    $ErrorActionPreference = $prevEAP

    Write-Host "  Exit code: $publishExit" -ForegroundColor DarkGray
    if ($publishExit -ne 0) {
        Write-Host "  ERROR: dotnet publish fallo (exit $publishExit)" -ForegroundColor Red
        $out | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        exit 1
    }

    Write-Host "  OK Publicacion completada" -ForegroundColor Green
} else {
    Write-Host "[3/6] SKIP: usando archivos existentes en publish\portable" -ForegroundColor Yellow
}

Write-Host ""

# ================================================================
# PASO 4: VERIFICAR ARCHIVOS CRITICOS
# ================================================================

Write-Host "[4/6] Verificando archivos criticos..." -ForegroundColor Yellow

$criticalFiles = @(
    "GestionTime.Desktop.exe",
    "GestionTime.Desktop.pri",
    "GestionTime.Desktop.dll",
    "appsettings.json",
    "Assets\app_logo.ico"
)

foreach ($f in $criticalFiles) {
    $fp = Join-Path $PublishDir $f
    if (-not (Test-Path $fp)) {
        Write-Host "  FALTA: $f (ruta: $fp)" -ForegroundColor Red
        Write-Host "  Contenido de PublishDir:" -ForegroundColor Yellow
        Get-ChildItem $PublishDir -ErrorAction SilentlyContinue | Select-Object Name | ForEach-Object { Write-Host "    $($_.Name)" -ForegroundColor DarkGray }
        exit 1
    }
    Write-Host "  OK $f" -ForegroundColor Gray
}

Write-Host ""

# ================================================================
# PASO 5: HEAT -> CANDLE -> LIGHT
# ================================================================

Push-Location $WixDir

Remove-Item "*.wixobj" -ErrorAction SilentlyContinue
Remove-Item "*.wixpdb" -ErrorAction SilentlyContinue
Remove-Item "Files.wxs" -ErrorAction SilentlyContinue

Write-Host "[5/6] WiX: heat -> candle -> light..." -ForegroundColor Yellow

# Proteger llamadas a WiX contra $ErrorActionPreference=Stop
$prevEAP = $ErrorActionPreference
$ErrorActionPreference = "Continue"

# 5a --- Heat: genera Files.wxs
Write-Host "  heat.exe - generando Files.wxs..." -ForegroundColor Gray
Write-Host "  SourceDir: $PublishDir" -ForegroundColor DarkGray

$heatOut = & heat.exe dir """$PublishDir""" -cg HarvestedFiles -gg -scom -sreg -sfrag -srd -dr INSTALLFOLDER -var var.SourceDir -out Files.wxs 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR heat.exe (exit $LASTEXITCODE):" -ForegroundColor Red
    $heatOut | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    $ErrorActionPreference = $prevEAP
    Pop-Location
    exit 1
}

# 5b --- Candle: compila wxs -> wixobj
Write-Host "  candle.exe - compilando..." -ForegroundColor Gray

$candleOut = & candle.exe Product.wxs Files.wxs "-dSourceDir=$PublishDir" "-dProductVersion=$VersionNumeric" -ext WixUtilExtension -arch x64 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR candle.exe (exit $LASTEXITCODE):" -ForegroundColor Red
    $candleOut | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    $ErrorActionPreference = $prevEAP
    Pop-Location
    exit 1
}

# 5c --- Light: enlaza wixobj -> MSI
$msiName = "GestionTime-v$Version-win-x64.msi"
$msiPath = Join-Path $InstallerDir $msiName

if (-not (Test-Path $InstallerDir)) { New-Item -ItemType Directory -Path $InstallerDir | Out-Null }
if (Test-Path $msiPath)             { Remove-Item $msiPath -Force }

Write-Host "  light.exe - generando $msiName..." -ForegroundColor Gray

$lightOut = & light.exe Product.wixobj Files.wixobj -ext WixUIExtension -ext WixUtilExtension -sice:ICE03 -sice:ICE60 -sice:ICE61 -out """$msiPath""" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR light.exe (exit $LASTEXITCODE):" -ForegroundColor Red
    $lightOut | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    $ErrorActionPreference = $prevEAP
    Pop-Location
    exit 1
}

$ErrorActionPreference = $prevEAP
Pop-Location

Write-Host "  OK MSI generado correctamente" -ForegroundColor Green
Write-Host ""

# ================================================================
# FINALIZACION
# ================================================================

$sizeMB = [math]::Round((Get-Item $msiPath).Length / 1MB, 2)

Write-Host "================================================================" -ForegroundColor Green
Write-Host "  BUILD MSI COMPLETADO" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Archivo:  $msiName"      -ForegroundColor Cyan
Write-Host "  Ruta:     $msiPath"      -ForegroundColor White
Write-Host "  Tamano:   $sizeMB MB"    -ForegroundColor Gray
Write-Host "  Version:  $Version"      -ForegroundColor Yellow
Write-Host ""

# Exportar para CI
Write-Output "MSI_PATH=$msiPath"
Write-Output "MSI_NAME=$msiName"
