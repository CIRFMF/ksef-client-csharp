using KSeF.Client.Core.Models.Authorization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Api.Builders.X509Certificates;

// Builder interfaces
public interface ISelfSignedCertificateForSignatureBuilder
{
    ISelfSignedCertificateForSignatureBuilderWithName WithGivenName(string name);
    ISelfSignedCertificateForSignatureBuilderWithName WithGivenNames(string[] names);
}

public interface ISelfSignedCertificateForSignatureBuilderWithName
{
    ISelfSignedCertificateForSignatureBuilderWithSurname WithSurname(string surname);
}

public interface ISelfSignedCertificateForSignatureBuilderWithSurname
{
    ISelfSignedCertificateForSignatureBuilderWithSerialNumber WithSerialNumber(string serialNumber);
}

public interface ISelfSignedCertificateForSignatureBuilderWithSerialNumber
{
    ISelfSignedCertificateForSignatureBuilderReady WithCommonName(string commonName);
}

public interface ISelfSignedCertificateForSignatureBuilderWithEncryption
{
    X509Certificate2 Build();
}

public interface ISelfSignedCertificateForSignatureBuilderReady
{
    ISelfSignedCertificateForSignatureBuilderWithEncryption AndEncryptionType(EncryptionMethodEnum encryptionType);
    X509Certificate2 Build();
}


internal class SelfSignedCertificateForSignatureBuilderImpl
    : ISelfSignedCertificateForSignatureBuilder
    , ISelfSignedCertificateForSignatureBuilderWithName
    , ISelfSignedCertificateForSignatureBuilderWithSurname
    , ISelfSignedCertificateForSignatureBuilderWithSerialNumber
    , ISelfSignedCertificateForSignatureBuilderReady
    , ISelfSignedCertificateForSignatureBuilderWithEncryption
{
    private readonly List<string> _subjectParts = [];
    private EncryptionMethodEnum _encryptionType = EncryptionMethodEnum.Rsa;

    public static ISelfSignedCertificateForSignatureBuilder Create() => new SelfSignedCertificateForSignatureBuilderImpl();

    public ISelfSignedCertificateForSignatureBuilderWithName WithGivenName(string name)
    {
        _subjectParts.Add($"2.5.4.42={name}");
        return this;
    }

    public ISelfSignedCertificateForSignatureBuilderWithName WithGivenNames(string[] names)
    {
        foreach (string name in names.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            _subjectParts.Add($"2.5.4.42={name}");
        }

        return this;
    }

    public ISelfSignedCertificateForSignatureBuilderWithSurname WithSurname(string surname)
    {
        _subjectParts.Add($"2.5.4.4={surname}");
        return this;
    }

    public ISelfSignedCertificateForSignatureBuilderWithSerialNumber WithSerialNumber(string serialNumber)
    {
        _subjectParts.Add($"2.5.4.5={serialNumber}");
        return this;
    }

    public ISelfSignedCertificateForSignatureBuilderReady WithCommonName(string commonName)
    {
        _subjectParts.Add($"2.5.4.3={commonName}");
        return this;
    }

    public ISelfSignedCertificateForSignatureBuilderWithEncryption AndEncryptionType(EncryptionMethodEnum encryptionType)
    {
        _encryptionType = encryptionType;
        return this;
    }

    public X509Certificate2 Build()
    {
        _subjectParts.Add("2.5.4.6=PL");

        X500DistinguishedName subjectName = new (string.Join(", ", _subjectParts));
              
        CertificateRequest request;
        if (_encryptionType == EncryptionMethodEnum.ECDsa)
        {
            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256); // P-256
            request = new CertificateRequest(subjectName, ecdsa, HashAlgorithmName.SHA256);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-61), DateTimeOffset.Now.AddYears(2));
        }
        else
        {
            using RSA rsa = RSA.Create(2048);
            request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-61), DateTimeOffset.Now.AddYears(2));
        }
    }
}

public static class SelfSignedCertificateForSignatureBuilder
{
    public static ISelfSignedCertificateForSignatureBuilder Create() =>
        SelfSignedCertificateForSignatureBuilderImpl.Create();
}
