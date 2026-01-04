# 📋 TABLA DE REFERENCIA RÁPIDA - RESPONSIVE

**Para consulta rápida mientras implementas**

---

## 🎯 BREAKPOINTS

| Nombre | Tamaño | AdaptiveTrigger | Uso Típico |
|--------|--------|-----------------|------------|
| **Wide** | >1400px | `MinWindowWidth="1400"` | Monitores 1920x1080, 4K |
| **Normal** | 1024-1399px | `MinWindowWidth="1024"` | Laptops 1366x768 |
| **Narrow** | <1024px | `MinWindowWidth="0"` | Tablets, ventanas pequeñas |

---

## 🔧 SETTERS COMUNES

### **Visibilidad**

```xml
<!-- Ocultar (no ocupa espacio) -->
<Setter Target="ElementoNombre.Visibility" Value="Collapsed"/>

<!-- Mostrar -->
<Setter Target="ElementoNombre.Visibility" Value="Visible"/>
```

### **Tamaño**

```xml
<!-- Ancho de columna -->
<Setter Target="Col_Nombre.Width" Value="70"/>
<Setter Target="Col_Nombre.Width" Value="*"/>
<Setter Target="Col_Nombre.Width" Value="Auto"/>
<Setter Target="Col_Nombre.Width" Value="0"/>  <!-- Ocultar -->

<!-- Alto de elemento -->
<Setter Target="ElementoNombre.Height" Value="50"/>
<Setter Target="ElementoNombre.MaxHeight" Value="60"/>
<Setter Target="ElementoNombre.MinHeight" Value="30"/>
```

### **Orientación**

```xml
<!-- StackPanel -->
<Setter Target="PanelNombre.Orientation" Value="Horizontal"/>
<Setter Target="PanelNombre.Orientation" Value="Vertical"/>

<!-- ItemsWrapGrid -->
<Setter Target="GridNombre.Orientation" Value="Horizontal"/>
<Setter Target="GridNombre.MaximumRowsOrColumns" Value="1"/>
```

### **Espaciado**

```xml
<!-- Spacing -->
<Setter Target="PanelNombre.Spacing" Value="12"/>

<!-- Padding -->
<Setter Target="ElementoNombre.Padding" Value="10,8,10,8"/>

<!-- Margin -->
<Setter Target="ElementoNombre.Margin" Value="0,8,0,0"/>
```

### **Fuente**

```xml
<Setter Target="TextoNombre.FontSize" Value="14"/>
<Setter Target="TextoNombre.FontWeight" Value="SemiBold"/>
```

### **Opacidad**

```xml
<!-- Transparente (sigue ocupando espacio) -->
<Setter Target="ElementoNombre.Opacity" Value="0"/>

<!-- Visible -->
<Setter Target="ElementoNombre.Opacity" Value="1"/>
```

---

## 📐 GRID SIZING

| Valor | Significado | Cuándo Usar |
|-------|-------------|-------------|
| `Auto` | Ajusta al contenido | Botones, iconos, labels |
| `*` | Ocupa espacio restante | Columna principal (ej: Acción) |
| `2*` | Doble que `*` | Columnas importantes |
| `150` | Píxeles fijos | ⚠️ Evitar en responsive |
| `0` | Ocultar columna | En estados Narrow |

---

## 🎨 PRIORIDAD DE COLUMNAS

### **Nivel 1: NUNCA OCULTAR** 🔥

- `Fecha` - Identificación temporal
- `Cliente` - Identificación principal
- `Estado` - Información de seguimiento

### **Nivel 2: IMPORTANTE** ⚡

- `Acción` - Descripción del trabajo
- `Inicio` - Hora de inicio
- `Fin` - Hora de cierre

### **Nivel 3: ÚTIL** 📊

- `Duración` - Tiempo total
- `Ticket` - Referencia externa

### **Nivel 4: SECUNDARIO** 📋

- `Grupo` - Categorización interna
- `Tipo` - Categorización interna
- `Tienda` - Ubicación específica

