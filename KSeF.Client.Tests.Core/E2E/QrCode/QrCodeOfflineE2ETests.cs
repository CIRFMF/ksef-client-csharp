#nullable enable
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
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Tests.Core.E2E.QrCode;

public class QrCodeOfflineE2EScenarioFixture
{
    public QrCodeOfflineE2EScenarioFixture()
    {
        AccessToken = string.Empty;
        Nip = string.Empty;
        InvoiceHash = string.Empty;
        PrivateKey = string.Empty;
    }

    public string AccessToken { get; set; }
    public string Nip { get; set; }
    public string InvoiceHash { get; set; }
    public string PrivateKey { get; set; }
    public CertificateResponse? Certificate { get; set; }
    public DateTime? InvoiceDate { get; set; }
}

[CollectionDefinition("QrCodeOfflineE2EScenario")]
public class QrCodeE2EScenarios
: ICollectionFixture<QrCodeOfflineE2EScenarioFixture>
{ }
[Collection("QrCodeOfflineE2EScenario")]
public class QrCodeOfflineE2ETests : TestBase
{
    private readonly QrCodeOfflineE2EScenarioFixture Fixture;
    private readonly VerificationLinkService linkService;

    public QrCodeOfflineE2ETests(QrCodeOfflineE2EScenarioFixture fixture)
    {
        Fixture = fixture;
        Fixture.Nip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, Fixture.Nip).GetAwaiter().GetResult();
        Fixture.AccessToken = authInfo.AccessToken.Token;

