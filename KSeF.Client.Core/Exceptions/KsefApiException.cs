using System;
using System.Net;

namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Reprezentuje ustrukturyzowany wyjątek API zawierający szczegóły błędu zwrócone przez interfejs API.
    /// </summary>
    public class KsefApiException : Exception
    {
        /// <summary>
        /// Kod stanu HTTP odpowiedzi.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Opcjonalna odpowiedź błędu z API.
        /// </summary>
        public ApiErrorResponse Error { get; }

        /// <summary>
        /// Inicjalizuje nową instancję klasy <see cref="KsefApiException"/>.
        /// </summary>
        /// <param name="message">Szczegółowy komunikat wyjątku.</param>
        /// <param name="statusCode">Kod stanu HTTP.</param>
        /// <param name="error">Szczegóły błędu zwrócone przez API (opcjonalnie).</param>
        /// <param name="innerException">Wewnętrzny wyjątek, jeśli wystąpił (opcjonalnie).</param>
        public KsefApiException(string message, HttpStatusCode statusCode, ApiErrorResponse error = null, Exception innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            Error = error;
        }
    }
}