# 📊 Verificar Logs - Exportación Excel

## 🎯 Objetivo

Verificar en **Output** de Visual Studio que la exportación Excel carga TODO el historial correctamente.

---

## 📝 Logs Esperados

### 1. Usuario Presiona "Exportar Excel"

```
═══════════════════════════════════════════════════════════════
📊 EXPORTAR A EXCEL - Iniciando proceso
📥 Cargando historial completo para exportación...
```

### 2. Carga del Historial Completo

```
📋 Listando partes - Filtros: limit=10000, offset=0
📡 Endpoint: GET /api/v1/partes?limit=10000&offset=0
✅ 1234 partes cargados
```

### 3. Análisis de Datos Cargados

```
✅ Historial cargado: 1234 partes totales
   • Rango de fechas: 2024-01-01 a 2025-01-29
📅 Semanas disponibles: 52
```

**Verificar:**
- ✅ `1234` debe ser el total de partes en la BD (no solo 25)
- ✅ Rango de fechas debe cubrir TODO el historial (varios meses)
- ✅ Semanas disponibles debe ser >>1 (ej: 20-60 semanas)

### 4. Usuario Selecciona Semana

```
✅ Semana seleccionada: Semana 42/2024 (14/10/2024 - 20/10/2024)
📊 Registros a exportar: 42
```

### 5. Exportación

```
📤 Iniciando exportación...
═══════════════════════════════════════════════════════════════
📊 EXPORTACIÓN A EXCEL - Iniciando
   • Archivo destino: C:\Users\...\GestionTime_Semana_2024_42.xlsx
   • Registros a exportar: 42
═══════════════════════════════════════════════════════════════
✅ Encabezados escritos (columnas: 8)
✅ Datos escritos (42 filas)
✅ Autofiltro aplicado
✅ Columnas autoajustadas
✅ Primera fila congelada
✅ Bordes aplicados
✅ Archivo Excel guardado exitosamente
═══════════════════════════════════════════════════════════════
✅ EXPORTACIÓN COMPLETADA EXITOSAMENTE
═══════════════════════════════════════════════════════════════
```

---

## 🔍 Verificación Paso a Paso

### 1. Abrir Output Window en Visual Studio

```
View → Output
```

O presiona: `Ctrl + W, O`

### 2. Seleccionar "Debug" en el dropdown

```
Show output from: [Debug ▼]
```

### 3. Limpiar Output (Opcional)

```
Clic derecho → Clear All
```

### 4. Reproducir el Flujo

1. Inicia la app (F5)
2. Login
3. Ve a DiarioPage
4. Observa logs de carga inicial:
   ```
   📥 CARGA DE PARTES
      • Tipo: CARGA INICIAL - Últimos 25 partes
   ✅ Petición exitosa en 234ms - 25 partes cargados
   ```
   
5. Presiona "Exportar Excel"
6. **CLAVE**: Observa que ahora carga MUCHOS más partes:
   ```
   📥 Cargando historial completo para exportación...
   ✅ 1234 partes cargados  ← ⚠️ DEBE SER >> 25
   ```

7. En el diálogo, verifica semanas disponibles
8. Selecciona una semana ANTIGUA (hace 2-3 meses)
9. Exporta
10. Verifica logs de exportación exitosa

---

## ❌ Logs de Error (Si Algo Falla)

### Error: No hay datos

```
⚠️ No hay datos disponibles para exportar
```

**Causa**: BD vacía o endpoint no responde  
**Solución**: Verificar conexión a backend

### Error: Endpoint devolvió null

```
⚠️ Endpoint devolvió null - Lista vacía
```

**Causa**: Backend no soporta `limit=10000`  
**Solución**: Reducir límite a `limit: 1000` en DiarioPage.xaml.cs línea 1447

### Error: Timeout

```
⚠️ Exportación cancelada por timeout o usuario
```

**Causa**: Carga muy lenta (>30 seg)  
**Solución**: Aumentar timeout en línea 1525:
```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)); // Era 30
```

---

## ✅ Verificación Exitosa

Si ves estos logs, el fix funciona correctamente:

```
✅ Historial cargado: [NÚMERO GRANDE] partes totales    ← > 100
✅ Rango de fechas: [FECHA ANTIGUA] a [FECHA RECIENTE]  ← Varios meses
📅 Semanas disponibles: [NÚMERO GRANDE]                  ← > 10
```

---

## 🧪 Test de Semana Antigua

**Objetivo**: Verificar que se pueden exportar semanas antiguas

1. En el diálogo, busca una semana de hace 3+ meses
2. Ejemplo: "Semana 30/2024 (22/07/2024 - 28/07/2024)"
3. Selecciona y exporta
4. Verifica que el Excel contiene datos de esa semana
5. Abre el Excel y verifica fechas:
   ```
   FECHA       HORA INICIO   ...
   22/07/2024  09:00         ...
   23/07/2024  10:30         ...
   ```

---

## 📊 Benchmark de Performance

| Escenario                | Tiempo Esperado |
|--------------------------|-----------------|
| Carga inicial (25)       | < 1 segundo     |
| Carga historial (1000)   | 1-2 segundos    |
| Carga historial (5000)   | 2-4 segundos    |
| Carga historial (10000)  | 3-6 segundos    |
| Exportación Excel (50)   | < 1 segundo     |
| Exportación Excel (500)  | 1-2 segundos    |

**Nota**: Loader visual se muestra durante la carga, usuario ve feedback claro.

---

## 🔧 Troubleshooting

### Problema: Aún solo muestra 1-2 semanas

**Verificar:**
1. ¿La carga muestra "Historial cargado: 1234 partes"?
   - ❌ No → Backend no responde o BD vacía
   - ✅ Sí → Continuar

2. ¿El número es > 100?
   - ❌ No → BD tiene pocos datos (normal en desarrollo)
   - ✅ Sí → Continuar

3. ¿Las fechas cubren varios meses?
   - ❌ No → Datos solo de última semana
   - ✅ Sí → Fix funciona correctamente

### Problema: Loader no se muestra

**Verificar:**
```csharp
// DiarioPage.xaml.cs línea ~1444
ViewModel.IsBusy = true;
LoadingOverlay.Visibility = Visibility.Visible;
LoadingRing.IsActive = true;
```

### Problema: Carga muy lenta

**Reducir límite:**
```csharp
// DiarioPage.xaml.cs línea ~1447
limit: 5000,  // En vez de 10000
```

---

**Última verificación**: 2025-01-29  
**Test automatizado**: `Scripts\Test-ExportFullHistory.ps1`
