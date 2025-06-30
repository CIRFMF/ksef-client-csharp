namespace KSeF.Client.Core.Models.Sessions.BatchSession;

public class OpenBatchSessionResponse
{
    public string ReferenceNumber { get; set; }

    public ICollection<PackagePartSignatureInitResponseType> PartUploadRequests { get; set; }

}
