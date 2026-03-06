## Rejestr zmian: Wersja 2.2.0
### Nowe
- Dodano możliwość przełączenia PascalCase/camelCase w nazwach właściwości zwracanych z API.
- Dodano obsługę endpointa `POST /permissions/query/entities/grants` umożliwiającego pobranie listy uprawnień do obsługi faktur w bieżącym kontekście logowania.
- Rozszerzono odpowiedź endpointu POST `/auth/challenge` o pole `clientIp` zwracające adres IP klienta zarejestrowany przez KSeF.
- Ustandaryzowano odpowiedzi `401 Unauthorized` oraz `403 Forbidden` do formatu Problem Details `(application/problem+json)`.
- Dodano schematy `UnauthorizedProblemDetails` oraz `ForbiddenProblemDetails`.
- Rozszerzono `ForbiddenProblemDetails` o wymagane pole `reasonCode` oraz o opcjonalny obiekt `security` (dodatkowe dane zależne od `reasonCode`).

## Rejestr zmian: Wersja 2.1.2
### Nowe

- Dodano nowy kod systemowy `FA_RR(1)` w `SystemCode` (wraz z mapowaniem w `SystemCodeHelper`).
- Dodano nowy test E2E dla faktury VAT RR: `AuthorizationPermissionsRRInvoicingE2ETests.RRInvoicingPermission_AllowsSendingFaRrInvoice`.

