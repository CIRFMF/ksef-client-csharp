using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using System;

namespace KSeF.Client.Core.Models
{
    public class AuthStatus
    {
        public DateTimeOffset StartDate { get; set; }
        public AuthenticationMethodEnum AuthenticationMethod { get; set; }
        public StatusInfo Status { get; set; }
        public bool? IsTokenRedeemed { get; set; }
        public DateTimeOffset? LastTokenRefreshDate { get; set; }
        public DateTimeOffset? RefreshTokenValidUntil {get; set;}
    }
}