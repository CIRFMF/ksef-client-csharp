#nullable enable
using KSeF.Client.Api.Builders.Certificates;
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.QRCode;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.DI;
using KSeF.Client.Extensions;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Tests;

public class VerificationLinkServiceTests : KsefIntegrationTestBase
{
    private readonly IVerificationLinkService verificationLinkService = new VerificationLinkService(new KSeFClientOptions() { BaseUrl = KsefEnvironmentsUris.TEST, BaseQRUrl = KsefQREnvironmentsUris.TEST });
    private readonly string BaseUrl = $"{KsefQREnvironmentsUris.TEST}";

    private static byte[] ComputeSha256(byte[] data)
    {
#if NETFRAMEWORK
        using SHA256 sha256 = SHA256.Create();
        return sha256.ComputeHash(data);
#else
        return SHA256.HashData(data);
#endif
    }


    // =============================================
    // Testy legacy (RSA) – tylko dla zgodności wstecznej; NIEZALECANE:
    // • Użycie RSA 2048-bit:
    //    - Większy rozmiar kluczy i linków
    //    - Wolniejsze operacje kryptograficzne
    //    - Dłuższe URL-e (gorszy UX)
    // =============================================
    [Theory]
    [InlineData("<root>test</root>")]
    [InlineData("<data>special & chars /?</data>")]
    public void BuildInvoiceVerificationUrlEncodesHashCorrectly(string xml)
    {
        // Arrange
        string nip = "1234567890";
        DateTime issueDate = new(2026, 1, 5);

        byte[] sha;
        sha = ComputeSha256(Encoding.UTF8.GetBytes(xml));

        string invoiceHash = Convert.ToBase64String(sha);
        string expectedHash = sha.EncodeBase64UrlToString();
        string expectedUrl = $"{BaseUrl}/invoice/{nip}/{issueDate:dd-MM-yyyy}/{expectedHash}";

        // Act
        string url = verificationLinkService.BuildInvoiceVerificationUrl(nip, issueDate, invoiceHash);

        // Assert
        Assert.Equal(expectedUrl, url);

        string[] segments = [.. new Uri(url)
            .Segments
            .Select(s => s.Trim('/'))];

        Assert.Equal("invoice", segments[1]);
        Assert.Equal(nip, segments[2]);
        Assert.Equal(issueDate.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture), segments[3]);
        Assert.Equal(expectedHash, segments[4]);
    }

    [Fact]
    public void BuildCertificateVerificationUrlWithRsaCertificateShouldMatchFormat()
    {
        // Arrange
        string nip = "0000000000";
        string xml = "<x/>";
        string invoiceHash;
        byte[] hashBytes = ComputeSha256(Encoding.UTF8.GetBytes(xml));
        invoiceHash = Convert.ToBase64String(hashBytes);

        // Create full self-signed RSA cert with private key
        using RSA rsa = RSA.Create(2048);
        CertificateRequest certificateRequest = new("CN=TestRSA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        X509Certificate2 fullCert = certificateRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));

        // Act
        string url = verificationLinkService.BuildCertificateVerificationUrl(nip, QRCodeContextIdentifierType.Nip, nip, invoiceHash, fullCert);

        // Assert
        string[] segments = [.. new Uri(url)
            .Segments
            .Select(s => s.Trim('/'))];
                
        Assert.Equal("certificate", segments[1]);
        Assert.Equal("Nip", segments[2]);
        Assert.Equal(nip, segments[3]);
        Assert.Equal(nip, segments[4]);
        Assert.Equal(fullCert.SerialNumber, segments[5]);
        Assert.False(string.IsNullOrWhiteSpace(segments[6])); // hash
        Assert.False(string.IsNullOrWhiteSpace(segments[7])); // signed hash
    }

    [Fact]
    public void BuildCertificateVerificationUrlWithEcdsaCertificateShouldMatchFormat()
    {
        // Arrange
        string nip = "0000000000";
        string xml = "<x/>";
        string invoiceHash;
        string cnEntry = "CN=TestECDSA";
        DateTimeOffset certificateValidNotBefore = DateTimeOffset.Now;
        DateTimeOffset certificateValidNotAfter = DateTimeOffset.Now.AddYears(1);
        byte[] hashBytes = ComputeSha256(Encoding.UTF8.GetBytes(xml));
        invoiceHash = Convert.ToBase64String(hashBytes);

        // Create full self-signed ECDsa cert with private key
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest certificateRequest = new(cnEntry, ecdsa, HashAlgorithmName.SHA256);
        X509Certificate2 fullCert = certificateRequest.CreateSelfSigned(certificateValidNotBefore, certificateValidNotAfter);

        // Act
#if NETFRAMEWORK
        // On .NET Framework, ExportPkcs8PrivateKeyPem() is not available.
        // GetRSAPrivateKey() returns null for ECDSA cert, so the value is always null here.
        string? rsaPrivateKeyPem = null;
#else
        string? rsaPrivateKeyPem = fullCert.GetRSAPrivateKey()?.ExportPkcs8PrivateKeyPem();
#endif
        string url = verificationLinkService.BuildCertificateVerificationUrl(nip, QRCodeContextIdentifierType.Nip, nip, invoiceHash, fullCert, rsaPrivateKeyPem);

        // Assert
        string[] segments = [.. new Uri(url)
            .Segments
            .Select(s => s.Trim('/'))];

        Assert.Equal("certificate", segments[1]);
        Assert.Equal("Nip", segments[2]);
        Assert.Equal(nip, segments[3]);
        Assert.Equal(nip, segments[4]);
        Assert.Equal(fullCert.SerialNumber, segments[5]);
        Assert.False(string.IsNullOrWhiteSpace(segments[6])); // hash
        Assert.False(string.IsNullOrWhiteSpace(segments[7])); // signed hash
    }


    [Fact]
    public void BuildCertificateVerificationUrlWithoutPrivateKeyShouldThrow()
    {
        const string ZeroNip = "0000000000";
        const string MinimalXml = "<x/>";
        const string ExpecterErrorMessage = "nie wspiera RSA";

        // Arrange: certyfikat z samym kluczem publicznym (bez prywatnego)
        using RSA rsa = RSA.Create(2048);
        CertificateRequest req = new("CN=PublicOnly", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        X509Certificate2 fullCert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));

        // Eksport tylko publicznego certyfikatu
        byte[] publicBytes = fullCert.Export(X509ContentType.Cert);
        X509Certificate2 pubOnly = X509CertificateLoaderExtensions.LoadCertificate(publicBytes); // brak prywatnego klucza

        string nip = ZeroNip;
        string xml = MinimalXml;
        string invoiceHash;
        byte[] hashBytes = ComputeSha256(Encoding.UTF8.GetBytes(xml));
        invoiceHash = Convert.ToBase64String(hashBytes);

        // Act & Assert: próba podpisania bez klucza prywatnego → wyjątek
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
        {
            // przekazujemy pusty ciąg Base64 jako "brakujący" klucz prywatny
            return verificationLinkService.BuildCertificateVerificationUrl(nip, QRCodeContextIdentifierType.Nip, nip, invoiceHash, pubOnly);
        });

        Assert.Contains(ExpecterErrorMessage, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCertificateVerificationUrlWithEmbeddedPrivateKeyShouldSucceed()
    {
        // Arrange: wygeneruj self-signed cert z kluczem RSA
        using RSA rsa = RSA.Create(2048);
        CertificateRequest req = new(
            "CN=FullCert",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss
        );
        X509Certificate2 fullCert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1)
        );

        // Zapisz PFX i zaimportuj z flagą Exportable — certyfikat ma teraz wbudowany klucz
        byte[] pfxBytes = fullCert.Export(X509ContentType.Pfx);
        X509Certificate2 certWithKey = X509CertificateLoaderExtensions
            .LoadPkcs12(pfxBytes);

        string nip = "0000000000";
        string xml = "<x/>";
        string invoiceHash;
        byte[] sha = ComputeSha256(Encoding.UTF8.GetBytes(xml));
        invoiceHash = Convert.ToBase64String(sha);

        // Act
        string url = verificationLinkService.BuildCertificateVerificationUrl(nip, QRCodeContextIdentifierType.Nip,
            nip,
            invoiceHash,
            certWithKey
        );

        // Assert: URL powinien zawierać URL-encoded Base64 podpisu (końcówka "==" → "%3D%3D")
        Assert.NotNull(url);
        Uri uri = new(url);
        string[] segments = uri.AbsolutePath.Split('/');
        string signedUrl = segments.Last();
        Assert.Matches("^[A-Za-z0-9_-]+$", signedUrl);
    }

    // =============================================
    // Rekomendowane testy ECC (ECDSA P-256):
    // • Bezpieczeństwo jak RSA-2048, ale mniejsze i szybsze klucze
    // • Krótsze podpisane URL-e → lepszy UX w QR i linkach
    // =============================================

    [Theory]
    [InlineData("<root>test</root>")]
    [InlineData("<data>special & chars /?</data>")]
    public void BuildInvoiceVerificationUrlEncodesHashCorrectlyEcc(string xml)
    {
        // Arrange – bez zmian, testuje enkodowanie hash
        string nip = "1234567890";
        DateTime issueDate = new(2026, 1, 5);

        byte[] sha;
        sha = ComputeSha256(Encoding.UTF8.GetBytes(xml));

        string invoiceHash = Convert.ToBase64String(sha);
        string expectedHash = sha.EncodeBase64UrlToString();

        string expectedUrl = $"{BaseUrl}/invoice/{nip}/{issueDate:dd-MM-yyyy}/{expectedHash}";

        // Act
        string url = verificationLinkService.BuildInvoiceVerificationUrl(nip, issueDate, invoiceHash);

        // Assert
        Assert.Equal(expectedUrl, url);
        string[] segments = [.. new Uri(url).Segments.Select(s => s.Trim('/'))];
        Assert.Equal("invoice", segments[1]);
        Assert.Equal(nip, segments[2]);
        Assert.Equal(issueDate.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture), segments[3]);
        Assert.Equal(expectedHash, segments[4]);
    }

    [Fact]
    public void BuildCertificateVerificationUrlWithEcdsaCertificateShouldMatchFormatEcc()
    {
        // Arrange – generowanie ECDSA P-256
        string nip = "0000000000";
        string xml = "<x/>";
        string invoiceHash;
        invoiceHash = Convert.ToBase64String(ComputeSha256(Encoding.UTF8.GetBytes(xml)));

        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest req = new("CN=TestECDSA", ecdsa, HashAlgorithmName.SHA256);
        X509Certificate2 fullCert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        // Act – jawnie przekazujemy prywatny klucz ECDSA
#if NETFRAMEWORK
        // ExportPkcs8PrivateKeyPem() is not available on .NET Framework 4.8.
        // The cert already has the private key embedded, so passing null is safe.
        string? privateKeyPem = null;
#else
        string? privateKeyPem = fullCert.GetECDsaPrivateKey()?.ExportPkcs8PrivateKeyPem();
#endif
        string url = verificationLinkService.BuildCertificateVerificationUrl(nip, QRCodeContextIdentifierType.Nip, nip, invoiceHash, fullCert, privateKeyPem);

        // Assert – format ścieżek
        string[] segments = [.. new Uri(url).Segments.Select(s => s.Trim('/'))];
        Assert.Equal("certificate", segments[1]);
        Assert.Equal("Nip", segments[2]);
        Assert.Equal(nip, segments[3]);
        Assert.Equal(nip, segments[4]);
        Assert.Equal(fullCert.SerialNumber, segments[5]);
        Assert.False(string.IsNullOrWhiteSpace(segments[6])); // hash
        Assert.False(string.IsNullOrWhiteSpace(segments[7])); // signed hash
    }

    [Fact]
    public void BuildCertificateVerificationUrlWithoutPrivateKeyShouldThrowEcc()
    {
        // Arrange – public-only ECC powinno rzucić
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest req = new("CN=PublicOnly", ecdsa, HashAlgorithmName.SHA256);
        X509Certificate2 fullCert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
        byte[] publicBytes = fullCert.Export(X509ContentType.Cert);
        X509Certificate2 pubOnly = X509CertificateLoaderExtensions.LoadCertificate(publicBytes);

        string nip = "0000000000";
        string xml = "<x/>";
        string invoiceHash;
        invoiceHash = Convert.ToBase64String(ComputeSha256(Encoding.UTF8.GetBytes(xml)));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            verificationLinkService.BuildCertificateVerificationUrl(nip, QRCodeContextIdentifierType.Nip, nip, invoiceHash, pubOnly)
        );
    }

    [Fact]
    public void BuildCertificateVerificationUrlWithEmbeddedEcdsaKeyShouldSucceedEcc()
    {
        const string ZeroNip = "0000000000";
        const string MinimalXml = "<x/>";
        const string UrlMatchPattern = "^[A-Za-z0-9_-]+$";
        // Arrange – certyfikat PFX ECDSA P-256 z flagą exportowalności
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest req = new("CN=FullEccCert", ecdsa, HashAlgorithmName.SHA256);
        X509Certificate2 fullCert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        byte[] pfx = fullCert.Export(X509ContentType.Pfx);
        X509Certificate2 certWithKey =
            X509CertificateLoaderExtensions
            .LoadPkcs12(pfx);

        string nip = ZeroNip;
        string xml = MinimalXml;
        string invoiceHash;
        invoiceHash = Convert.ToBase64String(ComputeSha256(Encoding.UTF8.GetBytes(xml)));

        // Act
        string url = verificationLinkService.BuildCertificateVerificationUrl(nip, QRCodeContextIdentifierType.Nip, nip, invoiceHash, certWithKey);

        // Assert: URL zawiera poprawny ECDSA podpis kodowany w Base64
        Uri uri = new(url);
        string[] segments = uri.AbsolutePath.Split('/');
        string signedUrl = segments.Last();
        Assert.Matches(UrlMatchPattern, signedUrl);
    }

    /// <summary>
    /// Test sprawdza, czy można poprawnie wygenerować link weryfikacyjny na podstawie certyfikatu dostarczonego z zewnątrz. W ramach testu następuje:
    /// - uwierzytelnienie właściciela i rejestrujacja nowego certyfikatu w KSeF,
    /// - pobranie certyfikatu i jego klucza prywatnego, połączenie ich w X509Certificate2,
    /// - wysłanie faktury do KSeF,
    /// - zbudowanie linku weryfikacyjnego i wykonanie HTTP GET, 
    /// - po odczytaniu strony oczekuje się, że certyfikatu istnieje a jego aktywność, poprawność podpisu oraz uprawnienia do wystawiania faktur są potwierdzone.
    /// Test potwierdza, że serwis weryfikacyjny działa poprawnie z certyfikatami zewnętrznymi i generuje w pełni działający URL.
    /// </summary>
    [Fact]
    public async Task BuildCertificateVerificationUrl_WithExternalyProvidedCertificate_ShouldSucced()
    {
        const int CertificateValidityDays = -1;

        const string TestCertificateName = "Test_VerificationLink_Certificate";
        const string InvoiceFileName = "invoice-template-fa-3.xml";
        const string CertificatesDirectoryName = "Certificates";

        const string CertificateExistsConfirmation = "Certyfikat istnieje";
        const string CertificateIsActiveConfirmation = "Certyfikat jest aktywny";
        const string CertificateIsCorrectConfirmation = "Podpis wystawcy jest prawid";
        const string CertificateAllowsForInvoicingConfirmation = "Wystawca posiada uprawnienia do wystawienia faktury";

        // uwierzytelnienie z uprawnieniami właścicielskimi
        string ownerNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse ownerAuthProcessResponse =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, ownerNip);
        string ownerAccessToken = ownerAuthProcessResponse.AccessToken.Token;

        // Pobieranie informacji o zarejestrowanych certyfikatach
        CertificateEnrollmentsInfoResponse certificateEnrollmentsInfoResponse =
            await CertificateUtils.GetCertificateEnrollmentDataAsync(KsefClient, ownerAccessToken, CancellationToken.None);

        (string csr, string key) = CryptographyService.GenerateCsrWithEcdsa(certificateEnrollmentsInfoResponse);
        SendCertificateEnrollmentRequest sendCertificateEnrollmentRequest = SendCertificateEnrollmentRequestBuilder
            .Create()
            .WithCertificateName(TestCertificateName)
            .WithCertificateType(CertificateType.Offline)
            .WithCsr(csr)
            .WithValidFrom(DateTimeOffset.UtcNow.AddDays(CertificateValidityDays))
            .Build();

        CertificateEnrollmentResponse certificateEnrollmentResponse = await CertificateUtils.SendCertificateEnrollmentAsync(KsefClient, ownerAccessToken, csr, CertificateType.Offline);
        string enrollmentReference = certificateEnrollmentResponse.ReferenceNumber;

        CertificateEnrollmentStatusResponse certificateEnrollmentStatusResponse =
                    await AsyncPollingUtils.PollAsync(
                        async () => await CertificateUtils.GetCertificateEnrollmentStatusAsync(KsefClient, enrollmentReference, ownerAccessToken, CancellationToken.None).ConfigureAwait(false),
                        result => result.Status.Code == CertificateStatusCodeResponse.RequestProcessedSuccessfully);

        CertificateEnrollmentStatusResponse enrollmentStatus = certificateEnrollmentStatusResponse;

        // Pobierz zarejestrowany certyfikat
        List<string> serialNumbers = new() { enrollmentStatus.CertificateSerialNumber };
        CertificateListRequest certificateListRequest = new() { CertificateSerialNumbers = serialNumbers };

        CertificateListResponse certificateListResponse = await CertificateUtils.GetCertificateListAsync(KsefClient, certificateListRequest, ownerAccessToken, CancellationToken.None);
        CertificateListResponse retrievedCertificates = certificateListResponse;

        CertificateResponse certificate = retrievedCertificates.Certificates.First();

        // Przygotowanie ścieżek
        string directoryPath = Path.Combine(AppContext.BaseDirectory, CertificatesDirectoryName);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string certificatePath = Path.Combine(directoryPath, $"{certificate.CertificateName}.der");
        string keyPath = Path.Combine(directoryPath, $"{certificate.CertificateName}.key");
        CertificateUtils.SaveCertificateAndKey(key, certificate, certificatePath, keyPath);
        X509Certificate2 certWithKey = CertificateUtils.LoadCertificateFromFiles(certificatePath, keyPath);

        // Budowanie url weryfikacyjnego
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // Otwarcie sesji
        OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient, encryptionData, ownerAccessToken, SystemCode.FA3);
        Assert.NotNull(openSessionResponse);
        Assert.False(string.IsNullOrWhiteSpace(openSessionResponse.ReferenceNumber));

        // Wysłanie faktury
        SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            ownerAccessToken,
            ownerNip,
            InvoiceFileName,
            encryptionData,
            CryptographyService,
            true);
        Assert.NotNull(sendInvoiceResponse);
        Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));

        SessionStatusResponse statusAfterSend = await AsyncPollingUtils.PollAsync(
            async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(KsefClient, openSessionResponse.ReferenceNumber, ownerAccessToken).ConfigureAwait(false),
            result => result is not null && result.SuccessfulInvoiceCount is not null);

        // Zamknięcie sesji
        await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient, openSessionResponse.ReferenceNumber, ownerAccessToken);

        // Pobranie faktur sesji (powinna być jedna)
        SessionInvoicesResponse sessionInvoiceResponse = await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(KsefClient, openSessionResponse.ReferenceNumber, ownerAccessToken);
        Assert.NotNull(sessionInvoiceResponse);
        Assert.NotEmpty(sessionInvoiceResponse.Invoices);
        Assert.Single(sessionInvoiceResponse.Invoices);
        string ksefNumber = sessionInvoiceResponse.Invoices.First().KsefNumber;
        string invoiceHash = sessionInvoiceResponse.Invoices.First().InvoiceHash;

        string url = verificationLinkService.BuildCertificateVerificationUrl(
            ownerNip,
            QRCodeContextIdentifierType.Nip,
            ownerNip,
            invoiceHash,
            certWithKey);

        using HttpClient httpClient = new();
        HttpResponseMessage response = await httpClient.GetAsync(url);

        Assert.NotNull(response);
        Assert.True(response.IsSuccessStatusCode);

        string html = await response.Content.ReadAsStringAsync();

        Assert.Contains(CertificateExistsConfirmation, html);
        Assert.Contains(CertificateIsActiveConfirmation, html);
        Assert.Contains(CertificateIsCorrectConfirmation, html);
        Assert.Contains(CertificateAllowsForInvoicingConfirmation, html);
    }
}