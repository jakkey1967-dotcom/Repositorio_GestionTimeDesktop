# ═══════════════════════════════════════════════════════════════
# COMPILAR INSTALADOR MSI LOCAL - GESTIONTIME DESKTOP v1.9.3
# Configurado para backend de Render: https://gestiontimeapi.onrender.com
# ═══════════════════════════════════════════════════════════════

param(
    [switch]$SkipPublish = $false,  # Si true, usa los archivos ya publicados
    [switch]$OpenFolder = $true     # Si true, abre la carpeta del MSI al finalizar
)

$ErrorActionPreference = "Stop"

# ════════════════════════════════════════════════════════════════
# CONFIGURACIÓN
# ════════════════════════════════════════════════════════════════

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$ProjectFile = Join-Path $ProjectRoot "GestionTime.Desktop.csproj"
$PublishDir = Join-Path $ProjectRoot "publish\portable"
$WixDir = Join-Path $ProjectRoot "WiX-v3-MSI"
$InstallerDir = Join-Path $ProjectRoot "installers"
$Version = "1.9.3"

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  🔨 BUILD MSI LOCAL - GestionTime Desktop v$Version" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📦 Backend configurado: " -NoNewline
Write-Host "Render (gestiontimeapi.onrender.com)" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════════════════
# PASO 1: VERIFICAR HERRAMIENTAS
# ════════════════════════════════════════════════════════════════

Write-Host "[1/6] 🔍 Verificando herramientas..." -ForegroundColor Yellow

# Verificar .NET 8 SDK
try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Host "  ✅ .NET SDK: $dotnetVersion" -ForegroundColor Gray
} catch {
    Write-Host "  ❌ ERROR: .NET 8 SDK no encontrado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Descarga desde: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Verificar WiX Toolset v3.14
$wixPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin"
if (-not (Test-Path $wixPath)) {
    Write-Host "  ❌ ERROR: WiX Toolset v3.14 no encontrado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Descarga desde:" -ForegroundColor Yellow
    Write-Host "https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe" -ForegroundColor Yellow
    exit 1
}

# Agregar WiX al PATH
if (-not ($env:Path -like "*$wixPath*")) {
    $env:Path += ";$wixPath"
}

Write-Host "  ✅ WiX Toolset v3.14" -ForegroundColor Gray

# ════════════════════════════════════════════════════════════════
# PASO 2: PUBLICAR APLICACIÓN
# ════════════════════════════════════════════════════════════════

if (-not $SkipPublish) {
    Write-Host ""
    Write-Host "[2/6] 📦 Publicando aplicación..." -ForegroundColor Yellow
    Write-Host "  📂 Destino: $PublishDir" -ForegroundColor Gray
    
    # Limpiar carpeta publish anterior
    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    }
    
    # Publicar con dotnet publish
    $publishArgs = @(
        "publish",
        "`"$ProjectFile`"",
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=false",
        "-p:PublishReadyToRun=true",
        "-o", "`"$PublishDir`""
    )
    
    Write-Host "  ⏳ Compilando y publicando (puede tardar 1-2 minutos)..." -ForegroundColor Gray
    
    $publishOutput = & dotnet @publishArgs 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "  ❌ ERROR: Fallo en dotnet publish" -ForegroundColor Red
        Write-Host ""
        Write-Host $publishOutput
        exit 1
    }
    
    Write-Host "  ✅ Publicación completada" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[2/6] ⏭️  Saltando publicación (usando archivos existentes)" -ForegroundColor Yellow
}

# ════════════════════════════════════════════════════════════════
# PASO 3: VERIFICAR ARCHIVOS CRÍTICOS
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "[3/6] 🔍 Verificando archivos críticos..." -ForegroundColor Yellow

$criticalFiles = @(
    "GestionTime.Desktop.exe",
    "GestionTime.Desktop.pri",
    "GestionTime.Desktop.dll",
    "appsettings.json",
    "Microsoft.WinUI.dll",
    "Assets\app_logo.ico"
)

