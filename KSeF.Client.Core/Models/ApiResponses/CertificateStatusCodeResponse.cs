namespace KSeF.Client.Core.Models.ApiResponses
{
    /// <summary>
    /// Kody statusów przetwarzania wniosku certyfikacyjnego.
    /// </summary>
    public static class CertificateStatusCodeResponse
    {
        /// <summary>
        /// Kod 100 — Wniosek przyjęty do realizacji.
        /// </summary>
        public const int RequestAccepted = 100;

        /// <summary>
        /// Kod 200 — Wniosek obsłużony (certyfikat wygenerowany).
        /// </summary>
        public const int RequestProcessedSuccessfully = 200;

        /// <summary>
        /// Kod 400 — Wniosek odrzucony.
        /// Klucz publiczny został już certyfikowany przez inny podmiot.
        /// </summary>
        public const int RequestRejectedPublicKeyAlreadyCertified = 400;

        /// <summary>
        /// Kod 400 — Wniosek odrzucony.
        /// Osiągnięto dopuszczalny limit posiadanych certyfikatów.
        /// </summary>
        public const int RequestRejectedCertificateLimitReached = 400;

        /// <summary>
        /// Kod 401 — Brak autoryzacji (niepoprawne lub brakujące poświadczenia).
        /// </summary>
        public const int Unauthorized = 401;

        /// <summary>
        /// Kod 500 — Nieznany błąd.
        /// </summary>
        public const int UnknownError = 500;

        /// <summary>
        /// Kod 550 — Operacja została anulowana przez system.
        /// Przetwarzanie zostało przerwane z przyczyn wewnętrznych systemu. Spróbuj ponownie później.
        /// </summary>
        public const int OperationCancelled = 550;
    }
}