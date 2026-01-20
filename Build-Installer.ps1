# ============================================
# SCRIPT DE COMPILACIÓN MSI - GESTIONTIME DESKTOP
# ============================================

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.4.1-beta"
)

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   COMPILANDO INSTALADOR MSI - GESTIONTIME DESKTOP v$Version" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Paso 1: Limpiar compilaciones anteriores
Write-Host "🧹 Limpiando compilaciones anteriores..." -ForegroundColor Yellow
dotnet clean GestionTime.Desktop.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error al limpiar el proyecto" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Limpieza completada" -ForegroundColor Green
Write-Host ""

# Paso 2: Publicar la aplicación .NET
Write-Host "📦 Publicando aplicación .NET..." -ForegroundColor Yellow
dotnet publish GestionTime.Desktop.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    -o "publish\portable"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error al publicar la aplicación" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Publicación completada" -ForegroundColor Green
Write-Host ""

# Paso 3: Verificar archivos críticos
Write-Host "🔍 Verificando archivos críticos..." -ForegroundColor Yellow
$criticalFiles = @(
    "publish\portable\GestionTime.Desktop.exe",
    "publish\portable\GestionTime.Desktop.pri",
    "publish\portable\appsettings.json",
    "publish\portable\Assets"
)

$allFilesExist = $true
foreach ($file in $criticalFiles) {
    if (Test-Path $file) {
        Write-Host "   ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "   ❌ FALTA: $file" -ForegroundColor Red
        $allFilesExist = $false
    }
}

if (-not $allFilesExist) {
    Write-Host "❌ Faltan archivos críticos. Abortando." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Paso 4: Compilar el instalador MSI con WiX
Write-Host "🔨 Compilando instalador MSI con WiX..." -ForegroundColor Yellow
dotnet build GestionTime.Installer/GestionTime.Installer.wixproj -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error al compilar el instalador MSI" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Instalador MSI compilado" -ForegroundColor Green
Write-Host ""

# Paso 5: Localizar el archivo MSI generado
$msiPath = Get-ChildItem -Path "GestionTime.Installer\bin\$Configuration" -Filter "*.msi" -Recurse | Select-Object -First 1

if ($msiPath) {
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "   ✅ INSTALADOR MSI CREADO EXITOSAMENTE" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "📍 Ubicación: $($msiPath.FullName)" -ForegroundColor Cyan
    Write-Host "📏 Tamaño: $([math]::Round($msiPath.Length/1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    
    # Copiar el MSI a la raíz del proyecto con nombre descriptivo
    $outputName = "GestionTime-Desktop-v$Version-Setup.msi"
    Copy-Item $msiPath.FullName -Destination $outputName -Force
    Write-Host "📦 Copiado a raíz: $outputName" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "🎉 ¡Listo para distribuir!" -ForegroundColor Green
} else {
    Write-Host "❌ No se encontró el archivo MSI generado" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   PROCESO COMPLETADO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
