# ═══════════════════════════════════════════════════════════════
# ANÁLISIS: Leer logs del Output y verificar banner
# ═══════════════════════════════════════════════════════════════
# Busca en los logs de Visual Studio y compara:
#   • Nombre del LOGIN
#   • Nombre del PERFIL (/profiles/me)
# ═══════════════════════════════════════════════════════════════

param(
    [string]$LogFile = $null  # Si no se especifica, pedirá pegarlo
)

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 ANÁLISIS: Banner desde logs de Visual Studio" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ═══════════════════════════════════════════════════════════════
# 1. OBTENER LOGS
# ═══════════════════════════════════════════════════════════════

if (-not $LogFile) {
    Write-Host "📋 PEGA AQUÍ LOS LOGS DEL OUTPUT DE VISUAL STUDIO" -ForegroundColor Yellow
    Write-Host "   (Desde 'Respuesta de login' hasta 'Banner actualizado')" -ForegroundColor Gray
    Write-Host "   Presiona CTRL+Z y ENTER cuando termines:" -ForegroundColor Gray
    Write-Host ""
    
    $logContent = @()
    while ($true) {
        $line = Read-Host
        if ($line -match '\x1A' -or [string]::IsNullOrWhiteSpace($line)) {
            break
        }
        $logContent += $line
    }
    
    $logs = $logContent -join "`n"
} else {
    $logs = Get-Content $LogFile -Raw
}

# ═══════════════════════════════════════════════════════════════
# 2. EXTRAER INFORMACIÓN DEL LOGIN
# ═══════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "📧 Buscando información del LOGIN..." -ForegroundColor Yellow

$loginEmail = ""
$loginName = ""

# Buscar: • UserEmail (del input): wsanchez@global-retail.com
if ($logs -match "UserEmail \(del input\):\s*(.+)") {
    $loginEmail = $Matches[1].Trim()
    Write-Host "   ✅ Email encontrado: $loginEmail" -ForegroundColor Green
}

# Buscar: • UserName (de login): Wilson Sánchez
if ($logs -match "UserName \(de login\):\s*(.+)") {
    $loginName = $Matches[1].Trim()
    Write-Host "   ✅ Nombre (login) encontrado: $loginName" -ForegroundColor Green
}

if ([string]::IsNullOrWhiteSpace($loginName)) {
    Write-Host "   ❌ No se encontró el nombre del login en los logs" -ForegroundColor Red
    exit 1
}

# ═══════════════════════════════════════════════════════════════
# 3. EXTRAER INFORMACIÓN DEL PERFIL (/profiles/me)
# ═══════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "👤 Buscando información del PERFIL..." -ForegroundColor Yellow

$profileName = ""

# Buscar: ✅ Perfil cargado: Francisco Santos | Francisco Santos | 626751367
if ($logs -match "Perfil cargado:\s*([^|]+)\|") {
    $profileName = $Matches[1].Trim()
    Write-Host "   ✅ Nombre (perfil) encontrado: $profileName" -ForegroundColor Green
}

# Buscar alternativo: • DisplayName: Francisco Santos
if ([string]::IsNullOrWhiteSpace($profileName) -and $logs -match "DisplayName:\s*(.+)") {
    $profileName = $Matches[1].Trim()
    Write-Host "   ✅ Nombre (banner) encontrado: $profileName" -ForegroundColor Green
}

if ([string]::IsNullOrWhiteSpace($profileName)) {
    Write-Host "   ❌ No se encontró el nombre del perfil en los logs" -ForegroundColor Red
    exit 1
}

# ═══════════════════════════════════════════════════════════════
# 4. COMPARAR
# ═══════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RESULTADO DEL ANÁLISIS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Login dice:        " -NoNewline -ForegroundColor White
Write-Host "$loginName" -ForegroundColor Green

Write-Host "Banner muestra:    " -NoNewline -ForegroundColor White

if ($loginName -eq $profileName) {
    Write-Host "$profileName" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ ¡CORRECTO! El banner muestra el nombre esperado" -ForegroundColor Green
} else {
    Write-Host "$profileName" -ForegroundColor Red
    Write-Host ""
    Write-Host "❌ ¡ERROR CONFIRMADO! El banner NO muestra el nombre correcto" -ForegroundColor Red
    Write-Host ""
    Write-Host "PROBLEMA:" -ForegroundColor Yellow
    Write-Host "   • Login devuelve:  $loginName" -ForegroundColor Gray
    Write-Host "   • Banner muestra:  $profileName" -ForegroundColor Red
    Write-Host ""
    Write-Host "CAUSA RAÍZ:" -ForegroundColor Yellow
    Write-Host "   → El endpoint /api/v1/profiles/me devuelve el perfil INCORRECTO" -ForegroundColor Red
    Write-Host "   → El backend está devolviendo el perfil de '$profileName'" -ForegroundColor Red
    Write-Host "   → Cuando debería devolver el perfil de '$loginName'" -ForegroundColor Red
    Write-Host ""
    Write-Host "FIX NECESARIO EN EL BACKEND:" -ForegroundColor Cyan
    Write-Host "   1. Verificar que la tabla user_profiles tenga columna user_id" -ForegroundColor White
    Write-Host "   2. Verificar que el endpoint /profiles/me filtre por user_id del token" -ForegroundColor White
    Write-Host "   3. Actualizar la relación en la BBDD si es necesaria" -ForegroundColor White
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📋 INFORMACIÓN TÉCNICA" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Email usado:       $loginEmail" -ForegroundColor Gray
Write-Host "Nombre esperado:   $loginName" -ForegroundColor Gray
Write-Host "Nombre recibido:   $profileName" -ForegroundColor Gray
Write-Host ""
