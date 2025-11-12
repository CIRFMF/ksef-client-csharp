using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features;

[CollectionDefinition("Session.feature")]
[Trait("Category", "Features")]
[Trait("Features", "sessions.feature")]
public class InteractiveSessionTests : KsefIntegrationTestBase
{
    private const string OwnerContextNip = "6351111111";
    private const string SecondContextNip = "6451111144";
    private const string operationForbidden = "HTTP 403: Forbidden";


    [Theory]
    [InlineData(SystemCode.FA2)]
    [InlineData(SystemCode.FA3)]
    [Trait("Scenario", "Pytam o status aktualnej sesji interaktywnej")]
    public async Task GivenActiveInteractiveSession_WhenCheckingStatus_ThenReturnsValidStatus(SystemCode systemCode)
    {
        Core.Models.Authorization.AuthenticationOperationStatusResponse authResult = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, OwnerContextNip);

        Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        Core.Models.Sessions.OnlineSession.OpenOnlineSessionRequest openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
           .Create()
           .WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(systemCode), schemaVersion: "1-0E", value: "FA")
           .WithEncryption(
               encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
               initializationVector: encryptionData.EncryptionInfo.InitializationVector)
           .Build();

        Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        Assert.NotNull(openOnlineSessionResponse);
        Assert.NotNull(openOnlineSessionResponse.ReferenceNumber);

        Core.Models.Sessions.SessionStatusResponse sessionStatusResponse = await KsefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);

        Assert.NotNull(sessionStatusResponse);
        Assert.NotNull(sessionStatusResponse.Status);
        Assert.True(sessionStatusResponse.Status.Code == OnlineSessionCodeResponse.SessionOpened);
    }

    [Theory]
    [InlineData(SystemCode.FA2)]
    [InlineData(SystemCode.FA3)]
    [Trait("Scenario", "Pytam o status innej sesji interaktywnej z mojego kontekstu")]
    public async Task GivenSessionFromSameContext_WhenCheckingStatus_ThenReturnsValidStatus(SystemCode systemCode)
    {
        Core.Models.Authorization.AuthenticationOperationStatusResponse authResult = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, OwnerContextNip);

        Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        Core.Models.Sessions.OnlineSession.OpenOnlineSessionRequest openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
               .Create()
               .WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(systemCode), schemaVersion: "1-0E", value: "FA")
               .WithEncryption(
                   encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                   initializationVector: encryptionData.EncryptionInfo.InitializationVector)
               .Build();

        Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        Assert.NotNull(openOnlineSessionResponse.ReferenceNumber);

        Core.Models.Sessions.SessionStatusResponse sessionStatusResponse = await KsefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        Assert.NotNull(sessionStatusResponse);
        Assert.True(sessionStatusResponse.Status.Code == OnlineSessionCodeResponse.SessionOpened);

        Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openSecondOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        Assert.NotNull(openSecondOnlineSessionResponse.ReferenceNumber);

        Core.Models.Sessions.SessionStatusResponse secondSessionStatusResponse = await KsefClient.GetSessionStatusAsync(openSecondOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);

        Assert.NotNull(secondSessionStatusResponse);
        Assert.True(secondSessionStatusResponse.Status.Code == OnlineSessionCodeResponse.SessionOpened);
    }

    [Theory]
    [InlineData(SystemCode.FA2)]
    [InlineData(SystemCode.FA3)]
    [Trait("Scenario", "Pytam o status innej sesji interaktywnej z innego kontekstu")]
    public async Task GivenSessionFromDifferentContext_WhenCheckingStatus_ThenReturnsAuthorizationError(SystemCode systemCode)
    {
        Core.Models.Authorization.AuthenticationOperationStatusResponse authResult = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, OwnerContextNip);

        Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        Core.Models.Sessions.OnlineSession.OpenOnlineSessionRequest openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
               .Create()
               .WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(systemCode), schemaVersion: "1-0E", value: "FA")
               .WithEncryption(
                   encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                   initializationVector: encryptionData.EncryptionInfo.InitializationVector)
               .Build();

        Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        Assert.NotNull(openOnlineSessionResponse);
        Assert.NotNull(openOnlineSessionResponse.ReferenceNumber);

        Core.Models.Sessions.SessionStatusResponse sessionStatusResponse = await KsefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        Assert.True(sessionStatusResponse.Status.Code == InvoiceInSessionStatusCodeResponse.AcceptedForProcessing);

        authResult = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, SecondContextNip); //new context
        Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openSecondOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);

        Core.Models.Sessions.SessionStatusResponse secondSessionStatusResponse = await KsefClient.GetSessionStatusAsync(openSecondOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        Assert.NotNull(secondSessionStatusResponse);
        Assert.NotNull(secondSessionStatusResponse.Status);
        Assert.True(secondSessionStatusResponse.Status.Code == OnlineSessionCodeResponse.SessionOpened);

        KsefApiException callFromSecondContextResponse = await Assert.ThrowsAsync<KsefApiException>(() =>
                    KsefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token));

        Assert.NotNull(callFromSecondContextResponse);
        Assert.Equal(operationForbidden, callFromSecondContextResponse?.Message);
    }

    [Theory]
    [InlineData(SystemCode.FA2)]
    [InlineData(SystemCode.FA3)]
    [Trait("Scenario", "Zamykam sesję interaktywną")]
    public async Task GivenInteractiveSession_WhenClosingSession_ThenSessionIsClosed(SystemCode systemCode)
    {
        Core.Models.Authorization.AuthenticationOperationStatusResponse authResult = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, OwnerContextNip);

        Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        Core.Models.Sessions.OnlineSession.OpenOnlineSessionRequest openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
               .Create()
               .WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(systemCode), schemaVersion: "1-0E", value: "FA")
               .WithEncryption(
                   encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                   initializationVector: encryptionData.EncryptionInfo.InitializationVector)
               .Build();

        Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        Core.Models.Sessions.SessionStatusResponse sessionStatusResponse = await KsefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        Assert.NotNull(sessionStatusResponse);
        Assert.NotNull(sessionStatusResponse.Status);
        Assert.True(sessionStatusResponse.Status.Code == OnlineSessionCodeResponse.SessionOpened);

        await KsefClient.CloseOnlineSessionAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        Core.Models.Sessions.SessionStatusResponse closedSessionStatusResponse = await KsefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);

        Assert.NotNull(closedSessionStatusResponse);
        Assert.NotNull(closedSessionStatusResponse.Status);
        Assert.False(closedSessionStatusResponse.Status.Code == OnlineSessionCodeResponse.SessionOpened);
        Assert.True(closedSessionStatusResponse.Status.Code == OnlineSessionCodeResponse.SessionCancelledNoInvoices);
    }
}