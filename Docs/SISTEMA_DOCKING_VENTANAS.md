# 🪟 Sistema de Docking de Ventanas - UsersOnlineWindow

## 📋 Descripción

Implementación de un sistema de "ventana acoplada" (docking) donde la ventana secundaria `UsersOnlineWindow` se posiciona automáticamente a la derecha de la ventana principal `MainWindow` y sigue sus movimientos y redimensionamientos en tiempo real.

---

## 🎯 Objetivos

1. ✅ **Snap/Follow**: La ventana secundaria sigue automáticamente a la principal
2. ✅ **Minimize/Restore Sync**: Sincroniza estados de minimizado/restaurado
3. ✅ **Evitar bucles**: Protección contra recursión infinita
4. ✅ **Logout Integration**: Se cierra automáticamente al hacer logout

---

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────────────────────────────┐
│                      MainWindow                                 │
│                   (Ventana Principal)                           │
│                                                                 │
│  ┌──────────────────────────────┐                              │
│  │   WindowDockService          │                              │
│  │  (Servicio de Docking)       │                              │
│  └──────────┬───────────────────┘                              │
│             │                                                   │
│             │ Attach/Detach                                     │
│             │ SnapNow()                                         │
│             │ SyncMinimizeState()                               │
│             │                                                   │
└─────────────┼───────────────────────────────────────────────────┘
              │
              │ Monitoreа posición/tamaño
              │ Timer cada 150ms
              │
┌─────────────▼───────────────────────────────────────────────────┐
│                  UsersOnlineWindow                              │
│                 (Ventana Secundaria)                            │
│                                                                 │
│  • Posición: X = MainWindow.X + MainWindow.Width + 10px        │
│  • Tamaño:   Height = MainWindow.Height                        │
│  • Sincroniza minimizado/restaurado                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📂 Archivos Implementados

### **1. Services\Windowing\WindowDockService.cs** ⭐

**Servicio principal de docking.**

```csharp
public sealed class WindowDockService : IDisposable
{
    // Métodos Públicos
    public void Attach(Window mainWindow, Window childWindow)
    public void Detach()
    public void SnapNow()
    
    // Métodos Privados
    private void StartSnapTimer()
    private void StopSnapTimer()
    private void OnSnapTimerTick(object? sender, object e)
    private void SyncMinimizeState()
    private void OnMainWindowActivated(object sender, WindowActivatedEventArgs args)
    private void OnMainWindowVisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
    private void OnChildWindowClosed(object sender, WindowEventArgs args)
    private AppWindow? GetAppWindow(Window window)
}
```

**Configuración:**
- `SnapGap = 0px`: Ventanas completamente pegadas (sin separación)
- `SnapInterval = 150ms`: Frecuencia de actualización

---

### **2. MainWindow.xaml.cs** (Modificaciones)

**Métodos Agregados:**

```csharp
/// <summary>Inicia el docking de la ventana de usuarios online.</summary>
public void AttachUsersOnlineWindow()

/// <summary>Detiene el docking de la ventana de usuarios online.</summary>
public void DetachUsersOnlineWindow()
```

**Modificación en `PerformLogoutAsync()`:**
```csharp
// 🆕 NUEVO: Detener docking service antes de cerrar ventana
DetachUsersOnlineWindow();

// Cerrar ventana de Usuarios Online si está abierta
CloseUsersOnlineWindow();
```

---

### **3. Views\UsersOnlineWindow.xaml.cs** (Modificaciones)

**Evento Agregado en Constructor:**

```csharp
// 🆕 NUEVO: Iniciar docking cuando la ventana se active
Activated += OnWindowActivated;
```

**Nuevo Método:**

```csharp
/// <summary>
/// Cuando la ventana se activa por primera vez, iniciar el docking
/// </summary>
private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
{
    if (args.WindowActivationState != WindowActivationState.Deactivated)
    {
        Activated -= OnWindowActivated; // Solo la primera vez
        
        App.Log?.LogInformation("🔗 Iniciando docking de UsersOnlineWindow...");
        App.MainWindowInstance?.AttachUsersOnlineWindow();
    }
}
```

