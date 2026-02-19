using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using System;

namespace KSeF.Client.Core.Models
{
    public class AuthStatus
    {
        public DateTimeOffset StartDate { get; set; }

		[Obsolete("Planowane wycofanie: 2026-11-16. Zaleca się korzystanie z AuthenticationMethodInfo.")]
		public AuthenticationMethodEnum AuthenticationMethod { get; set; }
        public AuthenticationMethodInfo AuthenticationMethodInfo { get; set; }
		public OperationStatusInfo Status { get; set; }
        public bool? IsTokenRedeemed { get; set; }
        public DateTimeOffset? LastTokenRefreshDate { get; set; }
        public DateTimeOffset? RefreshTokenValidUntil {get; set;}
    }
}