        linkService = new VerificationLinkService(new KSeFClientOptions() { BaseUrl = KsefEnvironmentsUris.TEST, BaseQRUrl = KsefQREnvironmentsUris.TEST });
    }

    public record TestScenario(
        string Name,
        Func<IKSeFClient, string, ICryptographyService, Task<(string, string)>> GenerateCsrAndPrivateKey, // WithRsa lub WithEcdsa
        string InvoiceTemplateName,  // "fa(2)" lub "fa(3)"
        Func<X509Certificate2, string, X509Certificate2> GetCertWithPrivateKey  // GetCertWithRsaKey lub GetCertWithEcdsaKey
    );

    // Replace TheoryData<object[]> with TheoryData<string> and use scenario name as the data row.
    // Use a static dictionary to map scenario names to TestScenario instances.

    public static readonly Dictionary<string, TestScenario> TestScenarioMap = new()
    {
        ["faktura FA(2), szyfrowanie RSA"] = new TestScenario(
            "faktura FA(2), szyfrowanie RSA",
            async (ksefClient, accessToken, cryptographyService) => await CertificateUtils.GenerateCsrAndPrivateKeyWithRsaAsync(ksefClient, accessToken, cryptographyService, RSASignaturePadding.Pkcs1),
            "invoice-template-fa-2.xml",
            (cert, key) => GetCertWithRsaKey(cert, key)),
        ["faktura FA(2), szyfrowanie ECdsa"] = new TestScenario(
            "faktura FA(2), szyfrowanie ECdsa",
            async (ksefClient, accessToken, cryptographyService) => await CertificateUtils.GenerateCsrAndPrivateKeyWithEcdsaAsync(ksefClient, accessToken, cryptographyService),
            "invoice-template-fa-2.xml",
            (cert, key) => GetCertWithEcdsaKey(cert, key)),
        ["faktura FA(3), szyfrowanie RSA"] = new TestScenario(
            "faktura FA(3), szyfrowanie RSA",
            async (ksefClient, accessToken, cryptographyService) => await CertificateUtils.GenerateCsrAndPrivateKeyWithRsaAsync(ksefClient, accessToken, cryptographyService, RSASignaturePadding.Pkcs1),
            "invoice-template-fa-3.xml",
            (cert, key) => GetCertWithRsaKey(cert, key)),
        ["faktura FA(3), szyfrowanie ECdsa"] = new TestScenario(
            "faktura FA(3), szyfrowanie ECdsa",
            async (ksefClient, accessToken, cryptographyService) => await CertificateUtils.GenerateCsrAndPrivateKeyWithEcdsaAsync(ksefClient, accessToken, cryptographyService),
            "invoice-template-fa-3.xml",
            (cert, key) => GetCertWithEcdsaKey(cert, key))
    };

    public static TheoryData<string> TestScenarios =>
    [
        "faktura FA(2), szyfrowanie RSA",
        "faktura FA(2), szyfrowanie ECdsa",
        "faktura FA(3), szyfrowanie RSA",
        "faktura FA(3), szyfrowanie ECdsa"
    ];

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
    public async Task QrCodeOfflineE2ETest(string scenarioName)
    {
        TestScenario testScenario = TestScenarioMap[scenarioName];

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
        Assert.Equal(200, enrollmentStatus.Status.Code);

        //Pobranie certyfikatu KSeF
        List<string> serialNumbers = [enrollmentStatus.CertificateSerialNumber];
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

        Fixture.InvoiceDate = DateTime.Parse("2025-10-01", CultureInfo.InvariantCulture);
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
        Assert.Contains(Fixture.InvoiceDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), invoiceForOfflineUrl);

        //Utworzenie kodu QR faktury (KOD I) dla trybu offline
        byte[]? qrOffline = QrCodeService.GenerateQrCode(invoiceForOfflineUrl);

        //Dodanie etykiety OFFLINE
        qrOffline = QrCodeService.AddLabelToQrCode(qrOffline, "OFFLINE");

        Assert.NotEmpty(qrOffline);

        //Utworzenie odnośnika (Url) do weryfikacji certyfikatu (KOD II)
        byte[] certBytes = Convert.FromBase64String(Fixture.Certificate.Certificate);

        X509Certificate2 cert = X509CertificateLoaderExtensions.LoadCertificate(certBytes);

        //Dodanie klucza prywatnego do certyfikatu
        X509Certificate2 certWithKey = testScenario.GetCertWithPrivateKey(cert, Fixture.PrivateKey);

        //Utworzenie kodu QR do weryfikacji certyfikatu (KOD II) dla trybu offline
        byte[] qrOfflineCertificate = QrCodeService.GenerateQrCode(
            linkService.BuildCertificateVerificationUrl(
                Fixture.Nip,
                QRCodeContextIdentifierType.Nip,
                Fixture.Nip,
                Fixture.InvoiceHash,
                certWithKey));

        //Dodanie etykiety CERTYFIKAT
        qrOfflineCertificate = QrCodeService.AddLabelToQrCode(qrOfflineCertificate, "CERTYFIKAT");

        Assert.NotEmpty(qrOfflineCertificate);
    }

    private static X509Certificate2 GetCertWithEcdsaKey(X509Certificate2 cert, string privateKey)
    {
        using ECDsa ecdsa = ECDsa.Create();
        byte[] privateKeyBytes = Convert.FromBase64String(privateKey);
#if NETFRAMEWORK
        ecdsa.ImportParameters(DecodeECPrivateKey(privateKeyBytes));
#else
        ecdsa.ImportECPrivateKey(privateKeyBytes, out _);
#endif
        return cert.CopyWithPrivateKey(ecdsa);
    }

