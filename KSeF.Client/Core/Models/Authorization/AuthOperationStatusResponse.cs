
namespace KSeF.Client.Core.Models.Authorization;


public class AuthOperationStatusResponse
{
    public TokenInfo AccessToken { get; set; }
    public TokenInfo RefreshToken { get; set; }
}

