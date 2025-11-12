> Info: 🔧 zmienione • ➕ dodane • ➖ usunięte • 🔀 przeniesione

## Changelog zmian – ## Wersja 2.0.0 RC5.7 

### Nowe
- **API Responses** — dodano zestaw klas reprezentujących odpowiedzi statusów operacji:
  - `AuthenticationStatusCodeResponse`
  - `CertificateStatusCodeResponse`
  - `InvoiceExportStatusCodeResponse`
  - `InvoiceInSessionStatusCodeResponse`
  - `OperationStatusCodeResponse`
- **Operation Status Codes** — dodano nowy kod statusu **550 – "OperationCancelled"**  

### Zmodyfikowane
- `BatchFilePartInfo` — pole `FileName` oznaczono jako **Obsolete** (planowane usunięcie w przyszłych wersjach).

## Changelog zmian – ## Wersja 2.0.0 RC5.6 

### Nowe
- **PdfTestApp** ➕
  - Dodano aplikację konsolową `KSeF.Client.Tests.PdfTestApp` do automatycznego generowania wizualizacji PDF faktur KSeF i dokumentów UPO.
  - Obsługuje generowanie PDF zarówno dla faktur (`faktura`, `invoice`) jak i dokumentów UPO (`upo`).
  - Automatyczna instalacja zależności: npm packages, Chromium (Playwright).
  - Dokumentacja w README.md z instrukcjami instalacji i przykładami użycia.

## Changelog zmian – ## Wersja 2.0.0 RC5.5 

### Nowe
- **Permissions / Builder** — dodano `EntityAuthorizationsQueryRequestBuilder` z krokiem `ReceivedForOwnerNip(string ownerNip)` dla zapytań **Received** w kontekście **NIP właściciela**; opcjonalnie `WithPermissionTypes(IEnumerable<InvoicePermissionType>)`.
- **E2E – AuthorizationPermissions** — dodano dwa scenariusz "Pobranie listy otrzymanych uprawnień podmiotowych jako właściciel w kontekście NIP":
  - `...ReceivedOwnerNip_Direct_FullFlow_ShouldFindGrantedPermission`.
  - `...ReceivedOwnerNip_Builder_FullFlow_ShouldFindGrantedPermission` (wariant z użyciem buildera).
- E2E - Upo** - dodano test sprawdzający wszystkie dostępne metody pobierania UPO na przykładzie sesji online:
  - `KSeF.Client.Tests.Core.E2E.OnlineSession.Upo.UpoRetrievalAsync_FullIntegrationFlow_AllStepsSucceed`.  
- E2E: Pobranie listy **moich uprawnień** w bieżącym kontekście **NIP** (właściciel) – test `PersonPermissions_OwnerNip_MyPermissions_E2ETests`.
- E2E: **Nadane uprawnienia** (właściciel, kontekst NIP) z filtrowaniem:
  - po **PESEL** uprawnionego — `PersonPermissions_OwnerNip_Granted_FilterAuthorizedPesel_E2ETests`
  - po **odcisku palca (fingerprint SHA-256)** uprawnionego — `PersonPermissions_OwnerNip_Granted_FilterAuthorizedFingerprint_E2ETests`
- E2E „Nadane uprawnienia” (owner, kontekst NIP) z filtrowaniem po **NIP uprawnionego**  
- **E2E – PersonalPermissions**: Pobranie listy **obowiązujących uprawnień** do pracy w KSeF jako **osoba uprawniona PESEL** w **kontekście NIP** — `PersonalPermissions_AuthorizedPesel_InNipContext_E2ETests`.
- **NuGet Packages**: Opublikowano paczki NuGet oraz dodano instrukcję instalacji.
- **KSeF.Client.Core** - dodano `EffectiveApiRateLimits` oraz `EffectiveApiRateLimitValues` dotyczące `/rate-limits`.
- **LimitsClient** - dodano obsługę endpointu GET `/rate-limits`:
  - `GetRateLimitsAsync(...)`
- **TestDataClient** - dodano obsługę endpointów POST i DELETE `/testdata/rate-limits`:
  - `SetRateLimitsAsync(...)`
  - `RestoreRateLimitsAsync(...)`
