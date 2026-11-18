namespace KSeF.Client.Core.Models.Authorization.Rev2
{
    /// <summary>
    /// Odpowiedź na żądanie wygenerowania tokena KSeF o określonym okresie ważności (POST /tokens/rev2).
    /// </summary>
    public class KsefTokenResponseRev2
    {
        /// <summary>
        /// Numer referencyjny tokena. Za jego pomocą można sprawdzić jego status lub go unieważnić.
        /// </summary>
        public string ReferenceNumber { get; set; }

        /// <summary>
        /// Token uwierzytelniający (zwracany tylko raz).
        /// </summary>
        public string Token { get; set; }
    }
}
