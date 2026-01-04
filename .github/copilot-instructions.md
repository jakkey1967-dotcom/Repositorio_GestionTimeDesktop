Objetivo: reducir tamaño del archivo sin romper nada y sin reescribirlo completo.
Límites: XAML <= 400 líneas, C# <= 700 líneas.

PRIORIDAD #1 (no romper comportamiento):
- NO cambies layout ni apariencia salvo lo imprescindible para extraer código.
- NO renombres bindings/propiedades/commands.
- Cambios pequeños, compilables.

Proceso (pasos obligatorios):
Paso 1) Añade marcadores GT-BEGIN/GT-END por secciones SIN cambiar layout.
Paso 2) Si hay ItemTemplate grande, muévelo a Templates/ y referencia por StaticResource.
Paso 3) Mueve estilos repetidos a Styles/ (ResourceDictionary) y reemplaza repetición por Style.
Paso 4) Si aún supera límites, extrae secciones a UserControls (Controls/) heredando DataContext.

Edición segura:
- Devuelve SOLO el bloque cambiado + instrucciones exactas de reemplazo (BEGIN/END).
- 1 archivo por respuesta.

IMPORTANTE (solo si se requiere reestructurar el layout para lograr el objetivo):
1) Devuélveme primero SOLO la estructura base (Grid + VisualStates + layout).
2) En una segunda respuesta, inserta controles dentro respetando bindings.
Si NO es necesario reestructurar, NO lo hagas.
