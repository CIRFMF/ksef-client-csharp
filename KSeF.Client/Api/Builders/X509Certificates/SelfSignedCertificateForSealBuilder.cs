using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Api.Builders.X509Certificates;

public interface ISelfSignedCertificateForSealBuilder
{
    ISelfSignedCertificateForSealBuilderWithOrganizationName WithOrganizationName(string organizationName);
}

public interface ISelfSignedCertificateForSealBuilderWithOrganizationName
{
    ISelfSignedCertificateForSealBuilderWithOrganizationIdentifier WithOrganizationIdentifier(string organizationIdentifier);
}

public interface ISelfSignedCertificateForSealBuilderWithOrganizationIdentifier
{
    ISelfSignedCertificateForSealBuilderReady WithCommonName(string commonName);
}

public interface ISelfSignedCertificateForSealBuilderReady
{
    X509Certificate2 Build();
}

// Implementation
internal class SelfSignedCertificateForSealBuilderImpl
    : ISelfSignedCertificateForSealBuilder
    , ISelfSignedCertificateForSealBuilderWithOrganizationName
    , ISelfSignedCertificateForSealBuilderWithOrganizationIdentifier
    , ISelfSignedCertificateForSealBuilderReady
{
    private readonly List<string> _subjectParts = [];

    public static ISelfSignedCertificateForSealBuilder Create() => new SelfSignedCertificateForSealBuilderImpl();

    public ISelfSignedCertificateForSealBuilderWithOrganizationName WithOrganizationName(string organizationName)
    {
        _subjectParts.Add($"2.5.4.10={organizationName}");
        return this;
    }

    public ISelfSignedCertificateForSealBuilderWithOrganizationIdentifier WithOrganizationIdentifier(string organizationIdentifier)
    {
        _subjectParts.Add($"2.5.4.97={organizationIdentifier}");
        return this;
    }

    public ISelfSignedCertificateForSealBuilderReady WithCommonName(string commonName)
    {
        _subjectParts.Add($"2.5.4.3={commonName}");
        return this;
    }

    public X509Certificate2 Build()
    {
        _subjectParts.Add("2.5.4.6=PL");

        var subjectName = string.Join(", ", _subjectParts);

        var certificate = new CertificateRequest(subjectName, RSA.Create(2048), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.Now.AddYears(2));

        return certificate;
    }
}

public static class SelfSignedCertificateForSealBuilder
{
    public static ISelfSignedCertificateForSealBuilder Create() =>
        SelfSignedCertificateForSealBuilderImpl.Create();
}
