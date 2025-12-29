# Segoe MDL2 Assets - Lista Completa de Iconos

## Iconos Disponibles en tu IconHelper.cs
? Ya implementados (24 iconos)

## Iconos Adicionales Útiles para GestionTime

### Gestión de Tiempo
| Código | Glyph | Descripción | Uso Sugerido |
|--------|-------|-------------|--------------|
| E823 | ? | Clock | Ya tienes - Hora/Tiempo |
| E787 | ?? | Calendar | Ya tienes - Fechas |
| EA8D | ?? | Timer | Temporizador/Duración |
| E916 | ?? | Alarm | Recordatorios |
| EB95 | ?? | Chart | Gráficas (para GraficaDiaPage) |

### Navegación
| Código | Glyph | Descripción | Uso Sugerido |
|--------|-------|-------------|--------------|
| E72B | ?? | Back | Ya tienes - Atrás |
| E72A | ?? | Forward | Ya tienes - Adelante |
| E80F | ?? | Home | Ya tienes - Inicio |
| E74A | ?? | Search | Búsqueda de partes |
| E8B3 | ?? | ChevronDown | Desplegables |
| E96E | ?? | ChevronUp | Contraer |

### Estados y Acciones
| Código | Glyph | Descripción | Uso Sugerido |
|--------|-------|-------------|--------------|
| E73E | ? | CheckMark | Ya tienes - Éxito |
| E783 | ?? | Warning | Ya tienes - Error |
| E946 | ?? | Info | Ya tienes - Información |
| E711 | ? | Cancel | Ya tienes - Cancelar |
| E74E | ?? | Save | Ya tienes - Guardar |
| E74D | ??? | Delete | Ya tienes - Eliminar |
| E72C | ?? | Refresh | Recargar datos |

### Filtros y Ordenación
| Código | Glyph | Descripción | Uso Sugerido |
|--------|-------|-------------|--------------|
| E71C | ?? | Sort | Ordenar lista |
| E8CB | ?? | SortUp | Orden ascendente |
| E8CC | ?? | SortDown | Orden descendente |
| E71D | ?? | Filter | Filtrar partes |
| E71E | ?? | FilterClear | Limpiar filtros |

### Reportes y Exportación
| Código | Glyph | Descripción | Uso Sugerido |
|--------|-------|-------------|--------------|
| E8A5 | ?? | Document | Reporte |
| E8C8 | ?? | Copy | Ya tienes - Copiar |
| E8DE | ?? | Export | Exportar datos |
| E8B2 | ??? | Print | Imprimir parte |
| E89E | ?? | Mail | Enviar por correo |

### Usuarios y Permisos
| Código | Glyph | Descripción | Uso Sugerido |
|--------|-------|-------------|--------------|
| E77B | ?? | Contact | Ya tienes - Usuario |
| EA8C | ?? | People | Grupo de usuarios |
| E8D7 | ?? | Permissions | Permisos |
| E7E8 | ?? | SignOut | Ya tienes - Salir |

### Configuración
| Código | Glyph | Descripción | Uso Sugerido |
|--------|-------|-------------|--------------|
| E713 | ?? | Settings | Ya tienes - Configuración |
| E700 | ?? | Color | Ya tienes - Tema |
| E706 | ?? | Volume | Notificaciones |
| E8F1 | ?? | Edit | Editar formulario |

### Tickets y Clientes
| Código | Glyph | Descripción | Uso Sugerido |
|--------|-------|-------------|--------------|
| E8A1 | ?? | Page | Ya tienes - Ticket |
| E8F4 | ?? | Shop | Tienda/Cliente |
| E8B7 | ??? | Tag | Etiquetas/Categorías |
| ECA5 | ?? | Pin | Marcar importante |

### UI y Controles
| Código | Glyph | Descripción | Uso Sugerido |
|--------|-------|-------------|--------------|
| E710 | ? | Add | Ya tienes - Añadir |
| E738 | ? | Remove | Quitar |
| E70F | ?? | Edit | Ya tienes - Editar |
| E8E6 | ??? | View | Ver detalles |
| E890 | ?? | Lock | Bloqueado |
| E785 | ?? | UnLock | Desbloqueado |

## Cómo Usar Estos Iconos

### 1. Agregar al IconHelper.cs
```csharp
/// <summary>Icono de gráfica/chart (&#xEB95;)</summary>
public const string Chart = "\uEB95";

/// <summary>Icono de filtro (&#xE71D;)</summary>
public const string Filter = "\uE71D";

/// <summary>Icono de exportar (&#xE8DE;)</summary>
public const string Export = "\uE8DE";
```

### 2. Usar en XAML
```xaml
<FontIcon Glyph="&#xEB95;" FontSize="20"/>
<!-- O con IconHelper (después de agregarlo) -->
<FontIcon Glyph="{x:Static helpers:IconHelper.Chart}" FontSize="20"/>
```

### 3. Usar en C#
```csharp
myIcon.Glyph = IconHelper.Chart;
```

## Iconos Recomendados para GraficaDiaPage

Para tu página de gráficas, podrías agregar:

```csharp
// En IconHelper.cs
public const string Chart = "\uEB95";          // Gráfica principal
public const string ChartPie = "\uEB05";       // Gráfica de pastel (donut)
public const string CalendarDay = "\uE8BF";    // Día específico
public const string Filter = "\uE71D";          // Filtros
public const string Export = "\uE8DE";          // Exportar reporte
public const string Print = "\uE8B2";           // Imprimir
public const string ZoomIn = "\uE8A3";          // Acercar gráfica
public const string ZoomOut = "\uE71F";         // Alejar gráfica
```

## Iconos Recomendados para DiarioPage

```csharp
// En IconHelper.cs
public const string List = "\uE8FD";            // Vista de lista
public const string CalendarWeek = "\uE8C0";    // Vista semanal
public const string CalendarMonth = "\uE787";   // Vista mensual (ya tienes)
public const string SortDate = "\uE8CB";        // Ordenar por fecha
public const string FilterDate = "\uE787";      // Filtrar por rango
```

## Referencias Completas

- **Galería completa**: https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font
- **Segoe MDL2 Assets**: https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font
- **WinUI 3 Gallery App**: Instalar desde Microsoft Store para ver todos los iconos en vivo

## Búsqueda Rápida

Para encontrar el código hexadecimal de un icono:
1. Abrir **WinUI 3 Gallery** (Microsoft Store)
2. Ir a **Typography** ? **Segoe Fluent Icons Font**
3. Buscar el icono visualmente
4. Copiar el código (ej: `E8C8`)
5. Usar como `\uE8C8` en C# o `&#xE8C8;` en XAML

---

**Fecha**: 2024-12-24  
**Total de iconos en Segoe MDL2**: ~2000+  
**Iconos documentados aquí**: 50+ más útiles
