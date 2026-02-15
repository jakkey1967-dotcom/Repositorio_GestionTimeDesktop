# INSTRUCCIONES PARA EJECUTAR EL TEST DE INFORMES

## Ejecutar el script de diagnóstico

**Para ejecutar el test automatizado del endpoint de informes:**

```powershell
# Desde la raíz del proyecto
cd C:\GestionTime\GestionTimeDesktop

# Ejecutar con parámetros predeterminados (recomendado)
.\Scripts\Test-InformesEndpoint-Auto.ps1

# O con parámetros personalizados:
.\Scripts\Test-InformesEndpoint-Auto.ps1 -Email "otro@email.com" -Password "otrapass" -Date "2026-02-10"
```

### 🔐 Credenciales predeterminadas (NO requiere input manual):
- **Email:** `psantos@global-retail.com` (Usuario ADMIN)
- **Password:** `12345678` (incluido en el script)
- **BaseUrl:** `https://gestiontimeapi.onrender.com`
- **Fecha a diagnosticar:** `2026-02-09`

> ⚠️ **Nota de seguridad:** La contraseña está hardcodeada solo para testing. En producción usar variables de entorno o input seguro.

### ✅ Resultado del último test (2026-02-14):

```
PASO 1: Autenticacion...
============================
Endpoint: https://gestiontimeapi.onrender.com/api/v1/auth/login-desktop
[OK] Login exitoso
  Token recibido: eyJhbGciOiJIUzI1NiIsInR5cCI6Ik...
  Usuario: Francisco Santos
  Email: psantos@global-retail.com
  Rol: ADMIN

PASO 2: Consultar Informes...
================================
Endpoint: https://gestiontimeapi.onrender.com/api/v2/informes/resumen?scope=day&date=2026-02-09

RESUMEN GENERAL:
  Partes: 11
  Tiempo Registrado: 907 min (15 h 7 m)
  Tiempo Real (sin solape): 613 min (10 h 13 m)
  Solape: 294 min (4 h 54 m)
  Inicio Global: 2026-02-09T07:55:00
  Fin Global: 2026-02-09T19:38:00

INTERVALOS CUBIERTOS (unidos, sin solape):
  2026-02-09T07:55:00 - 2026-02-09T13:30:00 (335 min)
  2026-02-09T15:00:00 - 2026-02-09T19:38:00 (278 min)
  Total cubierto: 613 min (10 h 13 m)

ANALISIS DE DISCREPANCIA:
====================================================
Comparacion con datos esperados:

  [ERROR] Partes: 11 (esperado: 5)
     Diferencia: +6 partes (+120 %)
  [ERROR] Tiempo Cubierto: 613 min (esperado: 510 min)
     Diferencia: +103 min (+1 h 43 m)

[ALERTA] DISCREPANCIA CONFIRMADA
```

### Qué hace el script:

1. **Login automático** en el backend
2. **Obtiene token JWT**
3. **Consulta el endpoint** `/api/v2/informes/resumen?scope=day&date=2026-02-09`
4. **Muestra la respuesta** detallada:
   - Número de partes
   - Tiempo registrado
   - Tiempo cubierto (sin solape)
   - Solape detectado
   - Intervalos cubiertos
   - Huecos (gaps)
5. **Compara con datos esperados**:
   - Esperado: 5 partes, 510 min (8h 30m)
   - Si hay discrepancia, la marca claramente
6. **Muestra JSON completo** de la respuesta

### Resultado esperado:

**Si hay discrepancia** (como se observa actualmente):
```
[ALERTA] DISCREPANCIA CONFIRMADA:
  [ERROR] Partes: 11 (esperado: 5)
     Diferencia: +6 partes (+120 %)
  [ERROR] Tiempo Cubierto: 613 min (esperado: 510 min)
     Diferencia: +103 min (+1 h 43 m)
```

**Si los datos son correctos**:
```
[OK] LOS DATOS COINCIDEN CON LO ESPERADO
  [OK] Partes: 5 (esperado: 5)
  [OK] Tiempo Cubierto: 510 min (esperado: 510 min)
```

### Logs adicionales

El frontend ahora también genera logs detallados en:
- Archivo: `logs/GestionTime_Desktop.log`
- Buscar: `[InformesService]`

Ejemplo de logs:
```
📊 [InformesService] Iniciando GetResumenAsync - Scope: day, Date: 2026-02-09, ...
📊 [InformesService] Endpoint construido: /api/v2/informes/resumen?scope=day&date=2026-02-09
📊 [InformesService] Respuesta recibida - Partes: 11, Registrado: 907min, Real: 613min, ...
📊 [InformesService] Intervalos cubiertos: 4
  ↳ 2026-02-09T07:55:00 - 2026-02-09T13:30:00 (335min)
  ↳ ...
```

### Próximos pasos según resultado:

1. ✅ **Si confirma discrepancia**: Investigar backend
   - Revisar `GestionTimeApi/Controllers/InformesController.cs`
   - Verificar query SQL para duplicados
   - Comparar con `/api/v2/partes/intervalos-cubiertos` (que funciona)

2. ✅ **Si datos son correctos**: Investigar frontend
   - Verificar caché
   - Revisar lógica de presentación

---

**Nota:** El script fue corregido para eliminar caracteres UTF-8 especiales (emojis, símbolos) que causaban errores en PowerShell.
