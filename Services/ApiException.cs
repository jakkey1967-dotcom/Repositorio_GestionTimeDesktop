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

        /// <summary>
        /// Detecta si un mensaje contiene HTML
        /// </summary>
        private static bool IsHtmlContent(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            
            return text.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<HTML", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<head>", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<body>", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("<meta", StringComparison.OrdinalIgnoreCase) ||
                   (text.TrimStart().StartsWith("<") && text.Contains("</"));
        }

        private static string BuildMessage(
            HttpStatusCode statusCode, 
            string path, 
            string? serverMessage,
            string? serverError)
        {
            var statusNumber = (int)statusCode;
            
            // 🆕 Si el mensaje del servidor es HTML, ignorarlo y usar mensaje amigable
            var isServerMessageHtml = IsHtmlContent(serverMessage);
            var isServerErrorHtml = IsHtmlContent(serverError);
            
            // Si el servidor envió un mensaje de error NO HTML, usarlo
            if (!isServerMessageHtml && !string.IsNullOrWhiteSpace(serverMessage))
            {
                return serverMessage;
            }
            
            if (!isServerErrorHtml && !string.IsNullOrWhiteSpace(serverError))
            {
                return serverError;
            }
            
            // Mensajes amigables según código de estado
            return statusCode switch
            {
                HttpStatusCode.BadRequest => 
                    "Solicitud incorrecta: Los datos enviados no son válidos.",
                
                HttpStatusCode.Unauthorized => 
                    "No autorizado: Credenciales incorrectas o sesión expirada.",
                
                HttpStatusCode.Forbidden => 
                    "Acceso denegado: No tienes permisos para realizar esta acción.",
                
                HttpStatusCode.NotFound => 
                    "Recurso no encontrado: Verifica la configuración del servidor.",
                
                HttpStatusCode.RequestTimeout => 
                    "Tiempo de espera agotado: El servidor tardó demasiado en responder.",
                
                HttpStatusCode.TooManyRequests => 
                    "Servidor saturado: Demasiadas peticiones. Espera un momento e intenta nuevamente.",
                
                HttpStatusCode.InternalServerError => 
                    "Error interno del servidor: Problema en el servicio. Contacta al administrador.",
                
                HttpStatusCode.BadGateway => 
                    "Error de conexión: No se puede acceder al servidor.",
                
                HttpStatusCode.ServiceUnavailable => 
                    "Servicio no disponible: El servidor está temporalmente fuera de línea o en mantenimiento.",
                
                HttpStatusCode.GatewayTimeout => 
                    "Tiempo de espera del gateway: El servidor no responde a tiempo.",
                
                _ when statusNumber >= 500 => 
                    $"Error del servidor ({statusNumber}): Problema en el servicio. Intenta más tarde.",
                
                _ when statusNumber >= 400 => 
                    $"Error en la solicitud ({statusNumber}): La petición no pudo ser procesada.",
                
                _ => 
                    $"Error {statusNumber} ({statusCode}): Error al procesar la solicitud."
            };
        }
    }
}
