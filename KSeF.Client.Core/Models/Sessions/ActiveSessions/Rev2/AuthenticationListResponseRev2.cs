using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Sessions.ActiveSessions.Rev2
{
    /// <summary>
    /// Odpowiedź <c>GET /auth/sessions/rev2</c>.
    /// </summary>
    public class AuthenticationListResponseRev2
    {
        public string ContinuationToken { get; set; }
        public ICollection<AuthenticationListItemRev2> Items { get; set; }
    }
}