$missingFiles = @()
foreach ($file in $criticalFiles) {
    $filePath = Join-Path $PublishDir $file
    if (-not (Test-Path $filePath)) {
        $missingFiles += $file
        Write-Host "  ❌ Falta: $file" -ForegroundColor Red
    } else {
        Write-Host "  ✅ $file" -ForegroundColor Gray
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "  ❌ ERROR: Archivos críticos no encontrados" -ForegroundColor Red
    exit 1
}

# Verificar Assets (14 imágenes)
$assetsDir = Join-Path $PublishDir "Assets"
$assetsCount = (Get-ChildItem $assetsDir -Filter "*.png" -File).Count
Write-Host "  ✅ Assets: $assetsCount imágenes" -ForegroundColor Gray

# ════════════════════════════════════════════════════════════════
# PASO 4: GENERAR LISTA DE ARCHIVOS CON HEAT.EXE
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "[4/6] 🔥 Generando lista de archivos con Heat.exe..." -ForegroundColor Yellow

Push-Location $WixDir

# Limpiar archivos temporales anteriores
Remove-Item "*.wixobj" -ErrorAction SilentlyContinue
Remove-Item "*.wixpdb" -ErrorAction SilentlyContinue
Remove-Item "Files.wxs" -ErrorAction SilentlyContinue

$heatArgs = @(
    "dir",
    "`"$PublishDir`"",
    "-cg", "HarvestedFiles",
    "-gg",
    "-scom",
    "-sreg",
    "-sfrag",
    "-srd",
    "-dr", "INSTALLFOLDER",
    "-var", "var.SourceDir",
    "-out", "Files.wxs"
)

Write-Host "  ⏳ Analizando archivos..." -ForegroundColor Gray

$heatOutput = & heat.exe @heatArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  ❌ ERROR: Heat.exe falló" -ForegroundColor Red
    Write-Host $heatOutput
    Pop-Location
    exit 1
}

Write-Host "  ✅ Files.wxs generado correctamente" -ForegroundColor Green

# ════════════════════════════════════════════════════════════════
# PASO 5: COMPILAR CON CANDLE.EXE
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "[5/6] 🕯️  Compilando con Candle.exe..." -ForegroundColor Yellow

$candleArgs = @(
    "Product.wxs",
    "Files.wxs",
    "-dSourceDir=`"$PublishDir`"",
    "-ext", "WixUtilExtension",
    "-arch", "x64"
)

Write-Host "  ⏳ Generando archivos intermedios..." -ForegroundColor Gray

$candleOutput = & candle.exe @candleArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  ❌ ERROR: Candle.exe falló" -ForegroundColor Red
    Write-Host $candleOutput
    Pop-Location
    exit 1
}

Write-Host "  ✅ Archivos .wixobj generados" -ForegroundColor Green

# ════════════════════════════════════════════════════════════════
# PASO 6: LINKAR CON LIGHT.EXE
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "[6/6] 💡 Linkando con Light.exe..." -ForegroundColor Yellow

# Crear carpeta de instaladores si no existe
if (-not (Test-Path $InstallerDir)) {
    New-Item -ItemType Directory -Path $InstallerDir | Out-Null
}

$msiFileName = "GestionTime-v$Version-win-x64.msi"
$msiPath = Join-Path $InstallerDir $msiFileName

# Eliminar MSI anterior si existe
if (Test-Path $msiPath) {
    Remove-Item $msiPath -Force
}

$lightArgs = @(
    "Product.wixobj",
    "Files.wixobj",
    "-ext", "WixUIExtension",
    "-ext", "WixUtilExtension",
    "-out", "`"$msiPath`""
)

Write-Host "  ⏳ Generando instalador MSI..." -ForegroundColor Gray

$lightOutput = & light.exe @lightArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  ❌ ERROR: Light.exe falló" -ForegroundColor Red
    Write-Host $lightOutput
    Pop-Location
    exit 1
}

Pop-Location

# ════════════════════════════════════════════════════════════════
# FINALIZACIÓN
# ════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ INSTALADOR MSI GENERADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Archivo: " -NoNewline
Write-Host $msiFileName -ForegroundColor Cyan
Write-Host ""
Write-Host "📂 Ruta completa:" -ForegroundColor Yellow
Write-Host "   $msiPath" -ForegroundColor White
Write-Host ""

# Mostrar tamaño del MSI
$msiSize = (Get-Item $msiPath).Length
$msiSizeMB = [math]::Round($msiSize / 1MB, 2)
Write-Host "💾 Tamaño: $msiSizeMB MB" -ForegroundColor Gray
Write-Host ""

# Verificar configuración de appsettings.json
$appsettingsPath = Join-Path $PublishDir "appsettings.json"
$appsettingsContent = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
$baseUrl = $appsettingsContent.Api.BaseUrl

Write-Host "⚙️  Configuración incluida:" -ForegroundColor Gray
Write-Host "   Backend: $baseUrl" -ForegroundColor White
Write-Host ""

Write-Host "📋 INSTRUCCIONES DE INSTALACIÓN:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Ejecutar el MSI con doble-click" -ForegroundColor White
Write-Host "2. Instala automáticamente en: C:\App\GestionTime-Desktop" -ForegroundColor White
Write-Host "3. Crea accesos directos en Menú Inicio y Escritorio" -ForegroundColor White
Write-Host "4. Conecta automáticamente a Render (gestiontimeapi.onrender.com)" -ForegroundColor White
Write-Host ""

# Abrir carpeta de instaladores
if ($OpenFolder) {
    Write-Host "📂 Abriendo carpeta de instaladores..." -ForegroundColor Gray
    Start-Process explorer.exe -ArgumentList $InstallerDir
}

Write-Host ""
Write-Host "✨ Build completado exitosamente" -ForegroundColor Green
Write-Host ""
