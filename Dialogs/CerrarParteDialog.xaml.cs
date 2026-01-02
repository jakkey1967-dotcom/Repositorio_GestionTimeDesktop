using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging; // 🆕 AGREGADO
using System;
using System.Text.RegularExpressions;
using GestionTime.Desktop.Models.Dtos;

namespace GestionTime.Desktop.Dialogs;

/// <summary>
/// Diálogo mejorado para cerrar un parte con validación robusta y UX profesional
/// </summary>
public sealed partial class CerrarParteDialog : ContentDialog
{
    // Regex para validar formato HH:mm estricto
    private static readonly Regex HoraRegex = new Regex(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);
    
    // Parte original que se va a cerrar
    private readonly ParteDto _parte;
    
    // Flag para evitar validación mientras se formatea
    private bool _suppressValidation = false;
    
    // Flag para detectar primera tecla después de recibir foco
    private bool _firstKeyAfterFocus = false;
    
    /// <summary>
    /// Hora de cierre confirmada por el usuario (null si canceló)
    /// </summary>
    public string? HoraCierreConfirmada { get; private set; }

    public CerrarParteDialog(ParteDto parte)
    {
        _parte = parte ?? throw new ArgumentNullException(nameof(parte));
        
        this.InitializeComponent();
        
        CargarDatosDelParte();
        ConfigurarEstadoInicial();
        
        App.Log?.LogInformation("📋 Diálogo CerrarParte abierto - Parte ID: {id}, HoraInicio: {inicio}", 
            _parte.Id, _parte.HoraInicio);
    }

    /// <summary>
    /// Carga los datos del parte original en los controles del diálogo
    /// </summary>
    private void CargarDatosDelParte()
    {
        try
        {
            // Fecha
            TxtFecha.Text = _parte.Fecha.ToString("dd/MM/yyyy");
            
            // Cliente
            TxtCliente.Text = string.IsNullOrWhiteSpace(_parte.Cliente) 
                ? "(Sin cliente)" 
                : _parte.Cliente;
            
            // Tienda (opcional - mostrar solo si existe)
            if (!string.IsNullOrWhiteSpace(_parte.Tienda))
            {
                TxtTienda.Text = _parte.Tienda;
                TxtTienda.Visibility = Visibility.Visible;
                LblTienda.Visibility = Visibility.Visible;
            }
            
            // Hora de inicio (DESTACADO)
            TxtHoraInicio.Text = _parte.HoraInicio ?? "--:--";
            
            // Chips de detalles (Ticket, Grupo, Tipo)
            if (!string.IsNullOrWhiteSpace(_parte.Ticket))
            {
                TxtTicket.Text = $"🎫 {_parte.Ticket}";
                ChipTicket.Visibility = Visibility.Visible;
            }
            
            if (!string.IsNullOrWhiteSpace(_parte.Grupo))
            {
                TxtGrupo.Text = $"📁 {_parte.Grupo}";
                ChipGrupo.Visibility = Visibility.Visible;
            }
            
            if (!string.IsNullOrWhiteSpace(_parte.Tipo))
            {
                TxtTipo.Text = $"🏷️ {_parte.Tipo}";
                ChipTipo.Visibility = Visibility.Visible;
            }
            
            App.Log?.LogDebug("✅ Datos del parte cargados en el diálogo correctamente");
        }
        catch (Exception ex)
        {
            App.Log?.LogError(ex, "❌ Error cargando datos del parte en el diálogo");
        }
    }

    /// <summary>
    /// Configura el estado inicial del diálogo
    /// </summary>
    private void ConfigurarEstadoInicial()
    {
        // Pre-rellenar con hora actual
        TxtHoraCierre.Text = DateTime.Now.ToString("HH:mm");
        
        // Deshabilitar botón primario inicialmente (se habilitará al validar)
        this.IsPrimaryButtonEnabled = false;
        
        // Validar el valor inicial
        ValidarYActualizarUI();
        
        // Focus en el campo de hora
        TxtHoraCierre.Focus(FocusState.Programmatic);
        TxtHoraCierre.SelectAll();
    }

    /// <summary>
    /// Botón "Ahora" - Establece la hora actual
    /// </summary>
    private void OnAhoraClick(object sender, RoutedEventArgs e)
    {
        var horaActual = DateTime.Now.ToString("HH:mm");
        
        _suppressValidation = true;
        TxtHoraCierre.Text = horaActual;
        _suppressValidation = false;
        
        ValidarYActualizarUI();
        
        App.Log?.LogDebug("🕐 Botón 'Ahora' presionado - Hora establecida: {hora}", horaActual);
    }

    /// <summary>
    /// Se dispara ANTES de que el texto cambie (permite filtrar caracteres)
    /// </summary>
    private void OnHoraCierreBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        // Si es la primera tecla después de recibir foco, permitir reemplazo total
        if (_firstKeyAfterFocus)
        {
            _firstKeyAfterFocus = false;
            return;
        }
        