#if NETFRAMEWORK
    /// <summary>
    /// Minimal SEC1 ECPrivateKey decoder for .NET Framework 4.8 (where ImportECPrivateKey is not available).
    /// Parses the DER-encoded EC private key and imports it via ECParameters.
    /// </summary>
    private static ECParameters DecodeECPrivateKey(byte[] sec1Key)
    {
        // SEC1 ECPrivateKey ::= SEQUENCE { version, privateKey, [0] parameters, [1] publicKey }
        // This is a simplified parser sufficient for test usage with known curves (P-256).
        using MemoryStream ms = new(sec1Key);
        using BinaryReader reader = new(ms);

        byte tag = reader.ReadByte(); // SEQUENCE
        if (tag != 0x30) throw new System.Security.Cryptography.CryptographicException("Invalid SEC1 EC private key");
        ReadDerLength(reader);

        // version (INTEGER)
        ReadDerTagAndContent(reader, 0x02);

        // privateKey (OCTET STRING)
        byte[] d = ReadDerTagAndContent(reader, 0x04);

        // Default to P-256
        ECParameters ecParams = new()
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = d
        };

        // Try to read optional [0] parameters and [1] publicKey
        while (ms.Position < ms.Length)
        {
            byte nextTag = reader.ReadByte();
            int len = ReadDerLength(reader);

            if ((nextTag & 0xA0) == 0xA0) // context-specific constructed
            {
                int tagNum = nextTag & 0x1F;
                if (tagNum == 1 && len > 0) // [1] publicKey (BIT STRING)
                {
                    byte[] pubWrapper = reader.ReadBytes(len);
                    // BIT STRING: first byte is unused bits count (should be 0)
                    if (pubWrapper.Length > 1 && pubWrapper[0] == 0x03) // nested BIT STRING
                    {
                        using MemoryStream bsMs = new(pubWrapper);
                        using BinaryReader bsReader = new(bsMs);
                        bsReader.ReadByte(); // 0x03 tag
                        int bsLen = ReadDerLength(bsReader);
                        byte unusedBits = bsReader.ReadByte();
                        byte[] qBytes = bsReader.ReadBytes(bsLen - 1);
                        if (qBytes.Length > 0 && qBytes[0] == 0x04) // uncompressed point
                        {
                            int coordLen = (qBytes.Length - 1) / 2;
                            ecParams.Q = new ECPoint
                            {
                                X = new byte[coordLen],
                                Y = new byte[coordLen]
                            };
                            Buffer.BlockCopy(qBytes, 1, ecParams.Q.X, 0, coordLen);
                            Buffer.BlockCopy(qBytes, 1 + coordLen, ecParams.Q.Y, 0, coordLen);
                        }
                    }
                    else if (pubWrapper.Length > 1 && pubWrapper[0] == 0x00)
                    {
                        // Direct uncompressed point after unused bits byte
                        byte[] qBytes = new byte[pubWrapper.Length - 1];
                        Buffer.BlockCopy(pubWrapper, 1, qBytes, 0, qBytes.Length);
                        if (qBytes.Length > 0 && qBytes[0] == 0x04)
                        {
                            int coordLen = (qBytes.Length - 1) / 2;
                            ecParams.Q = new ECPoint
                            {
                                X = new byte[coordLen],
                                Y = new byte[coordLen]
                            };
                            Buffer.BlockCopy(qBytes, 1, ecParams.Q.X, 0, coordLen);
                            Buffer.BlockCopy(qBytes, 1 + coordLen, ecParams.Q.Y, 0, coordLen);
                        }
                    }
                }
                else
                {
                    reader.ReadBytes(len); // skip
                }
            }
            else
            {
                reader.ReadBytes(len); // skip unknown
            }
        }

        return ecParams;
    }

    private static int ReadDerLength(BinaryReader reader)
    {
        byte b = reader.ReadByte();
        if (b < 0x80) return b;
        int numBytes = b & 0x7F;
        int length = 0;
        for (int i = 0; i < numBytes; i++)
            length = (length << 8) | reader.ReadByte();
        return length;
    }

    private static byte[] ReadDerTagAndContent(BinaryReader reader, byte expectedTag)
    {
        byte tag = reader.ReadByte();
        if (tag != expectedTag) throw new System.Security.Cryptography.CryptographicException($"Expected tag 0x{expectedTag:X2}, got 0x{tag:X2}");
        int len = ReadDerLength(reader);
        return reader.ReadBytes(len);
    }
#endif

    private static X509Certificate2 GetCertWithRsaKey(X509Certificate2 cert, string privateKey)
    {
        using RSA rsa = RSA.Create();
        byte[] privateKeyBytes = Convert.FromBase64String(privateKey);
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        return cert.CopyWithPrivateKey(rsa);
    }
}