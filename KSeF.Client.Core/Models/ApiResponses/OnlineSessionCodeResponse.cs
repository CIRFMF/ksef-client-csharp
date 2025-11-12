namespace KSeF.Client.Core.Models.ApiResponses
{
    /// <summary>
    /// Kody statusów sesji interaktywnej.
    /// </summary>
    public static class OnlineSessionCodeResponse
    {
        /// <summary>
        /// Kod 100 — Sesja interaktywna otwarta.
        /// </summary>
        public const int SessionOpened = 100;

        /// <summary>
        /// Kod 170 — Sesja interaktywna zamknięta.
        /// </summary>
        public const int SessionClosed = 170;

        /// <summary>
        /// Kod 200 — Sesja interaktywna przetworzona pomyślnie.
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
        /// Kod 415 — Błąd odszyfrowania dostarczonego klucza.
        /// </summary>
        public const int KeyDecryptionError = 415;

        /// <summary>
        /// Kod 440 — Sesja anulowana, nie przesłano faktur.
        /// </summary>
        public const int SessionCancelledNoInvoices = 440;

        /// <summary>
        /// Kod 445 — Błąd weryfikacji, brak poprawnych faktur.
        /// </summary>
        public const int NoValidInvoices = 445;

        /// <summary>
        /// Kod 500 — Nieznany błąd.
        /// </summary>
        public const int UnknownError = 500;
    }
}