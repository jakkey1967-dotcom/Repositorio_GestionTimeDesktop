# ===============================================
# FIX COMPLETO - Backend + Reinicio
# ===============================================

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "FIX COMPLETO - Backend JWT Authentication" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$programCs = "C:\GestionTime\GestionTimeApi\Program.cs"

# PASO 1: Verificar que el archivo existe
if (-not (Test-Path $programCs)) {
    Write-Host "[ERROR] No se encontro Program.cs en: $programCs" -ForegroundColor Red
    exit 1
}

Write-Host "[1/4] Verificando estado del fix..." -ForegroundColor Yellow

$content = Get-Content $programCs -Raw

# PASO 2: Verificar si el fix ya esta aplicado
if ($content -match "PRIORIDAD 1: Leer desde header Authorization") {
    Write-Host "   [OK] Fix YA ESTA APLICADO" -ForegroundColor Green
} else {
    Write-Host "   [INFO] Fix NO aplicado, aplicando ahora..." -ForegroundColor Yellow
    
    # Backup
    $backupPath = "$programCs.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item $programCs $backupPath
    Write-Host "   [OK] Backup creado: $backupPath" -ForegroundColor Green
    
    # Buscar el bloque a reemplazar
    $oldBlock = @'
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    if (ctx.Request.Cookies.TryGetValue("access_token", out var token))
                        ctx.Token = token;

                    return Task.CompletedTask;
                }
            };
'@

    $newBlock = @'
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    // PRIORIDAD 1: Leer desde header Authorization: Bearer {token}
                    var authHeader = ctx.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Token = authHeader.Substring("Bearer ".Length).Trim();
                        return Task.CompletedTask;
                    }

                    // PRIORIDAD 2: Leer desde cookie (Web)
                    if (ctx.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                    {
                        ctx.Token = cookieToken;
                        return Task.CompletedTask;
                    }

                    return Task.CompletedTask;
                }
            };
'@

    $content = $content -replace [regex]::Escape($oldBlock), $newBlock
    Set-Content $programCs -Value $content -NoNewline
    
    Write-Host "   [OK] Fix aplicado correctamente" -ForegroundColor Green
}

Write-Host ""
Write-Host "[2/4] Buscando proceso del backend..." -ForegroundColor Yellow

# PASO 3: Buscar proceso del backend
$backendProcess = Get-Process -Name "GestionTime.Api" -ErrorAction SilentlyContinue

if ($backendProcess) {
    Write-Host "   [OK] Backend encontrado (PID: $($backendProcess.Id))" -ForegroundColor Green
    Write-Host "   [INFO] Deteniendo backend..." -ForegroundColor Yellow
    
    Stop-Process -Id $backendProcess.Id -Force
    Start-Sleep -Seconds 2
    
    Write-Host "   [OK] Backend detenido" -ForegroundColor Green
} else {
    Write-Host "   [INFO] Backend no estaba ejecutandose" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[3/4] Compilando backend..." -ForegroundColor Yellow

cd C:\GestionTime\GestionTimeApi
$buildOutput = dotnet build --configuration Debug 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   [OK] Compilacion exitosa" -ForegroundColor Green
} else {
    Write-Host "   [ERROR] Error de compilacion" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "[4/4] Iniciando backend..." -ForegroundColor Yellow
Write-Host ""
Write-Host "===============================================" -ForegroundColor Green
Write-Host "BACKEND LISTO - Ejecuta 'dotnet run' ahora" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""
Write-Host "COMANDO:" -ForegroundColor Cyan
Write-Host "   cd C:\GestionTime\GestionTimeApi" -ForegroundColor White
Write-Host "   dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "Luego ejecuta el Desktop y el problema estara resuelto." -ForegroundColor Yellow
Write-Host ""
