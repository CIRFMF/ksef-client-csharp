> Info: 🔧 changed • ➕ added • ➖ deleted • 🔀 moved

## Changelog – Version 2.0.0 RC6.0.1

### New

- **Added descriptions of request builders in SDK**

### Modified

- **Changed `RestrictToPermanentStorageHwmDate` field to nullable**

## Changelog – Version 2.0.0 RC6.0

### New

- **Added `upoVersion` parameter** in `OpenBatchSessionAsync` and `OpenOnlineSessionAsync` methods
    - Allows you to select the UPO version (available values: `"upo-v4-3"` )
    - Sets the `X-KSeF-Feature` header with the appropriate version
    - Default: v4-2, from 5/01/2026 → v4-3
- **Added the ability to restore default API production limits in the TE environment**

### Modified

- **Added `SubjectDetail` in**
    - `GrantPermissionsAuthorizationRequest`
    - `GrantPermissionsPersonRequest`
    - `GrantPermissionsEuEntityRequest`
    - `GrantPermissionsIndirectEntityRequest`
    - `GrantPermissionsEntityRequest`
    - `GrantPermissionsSubunitRequest`
    - `GrantPermissionsEuEntityRepresentativeRequest` as per new API contract
- **Added `Extensions` property to `StatusInfo` object**

## Changelog – ## Version 2.0.0 RC5.7.2

### New

➖ Added validation of parameters passed in `(...)RequestBuilder` class methods, in accordance with the API documentation. ➖ Added the `TypeValueValidator` class, which allows for the verification of values assigned to `Type - Value` identifiers such as `ContextIdentifier` , `PersonTargetIdentifier` , etc.

### Modified

➖ Marked `TestDataSessionLimitsBase` class as 'obsolete' and replaced it with the `SessionLimits` class. ➖ Added a missing method in the `IOpenBatchSessionRequestBuilderBatchFile` interface. ➖ Added an E2E test demonstrating the possibility of using a certificate downloaded and saved on disk, along with its public key, to manage sessions and send invoices. ➖ Added methods to the CertificateUtils class. ➕ Added support for HWM as a benchmark for incremental invoice retrieval in the `IncrementalInvoiceRetrievalE2ETests` class ( `IncrementalInvoiceRetrieval_E2E_WithHwmShift` test).

## Changelog – ## Version 2.0.0 RC5.7.2

### New

- `EntityRoleType` → new enum ( `CourtBailiff` , `EnforcementAuthority` , `LocalGovernmentUnit` , `LocalGovernmentSubUnit` , `VatGroupUnit` , `VatGroupSubUnit` ) used in `EntityRole`
- `SubordinateEntityRoleType` → new enum ( `LocalGovernmentSubUnit` , `VatGroupSubUnit` ) used in `SubordinateEntityRole`
- Dependencies have been separated across .NET SDK versions.
- EditorConfig: C# 7.3, NRT off, force explicit types, Async*…Async, _underscore for private and protected fields.
- KSeF.Client.Api: Polish descriptions for public interfaces/types.
- Utils: ToVatEuFromDomestic(...) – improved heuristics and messages in Polish.

### Modified

