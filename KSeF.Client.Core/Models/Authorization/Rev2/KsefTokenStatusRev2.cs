namespace KSeF.Client.Core.Models.Authorization.Rev2
{
    /// <summary>
    /// Status tokena KSeF w kontrakcie Rev2 (rozszerzalna wartość wyliczeniowa <c>code</c>/<c>rawCode</c>/<c>displayName</c>).
    /// </summary>
    public class KsefTokenStatusRev2 : ExtensibleEnumValue<KsefTokenStatusRev2Code>
    {
    }

    /// <summary>
    /// Kody statusu tokena KSeF w odpowiedziach Rev2.
    /// </summary>
    public enum KsefTokenStatusRev2Code
    {
        /// <summary>
        /// Token został utworzony, ale trwa jego aktywacja i nadawanie uprawnień. Nie może być jeszcze wykorzystywany do uwierzytelniania.
        /// </summary>
        Pending,

        /// <summary>
        /// Token jest aktywny i może być wykorzystywany do uwierzytelniania.
        /// </summary>
        Active,

        /// <summary>
        /// Token jest w trakcie unieważniania. Nie może już być wykorzystywany do uwierzytelniania.
        /// </summary>
        Revoking,

        /// <summary>
        /// Token został unieważniony i nie może być wykorzystywany do uwierzytelniania.
        /// </summary>
        Revoked,

        /// <summary>
        /// Nie udało się aktywować tokena. Należy wygenerować nowy token.
        /// </summary>
        Failed,

        /// <summary>
        /// Token został wygenerowany poprawnie, ale okres jego ważności jeszcze się nie rozpoczął.
        /// </summary>
        NotYetActive,

        /// <summary>
        /// Token został wygenerowany za pomocą tokena KSeF i oczekuje na zatwierdzenie.
        /// Do czasu zatwierdzenia nie może być używany do uwierzytelniania.
        /// </summary>
        AwaitingApproval,

        /// <summary>
        /// Upłynął czas 24 godzin oczekiwania na zatwierdzenie. Token nie może już zostać zatwierdzony
        /// ani być wykorzystywany do uwierzytelniania.
        /// </summary>
        ApprovalExpired,

        /// <summary>
        /// Termin ważności tokena upłynął.
        /// </summary>
        Expired,

        /// <summary>
        /// Status nieznany w momencie wydania bieżącej wersji modelu. Rzeczywisty kod jest dostępny w polu <c>rawCode</c>.
        /// </summary>
        FutureValue
    }

    /// <summary>
    /// Statusy tokenów, po których można filtrować listę tokenów Rev2 (GET /tokens/rev2).
    /// </summary>
    public enum KsefTokenStatusFilterRev2
    {
        /// <summary>
        /// Token w trakcie aktywacji i nadawania uprawnień.
        /// </summary>
        Pending,

        /// <summary>
        /// Token aktywny.
        /// </summary>
        Active,

        /// <summary>
        /// Token w trakcie unieważniania.
        /// </summary>
        Revoking,

        /// <summary>
        /// Token unieważniony.
        /// </summary>
        Revoked,

        /// <summary>
        /// Aktywacja tokena zakończona niepowodzeniem.
        /// </summary>
        Failed,

        /// <summary>
        /// Token wygenerowany poprawnie, ale termin jego ważności jeszcze się nie rozpoczął.
        /// </summary>
        NotYetActive,

        /// <summary>
        /// Token wygenerowany za pomocą tokena KSeF i oczekujący na zatwierdzenie.
        /// </summary>
        AwaitingApproval,

        /// <summary>
        /// Upłynął czas 24 godzin oczekiwania na zatwierdzenie.
        /// </summary>
        ApprovalExpired,

        /// <summary>
        /// Termin ważności tokena upłynął.
        /// </summary>
        Expired
    }
}
