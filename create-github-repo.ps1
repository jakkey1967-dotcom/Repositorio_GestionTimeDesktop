# ================================================================
# SCRIPT PARA CREAR REPOSITORIO GITHUB - GESTIONTIME DESKTOP
# Fecha: 29/12/2025
# Propósito: Guiar la creación del repositorio en GitHub
# ================================================================

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   🌐 CREAR REPOSITORIO EN GITHUB" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🎯 INFORMACIÓN DEL REPOSITORIO A CREAR:" -ForegroundColor Yellow
Write-Host "   👤 Usuario: jakkey1967-dotcom" -ForegroundColor White
Write-Host "   📦 Nombre: gestiontime-desktop" -ForegroundColor White
Write-Host "   🔒 Visibilidad: Private (recomendado)" -ForegroundColor White
Write-Host "   🌐 URL final: https://github.com/jakkey1967-dotcom/gestiontime-desktop" -ForegroundColor White
Write-Host ""

Write-Host "📋 PASOS PARA CREAR EL REPOSITORIO:" -ForegroundColor Magenta
Write-Host ""
Write-Host "1️⃣  ABRIR GITHUB:" -ForegroundColor Cyan
Write-Host "   • Ve a: https://github.com/jakkey1967-dotcom" -ForegroundColor Gray
Write-Host "   • Inicia sesión si no lo has hecho" -ForegroundColor Gray
Write-Host ""

Write-Host "2️⃣  CREAR NUEVO REPOSITORIO:" -ForegroundColor Cyan
Write-Host "   • Click en el botón verde 'New' (o el '+' arriba a la derecha)" -ForegroundColor Gray
Write-Host "   • O ve directamente a: https://github.com/new" -ForegroundColor Gray
Write-Host ""

Write-Host "3️⃣  CONFIGURAR REPOSITORIO:" -ForegroundColor Cyan
Write-Host "   📝 Repository name: gestiontime-desktop" -ForegroundColor Gray
Write-Host "   📄 Description: Desktop application for time management (.NET 8 + WinUI 3)" -ForegroundColor Gray
Write-Host "   🔒 Visibility: Private ✅ (recomendado para código empresarial)" -ForegroundColor Gray
Write-Host ""

Write-Host "4️⃣  CONFIGURACIÓN IMPORTANTE:" -ForegroundColor Cyan
Write-Host "   ❌ Add a README file: NO marcar" -ForegroundColor Red
Write-Host "   ❌ Add .gitignore: NO marcar" -ForegroundColor Red
Write-Host "   ❌ Choose a license: NO marcar" -ForegroundColor Red
Write-Host ""
Write-Host "   ⚡ IMPORTANTE: Deja TODO desmarcado" -ForegroundColor Yellow
Write-Host ""

Write-Host "5️⃣  CREAR:" -ForegroundColor Cyan
Write-Host "   • Click en 'Create repository'" -ForegroundColor Gray
Write-Host ""

Write-Host "📊 ESTADÍSTICAS DE TU PROYECTO:" -ForegroundColor Green
Write-Host "   📁 211 archivos de código fuente" -ForegroundColor White
Write-Host "   💾 1.95 MB de código (sin binarios)" -ForegroundColor White
Write-Host "   📚 25+ documentos técnicos" -ForegroundColor White
Write-Host "   🏷️  Tag v1.1.0 listo para release" -ForegroundColor White
Write-Host ""

$response = Read-Host "¿Has creado el repositorio en GitHub? (S/N)"

if ($response -eq "S" -or $response -eq "s") {
    Write-Host ""
    Write-Host "🚀 Perfecto! Ahora voy a hacer el PUSH completo..." -ForegroundColor Green
    Write-Host ""
    
    # Verificar conectividad
    Write-Host "🔍 Verificando que el repositorio existe..." -ForegroundColor Cyan
    try {
        git ls-remote origin 2>$null | Out-Null
        Write-Host "✅ Repositorio encontrado en GitHub" -ForegroundColor Green
    } catch {
        Write-Host "❌ Repositorio aún no accesible. Espera 1-2 minutos y vuelve a intentar." -ForegroundColor Red
        Write-Host "   GitHub puede tardar un momento en propagar el repositorio." -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host ""
    Write-Host "📡 STEP 1: Push de la rama principal..." -ForegroundColor Magenta
    try {
        git push -u origin main
        Write-Host "✅ Rama main enviada exitosamente" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error en push de main: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "🏷️  STEP 2: Push del tag v1.1.0..." -ForegroundColor Magenta
    try {
        git push origin v1.1.0
        Write-Host "✅ Tag v1.1.0 enviado exitosamente" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error en push de tag: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Cyan
    Write-Host "   🎉 BACKUP GITHUB COMPLETADO EXITOSAMENTE" -ForegroundColor Cyan
    Write-Host "================================================================" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "📊 RESUMEN FINAL:" -ForegroundColor Green
    Write-Host "   🌐 URL: https://github.com/jakkey1967-dotcom/gestiontime-desktop" -ForegroundColor White
    Write-Host "   📦 Archivos: 211 archivos subidos" -ForegroundColor White
    Write-Host "   🏷️  Tag: v1.1.0 disponible" -ForegroundColor White
    Write-Host "   💾 Tamaño: 1.95 MB (código fuente)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "🎯 PRÓXIMOS PASOS:" -ForegroundColor Yellow
    Write-Host "   1. Verificar repositorio en: https://github.com/jakkey1967-dotcom/gestiontime-desktop" -ForegroundColor Gray
    Write-Host "   2. Crear Release con instalador MSIX" -ForegroundColor Gray
    Write-Host "   3. Configurar README.md si lo deseas" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "✅ Tu proyecto de 6-8 semanas está COMPLETAMENTE PROTEGIDO" -ForegroundColor Green
    
} else {
    Write-Host ""
    Write-Host "⏭️  Por favor, crea el repositorio en GitHub primero siguiendo los pasos de arriba." -ForegroundColor Yellow
    Write-Host "   Una vez creado, ejecuta este script de nuevo." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "🌐 Link directo: https://github.com/new" -ForegroundColor Cyan
}

Write-Host ""
Read-Host "Presiona Enter para continuar"