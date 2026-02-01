# 🎯 INTEGRACIÓN COMPLETA - Servicios de Catálogo en Desktop

> **Fecha:** 30 Enero 2026  
> **Estado:** ✅ IMPLEMENTADO  
> **Backend:** https://gestiontime-api.onrender.com/api/v1

---

## 📋 **¿QUÉ SE IMPLEMENTÓ?**

Se agregaron **tres servicios CRUD completos** en el Desktop para consumir los endpoints del backend:

| Servicio | Endpoints | DTOs | Paginación | Búsqueda |
|----------|-----------|------|------------|----------|
| `ClientesService` | 6 | ✅ | ✅ | ✅ |
| `TiposService` | 5 | ✅ | ✅ | ✅ |
| `GruposService` | 5 | ✅ | ✅ | ✅ |

---

## 📂 **ARCHIVOS CREADOS:**

### **DTOs (Models/Dtos/Catalog/):**
```
✅ ClienteDto.cs          - Modelo de Cliente + Requests
✅ TipoDto.cs             - Modelo de Tipo + Requests
✅ GrupoDto.cs            - Modelo de Grupo + Requests
✅ PagedResponse.cs       - Respuesta paginada genérica
```

### **Servicios (Services/Catalog/):**
```
✅ ClientesService.cs     - CRUD completo de Clientes
✅ TiposService.cs        - CRUD completo de Tipos
✅ GruposService.cs       - CRUD completo de Grupos
```

### **Configuración:**
```
✅ App.xaml.cs            - Registros de servicios
```

---

## 🚀 **USO EN CÓDIGO:**

### **1. Listar Clientes (Paginado + Búsqueda)**

```csharp
// Listar primeros 50 clientes
var response = await App.ClientesService.ListAsync(page: 1, pageSize: 50);

if (response != null)
{
    Console.WriteLine($"Total: {response.TotalCount}");
    Console.WriteLine($"Página {response.Page} de {response.TotalPages}");
    
    foreach (var cliente in response.Items)
    {
        Console.WriteLine($"ID: {cliente.Id}, Nombre: {cliente.Nombre}");
    }
    
    if (response.HasNextPage)
    {
        // Cargar siguiente página
        var nextPage = await App.ClientesService.ListAsync(page: 2, pageSize: 50);
    }
}
```

### **2. Buscar Clientes**

```csharp
// Buscar clientes que contengan "test" en nombre, nombreComercial o provincia
var response = await App.ClientesService.ListAsync(
    page: 1, 
    pageSize: 20, 
    search: "test"
);

Console.WriteLine($"Encontrados: {response?.TotalCount ?? 0} clientes");
```

### **3. Obtener Cliente por ID**

```csharp
var cliente = await App.ClientesService.GetByIdAsync(1);

if (cliente != null)
{
    Console.WriteLine($"Cliente: {cliente.Nombre}");
    Console.WriteLine($"Provincia: {cliente.Provincia}");
    Console.WriteLine($"Nota: {cliente.Nota}");
}
else
{
    Console.WriteLine("Cliente no encontrado");
}
```

### **4. Crear Cliente**

```csharp
var request = new ClienteCreateRequest
{
    Nombre = "Nuevo Cliente",
    IdPuntoop = 1234,
    LocalNum = 1,
    NombreComercial = "Cliente S.A.",
    Provincia = "Madrid",
    Nota = "Cliente creado desde Desktop"
};

try
{
    var cliente = await App.ClientesService.CreateAsync(request);
    Console.WriteLine($"Cliente creado con ID: {cliente.Id}");
}
catch (ApiException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Manejar errores de validación (400)
}
```

### **5. Actualizar Cliente (PUT completo)**

```csharp
var updateRequest = new ClienteUpdateRequest
{
    Nombre = "Cliente Actualizado",
    IdPuntoop = 1234,
    LocalNum = 2,
    NombreComercial = "Cliente Actualizado S.A.",
    Provincia = "Barcelona",
    Nota = "Actualizado desde Desktop"
};

try
{
    var cliente = await App.ClientesService.UpdateAsync(1, updateRequest);
    Console.WriteLine($"Cliente {cliente.Id} actualizado");
}
catch (ApiException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### **6. Actualizar solo Nota (PATCH)**

```csharp
// Solo actualiza la nota, sin necesidad de enviar todos los campos
var cliente = await App.ClientesService.UpdateNotaAsync(1, "Nueva nota");
Console.WriteLine($"Nota actualizada: {cliente.Nota}");
```

### **7. Eliminar Cliente**

```csharp
try
{
    var success = await App.ClientesService.DeleteAsync(1);
    if (success)
    {
        Console.WriteLine("Cliente eliminado correctamente");
    }
}
catch (ApiException ex)
{
    if (ex.StatusCode == HttpStatusCode.NotFound)
    {
        Console.WriteLine("Cliente no encontrado");
    }
}
```

---

## 🔧 **TIPOS Y GRUPOS - Igual de fácil:**

### **Listar Tipos**

```csharp
var tipos = await App.TiposService.ListAsync(page: 1, pageSize: 50);
foreach (var tipo in tipos.Items)
{
    Console.WriteLine($"{tipo.Id}: {tipo.Nombre} - {tipo.Descripcion}");
}
```

### **Crear Tipo**

```csharp
var request = new TipoCreateRequest
{
    Nombre = "Consultoría",
    Descripcion = "Servicios de consultoría técnica"
};

