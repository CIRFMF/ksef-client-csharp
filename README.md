# **KSeF Client**

## Wstęp – Struktura projektu i technologie

Repozytorium zawiera:

- **Implementacja klienta KSeF 2.0**
  - **KSeF.Client** - główna biblioteka klienta z logiką biznesową
  - **KSeF.Client.Core** - modele, interfejsy i wyjątki (wydzielone dla zgodności z .NET Standard 2.0)
  - **KSeF.Client.ClientFactory** - dodatkowa biblioteka klienta która umożliwia korzystanie z KSeFClient, CertificateFetcherServices oraz CryptographyServices w formie fabryk dla 3 niezależnych środowisk.

- **Testy**
  - **KSeF.Client.Tests** - testy jednostkowe i funkcjonalne
  - **KSeF.Client.Tests.Core** - testy E2E i integracyjne
  - **KSeF.Client.Tests.ClientFactory** - testy dla fabryk KSeFClient, CryptographyServices i CertificateFetcherServices
  - **KSeF.Client.Tests.Utils** - narzędzia pomocnicze do testów
  - **KSeF.Client.Tests.CertTestApp** - aplikacja konsolowa dla zobrazowania tworzenia przykładowego, testowego certyfikatu oraz podpisu XAdES
- **Przykładowa aplikacja**
  - **KSeF.DemoWebApp** - aplikacja demonstracyjna ASP.NET

Całość napisana jest w języku **C#** z wykorzystaniem platformy **.NET 8/9/10** (lub nowszej). Biblioteki **KSeF.Client** i **KSeF.Client.ClientFactory** wspierają również **netstandard2.0**, co zapewnia pełną kompatybilność z **.NET Framework 4.7.2+/4.8**. Do komunikacji HTTP wykorzystywany jest RestClient, a rejestracja i konfiguracja klienta KSeF odbywa się przez mechanizm Dependency Injection (Microsoft.Extensions.DependencyInjection).

### Wspierane platformy

| Biblioteka | Target Frameworks |
|---|---|
| **KSeF.Client** | `netstandard2.0` · `net8.0` · `net9.0` · `net10.0` |
| **KSeF.Client.Core** | `netstandard2.0` |
| **KSeF.Client.ClientFactory** | `netstandard2.0` · `net8.0` · `net9.0` · `net10.0` |

