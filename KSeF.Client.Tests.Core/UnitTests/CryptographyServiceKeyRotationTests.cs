using KSeF.Client.Api.Services;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Tests.Core.UnitTests.Helpers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.UnitTests;

/// <summary>
/// Testy jednostkowe <see cref="CryptographyService"/> – scenariusze rotacji i re-certyfikacji
/// kluczy publicznych KSeF (https://github.com/CIRFMF/ksef-docs/issues/737).
/// Każdy test używa <see cref="SequentialFetcher"/> symulującego kolejne odpowiedzi
/// endpointu GET /security/public-key-certificates.
/// </summary>
public class CryptographyServiceKeyRotationTests
{
    private const string TokenCertCn = "KSeF-Token";

    private static CryptographyService CreateService(SequentialFetcher fetcher) => new(fetcher);

    /// <summary>
    /// Re-certyfikacja: ten sam klucz RSA, ale nowy certyfikat.
    /// Po <see cref="CryptographyService.ForceRefreshAsync"/> modulus i eksponent
    /// muszą pozostać identyczne, mimo że thumbprint certyfikatu się zmienił.
    /// </summary>
    [Fact]
    public async Task AfterRecertification_PublicKeyRemainsTheSame()
    {
        // Arrange
        using RSA sharedRsa = RSA.Create(CertificateTestHelpers.RsaKeySize);

        X509Certificate2 oldCert = new CertificateRequest("CN=KSeF-Symmetric-Old", sharedRsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddYears(-1), DateTimeOffset.UtcNow.AddMonths(1));

        X509Certificate2 newCert = new CertificateRequest("CN=KSeF-Symmetric-New", sharedRsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(CertificateTestHelpers.CertStartOffsetMinutes), DateTimeOffset.UtcNow.AddYears(CertificateTestHelpers.CertValidityYears));

        X509Certificate2 tokenCert = CertificateTestHelpers.CreateRsaCert(TokenCertCn);

        ICollection<PemCertificateInfo> firstResponse = new[]
        {
            CertificateTestHelpers.ToPemInfo(oldCert, PublicKeyCertificateUsage.SymmetricKeyEncryption),
            CertificateTestHelpers.ToPemInfo(tokenCert, PublicKeyCertificateUsage.KsefTokenEncryption)
        };
        ICollection<PemCertificateInfo> secondResponse = new[]
        {
            CertificateTestHelpers.ToPemInfo(newCert, PublicKeyCertificateUsage.SymmetricKeyEncryption),
            CertificateTestHelpers.ToPemInfo(tokenCert, PublicKeyCertificateUsage.KsefTokenEncryption)
        };

        SequentialFetcher fetcher = new(firstResponse, secondResponse);
        CryptographyService svc = CreateService(fetcher);

        // Act
        await svc.WarmupAsync();
        RSAParameters keyBefore = svc.SymmetricKeyCertificate.GetRSAPublicKey()!.ExportParameters(false);

        await svc.ForceRefreshAsync();
        RSAParameters keyAfter = svc.SymmetricKeyCertificate.GetRSAPublicKey()!.ExportParameters(false);

        // Assert
        Assert.Equal(2, fetcher.CallCount);
        Assert.Equal(keyBefore.Modulus, keyAfter.Modulus);
        Assert.Equal(keyBefore.Exponent, keyAfter.Exponent);
        Assert.NotEqual(
            svc.SymmetricKeyCertificate.Thumbprint,
            oldCert.Thumbprint,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Rotacja planowa: okres przejściowy.
    /// API zwraca dwa certyfikaty SymmetricKeyEncryption jednocześnie;
    /// serwis powinien wybrać ten z późniejszą datą <c>ValidFrom</c>.
    /// </summary>
    [Fact]
    public async Task PlannedRotation_TransitionPeriod_NewerCertificateIsPreferred()
    {
        // Arrange
        using RSA oldRsa = RSA.Create(CertificateTestHelpers.RsaKeySize);
        using RSA newRsa = RSA.Create(CertificateTestHelpers.RsaKeySize);

        DateTimeOffset oldValidFrom = DateTimeOffset.UtcNow.AddYears(-1);
        DateTimeOffset newValidFrom = DateTimeOffset.UtcNow.AddDays(-1);

        X509Certificate2 oldSymCert = new CertificateRequest("CN=KSeF-Sym-Old", oldRsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(oldValidFrom, oldValidFrom.AddYears(CertificateTestHelpers.CertValidityYears));

        X509Certificate2 newSymCert = new CertificateRequest("CN=KSeF-Sym-New", newRsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(newValidFrom, newValidFrom.AddYears(CertificateTestHelpers.CertValidityYears));

        X509Certificate2 tokenCert = CertificateTestHelpers.CreateRsaCert(TokenCertCn);

        ICollection<PemCertificateInfo> transitionResponse = new[]
        {
            CertificateTestHelpers.ToPemInfo(oldSymCert, PublicKeyCertificateUsage.SymmetricKeyEncryption, validFrom: oldValidFrom),
            CertificateTestHelpers.ToPemInfo(newSymCert, PublicKeyCertificateUsage.SymmetricKeyEncryption, validFrom: newValidFrom),
            CertificateTestHelpers.ToPemInfo(tokenCert, PublicKeyCertificateUsage.KsefTokenEncryption)
        };

        SequentialFetcher fetcher = new(transitionResponse);
        CryptographyService svc = CreateService(fetcher);

        // Act
        await svc.WarmupAsync();

        RSAParameters selectedKeyParams = svc.SymmetricKeyCertificate.GetRSAPublicKey()!.ExportParameters(false);
        RSAParameters newKeyParams = newRsa.ExportParameters(false);

        // Assert – wybrany klucz musi odpowiadać nowszemu certyfikatowi
        Assert.Equal(newKeyParams.Modulus, selectedKeyParams.Modulus);
    }

    /// <summary>
    /// Rotacja planowa: nowy certyfikat opublikowany z wyprzedzeniem (validFrom w przyszłości).
    /// Klient wybiera certyfikat ważny w chwili operacji.
    /// Certyfikat z przyszłą datą validFrom nie może być użyty, nawet jeśli ma późniejszą datę.
    /// </summary>
    [Fact]
    public async Task PlannedRotation_FutureCertificatePublishedEarly_OldCertificateIsUsed()
    {
        // Arrange
        using RSA currentRsa = RSA.Create(CertificateTestHelpers.RsaKeySize);
        using RSA futureRsa = RSA.Create(CertificateTestHelpers.RsaKeySize);

        DateTimeOffset currentValidFrom = DateTimeOffset.UtcNow.AddYears(-1);
        DateTimeOffset futureValidFrom = DateTimeOffset.UtcNow.AddDays(7); // jeszcze nieważny

        X509Certificate2 currentSymCert = new CertificateRequest("CN=KSeF-Sym-Current", currentRsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(currentValidFrom, currentValidFrom.AddYears(CertificateTestHelpers.CertValidityYears));

        X509Certificate2 futureSymCert = new CertificateRequest("CN=KSeF-Sym-Future", futureRsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(futureValidFrom, futureValidFrom.AddYears(CertificateTestHelpers.CertValidityYears));

        X509Certificate2 tokenCert = CertificateTestHelpers.CreateRsaCert(TokenCertCn);

        ICollection<PemCertificateInfo> response = new[]
        {
            CertificateTestHelpers.ToPemInfo(currentSymCert, PublicKeyCertificateUsage.SymmetricKeyEncryption, validFrom: currentValidFrom),
            CertificateTestHelpers.ToPemInfo(futureSymCert,  PublicKeyCertificateUsage.SymmetricKeyEncryption, validFrom: futureValidFrom),
            CertificateTestHelpers.ToPemInfo(tokenCert, PublicKeyCertificateUsage.KsefTokenEncryption)
        };

        SequentialFetcher fetcher = new(response);
        CryptographyService svc = CreateService(fetcher);

        // Act
        await svc.WarmupAsync();
        RSAParameters selectedKeyParams = svc.SymmetricKeyCertificate.GetRSAPublicKey()!.ExportParameters(false);
        RSAParameters currentKeyParams = currentRsa.ExportParameters(false);

        // Assert – certyfikat z przyszłą datą validFrom musi zostać zignorowany
        Assert.Equal(currentKeyParams.Modulus, selectedKeyParams.Modulus);
    }

    /// <summary>
    /// Rotacja planowa: po zakończeniu okresu przejściowego.
    /// API przestaje zwracać stary certyfikat; po <see cref="CryptographyService.ForceRefreshAsync"/>
    /// serwis musi używać nowego.
    /// </summary>
    [Fact]
    public async Task PlannedRotation_AfterTransition_OnlyNewCertificateIsUsed()
    {
        // Arrange
        (PemCertificateInfo oldSym, PemCertificateInfo token) = CertificateTestHelpers.CreateCertPair("KSeF-Sym-Old", "KSeF-Token");
        (PemCertificateInfo newSym, PemCertificateInfo _) = CertificateTestHelpers.CreateCertPair("KSeF-Sym-New");

        ICollection<PemCertificateInfo> beforeRotation = new[] { oldSym, token };
        ICollection<PemCertificateInfo> afterRotation = new[] { newSym, token };

        SequentialFetcher fetcher = new(beforeRotation, afterRotation);
        CryptographyService svc = CreateService(fetcher);

        // Act
        await svc.WarmupAsync();
        byte[] certBefore = svc.SymmetricKeyCertificate.Export(X509ContentType.Cert);

        await svc.ForceRefreshAsync();
        byte[] certAfter = svc.SymmetricKeyCertificate.Export(X509ContentType.Cert);

        // Assert
        Assert.Equal(2, fetcher.CallCount);
        Assert.False(certBefore.SequenceEqual(certAfter),
            "Po rotacji planowej certyfikat (i klucz) powinien się zmienić.");
    }

    /// <summary>
    /// Rotacja awaryjna po incydencie bezpieczeństwa.
    /// Klient otrzymał błąd 21470 i wywołuje <see cref="CryptographyService.ForceRefreshAsync"/>;
    /// nowy klucz musi być inny niż skompromitowany i zgodny z certyfikatem zwróconym przez API.
    /// </summary>
    [Fact]
    public async Task EmergencyRotation_AfterError21470_ServiceUsesNewKey()
    {
        // Arrange
        using RSA compromisedRsa = RSA.Create(CertificateTestHelpers.RsaKeySize);
        using RSA newRsa = RSA.Create(CertificateTestHelpers.RsaKeySize);

        X509Certificate2 compromisedSymCert = new CertificateRequest("CN=KSeF-Sym-Compromised", compromisedRsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddYears(-1), DateTimeOffset.UtcNow.AddYears(1));

        X509Certificate2 newSymCert = new CertificateRequest("CN=KSeF-Sym-Emergency-New", newRsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(CertificateTestHelpers.CertStartOffsetMinutes), DateTimeOffset.UtcNow.AddYears(CertificateTestHelpers.CertValidityYears));

        X509Certificate2 tokenCert = CertificateTestHelpers.CreateRsaCert(TokenCertCn);

        ICollection<PemCertificateInfo> beforeIncident = new[]
        {
            CertificateTestHelpers.ToPemInfo(compromisedSymCert, PublicKeyCertificateUsage.SymmetricKeyEncryption),
            CertificateTestHelpers.ToPemInfo(tokenCert, PublicKeyCertificateUsage.KsefTokenEncryption)
        };
        ICollection<PemCertificateInfo> afterIncident = new[]
        {
            CertificateTestHelpers.ToPemInfo(newSymCert, PublicKeyCertificateUsage.SymmetricKeyEncryption),
            CertificateTestHelpers.ToPemInfo(tokenCert, PublicKeyCertificateUsage.KsefTokenEncryption)
        };

        SequentialFetcher fetcher = new(beforeIncident, afterIncident);
        CryptographyService svc = CreateService(fetcher);

        // Act
        await svc.WarmupAsync();
        RSAParameters keyBeforeIncident = svc.SymmetricKeyCertificate.GetRSAPublicKey()!.ExportParameters(false);

        await svc.ForceRefreshAsync();
        RSAParameters keyAfterIncident = svc.SymmetricKeyCertificate.GetRSAPublicKey()!.ExportParameters(false);

        // Assert
        Assert.Equal(2, fetcher.CallCount);
        Assert.False(
            keyBeforeIncident.Modulus!.SequenceEqual(keyAfterIncident.Modulus!),
            "Po rotacji awaryjnej klucz publiczny powinien się zmienić.");
        Assert.Equal(newRsa.ExportParameters(false).Modulus, keyAfterIncident.Modulus);
    }

    /// <summary>
    /// Każde wywołanie <see cref="CryptographyService.ForceRefreshAsync"/> musi
    /// trafiać do fetchera - nie może korzystać z cache'u.
    /// </summary>
    [Fact]
    public async Task ForceRefreshAsync_AlwaysCallsFetcher_NotCache()
    {
        // Arrange
        (PemCertificateInfo sym, PemCertificateInfo token) = CertificateTestHelpers.CreateCertPair();
        ICollection<PemCertificateInfo> response = new[] { sym, token };

        SequentialFetcher fetcher = new(response, response, response);
        CryptographyService svc = CreateService(fetcher);

        // Act
        await svc.WarmupAsync();       // call 1
        await svc.ForceRefreshAsync(); // call 2
        await svc.ForceRefreshAsync(); // call 3

        // Assert
        Assert.Equal(3, fetcher.CallCount);
    }

    /// <summary>
    /// Drugie wywołanie <see cref="CryptographyService.WarmupAsync"/> jest no-op.
    /// Fetcher powinien zostać wywołany dokładnie raz.
    /// </summary>
    [Fact]
    public async Task WarmupAsync_IsIdempotent_FetcherCalledOnce()
    {
        // Arrange
        (PemCertificateInfo sym, PemCertificateInfo token) = CertificateTestHelpers.CreateCertPair();
        ICollection<PemCertificateInfo> response = new[] { sym, token };

        SequentialFetcher fetcher = new(response);
        CryptographyService svc = CreateService(fetcher);

        // Act
        await svc.WarmupAsync();
        await svc.WarmupAsync();

        // Assert
        Assert.Equal(1, fetcher.CallCount);
    }
}