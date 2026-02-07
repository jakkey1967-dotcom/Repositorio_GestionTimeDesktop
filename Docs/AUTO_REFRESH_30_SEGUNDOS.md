# 🔄 AUTO-REFRESCO CADA 30 SEGUNDOS - Panel Usuarios Online

**Fecha**: 2026-02-02  
**Componente**: OnlineUsersPanel (panel integrado en DiarioPage)  
**Versión**: v1.1.0  

---

## 📋 **CAMBIOS IMPLEMENTADOS**

### ✅ **1. Cambio de intervalo: 15s → 30s**

**Archivo**: `ViewModels/OnlineUsersPanelViewModel.cs`

**ANTES**:
```csharp
_refreshTimer.Interval = TimeSpan.FromSeconds(15);
_log?.LogInformation("⏰ Timer de refresh iniciado (15s) - Panel integrado");
```

**DESPUÉS**:
```csharp
_refreshTimer.Interval = TimeSpan.FromSeconds(30);
_log?.LogInformation("⏰ Timer de refresh iniciado (30s) - Panel integrado");
```

---

### ✅ **2. Añadido flag IsRefreshing (evita solapamientos)**

**Archivo**: `ViewModels/OnlineUsersPanelViewModel.cs`

**NUEVA PROPIEDAD**:
```csharp
private bool _isRefreshing;
public bool IsRefreshing
{
    get => _isRefreshing;
    set
    {
        if (_isRefreshing != value)
        {
            _isRefreshing = value;
            OnPropertyChanged();
        }
    }
}
```

**ACTUALIZADO `RefreshAsync()`**:
```csharp
public async Task RefreshAsync()
{
    if (_cts?.Token.IsCancellationRequested == true)
        return;

    // ✅ Evitar solapamientos
    if (IsRefreshing)
    {
        _log?.LogDebug("⏭️ Refresh ya en curso, saltando...");
        return;
    }

    IsRefreshing = true;

    try
    {
        // ... código de refresh ...
    }
    finally
    {
        // ✅ Siempre liberar el flag
        IsRefreshing = false;
    }
}
```

---

### ✅ **3. Actualización automática del subtitle**

**Archivo**: `Views/Controls/OnlineUsersPanel.xaml.cs`

**AÑADIDO EN `OnViewModelPropertyChanged`**:
```csharp
else if (e.PropertyName == nameof(OnlineUsersPanelViewModel.IsRefreshing))
{
    // Actualizar subtitle cuando termina el refresh
    if (_viewModel?.IsRefreshing == false && _viewModel.GroupedUsers.Any())
    {
        UpdateSubtitle();
    }
}
```

**Resultado**: El subtitle "Online: X · Total: Y" se actualiza automáticamente tras cada refresh automático.

---

## 🎯 **FUNCIONALIDADES IMPLEMENTADAS**

| Requisito | Estado | Descripción |
|-----------|--------|-------------|
| ✅ Intervalo exacto 30s | Completado | Timer configurado a 30 segundos |
| ✅ No bloquear UI | Completado | Usa `DispatcherQueueTimer` |
| ✅ Reutiliza método existente | Completado | Llama a `RefreshAsync()` existente |
| ✅ Detener al cerrar | Completado | Se detiene en `Cleanup()` / `Dispose()` |
| ✅ Evitar solapamientos | Completado | Flag `IsRefreshing` + early return |
| ✅ Actualizar subtitle | Completado | Se actualiza con cada refresh |

---

## 📊 **FLUJO COMPLETO**

```
┌──────────────────────┐
│   OnlineUsersPanel   │
│   (UserControl)      │
└──────────┬───────────┘
           │
           │ Initialize(viewModel)
           │
           ▼
┌──────────────────────┐
│ OnlineUsersPanelVM   │
└──────────┬───────────┘
           │
           │ LoadAsync() → StartRefreshTimer()
           │
           ▼
┌──────────────────────┐
│  DispatcherTimer     │
│  Interval: 30s       │
└──────────┬───────────┘
           │
           │ Tick (cada 30s)
           │
           ▼
┌──────────────────────┐
│  RefreshAsync()      │
└──────────┬───────────┘
           │
           ├─► IsRefreshing = true
           ├─► GetUsersAsync()
           ├─► GroupAndSortUsers()
           ├─► Update GroupedUsers
           ├─► OnPropertyChanged("GroupedUsers")
           └─► IsRefreshing = false (finally)
                   │
                   ▼
┌──────────────────────────────┐
│  OnViewModelPropertyChanged  │
└──────────┬───────────────────┘
           │
           ├─► GroupedUsers changed → UpdateSubtitle()
           └─► IsRefreshing = false → UpdateSubtitle()
```

