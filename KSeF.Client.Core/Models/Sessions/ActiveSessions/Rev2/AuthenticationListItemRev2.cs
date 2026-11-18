using System;

namespace KSeF.Client.Core.Models.Sessions.ActiveSessions.Rev2
{
    /// <summary>
    /// Sesja uwierzytelnienia zwracana przez <c>GET /auth/sessions/rev2</c>.
    /// </summary>
    public class AuthenticationListItemRev2
    {
        public DateTimeOffset StartDate { get; set; }
        public AuthenticationMethodInfoRev2 AuthenticationMethodInfo { get; set; }
        public Status Status { get; set; }
        public bool? IsTokenRedeemed { get; set; }
        public DateTimeOffset? LastTokenRefreshDate { get; set; }
        public DateTimeOffset? RefreshTokenValidUntil { get; set; }
        public string ReferenceNumber { get; set; }
        public bool IsCurrent { get; set; }
    }
}
