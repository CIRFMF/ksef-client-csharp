namespace KSeF.Client.Core.Models.Authorization;


public class OperationToken
{
    public string Token { get; set; }

    public DateTime ValidUntil { get; set; }
}