## Rejestr zmian: Wersja 2.1.1
### Nowe
- Dodano parametr `enforceXadesCompliance` w metodzie `SubmitXadesAuthRequestAsync`, umożliwiający wcześniejsze włączenie nowych wymagań walidacji XAdES na środowiskach DEMO i PROD poprzez nagłówek `X-KSeF-Feature: enforce-xades-compliance`.
- Dodano wsparcie dla .NET Standard 2.0 dla Windows oraz .NET Framework 4.8, dzięki zaangażowaniu Kontrybutora [@marcinborecki](https://github.com/CIRFMF/ksef-client-csharp/pull/197)



## Rejestr zmian: Wersja 2.1.0
### Nowe
- `AuthStatus`, `AuthenticationListItem`: Wprowadzono nowy model `AuthenticationMethodInfo` opisujący metodę uwierzytelniania
- Dodano obsługę dwóch nowych endpointów:
  - POST `/testdata/context/block`: blokuje możliwość uwierzytelniania dla wskazanego kontekstu. Uwierzytelnianie zakończy się błędem 480.
  - POST `/testdata/context/unblock`: odblokowuje możliwość uwierzytelniania dla bieżącego kontekstu.
- `AuthStatus`, `AuthenticationListItem`: Wprowadzono nowy model `AuthenticationMethodInfo` opisujący metodę uwierzytelniania.

### Zmodyfikowane
- `AuthStatus`, `AuthenticationListItem`: Pole `AuthenticationMethod` oznaczono jako **Obsolete** (planowane wycofanie: 2026-11-16).
- Metody `AddBatchFilePart` oraz `AddBatchFileParts` z parametrem `fileName` oznaczono jako przestarzałe (Obsolete) i zostaną usunięte w niedalekiej przyszłości. Zaleca się używanie przeciążeń bez parametru `fileName`
- `KsefNumberValidator` zwraca teraz komunikat informujący o błędzie w przypadku nieprawidłowej sumy kontrolnej
- `DateRange`: Zmieniono typ pól `From` i `To` z DateTime na DateTimeOffset w celu poprawnej obsługi stref czasowych i offsetów zgodnie ze specyfikacją API KSeF (format ISO 8601 z offsetem/UTC/lokalny Europe/Warsaw)
	- Zmiana typu DateRange.From i DateRange.To z DateTime na DateTimeOffset może wpłynąć na Twoje rozwiązania. Jeśli korzystasz z DateRange do filtrowania faktur, sprawdź czy Twój kod poprawnie tworzy DateTimeOffset

## Rejestr zmian: Wersja 2.0.1
### Nowe
- Dodano obsługę wartości `InternalId` w `PersonalPermissionContextIdentifierType` oraz `PersonalPermissionsContextIdentifierType` umożliwiającą filtrowanie uprawnień osobistych według identyfikatora wewnętrznego.
- Dodano nowy model `OperationStatusInfo` do reprezentowania statusów operacji (w odróżnieniu od statusów faktur).
- Dodano `TooManyRequestsErrorResponse` do obsługi odpowiedzi HTTP 429 (zbyt wiele żądań).
- Dodano testy E2E dla uprawnień osobistych z filtrowaniem po `InternalId`: `PersonalPermissionsWithInternalIdFilterE2ETests`.
- Dodano walidator numerów NIP - na środowisku testowym użycie opcjonalne.

### Zmodyfikowane
- Poprawiono obsługę odpowiedzi HTTP 429 w metodzie `HandleTooManyRequestsAsync` w `RestClient`.
- Podzielono model `StatusInfo` na dwa odrębne typy:
  - `InvoiceStatusInfo` - do statusów związanych z fakturami
  - `OperationStatusInfo` - do statusów operacji
- Zaktualizowano szablon faktury `invoice-template-fa-3-with-custom-Subject2.xml`.
- Poprawiono testy E2E:
  - `CertificatesE2ETests` - usunięto równoległość z testu przekroczenia limitu certyfikatów
  - `DuplicateInvoiceE2ETests` - usunięto niepotrzebną asercję
  - `InvoiceE2ETests` - poprawiono generowanie i walidację `InternalId`
  - `PeppolPefE2ETests` - skrócono opóźnienia w testach
  - `EuRepresentativePermissionE2ETests` - weryfikacja uprawnień przedstawiciela po odcisku palca zamiast porównania liczby
  - `SubunitPermissionScenarioE2EFixture` - poprawki w scenariuszach uprawnień podjednostek
- Rozszerzono narzędzia pomocnicze `MiscellaneousUtils` oraz `OnlineSessionUtils` o dodatkowe metody.

### Dokumentacja
- Zaktualizowano `README.md` o instrukcje aktualizacji submodułu `ksef-pdf-generator`.
- Dodano wpis troubleshooting w `README.md` dla `KSeF.Client.Tests.PdfTestApp`.
- Poprawiono opis kodów QR w dokumentacji README.

### Poprawki
- Poprawiono test uprawnień jednostek podrzędnych.
- Poprawiono generowanie `InternalId` z poprawną sumą kontrolną w testach.

## Rejestr zmian: Wersja 2.0.0
### Nowe
- Dodano obsługę nagłówka `x-ms-meta-hash` zwracanego przez API (skrót SHA-256 dokumentu UPO w formacie Base64) oraz nowe metody w `UpoUtils` umożliwiające pobieranie UPO wraz z tym hashem.
- Dodano metodę `X509CertificateLoaderExtensions.MergeWithPemKeyNoProfileForEcdsa`, która ręcznie odszyfrowuje zaszyfrowane klucze ECDSA PKCS#8 w pamięci i importuje je jako efemeryczne, zapewniając działanie także w środowiskach, gdzie `ImportFromEncryptedPem` zawodzi (np. IIS z wyłączonym LoadUserProfile dla ECDSA).

### Zmodyfikowane
- Zmieniono obsługę błędów w metodzie `X509CertificateLoaderExtensions.MergeWithPemKey`przy ładowaniu zaszyfrowanych kluczy ECDSA: zamiast niejasnego komunikatu użytkownik dostaje prosty opis problemu, a biblioteka automatycznie wywołuje metodę `MergeWithPemKeyNoProfileForEcdsa`, która działa bez profilu użytkownika.

## Rejestr zmian: Wersja 2.0.0 RC6.1.1
### Nowe
- Usunięto przedrostek `/api` z adresów URL w `KSeFClient` oraz `RouteBuilder`.
- Poprawiono `KsefEnvironmentConfig` w projekcie `ClientFactory`.
- Dodano url środowiska PROD w `KsefEnvironmentsUris` oraz `KsefQREnvironmentsUris`.

### Zmodyfikowane
- Poprawiono działanie generatora PDF w aplikacji testowej `KSeF.Client.Tests.PdfTestApp`:
  - Dostosowano działanie pod nową wersję submodułu `ksef-pdf-generator`
  - Zaktualizowano dokumentację z sekcją troubleshootingu
  - Dodano instrukcje odświeżania submodułu `ksef-pdf-generator` (wymagane po aktualizacji ze starszych wersji)

## Rejestr zmian: Wersja 2.0.0 RC6.1
### Nowe
- Dodano wymaganą właściwość `timestampMs` w `AuthenticationChallengeResponse`.
- Dodano wymaganą właściwość `rateLimits.invoiceExportStatus` w `EffectiveApiRateLimits`.

### Zmodyfikowane
- Zmieniono adresy URL API KSeF oraz generowanie linków QR zgodnie z dokumentacją:
  - [srodowiska.md](https://github.com/CIRFMF/ksef-docs/blob/main/srodowiska.md)
  - [kody-qr.md](https://github.com/CIRFMF/ksef-docs/blob/main/kody-qr.md)
- Usunięto wartość wyliczeniową (enum): Token z właściwości `subjectIdentifierType` z `TestDataSubjectIdentifier`.
- Usunięto właściwość `batchFile.fileParts[].fileName` z `OpenBatchSessionRequest`.
- W celu zachowania kompatybilności z .NET Standard 2.0 zmieniono następujące typy:
  - `AttachmentPermissionRevokeRequest` - zmieniono typ pola `ExpectedDate` z `DateTime` na `string`.
  - `EuEntityRepresentativePersonByFpNoId` - zmieniono typ pola `BirthDate` z `DateTimeOffset` na `string`.
  - `PermissionsIndirectEntityPersonByFingerprintWithoutIdentifier` - zmieniono typ pola `BirthDate` z `DateTimeOffset` na `string`.
  - `PersonPermissionPersonByFingerprintNoId` - zmieniono typ pola `BirthDate` z `DateTimeOffset` na `string`.
  - `PersonPermissionSubjectPersonDetails` - zmieniono typ pola `BirthDate?` z `DateTimeOffset` na `string`.
  - `PermissionsSubunitPersonByFingerprintWithoutIdentifier` - zmieniono typ pola `BirthDate` z `DateTimeOffset` na `string`.

## Rejestr zmian: Wersja 2.0.0 RC6.0.2
### Nowe
- Dodano nowe przeciążenie metody `ExportInvoicesAsync(InvoiceExportRequest, string, CancellationToken)` niewymagające parametru includeMetadata.
- Dodano możliwość uwierzytelniania tokenem KSeF w KseF.DemoWebApp.
- Dodano metodę rozszerzającą `X509Certificate2.MergeWithPemKey` w `X509CertificateLoaderExtensions`, umożliwiającą bezpieczne łączenie publicznego certyfikatu z kluczem prywatnym (PEM) w pamięci (Ephemeral Key). Jej użycie rozwiązuje problem błędu _the password may be incorrect_ na środowiskach IIS oraz Azure Web Apps, gdzie profil użytkownika jest niedostępny.
- Dodano przeciążenie metody `BuildCertificateVerificationUrl`, które nie wymaga podawania numeru seryjnego certyfikatu, a odczytuje go z podanego w innym parametrze obiektu typu  `X509Certificate2`.
- Dodano plik `templates.md` w `KSeF.Client.Tests.Core/Templates` ze wskazówkami dotyczącymi testowania wysyłki faktur w Aplikacji Podatnika.
- Dodano metody `Invalidate()` oraz `RefreshAsync()` do klasy `KSeFFactoryCertificateFetcherServices`.

### Zmodyfikowane
- Parametr includeMetadata w metodzie `ExportInvoicesAsync(InvoiceExportRequest, string, bool, CancellationToken)` został oznaczony jako przestarzały (`[Obsolete]`).
- Zaktualizowano logikę `ExportInvoicesAsync`: nagłówek `x-ksef-feature: include-metadata` nie jest już wysyłany.


## Rejestr zmian: Wersja 2.0.0 RC6.0.1
### Nowe
- Dodano opisy budowniczych żądań w SDK.

### Zmodyfikowane
- Zmieniono pole `RestrictToPermanentStorageHwmDate` na nullowalne.


## Rejestr zmian – Wersja 2.0.0 RC6.0
### Nowe
- Dodano parametr `upoVersion` w metodach `OpenBatchSessionAsync` i `OpenOnlineSessionAsync`:
  - Pozwala wybrać wersję UPO (dostępne wartości: `"upo-v4-3"`).
  - Ustawia nagłówek `X-KSeF-Feature` z odpowiednią wersją (domyślnie `v4-2`, od 5.01.2026 → `v4-3`).
- Dodano możliwość przywrócenia na środowisku TE domyślnych limitów produkcyjnych API.

### Zmodyfikowane
- Dodano `SubjectDetail` w:
  - `GrantPermissionsAuthorizationRequest`,
  - `GrantPermissionsPersonRequest`,
  - `GrantPermissionsEuEntityRequest`,
  - `GrantPermissionsIndirectEntityRequest`,
  - `GrantPermissionsEntityRequest`,
  - `GrantPermissionsSubunitRequest`,
  - `GrantPermissionsEuEntityRepresentativeRequest`, zgodnie z nowym kontraktem API.
- Dodano właściwość `Extensions` w obiekcie `StatusInfo`.


## Rejestr zmian: Wersja 2.0.0 RC5.7.2
### Nowe
- Dodano walidację parametrów przekazywanych w metodach klas `(...)RequestBuilder` zgodnie z dokumentacją API.
- Dodano klasę `TypeValueValidator`, która umożliwia weryfikację wartości przypisanych do identyfikatorów `Type - Value` (`ContextIdentifier`, `PersonTargetIdentifier`, itp.).

### Zmodyfikowane
- Oznaczono klasę `TestDataSessionLimitsBase` jako 'obsolete' i zastąpiono ją klasą `SessionLimits`.
- Dodano brakującą metodę w interfejsie `IOpenBatchSessionRequestBuilderBatchFile`.
- Dodano test E2E prezentujący możliwość użycia pobranego i zapisanego na dysku certyfikatu wraz z kluczem publicznym do obsługi sesji i wysyłki faktury.
- Dodano metody do klasy CertificateUtils.
- Dodano obsługę HWM jako wzorcowego sposobu przyrostowego pobierania faktur w klasie `IncrementalInvoiceRetrievalE2ETests` (test `IncrementalInvoiceRetrieval_E2E_WithHwmShift`).


## Rejestr zmian: Wersja 2.0.0 RC5.7.2
### Nowe
- `EntityRoleType` → nowy enum (`CourtBailiff`, `EnforcementAuthority`, `LocalGovernmentUnit`, `LocalGovernmentSubUnit`, `VatGroupUnit`, `VatGroupSubUnit`) używany w `EntityRole`.
- `SubordinateEntityRoleType` → nowy enum (`LocalGovernmentSubUnit`, `VatGroupSubUnit`) używany w `SubordinateEntityRole`.
- Rozdzielono zależności na poszczególne wersje .NET SDK.
- EditorConfig: C# 7.3, NRT off, wymuszenie jawnych typów, Async*…Async, _underscore dla pól prywatnych i chronionych.
- `KSeF.Client.Api`: opisy publicznych interfejsów/typów w języku polskim.
- Utils: `ToVatEuFromDomestic(...)` - usprawniona logika działania i komunikaty w języku polskim.

### Zmodyfikowane
- Zmieniono nazwę `EuEntityPermissionsQueryPermissionType` → `EuEntityPermissionType`.
- `PersonPermission` pole `PermissionScope` zmieniono typ ze `string` na enum `PersonPermissionType` (zgłoszenie: https://github.com/CIRFMF/ksef-client-csharp/issues/131)
- `PersonPermission` pole `PermissionState` zmieniono typ ze `string` na  enum `PersonPermissionState`.
- `EntityRole` pole `Role` zmieniono typ ze `string` na  enum `EntityRoleType`.
- `SubordinateEntityRole` pole `Role` zmieniono typ ze `string` na  enum `SubordinateEntityRoleType`.
- `AuthorizationGrant` pole `PermissionScope` zmieniono typ ze `string` na  enum `AuthorizationPermissionType`.
- `EuEntityPermission` pole `PermissionScope` zmieniono typ ze `string` na  enum `EuEntityPermissionType`.


## Rejestr zmian: Wersja 2.0.0 RC5.7.1
### Nowe
**KSeF.Client** 🔧➕ został podzielony na mniejsze interfejsy (z własnymi implementacjami):
  - `IActiveSessionsClient`,
  - `IAuthorizationClient`,
  - `IBatchSessionClient`,
  - `ICertificateClient`,
  - `ICryptographyClient`,
  - `IGrantPermissionClient`,
  - `IInvoiceDownloadClient`,
  - `IKSeFClient`,
  - `IKsefTokenClient`,
  - `ILimitsClient`,
  - `IOnlineSessionClient`,
  - `IPeppolClient`,
  - `IPermissionOperationClient`,
  - `IRevokePermissionClient`,
  - `ISearchPermissionClient`,
  - `ISessionStatusClient`.

### Zmodyfikowane
- Usunięto klasę `ApiException` i zastąpiono użycie jej w summary klasą `KsefApiException`.


## Rejestr zmian: Wersja 2.0.0 RC5.7 
### Nowe
- **API Responses** — dodano zestaw klas reprezentujących odpowiedzi statusów operacji:
  - `AuthenticationStatusCodeResponse`,
  - `CertificateStatusCodeResponse`,
  - `InvoiceExportStatusCodeResponse`,
  - `InvoiceInSessionStatusCodeResponse`,
  - `OperationStatusCodeResponse`.
- **Operation Status Codes**: dodano nowy kod statusu **550 – "OperationCancelled"**.

### Zmodyfikowane
- `BatchFilePartInfo`: pole `FileName` oznaczono jako **Obsolete** (planowane usunięcie w przyszłych wersjach).


## Rejestr zmian: Wersja 2.0.0 RC5.6 
### Nowe
- **PdfTestApp**: aplikacja konsolowa `KSeF.Client.Tests.PdfTestApp` do automatycznego generowania wizualizacji faktur KSeF i dokumentów UPO w formacie PDF:
  - Obsługuje generowanie PDF zarówno dla faktur (`faktura`, `invoice`) jak i dokumentów UPO (`upo`).
  - Automatyczna instalacja zależności: npm packages, Chromium (Playwright).
  - Dokumentacja w README.md z instrukcjami instalacji i przykładami użycia.


## Rejestr zmian: Wersja 2.0.0 RC5.5 
### Nowe
- **Permissions / Builder** — dodano `EntityAuthorizationsQueryRequestBuilder` z krokiem `ReceivedForOwnerNip(string ownerNip)` dla zapytań _Received_ w kontekście _NIP właściciela_ opcjonalnie `WithPermissionTypes(IEnumerable<InvoicePermissionType>)`.
- **E2E – AuthorizationPermissions** — dodano dwa scenariusze "Pobranie listy otrzymanych uprawnień podmiotowych jako właściciel w kontekście NIP":
  - `...ReceivedOwnerNip_Direct_FullFlow_ShouldFindGrantedPermission`.
  - `...ReceivedOwnerNip_Builder_FullFlow_ShouldFindGrantedPermission` (wariant z użyciem buildera).
- **E2E - Upo** - dodano test sprawdzający wszystkie dostępne metody pobierania UPO na przykładzie sesji online: `KSeF.Client.Tests.Core.E2E.OnlineSession.Upo.UpoRetrievalAsync_FullIntegrationFlow_AllStepsSucceed`.
- E2E: Pobranie listy _moich uprawnień_ w bieżącym kontekście _NIP_ (właściciel) – test `PersonPermissions_OwnerNip_MyPermissions_E2ETests`.
- E2E: **Nadane uprawnienia** (właściciel, kontekst NIP) z filtrowaniem:
  - Po _PESEL_ uprawnionego: `PersonPermissions_OwnerNip_Granted_FilterAuthorizedPesel_E2ETests`.
  - Po _odcisku palca (fingerprint SHA-256)_ uprawnionego: `PersonPermissions_OwnerNip_Granted_FilterAuthorizedFingerprint_E2ETests`.
- E2E „Nadane uprawnienia” (owner, kontekst NIP) z filtrowaniem po _NIP uprawnionego_.
- **E2E – PersonalPermissions**: Pobranie listy _obowiązujących uprawnień_ do pracy w KSeF jako _osoba uprawniona PESEL_ w _kontekście NIP_: `PersonalPermissions_AuthorizedPesel_InNipContext_E2ETests`.
- **NuGet Packages**: Opublikowano paczki NuGet oraz dodano instrukcję instalacji.
- **KSeF.Client.Core** - dodano `EffectiveApiRateLimits` oraz `EffectiveApiRateLimitValues` dotyczące `/rate-limits`.
- **LimitsClient** - dodano obsługę endpointu GET `/rate-limits`: `GetRateLimitsAsync(...)`.
- **TestDataClient** - dodano obsługę endpointów POST i DELETE `/testdata/rate-limits`:
  - `SetRateLimitsAsync(...)`,
  - `RestoreRateLimitsAsync(...)`.
- **E2E - EnforcementOperations**:
  - `EnforcementOperationsE2ETests`
  - `EnforcementOperationsNegativeE2ETests` - Dodano testy E2E do nadawania uprawnień do wykonywania operacji komorniczych.

### Zmodyfikowane
- `EntityAuthorizationsAuthorizingEntityIdentifier` pole `Type` zmieniono typ ze `string` na  enum `AuthorizedIdentifierType`.
- `EntityAuthorizationsAuthorizedEntityIdentifier` pole `Type` zmieniono typ ze `string` na  enum  `AuthorizedIdentifier`.
- **Tests / Utils - Upo** - przeniesiono metody pomocnicze do pobierania UPO z klas testów do KSeF.Client.Tests.Utils.Upo.UpoUtils:
  - `...GetSessionInvoiceUpoAsync`,
  - `...GetSessionUpoAsync`,
  - `...GetUpoAsync`.
- `KSeF.Client.Tests.Core.E2E.OnlineSession.OnlineSessionE2ETests.OnlineSessionAsync_FullIntegrationFlow_AllStepsSucceed` - uproszczono test stosując pobieranie UPO z adresu przekazanego w metadanych pobranej faktury.
- `KSeF.Client.Tests.Core.E2E.BatchSession.BatchSessionStreamE2ETests.BatchSession_StreamBased_FullIntegrationFlow_ReturnsUpo` -  uproszczono test stosując pobieranie UPO z adresu przekazanego w metadanych pobranej faktury.
- `KSeF.Client.Tests.Core.E2E.BatchSession.BatchSessionE2ETests.BatchSession_FullIntegrationFlow_ReturnsUpo` - uproszczono test stosując pobieranie UPO z adresu przekazanego w metadanych pobranej faktury.
- **ServiceCollectionExtensions - AddCryptographyClient**: `KSeF.Client.DI.ServiceCollectionExtensions.AddCryptographyClient` - zmodyfikowano metodę konfiguracyjną rejestrującą klienta oraz serwis (HostedService) kryptograficzny.
  Zrezygnowano z pobierania trybu startowego z opcji. Obecnie metoda `AddCryptographyClient()` przyjmuje 2 opcjonalne parametry:
  - Delegat służący do pobrania publicznych certyfikatów KSeF (domyślnie jest to metoda `GetPublicCertificatesAsync()` w CryptographyClient).
  - Wartość z enum CryptographyServiceWarmupMode (domyślnie Blocking). Działanie każdego z trybów jest opisane w `CryptographyServiceWarmupMode.cs`.
  - Przykład użycia: `KSeF.DemoWebApp.Program.cs line 24`.
  - Przykład rejestracji serwisu i klienta kryptograficznego bez użycia hosta (z pominięciem AddCryptographyClient): `KSeF.Client.Tests.Core.E2E.TestBase.cs line 48-74`.
- `KSeF.Client.Core` - uporządkowano strukturę i doprecyzowano nazwy modeli oraz enumów. Modele potrzebne do manipulowania danymi testowymi obecnie znajdują się w folderze TestData (wcześniej Tests). Usunięto nieużywane klasy i enumy.
- `EntityAuthorizationsAuthorizingEntityIdentifier` pole `Type` zmieniono typ ze `string` na  enum `EntityAuthorizationsAuthorizingEntityIdentifierType`.
- `EntityAuthorizationsAuthorizedEntityIdentifier` pole `Type` zmieniono typ ze `string` na  enum  `EntityAuthorizationsAuthorizedEntityIdentifierType`.
- Poprawiono oznaczenia pól opcjonalnych w `SessionInvoice`.

### Usunięte
- **TestDataSessionLimitsBase**: usunięto pola `MaxInvoiceSizeInMib` oraz `MaxInvoiceWithAttachmentSizeInMib`.

### Uwaga / kompatybilność
 - `KSeF.Client.Core` - zmiana nazw niektórych modeli, dopasowanie namespace do zmienionej struktury plików i katalogów.
 - `KSeF.Client.DI.ServiceCollectionExtensions.AddCryptographyClient` - zmodyfikowano metodę konfiguracyjną rejestrującą klienta oraz serwis (HostedService) kryptograficzny.


## Rejestr zmian: Wersja 2.0.0 RC5.4.0
### Nowe
 - `QueryInvoiceMetadataAsync` - dodano parametr `sortOrder`, umożliwiający określenie kierunku sortowania wyników.

### Zmodyfikowane
 - Wyliczanie liczby części paczek na podstawie wielkości paczki oraz ustalonych limitów.
 - Dostosowanie nazewnictwa - zmiana z `OperationReferenceNumber` na `ReferenceNumber`.
 - Rozszerzone scenariusze testów uprawnień.
 - Rozszerzone scenariusze testów TestData.


## Rejestr zmian: Wersja 2.0.0 RC5.3.0
### Nowe
- **REST / Routing**: `IRouteBuilder` oraz `RouteBuilder` – centralne budowanie ścieżek (`/api/v2/...`) z opcjonalnym `apiVersion`.
- **REST / Typy i MIME**: `RestContentType` oraz `ToMime()` – jednoznaczne mapowanie `Json|Xml` → `application/*`.
- **REST / Baza klienta**: `ClientBase` — wspólna klasa bazowa klientów HTTP; centralizacja konstrukcji URL (via `RouteBuilder`).
- **REST / LimitsClient**: `ILimitsClient`, `LimitsClient` — obsługa API **Limits**: `GetLimitsForCurrentContext` i `GetLimitsForCurrentSubject`.
- **Testy / TestClient**: `ITestClient` i `TestClient` — klient udostępnia operacje:
    `CreatePersonAsync`, `RemovePersonAsync`, `CreateSubjectAsync`, `GrantTestDataPermissionsAsync`.
- **Testy / PEF**: Rozszerzone scenariusze E2E PEF (Peppol) – asercje statusów i uprawnień.
- **TestData / Requests**: modele requestów do środowiska testowego: `PersonCreateRequest`, `PersonRemoveRequest`, `SubjectCreateRequest`, `TestDataPermissionsGrantRequest`.
- **Templates**: szablon korekty PEF - `invoice-template-fa-3-pef-correction.xml` (na potrzeby testów).

### Zmodyfikowane
- **REST / Klient**:
  - Refaktor: generyczne `RestRequest<TBody>` i wariant bez body; spójne fluent‑metody `WithBody(...)`, `WithAccept(...)`, `WithTimeout(...)`, `WithApiVersion(...)`.
  - Redukcja duplikatów w `IRestClient.SendAsync(...)`; precyzyjniejsze komunikaty błędów.
  - Porządek w MIME i nagłówkach – jednolite ustawianie `Content-Type`/`Accept`.
  - Aktualizacja podpisów interfejsów (wewnętrznych) pod nową strukturę REST.
- **Routing / Spójność**: konsolidacja prefiksów w jednym miejscu (RouteBuilder) zamiast powielania `/api/v2` w klientach/testach.
- **System codes / PEF**: uzupełnione mapowania kodów systemowych i wersji pod **PEF** (serializacja/mapping).
- **Testy / Utils**: `AsyncPollingUtils` – stabilniejsze retry/backoff oraz czytelniejsze warunki.
- **Code style**: `var` → jawne typy; `ct` → `cancellationToken`; porządek właściwości; usunięte `unused using`.

### Usunięte
- **REST**: nadmiarowe przeciążenia `SendAsync(...)` i pomocnicze fragmenty w kliencie REST (po refaktorze).

### Poprawki i zmiany dokumentacji
- Doprecyzowane opisy `<summary>`/wyjątków w interfejsach oraz spójne nazewnictwo w testach i żądaniach (PEF/TestData).

**Uwaga (kompatybilność)**: zmiany w `IRestClient`/`RestRequest*` mają charakter **internal** – publiczny kontrakt `IKSeFClient` bez zmian funkcjonalnych w tym RC. Jeśli rozszerzasz warstwę REST, przejrzyj integracje pod nowy `RouteBuilder` i generyczne `RestRequest<TBody>`.


## Rejestr zmian: Wersja 2.0.0 RC5.2.0
### Nowe
- **Kryptografia**:
  - Obsługa ECDSA (krzywe eliptyczne, P-256) przy generowaniu CSR.
  - ECIES (ECDH + AES-GCM) jako alternatywa szyfrowania tokena KSeF.
  - `ICryptographyService`:
    - `GenerateCsrWithEcdsa(...)`,
    - `EncryptWithECDSAUsingPublicKey(byte[] content)` (ECIES: SPKI + nonce + tag + ciphertext),
    - `GetMetaDataAsync(Stream, ...)`,
    - `EncryptStreamWithAES256(Stream, ...)` oraz `EncryptStreamWithAES256Async(Stream, ...)`.
- **CertTestApp**: dodano możliwość eksportu utworzonych certyfikatów do plików PFX i CER w trybie `--output file`.
- **Build**: podpisywanie bibliotek silną nazwą - dodano pliki `.snk` i włączono podpisywanie dla `KSeF.Client` oraz `KSeF.Client.Core`.
- **Tests / Features**: rozszerzono scenariusze `.feature` (uwierzytelnianie, sesje, faktury, uprawnienia) oraz E2E (cykl życia certyfikatu, eksport faktur).

### Zmodyfikowane
- **Kryptografia**:
  - Usprawniono generowanie CSR ECDSA i obliczanie metadanych plików.
  - Dodano wsparcie dla pracy na strumieniach (`GetMetaData(...)`, `GetMetaDataAsync(...)`, `EncryptStreamWithAES256(...)`).
- **Modele / kontrakty API**:
  - Dostosowano modele do aktualnych kontraktów API.
  - Uspójniono modele eksportu i metadanych faktur (`InvoicePackage`, `InvoicePackagePart`, `ExportInvoicesResponse`, `InvoiceExportRequest`, `GrantPermissionsSubUnitRequest`, `PagedInvoiceResponse`).
- **Demo (QrCodeController)**: etykiety pod QR oraz weryfikacja certyfikatów w linkach weryfikacyjnych.

### Poprawki i zmiany dokumentacji
- **README**: doprecyzowano rejestrację DI i opis eksportu certyfikatów w CertTestApp.
- **Core**: `EncryptionMethodEnum` z wartościami `ECDsa`, `Rsa` (przygotowanie pod wybór metody szyfrowania).


## Rejestr zmian: Wersja 2.0.0 RC5.1.1
### Nowe
- **KSeF Client**:
  - Wyłączono serwis kryptograficzny z klienta KSeF
  - Wydzielono modele DTO do osobnego projektu `KSeF.Client.Core`, który jest zgodny z `NET Standard 2.0`
- **CertTestApp**: dodano aplikację konsolową do zobrazowania tworzenia przykładowego, testowego certyfikatu oraz podpisu XAdES.
- **Klient kryptograficzny**: nowy klient `CryptographyClient`.

### Zmodyfikowane
- **Porządkowanie projektu**:
  - Zmiany w namespace przygotowujące do dalszego wydzielania serwisów z klienta KSeF.
  - Dodana nowa konfiguracja DI dla klienta kryptograficznego.


## Rejestr zmian: Wersja 2.0.0 RC5.1
### Nowe
- **Tests**: obsługa `KsefApiException` (np. 403 *Forbidden*) w scenariuszach sesji i E2E.

### Zmodyfikowane
- **Invoices / Export**: `ExportInvoicesResponse` – usunięto pole `Status`; po `ExportInvoicesAsync` używaj `GetInvoiceExportStatusAsync(operationReferenceNumber)`.
- **Invoices / Metadata**: `pageSize` – zakres dozwolony **10–250** (zaktualizowane testy: „outside 10–250”).
- **Tests (E2E)**: pobieranie faktury: retry **5 → 10**, precyzyjny `catch` dla `KsefApiException`, asercje `IsNullOrWhiteSpace`.
- **Utils**: `OnlineSessionUtils` – prefiks **`PL`** dla `supplierNip` i `customerNip`.
- **Peppol tests**:
  - Zmieniono użycie NIP na format z prefiksem `PL...`.
  - Dodano asercję w testach PEF, jeśli faktura pozostaje w statusie *processing*.
- **Permissions**: dostosowanie modeli i testów do nowego kontraktu API.

### Usunięte
- **Invoices / Export**: `ExportInvoicesResponse.Status`.

### Poprawki i zmiany dokumentacji
- Przykłady eksportu bez `Status`.
- Opis wyjątków (`KsefApiException`, 403 *Forbidden*).
- Limit `pageSize` zaktualizowany do **10–250**.


## Rejestr zmian: Wersja 2.0.0 RC5
### Nowe
- **Auth**
  - `ContextIdentifierType` → dodano wartość `PeppolId`.
  - `AuthenticationMethod` → dodano wartość `PeppolSignature`.
  - `AuthTokenRequest` → nowe property `AuthorizationPolicy`.
  - `AuthorizationPolicy` → nowy model zastępujący `IpAddressPolicy`.
  - `AllowedIps` → nowy model z listami `Ip4Address`, `Ip4Range`, `Ip4Mask`.
  - `AuthTokenRequestBuilder` → nowa metoda `WithAuthorizationPolicy(...)`.
  - `ContextIdentifierType` → dodano wartość `PeppolId`.
- **Models**
  - `StatusInfo` → dodano property `StartDate`, `AuthenticationMethod`.
  - `AuthorizedSubject` → nowy model (`Nip`, `Name`, `Role`).
  - `ThirdSubjects` → nowy model (`IdentifierType`, `Identifier`, `Name`, `Role`).
  - `InvoiceSummary` → dodano property `HashOfCorrectedInvoice`, `AuthorizedSubject`, `ThirdSubjects`.
  - `AuthenticationKsefToken` → dodano property `LastUseDate`, `StatusDetails`.
  - `InvoiceExportRequest`, `ExportInvoicesResponse`, `InvoiceExportStatusResponse`, `InvoicePackage` → nowe modele eksportu faktur (zastępują poprzednie).
  - `FormType` → nowy enum (`FA`, `PEF`, `RR`) używany w `InvoiceQueryFilters`.
  - `OpenOnlineSessionResponse`:
      - Dodano property `ValidUntil : DateTimeOffset`.
      - Zmiana modelu żądania w dokumentacji endpointu `QueryInvoiceMetadataAsync` (z `QueryInvoiceRequest` na `InvoiceMetadataQueryRequest`).
      - Zmiana namespace z `KSeFClient` na `KSeF.Client`.
- **Enums**
  - `InvoicePermissionType` → dodano wartości `RRInvoicing` oraz `PefInvoicing`.
  - `AuthorizationPermissionType` → dodano wartość `PefInvoicing`.
  - `KsefTokenPermissionType` → dodano wartości `SubunitManage`, `EnforcementOperations`, `PeppolId`.
  - `ContextIdentifierType (Tokens)` → nowy enum (`Nip`, `Pesel`, `Fingerprint`).
  - `PersonPermissionsTargetIdentifierType` → dodano wartość `AllPartners`.
  - `SubjectIdentifierType` → dodano wartość `PeppolId`.
- **Interfaces**
  - `IKSeFClient` → nowe metody:
    - `ExportInvoicesAsync` – `POST /api/v2/invoices/exports`.
    - `GetInvoiceExportStatusAsync` – `GET /api/v2/invoices/exports/{operationReferenceNumber}`.
    - `GetAttachmentPermissionStatusAsync` – poprawiony na `GET /api/v2/permissions/attachments/status`.
    - `SearchGrantedPersonalPermissionsAsync` – `POST /api/v2/permissions/query/personal/grants`.
    - `GrantsPermissionAuthorizationAsync` – `POST /api/v2/permissions/authorizations/grants`.
    - `QueryPeppolProvidersAsync` – `GET /api/v2/peppol/query`.
- **Tests**: `Authenticate.feature.cs` → dodano testy end-to-end procesu uwierzytelniania.

### Zmodyfikowane
- **authv2.xsd**
  - ➖ Usunięto:
    - Element `OnClientIpChange (tns:IpChangePolicyEnum)`.
    - Regułę unikalności `oneIp`.
    - Cały model `IpAddressPolicy` (`IpAddress`, `IpRange`, `IpMask`).
  - ➕ Dodano:
    - Element `AuthorizationPolicy` (zamiast `IpAddressPolicy`).
    - Nowy model `AllowedIps` z kolekcjami:
      - `Ip4Address` – pattern z walidacją zakresów IPv4 (0–255).
      - `Ip4Range` – rozszerzony pattern z walidacją zakresu adresów.
      - `Ip4Mask` – rozszerzony pattern z walidacją maski (`/8`, `/16`, `/24`, `/32`).
  - 🔧 Zmieniono `minOccurs/maxOccurs` dla `Ip4Address`, `Ip4Range`, oraz `Ip4Mask`: wcześniej `minOccurs="0" maxOccurs="unbounded"` → teraz `minOccurs="0" maxOccurs="10"`
  - Podsumowanie:
    - Zmieniono nazwę `IpAddressPolicy` → `AuthorizationPolicy`.
    - Wprowadzono precyzyjniejsze regexy dla IPv4.
    - Ograniczono maksymalną liczbę wpisów do 10.
- **Invoices**
  - `InvoiceMetadataQueryRequest` → usunięto `SchemaType`.
  - `PagedInvoiceResponse` → `TotalCount` opcjonalny.
  - `Seller.Identifier` → opcjonalny, dodano `Seller.Nip` jako wymagane.
  - `AuthorizedSubject.Identifier` → usunięty, dodano `AuthorizedSubject.Nip`.
  - `fileHash` → usunięty.
  - `invoiceHash` → dodany.
  - `invoiceType` → teraz `InvoiceType` zamiast `InvoiceMetadataInvoiceType`.
  - `InvoiceQueryFilters` → `InvoicingMode` stał się opcjonalny (`InvoicingMode?`), dodano `FormType`, usunięto `IsHidden`.
  - `SystemCodes.cs` → dodano kody systemowe dla PEF oraz zaktualizowano mapowanie pod `FormType.PEF`.
- **Permissions**
  - `EuEntityAdministrationPermissionsGrantRequest` → dodano wymagane `SubjectName`.
  - `ProxyEntityPermissions` → uspójniono nazewnictwo poprzez zmianę na `AuthorizationPermissions`.
- **Tokens**
  - `QueryKsefTokensAsync` → dodano parametry `authorIdentifier`, `authorIdentifierType`, `description`; usunięto domyślną wartość `pageSize=10`.
  - Poprawiono generowanie query string: `status` powtarzany zamiast listy `statuses`.

### Poprawki i zmiany dokumentacji
- Poprawiono i uzupełniono opisy działania metod w interfejsach `IAuthCoordinator` oraz `ISignatureService`.
- W implementacjach zastosowano `<inheritdoc />` dla spójności dokumentacji.

### Zmiany kryptografii
- Dodano obsługę ECDSA przy generowaniu CSR (domyślnie algorytm IEEE P1363, możliwość nadpisania na RFC 3279 DER).
- Zmieniono padding RSA z PKCS#1 na PSS zgodnie ze specyfikacją KSeF API w implementacji `SignatureService`.

### Usunięte
- **Invoices**
  - `AsyncQueryInvoicesAsync` i `GetAsyncQueryInvoicesStatusAsync` → zastąpione przez metody eksportu.
  - `AsyncQueryInvoiceRequest`, `AsyncQueryInvoiceStatusResponse` → usunięte.
  - `InvoicesExportRequest` → zastąpione przez `InvoiceExportRequest`.
  - `InvoicesExportPackage` → zastąpione przez `InvoicePackage`.
  - `InvoicesMetadataQueryRequest` → zastąpione przez `InvoiceQueryFilters`.
  - `InvoiceExportFilters` → włączone do `InvoiceQueryFilters`.


## Rejestr zmian: Wersja 2.0.0 RC4
### 1. KSeF.Client
  - Usunięto `Page` i `PageSize` i dodano `HasMore` w:
    - `PagedInvoiceResponse`,
    - `PagedPermissionsResponse<TPermission>`,
    - `PagedAuthorizationsResponse<TAuthorization>`,
    - `PagedRolesResponse<TRole>`,
    - `SessionInvoicesResponse`.
   - Usunięto `InternalId` z wartości enum `TargetIdentifierType` w `GrantPermissionsIndirectEntityRequest`.
   - Zmieniono odpowiedź z `SessionInvoicesResponse` na nową `SessionFailedInvoicesResponse` w odpowiedzi endpointu `/sessions/{referenceNumber}/invoices/failed`, metoda `GetSessionFailedInvoicesAsync`.
   - Zmieniono na opcjonalne pole `to` w `InvoiceMetadataQueryRequest`, `InvoiceQueryDateRange`, `InvoicesAsyncQueryRequest`.
   - Zmieniono `AuthenticationOperationStatusResponse` na nową `AuthenticationListItem` w `AuthenticationListResponse` w odpowiedzi endpointu `/auth/sessions`.
   - Zmieniono model `InvoiceMetadataQueryRequest` adekwatnie do kontraktu API.
   - Dodano pole `CertificateType` w `SendCertificateEnrollmentRequest`, `CertificateResponse`, `CertificateMetadataListResponse` oraz `CertificateMetadataListRequest`.
   - Dodano `WithCertificateType` w `GetCertificateMetadataListRequestBuilder` oraz `SendCertificateEnrollmentRequestBuilder`.
   - Dodano brakujące pole `ValidUntil` w modelu `Session`.
   - Zmieniono `ReceiveDate` na `InvoicingDate` w modelu `SessionInvoice`.

### 2. KSeF.DemoWebApp/Controllers
- **OnlineSessionController.cs**: ➕ `GET /send-invoice-correction`.


## Rejestr zmian: Wersja 2.0.0 (2025-07-14)
### 1. KSeF.Client  
Zmiana wersji .NET z 8.0 na 9.0.

### 1.1 Api/Services
- **AuthCoordinator.cs**:
  - ➕ Dodano dodatkowy log `Status.Details`.
  - ➕ Dodano wyjątek przy `Status.Code == 400`.
  - ➖ Usunięto `ipAddressPolicy`.
- **CryptographyService.cs**:
  - ➕ Inicjalizacja certyfikatów.
  - ➕ Pola `symmetricKeyEncryptionPem` oraz `ksefTokenPem`.
- **SignatureService.cs**: 🔧 `Sign(...)` → `SignAsync(...)`.
- **QrCodeService.cs**: ➕ nowa usługa do generowania QrCodes.
- **VerificationLinkService.cs**: ➕ nowa usługa generowania linków do weryfikacji faktury.

### 1.2 Api/Builders
- **SendCertificateEnrollmentRequestBuilder.cs**:
  - 🔧 `ValidFrom` pole zmienione na opcjonalne.
  - ➖ Interfejs `WithValidFrom`.
- **OpenBatchSessionRequestBuilder.cs**:
  - 🔧 `WithBatchFile(...)` usunięto parametr `offlineMode`.
  - ➕ `WithOfflineMode(bool)` nowy opcjonalny krok do oznaczenia trybu offline.

### 1.3 Core/Models
- **StatusInfo.cs**:
  - ➕ dodano property `Details`.
  - ➖ `BasicStatusInfo` - usunięto klasę w celu ujednolicenia statusów.
- **PemCertificateInfo.cs**: ➕ `PublicKeyPem` - dodano nowe property.
- **DateType.cs**: ➕ `Invoicing`, `Acquisition`, `Hidden` - dodano nowe enumeratory do filtrowania faktur.
- **PersonPermission.cs**: 🔧 `PermissionScope` zmieniono z PermissionType zgodnie ze zmianą w kontrakcie.
- **PersonPermissionsQueryRequest.cs**: 🔧 `QueryType` - dodano nowe wymagane property do filtrowania w zadanym kontekście.
- **SessionInvoice.cs**: 🔧 `InvoiceFileName` - dodano nowe property.
- **ActiveSessionsResponse.cs**: / `Status.cs` / `Item.cs` (Sessions): ➕ nowe modele.

### 1.4 Core/Interfaces
- **IKSeFClient.cs**: 🔧 `GetAuthStatusAsync` → zmiana modelu zwracanego z `BasicStatusInfo` na `StatusInfo`.
  - ➕ Dodano metodę GetActiveSessions(accessToken, pageSize, continuationToken, cancellationToken).
  - ➕ Dodano metodę RevokeCurrentSessionAsync(token, cancellationToken).
  - ➕ Dodano metodę RevokeSessionAsync(referenceNumber, accessToken, cancellationToken).
- **ISignatureService.cs**: 🔧 `Sign` → `SignAsync`.
- **IQrCodeService.cs**: nowy interfejs do generowania QRcodes.
- **IVerificationLinkService.cs**: ➕ nowy interfejs do tworzenia linków weryfikacyjnych do faktury.

### 1.5 DI & Dependencies
- **ServiceCollectionExtensions.cs**: ➕ rejestracja `IQrCodeService`, `IVerificationLinkService`.
- **ServiceCollectionExtensions.cs**: ➕ dodano obsługę nowej właściwości `WebProxy` z `KSeFClientOptions`.
- **KSeFClientOptions.cs**: 🔧 walidacja `BaseUrl`.
- **KSeFClientOptions.cs**:
  - ➕ dodano właściwości `WebProxy` typu `IWebProxy`.
  - ➕ Dodano CustomHeaders - umożliwia dodawanie dodatkowych nagłówków do klienta Http.
- **KSeF.Client.csproj**: ➕ `QRCoder` oraz `System.Drawing.Common`.

### 1.6 Http
- **KSeFClient.cs**:
  - ➕ Nagłówki `X-KSeF-Session-Id` oraz `X-Environment`.
  - ➕ `Content-Type: application/octet-stream`.

### 1.7 RestClient
- **RestClient.cs**: 🔧 `Uproszczona implementacja IRestClient'.

### 1.8 Usunięto
- **KSeFClient.csproj.cs**: ➖ `KSeFClient` - nadmiarowy plik projektu, który był nieużywany.

### 2. KSeF.Client.Tests
- **Nowe pliki**: `QrCodeTests.cs`, `VerificationLinkServiceTests.cs`
  - Wspólne: 🔧 `Thread.Sleep` → `Task.Delay`.
  - ➕ `ExpectedPermissionsAfterRevoke`; 4-krokowy flow; obsługa 400.
  - Wybrane: **Authorization.cs**, `EntityPermission*.cs`, **OnlineSession.cs**, **TestBase.cs**.


### 3. KSeF.DemoWebApp/Controllers
- **QrCodeController.cs**:
  - ➕ `GET /qr/certificate`.
  - ➕`/qr/invoice/ksef`.
  - ➕`qr/invoice/offline`.
- **ActiveSessionsController.cs**: ➕ `GET /sessions/active`.
- **AuthController.cs**:
  - ➕ `GET /auth-with-ksef-certificate`.
  - 🔧 fallback `contextIdentifier`.
- **BatchSessionController.cs**:
  - ➕ `WithOfflineMode(false)`.
  - 🔧 pętla `var`.
- **CertificateController.cs**:
  - ➕ `serialNumber`, `name`.
  - ➕ Builder.
- **OnlineSessionController.cs**:
  - ➕ `WithOfflineMode(false)`.
  - 🔧 `WithInvoiceHash`.

### 4. Podsumowanie

| Typ zmiany | Liczba plików |
|------------|---------------|
| ➕ dodane   | 12 |
| 🔧 zmienione| 33 |
| ➖ usunięte | 3 |


## Rejestr zmian: Wersja `2025-07-15`
### 1. KSeF.Client

#### 1.1 Api/Services
- **CryptographyService.cs**:
  - ➕ Dodano `EncryptWithEciesUsingPublicKey(byte[] content)` — domyślna metoda szyfrowania ECIES (ECDH + AES-GCM) na krzywej P-256.
  - 🔧 Metodę `EncryptKsefTokenWithRSAUsingPublicKey(...)` można przełączyć na ECIES lub zachować RSA-OAEP SHA-256 przez parametr `EncryptionMethod`.

- **AuthCoordinator.cs**:
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
  ```
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
- **ICryptographyService.cs**
  ➕ Dodano metody:
  ```csharp
  byte[] EncryptWithEciesUsingPublicKey(byte[] content);
  void EncryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv);
  ```

- **IAuthCoordinator.cs**
  🔧 `AuthKsefTokenAsync(...)` przyjmuje dodatkowy parametr:
  ```csharp
  EncryptionMethod encryptionMethod = EncryptionMethod.Ecies
  ```

### 2. KSeF.Client.Tests
- **AuthorizationTests.cs**: ➕ testy end-to-end `AuthKsefTokenAsync(...)` w wariantach `Ecies` i `Rsa`.
- **QrCodeTests.cs**: ➕ rozbudowano testy `BuildCertificateQr` o scenariusze z ECDSA P-256; poprzednie testy RSA pozostawione zakomentowane.
- **VerificationLinkServiceTests.cs**: ➕ dodano testy generowania i weryfikacji linków dla certyfikatów ECDSA P-256.
- **BatchSession.cs**: ➕ testy end-to-end dla wysyłki partów z wykorzystaniem strumieni.

### 3. KSeF.DemoWebApp/Controllers
- **QrCodeController.cs**: 🔧 Akcja `GetCertificateQr(...)` przyjmuje teraz opcjonalny parametr:
  ```csharp
  string privateKey = ""
  ```
  Jeśli nie jest podany, używany jest osadzony klucz w certyfikacie.


### Rozwiązania zgłoszonych: `2025-07-21`
- **#1 Metoda AuthCoordinator.AuthAsync() zawiera błąd**: 🔧 `KSeF.Client/Api/Services/AuthCoordinator.cs`: usunięto 2 linie zbędnego kodu challenge.
- **#2 Błąd w AuthController.cs**: 🔧 `KSeF.DemoWebApp/Controllers/AuthController.cs` - poprawiono logikę `AuthStepByStepAsync` (2 additions, 6 deletions) — fallback `contextIdentifier`.
- **#3 „Śmieciowa” klasa XadeSDummy**: 🔀 przeniesiono `XadeSDummy` z `KSeF.Client.Api.Services` do `WebApplication.Services` (zmiana namespace).
- **#4 Optymalizacja RestClient**: 🔧 `KSeF.Client/Http/RestClient.cs`: uproszczono przeciążenia `SendAsync` (24 additions, 11 deletions), usunięto dead-code, dodano performance benchmark `perf(#4)`.
- **#5 Uporządkowanie języka komunikatów**: ➕ `KSeF.Client/Resources/Strings.en.resx` & `Strings.pl.resx`: dodano 101 nowych wpisów w obu plikach; skonfigurowano lokalizację w DI.
- **#6 Wsparcie dla AOT**: ➕ `KSeF.Client/KSeF.Client.csproj`: dodano `<PublishAot>`, `<SelfContained>`, `<InvariantGlobalization>`, runtime identifiers `win-x64;linux-x64;osx-arm64`.
- **#7 Nadmiarowy plik KSeFClient.csproj**: ➖ Usunięto nieużywany plik projektu `KSeFClient.csproj` z repozytorium.

### Inne zmiany
- **QrCodeService.cs**: ➕ nowa implementacji PNG-QR (`GenerateQrCode`, `ResizePng`, `AddLabelToQrCode`).
- **PemCertificateInfo.cs**: ➖ Usunięto właściwości PublicKeyPem.
- **ServiceCollectionExtensions.cs**: ➕ konfiguracja lokalizacji (`pl-PL`, `en-US`) i rejestracji `IQrCodeService`/`IVerificationLinkService`.
- **AuthTokenRequest.cs**: dostosowanie serializacji XML do nowego schematu XSD.
- **README.md**: poprawione środowisko w przykładzie rejestracji KSeFClient w kontenerze DI.


## Rejestr zmian: Wersja `2025-08-31`
### KSeF.Client.Tests
**Utils**
- ➕ Nowe utils usprawniające uwierzytelnianie, obsługę sesji interaktywnych, wsadowych, zarządzanie uprawnieniami, oraz ich metody wspólne: **AuthenticationUtils.cs**, **OnlineSessionUtils.cs**, **MiscellaneousUtils.cs**, **BatchSessionUtils.cs**, **PermissionsUtils.cs**.
- 🔧 Refactor testów - użycie nowych klas utils.
- 🔧 Zmiana kodu statusu zamknięcia sesji interaktywnej z 300 na 170.
- 🔧 Zmiana kodu statusu zamknięcia sesji wsadowej z 300 na 150.