- **E2E - EnforcementOperations**
  - `EnforcementOperationsE2ETests`
  - `EnforcementOperationsNegativeE2ETests`
    - Dodano testy E2E do nadawania uprawnień do wykonywania operacji komorniczych.
  
### Zmodyfikowane
- `EntityAuthorizationsAuthorizingEntityIdentifier` pole `Type` zmieniono typ ze `string` na  enum `AuthorizedIdentifierType`.
- `EntityAuthorizationsAuthorizedEntityIdentifier` pole `Type` zmieniono typ ze `string` na  enum  `AuthorizedIdentifier`.
- **Tests / Utils - Upo** - przeniesiono metody pomocnicze do pobierania UPO z klas testów do KSeF.Client.Tests.Utils.Upo.UpoUtils:
  - `...GetSessionInvoiceUpoAsync`,
  - `...GetSessionUpoAsync`,
  - `...GetUpoAsync`
- `KSeF.Client.Tests.Core.E2E.OnlineSession.OnlineSessionE2ETests.OnlineSessionAsync_FullIntegrationFlow_AllStepsSucceed` uproszczono test stosując pobieranie UPO z adresu przekazanego w metadanych pobranej faktury, 
- `KSeF.Client.Tests.Core.E2E.BatchSession.BatchSessionStreamE2ETests.BatchSession_StreamBased_FullIntegrationFlow_ReturnsUpo` uproszczono test stosując pobieranie UPO z adresu przekazanego w metadanych pobranej faktury, 
- `KSeF.Client.Tests.Core.E2E.BatchSession.BatchSessionE2ETests.BatchSession_FullIntegrationFlow_ReturnsUpo` uproszczono test stosując pobieranie UPO z adresu przekazanego w metadanych pobranej faktury
- **ServiceCollectionExtensions - AddCryptographyClient**
- `KSeF.Client.DI.ServiceCollectionExtensions.AddCryptographyClient` zmodyfikowano metodę konfiguracyjną rejestrującą klienta oraz serwis (HostedService) kryptograficzny. Zrezygnowano z pobierania trybu startowego z opcji. Obecnie metoda AddCryptographyClient() przyjmuje 2 opcjonalne parametry: 
  - delegat służący do pobrania publicznych certyfikatów KSeF (domyślnie jest to metoda GetPublicCertificatesAsync() w CryptographyClient)
  - wartość z enum CryptographyServiceWarmupMode (domyślnie Blocking). Działanie każdego z trybów jest opisane w CryptographyServiceWarmupMode.cs
Przykład użycia: `KSeF.DemoWebApp.Program.cs line 24`
Przykład rejestracji serwisu i klienta kryptograficznego bez użycia hosta (z pominięciem AddCryptographyClient): `KSeF.Client.Tests.Core.E2E.TestBase.cs line 48-74`
- `KSeF.Client.Core` - uporządkowano strukturę i doprecyzowano nazwy modeli oraz enumów. Modele potrzebne do manipulowania danymi testowymi obecnie znajdują się w folderze TestData (wcześniej Tests). Usunięto nieużywane klasy i enumy. 
- `EntityAuthorizationsAuthorizingEntityIdentifier` pole `Type` zmieniono typ ze `string` na  enum `EntityAuthorizationsAuthorizingEntityIdentifierType`.
- `EntityAuthorizationsAuthorizedEntityIdentifier` pole `Type` zmieniono typ ze `string` na  enum  `EntityAuthorizationsAuthorizedEntityIdentifierType`.
- Poprawiono oznaczenia pól opcjonalnych w `SessionInvoice`.
### Usunięte
- **TestDataSessionLimitsBase**
  - usunięto pola `MaxInvoiceSizeInMib` oraz `MaxInvoiceWithAttachmentSizeInMib`.

### Uwaga / kompatybilność
 - `KSeF.Client.Core` - zmiana nazw niektórych modeli, dopasowanie namespace do zmienionej struktury plików i katalogów. 
 - `KSeF.Client.DI.ServiceCollectionExtensions.AddCryptographyClient` zmodyfikowano metodę konfiguracyjną rejestrującą klienta oraz serwis (HostedService) kryptograficzny.

---
# Changelog zmian – ## Wersja 2.0.0 RC5.4.0
---

### Nowe
 - `QueryInvoiceMetadataAsync` - Dodano parametr `sortOrder`, umożliwiający określenie kierunku sortowania wyników.

