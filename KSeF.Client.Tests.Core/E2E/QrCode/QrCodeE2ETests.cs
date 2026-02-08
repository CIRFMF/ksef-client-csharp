#nullable enable
using KSeF.Client.Api.Services;
using KSeF.Client.Tests.Utils;
using KSeF.Client.DI;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using KSeF.Client.Core.Models.QRCode;
using KSeF.Client.Extensions;

namespace KSeF.Client.Tests.Core.E2E.QrCode;

/// <summary>
/// Testy integracyjne generowania kodów QR certyfikatów i faktur.
/// </summary>
[Collection("QrCodeScenario")]
public class QrCodeE2ETests : TestBase
{
    private const int RsaKeySize = 2048;
    private const string GeneratedQrCodeLabel = "CERTYFIKAT";
    private const string InvoiceXml = "<x/>";
    private const int PixelsPerModule = 5;
    private const string Base64PngPrefix = "iVBOR";
    private const string CertificateSubjectName = "CN=Test";

    private readonly ECCurve CurveName = ECCurve.NamedCurves.nistP256;
    private readonly string Nip = MiscellaneousUtils.GetRandomNip();
    private readonly VerificationLinkService linkService;


    public QrCodeE2ETests()
    {
        linkService = new VerificationLinkService(new KSeFClientOptions() { BaseUrl = KsefQREnvironmentsUris.TEST, BaseQRUrl = KsefQREnvironmentsUris.TEST });
    }

    #region Testy RSA
    /// <summary>
    /// Test generowania kodu QR z certyfikatem RSA 2048-bit (klucz prywatny osadzony w certyfikacie).
    /// Bezpieczeństwo porównywalne z ECC P-256, ale większy rozmiar i wolniejsze operacje.
    /// </summary>
    [Fact]
    public void BuildCertificateQrWithEmbeddedPrivateKeyShouldReturnBase64Png()
    {
        //Arrange
        // Przygotowanie: certyfikat self-signed PFX z eksportowalnym kluczem
        using RSA rsa = RSA.Create(RsaKeySize);
        CertificateRequest req = new(
            CertificateSubjectName, rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss
        );
        X509Certificate2 fullCert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1)
        );
        byte[] pfxBytes = fullCert.Export(X509ContentType.Pfx);

        X509Certificate2 certWithKey = X509CertificateLoaderExtensions
            .LoadPkcs12(pfxBytes);

        string invoiceHash;
#if NETFRAMEWORK
        using (SHA256 sha = SHA256.Create()) { byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(InvoiceXml)); invoiceHash = Convert.ToBase64String(hashBytes); }
#else
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(InvoiceXml));
        invoiceHash = Convert.ToBase64String(hashBytes);
#endif

        // Act
        // Brak jawnego klucza prywatnego → użycie wbudowanego klucza
        string url = linkService.BuildCertificateVerificationUrl(Nip, QRCodeContextIdentifierType.Nip, Nip, invoiceHash, certWithKey);
        byte[] qrBytes = QrCodeService.GenerateQrCode(url, PixelsPerModule);
        byte[] labeled = QrCodeService.AddLabelToQrCode(qrBytes, GeneratedQrCodeLabel);
        string pngBase64 = Convert.ToBase64String(labeled);

        // Assert
        // Ciąg Base64 rozpoczyna się od standardowego PNG prefixu "iVBOR"
        Assert.StartsWith(Base64PngPrefix, pngBase64);
    }

    /// <summary>
    /// Test generowania kodu QR z certyfikatem RSA 2048-bit (tylko publiczny, klucz prywatny przekazany jawnie).
    /// </summary>
    [Fact]
    public void BuildCertificateQrPublicOnlyWithPrivateKeyParamShouldReturnBase64Png()
    {
        // Arrange
        // Certyfikat self-signed z kluczem RSA
        using RSA rsa = RSA.Create(RsaKeySize);
        CertificateRequest req = new(
            CertificateSubjectName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss
        );
        X509Certificate2 fullCert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1)
        );

        byte[] pfxBytes = fullCert.Export(X509ContentType.Pfx);

        X509Certificate2 certWithKey = X509CertificateLoaderExtensions
            .LoadPkcs12(pfxBytes);

        string invoiceHash;
#if NETFRAMEWORK
        using (SHA256 sha = SHA256.Create()) { byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(InvoiceXml)); invoiceHash = Convert.ToBase64String(hashBytes); }
#else
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(InvoiceXml));
        invoiceHash = Convert.ToBase64String(hashBytes);
