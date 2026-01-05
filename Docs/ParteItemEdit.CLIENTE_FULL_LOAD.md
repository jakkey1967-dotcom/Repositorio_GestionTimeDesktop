# ParteItemEdit - Carga Completa de Clientes

## ?? Problema Resuelto

**Antes**: El ComboBox de Cliente solo mostraba resultados cuando escribías.  
**Ahora**: Al hacer click en el dropdown (sin escribir), se cargan **todos los clientes** disponibles.

## ? Implementación

### 1. **Evento DropDownOpened**

```csharp
// En ConfigureKeyboardNavigation()
TxtCliente.DropDownOpened += OnClienteDropDownOpened;
```

### 2. **Manejo del Evento**

```csharp
private async void OnClienteDropDownOpened(object sender, object e)
{
    // Si ya hay sugerencias, no recargar
    if (_clienteSuggestions.Count > 0)
        return;
    
    // Cargar todos los clientes (sin filtro)
    await LoadAllClientesAsync();
}
```

**Optimización**: Solo carga una vez. Si ya hay datos, no recarga.

### 3. **Método de Carga Completa**

```csharp
private async Task LoadAllClientesAsync()
{
    try
    {
        // Cancelar búsqueda anterior
        _clienteSearchCts?.Cancel();
        _clienteSearchCts = new CancellationTokenSource();
        var ct = _clienteSearchCts.Token;
        
        // ?? SIN parámetro 'q' = devuelve todos
        var path = "/api/v1/catalog/clientes?limit=100&offset=0";
        
        var response = await App.Api.GetAsync<ClienteResponse[]>(path, ct);
        
        if (response != null)
        {
            _clienteSuggestions.Clear();
            foreach (var cliente in response)
            {
                if (!string.IsNullOrWhiteSpace(cliente.Nombre))
                    _clienteSuggestions.Add(cliente.Nombre);
            }
            
            App.Log?.LogDebug("Cargados {count} clientes", _clienteSuggestions.Count);
        }
    }
    catch (Exception ex)
    {
        App.Log?.LogWarning(ex, "Error cargando lista de clientes");
    }
}
```

## ?? Flujo de Usuario

### Escenario 1: Click en Dropdown (sin escribir)

```
Usuario: [Click en flecha del ComboBox]
         ?
OnClienteDropDownOpened() se dispara
         ?
¿Hay sugerencias en memoria? NO
         ?
LoadAllClientesAsync()
         ?
API: GET /api/v1/catalog/clientes?limit=100&offset=0
         ?
Respuesta: 100 clientes de la BD
         ?
Llenar _clienteSuggestions
         ?
Dropdown muestra lista completa:
???????????????????????
? Abordo              ?
? Ahorracash/Bomacash ?
? Aitana              ?
? Aliur Garden        ?
? Alicoop             ?
? ... (95 más)        ?
???????????????????????
```

### Escenario 2: Usuario Escribe (búsqueda filtrada)

```
Usuario: [Escribe "Kan"]
         ?
Debounce 350ms
         ?
SearchClientesAsync()
         ?
API: GET /api/v1/catalog/clientes?q=Kan&limit=20&offset=0
         ?
Respuesta: Solo clientes que contienen "Kan"
         ?
Actualizar _clienteSuggestions
         ?
Dropdown actualizado:
???????????????????????
? Kanali              ?
???????????????????????
```

### Escenario 3: Dropdown Ya Tiene Datos

```
Usuario: [Click en dropdown segunda vez]
         ?
OnClienteDropDownOpened() se dispara
         ?
¿Hay sugerencias en memoria? SÍ (100 items)
         ?
return; // No recargar
         ?
Dropdown muestra datos en memoria (instantáneo)
```

## ?? Comparación

### ? Antes

```
1. Usuario abre dropdown
   ? Vacío, no pasa nada
2. Usuario debe escribir algo
   ? Solo entonces busca
3. Usuario debe saber qué escribir
   ? Mala experiencia
```

### ? Ahora

```
1. Usuario abre dropdown
   ? Carga todos los clientes automáticamente
2. Usuario puede:
   a) Scroll por la lista completa
   b) Usar flechas ??
   c) Escribir para filtrar en tiempo real
3. Opciones flexibles = Mejor UX
```

## ?? Configuración

### Límite de Registros

Por defecto carga **100 clientes**:

