namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Reprezentuje odpowiedź Problem Details (application/problem+json) dla błędów HTTP 429 Too Many Requests.
    /// </summary>
    public class TooManyRequestsProblemDetails
    {
        /// <summary>
        /// Krótki, czytelny tytuł błędu (np. "Too Many Requests").
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Kod statusu HTTP (429).
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// URI identyfikujące konkretne wystąpienie błędu.
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Informacja opisująca przyczynę przekroczenia limitu żądań oraz wskazówki
        /// dotyczące ponowienia żądania.
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
