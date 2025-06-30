using KSeF.Client.Core.Models.Certificates;

namespace KSeFClient.Api.Builders.Certificates;

public interface IRevokeCertificateRequestBuilder
{
    IRevokeCertificateRequestBuilder WithRevocationReason(CertificateRevocationReason revocationReason);
    CertificateRevokeRequest Build();
}

internal class RevokeCertificateRequestBuilderImpl : IRevokeCertificateRequestBuilder
{
    private CertificateRevocationReason _revocationReason = CertificateRevocationReason.Unspecified;
    private bool _revocationReasonSet = false;

    public static IRevokeCertificateRequestBuilder Create() => new RevokeCertificateRequestBuilderImpl();

    public IRevokeCertificateRequestBuilder WithRevocationReason(CertificateRevocationReason revocationReason)
    {
        _revocationReason = revocationReason;
        _revocationReasonSet = true;
        return this;
    }

    public CertificateRevokeRequest Build()
    {
        // If not set, default to Unspecified
        return new CertificateRevokeRequest
        {
            RevocationReason = _revocationReasonSet ? _revocationReason : CertificateRevocationReason.Unspecified
        };
    }
}

public static class RevokeCertificateRequestBuilder
{
    public static IRevokeCertificateRequestBuilder Create() =>
        RevokeCertificateRequestBuilderImpl.Create();
}
