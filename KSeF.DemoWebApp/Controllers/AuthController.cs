using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WebApplication.Services;

namespace WebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{

    private readonly IAuthCoordinator _authCoordinator;
    private readonly IAuthorizationClient _authorizationClient;
    private readonly ISignatureService _signatureService;


    private readonly string _contextIdentifier;
    private readonly string? xMLDirectory;

    public AuthController(IAuthCoordinator authCoordinator, IConfiguration configuration, IAuthorizationClient authorizationClient, ISignatureService signatureService)
    {
        _authCoordinator = authCoordinator;
        _authorizationClient = authorizationClient;
        _signatureService = signatureService;
        _contextIdentifier = configuration["Tools:contextIdentifier"]!;
        xMLDirectory = configuration["Tools:XMLDirectory"];
    }

    [HttpPost("auth-by-coordinator-with-PZ")]
    public async Task<ActionResult<AuthenticationOperationStatusResponse>> AuthWithPZAsync(string contextIdentifier, CancellationToken cancellationToken)
    {
        // Inicjalizacja przykładowego identyfikatora - w tym przypadku NIP.

        return await _authCoordinator.AuthAsync(
                                                    AuthenticationTokenContextIdentifierType.Nip,
                                                    !string.IsNullOrWhiteSpace(contextIdentifier) ? contextIdentifier : _contextIdentifier,
                                                    AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject,
                                                    xmlSigner: (xml) => { return XadeSDummy.SignWithPZ(xml, xMLDirectory); },
                                                    authorizationPolicy: null,
                                                    cancellationToken);
    }

    [HttpPost("auth-step-by-step")]
    public async Task<ActionResult<AuthenticationOperationStatusResponse>> AuthStepByStepAsync(string contextIdentifier, CancellationToken cancellationToken)
    {

        return await _authorizationClient
            .AuthSessionStepByStep(
            AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject,
            !string.IsNullOrWhiteSpace(contextIdentifier) ? contextIdentifier : _contextIdentifier,
            (xml) => { return XadeSDummy.SignWithPZ(xml, xMLDirectory); },
            authorizationPolicy: null,
            cancellationToken);
    }

    [HttpGet("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return await _authorizationClient.RefreshAccessTokenAsync(
            refreshToken,
            cancellationToken);
    }

    [HttpGet("auth-with-ksef-certificate")]
    public async Task<AuthenticationOperationStatusResponse> AuthWithKsefCert(string certInBase64, string contextIdentifier, string privateKey, [FromServices] ISignatureService signatureService, CancellationToken cancellationToken)
    {
        byte[] cert = Convert.FromBase64String(certInBase64);
        X509Certificate2 x509 = new X509Certificate2(cert);
        byte[] privateKeyBytes = Convert.FromBase64String(privateKey);

        using RSA rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

        AuthenticationChallengeResponse challengeResponse = await _authorizationClient
           .GetAuthChallengeAsync(cancellationToken);

        string challenge = challengeResponse.Challenge;

        IAuthTokenRequestBuilderReady authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, contextIdentifier)
            .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject);


        AuthenticationTokenRequest authorizeRequest = authTokenRequest.Build();

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authorizeRequest);

        string signedXml = signatureService.Sign(unsignedXml, x509.CopyWithPrivateKey(rsa));

        SignatureResponse authSubmission = await _authorizationClient
           .SubmitXadesAuthRequestAsync(signedXml, false, cancellationToken);

        AuthStatus authStatus;
        DateTime startTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMinutes(2);

        do
        {
            authStatus = await _authorizationClient.GetAuthStatusAsync(authSubmission.ReferenceNumber, authSubmission.AuthenticationToken.Token, cancellationToken);

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
        AuthenticationOperationStatusResponse accessTokenResponse = await _authorizationClient.GetAccessTokenAsync(authSubmission.AuthenticationToken.Token, cancellationToken);

        // 7) Zwróć token            
        return accessTokenResponse;
    }

    [HttpPost("access-token")]
    public async Task<AuthenticationOperationStatusResponse> GetAuthOperationStatusAsync([FromBody] CertificateRequestModel certificateRequestModel)
    {
        AuthenticationChallengeResponse challengeResponse = await _authorizationClient.GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, certificateRequestModel.ContextIdentifier.Value)
            .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
            .Build();

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
                .Create()
                .WithGivenName(certificateRequestModel.GivenName)
                .WithSurname(certificateRequestModel.Surname)
                .WithSerialNumber($"{certificateRequestModel.SerialNumberPrefix}-{certificateRequestModel.SerialNumber}")
                .WithCommonName(certificateRequestModel.CommonName)
                .Build();

        string signedXml = _signatureService.Sign(unsignedXml, certificate);
        SignatureResponse signatureResponse = await _authorizationClient.SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus authStatus;
        DateTime startTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMinutes(2);

        do
        {
            authStatus = await _authorizationClient
                .GetAuthStatusAsync(signatureResponse.ReferenceNumber, signatureResponse.AuthenticationToken.Token);

            if (authStatus.Status.Code != 200)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        while (authStatus.Status.Code == 100
               && (DateTime.UtcNow - startTime) < timeout);

        if (authStatus.Status.Code != 200)
        {
            string msg = $"Uwierzytelnienie nie powiodło się. Kod statusu: {authStatus?.Status.Code}, opis: {authStatus?.Status.Description}.";

            throw new Exception(msg);
        }

        AuthenticationOperationStatusResponse authResult = await _authorizationClient.GetAccessTokenAsync(signatureResponse.AuthenticationToken.Token);
        return authResult;
    }
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