---

## 🔄 ESTRATEGIA DE OCULTACIÓN

### **Tamaño Grande (>1400px)**
```
✅ Mostrar: TODO (11 columnas)
```

### **Tamaño Mediano (1024-1399px)**
```
✅ Mostrar: Fecha, Cliente, Acción, Inicio, Fin, Duración, Ticket, Estado (8 columnas)
❌ Ocultar: Tienda, Grupo, Tipo (3 columnas)
```

### **Tamaño Pequeño (<1024px)**
```
✅ Mostrar: Fecha, Cliente, Acción, Inicio, Estado (5 columnas)
❌ Ocultar: Tienda, Fin, Duración, Ticket, Grupo, Tipo (6 columnas)
```

---

## 💻 CÓDIGO TEMPLATE

### **Estructura Básica**

```xml
<Grid x:Name="RootGrid">
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup x:Name="WindowSizeStates">
            
            <!-- Estado 1: Wide -->
            <VisualState x:Name="WideState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="1400"/>
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <!-- Setters aquí -->
                </VisualState.Setters>
            </VisualState>
            
            <!-- Estado 2: Normal -->
            <VisualState x:Name="NormalState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="1024"/>
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <!-- Setters aquí -->
                </VisualState.Setters>
            </VisualState>
            
            <!-- Estado 3: Narrow -->
            <VisualState x:Name="NarrowState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="0"/>
                </VisualState.StateTriggers>
                <VisualState.Setters>
                    <!-- Setters aquí -->
                </VisualState.Setters>
            </VisualState>
            
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
    
    <!-- Tu contenido aquí -->
</Grid>
```

### **Ejemplo: Ocultar Columna**

```xml
<!-- Wide: Mostrar -->
<VisualState x:Name="WideState">
    <VisualState.Setters>
        <Setter Target="Col_Tienda.Width" Value="55"/>
    </VisualState.Setters>
</VisualState>

<!-- Normal/Narrow: Ocultar -->
<VisualState x:Name="NormalState">
    <VisualState.Setters>
        <Setter Target="Col_Tienda.Width" Value="0"/>
    </VisualState.Setters>
</VisualState>
```

### **Ejemplo: Cambiar Orientación**

```xml
<!-- Wide/Normal: Horizontal -->
<VisualState x:Name="WideState">
    <VisualState.Setters>
        <Setter Target="ButtonsPanel.Orientation" Value="Horizontal"/>
    </VisualState.Setters>
</VisualState>

<!-- Narrow: Vertical -->
<VisualState x:Name="NarrowState">
    <VisualState.Setters>
        <Setter Target="ButtonsPanel.Orientation" Value="Vertical"/>
    </VisualState.Setters>
</VisualState>
```

---

## 🚨 ERRORES COMUNES

| Error | Problema | Solución |
|-------|----------|----------|
| Elemento no cambia | Nombre incorrecto en Target | Verificar `x:Name` coincide |
| "Property not found" | Target apunta a propiedad inexistente | Revisar documentación del control |
| Cambios no se aplican | VisualStateManager mal ubicado | Debe estar DENTRO del Grid principal |
| Todos los estados activos | Triggers con mismo `MinWindowWidth` | Ordenar de mayor a menor |

---

## 🎯 NOMENCLATURA RECOMENDADA

### **Para Columnas de Grid**

```xml
<ColumnDefinition x:Name="Col_Fecha"/>
<ColumnDefinition x:Name="Col_Cliente"/>
<ColumnDefinition x:Name="Col_Tienda"/>
<!-- etc. -->
```

### **Para Paneles**

```xml
<StackPanel x:Name="UserInfoPanel"/>
<StackPanel x:Name="ButtonsPanel"/>
<Grid x:Name="FiltersGrid"/>
```

### **Para Imágenes**

```xml
<Image x:Name="LogoImageBanner"/>
<Image x:Name="BackgroundImage"/>
```

---

