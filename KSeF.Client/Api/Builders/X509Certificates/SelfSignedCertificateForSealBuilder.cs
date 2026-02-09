using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Api.Builders.X509Certificates;

/// <summary>
/// Buduje samopodpisany certyfikat X.509 do pieczęci (seal) podmiotu w KSeF.
/// </summary>
public interface ISelfSignedCertificateForSealBuilder
{
    /// <summary>
    /// Dodaje nazwę organizacji (Organization Name, OID 2.5.4.10).
    /// </summary>
    /// <param name="organizationName">Nazwa organizacji.</param>
    /// <returns>Interfejs pozwalający ustawić identyfikator organizacji.</returns>
    ISelfSignedCertificateForSealBuilderWithOrganizationName WithOrganizationName(string organizationName);
}

/// <summary>
/// Etap budowy certyfikatu po ustawieniu nazwy organizacji.
/// </summary>
public interface ISelfSignedCertificateForSealBuilderWithOrganizationName
{
    /// <summary>
    /// Dodaje identyfikator organizacji (Organization Identifier, OID 2.5.4.97).
    /// </summary>
    /// <param name="organizationIdentifier">Identyfikator organizacji (np. NIP w formacie zgodnym ze standardem).</param>
    /// <returns>Interfejs pozwalający ustawić nazwę wspólną (CN).</returns>
    ISelfSignedCertificateForSealBuilderWithOrganizationIdentifier WithOrganizationIdentifier(string organizationIdentifier);
}

/// <summary>
/// Etap budowy certyfikatu po ustawieniu identyfikatora organizacji.
/// </summary>
public interface ISelfSignedCertificateForSealBuilderWithOrganizationIdentifier
{
    /// <summary>
    /// Dodaje nazwę wspólną (Common Name, OID 2.5.4.3) certyfikatu.
    /// </summary>
    /// <param name="commonName">Nazwa wspólna (CN) certyfikatu.</param>
    /// <returns>Interfejs pozwalający zbudować certyfikat.</returns>
    ISelfSignedCertificateForSealBuilderReady WithCommonName(string commonName);
}

/// <summary>
/// Ostatni etap budowy certyfikatu pieczęci.
/// </summary>
public interface ISelfSignedCertificateForSealBuilderReady
{
    /// <summary>
    /// Tworzy samopodpisany certyfikat X.509 dla pieczęci organizacji.
    /// </summary>
    /// <returns>Samopodpisany certyfikat X.509.</returns>
    X509Certificate2 Build();
}

/// <inheritdoc />
internal sealed class SelfSignedCertificateForSealBuilderImpl
    : ISelfSignedCertificateForSealBuilder
    , ISelfSignedCertificateForSealBuilderWithOrganizationName
    , ISelfSignedCertificateForSealBuilderWithOrganizationIdentifier
    , ISelfSignedCertificateForSealBuilderReady
{
    private readonly List<string> _subjectParts = [];

    /// <summary>
    /// Tworzy nową implementację buildera certyfikatu pieczęci.
    /// </summary>
    /// <returns>Interfejs startowy buildera.</returns>
    public static ISelfSignedCertificateForSealBuilder Create() => new SelfSignedCertificateForSealBuilderImpl();

    /// <inheritdoc />
    public ISelfSignedCertificateForSealBuilderWithOrganizationName WithOrganizationName(string organizationName)
    {
        _subjectParts.Add($"2.5.4.10={organizationName}");
        return this;
    }

    /// <inheritdoc />
    public ISelfSignedCertificateForSealBuilderWithOrganizationIdentifier WithOrganizationIdentifier(string organizationIdentifier)
    {
        _subjectParts.Add($"2.5.4.97={organizationIdentifier}");
        return this;
    }

    /// <inheritdoc />
    public ISelfSignedCertificateForSealBuilderReady WithCommonName(string commonName)
    {
        _subjectParts.Add($"2.5.4.3={commonName}");
        return this;
    }

    /// <inheritdoc />
    public X509Certificate2 Build()
    {
        _subjectParts.Add("2.5.4.6=PL");
        string subjectDN = string.Join(", ", _subjectParts);

#if NETSTANDARD2_0
        // NAPRAWA: DateTimeOffset.UtcNow zamiast .Now — NotBefore i NotAfter muszą mieć
        // spójne offsety (oba UTC), aby certyfikat nie zależał od strefy czasowej maszyny.
        return Compatibility.SelfSignedCertificateCompat.CreateSelfSignedRsa(
            subjectDN,
            DateTimeOffset.UtcNow.AddMinutes(-61),
            DateTimeOffset.UtcNow.AddYears(2));
#else
        // NAPRAWA: DateTimeOffset.UtcNow zamiast .Now — spójność z NotBefore (UTC).
        // Mieszanie .UtcNow (NotBefore) z .Now (NotAfter) powodowało zależność certyfikatu
        // od strefy czasowej maszyny — różne offsety w jednym wywołaniu CreateSelfSigned.
        X509Certificate2 certificate = new CertificateRequest(subjectDN, RSA.Create(2048), HashAlgorithmName.SHA256, RSASignaturePadding.Pss)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-61), DateTimeOffset.UtcNow.AddYears(2));

        return certificate;
#endif
    }
}

/// <summary>
/// Pomocnicza klasa startowa do tworzenia buildera certyfikatu pieczęci.
/// </summary>
public static class SelfSignedCertificateForSealBuilder
{
    /// <summary>
    /// Tworzy nowy builder samopodpisanego certyfikatu pieczęci.
    /// </summary>
    /// <returns>Interfejs startowy buildera.</returns>
    public static ISelfSignedCertificateForSealBuilder Create() =>
        SelfSignedCertificateForSealBuilderImpl.Create();
}