using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Tests.config;
using KSeFClient;
using KSeFClient.Api.Builders.Auth;
using KSeFClient.Core.Models;
using KSeFClient.Http;


namespace KSeF.Client.Tests;

public class TestBase
{
    internal string AccessToken { get; private set; }
    internal string RefreshToken { get; private set; }
    internal IKSeFClient kSeFClient { get; private set; }

    internal string env = TestConfig.Load()["ApiSettings:BaseUrl"] ?? KSeFClient.DI.KsefEnviromentsUris.TEST;
    internal Random randomGenerator;
    internal string NIP;

    internal ISignatureService signatureService { get; private set; }

    internal readonly HttpClient httpClient;
    internal readonly RestClient restClient;
    internal ContextIdentifierType contextIdentifierType;
    internal const int sleepTime = 350;
    internal TestBase(ContextIdentifierType contextIdentifierType = ContextIdentifierType.Nip)
    {
        this.contextIdentifierType = contextIdentifierType;
        randomGenerator = new Random();

        signatureService = new SignatureService();

        httpClient = new HttpClient { BaseAddress = new Uri(env) };
        restClient = new RestClient(httpClient);

        kSeFClient = new KSeFClient.Http.KSeFClient(
            restClient
        );

        AuthenticateAsync().GetAwaiter().GetResult();
    }

    internal async Task<AuthOperationStatusResponse> AuthenticateAsync()
    {
        var random = randomGenerator.Next(100000000, 999999999);
        var randomString = 7 + random.ToString();
        NIP = randomString;

        var challengeResponse = await kSeFClient
            .GetAuthChallengeAsync();


        var authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(contextIdentifierType, randomString)
           .WithIdentifierType(SubjectIdentifierTypeEnum.CertificateSubject) // or Fingerprint
           .WithIpAddressPolicy(new IpAddressPolicy { /* ... */ })
           .Build();

        var unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        var certificate = SelfSignedCertificateForSignatureBuilder
            .Create()
            .WithGivenName("A")
            .WithSurname("R")
            .WithSerialNumber("PNOPL-9" + randomString) // Alternatywnie: TINPL-1234567890
            .WithCommonName("A R")
            .Build();

        var signedXml = await signatureService.Sign(unsignedXml, certificate);

        var authOperationInfo = await kSeFClient
          .SubmitXadesAuthRequestAsync(signedXml,false,CancellationToken.None);


        AuthStatus accessTokenResponse;
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(2);

        do
        {
            accessTokenResponse = await kSeFClient
                .GetAuthStatusAsync(authOperationInfo.ReferenceNumber,authOperationInfo.AuthenticationToken.Token);

            Console.WriteLine(
                $"Polling: StatusCode={accessTokenResponse.Status.Code}, " +
                $"Description='{accessTokenResponse.Status.Description}', " +
                $"Elapsed={DateTime.UtcNow - startTime:mm\\:ss}");

            if (accessTokenResponse.Status.Code != 200)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        while (accessTokenResponse.Status.Code == 100
               && (DateTime.UtcNow - startTime) < timeout);


        if (accessTokenResponse.Status.Code != 200)
        {
            var msg = $"Authentication failed. Status code: {accessTokenResponse?.Status.Code}, description: {accessTokenResponse?.Status.Description}.";
           
            throw new Exception(msg);
        }

        var authResult = await kSeFClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);

        // Simulate authentication and set the access token
        AccessToken = authResult.AccessToken.Token;
        RefreshToken = authResult.RefreshToken.Token;

        return authResult;
    }
}
