# Agent rules (GestionTime)

File limits:
- *.xaml <= 400 lines
- *.cs <= 700 lines

Safe editing:
Only edit inside GT-BEGIN/GT-END blocks. If missing, add blocks first.
Never rewrite full files. Keep each step compilable.

Splitting:
- XAML: Templates -> Styles -> UserControls
- C#: partials -> Services -> Helpers
