Objetivo: reducir tamaño del archivo sin romper nada y sin reescribirlo completo.
Límites: XAML <= 400 líneas, C# <= 700 líneas.

PRIORIDAD #1 (no romper comportamiento):
- NO cambies layout ni apariencia salvo lo imprescindible para extraer código.
- NO renombres bindings/propiedades/commands (ni x:Name si se usa).
- Cambios pequeños y compilables.

Proceso (pasos obligatorios):

A) Para XAML
1) Añade marcadores GT-BEGIN/GT-END por secciones SIN cambiar layout.
2) Si hay ItemTemplate grande, muévelo a Templates/ y referencia por StaticResource.
3) Mueve estilos repetidos a Styles/ (ResourceDictionary) y reemplaza repetición por Style.
4) Si aún supera límites, extrae secciones a UserControls (Controls/) heredando DataContext.

B) Para C# (.cs)
1) Añade marcadores GT-BEGIN/GT-END por bloques lógicos SIN cambiar comportamiento:
   INIT, LOAD, SAVE, FILTERS, VALIDATION, MAPPING, NAVIGATION, LOGGING, ERRORS.
2) Extrae funciones puras (formateo HH:mm, parsing, cálculos, utilidades) a Helpers/ o Extensions/.
3) Extrae IO/API/DB/HTTP a Services/ (el .cs queda como orquestador).
4) Si aún supera 700 líneas, divide en partial manteniendo el mismo nombre de clase:
   *.Commands.cs, *.Loading.cs, *.Validation.cs, *.Mapping.cs (si aplica).
Nota: en code-behind (*.xaml.cs) dejar mínimo (InitializeComponent + wiring UI inevitable). Lo demás a VM/Services/Helpers.

Edición segura:
- Devuelve SOLO el bloque cambiado + instrucciones exactas de reemplazo (BEGIN/END).
- 1 archivo por respuesta.
- Si requiere muchos cambios, divide en pasos (máx ~60–120 líneas modificadas por paso).

IMPORTANTE (solo si se requiere reestructurar el layout para lograr el objetivo):
1) Devuélveme primero SOLO la estructura base (Grid + VisualStates + layout).
2) Luego en una segunda respuesta, inserta controles dentro respetando bindings.
Si NO es necesario reestructurar, NO lo hagas.

Ahora te paso el archivo (o el bloque) a modificar:
[PEGA AQUÍ EL XAML o C#]
