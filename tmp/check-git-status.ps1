# ================================================================
# VERIFICADOR DE ESTADO GITHUB - GESTIONTIME DESKTOP  
# Fecha: 28/01/2025
# Propósito: Comprobar estado del backup y sincronización
# ================================================================

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? VERIFICADOR DE ESTADO GITHUB BACKUP" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Verificar directorio
$projectPath = "C:\GestionTime\GestionTime.Desktop"
$currentPath = Get-Location

if ($currentPath.Path -ne $projectPath) {
    Write-Host "?? Cambiando al directorio del proyecto..." -ForegroundColor Yellow
    Set-Location $projectPath
}

# ================================================================
# VERIFICACIÓN 1: ESTADO DEL REPOSITORIO LOCAL
# ================================================================

Write-Host "?? VERIFICACIÓN 1: Estado del repositorio local" -ForegroundColor Magenta
Write-Host "????????????????????????????????????????????????" -ForegroundColor Gray

# Verificar si es un repositorio Git
if (-not (Test-Path ".git")) {
    Write-Host "? NO ES UN REPOSITORIO GIT" -ForegroundColor Red
    Write-Host "   Para inicializar: .\setup-git-backup.ps1" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "??  ESTADO: SIN BACKUP CONFIGURADO" -ForegroundColor Red
    return
}

Write-Host "? Es un repositorio Git válido" -ForegroundColor Green

# Información básica
try {
    $currentBranch = git branch --show-current 2>$null
    Write-Host "?? Rama actual: $currentBranch" -ForegroundColor Cyan
    
    $gitUser = git config user.name 2>$null
    $gitEmail = git config user.email 2>$null
    Write-Host "?? Usuario configurado: $gitUser <$gitEmail>" -ForegroundColor White
    
} catch {
    Write-Host "??  Error obteniendo información básica" -ForegroundColor Yellow
}

Write-Host ""

# ================================================================
# VERIFICACIÓN 2: ESTADO DE ARCHIVOS
# ================================================================

Write-Host "?? VERIFICACIÓN 2: Estado de archivos" -ForegroundColor Magenta  
Write-Host "????????????????????????????????????????????????" -ForegroundColor Gray

try {
    # Verificar archivos modificados
    $gitStatus = git status --porcelain 2>$null
    
    if ([string]::IsNullOrWhiteSpace($gitStatus)) {
        Write-Host "? Working directory limpio (sin cambios pendientes)" -ForegroundColor Green
    } else {
        $modifiedFiles = $gitStatus | Measure-Object
        Write-Host "??  $($modifiedFiles.Count) archivos con cambios pendientes:" -ForegroundColor Yellow
        
        git status --porcelain | ForEach-Object {
            $status = $_.Substring(0,2)
            $file = $_.Substring(3)
            $statusText = switch ($status.Trim()) {
                "M" { "?? Modificado" }
                "A" { "? Agregado" }
                "D" { "???  Eliminado" }
                "R" { "?? Renombrado" }
                "??" { "? Sin seguimiento" }
                default { "?? $status" }
            }
            Write-Host "   $statusText : $file" -ForegroundColor Gray
        }
        
        Write-Host "   ?? Ejecuta: git add . && git commit -m 'Update'" -ForegroundColor Yellow
    }
} catch {
    Write-Host "? Error verificando estado de archivos" -ForegroundColor Red
}

Write-Host ""

# ================================================================
# VERIFICACIÓN 3: REMOTE Y CONECTIVIDAD
# ================================================================

Write-Host "?? VERIFICACIÓN 3: Conectividad GitHub" -ForegroundColor Magenta
Write-Host "????????????????????????????????????????????????" -ForegroundColor Gray

