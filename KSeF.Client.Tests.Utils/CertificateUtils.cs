#nullable enable
using KSeF.Client.Api.Builders.Certificates;
using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Extensions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Utils;

public static class CertificateUtils
{
    public static async Task<(string csrBase64Encoded, string privateKeyBase64Encoded)> GenerateCsrAndPrivateKeyWithRsaAsync(IKSeFClient ksefClient, string accessToken, ICryptographyService cryptographyService, RSASignaturePadding? padding = null)
    {
        CertificateEnrollmentsInfoResponse enrollmentData = await ksefClient
            .GetCertificateEnrollmentDataAsync(accessToken).ConfigureAwait(false);

        return cryptographyService.GenerateCsrWithRsa(enrollmentData, padding);
    }

    public static async Task<(string csrBase64Encoded, string privateKeyBase64Encoded)> GenerateCsrAndPrivateKeyWithEcdsaAsync(IKSeFClient ksefClient, string accessToken, ICryptographyService cryptographyService)
    {
        CertificateEnrollmentsInfoResponse enrollmentData = await ksefClient
            .GetCertificateEnrollmentDataAsync(accessToken).ConfigureAwait(false);

        return cryptographyService.GenerateCsrWithEcdsa(enrollmentData);
    }

    public static async Task<CertificateEnrollmentResponse> SendCertificateEnrollmentAsync(IKSeFClient ksefClient, string accessToken, string csrBase64Encoded, CertificateType certificateType = CertificateType.Authentication)
    {
        SendCertificateEnrollmentRequest request = SendCertificateEnrollmentRequestBuilder.Create()
                   .WithCertificateName("Test Certificate")
                   .WithCertificateType(certificateType)
                   .WithCsr(csrBase64Encoded)
                   .WithValidFrom(DateTimeOffset.UtcNow)
                   .Build();

        CertificateEnrollmentResponse certificateEnrollmentResponse = await ksefClient.SendCertificateEnrollmentAsync(request, accessToken).ConfigureAwait(false);

        return certificateEnrollmentResponse;
    }

    public static async Task RevokeCertificateAsync(IKSeFClient ksefClient, string accessToken, string certificateSerialNumber)
    {
        CertificateRevokeRequest request = RevokeCertificateRequestBuilder.Create()
            .Build();

        await ksefClient.RevokeCertificateAsync(request, certificateSerialNumber, accessToken).ConfigureAwait(false);
    }

    public static X509Certificate2 CreateCertificateWithPrivateKey(CertificateResponse response, string privateKeyBase64Encoded)
    {
        byte[] certBytes = Convert.FromBase64String(response.Certificate);
        X509Certificate2 certificate =
        certBytes.LoadPkcs12();

        using RSA rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64Encoded), out _);

        return certificate.CopyWithPrivateKey(rsa);
    }

    /// <summary>
    /// Tworzy testowy, samopodpisany certyfikat przeznaczony do składania podpisu (XAdES).
    /// </summary>
    /// <param name="givenName">Imię właściciela certyfikatu.</param>
    /// <param name="surname">Nazwisko właściciela certyfikatu.</param>
    /// <param name="serialNumberPrefix">Prefiks numeru seryjnego.</param>
    /// <param name="serialNumber">Numer seryjny.</param>
    /// <param name="commonName">Wspólna nazwa (CN) certyfikatu.</param>
    /// <returns><see cref="X509Certificate2"/> będący samopodpisanym certyfikatem do podpisu.</returns>
    public static X509Certificate2 GetPersonalCertificate(
        string givenName,
        string surname,
        string serialNumberPrefix,
        string serialNumber,
        string commonName,
        EncryptionMethodEnum encryptionType = EncryptionMethodEnum.Rsa
        )
    {
        X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
                    .Create()
                    .WithGivenName(givenName)
                    .WithSurname(surname)
                    .WithSerialNumber($"{serialNumberPrefix}-{serialNumber}")
                    .WithCommonName(commonName)
                    .AndEncryptionType(encryptionType)
                    .Build();
        return certificate;
    }

    /// <summary>
    /// Tworzy testowy, samopodpisany certyfikat pieczęci podmiotu.
    /// </summary>
    /// <param name="organizationName">Nazwa organizacji.</param>
    /// <param name="organizationIdentifier">Identyfikator organizacji (np. NIP).</param>
    /// <param name="commonName">Wspólna nazwa (CN) certyfikatu.</param>
    /// <returns><see cref="X509Certificate2"/> będący samopodpisanym certyfikatem pieczęci.</returns>
    public static X509Certificate2 GetCompanySeal(
        string organizationName,
        string organizationIdentifier,
        string commonName)
    {
        X509Certificate2 certificate = SelfSignedCertificateForSealBuilder
                    .Create()
                    .WithOrganizationName(organizationName)
                    .WithOrganizationIdentifier(organizationIdentifier)
                    .WithCommonName(commonName)
                    .Build();
        return certificate;
    }

    /// <summary>
    /// Zwraca odcisk palca certyfikatu w formie SHA256.
    /// </summary>
    /// <param name="certificate"></param>
    /// <returns></returns>
    public static string GetSha256Fingerprint(X509Certificate2 certificate)
    {
        byte[] raw = certificate.RawData;
#if NETFRAMEWORK
        using SHA256 sha = SHA256.Create();
        byte[] sha256Bytes = sha.ComputeHash(raw);
        string sha256Fingerprint = BitConverter.ToString(sha256Bytes).Replace("-", "").ToUpperInvariant();
#else
        byte[] sha256Bytes = SHA256.HashData(raw);
        string sha256Fingerprint = Convert.ToHexString(sha256Bytes).ToUpperInvariant();
#endif
        return sha256Fingerprint;
    }
}