Dzięki `netstandard2.0` biblioteka jest w pełni kompatybilna z:
- **.NET Framework 4.7.2+** / **.NET Framework 4.8** (wymaga Windows)
- **.NET Core 2.0+**
- **.NET 5+**
- **Mono 5.4+**
- **Xamarin**

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
Dokładny poradnik jest dostępny w pliku [*nuget-package.md*](https://github.com/CIRFMF/ksef-client-csharp/blob/main/nuget-package.md).

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
builder.Services.AddCryptographyClient();
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
// Rejestracja własnej implementacji ICertificateFetcher.
builder.Services.AddSingleton<ICertificateFetcher, MyCertificateFetcher>();
// Rejestracja klienta kryptograficznego KSeF w trybie NonBlocking.
// (automatycznie użyje powyższego MyCertificateFetcher).
builder.Services.AddCryptographyClient(CryptographyServiceWarmupMode.NonBlocking);
```

**Uwaga:** `AddCryptographyClient` jest wymagany dla operacji wymagających szyfrowania (np. sesje wsadowe, eksport faktur).

**Uwaga:** `AddCryptographyClient` rejestruje serwis kryptograficzny `CryptographyService` oraz zapewnia dodanie algorytmu kryptograficznego dla ECDSA z SHA-256 do klasy `CryptoConfig`.

#### Użycie `AddCryptographyClient` z przekazaniem własnego delegata (np. `GetCertificates()`) z własnego serwisu (np. `MyCertificateService`); Ten sposób inicjalizacji nie jest polecany.
```csharp
// Rejestracja serwisu w kontenerze DI
builder.Services.AddSingleton<IMyCCertificateService, MyCertificateService>();

// Przekazanie delegata do AddCryptographyClient
builder.Services.AddCryptographyClient(
    pemCertificatesFetcher: async (cancellationToken) =>
    {
        // Pobranie serwisu z kontenera DI
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var myFetcher = scope.ServiceProvider.GetRequiredService<IMyCertificateService>();
        
        // Wywołanie metody z pobranego serwisu
        return await myFetcher.GetCertificates(cancellationToken);
    },
    warmupMode: CryptographyServiceWarmupMode.NonBlocking
);
```
**Uwaga:** Sygnatura metody `GetCertificates()` musi zwracać `Task<ICollection<PemCertificateInfo>>` i przyjmować `CancellationToken` jako parametr. Tworzenie scope'a jest konieczne, ponieważ delegat będzie wywołany później, a nie od razu podczas konfiguracji. 

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
# Wszystkie testy na domyślnych TFM
dotnet test KSeF.Client.sln

# Testy na .NET Framework 4.8 (wymaga Windows lub Mono)
dotnet test KSeF.Client.sln --framework net48

# Testy na konkretnym TFM
dotnet test KSeF.Client.sln --framework net8.0
dotnet test KSeF.Client.sln --framework net9.0
dotnet test KSeF.Client.sln --framework net10.0
```

Projekty testowe wspierają: `net48` · `net8.0` · `net9.0` · `net10.0`.
## Rozwiązywanie problemów (Troubleshooting)

### Błąd: "The password may be incorrect" na IIS / Azure

Jeśli podczas ładowania certyfikatu z kluczem prywatnym otrzymujesz wyjątek:

> **System.Security.Cryptography.CryptographicException:** The EncryptedPrivateKeyInfo structure was decoded but was not successfully interpreted, the password may be incorrect.

Oznacza to zazwyczaj problem środowiskowy (brak załadowanego profilu użytkownika na IIS/Azure), a nie błędne hasło. System Windows próbuje zapisać tymczasowy klucz na dysku, ale nie ma do tego uprawnień.

#### Rozwiązanie

Zamiast standardowego `X509Certificate2.CreateFromEncryptedPem`, użyj metody rozszerzającej `X509Certificate2.MergeWithPemKey` dostępnej w bibliotece, która ładuje klucz w pamięci (Ephemeral Key):
```
using System.Security.Cryptography.X509Certificates;
using KSeF.Client.Extensions;

// 1. Wczytaj certyfikat publiczny i treść klucza PEM
X509Certificate2 publicCert = new X509Certificate2("cert.cer");
string privateKeyPem = File.ReadAllText("key.pem");

// 2. Połącz bezpiecznie (działa na Azure/IIS bez LoadUserProfile)
X509Certificate2 cert = publicCert.MergeWithPemKey(privateKeyPem, "TwojeHaslo");
```

### Generowania kodów QR na środowisku Linux / Błąd: wyjątek inicjalizacji SkiaSharp (`libfontconfig.so.1`)

Jeśli podczas generowania kodów QR pojawia się wyjątek podobny do:

> **TypeInitializationException:** The type initializer for `SkiaSharp.SKImageInfo` threw an exception  
> `libfontconfig.so.1: cannot open shared object file`

Oznacza to brak systemowej biblioteki `libfontconfig` w środowisku uruchomieniowym (np. w najnowszych obrazach Docker `aspnet/runtime`).

SkiaSharp korzysta z natywnych bibliotek, których obecność jest wymagana już na etapie inicjalizacji i nie może być zapewniona po stronie biblioteki aplikacyjnej.

#### Rozwiązanie

Doinstalować bibliotekę `libfontconfig` w środowisku uruchomieniowym, np. w obrazie Docker:

```
dockerfile
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 \
 && rm -rf /var/lib/apt/lists/*
 ```

## Więcej informacji

Szczegółowe przykłady użycia i dokumentacja API dostępne są w [repozytorium dokumentacji](https://github.com/CIRFMF/ksef-docs).
