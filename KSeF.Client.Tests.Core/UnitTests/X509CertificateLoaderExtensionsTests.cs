#if !NETFRAMEWORK
using KSeF.Client.Extensions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.UnitTests;

/// <summary>
/// Zawiera testy jednostkowe metod rozszerzeń, które łączą certyfikaty X.509 z kluczami prywatnymi zakodowanymi w formacie PEM.
/// </summary>
public class X509CertificateLoaderExtensionsTests
{
    /// <summary>
    /// Sprawdza, czy metoda MergeWithPemKey poprawnie łączy zwykły (niezaszyfrowany) klucz RSA PEM z certyfikatem
    /// X509Certificate2, tworząc certyfikat zawierający klucz prywatny.
    /// </summary>
    [Fact]
    public void MergeWithPemKey_ShouldMergeRsaKey_WhenKeyIsPlain()
    {
        // Arrange
        (X509Certificate2? publicCert, string? pemKey) = TestCertGenerator.GenerateRsa(encrypted: false);

        // Act
        X509Certificate2 merged = publicCert.MergeWithPemKey(pemKey);

        // Assert
        Assert.True(merged.HasPrivateKey);
        Assert.NotNull(merged.GetRSAPrivateKey());
        Assert.Equal(publicCert.Thumbprint, merged.Thumbprint);
    }

    /// <summary>
    /// Sprawdza, czy metoda MergeWithPemKey poprawnie łączy zwykły (niezaszyfrowany) klucz ECDSA PEM
    /// z certyfikatem X509Certificate2, tworząc certyfikat zawierający klucz prywatny.
    /// </summary>
    [Fact]
    public void MergeWithPemKey_ShouldMergeEcdsaKey_WhenKeyIsPlain()
    {
        // Arrange
        (X509Certificate2? publicCert, string? pemKey) = TestCertGenerator.GenerateEcdsa(encrypted: false);

        // Act
        X509Certificate2 merged = publicCert.MergeWithPemKey(pemKey);

        // Assert
        Assert.True(merged.HasPrivateKey);
        Assert.NotNull(merged.GetECDsaPrivateKey());
        Assert.Equal(publicCert.Thumbprint, merged.Thumbprint);
    }

    /// <summary>
    /// Sprawdza, czy metoda MergeWithPemKey poprawnie łączy zaszyfrowany klucz prywatny PEM z certyfikatem,
    /// gdy zostanie podane prawidłowe hasło.
    /// </summary>
    [Fact]
    public void MergeWithPemKey_ShouldMergeEncryptedKey_WhenPasswordIsCorrect()
    {
        // Arrange
        string password = "StrongPassword123!";
        (X509Certificate2? publicCert, string? pemKey) = TestCertGenerator.GenerateRsa(encrypted: true, password);

        // Act
        X509Certificate2 merged = publicCert.MergeWithPemKey(pemKey, password);

        // Assert
        Assert.True(merged.HasPrivateKey);
    }

    /// <summary>
    /// Sprawdza, czy metoda MergeWithPemKey zgłasza wyjątek ArgumentException,
    /// gdy przekazany klucz PEM jest zaszyfrowany, ale nie podano hasła.
    /// </summary>
    [Fact]
    public void MergeWithPemKey_ShouldThrowArgumentException_WhenKeyIsEncryptedButPasswordMissing()
    {
        // Arrange
        (X509Certificate2? publicCert, string? pemKey) = TestCertGenerator.GenerateRsa(encrypted: true, "Pass");

        // Act & Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            publicCert.MergeWithPemKey(pemKey, null));

        Assert.Contains("wymaga podania hasła", ex.Message);
    }

    /// <summary>
    /// Sprawdza, czy metoda MergeWithPemKey zgłasza wyjątek InvalidOperationException,
    /// gdy próbuje się dołączyć klucz prywatny do certyfikatu, który już posiada klucz prywatny.
    /// </summary>
    [Fact]
    public void MergeWithPemKey_ShouldThrowInvalidOperation_WhenCertAlreadyHasPrivateKey()
    {
        // Arrange
        using RSA rsa = RSA.Create();
        CertificateRequest req = new(
            "CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        X509Certificate2 certWithKey = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));

        string dummyPem = rsa.ExportPkcs8PrivateKeyPem();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            certWithKey.MergeWithPemKey(dummyPem));
    }

    /// <summary>
    /// Sprawdza, czy MergeWithPemKeyNoProfileForEcdsa poprawnie łączy zaszyfrowany
    /// klucz ECDSA PKCS#8 z certyfikatem, gdy podano prawidłowe hasło.
    /// </summary>
    [Fact]
    public void MergeWithPemKeyNoProfileForEcdsa_ShouldMergeEncryptedEcdsaKey_WhenPasswordIsCorrect()
    {
        // Arrange
        const string password = "StrongPassword123!";
        (X509Certificate2 publicCert, string pemKey) = TestCertGenerator.GenerateEcdsa(encrypted: true, password);

        // Act
        X509Certificate2 merged = publicCert.MergeWithPemKeyNoProfileForEcdsa(pemKey, password);

        // Assert
        Assert.True(merged.HasPrivateKey);
        Assert.NotNull(merged.GetECDsaPrivateKey());
        Assert.Equal(publicCert.Thumbprint, merged.Thumbprint);
    }

