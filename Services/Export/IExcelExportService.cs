using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionTime.Desktop.Models.Dtos;

namespace GestionTime.Desktop.Services.Export;

/// <summary>Servicio para exportar partes a archivos Excel (.xlsx).</summary>
public interface IExcelExportService
{
    /// <summary>Exporta una colección de partes a un archivo Excel.</summary>
    /// <param name="partes">Colección de partes a exportar</param>
    /// <param name="filePath">Ruta completa del archivo de destino (.xlsx)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Tarea que representa la operación asíncrona</returns>
    /// <exception cref="ArgumentNullException">Si partes o filePath son null/vacíos</exception>
    /// <exception cref="InvalidOperationException">Si hay errores durante la exportación</exception>
    Task ExportAsync(IEnumerable<ParteDto> partes, string filePath, CancellationToken cancellationToken = default);
}
