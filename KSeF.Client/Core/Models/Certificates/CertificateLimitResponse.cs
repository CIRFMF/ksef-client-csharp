namespace KSeF.Client.Core.Models.Certificates;

public class CertificateLimitResponse
{
    public bool CanRequest { get; set; }
    public Enrollment Enrollment { get; set; }
    public Certificate Certificate { get; set; }
}
