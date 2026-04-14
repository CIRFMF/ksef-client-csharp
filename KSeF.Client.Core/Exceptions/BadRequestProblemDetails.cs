using System.Collections.Generic;

namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Reprezentuje odpowiedź Problem Details (application/problem+json) dla błędów HTTP 400 Bad Request.
    /// </summary>
    public class BadRequestProblemDetails
    {
        /// <summary>
        /// Krótki, czytelny tytuł błędu (np. "Bad Request").
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Kod statusu HTTP (400).
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// URI identyfikujące konkretne wystąpienie błędu.
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Ogólny opis problemu.
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// Lista błędów powiązanych z żądaniem.
        /// </summary>
        public List<BadRequestApiError> Errors { get; set; }

        /// <summary>
        /// Data i czas wystąpienia błędu w UTC.
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Identyfikator śledzenia błędu.
        /// </summary>
        public string TraceId { get; set; }
    }
}
