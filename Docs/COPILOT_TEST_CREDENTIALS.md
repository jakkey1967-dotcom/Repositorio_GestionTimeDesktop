# 📝 CONFIGURACIÓN DE TESTING - GestionTime Desktop

## Script de Diagnóstico de Informes

### Credenciales Predeterminadas (Memorizar)

**Para ejecutar tests del endpoint `/api/v2/informes/resumen`:**

```powershell
# Script automatizado
.\Scripts\Test-InformesEndpoint-Auto.ps1

# Parámetros predeterminados (ya incluidos en el script):
Email:    psantos@global-retail.com
Password: 12345678
BaseUrl:  https://gestiontimeapi.onrender.com
Date:     2026-02-09
```

### Información del Usuario de Testing

- **Nombre completo:** Francisco Santos
- **Email:** psantos@global-retail.com
- **Rol:** ADMIN
- **Password de testing:** 12345678

### Endpoints Relevantes

```
Login:    /api/v1/auth/login-desktop
Informes: /api/v2/informes/resumen
BaseUrl:  https://gestiontimeapi.onrender.com
```

### Uso en Instrucciones de Copilot

Cuando necesites ejecutar tests de endpoints del backend, usa SIEMPRE estos parámetros predeterminados sin solicitar input manual:

```powershell
# Ejemplo de script con credenciales integradas
param(
    [string]$Email = "psantos@global-retail.com",
    [string]$Password = "12345678",
    [string]$BaseUrl = "https://gestiontimeapi.onrender.com"
)
```

### Resultado Esperado (Última ejecución 2026-02-14)

```
[OK] Login exitoso
  Token recibido: eyJhbGciOiJIUzI1NiIsInR5cCI6Ik...
  Usuario: Francisco Santos
  Email: psantos@global-retail.com
  Rol: ADMIN
  SessionId: ac5edeb4-e9bc-4829-9f17-52da787ae4a3

[RESPUESTA DEL ENDPOINT]
  Partes: 11
  Tiempo Registrado: 907 min (15 h 7 m)
  Tiempo Real: 613 min (10 h 13 m)
  Solape: 294 min (4 h 54 m)

[ALERTA] DISCREPANCIA CONFIRMADA
```

### Notas para Copilot

1. **Siempre usar estas credenciales** para tests automáticos
2. **NO solicitar input manual** de contraseña
3. **Endpoint de login correcto:** `/api/v1/auth/login-desktop` (NO `/api/v2/auth/login`)
4. **Token en respuesta:** `accessToken` (NO `token`)
5. **Usuario en respuesta:** `user.fullName` (NO `user.nombre + user.apellidos`)

---

**Última actualización:** 2026-02-14  
**Script de referencia:** `Scripts/Test-InformesEndpoint-Auto.ps1`
