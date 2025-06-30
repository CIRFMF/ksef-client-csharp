

namespace KSeF.Client.Core.Models.Authorization;

public class QueryKsefTokensResponse
{
    /// <summary>
    /// Token służący do pobrania kolejnej strony wyników. Jeśli jest pusty, to nie ma kolejnych stron.
    /// </summary>
    public string ContinuationToken { get; set; }

    /// <summary>
    /// Lista tokenów uwierzytelniających.
    /// </summary>
    public ICollection<AuthenticationKsefToken> Tokens { get; set; }
}