### Zmodyfikowane
 - Wyliczanie liczby części paczek na podstawie wielkości paczki oraz ustalonych limitów
 - Dostosowanie nazewnictwa - zmiana z `OperationReferenceNumber` na `ReferenceNumber`
 - Rozszerzone scenariusze testów uprawnień
 - Rozszerzone scenariusze testów TestData

---
# Changelog zmian – ## Wersja 2.0.0 RC5.3.0
---

### Nowe
- **REST / Routing**
  - `IRouteBuilder` + `RouteBuilder` – centralne budowanie ścieżek (`/api/v2/...`) z opcjonalnym `apiVersion`. ➕
- **REST / Typy i MIME**
  - `RestContentType` + `ToMime()` – jednoznaczne mapowanie `Json|Xml` → `application/*`. ➕
- **REST / Baza klienta**
  - `ClientBase` — wspólna klasa bazowa klientów HTTP; centralizacja konstrukcji URL (via `RouteBuilder`);
 - **REST / LimitsClient**
  - `ILimitsClient`, `LimitsClient` — obsługa API **Limits**: `GetLimitsForCurrentContext`, `GetLimitsForCurrentSubject`;
 - **Testy / TestClient**
  - `ITestClient`, `TestClient` — klient udostępnia operacje:
    `CreatePersonAsync`, `RemovePersonAsync`, `CreateSubjectAsync`, `GrantTestDataPermissionsAsync`. ➕
- **Testy / PEF**
  - Rozszerzone scenariusze E2E PEF (Peppol) – asercje statusów i uprawnień. ➕
- **TestData / Requests**
  - Modele requestów do środowiska testowego: `PersonCreateRequest`, `PersonRemoveRequest`, `SubjectCreateRequest`, `TestDataPermissionsGrantRequest`. ➕
- **Templates**
  - Szablon korekty PEF: `invoice-template-fa-3-pef-correction.xml` (na potrzeby testów). ➕

### Zmodyfikowane
- **REST / Klient**
  - Refactor: generyczne `RestRequest<TBody>` i wariant bez body; spójne fluent‑metody `WithBody(...)`, `WithAccept(...)`, `WithTimeout(...)`, `WithApiVersion(...)`. 🔧
  - Redukcja duplikatów w `IRestClient.SendAsync(...)`; precyzyjniejsze komunikaty błędów. 🔧
  - Porządek w MIME i nagłówkach – jednolite ustawianie `Content-Type`/`Accept`. 🔧
  - Aktualizacja podpisów interfejsów (wewnętrznych) pod nową strukturę REST. 🔧
- **Routing / Spójność**
  - Konsolidacja prefiksów w jednym miejscu (RouteBuilder) zamiast powielania `"/api/v2"` w klientach/testach. 🔧
- **System codes / PEF**
  - Uzupełnione mapowania kodów systemowych i wersji pod **PEF** (serializacja/mapping). 🔧
- **Testy / Utils**
  - `AsyncPollingUtils` – stabilniejsze retry/backoff, czytelniejsze warunki. 🔧
- **Code style**
  - `var` → jawne typy; `ct` → `cancellationToken`; porządek właściwości; usunięte `unused using`. 🔧

### Usunięte
- **REST**
  - Nadmiarowe przeciążenia `SendAsync(...)` i pomocnicze fragmenty w kliencie REST (po refaktorze). ➖

### Poprawki i zmiany dokumentacji
- Doprecyzowane opisy `<summary>`/wyjątków w interfejsach oraz spójne nazewnictwo w testach i requestach (PEF/TestData). 🔧

**Uwaga (kompatybilność)**: zmiany w `IRestClient`/`RestRequest*` mają charakter **internal** – publiczny kontrakt `IKSeFClient` bez zmian funkcjonalnych w tym RC. Jeśli rozszerzałeś warstwę REST, przejrzyj integracje pod nowy `RouteBuilder` i generyczne `RestRequest<TBody>`. 🔧

---
# Changelog zmian – ## Wersja 2.0.0 RC5.2.0
---

