namespace KSeF.Client.Core.Models.Authorization;

public class AuthChallengeResponse
{
    public string Challenge { get; set; }

    public DateTimeOffset Timestamp { get; set; }

}
