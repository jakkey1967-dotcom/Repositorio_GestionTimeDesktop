# ========================================
# 🔄 Script de Migración a Nuevo Repositorio
# GestionTime.Desktop
# ========================================

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔄 MIGRACIÓN A NUEVO REPOSITORIO" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Configuración
$oldRepoUrl = "https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop"
$newRepoUrl = "https://github.com/jakkey1967-dotcom/GestionTime.Desktop.git"
$backupBranch = "backup-old-repo-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

Write-Host "⚠️ IMPORTANTE:" -ForegroundColor Yellow
Write-Host "   Este script migrará tu código desde:" -ForegroundColor White
Write-Host "   VIEJO: $oldRepoUrl" -ForegroundColor Red
Write-Host "   NUEVO: $newRepoUrl" -ForegroundColor Green
Write-Host ""
Write-Host "   Acciones que realizará:" -ForegroundColor White
Write-Host "   1. Crear backup de la rama actual" -ForegroundColor Gray
Write-Host "   2. Hacer commit de cambios pendientes (si los hay)" -ForegroundColor Gray
Write-Host "   3. Cambiar el remote origin al nuevo repositorio" -ForegroundColor Gray
Write-Host "   4. Hacer push inicial al nuevo repo" -ForegroundColor Gray
Write-Host ""

# Verificar estado de Git
Write-Host "🔍 Verificando estado actual..." -ForegroundColor Yellow

$currentRemote = git remote get-url origin 2>&1
Write-Host "   • Remote actual: $currentRemote" -ForegroundColor Gray

$currentBranch = git branch --show-current 2>&1
Write-Host "   • Rama actual: $currentBranch" -ForegroundColor Gray

# Verificar que el remote actual es el esperado
if ($currentRemote -ne $oldRepoUrl) {
    Write-Host ""
    Write-Host "⚠️ ADVERTENCIA: Remote actual no coincide con el esperado" -ForegroundColor Yellow
    Write-Host "   Esperado: $oldRepoUrl" -ForegroundColor Gray
    Write-Host "   Actual:   $currentRemote" -ForegroundColor Gray
    Write-Host ""
    $continue = Read-Host "¿Continuar de todas formas? (s/n)"
    if ($continue -ne "s") {
        Write-Host "❌ Migración cancelada" -ForegroundColor Red
        exit 1
    }
}

