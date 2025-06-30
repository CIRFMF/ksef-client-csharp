namespace KSeF.Client.Core.Models.Sessions.BatchSession;

public class PackagePartSignatureInitResponseType
{
    public string Method { get; set; }

    public int OrdinalNumber { get; set; }

    public Uri Url { get; set; }

    public IDictionary<string, string> Headers { get; set; }

}
