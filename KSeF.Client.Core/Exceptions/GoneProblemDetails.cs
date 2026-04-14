namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Reprezentuje odpowiedź Problem Details (application/problem+json) dla błędów HTTP 410 Gone.
    /// </summary>
    public class GoneProblemDetails
    {
        /// <summary>
        /// Krótki, czytelny tytuł błędu (np. "Gone").
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Kod statusu HTTP (410).
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
        /// Data i czas wystąpienia błędu w UTC.
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Identyfikator śledzenia błędu.
        /// </summary>
        public string TraceId { get; set; }
    }
}
