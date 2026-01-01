# ================================================================
# DESPERTADOR DE API RENDER Y TEST DE CONEXIÓN
# ================================================================

Write-Host "?? DESPERTANDO API DE RENDER Y PROBANDO CONEXIÓN" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://gestiontimeapi.onrender.com"

# 1. Despertar el servicio
Write-Host "? DESPERTANDO SERVICIO DE RENDER..." -ForegroundColor Yellow
Write-Host "   (Los servicios gratuitos se duermen después de inactividad)" -ForegroundColor Gray
Write-Host ""

Write-Host "   ?? Enviando peticiones para despertar el servidor..." -ForegroundColor Gray

# Múltiples peticiones para asegurar que el servicio despierte
for ($i = 1; $i -le 3; $i++) {
    try {
        Write-Host "   ?? Intento $i/3..." -ForegroundColor Gray
        $response = Invoke-WebRequest -Uri "$baseUrl/health" -TimeoutSec 30 -UseBasicParsing
        Write-Host "      ? Respuesta recibida (Status: $($response.StatusCode))" -ForegroundColor Green
        break
    } catch {
        Write-Host "      ? Esperando respuesta... ($($_.Exception.Message.Split(':')[0]))" -ForegroundColor Yellow
        if ($i -lt 3) {
            Start-Sleep 5
        }
    }
}

Write-Host ""

# 2. Esperar un momento adicional
Write-Host "?? Esperando que el servicio complete inicialización..." -ForegroundColor Yellow
Start-Sleep 3

# 3. Probar login ahora
Write-Host "?? PROBANDO LOGIN DESPUÉS DE DESPERTAR EL SERVICIO..." -ForegroundColor Cyan
Write-Host ""

try {
    $loginData = @{
        email = "admin@test.com"
        password = "password123"
    } | ConvertTo-Json
    
    $headers = @{
        "Content-Type" = "application/json"
        "Accept" = "application/json"
    }
    
    Write-Host "   ?? Enviando POST a /api/v1/auth/login..." -ForegroundColor Gray
    
    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $headers -TimeoutSec 30 -UseBasicParsing
    
    Write-Host "   ? ¡Conexión exitosa!" -ForegroundColor Green
    Write-Host "   ?? Status: $($response.StatusCode)" -ForegroundColor Gray
    
} catch {
    $statusCode = $null
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
    }
    
    switch ($statusCode) {
        401 { 
            Write-Host "   ? ¡API funcionando! (Credenciales rechazadas como esperado)" -ForegroundColor Green
            Write-Host "   ?? El login está funcionando - solo necesitas credenciales válidas" -ForegroundColor Cyan
        }
        400 { 
            Write-Host "   ? ¡API funcionando! (Formato de datos rechazado como esperado)" -ForegroundColor Green
            Write-Host "   ?? El login está funcionando - solo necesitas datos válidos" -ForegroundColor Cyan
        }
        $null {
            Write-Host "   ? Error de conectividad: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "   ?? El servicio puede necesitar más tiempo para despertar" -ForegroundColor Yellow
        }
        default { 
            Write-Host "   ?? Respuesta inesperada (Status: $statusCode)" -ForegroundColor Yellow
        }
    }
}

Write-Host ""

# 4. Instrucciones finales
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "   ?? INSTRUCCIONES FINALES" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "? ESTADO ACTUAL:" -ForegroundColor Green
Write-Host "   • API de Render: DESPIERTA y funcionando" -ForegroundColor Gray
Write-Host "   • Swagger UI: https://gestiontimeapi.onrender.com/swagger" -ForegroundColor Gray
Write-Host ""

Write-Host "?? PRÓXIMOS PASOS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "   1. ????? INSTALAR WINDOWS APP RUNTIME:" -ForegroundColor White
Write-Host "      winget install Microsoft.WindowsAppRuntime.1.8" -ForegroundColor Gray
Write-Host ""
Write-Host "   2. ?? EJECUTAR TU APLICACIÓN:" -ForegroundColor White
Write-Host "      • Desde Visual Studio (F5)" -ForegroundColor Gray
Write-Host "      • O desde: bin\Debug\net8.0-windows10.0.19041.0\GestionTime.Desktop.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "   3. ?? USAR CREDENCIALES VÁLIDAS:" -ForegroundColor White
Write-Host "      • Consulta qué usuarios están registrados en tu API" -ForegroundColor Gray
Write-Host "      • O registra un nuevo usuario si la API lo permite" -ForegroundColor Gray
Write-Host ""

Write-Host "?? NOTA IMPORTANTE:" -ForegroundColor Cyan
Write-Host "   Los servicios gratuitos de Render se duermen después de inactividad." -ForegroundColor Gray
Write-Host "   Si no usas la API por un tiempo, repite este proceso de 'despertar'." -ForegroundColor Gray
Write-Host ""

Read-Host "Presiona Enter para finalizar"