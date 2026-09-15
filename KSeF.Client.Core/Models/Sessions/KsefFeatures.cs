namespace KSeF.Client.Core.Models.Sessions
{
    /// <summary>
    /// Wartości nagłówka <c>X-KSeF-Feature</c> używane przy otwieraniu sesji.
    /// </summary>
    public static class KsefFeatures
    {
        /// <summary>
        /// Nazwa nagłówka HTTP przekazywanego do API.
        /// </summary>
        public const string HeaderName = "X-KSeF-Feature";

        /// <summary>
        /// Włącza walidację numerów NIP oraz identyfikatorów wewnętrznych podmiotów wskazanych na fakturze.
        /// </summary>
        public const string SubjectIdentifierValidation = "subject-identifier-validation";
    }
}
