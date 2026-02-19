using KSeF.Client.Api.Builders.Certificates;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Extensions;
using KSeF.Client.Core.Models.Certificates;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#if NETFRAMEWORK
using KSeF.Client.Tests.Compatibility;
#endif

namespace KSeF.Client.Tests.Utils;

/// <summary>
/// Zestaw metod pomocniczych do obsługi cyklu życia certyfikatów w testach:
/// generowanie CSR i klucza prywatnego, wysyłka wniosku certyfikacyjnego,
/// unieważnianie certyfikatu oraz łączenie certyfikatu z kluczem prywatnym.
/// </summary>
internal static class CertificateUtils
{
    /// <summary>
    /// Generuje CSR (Certificate Signing Request) oraz klucz prywatny w formacie Base64.
    /// </summary>
    /// <remarks>
    /// Najpierw pobiera dane niezbędne do wygenerowania CSR z API KSeF, a następnie
    /// używa <see cref="ICryptographyService"/> do przygotowania CSR z wykorzystaniem RSA.
    /// </remarks>
    /// <param name="ksefClient">Klient KSeF do komunikacji z API.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="cryptographyService">Usługa kryptograficzna do generowania CSR.</param>
    /// <returns>Krotka: (csrBase64Encoded, privateKeyBase64Encoded) — oba w Base64.</returns>
    /// <exception cref="Exception">Wyjątki mogą pochodzić z warstwy API lub kryptograficznej.</exception>
    internal static async Task<(string csrBase64Encoded, string privateKeyBase64Encoded)> GenerateCsrAndPrivateKeyAsync(IKSeFClient ksefClient, string accessToken, ICryptographyService cryptographyService, RSASignaturePadding padding)
    {
        CertificateEnrollmentsInfoResponse enrollmentData = await ksefClient
            .GetCertificateEnrollmentDataAsync(accessToken)
            .ConfigureAwait(false);

        return cryptographyService.GenerateCsrWithRsa(enrollmentData, padding);
    }

    /// <summary>
    /// Wysyła wniosek certyfikacyjny (CSR) do KSeF i zwraca numer referencyjny operacji.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF do komunikacji z API.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="csrBase64Encoded">CSR w Base64 (DER).</param>
    /// <param name="certificateType">Typ certyfikatu (domyślnie Authentication).</param>
    /// <returns>Odpowiedź z numerem referencyjnym i znacznikiem czasu.</returns>
    internal static async Task<CertificateEnrollmentResponse> SendCertificateEnrollmentAsync(IKSeFClient ksefClient, string accessToken, string csrBase64Encoded, CertificateType certificateType = CertificateType.Authentication)
    {
        SendCertificateEnrollmentRequest request = SendCertificateEnrollmentRequestBuilder.Create()
                   .WithCertificateName("Test Certificate")
                   .WithCertificateType(certificateType)
                   .WithCsr(csrBase64Encoded)
                   .WithValidFrom(DateTimeOffset.UtcNow)
                   .Build();

        CertificateEnrollmentResponse certificateEnrollmentResponse = await ksefClient.SendCertificateEnrollmentAsync(request, accessToken)
            .ConfigureAwait(false);

        return certificateEnrollmentResponse;
    }

    /// <summary>
    /// Unieważnia certyfikat o podanym numerze seryjnym.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF do komunikacji z API.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="certificateSerialNumber">Numer seryjny certyfikatu do unieważnienia.</param>
    internal static async Task RevokeCertificateAsync(IKSeFClient ksefClient, string accessToken, string certificateSerialNumber)
    {
        CertificateRevokeRequest request = RevokeCertificateRequestBuilder.Create()
            .Build();

        await ksefClient.RevokeCertificateAsync(request, certificateSerialNumber, accessToken)
             .ConfigureAwait(false);
    }

