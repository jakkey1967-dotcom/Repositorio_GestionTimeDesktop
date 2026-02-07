# ====================================================
# Fix-LogoutEndpoint.ps1
# Arregla el endpoint /logout para actualizar presencia
# ====================================================

$authControllerPath = "C:\GestionTime\GestionTimeApi\Controllers\AuthController.cs"

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "FIX: ENDPOINT /LOGOUT DEBE ACTUALIZAR PRESENCIA" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

# Backup
$backupPath = "$authControllerPath.backup-logout"
Copy-Item $authControllerPath $backupPath -Force
Write-Host "Backup creado: $backupPath" -ForegroundColor Gray
Write-Host ""

# Leer contenido
$content = Get-Content $authControllerPath -Raw

# Buscar el método Logout
if ($content -match '\[HttpPost\("logout"\)\]\s+public async Task<IActionResult> Logout\(\)') {
    Write-Host "Metodo Logout encontrado" -ForegroundColor Green
    
    # Reemplazar el método completo
    $oldLogout = @'
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        logger.LogInformation("Logout solicitado{UserInfo}", 
            userId != null ? $" por UserId: {userId}" : "");

        // Revoca el refresh actual (si existe)
        if (Request.Cookies.TryGetValue("refresh_token", out var rawRefresh) && !string.IsNullOrWhiteSpace(rawRefresh))
        {
            var hash = RefreshTokenService.Hash(rawRefresh);

            var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash);
            if (token is not null && token.RevokedAt == null)
            {
                logger.LogDebug("Refresh token revocado");
            }
        }

        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            Path = "/api/v1/auth/refresh"
        });

        logger.LogInformation("Logout completado");

        return Ok(new { message = "bye" });
    }
'@

    $newLogout = @'
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        logger.LogInformation("Logout solicitado{UserInfo}", 
            userId != null ? $" por UserId: {userId}" : "");

        // 1. Revoca el refresh actual (si existe)
        if (Request.Cookies.TryGetValue("refresh_token", out var rawRefresh) && !string.IsNullOrWhiteSpace(rawRefresh))
        {
            var hash = RefreshTokenService.Hash(rawRefresh);

            var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash);
            if (token is not null && token.RevokedAt == null)
            {
                token.RevokedAt = DateTime.UtcNow;
                logger.LogDebug("Refresh token revocado: {TokenId}", token.Id);
            }
        }

        // 2. Revocar TODAS las sesiones activas del usuario (marcar offline inmediatamente)
        if (userId != null && Guid.TryParse(userId, out var userIdGuid))
        {
            var activeSessions = await db.UserSessions
                .Where(s => s.UserId == userIdGuid && s.RevokedAt == null)
                .ToListAsync();
            
            foreach (var session in activeSessions)
            {
                session.RevokedAt = DateTime.UtcNow;
            }
            
            if (activeSessions.Any())
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Revocadas {Count} sesiones activas del usuario {UserId}", activeSessions.Count, userIdGuid);
            }
        }

        // 3. Borrar cookies
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            Path = "/api/v1/auth/refresh"
        });

        logger.LogInformation("Logout completado para UserId: {UserId}", userId);

        return Ok(new { message = "bye" });
    }
'@

    $content = $content.Replace($oldLogout, $newLogout)
    
    # Guardar
    Set-Content $authControllerPath -Value $content -NoNewline
    
    Write-Host "Cambios aplicados:" -ForegroundColor Green
    Write-Host "  1. Refresh token ahora se revoca correctamente (RevokedAt)" -ForegroundColor Gray
    Write-Host "  2. TODAS las sesiones activas se revocan (UserSessions)" -ForegroundColor Gray
    Write-Host "  3. Usuario se marca OFFLINE inmediatamente al hacer logout" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "===================================================================" -ForegroundColor Cyan
    Write-Host "SIGUIENTE PASO:" -ForegroundColor Cyan
    Write-Host "===================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Reinicia el backend (GestionTimeApi)" -ForegroundColor Yellow
    Write-Host "2. Prueba el logout desde el Desktop" -ForegroundColor Yellow
    Write-Host "3. Verifica que el usuario se marca offline inmediatamente" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Ahora el logout SI actualiza la presencia correctamente." -ForegroundColor Green
    Write-Host ""
}
else {
    Write-Host "ERROR: No se encontro el metodo Logout" -ForegroundColor Red
    Write-Host "El archivo puede tener un formato diferente" -ForegroundColor Yellow
    Write-Host ""
}
