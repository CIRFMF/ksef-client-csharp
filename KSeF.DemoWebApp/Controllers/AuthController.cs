using KSeFClient.Api.Services;
using Microsoft.AspNetCore.Mvc;
using KSeFClient;
using KSeFClient.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeFClient.Api.Builders.Auth;

namespace WebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{

    private readonly IAuthCoordinator authCoordinator;
    private readonly IKSeFClient ksefClient;


    private readonly string _contextIdentifier;
    private readonly string? xMLDirectory;

    public AuthController(IAuthCoordinator authCoordinator, IConfiguration configuration, IKSeFClient ksefClient)
    {
        this.authCoordinator = authCoordinator;
        this.ksefClient = ksefClient;

        _contextIdentifier = configuration["Tools:contextIdentifier"]!;
        xMLDirectory = configuration["Tools:XMLDirectory"];
    }

    [HttpPost("auth-by-coordinator-with-PZ")]
    public async Task<ActionResult<AuthOperationStatusResponse>> AuthWithPZAsync(string contextIdentifier, CancellationToken cancellationToken)
    {
        // Inicjalizacja przyk�dowego identyfikatora - w tym przypadku NIP.

        return await authCoordinator.AuthAsync(
                                                    ContextIdentifierType.Nip,
                                                    string.IsNullOrWhiteSpace(contextIdentifier) ? contextIdentifier : _contextIdentifier,
                                                    SubjectIdentifierTypeEnum.CertificateSubject,
                                                    xmlSigner: (xml) => { return XadeSDummy.SignWithPZ(xml, xMLDirectory); },
                                                    ipAddressPolicy: null,
                                                    cancellationToken);
    }

    [HttpPost("auth-step-by-step")]
    public async Task<ActionResult<AuthOperationStatusResponse>> AuthStepByStepAsync(string contextIdentifier, CancellationToken cancellationToken)
    {

        return await ksefClient
            .AuthSessionStepByStep(
            SubjectIdentifierTypeEnum.CertificateSubject,
            string.IsNullOrWhiteSpace(contextIdentifier) ? contextIdentifier : _contextIdentifier,
            (xml) => { return XadeSDummy.SignWithPZ(xml, xMLDirectory); },
            ipAddressPolicy: null,
            cancellationToken);
    }

    [HttpGet("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return await ksefClient.RefreshAccessTokenAsync(
            refreshToken,
            cancellationToken);
    }
}

public static class AuthSessionStepByStepHelper
{
    public static async Task<AuthOperationStatusResponse>
        AuthSessionStepByStep(this IKSeFClient ksefClient, SubjectIdentifierTypeEnum authIdentifierType, string contextIdentifier, Func<string, Task<string>> xmlSigner, IpAddressPolicy? ipAddressPolicy = null, CancellationToken cancellationToken = default)
    {

        // Wykonanie auth challenge.
        var challengeResponse = await ksefClient
            .GetAuthChallengeAsync();

        Console.WriteLine(challengeResponse.Challenge);

        // Wymagany jest podpis cyfrowy w formacie XAdES-BES.
        var authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(ContextIdentifierType.Nip, contextIdentifier)
            .WithIdentifierType(authIdentifierType)
            .WithIpAddressPolicy(ipAddressPolicy ?? new IpAddressPolicy { /* ... */ })      // optional
            .Build();

        var unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        // TODO Trzeba podpisac XML przed wys�aniem
        var signedXml = await xmlSigner.Invoke(unsignedXml);

        // Przesłanie podpisanego XML do systemu KSeF
        var authOperationInfo = await ksefClient.
            SubmitXadesAuthRequestAsync(signedXml, false, cancellationToken);


        // Uzyskanie accessToken w celu uwierzytelniania 
        var accessTokenResult = await ksefClient
            .GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token, cancellationToken);

        return accessTokenResult;
    }
}