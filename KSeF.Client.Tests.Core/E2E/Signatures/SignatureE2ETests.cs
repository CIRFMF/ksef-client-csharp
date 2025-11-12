using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Signatures;
public class SignatureE2ETests : TestBase
{
    /// <summary>
    /// Użycie SignatureService do podpisania dokumentu XML
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateSignedXmlDocument_ValidInput_Success()
    {
        // Arrange
        string pesel = MiscellaneousUtils.GetRandomPesel();

        AuthenticationChallengeResponse challengeResponse = await AuthorizationClient.GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(AuthenticationTokenContextIdentifierType.Nip, MiscellaneousUtils.GetRandomNip())
           .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
           .WithAuthorizationPolicy(new AuthenticationTokenAuthorizationPolicy { /* ... */ })
           .Build();

        string unsignedXml = authTokenRequest.SerializeToXmlString();

        System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
            .Create()
            .WithGivenName("A")
            .WithSurname("R")
            .WithSerialNumber("PNOPL-" + pesel)
            .WithCommonName("A R")
            .Build();

        // Act
        string signedXml = SignatureService.Sign(unsignedXml, certificate);

        // Assert
        Assert.NotNull(signedXml);
    }

    /// <summary>
    /// Wysłanie podpisanego dokumentu XML do KSeF i otrzymanie tokenu uwierzytelniającego.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task SubmitXadesAuthRequestAsync_E2E_Positive()
    {
        // Arrange
        string pesel = MiscellaneousUtils.GetRandomPesel();

        AuthenticationChallengeResponse challengeResponse = await AuthorizationClient.GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(AuthenticationTokenContextIdentifierType.Nip, MiscellaneousUtils.GetRandomNip())
           .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
           .WithAuthorizationPolicy(new AuthenticationTokenAuthorizationPolicy { /* ... */ })
           .Build();

        string unsignedXml = authTokenRequest.SerializeToXmlString();

        System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
            .Create()
            .WithGivenName("A")
            .WithSurname("R")
            .WithSerialNumber("PNOPL-" + pesel)
            .WithCommonName("A R")
            .Build();

        string signedXml = SignatureService.Sign(unsignedXml, certificate);

        // Act
        SignatureResponse authOperationInfo = await AuthorizationClient
            .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        // Assert
        Assert.NotNull(authOperationInfo);
        Assert.NotNull(authOperationInfo.ReferenceNumber);
        Assert.NotNull(authOperationInfo.AuthenticationToken);
    }
}
