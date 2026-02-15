using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionTime.Desktop.Models.Enums;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Services.Export;
using GestionTime.Desktop.Services.Reports;
using GestionTime.Desktop.ViewModels.Reports;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace GestionTime.Desktop.Views.Reports;

/// <summary>Ventana de Informes de Partes.</summary>
public sealed partial class ReportsWindow : Window
{
    public ReportsViewModel ViewModel { get; }
    private Window? _parentWindow;

    public ReportsWindow(InformesService informesService, UserRole userRole, Window parentWindow)
    {
        // IMPORTANTE: InitializeComponent DEBE ser lo primero para inicializar dispatcher/UI
        this.InitializeComponent();

        _parentWindow = parentWindow;

        // Obtener el ID del usuario actual desde el perfil
        var currentUserId = App.CurrentUserProfile?.Id;

        ViewModel = new ReportsViewModel(informesService, userRole, currentUserId);

        // Configurar tamaño de ventana y habilitar Ctrl+Alt+P
        WindowSizeManager.SetSizeForPage(this, typeof(ReportsWindow));

        Closed += OnWindowClosed;

        // GT-BEGIN: Suscribirse a cambios de Resumen para mostrar notificaciones
        // IMPORTANTE: Diferido al siguiente ciclo del dispatcher para evitar reentrancia
        // (PropertyChanged → modifica más propiedades → crash WinUI por reentrancia en x:Bind)
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.Resumen) && ViewModel.Resumen != null)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    try { ViewModel.ShowSearchResultNotifications(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ReportsWindow] Error en notificaciones: {ex}"); }
                });
            }
        };
        // GT-END

        // Carga inicial (fire-and-forget seguro)
        _ = SafeLoadInitialDataAsync();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        ViewModel.CancelSearch();
        _parentWindow?.Activate();
    }

    /// <summary>Wrapper seguro para LoadInitialDataAsync (evita crash en fire-and-forget).</summary>
    private async Task SafeLoadInitialDataAsync()
    {
        try
        {
            await ViewModel.LoadInitialDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReportsWindow] ERROR CRÍTICO en carga inicial: {ex}");
        }
    }

    private void OnScopeDay_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Scope = "day";
    }

    private void OnScopeWeek_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Scope = "week";
    }

    private void OnScopeRange_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Scope = "range";
    }

    private static string GetCurrentWeekIso()
    {
        var today = DateTime.Now;
        var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(today);
        return $"{today.Year}-W{weekNum:D2}";
    }

    // GT-BEGIN: Export / Share helpers

    /// <summary>Construye datos de exportación desde el ViewModel actual.</summary>
    private ReportExportData BuildExportData()
    {
        var data = new ReportExportData
        {
            Title = "Informe GestionTime",
            ScopeText = ViewModel.BannerContextText,
            AgentText = ViewModel.AgentDisplayText,
            PartsCount = ViewModel.Resumen?.PartsCount ?? 0,
            RecordedTime = ViewModel.RecordedTime,
            CoveredTime = ViewModel.CoveredTime,
            OverlapTime = ViewModel.OverlapTime,
            FirstStart = ViewModel.FirstStartFormatted,
            LastEnd = ViewModel.LastEndFormatted,
            StatusMessage = ViewModel.StatusMessage,
            WeekTotalHours = ViewModel.WeekTotalHours
        };

        foreach (var item in ViewModel.WeekChartItems)
        {
            data.DayBreakdown.Add(new DayExportRow
            {
                Day = item.DayLabel,
                Hours = item.HoursText,
                Percent8h = item.Percent8h,
                WeeklyPercent = item.Percentage
            });
        }

        return data;
    }

    private async void OnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Resumen == null) return;

        try
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"Informe_GestionTime_{DateTime.Now:yyyyMMdd_HHmm}"
            };
            savePicker.FileTypeChoices.Add("Excel", new List<string> { ".xlsx" });

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file == null) return;

            var service = new ReportExportService();
            await service.ExportToExcelAsync(BuildExportData(), file.Path);

            ViewModel.NotificationMessage = $"✅ Informe exportado a Excel: {file.Name}";
            ViewModel.NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
            ViewModel.ShowNotification = true;
        }
        catch (Exception ex)
        {
            ViewModel.NotificationMessage = $"❌ Error al exportar a Excel: {ex.Message}";
            ViewModel.NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
            ViewModel.ShowNotification = true;
        }
    }

    private async void OnExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Resumen == null) return;

        try
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"Informe_GestionTime_{DateTime.Now:yyyyMMdd_HHmm}"
            };
            savePicker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file == null) return;

            var service = new ReportExportService();
            await service.ExportToPdfAsync(BuildExportData(), file.Path);

            ViewModel.NotificationMessage = $"✅ Informe exportado a PDF: {file.Name}";
            ViewModel.NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
            ViewModel.ShowNotification = true;
        }
        catch (Exception ex)
        {
            ViewModel.NotificationMessage = $"❌ Error al exportar a PDF: {ex.Message}";
            ViewModel.NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
            ViewModel.ShowNotification = true;
        }
    }

    private async void OnShareEmail_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Resumen == null) return;

        try
        {
            var service = new ReportExportService();
            var body = service.BuildEmailBody(BuildExportData());
            var subject = $"Informe GestionTime - {ViewModel.BannerContextText}";

            var mailto = $"mailto:?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
            await Windows.System.Launcher.LaunchUriAsync(new Uri(mailto));
        }
        catch (Exception ex)
        {
            ViewModel.NotificationMessage = $"❌ Error al abrir email: {ex.Message}";
            ViewModel.NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
            ViewModel.ShowNotification = true;
        }
    }

    // GT-END
}
