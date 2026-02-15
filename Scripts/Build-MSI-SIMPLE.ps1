# BUILD MSI v1.9.3-beta - Script Simplificado (Sin Emojis)
# ================================================================

param(
    [switch]$SkipPublish = $false,
    [switch]$OpenFolder = $true
)

$ErrorActionPreference = "Stop"

# CONFIGURACION
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$ProjectFile = Join-Path $ProjectRoot "GestionTime.Desktop.csproj"
$PublishDir = Join-Path $ProjectRoot "publish\portable"
$WixDir = Join-Path $ProjectRoot "WiX-v3-MSI"
$InstallerDir = Join-Path $ProjectRoot "installers"
$Version = "1.9.3"

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  BUILD MSI LOCAL - GestionTime Desktop v$Version" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: VERIFICAR HERRAMIENTAS
Write-Host "[1/6] Verificando herramientas..." -ForegroundColor Yellow

# Verificar .NET 8 SDK
try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Host "  [OK] .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  [ERROR] .NET 8 SDK no encontrado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Descarga desde: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Verificar WiX Toolset v3.14
$wixPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin"
if (-not (Test-Path $wixPath)) {
    Write-Host "  [ERROR] WiX Toolset v3.14 no encontrado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Descarga desde:" -ForegroundColor Yellow
    Write-Host "https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe" -ForegroundColor Yellow
    exit 1
}

# Agregar WiX al PATH
if (-not ($env:Path -like "*$wixPath*")) {
    $env:Path += ";$wixPath"
}

Write-Host "  [OK] WiX Toolset v3.14" -ForegroundColor Green

# PASO 2: PUBLICAR APLICACION
if (-not $SkipPublish) {
    Write-Host ""
    Write-Host "[2/6] Publicando aplicacion..." -ForegroundColor Yellow
    Write-Host "  Destino: $PublishDir" -ForegroundColor Gray
    
    # Limpiar carpeta publish anterior
    if (Test-Path $PublishDir) {
        Write-Host "  Limpiando carpeta anterior..." -ForegroundColor Gray
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
    
    Write-Host "  Compilando y publicando (1-2 minutos)..." -ForegroundColor Gray
    
    $publishOutput = & dotnet @publishArgs 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "  [ERROR] Fallo en dotnet publish" -ForegroundColor Red
        Write-Host $publishOutput
        exit 1
    }
    
    Write-Host "  [OK] Publicacion completada" -ForegroundColor Green
    
    # Verificar archivos criticos
    $criticos = @(
        "GestionTime.Desktop.exe",
        "GestionTime.Desktop.pri",
        "appsettings.json",
        "window-config.ini"
    )
    
    foreach ($archivo in $criticos) {
        $ruta = Join-Path $PublishDir $archivo
        if (-not (Test-Path $ruta)) {
            Write-Host "  [WARNING] Archivo critico faltante: $archivo" -ForegroundColor Yellow
        }
    }
    
    # Verificar carpeta Assets
    $assetsPath = Join-Path $PublishDir "Assets"
    if (Test-Path $assetsPath) {
        $assetsCount = (Get-ChildItem $assetsPath).Count
        Write-Host "  [OK] Assets: $assetsCount archivos" -ForegroundColor Green
    } else {
        Write-Host "  [WARNING] Carpeta Assets no encontrada" -ForegroundColor Yellow
    }
    
} else {
    Write-Host ""
    Write-Host "[2/6] Saltando publicacion (usando archivos existentes)..." -ForegroundColor Yellow
    
    if (-not (Test-Path $PublishDir)) {
        Write-Host "  [ERROR] Carpeta publish no existe: $PublishDir" -ForegroundColor Red
        Write-Host "  Ejecuta sin -SkipPublish primero" -ForegroundColor Yellow
        exit 1
    }
}

# PASO 3: GENERAR ARCHIVOS WIX
Write-Host ""
Write-Host "[3/6] Generando archivos WiX..." -ForegroundColor Yellow

# Generar nuevo GUID para ProductId
$productId = [guid]::NewGuid().ToString().ToUpper()
Write-Host "  ProductId: $productId" -ForegroundColor Gray

# Leer Product.wxs
$productWxsPath = Join-Path $WixDir "Product.wxs"
if (-not (Test-Path $productWxsPath)) {
    Write-Host "  [ERROR] Product.wxs no encontrado: $productWxsPath" -ForegroundColor Red
    exit 1
}

Write-Host "  [OK] Product.wxs encontrado" -ForegroundColor Green

# PASO 4: GENERAR COMPONENTES CON HEAT
Write-Host ""
Write-Host "[4/7] Generando componentes con heat.exe..." -ForegroundColor Yellow

