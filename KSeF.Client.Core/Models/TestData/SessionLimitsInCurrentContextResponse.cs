namespace KSeF.Client.Core.Models.TestData
{
    public class SessionLimitsInCurrentContextResponse
    {
        public SessionLimits OnlineSession { get; set; }
        public SessionLimits BatchSession { get; set; }
        public CollectiveIdentifierSessionLimits CollectiveIdentifier { get; set; }
        public TokensContextLimits Tokens { get; set; }
    }
}
