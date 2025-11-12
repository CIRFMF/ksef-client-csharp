using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.QRCode;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.DI;
using KSeF.Client.Extensions;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Tests.Core.E2E.QrCode;

public class QrCodeOfflineE2EScenarioFixture
{
    public string? AccessToken { get; set; }
    public string? Nip { get; set; }
    public string? InvoiceHash { get; set; }
    public string? PrivateKey { get; set; }
    public CertificateResponse? Certificate { get; set; }
    public DateTime? InvoiceDate { get; set; }
}

[CollectionDefinition("QrCodeOfflineE2EScenario")]
public class QrCodeE2EScenarioCollection
: ICollectionFixture<QrCodeOfflineE2EScenarioFixture>
{ }
[Collection("QrCodeOfflineE2EScenario")]
public class QrCodeOfflineE2ETests : TestBase
{
    private readonly QrCodeOfflineE2EScenarioFixture Fixture;
    private readonly VerificationLinkService linkService;
    private readonly QrCodeService qrCodeService;

    public QrCodeOfflineE2ETests(QrCodeOfflineE2EScenarioFixture fixture)
    {
        Fixture = fixture;
        Fixture.Nip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, Fixture.Nip).GetAwaiter().GetResult();
        Fixture.AccessToken = authInfo.AccessToken.Token;