    /// <summary>
    /// Sprawdza, czy MergeWithPemKeyNoProfileForEcdsa zgłasza CryptographicException,
    /// gdy użyto nieprawidłowego hasła do zaszyfrowanego klucza ECDSA.
    /// </summary>
    [Fact]
    public void MergeWithPemKeyNoProfileForEcdsa_ShouldThrowCryptographicException_WhenPasswordIsWrong()
    {
        // Arrange
        const string correctPassword = "P@ssw0rd!";
        const string wrongPassword = "WrongP@ss";
        (X509Certificate2 publicCert, string pemKey) = TestCertGenerator.GenerateEcdsa(encrypted: true, correctPassword);

        // Act & Assert
        Assert.Throws<CryptographicException>(() =>
            publicCert.MergeWithPemKeyNoProfileForEcdsa(pemKey, wrongPassword));
    }

    /// <summary>
    /// Sprawdza, czy MergeWithPemKeyNoProfileForEcdsa zgłasza NotSupportedException,
    /// gdy zaszyfrowany klucz PKCS#8 nie reprezentuje klucza ECDSA (np. jest to klucz RSA).
    /// </summary>
    [Fact]
    public void MergeWithPemKeyNoProfileForEcdsa_ShouldThrowNotSupportedException_WhenPemContainsRsaKey()
    {
        // Arrange – certyfikat ECDSA, ale zaszyfrowany klucz RSA
        const string password = "Sup3rSecret!";
        X509Certificate2 publicCert = TestCertGenerator.GenerateEcdsa(encrypted: false).PublicCert;
        string rsaPem = TestCertGenerator.GenerateRsaEncryptedPem(password);

        // Act & Assert
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            publicCert.MergeWithPemKeyNoProfileForEcdsa(rsaPem, password));

