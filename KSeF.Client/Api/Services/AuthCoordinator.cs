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
public class AuthCoordinator : IAuthCoordinator
{
    private readonly IAuthorizationClient _authorizationClient;

    public AuthCoordinator(
        IAuthorizationClient authorizationClient
        )
    {
        _authorizationClient = authorizationClient;
    }

    /// <inheritdoc />
    public async Task<AuthenticationOperationStatusResponse> AuthKsefTokenAsync(
        AuthenticationTokenContextIdentifierType contextIdentifierType,
        string contextIdentifierValue,
        string tokenKsef,
        ICryptographyService cryptographyService,
        EncryptionMethodEnum encryptionMethod = EncryptionMethodEnum.ECDsa,
        AuthenticationTokenAuthorizationPolicy ipAddressPolicy = default,
        CancellationToken cancellationToken = default)
    {
        // 1) Pobranie challenge i timestamp
        AuthenticationChallengeResponse challengeResponse = await _authorizationClient
            .GetAuthChallengeAsync(cancellationToken);

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
        IAuthKsefTokenRequestBuilderWithEncryptedToken requestBuilder = AuthKsefTokenRequestBuilder
            .Create()
            .WithChallenge(challenge)
            .WithContext(contextIdentifierType, contextIdentifierValue)
            .WithEncryptedToken(encryptedToken);

        if (ipAddressPolicy != null)
        {
            requestBuilder = requestBuilder.WithAuthorizationPolicy(ipAddressPolicy);
        }

        AuthenticationKsefTokenRequest authKsefTokenRequest = requestBuilder.Build();

        // 5) Wysłanie do KSeF
        SignatureResponse submissionResponse = await _authorizationClient
            .SubmitKsefTokenAuthRequestAsync(authKsefTokenRequest, cancellationToken);

        // 6) Odpytanie o gotowość tokenu
        await WaitForAuthCompletionAsync(submissionResponse, cancellationToken);

        AuthenticationOperationStatusResponse accessTokenResponse = await _authorizationClient.GetAccessTokenAsync(submissionResponse.AuthenticationToken.Token, cancellationToken);

        // 7) Zwróć token            
        return accessTokenResponse;
    }

    /// <inheritdoc />
    public async Task<AuthenticationOperationStatusResponse> AuthAsync(
        AuthenticationTokenContextIdentifierType contextIdentifierType,
        string contextIdentifierValue,
        AuthenticationTokenSubjectIdentifierTypeEnum identifierType,
        Func<string, Task<string>> xmlSigner,
        AuthenticationTokenAuthorizationPolicy ipAddressPolicy = default,
        CancellationToken cancellationToken = default,
        bool verifyCertificateChain = false)
    {
        // 1) Challenge
        AuthenticationChallengeResponse challengeResponse = await _authorizationClient
            .GetAuthChallengeAsync(cancellationToken);

        string challenge = challengeResponse.Challenge;

        // 2) Budowa obiektu AuthKsefTokenRequest
        IAuthTokenRequestBuilderReady authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challenge)
            .WithContext(contextIdentifierType, contextIdentifierValue)
            .WithIdentifierType(identifierType);

        if (ipAddressPolicy != null)
        {
            authTokenRequest = authTokenRequest
            .WithAuthorizationPolicy(ipAddressPolicy);
        }

        AuthenticationTokenRequest authorizeRequest = authTokenRequest.Build();

        // 3) Serializacja do XML
        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authorizeRequest);

        // 4) Wywołanie mechanizmu podpisującego XML
        string signedXml = await xmlSigner.Invoke(unsignedXml);

        // 5) Przesłanie podpisanego XML do systemu KSeF
        SignatureResponse authSubmission = await _authorizationClient
            .SubmitXadesAuthRequestAsync(signedXml, false, cancellationToken);

        // 6) Odpytanie o gotowość tokenu
        await WaitForAuthCompletionAsync(authSubmission, cancellationToken);

        AuthenticationOperationStatusResponse accessTokenResponse = await _authorizationClient.GetAccessTokenAsync(authSubmission.AuthenticationToken.Token, cancellationToken);

        // 7) Zwrócenie tokena           
        return accessTokenResponse;
    }

    /// <summary>
    /// Oczekuje na zakończenie operacji uwierzytelnienia, sprawdzając status co sekundę.
    /// </summary>
    private async Task WaitForAuthCompletionAsync(
        SignatureResponse authOperationInfo,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromMinutes(2);
        DateTime startTime = DateTime.UtcNow;
        AuthStatus authStatus;

        do
        {
            authStatus = await _authorizationClient.GetAuthStatusAsync(
                authOperationInfo.ReferenceNumber,
                authOperationInfo.AuthenticationToken.Token,
                cancellationToken);

            // (4xx) - błąd po stronie danych/żądania
            if (authStatus.Status.Code >= AuthenticationStatusCodeResponse.BadRequest && authStatus.Status.Code < AuthenticationStatusCodeResponse.UnknownError)
            {
                string details = authStatus.Status.Details != null && authStatus.Status.Details.Any()
                    ? string.Join(", ", authStatus.Status.Details)
                    : "brak szczegółów";

                throw new Exception(
                    $"Błąd autoryzacji KSeF. " +
                    $"Status: {authStatus.Status.Code}, " +
                    $"Opis: {authStatus.Status.Description}, " +
                    $"Szczegóły: {details}");
            }

            // Sukces - wyjście z pętli
            if (authStatus.Status.Code == AuthenticationStatusCodeResponse.AuthenticationSuccess)
            {
                return;
            }

            // Status 100 (Processing) lub inne - czekamy przed kolejną próbą
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        while (authStatus.Status.Code != AuthenticationStatusCodeResponse.AuthenticationSuccess
            && !cancellationToken.IsCancellationRequested
            && (DateTime.UtcNow - startTime) < effectiveTimeout);

        // Timeout lub nieoczekiwany status
        if (authStatus.Status.Code != AuthenticationStatusCodeResponse.AuthenticationSuccess)
        {
            string details = authStatus.Status.Details != null && authStatus.Status.Details.Any()
                ? string.Join(", ", authStatus.Status.Details)
                : "brak szczegółów";

            throw new TimeoutException(
                $"Timeout uwierzytelniania: Brak tokena po {effectiveTimeout.TotalSeconds}s. " +
                $"Status: {authStatus.Status.Code}, " +
                $"Opis: {authStatus.Status.Description}, " +
                $"Szczegóły: {details}");
        }
    }

    /// <inheritdoc />
    public Task<TokenInfo> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => _authorizationClient.RefreshAccessTokenAsync(refreshToken, cancellationToken)
                         .ContinueWith(t => t.Result.AccessToken, cancellationToken);
}