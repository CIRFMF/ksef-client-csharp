#nullable enable
using KSeF.Client.Core.Models.Certificates;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.UnitTests.Helpers;

/// <summary>
/// Pomocnicze metody do tworzenia certyfikatów X.509 na potrzeby testów jednostkowych.
/// </summary>
internal static class CertificateTestHelpers
{
    internal const int RsaKeySize = 2048;
    internal const int CertValidityYears = 2;
    internal const int CertStartOffsetMinutes = -1;

    /// <summary>
    /// Tworzy samopodpisany certyfikat RSA 2048-bit z podanym przedziałem ważności.
    /// </summary>
    internal static X509Certificate2 CreateRsaCert(string cn, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using RSA rsa = RSA.Create(RsaKeySize);
        CertificateRequest req = new($"CN={cn}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return req.CreateSelfSigned(notBefore, notAfter);
    }

    /// <summary>
    /// Tworzy samopodpisany certyfikat RSA 2048-bit ważny przez 2 lata od teraz.
    /// </summary>
    internal static X509Certificate2 CreateRsaCert(string cn) =>
        CreateRsaCert(cn, DateTimeOffset.UtcNow.AddMinutes(CertStartOffsetMinutes), DateTimeOffset.UtcNow.AddYears(CertValidityYears));

    /// <summary>
    /// Tworzy <see cref="PemCertificateInfo"/> z certyfikatu X.509.
    /// </summary>
    internal static PemCertificateInfo ToPemInfo(
        X509Certificate2 cert,
        PublicKeyCertificateUsage usage,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validTo = null) => new()
        {
            Certificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert)),
            ValidFrom = validFrom ?? cert.NotBefore,
            ValidTo = validTo ?? cert.NotAfter,
            Usage = new[] { usage }
        };

    /// <summary>
    /// Tworzy parę certyfikatów (SymmetricKeyEncryption + KsefTokenEncryption) na potrzeby testów serwisu kryptograficznego.
    /// </summary>
    internal static (PemCertificateInfo Symmetric, PemCertificateInfo Token) CreateCertPair(
        string cnSymmetric = "KSeF-Symmetric",
        string cnToken = "KSeF-Token",
        DateTimeOffset? symmetricValidFrom = null)
    {
        X509Certificate2 symCert = CreateRsaCert(cnSymmetric);
        X509Certificate2 tokCert = CreateRsaCert(cnToken);

        PemCertificateInfo symInfo = ToPemInfo(symCert, PublicKeyCertificateUsage.SymmetricKeyEncryption,
            validFrom: symmetricValidFrom ?? (DateTimeOffset)symCert.NotBefore);
        PemCertificateInfo tokInfo = ToPemInfo(tokCert, PublicKeyCertificateUsage.KsefTokenEncryption);

        return (symInfo, tokInfo);
    }
}
