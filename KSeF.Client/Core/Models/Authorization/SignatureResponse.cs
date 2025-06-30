using KSeF.Client.Core.Models.Authorization;

namespace KSeFClient.Core.Models;


public class SignatureResponse
{
    public string ReferenceNumber { get; set; }

    public OperationToken AuthenticationToken { get; set; }

}

