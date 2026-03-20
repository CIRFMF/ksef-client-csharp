#nullable enable
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.KsefToken;

[Collection("KsefTokenScenario")]
public class KsefTokenE2ETests : TestBase
{
    private const int SuccessfulAuthStatusCode = 200;
    private const int TokenActivationTimeoutSeconds = 60;

    private string AccessToken { get; }
    private string Nip { get; }

    public KsefTokenE2ETests()
    {
        Nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, Nip)
                                          .GetAwaiter()
                                          .GetResult();
        AccessToken = authInfo.AccessToken.Token;
    }

    /// <summary>
    /// Test E2E weryfikujący pełny cykl życia tokena KSeF:
    /// generowanie, oczekiwanie na aktywację, uwierzytelnienie tokenem, unieważnienie oraz weryfikacja unieważnienia.
    /// </summary>
    /// <remarks>
    /// Kroki:
    /// 1) Generuje token KSeF z uprawnieniami (InvoiceRead, InvoiceWrite).
    /// 2) Czeka aż status tokena zmieni się na Active.
    /// 3) Uwierzytelnia się do KSeF używając tokena (RSA-OAEP SHA-256 na ciągu "token|timestamp") i pobiera access/refresh token.
    /// 4) Unieważnia wygenerowany token.
    /// 5) Sprawdza czy token ma status Revoked.
    /// </remarks>
    [Fact]
    public async Task KsefTokensAsync_FullIntegrationFlow_AllStepsSucceed()
    {
        // 1) Wygeneruj token KSeF z uprawnieniami
        KsefTokenRequest createTokenRequest = new KsefTokenRequest
        {
            Permissions = [
                KsefTokenPermissionType.InvoiceRead,
                KsefTokenPermissionType.InvoiceWrite
            ],
            Description = "E2E token"
        };

        KsefTokenResponse tokenResponse = await KsefClient.GenerateKsefTokenAsync(
            createTokenRequest,
            AccessToken,
            CancellationToken
        );
        Assert.NotNull(tokenResponse);
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.ReferenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.Token));

        // 2) Poczekaj, aż token stanie się aktywny (polling)
        int activationAttempts = Math.Max(1, (TokenActivationTimeoutSeconds * 1000) / SleepTime);
        AuthenticationKsefToken activeToken = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetKsefTokenAsync(tokenResponse.ReferenceNumber, AccessToken, CancellationToken).ConfigureAwait(false),
            result => result is not null && result.Status == AuthenticationKsefTokenStatus.Active,
            maxAttempts: activationAttempts,
            cancellationToken: CancellationToken);

        Assert.NotNull(activeToken);
        Assert.Equal(AuthenticationKsefTokenStatus.Active, activeToken.Status);

        // 3) Uwierzytelnij w KSeF przy użyciu tokena KSeF (polling statusu 200 w AuthenticateWithKsefTokenAsync)
        AuthenticationOperationStatusResponse authResult = await AuthenticateWithKsefTokenAsync(tokenResponse.Token, Nip);
        Assert.NotNull(authResult);
        Assert.False(string.IsNullOrWhiteSpace(authResult.AccessToken?.Token));
        Assert.False(string.IsNullOrWhiteSpace(authResult.RefreshToken?.Token));

        // 4) Unieważnij token
        await KsefClient.RevokeKsefTokenAsync(
            tokenResponse.ReferenceNumber,
            AccessToken,
            CancellationToken);

        // 5) Zweryfikuj, że token został unieważniony (polling)
        AuthenticationKsefToken revokedToken = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetKsefTokenAsync(
                tokenResponse.ReferenceNumber,
                AccessToken,
                CancellationToken).ConfigureAwait(false),
            result => result is not null && result.Status == AuthenticationKsefTokenStatus.Revoked,
            maxAttempts: activationAttempts,
            cancellationToken: CancellationToken);

        Assert.NotNull(revokedToken);
        Assert.Equal(AuthenticationKsefTokenStatus.Revoked, revokedToken.Status);
    }

    [Fact]
    public async Task When_KsefTokensAreActive_ThenTryToRevokeOneWithAnother_ShouldThrowKsefApiException()
    {
        // 1) Przygotuj dwa tokeny
        KsefTokenRequest createTokenRequest = new KsefTokenRequest
        {
            Permissions = [
                KsefTokenPermissionType.InvoiceRead,
                KsefTokenPermissionType.InvoiceWrite
            ],
            Description = "E2E token"
        };

        KsefTokenResponse tokenResponse = await KsefClient.GenerateKsefTokenAsync(
            createTokenRequest,
            AccessToken,
            CancellationToken
        );
        Assert.NotNull(tokenResponse);
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.ReferenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.Token));

        KsefTokenResponse secondTokenResponse = await KsefClient.GenerateKsefTokenAsync(
            createTokenRequest,
            AccessToken,
            CancellationToken
        );

        Assert.NotNull(secondTokenResponse);
        Assert.False(string.IsNullOrWhiteSpace(secondTokenResponse.ReferenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(secondTokenResponse.Token));

        // 2) Poczekaj, aż token stanie się aktywny (polling)
        int activationAttempts = Math.Max(1, (TokenActivationTimeoutSeconds * 1000) / SleepTime);
        AuthenticationKsefToken activeToken = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetKsefTokenAsync(secondTokenResponse.ReferenceNumber, AccessToken, CancellationToken).ConfigureAwait(false),
            result => result is not null && result.Status == AuthenticationKsefTokenStatus.Active,
            maxAttempts: activationAttempts,
            cancellationToken: CancellationToken);

        Assert.NotNull(activeToken);
        Assert.Equal(AuthenticationKsefTokenStatus.Active, activeToken.Status);

        // 3) Uwierzytelnij w KSeF przy użyciu drugiego tokena KSeF (polling statusu 200 w AuthenticateWithKsefTokenAsync)
        AuthenticationOperationStatusResponse authResult = await AuthenticateWithKsefTokenAsync(secondTokenResponse.Token, Nip);
        Assert.NotNull(authResult);
        Assert.False(string.IsNullOrWhiteSpace(authResult.AccessToken?.Token));
        Assert.False(string.IsNullOrWhiteSpace(authResult.RefreshToken?.Token));

        // 4) Unieważnij token pierwszy tokenem drugim
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(async () => { await KsefClient.RevokeKsefTokenAsync(
            tokenResponse.ReferenceNumber,
            authResult.AccessToken?.Token,
            CancellationToken).ConfigureAwait(false); }).ConfigureAwait(false);

        // 5) Unieważnij tokeny
        await KsefClient.RevokeKsefTokenAsync(
            tokenResponse.ReferenceNumber,
            AccessToken,
            CancellationToken);

        await KsefClient.RevokeKsefTokenAsync(
            secondTokenResponse.ReferenceNumber,
            AccessToken,
            CancellationToken);

        // 6) Zweryfikuj, że tokeny zostały unieważnione (polling)
        AuthenticationKsefToken revokedToken = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetKsefTokenAsync(
                tokenResponse.ReferenceNumber,
                authResult.AccessToken?.Token,
                CancellationToken).ConfigureAwait(false),
            result => result is not null && result.Status == AuthenticationKsefTokenStatus.Revoked,
            maxAttempts: activationAttempts,
            cancellationToken: CancellationToken);

        Assert.NotNull(revokedToken);
        Assert.Equal(AuthenticationKsefTokenStatus.Revoked, revokedToken.Status);

        AuthenticationKsefToken revokedSecondToken = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetKsefTokenAsync(
                secondTokenResponse.ReferenceNumber,
                authResult.AccessToken?.Token,
                CancellationToken).ConfigureAwait(false),
            result => result is not null && result.Status == AuthenticationKsefTokenStatus.Revoked,
            maxAttempts: activationAttempts,
            cancellationToken: CancellationToken);

        Assert.NotNull(revokedSecondToken);
        Assert.Equal(AuthenticationKsefTokenStatus.Revoked, revokedSecondToken.Status);
    }

    /// <summary>
    /// Przeprowadza pełny proces uwierzytelnienia przy użyciu tokena KSeF.
    /// </summary>
    /// <remarks>
    /// Pobiera wyzwanie (challenge) wraz ze znacznikiem czasu, szyfruje ciąg "token|timestampMs" kluczem publicznym KSeF (RSA-OAEP SHA-256),
    /// wysyła żądanie uwierzytelnienia, a następnie polluje status (do 200) i pobiera access/refresh token.
    /// </remarks>
    /// <param name="ksefToken">Wygenerowany token KSeF w postaci jawnej (string).</param>
    /// <param name="nip">Identyfikator kontekstu (NIP) uwierzytelnienia.</param>
    /// <returns><see cref="AuthenticationOperationStatusResponse"/> zawierająca access oraz refresh token.</returns>
    private async Task<AuthenticationOperationStatusResponse> AuthenticateWithKsefTokenAsync(string ksefToken, string nip)
    {
        // 1) Pobierz challenge i timestamp
        AuthenticationChallengeResponse challenge = await AuthorizationClient.GetAuthChallengeAsync(CancellationToken).ConfigureAwait(false);
        long timestampMs = challenge.Timestamp.ToUnixTimeMilliseconds();

        // 2) Przygotuj "token|timestamp" i zaszyfruj RSA-OAEP SHA-256 zgodnie z wymaganiem API
        string tokenWithTimestamp = $"{ksefToken}|{timestampMs}";
        byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenWithTimestamp);
        byte[] encrypted = CryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes);
        string encryptedTokenB64 = Convert.ToBase64String(encrypted);

        // 3) Wyślij żądanie uwierzytelnienia tokenem KSeF
        AuthenticationKsefTokenRequest request = new()
        {
            Challenge = challenge.Challenge,
            ContextIdentifier = new AuthenticationTokenContextIdentifier
            {
                Type = AuthenticationTokenContextIdentifierType.Nip,
                Value = nip
            },
            EncryptedToken = encryptedTokenB64,
            AuthorizationPolicy = null
        };

        SignatureResponse signature = await AuthorizationClient.SubmitKsefTokenAuthRequestAsync(request, CancellationToken).ConfigureAwait(false);
        Assert.False(string.IsNullOrWhiteSpace(signature.ReferenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(signature.AuthenticationToken?.Token));

        // 4) Poll statusu aż do 200 lub timeoutu (2 minuty)
        TimeSpan pollTimeout = TimeSpan.FromMinutes(2);
        int statusAttempts = Math.Max(1, (int)(pollTimeout.TotalMilliseconds / SleepTime));

        AuthStatus status = await AsyncPollingUtils.PollAsync(
            async () => await AuthorizationClient.GetAuthStatusAsync(signature.ReferenceNumber, signature.AuthenticationToken.Token, CancellationToken).ConfigureAwait(false),
            result => result is not null && result.Status?.Code == SuccessfulAuthStatusCode,
            maxAttempts: statusAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        Assert.Equal(SuccessfulAuthStatusCode, status.Status.Code);
        Assert.NotNull(status.IsTokenRedeemed);
        Assert.Null(status.RefreshTokenValidUntil);
        Assert.Null(status.LastTokenRefreshDate);

        // 5) Pobierz access/refresh tokeny
        AuthenticationOperationStatusResponse tokens = await AuthorizationClient.GetAccessTokenAsync(signature.AuthenticationToken.Token, CancellationToken).ConfigureAwait(false);
        return tokens;
    }

    /// <summary>
    /// Test E2E weryfikujący pobieranie i filtrowanie wygenerowanych tokenów KSeF:
    /// generowanie, oczekiwanie na aktywację, wyszukanie wygenerowanych tokenów, unieważnienie oraz weryfikacja unieważnienia.
    /// </summary>
    /// <remarks>
    /// Kroki:
    /// 1) Generuje 5 tokenów KSeF z uprawnieniami (InvoiceRead, InvoiceWrite).
    /// 2) Czeka aż status tokenów zmieni się na Active.
    /// 3) Wyszukuje wygenerowane tokeny:
    /// 3a) Wyszukuje wszystkie wygenerowane tokeny
    /// 3b) Wyszukuje wybrany token
    /// 4) Unieważnia wygenerowane tokeny.
    /// 5) Sprawdza czy tokeny mają status Revoked.
    /// </remarks>
    [Fact]
    public async Task KsefTokensAsync_GenerateAndFilter_Positive()
    {
        // 1) Wygeneruj tokeny KSeF z uprawnieniami
        List<(KsefTokenResponse, AuthenticationKsefToken)> createdTokens = new();
        const int activationAttempts = 30;
        for (int i = 0; i < 5; i++)
        {
            KsefTokenRequest createTokenRequest = new KsefTokenRequest
            {
                Permissions = [
                    KsefTokenPermissionType.InvoiceRead,
                KsefTokenPermissionType.InvoiceWrite
                ],
                Description = $"E2E token - {i}"
            };

            KsefTokenResponse tokenResponse = await KsefClient.GenerateKsefTokenAsync(
                createTokenRequest,
                AccessToken,
                CancellationToken
            );
            Assert.NotNull(tokenResponse);
            Assert.False(string.IsNullOrWhiteSpace(tokenResponse.ReferenceNumber));
            Assert.False(string.IsNullOrWhiteSpace(tokenResponse.Token));

            // 2) Poczekaj, aż token stanie się aktywny
            AuthenticationKsefToken activeToken = await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.GetKsefTokenAsync(tokenResponse.ReferenceNumber, AccessToken, CancellationToken).ConfigureAwait(false),
                result => result is not null && result.Status == AuthenticationKsefTokenStatus.Active,
                maxAttempts: activationAttempts,
                cancellationToken: CancellationToken);

            Assert.NotNull(activeToken);
            Assert.Equal(AuthenticationKsefTokenStatus.Active, activeToken.Status);

            createdTokens.Add((tokenResponse, activeToken));
        }

        // 3) Wyszukaj wygenerowane tokeny

        QueryKsefTokensResponse queryAllResult = await KsefClient.QueryKsefTokensAsync(AccessToken);

        Assert.NotNull(queryAllResult);
        Assert.True(queryAllResult.Tokens.Count >= createdTokens.Count);

        // 3a) Wyszukaj wszystkie wygenerowane tokeny

        // 3b) Wyszukaj wybrany token
        QueryKsefTokensResponse singleResult = await KsefClient.QueryKsefTokensAsync(AccessToken,
            description: "E2E token - 2");
        Assert.NotNull(singleResult);
        Assert.True(singleResult.Tokens.Count == 1);
        Assert.True(singleResult.Tokens.First().Description == "E2E token - 2");

        // 4) Unieważnij token
        foreach ((KsefTokenResponse? tokenResponse, AuthenticationKsefToken _) in createdTokens)
        {
            await KsefClient.RevokeKsefTokenAsync(
            tokenResponse.ReferenceNumber,
            AccessToken,
            CancellationToken);

            // 5) Zweryfikuj, że token został unieważniony
            AuthenticationKsefToken revokedToken = await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.GetKsefTokenAsync(
                    tokenResponse.ReferenceNumber,
                    AccessToken,
                    CancellationToken).ConfigureAwait(false),
                result => result is not null && result.Status == AuthenticationKsefTokenStatus.Revoked,
                maxAttempts: activationAttempts,
                cancellationToken: CancellationToken);

            Assert.NotNull(revokedToken);
            Assert.Equal(AuthenticationKsefTokenStatus.Revoked, revokedToken.Status);
        }
    }
}