---

### **4. App.xaml.cs** (Modificaciones)

**Propiedad Agregada:**

```csharp
// 🆕 NUEVO: Servicio de docking de ventanas
public static Services.Windowing.WindowDockService? WindowDockService { get; set; }
```

---

## 🔄 Flujo de Funcionamiento

### **Apertura de UsersOnlineWindow**

```
Usuario abre UsersOnlineWindow (desde DiarioPage o ConfiguracionWindow)
    ↓
UsersOnlineWindow.Constructor()
    ↓
Se suscribe al evento Activated
    ↓
Ventana se muestra y Activated se dispara
    ↓
OnWindowActivated() ejecuta:
    ↓
App.MainWindowInstance.AttachUsersOnlineWindow()
    ↓
Se crea WindowDockService (si no existe)
    ↓
WindowDockService.Attach(mainWindow, childWindow)
    ↓
    ├─ Obtiene AppWindow de ambas ventanas
    ├─ Aplica snap inicial (SnapNow())
    ├─ Inicia timer de 150ms
    └─ Suscribe eventos (Activated, VisibilityChanged, Closed)
```

### **Movimiento de MainWindow**

```
Usuario mueve MainWindow
    ↓
Timer tick detecta cambio de posición (cada 150ms)
    ↓
OnSnapTimerTick() compara posición actual vs. última
    ↓
Detecta cambio → Ejecuta SnapNow()
    ↓
SnapNow() calcula nueva posición de UsersOnlineWindow:
    ├─ childX = mainX + mainWidth + 10px
    ├─ childY = mainY
    └─ childHeight = mainHeight
    ↓
Aplica posición con AppWindow.Move() y AppWindow.Resize()
```

### **Redimensionamiento de MainWindow**

```
Usuario redimensiona MainWindow
    ↓
Timer tick detecta cambio de tamaño
    ↓
SnapNow() ajusta UsersOnlineWindow:
    ├─ Misma altura que MainWindow
    └─ Reposiciona X (por si cambió el ancho)
```

### **Minimizado de MainWindow**

```
Usuario minimiza MainWindow
    ↓
Timer tick ejecuta SyncMinimizeState()
    ↓
Detecta: mainPresenter.State == Minimized
    ↓
Ejecuta: childPresenter.Minimize()
    ↓
Log: "⬇️ MainWindow MINIMIZADA - Minimizando UsersOnlineWindow"
```

### **Restauración de MainWindow**

```
Usuario restaura MainWindow
    ↓
SyncMinimizeState() detecta cambio
    ↓
Ejecuta: childPresenter.Restore()
    ↓
Aplica snap para reposicionar correctamente
    ↓
Log: "⬆️ MainWindow RESTAURADA - Restaurando UsersOnlineWindow"
```

### **Logout**

```
Usuario hace logout (X o botón Salir)
    ↓
MainWindow.PerformLogoutAsync()
    ↓
Ejecuta: DetachUsersOnlineWindow()
    ↓
WindowDockService.Detach()
    ├─ Detiene timer
    ├─ Desuscribe eventos
    └─ Limpia referencias
    ↓
Ejecuta: CloseUsersOnlineWindow()
    ↓
UsersOnlineWindow se cierra
    ↓
Navega a LoginPage
```

---

## 🛡️ Protección contra Bucles

### **Flag `_isSnapping`**

```csharp
private bool _isSnapping = false;

public void SnapNow()
{
    if (_isSnapping) return; // ✅ Evita re-entrada
    
    _isSnapping = true;
    try
    {
        // Aplicar snap...
    }
    finally
    {
        _isSnapping = false; // ✅ Siempre se libera
    }
}
```

### **Desuscripción en Activated**

```csharp
private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
{
    if (args.WindowActivationState != WindowActivationState.Deactivated)
    {
        Activated -= OnWindowActivated; // ✅ Solo se ejecuta una vez
        App.MainWindowInstance?.AttachUsersOnlineWindow();
    }
}
```

---

## 📊 Logs Detallados

