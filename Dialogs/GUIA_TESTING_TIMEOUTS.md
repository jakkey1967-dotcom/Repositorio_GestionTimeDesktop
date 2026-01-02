# 🧪 GUÍA DE TESTING - SOLUCIÓN DE TIMEOUTS

**Fecha:** 2026-01-02  
**Objetivo:** Verificar que la solución de retry automático funciona correctamente  
**Duración estimada:** 15-30 minutos

---

## 🎯 **OBJETIVO DEL TESTING**

Verificar que:
1. ✅ Las peticiones HTTP **ya no se cancelan prematuramente**
2. ✅ El **retry automático funciona** correctamente
3. ✅ Los **logs muestran información detallada** de reintentos
4. ✅ La **experiencia del usuario mejora** (sin errores visibles)

---

## 📋 **PRE-REQUISITOS**

Antes de empezar, asegúrate de:

- [x] **Build exitoso** (ya verificado ✅)
- [ ] **Backend accesible** (https://tu-api.com o Render URL)
- [ ] **Usuario de prueba** con credenciales válidas
- [ ] **Conexión a internet** estable
- [ ] **Visual Studio cerrado** (para ver logs en tiempo real)

---

## 🧪 **TESTS A EJECUTAR**

### **Test 1: Carga Normal (Sin Reintentos) ✅**

**Objetivo:** Verificar que la funcionalidad básica no se rompió.

**Pasos:**
1. Ejecutar la aplicación
2. Hacer login con usuario válido
3. Esperar a que cargue la lista de partes
4. Verificar que se muestra correctamente

**Resultado esperado:**
```log
🌐 ApiClient inicializado - BaseUrl: https://..., Timeout: 120s
📥 CARGA DE PARTES
GET /api/v1/partes?fecha=2025-12-04
📅 2025-12-04: 5 partes recibidos
✅ 150 partes cargados correctamente
```

**✅ PASS si:**
- Lista de partes se carga correctamente
- No aparecen errores en UI
- Logs muestran timeout de 120s

**❌ FAIL si:**
- Lista vacía sin razón
- Errores en UI
- Timeout sigue en 60s

---

### **Test 2: Servidor Lento Simulado (Con Reintentos) ⚡**

**Objetivo:** Forzar reintentos desconectando temporalmente la red.

**Pasos:**
1. Abrir la aplicación
2. **DESACTIVAR WiFi/Ethernet** por 3 segundos
3. **REACTIVAR conexión**
4. Presionar **F5** (refrescar)
5. Observar logs

**Resultado esperado:**
```log
📡 GET /api/v1/partes?fecha=2025-12-04
⚠️ Intento 1/3 fallido para 2025-12-04 - A task was canceled.
   Reintentando en 500ms...
🔄 Reintento 2/3 - GET /api/v1/partes?fecha=2025-12-04
✅ Exitoso en intento 2 para 2025-12-04
```

**✅ PASS si:**
- La app NO muestra error al usuario
- Los logs muestran reintentos
- La lista de partes se carga correctamente después del reintento

**❌ FAIL si:**
- La app muestra error al usuario
- No hay logs de reintento
- La lista queda vacía

---

### **Test 3: Múltiples Peticiones Concurrentes 🔄**

**Objetivo:** Verificar que el retry funciona en carga paralela.

**Pasos:**
1. Abrir la aplicación
2. Cambiar fecha del DatePicker varias veces rápidamente
3. Observar que se cancelan cargas anteriores
4. Esperar a que termine la última carga
5. Revisar logs

**Resultado esperado:**
```log
🔄 Cargando partes día por día (31 peticiones)
Carga de partes cancelada  // Varias veces
📥 CARGA DE PARTES         // Última carga
✅ 150 partes cargados correctamente
```

**✅ PASS si:**
- Solo la última petición se completa
- No hay errores de timeout
- La UI no se congela

**❌ FAIL si:**
- Múltiples errores visibles
- UI congelada
- Logs muestran todos fallando

---

### **Test 4: Cold-Start del Backend (Render) ❄️**

**Objetivo:** Verificar comportamiento con backend en cold-start.

**Pre-condición:** Backend en Render inactivo por >15 minutos.

**Pasos:**
1. **Esperar 15-20 minutos** sin usar la app
2. Volver a abrir la aplicación
3. Hacer login
4. Observar tiempo de carga
5. Revisar logs

**Resultado esperado:**
```log
🌐 ApiClient inicializado - BaseUrl: https://..., Timeout: 120s
📡 GET /api/v1/auth/login
   ⏱️ 8500ms  // Primera petición lenta (cold-start)
✅ Token extraído de JSON response
📡 GET /api/v1/partes?fecha=2025-12-04
   ⏱️ 1200ms  // Peticiones subsiguientes rápidas
✅ 150 partes cargados correctamente
```

**✅ PASS si:**
- Login exitoso (puede tomar 5-10 segundos)
- Partes cargan correctamente después
- No hay errores de timeout

**❌ FAIL si:**
- Login falla por timeout
- Múltiples reintentos necesarios
- Timeout de 120s no es suficiente

**📝 NOTA:** Si falla, considerar aumentar timeout a 180s (3 minutos).

---

### **Test 5: Cierre de Parte (Funcionalidad Crítica) 🔒**

**Objetivo:** Verificar que el cierre de partes funciona con los cambios.

**Pasos:**
1. Abrir un parte existente (estado: Abierto)
2. Click derecho → "Cerrar"
3. Ingresar hora de cierre: `14:30`
4. Click "Cerrar"
5. Observar logs y resultado

**Resultado esperado:**
```log
🔒 CERRAR PARTE - INICIO DEL PROCESO
🎯 PASO 2: Enviar petición de cierre al backend...
🔄 MÉTODO 1: Intentando POST /close
✅ POST /close EXITOSO
   ⏱️ Tiempo de respuesta: 250ms
✅ PROCESO COMPLETADO EXITOSAMENTE
```

**✅ PASS si:**
- Parte se cierra correctamente
- No hay reintentos (si el servidor responde rápido)
- Lista se actualiza mostrando parte cerrado

**❌ FAIL si:**
- Error al cerrar
- Parte no aparece cerrado en lista
- Logs muestran múltiples fallos

---

### **Test 6: Backend Completamente Inaccesible (Escenario Extremo) ❌**

**Objetivo:** Verificar graceful degradation.

**Pasos:**
1. **DESACTIVAR WiFi/Ethernet completamente**
2. Abrir la aplicación
3. Intentar hacer login
4. Observar comportamiento
5. **REACTIVAR conexión**

**Resultado esperado:**
```log
📡 GET /api/v1/auth/login
⚠️ Intento 1/3 fallido - No such host is known
   Reintentando en 500ms...
🔄 Reintento 2/3 - GET /api/v1/auth/login
⚠️ Intento 2/3 fallido - No such host is known
   Reintentando en 1000ms...
🔄 Reintento 3/3 - GET /api/v1/auth/login
❌ Todos los intentos (3) fallaron - Último error: No such host is known
```

**✅ PASS si:**
- App NO se crashea
- Muestra mensaje de error amigable al usuario
- Logs muestran los 3 reintentos
- Al reactivar conexión, funciona normalmente

**❌ FAIL si:**
- App se crashea
- No se ven reintentos
- Mensaje de error confuso

---

## 📊 **MÉTRICAS A RECOPILAR**

Durante el testing, anota:

| Métrica | Valor Esperado | Valor Real | ✅/❌ |
|---------|----------------|------------|-------|
| **Timeout HttpClient** | 120s | ___ | ___ |
| **Reintentos promedio** | 0-1 | ___ | ___ |
| **Tiempo de carga (normal)** | <5s | ___ | ___ |
| **Tiempo de carga (cold-start)** | <15s | ___ | ___ |
| **Tasa de éxito** | >95% | ___ | ___ |
| **Errores visibles al usuario** | 0 | ___ | ___ |

---

## 🐛 **QUÉ HACER SI FALLA UN TEST**

### **Si Test 1 falla (Carga normal):**
1. Verificar conexión a internet
2. Verificar URL del backend
3. Verificar credenciales
4. Revisar logs completos

### **Si Test 2 falla (Reintentos):**
1. Verificar que `maxRetries = 3` en código
2. Verificar logs de `FetchDayLimitedAsync`
3. Aumentar `retryDelay` inicial si es necesario

### **Si Test 3 falla (Concurrencia):**
1. Reducir `SemaphoreSlim` de 3 a 1
2. Aumentar timeout a 180s
3. Implementar throttling adicional

### **Si Test 4 falla (Cold-start):**
1. Aumentar timeout a 180s (3 minutos)
2. Implementar warming request
3. Contactar con backend para optimización

### **Si Test 5 falla (Cierre):**
1. Verificar endpoint `/close` activo
2. Verificar fallback a PUT funciona
3. Revisar logs detallados de cierre

### **Si Test 6 falla (Sin conexión):**
1. Verificar manejo de `OperationCanceledException`
2. Mejorar mensajes de error al usuario
3. Implementar retry exponencial más largo

---

## ✅ **CRITERIOS DE ACEPTACIÓN**

La solución se considera **EXITOSA** si:

- [x] **Build compilado** sin errores ✅
- [ ] **Test 1 PASS** - Carga normal funciona
- [ ] **Test 2 PASS** - Reintentos funcionan
- [ ] **Test 3 PASS** - Concurrencia manejada
- [ ] **Test 4 PASS** - Cold-start tolerado
- [ ] **Test 5 PASS** - Cierre de parte funciona
- [ ] **Test 6 PASS** - Sin conexión manejado gracefully

**Mínimo requerido:** 5 de 6 tests PASS.

---

## 📝 **REPORTE DE TESTING**

Completa este reporte después del testing:

```
╔═══════════════════════════════════════════════════════════════╗
║              REPORTE DE TESTING - SOLUCIÓN TIMEOUTS          ║
╠═══════════════════════════════════════════════════════════════╣
║ Fecha: _______________                                       ║
║ Tester: ______________                                       ║
║ Versión: v1.0                                                ║
╠═══════════════════════════════════════════════════════════════╣
║ Test 1 (Carga normal):        [ ] PASS  [ ] FAIL           ║
║ Test 2 (Reintentos):          [ ] PASS  [ ] FAIL           ║
║ Test 3 (Concurrencia):        [ ] PASS  [ ] FAIL           ║
║ Test 4 (Cold-start):          [ ] PASS  [ ] FAIL           ║
║ Test 5 (Cierre parte):        [ ] PASS  [ ] FAIL           ║
║ Test 6 (Sin conexión):        [ ] PASS  [ ] FAIL           ║
╠═══════════════════════════════════════════════════════════════╣
║ TOTAL PASS: ___ de 6                                         ║
║ RESULTADO GENERAL: [ ] ✅ EXITOSO  [ ] ❌ REQUIERE AJUSTES  ║
╠═══════════════════════════════════════════════════════════════╣
║ OBSERVACIONES:                                                ║
║ _____________________________________________________________║
║ _____________________________________________________________║
║ _____________________________________________________________║
╚═══════════════════════════════════════════════════════════════╝
```

---

## 🚀 **PRÓXIMOS PASOS SEGÚN RESULTADO**

### **Si 6/6 PASS:** ✅
- **Desplegar a producción**
- Monitorear por 48 horas
- Recopilar métricas reales

### **Si 5/6 PASS:** ⚠️
- **Aplicar ajustes menores**
- Re-testear el test fallido
- Desplegar con precaución

### **Si 4/6 o menos PASS:** ❌
- **Revisar logs detallados**
- **Ajustar configuración:**
  - Aumentar timeout a 180s
  - Aumentar maxRetries a 5
  - Reducir concurrencia a 1
- **Re-testear completamente**

---

## 📚 **RECURSOS ADICIONALES**

- **Logs detallados:** `C:\Logs\GestionTime\app.log`
- **Documentación:** `Dialogs/SOLUCION_TIMEOUTS_HTTP.md`
- **Resumen solución:** `Dialogs/RESUMEN_SOLUCION_TIMEOUTS.md`
- **Este archivo:** `Dialogs/GUIA_TESTING_TIMEOUTS.md`

---

**Autor:** GitHub Copilot  
**Fecha:** 2026-01-02  
**Versión:** Guía de Testing v1.0  
**Estado:** ✅ **LISTO PARA EJECUTAR**  
**Duración estimada:** 15-30 minutos
