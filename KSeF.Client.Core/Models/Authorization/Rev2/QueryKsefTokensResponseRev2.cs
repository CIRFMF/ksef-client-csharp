using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Authorization.Rev2
{
    /// <summary>
    /// Odpowiedź na zapytanie o listę tokenów KSeF w kontrakcie Rev2 (GET /tokens/rev2).
    /// </summary>
    public class QueryKsefTokensResponseRev2
    {
        /// <summary>
        /// Token służący do pobrania kolejnej strony wyników. Jeśli jest pusty, to nie ma kolejnych stron.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Lista tokenów uwierzytelniających.
        /// </summary>
        public ICollection<AuthenticationKsefTokenRev2> Tokens { get; set; }
    }
}