        linkService = new VerificationLinkService(new KSeFClientOptions() { BaseUrl = KsefEnvironmentsUris.TEST });
        qrCodeService = new QrCodeService();
    }

    public record TestScenario(
        string Name,
        Func<IKSeFClient, string, ICryptographyService, Task<(string, string)>> GenerateCsrAndPrivateKey, // WithRsa lub WithEcdsa
        string InvoiceTemplateName,  // "fa(2)" lub "fa(3)"
        Func<X509Certificate2, string, X509Certificate2> GetCertWithPrivateKey  // GetCertWithRsaKey lub GetCertWithEcdsaKey
    );

    public static IEnumerable<object[]> TestScenarios => new[]
    {
        new object[] { new TestScenario("faktura FA(2), szyfrowanie RSA",
            async (ksefClient, accessToken, cryptographyService) => await CertificateUtils.GenerateCsrAndPrivateKeyWithRsaAsync(ksefClient, accessToken, cryptographyService, RSASignaturePadding.Pkcs1),
            "invoice-template-fa-2.xml", 
            (cert, key) => GetCertWithRsaKey(cert,key)) },
    
    
        new object[] { new TestScenario("faktura FA(2), szyfrowanie ECdsa",
            async (ksefClient, accessToken, cryptographyService) => await CertificateUtils.GenerateCsrAndPrivateKeyWithEcdsaAsync(ksefClient, accessToken, cryptographyService),
            "invoice-template-fa-2.xml", 
            (cert, key) => GetCertWithEcdsaKey(cert, key)) },
    
        new object[] { new TestScenario("faktura FA(3), szyfrowanie RSA",
            async (ksefClient, accessToken, cryptographyService) => await CertificateUtils.GenerateCsrAndPrivateKeyWithRsaAsync(ksefClient, accessToken, cryptographyService, RSASignaturePadding.Pkcs1),
            "invoice-template-fa-3.xml", 
            (cert, key) => GetCertWithRsaKey(cert, key)) },
    
        new object[] { new TestScenario("faktura FA(3), szyfrowanie ECdsa", 
            async(ksefClient, accessToken, cryptographyService) => await CertificateUtils.GenerateCsrAndPrivateKeyWithEcdsaAsync(ksefClient, accessToken, cryptographyService), 
            "invoice-template-fa-3.xml", 
            (cert, key) => GetCertWithEcdsaKey(cert, key)) }
    };

    /// <summary>
    /// End-to-end test weryfikujący pełny, zakończony sukcesem przebieg wystawienia kodów QR do faktury w trybie offline (offlineMode = true).
    /// Test używa faktury FA(2) lub FA(3) oraz certyfikatów szyfrowanych RSA lub ECDSA.
    /// </summary>
    /// <remarks>
    /// Kroki:
    /// 1. Autoryzacja, pozyskanie tokenu dostępu za pomocą RSA. (w konstruktorze).
    /// 2. Utworzenie Certificate Signing Request (CSR) oraz klucz prywatny za pomocą RSA lub ECDSA.
    /// 3. Zapisanie klucza prywatnego (private key).
    /// 4. Utworzenie i wysłanie żądania wystawienia certyfikatu KSeF.
    /// 5. Sprawdzenie statusu żądania, oczekiwanie na zakończenie przetwarzania CSR.
    /// 6. Pobranie certyfikatu KSeF.
    /// 7. Odfiltrowanie i zapisanie właściwego certyfikatu.
    /// Następnie cały proces odbywa się offline, bez kontaktu z KSeF:
    /// 8. Przygotowanie faktury FA(2) lub FA(3) w formacie XML.
    /// 9. Zapisanie skrótu faktury (hash).
    /// 10. Utworzenie odnośnika (Url) do weryfikacji faktury (KOD I).
    /// 11. Utworzenie kodu QR faktury (KOD I) dla trybu offline.
    /// 12. Utworzenie odnośnika (Url) do weryfikacji certyfikatu (KOD II).
    /// 13. Utworzenie kodu QR do weryfikacji certyfikatu (KOD II) dla trybu offline.
    /// </remarks>
    [Theory]
    [MemberData(nameof(TestScenarios))]
    public async Task QrCodeOfflineE2ETest(TestScenario testScenario)
    {
        //Utworzenie Certificate Signing Request (csr) oraz klucz prywatny za pomocą RSA lub ECDSA
        (string csr, string privateKey) = await testScenario.GenerateCsrAndPrivateKey(KsefClient, Fixture.AccessToken, CryptographyService);

        // Zapisanie klucza prywatnego (private key) do pamięci tylko na potrzeby testu, w rzeczywistości powinno być bezpiecznie przechowywane
        Fixture.PrivateKey = privateKey;

        //Utworzenie i wysłanie żądania wystawienia certyfikatu KSeF
        CertificateEnrollmentResponse certificateEnrollment = await CertificateUtils.SendCertificateEnrollmentAsync(KsefClient, Fixture.AccessToken, csr, CertificateType.Offline);

        //Sprawdzenie statusu żądania, oczekiwanie na zakończenie przetwarzania CSR
        CertificateEnrollmentStatusResponse enrollmentStatus = await KsefClient
            .GetCertificateEnrollmentStatusAsync(certificateEnrollment.ReferenceNumber, Fixture.AccessToken, CancellationToken.None);
        int numbersOfTriesForCertificate = 0;
        while (enrollmentStatus.Status.Code == 100 && numbersOfTriesForCertificate < 10)
        {
            await Task.Delay(1000);
            enrollmentStatus = await KsefClient
                            .GetCertificateEnrollmentStatusAsync(certificateEnrollment.ReferenceNumber, Fixture.AccessToken, CancellationToken.None);
            numbersOfTriesForCertificate++;
        }
        Assert.True(enrollmentStatus.Status.Code == 200);

        //Pobranie certyfikatu KSeF
        List<string> serialNumbers = new List<string> { enrollmentStatus.CertificateSerialNumber };
        CertificateListResponse certificateListResponse = await KsefClient
            .GetCertificateListAsync(new CertificateListRequest { CertificateSerialNumbers = serialNumbers }, Fixture.AccessToken, CancellationToken.None);
        Assert.NotNull(certificateListResponse);
        Assert.Single(certificateListResponse.Certificates);

        //Odfiltrowanie i zapisanie właściwego certyfikatu do pamięci, w rzeczywistości powinien być bezpiecznie przechowywany
        Fixture.Certificate = certificateListResponse.Certificates
            .Single(x => x.CertificateType == CertificateType.Offline &&
                        x.CertificateSerialNumber == enrollmentStatus.CertificateSerialNumber);
        Assert.NotNull(Fixture.Certificate);

        //=====Od tego momentu tryb offline bez dostępu do KSeF=====

        //Przygotowanie faktury FA(2) lub FA(3) w formacie XML
        string path = Path.Combine(AppContext.BaseDirectory, "Templates", testScenario.InvoiceTemplateName);
        string xml = File.ReadAllText(path, Encoding.UTF8);
        xml = xml.Replace("#nip#", Fixture.Nip);
        xml = xml.Replace("2025-09-01", "2025-10-01");

        Fixture.InvoiceDate = DateTime.Parse("2025-10-01");
        MemoryStream memoryStream = new(Encoding.UTF8.GetBytes(xml));

        //gotową fakturę należy zapisać, aby wysłać do KSeF później (zgodnie z obowiązującymi przepisami), oznaczoną jako offlineMode = true
        byte[] invoice = memoryStream.ToArray();

        FileMetadata? invoiceMetadata = CryptographyService.GetMetaData(invoice);
        //Zapisanie skrótu faktury (hash)
        Fixture.InvoiceHash = invoiceMetadata.HashSHA;

        //Utworzenie odnośnika (Url) do weryfikacji faktury (KOD I)
        string invoiceForOfflineUrl = linkService.BuildInvoiceVerificationUrl(Fixture.Nip, Fixture.InvoiceDate.Value, Fixture.InvoiceHash);

        Assert.NotNull(invoiceForOfflineUrl);
        Assert.Contains(Convert.FromBase64String(Fixture.InvoiceHash).EncodeBase64UrlToString(), invoiceForOfflineUrl);
        Assert.Contains(Fixture.Nip, invoiceForOfflineUrl);
        Assert.Contains(Fixture.InvoiceDate.Value.ToString("dd-MM-yyyy"), invoiceForOfflineUrl);

        //Utworzenie kodu QR faktury (KOD I) dla trybu offline
        byte[]? qrOffline = qrCodeService.GenerateQrCode(invoiceForOfflineUrl);

        //Dodanie etykiety OFFLINE
        qrOffline = qrCodeService.AddLabelToQrCode(qrOffline, "OFFLINE");

        Assert.NotEmpty(qrOffline);

        //Utworzenie odnośnika (Url) do weryfikacji certyfikatu (KOD II)
        byte[] certBytes = Convert.FromBase64String(Fixture.Certificate.Certificate);
        X509Certificate2 cert = X509CertificateLoader.LoadCertificate(certBytes);

        //Dodanie klucza prywatnego do certyfikatu
        X509Certificate2 certWithKey = testScenario.GetCertWithPrivateKey(cert, Fixture.PrivateKey);

        //Utworzenie kodu QR do weryfikacji certyfikatu (KOD II) dla trybu offline
        byte[] qrOfflineCertificate = qrCodeService.GenerateQrCode(
            linkService.BuildCertificateVerificationUrl(
                Fixture.Nip,
                QRCodeContextIdentifierType.Nip,
                Fixture.Nip,
                Fixture.Certificate.CertificateSerialNumber,
                Fixture.InvoiceHash,
                certWithKey));

        //Dodanie etykiety CERTYFIKAT
        qrOfflineCertificate = qrCodeService.AddLabelToQrCode(qrOfflineCertificate, "CERTYFIKAT");

        Assert.NotEmpty(qrOfflineCertificate);
    }

    private static X509Certificate2 GetCertWithEcdsaKey(X509Certificate2 cert, string privateKey)
    {
        using ECDsa ecdsa = ECDsa.Create();
        byte[] privateKeyBytes = Convert.FromBase64String(privateKey);
        ecdsa.ImportECPrivateKey(privateKeyBytes, out _);
        return cert.CopyWithPrivateKey(ecdsa);
    }

    private static X509Certificate2 GetCertWithRsaKey(X509Certificate2 cert, string privateKey)
    {
        using RSA rsa = RSA.Create();
        byte[] privateKeyBytes = Convert.FromBase64String(privateKey);
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        return cert.CopyWithPrivateKey(rsa);
    }
}