# 📦 Sistema de Control de Versiones de Clientes

## Resumen

Sistema para rastrear qué versión de la aplicación usa cada cliente al hacer login.
El backend puede verificar si el cliente está actualizado y notificar si hay actualizaciones.

---

## 🖥️ Frontend (Implementado)

### 1. Header en TODAS las peticiones HTTP

Cada petición del `ApiClient` incluye automáticamente:
```
X-App-Version: 2.0.0-beta
X-App-Platform: Desktop
```

**El backend puede leer estos headers desde cualquier request** para registrar la versión.

### 2. Campo `appVersion` en el Login

`POST /api/v1/auth/login` ahora envía:
```json
{
  "email": "user@company.com",
  "password": "***",
  "appVersion": "2.0.0-beta",
  "platform": "Desktop"
}
```

### 3. Endpoint dedicado (POST /api/v1/client-version)

Después de un login exitoso, el cliente llama:
```
POST /api/v1/client-version
Authorization: Bearer <token>
```

**Body:**
```json
{
  "appVersion": "2.0.0-beta",
  "platform": "Desktop",
  "osVersion": "Microsoft Windows NT 10.0.19045.0",
  "machineName": "PC-OFICINA01"
}
```

**Respuesta esperada:**
```json
{
  "ok": true,
  "updateRequired": false,
  "latestVersion": "2.0.0-beta",
  "updateUrl": null,
  "message": null
}
```

Si hay actualización obligatoria:
```json
{
  "ok": true,
  "updateRequired": true,
  "latestVersion": "2.1.0",
  "updateUrl": "https://github.com/.../releases/latest",
  "message": "Hay una versión más reciente disponible. Por favor, actualiza."
}
```

---

## 🔧 Backend (Por implementar)

### Tabla SQL sugerida

```sql
CREATE TABLE client_versions (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id),
    app_version VARCHAR(50) NOT NULL,
    platform VARCHAR(20) NOT NULL DEFAULT 'Desktop',
    os_version VARCHAR(100),
    machine_name VARCHAR(100),
    logged_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    -- Índices
    CONSTRAINT fk_client_versions_user FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_client_versions_user ON client_versions(user_id);
CREATE INDEX idx_client_versions_logged_at ON client_versions(logged_at DESC);
CREATE INDEX idx_client_versions_version ON client_versions(app_version);
```

### Tabla de versión mínima requerida

```sql
CREATE TABLE app_settings (
    key VARCHAR(100) PRIMARY KEY,
    value VARCHAR(500) NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Insertar versión mínima
INSERT INTO app_settings (key, value) VALUES ('min_client_version', '2.0.0');
INSERT INTO app_settings (key, value) VALUES ('latest_client_version', '2.0.0-beta');
INSERT INTO app_settings (key, value) VALUES ('update_url', 'https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/latest');
```

### Controller sugerido

```csharp
[ApiController]
[Route("api/v1/client-version")]
[Authorize]
public class ClientVersionController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterVersion([FromBody] RegisterVersionRequest request)
    {
        var userId = GetCurrentUserId(); // Extraer del token JWT
        
        // 1. Guardar en base de datos
        await _db.ClientVersions.AddAsync(new ClientVersion
        {
            UserId = userId,
            AppVersion = request.AppVersion,
            Platform = request.Platform,
            OsVersion = request.OsVersion,
            MachineName = request.MachineName,
            LoggedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        
        // 2. Verificar si necesita actualización
        var minVersion = await _db.AppSettings
            .Where(s => s.Key == "min_client_version")
            .Select(s => s.Value)
            .FirstOrDefaultAsync() ?? "1.0.0";
            
        var latestVersion = await _db.AppSettings
            .Where(s => s.Key == "latest_client_version")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
            
        var updateUrl = await _db.AppSettings
            .Where(s => s.Key == "update_url")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        
        var needsUpdate = CompareVersions(request.AppVersion, minVersion) < 0;
        
        return Ok(new
        {
            ok = true,
            updateRequired = needsUpdate,
            latestVersion = latestVersion,
            updateUrl = needsUpdate ? updateUrl : null,
            message = needsUpdate 
                ? $"Tu versión ({request.AppVersion}) es anterior a la mínima requerida ({minVersion}). Actualiza la aplicación."
                : null
        });
    }
    
    /// <summary>GET para admin: ver versiones de todos los clientes.</summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllClientVersions()
    {
        var versions = await _db.ClientVersions
            .Include(cv => cv.User)
            .GroupBy(cv => cv.UserId)
            .Select(g => g.OrderByDescending(cv => cv.LoggedAt).First())
            .ToListAsync();
            
        return Ok(versions.Select(cv => new
        {
            userId = cv.UserId,
            userName = cv.User.FullName,
            userEmail = cv.User.Email,
            appVersion = cv.AppVersion,
            platform = cv.Platform,
            osVersion = cv.OsVersion,
            machineName = cv.MachineName,
            lastLogin = cv.LoggedAt
        }));
    }
    
    /// <summary>Compara dos versiones semánticas (sin sufijo).</summary>
    private static int CompareVersions(string? v1, string? v2)
    {
        if (string.IsNullOrEmpty(v1)) return -1;
        if (string.IsNullOrEmpty(v2)) return 1;
        
        // Eliminar sufijos (-beta, -rc, etc.)
        var clean1 = v1.Split('-')[0];
        var clean2 = v2.Split('-')[0];
        
        if (Version.TryParse(clean1, out var ver1) && Version.TryParse(clean2, out var ver2))
            return ver1.CompareTo(ver2);
            
        return string.Compare(clean1, clean2, StringComparison.Ordinal);
    }
}
```

