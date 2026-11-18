namespace KSeF.Client.Core.Models.Sessions.ActiveSessions.Rev2
{
    /// <summary>
    /// Metoda uwierzytelnienia w kontrakcie Rev2.
    /// </summary>
    public class AuthenticationMethodInfoRev2
    {
        /// <summary>
        /// Kategoria metody uwierzytelnienia.
        /// </summary>
        public ExtensibleEnumValue<AuthenticationMethodCategoryRev2Code> Category { get; set; }

        /// <summary>
        /// Kod metody uwierzytelnienia.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Nazwa metody uwierzytelnienia do wyświetlenia użytkownikowi.
        /// </summary>
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Kody kategorii metody uwierzytelnienia rozpoznawane przez rewizję Rev2.
    /// </summary>
    public enum AuthenticationMethodCategoryRev2Code
    {
        /// <summary>Uwierzytelnienie podpisem XAdES.</summary>
        XadesSignature,

        /// <summary>Uwierzytelnienie za pomocą Węzła Krajowego.</summary>
        NationalNode,

        /// <summary>Uwierzytelnienie tokenem.</summary>
        Token,

        /// <summary>Uwierzytelnienie inną metodą.</summary>
        Other,

        /// <summary>Kategoria nieznana w bieżącej rewizji; rzeczywisty kod jest dostępny w <c>RawCode</c>.</summary>
        FutureValue
    }
}
