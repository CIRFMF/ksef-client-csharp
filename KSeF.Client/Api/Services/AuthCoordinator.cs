using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeFClient.Api.Builders.Auth;
using KSeFClient.Core.Interfaces;
using KSeFClient.Core.Models;
using System.Text;

namespace KSeFClient.Api.Services;

/// <summary>
/// Orkiestruje pełny, jednolity proces uwierzytelniania:
/// 1) Challenge
/// 2) Budowa i serializacja XML
/// 3) Podpisanie XAdES-BES
/// 4) Wysłanie podpisanego XML
/// 5) Polling po token
/// </summary>
public class AuthCoordinator : IAuthCoordinator
{
    private readonly IKSeFClient _ksefClient;

    public AuthCoordinator(
        IKSeFClient ksefClient
        )
    {
        _ksefClient = ksefClient;
    }

    /// <summary>
    /// Wykonuje cały flow z KSeF tokenem i zwraca finalny JWT accessToken.
    /// </summary>
    public async Task<AuthOperationStatusResponse> AuthKsefTokenAsync(
        ContextIdentifierType contextIdentifierType,
        string contextIdentifierValue,
        string tokenKsef,
        ICryptographyService cryptographyService,
        IpAddressPolicy ipAddressPolicy = default,
        CancellationToken cancellationToken = default)
    {
        // 1) Pobranie challenge i timestamp


        var challengeResponse = await _ksefClient
            .GetAuthChallengeAsync(cancellationToken);

        var challenge = challengeResponse.Challenge;
        var timestamp = challengeResponse.Timestamp;

        var timestampMs = challengeResponse.Timestamp.ToUnixTimeMilliseconds();

        // 2) Tworzenie ciągu token|timestamp
        var tokenWithTimestamp = $"{tokenKsef}|{timestampMs}";
        var tokenBytes = Encoding.UTF8.GetBytes(tokenWithTimestamp);

        // 3) Szyfrowanie RSA-OAEP SHA-256
        var encryptedBytes = cryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(
            tokenBytes);

        var encryptedToken = Convert.ToBase64String(encryptedBytes);

        // 4) Budowa requesta
        var builder = AuthKsefTokenRequestBuilder
            .Create()
            .WithChallenge(challenge)
            .WithContext(contextIdentifierType, contextIdentifierValue)
            .WithEncryptedToken(encryptedToken);

        if (ipAddressPolicy != null)
            builder = builder.WithIpAddressPolicy(ipAddressPolicy);

        var authKsefTokenRequest = builder.Build();

        // 5) Wysłanie do KSeF
        var submissionRef = await _ksefClient
            .SubmitKsefTokenAuthRequestAsync(authKsefTokenRequest, cancellationToken);

        // 6) Polling po dostęp do tokena
        AuthStatus authStatus;
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(2);

        do
        {
            authStatus = await _ksefClient.GetAuthStatusAsync(submissionRef.ReferenceNumber, submissionRef.AuthenticationToken.Token, cancellationToken);

            Console.WriteLine(
                $"Polling: StatusCode={authStatus.Status.Code}, " +
                $"Description='{authStatus.Status.Description}', " +
                $"Elapsed={DateTime.UtcNow - startTime:mm\\:ss}");

            if (authStatus.Status.Code != 200 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        while (authStatus.Status.Code == 100
            && !cancellationToken.IsCancellationRequested
            && (DateTime.UtcNow - startTime) < timeout);

        if (authStatus.Status.Code != 200)
        {
            Console.WriteLine("Timeout: Brak tokena po 2 minutach.");
            throw new Exception("Timeout Uwierzytelniania: Brak tokena po 2 minutach.");
        }
        var accessTokenResponse = await _ksefClient.GetAccessTokenAsync(submissionRef.AuthenticationToken.Token, cancellationToken);

        // 7) Zwróć token            
        return accessTokenResponse;
    }


    /// <summary>
    /// Wykonuje cały flow i zwraca finalny JWT accessToken.
    /// </summary>
    public async Task<AuthOperationStatusResponse> AuthAsync(
        ContextIdentifierType contextIdentifierType,
        string contextIdentifierValue,
        SubjectIdentifierTypeEnum identifierType,
        Func<string, Task<string>> xmlSigner,
        IpAddressPolicy ipAddressPolicy = default,
        CancellationToken cancellationToken = default,
        bool verifyCertificateChain = false)
    {
        // 1) Challenge


        var challengeResponse = await _ksefClient
            .GetAuthChallengeAsync(cancellationToken);

        var challenge = challengeResponse.Challenge;

        // 2) Budowa obiektu AuthKsefTokenRequest
        var authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challenge)
            .WithContext(contextIdentifierType, contextIdentifierValue)
            .WithIdentifierType(identifierType);

        ipAddressPolicy = new IpAddressPolicy
        {
            OnClientIpChange = IpChangePolicy.Reject, // Odrzuć, jeśli IP się zmieni
            AllowedIps = new AllowedIps
            {
                IpAddress = ["192.168.0.1, 192.222.111"],
                IpMask = ["192.168.1.0/24"], // Przykładowa maska
                IpRange = ["222.1111.0-222.1111.255"] // Przykładowy zakres IP
            }
        };

        if (ipAddressPolicy != null)
        {
            authTokenRequest = authTokenRequest
            .WithIpAddressPolicy(ipAddressPolicy);      // optional                
        }

        AuthTokenRequest authorizeRequest = authTokenRequest.Build();

        // 3) Serializacja do XML
        var unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authorizeRequest);

        // 4) wywołanie mechanizmu podpisującego XML
        var signedXml = await xmlSigner.Invoke(unsignedXml);

        // 5)// Przesłanie podpisanego XML do systemu KSeF
        var authSubmission = await _ksefClient
            .SubmitXadesAuthRequestAsync(signedXml, false, cancellationToken);

        // 6) Pollowanie aż status.code == 0
        AuthStatus authStatus;
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(2);

        do
        {
            authStatus = await _ksefClient.GetAuthStatusAsync(authSubmission.ReferenceNumber, authSubmission.AuthenticationToken.Token, cancellationToken);

            Console.WriteLine(
                $"Polling: StatusCode={authStatus.Status.Code}, " +
                $"Description='{authStatus.Status.Description}', " +
                $"Elapsed={DateTime.UtcNow - startTime:mm\\:ss}");

            if (authStatus.Status.Code != 200 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        while (authStatus.Status.Code != 200
            && !cancellationToken.IsCancellationRequested
            && (DateTime.UtcNow - startTime) < timeout);

        if (authStatus.Status.Code != 200)
        {
            Console.WriteLine("Timeout: Brak tokena po 2 minutach.");
            throw new Exception("Timeout Uwierzytelniania: Brak tokena po 2 minutach.");
        }
        var accessTokenResponse = await _ksefClient.GetAccessTokenAsync(authSubmission.AuthenticationToken.Token, cancellationToken);

        // 7) Zwróć token            
        return accessTokenResponse;
    }

    // (Implementacje Refresh i Revoke wedle IAuthService)
    public Task<TokenInfo> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => _ksefClient.RefreshAccessTokenAsync(refreshToken, cancellationToken)
                         .ContinueWith(t => t.Result.AccessToken, cancellationToken);
    public Task RevokeTokenAsync(string accessToken, CancellationToken cancellationToken = default)
        => _ksefClient.RevokeAccessTokenAsync(accessToken, cancellationToken);

}
