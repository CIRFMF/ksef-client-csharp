using KSeF.Client.Core.Models.Authorization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Api.Builders.X509Certificates;

/// <summary>
/// Buduje samopodpisany certyfikat X.509 do podpisu w procesie uwierzytelniania KSeF.
/// </summary>
public interface ISelfSignedCertificateForSignatureBuilder
{
    /// <summary>
    /// Dodaje imię posiadacza certyfikatu (atrybut givenName, OID 2.5.4.42).
    /// </summary>
    /// <param name="name">Imię posiadacza certyfikatu.</param>
    /// <returns>Interfejs pozwalający ustawić kolejne imię właściciela certyfikatu.</returns>
    ISelfSignedCertificateForSignatureBuilderWithName WithGivenName(string name);

    /// <summary>
    /// Dodaje jedno lub więcej imion posiadacza certyfikatu (atrybut givenName, OID 2.5.4.42).
    /// </summary>
    /// <param name="names">Tablica imion posiadacza certyfikatu.</param>
    /// <returns>Interfejs pozwalający ustawić nazwisko.</returns>
    ISelfSignedCertificateForSignatureBuilderWithName WithGivenNames(string[] names);
}

/// <summary>
/// Etap budowy certyfikatu po ustawieniu imienia / imion.
/// </summary>
public interface ISelfSignedCertificateForSignatureBuilderWithName
{
    /// <summary>
    /// Dodaje nazwisko posiadacza certyfikatu (atrybut surname, OID 2.5.4.4).
    /// </summary>
    /// <param name="surname">Nazwisko posiadacza certyfikatu.</param>
    /// <returns>Interfejs pozwalający ustawić numer seryjny.</returns>
    ISelfSignedCertificateForSignatureBuilderWithSurname WithSurname(string surname);
}

/// <summary>
/// Etap budowy certyfikatu po ustawieniu nazwiska.
/// </summary>
public interface ISelfSignedCertificateForSignatureBuilderWithSurname
{
    /// <summary>
    /// Dodaje numer seryjny posiadacza certyfikatu (atrybut serialNumber, OID 2.5.4.5).
    /// </summary>
    /// <param name="serialNumber">Numer seryjny identyfikujący posiadacza certyfikatu.</param>
    /// <returns>Interfejs pozwalający ustawić nazwę wspólną (CN).</returns>
    ISelfSignedCertificateForSignatureBuilderWithSerialNumber WithSerialNumber(string serialNumber);
}

/// <summary>
/// Etap budowy certyfikatu po ustawieniu numeru seryjnego.
/// </summary>
public interface ISelfSignedCertificateForSignatureBuilderWithSerialNumber
{
    /// <summary>
    /// Dodaje nazwę wspólną (Common Name, OID 2.5.4.3) posiadacza certyfikatu.
    /// </summary>
    /// <param name="commonName">Nazwa wspólna (CN) certyfikatu.</param>
    /// <returns>Interfejs pozwalający wybrać typ szyfrowania lub zbudować certyfikat.</returns>
    ISelfSignedCertificateForSignatureBuilderReady WithCommonName(string commonName);
}

/// <summary>
/// Etap budowy certyfikatu po ustawieniu wszystkich danych DN i typu szyfrowania.
/// </summary>
public interface ISelfSignedCertificateForSignatureBuilderWithEncryption
{
    /// <summary>
    /// Tworzy samopodpisany certyfikat X.509 na podstawie ustawionych danych.
    /// </summary>
    /// <returns>Samopodpisany certyfikat X.509.</returns>
    X509Certificate2 Build();
}

/// <summary>
/// Etap budowy certyfikatu po ustawieniu danych DN, przed wyborem typu szyfrowania.
/// </summary>
public interface ISelfSignedCertificateForSignatureBuilderReady
{
    /// <summary>
    /// Ustawia typ szyfrowania (klucz i algorytm), jaki ma być użyty do wygenerowania certyfikatu.
    /// Domyślnie używany jest <see cref="EncryptionMethodEnum.Rsa"/>.
    /// </summary>
    /// <param name="encryptionType">Typ szyfrowania (RSA lub ECDsa).</param>
    /// <returns>Interfejs pozwalający zbudować certyfikat.</returns>
    ISelfSignedCertificateForSignatureBuilderWithEncryption AndEncryptionType(EncryptionMethodEnum encryptionType);

