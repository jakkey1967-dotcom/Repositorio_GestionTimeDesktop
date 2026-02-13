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
  - `GT-BEGIN`
  - `GT-END`
- Máximo ~60–120 líneas modificadas por paso. Si excede, divide en pasos.

---

## 4) Proceso de reducción de tamaño

### A) XAML
1) Añade marcadores GT-BEGIN/GT-END por secciones sin cambiar el layout.
2) Si hay `ItemTemplate` grande:
   - Mover a `ResourceDictionary` (Templates/) y referenciar por `StaticResource`.
3) Estilos repetidos:
   - Extraer a `Styles/` (ResourceDictionary) y usar `Style`/`BasedOn`.
4) Si aún es grande:
   - Extraer secciones a `UserControl` en `Controls/` heredando `DataContext`.
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

## 5) Estilo de código: XML Comments (C#) + StyleCop/Analyzers

### Objetivo
Normalizar comentarios XML para cumplir analyzers sin tocar lógica.

### Alcance
- Solo tocar documentación XML (`///`).
- No reordenar código, no reformatear bloques, no cambiar firmas, no tocar XAML.
- Si un miembro público NO tiene XML docs, NO añadirlos salvo que el proyecto ya lo exija.

### Reglas obligatorias (formato)
1) TODOS los XML docs en 1 línea:
   - Correcto: `/// <summary>Texto.</summary>`
   - Correcto: `/// <remarks>Texto.</remarks>`
   - Prohibido: `<summary>` multilínea.
2) Terminar SIEMPRE con punto final.
3) Si el texto pasa ~80 chars:
   - Acortar summary a 1 frase.
   - Detalles extra en `<remarks>` también 1 línea.
4) No usar `//` para documentar APIs públicas:
   - Si existe `//` describiendo un miembro público, convertirlo a XML doc 1 línea.
5) No convertir a multilínea bajo ningún motivo.

### Entrega de cambios de docs
- Devuelve SOLO bloques modificados con GT-BEGIN/GT-END + reemplazo exacto.

---

## 6) Criterio para partial classes (cuando usar y cómo)

### Cuándo SÍ usar `partial`
Usar `partial` SOLO si:
- El archivo supera ~700 líneas (o está cerca y seguirá creciendo).
- Hay bloques claramente separables sin dependencias circulares (commands/loading/validation/mapping).
- El objetivo es separar por responsabilidad sin cambiar la API pública.

### Cuándo NO usar `partial`
No usar `partial` si:
- El problema se resuelve extrayendo a `Services/` o `Helpers/`.
- Es code-behind XAML: ahí mantener mínimo y mover lo demás a VM/Services.

### Estructura recomendada (nombres)
Mantener el MISMO nombre de clase y namespace:
- `NombreClase.Commands.cs`
- `NombreClase.Loading.cs`
- `NombreClase.Validation.cs`
- `NombreClase.Mapping.cs`
- `NombreClase.Logging.cs` (solo si es grande y repetitivo)

### Reglas de partial
- No mover campos privados críticos sin revisar inicialización.
- No duplicar propiedades/commands.
- Los `partial` no deben cambiar comportamiento ni orden lógico del flujo.
- Si hay estado compartido, mantenerlo en el archivo principal y exponerlo por miembros existentes (sin renombrar).

---

## 7) Publicación / Instalación (fuera de este archivo)
Esta guía NO debe estar aquí (para evitar ruido y cambios accidentales).
Mover a `Docs/` y mantenerla como documentación separada (ej: `Docs/Publicacion-Instalacion.md`).
Copilot NO debe editar esa guía salvo petición explícita.
