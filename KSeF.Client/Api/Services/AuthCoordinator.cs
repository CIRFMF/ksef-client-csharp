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
        EncryptionMethodEnum encryptionMethod = EncryptionMethodEnum.ECDsa,
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
            authStatus = await authorizationClient.GetAuthStatusAsync(
                authOperationInfo.ReferenceNumber,
                authOperationInfo.AuthenticationToken.Token,
                cancellationToken).ConfigureAwait(false);

            // (4xx) - błąd po stronie danych/żądania
            if (authStatus.Status.Code >= AuthenticationStatusCodeResponse.BadRequest && authStatus.Status.Code < AuthenticationStatusCodeResponse.UnknownError)
            {
                string details = authStatus.Status.Details != null && authStatus.Status.Details?.Count > 0
                    ? string.Join(", ", authStatus.Status.Details)
                    : "brak szczegółów";

                throw new InvalidOperationException(
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
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }
        while (authStatus.Status.Code != AuthenticationStatusCodeResponse.AuthenticationSuccess
            && !cancellationToken.IsCancellationRequested
            && (DateTime.UtcNow - startTime) < effectiveTimeout);

        // Timeout lub nieoczekiwany status
        if (authStatus.Status.Code != AuthenticationStatusCodeResponse.AuthenticationSuccess)
        {
            string details = authStatus.Status.Details != null && authStatus.Status.Details.Count > 0
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
        => authorizationClient.RefreshAccessTokenAsync(refreshToken, cancellationToken)
                         .ContinueWith(t => t.Result.AccessToken, cancellationToken);
}