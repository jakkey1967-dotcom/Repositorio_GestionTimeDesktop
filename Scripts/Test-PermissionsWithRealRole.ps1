<#
.SYNOPSIS
    Script de testing para verificar el sistema de permisos con roles reales en SettingsWindow.

.DESCRIPTION
    Este script ayuda a verificar que los candados del menú de Settings se muestren
    correctamente según el rol real del usuario (USER, EDITOR, ADMIN).

.NOTES
    Autor: Sistema de Permisos
    Fecha: 2025-02-03
    Fix: FIX_PERMISOS_SETTINGS_ROL_REAL.md
#>

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN: Sistema de Permisos con Rol Real" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# PASO 1: Verificar archivo de usuario actual
# ============================================================

Write-Host "📂 PASO 1: Verificando archivo de información de usuario..." -ForegroundColor Yellow
Write-Host ""

$userInfoPath = Join-Path $env:LOCALAPPDATA "GestionTime\user-info.json"

if (Test-Path $userInfoPath) {
    Write-Host "✅ Archivo encontrado: $userInfoPath" -ForegroundColor Green
    Write-Host ""
    
    try {
        $userInfo = Get-Content $userInfoPath -Raw | ConvertFrom-Json
        
        Write-Host "👤 Información de Usuario:" -ForegroundColor Cyan
        Write-Host "   • Nombre:        $($userInfo.UserName)" -ForegroundColor White
        Write-Host "   • Email:         $($userInfo.UserEmail)" -ForegroundColor White
        Write-Host "   • Rol:           $($userInfo.UserRole)" -ForegroundColor Yellow -NoNewline
        
        # Indicar qué candados debe ver según el rol
        Write-Host "  ◄─── ROL ACTUAL" -ForegroundColor Magenta
        Write-Host "   • Avatar:        $($userInfo.UserAvatar)" -ForegroundColor White
        Write-Host "   • Última actualización: $($userInfo.LastUpdated)" -ForegroundColor White
        Write-Host ""
        
        # Mostrar permisos esperados según el rol
        Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host "  PERMISOS ESPERADOS PARA ROL: $($userInfo.UserRole)" -ForegroundColor Cyan
        Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host ""
        
        switch ($userInfo.UserRole.ToUpper()) {
            "USER" {
                Write-Host "✅ Secciones PERMITIDAS (candado 🔓 verde):" -ForegroundColor Green
                Write-Host "   • Perfil y cuenta" -ForegroundColor Green
                Write-Host "   • Usuarios online / Presencia" -ForegroundColor Green
                Write-Host "   • Salir" -ForegroundColor Green
                Write-Host ""
                Write-Host "❌ Secciones BLOQUEADAS (candado 🔒 amarillo):" -ForegroundColor Red
                Write-Host "   • Permisos y roles" -ForegroundColor Red
                Write-Host "   • Clientes" -ForegroundColor Red
                Write-Host "   • Grupos y Tipos" -ForegroundColor Red
                Write-Host "   • Integraciones" -ForegroundColor Red
                Write-Host "   • Importación / Exportación" -ForegroundColor Red
                Write-Host "   • Parámetros" -ForegroundColor Red
            }
            "EDITOR" {
                Write-Host "✅ Secciones PERMITIDAS (candado 🔓 verde):" -ForegroundColor Green
                Write-Host "   • Perfil y cuenta" -ForegroundColor Green
                Write-Host "   • Clientes" -ForegroundColor Green
                Write-Host "   • Grupos y Tipos" -ForegroundColor Green
                Write-Host "   • Usuarios online / Presencia" -ForegroundColor Green
                Write-Host "   • Salir" -ForegroundColor Green
                Write-Host ""
                Write-Host "❌ Secciones BLOQUEADAS (candado 🔒 amarillo):" -ForegroundColor Red
                Write-Host "   • Permisos y roles" -ForegroundColor Red
                Write-Host "   • Integraciones" -ForegroundColor Red
                Write-Host "   • Importación / Exportación" -ForegroundColor Red
                Write-Host "   • Parámetros" -ForegroundColor Red
            }
            "ADMIN" {
                Write-Host "✅ TODAS las secciones PERMITIDAS (candado 🔓 verde):" -ForegroundColor Green
                Write-Host "   • Perfil y cuenta" -ForegroundColor Green
                Write-Host "   • Permisos y roles" -ForegroundColor Green
                Write-Host "   • Clientes" -ForegroundColor Green
                Write-Host "   • Grupos y Tipos" -ForegroundColor Green
                Write-Host "   • Integraciones" -ForegroundColor Green
                Write-Host "   • Importación / Exportación" -ForegroundColor Green
                Write-Host "   • Usuarios online / Presencia" -ForegroundColor Green
                Write-Host "   • Parámetros" -ForegroundColor Green
                Write-Host "   • Salir" -ForegroundColor Green
            }
            default {
                Write-Host "⚠️ Rol desconocido: $($userInfo.UserRole)" -ForegroundColor Yellow
                Write-Host "   Por defecto se usará: USER (más restrictivo)" -ForegroundColor Yellow
            }
        }
        
        Write-Host ""
    }
    catch {
        Write-Host "❌ Error leyendo archivo JSON: $_" -ForegroundColor Red
        Write-Host ""
    }
}
else {
    Write-Host "❌ Archivo NO encontrado: $userInfoPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "⚠️ Esto significa que:" -ForegroundColor Yellow
    Write-Host "   • No has iniciado sesión todavía" -ForegroundColor Yellow
    Write-Host "   • O el archivo fue eliminado" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "💡 Solución:" -ForegroundColor Cyan
    Write-Host "   1. Inicia sesión en la aplicación" -ForegroundColor Cyan
    Write-Host "   2. Vuelve a ejecutar este script" -ForegroundColor Cyan
    Write-Host ""
}

