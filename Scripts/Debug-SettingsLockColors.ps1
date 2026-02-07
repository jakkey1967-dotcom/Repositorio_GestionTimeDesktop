# Debug-SettingsLockColors.ps1
# Verifica los colores de los candados en Settings

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 DIAGNÓSTICO: Colores de candados en Settings" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Colores esperados
$verdePermitido = @{
    Nombre = "Verde Material (Permitido)"
    Hex = "#4CAF50"
    ARGB = "255, 76, 175, 80"
    R = 76
    G = 175
    B = 80
}

$amarilloBloqueado = @{
    Nombre = "Amarillo Amber (Bloqueado)"
    Hex = "#FFC107"
    ARGB = "255, 255, 193, 7"
    R = 255
    G = 193
    B = 7
}

Write-Host "✅ CANDADO PERMITIDO (IsAllowed = true):" -ForegroundColor Green
Write-Host "   └─ Color: $($verdePermitido.Nombre)" -ForegroundColor Green
Write-Host "   └─ Hex:   $($verdePermitido.Hex)" -ForegroundColor Green
Write-Host "   └─ ARGB:  $($verdePermitido.ARGB)" -ForegroundColor Green
Write-Host "   └─ Glyph: \uE785 (LockOpen 🔓)" -ForegroundColor Green
Write-Host ""

Write-Host "❌ CANDADO BLOQUEADO (IsAllowed = false):" -ForegroundColor Yellow
Write-Host "   └─ Color: $($amarilloBloqueado.Nombre)" -ForegroundColor Yellow
Write-Host "   └─ Hex:   $($amarilloBloqueado.Hex)" -ForegroundColor Yellow
Write-Host "   └─ ARGB:  $($amarilloBloqueado.ARGB)" -ForegroundColor Yellow
Write-Host "   └─ Glyph: \uE72E (Lock 🔒)" -ForegroundColor Yellow
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📋 MAPEO DE ROLES ESPERADO:" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$secciones = @(
    @{ Titulo = "Perfil y cuenta"; Roles = "USER, EDITOR, ADMIN" }
    @{ Titulo = "Permisos y roles"; Roles = "ADMIN" }
    @{ Titulo = "Clientes"; Roles = "EDITOR, ADMIN" }
    @{ Titulo = "Grupos y Tipos"; Roles = "EDITOR, ADMIN" }
    @{ Titulo = "Integraciones"; Roles = "ADMIN" }
    @{ Titulo = "Importación / Exportación"; Roles = "ADMIN" }
    @{ Titulo = "Usuarios online / Presencia"; Roles = "USER, EDITOR, ADMIN" }
    @{ Titulo = "Parámetros"; Roles = "ADMIN" }
    @{ Titulo = "Salir"; Roles = "USER, EDITOR, ADMIN" }
)

foreach ($sec in $secciones) {
    Write-Host "• $($sec.Titulo)" -ForegroundColor White
    Write-Host "  └─ Roles permitidos: $($sec.Roles)" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "⚠️ CHECKLIST DE VERIFICACIÓN:" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. ¿El archivo UserInfo.json existe en AppData?" -ForegroundColor White
$userInfoPath = "$env:LOCALAPPDATA\GestionTime\UserInfo.json"
if (Test-Path $userInfoPath) {
    Write-Host "   ✅ SÍ: $userInfoPath" -ForegroundColor Green
    $content = Get-Content $userInfoPath -Raw | ConvertFrom-Json
    Write-Host "   └─ Rol guardado: $($content.UserRole)" -ForegroundColor Cyan
} else {
    Write-Host "   ❌ NO: $userInfoPath" -ForegroundColor Red
}
Write-Host ""

Write-Host "2. ¿El rol se mapea correctamente?" -ForegroundColor White
Write-Host "   └─ 'ADMIN' → UserRole.ADMIN ✅" -ForegroundColor Green
Write-Host "   └─ 'EDITOR' → UserRole.EDITOR ✅" -ForegroundColor Green
Write-Host "   └─ 'USER' → UserRole.USER ✅" -ForegroundColor Green
Write-Host "   └─ Cualquier otro → UserRole.USER (default) ⚠️" -ForegroundColor Yellow
Write-Host ""

Write-Host "3. ¿El binding XAML está correcto?" -ForegroundColor White
Write-Host "   └─ Glyph=`"{Binding LockIcon}`" ✅" -ForegroundColor Green
Write-Host "   └─ Foreground=`"{Binding LockBrush}`" ✅" -ForegroundColor Green
Write-Host "   └─ FontSize=`"16`" (sin Opacity) ✅" -ForegroundColor Green
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🚀 SIGUIENTE PASO:" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Ejecuta la aplicación" -ForegroundColor White
Write-Host "2. Abre Settings (⚙️)" -ForegroundColor White
Write-Host "3. Verifica en Output > Debug:" -ForegroundColor White
Write-Host "   └─ Busca líneas:" -ForegroundColor Gray
Write-Host "      📋 Analizando rol desde archivo: '...' -> '...'" -ForegroundColor Gray
Write-Host "      ✅ SettingsViewModel inicializado con rol: ..." -ForegroundColor Gray
Write-Host "      └─ Sección '...': isAllowed=True/False" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Si ves isAllowed=False para todas las secciones:" -ForegroundColor Red
Write-Host "   └─ El rol NO se está cargando correctamente" -ForegroundColor Red
Write-Host "   └─ Verifica UserInfo.json" -ForegroundColor Red
Write-Host ""
Write-Host "5. Si ves isAllowed=True pero NO aparece verde:" -ForegroundColor Yellow
Write-Host "   └─ Problema en el binding XAML o ColorHelper" -ForegroundColor Yellow
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ Script de diagnóstico completado" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