### Nowe
- **Kryptografia**
  - Obsługa ECDSA (krzywe eliptyczne, P-256) przy generowaniu CSR ➕
  - ECIES (ECDH + AES-GCM) jako alternatywa szyfrowania tokena KSeF ➕
  - `ICryptographyService`:
    - `GenerateCsrWithEcdsa(...)` ➕
    - `EncryptWithECDSAUsingPublicKey(byte[] content)` (ECIES: SPKI + nonce + tag + ciphertext) ➕
    - `GetMetaDataAsync(Stream, ...)` ➕
    - `EncryptStreamWithAES256(Stream, ...)` oraz `EncryptStreamWithAES256Async(Stream, ...)` ➕
- **CertTestApp** ➕
  - Dodano możliwość eksportu utworzonych certyfikatów do plików PFX i CER w trybie `--output file`.
- **Build** ➕
  - Podpisywanie bibliotek silną nazwą: dodano pliki `.snk` i włączono podpisywanie dla `KSeF.Client` oraz `KSeF.Client.Core`.
- **Tests / Features** ➕
  - Rozszerzono scenariusze `.feature` (uwierzytelnianie, sesje, faktury, uprawnienia) oraz E2E (cykl życia certyfikatu, eksport faktur).

### Zmodyfikowane
- **Kryptografia** 🔧
  - Usprawniono generowanie CSR ECDSA i obliczanie metadanych plików; dodano wsparcie dla pracy na strumieniach (`GetMetaData(...)`, `GetMetaDataAsync(...)`, `EncryptStreamWithAES256(...)`).
- **Modele / kontrakty API** 🔧
  - Dostosowano modele do aktualnych kontraktów API; uspójniono modele eksportu i metadanych faktur (`InvoicePackage`, `InvoicePackagePart`, `ExportInvoicesResponse`, `InvoiceExportRequest`, `GrantPermissionsSubUnitRequest`, `PagedInvoiceResponse`).
- **Demo (QrCodeController)** 🔧
  - Etykiety pod QR oraz weryfikacja certyfikatów w linkach weryfikacyjnych.

### Poprawki i zmiany dokumentacji
- **README** 🔧
  - Doprecyzowano rejestrację DI i opis eksportu certyfikatów w CertTestApp.
- **Core** 🔧
  - `EncryptionMethodEnum` z wartościami `ECDsa`, `Rsa` (przygotowanie pod wybór metody szyfrowania).

---

---
# Changelog zmian – ## Wersja 2.0.0 RC5.1.1
---
### Nowe
- **KSeF Client**
  - Wyłączono serwis kryptograficzny z klienta KSeF 🔧
  - Wydzielono modele DTO do osobnego projektu `KSeF.Client.Core`, który jest zgodny z `NET Standard 2.0` ➕
- **CertTestApp** ➕
  - Dodano aplikację konsolową do zobrazowania tworzenia przykładowego, testowego certyfikatu oraz podpisu XAdES.
- **Klient kryptograficzny**
  - nowy klient  `CryptographyClient` ➕

- **porządkowanie projektu**
  - zmiany w namespace przygotowujące do dalszego wydzielania serwisów z klienta KSeF 🔧
  - dodana nowa konfiguracja DI dla klienta kryptograficznego 🔧

---
# Changelog zmian – ## Wersja 2.0.0 RC5.1
---

### Nowe
- **Tests**
  - Obsługa `KsefApiException` (np. 403 *Forbidden*) w scenariuszach sesji i E2E.

### Zmodyfikowane
- **Invoices / Export**
  - `ExportInvoicesResponse` – usunięto pole `Status`; po `ExportInvoicesAsync` używaj `GetInvoiceExportStatusAsync(operationReferenceNumber)`.
- **Invoices / Metadata**
  - `pageSize` – zakres dozwolony **10–250** (zaktualizowane testy: „outside 10–250”).
- **Tests (E2E)**
  - Pobieranie faktury: retry **5 → 10**, precyzyjny `catch` dla `KsefApiException`, asercje `IsNullOrWhiteSpace`.
- **Utils**
  - `OnlineSessionUtils` – prefiks **`PL`** dla `supplierNip` i `customerNip`.
- **Peppol tests**
  - Zmieniono użycie NIP na format z prefiksem `PL...`.
  - Dodano asercję w testach PEF, jeśli faktura pozostaje w statusie *processing*.
- **Permissions**
  - Dostosowanie modeli i testów do nowego kontraktu API.
