namespace KSeF.Client.Core.Models.TestData
{
    /// <summary>
    /// Limity tokenów KSeF w bieżącym kontekście.
    /// </summary>
    public class TokensContextLimits
    {
        /// <summary>
        /// Maksymalna liczba tokenów KSeF w kontekście wliczanych do limitu
        /// (statusy Active, NotYetActive, Pending i AwaitingApproval).
        /// </summary>
        public int MaxTokens { get; set; }
    }
}
