namespace KSeF.Client.Core.Models.Certificates;

public class CertificateInfo
{
    public string CertificateSerialNumber { get; set; }
    public string Name { get; set; }
    public string CommonName { get; set; }
    public string Status { get; set; }
    public string SubjectIdentifier { get; set; }
    public string SubjectIdentifierType { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidTo { get; set; }
    public DateTimeOffset LastUseDate { get; set; }
}
