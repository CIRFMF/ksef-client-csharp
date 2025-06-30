namespace KSeF.Client.Core.Models.Certificates;

public class CertificateMetadataListResponse
{
    public ICollection<CertificateInfo> Certificates { get; set; }
    public bool HasMore { get; set; }
}
