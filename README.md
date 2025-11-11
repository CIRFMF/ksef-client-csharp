# **KSeF Client**

## Wstęp – Struktura projektu i technologie

Repozytorium zawiera:

- **Implementacja klienta KSeF 2.0**
  - **KSeF.Client** - główna biblioteka klienta z logiką biznesową
  - **KSeF.Client.Core** - modele, interfejsy i wyjątki (wydzielone dla zgodności z .NET Standard 2.0)
- **Testy**
  - **KSeF.Client.Tests** - testy jednostkowe i funkcjonalne
  - **KSeF.Client.Tests.Core** - testy E2E i integracyjne
  - **KSeF.Client.Tests.Utils** - narzędzia pomocnicze do testów
  - **KSeF.Client.Tests.CertTestApp** - aplikacja konsolowa dla zobrazowania tworzenia przykładowego, testowego certyfikatu oraz podpisu XAdES
- **Przykładowa aplikacja**
  - **KSeF.DemoWebApp** - aplikacja demonstracyjna ASP.NET

Całość napisana jest w języku **C#** z wykorzystaniem platformy **.NET 8/9** (lub nowszej). Do komunikacji HTTP wykorzystywany jest RestClient, a rejestracja i konfiguracja klienta KSeF odbywa się przez mechanizm Dependency Injection (Microsoft.Extensions.DependencyInjection).

## Struktura projektu

### KSeF.Client

Zawiera implementację komunikacji z API KSeF oraz logikę biznesową:

- **Api/**  
  - **Builders/** - buildery do konstrukcji requestów API (Auth, Certificates, Permissions, Sessions, itp.)
  - **Services/** - serwisy biznesowe (AuthCoordinator, CryptographyService, SignatureService, TokenService, QrCodeService, VerificationLinkService)

- **Clients/**  
  Implementacje klientów specjalizowanych (CryptographyClient, KSeFClient)

- **DI/**  
  Konfiguracja Dependency Injection i opcje klienta (KSeFClientOptions, CryptographyClientOptions, ServiceCollectionExtensions)

- **Extensions/**  
  Metody rozszerzające (Base64UrlExtensions)

- **Http/**  
  Implementacja komunikacji HTTP (KSeFClient, RestClient, JsonUtil)

### KSeF.Client.Core

Biblioteka zawierająca wspólne typy i interfejsy:

- **Exceptions/**  
  Wyjątki specyficzne dla KSeF (KsefApiException, KsefRateLimitException)

- **Interfaces/**  
  Interfejsy usług i klientów (IKSeFClient, ICryptographyService, ISignatureService, IAuthCoordinator, IQrCodeService, IVerificationLinkService)

- **Models/**  
  Modele danych odpowiadające strukturom API KSeF (Authorization, Certificates, Invoices, Sessions, Permissions, Token, QRCode, Peppol)

- **KsefNumberValidator.cs**  
  Walidator numerów faktury nadawanych przez KSeF


## Instalacja i konfiguracja

Aby użyć biblioteki KSeF.Client w swoim projekcie, dodaj referencję do projektu **KSeF.Client** (zawiera on już referencję do KSeF.Client.Core).

### Pakiety Nuget 

Projekty KSeF.Client i KSeF.Client.Core są dostępne jako pakiety nuget w GitHub Packages organizacji CIRFMF.

Opis paczek:
* KSeF.Client - główna biblioteka klienta z logiką biznesową
* KSeF.Client.Core - modele, interfejsy i wyjątki 

Należy najpierw skonfigurować dostęp do paczek NuGet opublikowanych w GitHub Packages organizacji CIRFMF.
Wymaga to autoryzacji przy pomocy osobistego tokena dostępu (Personal Access Token – PAT) z uprawnieniem read:packages.
Dokładny poradnik jest dostępny w pliku [*nuget-packege.md*](https://github.com/CIRFMF/ksef-client-csharp/blob/main/nuget-packege.md).

### Przykładowa rejestracja klienta KSeF w kontenerze DI

#### Minimalna konfiguracja

```csharp
using KSeF.Client.DI;

WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Rejestracja klienta KSeF
builder.Services.AddKSeFClient(options =>
{
    options.BaseUrl = KsefEnvironmentsUris.TEST; // lub PRODUCTION, DEMO
});

// Rejestracja serwisu kryptograficznego (wymagane dla operacji wymagających szyfrowania)
builder.Services.AddCryptographyClient(options =>
{
    options.WarmupOnStart = WarmupMode.NonBlocking;
});
```

#### Pełna konfiguracja z dodatkowymi opcjami

Konfiguracja może być wczytana z pliku `appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://ksef-test.mf.gov.pl",
    "CustomHeaders": { 
      "X-Custom-Header": "value"
    }
  }
}
```

```csharp
using KSeF.Client.DI;
using KSeF.Client.Core.Interfaces.Clients;

WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Wczytanie konfiguracji z appsettings.json
KSeFClientOptions? apiSettings = builder.Configuration.GetSection("ApiSettings").Get<KSeFClientOptions>();

// Rejestracja klienta KSeF z konfiguracją z appsettings
builder.Services.AddKSeFClient(options =>
{
    options.BaseUrl = apiSettings?.BaseUrl ?? KsefEnvironmentsUris.TEST;
    options.WebProxy = apiSettings?.WebProxy; // opcjonalnie: konfiguracja proxy
    options.CustomHeaders = apiSettings?.CustomHeaders ?? new Dictionary<string, string>();
});

// Rejestracja klienta kryptograficznego z custom fetcher
builder.Services.AddCryptographyClient(
    options =>
    {
        options.WarmupOnStart = WarmupMode.NonBlocking;
    },
    // Delegat pobierający certyfikaty - będzie wywołany przez bibliotekę, nie od razu
    pemCertificatesFetcher: async (serviceProvider, cancellationToken) =>
    {
        KSeF.Client.Core.Interfaces.Services.ICryptographyClient cryptographyClient = serviceProvider.GetRequiredService<ICryptographyClient>();
        return await cryptographyClient.GetPublicCertificatesAsync(cancellationToken);
    });

// rejestracja rozszerzeń ECDSA dla podpisów XML-DSIG/XAdES
CryptoConfig.AddAlgorithm(typeof(Ecdsa256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256");
```

**Uwaga:** `AddCryptographyClient` jest wymagany jeśli planujesz używać operacji wymagających szyfrowania (np. sesje wsadowe, eksport faktur).

### Przykład użycia

```csharp
// Wstrzyknięcie klienta przez DI
public class InvoiceService
{
    private readonly IKSeFClient _ksefClient;
    private readonly ICryptographyService _cryptographyService;

    public InvoiceService(IKSeFClient ksefClient, ICryptographyService cryptographyService)
    {
        _ksefClient = ksefClient;
        _cryptographyService = cryptographyService;
    }

    // pełna implementacja np. WebApplication.Controllers.OnlineSessionController
    // Wysłanie faktury w sesji interaktywnej
    public async Task<string> SendInvoiceAsync(string invoiceXml, string sessionReferenceNumber, string accessToken)
    {
        // ...
        SendInvoiceResponse response = await _ksefClient.SendOnlineSessionInvoiceAsync(
            request,
            sessionReferenceNumber,
            accessToken,
            CancellationToken.None);
        
        return response.ReferenceNumber;
    }
}
```

## Testowanie

Projekt zawiera rozbudowany zestaw testów:

- **Testy E2E i integracyjne** (`KSeF.Client.Tests.Core`)
- **Testy jednostkowe i funkcjonalne** (`KSeF.Client.Tests`)
- **Narzędzia testowe** (`KSeF.Client.Tests.Utils`)

### Uruchomienie testów

```bash
dotnet test KSeF.Client.sln
```

## Więcej informacji

Szczegółowe przykłady użycia i dokumentacja API dostępne są w [repozytorium dokumentacji](https://github.com/CIRFMF/ksef-docs).
