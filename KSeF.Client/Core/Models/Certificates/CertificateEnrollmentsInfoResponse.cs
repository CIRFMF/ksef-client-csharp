namespace KSeF.Client.Core.Models.Certificates;

public class CertificateEnrollmentsInfoResponse
{
    public ICollection<string> GivenNames { get; set; }
    public string Surname { get; set; }
    public string CommonName { get; set; }
    public string SerialNumber { get; set; }
    public string CountryName { get; set; }
    public string UniqueIdentifier { get; set; }
    public string OrganizationName { get; set; }
    public string OrganizationIdentifier { get; set; }
}