### Usunięte
- **Invoices / Export**
  - `ExportInvoicesResponse.Status`.

### Poprawki i zmiany dokumentacji
- Przykłady eksportu bez `Status`.
- Opis wyjątków (`KsefApiException`, 403 *Forbidden*).
- Limit `pageSize` zaktualizowany do **10–250**.

---
# Changelog zmian – ### Wersja 2.0.0 RC5
---

### Nowe
- **Auth**
  - `ContextIdentifierType` → dodano wartość `PeppolId`
  - `AuthenticationMethod` → dodano wartość `PeppolSignature`
  - `AuthTokenRequest` → nowe property `AuthorizationPolicy`
  - `AuthorizationPolicy` → nowy model zastępujący `IpAddressPolicy`
  - `AllowedIps` → nowy model z listami `Ip4Address`, `Ip4Range`, `Ip4Mask`
  - `AuthTokenRequestBuilder` → nowa metoda `WithAuthorizationPolicy(...)`
  - `ContextIdentifierType` → dodano wartość `PeppolId`
- **Models**
  - `StatusInfo` → dodano property `StartDate`, `AuthenticationMethod`
  - `AuthorizedSubject` → nowy model (`Nip`, `Name`, `Role`)
  - `ThirdSubjects` → nowy model (`IdentifierType`, `Identifier`, `Name`, `Role`)
  - `InvoiceSummary` → dodano property `HashOfCorrectedInvoice`, `AuthorizedSubject`, `ThirdSubjects`
  - `AuthenticationKsefToken` → dodano property `LastUseDate`, `StatusDetails`
  - `InvoiceExportRequest`, `ExportInvoicesResponse`, `InvoiceExportStatusResponse`, `InvoicePackage` → nowe modele eksportu faktur (zastępują poprzednie)
  - `FormType` → nowy enum (`FA`, `PEF`, `RR`) używany w `InvoiceQueryFilters`
  - `OpenOnlineSessionResponse`
      - dodano property `ValidUntil : DateTimeOffset`
      - zmiana modelu requesta w dokumentacji endpointu `QueryInvoiceMetadataAsync` (z `QueryInvoiceRequest` na `InvoiceMetadataQueryRequest`)
      - zmiana namespace z `KSeFClient` na `KSeF.Client`
- **Enums**
  - `InvoicePermissionType` → dodano wartości `RRInvoicing`, `PefInvoicing`
  - `AuthorizationPermissionType` → dodano wartość `PefInvoicing`
  - `KsefTokenPermissionType` → dodano wartości `SubunitManage`, `EnforcementOperations`, `PeppolId`
  - `ContextIdentifierType (Tokens)` → nowy enum (`Nip`, `Pesel`, `Fingerprint`)
  - `PersonPermissionsTargetIdentifierType` → dodano wartość `AllPartners`
  - `SubjectIdentifierType` → dodano wartość `PeppolId`
- **Interfaces**
  - `IKSeFClient` → nowe metody:
    - `ExportInvoicesAsync` – `POST /api/v2/invoices/exports`
    - `GetInvoiceExportStatusAsync` – `GET /api/v2/invoices/exports/{operationReferenceNumber}`
    - `GetAttachmentPermissionStatusAsync` – poprawiony na `GET /api/v2/permissions/attachments/status`
    - `SearchGrantedPersonalPermissionsAsync` – `POST /api/v2/permissions/query/personal/grants`
    - `GrantsPermissionAuthorizationAsync` – `POST /api/v2/permissions/authorizations/grants`
    - `QueryPeppolProvidersAsync` – `GET /api/v2/peppol/query`
- **Tests**
  - `Authenticate.feature.cs` → dodano testy end-to-end dla procesu uwierzytelniania.

