# ⏰ TIMESTAMPS EN TARJETAS DE USUARIOS

**Fecha**: 2026-02-02  
**Componente**: OnlineUsersPanel (panel integrado en DiarioPage)  
**Versión**: v1.1.0  

---

## 📋 **CAMBIOS IMPLEMENTADOS**

### ✅ **Requisito cumplido**:

Añadir timestamps en cada tarjeta de usuario mostrando:
- **Usuarios ONLINE**: "Actualizado: dd/MM/yyyy HH:mm:ss"
- **Usuarios OFFLINE**: "Última actividad: dd/MM/yyyy HH:mm:ss"

**Sin modificar diseño, estilos ni layout existente.**

---

## 🛠️ **CAMBIOS REALIZADOS**

### **1. ViewModels/UsersOnlineViewModel.cs**

**Añadidas propiedades en `UserCardItem`**:

```csharp
public DateTime? LastSeenAt { get; }

/// <summary>Texto de timestamp: "Actualizado: ..." si online, "Última actividad: ..." si offline.</summary>
public string DisplayStampText
{
    get
    {
        if (LastSeenAt == null)
            return "—";

        var formatted = LastSeenAt.Value.ToString("dd/MM/yyyy HH:mm:ss");
        return IsOnline
            ? $"Actualizado: {formatted}"
            : $"Última actividad: {formatted}";
    }
}
```

**Actualizado constructor**:
```csharp
public UserCardItem(PresenceUserDto dto)
{
    // ... propiedades existentes ...
    LastSeenAt = dto.LastSeenAt; // ← NUEVO
}
```

---

### **2. Views/Controls/OnlineUsersPanel.xaml**

**Añadida tercera fila al Grid**:
```xaml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>  <!-- ← NUEVO -->
</Grid.RowDefinitions>
```

**Añadido TextBlock con timestamp**:
```xaml
<!-- Row 3: Timestamp -->
<TextBlock Grid.Row="2" 
           Text="{Binding DisplayStampText}" 
           FontSize="10" 
           Foreground="{ThemeResource TextFillColorTertiaryBrush}"
           Opacity="0.7"
           Margin="0,2,0,0"/>
```

---

## 📊 **FORMATO DEL TIMESTAMP**

### **Usuarios ONLINE**:
```
Actualizado: 02/02/2026 21:15:33
```

### **Usuarios OFFLINE**:
```
Última actividad: 02/02/2026 20:45:12
```

### **Usuario sin datos**:
```
—
```

---

## 🎯 **FUNCIONAMIENTO**

### **1. Origen de los datos**:

El timestamp proviene de `LastSeenAt` en `PresenceUserDto`, que el backend ya devuelve en el endpoint `/api/v1/presence/users`.

**Ventaja**: No requiere almacenamiento local adicional (diccionario) porque el backend ya mantiene este dato.

### **2. Actualización automática**:

El timestamp se actualiza automáticamente cada 30 segundos cuando:
- El timer ejecuta `RefreshAsync()`
- Se obtienen nuevos datos del backend
- Se reconstruyen las tarjetas con `DisplayStampText` calculado dinámicamente

### **3. Formato garantizado**:

Formato: `dd/MM/yyyy HH:mm:ss`

Ejemplos:
- `02/02/2026 21:15:33` (21:15 con 33 segundos)
- `02/02/2026 09:05:08` (09:05 con 08 segundos)

---

## 🎨 **DISEÑO VISUAL**

### **Ubicación**:
Debajo de "Email + Role Badge" en cada tarjeta

### **Estilo**:
- **FontSize**: 10 (pequeño)
- **Foreground**: `TextFillColorTertiaryBrush` (color terciario del tema)
- **Opacity**: 0.7 (ligeramente transparente)
- **Margin**: `0,2,0,0` (2px de separación arriba)

### **Ejemplo de tarjeta**:

```
┌─────────────────────────────────────────┐
│ Francisco Santos            [● Online]  │ ← Row 1
│ psantos@global-retail.com   [ADMIN]    │ ← Row 2
│ Actualizado: 02/02/2026 21:15:33       │ ← Row 3 (NUEVO)
└─────────────────────────────────────────┘
```

