# Sistema de Estados de Partes (v2.0)

## Descripción
Sistema de control de estados para partes de trabajo con 4 estados posibles y acciones desde un icono clicable en el ListView.

## Estados e Iconos

| Estado | Icono | Color | Código | Acciones Disponibles |
|--------|-------|-------|--------|---------------------|
| **ABIERTO** | ?? (E768) | ?? Verde (#10B981) | 0 | Pausar, Cerrar |
| **PAUSADO** | ?? (E769) | ?? Amarillo (#F59E0B) | 1 | Reanudar, Cerrar |
| **CERRADO** | ? (E73E) | ?? Rojo (#EF4444) | 2 | Duplicar |
| **ANULADO** | ? (E711) | ? Gris (#6B7280) | 3 | *(futuro)* |

## Diagrama de Transiciones

```
   ???????????
   ?  NUEVO  ? (al crear)
   ???????????
        ? automático
        ?
   ???????????     Pausar      ???????????
   ? ABIERTO ? ??????????????? ? PAUSADO ?
   ?   ??    ? ??????????????? ?   ??    ?
   ???????????    Reanudar     ???????????
        ?                           ?
        ? Cerrar                    ? Cerrar
        ?                           ?
   ???????????????????????????????????????
   ?              CERRADO                ?
   ?                ?                   ?
   ???????????????????????????????????????
                    ?
                    ? Duplicar
                    ?
              ???????????
              ? ABIERTO ? (nuevo parte)
              ?   ??    ?
              ???????????
```

## Implementación

### ParteDto (`Models/Dtos/ParteDto.cs`)
```csharp
public enum ParteEstado
{
    Abierto = 0,    // En curso activo
    Pausado = 1,    // Temporalmente detenido
    Cerrado = 2,    // Finalizado
    Anulado = 3     // Cancelado (futuro)
}

// Propiedades principales
EstadoParte      // Enum del estado actual
EstadoIcono      // Glyph para FontIcon (\uE768, etc.)
EstadoColor      // Color hex (#10B981, etc.)
EstadoTexto      // "ABIERTO", "PAUSADO", etc.

// Propiedades de permisos
CanPausar        // true si está Abierto
CanReanudar      // true si está Pausado
CanCerrar        // true si está Abierto o Pausado
CanDuplicar      // true si está Cerrado

// Tiempo con pausas
TiempoAcumuladoMin   // Minutos acumulados hasta la última pausa
UltimaReanudacion    // DateTime de última reanudación
```

### Converters (`Helpers/Converters.cs`)
- `EstadoToColorConverter` - Estado ? SolidColorBrush
- `EstadoToIconConverter` - Estado ? Glyph string
- `EstadoToActionVisibilityConverter` - Estado + Acción ? Visibility

### DiarioPage.xaml
- **Una sola columna "Estado"** con icono clicable
- **MenuFlyout** con acciones contextuales según estado
- Eliminada columna "Cerrar" con CheckBox

### DiarioPage.xaml.cs - Handlers de Acciones
```csharp
OnPausarClick()    // ABIERTO ? PAUSADO
OnReanudarClick()  // PAUSADO ? ABIERTO
OnCerrarClick()    // ABIERTO/PAUSADO ? CERRADO
OnDuplicarClick()  // Crea nuevo parte basado en uno cerrado
```

## Flujo de Usuario

### 1. Crear Nuevo Parte
1. Usuario pulsa "Nuevo"
2. Sistema verifica si hay partes ABIERTOS/PAUSADOS
3. Si hay solapes ? Diálogo con opciones:
   - **Cerrar anteriores** ? hora_fin = hora_inicio del nuevo
   - **Mantener abiertos** ? Permite solape
   - **Cancelar** ? No crea nada
4. Abre editor con `hora_inicio = ahora`, `estado = ABIERTO`

### 2. Pausar un Parte (?? ? ??)
1. Click en icono verde ??
2. Seleccionar "?? Pausar"
3. Guarda `tiempo_acumulado_min` hasta el momento
4. Cambia a `estado = PAUSADO`

### 3. Reanudar un Parte (?? ? ??)
1. Click en icono amarillo ??
2. Seleccionar "?? Reanudar"
3. Guarda `ultima_reanudacion = ahora`
4. Cambia a `estado = ABIERTO`

### 4. Cerrar un Parte (??/?? ? ?)
1. Click en icono (verde o amarillo)
2. Seleccionar "? Cerrar"
3. Confirmar en diálogo
4. `hora_fin = ahora`, `estado = CERRADO`
5. Calcula `duracion_min` total

### 5. Duplicar un Parte (? ? ?? nuevo)
1. Click en icono rojo ?
2. Seleccionar "?? Duplicar"
3. Confirmar en diálogo
4. Abre editor con datos copiados:
   - `hora_inicio = ahora`
   - `hora_fin = vacío`
   - `estado = ABIERTO`
   - Cliente, Tienda, Acción, etc. copiados

## Cálculo de Tiempo

### Sin Pausas
```
duracion = hora_fin - hora_inicio
```

### Con Pausas
```
duracion = tiempo_acumulado_min + (ahora - ultima_reanudacion)
```

## Logging

```log
PARTE_PAUSE: Pausando parte {id}
PARTE_RESUME: Reanudando parte {id}
PARTE_CLOSE: Cerrando parte {id} con hora_fin={hora}
PARTE_DUPLICATE: Duplicando parte {id}
SOLAPE_CLOSE_PREV: Cerrando {count} partes por solape
SOLAPE_KEEP_OPEN: Manteniendo partes abiertos (solape permitido)
SOLAPE_CANCEL: Usuario canceló creación
```

## Cambios en Backend Requeridos

### Base de Datos
```sql
ALTER TABLE partes ADD COLUMN estado_parte INTEGER DEFAULT 0;
ALTER TABLE partes ADD COLUMN tiempo_acumulado_min INTEGER DEFAULT 0;
ALTER TABLE partes ADD COLUMN ultima_reanudacion TIMESTAMP NULL;
```

### API - Campos en JSON
```json
{
  "id": 123,
  "estado_parte": 0,
  "is_abierto": true,
  "tiempo_acumulado_min": 45,
  "ultima_reanudacion": "2024-01-15T10:30:00",
  ...
}
```

## Archivos Modificados

| Archivo | Cambios |
|---------|---------|
| `Models/Dtos/ParteDto.cs` | +ParteEstado enum, +EstadoIcono, +EstadoColor, +CanPausar/Reanudar/Cerrar/Duplicar |
| `Helpers/Converters.cs` | +EstadoToColorConverter, +EstadoToIconConverter, +EstadoToActionVisibilityConverter |
| `Views/DiarioPage.xaml` | Columna Estado con icono+MenuFlyout, quitada columna Cerrar |
| `Views/DiarioPage.xaml.cs` | +OnPausarClick, +OnReanudarClick, +OnCerrarClick, +OnDuplicarClick, +Lógica solapes en OnNuevo |
