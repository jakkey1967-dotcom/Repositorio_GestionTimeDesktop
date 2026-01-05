# Instrucciones para agregar la imagen de login

## ?? Imagen Requerida

Para que el login funcione correctamente, necesitas guardar la imagen proporcionada en la carpeta Assets del proyecto.

## ?? Ubicación

Guarda la imagen de login como:
```
C:\GestionTime\GestionTime.Desktop\Assets\login_bg.png
```

## ??? Especificaciones de la Imagen

- **Nombre del archivo**: `login_bg.png`
- **Dimensiones recomendadas**: 485x640 píxeles (o mayor con la misma proporción)
- **Formato**: PNG (con transparencia si es necesario)
- **Contenido**: La imagen con el smartphone isométrico, gráficos, texto "GestiónTime" vertical, y el texto "INTRODUCIENDO SISTEMA OPERATIVO DE GESTIÓN"

## ?? Configuración del Proyecto

Si la carpeta `Assets` no existe, créala dentro del directorio del proyecto.

Después de guardar la imagen, asegúrate de que esté incluida en el proyecto:

1. En Visual Studio, click derecho en el proyecto ? Add ? Existing Item
2. Selecciona `login_bg.png`
3. En las propiedades del archivo (click derecho ? Properties):
   - **Build Action**: Content
   - **Copy to Output Directory**: Copy if newer

## ?? Imagen Alternativa

Si no tienes la imagen específica, el formulario se verá con un fondo teal sólido (#085F68) que ya está configurado como fallback.

Para crear una imagen personalizada, usa las siguientes características:
- Fondo con gradiente teal (#0B8C99 ? #085F68)
- Decoraciones diagonales con colores #0FA7B6 y #085F68
- Logo "GestiónTime" rotado verticalmente
- Ilustración isométrica del smartphone con gráficos
- Texto descriptivo en la parte inferior

## ? Verificación

Después de agregar la imagen, compila y ejecuta el proyecto. Deberías ver:
- Ventana de 1050x720 píxeles
- Card central de 970x640 píxeles
- Lado izquierdo con la imagen de branding
- Lado derecho con el formulario de login

Si la imagen no aparece, verifica:
1. Que el nombre del archivo sea exactamente `login_bg.png`
2. Que esté en la carpeta `Assets`
3. Que las propiedades del archivo estén configuradas correctamente (Build Action: Content)
4. Que la ruta en el XAML sea `ms-appx:///Assets/login_bg.png`

## ?? Troubleshooting

Si la imagen no carga:
- Verifica en el Output window de Visual Studio si hay errores de carga de recursos
- Revisa que el archivo se copie al directorio de salida (bin/Debug/...)
- Asegúrate de que el formato de la imagen sea válido (PNG)
