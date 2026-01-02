using System;
using System.Collections.Generic;
using System.Linq;

namespace GestionTime.Desktop.Helpers
{
    /// <summary>
    /// Clase auxiliar para calcular intervalos de tiempo sin solapamiento
    /// </summary>
    public static class IntervalMerger
    {
        /// <summary>
        /// Representa un intervalo de tiempo
        /// </summary>
        public record Interval(DateTime Start, DateTime End)
        {
            public TimeSpan Duration => End - Start;
        }

        /// <summary>
        /// Resultado del cálculo de cobertura de intervalos
        /// </summary>
        public record CoverageResult(
            TimeSpan TotalCovered,      // Tiempo total cubierto (sin solapamiento)
            TimeSpan TotalOverlap,      // Tiempo total solapado
            List<Interval> MergedIntervals // Lista de intervalos unidos
        );

        /// <summary>
        /// Calcula la cobertura de tiempo sin solapamiento usando merge de intervalos
        /// </summary>
        /// <param name="intervals">Lista de intervalos a procesar</param>
        /// <returns>Resultado con tiempo cubierto, solapado e intervalos unidos</returns>
        public static CoverageResult ComputeCoverage(IEnumerable<Interval> intervals)
        {
            var intervalsList = intervals.ToList();
            
            // Si no hay intervalos, retornar resultado vacío
            if (!intervalsList.Any())
            {
                return new CoverageResult(
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    new List<Interval>()
                );
            }

            // Calcular duración total de todos los intervalos (incluyendo solapamientos)
            var totalDuration = intervalsList.Sum(i => i.Duration.TotalMinutes);

            // Ordenar intervalos por hora de inicio
            var sortedIntervals = intervalsList
                .OrderBy(i => i.Start)
                .ToList();

            // Merge de intervalos solapados o contiguos
            var mergedIntervals = new List<Interval>();
            var current = sortedIntervals[0];

            foreach (var next in sortedIntervals.Skip(1))
            {
                // Si el siguiente intervalo se solapa o es contiguo con el actual, fusionar
                if (next.Start <= current.End)
                {
                    // Extender el intervalo actual hasta el fin del siguiente
                    current = current with { End = Max(current.End, next.End) };
                }
                else
                {
                    // No hay solapamiento, guardar el intervalo actual y comenzar uno nuevo
                    mergedIntervals.Add(current);
                    current = next;
                }
            }
            
            // Agregar el último intervalo
            mergedIntervals.Add(current);

            // Calcular tiempo cubierto (suma de intervalos unidos)
            var totalCovered = TimeSpan.FromMinutes(
                mergedIntervals.Sum(i => i.Duration.TotalMinutes)
            );

            // Calcular tiempo solapado (diferencia)
            var totalOverlap = TimeSpan.FromMinutes(
                Math.Max(0, totalDuration - totalCovered.TotalMinutes)
            );

            return new CoverageResult(
                totalCovered,
                totalOverlap,
                mergedIntervals
            );
        }

        /// <summary>
        /// Formatea un intervalo como string (ej: "08:10-09:05")
        /// </summary>
        public static string FormatInterval(Interval interval)
        {
            return $"{interval.Start:HH:mm}–{interval.End:HH:mm}";
        }

        /// <summary>
        /// Formatea un TimeSpan como "Xh Ym" o "Xm" según corresponda
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            var totalMinutes = (int)Math.Round(duration.TotalMinutes);
            
            if (totalMinutes < 60)
            {
                return $"{totalMinutes}m";
            }
            
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;
            
            if (minutes == 0)
            {
                return $"{hours}h";
            }
            
            return $"{hours}h {minutes}m";
        }

        /// <summary>
        /// Helper para obtener el máximo de dos DateTime
        /// </summary>
        private static DateTime Max(DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }
    }
}
