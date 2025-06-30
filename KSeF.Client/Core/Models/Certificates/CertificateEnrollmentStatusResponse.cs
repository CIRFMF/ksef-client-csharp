namespace KSeF.Client.Core.Models.Certificates;

public class CertificateEnrollmentStatusResponse
{
    public DateTime RequestDate { get; set; }
    public CertificateStatus Status { get; set; }
    public string RejectionReason { get; set; }
    public string CertificateSerialNumber { get; set; }
}
