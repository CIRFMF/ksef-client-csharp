using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Services;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.DI;
using KSeF.Client.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Tests.CertTestApp;

/// <summary>
/// XAdES Certificate Authentication Demonstration
/// Uses certificate with XAdES signature for authentication
/// </summary>
public class Program
{
    private static IConfiguration? _configuration;

    public static async Task Main(string[] args)
    {
        // Load configuration from appsettings.json
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Output mode: screen (default) or file
        string outputMode = ParseOutputMode(args);
        Console.WriteLine("KSeF.Client.Tests.CertTestApp – XAdES authentication process demonstration");
        Console.WriteLine($"Output mode: {outputMode}");

        // 0) DI and client configuration
        ServiceCollection services = new ServiceCollection();
        services.AddKSeFClient(options =>
        {
            options.BaseUrl = KsefEnvironmentsUris.DEMO;
        });

        // NOTE! In tests we don't use AddCryptographyClient, instead we register manually, because it starts a HostedService in the background
        services.AddSingleton<ICryptographyClient, CryptographyClient>();
        services.AddSingleton<ICryptographyService, CryptographyService>(serviceProvider =>
        {
            // Default delegate definition
            return new CryptographyService(async cancellationToken =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ICryptographyClient cryptographyClient = scope.ServiceProvider.GetRequiredService<ICryptographyClient>();
                return await cryptographyClient.GetPublicCertificatesAsync(cancellationToken);
            });
        });
        // Register hosted service as singleton for testing purposes
        services.AddSingleton<CryptographyWarmupHostedService>();

        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();

        // optional: initialization or other startup activities
        // Start hosted service in blocking (default) mode for testing purposes
        scope.ServiceProvider.GetRequiredService<CryptographyWarmupHostedService>()
                   .StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        IKSeFClient ksefClient = provider.GetRequiredService<IKSeFClient>();
        ISignatureService signatureService = provider.GetRequiredService<ISignatureService>();

        try
        {
            // 1) NIP (from parameter or random)
            Console.WriteLine("[1] Preparing NIP...");
            string? nipArg = ParseNip(args);
            string configNip = _configuration?["KSeF:Authentication:NipNumber"] ?? string.Empty;
            string nip = !string.IsNullOrWhiteSpace(nipArg) ? nipArg.Trim() : configNip;
            Console.WriteLine($"    NIP: {nip} {(string.IsNullOrWhiteSpace(nipArg) ? "(from configuration)" : "(from parameter)")}");

            // 2) Challenge
            Console.WriteLine("[2] Retrieving challenge from KSeF...");
            AuthenticationChallengeResponse challengeResponse = await ksefClient.GetAuthChallengeAsync();
            Console.WriteLine($"    Challenge: {challengeResponse.Challenge}");

            // 3) Build AuthTokenRequest
            Console.WriteLine("[3] Building AuthTokenRequest (builder)...");
            // IMPORTANT: For EU certificates (non-Polish), use CertificateSubject
            // For EU/Danish certificates, MUST use CertificateSubject (not CertificateFingerprint)
            AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
                .Create()
                .WithChallenge(challengeResponse.Challenge)
                .WithContext(AuthenticationTokenContextIdentifierType.Nip, nip)
                .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateFingerprint) // Danish certificate requires CertificateSubject
                .Build();

            // 4) Serialize to XML
            Console.WriteLine("[4] Serializing request to XML (unsigned)...");
            string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);
            PrintXmlToConsole(unsignedXml, "XML before signature");

            // 5) Load EU Certificate for XAdES signature <--- MODIFIED STEP
            Console.WriteLine("[5] Loading external certificate from PFX/P12 file...");

            // Load certificate path and password from configuration
            string certificatePath = _configuration?["KSeF:Certificate:FilePath"] ?? string.Empty;
            string certificatePassword = _configuration?["KSeF:Certificate:Password"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(certificatePath) || string.IsNullOrWhiteSpace(certificatePassword))
            {
                throw new InvalidOperationException("Certificate path or password not configured in appsettings.json");
            }

            // IMPORTANT: X509KeyStorageFlags.MachineKeySet can be used for server environments. 
            // X509KeyStorageFlags.Exportable is useful for development.
            X509Certificate2 certificate = new X509Certificate2(
                certificatePath,
                certificatePassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable
            );
            Console.WriteLine($"    Certificate Subject: {certificate.Subject}");
            Console.WriteLine($"    Certificate Thumbprint: {certificate.Thumbprint}");
            // 6) XAdES signature
            Console.WriteLine("[6] Signing XML (XAdES)...");
            string signedXml = signatureService.Sign(unsignedXml, certificate);

            // Output mode
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            if (outputMode.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                string filePath = Path.Combine(Environment.CurrentDirectory, $"signed-auth-{timestamp}.xml");
                await File.WriteAllTextAsync(filePath, signedXml, Encoding.UTF8);
                Console.WriteLine($"Saved signed XML: {filePath}");
            }
            else
            {
                PrintXmlToConsole(signedXml, "XML after signature (XAdES)");
            }

            // 7) Submit signed XML to KSeF
            Console.WriteLine("[7] Sending signed XML to KSeF...");
            SignatureResponse submission = await ksefClient.SubmitXadesAuthRequestAsync(signedXml, verifyCertificateChain: false);
            Console.WriteLine($"    ReferenceNumber: {submission.ReferenceNumber}");

            // 8) Poll for status
            Console.WriteLine("[8] Polling for authentication operation status...");
            DateTime startTime = DateTime.UtcNow;
            TimeSpan timeout = TimeSpan.FromMinutes(2);
            AuthStatus status;
            do
            {
                status = await ksefClient.GetAuthStatusAsync(submission.ReferenceNumber, submission.AuthenticationToken.Token);
                Console.WriteLine($"      Status: {status.Status.Code} - {status.Status.Description} | elapsed: {DateTime.UtcNow - startTime:mm\\:ss}");
                if (status.Status.Code != 200)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            while (status.Status.Code == 100 && (DateTime.UtcNow - startTime) < timeout);

            if (status.Status.Code != 200)
            {
                Console.WriteLine("[!] Authentication failed or timeout exceeded.");
                Console.WriteLine($"    Code: {status.Status.Code}, Description: {status.Status.Description}");
                return;
            }

            // 9) Retrieve access token
            Console.WriteLine("[9] Retrieving access token...");
            AuthenticationOperationStatusResponse tokenResponse = await ksefClient.GetAccessTokenAsync(submission.AuthenticationToken.Token);

            string accessToken = tokenResponse.AccessToken?.Token ?? string.Empty;
            string refreshToken = tokenResponse.RefreshToken?.Token ?? string.Empty;
            Console.WriteLine($"    AccessToken: {accessToken}");
            Console.WriteLine($"    RefreshToken: {refreshToken}");

            Console.WriteLine("Completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred during the demonstration process.");
            Console.WriteLine(ex.ToString());
        }
        Console.ReadKey();
    }

    private static void PrintXmlToConsole(string xml, string title)
    {
        Console.WriteLine($"----- {title} -----");
        Console.WriteLine(xml);
        Console.WriteLine($"----- END: {title} -----\n");
    }

    private static string ParseOutputMode(string[] args)
    {
        // accepted: --output screen|file
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--output", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                string val = args[i + 1].Trim();
                if (val.Equals("file", StringComparison.OrdinalIgnoreCase)) return "file";
                return "screen";
            }
        }
        return "screen";
    }

    private static string? ParseNip(string[] args)
    {
        // accepted: --nip 1111111111
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--nip", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1].Trim();
            }
        }
        return null;
    }
}
