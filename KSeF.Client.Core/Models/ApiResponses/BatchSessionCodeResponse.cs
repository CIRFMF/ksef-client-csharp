namespace KSeF.Client.Core.Models.ApiResponses
{
    /// <summary>
    /// Kody statusów sesji wsadowej.
    /// </summary>
    public static class BatchSessionCodeResponse
    {
        /// <summary>
        /// Kod 100 — Sesja wsadowa rozpoczęta.
        /// </summary>
        public const int SessionStarted = 100;

        /// <summary>
        /// Kod 150 — Trwa przetwarzanie.
        /// </summary>
        public const int Processing = 150;

        /// <summary>
        /// Kod 200 — Sesja wsadowa przetworzona pomyślnie.
        /// </summary>
        public const int ProcessedSuccessfully = 200;

        /// <summary>
        /// Kod 400 — Nieprawidłowe żądanie.
        /// </summary>
        public const int BadRequest = 400;

        /// <summary>
        /// Kod 401 — Brak autoryzacji (niepoprawne lub brakujące poświadczenia).
        /// </summary>
        public const int Unauthorized = 401;

        /// <summary>
        /// Kod 403 — Brak uprawnień do wykonania operacji.
        /// </summary>
        public const int Forbidden = 403;

        /// <summary>
        /// Kod 405 — Błąd weryfikacji poprawności dostarczonych elementów paczki.
        /// </summary>
        public const int ValidationError = 405;

        /// <summary>
        /// Kod 415 — Błąd odszyfrowania dostarczonego klucza.
        /// </summary>
        public const int KeyDecryptionError = 415;

        /// <summary>
        /// Kod 420 — Przekroczony limit faktur w sesji.
        /// </summary>
        public const int InvoiceLimitExceeded = 420;

        /// <summary>
        /// Kod 430 — Błąd dekompresji pierwotnego archiwum.
        /// </summary>
        public const int ArchiveDecompressionError = 430;

        /// <summary>
        /// Kod 435 — Błąd odszyfrowania zaszyfrowanych części archiwum.
        /// </summary>
        public const int ArchivePartDecryptionError = 435;

        /// <summary>
        /// Kod 440 — Sesja anulowana, przekroczono czas wysyłki.
        /// </summary>
        public const int SessionTimeoutCancelled = 440;

        /// <summary>
        /// Kod 445 — Błąd weryfikacji, brak poprawnych faktur.
        /// </summary>
        public const int NoValidInvoices = 445;

        /// <summary>
        /// Kod 500 — Nieznany błąd ({statusCode}).
        /// </summary>
        public const int UnknownError = 500;
    }
}