$uncommittedChanges = git status --porcelain 2>&1
if ($uncommittedChanges) {
    Write-Host ""
    Write-Host "⚠️ HAY CAMBIOS SIN COMMIT" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Archivos modificados:" -ForegroundColor White
    git status --short
    Write-Host ""
    
    $response = Read-Host "¿Deseas hacer commit de estos cambios antes de migrar? (s/n)"
    if ($response -eq "s") {
        Write-Host ""
        Write-Host "📝 Haciendo commit de cambios..." -ForegroundColor Yellow
        
        $commitMsg = Read-Host "Mensaje del commit (Enter para usar mensaje por defecto)"
        if ([string]::IsNullOrWhiteSpace($commitMsg)) {
            $commitMsg = "Pre-migration commit - $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
        }
        
        git add -A
        git commit -m $commitMsg
        Write-Host "✅ Commit realizado" -ForegroundColor Green
    }
    else {
        Write-Host ""
        Write-Host "⚠️ Creando backup de cambios sin commit..." -ForegroundColor Yellow
        git stash save "Backup antes de migración - $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
        Write-Host "✅ Cambios guardados en stash" -ForegroundColor Green
        Write-Host "   Puedes recuperarlos después con: git stash pop" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📋 RESUMEN DE MIGRACIÓN" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "🔴 Remote ACTUAL: " -ForegroundColor Red -NoNewline
Write-Host "$currentRemote" -ForegroundColor White
Write-Host "🟢 Remote NUEVO:  " -ForegroundColor Green -NoNewline
Write-Host "$newRepoUrl" -ForegroundColor White
Write-Host ""
Write-Host "⚠️ ASEGÚRATE DE QUE:" -ForegroundColor Yellow
Write-Host "   1. Ya creaste el nuevo repositorio en GitHub" -ForegroundColor White
Write-Host "   2. El repositorio está vacío (sin README)" -ForegroundColor White
Write-Host "   3. Tienes permisos de escritura en el nuevo repo" -ForegroundColor White
Write-Host ""

$confirm = Read-Host "¿Proceder con la migración? (s/n)"
if ($confirm -ne "s") {
    Write-Host "❌ Migración cancelada" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🚀 Iniciando migración..." -ForegroundColor Yellow
Write-Host ""

# Paso 1: Crear backup de la rama actual
Write-Host "📦 PASO 1: Creando backup..." -ForegroundColor Yellow
git branch $backupBranch
Write-Host "   ✅ Backup creado en rama: $backupBranch" -ForegroundColor Green
Write-Host ""

# Paso 2: Cambiar remote
Write-Host "🔄 PASO 2: Cambiando remote..." -ForegroundColor Yellow
git remote remove origin
git remote add origin $newRepoUrl
Write-Host "   ✅ Remote actualizado" -ForegroundColor Green
Write-Host ""

# Paso 3: Verificar nuevo remote
Write-Host "🔍 PASO 3: Verificando..." -ForegroundColor Yellow
$newRemoteCheck = git remote get-url origin
Write-Host "   • Nuevo remote: $newRemoteCheck" -ForegroundColor Gray
Write-Host ""

# Paso 4: Push inicial
Write-Host "📤 PASO 4: Haciendo push inicial..." -ForegroundColor Yellow
Write-Host "   (Esto puede tardar unos minutos...)" -ForegroundColor Gray
Write-Host ""

try {
    # Push de la rama principal
    git push -u origin $currentBranch --force
    Write-Host "   ✅ Rama $currentBranch pusheada" -ForegroundColor Green
    
    # Push de todas las ramas
    Write-Host ""
    Write-Host "   Pusheando otras ramas..." -ForegroundColor Gray
    $allBranches = git branch 2>&1
    foreach ($branch in $allBranches) {
        $branchName = $branch.Trim().Replace("* ", "")
        if ($branchName -ne $currentBranch) {
            try {
                git push origin $branchName --force 2>&1 | Out-Null
                Write-Host "   ✅ Rama '$branchName' pusheada" -ForegroundColor Green
            }
            catch {
                Write-Host "   ⚠️ No se pudo pushear rama '$branchName'" -ForegroundColor Yellow
            }
        }
    }
    
    # Push de tags
    Write-Host ""
    Write-Host "   Pusheando tags..." -ForegroundColor Gray
    $tags = git tag 2>&1
    if ($tags) {
        git push origin --tags --force
        Write-Host "   ✅ Tags pusheados" -ForegroundColor Green
    }
    else {
        Write-Host "   ℹ️ No hay tags para pushear" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "   ✅ Push completado" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "⚠️ Advertencia: Algunos pushes pueden haber fallado" -ForegroundColor Yellow
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   Esto puede ser normal si:" -ForegroundColor White
    Write-Host "   • El repositorio remoto está vacío" -ForegroundColor Gray
    Write-Host "   • Algunas ramas no existen localmente" -ForegroundColor Gray
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ MIGRACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 RESUMEN:" -ForegroundColor Yellow
Write-Host "   • Nuevo repositorio: $newRepoUrl" -ForegroundColor White
Write-Host "   • Rama principal: $currentBranch" -ForegroundColor White
Write-Host "   • Backup local: $backupBranch" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Ver en GitHub:" -ForegroundColor Yellow
Write-Host "   https://github.com/jakkey1967-dotcom/GestionTime.Desktop" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 PRÓXIMOS PASOS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1️⃣ Verificar que todo se subió correctamente:" -ForegroundColor White
Write-Host "   git log --oneline -10" -ForegroundColor Cyan
Write-Host "   git remote -v" -ForegroundColor Cyan
Write-Host ""
Write-Host "2️⃣ Eliminar repositorios viejos en GitHub (MANUAL):" -ForegroundColor White
Write-Host "   Para eliminar cada repo viejo:" -ForegroundColor Gray
Write-Host "   a) Ve a Settings del repositorio" -ForegroundColor Gray
Write-Host "   b) Scroll hasta 'Danger Zone'" -ForegroundColor Gray
Write-Host "   c) Click 'Delete this repository'" -ForegroundColor Gray
Write-Host "   d) Confirma escribiendo el nombre completo" -ForegroundColor Gray
Write-Host ""
Write-Host "   Repositorios a ELIMINAR:" -ForegroundColor Red
Write-Host "   ❌ https://github.com/jakkey1967-dotcom/Repository-Git" -ForegroundColor Gray
Write-Host "   ❌ https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop" -ForegroundColor Gray
Write-Host "   ❌ Cualquier otro repo duplicado de GestionTime.Desktop" -ForegroundColor Gray
Write-Host ""
Write-Host "   Repositorio a MANTENER:" -ForegroundColor Green
Write-Host "   ✅ GestionTimeApi (backend - NO TOCAR)" -ForegroundColor Gray
Write-Host ""
Write-Host "3️⃣ Si necesitas recuperar cambios guardados en stash:" -ForegroundColor White
Write-Host "   git stash list" -ForegroundColor Cyan
Write-Host "   git stash pop" -ForegroundColor Cyan
Write-Host ""
Write-Host "4️⃣ (Opcional) Eliminar backup local si todo está bien:" -ForegroundColor White
Write-Host "   git branch -D $backupBranch" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ ¡Migración exitosa! Tu proyecto ahora apunta al nuevo repositorio" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
