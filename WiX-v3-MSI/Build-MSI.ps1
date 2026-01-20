# ═══════════════════════════════════════════════════════════════
# COMPILAR INSTALADOR MSI - GESTIONTIME DESKTOP v1.4.1-beta
# WiX Toolset v3.14
# ═══════════════════════════════════════════════════════════════

param(
    [string]$SourceDir = "..\publish\portable",
    [string]$OutputDir = "..\installers",
    [string]$Version = "1.4.1-beta"
)

$ErrorActionPreference = "Stop"

# Agregar WiX al PATH
$wixPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin"
if (-not ($env:Path -like "*$wixPath*")) {
    $env:Path += ";$wixPath"
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  COMPILANDO INSTALADOR MSI - GestionTime Desktop $Version" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar que WiX está disponible
try {
    $null = & candle.exe -? 2>&1
} catch {
    Write-Host "[ERROR] WiX Toolset v3.14 no encontrado" -ForegroundColor Red
    Write-Host ""
    Write-Host "Por favor instala WiX v3.14 desde:" -ForegroundColor Yellow
    Write-Host "https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Verificar que existe la carpeta publish\portable
$SourcePath = Resolve-Path $SourceDir -ErrorAction SilentlyContinue
if (-not $SourcePath) {
    Write-Host "[ERROR] No se encontro la carpeta: $SourceDir" -ForegroundColor Red
    Write-Host ""
    Write-Host "Primero ejecuta:" -ForegroundColor Yellow
    Write-Host "  dotnet publish GestionTime.Desktop.csproj -c Release -r win-x64 --self-contained true -o publish\portable" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "[1/6] Verificando archivos criticos..." -ForegroundColor Green

# Verificar archivos críticos
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
    $filePath = Join-Path $SourcePath $file
    if (-not (Test-Path $filePath)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "[ERROR] Archivos criticos no encontrados:" -ForegroundColor Red
    foreach ($file in $missingFiles) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    Write-Host ""
    exit 1
}

Write-Host "  OK - Todos los archivos criticos encontrados" -ForegroundColor Gray

# Crear directorio de salida
Write-Host "[2/6] Preparando directorios..." -ForegroundColor Green
$OutputPath = Resolve-Path $OutputDir -ErrorAction SilentlyContinue
if (-not $OutputPath) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    $OutputPath = Resolve-Path $OutputDir
}

# Limpiar archivos de compilación anteriores
Write-Host "[3/6] Limpiando archivos temporales..." -ForegroundColor Green
Remove-Item "*.wixobj" -ErrorAction SilentlyContinue
Remove-Item "*.wixpdb" -ErrorAction SilentlyContinue
Remove-Item "Files.wxs" -ErrorAction SilentlyContinue
Remove-Item "$OutputPath\GestionTime-*.msi" -ErrorAction SilentlyContinue

# Generar Files.wxs con Heat.exe (incluye TODAS las DLLs automáticamente)
Write-Host "[4/6] Generando lista de archivos con Heat.exe..." -ForegroundColor Green
Write-Host "  Analizando $((Get-ChildItem $SourcePath -Recurse -File).Count) archivos..." -ForegroundColor Gray

$heatArgs = @(
    "dir", $SourcePath,
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

& heat.exe $heatArgs 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Heat.exe fallo con codigo: $LASTEXITCODE" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Verificar que se generó Files.wxs
if (-not (Test-Path "Files.wxs")) {
    Write-Host ""
    Write-Host "[ERROR] No se genero Files.wxs" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "  OK - Files.wxs generado con $(([xml](Get-Content Files.wxs)).Wix.Fragment.ComponentGroup.Component.Count) componentes" -ForegroundColor Gray

# Compilar con Candle
Write-Host "[5/6] Compilando archivos WiX..." -ForegroundColor Green
Write-Host "  Ejecutando candle.exe..." -ForegroundColor Gray

$candleArgs = @(
    "Product.wxs",
    "Files.wxs",
    "-ext", "WixUIExtension",
    "-ext", "WixUtilExtension",
    "-arch", "x64",
    "-dSourceDir=$SourcePath"
)

& candle.exe $candleArgs 2>&1 | ForEach-Object {
    if ($_ -match "error") {
        Write-Host "  [ERROR] $_" -ForegroundColor Red
    } elseif ($_ -match "warning") {
        Write-Host "  [WARN] $_" -ForegroundColor Yellow
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Candle.exe fallo con codigo: $LASTEXITCODE" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "  OK - Compilacion exitosa" -ForegroundColor Gray

# Enlazar con Light
Write-Host "[6/6] Generando archivo MSI..." -ForegroundColor Green
Write-Host "  Ejecutando light.exe..." -ForegroundColor Gray

$msiPath = Join-Path $OutputPath "GestionTime-$Version.msi"

$lightArgs = @(
    "Product.wixobj",
    "Files.wixobj",
    "-ext", "WixUIExtension",
    "-ext", "WixUtilExtension",
    "-cultures:en-us",
    "-out", $msiPath,
    "-sval"
)

& light.exe $lightArgs 2>&1 | ForEach-Object {
    if ($_ -match "error") {
        Write-Host "  [ERROR] $_" -ForegroundColor Red
    } elseif ($_ -match "warning") {
        Write-Host "  [WARN] $_" -ForegroundColor Yellow
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Light.exe fallo con codigo: $LASTEXITCODE" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Limpiar archivos temporales
Remove-Item "*.wixobj" -ErrorAction SilentlyContinue
Remove-Item "*.wixpdb" -ErrorAction SilentlyContinue
Remove-Item "Files.wxs" -ErrorAction SilentlyContinue

# Verificar que se creó el MSI
if (Test-Path $msiPath) {
    $size = (Get-Item $msiPath).Length / 1MB
    
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "  INSTALADOR MSI CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "Archivo creado:" -ForegroundColor Cyan
    Write-Host "  $msiPath" -ForegroundColor White
    Write-Host ""
    Write-Host "Tamano: $([math]::Round($size, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "PARA PROBAR:" -ForegroundColor Yellow
    Write-Host "  1. Ejecuta el MSI como Administrador" -ForegroundColor Gray
    Write-Host "  2. Sigue el asistente de instalacion" -ForegroundColor Gray
    Write-Host "  3. La app se instalara en:" -ForegroundColor Gray
    Write-Host "     C:\App\GestionTime-Desktop\" -ForegroundColor White
    Write-Host ""
    Write-Host "PARA DESINSTALAR:" -ForegroundColor Yellow
    Write-Host "  Configuracion > Aplicaciones > GestionTime Desktop > Desinstalar" -ForegroundColor Gray
    Write-Host ""
    Write-Host "PARA PUBLICAR EN GITHUB:" -ForegroundColor Yellow
    Write-Host "  1. Ve a: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new" -ForegroundColor Gray
    Write-Host "  2. Tag: v$Version" -ForegroundColor Gray
    Write-Host "  3. Adjunta: GestionTime-$Version.msi" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "[ERROR] No se pudo crear el archivo MSI" -ForegroundColor Red
    Write-Host ""
    exit 1
}