var tipo = await App.TiposService.CreateAsync(request);
```

### **Listar Grupos**

```csharp
var grupos = await App.GruposService.ListAsync(page: 1, pageSize: 50);
```

### **Crear Grupo**

```csharp
var request = new GrupoCreateRequest
{
    Nombre = "VIP",
    Descripcion = "Clientes VIP prioritarios"
};

var grupo = await App.GruposService.CreateAsync(request);
```

---

## 📊 **MODELO DE RESPUESTA PAGINADA:**

```csharp
public sealed class PagedResponse<T>
{
    public List<T> Items { get; set; }           // Elementos de la página actual
    public int TotalCount { get; set; }          // Total de elementos (sin paginar)
    public int Page { get; set; }                // Página actual
    public int PageSize { get; set; }            // Elementos por página
    public int TotalPages { get; set; }          // Total de páginas
    public bool HasNextPage { get; set; }        // ¿Hay más páginas?
    public bool HasPreviousPage { get; set; }    // ¿Hay páginas anteriores?
}
```

---

## ⚠️ **BREAKING CHANGES (vs endpoints antiguos):**

### **LocalNum e IdPuntoop ahora son `int?` (antes `string`):**

```csharp
// ❌ ANTES (INCORRECTO ahora)
var cliente = new ClienteCreateRequest
{
    LocalNum = "TEST-001",
    IdPuntoop = "9999"
};

// ✅ AHORA (CORRECTO)
var cliente = new ClienteCreateRequest
{
    LocalNum = 1,
    IdPuntoop = 9999
};
```

---

## 🎨 **EJEMPLO COMPLETO - Lista con Paginación:**

```csharp
using GestionTime.Desktop.Models.Dtos.Catalog;

public async Task MostrarClientesConPaginacionAsync()
{
    int currentPage = 1;
    int pageSize = 20;
    
    while (true)
    {
        var response = await App.ClientesService.ListAsync(currentPage, pageSize);
        
        if (response == null || !response.Items.Any())
        {
            Console.WriteLine("No hay clientes");
            break;
        }
        
        Console.WriteLine($"\n=== Página {response.Page} de {response.TotalPages} ===");
        Console.WriteLine($"Total: {response.TotalCount} clientes");
        Console.WriteLine();
        
        foreach (var cliente in response.Items)
        {
            Console.WriteLine($"{cliente.Id}: {cliente.Nombre} ({cliente.Provincia})");
        }
        
        if (!response.HasNextPage)
        {
            Console.WriteLine("\n(Última página)");
            break;
        }
        
        Console.Write("\n¿Siguiente página? (s/n): ");
        var key = Console.ReadLine();
        
        if (key?.ToLower() != "s")
            break;
        
        currentPage++;
    }
}
```

---

## 🧪 **TESTING:**

```powershell
# Ejecutar script de verificación
.\Scripts\Test-CatalogServices.ps1
```

---

## ✅ **CHECKLIST DE INTEGRACIÓN:**

- [x] DTOs creados (Cliente, Tipo, Grupo, PagedResponse)
- [x] Servicios CRUD implementados
- [x] Registrados en App.xaml.cs
- [x] Compilación exitosa
- [x] Logging detallado en todos los métodos
- [x] Soporte para paginación
- [x] Soporte para búsqueda
- [x] Manejo de errores con ApiException
- [ ] Crear vistas CRUD (opcional)
- [ ] Implementar caché local (opcional)
- [ ] Agregar validación en el cliente (opcional)

---

## 🚀 **PRÓXIMOS PASOS:**

### **Opcional 1: Crear Vistas CRUD**
```
Views/Catalog/ClientesPage.xaml
Views/Catalog/TiposPage.xaml
Views/Catalog/GruposPage.xaml
```

### **Opcional 2: Agregar a menú principal**
```xaml
<MenuFlyoutItem Text="📋 Gestionar Clientes" Click="OnClientesClick" />
<MenuFlyoutItem Text="📂 Gestionar Tipos" Click="OnTiposClick" />
<MenuFlyoutItem Text="🗂️ Gestionar Grupos" Click="OnGruposClick" />
```

---

**Versión:** 1.0  
**Última actualización:** 30 Enero 2026  
**Estado:** ✅ LISTO PARA USAR
