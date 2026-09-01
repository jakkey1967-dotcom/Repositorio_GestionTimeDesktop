using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GestionTime.Desktop.Helpers;
using GestionTime.Desktop.Models.Export;
using Microsoft.UI.Xaml.Controls;

namespace GestionTime.Desktop.Dialogs;

/// <summary>Diálogo para exportar partes a Excel por rango de semanas ISO.</summary>
public sealed partial class ExportWeekDialog : ContentDialog, INotifyPropertyChanged
{
    private bool _isUpdatingDates;
    private DateTimeOffset? _startDate;
    private DateTimeOffset? _endDate;
    private int _selectedModeIndex;
    private bool _isRangeValid;
    private bool _showInvalidRange;
    private string _rangeSummaryText = string.Empty;
    private string _weeksSummaryText = string.Empty;
    private string _modeSummaryText = string.Empty;
    private string _filesSummaryText = string.Empty;

    /// <summary>Fecha inicial seleccionada en el DatePicker.</summary>
    public DateTimeOffset? StartDate
    {
        get => _startDate;
        set
        {
            if (_startDate == value)
                return;
            _startDate = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Fecha final seleccionada en el DatePicker.</summary>
    public DateTimeOffset? EndDate
    {
        get => _endDate;
        set
        {
            if (_endDate == value)
                return;
            _endDate = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Índice del modo: 0 unificado, 1 un archivo por semana.</summary>
    public int SelectedModeIndex
    {
        get => _selectedModeIndex;
        set
        {
            if (_selectedModeIndex == value)
                return;
            _selectedModeIndex = value;
            OnPropertyChanged();
            RefreshSummaries();
        }
    }

    /// <summary>Indica si el rango efectivo es válido.</summary>
    public bool IsRangeValid
    {
        get => _isRangeValid;
        private set
        {
            if (_isRangeValid == value)
                return;
            _isRangeValid = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Muestra el aviso de rango inválido.</summary>
    public bool ShowInvalidRange
    {
        get => _showInvalidRange;
        private set
        {
            if (_showInvalidRange == value)
                return;
            _showInvalidRange = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Texto del rango efectivo lunes-domingo.</summary>
    public string RangeSummaryText
    {
        get => _rangeSummaryText;
        private set
        {
            if (_rangeSummaryText == value)
                return;
            _rangeSummaryText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Texto con el número de semanas completas.</summary>
    public string WeeksSummaryText
    {
        get => _weeksSummaryText;
        private set
        {
            if (_weeksSummaryText == value)
                return;
            _weeksSummaryText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Texto del modo seleccionado.</summary>
    public string ModeSummaryText
    {
        get => _modeSummaryText;
        private set
        {
            if (_modeSummaryText == value)
                return;
            _modeSummaryText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Texto con el número estimado de archivos.</summary>
    public string FilesSummaryText
    {
        get => _filesSummaryText;
        private set
        {
            if (_filesSummaryText == value)
                return;
            _filesSummaryText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Lunes efectivo del rango.</summary>
    public DateTime EffectiveMonday { get; private set; }

    /// <summary>Domingo efectivo del rango.</summary>
    public DateTime EffectiveSunday { get; private set; }

    /// <summary>Semanas ISO incluidas en el rango.</summary>
    public IReadOnlyList<WeekOption> Weeks { get; private set; } = Array.Empty<WeekOption>();

    /// <summary>Modo de exportación seleccionado.</summary>
    public ExportMode SelectedMode => SelectedModeIndex == 1 ? ExportMode.OneFilePerWeek : ExportMode.Unified;

    public ExportWeekDialog()
    {
        this.InitializeComponent();
        InitializeDefaults();
    }

    /// <summary>Inicializa el diálogo con la semana ISO actual completa.</summary>
    public void InitializeDefaults()
    {
        var today = DateTime.Today;
        var monday = IsoWeekRangeHelper.GetMonday(today);
        var sunday = IsoWeekRangeHelper.GetSunday(today);
        _isUpdatingDates = true;
        StartDate = new DateTimeOffset(monday);
        EndDate = new DateTimeOffset(sunday);
        SelectedModeIndex = 0;
        _isUpdatingDates = false;
        RecalculateEffectiveRange();
        CalendarFirstDayHelper.Attach(DpStartDate);
        CalendarFirstDayHelper.Attach(DpEndDate);
    }

    /// <summary>Construye la solicitud de exportación a partir del rango actual.</summary>
    public ExportRangeRequest ToRequest()
    {
        return new ExportRangeRequest
        {
            EffectiveMonday = EffectiveMonday,
            EffectiveSunday = EffectiveSunday,
            Mode = SelectedMode,
            Weeks = Weeks
        };
    }

    private void OnStartDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (_isUpdatingDates)
            return;

        RecalculateEffectiveRange(normalizeStart: true);
    }

    private void OnEndDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (_isUpdatingDates)
            return;

        RecalculateEffectiveRange(normalizeEnd: true);
    }

    private void OnModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshSummaries();
    }

    private void RecalculateEffectiveRange(bool normalizeStart = false, bool normalizeEnd = false)
    {
        if (StartDate == null || EndDate == null)
        {
            IsRangeValid = false;
            ShowInvalidRange = false;
            Weeks = Array.Empty<WeekOption>();
            RangeSummaryText = "Selecciona fecha inicial y fecha final.";
            WeeksSummaryText = "Semanas completas: 0";
            RefreshSummaries();
            return;
        }

        var start = StartDate.Value.Date;
        var end = EndDate.Value.Date;
        var monday = IsoWeekRangeHelper.GetMonday(start);
        var sunday = IsoWeekRangeHelper.GetSunday(end);

        _isUpdatingDates = true;
        try
        {
            if (normalizeStart && StartDate.Value.Date != monday)
                StartDate = new DateTimeOffset(monday);
            if (normalizeEnd && EndDate.Value.Date != sunday)
                EndDate = new DateTimeOffset(sunday);
        }
        finally
        {
            _isUpdatingDates = false;
        }

        EffectiveMonday = monday;
        EffectiveSunday = sunday;
        ShowInvalidRange = sunday < monday;
        IsRangeValid = !ShowInvalidRange;
        Weeks = IsRangeValid ? IsoWeekRangeHelper.EnumerateWeeks(monday, sunday) : Array.Empty<WeekOption>();
        RangeSummaryText = ShowInvalidRange
            ? "Rango inválido: la fecha final es anterior a la inicial."
            : $"Rango efectivo: {monday:dd/MM/yyyy} (lunes) a {sunday:dd/MM/yyyy} (domingo)";
        WeeksSummaryText = $"Semanas completas incluidas: {Weeks.Count}";
        RefreshSummaries();
    }

    private void RefreshSummaries()
    {
        ModeSummaryText = SelectedMode == ExportMode.OneFilePerWeek
            ? "Modo: un archivo Excel por semana"
            : "Modo: unificar en un solo archivo Excel";

        if (!IsRangeValid)
        {
            FilesSummaryText = "Archivos estimados: 0";
            return;
        }

        FilesSummaryText = SelectedMode == ExportMode.Unified
            ? "Archivos estimados: 1 (el recuento de registros se calculará al exportar)"
            : $"Archivos estimados: hasta {Weeks.Count} (solo semanas con registros)";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

