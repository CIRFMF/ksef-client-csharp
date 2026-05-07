
namespace KSeF.Client.Core.Models.Authorization
{
    public class AuthenticationKsefTokenRequest
    {
        public string Challenge { get; set; }
        public AuthenticationTokenContextIdentifier ContextIdentifier { get; set; }
        public string EncryptedToken { get; set; }
#nullable enable
        public string? PublicKeyId { get; set; }

        public AuthenticationTokenAuthorizationPolicy AuthorizationPolicy { get; set; }
    }
}