### Opción alternativa: Leer del Header (sin endpoint nuevo)

El backend puede leer la versión desde el header en **cualquier** middleware:

```csharp
// En un middleware o filtro global
public class ClientVersionMiddleware
{
    public async Task InvokeAsync(HttpContext context, IClientVersionService versionService)
    {
        var appVersion = context.Request.Headers["X-App-Version"].FirstOrDefault();
        var platform = context.Request.Headers["X-App-Platform"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(appVersion))
        {
            var userId = context.User?.FindFirst("sub")?.Value;
            if (userId != null)
            {
                // Registrar asíncronamente (fire & forget, no bloquear request)
                _ = versionService.TrackVersionAsync(userId, appVersion, platform);
            }
        }
        
        await _next(context);
    }
}
```

---

## 📊 Consultas útiles para Admin

```sql
-- Ver última versión de cada usuario
SELECT DISTINCT ON (user_id)
    u.email, u.full_name, cv.app_version, cv.platform, 
    cv.os_version, cv.machine_name, cv.logged_at
FROM client_versions cv
JOIN users u ON u.id = cv.user_id
ORDER BY user_id, cv.logged_at DESC;

-- Usuarios con versión desactualizada
SELECT u.email, u.full_name, cv.app_version, cv.logged_at
FROM client_versions cv
JOIN users u ON u.id = cv.user_id
WHERE cv.id = (SELECT MAX(id) FROM client_versions WHERE user_id = cv.user_id)
  AND cv.app_version < (SELECT value FROM app_settings WHERE key = 'min_client_version');

-- Distribución de versiones
SELECT app_version, COUNT(DISTINCT user_id) as users_count
FROM client_versions cv
WHERE cv.id = (SELECT MAX(id) FROM client_versions c2 WHERE c2.user_id = cv.user_id)
GROUP BY app_version
ORDER BY app_version DESC;
```

---

## 📁 Archivos modificados/creados (Frontend)

| Archivo | Cambio |
|---------|--------|
| `Models/Dtos/ClientVersionDto.cs` | 🆕 DTO + Response |
| `Services/ClientVersionService.cs` | 🆕 Servicio de registro |
| `Services/ApiClient.cs` | ✏️ Headers X-App-Version/Platform, campo appVersion en LoginRequest |
| `Views/LoginPage.xaml.cs` | ✏️ Llamada a RegisterVersionAsync() post-login |

---

## ✅ Estado

- [x] Frontend: Headers automáticos en toda petición
- [x] Frontend: Versión en LoginRequest
- [x] Frontend: Servicio ClientVersionService
- [x] Frontend: Llamada post-login (fire & forget, no bloqueante)
- [x] Frontend: Notificación si `updateRequired = true`
- [ ] Backend: Crear tabla `client_versions`
- [ ] Backend: Crear tabla `app_settings` (o similar)
- [ ] Backend: Implementar `POST /api/v1/client-version`
- [ ] Backend: Implementar `GET /api/v1/client-version/all` (admin)
- [ ] Backend: Leer `appVersion` del login y guardar
