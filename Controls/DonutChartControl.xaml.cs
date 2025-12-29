using GestionTime.Desktop.Models;
using GestionTime.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace GestionTime.Desktop.Controls;

public sealed partial class DonutChartControl : UserControl
{
    public GraficaDiaViewModel? ViewModel { get; set; }
    
    private const double RADIO_EXTERIOR = 180;
    private const double RADIO_INTERIOR = 120;
    private const double GROSOR_BASE = 60;
    private const double GROSOR_SOLAPE = 70;
    
    private List<(Path Path, SegmentoGrafica Segmento)> _segmentosVisibles = new();
    
    public DonutChartControl()
    {
        this.InitializeComponent();
    }
    
    public void DibujarGrafica(List<SegmentoGrafica> segmentos)
    {
        ChartCanvas.Children.Clear();
        _segmentosVisibles.Clear();
        
        if (segmentos == null || segmentos.Count == 0)
        {
            return;
        }
        
        var centro = new Point(200, 200);
        
        // Dibujar marcas de hora del reloj
        DibujarMarcasReloj(centro);
        
        foreach (var segmento in segmentos)
        {
            if (segmento.AnguloBarrido <= 0) continue;
            
            // Decidir si este segmento debe "explotar" por solape
            var offsetRadial = segmento.DebeExplotar && (ViewModel?.MostrarSolapes ?? true) ? 10 : 0;
            
            // EFECTO 3D: Dibujar sombra primero (debajo del segmento)
            var pathSombra = CrearArcoDonut(
                new Point(centro.X + 3, centro.Y + 3), // Offset para sombra
                RADIO_EXTERIOR,
                GROSOR_BASE,
                segmento.AnguloInicio,
                segmento.AnguloBarrido,
                offsetRadial);
            
            pathSombra.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 0, 0, 0));
            pathSombra.StrokeThickness = 0;
            
            ChartCanvas.Children.Add(pathSombra);
            
            // Dibujar halo si tiene solape
            if (segmento.DebeExplotar && (ViewModel?.MostrarSolapes ?? true))
            {
                var pathHalo = CrearArcoDonut(
                    centro,
                    RADIO_EXTERIOR + 10,
                    GROSOR_SOLAPE + 10,
                    segmento.AnguloInicio,
                    segmento.AnguloBarrido,
                    offsetRadial);
                
                pathHalo.Fill = new SolidColorBrush(
                    Windows.UI.Color.FromArgb(100, segmento.Color.R, segmento.Color.G, segmento.Color.B));
                pathHalo.StrokeThickness = 0;
                
                ChartCanvas.Children.Add(pathHalo);
            }
            
            // Dibujar segmento principal con degradado 3D
            var grosor = segmento.DebeExplotar ? GROSOR_SOLAPE : GROSOR_BASE;
            var path = CrearArcoDonut(
                centro,
                RADIO_EXTERIOR,
                grosor,
                segmento.AnguloInicio,
                segmento.AnguloBarrido,
                offsetRadial);
            
