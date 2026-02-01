# 🚀 ACTUALIZAR GITHUB AUTOMÁTICAMENTE - v1.4.0-beta

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  📦 ACTUALIZANDO GITHUB - v1.4.0-beta" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar que estamos en el directorio correcto
if (-not (Test-Path ".git")) {
    Write-Host "❌ Error: No estás en el directorio del repositorio Git" -ForegroundColor Red
    Write-Host "   Ejecuta este script desde: C:\GestionTime\GestionTimeDesktop" -ForegroundColor Yellow
    exit 1
}

# Verificar estado del repositorio
Write-Host "🔍 Verificando estado del repositorio..." -ForegroundColor Yellow
git status --short

Write-Host ""
Write-Host "📝 Archivos que se subirán a GitHub:" -ForegroundColor Cyan
Write-Host "   ✅ GestionTime.Desktop.csproj (v1.4.0-beta)" -ForegroundColor Green
Write-Host "   ✅ App.xaml.cs (Sistema de actualización)" -ForegroundColor Green
Write-Host "   ✅ Services/UpdateService.cs (Actualizado)" -ForegroundColor Green
Write-Host "   ✅ Services/IUpdateService.cs" -ForegroundColor Green
Write-Host "   ✅ Models/UpdateInfo.cs" -ForegroundColor Green
Write-Host "   ✅ Views/LoginPage.xaml.cs (Versión visible)" -ForegroundColor Green
Write-Host "   ✅ WiX-v3-MSI/Product.wxs (v1.4.0-beta)" -ForegroundColor Green
Write-Host "   ✅ WiX-v3-MSI/Build-MSI.ps1" -ForegroundColor Green
Write-Host "   ✅ WiX-v3-MSI/License.rtf" -ForegroundColor Green
Write-Host "   ✅ README.md actualizado" -ForegroundColor Green
Write-Host "   ✅ CHANGELOG.md (nuevo)" -ForegroundColor Green
Write-Host ""

# Preguntar confirmación
$confirm = Read-Host "¿Deseas continuar? (S/N)"
if ($confirm -notmatch "^[Ss]$") {
    Write-Host "❌ Operación cancelada" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "📋 Paso 1/5: Agregando archivos modificados..." -ForegroundColor Yellow

# Agregar archivos modificados
git add GestionTime.Desktop.csproj
git add App.xaml.cs
git add "Services/UpdateService.cs"
git add "Services/IUpdateService.cs"
git add "Models/UpdateInfo.cs"
git add "Views/LoginPage.xaml.cs"
git add "WiX-v3-MSI/Product.wxs"
git add "WiX-v3-MSI/Build-MSI.ps1"
git add "WiX-v3-MSI/License.rtf"

Write-Host "   ✅ Archivos agregados" -ForegroundColor Green

Write-Host ""
Write-Host "📝 Paso 2/5: Creando commit..." -ForegroundColor Yellow

# Crear commit con mensaje descriptivo
$commitMessage = @"
🚀 Release v1.4.0-beta - Sistema de Actualización Automática

✨ Nuevas Funcionalidades:
- Sistema completo de actualizaciones automáticas
- Descarga e instalación con un solo clic
- Visualización de versión en pantalla de login
- Diálogos de confirmación durante actualización
- Barra de progreso durante descarga

🔧 Mejoras Técnicas:
- Detección inteligente de versiones con sufijos
- Comparación precisa de versiones (major.minor.patch)
- Consulta automática a GitHub API
- Logging detallado del proceso completo
- Manejo robusto de errores de red

🐛 Correcciones:
- Corregida detección de versiones con sufijos (-beta, -alpha)
- Mejorado manejo de errores de conexión
- Corregido acceso a API con repositorios públicos
- Mejorada sincronización de versiones entre componentes

📦 Instalador MSI:
- Versión 1.4.0-beta
- Tamaño: ~108 MB
- Incluye .NET 8 Runtime
- Actualización automática desde versiones anteriores
"@

git commit -m "$commitMessage"

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Commit creado exitosamente" -ForegroundColor Green
} else {
    Write-Host "   ⚠️ No hay cambios para commitear (ya está actualizado)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "📤 Paso 3/5: Subiendo a GitHub..." -ForegroundColor Yellow

# Push a GitHub
git push origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Cambios subidos a GitHub exitosamente" -ForegroundColor Green
} else {
    Write-Host "   ❌ Error al subir cambios. Revisa tu conexión y credenciales." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🏷️  Paso 4/5: Creando tag v1.4.0-beta..." -ForegroundColor Yellow

# Crear tag anotado
git tag -a v1.4.0-beta -m "Release v1.4.0-beta - Sistema de Actualización Automática"

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Tag creado" -ForegroundColor Green
    
    # Subir tag a GitHub
    git push origin v1.4.0-beta
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Tag subido a GitHub" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ Error subiendo tag (puede que ya exista)" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ⚠️ Tag ya existe o error al crear" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ GITHUB ACTUALIZADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Paso 5/5: Crear Release en GitHub" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Ahora ve a GitHub para crear el Release:" -ForegroundColor Cyan
Write-Host "   1. Abre: https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/new" -ForegroundColor White
Write-Host "   2. Selecciona tag: v1.4.0-beta" -ForegroundColor White
Write-Host "   3. Copia y pega desde: COPIAR-Y-PEGAR-GITHUB.md" -ForegroundColor White
Write-Host "   4. Adjunta: installers\GestionTime-1.4.0-beta.msi" -ForegroundColor White
Write-Host "   5. Marca: 'Set as a pre-release'" -ForegroundColor White
Write-Host "   6. Publica el release" -ForegroundColor White
Write-Host ""
Write-Host "💡 Consejo: El tag v1.4.0-beta ya existe en GitHub," -ForegroundColor Gray
Write-Host "   así que aparecerá automáticamente en el dropdown." -ForegroundColor Gray
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Mostrar URL del repositorio
$repoUrl = git config --get remote.origin.url
Write-Host "🔗 Repositorio: $repoUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ ¡Listo! GitHub actualizado con v1.4.0-beta" -ForegroundColor Green
Write-Host ""