### Zmodyfikowane
- **authv2.xsd**
  - ➖ Usunięto:
    - element `OnClientIpChange (tns:IpChangePolicyEnum)`
    - regułę unikalności `oneIp`
    - cały model `IpAddressPolicy` (`IpAddress`, `IpRange`, `IpMask`)
  - Dodano:
    - element `AuthorizationPolicy` (zamiast `IpAddressPolicy`)
    - nowy model `AllowedIps` z kolekcjami:
      - `Ip4Address` – pattern z walidacją zakresów IPv4 (0–255)
      - `Ip4Range` – rozszerzony pattern z walidacją zakresu adresów
      - `Ip4Mask` – rozszerzony pattern z walidacją maski (`/8`, `/16`, `/24`, `/32`)
  - Zmieniono:
    - `minOccurs/maxOccurs` dla `Ip4Address`, `Ip4Range`, `Ip4Mask`:  
      wcześniej `minOccurs="0" maxOccurs="unbounded"` → teraz `minOccurs="0" maxOccurs="10"`
  - Podsumowanie:
    - Zmieniono nazwę `IpAddressPolicy` → `AuthorizationPolicy`
    - Wprowadzono precyzyjniejsze regexy dla IPv4
    - Ograniczono maksymalną liczbę wpisów do 10
- **Invoices**
  - `InvoiceMetadataQueryRequest` → usunięto `SchemaType`
  - `PagedInvoiceResponse` → `TotalCount` opcjonalny
  - `Seller.Identifier` → opcjonalny, dodano `Seller.Nip` jako wymagane
  - `AuthorizedSubject.Identifier` → usunięty, dodano `AuthorizedSubject.Nip`
  - `fileHash` → usunięty
  - `invoiceHash` → dodany
  - `invoiceType` → teraz `InvoiceType` zamiast `InvoiceMetadataInvoiceType`
  - `InvoiceQueryFilters` → `InvoicingMode` stał się opcjonalny (`InvoicingMode?`), dodano `FormType`, usunięto `IsHidden`
  - `SystemCodes.cs` → dodano kody systemowe dla PEF oraz zaktualizowano mapowanie pod `FormType.PEF`
- **Permissions**
  - `EuEntityAdministrationPermissionsGrantRequest` → dodano wymagane `SubjectName`
  - `ProxyEntityPermissions` → uspójniono nazewnictwo poprzez zmianę na `AuthorizationPermissions`
- **Tokens**
  - `QueryKsefTokensAsync` → dodano parametry `authorIdentifier`, `authorIdentifierType`, `description`; usunięto domyślną wartość `pageSize=10`
  - poprawiono generowanie query string: `status` powtarzany zamiast listy `statuses`

### Poprawki i zmiany dokumentacji
- poprawiono i uzupełniono opisy działania metod w interfejsach `IAuthCoordinator` oraz `ISignatureService`
  - w implementacjach zastosowano `<inheritdoc />` dla spójności dokumentacji

### Zmiany kryptografii
- dodano obsługę ECDSA przy generowaniu CSR (domyślnie algorytm IEEE P1363, możliwość nadpisania na RFC 3279 DER)
- zmieniono padding RSA z PKCS#1 na PSS zgodnie ze specyfikacją KSeF API w implementacji `SignatureService`

### Usunięte
- **Invoices**
  - `AsyncQueryInvoicesAsync` i `GetAsyncQueryInvoicesStatusAsync` → zastąpione przez metody eksportu
  - `AsyncQueryInvoiceRequest`, `AsyncQueryInvoiceStatusResponse` → usunięte
  - `InvoicesExportRequest` → zastąpione przez `InvoiceExportRequest`
  - `InvoicesExportPackage` → zastąpione przez `InvoicePackage`
  - `InvoicesMetadataQueryRequest` → zastąpione przez `InvoiceQueryFilters`
  - `InvoiceExportFilters` → włączone do `InvoiceQueryFilters`





---
# Changelog zmian – ### Wersja 2.0.0 RC4

---

