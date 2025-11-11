using System.Security.Cryptography;
using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Services;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.DI;
using KSeF.Client.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using KSeF.Client.Extensions;

// Zarejestruj rozszerzenia EDCSA
CryptoConfig.AddAlgorithm(typeof(Ecdsa256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256");

// Tryb wyjścia: screen (domyślnie) lub file
string outputMode = ParseOutputMode(args);
Console.WriteLine("KSeF.Client.Tests.CertTestApp – demonstracja procesu uwierzytelnienia XAdES");
Console.WriteLine($"Tryb wyjścia: {outputMode}");

// 0) DI i konfiguracja klienta
ServiceCollection services = new ServiceCollection();
services.AddKSeFClient(options =>
{
    options.BaseUrl = KsefEnvironmentsUris.TEST;
});

// UWAGA! w testach nie używamy AddCryptographyClient tylko rejestrujemy ręcznie, bo on uruchamia HostedService w tle
services.AddSingleton<ICryptographyClient, CryptographyClient>();
services.AddSingleton<ICryptographyService, CryptographyService>(serviceProvider =>
{
    // Definicja domyślnego delegata
    return new CryptographyService(async cancellationToken =>
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ICryptographyClient cryptographyClient = scope.ServiceProvider.GetRequiredService<ICryptographyClient>();
        return await cryptographyClient.GetPublicCertificatesAsync(cancellationToken);
    });
});
// Rejestracja usługi hostowanej (Hosted Service) jako singleton na potrzeby testów
services.AddSingleton<CryptographyWarmupHostedService>();

ServiceProvider provider = services.BuildServiceProvider();

using IServiceScope scope = provider.CreateScope();

// opcjonalne: inicjalizacja lub inne czynności startowe
// Uruchomienie usługi hostowanej w trybie blokującym (domyślnym) na potrzeby testów
scope.ServiceProvider.GetRequiredService<CryptographyWarmupHostedService>()
           .StartAsync(CancellationToken.None).GetAwaiter().GetResult();

IKSeFClient ksefClient = provider.GetRequiredService<IKSeFClient>();
ISignatureService signatureService = provider.GetRequiredService<ISignatureService>();

try
{
    // 1) NIP (z parametru lub losowy)
    Console.WriteLine("[1] Przygotowanie NIP...");
    string? nipArg = ParseNip(args);
    string nip = string.IsNullOrWhiteSpace(nipArg) ? MiscellaneousUtils.GetRandomNip() : nipArg.Trim();
    Console.WriteLine($"    NIP: {nip} {(string.IsNullOrWhiteSpace(nipArg) ? "(losowy)" : "(z parametru)")}");

    // 2) Challenge
    Console.WriteLine("[2] Pobieranie wyzwania (challenge) z KSeF...");
    AuthenticationChallengeResponse challengeResponse = await ksefClient.GetAuthChallengeAsync();
    Console.WriteLine($"    Challenge: {challengeResponse.Challenge}");

    // 3) Budowa AuthTokenRequest
    Console.WriteLine("[3] Budowanie AuthTokenRequest (builder)...");
    AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
        .Create()
        .WithChallenge(challengeResponse.Challenge)
        .WithContext(AuthenticationTokenContextIdentifierType.Nip, nip)
        .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
        .Build();

    // 4) Serializacja do XML
    Console.WriteLine("[4] Serializacja żądania do XML (unsigned)...");
    string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);
    PrintXmlToConsole(unsignedXml, "XML przed podpisem");

    // 5) Samopodpisany certyfikat do podpisu XAdES
    Console.WriteLine("[5] Generowanie samopodpisanego certyfikatu testowego (Utils)...");
    X509Certificate2 certificate = CertificateUtils.GetPersonalCertificate("A", "R", "TINPL", nip, "A R");
    Console.WriteLine($"    Certyfikat: {certificate.Subject}");

    // (5a) Zapis certyfikatu gdy tryb file
    // Eksportowanie: PFX (z kluczem prywatnym) oraz CER (część publiczna)
    string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
    if (outputMode.Equals("file", StringComparison.OrdinalIgnoreCase))
    {
        string certPfxPath = Path.Combine(Environment.CurrentDirectory, $"cert-{timestamp}.pfx");
        string certCerPath = Path.Combine(Environment.CurrentDirectory, $"cert-{timestamp}.cer");

        byte[] pfxBytes = certificate.Export(X509ContentType.Pfx, string.Empty);
        byte[] cerBytes = certificate.Export(X509ContentType.Cert);

        await File.WriteAllBytesAsync(certPfxPath, pfxBytes);
        await File.WriteAllBytesAsync(certCerPath, cerBytes);

        Console.WriteLine($"    Zapisano certyfikat PFX: {certPfxPath}");
        Console.WriteLine($"    Zapisano certyfikat CER: {certCerPath}");
    }

    // 6) Podpis XAdES
    Console.WriteLine("[6] Podpisywanie XML (XAdES)...");
    string signedXml = signatureService.Sign(unsignedXml, certificate);

    // Tryb wyjścia:
    // - file: zapis do pliku (bez wyświetlania XML w konsoli)
    // - screen lub pusty: wyświetlenie XML w konsoli (bez zapisu do pliku)
    if (outputMode.Equals("file", StringComparison.OrdinalIgnoreCase))
    {
        string filePath = Path.Combine(Environment.CurrentDirectory, $"signed-auth-{timestamp}.xml");
        await File.WriteAllTextAsync(filePath, signedXml, Encoding.UTF8);
        Console.WriteLine($"Zapisano podpisany XML: {filePath}");
    }
    else
    {
        PrintXmlToConsole(signedXml, "XML po podpisie (XAdES)");
    }

    // 7) Przesłanie podpisanego XML do KSeF
    Console.WriteLine("[7] Wysyłanie podpisanego XML do KSeF...");
    SignatureResponse submission = await ksefClient.SubmitXadesAuthRequestAsync(signedXml, verifyCertificateChain: false);
    Console.WriteLine($"    ReferenceNumber: {submission.ReferenceNumber}");

    // 8) Odpytanie o status
    Console.WriteLine("[8] Odpytanie o status operacji uwierzytelnienia...");
    DateTime startTime = DateTime.UtcNow;
    TimeSpan timeout = TimeSpan.FromMinutes(2);
    AuthStatus status;
    do
    {
        status = await ksefClient.GetAuthStatusAsync(submission.ReferenceNumber, submission.AuthenticationToken.Token);
        Console.WriteLine($"      Status: {status.Status.Code} - {status.Status.Description} | upłynęło: {DateTime.UtcNow - startTime:mm\\:ss}");
        if (status.Status.Code != 200)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
    while (status.Status.Code == 100 && (DateTime.UtcNow - startTime) < timeout);

    if (status.Status.Code != 200)
    {
        Console.WriteLine("[!] Uwierzytelnienie nie powiodło się lub przekroczono czas oczekiwania.");
        Console.WriteLine($"    Kod: {status.Status.Code}, Opis: {status.Status.Description}");
        return;
    }

    // 9) Pobranie access token
    Console.WriteLine("[9] Pobieranie access token...");
    AuthenticationOperationStatusResponse tokenResponse = await ksefClient.GetAccessTokenAsync(submission.AuthenticationToken.Token);

    string accessToken = tokenResponse.AccessToken?.Token ?? string.Empty;
    string refreshToken = tokenResponse.RefreshToken?.Token ?? string.Empty;
    Console.WriteLine($"    AccessToken: {accessToken}");
    Console.WriteLine($"    RefreshToken: {refreshToken}");

    Console.WriteLine("Zakończono pomyślnie.");
}
catch (Exception ex)
{
    Console.WriteLine("Wystąpił błąd podczas procesu demonstracyjnego.");
    Console.WriteLine(ex.ToString());
}
Console.ReadKey();

static void PrintXmlToConsole(string xml, string title)
{
    Console.WriteLine($"----- {title} -----");
    Console.WriteLine(xml);
    Console.WriteLine($"----- KONIEC: {title} -----\n");
}

static string ParseOutputMode(string[] args)
{
    // akceptowane: --output screen|file
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

static string? ParseNip(string[] args)
{
    // akceptowane: --nip 1111111111
    for (int i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], "--nip", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            return args[i + 1].Trim();
        }
    }
    return null;
}
