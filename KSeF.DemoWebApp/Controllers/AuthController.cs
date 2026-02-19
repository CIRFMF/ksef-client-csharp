using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Extensions;
using KSeF.DemoWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IAuthCoordinator authCoordinator,
    IConfiguration configuration,
    IAuthorizationClient authorizationClient,
    ICryptographyService cryptographyService) : ControllerBase
{

    private readonly string _contextIdentifier = configuration["Tools:ContextIdentifier"]!;
    private readonly string xMLDirectory = configuration["Tools:XMLDirectory"] ?? string.Empty;

    private readonly ICryptographyService _cryptographyService;

    [HttpPost("auth-by-coordinator-with-PZ")]
    public async Task<ActionResult<AuthenticationOperationStatusResponse>> AuthWithPZAsync(string contextIdentifier, CancellationToken cancellationToken)
    {
        // Inicjalizacja przykładowego identyfikatora - w tym przypadku NIP.

        return await authCoordinator.AuthAsync(
                                                    AuthenticationTokenContextIdentifierType.Nip,
                                                    !string.IsNullOrWhiteSpace(contextIdentifier) ? contextIdentifier : _contextIdentifier,
                                                    AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject,
                                                    xmlSigner: (xml) => { return XadeSDummy.SignWithPZ(xml, xMLDirectory, timeout: TimeSpan.FromMinutes(10)); },
                                                    authorizationPolicy: null,
                                                    cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("auth-step-by-step")]
    public async Task<ActionResult<AuthenticationOperationStatusResponse>> AuthStepByStepAsync(string contextIdentifier, CancellationToken cancellationToken)
    {
        return await authorizationClient
            .AuthSessionStepByStep(
                AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject,
                contextIdentifier,
                authorizationPolicy: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return await authorizationClient.RefreshAccessTokenAsync(
            refreshToken,
            cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("auth-with-ksef-certificate")]
    public async Task<AuthenticationOperationStatusResponse> AuthWithKsefCert(string certInBase64, string contextIdentifier, string privateKey, CancellationToken cancellationToken)
    {
        byte[] cert = Convert.FromBase64String(certInBase64);
        X509Certificate2 x509 = cert.LoadPkcs12();
        byte[] privateKeyBytes = Convert.FromBase64String(privateKey);

        using RSA rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

        AuthenticationChallengeResponse challengeResponse = await authorizationClient
           .GetAuthChallengeAsync(cancellationToken).ConfigureAwait(false);

        string challenge = challengeResponse.Challenge;

        IAuthTokenRequestBuilderReady authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, contextIdentifier)
            .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject);


        AuthenticationTokenRequest authorizeRequest = authTokenRequest.Build();

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authorizeRequest);

        string signedXml = SignatureService.Sign(unsignedXml, x509.CopyWithPrivateKey(rsa));

        SignatureResponse authSubmission = await authorizationClient
           .SubmitXadesAuthRequestAsync(signedXml, false, false, cancellationToken).ConfigureAwait(false);

        AuthStatus authStatus;
        DateTime startTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMinutes(2);

        do
        {
            authStatus = await authorizationClient.GetAuthStatusAsync(authSubmission.ReferenceNumber, authSubmission.AuthenticationToken.Token, cancellationToken).ConfigureAwait(false);

            Console.WriteLine(
                $"Polling: StatusCode={authStatus.Status.Code}, " +
                $"Description='{authStatus.Status.Description}', " +
                $"Elapsed={DateTime.UtcNow - startTime:mm\\:ss}");

            if (authStatus.Status.Code != 200 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }
        while (authStatus.Status.Code != 200
            && !cancellationToken.IsCancellationRequested
            && (DateTime.UtcNow - startTime) < timeout);

        if (authStatus.Status.Code != 200)
        {
            Console.WriteLine("Timeout: Brak tokena po 2 minutach.");
            throw new TimeoutException("Timeout Uwierzytelniania: Brak tokena po 2 minutach.");
        }
        AuthenticationOperationStatusResponse accessTokenResponse = await authorizationClient.GetAccessTokenAsync(authSubmission.AuthenticationToken.Token, cancellationToken).ConfigureAwait(false);

        // 7) Zwróć token            
        return accessTokenResponse;
    }

    [HttpPost("access-token")]
    public async Task<AuthenticationOperationStatusResponse> GetAuthOperationStatusAsync([FromBody] CertificateRequestModel certificateRequestModel)
    {
        AuthenticationChallengeResponse challengeResponse = await authorizationClient.GetAuthChallengeAsync().ConfigureAwait(false);

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, certificateRequestModel.ContextIdentifier.Value)
            .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
            .Build();

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
                .Create()
                .WithGivenName(certificateRequestModel.GivenName)
                .WithSurname(certificateRequestModel.Surname)
                .WithSerialNumber($"{certificateRequestModel.SerialNumberPrefix}-{certificateRequestModel.SerialNumber}")
                .WithCommonName(certificateRequestModel.CommonName)
                .Build();

        string signedXml = SignatureService.Sign(unsignedXml, certificate);
        SignatureResponse signatureResponse = await authorizationClient.SubmitXadesAuthRequestAsync(signedXml, false, false, CancellationToken.None).ConfigureAwait(false);

        AuthStatus authStatus;
        DateTime startTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMinutes(2);

        do
        {
            authStatus = await authorizationClient
                .GetAuthStatusAsync(signatureResponse.ReferenceNumber, signatureResponse.AuthenticationToken.Token).ConfigureAwait(false);

            if (authStatus.Status.Code != 200)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }
        while (authStatus.Status.Code == 100
               && (DateTime.UtcNow - startTime) < timeout);

        if (authStatus.Status.Code != 200)
        {
            string msg = $"Uwierzytelnienie nie powiodło się. Kod statusu: {authStatus?.Status.Code}, opis: {authStatus?.Status.Description}.";

            throw new InvalidOperationException(msg);
        }

        AuthenticationOperationStatusResponse authResult = await authorizationClient.GetAccessTokenAsync(signatureResponse.AuthenticationToken.Token).ConfigureAwait(false);
        return authResult;
    }

    [HttpPost("auth-with-ksef-token")]
    public async Task<AuthenticationOperationStatusResponse> GetAccessTokenWithKsefTokenAsync([FromBody] string ksefToken, CancellationToken cancellationToken)
    {
        AuthenticationChallengeResponse challengeResponse = await authorizationClient.GetAuthChallengeAsync(cancellationToken).ConfigureAwait(false);

        long timeStamp = challengeResponse.Timestamp.ToUnixTimeMilliseconds();

        string tokenWithTimestamp = $"{ksefToken}|{timeStamp}";
        byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenWithTimestamp);
        byte[] encryptedBytes = cryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes);

        string encryptedTokenBase64 = Convert.ToBase64String(encryptedBytes);

        IAuthKsefTokenRequestBuilderWithEncryptedToken builder = AuthKsefTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, _contextIdentifier)
            .WithEncryptedToken(encryptedTokenBase64);

        SignatureResponse signatureResponse = await authorizationClient
            .SubmitKsefTokenAuthRequestAsync(builder.Build(), cancellationToken).ConfigureAwait(false);

        AuthStatus authStatus = await authorizationClient
            .GetAuthStatusAsync(signatureResponse.ReferenceNumber, signatureResponse.AuthenticationToken.Token, cancellationToken).ConfigureAwait(false);

        AuthenticationOperationStatusResponse authResult = await authorizationClient
            .GetAccessTokenAsync(signatureResponse.AuthenticationToken.Token, cancellationToken).ConfigureAwait(false);

        return authResult;
    }

    public class CertificateRequestModel
    {
        public ContextIdentifier ContextIdentifier { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string SerialNumberPrefix { get; set; }
        public string SerialNumber { get; set; }
        public string CommonName { get; set; }
    }
}