### **Attach**
```
═══════════════════════════════════════════════════════════════
📌 DOCK ATTACH - Iniciando acoplamiento de ventanas
═══════════════════════════════════════════════════════════════
📐 Posición inicial MainWindow: X=100, Y=100, W=1200, H=800
✅ DOCK ATTACHED correctamente
   • Timer de snap: 150ms
   • Gap entre ventanas: 0px (pegadas completamente)
═══════════════════════════════════════════════════════════════
```

### **Snap**
```
🔄 SNAP APPLIED:
   • Main: X=150, Y=200, W=1400, H=900
   • Child: X=1560, Y=200, W=400, H=900
```

### **Minimize/Restore**
```
⬇️ MainWindow MINIMIZADA - Minimizando UsersOnlineWindow
⬆️ MainWindow RESTAURADA - Restaurando UsersOnlineWindow
```

### **Detach**
```
═══════════════════════════════════════════════════════════════
📌 DOCK DETACH - Desacoplando ventanas
═══════════════════════════════════════════════════════════════
⏰ Timer de snap detenido
✅ DOCK DETACHED correctamente
═══════════════════════════════════════════════════════════════
```

---

## 🧪 Casos de Prueba

| # | Escenario | Resultado Esperado | ✅ |
|---|-----------|-------------------|---|
| 1 | Abrir UsersOnlineWindow | Se posiciona a la derecha de MainWindow | ✅ |
| 2 | Mover MainWindow | UsersOnlineWindow sigue el movimiento | ✅ |
| 3 | Redimensionar MainWindow | UsersOnlineWindow ajusta altura | ✅ |
| 4 | Minimizar MainWindow | UsersOnlineWindow se minimiza | ✅ |
| 5 | Restaurar MainWindow | UsersOnlineWindow se restaura y reposiciona | ✅ |
| 6 | Cerrar UsersOnlineWindow | Docking se detiene automáticamente | ✅ |
| 7 | Logout con ventana abierta | UsersOnlineWindow se cierra antes de ir a Login | ✅ |
| 8 | Mover UsersOnlineWindow manualmente | Se permite (no fuerza snap hasta que MainWindow se mueva) | ✅ |
| 9 | Doble clic en botón usuarios online | No crashea (verifica si ya está abierta) | ✅ |

---

## ⚙️ Configuración Ajustable

Si necesitas cambiar el comportamiento, modifica estas constantes en `WindowDockService.cs`:

```csharp
private const int SnapGap = 0;         // Separación en píxeles (default: 0 - completamente pegadas)
private const int SnapInterval = 150;  // Frecuencia del timer en ms (default: 150)
```

**Ejemplos:**
- `SnapGap = 0`: Ventanas pegadas completamente ✅ **(configuración actual)**
- `SnapGap = 5`: Pequeña separación visual
- `SnapGap = 20`: Mayor separación visual
- `SnapInterval = 100`: Actualización más frecuente (más suave pero más CPU)
- `SnapInterval = 300`: Menos frecuente (menos CPU pero puede parecer menos responsive)

---

## 🚀 Ventajas del Diseño

1. ✅ **Servicio Independiente**: `WindowDockService` es reutilizable
2. ✅ **Cambios Mínimos**: Solo 4 archivos modificados
3. ✅ **Sin Romper Nada**: La ventana funciona igual si el docking falla
4. ✅ **Logging Completo**: Trazabilidad de todas las operaciones
5. ✅ **Thread-Safe**: Usa DispatcherQueue para operaciones UI
6. ✅ **Memory-Safe**: Dispose y cleanup correctos
7. ✅ **Protección contra Bucles**: Flags y desuscripciones

---

## 🔜 Mejoras Futuras (Opcional)

1. **Posiciones Guardadas**: Recordar si el usuario desacopló manualmente
2. **Múltiples Posiciones**: Permitir snap left/right/bottom
3. **Animaciones**: Transiciones suaves al snap
4. **Configuración UI**: Panel para ajustar gap y frecuencia
5. **Dock con Múltiples Ventanas**: Acoplar más de una ventana secundaria

---

**Versión:** v1.5.0-beta  
**Fecha:** 2024  
**Autor:** GestionTime Development Team
