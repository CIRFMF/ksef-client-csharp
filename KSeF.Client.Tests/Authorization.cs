#nullable enable
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests;

public class Authorization : KsefIntegrationTestBase
{
    [Fact]
    /// <summary>
    /// Uwierzytelnia przy użyciu certyfikatu i NIP-u kontekstu, sesja szyfrowana, rola: właściciel.
    /// </summary>
    public async Task AuthAsyncFullIntegrationFlowReturnsAccessToken()
    {
        // Arrange & Act
        AuthenticationOperationStatusResponse authResult = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, MiscellaneousUtils.GetRandomNip());

        // Assert
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);

        // (opcjonalnie: Assert na format tokena, Claims, czas ważności itp.)
    }

    [Fact]
    /// <summary>
    /// Uwierzytelnia przy użyciu certyfikatu i NIP-u kontekstu, sesja szyfrowana, rola: właściciel.
    /// </summary>
    public async Task AuthAsyncFullIntegrationFlowWithKSeFTokenRSAReturnsAccessToken()
    {
        // Arrange
        // Uwierzytelnij
        AuthenticationOperationStatusResponse authInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, MiscellaneousUtils.GetRandomNip());
        // Najpierw trzeba uwierzytelnić jako właściciel, aby otrzymać token KSeF
        KsefTokenPermissionType[] permissions =
        [
            KsefTokenPermissionType.InvoiceWrite,
            KsefTokenPermissionType.InvoiceRead
        ];

        await Task.Delay(SleepTime);
        KsefTokenResponse ownerToken = await KsefClient.GenerateKsefTokenAsync(new KsefTokenRequest() { Description = $"Wystawianie i przeglądanie faktur", Permissions = permissions }, authInfo.AccessToken.Token);

        await Task.Delay(SleepTime);
        AuthenticationKsefToken ksefTokenStatus = await KsefClient.GetKsefTokenAsync(ownerToken.ReferenceNumber, authInfo.AccessToken.Token);

        await Task.Delay(SleepTime);
        IAuthCoordinator authCoordinator = new AuthCoordinator(AuthorizationClient);
        await Task.Delay(SleepTime);

        AuthenticationTokenContextIdentifierType contextType = AuthenticationTokenContextIdentifierType.Nip;
        string contextValue = ksefTokenStatus.ContextIdentifier.Value;

        AuthenticationTokenAuthorizationPolicy? authorizationPolicy = null;


        // Act
        AuthenticationOperationStatusResponse result = await authCoordinator.AuthKsefTokenAsync(
            contextType,
            contextValue,
            ownerToken.Token,
            CryptographyService,
            EncryptionMethodEnum.Rsa,
            authorizationPolicy,
            default
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);

        // (opcjonalnie: Assert na format tokena, Claims, czas ważności itp.)
    }

    [Theory]
    [InlineData(EncryptionMethodEnum.Rsa)]
    public async Task KsefClientAuthorizationAuthCoordinatorServicePositive(EncryptionMethodEnum encryptionMethod)
    {
        // Arrange
        IAuthCoordinator authCoordinatorService = new AuthCoordinator(AuthorizationClient);
        string testNip = MiscellaneousUtils.GetRandomNip();
        AuthenticationTokenContextIdentifierType contextIdentifierType = AuthenticationTokenContextIdentifierType.Nip;
        AuthenticationOperationStatusResponse authInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, testNip, contextIdentifierType);
        KsefTokenPermissionType[] permissions =
        [
            KsefTokenPermissionType.InvoiceWrite,
            KsefTokenPermissionType.InvoiceRead
        ];
        KsefTokenResponse ownerToken = await KsefClient.GenerateKsefTokenAsync(new KsefTokenRequest()
        {
            Description = $"Wystawianie i przeglądanie faktur",
            Permissions = permissions
        },
            authInfo.AccessToken.Token);

        // Act
        AuthenticationOperationStatusResponse authResult = await authCoordinatorService.AuthKsefTokenAsync(AuthenticationTokenContextIdentifierType.Nip,
            testNip,
            ownerToken.Token,
            CryptographyService,
            encryptionMethod);

        // Assert
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);
        Assert.NotNull(authResult.AccessToken.Token);
        Assert.NotNull(authResult.RefreshToken);
        Assert.NotNull(authResult.RefreshToken.Token);
    }
}