## 📊 TESTING CHECKLIST

```
✅ Tamaño 1920x1080 (Wide)
   [ ] Todas las columnas visibles
   [ ] Logo 60px
   [ ] Botones horizontales
   [ ] Info usuario visible

✅ Tamaño 1366x768 (Normal)
   [ ] Columnas secundarias ocultas (Tienda, Grupo, Tipo)
   [ ] Logo 50px
   [ ] Botones horizontales
   [ ] Info usuario visible

✅ Tamaño 1024x768 (Narrow)
   [ ] Solo columnas esenciales visibles
   [ ] Logo 40px
   [ ] Botones verticales
   [ ] Info usuario oculta

✅ General
   [ ] No hay overlapping de elementos
   [ ] Texto no se corta (usa ellipsis)
   [ ] Scroll funciona correctamente
   [ ] Transiciones suaves
```

---

## 🔧 DEBUGGING

### **Live Visual Tree (Visual Studio)**

```
Debug → Windows → Live Visual Tree
```

- ✅ Ver jerarquía de elementos
- ✅ Inspeccionar propiedades en tiempo real
- ✅ Identificar qué estado está activo

### **Output Window**

```
Debug → Windows → Output
```

Buscar logs como:
```
VisualStateManager: WideState activated
VisualStateManager: NormalState activated
```

### **XAML Hot Reload**

```
Modificar XAML → Ver cambios sin reiniciar
```

⚠️ **No funciona con VisualStateManager** - Requiere reiniciar app

---

## 💡 TIPS PRO

### **1. Usar Variables para Breakpoints**

```xml
<!-- Definir en recursos -->
<x:Double x:Key="WideBreakpoint">1400</x:Double>
<x:Double x:Key="NormalBreakpoint">1024</x:Double>

<!-- Usar en triggers -->
<AdaptiveTrigger MinWindowWidth="{StaticResource WideBreakpoint}"/>
```

### **2. Transiciones Suaves**

```xml
<VisualState.Storyboard>
    <Storyboard>
        <DoubleAnimation Storyboard.TargetName="Logo"
                         Storyboard.TargetProperty="MaxHeight"
                         To="40"
                         Duration="0:0:0.3"/>
    </Storyboard>
</VisualState.Storyboard>
```

### **3. Estados Personalizados**

```xml
<!-- Estado para tablets en landscape -->
<VisualState x:Name="TabletLandscape">
    <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="768"/>
    </VisualState.StateTriggers>
</VisualState>
```

---

## 📚 RECURSOS RÁPIDOS

| Recurso | Ubicación | Uso |
|---------|-----------|-----|
| Guía Completa | `Doc/GUIA_DISENO_RESPONSIVE.md` | Implementación detallada |
| Ejemplo XAML | `Doc/EJEMPLO_RESPONSIVE_COMPLETO.xaml` | Código listo para copiar |
| Mejores Prácticas | `Doc/MEJORES_PRACTICAS_RESPONSIVE.md` | Tips avanzados |
| Resumen | `Doc/RESUMEN_EJECUTIVO_RESPONSIVE.md` | Overview rápido |

---

## 🚀 QUICK START (2 MINUTOS)

```xml
<!-- 1. Copiar esto al inicio de tu Grid principal -->
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup>
        <VisualState x:Name="WideState">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="1400"/>
            </VisualState.StateTriggers>
        </VisualState>
        <VisualState x:Name="NarrowState">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="0"/>
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <!-- Ejemplo: Ocultar columna Tienda -->
                <Setter Target="Col_Tienda.Width" Value="0"/>
            </VisualState.Setters>
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>

<!-- 2. Agregar x:Name a columnas que quieres controlar -->
<ColumnDefinition x:Name="Col_Tienda" Width="55"/>

<!-- 3. Compilar y probar redimensionando ventana -->
```

---

**Última actualización:** 2025-01-27  
**Versión:** Referencia Rápida v1.0  
**Estado:** ✅ Lista para Consulta

