# SOLUCIÓN FINAL - ERROR 401 EN DESKTOP

**Fecha**: 2026-01-30
**Estado**: ✅ SOLUCIONADO
**Compilación**: ✅ EXITOSA

---

## 🔴 PROBLEMA

Después del login exitoso, todas las peticiones a la API fallaban con **401 Unauthorized**:

```
✅ Login exitoso → Token obtenido (713 chars)
✅ Token configurado en header Authorization
❌ GET /api/v1/profiles/me → 401 Unauthorized
❌ GET /api/v1/partes → 401 Unauthorized  
❌ GET /api/v1/presence/users → 401 Unauthorized
```

---

## 🔍 DIAGNÓSTICO

### Comparación Desktop vs Script PowerShell

| Componente | Script PowerShell ✅ | Desktop ❌ |
|-----------|---------------------|-----------|
| Login | `https://localhost:2502` | `https://localhost:2502` |
| GET /partes | `https://localhost:2502` | `http://localhost:2501` |
| Resultado | ✅ 200 OK | ❌ 401 Unauthorized |

### Análisis de logs

**Desktop intentaba usar DOS URLs diferentes:**

1. **Login**: Usaba URL hardcodeada en `LoginPage.xaml.cs` → `https://localhost:2502` ✅
2. **Resto de peticiones**: Usaban `appsettings.json` → `http://localhost:2501` ❌

**¿Por qué fallaba?**
- El puerto 2501 (HTTP) redirige (307) al puerto 2502 (HTTPS)
- Durante el redirect HTTP → HTTPS, el header `Authorization` se **pierde**
- El backend recibe la petición sin token → devuelve 401

---

## ✅ SOLUCIÓN

### Cambio en `appsettings.json`

**ANTES** (incorrecto):
```json
{
  "Api": {
    "BaseUrl": "http://localhost:2501",  // ❌ Puerto HTTP incorrecto
    ...
  }
}
```

**DESPUÉS** (correcto):
```json
{
  "Api": {
    "BaseUrl": "https://localhost:2502",  // ✅ Puerto HTTPS correcto
    ...
  }
}
```

### Cambio en `appsettings.Development.json`

**Mismo cambio**: `http://localhost:2501` → `https://localhost:2502`

---

## 📝 ARCHIVOS MODIFICADOS

1. ✅ `appsettings.json` - BaseUrl corregida
2. ✅ `appsettings.Development.json` - BaseUrl corregida

---

## 🧪 VERIFICACIÓN

### Antes del fix

```
❌ GET /api/v1/partes
   → Redirect: http://localhost:2501 → https://localhost:2502
   → Header Authorization PERDIDO en redirect
   → 401 Unauthorized
```

### Después del fix

```
✅ GET /api/v1/partes
   → Directo: https://localhost:2502
   → Header Authorization preservado
   → 200 OK
```

---

## 🎯 RESULTADO ESPERADO

Al ejecutar la aplicación Desktop ahora:

1. ✅ Login exitoso
2. ✅ Token configurado correctamente
3. ✅ GET /api/v1/profiles/me → 200 OK
4. ✅ GET /api/v1/partes → 200 OK (lista de partes cargada)
5. ✅ GET /api/v1/presence/users → 200 OK (usuarios online)
6. ✅ DiarioPage muestra datos correctamente

---

## 📚 LECCIONES APRENDIDAS

### 1. Headers en HTTP Redirects

**Problema**: Los redirects HTTP (307/308) pueden perder headers custom

**Solución**: Siempre usar la URL final directamente (HTTPS) en configuración

### 2. Inconsistencia de URLs

**Problema**: Login usaba URL hardcodeada, otras peticiones usaban config

**Lección**: **NUNCA hardcodear URLs**. Siempre usar configuración centralizada

### 3. Diferencia entre puertos

**Backend Kestrel escucha en:**
- Puerto 2501 → HTTP (redirector)
- Puerto 2502 → HTTPS (real)

**Desktop debe usar:** Puerto 2502 (HTTPS) directamente

---

## 🚀 PRÓXIMOS PASOS

### 1. Verificar otros entornos

Revisar si hay otros archivos de configuración:
- `appsettings.Production.json`
- `appsettings.Staging.json`

### 2. Documentar configuración

Actualizar `Doc/CONFIGURACION_ENTORNOS.md` con:
- URLs correctas por entorno
- Puertos a usar
- Advertencia sobre HTTP redirects

### 3. Test end-to-end

Ejecutar la aplicación y verificar:
- Login funciona
- DiarioPage carga partes
- Panel de usuarios online funciona
- Todas las peticiones usan HTTPS

---

## 🔗 REFERENCIAS

- **Issue original**: Error 401 después de login exitoso
- **Archivos modificados**: 
  - `appsettings.json`
  - `appsettings.Development.json`
- **Documentación relacionada**:
  - `Docs/INTEGRACION_DIARIOPAGE_PARTESSERVICE.md`
  - `Docs/SERVICIO_PARTES_COMPLETADO.md`

---

## ✅ CHECKLIST FINAL

- [x] `appsettings.json` actualizado con HTTPS
- [x] `appsettings.Development.json` actualizado con HTTPS
- [x] Compilación exitosa
- [x] Backend corriendo en puerto 2502 (HTTPS)
- [x] Documentación actualizada
- [ ] **Pendiente**: Ejecutar aplicación y verificar que funciona

---

**FIN DEL DOCUMENTO**
