# ========================================
# 📦 Script de Compilación - Portable ZIP
# GestionTime Desktop - Versión 1.0.0
# Instalación simple y profesional
# ========================================

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📦 GESTIONTIME DESKTOP - BUILD PORTABLE" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Configuración
$version = "1.0.0"
$outputFolder = "publish"
$zipName = "GestionTime-Desktop-v$version-win-x64.zip"

# Limpiar builds anteriores
Write-Host "🧹 Limpiando builds anteriores..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path $outputFolder) {
    Remove-Item -Path $outputFolder -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path $zipName) {
    Remove-Item -Path $zipName -Force -ErrorAction SilentlyContinue
}
Write-Host "✅ Limpieza completada" -ForegroundColor Green
Write-Host ""

# Crear carpeta de salida
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null

# Verificar appsettings.json
Write-Host "🔍 Verificando configuración..." -ForegroundColor Yellow
if (-not (Test-Path "appsettings.json")) {
    Write-Host "❌ ERROR: No se encuentra appsettings.json" -ForegroundColor Red
    exit 1
}

$config = Get-Content "appsettings.json" -Raw | ConvertFrom-Json
Write-Host "📋 Configuración:" -ForegroundColor Cyan
Write-Host "   • API URL: $($config.Api.BaseUrl)" -ForegroundColor White
Write-Host "   • Login Path: $($config.Api.LoginPath)" -ForegroundColor White
Write-Host ""

# Compilar aplicación
Write-Host "🔨 Compilando aplicación..." -ForegroundColor Yellow
Write-Host "   • Configuración: Release" -ForegroundColor White
Write-Host "   • Plataforma: win-x64" -ForegroundColor White
Write-Host "   • Modo: Self-contained (.NET 8 incluido)" -ForegroundColor White
Write-Host ""

$publishArgs = @(
    "publish",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "/p:PublishSingleFile=false",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:PublishTrimmed=false",
    "/p:PublishReadyToRun=true"
)

$buildProcess = Start-Process -FilePath "dotnet" -ArgumentList $publishArgs -NoNewWindow -Wait -PassThru

if ($buildProcess.ExitCode -ne 0) {
    Write-Host "❌ ERROR: La compilación falló" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Compilación exitosa" -ForegroundColor Green
Write-Host ""

# Localizar carpeta de publicación
$publishPath = Get-ChildItem -Path "bin\Release" -Recurse -Directory -Filter "publish" | Select-Object -First 1

if (-not $publishPath) {
    Write-Host "❌ ERROR: No se encontró la carpeta de publicación" -ForegroundColor Red
    exit 1
}

Write-Host "📂 Carpeta de publicación: $($publishPath.FullName)" -ForegroundColor Cyan
Write-Host ""

# Copiar archivos
Write-Host "📦 Copiando archivos..." -ForegroundColor Yellow
Copy-Item -Path "$($publishPath.FullName)\*" -Destination $outputFolder -Recurse -Force

# Verificar archivos críticos
$criticalFiles = @(
    "$outputFolder\GestionTime.Desktop.exe",
    "$outputFolder\appsettings.json"
)

$missingFiles = @()
foreach ($file in $criticalFiles) {
    if (-not (Test-Path $file)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host "❌ ERROR: Archivos críticos faltantes:" -ForegroundColor Red
    foreach ($file in $missingFiles) {
        Write-Host "   • $file" -ForegroundColor Red
    }
    exit 1
}

Write-Host "✅ Todos los archivos críticos presentes" -ForegroundColor Green
Write-Host ""

# Calcular tamaño
$totalSize = (Get-ChildItem -Path $outputFolder -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "💾 Tamaño total: $("{0:N2}" -f $totalSize) MB" -ForegroundColor Cyan
Write-Host ""

# Crear archivo ZIP
Write-Host "🗜️ Creando archivo ZIP..." -ForegroundColor Yellow
Compress-Archive -Path "$outputFolder\*" -DestinationPath $zipName -Force
$zipSize = (Get-Item $zipName).Length / 1MB
Write-Host "✅ ZIP creado: $zipName ($("{0:N2}" -f $zipSize) MB)" -ForegroundColor Green
Write-Host ""

# Crear README para el instalador
$readmeContent = @"
═══════════════════════════════════════════════════════════════
📦 GESTIONTIME DESKTOP - Instalador v$version
═══════════════════════════════════════════════════════════════

✅ INSTALACIÓN

1. Extraer el contenido del ZIP a una carpeta (ej: C:\GestionTime)
2. Ejecutar GestionTime.Desktop.exe
3. Iniciar sesión con tus credenciales

⚙️ REQUISITOS

• Windows 10 versión 1809 o superior (Windows 11 recomendado)
• NO requiere instalación de .NET (incluido en el paquete)
• 200 MB de espacio en disco
• Conexión a Internet para usar la aplicación

🔧 CONFIGURACIÓN

El archivo appsettings.json contiene la configuración:
• API URL: https://gestiontimeapi.onrender.com
• Logs: Se guardan en carpeta "logs" junto al ejecutable

📁 ESTRUCTURA

GestionTime.Desktop.exe    - Ejecutable principal
appsettings.json          - Configuración
Assets\                   - Recursos (logos, imágenes)
*.dll                     - Bibliotecas necesarias

🚀 PRIMERA EJECUCIÓN

Al iniciar por primera vez:
1. La app puede tardar 30-60 segundos conectando (Render free tier)
2. Se creará automáticamente la carpeta de logs
3. Usar credenciales proporcionadas por el administrador

📝 LOGS

Los logs se guardan en: .\logs\gestiontime_YYYYMMDD.log
Nivel por defecto: Information

🆘 SOLUCIÓN DE PROBLEMAS

1. Si no inicia: Ejecutar como Administrador
2. Si no conecta: Verificar firewall/antivirus
3. Si falla el login: Verificar credenciales y que el backend esté activo

📞 SOPORTE

Para asistencia técnica, contactar con el administrador del sistema.

═══════════════════════════════════════════════════════════════
Fecha de build: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Versión: $version
"@

$readmeContent | Out-File -FilePath "$outputFolder\LEEME.txt" -Encoding UTF8

# Resumen final
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ BUILD COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📦 DISTRIBUCIÓN:" -ForegroundColor Yellow
Write-Host "   • ZIP: $zipName ($("{0:N2}" -f $zipSize) MB)" -ForegroundColor White
Write-Host "   • Carpeta: $outputFolder\ ($("{0:N2}" -f $totalSize) MB)" -ForegroundColor White
Write-Host ""
Write-Host "📋 ARCHIVOS INCLUIDOS:" -ForegroundColor Yellow
Write-Host "   • Ejecutable principal" -ForegroundColor White
Write-Host "   • .NET 8 Runtime (self-contained)" -ForegroundColor White
Write-Host "   • WinUI 3 libraries" -ForegroundColor White
Write-Host "   • appsettings.json configurado" -ForegroundColor White
Write-Host "   • Assets (logos, fondos)" -ForegroundColor White
Write-Host "   • Archivo LEEME.txt" -ForegroundColor White
Write-Host ""
Write-Host "🚀 INSTALACIÓN:" -ForegroundColor Yellow
Write-Host "   1. Extraer ZIP a C:\GestionTime (o cualquier carpeta)" -ForegroundColor White
Write-Host "   2. Ejecutar GestionTime.Desktop.exe" -ForegroundColor White
Write-Host "   3. (Opcional) Crear acceso directo en Escritorio" -ForegroundColor White
Write-Host ""
Write-Host "✅ LISTO PARA DISTRIBUCIÓN" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
