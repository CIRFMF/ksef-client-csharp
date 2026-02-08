#nullable enable
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using KSeF.Client.Tests.Utils;


namespace KSeF.Client.Tests.Core.E2E.Authorization.Sessions;

public class SessionE2ETests : TestBase
{
    private readonly string accessToken;
    private readonly string refreshToken;
    private readonly AuthenticationMethodEnum authenticationMethod;
    private readonly string nip;

    private const string ExpectedErrorMessage = "21301: Brak autoryzacji. - Status uwierzytelnienia (425) nie pozwala na odświeżenie tokenu dostępowego.";
    public SessionE2ETests()
    {
        nip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse auth = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, nip)
            .GetAwaiter()
            .GetResult();

        authenticationMethod = AuthenticationMethodEnum.QualifiedSignature;
        accessToken = auth.AccessToken.Token;
        refreshToken = auth.RefreshToken.Token;
    }

    /// <summary>
    /// Pobiera listę aktywnych sesji i weryfikuje, że istnieje co najmniej jedna,
    /// a wśród nich bieżąca (IsCurrent = true).
    /// </summary>
    [Fact]
    public async Task GetActiveSessionsAsyncListActiveSessionsReturnsAtLeastOneWithCurrent()
    {
        // Arrange
        const int pageSize = 20;
        string continuationToken = string.Empty;
        List<AuthenticationListItem> all = [];

        // Act
        do
        {
            AuthenticationListResponse page = await ActiveSessionsClient.GetActiveSessions(accessToken, pageSize, continuationToken, CancellationToken.None);
            continuationToken = page.ContinuationToken;
            if (page.Items != null)
            {
                all.AddRange(page.Items);
            }
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        // Assert
        Assert.NotNull(all);
        Assert.NotEmpty(all);
        Assert.Contains(all, x => x.IsCurrent);

        foreach (AuthenticationListItem item in all)
        {
            Assert.NotNull(item);
            Assert.True(!string.IsNullOrWhiteSpace(item.ReferenceNumber));
            Assert.NotNull(item.Status.Code);
            Assert.NotNull(item.StartDate);
            Assert.Equal(item.AuthenticationMethod, authenticationMethod);
            Assert.True(DateTime.UtcNow.AddMinutes(-1) < item.StartDate.DateTime && item.StartDate.DateTime < DateTime.UtcNow.AddMinutes(1));
            Assert.True(item.IsTokenRedeemed);
            Assert.True(item.IsCurrent);
            Assert.NotNull(item.RefreshTokenValidUntil);
            Assert.Null(item.LastTokenRefreshDate);
        }
    }

    /// <summary>
    /// Unieważnia bieżącą sesję (po refresh tokenie) i weryfikuje, że odświeżenie tokenu
    /// kończy się błędem 21304: Brak uwierzytelnienia. - Nieprawidłowy token.
    /// </summary>
    [Theory]
    [InlineData($"{nameof(accessToken)}")]
    [InlineData($"{nameof(refreshToken)}")]
    public async Task RevokeCurrentSessionAsyncRevokeByRefreshTokenRefreshFailsWth21304Code(string tokenType)
    {
        // Arrange
        string tokenToRevoke = tokenType == nameof(accessToken) ? accessToken : refreshToken;
        
        // Act
        await ActiveSessionsClient.RevokeCurrentSessionAsync(tokenToRevoke, CancellationToken.None);

        // Assert
        KsefApiException exception = await Assert.ThrowsAsync<KsefApiException>(() =>
            AuthorizationClient.RefreshAccessTokenAsync(refreshToken, CancellationToken.None));
        Assert.Equal(ExpectedErrorMessage, exception.Message);
    }

    /// <summary>
    /// Tworzy drugą sesję w tym samym kontekście, unieważnia ją po numerze referencyjnym
    /// i sprawdza, że jej refresh token nie działa kończąc błędem 21304: Brak uwierzytelnienia. - Nieprawidłowy token.
    /// </summary>
    [Fact]
    public async Task RevokeSessionAsyncRevokeByReferenceNumberTargetRefreshFailsWith401()
    {
        // Arrange
        AuthenticationOperationStatusResponse secondAuth = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip);
        string secondAccessToken = secondAuth.AccessToken.Token;
        string secondRefreshToken = secondAuth.RefreshToken.Token;

        AuthenticationListResponse list = await ActiveSessionsClient.GetActiveSessions(secondAccessToken, 20, null, CancellationToken.None);
        string? secondSessionRef = list.Items?.FirstOrDefault(i => i.IsCurrent)?.ReferenceNumber;

        Assert.False(string.IsNullOrWhiteSpace(secondSessionRef));

        // Act
        await ActiveSessionsClient.RevokeSessionAsync(secondSessionRef!, accessToken, CancellationToken.None);

        // Assert
        KsefApiException exception = await Assert.ThrowsAsync<KsefApiException>(() =>
            AuthorizationClient.RefreshAccessTokenAsync(secondRefreshToken, CancellationToken.None));
        Assert.Equal(ExpectedErrorMessage, exception.Message);
    }

    /// <summary>
    /// Odświeża token dostępu przy użyciu refresh tokenu i weryfikuje,
    /// że otrzymany access token różni się od początkowego.
    /// </summary>
    [Fact]
    public async Task RefreshAccessTokenAsyncWithRefreshTokenReturnsNewAccessToken()
    {
        // Arrange
        AuthenticationOperationStatusResponse authenticationResponse = await AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient);

        string initialAccessToken = authenticationResponse.AccessToken.Token;
        string initialRefreshToken = authenticationResponse.RefreshToken.Token;

        // Act
        RefreshTokenResponse refreshResult = await AuthorizationClient.RefreshAccessTokenAsync(initialRefreshToken, CancellationToken.None);

        // Assert
        Assert.NotNull(refreshResult);
        Assert.NotNull(refreshResult.AccessToken);
        Assert.NotEqual(initialAccessToken, refreshResult.AccessToken.Token);
    }
}