# ============================================================
# PASO 2: Buscar logs de SettingsViewModel
# ============================================================

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  PASO 2: Buscando logs de SettingsViewModel" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$logsPath = "C:\App\GestionTime-Desktop\logs"

if (Test-Path $logsPath) {
    Write-Host "📂 Buscando en: $logsPath" -ForegroundColor Yellow
    Write-Host ""
    
    # Buscar últimos logs de Settings
    $settingsLogs = Get-ChildItem $logsPath -Filter "*.log" | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 3 | 
        ForEach-Object { Select-String -Path $_.FullName -Pattern "Settings iniciado con rol" -Context 0,2 }
    
    if ($settingsLogs) {
        Write-Host "✅ Logs encontrados:" -ForegroundColor Green
        Write-Host ""
        
        foreach ($log in $settingsLogs) {
            Write-Host "   $($log.Line)" -ForegroundColor White
            foreach ($context in $log.Context.PostContext) {
                Write-Host "   $context" -ForegroundColor Gray
            }
            Write-Host ""
        }
    }
    else {
        Write-Host "⚠️ No se encontraron logs de SettingsViewModel" -ForegroundColor Yellow
        Write-Host "   Esto significa que no has abierto Settings todavía" -ForegroundColor Yellow
        Write-Host ""
    }
}
else {
    Write-Host "❌ Carpeta de logs NO encontrada: $logsPath" -ForegroundColor Red
    Write-Host ""
}

# ============================================================
# PASO 3: Instrucciones de testing
# ============================================================

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  PASO 3: Instrucciones de Testing" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 Para verificar el sistema de permisos:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1️⃣  Abrir Settings:" -ForegroundColor Cyan
Write-Host "   • Presiona Ctrl+Alt+P" -ForegroundColor White
Write-Host "   • O haz click en el botón de configuración" -ForegroundColor White
Write-Host ""

Write-Host "2️⃣  Verificar candados en menú lateral:" -ForegroundColor Cyan
Write-Host "   • 🔓 Verde = Permitido (puedes acceder)" -ForegroundColor Green
Write-Host "   • 🔒 Amarillo = Bloqueado (no puedes acceder)" -ForegroundColor Yellow
Write-Host ""

Write-Host "3️⃣  Intentar acceder a sección bloqueada:" -ForegroundColor Cyan
Write-Host "   • Haz click en una sección con candado 🔒 amarillo" -ForegroundColor White
Write-Host "   • DEBE aparecer InfoBar: 'No tienes permisos...'" -ForegroundColor White
Write-Host "   • NO debe cargar el contenido de esa sección" -ForegroundColor White
Write-Host "   • NO debe ejecutar llamadas API" -ForegroundColor White
Write-Host ""

Write-Host "4️⃣  Verificar logs:" -ForegroundColor Cyan
Write-Host "   • Buscar: 'Settings iniciado con rol'" -ForegroundColor White
Write-Host "   • Buscar: 'Intento de acceso bloqueado'" -ForegroundColor White
Write-Host "   • Comando:" -ForegroundColor White
Write-Host "     Select-String -Path '$logsPath\*.log' -Pattern 'Settings iniciado|acceso bloqueado'" -ForegroundColor Gray
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  CAMBIAR ROL PARA TESTING" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "⚠️ Para probar con diferentes roles:" -ForegroundColor Yellow
Write-Host ""

Write-Host "Opción A - Cambiar en Base de Datos (backend):" -ForegroundColor Cyan
Write-Host "   UPDATE users SET role = 'EDITOR' WHERE email = 'tu_email@example.com';" -ForegroundColor Gray
Write-Host "   Luego: Cerrar sesión → Volver a loguearse" -ForegroundColor White
Write-Host ""

Write-Host "Opción B - Editar archivo directamente (solo para testing):" -ForegroundColor Cyan
Write-Host "   1. Abre: $userInfoPath" -ForegroundColor White
Write-Host "   2. Cambia 'UserRole': 'USER' por 'EDITOR' o 'ADMIN'" -ForegroundColor White
Write-Host "   3. Guarda el archivo" -ForegroundColor White
Write-Host "   4. Cierra y reabre Settings (Ctrl+Alt+P)" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  REPORTAR PROBLEMAS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Si encuentras un problema:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Captura de pantalla del menú Settings con candados" -ForegroundColor White
Write-Host "2. Contenido del archivo user-info.json" -ForegroundColor White
Write-Host "3. Logs que contengan 'Settings iniciado con rol'" -ForegroundColor White
Write-Host "4. Descripción de lo esperado vs lo que ocurre" -ForegroundColor White
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  FIN DEL SCRIPT" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Script completado" -ForegroundColor Green
Write-Host ""