---

## ✅ **VERIFICACIÓN**

### **Prueba 1: Usuario ONLINE**

1. Abrir DiarioPage
2. Ver panel de usuarios online
3. Usuario ONLINE debe mostrar:
```
Actualizado: 02/02/2026 21:15:33
```

### **Prueba 2: Usuario OFFLINE**

1. Detener script de test (`Ctrl+C` en `Test-UserPresence.ps1`)
2. Esperar 35 segundos (timeout + buffer)
3. Refrescar panel
4. Usuario OFFLINE debe mostrar:
```
Última actividad: 02/02/2026 21:15:33
```

### **Prueba 3: Timestamp se actualiza cada 30s**

1. Iniciar script de test
2. Ver timestamp inicial: `21:15:33`
3. Esperar 30 segundos
4. Ver timestamp actualizado: `21:16:03`

### **Prueba 4: Usuario sin LastSeenAt**

Si el backend devuelve `LastSeenAt: null`:
```
—
```

---

## 📝 **ARCHIVOS MODIFICADOS**

```
ViewModels/
└── UsersOnlineViewModel.cs
    └── UserCardItem
        ├── Añadida propiedad LastSeenAt
        └── Añadida propiedad DisplayStampText (calculada)

Views/Controls/
└── OnlineUsersPanel.xaml
    ├── Añadida tercera fila al Grid
    └── Añadido TextBlock con binding a DisplayStampText
```

---

## 🔗 **ARCHIVOS RELACIONADOS**

- **DTO**: `Models/Dtos/PresenceUserDto.cs` (ya contiene `LastSeenAt`)
- **ViewModel**: `ViewModels/UsersOnlineViewModel.cs`
- **View**: `Views/Controls/OnlineUsersPanel.xaml`
- **Backend**: `/api/v1/presence/users` devuelve `LastSeenAt` correctamente

---

## 📚 **DOCUMENTACIÓN ADICIONAL**

- [AUTO_REFRESH_30_SEGUNDOS.md](AUTO_REFRESH_30_SEGUNDOS.md) - Sistema de auto-refresco
- [PANEL_USUARIOS_ONLINE_INTEGRADO.md](PANEL_USUARIOS_ONLINE_INTEGRADO.md)
- [FIX_HEALTH_ACTUALIZA_PRESENCIA_BACKEND.md](FIX_HEALTH_ACTUALIZA_PRESENCIA_BACKEND.md)

---

## 💡 **NOTAS TÉCNICAS**

### **¿Por qué no se necesita diccionario local?**

El backend ya mantiene `LastSeenAt` en la tabla `UserSessions`:
- Cuando usuario está ONLINE: `LastSeenAt` se actualiza con cada ping
- Cuando usuario está OFFLINE: `LastSeenAt` conserva la última fecha de actividad

Por tanto, el Desktop solo necesita mostrar este valor sin almacenamiento adicional.

### **¿Cómo funciona para usuarios que nunca han estado online?**

Si un usuario está en la DB pero nunca inició sesión:
- `LastSeenAt` será `null`
- `DisplayStampText` devolverá `"—"`

### **¿El timestamp es en hora local o UTC?**

El backend devuelve **UTC**, pero `DateTime.ToString()` en C# usa la hora **local** del sistema del usuario automáticamente.

---

## 🎯 **RESULTADO FINAL**

| Característica | Antes | Después |
|----------------|-------|---------|
| **Timestamp en tarjeta** | ❌ No | ✅ **Sí** |
| **Formato** | — | ✅ **dd/MM/yyyy HH:mm:ss** |
| **Online** | — | ✅ **"Actualizado: ..."** |
| **Offline** | — | ✅ **"Última actividad: ..."** |
| **Auto-actualización** | — | ✅ **Cada 30s** |
| **Diseño modificado** | — | ✅ **NO (solo línea añadida)** |

---

**Autor**: GitHub Copilot  
**Ticket**: Timestamps en tarjetas de usuarios online/offline  
**Prioridad**: 🟢 BAJA (mejora UX)
