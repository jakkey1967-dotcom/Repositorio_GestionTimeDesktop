
---
applyTo: "**/*.cs"
---

# C# Rules (Max 600–700 líneas)
- Si el archivo supera 600–700 líneas:
  1) Convertir a partial: *.Commands.cs, *.Loading.cs, *.Validation.cs
  2) Mover lógica de IO/API a Services
  3) Mover funciones puras a Helpers
- No reescribir clases completas. Devuelve cambios por bloque (GT-BEGIN/GT-END).
- MVVM:
  - Code-behind mínimo
  - Commands en ViewModel
  - Services sin UI
- Async/await correcto y sufijo Async.
