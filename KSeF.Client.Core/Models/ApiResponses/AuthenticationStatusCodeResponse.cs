namespace KSeF.Client.Core.Models.ApiResponses
{
    /// <summary>
    /// Zbiór statusów uwierzytelnienia zwracanych przez proces pobierania statusu uwierzytelniania.
    /// </summary>
    public static class AuthenticationStatusCodeResponse
    {
        /// <summary>
        /// Kod 100 — Uwierzytelnianie w toku.
        /// </summary>
        public const int AuthenticationInProgress = 100;

        /// <summary>
        /// Kod 200 — Uwierzytelnienie zakończone sukcesem.
        /// </summary>
        public const int AuthenticationSuccess = 200;

        /// <summary>
        /// Kod 400 — Nieprawidłowe żądanie.
        /// </summary>
        public const int BadRequest = 400;

        /// <summary>
        /// Kod 401 — Brak autoryzacji (niepoprawne lub brakujące poświadczenia).
        /// </summary>
        public const int Unauthorized = 401;

        /// <summary>
        /// Kod 415 — Uwierzytelnienie zakończone niepowodzeniem. Brak przypisanych uprawnień.
        /// </summary>
        public const int AuthenticationFailedNoPermissions = 415;

        /// <summary>
        /// Kod 425 — Uwierzytelnienie unieważnione. 
        /// Uwierzytelnienie i powiązane refresh tokeny zostały unieważnione przez użytkownika.
        /// </summary>
        public const int AuthenticationRevoked = 425;

        /// <summary>
        /// Kod 450 — Uwierzytelnienie zakończone niepowodzeniem z powodu błędnego tokenu. Nieprawidłowy token.
        /// </summary>
        public const int InvalidToken = 450;

        /// <summary>
        /// Kod 450 — Uwierzytelnienie zakończone niepowodzeniem z powodu błędnego tokenu. Nieprawidłowy czas tokena.
        /// </summary>
        public const int InvalidTokenTime = 450;

        /// <summary>
        /// Kod 450 — Uwierzytelnienie zakończone niepowodzeniem z powodu błędnego tokenu. Token unieważniony.
        /// </summary>
        public const int RevokedToken = 450;

        /// <summary>
        /// Kod 450 — Uwierzytelnienie zakończone niepowodzeniem z powodu błędnego tokenu. Token nieaktywny.
        /// </summary>
        public const int InactiveToken = 450;

        /// <summary>
        /// Kod 460 — Uwierzytelnienie zakończone niepowodzeniem z powodu błędu certyfikatu. Nieważny certyfikat.
        /// </summary>
        public const int InvalidCertificate = 460;

        /// <summary>
        /// Kod 460 — Uwierzytelnienie zakończone niepowodzeniem z powodu błędu certyfikatu. 
        /// Błąd weryfikacji łańcucha certyfikatów.
        /// </summary>
        public const int CertificateChainError = 460;

        /// <summary>
        /// Kod 460 — Uwierzytelnienie zakończone niepowodzeniem z powodu błędu certyfikatu. 
        /// Niezaufany łańcuch certyfikatów.
        /// </summary>
        public const int UntrustedCertificateChain = 460;

        /// <summary>
        /// Kod 460 — Uwierzytelnienie zakończone niepowodzeniem z powodu błędu certyfikatu. 
        /// Certyfikat odwołany.
        /// </summary>
        public const int RevokedCertificate = 460;

        /// <summary>
        /// Kod 460 — Uwierzytelnienie zakończone niepowodzeniem z powodu błędu certyfikatu. 
        /// Niepoprawny certyfikat.
        /// </summary>
        public const int InvalidCertFormat = 460;

        /// <summary>
        /// Kod 470 — Uwierzytelnienie zakończone niepowodzeniem. 
        /// Próba wykorzystania metod autoryzacyjnych osoby zmarłej.
        /// </summary>
        public const int DeceasedUserAuthAttempt = 470;

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