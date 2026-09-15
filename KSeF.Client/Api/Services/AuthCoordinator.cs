using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using System.Text;

namespace KSeF.Client.Api.Services;

/// <inheritdoc />
public class AuthCoordinator(
    IAuthorizationClient authorizationClient) : IAuthCoordinator
{

    /// <inheritdoc />
    public async Task<AuthenticationOperationStatusResponse> AuthKsefTokenAsync(
        AuthenticationTokenContextIdentifierType contextIdentifierType,
        string contextIdentifierValue,
        string tokenKsef,
        ICryptographyService cryptographyService,
        EncryptionMethodEnum encryptionMethod = EncryptionMethodEnum.Rsa,
        AuthenticationTokenAuthorizationPolicy authorizationPolicy = default,
        CancellationToken cancellationToken = default)
    {
        // 1) Pobranie challenge i timestamp
        AuthenticationChallengeResponse challengeResponse = await authorizationClient
            .GetAuthChallengeAsync(cancellationToken).ConfigureAwait(false);

        string challenge = challengeResponse.Challenge;

        long timestampMs = challengeResponse.Timestamp.ToUnixTimeMilliseconds();

        // 2) Tworzenie ciągu token|timestamp
        string tokenWithTimestamp = $"{tokenKsef}|{timestampMs}";
        byte[] tokenBytes = Encoding.UTF8.GetBytes(tokenWithTimestamp);

        // 3) Szyfrowanie RSA-OAEP SHA-256
        byte[] tokenEncryptedBytes = encryptionMethod switch
        {
            EncryptionMethodEnum.Rsa => cryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes),
            EncryptionMethodEnum.ECDsa => cryptographyService.EncryptWithECDSAUsingPublicKey(tokenBytes),
            _ => throw new ArgumentOutOfRangeException(nameof(encryptionMethod))
        };

        string encryptedToken = Convert.ToBase64String(tokenEncryptedBytes);

        // 4) Budowa żądania
        IAuthKsefTokenRequestBuilderWithEncryptedToken authKsefTokenRequest = AuthKsefTokenRequestBuilder
            .Create()
            .WithChallenge(challenge)
            .WithContext(contextIdentifierType, contextIdentifierValue)
            .WithEncryptedToken(encryptedToken);

        if (authorizationPolicy != null)
        {
            authKsefTokenRequest = authKsefTokenRequest.WithAuthorizationPolicy(authorizationPolicy);
        }

        if (cryptographyService.KsefTokenPublicKeyId != null)
        {
            authKsefTokenRequest = authKsefTokenRequest.WithPublicKeyId(cryptographyService.KsefTokenPublicKeyId);
        }

        // 5) Wysłanie do KSeF
        SignatureResponse submissionResponse = await authorizationClient
            .SubmitKsefTokenAuthRequestAsync(authKsefTokenRequest.Build(), cancellationToken).ConfigureAwait(false);

        // 6) Odpytanie o gotowość tokenu
        await WaitForAuthCompletionAsync(submissionResponse, cancellationToken).ConfigureAwait(false);

        // 7) Pobranie tokenu dostępowego
        AuthenticationOperationStatusResponse accessTokenResponse = await authorizationClient.GetAccessTokenAsync(submissionResponse.AuthenticationToken.Token, cancellationToken).ConfigureAwait(false);

        // 8) Zwróć token            
        return accessTokenResponse;
    }

    /// <inheritdoc />
    public async Task<AuthenticationOperationStatusResponse> AuthAsync(
        AuthenticationTokenContextIdentifierType contextIdentifierType,
        string contextIdentifierValue,
        AuthenticationTokenSubjectIdentifierTypeEnum identifierType,
        Func<string, Task<string>> xmlSigner,
        AuthenticationTokenAuthorizationPolicy authorizationPolicy = default,
        bool verifyCertificateChain = false,
        CancellationToken cancellationToken = default)
    {
        // 1) Challenge
        AuthenticationChallengeResponse challengeResponse = await authorizationClient
            .GetAuthChallengeAsync(cancellationToken).ConfigureAwait(false);

        string challenge = challengeResponse.Challenge;

        // 2) Budowa obiektu AuthKsefTokenRequest
        IAuthTokenRequestBuilderReady authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challenge)
            .WithContext(contextIdentifierType, contextIdentifierValue)
            .WithIdentifierType(identifierType);

        if (authorizationPolicy != null)
        {
            authTokenRequest = authTokenRequest
            .WithAuthorizationPolicy(authorizationPolicy);
        }

        AuthenticationTokenRequest authorizeRequest = authTokenRequest.Build();

        // 3) Serializacja do XML
        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authorizeRequest);

        // 4) wywołanie mechanizmu podpisującego XML
        string signedXml = await xmlSigner.Invoke(unsignedXml).ConfigureAwait(false);

        // 5)// Przesłanie podpisanego XML do systemu KSeF
        SignatureResponse authSubmission = await authorizationClient
            .SubmitXadesAuthRequestAsync(signedXml, false, cancellationToken: cancellationToken).ConfigureAwait(false);

        // 6) Odpytanie o gotowość tokenu
        await WaitForAuthCompletionAsync(authSubmission, cancellationToken).ConfigureAwait(false);

        AuthenticationOperationStatusResponse accessTokenResponse = await authorizationClient.GetAccessTokenAsync(authSubmission.AuthenticationToken.Token, cancellationToken).ConfigureAwait(false);

        // 7) Zwrócenie tokena           
        return accessTokenResponse;
    }

    /// <summary>
    /// Odpytuje status operacji uwierzytelnienia aż do uzyskania kodu 200
    /// (<see cref="AuthenticationStatusCodeResponse.AuthenticationSuccess"/>).
    /// Po przekroczeniu <paramref name="timeout"/> zgłasza <see cref="TimeoutException"/> z ostatnim znanym statusem.
    /// </summary>
    private async Task WaitForAuthCompletionAsync(
        SignatureResponse authOperationInfo,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromMinutes(1);
        TimeSpan pollInterval = TimeSpan.FromSeconds(2);
        DateTime deadline = DateTime.UtcNow + effectiveTimeout;

        AuthStatus authStatus;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            authStatus = await authorizationClient.GetAuthStatusAsync(
                authOperationInfo.ReferenceNumber,
                authOperationInfo.AuthenticationToken.Token,
                cancellationToken).ConfigureAwait(false);

            if (authStatus.Status.Code == AuthenticationStatusCodeResponse.AuthenticationSuccess)
            {
                return;
            }

            await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException(
            $"Uwierzytelnianie nie zakończyło się kodem 200 w ciągu {effectiveTimeout.TotalSeconds}s. " +
            $"Ostatni status: {authStatus.Status.Code}, " +
            $"Opis: {authStatus.Status.Description}, " +
            $"Szczegóły: {FormatDetails(authStatus)}");
    }

    /// <inheritdoc />
    public async Task<TokenInfo> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        RefreshTokenResponse response = await authorizationClient
            .RefreshAccessTokenAsync(refreshToken, cancellationToken).ConfigureAwait(false);

        return response.AccessToken;
    }

	private static string FormatDetails(AuthStatus authStatus) =>
	authStatus.Status.Details != null && authStatus.Status.Details.Count > 0
		? string.Join(", ", authStatus.Status.Details)
		: "brak szczegółów";
}