namespace KSeF.Client.Core.Models.Certificates;

public class CertificateStatus
{
    public int Code { get; set; }
    public string Description { get; set; }
    public ICollection<string> Details { get; set; }
}