- Renamed `EuEntityPermissionsQueryPermissionType` → `EuEntityPermissionType`
- `PersonPermission` `PermissionScope` field changed type from `string` to enum `PersonPermissionType`
     (report: https://github.com/CIRFMF/ksef-client-csharp/issues/131)
- `PersonPermission` `PermissionState` field changed from `string` to enum `PersonPermissionState`
- `EntityRole` `Role` field changed from `string` to enum `EntityRoleType`
- `SubordinateEntityRole` `Role` field changed from `string` to enum `SubordinateEntityRoleType`
- `AuthorizationGrant` `PermissionScope` field type changed from `string` to enum `AuthorizationPermissionType`
- `EuEntityPermission` `PermissionScope` field changed type from `string` to enum `EuEntityPermissionType`

## Changelog – ## Version 2.0.0 RC5.7.1

### New

- **KSeF.Client** 🔧➕

    - Split `IKSefClient` into smaller interfaces:
        - `IActiveSessionsClient`
        - `IAuthorizationClient`
        - `IBatchSessionClient`
        - `ICertificateClient`
        - `ICryptographyClient`
        - `IGrantPermissionClient`
        - `IInvoiceDownloadClient`
        - `IKSeFClient`
        - `IKsefTokenClient`
        - `ILimitsClient`
        - `IOnlineSessionClient`
        - `IPeppolClient`
        - `IPermissionOperationClient`
        - `IRevokePermissionClient`
        - `ISearchPermissionClient`
        - `ISessionStatusClient`

    and added smaller implementations of them.

### Modified

➖ Removed the `ApiException` class and replaced its use in summary with the `KsefApiException` class

## Changelog – ## Version 2.0.0 RC5.7

### New

- **API Responses** - added a set of classes representing operation status responses:
    - `AuthenticationStatusCodeResponse`
    - `CertificateStatusCodeResponse`
    - `InvoiceExportStatusCodeResponse`
    - `InvoiceInSessionStatusCodeResponse`
    - `OperationStatusCodeResponse`
- **Operation Status Codes** - Added new status code **550 - "OperationCancelled"**

### Modified

- `BatchFilePartInfo` - `FileName` field marked as **Obsolete** (planned to be removed in future versions).

## Changelog – ## Version 2.0.0 RC5.6

### New

- **PdfTestApp** ➕
    - Added `KSeF.Client.Tests.PdfTestApp` console application for automatic generation of PDF visualizations of KSeF invoices and UPO documents.
    - Supports PDF generation for both invoices ( `faktura` , `invoice` ) and UPO documents ( `upo` ).
    - Automatic installation of dependencies: npm packages, Chromium (Playwright).
    - Documentation in README.md with installation instructions and usage examples.

## Changelog – ## Version 2.0.0 RC5.5

### New

- **Permissions / Builder** - added `EntityAuthorizationsQueryRequestBuilder` with `ReceivedForOwnerNip(string ownerNip)` step for **Received** queries in the context of **the owner's NIP** ; optionally `WithPermissionTypes(IEnumerable<InvoicePermissionType>)` .
- **E2E – AuthorizationPermissions** – two scenarios have been added: "Downloading the list of received entity permissions as an owner in the context of the Tax Identification Number":
    - `...ReceivedOwnerNip_Direct_FullFlow_ShouldFindGrantedPermission` .
    - `...ReceivedOwnerNip_Builder_FullFlow_ShouldFindGrantedPermission` (builder variant).
- E2E - Upo** - added a test checking all available UPO download methods using the example of an online session:
    - `KSeF.Client.Tests.Core.E2E.OnlineSession.Upo.UpoRetrievalAsync_FullIntegrationFlow_AllStepsSucceed` .
- E2E: Retrieving the list **of my permissions** in the current **NIP** context (owner) – `PersonPermissions_OwnerNip_MyPermissions_E2ETests` test.
- E2E: **Granted permissions** (owner, NIP context) with filtering:
    - by the authorized person's **PESEL number** — `PersonPermissions_OwnerNip_Granted_FilterAuthorizedPesel_E2ETests`
    - by **fingerprint (SHA-256 fingerprint)** of the authorized person - `PersonPermissions_OwnerNip_Granted_FilterAuthorizedFingerprint_E2ETests`
- E2E "Granted authorizations" (owner, NIP context) with filtering by **the NIP of the authorized person**
- **E2E – PersonalPermissions** : Downloading the list of **valid permissions** to work in KSeF as **a PESEL authorized person** in **the NIP context** — `PersonalPermissions_AuthorizedPesel_InNipContext_E2ETests` .
- **NuGet Packages** : Published NuGet packages and added installation instructions.
- **KSeF.Client.Core** - added `EffectiveApiRateLimits` and `EffectiveApiRateLimitValues` for `/rate-limits` .
- **LimitsClient** - added support for GET `/rate-limits` endpoint:
    - `GetRateLimitsAsync(...)`
- **TestDataClient** - added support for POST and DELETE endpoints `/testdata/rate-limits` :
    - `SetRateLimitsAsync(...)`
    - `RestoreRateLimitsAsync(...)`
- **E2E - Enforcement Operations**
    - `EnforcementOperationsE2ETests`
    - `EnforcementOperationsNegativeE2ETests`
        - Added E2E tests for granting permissions to perform bailiff operations.

### Modified

- `EntityAuthorizationsAuthorizingEntityIdentifier` field `Type` changed from `string` to enum `AuthorizedIdentifierType` .
- `EntityAuthorizationsAuthorizedEntityIdentifier` `Type` field changed from `string` to enum `AuthorizedIdentifier` .
- **Tests / Utils - Upo** - moved helper methods for retrieving UPO from test classes to KSeF.Client.Tests.Utils.Upo.UpoUtils:
    - `...GetSessionInvoiceUpoAsync` ,
    - `...GetSessionUpoAsync` ,
    - `...GetUpoAsync`
- `KSeF.Client.Tests.Core.E2E.OnlineSession.OnlineSessionE2ETests.OnlineSessionAsync_FullIntegrationFlow_AllStepsSucceed` simplified test by downloading UPO from the address provided in the metadata of the downloaded invoice,
- `KSeF.Client.Tests.Core.E2E.BatchSession.BatchSessionStreamE2ETests.BatchSession_StreamBased_FullIntegrationFlow_ReturnsUpo` simplified the test by downloading UPO from the address provided in the metadata of the downloaded invoice,
- `KSeF.Client.Tests.Core.E2E.BatchSession.BatchSessionE2ETests.BatchSession_FullIntegrationFlow_ReturnsUpo` simplified the test by downloading UPO from the address provided in the metadata of the downloaded invoice
- **ServiceCollectionExtensions - AddCryptographyClient**
- `KSeF.Client.DI.ServiceCollectionExtensions.AddCryptographyClient` : The configuration method registering the client and the cryptographic service (HostedService) has been modified. The startup mode has been removed from the options. The AddCryptographyClient() method now accepts two optional parameters:
    - delegate for retrieving KSeF public certificates (by default it is the GetPublicCertificatesAsync() method in CryptographyClient)
    - value from the CryptographyServiceWarmupMode enum (Blocking by default). The operation of each mode is described in CryptographyServiceWarmupMode.cs. Example usage: `KSeF.DemoWebApp.Program.cs line 24` Example of registering a service and cryptographic client without using a host (omitting AddCryptographyClient): `KSeF.Client.Tests.Core.E2E.TestBase.cs line 48-74`
- `KSeF.Client.Core` - Cleaned up the structure and clarified the model and enum names. Models needed to manipulate test data are now located in the TestData (formerly Tests) folder. Unused classes and enums have been removed.
- `EntityAuthorizationsAuthorizingEntityIdentifier` `Type` field changed from `string` to enum `EntityAuthorizationsAuthorizingEntityIdentifierType` .
- `EntityAuthorizationsAuthorizedEntityIdentifier` `Type` field changed from `string` to enum `EntityAuthorizationsAuthorizedEntityIdentifierType` .
- Corrected markings of optional fields in `SessionInvoice` .

### Deleted

- **TestDataSessionLimitsBase**
    - `MaxInvoiceSizeInMib` and `MaxInvoiceWithAttachmentSizeInMib` fields removed.

### Note/Compatibility

- `KSeF.Client.Core` - renaming some models, adjusting namespace to the changed file and directory structure.
- `KSeF.Client.DI.ServiceCollectionExtensions.AddCryptographyClient` the configuration method registering the client and the cryptographic service (HostedService) has been modified.

---

# Changelog – ## Version 2.0.0 RC5.4.0

---

### New

- `QueryInvoiceMetadataAsync` - Added `sortOrder` parameter to specify the sorting direction of the results.

### Modified

- Calculating the number of parcel parts based on the parcel size and established limits
- Naming adjustment - change from `OperationReferenceNumber` to `ReferenceNumber`
- Extended Permission Test Scenarios
- Extended TestData test scenarios

---

# Changelog – ## Version 2.0.0 RC5.3.0

---

### New

- **REST / Routing**
    - `IRouteBuilder` + `RouteBuilder` – centralized path building ( `/api/v2/...` ) with optional `apiVersion` . ➕
- **REST / Types and MIME**
    - `RestContentType` + `ToMime()` – unambiguous mapping `Json|Xml` → `application/*` . ➕
- **REST / Customer Database**
    - `ClientBase` — common base class for HTTP clients; centralization of URL construction (via `RouteBuilder` );
- **REST / LimitsClient**
    - `ILimitsClient` , `LimitsClient` - API support **Limits** : `GetLimitsForCurrentContext` , `GetLimitsForCurrentSubject` ;
- **Tests / TestClient**
    - `ITestClient` , `TestClient` - the client provides operations: `CreatePersonAsync` , `RemovePersonAsync` , `CreateSubjectAsync` , `GrantTestDataPermissionsAsync` . ➕
- **Tests / PEF**
    - Extended E2E PEF (Peppol) Scenarios – Status and Permission Assertions. ➕
- **TestData / Requests**
    - Test environment request models: `PersonCreateRequest` , `PersonRemoveRequest` , `SubjectCreateRequest` , `TestDataPermissionsGrantRequest` . ➕
- **Templates**
    - PEF correction template: `invoice-template-fa-3-pef-correction.xml` (for testing purposes). ➕

### Modified

- **REST / Client**
    - Refactor: generic `RestRequest<TBody>` and bodyless variant; fluent‑consistent `WithBody(...)` , `WithAccept(...)` , `WithTimeout(...)` , `WithApiVersion(...)` methods. 🔧
    - Reduced duplicates in `IRestClient.SendAsync(...)` ; more precise error messages. 🔧
    - MIME and header order – uniform `Content-Type` / `Accept` setting. 🔧
    - Updated interface signatures (internal) to match the new REST framework. 🔧
- **Routing / Consistency**
    - Consolidate prefixes in one place (RouteBuilder) instead of duplicating `"/api/v2"` in clients/tests. 🔧
- **System codes / PEF**
    - Updated system code and version mappings for **PEF** (serialization/mapping). 🔧
- **Tests / Utils**
    - `AsyncPollingUtils` – more stable retry/backoff, clearer conditions. 🔧
- **Code style**
    - `var` → explicit types; `ct` → `cancellationToken` ; property order; removed `unused using` . 🔧

### Deleted

- **REST**
    - Redundant `SendAsync(...)` overloads and helper fragments in the REST client (after refactoring). ➖

### Documentation corrections and changes

- Clarified `<summary>` /exception descriptions in interfaces and consistent naming in tests and requests (PEF/TestData). 🔧

**Note (compatibility)** : changes to `IRestClient` / `RestRequest*` are **internal** – the public `IKSeFClient` contract remains unchanged in this RC. If you've extended the REST layer, review the integrations for the new `RouteBuilder` and generic `RestRequest<TBody>` . 🔧

---

# Changelog – ## Version 2.0.0 RC5.2.0

---

### New

- **Cryptography**
    - Support for ECDSA (elliptic curves, P-256) in CSR generation ➕
    - ECIES (ECDH + AES-GCM) as an alternative to KSeF token encryption ➕
    - `ICryptographyService` :
        - `GenerateCsrWithEcdsa(...)` ➕
        - `EncryptWithECDSAUsingPublicKey(byte[] content)` (ECIES: SPKI + nonce + tag + ciphertext) ➕
        - `GetMetaDataAsync(Stream, ...)` ➕
        - `EncryptStreamWithAES256(Stream, ...)` and `EncryptStreamWithAES256Async(Stream, ...)` ➕
- **CertTestApp** ➕
    - Added the ability to export created certificates to PFX and CER files in the `--output file` mode.
- **Build** ➕
    - Strong name library signing: added `.snk` files and enabled signing for `KSeF.Client` and `KSeF.Client.Core` .
- **Tests / Features** ➕
    - `.feature` (authentication, sessions, invoices, permissions) and E2E (certificate lifecycle, invoice export) scenarios have been extended.

### Modified

- **Cryptography** 🔧
    - Improved ECDSA CSR generation and file metadata calculation; added support for working with streams ( `GetMetaData(...)` , `GetMetaDataAsync(...)` , `EncryptStreamWithAES256(...)` ).
- **API Models / Contracts** 🔧
    - Adapted models to current API contracts; standardized export and invoice metadata models ( `InvoicePackage` , `InvoicePackagePart` , `ExportInvoicesResponse` , `InvoiceExportRequest` , `GrantPermissionsSubUnitRequest` , `PagedInvoiceResponse` ).
- **Demo (QrCodeController)** 🔧
    - QR labels and certificate verification in verification links.

### Documentation corrections and changes

- **README** 🔧
    - Clarified DI registration and certificate export description in CertTestApp.
- **Core** 🔧
    - `EncryptionMethodEnum` with values `ECDsa` , `Rsa` (preparation for selecting the encryption method).

---

---

# Changelog – ## Version 2.0.0 RC5.1.1

---

### New

- **KSeF Client**

    - Cryptographic service disabled from KSeF client 🔧
    - DTO models have been separated into a separate `KSeF.Client.Core` project, which is compliant with `NET Standard 2.0` ➕

- **CertTestApp** ➕

    - Added a console application to illustrate the creation of a sample, test certificate and XAdES signature.

- **Crypto client**

    - new `CryptographyClient` ➕

- **organizing the project**

    - Namespace changes preparing for further separation of services from the KSeF client 🔧
    - added new DI configuration for crypto client 🔧

---

# Changelog – ## Version 2.0.0 RC5.1

---

### New

- **Tests**
    - Handling `KsefApiException` (e.g. 403 *Forbidden* ) in session and E2E scenarios.

### Modified

- **Invoices / Export**
    - `ExportInvoicesResponse` – `Status` field removed; after `ExportInvoicesAsync` use `GetInvoiceExportStatusAsync(operationReferenceNumber)` .
- **Invoices / Metadata**
    - `pageSize` – allowed range **10–250** (updated tests: “outside 10–250”).
- **Tests (E2E)**
    - Invoice fetching: retry **5 → 10** , precise `catch` for `KsefApiException` , `IsNullOrWhiteSpace` assertions.
- **Utils**
    - `OnlineSessionUtils` – **`PL`** prefix for `supplierNip` and `customerNip` .
- **Peppol tests**
    - The use of the NIP number has been changed to the format with `PL...` prefix.
    - Added assertion in PEF tests if invoice remains in *processing* status.
- **Permissions**
    - Adapting models and tests to the new API contract.

### Deleted

- **Invoices / Export**
    - `ExportInvoicesResponse.Status` .

### Corrections and changes to documentation

- Examples of export without `Status` .
- Exception description ( `KsefApiException` , 403 *Forbidden* ).
- `pageSize` limit updated to **10–250** .

---

# Changelog – ### Version 2.0.0 RC5

---

### New

- **Auth**
    - `ContextIdentifierType` → added `PeppolId` value
    - `AuthenticationMethod` → added `PeppolSignature` value
    - `AuthTokenRequest` → new property `AuthorizationPolicy`
    - `AuthorizationPolicy` → new model replacing `IpAddressPolicy`
    - `AllowedIps` → new model with `Ip4Address` , `Ip4Range` , `Ip4Mask` lists
    - `AuthTokenRequestBuilder` → new method `WithAuthorizationPolicy(...)`
    - `ContextIdentifierType` → added `PeppolId` value
- **Models**
    - `StatusInfo` → added property `StartDate` , `AuthenticationMethod`
    - `AuthorizedSubject` → new model ( `Nip` , `Name` , `Role` )
    - `ThirdSubjects` → new model ( `IdentifierType` , `Identifier` , `Name` , `Role` )
    - `InvoiceSummary` → added property `HashOfCorrectedInvoice` , `AuthorizedSubject` , `ThirdSubjects`
    - `AuthenticationKsefToken` → added property `LastUseDate` , `StatusDetails`
    - `InvoiceExportRequest` , `ExportInvoicesResponse` , `InvoiceExportStatusResponse` , `InvoicePackage` → new invoice export models (replace previous ones)
    - `FormType` → new enum ( `FA` , `PEF` , `RR` ) used in `InvoiceQueryFilters`
    - `OpenOnlineSessionResponse`
        - added property `ValidUntil : DateTimeOffset`
        - change of the request model in the `QueryInvoiceMetadataAsync` endpoint documentation (from `QueryInvoiceRequest` to `InvoiceMetadataQueryRequest` )
        - namespace change from `KSeFClient` to `KSeF.Client`
- **Enums**
    - `InvoicePermissionType` → added values `RRInvoicing` , `PefInvoicing`
    - `AuthorizationPermissionType` → added `PefInvoicing` value
    - `KsefTokenPermissionType` → added values `SubunitManage` , `EnforcementOperations` , `PeppolId`
    - `ContextIdentifierType (Tokens)` → new enum ( `Nip` , `Pesel` , `Fingerprint` )
    - `PersonPermissionsTargetIdentifierType` → added value `AllPartners`
    - `SubjectIdentifierType` → `PeppolId` value added
- **Interfaces**
    - `IKSeFClient` → new methods:
        - `ExportInvoicesAsync` – `POST /api/v2/invoices/exports`
        - `GetInvoiceExportStatusAsync` – `GET /api/v2/invoices/exports/{operationReferenceNumber}`
        - `GetAttachmentPermissionStatusAsync` – fixed to `GET /api/v2/permissions/attachments/status`
        - `SearchGrantedPersonalPermissionsAsync` – `POST /api/v2/permissions/query/personal/grants`
        - `GrantsPermissionAuthorizationAsync` – `POST /api/v2/permissions/authorizations/grants`
        - `QueryPeppolProvidersAsync` – `GET /api/v2/peppol/query`
- **Tests**
    - `Authenticate.feature.cs` → added end-to-end tests for the authentication process.

### Modified

- **authv2.xsd**
    - ➖ Removed:
        - element `OnClientIpChange (tns:IpChangePolicyEnum)`
        - `oneIp` uniqueness rule
        - the entire `IpAddressPolicy` model ( `IpAddress` , `IpRange` , `IpMask` )
    - Added:
        - `AuthorizationPolicy` element (instead of `IpAddressPolicy` )
        - new `AllowedIps` model with collections:
            - `Ip4Address` – pattern with validation of IPv4 ranges (0–255)
            - `Ip4Range` – extended pattern with address range validation
            - `Ip4Mask` – extended pattern with mask validation ( `/8` , `/16` , `/24` , `/32` )
    - Changed:
        - `minOccurs/maxOccurs` for `Ip4Address` , `Ip4Range` , `Ip4Mask` :
             previously `minOccurs="0" maxOccurs="unbounded"` → now `minOccurs="0" maxOccurs="10"`
    - Summary:
        - Renamed `IpAddressPolicy` → `AuthorizationPolicy`
        - More precise regexes for IPv4 have been introduced
        - The maximum number of entries has been limited to 10
    - **Invoices**
        - `InvoiceMetadataQueryRequest` → removed `SchemaType`
        - `PagedInvoiceResponse` → `TotalCount` optional
        - `Seller.Identifier` → optional, added `Seller.Nip` as required
        - `AuthorizedSubject.Identifier` → removed, added `AuthorizedSubject.Nip`
        - `fileHash` → deleted
        - `invoiceHash` → added
        - `invoiceType` → now `InvoiceType` instead of `InvoiceMetadataInvoiceType`
        - `InvoiceQueryFilters` → `InvoicingMode` became optional ( `InvoicingMode?` ), added `FormType` , removed `IsHidden`
        - `SystemCodes.cs` → added system codes for PEF and updated mapping under `FormType.PEF`
    - **Permissions**
        - `EuEntityAdministrationPermissionsGrantRequest` → added required `SubjectName`
        - `ProxyEntityPermissions` → the naming has been made more consistent by changing it to `AuthorizationPermissions`
    - **Tokens**
        - `QueryKsefTokensAsync` → added `authorIdentifier` , `authorIdentifierType` , `description` parameters; removed default value `pageSize=10`
        - improved query string generation: `status` repeated instead of a list `statuses`

### Corrections and changes to documentation

- the descriptions of the methods in `IAuthCoordinator` and `ISignatureService` interfaces have been improved and supplemented
    - `<inheritdoc />` is used in implementations for documentation consistency

### Cryptography changes

- added ECDSA support for CSR generation (IEEE P1363 algorithm by default, can be overridden with RFC 3279 DER)
- changed RSA padding from PKCS#1 to PSS according to KSeF API specification in `SignatureService` implementation

### Deleted

- **Invoices**
    - `AsyncQueryInvoicesAsync` and `GetAsyncQueryInvoicesStatusAsync` → replaced by export methods
    - `AsyncQueryInvoiceRequest` , `AsyncQueryInvoiceStatusResponse` → deleted
    - `InvoicesExportRequest` → replaced by `InvoiceExportRequest`
    - `InvoicesExportPackage` → replaced by `InvoicePackage`
    - `InvoicesMetadataQueryRequest` → replaced by `InvoiceQueryFilters`
    - `InvoiceExportFilters` → incorporated into `InvoiceQueryFilters`

---

# Changelog – ### Version 2.0.0 RC4

---

## 1. KSeF.Client

- Removed `Page` and `PageSize` and added `HasMore` in:
    - `PagedInvoiceResponse`
    - `PagedPermissionsResponse<TPermission>`
    - `PagedAuthorizationsResponse<TAuthorization>`
    - `PagedRolesResponse<TRole>`
    - `SessionInvoicesResponse`
- Removed `InternalId` from the `TargetIdentifierType` enum value in `GrantPermissionsIndirectEntityRequest`
- Changed the response from `SessionInvoicesResponse` to the new `SessionFailedInvoicesResponse` in the response of the endpoint `/sessions/{referenceNumber}/invoices/failed` , method `GetSessionFailedInvoicesAsync` .
- Changed to optional field `to` in `InvoiceMetadataQueryRequest` , `InvoiceQueryDateRange` , `InvoicesAsyncQueryRequest` .
- Changed `AuthenticationOperationStatusResponse` to the new `AuthenticationListItem` in `AuthenticationListResponse` in the `/auth/sessions` endpoint response.
- `InvoiceMetadataQueryRequest` model has been changed to match the API contract.
- Added `CertificateType` field in `SendCertificateEnrollmentRequest` , `CertificateResponse` , `CertificateMetadataListResponse` and `CertificateMetadataListRequest` .
- Added `WithCertificateType` in `GetCertificateMetadataListRequestBuilder` and `SendCertificateEnrollmentRequestBuilder` .
- Added missing `ValidUntil` field in `Session` model.
- Changed `ReceiveDate` to `InvoicingDate` in `SessionInvoice` model.

## 2. KSeF.DemoWebApp/Controllers

- **OnlineSessionController.cs** : ➕ `GET /send-invoice-correction` - Example of implementation and use of technical correction

---

```

```

# Changelog – `## 2.0.0 (2025-07-14)` (KSeF.Client)

---

## 1. KSeF.Client

.NET 8.0 to .NET 9/0 upgrade

### 1.1 API/Services

- **AuthCoordinator.cs** : 🔧 Added additional `Status.Details` log; 🔧 Added exception for `Status.Code == 400` ; ➖ Removed `ipAddressPolicy`
- **CryptographyService.cs** : ➕ certificate initialization; ➕ `symmetricKeyEncryptionPem` , `ksefTokenPem` fields
- **SignatureService.cs** : 🔧 `Sign(...)` → `SignAsync(...)`
- **QrCodeService.cs** : ➕ new service for generating QrCodes
- **VerificationLinkService.cs** : ➕ new invoice verification link generation service

### 1.2 API/Builders

- **SendCertificateEnrollmentRequestBuilder.cs** : 🔧 `ValidFrom` field changed to optional ; ➖ `WithValidFrom` interface
- **OpenBatchSessionRequestBuilder.cs** : 🔧 `WithBatchFile(...)` removed `offlineMode` parameter ; ➕ `WithOfflineMode(bool)` new optional step to mark offline mode

### 1.3 Core/Models

- **StatusInfo.cs** : 🔧 added `Details` property; ➖ `BasicStatusInfo` - removed the class in the status unification tool
- **PemCertificateInfo.cs** : ➕ `PublicKeyPem` - new property added
- **DateType.cs** : ➕ `Invoicing` , `Acquisition` , `Hidden` - new enumerators added for filtering invoices
- **PersonPermission.cs** : 🔧 `PermissionScope` changed from PermissionType according to change in contract
- **PersonPermissionsQueryRequest.cs** : 🔧 `QueryType` - added new required property for filtering in a given context
- **SessionInvoice.cs** : 🔧 `InvoiceFileName` - new property added
- **ActiveSessionsResponse.cs** / `Status.cs` / `Item.cs` (Sessions): ➕ new models

### 1.4 Core/Interfaces

- **IKSeFClient.cs** : 🔧 `GetAuthStatusAsync` → change of the return model from `BasicStatusInfo` to `StatusInfo` ➕ Added the GetActiveSessions(accessToken, pageSize, continuationToken, cancellationToken) method ➕ Added the RevokeCurrentSessionAsync(token, cancellationToken) method ➕ Added the RevokeSessionAsync(referenceNumber, accessToken, cancellationToken) method
- **ISignatureService.cs** : 🔧 `Sign` → `SignAsync`
- **IQrCodeService.cs** : new interface for generating QRcodes
- **IVerificationLinkService.cs** : ➕ new interface for creating invoice verification links

### 1.5 DI &amp; Dependencies

- **ServiceCollectionExtensions.cs** : ➕ registration `IQrCodeService` , `IVerificationLinkService`
- **ServiceCollectionExtensions.cs** : ➕ added support for new `WebProxy` property from `KSeFClientOptions`
- **KSeFClientOptions.cs** : 🔧 `BaseUrl` validation
- **KSeFClientOptions.cs** : ➕ Added `WebProxy` properties of type `IWebProxy` ➕ Added CustomHeaders - allows adding additional headers to the Http client
- **KSeF.Client.csproj** : ➕ `QRCoder` , `System.Drawing.Common`

### 1.6 Http

- **KSeFClient.cs** : ➕ headers `X-KSeF-Session-Id` , `X-Environment` ; ➕ `Content-Type: application/octet-stream`

### 1.7 RestClient

- **RestClient.cs** : 🔧 `Simplified IRestClient implementation'

### 1.8 Removed

- **KSeFClient.csproj.cs** : ➖ `KSeFClient` - a redundant project file that was unused

---

## 2. KSeF.Client.Tests

## **New files** : `QrCodeTests.cs` , `VerificationLinkServiceTests.cs`
 Common: 🔧 `Thread.Sleep` → `Task.Delay` ; ➕ `ExpectedPermissionsAfterRevoke` ; 4-step flow; 400 handling
 Selected: **Authorization.cs** , `EntityPermission*.cs` , **OnlineSession.cs** , **TestBase.cs**

## 3. KSeF.DemoWebApp/Controllers

- **QrCodeController.cs** : ➕ `GET /qr/certificate` ➕ `/qr/invoice/ksef` ➕ `qr/invoice/offline`
- **ActiveSessionsController.cs** : ➕ `GET /sessions/active`
- **AuthController.cs** : ➕ `GET /auth-with-ksef-certificate` ; 🔧 fallback `contextIdentifier`
- **BatchSessionController.cs** : ➕ `WithOfflineMode(false)` ; 🔧 `var` loop
- **CertificateController.cs** : ➕ `serialNumber` , `name` ; ➕builder
- **OnlineSessionController.cs** : ➕ `WithOfflineMode(false)` 🔧 `WithInvoiceHash`

---

## 4. Summary

Type of change | Number of files
--- | ---
➕ added | 12
🔧 changed | 33
➖ deleted | 3

---

## [next-version] – `2025-07-15`

### 1. KSeF.Client

#### 1.1 API/Services

- **CryptographyService.cs**

    - ➕ Added `EncryptWithEciesUsingPublicKey(byte[] content)` - default ECIES (ECDH + AES-GCM) encryption method on P-256 curve.
    - 🔧 The `EncryptKsefTokenWithRSAUsingPublicKey(...)` method can be switched to ECIES or keep RSA-OAEP SHA-256 via the `EncryptionMethod` parameter.

- **AuthCoordinator.cs**

    - 🔧 `AuthKsefTokenAsync(...)` signature extended with an optional parameter:

    ```csharp
    EncryptionMethod encryptionMethod = EncryptionMethod.Ecies
    ```

    — ECIES by default, with possible fallback to RSA.

#### 1.2 Core/Models

- **EncryptionMethod.cs**
     ➕ New enum:
    ```csharp
    public enum EncryptionMethod
    {
        Ecies,
        Rsa
    }
    ```
- **InvoiceSummary.cs** ➕ New fields added:
    ```csharp
      public DateTimeOffset IssueDate { get; set; }
      public DateTimeOffset InvoicingDate { get; set; }
      public DateTimeOffset PermanentStorageDate { get; set; }
    ```
- **InvoiceMetadataQueryRequest.cs**
     🔧 New types without the `Name` field have been added to `Seller` and `Buyer` :

#### 1.3 Core/Interfaces

- **ICryptographyService.cs** ➕ Methods added:

    ```csharp
    byte[] EncryptWithEciesUsingPublicKey(byte[] content);
    void EncryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv);
    ```

- **IAuthCoordinator.cs** 🔧 `AuthKsefTokenAsync(...)` takes an additional parameter:

    ```csharp
    EncryptionMethod encryptionMethod = EncryptionMethod.Ecies
    ```

---

### 2. KSeF.Client.Tests

- **AuthorizationTests.cs** ➕ End-to-end tests for `AuthKsefTokenAsync(...)` in `Ecies` and `Rsa` variants.

- **QrCodeTests.cs** ➕ Extended `BuildCertificateQr` tests to include ECDSA P-256 scenarios; left previous RSA tests commented out.

- **VerificationLinkServiceTests.cs** ➕ Added link generation and verification tests for ECDSA P-256 certificates.

- **BatchSession.cs** ➕ End-to-end tests for batch dispatch using streams.

---

### 3. KSeF.DemoWebApp/Controllers

- **QrCodeController.cs** 🔧 The `GetCertificateQr(...)` action now accepts an optional parameter:

    ```csharp
    string privateKey = ""
    ```

    - if not provided, the embedded key in the certificate is used.

---

```

