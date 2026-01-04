using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GestionTime.Desktop.Helpers;

/// <summary>
/// Helper para navegación entre controles en ParteItemEdit
/// Gestiona el orden de TabIndex y movimiento entre campos
/// </summary>
public sealed class ParteItemEditNavigation
{
    private readonly List<(Control control, int tabIndex)> _controls;
    
    public ParteItemEditNavigation(
        CalendarDatePicker dpFecha,
        AutoSuggestBox txtCliente,
        TextBox txtTienda,
        TextBox txtHoraInicio,
        TextBox txtHoraFin,
        TextBox txtTicket,
        ComboBox cmbGrupo,
        ComboBox cmbTipo,
        TextBox txtAccion,
        Button btnGuardar,
        Button btnCancelar,
        Button btnSalir)
    {
        _controls = new List<(Control control, int tabIndex)>
        {
            (dpFecha, dpFecha.TabIndex),
            (txtCliente, txtCliente.TabIndex),
            (txtTienda, txtTienda.TabIndex),
            (txtHoraInicio, txtHoraInicio.TabIndex),
            (txtHoraFin, txtHoraFin.TabIndex),
            (txtTicket, txtTicket.TabIndex),
            (cmbGrupo, cmbGrupo.TabIndex),
            (cmbTipo, cmbTipo.TabIndex),
            (txtAccion, txtAccion.TabIndex),
            (btnGuardar, btnGuardar.TabIndex),
            (btnCancelar, btnCancelar.TabIndex),
            (btnSalir, btnSalir.TabIndex)
        };
    }
    
    /// <summary>
    /// Mueve el foco al siguiente control según el orden de TabIndex
    /// </summary>
    public void MoveToNextControl(Control? currentControl)
    {
        if (currentControl == null) return;
        
        var currentTabIndex = currentControl.TabIndex;
        App.Log?.LogDebug("Moviendo desde {name} (TabIndex={index})", 
            currentControl.Name, currentTabIndex);
        
        var nextControl = FindNextTabControl(currentTabIndex);
        
        if (nextControl != null)
        {
            App.Log?.LogDebug("Siguiente control: {name} (TabIndex={index})", 
                nextControl.Name, nextControl.TabIndex);
            nextControl.Focus(FocusState.Keyboard);
        }
        else
        {
            App.Log?.LogDebug("No se encontró siguiente control");
        }
    }
    
    /// <summary>
    /// Encuentra el siguiente control según TabIndex
    /// </summary>
    private Control? FindNextTabControl(int currentTabIndex)
    {
        var nextControl = _controls
            .Where(c => c.tabIndex > currentTabIndex && c.control.IsTabStop)
            .OrderBy(c => c.tabIndex)
            .FirstOrDefault();
        
        return nextControl.control;
    }
}