        // Solo permitir dígitos y dos puntos
        if (!string.IsNullOrEmpty(args.NewText))
        {
            foreach (char c in args.NewText)
            {
                if (!char.IsDigit(c) && c != ':')
                {
                    args.Cancel = true;
                    App.Log?.LogDebug("❌ Carácter no permitido rechazado: '{char}'", c);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Se dispara cuando el usuario recibe foco en el campo
    /// </summary>
    private void OnHoraCierreGotFocus(object sender, RoutedEventArgs e)
    {
        _firstKeyAfterFocus = true;
        TxtHoraCierre.SelectAll();
        
        App.Log?.LogDebug("🎯 Campo HoraCierre recibió foco - Texto seleccionado para reemplazo");
    }

    /// <summary>
    /// Se dispara después de que el texto cambia
    /// </summary>
    private void OnHoraCierreTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressValidation)
            return;
        
        var text = TxtHoraCierre.Text ?? string.Empty;
        
        // Auto-formateo: insertar ":" automáticamente
        if (text.Length == 2 && !text.Contains(":"))
        {
            _suppressValidation = true;
            TxtHoraCierre.Text = text + ":";
            TxtHoraCierre.SelectionStart = 3; // Posicionar después del ":"
            _suppressValidation = false;
            
            App.Log?.LogDebug("🔧 Auto-formato aplicado: '{text}' → '{formatted}'", text, TxtHoraCierre.Text);
        }
        
        // Limitar longitud a 5 caracteres (HH:mm)
        if (text.Length > 5)
        {
            _suppressValidation = true;
            TxtHoraCierre.Text = text.Substring(0, 5);
            TxtHoraCierre.SelectionStart = 5;
            _suppressValidation = false;
        }
        
        // Validar y actualizar UI
        ValidarYActualizarUI();
    }

    /// <summary>
    /// Valida la hora ingresada y actualiza el estado del botón y mensajes
    /// </summary>
    private void ValidarYActualizarUI()
    {
        var text = TxtHoraCierre.Text?.Trim() ?? string.Empty;
        
        // Validar formato HH:mm
        bool esValido = HoraRegex.IsMatch(text);
        
        // Validación adicional: hora de cierre debe ser >= hora de inicio
        if (esValido && !string.IsNullOrEmpty(_parte.HoraInicio))
        {
            if (TimeSpan.TryParse(text, out var horaCierre) && 
                TimeSpan.TryParse(_parte.HoraInicio, out var horaInicio))
            {
                // Permitir que hora de cierre sea menor (parte que cruza medianoche)
                // pero loguear advertencia
                if (horaCierre < horaInicio)
                {
                    App.Log?.LogWarning("⚠️ Hora de cierre ({cierre}) es anterior a hora de inicio ({inicio}) - Posible cruce de medianoche",
                        text, _parte.HoraInicio);
                }
            }
        }
        
        // Actualizar UI según validación
        if (esValido)
        {
            // ✅ VÁLIDO
            this.IsPrimaryButtonEnabled = true;
            ErrorBorder.Visibility = Visibility.Collapsed;
            SuccessBorder.Visibility = Visibility.Visible;
            TxtHoraCierre.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Verde
            
            App.Log?.LogDebug("✅ Hora válida: {hora}", text);
        }
        else if (text.Length == 5)
        {
            // ❌ INVÁLIDO (5 caracteres pero formato incorrecto)
            this.IsPrimaryButtonEnabled = false;
            ErrorBorder.Visibility = Visibility.Visible;
            SuccessBorder.Visibility = Visibility.Collapsed;
            TxtError.Text = "Formato de hora inválido. Use HH:mm (00:00 - 23:59)";
            TxtHoraCierre.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Rojo
            
            App.Log?.LogDebug("❌ Hora inválida: {hora}", text);
        }
        else
        {
            // ⏳ INCOMPLETO
            this.IsPrimaryButtonEnabled = false;
            ErrorBorder.Visibility = Visibility.Collapsed;
            SuccessBorder.Visibility = Visibility.Collapsed;
            TxtHoraCierre.ClearValue(TextBox.BorderBrushProperty); // Restaurar borde por defecto
            
            App.Log?.LogDebug("⏳ Hora incompleta: {hora}", text);
        }
    }

    /// <summary>
    /// Se dispara cuando el usuario presiona "Cerrar"
    /// </summary>
    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var horaCierre = TxtHoraCierre.Text?.Trim() ?? string.Empty;
        
        // Validación final
        if (!HoraRegex.IsMatch(horaCierre))
        {
            args.Cancel = true; // Cancelar el cierre del diálogo
            
            ErrorBorder.Visibility = Visibility.Visible;
            TxtError.Text = "⚠️ Por favor, ingrese una hora válida antes de cerrar.";
            
            App.Log?.LogWarning("⚠️ Intento de cerrar con hora inválida: {hora}", horaCierre);
            return;
        }
        
        // Guardar resultado
        HoraCierreConfirmada = horaCierre;
        
        App.Log?.LogInformation("✅ Diálogo cerrado - Hora de cierre confirmada: {hora}", horaCierre);
    }

    /// <summary>
    /// Se dispara cuando el usuario presiona "Cancelar"
    /// </summary>
    private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        HoraCierreConfirmada = null;
        
        App.Log?.LogInformation("❌ Diálogo cancelado - No se cierra el parte");
    }
}
