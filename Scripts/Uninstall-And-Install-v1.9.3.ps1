# SOLUCION RAPIDA - Desinstalar version anterior e instalar nueva
# ================================================================

Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host "  DESINSTALAR GESTIONTIME 1.9.0 E INSTALAR 1.9.3-beta" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Desinstalar version anterior
Write-Host "[1/2] Desinstalando GestionTime Desktop 1.9.0..." -ForegroundColor Yellow

$product = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "GestionTime Desktop" }

if ($product) {
    Write-Host "  Encontrado: $($product.Name) $($product.Version)" -ForegroundColor Gray
    Write-Host "  ProductId: $($product.IdentifyingNumber)" -ForegroundColor Gray
    Write-Host "  Desinstalando (puede tardar 30-60 segundos)..." -ForegroundColor Gray
    
    $result = $product.Uninstall()
    
    if ($result.ReturnValue -eq 0) {
        Write-Host "  [OK] Desinstalacion completada" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Fallo al desinstalar. Codigo: $($result.ReturnValue)" -ForegroundColor Red
        Write-Host ""
        Write-Host "SOLUCION MANUAL:" -ForegroundColor Yellow
        Write-Host "1. Panel de Control -> Programas -> Desinstalar un programa" -ForegroundColor Gray
        Write-Host "2. Buscar 'GestionTime Desktop'" -ForegroundColor Gray
        Write-Host "3. Desinstalar" -ForegroundColor Gray
        exit 1
    }
} else {
    Write-Host "  No hay version anterior instalada" -ForegroundColor Gray
}

Start-Sleep -Seconds 2

# PASO 2: Instalar version nueva
Write-Host ""
Write-Host "[2/2] Instalando GestionTime Desktop 1.9.3-beta..." -ForegroundColor Yellow

$msiPath = "C:\GestionTime\GestionTimeDesktop\installers\GestionTime-v1.9.3-beta.msi"

if (-not (Test-Path $msiPath)) {
    Write-Host "  [ERROR] MSI no encontrado: $msiPath" -ForegroundColor Red
    exit 1
}

Write-Host "  MSI: $msiPath" -ForegroundColor Gray
Write-Host "  Instalando (puede tardar 2-3 minutos)..." -ForegroundColor Gray

# Instalar con UI
$process = Start-Process "msiexec.exe" -ArgumentList "/i `"$msiPath`"" -Wait -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "  [OK] Instalacion completada" -ForegroundColor Green
    Write-Host ""
    Write-Host "=============================================================" -ForegroundColor Cyan
    Write-Host "  INSTALACION EXITOSA" -ForegroundColor Green
    Write-Host "=============================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "VERIFICAR:" -ForegroundColor Yellow
    Write-Host "1. Abrir GestionTime Desktop" -ForegroundColor Gray
    Write-Host "2. Menu -> Ayuda -> Notas de Version" -ForegroundColor Gray
    Write-Host "3. Debe decir: v1.9.3-beta" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "  [ERROR] Instalacion fallo. Codigo: $($process.ExitCode)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Ver logs en:" -ForegroundColor Yellow
    Write-Host "C:\GestionTime\GestionTimeDesktop\installers\install.log" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Presiona cualquier tecla para salir..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
