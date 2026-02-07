# =========================================================
# Script de diagnóstico de problemas de roles en Settings
# =========================================================
# Investiga por qué los candados muestran lo mismo para USER y ADMIN

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  DIAGNOSTICO: Problema de Roles en SettingsWindow" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Problema reportado:" -ForegroundColor Yellow
Write-Host "   * Con rol USER: Funciona correctamente (candados bien)" -ForegroundColor White
Write-Host "   * Con rol ADMIN: Sale igual (deberia mostrar todos abiertos)" -ForegroundColor White
Write-Host ""

# ============================================================
# PASO 1: Verificar archivo user-info.json
# ============================================================

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  PASO 1: Verificar Archivo de Usuario" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

$userInfoPath = Join-Path $env:LOCALAPPDATA "GestionTime\user-info.json"

if (Test-Path $userInfoPath) {
    Write-Host "OK Archivo encontrado: $userInfoPath" -ForegroundColor Green
    Write-Host ""
    
    try {
        $userInfo = Get-Content $userInfoPath -Raw | ConvertFrom-Json
        
        Write-Host "Contenido del archivo:" -ForegroundColor Cyan
        Write-Host ($userInfo | Format-List | Out-String) -ForegroundColor White
        
        # Verificar valor exacto del rol
        $rolValue = $userInfo.UserRole
        Write-Host "Analisis del campo UserRole:" -ForegroundColor Yellow
        Write-Host "   * Valor: `"$rolValue`"" -ForegroundColor White
        Write-Host "   * Tipo: $($rolValue.GetType().Name)" -ForegroundColor White
        Write-Host "   * Length: $($rolValue.Length)" -ForegroundColor White
        Write-Host "   * ToUpper: `"$($rolValue.ToUpper())`"" -ForegroundColor White
        Write-Host ""
        
        # Verificar si es un valor válido
        $validRoles = @("USER", "EDITOR", "ADMIN")
        $isValid = $validRoles -contains $rolValue.ToUpper()
        
        if ($isValid) {
            Write-Host "OK Rol es VALIDO: $($rolValue.ToUpper())" -ForegroundColor Green
        }
        else {
            Write-Host "ERROR Rol NO es valido: `"$rolValue`"" -ForegroundColor Red
            Write-Host ""
            Write-Host "ADVERTENCIA Problema detectado:" -ForegroundColor Yellow
            Write-Host "   El archivo contiene un rol invalido." -ForegroundColor White
            Write-Host "   Valores validos: USER, EDITOR, ADMIN" -ForegroundColor White
            Write-Host ""
            Write-Host "Posibles causas:" -ForegroundColor Cyan
            Write-Host "   1. Backend devuelve NULL -> UserRoleSafe = `"Usuario`" (string invalido)" -ForegroundColor White
            Write-Host "   2. Backend devuelve string diferente (ej: `"user`" en minuscula)" -ForegroundColor White
            Write-Host "   3. Campo `"role`" no existe en respuesta de login" -ForegroundColor White
            Write-Host ""
        }
    }
    catch {
        Write-Host "ERROR leyendo archivo: $_" -ForegroundColor Red
        Write-Host ""
    }
}
else {
    Write-Host "ERROR Archivo NO encontrado: $userInfoPath" -ForegroundColor Red
    Write-Host "   Por favor, inicia sesion primero." -ForegroundColor Yellow
    Write-Host ""
}

# ============================================================
# PASO 2: Buscar logs de LOGIN
# ============================================================

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  PASO 2: Buscar Logs de Login (UserRole)" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

$logsPath = "C:\App\GestionTime-Desktop\logs"