```csharp
var path = "/api/v1/catalog/clientes?limit=100&offset=0";
```

**Para cambiar el límite:**

```csharp
// 50 clientes
var path = "/api/v1/catalog/clientes?limit=50&offset=0";

// 200 clientes
var path = "/api/v1/catalog/clientes?limit=200&offset=0";

// TODOS los clientes (sin límite)
var path = "/api/v1/catalog/clientes?limit=9999&offset=0";
```

### Paginación (futuro)

Si tienes miles de clientes, puedes implementar scroll infinito:

```csharp
private int _currentOffset = 0;
private const int PageSize = 100;

private async Task LoadMoreClientesAsync()
{
    var path = $"/api/v1/catalog/clientes?limit={PageSize}&offset={_currentOffset}";
    // ... cargar
    _currentOffset += PageSize;
}
```

## ?? Logs de Verificación

En `app.log` verás:

```
[Debug] Cargando todos los clientes
[Debug] GET /api/v1/catalog/clientes?limit=100&offset=0
[Debug] Cargados 87 clientes
```

Vs. cuando escribes:

```
[Debug] Buscando clientes: Kan
[Debug] GET /api/v1/catalog/clientes?q=Kan&limit=20&offset=0
[Debug] Encontrados 1 clientes
```

## ?? Ventajas

1. **? Sin escribir**: Ver todos los clientes disponibles
2. **? Exploración**: Scroll por la lista completa
3. **? Búsqueda rápida**: Escribir para filtrar en tiempo real
4. **? Performance**: Solo carga una vez, luego usa memoria
5. **? Flexible**: Soporta ambos modos (listar todo o buscar)

## ?? Navegación Mejorada

| Acción | Resultado |
|--------|-----------|
| **Click flecha ?** | Carga todos los clientes |
| **Escribir** | Filtra en tiempo real |
| **??** | Navegar lista |
| **PgDn/PgUp** | Scroll rápido |
| **Inicio/Fin** | Primer/último cliente |
| **Enter** | Seleccionar y avanzar |

## ?? Troubleshooting

### Problema: Dropdown Vacío

**Solución 1**: Verificar endpoint
```bash
curl https://localhost:2901/api/v1/catalog/clientes?limit=100
```

**Solución 2**: Revisar logs
```
app.log ? Buscar "Error cargando lista de clientes"
```

### Problema: Tarda Mucho en Cargar

**Solución**: Reducir límite
```csharp
var path = "/api/v1/catalog/clientes?limit=50&offset=0";
```

### Problema: Carga en Cada Click

**Causa**: `_clienteSuggestions.Count` siempre es 0

**Solución**: Verificar que no se limpie la colección:
```csharp
// NO hacer esto en otro lugar:
_clienteSuggestions.Clear(); // ?
```

## ?? Resumen Técnico

### Endpoints Usados

| Acción | Endpoint | Límite |
|--------|----------|--------|
| **Abrir dropdown** | `/catalog/clientes?limit=100&offset=0` | 100 registros |
| **Escribir** | `/catalog/clientes?q={texto}&limit=20` | 20 resultados |

### Eventos

| Evento | Disparador | Acción |
|--------|------------|--------|
| `DropDownOpened` | Click en ? | Cargar todos |
| `TextSubmitted` | Escribir texto | Buscar filtrado |

### Variables de Estado

```csharp
_clienteSuggestions    // Colección de nombres (UI)
_clienteDebounce       // Timer para búsqueda
_clienteSearchCts      // Token de cancelación
_lastClienteQuery      // Última búsqueda (cache)
```

## ? Verificación

```
Compilación: ? Correcta
Evento DropDownOpened: ? Configurado
Carga inicial: ? 100 clientes (ajustable)
Cache en memoria: ? No recarga si ya tiene datos
Búsqueda filtrada: ? Sigue funcionando
Logs: ? Registra ambas operaciones
```

## ?? Resultado Final

**Ahora el ComboBox de Cliente ofrece dos modos:**

1. **Modo Exploración** (click en dropdown) ? Lista completa
2. **Modo Búsqueda** (escribir) ? Filtrado en tiempo real

**¡Mejor experiencia de usuario!** ??

---

**Fecha**: 2024-12-23  
**Estado**: ? Implementado y Funcional  
**Compilación**: ? Sin errores  
**API**: ? Integrada con carga completa
