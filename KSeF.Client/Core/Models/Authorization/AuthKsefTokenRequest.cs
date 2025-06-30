
namespace KSeF.Client.Core.Models.Authorization;

public class AuthKsefTokenRequest
{
    public string Challenge { get; set; }
    public AuthContextIdentifier ContextIdentifier { get; set; }
    public string EncryptedToken { get; set; }

    public IpAddressPolicy IpAddressPolicy { get; set; } 
}
