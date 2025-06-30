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

public interface ISelfSignedCertificateForSignatureBuilderReady
{
    X509Certificate2 Build();
}


// Implementation
internal class SelfSignedCertificateForSignatureBuilderImpl
    : ISelfSignedCertificateForSignatureBuilder
    , ISelfSignedCertificateForSignatureBuilderWithName
    , ISelfSignedCertificateForSignatureBuilderWithSurname
    , ISelfSignedCertificateForSignatureBuilderWithSerialNumber
    , ISelfSignedCertificateForSignatureBuilderReady
{
    private readonly List<string> _subjectParts = [];

    public static ISelfSignedCertificateForSignatureBuilder Create() => new SelfSignedCertificateForSignatureBuilderImpl();

    public ISelfSignedCertificateForSignatureBuilderWithName WithGivenName(string name)
    {
        _subjectParts.Add($"2.5.4.42={name}");
        return this;
    }

    public ISelfSignedCertificateForSignatureBuilderWithName WithGivenNames(string[] names)
    {
        foreach (var name in names.Where(x => !string.IsNullOrWhiteSpace(x)))
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

    public X509Certificate2 Build()
    {
        _subjectParts.Add("2.5.4.6=PL");

        var subjectName = string.Join(", ", _subjectParts);

        var certificate = new CertificateRequest(subjectName, RSA.Create(2048), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.Now.AddYears(2));

        return certificate;
    }
}

public static class SelfSignedCertificateForSignatureBuilder
{
    public static ISelfSignedCertificateForSignatureBuilder Create() =>
        SelfSignedCertificateForSignatureBuilderImpl.Create();
}