    /// <summary>
    /// Tworzy samopodpisany certyfikat X.509 z użyciem domyślnego typu szyfrowania.
    /// </summary>
    /// <returns>Samopodpisany certyfikat X.509.</returns>
    X509Certificate2 Build();
}

/// <inheritdoc />
internal sealed class SelfSignedCertificateForSignatureBuilderImpl
    : ISelfSignedCertificateForSignatureBuilder
    , ISelfSignedCertificateForSignatureBuilderWithName
    , ISelfSignedCertificateForSignatureBuilderWithSurname
    , ISelfSignedCertificateForSignatureBuilderWithSerialNumber
    , ISelfSignedCertificateForSignatureBuilderReady
    , ISelfSignedCertificateForSignatureBuilderWithEncryption
{
    private readonly List<string> _subjectParts = [];
    private EncryptionMethodEnum _encryptionType = EncryptionMethodEnum.Rsa;

    /// <summary>
    /// Tworzy nową implementację buildera certyfikatu do podpisu.
    /// </summary>
    /// <returns>Interfejs startowy buildera.</returns>
    public static ISelfSignedCertificateForSignatureBuilder Create() => new SelfSignedCertificateForSignatureBuilderImpl();

    /// <inheritdoc />
    public ISelfSignedCertificateForSignatureBuilderWithName WithGivenName(string name)
    {
        _subjectParts.Add($"2.5.4.42={name}");
        return this;
    }

    /// <inheritdoc />
    public ISelfSignedCertificateForSignatureBuilderWithName WithGivenNames(string[] names)
    {
        foreach (string name in names.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            _subjectParts.Add($"2.5.4.42={name}");
        }

        return this;
    }

    /// <inheritdoc />
    public ISelfSignedCertificateForSignatureBuilderWithSurname WithSurname(string surname)
    {
        _subjectParts.Add($"2.5.4.4={surname}");
        return this;
    }

    /// <inheritdoc />
    public ISelfSignedCertificateForSignatureBuilderWithSerialNumber WithSerialNumber(string serialNumber)
    {
        _subjectParts.Add($"2.5.4.5={serialNumber}");
        return this;
    }

    /// <inheritdoc />
    public ISelfSignedCertificateForSignatureBuilderReady WithCommonName(string commonName)
    {
        _subjectParts.Add($"2.5.4.3={commonName}");
        return this;
    }

    /// <inheritdoc />
    public ISelfSignedCertificateForSignatureBuilderWithEncryption AndEncryptionType(EncryptionMethodEnum encryptionType)
    {
        _encryptionType = encryptionType;
        return this;
    }

    /// <inheritdoc />
    public X509Certificate2 Build()
    {
#if NETSTANDARD2_0
        throw new PlatformNotSupportedException(
            "Self-signed certificate generation is not supported on netstandard2.0. " +
            "Use .NET 8.0 or later, or provide a pre-existing certificate.");
#else
        _subjectParts.Add("2.5.4.6=PL");

        X500DistinguishedName subjectName = new(string.Join(", ", _subjectParts));

        CertificateRequest request;
        if (_encryptionType == EncryptionMethodEnum.ECDsa)
        {
            using ECDsa ecdsa = ECDsa.Create(); // P-256
            request = new CertificateRequest(subjectName, ecdsa, HashAlgorithmName.SHA256);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-61), DateTimeOffset.Now.AddYears(2));
        }
        else
        {
            using RSA rsa = RSA.Create(2048);
            request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-61), DateTimeOffset.Now.AddYears(2));
        }
#endif
    }
}

/// <summary>
/// Pomocnicza klasa startowa do tworzenia buildera certyfikatu do podpisu.
/// </summary>
public static class SelfSignedCertificateForSignatureBuilder
{
    /// <summary>
    /// Tworzy nowy builder samopodpisanego certyfikatu do podpisu.
    /// </summary>
    /// <returns>Interfejs startowy buildera.</returns>
    public static ISelfSignedCertificateForSignatureBuilder Create() =>
        SelfSignedCertificateForSignatureBuilderImpl.Create();
}