#endif

        // Act
        // Nie podajemy privateKey — metoda użyje certWithKey.GetRSAPrivateKey()
        string url = linkService.BuildCertificateVerificationUrl(Nip, QRCodeContextIdentifierType.Nip,
            Nip,
            invoiceHash,
            certWithKey
        );

        byte[] qrBytes = QrCodeService.GenerateQrCode(url, PixelsPerModule);
        byte[] labeled = QrCodeService.AddLabelToQrCode(qrBytes, GeneratedQrCodeLabel);
        string pngBase64 = Convert.ToBase64String(labeled);

        // Assert
        // Base64 PNG zaczyna się od "iVBOR"
        Assert.StartsWith(Base64PngPrefix, pngBase64);
    }
#endregion

    #region Testy ECC (rekomendowane)
    /// <summary>
    /// Test generowania kodu QR z certyfikatem ECC (ECDSA P-256, klucz prywatny osadzony w certyfikacie).
    /// Rekomendowane: mniejsze klucze, szybsze operacje, krótsze linki QR.
    /// </summary>
    [Fact]
    public void BuildCertificateQrWithEmbeddedEccPrivateKeyShouldReturnBase64Png()
    {
        // Arrange
        // Certyfikat self-signed ECDSA prime256v1 (P-256)
        using ECDsa ecdsa = ECDsa.Create(CurveName);
        CertificateRequest req = new(
            CertificateSubjectName, ecdsa,
            HashAlgorithmName.SHA256
        );
        X509Certificate2 fullCert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1)
        );
        byte[] pfxBytes = fullCert.Export(X509ContentType.Pfx);

        X509Certificate2 certWithKey = X509CertificateLoaderExtensions
            .LoadPkcs12(pfxBytes);

        string invoiceHash;
#if NETFRAMEWORK
        using (SHA256 sha = SHA256.Create()) { invoiceHash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(InvoiceXml))); }
#else
        invoiceHash = Convert.ToBase64String(
        SHA256.HashData(Encoding.UTF8.GetBytes(InvoiceXml)));
#endif

        // Act
        // Brak jawnego klucza prywatnego → użycie osadzonego klucza ECDSA
        string url = linkService.BuildCertificateVerificationUrl(Nip, QRCodeContextIdentifierType.Nip, Nip, invoiceHash, certWithKey);
        byte[] qrBytes = QrCodeService.GenerateQrCode(url, PixelsPerModule);
        byte[] labeled = QrCodeService.AddLabelToQrCode(qrBytes, GeneratedQrCodeLabel);
        string pngBase64 = Convert.ToBase64String(labeled);

        // Assert
        // Base64 PNG zaczyna się od "iVBOR"
        Assert.StartsWith(Base64PngPrefix, pngBase64);
    }

    /// <summary>
    /// Test generowania kodu QR z certyfikatem ECC (ECDSA P-256, jawny import klucza prywatnego).
    /// </summary>
    [Fact]
    public void BuildCertificateQrPublicEccOnlyWithPrivateKeyParamShouldReturnBase64Png()
    {
        // Arrange
        // Certyfikat self-signed ECDSA prime256v1 (P-256)
        using ECDsa ecdsa = ECDsa.Create(CurveName);
        CertificateRequest req = new(
            CertificateSubjectName, ecdsa,
            HashAlgorithmName.SHA256
        );
        X509Certificate2 fullCert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1)
        );

        byte[] pfxBytes = fullCert.Export(X509ContentType.Pfx);

        X509Certificate2 certWithKey = X509CertificateLoaderExtensions
            .LoadPkcs12(pfxBytes);

        string invoiceHash;
#if NETFRAMEWORK
        using (SHA256 sha = SHA256.Create()) { invoiceHash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(InvoiceXml))); }
#else
        invoiceHash = Convert.ToBase64String(
SHA256.HashData(Encoding.UTF8.GetBytes(InvoiceXml)));
#endif

        // Act
        // Jawny import klucza prywatnego P-256
        string url = linkService.BuildCertificateVerificationUrl(Nip, QRCodeContextIdentifierType.Nip, Nip, invoiceHash, certWithKey);
        byte[] qrBytes = QrCodeService.GenerateQrCode(url, PixelsPerModule);
        byte[] labeled = QrCodeService.AddLabelToQrCode(qrBytes, GeneratedQrCodeLabel);
        string pngBase64 = Convert.ToBase64String(labeled);

        // Assert
        // Base64 PNG zaczyna się od "iVBOR"
        Assert.StartsWith(Base64PngPrefix, pngBase64);
    }
    #endregion
}