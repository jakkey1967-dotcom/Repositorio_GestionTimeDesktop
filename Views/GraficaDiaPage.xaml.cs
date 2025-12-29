using GestionTime.Desktop.Models;
using GestionTime.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace GestionTime.Desktop.Views;

public sealed partial class GraficaDiaPage : Page
{
    public GraficaDiaViewModel ViewModel { get; } = new();
    
    public GraficaDiaPage()
    {
        this.InitializeComponent();
        
        // Configurar bindings manuales
        ChartControl.ViewModel = ViewModel;
        
        // Suscribirse a eventos del ViewModel
        ViewModel.SegmentosCalculados += OnSegmentosCalculados;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Configurar fecha inicial
        DpFecha.Date = DateTimeOffset.Now;
        ViewModel.FechaSeleccionada = DateTime.Today;
        
        // Cargar datos iniciales
        this.Loaded += OnPageLoaded;
    }
    
    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= OnPageLoaded;
        
        try
        {
            App.Log?.LogInformation("GraficaDiaPage cargada");
            await ViewModel.RecalcularCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "Error cargando página de gráfica");
        }
    }
    
    private void OnFechaChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (args.NewDate.HasValue)
        {
            ViewModel.FechaSeleccionada = args.NewDate.Value.DateTime.Date;
            App.Log?.LogInformation("Fecha cambiada a {fecha}", ViewModel.FechaSeleccionada.ToString("yyyy-MM-dd"));
        }
    }
    
    private void OnSegmentosCalculados(object? sender, System.Collections.Generic.List<SegmentoGrafica> segmentos)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ChartControl.DibujarGrafica(segmentos);
            LeyendaRepeater.ItemsSource = segmentos.Where(s => s.Etiqueta != "Tiempo muerto").ToList();
        });
    }
    
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.TotalTrabajado):
                    // Convertir minutos a formato HH:mm
                    var horas = ViewModel.TotalTrabajado / 60;
                    var minutos = ViewModel.TotalTrabajado % 60;
                    TxtTotalTrabajado.Text = $"{horas:D2}:{minutos:D2}";
                    TxtMetricaTrabajado.Text = FormatMinutos(ViewModel.TotalTrabajado);
                    break;
                    
                case nameof(ViewModel.TotalSolapado):
                    if (ViewModel.TotalSolapado > 0)
                    {
                        TxtTotalSolapado.Text = $"? {ViewModel.TotalSolapado} min solapados";
                        TxtTotalSolapado.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        TxtTotalSolapado.Visibility = Visibility.Collapsed;
                    }
                    TxtMetricaSolapado.Text = FormatMinutos(ViewModel.TotalSolapado);
                    break;
                    
                case nameof(ViewModel.TiempoMuerto):
                    TxtMetricaMuerto.Text = FormatMinutos(ViewModel.TiempoMuerto);
                    break;
                    
                case nameof(ViewModel.RankingTop):
                    TxtRanking.Text = ViewModel.RankingTop;
                    break;
                    
                case nameof(ViewModel.IsLoading):
                    LoadingRing.IsActive = ViewModel.IsLoading;
                    ChartControl.Visibility = ViewModel.IsLoading ? Visibility.Collapsed : Visibility.Visible;
                    break;
            }
        });
    }
    
    private string FormatMinutos(int minutos)
    {
        var horas = minutos / 60;
        var mins = minutos % 60;
        return $"{horas}h {mins}m";
    }
    
    private void OnRecalcularClick(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.RecalcularCommand.ExecuteAsync(null);
    }
    
    private void OnAgrupacionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbAgrupacion.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.AgrupacionActual = tag switch
            {
                "Ticket" => TipoAgrupacion.Ticket,
                "Cliente" => TipoAgrupacion.Cliente,
                "Tipo" => TipoAgrupacion.Tipo,
                "Grupo" => TipoAgrupacion.Grupo,
                _ => TipoAgrupacion.Individual
            };
        }
    }
    
    private void OnSolapesToggled(object sender, RoutedEventArgs e)
    {
        ViewModel.MostrarSolapes = TglSolapes.IsOn;
    }
}
