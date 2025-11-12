using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Token;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Authorization;

public class AuthorizationE2ETests : TestBase
{
    private const string OwnerRole = "owner";
    private const string ChallengeToken = "samplechallengeToken!@#123";

    /// <summary>
    /// Uwierzytelnia klienta KSeF i sprawdza, czy zwrócony token dostępu jest poprawny
    /// </summary>
    [Theory]
    [InlineData(EncryptionMethodEnum.Rsa)]
    [InlineData(EncryptionMethodEnum.ECDsa)]
    public async Task AuthAsync_FullIntegrationFlow_ReturnsAccessToken(EncryptionMethodEnum encryptionMethodEnum)
    {
        // Arrange & Act
        AuthenticationOperationStatusResponse authResult =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, default, encryptionMethodEnum);

        // Assert
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);

        PersonToken personToken = TokenService.MapFromJwt(authResult.AccessToken!.Token!);
        Assert.NotNull(personToken);
        Assert.Contains(OwnerRole, personToken.Roles, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Budowanie żądania tokenu autoryzacyjnego.
    /// </summary>
    [Fact]
    public void AuthTokenRequestBuilder_Create_ShouldReturnObject()
    {
        AuthenticationTokenContextIdentifierType contextIdentifierType = 
            AuthenticationTokenContextIdentifierType.Nip;
        string nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationTokenSubjectIdentifierTypeEnum subjectIdentifierTypeEnum = AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject;

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(ChallengeToken)
           .WithContext(contextIdentifierType, nip)
           .WithIdentifierType(subjectIdentifierTypeEnum)
           .WithAuthorizationPolicy(null)
           .Build();

        Assert.NotNull(authTokenRequest);
        Assert.Equal(ChallengeToken, authTokenRequest.Challenge);
        Assert.Equal(contextIdentifierType, authTokenRequest.ContextIdentifier.Type);
        Assert.Equal(nip, authTokenRequest.ContextIdentifier.Value);
        Assert.Equal(subjectIdentifierTypeEnum, authTokenRequest.SubjectIdentifierType);
    }
}

