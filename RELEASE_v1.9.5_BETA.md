# 🚀 Release v1.9.5 Beta — GestionTime Desktop

**Fecha:** 2 de Febrero de 2025  
**Tag:** `v1.9.5-beta`  
**Commits desde v1.9.0-beta:** 14

---

## ✨ Resumen Ejecutivo

Esta versión añade el **Sistema de Informes** completo (ventana dedicada con gráficas, exportación y resolución de solapamientos), el **Sistema de Notas de Cliente dual** (global + personal por rol), y mejoras en el **Perfil de usuario**.

---

## 🆕 Funcionalidades Principales

### 📊 Sistema de Informes
- Ventana dedicada con scopes Día/Semana/Rango
- Gráfica semanal de barras (Lun-Sáb) con validación vs 8h
- Exportar a **Excel**, **PDF** y compartir por **Email**
- **Detección de solapamientos** con tabla detallada
- **Edición inline** de Hora Inicio/Fin por parte solapado
- **Solución Automática** de solapamientos (greedy por duración)
- Selector de agente para EDITOR/ADMIN

### 📝 Notas de Cliente (Global + Personal)
- **Nota global**: una por cliente, editable solo por EDITOR/ADMIN
- **Nota personal**: una por usuario y cliente, privada
- ContentDialog con 2 secciones + guardado independiente
- Fallback a nota legacy si backend v2 no disponible
- **Nuevos endpoints** `/api/v2/clientes/{id}/notas`

### 👤 Perfil en Settings
- Edición inline con 11 campos
- Recarga automática desde servidor al abrir

---

## 🔒 Seguridad

| Área | USER | EDITOR/ADMIN |
|------|------|--------------|
| Informes: ver sus datos | ✅ | ✅ |
| Informes: ver otros agentes | ❌ | ✅ |
| Nota global: leer | ✅ | ✅ |
| Nota global: editar | ❌ | ✅ |
| Nota personal: editar | ✅ (propia) | ✅ (propia) |

---

## 📦 Paquetes Añadidos

| Paquete | Versión | Uso |
|---------|---------|-----|
| ClosedXML | 0.104.2 | Exportación Excel |
| QuestPDF | 2024.12.2 | Exportación PDF |
| SkiaSharp | 2.88.9 | Logo con bordes redondeados en PDF |

---

## 📁 Archivos Nuevos (Desktop)

```
ViewModels/Reports/ReportsViewModel.cs
ViewModels/Reports/ReportsViewModel.Overlap.cs
Views/Reports/ReportsWindow.xaml
Views/Reports/ReportsWindow.xaml.cs
Services/Reports/InformesService.cs
Services/Export/ReportExportService.cs
Models/Dtos/Reports/InformeResumenDto.cs
Models/Dtos/Catalog/ClienteNotasDto.cs
Docs/NOTAS_CLIENTE_GLOBAL_PERSONAL.md
Docs/SISTEMA_INFORMES_IMPLEMENTACION.md
Docs/MEJORAS_UI_INFORMES_GRAFICA_SEMANAL.md
```

## 📁 Archivos Nuevos (Backend)

```
GestionTime.Domain/Work/ClienteNota.cs
Contracts/Catalog/ClienteNotasDto.cs
Controllers/V2/ClienteNotasController.cs
scripts/Migration-ClienteNotas.sql
```

---

## ⚠️ Requisitos de Despliegue

### Backend (antes de desktop)
1. Ejecutar `scripts/Migration-ClienteNotas.sql` en PostgreSQL
2. Desplegar API con nuevo controller `V2/ClienteNotasController`
3. Verificar que `GET /api/v2/clientes/{id}/notas` responde

### Desktop
1. Compilar y desplegar MSI actualizado
2. Verificar con roles USER, EDITOR y ADMIN

### Compatibilidad
- ✅ Compatible con backend sin v2 (fallback a nota legacy)
- ✅ Endpoints v1 no modificados
- ✅ Base de datos retrocompatible (tabla nueva, sin ALTER)

---

## 🧪 Testing

- [x] Build compila sin errores (Desktop + Backend)
- [ ] Probar Informes: Día, Semana, Rango
- [ ] Probar Exportar: Excel, PDF, Email
- [ ] Probar Solapamientos: edición inline + Solución Automática
- [ ] Probar Notas: USER readonly global, EDITOR/ADMIN editable
- [ ] Probar Fallback notas: sin endpoint v2 → nota legacy
- [ ] Probar Perfil: editar y reabrir Settings