public static class AuthSessionStepByStepHelper
{
    public static async Task<AuthenticationOperationStatusResponse>
        AuthSessionStepByStep(this IAuthorizationClient authorizationClient,
        AuthenticationTokenSubjectIdentifierTypeEnum authIdentifierType, string contextIdentifier,
        AuthenticationTokenAuthorizationPolicy? authorizationPolicy = null, CancellationToken cancellationToken = default)
    {

        // Wykonanie auth challenge.
        AuthenticationChallengeResponse challengeResponse = await authorizationClient
            .GetAuthChallengeAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine(challengeResponse.Challenge);

        // Wymagany jest podpis cyfrowy w formacie XAdES-BES.
        AuthenticationTokenRequest authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, contextIdentifier)
            .WithIdentifierType(authIdentifierType)    // optional
            .WithAuthorizationPolicy(authorizationPolicy)    // optional
            .Build();

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        X509Certificate2 certificate =
            GetPersonalCertificate("A", "R", contextIdentifier.Length == 11 ? "PNOPL" : "TINPL", contextIdentifier, "A R");

        string signedXml = SignatureService.Sign(unsignedXml, certificate);

        // Przesłanie podpisanego XML do systemu KSeF
        SignatureResponse authOperationInfo = await authorizationClient.
            SubmitXadesAuthRequestAsync(signedXml, false, false, cancellationToken).ConfigureAwait(false);

