namespace KSeF.Client.Core.Models.ApiResponses
{
    /// <summary>
    /// Bazowe kody statusów wspólne dla większości operacji.
    /// </summary>
    public static class CommonCodeResponse
    {
        /// <summary>
        /// Kod 200 — Operacja zakończona sukcesem.
        /// </summary>
        public const int Success = 200;

        /// <summary>
        /// Kod 201 — Nowy zasób został utworzony.
        /// </summary>
        public const int Created = 201;

        /// <summary>
        /// Kod 202 — Operacja zaakceptowana do realizacji.
        /// </summary>
        public const int Accepted = 202;

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
        /// Kod 500 — Wewnętrzny błąd serwera lub nieznany błąd.
        /// </summary>
        public const int InternalServerError = 500;
    }
}