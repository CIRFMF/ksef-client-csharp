namespace KSeF.Client.Core.Models.ApiResponses
{
    /// <summary>
    /// Kody statusów operacji (informacje o aktualnym statusie operacji).
    /// </summary>
    public static class OperationStatusCodeResponse
    {
        /// <summary>
        /// Kod 100 — Operacja przyjęta do realizacji.
        /// </summary>
        public const int AcceptedForProcessing = 100;

        /// <summary>
        /// Kod 200 — Operacja zakończona sukcesem.
        /// </summary>
        public const int Success = 200;

        /// <summary>
        /// Kod 400 — Operacja zakończona niepowodzeniem.
        /// </summary>
        public const int Failure = 400;

        /// <summary>
        /// Kod 401 — Brak autoryzacji (niepoprawne lub brakujące poświadczenia).
        /// </summary>
        public const int Unauthorized = 401;

        /// <summary>
        /// Kod 410 — Podane identyfikatory są niezgodne lub pozostają w niewłaściwej relacji.
        /// </summary>
        public const int InvalidOrInconsistentIdentifiers = 410;

        /// <summary>
        /// Kod 420 — Użyte poświadczenia nie mają uprawnień do wykonania tej operacji.
        /// </summary>
        public const int InsufficientPermissions = 420;

        /// <summary>
        /// Kod 430 — Kontekst identyfikatora nie odpowiada wymaganej roli lub uprawnieniom.
        /// </summary>
        public const int ContextRoleOrPermissionMismatch = 430;

        /// <summary>
        /// Kod 440 — Operacja niedozwolona dla wskazanych powiązań identyfikatorów.
        /// </summary>
        public const int OperationNotAllowedForIdentifierRelation = 440;

        /// <summary>
        /// Kod 450 — Operacja niedozwolona dla wskazanego identyfikatora lub jego typu.
        /// </summary>
        public const int OperationNotAllowedForIdentifierType = 450;

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