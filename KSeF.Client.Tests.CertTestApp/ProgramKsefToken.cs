using KSeF.Client.Api.Services;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.DI;
using KSeF.Client.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.KsefTokenApp;

/// <summary>
/// Demonstration of KSeF Token authentication - NO CERTIFICATE REQUIRED!
/// This is the simpler authentication method for KSeF 2.0.
/// 
/// PREREQUISITES:
/// You need to first authenticate once with a certificate to obtain an access token,
/// then you can generate KSeF tokens that can be used for authentication without certificates.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("KSeF.Client - KSeF Token Authentication (No Certificate Required)");
        Console.WriteLine("====================================================================\n");
        Console.WriteLine("NOTE: This demonstrates KSeF Token auth which doesn't require certificates");
        Console.WriteLine("      once you have generated a KSeF token.\n");

        // 0) Setup DI
        ServiceCollection services = new ServiceCollection();
        services.AddKSeFClient(options =>
        {
            options.BaseUrl = KsefEnvironmentsUris.DEMO;
        });

        services.AddSingleton<ICryptographyClient, CryptographyClient>();
        services.AddSingleton<ICryptographyService, CryptographyService>(serviceProvider =>
        {
            return new CryptographyService(async cancellationToken =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ICryptographyClient cryptographyClient = scope.ServiceProvider.GetRequiredService<ICryptographyClient>();
                return await cryptographyClient.GetPublicCertificatesAsync(cancellationToken);
            });
        });
        services.AddSingleton<CryptographyWarmupHostedService>();

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<CryptographyWarmupHostedService>()
           .StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        IKSeFClient ksefClient = provider.GetRequiredService<IKSeFClient>();
        ICryptographyService cryptographyService = provider.GetRequiredService<ICryptographyService>();

        try
        {
            Console.WriteLine("=== STEP 1: INITIAL SETUP (One-time, requires certificate) ===\n");
            Console.WriteLine("First, we need to authenticate once with a certificate to get an access token.");
            Console.WriteLine("This is a one-time setup. After this, you can use KSeF tokens instead of certificates.\n");

            // Step 1: Initial authentication with certificate (one-time setup)
            string testNip = MiscellaneousUtils.GetRandomNip();
            Console.WriteLine($"[1.1] Generated test NIP: {testNip}");

            Console.WriteLine("[1.2] Performing initial authentication with test certificate...");
            string initialAccessToken = await InitialAuthenticationWithCertificate(ksefClient, testNip);
            Console.WriteLine($"      ✓ Initial access token obtained: {initialAccessToken[..15]}...\n");

            Console.WriteLine("=== STEP 2: GENERATE KSEF TOKEN (No certificate needed!) ===\n");

            // Step 2: Generate a KSeF token (this is like an API key)
            Console.WriteLine("[2.1] Generating KSeF token...");
            KsefTokenResponse tokenResponse = await GenerateKsefToken(
                ksefClient,
                initialAccessToken,
                "Demo Token - No Certificate Required"
            );
            Console.WriteLine($"      ✓ KSeF Token generated!");
            Console.WriteLine($"      Token: {tokenResponse.Token}");
            Console.WriteLine($"      Reference Number: {tokenResponse.ReferenceNumber}\n");

            // Step 3: Wait for token to become active
            Console.WriteLine("[2.2] Waiting for token to become active...");
            AuthenticationKsefToken activeToken = await WaitForTokenActivation(
                ksefClient,
                tokenResponse.ReferenceNumber,
                initialAccessToken
            );
            Console.WriteLine($"      ✓ Token is now active! Status: {activeToken.Status}\n");

            Console.WriteLine("=== STEP 3: AUTHENTICATE WITH KSEF TOKEN (No certificate!) ===\n");
            Console.WriteLine("Now we can authenticate using ONLY the token - no certificate needed!");
            Console.WriteLine("This is what you're looking for - simple token-based auth!\n");

            // Step 4: Authenticate using the KSeF token (NO CERTIFICATE!)
            Console.WriteLine("[3.1] Authenticating with KSeF token (no certificate required)...");
            string newAccessToken = await AuthenticateWithKsefToken(
                ksefClient,
                cryptographyService,
                tokenResponse.Token,
                testNip
            );
            Console.WriteLine($"      ✓ Authenticated successfully!");
            Console.WriteLine($"      New Access Token: {newAccessToken[..15]}...\n");

            Console.WriteLine("=== STEP 4: USE THE TOKEN FOR OPERATIONS ===\n");

            // Step 5: Use the token to perform operations
            Console.WriteLine("[4.1] Opening online session with token-based auth...");
            OpenOnlineSessionResponse session = await OpenSession(
                ksefClient,
                cryptographyService,
                newAccessToken
            );
            Console.WriteLine($"      ✓ Session opened: {session.ValidUntil}");

            // Cleanup
            Console.WriteLine("[4.2] Closing session...");
            await ksefClient.CloseOnlineSessionAsync(
                session.ReferenceNumber,
                newAccessToken,
                CancellationToken.None
            );
            Console.WriteLine("      ✓ Session closed\n");

            Console.WriteLine("[4.3] Revoking KSeF token...");
            await ksefClient.RevokeKsefTokenAsync(
                tokenResponse.ReferenceNumber,
                initialAccessToken,
                CancellationToken.None
            );
            Console.WriteLine("      ✓ Token revoked\n");

            Console.WriteLine("════════════════════════════════════════════════════════════════");
            Console.WriteLine("✓ SUCCESS! Complete flow demonstrated!");
            Console.WriteLine("════════════════════════════════════════════════════════════════\n");
            Console.WriteLine("KEY TAKEAWAYS:");
            Console.WriteLine("  1. Initial setup requires certificate authentication (one time)");
            Console.WriteLine("  2. Generate KSeF token using the initial access token");
            Console.WriteLine("  3. Use KSeF token for auth - NO CERTIFICATE NEEDED!");
            Console.WriteLine("  4. KSeF token works like an API key");
            Console.WriteLine("  5. Token can be used repeatedly until revoked");
            Console.WriteLine("\nThis is the 'token-based' auth you were asking about!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine($"\nDetails:\n{ex}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Initial authentication with certificate (one-time setup).
    /// After this, you can use KSeF tokens instead.
    /// </summary>
    private static async Task<string> InitialAuthenticationWithCertificate(
        IKSeFClient ksefClient,
        string nip)
    {
        // This is the same as ProgramTokenAuth.cs - using test certificate
        SignatureService signatureService = new SignatureService();

        AuthenticationChallengeResponse challengeResponse = await ksefClient.GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = KSeF.Client.Api.Builders.Auth.AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, nip)
            .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
            .Build();

        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        X509Certificate2 certificate = CertificateUtils.GetPersonalCertificate(
            "Test", "User", "TINPL", nip, "Test User"
        );

        string signedXml = signatureService.Sign(unsignedXml, certificate);

        SignatureResponse submission = await ksefClient.SubmitXadesAuthRequestAsync(signedXml, false);

        AuthStatus finalStatus;

        finalStatus = await ksefClient.GetAuthStatusAsync(
            submission.ReferenceNumber,
            submission.AuthenticationToken.Token
        );
        if (finalStatus.Status.Code == 100)

            if (finalStatus.Status.Code != 200)
                throw new Exception($"Auth failed: {finalStatus.Status.Description}");

        AuthenticationOperationStatusResponse tokenResponse = await ksefClient.GetAccessTokenAsync(submission.AuthenticationToken.Token);
        return tokenResponse.AccessToken.Token;
    }

    /// <summary>
    /// Generate a KSeF token (like an API key).
    /// This token can be used for authentication WITHOUT a certificate!
    /// </summary>
    private static async Task<KsefTokenResponse> GenerateKsefToken(
        IKSeFClient ksefClient,
        string accessToken,
        string description)
    {
        KsefTokenRequest request = new KsefTokenRequest
        {
            Permissions = new[]
            {
                KsefTokenPermissionType.InvoiceRead,
                KsefTokenPermissionType.InvoiceWrite,
                KsefTokenPermissionType.CredentialsManage
            },
            Description = description
        };

        return await ksefClient.GenerateKsefTokenAsync(
            request,
            accessToken,
            CancellationToken.None
        );
    }

    /// <summary>
    /// Wait for the KSeF token to become active.
    /// </summary>
    private static async Task<AuthenticationKsefToken> WaitForTokenActivation(
        IKSeFClient ksefClient,
        string tokenReferenceNumber,
        string accessToken)
    {
        AuthenticationKsefToken token;
        int attempts = 0;
        do
        {
            token = await ksefClient.GetKsefTokenAsync(
                tokenReferenceNumber,
                accessToken,
                CancellationToken.None
            );

            if (token.Status != AuthenticationKsefTokenStatus.Active)
            {
                await Task.Delay(1000);
                attempts++;
            }
        }
        while (token.Status != AuthenticationKsefTokenStatus.Active && attempts < 60);

        if (token.Status != AuthenticationKsefTokenStatus.Active)
            throw new Exception("Token did not become active in time");

        return token;
    }

    /// <summary>
    /// Authenticate using KSeF token - NO CERTIFICATE REQUIRED!
    /// This is the key feature you're asking about.
    /// </summary>
    private static async Task<string> AuthenticateWithKsefToken(
        IKSeFClient ksefClient,
        ICryptographyService cryptographyService,
        string ksefToken,
        string nip)
    {
        // 1. Get challenge with timestamp
        AuthenticationChallengeResponse challenge = await ksefClient.GetAuthChallengeAsync();
        long timestampMs = challenge.Timestamp.ToUnixTimeMilliseconds();

        // 2. Encrypt "token|timestamp" with KSeF public key
        string tokenWithTimestamp = $"{ksefToken}|{timestampMs}";
        byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenWithTimestamp);
        byte[] encrypted = cryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes);
        string encryptedTokenB64 = Convert.ToBase64String(encrypted);

        // 3. Build authentication request (no certificate needed!)
        AuthenticationKsefTokenRequest request = new AuthenticationKsefTokenRequest
        {
            Challenge = challenge.Challenge,
            ContextIdentifier = new AuthenticationTokenContextIdentifier
            {
                Type = AuthenticationTokenContextIdentifierType.Nip,
                Value = nip
            },
            EncryptedToken = encryptedTokenB64
        };

        // 4. Submit authentication request
        SignatureResponse signature = await ksefClient.SubmitKsefTokenAuthRequestAsync(
            request,
            CancellationToken.None
        );

        // 5. Poll for authentication completion
        AuthStatus status;
        do
        {
            status = await ksefClient.GetAuthStatusAsync(
                signature.ReferenceNumber,
                signature.AuthenticationToken.Token
            );

            if (status.Status.Code == 100)
                await Task.Delay(1000);
        }
        while (status.Status.Code == 100);

        if (status.Status.Code != 200)
            throw new Exception($"Authentication failed: {status.Status.Description}");

        // 6. Get access token
        AuthenticationOperationStatusResponse tokens = await ksefClient.GetAccessTokenAsync(signature.AuthenticationToken.Token);
        return tokens.AccessToken.Token;
    }

    private static async Task<OpenOnlineSessionResponse> OpenSession(
        IKSeFClient ksefClient,
        ICryptographyService cryptographyService,
        string accessToken)
    {
        Core.Models.Sessions.EncryptionData encryptionData = cryptographyService.GetEncryptionData();

        OpenOnlineSessionRequest request = OpenOnlineSessionRequestBuilder
            .Create()
            .WithFormCode("FA (2)", "1-0E", "FA")
            .WithEncryption(
                encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                encryptionData.EncryptionInfo.InitializationVector
            )
            .Build();

        return await ksefClient.OpenOnlineSessionAsync(
            request,
            accessToken,
            CancellationToken.None
        );
    }
}
