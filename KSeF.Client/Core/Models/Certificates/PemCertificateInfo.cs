
namespace KSeF.Client.Core.Models.Certificates;

public class PemCertificateInfo
{
    public string CertificatePem { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public ICollection<PublicKeyCertificateUsage> Usage { get; set; }
}
