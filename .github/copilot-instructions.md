# Copilot Instructions (GestionTime Desktop)

## 1) Objetivo y límites (NO romper nada)
- Objetivo: reducir tamaño de archivos sin reescribirlos completos.
- Límites: XAML <= 400 líneas, C# <= 700 líneas.
- Cambios: pequeños, compilables, sin efectos colaterales.

## 2) Reglas de seguridad (prioridad absoluta)
- NO cambies layout ni apariencia salvo lo imprescindible para extraer/reutilizar código.
- NO renombres bindings/propiedades/commands (ni `x:Name` si se usa).
- NO cambies rutas/endpoints/contratos DTO si ya están operativos (solo si el pedido lo exige).
- NO dupliques DTOs/Models/Helpers: antes de crear, busca si ya existen y reutiliza.
- Mantén el comportamiento exacto: mismas validaciones, mismos flujos, mismos estados.

## 3) Formato de entrega (obligatorio)
- 1 archivo por respuesta.
- Devuelve SOLO el bloque cambiado + instrucciones exactas de reemplazo.
- Encapsula cambios con marcadores:

```csharp
// GT-BEGIN: Validación de email
if (string.IsNullOrWhiteSpace(email))
    return false;
// GT-END
```

**Instrucciones:** Reemplazar líneas 45-47 en `LoginViewModel.cs`

- Máximo ~60–120 líneas modificadas por paso. Si excede, divide en pasos.

---

## 4) Proceso de reducción de tamaño

### A) XAML
1) Añade marcadores GT-BEGIN/GT-END por secciones sin cambiar el layout.
2) Si hay `ItemTemplate` grande:
   - Mover a `ResourceDictionary` (Templates/) y referenciar por `StaticResource`.
3) Estilos repetidos:
   - Extraer a `Styles/` (ResourceDictionary) y usar `Style`/`BasedOn`.
4) Si aún es grande (>400 líneas):
   - Extraer secciones a `UserControl` en `Controls/` heredando `DataContext`.
   - **Cuándo extraer UserControl:**
     - ✅ Sección XAML >80 líneas repetible
     - ✅ Tiene su propia lógica de negocio
     - ✅ Aparece en >2 vistas
   - **NO extraer si:**
     - ❌ Solo mejora estética (usar Styles)
     - ❌ Es un ItemTemplate (usar ResourceDictionary)
   - El code-behind del UserControl debe ser mínimo (solo lo inevitable).

### B) C# (.cs)
1) Añade GT-BEGIN/GT-END por bloques lógicos sin cambiar comportamiento:
   - INIT, LOAD, SAVE, FILTERS, VALIDATION, MAPPING, NAVIGATION, LOGGING, ERRORS.
2) Extrae funciones puras (sin IO):
   - Formateo HH:mm, parsing, cálculos, validaciones simples => `Helpers/` o `Extensions/`.
3) Extrae IO/API/DB/HTTP:
   - Mantener en `Services/` para que el .cs principal sea orquestador.
4) Si aún supera 700 líneas:
   - Aplicar `partial class` con criterio (ver sección 6).

---

## 5) Estilo de código: XML Comments

### Formato obligatorio
- **1 línea:** `/// <summary>Texto.</summary>` (NO multilínea)
- **Punto final:** Todas las descripciones terminan con `.`
- **Si >80 chars:** Resumir + usar `<remarks>` en 1 línea:
  ```csharp
  /// <summary>Valida credenciales de usuario.</summary>
  /// <remarks>Usa bcrypt para hash y verifica expiración de contraseña.</remarks>
  ```
- **NO usar `//`** para documentar APIs públicas (convertir a XML de 1 línea)

### Alcance
- Solo tocar documentación XML (`///`).
- No reordenar código, no reformatear bloques, no cambiar firmas.
- Si un miembro público NO tiene XML docs, NO añadirlos salvo que el proyecto ya lo exija.

### Entrega
Solo bloques modificados con `GT-BEGIN/GT-END`.

---

## 6) Criterio para partial classes

### Cuándo SÍ usar `partial`
✅ Archivo >700 líneas + separable por responsabilidad:
 - `*.Commands.cs`: >10 comandos (RelayCommand, AsyncCommand)
 - `*.Loading.cs`: >5 métodos de carga (LoadAsync, RefreshAsync)
 - `*.Validation.cs`: >8 métodos de validación
 - `*.Mapping.cs`: >6 conversiones (ToDto, FromDto)
 - `*.Logging.cs`: Solo si logging es >100 líneas

### Cuándo NO usar `partial`
❌ NO usar si:
 - El archivo es <700 líneas
 - El problema se resuelve extrayendo a `Services/` o `Helpers/`
 - Es code-behind XAML: ahí mantener mínimo y mover lo demás a VM/Services

### Estructura recomendada
Mantener el MISMO nombre de clase y namespace:
```
ViewModels/
├── DiarioViewModel.cs (archivo principal)
├── DiarioViewModel.Commands.cs
├── DiarioViewModel.Loading.cs
└── DiarioViewModel.Validation.cs
```

### Reglas de partial
- No mover campos privados críticos sin revisar inicialización.
- No duplicar propiedades/commands.
- Los `partial` no deben cambiar comportamiento ni orden lógico del flujo.
- Si hay estado compartido, mantenerlo en el archivo principal y exponerlo por miembros existentes (sin renombrar).

---

## 7) Referencias de proyecto

### Publicación / Instalación
Ver documentación separada: `Docs/BUILD_MSI_v1.9.3_BETA.md`

**Notas clave:**
- Ruta de instalación: `C:\App\GestionTime-Desktop` (hardcoded)
- Versión actual: Definida en `Directory.Build.props`
- Archivos críticos: `.exe`, `.pri`, `Assets/`, `window-config.ini`, `appsettings.json`

**Copilot NO debe editar guías de publicación salvo petición explícita.**

---

## 8) Troubleshooting

Si Copilot sugiere cambios no solicitados o masivos:
1. Responder: **"Revisar §2 (Reglas de seguridad)"**
2. Pedir: **"Solo bloques con GT-BEGIN/GT-END por §3"**
3. Si persiste: **"Dividir en pasos de máx 120 líneas"**

Si no encuentra DTOs/Models/Helpers existentes:
1. Verificar con: `code_search` o `get_symbols_by_name`
2. Reutilizar antes de crear duplicados
