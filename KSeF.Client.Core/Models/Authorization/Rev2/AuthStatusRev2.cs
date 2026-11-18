using KSeF.Client.Core.Models.Sessions.ActiveSessions.Rev2;
using System;

namespace KSeF.Client.Core.Models.Authorization.Rev2
{
    /// <summary>
    /// Status operacji uwierzytelniania zwracany przez <c>GET /auth/rev2/{referenceNumber}</c>.
    /// </summary>
    public class AuthStatusRev2
    {
        public DateTimeOffset StartDate { get; set; }
        public AuthenticationMethodInfoRev2 AuthenticationMethodInfo { get; set; }
        public OperationStatusInfo Status { get; set; }
        public bool? IsTokenRedeemed { get; set; }
        public DateTimeOffset? LastTokenRefreshDate { get; set; }
        public DateTimeOffset? RefreshTokenValidUntil { get; set; }
    }
}
