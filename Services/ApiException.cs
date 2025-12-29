using System;
using System.Net;

namespace GestionTime.Desktop.Services
{
    /// <summary>
    /// Excepción personalizada para errores de API que incluye el mensaje del servidor
    /// </summary>
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string Path { get; }
        public string? ServerMessage { get; }
        public string? ServerError { get; }

        public ApiException(
            HttpStatusCode statusCode, 
            string path, 
            string? serverMessage = null,
            string? serverError = null) 
            : base(BuildMessage(statusCode, path, serverMessage, serverError))
        {
            StatusCode = statusCode;
            Path = path;
            ServerMessage = serverMessage;
            ServerError = serverError;
        }

        private static string BuildMessage(
            HttpStatusCode statusCode, 
            string path, 
            string? serverMessage,
            string? serverError)
        {
            // Construir mensaje de error amigable
            var msg = $"Error {(int)statusCode} ({statusCode})";
            
            // Si el servidor envió un mensaje de error, usarlo
            if (!string.IsNullOrWhiteSpace(serverMessage))
            {
                return $"{msg}: {serverMessage}";
            }
            
            if (!string.IsNullOrWhiteSpace(serverError))
            {
                return $"{msg}: {serverError}";
            }
            
            // Mensajes por defecto según código de estado
            return statusCode switch
            {
                HttpStatusCode.Unauthorized => $"{msg}: No autorizado. Por favor, inicia sesión nuevamente.",
                HttpStatusCode.Forbidden => $"{msg}: No tienes permisos para realizar esta acción.",
                HttpStatusCode.NotFound => $"{msg}: Recurso no encontrado.",
                HttpStatusCode.BadRequest => $"{msg}: Solicitud inválida.",
                HttpStatusCode.InternalServerError => $"{msg}: Error interno del servidor.",
                HttpStatusCode.ServiceUnavailable => $"{msg}: Servicio no disponible.",
                _ => $"{msg}: Error al procesar la solicitud."
            };
        }
    }
}
