#nullable enable
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Authorization.Rev2;
using KSeF.Client.Core.Models.Sessions.ActiveSessions.Rev2;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.E2E.Authorization;

/// <summary>
/// E2E dla operacji uwierzytelniania w rewizji Rev2:
/// <c>GET /auth/rev2/{referenceNumber}</c> oraz <c>GET /auth/sessions/rev2</c>.
/// </summary>
public class AuthorizationRev2E2ETests : TestBase
{
    private const int AuthInProgressCode = 100;
    private const int AuthSuccessCode = 200;

    /// <summary>
    /// Uwierzytelnienie XAdES, odpytywanie statusu przez endpoint Rev2 aż do zakończenia,
    /// weryfikacja kategorii metody uwierzytelnienia w postaci code/rawCode/displayName.
    /// </summary>
    [Fact]
    public async Task GetAuthStatusRev2Async_AfterXadesAuthentication_ReturnsSuccessWithExtensibleCategory()
    {
        // Arrange
        string nip = MiscellaneousUtils.GetRandomNip();
        X509Certificate2 certificate = CertificateUtils.GetPersonalCertificate("A", "R", "TINPL", nip, "A R");

        AuthenticationChallengeResponse challenge = await AuthorizationClient.GetAuthChallengeAsync(CancellationToken);
        AuthenticationTokenRequest authTokenRequest = AuthenticationUtils.GetAuthorizationTokenRequest(
            challenge.Challenge,
            AuthenticationTokenContextIdentifierType.Nip,
            nip);
        string signedXml = SignatureService.Sign(AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest), certificate);

        SignatureResponse authOperation = await AuthorizationClient.SubmitXadesAuthRequestAsync(signedXml, false, false, CancellationToken);

        // Act
        AuthStatusRev2 status = await AsyncPollingUtils.PollAsync(
            () => AuthorizationClient.GetAuthStatusRev2Async(authOperation.ReferenceNumber, authOperation.AuthenticationToken.Token, CancellationToken),
            result => result is not null && result.Status.Code != AuthInProgressCode,
            cancellationToken: CancellationToken);

        // Assert
        Assert.Equal(AuthSuccessCode, status.Status.Code);
        Assert.NotEqual(default, status.StartDate);
        Assert.NotNull(status.AuthenticationMethodInfo);
        Assert.False(string.IsNullOrWhiteSpace(status.AuthenticationMethodInfo.Code));
        Assert.False(string.IsNullOrWhiteSpace(status.AuthenticationMethodInfo.DisplayName));
        Assert.NotNull(status.AuthenticationMethodInfo.Category);
        Assert.Equal(AuthenticationMethodCategoryRev2Code.XadesSignature, status.AuthenticationMethodInfo.Category.Code);
        Assert.Equal(nameof(AuthenticationMethodCategoryRev2Code.XadesSignature), status.AuthenticationMethodInfo.Category.RawCode);
        Assert.False(string.IsNullOrWhiteSpace(status.AuthenticationMethodInfo.Category.DisplayName));
    }

    /// <summary>
    /// Pobiera listę aktywnych sesji w rewizji Rev2 i weryfikuje, że zawiera bieżącą sesję
    /// z kategorią metody uwierzytelnienia w postaci code/rawCode/displayName.
    /// </summary>
    [Fact]
    public async Task GetActiveSessionsRev2Async_AfterAuthentication_ReturnsCurrentSession()
    {
        // Arrange
        string nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse auth = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip);
        string accessToken = auth.AccessToken.Token;

        List<AuthenticationListItemRev2> all = [];
        string? continuationToken = null;

        // Act
        do
        {
            AuthenticationListResponseRev2 page = await ActiveSessionsClient.GetActiveSessionsRev2Async(accessToken, 10, continuationToken, CancellationToken);
            Assert.NotNull(page);
            if (page.Items is not null)
            {
                all.AddRange(page.Items);
            }
            continuationToken = page.ContinuationToken;
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        // Assert
        AuthenticationListItemRev2 current = Assert.Single(all, x => x.IsCurrent);
        Assert.False(string.IsNullOrWhiteSpace(current.ReferenceNumber));
        Assert.Equal(AuthSuccessCode, current.Status.Code);
        Assert.True(current.IsTokenRedeemed);
        Assert.NotNull(current.RefreshTokenValidUntil);
        Assert.NotNull(current.AuthenticationMethodInfo?.Category);
        Assert.Equal(AuthenticationMethodCategoryRev2Code.XadesSignature, current.AuthenticationMethodInfo.Category.Code);
        Assert.False(string.IsNullOrWhiteSpace(current.AuthenticationMethodInfo.Category.RawCode));
    }
}