public static class AuthSessionStepByStepHelper
{
    public static async Task<AuthenticationOperationStatusResponse>
        AuthSessionStepByStep(this IAuthorizationClient authorizationClient, AuthenticationTokenSubjectIdentifierTypeEnum authIdentifierType, string contextIdentifier, Func<string, Task<string>> xmlSigner, AuthenticationTokenAuthorizationPolicy? authorizationPolicy = null, CancellationToken cancellationToken = default)
    {

        // Wykonanie auth challenge.
        AuthenticationChallengeResponse challengeResponse = await authorizationClient
            .GetAuthChallengeAsync();

        Console.WriteLine(challengeResponse.Challenge);

        // Wymagany jest podpis cyfrowy w formacie XAdES-BES.
        AuthenticationTokenRequest authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, contextIdentifier)
            .WithIdentifierType(authIdentifierType)
            .WithAuthorizationPolicy(authorizationPolicy ?? new AuthenticationTokenAuthorizationPolicy { /* ... */ })      // optional
            .Build();

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        // TODO Trzeba podpisac XML przed wysłaniem
        string signedXml = await xmlSigner.Invoke(unsignedXml);

        // Przesłanie podpisanego XML do systemu KSeF
        SignatureResponse authOperationInfo = await authorizationClient.
            SubmitXadesAuthRequestAsync(signedXml, false, cancellationToken);

        AuthStatus authorizationStatus;
        int maxRetry = 5;
        int currentLoginAttempt = 0;
        TimeSpan sleepTime = TimeSpan.FromSeconds(1);

        do
        {
            if (currentLoginAttempt >= maxRetry)
            {
                throw new Exception("Autoryzacja nieudana - przekroczono liczbę dozwolonych prób logowania.");
            }

            await Task.Delay(sleepTime + TimeSpan.FromSeconds(currentLoginAttempt));
            authorizationStatus = await authorizationClient.GetAuthStatusAsync(
                authOperationInfo.ReferenceNumber,
                authOperationInfo.AuthenticationToken.Token);
            currentLoginAttempt++;
        }
        while (authorizationStatus.Status.Code != 200);

        // Uzyskanie accessToken w celu uwierzytelniania 
        AuthenticationOperationStatusResponse accessTokenResult = await authorizationClient
            .GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token, cancellationToken);

        return accessTokenResult;
    }
}