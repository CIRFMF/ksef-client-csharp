
using System;

namespace KSeF.Client.Core.Models.Sessions.ActiveSessions
{
    public class AuthenticationListItem
    {
        public DateTimeOffset StartDate { get; set; }

		[Obsolete("Pole jest zwracane tylko przez operacje wycofywane 15.06.2027. Użyj AuthenticationMethodInfo albo operacji rev2.")]
		public AuthenticationMethodEnum AuthenticationMethod { get; set; }
		public AuthenticationMethodInfo AuthenticationMethodInfo { get; set; }

		public Status Status { get; set; }
        public bool? IsTokenRedeemed { get; set; }
        public DateTimeOffset? LastTokenRefreshDate { get; set; }
        public DateTimeOffset? RefreshTokenValidUntil { get; set; }
        public string ReferenceNumber { get; set; }
        public bool IsCurrent { get; set; }
    }
}
