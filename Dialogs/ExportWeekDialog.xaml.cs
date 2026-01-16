using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GestionTime.Desktop.Models.Export;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace GestionTime.Desktop.Dialogs;

/// <summary>Diálogo para seleccionar una semana y exportar partes a Excel.</summary>
public sealed partial class ExportWeekDialog : ContentDialog, INotifyPropertyChanged
{
    /// <summary>Lista de semanas disponibles para exportar.</summary>
    public List<WeekOption> Weeks { get; set; } = new();

    private WeekOption? _selectedWeek;
    /// <summary>Semana seleccionada por el usuario.</summary>
    public WeekOption? SelectedWeek
    {
        get => _selectedWeek;
        set
        {
            if (_selectedWeek != value)
            {
                _selectedWeek = value;
                OnPropertyChanged();
                IsWeekSelected = value != null;
            }
        }
    }

    private bool _isWeekSelected;
    /// <summary>Indica si hay una semana seleccionada (para habilitar botón Exportar).</summary>
    public bool IsWeekSelected
    {
        get => _isWeekSelected;
        set
        {
            if (_isWeekSelected != value)
            {
                _isWeekSelected = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _hasNoWeeks;
    /// <summary>Indica si no hay semanas disponibles.</summary>
    public bool HasNoWeeks
    {
        get => _hasNoWeeks;
        set
        {
            if (_hasNoWeeks != value)
            {
                _hasNoWeeks = value;
                OnPropertyChanged();
            }
        }
    }

    private int _recordCount;
    /// <summary>Número de registros que se exportarán.</summary>
    public int RecordCount
    {
        get => _recordCount;
        set
        {
            if (_recordCount != value)
            {
                _recordCount = value;
                OnPropertyChanged();
            }
        }
    }

    public ExportWeekDialog()
    {
        this.InitializeComponent();
    }

    /// <summary>Configura el diálogo con las semanas disponibles.</summary>
    /// <param name="weeks">Lista de semanas calculadas desde los datos</param>
    /// <param name="recordCounts">Diccionario con el conteo de registros por semana</param>
    public void SetWeeks(List<WeekOption> weeks, Dictionary<WeekOption, int> recordCounts)
    {
        Weeks = weeks ?? new List<WeekOption>();
        HasNoWeeks = !Weeks.Any();
        IsWeekSelected = false;
        RecordCount = 0;

        // Configurar el conteo de registros
        _recordCounts = recordCounts ?? new Dictionary<WeekOption, int>();

        App.Log?.LogInformation("📊 ExportWeekDialog: {count} semanas disponibles", Weeks.Count);
    }

    private Dictionary<WeekOption, int> _recordCounts = new();

    /// <summary>Evento cuando cambia la selección de semana.</summary>
    private void OnWeekSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsWeekSelected = SelectedWeek != null;

        if (SelectedWeek != null && _recordCounts.TryGetValue(SelectedWeek, out var count))
        {
            RecordCount = count;
            App.Log?.LogDebug("📊 Semana seleccionada: {week} - {count} registros", SelectedWeek.DisplayText, count);
        }
        else
        {
            RecordCount = 0;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