        AuthStatus authorizationStatus;
        int maxRetry = 5;
        int currentLoginAttempt = 0;
        TimeSpan sleepTime = TimeSpan.FromSeconds(1);

        do
        {
            if (currentLoginAttempt >= maxRetry)
            {
                throw new InvalidOperationException("Autoryzacja nieudana - przekroczono liczbę dozwolonych prób logowania.");
            }

            await Task.Delay(sleepTime + TimeSpan.FromSeconds(currentLoginAttempt), cancellationToken).ConfigureAwait(false);
            authorizationStatus = await authorizationClient.GetAuthStatusAsync(
                authOperationInfo.ReferenceNumber,
                authOperationInfo.AuthenticationToken.Token,
                cancellationToken).ConfigureAwait(false);
            currentLoginAttempt++;
        }
        while (authorizationStatus.Status.Code != 200);

        // Uzyskanie accessToken w celu uwierzytelniania 
        AuthenticationOperationStatusResponse accessTokenResult = await authorizationClient
            .GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token, cancellationToken).ConfigureAwait(false);

        return accessTokenResult;
    }

    /// <summary>
    /// Tworzy testowy, samopodpisany certyfikat przeznaczony do składania podpisu (XAdES).
    /// </summary>
    /// <param name="givenName">Imię właściciela certyfikatu.</param>
    /// <param name="surname">Nazwisko właściciela certyfikatu.</param>
    /// <param name="serialNumberPrefix">Prefiks numeru seryjnego.</param>
    /// <param name="serialNumber">Numer seryjny.</param>
    /// <param name="commonName">Wspólna nazwa (CN) certyfikatu.</param>
    /// <param name="encryptionType">Rodzaj certyfikatu</param>
    /// <returns><see cref="X509Certificate2"/> będący samopodpisanym certyfikatem do podpisu.</returns>
    public static X509Certificate2 GetPersonalCertificate(
        string givenName,
        string surname,
        string serialNumberPrefix,
        string serialNumber,
        string commonName,
        EncryptionMethodEnum encryptionType = EncryptionMethodEnum.Rsa
        )
    {
        X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
                    .Create()
                    .WithGivenName(givenName)
                    .WithSurname(surname)
                    .WithSerialNumber($"{serialNumberPrefix}-{serialNumber}")
                    .WithCommonName(commonName)
                    .AndEncryptionType(encryptionType)
                    .Build();
        return certificate;
    }
}