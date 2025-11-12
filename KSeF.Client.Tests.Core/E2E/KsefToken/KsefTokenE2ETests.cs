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
        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, Nip)
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
    /// 5) Sprawdza, że token ma status Revoked.
    /// </remarks>
    [Fact]
    public async Task KsefTokensAsync_FullIntegrationFlow_AllStepsSucceed()
    {
        // 1) Wygeneruj token KSeF z uprawnieniami
        KsefTokenResponse tokenResponse = await GenerateKsefTokenAsync("E2E token");
        Assert.NotNull(tokenResponse);
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.ReferenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.Token));

        // 2) Poczekaj, aż token stanie się aktywny (polling)
        int activationAttempts = Math.Max(1, (TokenActivationTimeoutSeconds * 1000) / SleepTime);
        AuthenticationKsefToken activeToken = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetKsefTokenAsync(tokenResponse.ReferenceNumber, AccessToken, CancellationToken),
            result => result is not null && result.Status == AuthenticationKsefTokenStatus.Active,
            delay: TimeSpan.FromMilliseconds(SleepTime),
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
        await RevokeKsefTokenAsync(tokenResponse.ReferenceNumber);

        // 5) Zweryfikuj, że token został unieważniony (polling)
        AuthenticationKsefToken revokedToken = await AsyncPollingUtils.PollAsync(
            async () => await GetKsefTokenByReferenceAsync(tokenResponse.ReferenceNumber),
            result => result is not null && result.Status == AuthenticationKsefTokenStatus.Revoked,
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: activationAttempts,
            cancellationToken: CancellationToken);

        Assert.NotNull(revokedToken);
        Assert.Equal(AuthenticationKsefTokenStatus.Revoked, revokedToken.Status);
    }

    /// <summary>
    /// Generuje token KSeF z podanym opisem oraz zestawem uprawnień (odczyt i zapis faktur).
    /// </summary>
    /// <param name="description">Opis tokena widoczny w KSeF.</param>
    /// <returns><see cref="KsefTokenResponse"/> zawierająca numer referencyjny oraz wygenerowany token.</returns>
    private async Task<KsefTokenResponse> GenerateKsefTokenAsync(string description)
    {
        KsefTokenRequest request = new KsefTokenRequest
        {
            Permissions =
            [
                KsefTokenPermissionType.InvoiceRead,
                KsefTokenPermissionType.InvoiceWrite
            ],
            Description = description
        };

        return await KsefClient.GenerateKsefTokenAsync(request, AccessToken, CancellationToken);
    }

    /// <summary>
    /// Przeprowadza pełny proces uwierzytelnienia przy użyciu tokena KSeF.
    /// </summary>
    /// <remarks>
    /// Pobiera wyzwanie (challenge) wraz ze znacznikiem czasu, szyfruje ciąg "token|timestampMs" kluczem publicznym KSeF (RSA-OAEP SHA-256),
    /// wysyła żądanie uwierzytelnienia, a następnie polluje status (do 200) i pobiera access/refresh token.
    /// </remarks>
    /// <param name="ksefToken">Wygenerowany token KSeF w postaci jawnej (string).</param>
    /// <param name="nip">Identyfikator kontekstu (NIP) dla uwierzytelnienia.</param>
    /// <returns><see cref="AuthenticationOperationStatusResponse"/> zawierająca access oraz refresh token.</returns>
    private async Task<AuthenticationOperationStatusResponse> AuthenticateWithKsefTokenAsync(string ksefToken, string nip)
    {
        // 1) Pobierz challenge i timestamp
        AuthenticationChallengeResponse challenge = await AuthorizationClient.GetAuthChallengeAsync(CancellationToken);
        long timestampMs = challenge.Timestamp.ToUnixTimeMilliseconds();

        // 2) Przygotuj "token|timestamp" i zaszyfruj RSA-OAEP SHA-256 zgodnie z wymaganiem API
        string tokenWithTimestamp = $"{ksefToken}|{timestampMs}";
        byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenWithTimestamp);
        byte[] encrypted = CryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes);
        string encryptedTokenB64 = Convert.ToBase64String(encrypted);

        // 3) Wyślij żądanie uwierzytelnienia tokenem KSeF
        AuthenticationKsefTokenRequest request = new AuthenticationKsefTokenRequest
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

        SignatureResponse signature = await AuthorizationClient.SubmitKsefTokenAuthRequestAsync(request, CancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(signature.ReferenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(signature.AuthenticationToken?.Token));

        // 4) Poll statusu aż do 200 lub timeoutu (2 minuty)
        TimeSpan pollTimeout = TimeSpan.FromMinutes(2);
        int statusAttempts = Math.Max(1, (int)(pollTimeout.TotalMilliseconds / SleepTime));

        AuthStatus status = await AsyncPollingUtils.PollAsync(
            async () => await AuthorizationClient.GetAuthStatusAsync(signature.ReferenceNumber, signature.AuthenticationToken.Token, CancellationToken),
            result => result is not null && result.Status?.Code == SuccessfulAuthStatusCode,
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: statusAttempts,
            cancellationToken: CancellationToken);

        Assert.Equal(SuccessfulAuthStatusCode, status.Status.Code);

        // 5) Pobierz access/refresh tokeny
        AuthenticationOperationStatusResponse tokens = await AuthorizationClient.GetAccessTokenAsync(signature.AuthenticationToken.Token, CancellationToken);
        return tokens;
    }

    /// <summary>
    /// Pobiera informacje o tokenie KSeF na podstawie numeru referencyjnego.
    /// </summary>
    /// <param name="referenceNumber">Numer referencyjny tokena.</param>
    /// <returns>Obiekt <see cref="AuthenticationKsefToken"/> z aktualnym statusem i metadanymi.</returns>
    private async Task<AuthenticationKsefToken> GetKsefTokenByReferenceAsync(string referenceNumber)
        => await KsefClient.GetKsefTokenAsync(referenceNumber, AccessToken, CancellationToken);

    /// <summary>
    /// Unieważnia token KSeF o podanym numerze referencyjnym.
    /// </summary>
    /// <param name="referenceNumber">Numer referencyjny tokena do unieważnienia.</param>
    private async Task RevokeKsefTokenAsync(string referenceNumber)
        => await KsefClient.RevokeKsefTokenAsync(referenceNumber, AccessToken, CancellationToken);
}