```

## Solutions submitted - `2025-07-21`

- **#1 AuthCoordinator.AuthAsync() method contains an error**
     🔧 `KSeF.Client/Api/Services/AuthCoordinator.cs` : removed 2 lines of unnecessary challenge code

- **#2 Error in AuthController.cs**
     🔧 `KSeF.DemoWebApp/Controllers/AuthController.cs` : improved `AuthStepByStepAsync` logic (2 additions, 6 deletions) — fallback `contextIdentifier`

- **#3 XadeSDummy's "Trash" Class**
     🔀 Moved `XadeSDummy` from `KSeF.Client.Api.Services` to `WebApplication.Services` (namespace change) after

- **#4 RestClient Optimization**
     🔧 `KSeF.Client/Http/RestClient.cs` : simplified `SendAsync` overloads (24 additions, 11 deletions), removed dead-code, added performance benchmark `perf(#4)`

- **#5 Organizing the language of messages**
     ➕ `KSeF.Client/Resources/Strings.en.resx` &amp; `Strings.pl.resx` : added 101 new entries in both files; configured location in DI

- **#6 Support for AOT**
     ➕ `KSeF.Client/KSeF.Client.csproj` : added `<PublishAot>` , `<SelfContained>` , `<InvariantGlobalization>` , runtime identifiers `win-x64;linux-x64;osx-arm64`

- **#7 Redundant KSeFClient.csproj file**
     ➖ Removed unused `KSeFClient.csproj` project file from the repository

---

## Other changes

- **QrCodeService.cs** : ➕ new PNG-QR implementation ( `GenerateQrCode` , `ResizePng` , `AddLabelToQrCode` );

- **PemCertificateInfo.cs** : ➖ Removed PublicKeyPem properties;

- **ServiceCollectionExtensions.cs** : ➕ localization configuration ( `pl-PL` , `en-US` ) and `IQrCodeService` / `IVerificationLinkService` registration

- **AuthTokenRequest.cs** : adapting XML serialization to the new XSD schema

- **README.md** : improved environment in the KSeFClient registration example in the DI container.

---

```

```

## [next-version] – `2025-08-31`

---

### 2. KSeF.Client.Tests

- **Utils** ➕ New utils improving authentication, handling interactive and batch sessions, permission management, and their common methods: **AuthenticationUtils.cs** , **OnlineSessionUtils.cs** , **MiscellaneousUtils.cs** , **BatchSessionUtils.cs** , **PermissionsUtils.cs** . 🔧 Refactor tests - using new utils classes. 🔧 Changed the status code for closing an interactive session from 300 to 170. 🔧 Changed the status code for closing a batch session from 300 to 150.

---