            // Crear degradado radial para efecto 3D
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0.3, 0.3),
                EndPoint = new Point(0.7, 0.7)
            };
            
            // Color base
            var colorBase = segmento.Color;
            
            // Color más claro (highlight)
            var colorClaro = Windows.UI.Color.FromArgb(
                255,
                (byte)Math.Min(255, colorBase.R + 40),
                (byte)Math.Min(255, colorBase.G + 40),
                (byte)Math.Min(255, colorBase.B + 40)
            );
            
            // Color más oscuro (shadow)
            var colorOscuro = Windows.UI.Color.FromArgb(
                255,
                (byte)Math.Max(0, colorBase.R - 30),
                (byte)Math.Max(0, colorBase.G - 30),
                (byte)Math.Max(0, colorBase.B - 30)
            );
            
            gradientBrush.GradientStops.Add(new GradientStop { Color = colorClaro, Offset = 0.0 });
            gradientBrush.GradientStops.Add(new GradientStop { Color = colorBase, Offset = 0.5 });
            gradientBrush.GradientStops.Add(new GradientStop { Color = colorOscuro, Offset = 1.0 });
            
            path.Fill = gradientBrush;
            path.Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 40));
            path.StrokeThickness = 2;
            path.Tag = segmento;
            
            // Animación de entrada
            path.Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 0.95,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, path);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");
            var sb = new Storyboard();
            sb.Children.Add(fadeIn);
            sb.Begin();
            
            ChartCanvas.Children.Add(path);
            _segmentosVisibles.Add((path, segmento));
            
            // Dibujar etiqueta de hora en el segmento
            DibujarEtiquetaHora(centro, segmento);
        }
        
        App.Log?.LogDebug("DonutChart dibujado: {count} segmentos con efecto 3D", segmentos.Count);
    }
    
    private void DibujarMarcasReloj(Point centro)
    {
        // Reloj simplificado: Mostrar solo horas intermedias (07-11 y 13-17)
        // Excluir 06:00, 12:00 y 18:00
        
        // Dibujar marcas de hora de 6 a 18 (pero solo números para 7-11 y 13-17)
        for (int hora = 6; hora <= 18; hora++)
        {
            var minutos = hora * 60;
            var minutosDesdeMediadia = minutos - 720; // 720 = 12:00
            var angulo = -90.0 + (minutosDesdeMediadia * 360.0 / 1440.0);
            while (angulo < 0) angulo += 360;
            while (angulo >= 360) angulo -= 360;
            
            var radianes = angulo * Math.PI / 180;
            
            // Línea de marca (más larga para las horas principales)
            bool esPrincipal = (hora == 6 || hora == 12 || hora == 18);
            var radioInterior = RADIO_INTERIOR - (esPrincipal ? 18 : 10);
            var radioExterior = RADIO_INTERIOR - 4;
            
            var puntoInicio = new Point(
                centro.X + radioInterior * Math.Cos(radianes),
                centro.Y + radioInterior * Math.Sin(radianes)
            );
            
            var puntoFin = new Point(
                centro.X + radioExterior * Math.Cos(radianes),
                centro.Y + radioExterior * Math.Sin(radianes)
            );
            
            var linea = new Line
            {
                X1 = puntoInicio.X,
                Y1 = puntoInicio.Y,
                X2 = puntoFin.X,
                Y2 = puntoFin.Y,
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(
                    esPrincipal ? (byte)220 : (byte)140, 160, 160, 160)),
                StrokeThickness = esPrincipal ? 3 : 1.5
            };
            
            ChartCanvas.Children.Add(linea);
        }
        
        // Dibujar números SOLO para horas 7-11 y 13-17 (excluir 6, 12, 18)
        for (int hora = 7; hora <= 17; hora++)
        {
            // Saltar 12:00
            if (hora == 12) continue;
            
            var minutos = hora * 60;
            var minutosDesdeMediadia = minutos - 720;
            var angulo = -90.0 + (minutosDesdeMediadia * 360.0 / 1440.0);
            while (angulo < 0) angulo += 360;
            while (angulo >= 360) angulo -= 360;
            
            var radianes = angulo * Math.PI / 180;
            
            var radioTexto = RADIO_INTERIOR - 32;
            var puntoTexto = new Point(
                centro.X + radioTexto * Math.Cos(radianes),
                centro.Y + radioTexto * Math.Sin(radianes)
            );
            
            var texto = new TextBlock
            {
                Text = $"{hora:D2}:00",
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Normal,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 140, 140, 140)),
                TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center
            };
            
            Canvas.SetLeft(texto, puntoTexto.X - 20);
            Canvas.SetTop(texto, puntoTexto.Y - 9);
            
            ChartCanvas.Children.Add(texto);
        }
    }
    
    private void DibujarEtiquetaHora(Point centro, SegmentoGrafica segmento)
    {
        // Calcular el ángulo medio del segmento
        var anguloMedio = segmento.AnguloInicio + (segmento.AnguloBarrido / 2);
        var radianes = anguloMedio * Math.PI / 180;
        
        // Posicionar la etiqueta en el medio del arco
        var radioEtiqueta = RADIO_EXTERIOR - (GROSOR_BASE / 2);
        var offsetRadial = segmento.DebeExplotar && (ViewModel?.MostrarSolapes ?? true) ? 10 : 0;
        
        var offsetX = offsetRadial * Math.Cos(radianes);
        var offsetY = offsetRadial * Math.Sin(radianes);
        
        var puntoEtiqueta = new Point(
            centro.X + (radioEtiqueta + offsetRadial) * Math.Cos(radianes) + offsetX,
            centro.Y + (radioEtiqueta + offsetRadial) * Math.Sin(radianes) + offsetY
        );
        
        // Solo mostrar etiquetas si el segmento es lo suficientemente grande
        if (segmento.AnguloBarrido < 8) return; // Reducido de 10 a 8 para mostrar más etiquetas
        
        // Crear fondo semi-transparente para mejor legibilidad
        var fondo = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 0, 0, 0)),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 2, 4, 2),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 255, 255, 255)),
            BorderThickness = new Thickness(1)
        };
        
        var texto = new TextBlock
        {
            FontSize = 9,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
            LineHeight = 10
        };
        
        // Formato según tamaño del segmento - siempre en formato HH:mm
        if (segmento.AnguloBarrido >= 25)
        {
            // Segmento grande: mostrar ambas horas en formato HH:mm
            texto.Text = $"{segmento.HoraInicio}\n{segmento.HoraFin}";
        }
        else if (segmento.AnguloBarrido >= 12)
        {
            // Segmento mediano: mostrar solo hora de inicio en formato HH:mm
            texto.Text = segmento.HoraInicio;
            texto.FontSize = 8;
        }
        else
        {
            // Segmento pequeño: mostrar solo la hora (sin minutos)
            var horaInicio = segmento.HoraInicio.Split(':')[0];
            texto.Text = $"{horaInicio}h";
            texto.FontSize = 8;
        }
        
        fondo.Child = texto;
        
        Canvas.SetLeft(fondo, puntoEtiqueta.X - 20);
        Canvas.SetTop(fondo, puntoEtiqueta.Y - 10);
        
        ChartCanvas.Children.Add(fondo);
    }
    
    private Path CrearArcoDonut(
        Point centro,
        double radioExterior,
        double grosor,
        double anguloInicio,
        double anguloBarrido,
        double offsetRadial)
    {
        var radioInterior = radioExterior - grosor;
        
        // Calcular offset si el segmento debe "explotar"
        var offsetX = offsetRadial * Math.Cos((anguloInicio + anguloBarrido / 2) * Math.PI / 180);
        var offsetY = offsetRadial * Math.Sin((anguloInicio + anguloBarrido / 2) * Math.PI / 180);
        var centroOffset = new Point(centro.X + offsetX, centro.Y + offsetY);
        
        var path = new Path();
        var geometry = new PathGeometry();
        var figure = new PathFigure();
        
        // Punto inicial (arco exterior)
        var inicioExterior = PuntoEnCirculo(centroOffset, radioExterior, anguloInicio);
        figure.StartPoint = inicioExterior;
        
        // Arco exterior
        var finExterior = PuntoEnCirculo(centroOffset, radioExterior, anguloInicio + anguloBarrido);
        var arcoExterior = new ArcSegment
        {
            Point = finExterior,
            Size = new Size(radioExterior, radioExterior),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = anguloBarrido > 180
        };
        figure.Segments.Add(arcoExterior);
        
        // Línea al interior
        var finInterior = PuntoEnCirculo(centroOffset, radioInterior, anguloInicio + anguloBarrido);
        figure.Segments.Add(new LineSegment { Point = finInterior });
        
        // Arco interior (reverso)
        var inicioInterior = PuntoEnCirculo(centroOffset, radioInterior, anguloInicio);
        var arcoInterior = new ArcSegment
        {
            Point = inicioInterior,
            Size = new Size(radioInterior, radioInterior),
            SweepDirection = SweepDirection.Counterclockwise,
            IsLargeArc = anguloBarrido > 180
        };
        figure.Segments.Add(arcoInterior);
        
        figure.IsClosed = true;
        geometry.Figures.Add(figure);
        path.Data = geometry;
        
        return path;
    }
    
    private Point PuntoEnCirculo(Point centro, double radio, double angulo)
    {
        var radianes = angulo * Math.PI / 180;
        return new Point(
            centro.X + radio * Math.Cos(radianes),
            centro.Y + radio * Math.Sin(radianes)
        );
    }
    
    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(ChartCanvas).Position;
        
        bool encontrado = false;
        
        foreach (var (path, segmento) in _segmentosVisibles)
        {
            if (ContieneRectangulo(path, pos))
            {
                TooltipText.Text = segmento.TooltipText;
                TooltipBorder.Visibility = Visibility.Visible;
                
                var tooltipX = Math.Min(pos.X + 15, ChartCanvas.ActualWidth - TooltipBorder.ActualWidth - 10);
                var tooltipY = Math.Min(pos.Y + 15, ChartCanvas.ActualHeight - TooltipBorder.ActualHeight - 10);
                
                Canvas.SetLeft(TooltipBorder, tooltipX);
                Canvas.SetTop(TooltipBorder, tooltipY);
                
                // Highlight 3D del segmento al hover
                path.Opacity = 1.0;
                path.StrokeThickness = 3;
                encontrado = true;
            }
            else
            {
                path.Opacity = 0.95;
                path.StrokeThickness = 2;
            }
        }
        
        if (!encontrado)
        {
            TooltipBorder.Visibility = Visibility.Collapsed;
        }
    }
    
    private void OnCanvasPointerExited(object sender, PointerRoutedEventArgs e)
    {
        TooltipBorder.Visibility = Visibility.Collapsed;
        
        foreach (var (path, _) in _segmentosVisibles)
        {
            path.Opacity = 0.95;
            path.StrokeThickness = 2;
        }
    }
    
    private void OnCanvasTapped(object sender, TappedRoutedEventArgs e)
    {
        var pos = e.GetPosition(ChartCanvas);
        
        foreach (var (path, segmento) in _segmentosVisibles)
        {
            if (ContieneRectangulo(path, pos))
            {
                App.Log?.LogInformation("Segmento seleccionado: {etiqueta} ({inicio} - {fin})", 
                    segmento.Etiqueta, segmento.HoraInicio, segmento.HoraFin);
                
                // TODO: Notificar a DiarioPage para filtrar
                break;
            }
        }
    }
    
    private bool ContieneRectangulo(Path path, Point punto)
    {
        try
        {
            // Hit test simple usando bounding box
            var transform = path.TransformToVisual(ChartCanvas);
            var bounds = path.Data.Bounds;
            var topLeft = transform.TransformPoint(new Point(bounds.X, bounds.Y));
            var bottomRight = transform.TransformPoint(new Point(bounds.X + bounds.Width, bounds.Y + bounds.Height));
            
            return punto.X >= topLeft.X && punto.X <= bottomRight.X &&
                   punto.Y >= topLeft.Y && punto.Y <= bottomRight.Y;
        }
        catch
        {
            return false;
        }
    }
}
