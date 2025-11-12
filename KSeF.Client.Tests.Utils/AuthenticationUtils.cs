using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Core.Models;
using System.Security.Cryptography.X509Certificates;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;

namespace KSeF.Client.Tests.Utils;
public static class AuthenticationUtils
{
    private const int AuthInProgressCode = 100;
    private const int AuthSuccessCode = 200;

    /// <summary>
    /// Przeprowadza pełny proces uwierzytelnienia w KSeF z wykorzystaniem podpisu XAdES dla wskazanego identyfikatora.
    /// </summary>
    public static async Task<AuthenticationOperationStatusResponse> AuthenticateAsync(
        IAuthorizationClient authorizationClient,
        ISignatureService signatureService,
        string identifierValue,
        AuthenticationTokenContextIdentifierType contextIdentifierType = AuthenticationTokenContextIdentifierType.Nip,
        EncryptionMethodEnum encryptionMethod = EncryptionMethodEnum.Rsa)
    {
        AuthenticationChallengeResponse challengeResponse = await authorizationClient
            .GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = GetAuthorizationTokenRequest(
            challengeResponse.Challenge,
            contextIdentifierType,
            identifierValue,
            AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject);

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        X509Certificate2 certificate = CertificateUtils.GetPersonalCertificate("A", "R", identifierValue.Length == 11 ? "PNOPL" : "TINPL", identifierValue, "A R");

        string signedXml = signatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await authorizationClient
            .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus finalStatus = await WaitForAuthCompletionAsync(authorizationClient, authOperationInfo);
        EnsureSuccess(finalStatus);

        AuthenticationOperationStatusResponse authResult =
            await authorizationClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
        return authResult;
    }

    /// <summary>
    /// Przeprowadza pełny proces uwierzytelnienia w KSeF z wykorzystaniem podpisu XAdES dla wskazanego numeru identyfikatora w kontekście innego podmiotu.
    /// </summary>
    public static async Task<AuthenticationOperationStatusResponse> AuthenticateAsync(
        IAuthorizationClient authorizationClient,
        ISignatureService signatureService,
        string identifierValue,
        string contextIdentifierValue,
        AuthenticationTokenContextIdentifierType contextIdentifierType = AuthenticationTokenContextIdentifierType.Nip)
    {
        AuthenticationChallengeResponse challengeResponse = await authorizationClient   
            .GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = GetAuthorizationTokenRequest(
            challengeResponse.Challenge,
            contextIdentifierType,
            contextIdentifierValue,
            AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject);

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        X509Certificate2 certificate =
            CertificateUtils.GetPersonalCertificate("A", "R", identifierValue.Length == 11 ? "PNOPL" : "TINPL", identifierValue, "A R");

        string signedXml = signatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await authorizationClient
            .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus finalStatus = await WaitForAuthCompletionAsync(authorizationClient, authOperationInfo);
        EnsureSuccess(finalStatus);

        return await authorizationClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
    }

    /// <summary>
    /// Przeprowadza proces uwierzytelnienia w KSeF generując losowy NIP (test) i wykorzystując podpis XAdES.
    /// </summary>
    public static async Task<AuthenticationOperationStatusResponse> AuthenticateAsync(
        IAuthorizationClient authorizationClient,
        ISignatureService signatureService,
        AuthenticationTokenContextIdentifierType contextIdentifierType = AuthenticationTokenContextIdentifierType.Nip,
        EncryptionMethodEnum encryptionMethod = EncryptionMethodEnum.Rsa
        )
    {
        string nip = MiscellaneousUtils.GetRandomNip();

        AuthenticationChallengeResponse challengeResponse = await authorizationClient
            .GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = GetAuthorizationTokenRequest(
            challengeResponse.Challenge,
            contextIdentifierType,
            nip,
            AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject);

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        X509Certificate2 certificate = CertificateUtils.GetPersonalCertificate("A", "R", "TINPL", nip, "A R", encryptionMethod);

        string signedXml = signatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await authorizationClient
            .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus finalStatus = await WaitForAuthCompletionAsync(authorizationClient, authOperationInfo);
        EnsureSuccess(finalStatus);

        return await authorizationClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
    }