$heatOutput = Join-Path $WixDir "HarvestedFiles.wxs"

$heatArgs = @(
    "dir", "`"$PublishDir`"",
    "-cg", "HarvestedFiles",
    "-gg",
    "-scom", "-sreg", "-sfrag", "-srd",
    "-dr", "INSTALLFOLDER",
    "-var", "var.PublishDir",
    "-out", "`"$heatOutput`""
)

Write-Host "  Ejecutando: heat.exe (con -srd para suprimir directorio raiz)..." -ForegroundColor Gray

$heatResult = & heat.exe @heatArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  [ERROR] Heat.exe fallo" -ForegroundColor Red
    Write-Host $heatResult
    exit 1
}

Write-Host "  [OK] Componentes generados exitosamente" -ForegroundColor Green

# PASO 5: COMPILAR CON CANDLE
Write-Host ""
Write-Host "[5/7] Compilando WiX (candle.exe)..." -ForegroundColor Yellow

$candleOutput = Join-Path $WixDir "Product.wixobj"
$harvestedOutput = Join-Path $WixDir "HarvestedFiles.wixobj"

# Compilar Product.wxs
$candleArgs = @(
    "`"$productWxsPath`"",
    "-out", "`"$candleOutput`"",
    "-dPublishDir=`"$PublishDir`"",
    "-dVersion=$Version",
    "-dProductId=$productId",
    "-ext", "WixUtilExtension",
    "-arch", "x64"
)

Write-Host "  Compilando Product.wxs..." -ForegroundColor Gray

$candleResult = & candle.exe @candleArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  [ERROR] Candle.exe fallo" -ForegroundColor Red
    Write-Host $candleResult
    exit 1
}

# Compilar HarvestedFiles.wxs
$candleArgs2 = @(
    "`"$heatOutput`"",
    "-out", "`"$harvestedOutput`"",
    "-dPublishDir=`"$PublishDir`"",
    "-ext", "WixUtilExtension",
    "-arch", "x64"
)

Write-Host "  Compilando HarvestedFiles.wxs..." -ForegroundColor Gray

$candleResult2 = & candle.exe @candleArgs2 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  [ERROR] Candle.exe fallo en HarvestedFiles" -ForegroundColor Red
    Write-Host $candleResult2
    exit 1
}

Write-Host "  [OK] Compilacion WiX exitosa" -ForegroundColor Green

# PASO 6: ENLAZAR CON LIGHT
Write-Host ""
Write-Host "[6/7] Enlazando MSI (light.exe)..." -ForegroundColor Yellow

# Crear carpeta installers si no existe
if (-not (Test-Path $InstallerDir)) {
    New-Item -Path $InstallerDir -ItemType Directory -Force | Out-Null
}

$msiOutput = Join-Path $InstallerDir "GestionTime-v$Version-beta.msi"

$lightArgs = @(
    "`"$candleOutput`"",
    "`"$harvestedOutput`"",
    "-out", "`"$msiOutput`"",
    "-ext", "WixUIExtension",
    "-ext", "WixUtilExtension",
    "-cultures:es-ES",
    "-sice:ICE03",
    "-sice:ICE61",
    "-spdb"
)

Write-Host "  Ejecutando: light.exe..." -ForegroundColor Gray
Write-Host "  Suprimiendo validaciones ICE03 (Language Ids) y ICE61 (UpgradeCode)..." -ForegroundColor Gray

$lightResult = & light.exe @lightArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  [ERROR] Light.exe fallo" -ForegroundColor Red
    Write-Host $lightResult
    exit 1
}

Write-Host "  [OK] MSI generado exitosamente" -ForegroundColor Green

# PASO 7: VERIFICAR OUTPUT
Write-Host ""
Write-Host "[7/7] Verificando MSI generado..." -ForegroundColor Yellow

if (-not (Test-Path $msiOutput)) {
    Write-Host "  [ERROR] MSI no encontrado: $msiOutput" -ForegroundColor Red
    exit 1
}

$msiSize = (Get-Item $msiOutput).Length / 1MB

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  BUILD COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Version: $Version-beta" -ForegroundColor White
Write-Host "  MSI: GestionTime-v$Version-beta.msi" -ForegroundColor White
Write-Host "  Tamano: $([math]::Round($msiSize, 1)) MB" -ForegroundColor White
Write-Host "  Ruta: $msiOutput" -ForegroundColor White
Write-Host ""

# Abrir carpeta de instaladores
if ($OpenFolder -and (Test-Path $InstallerDir)) {
    Write-Host "Abriendo carpeta de instaladores..." -ForegroundColor Gray
    explorer.exe $InstallerDir
}

Write-Host ""
Write-Host "Presiona cualquier tecla para salir..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