if (Test-Path $logsPath) {
    Write-Host "Buscando logs en: $logsPath" -ForegroundColor Yellow
    Write-Host ""
    
    # Buscar líneas con "UserRole (de login)"
    $loginLogs = Get-ChildItem $logsPath -Filter "*.log" | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 3 | 
        ForEach-Object { Select-String -Path $_.FullName -Pattern "UserRole \(de login\)" }
    
    if ($loginLogs) {
        Write-Host "OK Logs de login encontrados:" -ForegroundColor Green
        Write-Host ""
        
        foreach ($log in $loginLogs | Select-Object -First 5) {
            Write-Host "   $($log.Line)" -ForegroundColor White
        }
        Write-Host ""
        
        # Analizar el valor del rol en los logs
        $rolePattern = "UserRole \(de login\):\s*(.+)"
        $roles = $loginLogs | ForEach-Object {
            if ($_.Line -match $rolePattern) {
                $matches[1].Trim()
            }
        } | Select-Object -Unique
        
        Write-Host "Roles encontrados en logs:" -ForegroundColor Yellow
        foreach ($r in $roles) {
            Write-Host "   * `"$r`"" -ForegroundColor White
        }
        Write-Host ""
        
        # Verificar si hay roles inválidos
        $invalidRoles = $roles | Where-Object { $_ -notin @("USER", "EDITOR", "ADMIN") }
        if ($invalidRoles) {
            Write-Host "ADVERTENCIA ROLES INVALIDOS DETECTADOS:" -ForegroundColor Red
            foreach ($r in $invalidRoles) {
                Write-Host "   * `"$r`" <- NO es USER, EDITOR ni ADMIN" -ForegroundColor Red
            }
            Write-Host ""
        }
    }
    else {
        Write-Host "ADVERTENCIA No se encontraron logs de login recientes" -ForegroundColor Yellow
        Write-Host ""
    }
}

# ============================================================
# PASO 3: Buscar logs de SETTINGS
# ============================================================

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  PASO 3: Buscar Logs de Settings (Rol Cargado)" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

if (Test-Path $logsPath) {
    # Buscar "Settings iniciado con rol"
    $settingsLogs = Get-ChildItem $logsPath -Filter "*.log" | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 3 | 
        ForEach-Object { Select-String -Path $_.FullName -Pattern "Settings iniciado con rol" }
    
    if ($settingsLogs) {
        Write-Host "OK Logs de Settings encontrados:" -ForegroundColor Green
        Write-Host ""
        
        foreach ($log in $settingsLogs | Select-Object -First 5) {
            Write-Host "   $($log.Line)" -ForegroundColor White
        }
        Write-Host ""
        
        # Buscar también el log de carga desde archivo
        $loadLogs = Get-ChildItem $logsPath -Filter "*.log" | 
            Sort-Object LastWriteTime -Descending | 
            Select-Object -First 3 | 
            ForEach-Object { Select-String -Path $_.FullName -Pattern "Rol de usuario cargado desde archivo" }
        
        if ($loadLogs) {
            Write-Host "Logs de carga desde archivo:" -ForegroundColor Cyan
            Write-Host ""
            
            foreach ($log in $loadLogs | Select-Object -First 5) {
                Write-Host "   $($log.Line)" -ForegroundColor White
            }
            Write-Host ""
        }
    }
    else {
        Write-Host "ADVERTENCIA No se encontraron logs de Settings" -ForegroundColor Yellow
        Write-Host "   Todavia no has abierto Settings (Ctrl+Alt+P)" -ForegroundColor Yellow
        Write-Host ""
    }
}

# ============================================================
# RESUMEN Y RECOMENDACIONES
# ============================================================

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  RECOMENDACIONES" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Acciones recomendadas:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1. Verifica el valor exacto en user-info.json" -ForegroundColor White
Write-Host "   * Si es `"Usuario`" -> Backend devuelve NULL" -ForegroundColor White
Write-Host "   * Si es `"USER`" o `"ADMIN`" -> El problema esta en otro lado" -ForegroundColor White
Write-Host ""

Write-Host "2. Cierra sesion y vuelve a loguearte" -ForegroundColor White
Write-Host "   * Monitorea los logs durante el login" -ForegroundColor White
Write-Host "   * Busca: `"UserRole (de login): [valor]`"" -ForegroundColor White
Write-Host ""

Write-Host "3. Abre Settings y verifica candados" -ForegroundColor White
Write-Host "   * Busca log: `"Settings iniciado con rol: [valor]`"" -ForegroundColor White
Write-Host "   * Compara con el rol esperado" -ForegroundColor White
Write-Host ""

Write-Host "4. Si el problema persiste:" -ForegroundColor White
Write-Host "   * Ejecuta: .\Scripts\Test-Backend-LoginEndpoint.ps1" -ForegroundColor White
Write-Host "   * Verifica respuesta exacta del backend" -ForegroundColor White
Write-Host ""

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  FIN DEL DIAGNOSTICO" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "OK Script completado" -ForegroundColor Green
Write-Host ""
