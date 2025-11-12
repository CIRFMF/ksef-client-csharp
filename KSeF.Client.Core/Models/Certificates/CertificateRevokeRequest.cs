namespace KSeF.Client.Core.Models.Certificates
{
    public class CertificateRevokeRequest
    {
        public CertificateRevocationReason? RevocationReason { get; set; }
    }
    public enum CertificateRevocationReason
    {
        Unspecified = 1,
        Superseded = 2,
        KeyCompromise = 3
    }
}
