namespace KSeF.Client.Core.Models.ApiResponses
{
    /// <summary>
    /// Kody statusów faktur w sesji.
    /// </summary>
    public static class InvoiceInSessionStatusCodeResponse
    {
        /// <summary>
        /// Kod 100 — Faktura przyjęta do dalszego przetwarzania.
        /// </summary>
        public const int AcceptedForProcessing = 100;

        /// <summary>
        /// Kod 150 — Trwa przetwarzanie.
        /// </summary>
        public const int Processing = 150;

        /// <summary>
        /// Kod 200 — Sukces.
        /// </summary>
        public const int Success = 200;

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
        /// Kod 405 — Przetwarzanie anulowane.
        /// </summary>
        public const int ProcessingCancelled = 405;

        /// <summary>
        /// Kod 410 — Nieprawidłowy zakres uprawnień.
        /// </summary>
        public const int InvalidPermissions = 410;

        /// <summary>
        /// Kod 415 — Brak możliwości wysyłania faktury z załącznikiem.
        /// </summary>
        public const int AttachmentNotAllowed = 415;

        /// <summary>
        /// Kod 430 — Błąd weryfikacji pliku faktury.
        /// </summary>
        public const int InvoiceFileValidationError = 430;

        /// <summary>
        /// Kod 435 — Błąd odszyfrowania pliku.
        /// </summary>
        public const int FileDecryptionError = 435;

        /// <summary>
        /// Kod 440 — Duplikat faktury.
        /// </summary>
        public const int DuplicateInvoice = 440;

        /// <summary>
        /// Kod 450 — Błąd weryfikacji semantyki dokumentu faktury.
        /// </summary>
        public const int InvoiceSemanticValidationError = 450;

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