try {
    $remotes = git remote -v 2>$null
    
    if ([string]::IsNullOrWhiteSpace($remotes)) {
        Write-Host "??  No hay remotes configurados" -ForegroundColor Yellow
        Write-Host "   Para configurar: git remote add origin https://github.com/USERNAME/repo.git" -ForegroundColor Yellow
    } else {
        Write-Host "? Remotes configurados:" -ForegroundColor Green
        $remotes | ForEach-Object {
            Write-Host "   $($_)" -ForegroundColor Gray
        }
        
        # Verificar conectividad con GitHub
        Write-Host "?? Verificando conectividad..." -ForegroundColor Cyan
        try {
            git ls-remote origin 2>$null | Out-Null
            Write-Host "? Conectividad con GitHub: OK" -ForegroundColor Green
        } catch {
            Write-Host "? Sin conectividad con GitHub" -ForegroundColor Red
            Write-Host "   Verificar: Internet, credenciales, URL del repositorio" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "? Error verificando remotes" -ForegroundColor Red
}

Write-Host ""

# ================================================================
# VERIFICACIÓN 4: HISTORIAL Y COMMITS
# ================================================================

Write-Host "?? VERIFICACIÓN 4: Historial de commits" -ForegroundColor Magenta
Write-Host "????????????????????????????????????????????????" -ForegroundColor Gray

try {
    # Contar commits
    $commitCount = git rev-list --count HEAD 2>$null
    Write-Host "?? Total de commits: $commitCount" -ForegroundColor Cyan
    
    # Último commit
    $lastCommit = git log -1 --oneline 2>$null
    Write-Host "?? Último commit: $lastCommit" -ForegroundColor White
    
    # Commits recientes
    Write-Host "?? Últimos 5 commits:" -ForegroundColor Cyan
    git log --oneline -5 2>$null | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "? Error obteniendo historial" -ForegroundColor Red
}

Write-Host ""

# ================================================================
# VERIFICACIÓN 5: TAGS Y RELEASES  
# ================================================================

Write-Host "???  VERIFICACIÓN 5: Tags y releases" -ForegroundColor Magenta
Write-Host "????????????????????????????????????????????????" -ForegroundColor Gray

try {
    $tags = git tag -l 2>$null
    
    if ([string]::IsNullOrWhiteSpace($tags)) {
        Write-Host "??  No hay tags creados" -ForegroundColor Yellow
        Write-Host "   Para crear: git tag -a v1.0.0 -m 'Release v1.0.0'" -ForegroundColor Yellow
    } else {
        Write-Host "? Tags encontrados:" -ForegroundColor Green
        $tags | ForEach-Object {
            # Obtener fecha del tag
            try {
                $tagDate = git log -1 --format=%ai $_ 2>$null
                Write-Host "   ???  $_ (creado: $tagDate)" -ForegroundColor Gray
            } catch {
                Write-Host "   ???  $_" -ForegroundColor Gray
            }
        }
        
        # Tag más reciente
        $latestTag = git describe --tags --abbrev=0 2>$null
        Write-Host "?? Tag más reciente: $latestTag" -ForegroundColor Cyan
    }
} catch {
    Write-Host "? Error verificando tags" -ForegroundColor Red
}

Write-Host ""

# ================================================================
# VERIFICACIÓN 6: ANÁLISIS DE ARCHIVOS
# ================================================================

Write-Host "?? VERIFICACIÓN 6: Análisis de archivos del proyecto" -ForegroundColor Magenta
Write-Host "????????????????????????????????????????????????" -ForegroundColor Gray

try {
    # Archivos rastreados por Git
    $trackedFiles = git ls-files 2>$null
    $trackedCount = ($trackedFiles | Measure-Object).Count
    Write-Host "?? Archivos rastreados por Git: $trackedCount" -ForegroundColor Cyan
    
    # Análisis por tipo de archivo
    $groupedFiles = $trackedFiles | Group-Object { [System.IO.Path]::GetExtension($_) } | Sort-Object Count -Descending
    
    Write-Host "?? Distribución por tipo de archivo:" -ForegroundColor Cyan
    $groupedFiles | ForEach-Object {
        $extension = if ([string]::IsNullOrWhiteSpace($_.Name)) { "(sin extensión)" } else { $_.Name }
        Write-Host "   $extension : $($_.Count) archivos" -ForegroundColor Gray
    }
    
    # Archivos más grandes
    Write-Host "?? Archivos más grandes (top 5):" -ForegroundColor Cyan
    Get-ChildItem -Recurse | Where-Object { -not $_.PSIsContainer } | Sort-Object Length -Descending | Select-Object -First 5 | ForEach-Object {
        $sizeMB = [math]::Round($_.Length / 1MB, 2)
        if ($sizeMB -gt 0) {
            Write-Host "   $($_.Name): $sizeMB MB" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "? Error analizando archivos" -ForegroundColor Red
}

Write-Host ""

# ================================================================
# VERIFICACIÓN 7: SINCRONIZACIÓN CON GITHUB
# ================================================================

Write-Host "?? VERIFICACIÓN 7: Sincronización con GitHub" -ForegroundColor Magenta
Write-Host "????????????????????????????????????????????????" -ForegroundColor Gray

try {
    # Verificar si hay cambios sin sincronizar
    $hasRemote = git remote 2>$null
    
    if ([string]::IsNullOrWhiteSpace($hasRemote)) {
        Write-Host "??  No hay remote configurado - Solo backup local" -ForegroundColor Yellow
    } else {
        # Comparar con origin/main
        try {
            git fetch origin 2>$null
            
            $ahead = git rev-list --count origin/main..HEAD 2>$null
            $behind = git rev-list --count HEAD..origin/main 2>$null
            
            if ($ahead -eq 0 -and $behind -eq 0) {
                Write-Host "? Sincronizado con GitHub (sin diferencias)" -ForegroundColor Green
            } elseif ($ahead -gt 0 -and $behind -eq 0) {
                Write-Host "??  $ahead commits pendientes de push a GitHub" -ForegroundColor Yellow
                Write-Host "   Ejecutar: git push origin main" -ForegroundColor Yellow
            } elseif ($ahead -eq 0 -and $behind -gt 0) {
                Write-Host "??  $behind commits nuevos en GitHub" -ForegroundColor Yellow  
                Write-Host "   Ejecutar: git pull origin main" -ForegroundColor Yellow
            } else {
                Write-Host "?? $ahead commits locales, $behind commits remotos" -ForegroundColor Yellow
                Write-Host "   Posible divergencia - revisar manualmente" -ForegroundColor Yellow
            }
            
        } catch {
            Write-Host "??  Error verificando sincronización (posible conectividad)" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "? Error verificando sincronización" -ForegroundColor Red
}

Write-Host ""

# ================================================================
# VERIFICACIÓN 8: SALUD DEL REPOSITORIO
# ================================================================

Write-Host "?? VERIFICACIÓN 8: Salud del repositorio" -ForegroundColor Magenta
Write-Host "????????????????????????????????????????????????" -ForegroundColor Gray

try {
    # Verificar integridad
    $fsck = git fsck --quiet 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Integridad del repositorio: OK" -ForegroundColor Green
    } else {
        Write-Host "??  Posibles problemas de integridad detectados" -ForegroundColor Yellow
    }
    
    # Tamaño del repositorio
    if (Test-Path ".git") {
        $gitSize = (Get-ChildItem ".git" -Recurse | Measure-Object Length -Sum).Sum
        $gitSizeMB = [math]::Round($gitSize / 1MB, 2)
        Write-Host "?? Tamaño del repositorio (.git): $gitSizeMB MB" -ForegroundColor Cyan
    }
    
    # Verificar .gitignore
    if (Test-Path ".gitignore") {
        Write-Host "? .gitignore presente" -ForegroundColor Green
    } else {
        Write-Host "??  .gitignore no encontrado" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "? Error verificando salud del repositorio" -ForegroundColor Red
}

Write-Host ""

# ================================================================
# RESUMEN Y RECOMENDACIONES
# ================================================================

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   ?? RESUMEN DEL ESTADO DE BACKUP" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Calcular score de salud
$healthScore = 0
$maxScore = 8

# Scoring
if (Test-Path ".git") { $healthScore++ }
if (-not [string]::IsNullOrWhiteSpace((git remote 2>$null))) { $healthScore++ }
if ((git status --porcelain 2>$null).Length -eq 0) { $healthScore++ }
if (-not [string]::IsNullOrWhiteSpace((git tag -l 2>$null))) { $healthScore++ }
if (Test-Path ".gitignore") { $healthScore++ }
try { git fsck --quiet 2>$null; if ($LASTEXITCODE -eq 0) { $healthScore++ } } catch { }
try { git ls-remote origin 2>$null | Out-Null; $healthScore++ } catch { }
if ((git log --oneline 2>$null | Measure-Object).Count -gt 0) { $healthScore++ }

$healthPercent = [math]::Round(($healthScore / $maxScore) * 100, 0)
$healthColor = if ($healthPercent -ge 80) { "Green" } elseif ($healthPercent -ge 60) { "Yellow" } else { "Red" }

Write-Host "?? SCORE DE SALUD DEL BACKUP: $healthScore/$maxScore ($healthPercent%)" -ForegroundColor $healthColor
Write-Host ""

if ($healthPercent -ge 80) {
    Write-Host "?? EXCELENTE: Tu backup está configurado correctamente" -ForegroundColor Green
    Write-Host "   ? Repositorio saludable" -ForegroundColor Green
    Write-Host "   ? Conectividad con GitHub" -ForegroundColor Green
    Write-Host "   ? Archivos sincronizados" -ForegroundColor Green
} elseif ($healthPercent -ge 60) {
    Write-Host "??  BUENO: Backup funcional con mejoras menores" -ForegroundColor Yellow
} else {
    Write-Host "? NECESITA ATENCIÓN: Problemas críticos detectados" -ForegroundColor Red
}

Write-Host ""

Write-Host "???  ACCIONES RECOMENDADAS:" -ForegroundColor Yellow
if (-not (Test-Path ".git")) {
    Write-Host "   ?? CRÍTICO: Inicializar Git - .\setup-git-backup.ps1" -ForegroundColor Red
}

$gitStatus = git status --porcelain 2>$null
if (-not [string]::IsNullOrWhiteSpace($gitStatus)) {
    Write-Host "   ?? Hacer commit de cambios pendientes" -ForegroundColor Yellow
}

$hasRemote = git remote 2>$null
if ([string]::IsNullOrWhiteSpace($hasRemote)) {
    Write-Host "   ?? Configurar remote de GitHub" -ForegroundColor Yellow
}

try {
    git fetch origin 2>$null
    $ahead = git rev-list --count origin/main..HEAD 2>$null
    if ($ahead -gt 0) {
        Write-Host "   ??  Push cambios a GitHub: git push origin main" -ForegroundColor Yellow
    }
} catch { }

$tags = git tag -l 2>$null
if ([string]::IsNullOrWhiteSpace($tags)) {
    Write-Host "   ???  Crear tag de versión: git tag -a v1.1.0 -m 'Release'" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "?? COMANDOS ÚTILES PARA MANTENIMIENTO:" -ForegroundColor Cyan
Write-Host "   git status                   # Ver estado actual" -ForegroundColor Gray
Write-Host "   git add . && git commit -m 'Update'  # Commit cambios" -ForegroundColor Gray
Write-Host "   git push origin main         # Enviar a GitHub" -ForegroundColor Gray
Write-Host "   git pull origin main         # Obtener cambios" -ForegroundColor Gray
Write-Host "   git tag -a v1.x.x -m 'Release'      # Crear tag" -ForegroundColor Gray
Write-Host ""

Read-Host "Presiona Enter para finalizar verificación"