    /// <summary>
    /// Przeprowadza uwierzytelnienie dla dostarczonego certyfikatu i parametrów identyfikatora kontekstu.
    /// </summary>
    public static async Task<AuthenticationOperationStatusResponse> AuthenticateAsync(
        IAuthorizationClient authorizationClient,
        ISignatureService signatureService,
        string contextIdentifierValue,
        AuthenticationTokenContextIdentifierType contextIdentifierType,
        X509Certificate2 certificate,
        AuthenticationTokenSubjectIdentifierTypeEnum subjectIdentifierType = AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
    {
        AuthenticationChallengeResponse challengeResponse = await authorizationClient
            .GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = GetAuthorizationTokenRequest(
            challengeResponse.Challenge,
            contextIdentifierType,
            contextIdentifierValue,
            subjectIdentifierType);

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        string signedXml = signatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await authorizationClient
            .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus finalStatus = await WaitForAuthCompletionAsync(authorizationClient, authOperationInfo);
        EnsureSuccess(finalStatus);

        return await authorizationClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
    }

    /// <summary>
    /// Buduje żądanie tokenu autoryzacyjnego (AuthTokenRequest).
    /// </summary>
    public static AuthenticationTokenRequest GetAuthorizationTokenRequest(
        string challengeToken,
        AuthenticationTokenContextIdentifierType contextIdentifierType,
        string nip,
        AuthenticationTokenSubjectIdentifierTypeEnum subjectIdentifierTypeEnum = AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
    {
        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeToken)
           .WithContext(contextIdentifierType, nip)
           .WithIdentifierType(subjectIdentifierTypeEnum)
           .WithAuthorizationPolicy(null)
           .Build();

        return authTokenRequest;
    }

    /// <summary>
    /// Wspólna logika oczekiwania na zakończenie operacji uwierzytelnienia.
    /// Zwraca finalny AuthStatus (kod != 100) lub ostatni status po przekroczeniu limitu czasu.
    /// </summary>
    private static async Task<AuthStatus> WaitForAuthCompletionAsync(
        IAuthorizationClient authorizationClient,
        SignatureResponse authOperationInfo,
        TimeSpan? timeout = null,
        TimeSpan? pollDelay = null)
    {
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromMinutes(2);
        TimeSpan delay = pollDelay ?? TimeSpan.FromSeconds(1);

        // Wylicz liczbę prób (>=1)
        int maxAttempts = (int)Math.Ceiling(effectiveTimeout.TotalMilliseconds / delay.TotalMilliseconds);
        if (maxAttempts <= 0) maxAttempts = 1;

        DateTime startTime = DateTime.UtcNow;
        AuthStatus? lastStatus = null;

        try
        {
            // Pollujemy aż status != 100 (czyli zakończony sukcesem lub błędem).
            AuthStatus finalStatus = await AsyncPollingUtils.PollAsync(
                action: async () =>
                {
                    AuthStatus status = await authorizationClient
                        .GetAuthStatusAsync(authOperationInfo.ReferenceNumber, authOperationInfo.AuthenticationToken.Token)
                        .ConfigureAwait(false);

                    lastStatus = status;

                    Console.WriteLine(
                        $"Odpytanie: KodStatusu={status.Status.Code}, " +
                        $"Opis='{status.Status.Description}', " +
                        $"Upłynęło={DateTime.UtcNow - startTime:mm\\:ss}");

                    return status;
                },
                condition: s => s.Status.Code != AuthInProgressCode,
                description: "Oczekiwanie na zakończenie uwierzytelnienia",
                delay: delay,
                maxAttempts: maxAttempts
            ).ConfigureAwait(false);

            return finalStatus;
        }
        catch (TimeoutException)
        {
            return lastStatus ?? new AuthStatus
            {
                Status = new StatusInfo
                {
                    Code = AuthInProgressCode,
                    Description = "Brak finalnego statusu przed upływem limitu czasu."
                }
            };
        }
    }

    private static void EnsureSuccess(AuthStatus status)
    {
        if (status.Status.Code != AuthSuccessCode)
        {
            string msg = $"Uwierzytelnienie nie powiodło się. Kod statusu: {status?.Status.Code}, opis: {status?.Status.Description}.";
            throw new Exception(msg);
        }
    }
}