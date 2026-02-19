#if !NETFRAMEWORK
// Guard: CertificateRequest (używany przez buildery) nie jest dostępny
// na Mono/macOS (.NET Framework 4.8) — testy uruchamiane tylko na net8/9/10.

using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Models.Authorization;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.UnitTests;

/// <summary>
/// Testy regresyjne: certyfikaty samopodpisane muszą mieć daty NotBefore i NotAfter
/// oparte na UTC, aby uniknąć niespójności zależnych od strefy czasowej maszyny.
/// <para>
/// Kontekst: przed naprawą buildery używały DateTimeOffset.UtcNow dla NotBefore,
/// ale DateTimeOffset.Now (czas lokalny) dla NotAfter — mieszanie offsetów
/// w jednym wywołaniu CreateSelfSigned. Ten test zapobiega regresji.
/// </para>
/// </summary>
public class SelfSignedCertificateBuilderTests
{
    /// <summary>
    /// Weryfikuje, że certyfikat podpisu (Signature) ma poprawne daty w UTC
    /// i odpowiednią ważność (~2 lata) niezależnie od strefy czasowej maszyny.
    /// </summary>
    [Theory]
    [InlineData(EncryptionMethodEnum.Rsa)]
    [InlineData(EncryptionMethodEnum.ECDsa)]
    public void Build_SignatureBuilder_ShouldCreateCertificateWithConsistentUtcDates(
        EncryptionMethodEnum encryptionType)
    {
        // Arrange — zapamiętanie czasu UTC przed i po Build(), aby ustalić tolerancję
        DateTime utcBefore = DateTime.UtcNow;

        // Act
        using X509Certificate2 cert = SelfSignedCertificateForSignatureBuilder
            .Create()
            .WithGivenName("Test")
            .WithSurname("Regresja")
            .WithSerialNumber("PNOPL-00000000001")
            .WithCommonName("Test Regresja UTC")
            .AndEncryptionType(encryptionType)
            .Build();

        DateTime utcAfter = DateTime.UtcNow;

        // Assert — daty certyfikatu po konwersji do UTC
        DateTime notBeforeUtc = cert.NotBefore.ToUniversalTime();
        DateTime notAfterUtc = cert.NotAfter.ToUniversalTime();

        // NotBefore powinien być ~61 minut przed momentem tworzenia (tolerancja ±5s na wolnych maszynach CI)
        DateTime expectedNotBefore = utcBefore.AddMinutes(-61);
        Assert.True(
            Math.Abs((notBeforeUtc - expectedNotBefore).TotalSeconds) < 5,
            $"NotBefore ({notBeforeUtc:O}) powinien być ~61 min przed UtcNow ({utcBefore:O})");

        // NotAfter powinien być ~2 lata od momentu tworzenia (tolerancja ±5s)
        DateTime expectedNotAfterMin = utcBefore.AddYears(2);
        DateTime expectedNotAfterMax = utcAfter.AddYears(2);
        Assert.True(
            notAfterUtc >= expectedNotAfterMin.AddSeconds(-5) && notAfterUtc <= expectedNotAfterMax.AddSeconds(5),
            $"NotAfter ({notAfterUtc:O}) powinien być ~2 lata od UtcNow ({utcBefore:O})");

        // Podstawowe asercje poprawności certyfikatu
        Assert.True(notAfterUtc > notBeforeUtc,
            "NotAfter musi być późniejszy niż NotBefore");
        Assert.True(cert.HasPrivateKey,
            "Certyfikat musi zawierać klucz prywatny");
    }

    /// <summary>
    /// Weryfikuje, że certyfikat pieczęci (Seal) ma poprawne daty w UTC
    /// i odpowiednią ważność (~2 lata). SealBuilder obsługuje tylko RSA.
    /// </summary>
    [Fact]
    public void Build_SealBuilder_ShouldCreateCertificateWithConsistentUtcDates()
    {
        // Arrange
        DateTime utcBefore = DateTime.UtcNow;

        // Act
        using X509Certificate2 cert = SelfSignedCertificateForSealBuilder
            .Create()
            .WithOrganizationName("Firma Testowa Regresja sp. z o.o.")
            .WithOrganizationIdentifier("VATPL-0000000000")
            .WithCommonName("Regresja UTC Seal")
            .Build();

        DateTime utcAfter = DateTime.UtcNow;

        // Assert — analogicznie jak w teście SignatureBuilder
        DateTime notBeforeUtc = cert.NotBefore.ToUniversalTime();
        DateTime notAfterUtc = cert.NotAfter.ToUniversalTime();

        // NotBefore: ~61 minut wstecz od UtcNow
        DateTime expectedNotBefore = utcBefore.AddMinutes(-61);
        Assert.True(
            Math.Abs((notBeforeUtc - expectedNotBefore).TotalSeconds) < 5,
            $"NotBefore ({notBeforeUtc:O}) powinien być ~61 min przed UtcNow ({utcBefore:O})");

        // NotAfter: ~2 lata od UtcNow — potwierdza użycie UtcNow (nie .Now)
        DateTime expectedNotAfterMin = utcBefore.AddYears(2);
        DateTime expectedNotAfterMax = utcAfter.AddYears(2);
        Assert.True(
            notAfterUtc >= expectedNotAfterMin.AddSeconds(-5) && notAfterUtc <= expectedNotAfterMax.AddSeconds(5),
            $"NotAfter ({notAfterUtc:O}) powinien być ~2 lata od UtcNow ({utcBefore:O})");

        Assert.True(notAfterUtc > notBeforeUtc,
            "NotAfter musi być późniejszy niż NotBefore");
        Assert.True(cert.HasPrivateKey,
            "Certyfikat musi zawierać klucz prywatny");
    }
}
#endif
