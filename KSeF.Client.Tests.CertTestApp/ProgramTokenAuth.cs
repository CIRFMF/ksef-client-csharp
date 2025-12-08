using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Services;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.DI;
using KSeF.Client.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Tests.TokenAuthApp;

/// <summary>
/// Demonstration of KSeF authentication using a pre-generated KSeF token.
/// NO CERTIFICATE REQUIRED - just insert your KSeF token below!
/// </summary>
public class Program
{
    // ============================================
    // CONFIGURATION - INSERT YOUR TOKEN HERE
    // ============================================
    private const string KSEF_TOKEN = "20251117-EC-1F7F420000-A32F86EE05-85|nip-5272689745|a3d6a7bcd40545ca96a12ffc360f5dc892cfdd79a1f64a9a96b017b805117f01"; // <--- CHANGE THIS to your KSeF token
    private const string NIP_NUMBER = "5272689745"; // <--- CHANGE THIS to your NIP

    public static async Task Main(string[] args)
    {
        Console.WriteLine("KSeF.Client - Token-Based Authentication Demonstration");
        Console.WriteLine("========================================================\n");

        // 0) Setup DI and client configuration
        ServiceCollection services = new ServiceCollection();
        services.AddKSeFClient(options =>
        {
            options.BaseUrl = KsefEnvironmentsUris.DEMO; // Use DEMO environment for testing
        });

        // Register cryptography services
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

        // Start cryptography service
        scope.ServiceProvider.GetRequiredService<CryptographyWarmupHostedService>()
           .StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        IKSeFClient ksefClient = provider.GetRequiredService<IKSeFClient>();
        ISignatureService signatureService = provider.GetRequiredService<ISignatureService>();
        ICryptographyService cryptographyService = provider.GetRequiredService<ICryptographyService>();


        try
        {
            // Validate configuration
            if (KSEF_TOKEN == "YOUR_KSEF_TOKEN_HERE")
            {
                Console.WriteLine("ERROR: Please set your KSeF token in the KSEF_TOKEN constant!");
                Console.WriteLine("Update the constant at the top of Program.cs");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Using NIP: {NIP_NUMBER}");
            Console.WriteLine($"Using KSeF Token: {KSEF_TOKEN[..10]}...\n");

            // Step 1: Authenticate with KSeF token (NO CERTIFICATE!)
            Console.WriteLine("[1] Authenticating with KSeF token (no certificate required)...");
            (string accessToken, string referenceNumber, string authToken) authResult = await AuthenticateWithKsefTokenAsync(
                ksefClient,
                cryptographyService,
                KSEF_TOKEN,
                NIP_NUMBER
            );
            string accessToken = authResult.accessToken;
            string referenceNumber = authResult.referenceNumber;
            string authToken = authResult.authToken;
            Console.WriteLine($"    ✓ Access Token obtained: {accessToken[..20]}...{accessToken[^10..]}");
            Console.WriteLine($"    ✓ Reference Number: {referenceNumber}");

            // Step 2: Call GET /api/v2/auth/:referenceNumber to retrieve authentication details
            Console.WriteLine("\n[2] Calling GET /api/v2/auth/:referenceNumber...");
            await GetAuthenticationDetailsAsync(ksefClient, referenceNumber, authToken);

            // Step 3: Use the access token to open an online session
            Console.WriteLine("\n[3] Opening online session...");
            (OpenOnlineSessionResponse sessionResponse, EncryptionData encryptionData) = await OpenOnlineSessionAsync(
                ksefClient,
                cryptographyService,
                accessToken
            );

            Console.WriteLine($"    Session Reference Number: {sessionResponse.ReferenceNumber}");
            
            // Step 4: Get session status
            Console.WriteLine("\n[4] Checking session status...");
            SessionStatusResponse sessionStatus = await ksefClient.GetSessionStatusAsync(
                sessionResponse.ReferenceNumber,
                accessToken,
                CancellationToken.None
            );
            Console.WriteLine($"    Status: {sessionStatus.Status.Code} - {sessionStatus.Status.Description}");
            Console.WriteLine($"    Invoices Count: {sessionStatus.InvoiceCount}");

            // Step 5: Send XML invoice to KSeF
            Console.WriteLine("\n[5] Sending XML invoice to KSeF...");
            const string INVOICE_XML_PATH = "C:\\Users\\jesper.madsen\\Downloads\\FA_3_Przykład_1_Logstor (2).xml";

            if (!File.Exists(INVOICE_XML_PATH))
            {
                Console.WriteLine($"    ⚠ Invoice file not found at: {INVOICE_XML_PATH}");
                Console.WriteLine("    Please update INVOICE_XML_PATH constant with the correct path to your XML invoice file.");
            }
            else
            {
                Console.WriteLine($"    Reading invoice from: {INVOICE_XML_PATH}");
                string invoiceXml = await File.ReadAllTextAsync(INVOICE_XML_PATH);
                Console.WriteLine($"    Invoice XML size: {invoiceXml.Length} characters");

                // Encrypt the invoice with the session encryption keys
                Console.WriteLine("    Encrypting invoice...");
                byte[] invoiceBytes = Encoding.UTF8.GetBytes(invoiceXml);
                byte[] encryptedInvoice = cryptographyService.EncryptBytesWithAES256(
                    invoiceBytes,
                    encryptionData.CipherKey,
                    encryptionData.CipherIv
                );


                // Calculate metadata for both original and encrypted invoice
                FileMetadata invoiceMetadata = cryptographyService.GetMetaData(invoiceBytes);
                FileMetadata encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

                Console.WriteLine($"    Original invoice hash: {invoiceMetadata.HashSHA}");
                Console.WriteLine($"    Encrypted invoice hash: {encryptedInvoiceMetadata.HashSHA}");

                // Build the request using the builder
                SendInvoiceRequest sendInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
                    .Create()
                    .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
                    .WithEncryptedDocumentHash(encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
                    .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
                    .Build();

                Console.WriteLine("    Submitting invoice to KSeF...");
                SendInvoiceResponse response = await ksefClient.SendOnlineSessionInvoiceAsync(
                    sendInvoiceRequest,
                    sessionResponse.ReferenceNumber,
                    accessToken,
                    CancellationToken.None
                );

                Console.WriteLine($"\n    [Invoice Submission Response]");
                Console.WriteLine($"    ✓ Invoice submitted successfully!");
                Console.WriteLine($"    Invoice Reference Number: {response.ReferenceNumber}");

                // Step 5.1: Check the invoice status and wait for KSeF number
                Console.WriteLine("\n    [5.1] Waiting for invoice to be processed and assigned a KSeF number...");
                SessionInvoice invoiceDetails = null;
                DateTime startWait = DateTime.UtcNow;
                TimeSpan maxWaitTime = TimeSpan.FromMinutes(2);
                int attemptCount = 0;

                while (string.IsNullOrEmpty(invoiceDetails?.KsefNumber) && (DateTime.UtcNow - startWait) < maxWaitTime)
                {
                    attemptCount++;
                    invoiceDetails = await ksefClient.GetSessionInvoiceAsync(
                        sessionResponse.ReferenceNumber,
                        response.ReferenceNumber,
                        accessToken,
                        CancellationToken.None
                    );

                    Console.WriteLine($"    Attempt {attemptCount}: Status = {invoiceDetails.Status.Code} - {invoiceDetails.Status.Description}");

                    if (string.IsNullOrEmpty(invoiceDetails.KsefNumber))
                    {
                        Console.WriteLine($"    KSeF Number not assigned yet, waiting 2 seconds... (elapsed: {DateTime.UtcNow - startWait:mm\\:ss})");
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    else
                    {
                        Console.WriteLine($"    ✓ KSeF Number assigned: {invoiceDetails.KsefNumber}");
                        break;
                    }
                }

                if (string.IsNullOrEmpty(invoiceDetails?.KsefNumber))
                {
                    Console.WriteLine($"    ⚠ Timeout: KSeF number not assigned within {maxWaitTime.TotalMinutes} minutes");
                    Console.WriteLine($"    Final Status: {invoiceDetails?.Status.Code} - {invoiceDetails?.Status.Description}");
                }
                else
                {
                    Console.WriteLine($"\n    [Invoice Details]");
                    Console.WriteLine($"    Invoice Status Code: {invoiceDetails.Status.Code}");
                    Console.WriteLine($"    Invoice Status Description: {invoiceDetails.Status.Description}");
                    Console.WriteLine($"    KSeF Number: {invoiceDetails.KsefNumber}");
                }

                // Step 5.2: Get the UPO (Urzędowe Poświadczenie Odbioru) if available
                if (!string.IsNullOrEmpty(invoiceDetails.KsefNumber))
                {
                    Console.WriteLine("\n    [5.2] Retrieving UPO (Official Confirmation of Receipt)...");
                    string upo = await ksefClient.GetSessionInvoiceUpoByReferenceNumberAsync(
                        sessionResponse.ReferenceNumber,
                        response.ReferenceNumber,
                        accessToken,
                        CancellationToken.None
                    );
                    Console.WriteLine($"    UPO XML length: {upo.Length} characters");
                    Console.WriteLine($"    UPO preview (first 200 chars): {upo.Substring(0, Math.Min(200, upo.Length))}...");
                }
                else
                {
                    Console.WriteLine("\n    [5.2] UPO not available yet - invoice may still be processing");
                }
            }

            // Step 6: Query received invoices (invoices sent to us)
            Console.WriteLine("\n[6] Querying invoices received by our NIP...");
            await QueryReceivedInvoicesAsync(ksefClient, accessToken);

            // Step 7: Close the session
            Console.WriteLine("\n[7] Closing session...");
            await ksefClient.CloseOnlineSessionAsync(
                sessionResponse.ReferenceNumber,
                accessToken,
                CancellationToken.None
            );
            Console.WriteLine("    Session closed successfully");

            Console.WriteLine("\n✓ Demonstration completed successfully!");
            Console.WriteLine("\nKey Points:");
            Console.WriteLine("  - No external certificate needed (test certificate generated)");
            Console.WriteLine("  - Access token can be reused for multiple operations");
            Console.WriteLine("  - Token is valid for the session lifetime");
            Console.WriteLine("  - This approach works for all KSeF operations");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error occurred: {ex.Message}");
            Console.WriteLine($"\nFull error:\n{ex}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Authenticates with KSeF using ONLY a KSeF token - NO CERTIFICATE REQUIRED!
    /// This is the simple token-based authentication method.
    /// Returns: (accessToken, referenceNumber, authenticationToken)
    /// </summary>
    private static async Task<(string accessToken, string referenceNumber, string authToken)> AuthenticateWithKsefTokenAsync(
        IKSeFClient ksefClient,
        ICryptographyService cryptographyService,
        string ksefToken,
        string nip)
    {
        Console.WriteLine("    [1.1] Getting challenge from KSeF...");
        AuthenticationChallengeResponse challenge = await ksefClient.GetAuthChallengeAsync();
        long timestampMs = challenge.Timestamp.ToUnixTimeMilliseconds();
        Console.WriteLine($"          Challenge: {challenge.Challenge}");
        Console.WriteLine($"          Timestamp: {timestampMs}");

        Console.WriteLine("    [1.2] Encrypting token with KSeF public key...");
        // Format: "token|timestamp" encrypted with RSA-OAEP SHA-256
        string tokenWithTimestamp = $"{ksefToken}|{timestampMs}";
        byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenWithTimestamp);
        byte[] encrypted = cryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes);
        string encryptedTokenB64 = Convert.ToBase64String(encrypted);

        Console.WriteLine("    [1.3] Building authentication request...");
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

        Console.WriteLine("    [1.4] Submitting KSeF token authentication...");
        SignatureResponse signature = await ksefClient.SubmitKsefTokenAuthRequestAsync(
            request,
            CancellationToken.None
        );
        Console.WriteLine($"          Reference Number: {signature.ReferenceNumber}");

        Console.WriteLine("    [1.5] Waiting for authentication to complete...");
        AuthStatus status;

        status = await ksefClient.GetAuthStatusAsync(
            signature.ReferenceNumber,
            signature.AuthenticationToken.Token
        );


        if (status.Status.Code != 200)
        {
            throw new Exception($"Authentication failed: {status.Status.Code} - {status.Status.Description}");
        }

        Console.WriteLine("    [1.6] Retrieving access token...");
        AuthenticationOperationStatusResponse tokens = await ksefClient.GetAccessTokenAsync(
            signature.AuthenticationToken.Token
        );

        string accessToken = tokens.AccessToken?.Token ?? throw new Exception("Access token is null");
        return (accessToken, signature.ReferenceNumber, signature.AuthenticationToken.Token);
    }

    /// <summary>
    /// Retrieves authentication details by calling GET /api/v2/auth/:referenceNumber
    /// </summary>
    private static async Task GetAuthenticationDetailsAsync(
        IKSeFClient ksefClient,
        string referenceNumber,
        string authenticationToken)
    {
        Console.WriteLine($"    Calling: GET /api/v2/auth/{referenceNumber}");

        AuthStatus authDetails = await ksefClient.GetAuthStatusAsync(
            referenceNumber,
            authenticationToken
        );

        Console.WriteLine($"\n    === Authentication Details Response ===");
        Console.WriteLine($"    Status Code: {authDetails.Status.Code}");
        Console.WriteLine($"    Status Description: {authDetails.Status.Description}");
        Console.WriteLine($"    Start Date: {authDetails.StartDate}");
        Console.WriteLine($"    Authentication Method: {authDetails.AuthenticationMethod}");
        Console.WriteLine($"    ========================================\n");
    }

    /// <summary>
    /// Opens an online session using the access token.
    /// Returns both the session response and the encryption data needed for invoice submission.
    /// </summary>
    private static async Task<(OpenOnlineSessionResponse response, EncryptionData encryptionData)> OpenOnlineSessionAsync(
        IKSeFClient ksefClient,
        ICryptographyService cryptographyService,
        string accessToken)
    {
        EncryptionData encryptionData = cryptographyService.GetEncryptionData();

        OpenOnlineSessionRequest request = OpenOnlineSessionRequestBuilder
            .Create()
            .WithFormCode(systemCode: "FA (3)", schemaVersion: "1-0E", value: "FA") // FA3 format
            .WithEncryption(
                encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                initializationVector: encryptionData.EncryptionInfo.InitializationVector
            )
            .Build();

        OpenOnlineSessionResponse response = await ksefClient.OpenOnlineSessionAsync(request, accessToken, CancellationToken.None);
        return (response, encryptionData);
    }

    /// <summary>
    /// Query invoices received by our NIP (Subject2 = invoices where we are the buyer)
    /// </summary>
    private static async Task QueryReceivedInvoicesAsync(IKSeFClient ksefClient, string accessToken)
    {
        // Subject2 = invoices where our NIP is the buyer (received invoices)
        InvoiceQueryFilters filters = new InvoiceQueryFilters
        {
            SubjectType = InvoiceSubjectType.Subject2, // We are the buyer
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddDays(-30), // Last 30 days
                To = DateTime.UtcNow,
                DateType = DateType.Issue
            }
        };

        Console.WriteLine("    Searching for invoices where we are the buyer (last 30 days)...");
        
        PagedInvoiceResponse result = await ksefClient.QueryInvoiceMetadataAsync(
            requestPayload: filters,
            accessToken: accessToken,
            pageOffset: 0,
            pageSize: 10,
            sortOrder: SortOrder.Desc,
            cancellationToken: CancellationToken.None
        );

        Console.WriteLine($"\n    Found {result.Invoices.Count} numbers of invoices");
        Console.WriteLine($"    has more: {result.HasMore}");

        if (result.Invoices != null && result.Invoices.Any())
        {
            Console.WriteLine($"\n    ✓ Retrieved {result.Invoices.Count()} invoice(s):");
            
            foreach (InvoiceSummary? invoice in result.Invoices)
            {
                Console.WriteLine($"\n    ----------------------------------------");
                Console.WriteLine($"    KSeF Number: {invoice.KsefNumber}");
                Console.WriteLine($"    Invoice Number: {invoice.InvoiceNumber}");
                Console.WriteLine($"    Seller NIP: {invoice.Seller.Nip}");
                Console.WriteLine($"    Seller Name: {invoice.Seller.Name}");
                Console.WriteLine($"    Gross Amount: {invoice.GrossAmount} {invoice.Currency}");
                Console.WriteLine($"    Issue Date: {invoice.IssueDate}");
                Console.WriteLine($"    Acquisition Date: {invoice.AcquisitionDate}");

                // Download the full invoice XML
                Console.WriteLine($"\n    Downloading invoice XML for {invoice.KsefNumber}...");
                try
                {
                    string invoiceXml = await ksefClient.GetInvoiceAsync(
                        invoice.KsefNumber,
                        accessToken,
                        CancellationToken.None
                    );
                    Console.WriteLine($"    ✓ Downloaded XML ({invoiceXml.Length} characters)");
                    
                    // Save invoice to disk
                    string downloadDir = @"c:\Users\jesper.madsen\Downloads\ReceivedInvoices";
                    Directory.CreateDirectory(downloadDir); // Create directory if it doesn't exist
                    
                    string fileName = $"{invoice.KsefNumber}_{invoice.InvoiceNumber?.Replace("/", "-")}_{invoice.IssueDate:yyyyMMdd}.xml";
                    string filePath = Path.Combine(downloadDir, fileName);
                    
                    // Check if file already exists to avoid duplicate downloads
                    if (File.Exists(filePath))
                    {
                        Console.WriteLine($"    ⊘ Already exists: {fileName} (skipped)");
                    }
                    else
                    {
                        await File.WriteAllTextAsync(filePath, invoiceXml);
                        Console.WriteLine($"    ✓ Saved to: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ✗ Failed to download invoice: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("\n    No invoices found for the specified criteria.");
            Console.WriteLine("    This might mean:");
            Console.WriteLine("    - No invoices have been sent to your NIP in the last 30 days");
            Console.WriteLine("    - You're using a test NIP that hasn't received any invoices");
        }
    }
}