---

## 🚀 **CICLO DE VIDA**

### **1. Inicio del panel**:
```csharp
Initialize(viewModel)
   └─► LoadAsync()
          └─► StartRefreshTimer()  // ✅ Timer inicia
```

### **2. Durante el uso**:
```
T+0s:  Carga inicial
T+30s: Refresh automático #1
T+60s: Refresh automático #2
T+90s: Refresh automático #3
...
```

### **3. Cierre del panel**:
```csharp
Cleanup()
   └─► StopRefreshTimer()  // ✅ Timer detiene
          └─► Dispose()
```

---

## ✅ **VERIFICACIÓN**

### **Prueba 1: Intervalo de 30 segundos**

1. Abrir DiarioPage
2. Ver el panel de usuarios online
3. **Esperar 30 segundos**
4. Verificar en logs:
```
[Debug] 🔄 Refrescando usuarios en panel integrado...
[Debug] ✅ Usuarios refrescados: 6 usuarios
```

### **Prueba 2: No hay solapamientos**

1. Simular refresh lento (backend tarda >30s en responder)
2. Verificar en logs:
```
[Debug] ⏭️ Refresh ya en curso, saltando...
```

### **Prueba 3: Subtitle se actualiza**

1. Ver subtitle inicial: "Online: 1 · Total: 6"
2. Simular que otro usuario se conecta (script de test)
3. **Esperar 30 segundos**
4. Verificar subtitle actualizado: "Online: 2 · Total: 6"

### **Prueba 4: Timer se detiene al cerrar**

1. Abrir DiarioPage
2. Navegar a otra página
3. Verificar en logs:
```
[Information] ⏰ Timer de refresh detenido - Panel integrado
```

---

## 📝 **ARCHIVOS MODIFICADOS**

```
ViewModels/
└── OnlineUsersPanelViewModel.cs
    ├── Añadida propiedad IsRefreshing
    ├── Cambiado intervalo: 15s → 30s
    └── Protección contra solapamientos

Views/Controls/
└── OnlineUsersPanel.xaml.cs
    └── Actualización de subtitle en OnViewModelPropertyChanged
```

---

## 🔗 **ARCHIVOS RELACIONADOS**

- **ViewModel**: `ViewModels/OnlineUsersPanelViewModel.cs`
- **View**: `Views/Controls/OnlineUsersPanel.xaml.cs`
- **XAML**: `Views/Controls/OnlineUsersPanel.xaml`
- **Service**: `Services/Presence/PresenceService.cs`

---

## 📚 **DOCUMENTACIÓN ADICIONAL**

- [PANEL_USUARIOS_ONLINE_INTEGRADO.md](PANEL_USUARIOS_ONLINE_INTEGRADO.md)
- [SOLUCION_HEARTBEAT_PRESENCIA.md](SOLUCION_HEARTBEAT_PRESENCIA.md)
- [SISTEMA_USUARIOS_ONLINE_Y_API.md](SISTEMA_USUARIOS_ONLINE_Y_API.md)

---

## 🎯 **RESULTADO FINAL**

| Característica | Antes | Después |
|----------------|-------|---------|
| **Intervalo refresh** | 15 segundos | ✅ **30 segundos** |
| **Protección solapamiento** | ❌ No | ✅ **Sí (flag IsRefreshing)** |
| **Actualización subtitle** | Manual | ✅ **Automática** |
| **Ciclo de vida** | Correcto | ✅ **Correcto** |

---

**Autor**: GitHub Copilot  
**Ticket**: Auto-refresco cada 30 segundos en panel usuarios  
**Prioridad**: 🟡 MEDIA (mejora UX)