        Assert.Contains("OID", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sprawdza, czy MergeWithPemKeyNoProfileForEcdsa zgłasza CryptographicException,
    /// gdy przekazany PEM zawiera zwykły (niezaszyfrowany) klucz, a metoda oczekuje
    /// bloku „ENCRYPTED PRIVATE KEY”.
    /// </summary>
    [Fact]
    public void MergeWithPemKeyNoProfileForEcdsa_ShouldThrowCryptographicException_WhenPemIsPlainNotEncrypted()
    {
        // Arrange – metoda oczekuje ENCRYPTED PRIVATE KEY
        (X509Certificate2 publicCert, string plainPem) = TestCertGenerator.GenerateEcdsa(encrypted: false);

        // Act & Assert
        CryptographicException ex = Assert.Throws<CryptographicException>(() =>
            publicCert.MergeWithPemKeyNoProfileForEcdsa(plainPem, "any"));

        Assert.Contains("ENCRYPTED PRIVATE KEY", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sprawdza, czy MergeWithPemKeyNoProfileForEcdsa zgłasza ArgumentNullException,
    /// gdy przekazany PEM jest null lub składa się wyłącznie ze znaków odstępu.
    /// </summary>
    [Fact]
    public void MergeWithPemKeyNoProfileForEcdsa_ShouldThrowArgumentNullException_WhenPemIsNullOrWhitespace()
    {
        // Arrange
        X509Certificate2 publicCert = TestCertGenerator.GenerateEcdsa(encrypted: false).PublicCert;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            publicCert.MergeWithPemKeyNoProfileForEcdsa(null!, "pwd"));

        Assert.Throws<ArgumentNullException>(() =>
            publicCert.MergeWithPemKeyNoProfileForEcdsa("   ", "pwd"));
    }

    /// <summary>
    /// Sprawdza, czy MergeWithPemKeyNoProfileForEcdsa zgłasza ArgumentException,
    /// gdy hasło jest puste lub ma wartość null dla zaszyfrowanego klucza.
    /// </summary>
    [Fact]
    public void MergeWithPemKeyNoProfileForEcdsa_ShouldThrowArgumentException_WhenPasswordIsNullOrEmpty()
    {
        // Arrange
        const string password = "Test123!";
        (X509Certificate2 publicCert, string pemKey) = TestCertGenerator.GenerateEcdsa(encrypted: true, password);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            publicCert.MergeWithPemKeyNoProfileForEcdsa(pemKey, null!));

        Assert.Throws<ArgumentException>(() =>
            publicCert.MergeWithPemKeyNoProfileForEcdsa(pemKey, string.Empty));
    }

    /// <summary>
    /// Sprawdza, czy MergeWithPemKeyNoProfileForEcdsa zgłasza ArgumentNullException,
    /// gdy przekazany certyfikat publiczny ma wartość null.
    /// </summary>
    [Fact]
    public void MergeWithPemKeyNoProfileForEcdsa_ShouldThrowArgumentNullException_WhenPublicCertIsNull()
    {
        // Arrange
        const string password = "StrongPassword123!";
        string pemKey = TestCertGenerator.GenerateEcdsa(encrypted: true, password).PemKey;

        // Act & Assert (wywołanie metody rozszerzającej w formie statycznej, aby przekazać null)
        Assert.Throws<ArgumentNullException>(() =>
            X509CertificateLoaderExtensions.MergeWithPemKeyNoProfileForEcdsa(null!, pemKey, password));
    }

    /// <summary>
    /// Sprawdza, czy MergeWithPemKeyNoProfileForEcdsa zgłasza CryptographicException,
    /// gdy blok ENCRYPTED PRIVATE KEY zawiera niepoprawne dane Base64.
    /// </summary>
    [Fact]
    public void MergeWithPemKeyNoProfileForEcdsa_ShouldThrowCryptographicException_WhenPemBase64IsInvalid()
    {
        // Arrange
        X509Certificate2 publicCert = TestCertGenerator.GenerateEcdsa(encrypted: false).PublicCert;
        const string invalidPem =
            "-----BEGIN ENCRYPTED PRIVATE KEY-----\n@@@not-base64@@@\n-----END ENCRYPTED PRIVATE KEY-----";

        // Act & Assert
        Assert.Throws<CryptographicException>(() =>
            publicCert.MergeWithPemKeyNoProfileForEcdsa(invalidPem, "pwd"));
    }

    /// <summary>
    /// Zapewnia pomocnicze metody do generowania testowych certyfikatów X.509
    /// oraz odpowiadających im kluczy prywatnych w formacie PEM.
    /// </summary>
    internal static class TestCertGenerator
    {
        /// <summary>
        /// Generuje testowy certyfikat X.509 z kluczem RSA oraz odpowiadający mu klucz prywatny w formacie PEM.
        /// </summary>
        /// <param name="encrypted">Określa, czy klucz prywatny PEM ma być zaszyfrowany.</param>
        /// <param name="password">Hasło używane do szyfrowania klucza RSA; wymagane, gdy <paramref name="encrypted"/> jest równe true.</param>
        public static (X509Certificate2 PublicCert, string PemKey) GenerateRsa(bool encrypted, string? password = null)
        {
            using RSA rsa = RSA.Create(2048);
            CertificateRequest req = new(
                "CN=TestRSA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            using X509Certificate2 fullCert = req.CreateSelfSigned(
                DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));

            X509Certificate2 publicCert = X509CertificateLoaderExtensions.LoadCertificate(fullCert.Export(X509ContentType.Cert));

            PbeParameters pbeParams = new(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA256,
                100000);

            string pemKey = encrypted
                ? rsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParams)
                : rsa.ExportPkcs8PrivateKeyPem();

            return (publicCert, pemKey);
        }

        /// <summary>
        /// Generuje testowy certyfikat X.509 z kluczem ECDSA oraz odpowiadający mu klucz prywatny w formacie PEM.
        /// </summary>
        /// <param name="encrypted">Określa, czy klucz prywatny PEM ma być zaszyfrowany.</param>
        /// <param name="password">Hasło używane do szyfrowania klucza ECDSA; wymagane, gdy <paramref name="encrypted"/> jest równe true.</param>
        public static (X509Certificate2 PublicCert, string PemKey) GenerateEcdsa(bool encrypted, string? password = null)
        {
            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest req = new(
                "CN=TestECDSA", ecdsa, HashAlgorithmName.SHA256);

            using X509Certificate2 fullCert = req.CreateSelfSigned(
                DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));

            X509Certificate2 publicCert = X509CertificateLoaderExtensions.LoadCertificate(fullCert.Export(X509ContentType.Cert));

            PbeParameters pbeParams = new(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA256,
                100000);

            string pemKey = encrypted
                ? ecdsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParams)
                : ecdsa.ExportPkcs8PrivateKeyPem();

            return (publicCert, pemKey);
        }

        /// <summary>
        /// Generuje zaszyfrowany klucz RSA w formacie PKCS#8 (PEM) na potrzeby testów,
        /// używany do sprawdzenia reakcji na niezgodny algorytm (RSA zamiast ECDSA).
        /// </summary>
        /// <param name="password">Hasło używane do szyfrowania klucza RSA.</param>
        public static string GenerateRsaEncryptedPem(string password)
        {
            using RSA rsa = RSA.Create(2048);

            PbeParameters pbeParams = new(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA256,
                100_000);

            return rsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParams);
        }
    }
}
#endif
