using KSeF.Client.Api.Services;
using KSeF.Client.Core.Models.QRCode;
using KSeF.Client.DI;
using KSeF.Client.Extensions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Tests.Features.QrCode;

public class QrCodeTests
{
    private readonly VerificationLinkService _linkSvc;

    public QrCodeTests()
    {
        _linkSvc = new VerificationLinkService(new KSeFClientOptions()  {BaseUrl = KsefEnvironmentsUris.TEST, BaseQRUrl = KsefQREnvironmentsUris.TEST });
    }

    [Theory]
    [InlineData("RSA", "TestRsaPublic")]
    [InlineData("ECC", "TestEccPublic")]
    [Trait("Category", "QRCode")]
    [Trait("Scenario", "Zbuduj link weryfikujący używając certyfikatu publicznego")]
    [Trait("Expected", "InvalidOperationException")]
    public void GivenPublicOnlyCertificateWhenBuildingVerificationUrlThenThrowsInvalidOperationException(
        string keyType, string subjectName)
    {
        // Arrange
        X509Certificate2 publicCert;
        if (keyType == "RSA")
        {
            using RSA rsa = RSA.Create(2048);
            CertificateRequest req = new($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
            publicCert = cert.Export(X509ContentType.Cert).LoadCertificate();
        }
        else
        {
            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest req = new($"CN={subjectName}", ecdsa, HashAlgorithmName.SHA256);
            X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
            publicCert = cert.Export(X509ContentType.Cert).LoadCertificate();
        }

        string nip = "0000000000";
        string xml = "<x/>";
        string invoiceHash;
#if NETFRAMEWORK
        using SHA256 sha256 = SHA256.Create();
        invoiceHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(xml)));
#else
        invoiceHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(xml)));
#endif

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _linkSvc.BuildCertificateVerificationUrl(
                nip,
                QRCodeContextIdentifierType.Nip,
                nip,
                invoiceHash,
                publicCert
            )
        );
    }

}
