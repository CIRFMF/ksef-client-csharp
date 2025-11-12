namespace KSeF.Client.Core.Models.ApiResponses
{
    /// <summary>
    /// Kody statusów eksportu paczek faktur.
    /// </summary>
    public static class InvoiceExportStatusCodeResponse
    {
        /// <summary>
        /// Kod 100 — Eksport faktur w toku.
        /// </summary>
        public const int ExportInProgress = 100;

        /// <summary>
        /// Kod 200 — Eksport faktur zakończony sukcesem.
        /// </summary>
        public const int ExportSuccess = 200;

        /// <summary>
        /// Kod 210 — Eksport faktur wygasł i nie jest już dostępny do pobrania.
        /// </summary>
        public const int ExportExpired = 210;

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
        /// Kod 500 — Nieznany błąd ({statusCode}).
        /// </summary>
        public const int UnknownError = 500;

        /// <summary>
        /// Kod 550 — Operacja została anulowana przez system.
        /// Przetwarzanie zostało przerwane z przyczyn wewnętrznych systemu. Spróbuj ponownie później.
        /// </summary>
        public const int OperationCancelled = 550;
    }
}