    /// <summary>
    /// Tworzy egzemplarz <see cref="X509Certificate2"/> zawierający klucz prywatny
    /// na podstawie odpowiedzi z API (DER w Base64) oraz klucza prywatnego (Base64).
    /// </summary>
    /// <param name="response">Odpowiedź z certyfikatem w Base64 (DER).</param>
    /// <param name="privateKeyBase64Encoded">Klucz prywatny RSA w Base64.</param>
    /// <returns>Certyfikat X509 połączony z kluczem prywatnym.</returns>
    /// <exception cref="FormatException">Gdy dane Base64 mają niepoprawny format.</exception>
    /// <exception cref="CryptographicException">Gdy import klucza prywatnego nie powiedzie się.</exception>
    internal static X509Certificate2 CreateCertificateWithPrivateKey(CertificateResponse response, string privateKeyBase64Encoded)
    {
        byte[] certBytes = Convert.FromBase64String(response.Certificate);
        X509Certificate2 certificate = certBytes.LoadPkcs12();

        using RSA rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64Encoded), out _);

        return certificate.CopyWithPrivateKey(rsa);
    }

    /// <summary>
    /// Pobiera dane niezbędne do wygenerowania CSR.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF do komunikacji z API.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Dane niezbędne do wygenerowania CSR.</returns>
    internal static async Task<CertificateEnrollmentsInfoResponse> GetCertificateEnrollmentDataAsync(IKSeFClient ksefClient, string accessToken, CancellationToken cancellationToken)
    {
        CertificateEnrollmentsInfoResponse certificateEnrollmentsInfoResponse =
            await ksefClient.GetCertificateEnrollmentDataAsync(accessToken, cancellationToken).ConfigureAwait(false);

        return certificateEnrollmentsInfoResponse;
    }

    /// <summary>
    /// Pobiera status wystawienia certyfikatu (pojedyncze wywołanie).
    /// <param name="ksefClient">Klient KSeF do komunikacji z API.</param>
    /// <param name="enrollmentReference">Numer referencyjny wniosku certyfikacyjnego.</param>
    /// <param name="accessToken">Token dostępu.</param>"
    /// <param name="cancellationToken">Token anulowania.</param>"
    /// </summary>
    internal static async Task<CertificateEnrollmentStatusResponse> GetCertificateEnrollmentStatusAsync(IKSeFClient ksefClient, string enrollmentReference, string accessToken, CancellationToken cancellationToken)
    {
        CertificateEnrollmentStatusResponse certificateEnrollmentStatusResponse = await ksefClient
            .GetCertificateEnrollmentStatusAsync(enrollmentReference, accessToken, cancellationToken).ConfigureAwait(false);

        return certificateEnrollmentStatusResponse;
    }

    /// <summary>
    /// Pobiera wystawione certyfikaty na podstawie podanych numerów seryjnych.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF do komunikacji z API.</param>
    /// <param name="certificateListRequest"></param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Listę wystawionych certyfikatów.</returns>
    internal static async Task<CertificateListResponse> GetCertificateListAsync(IKSeFClient ksefClient, CertificateListRequest certificateListRequest, string accessToken, CancellationToken cancellationToken)
    {
        CertificateListResponse certificateListResponse = await ksefClient
            .GetCertificateListAsync(certificateListRequest, accessToken, cancellationToken).ConfigureAwait(false);

        return certificateListResponse;
    }

    /// <summary>
    /// Zapisuje certyfikat i klucz prywatny do plików.
    /// </summary>
    /// <param name="key">Klucz prywatny</param>
    /// <param name="certificate">Certyfikat.</param>
    /// <param name="certificatePath">Ścieżka do docelowego pliku certyfikatu.</param>
    /// <param name="keyPath">Ścieżka do docelowego pliku klucza prywatnego.</param>
    internal static void SaveCertificateAndKey(string key, CertificateResponse certificate, string certificatePath, string keyPath)
    {
        // Zapis Certyfikatu
        byte[] certBytes = Convert.FromBase64String(certificate.Certificate);
        File.WriteAllBytes(certificatePath, certBytes);

        // Zapis Klucza Prywatnego (Format PEM - PKCS#8 Unencrypted)
        File.WriteAllText(keyPath, key);
    }

    /// <summary>
    /// Tworzy certyfikat X509 z plików certyfikatu i klucza prywatnego.
    /// </summary>
    /// <param name="certificatePath">Ścieżka do pliku z certyfikatem.</param>
    /// <param name="keyPath">Ścieżka do pliku z kluczem prywatnym.</param>
    /// <returns></returns>
    internal static X509Certificate2 LoadCertificateFromFiles(string certificatePath, string keyPath)
    {
        // Wczytywanie certyfikatu z pliku
        X509Certificate2 certificatePem = X509CertificateLoaderExtensions.LoadCertificateFromFile(certificatePath);
        string keyPem = File.ReadAllText(keyPath);

        ECDsa ecdsa = ECDsa.Create();
        byte[] keyBytes = Convert.FromBase64String(keyPem);
        ecdsa.ImportECPrivateKey(keyBytes, out _);

        X509Certificate2 certWithKey = certificatePem.CopyWithPrivateKey(ecdsa);
        return certWithKey;
    }
}