# GestionTime - AGENT RULES

## File size limits (mandatory)
- C# (*.cs): keep <= 700 lines. If > 700, split (partial, services, helpers).
- XAML (*.xaml): keep <= 400 lines. If > 400, split (UserControls, Templates, Styles).

## Safe editing by blocks (mandatory)
Only edit code inside explicit markers. If markers are missing, add them first.
- XAML:
  <!-- GT-BEGIN:NAME -->
  <!-- GT-END:NAME -->
- C#:
  // GT-BEGIN:NAME
  // GT-END:NAME

## Anti-destruction rules
1) Do NOT rewrite full files. Return only the changed block + exact replace instructions.
2) Do NOT rename bindings/commands unless you update ALL references.
3) One file per step. Keep each step compilable.

## Splitting strategy
- XAML too big:
  1) Move ItemTemplate -> /Templates/*.xaml
  2) Move repeated properties -> /Styles/*.xaml (ResourceDictionary)
  3) Move sections -> /Controls/*Control.xaml (UserControls)
- C# too big:
  1) Use partials: *.Commands.cs, *.Loading.cs, *.Validation.cs
  2) IO/API -> /Services
  3) Pure logic -> /Helpers
  4) Keep code-behind minimal (InitializeComponent + unavoidable UI events)

## Logging
- Prefix: [AUTH][DIARIO][PARTE][API]
- Include CorrelationId + duration (ms)
- Never log secrets (password/token); mask them.
