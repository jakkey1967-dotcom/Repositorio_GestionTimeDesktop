# ========================================
# 🚀 Script Completo de Preparación y Migración
# GestionTime Desktop
# ========================================

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🚀 PREPARACIÓN COMPLETA PARA NUEVO REPOSITORIO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Configuración
$newRepoUrl = "https://github.com/jakkey1967-dotcom/GestionTime.Desktop.git"

# Paso 1: Verificar archivos nuevos
Write-Host "📋 PASO 1: Verificando archivos del repositorio..." -ForegroundColor Yellow
Write-Host ""

$requiredFiles = @{
    "README.md" = "✅ Documentación principal"
    "CHANGELOG.md" = "✅ Historial de cambios"
    "CONTRIBUTING.md" = "✅ Guía de contribución"
    "LICENSE" = "✅ Licencia del software"
    ".gitignore" = "✅ Archivos a ignorar"
    "migrate-to-new-repo.ps1" = "✅ Script de migración"
    "build-msi.ps1" = "✅ Script de build MSI"
}

$allFilesPresent = $true
foreach ($file in $requiredFiles.Keys) {
    if (Test-Path $file) {
        Write-Host "   $($requiredFiles[$file]) - $file" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Falta - $file" -ForegroundColor Red
        $allFilesPresent = $false
    }
}

if (-not $allFilesPresent) {
    Write-Host ""
    Write-Host "⚠️ Faltan archivos importantes. Verifica que estés en el directorio correcto." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "✅ Todos los archivos necesarios están presentes" -ForegroundColor Green
Write-Host ""

# Paso 2: Ver estado actual
Write-Host "📊 PASO 2: Estado actual de Git..." -ForegroundColor Yellow
Write-Host ""

git status --short

Write-Host ""

# Paso 3: Revisar cambios
Write-Host "🔍 PASO 3: Archivos a commitear..." -ForegroundColor Yellow
Write-Host ""

$uncommitted = git status --porcelain 2>&1
if ($uncommitted) {
    Write-Host "   Archivos modificados/nuevos:" -ForegroundColor White
    git status --short | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
    Write-Host ""
    
    Write-Host "📝 Resumen de cambios importantes:" -ForegroundColor Cyan
    Write-Host "   • README.md actualizado con documentación completa" -ForegroundColor Gray
    Write-Host "   • CHANGELOG.md con versión 1.0.0" -ForegroundColor Gray
    Write-Host "   • CONTRIBUTING.md para guiar colaboradores" -ForegroundColor Gray
    Write-Host "   • LICENSE con términos propietarios" -ForegroundColor Gray
    Write-Host "   • .gitignore limpio para .NET 8 + WinUI 3" -ForegroundColor Gray
    Write-Host "   • Scripts de build (MSI) actualizados" -ForegroundColor Gray
    Write-Host "   • Icono configurado (app_logo.ico)" -ForegroundColor Gray
    Write-Host ""
    
    $response = Read-Host "¿Deseas hacer commit de estos cambios? (s/n)"
    if ($response -eq "s") {
        Write-Host ""
        Write-Host "📦 Haciendo commit..." -ForegroundColor Yellow
        
        # Agregar todos los cambios
        git add -A
        
        # Commit con mensaje descriptivo
        $commitMessage = @"
chore: preparar repositorio para migración

- Actualizar README.md con documentación completa
- Agregar CHANGELOG.md con versión 1.0.0
- Agregar CONTRIBUTING.md para colaboradores
- Agregar LICENSE con términos propietarios
- Limpiar .gitignore para .NET 8 + WinUI 3
- Actualizar scripts de build (MSI)
- Configurar icono de aplicación (app_logo.ico)
- Agregar guías de migración y configuración

Version: 1.0.0
Date: $(Get-Date -Format 'yyyy-MM-dd')
"@
        
        git commit -m $commitMessage
        Write-Host "✅ Commit realizado" -ForegroundColor Green
    }
    else {
        Write-Host ""
        Write-Host "⚠️ No se hizo commit. Continuar sin cambios pendientes..." -ForegroundColor Yellow
    }
}
else {
    Write-Host "✅ No hay cambios pendientes" -ForegroundColor Green
}

Write-Host ""

# Paso 4: Verificar remote actual
Write-Host "🔗 PASO 4: Verificando configuración de Git..." -ForegroundColor Yellow
Write-Host ""

$currentRemote = git remote get-url origin 2>&1
$currentBranch = git branch --show-current 2>&1

Write-Host "   • Remote actual: $currentRemote" -ForegroundColor Gray
Write-Host "   • Rama actual: $currentBranch" -ForegroundColor Gray
Write-Host ""

# Paso 5: Instrucciones finales
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ PREPARACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 SIGUIENTE PASO: CREAR REPOSITORIO EN GITHUB" -ForegroundColor Yellow
Write-Host ""
Write-Host "1️⃣ Ve a: https://github.com/new" -ForegroundColor White
Write-Host ""
Write-Host "2️⃣ Configura el repositorio:" -ForegroundColor White
Write-Host "   • Repository name: GestionTime.Desktop" -ForegroundColor Gray
Write-Host "   • Description: Aplicación desktop WinUI 3 para gestión de partes de trabajo" -ForegroundColor Gray
Write-Host "   • Visibility: Private (o Public)" -ForegroundColor Gray
Write-Host "   • ❌ NO marcar 'Initialize with README'" -ForegroundColor Red
Write-Host "   • ❌ NO agregar .gitignore" -ForegroundColor Red
Write-Host "   • ❌ NO agregar license" -ForegroundColor Red
Write-Host ""
Write-Host "3️⃣ Click 'Create repository'" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$ready = Read-Host "¿Ya creaste el repositorio en GitHub? (s/n)"
if ($ready -eq "s") {
    Write-Host ""
    Write-Host "🚀 Ejecutando migración..." -ForegroundColor Yellow
    Write-Host ""
    
    # Ejecutar script de migración
    .\migrate-to-new-repo.ps1
}
else {
    Write-Host ""
    Write-Host "📝 CUANDO ESTÉS LISTO:" -ForegroundColor Yellow
    Write-Host "   1. Crea el repositorio en GitHub" -ForegroundColor White
    Write-Host "   2. Ejecuta: .\migrate-to-new-repo.ps1" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "✅ Todo está preparado para la migración" -ForegroundColor Green
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
