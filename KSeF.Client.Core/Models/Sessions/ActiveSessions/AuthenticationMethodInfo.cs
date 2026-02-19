namespace KSeF.Client.Core.Models.Sessions.ActiveSessions
{
    public class AuthenticationMethodInfo
    {
        public AuthenticationMethodInfoCategory Category { get; set; }
		public string Code { get; set; }
		public string DisplayName { get; set; }
	}

	public enum AuthenticationMethodInfoCategory
    {
		//Uwierzytelnienie podpisem Xades.
		XadesSignature,

		//Uwierzytelnienie za pomocą Węzła Krajowego (login.gov.pl).
		NationalNode,

		//Uwierzytelnienie tokenem.
		Token,

		//Uwierzytelnienie inną metodą.
		Other
	}
}