## 1. KSeF.Client
  - Usunięto `Page` i `PageSize` i dodano `HasMore` w: 
    - `PagedInvoiceResponse`
    - `PagedPermissionsResponse<TPermission>`
    - `PagedAuthorizationsResponse<TAuthorization>`
    - `PagedRolesResponse<TRole>`
    - `SessionInvoicesResponse`
   - Usunięto `InternalId` z wartości enum `TargetIdentifierType` w `GrantPermissionsIndirectEntityRequest`
   - Zmieniono odpowiedź z `SessionInvoicesResponse` na nową `SessionFailedInvoicesResponse` w odpowiedzi endpointu `/sessions/{referenceNumber}/invoices/failed`, metoda `GetSessionFailedInvoicesAsync`.
   - Zmieniono na opcjonalne pole `to` w `InvoiceMetadataQueryRequest`, `InvoiceQueryDateRange`, `InvoicesAsyncQueryRequest`.
   - Zmieniono `AuthenticationOperationStatusResponse` na nową `AuthenticationListItem` w `AuthenticationListResponse` w odpowiedzi endpointu `/auth/sessions`.
   - Zmieniono model `InvoiceMetadataQueryRequest` adekwatnie do kontraktu API.
   - Dodano pole `CertificateType` w `SendCertificateEnrollmentRequest`, `CertificateResponse`, `CertificateMetadataListResponse` oraz `CertificateMetadataListRequest`.
   - Dodano `WithCertificateType` w `GetCertificateMetadataListRequestBuilder` oraz `SendCertificateEnrollmentRequestBuilder`.
   - Dodano brakujące pole `ValidUntil` w modelu `Session`.
   - Zmieniono `ReceiveDate` na `InvoicingDate` w modelu `SessionInvoice`.

   
## 2. KSeF.DemoWebApp/Controllers
- **OnlineSessionController.cs**: ➕ `GET /send-invoice-correction` - Przykład implementacji i użycia korekty technicznej
---

```
```

# Changelog zmian – `## 2.0.0 (2025-07-14)` (KSeF.Client)

---

## 1. KSeF.Client
Zmiana wersji .NET 8.0 na .NET 9/0

### 1.1 Api/Services
- **AuthCoordinator.cs**: 🔧 Dodano dodatkowy log `Status.Details`; 🔧 dodano wyjątek przy `Status.Code == 400`; ➖ usunięto `ipAddressPolicy`
- **CryptographyService.cs**: ➕ inicjalizacja certyfikatów; ➕ pola `symmetricKeyEncryptionPem`, `ksefTokenPem`
- **SignatureService.cs**: 🔧 `Sign(...)` → `SignAsync(...)`
- **QrCodeService.cs**: ➕ nowa usługa do generowania QrCodes
- **VerificationLinkService.cs**: ➕ nowa usługa generowania linków do weryfikacji faktury

### 1.2 Api/Builders
- **SendCertificateEnrollmentRequestBuilder.cs**: 🔧 `ValidFrom` pole zmienione na opcjonalne ; ➖ interfejs `WithValidFrom`
- **OpenBatchSessionRequestBuilder.cs**: 🔧 `WithBatchFile(...)` usunięto parametr `offlineMode`; ➕ `WithOfflineMode(bool)` nowy opcjonalny krok do oznaczenia trybu offline

### 1.3 Core/Models
- **StatusInfo.cs**: 🔧 dodano property `Details`; ➖ `BasicStatusInfo` - usunięto klase w c elu unifikacji statusów
- **PemCertificateInfo.cs**: ➕ `PublicKeyPem` - dodano nowe property
- **DateType.cs**: ➕ `Invoicing`, `Acquisition`, `Hidden` - dodano nowe enumeratory do filtrowania faktur
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

## [next-version] – `2025-07-15`

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
  🔧 w `Seller` oraz `Buyer` dodano nowe typy bez pola `Name`:

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

## Rozwiązania zgłoszonych  - `2025-07-21`

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

- **ServiceCollectionExtensions.cs**: ➕ konfiguracja lokalizacji (`pl-PL`, `en-US`) i rejestracji `IQrCodeService`/`IVerificationLinkService`
- **AuthTokenRequest.cs**: dostosowanie serializacji XML do nowego schematu XSD
- **README.md**: poprawione środowisko w przykładzie rejestracji KSeFClient w kontenerze DI.
---

```
```

## [next-version] – `2025-08-31`
---

### 2. KSeF.Client.Tests

* **Utils**
  ➕ Nowe utils usprawniające uwierzytelnianie, obsługę sesji interaktywnych, wsadowych, zarządzanie uprawnieniami, oraz ich metody wspólne: **AuthenticationUtils.cs**, **OnlineSessionUtils.cs**, **MiscellaneousUtils.cs**, **BatchSessionUtils.cs**, **PermissionsUtils.cs**.
  🔧 Refactor testów - użycie nowych klas utils.
  🔧 Zmiana kodu statusu dla zamknięcia sesji interaktywnej z 300 na 170.
  🔧 Zmiana kodu statusu dla zamknięcia sesji wsadowej z 300 na 150.
---

```
```
