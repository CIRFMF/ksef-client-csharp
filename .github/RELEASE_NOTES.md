
## Changelog zmian – `RC3 (2025-08-12)` (KSeF.Client)

### 1. KSeF.Client

#### 1.1 Api/Services
- **CryptographyService.cs**  
  - ➕ Dodano `EncryptWithEciesUsingPublicKey(byte[] content)` — domyślna metoda szyfrowania ECIES (ECDH + AES-GCM) na krzywej P-256.  
  - 🔧 Metodę `EncryptKsefTokenWithRSAUsingPublicKey(...)` można przełączyć na ECIES lub zachować RSA-OAEP SHA-256 przez parametr `EncryptionMethod`.

- **AuthCoordinator.cs**  
  - 🔧 Sygnatura `AuthKsefTokenAsync(...)` rozszerzona o opcjonalny parametr:
    ```csharp
    EncryptionMethod encryptionMethod = EncryptionMethod.Ecies
    ```  
    — domyślnie ECIES, z możliwością fallback do RSA.

#### 1.2 Core/Models
- **EncryptionMethod.cs**  
  ➕ Nowy enum:
  ```csharp
  public enum EncryptionMethod
  {
      Ecies,
      Rsa
  }
  ````
- **InvoiceSummary.cs** 
  ➕ Dodano nowe pola:
  ```csharp
    public DateTimeOffset IssueDate { get; set; }
    public DateTimeOffset InvoicingDate { get; set; }
    public DateTimeOffset PermanentStorageDate { get; set; }
  ```
- **InvoiceMetadataQueryRequest.cs**  
  🔧 w `Seller` oraz `Buyer` odano nowe typy bez pola `Name`:

#### 1.3 Core/Interfaces

* **ICryptographyService.cs**
  ➕ Dodano metody:

  ```csharp
  byte[] EncryptWithEciesUsingPublicKey(byte[] content);
  void EncryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv);
  ```

* **IAuthCoordinator.cs**
  🔧 `AuthKsefTokenAsync(...)` przyjmuje dodatkowy parametr:

  ```csharp
  EncryptionMethod encryptionMethod = EncryptionMethod.Ecies
  ```

---

### 2. KSeF.Client.Tests

* **AuthorizationTests.cs**
  ➕ Testy end-to-end dla `AuthKsefTokenAsync(...)` w wariantach `Ecies` i `Rsa`.

* **QrCodeTests.cs**
  ➕ Rozbudowano testy `BuildCertificateQr` o scenariusze z ECDSA P-256; poprzednie testy RSA pozostawione zakomentowane.

* **VerificationLinkServiceTests.cs**
  ➕ Dodano testy generowania i weryfikacji linków dla certyfikatów ECDSA P-256.

* **BatchSession.cs**
  ➕ Testy end-to-end dla wysyłki partów z wykorzystaniem strumieni.
---

### 3. KSeF.DemoWebApp/Controllers

* **QrCodeController.cs**
  🔧 Akcja `GetCertificateQr(...)` przyjmuje teraz opcjonalny parametr:

  ```csharp
  string privateKey = ""
  ```

  — jeśli nie jest podany, używany jest osadzony klucz w certyfikacie.

---

```
```
> • 🔀 przeniesione

## Rozwiązania zgłoszonych issues  - `2025-07-21`

- **#1 Metoda AuthCoordinator.AuthAsync() zawiera błąd**  
  🔧 `KSeF.Client/Api/Services/AuthCoordinator.cs`: usunięto 2 linie zbędnego kodu challenge 

- **#2 Błąd w AuthController.cs**  
  🔧 `KSeF.DemoWebApp/Controllers/AuthController.cs`: poprawiono logikę `AuthStepByStepAsync` (2 additions, 6 deletions) — fallback `contextIdentifier`

- **#3 „Śmieciowa” klasa XadeSDummy**  
  🔀 Przeniesiono `XadeSDummy` z `KSeF.Client.Api.Services` do `WebApplication.Services` (zmiana namespace)
po
- **#4 Optymalizacja RestClient**  
  🔧 `KSeF.Client/Http/RestClient.cs`: uproszczono przeciążenia `SendAsync` (24 additions, 11 deletions), usunięto dead-code, dodano performance benchmark `perf(#4)` 

- **#5 Uporządkowanie języka komunikatów**  
  ➕ `KSeF.Client/Resources/Strings.en.resx` & `Strings.pl.resx`: dodano 101 nowych wpisów w obu plikach; skonfigurowano lokalizację w DI 

- **#6 Wsparcie dla AOT**  
  ➕ `KSeF.Client/KSeF.Client.csproj`: dodano `<PublishAot>`, `<SelfContained>`, `<InvariantGlobalization>`, runtime identifiers `win-x64;linux-x64;osx-arm64`

- **#7 Nadmiarowy plik KSeFClient.csproj**  
  ➖ Usunięto nieużywany plik projektu `KSeFClient.csproj` z repozytorium

---

## Inne zmiany

- **QrCodeService.cs**: ➕ nowa implementacji PNG-QR (`GenerateQrCode`, `ResizePng`, `AddLabelToQrCode`); 

- **PemCertificateInfo.cs**: ➖ Usunięto właściwości PublicKeyPem; 

- **ServiceCollectionExtensions.cs**: ➕ konfiguracjia lokalizacji (`pl-PL`, `en-US`) i rejestracji `IQrCodeService`/`IVerificationLinkService`
- **AuthTokenRequest.cs**: dostosowanie serializacji XML do nowego schematu XSD
- **README.md**: poprawione środowisko w przykładzie rejestracji KSeFClient w kontenerze DI.
---



## Changelog zmian – `RC2 (2025-07-14)` (KSeF.Client)

> Info: 🔧 zmienione • ➕ dodane • ➖ usunięte

---

## 1. KSeF.Client
Zmiana wersji .NET 8.0 na .NET 9/0

### 1.1 Api/Services
- **AuthCoordinator.cs**: 🔧 Dodano dodatkowy log `Status.Details`; 🔧 dodano wyjątek przy `Status.Code == 400`; ➖ usunięto `ipAddressPolicy`
- **CryptographyService.cs**: ➕ inicjalizacja certyfikatów; ➕ pola `symetricKeyEncryptionPem`, `ksefTokenPem`
- **SignatureService.cs**: 🔧 `Sign(...)` → `SignAsync(...)`
- **QrCodeService.cs**: ➕ nowa usługa do generowania QrCodes
- **VerificationLinkService.cs**: ➕ nowa usługa generowania linków do weryfikacji faktury

### 1.2 Api/Builders
- **SendCertificateEnrollmentRequestBuilder.cs**: 🔧 `ValidFrom` pole zmienione na opcjonalne ; ➖ interfejs `WithValidFrom`
- **OpenBatchSessionRequestBuilder.cs**: 🔧 `WithBatchFile(...)` usunięto parametr `offlineMode`; ➕ `WithOfflineMode(bool)` nopwy opcjonalny krok do oznaczenia trybu offline

### 1.3 Core/Models
- **StatusInfo.cs**: 🔧 dodano property `Details`; ➖ `BasicStatusInfo` - usunięto klase w c elu unifikacji statusów
- **PemCertificateInfo.cs**: ➕ `PublicKeyPem` - dodano nowe property
- **DateType.cs**: ➕ `Invoicing`, `Acquisition`, `Hidden` - dodano nowe emumeratory do filtrowania faktur
- **PersonPermission.cs**: 🔧 `PermissionScope` zmieniono z PermissionType zgodnie ze zmianą w kontrakcie
- **PersonPermissionsQueryRequest.cs**: 🔧 `QueryType` - dodano nowe wymagane property do filtrowania w zadanym kontekście
- **SessionInvoice.cs**: 🔧 `InvoiceFileName` - dodano nowe property 
- **ActiveSessionsResponse.cs** / `Status.cs` / `Item.cs` (Sessions): ➕ nowe modele

### 1.4 Core/Interfaces
- **IKSeFClient.cs**: 🔧 `GetAuthStatusAsync` → zmiana modelu zwracanego z `BasicStatusInfo` na `StatusInfo` 
➕ Dodano metodę GetActiveSessions(accessToken, pageSize, continuationToken, cancellationToken)
➕ Dodano metodę RevokeCurrentSessionAsync(token, cancellationToken)
➕ Dodano metodę RevokeSessionAsync(referenceNumber, accessToken, cancellationToken)
- **ISignatureService.cs**: 🔧 `Sign` → `SignAsync`
- **IQrCodeService.cs**: nowy interfejs do generowania QRcodes 
- **IVerificationLinkService.cs**: ➕ nowy interfejs do tworzenia linków weryfikacyjnych do faktury

### 1.5 DI & Dependencies
- **ServiceCollectionExtensions.cs**: ➕ rejestracja `IQrCodeService`, `IVerificationLinkService`
- **ServiceCollectionExtensions.cs**: ➕ dodano obsługę nowej właściwości `WebProxy` z `KSeFClientOptions`
- **KSeFClientOptions.cs**: 🔧 walidacja `BaseUrl`
- **KSeFClientOptions.cs**: ➕ dodano właściwości `WebProxy` typu `IWebProxy`
➕ Dodano CustomHeaders - umożliwia dodawanie dodatkowych nagłówków do klienta Http
- **KSeF.Client.csproj**: ➕ `QRCoder`, `System.Drawing.Common`

### 1.6 Http
- **KSeFClient.cs**: ➕ nagłówki `X-KSeF-Session-Id`, `X-Environment`; ➕ `Content-Type: application/octet-stream`

### 1.7 RestClient
- **RestClient.cs**: 🔧 `Uproszczona implementacja IRestClient'

### 1.8 Usunięto
- **KSeFClient.csproj.cs**: ➖ `KSeFClient` - nadmiarowy plik projektu, który był nieużywany
---

## 2. KSeF.Client.Tests
**Nowe pliki**: `QrCodeTests.cs`, `VerificationLinkServiceTests.cs`  
Wspólne: 🔧 `Thread.Sleep` → `Task.Delay`; ➕ `ExpectedPermissionsAfterRevoke`; 4-krokowy flow; obsługa 400  
Wybrane: **Authorization.cs**, `EntityPermission*.cs`, **OnlineSession.cs**, **TestBase.cs**

---

## 3. KSeF.DemoWebApp/Controllers
- **QrCodeController.cs**: ➕ `GET /qr/certificate` ➕`/qr/invoice/ksef` ➕`qr/invoice/offline`
- **ActiveSessionsController.cs**: ➕ `GET /sessions/active`
- **AuthController.cs**: ➕ `GET /auth-with-ksef-certificate`; 🔧 fallback `contextIdentifier`
- **BatchSessionController.cs**: ➕ `WithOfflineMode(false)`; 🔧 pętla `var`
- **CertificateController.cs**: ➕ `serialNumber`, `name`; ➕ builder
- **OnlineSessionController.cs**: ➕ `WithOfflineMode(false)` 🔧 `WithInvoiceHash`

---

## 4. Podsumowanie

| Typ zmiany | Liczba plików |
|------------|---------------|
| ➕ dodane   | 12 |
| 🔧 zmienione| 33 |
| ➖ usunięte | 3 |

---
