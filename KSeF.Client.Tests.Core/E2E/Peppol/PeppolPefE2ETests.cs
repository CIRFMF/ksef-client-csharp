using KSeF.Client.Api.Builders.AuthorizationPermissions;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Peppol;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace KSeF.Client.Tests.Core.E2E.Peppol;

/// <summary>
/// Scenariusz E2E:
/// 1) Dostawca rejestruje się automatycznie (pierwsze uwierzytelnienie pieczęcią z O + CN=PeppolId).
/// 2) Firma nadaje uprawnienie PefInvoiceWrite (PefInvoicing) temu dostawcy.
/// 3) Dostawca wysyła fakturę PEF w imieniu firmy.
/// </summary>
[Collection("OnlineSessionScenario")]
public class PeppolPefE2ETests : TestBase
{
    protected ITestDataClient _testClient => _scope.ServiceProvider.GetRequiredService<ITestDataClient>();

    // Wymaganie PeppolId (CN):
    private static readonly Regex PeppolIdRegex =
        new(@"^P[A-Z]{2}[0-9]{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private string? _accessToken; // token firmy (NIP) – do odczytów i ewentualnie innych operacji
    private string? _refreshToken;
    private readonly string _companyNip;
    private readonly string _buyerNip;
    private readonly string _iban;
    private string? _peppol;
    private string? _privateKeyBase64;
    private string? _publicKeyBase64;

    private string? _pefTemplate;
    private string? _pefCorrectionTemplate;

    public PeppolPefE2ETests() : base()
    {
        // Token firmy (XAdES) – jak w pozostałych E2E (posłuży do odczytów/list i ewentualnej sesji, ale wysyłkę robi dostawca)
        _companyNip = MiscellaneousUtils.GetRandomNip();
        _buyerNip = MiscellaneousUtils.GetRandomNip();
        _iban = MiscellaneousUtils.GeneratePolishIban();

        AuthenticationOperationStatusResponse auth = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, SignatureService, _companyNip)
            .GetAwaiter().GetResult();

        _accessToken = auth.AccessToken.Token;
        _refreshToken = auth.RefreshToken.Token;
    }

    [Theory]
    [InlineData("invoice-template-fa-3-pef.xml", false)]
    [InlineData("invoice-template-fa-3-pef-with-attachment.xml", true)]
    public async Task Peppol_PEF_FullFlow_AutoRegister_Grant_Send(string template, bool hasAttachment)
    {
        _pefTemplate = template;

        if (hasAttachment)
        {
            Client.Core.Models.TestData.AttachmentPermissionGrantRequest request = new() { Nip = _companyNip };
            await _testClient.EnableAttachmentAsync(request);
            await Task.Delay(SleepTime);

            RefreshTokenResponse refreshToken = await AuthorizationClient.RefreshAccessTokenAsync(_refreshToken);
            _accessToken = refreshToken.AccessToken.Token;
        }


        // === 0) AUTO-REJESTRACJA dostawcy: pierwsze uwierzytelnienie pieczęcią z O + CN=PeppolId
        // Arrange
        // (dane przygotowane w konstruktorze)

        // Act
        (string peppolId, string providerToken) = await AutoRegisterProviderAsync();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(peppolId));
        Assert.Matches(PeppolIdRegex, peppolId);
        Assert.False(string.IsNullOrWhiteSpace(providerToken));
        _peppol = peppolId;

        // === 1) RESOLVE PROVIDER===
        // Arrange

        // Act
        PeppolProvider? provider = await FindProviderAsync(peppolId);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal(peppolId, provider!.Id);

        // === 2) GRANT: Firma -> Dostawca (PefInvoicing) ===
        // Arrange
        // (peppolId + _accessToken)

        // Act
        await GrantPefInvoicingAsync(peppolId);

        // Assert (wstępna weryfikacja, bez przeszukiwania całej listy)
        EntityAuthorizationsQueryRequest query = new EntityAuthorizationsQueryRequest
        {
            AuthorizingIdentifier = new EntityAuthorizationsAuthorizingEntityIdentifier { Type = EntityAuthorizationsAuthorizingEntityIdentifierType.Nip, Value = _companyNip },
            AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier { Type = EntityAuthorizationsAuthorizedEntityIdentifierType.PeppolId, Value = peppolId },
            QueryType = QueryType.Granted,
            PermissionTypes = new() { InvoicePermissionType.PefInvoicing }
        };

        PagedAuthorizationsResponse<AuthorizationGrant> authz = await KsefClient.SearchEntityAuthorizationGrantsAsync(
            requestPayload: query,
            accessToken: _accessToken,
            pageOffset: 0,
            pageSize: 10,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(authz);

        // === 3) WYSYŁKA PEF przez dostawcę ===
        // Arrange
        // (providerToken, NIPy, IBAN, template)

        // Act
        string upo = await SendPefInvoiceFlowAsync(providerToken);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(upo));
    }

    [Theory]
    //[InlineData("invoice-template-fa-3-pef.xml", "invoice-template-fa-3-pef-correction.xml", false)]
    [InlineData("invoice-template-fa-3-pef-with-attachment.xml", "invoice-template-fa-3-pef-correction.xml", true)]

    public async Task Peppol_PEF_Correction_FullFlow_AutoRegister_Grant_Send(string template, string correctionTemplate, bool hasAttachment)
    {
        _pefTemplate = template;
        _pefCorrectionTemplate = correctionTemplate;


        if (hasAttachment)
        {
            Client.Core.Models.TestData.AttachmentPermissionGrantRequest request = new() { Nip = _companyNip };
            await _testClient.EnableAttachmentAsync(request);
            await Task.Delay(SleepTime);

            RefreshTokenResponse refreshToken = await AuthorizationClient.RefreshAccessTokenAsync(_refreshToken);
            _accessToken = refreshToken.AccessToken.Token;
        }


        // === 0) AUTO-REJESTRACJA dostawcy: pierwsze uwierzytelnienie pieczęcią z O + CN=PeppolId
        // Arrange
        // (dane przygotowane w konstruktorze)

        // Act
        (string peppolId, string providerToken) = await AutoRegisterProviderAsync();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(peppolId));
        Assert.Matches(PeppolIdRegex, peppolId);
        Assert.False(string.IsNullOrWhiteSpace(providerToken));
        _peppol = peppolId;

        // === 1) RESOLVE PROVIDER===
        // Arrange

        // Act
        PeppolProvider? provider = await FindProviderAsync(peppolId);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal(peppolId, provider!.Id);

        // === 2) GRANT: Firma -> Dostawca (PefInvoicing) ===
        // Arrange
        // (peppolId + _accessToken)

        // Act
        await GrantPefInvoicingAsync(peppolId);

            // Assert (wstępna weryfikacja, bez przeszukiwania całej listy)
        EntityAuthorizationsQueryRequest query = new EntityAuthorizationsQueryRequest
        {
            AuthorizingIdentifier = new EntityAuthorizationsAuthorizingEntityIdentifier { Type = EntityAuthorizationsAuthorizingEntityIdentifierType.Nip, Value = _companyNip },
            AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier { Type = EntityAuthorizationsAuthorizedEntityIdentifierType.PeppolId, Value = peppolId },
            QueryType = QueryType.Granted,
            PermissionTypes = new() { InvoicePermissionType.PefInvoicing }
        };

        PagedAuthorizationsResponse<AuthorizationGrant> authz = await KsefClient.SearchEntityAuthorizationGrantsAsync(
            requestPayload: query,
            accessToken: _accessToken,
            pageOffset: 0,
            pageSize: 10,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(authz);

        // === 3) WYSYŁKA PEF przez dostawcę ===
        // Arrange
        // (providerToken, NIPy, IBAN, template)

        // Act
        string upo = await SendPefInvoiceFlowAsync(providerToken);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(upo));


        await Task.Delay(SleepTime * 2);

        // === 4) Wysyłka KOREKTY do faktury bazowej ===
        // Przygotowanie template'u korekty: wstrzyknięcie numeru KSeF oryginału + przyczyna + pozycje
        string correctionUpo = await SendPefCorrectionInvoiceFlowAsync(
            providerToken
        //,                originalKsefNumber: baseInvoice.KsefNumber
        );

        Assert.False(string.IsNullOrWhiteSpace(correctionUpo));
    }






    // -----------------------------
    // KROK 0: Auto-rejestracja
    // -----------------------------
    private async Task<(string peppolId, string providerToken)> AutoRegisterProviderAsync()
    {
        string country = (Environment.GetEnvironmentVariable("KSEF_PEP_COUNTRY") ?? "PL").ToUpperInvariant();
        string peppolId = $"P{country}{new Random().Next(0, 1_000_000):000000}";
        Assert.True(PeppolIdRegex.IsMatch(peppolId), $"PeppolId '{peppolId}' nie spełnia ^P[A-Z]{{2}}[0-9]{{6}}$.");

        string organizationName = Environment.GetEnvironmentVariable("KSEF_PEP_ORG") ?? "E2E Peppol Test Provider";
        string organizationIdentifier = Environment.GetEnvironmentVariable("KSEF_PEP_ORG_ID") ?? peppolId;

        // Pieczęć ustawia O + CN (spełniamy wymagania Peppol)
        X509Certificate2 providerSeal = CertificateUtils.GetCompanySeal(
            organizationName: organizationName,
            organizationIdentifier: organizationIdentifier,
            commonName: peppolId);

        using RSA rsaPrivateKey = providerSeal.GetRSAPrivateKey();
        using RSA rsaPublicKey = providerSeal.GetRSAPublicKey();

        _privateKeyBase64 =
            "-----BEGIN PRIVATE KEY-----\n" +
            Convert.ToBase64String(rsaPrivateKey.ExportPkcs8PrivateKey(), Base64FormattingOptions.InsertLineBreaks) +
            "\n-----END PRIVATE KEY-----";

        _publicKeyBase64 =
            "-----BEGIN PUBLIC KEY-----\n" +
            Convert.ToBase64String(rsaPublicKey.ExportSubjectPublicKeyInfo(), Base64FormattingOptions.InsertLineBreaks) +
            "\n-----END PUBLIC KEY-----";

        AuthenticationOperationStatusResponse providerAuth = await AuthenticationUtils.AuthenticateAsync(
            authorizationClient: AuthorizationClient,
            signatureService: SignatureService,
            certificate: providerSeal,
            contextIdentifierType: AuthenticationTokenContextIdentifierType.PeppolId,
            contextIdentifierValue: peppolId);

        Assert.NotNull(providerAuth?.AccessToken);
        return (peppolId, providerAuth.AccessToken.Token);
    }

    // -----------------------------
    // KROK 1: Znalezienie dostawcy - Lista dostawców – paginacja HasMore + krótki retry, szukamy KONKRETNEGO peppolId
    // -----------------------------
    private async Task<PeppolProvider?> FindProviderAsync(string peppolId)
    {
        PeppolProvider? resolved = null;

        await AsyncPollingUtils.PollAsync(
            description: $"Znaleziono PeppolId {peppolId}",
            check: async () =>
            {
                int? pageOffset = null;
                const int pageSize = 100;
                int guardPages = 100;

                do
                {
                    QueryPeppolProvidersResponse page = await KsefClient.QueryPeppolProvidersAsync(
                        accessToken: _accessToken,
                        pageOffset: pageOffset,
                        pageSize: pageSize,
                        cancellationToken: CancellationToken.None);

                    PeppolProvider? hit = page?.PeppolProviders?.FirstOrDefault(p => p.Id == peppolId);
                    if (hit != null)
                    {
                        resolved = hit;
                        return true;
                    }

                    if (page?.HasMore == true)
                    {
                        pageOffset = (pageOffset ?? 0) + (page?.PeppolProviders?.Count ?? 0);
                    }
                    else break;
                } while (guardPages-- > 0);

                return false;
            },
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: 3);

        Assert.True(resolved != null,
            $"Brak PeppolId '{peppolId}' po auto-rejestracji. Sprawdź CN/O oraz zwiększ retry.");

        return resolved;
    }

    // -----------------------------
    // KROK 2: Grant PefInvoicing - Firma nadaje uprawnienie PefInvoiceWrite (PefInvoicing - uprawnienie sesyjne) dla DOSTAWCY (kontekst żądania: PeppolId)
    //    - bearer = token firny 
    //    - Subject = PeppolId (komu przyznajemy uprawnienie)
    // -----------------------------
    private async Task GrantPefInvoicingAsync(string peppolId)
    {
        GrantPermissionsAuthorizationRequest grantReq = GrantAuthorizationPermissionsRequestBuilder
            .Create()
            .WithSubject(new AuthorizationSubjectIdentifier
            {
                Type = AuthorizationSubjectIdentifierType.PeppolId,
                Value = peppolId
            })
            .WithPermission(AuthorizationPermissionType.PefInvoicing)
            .WithDescription($"E2E: Nadanie uprawnienia do wystawiania faktur PEF dla firmy {_companyNip} (na wniosek {peppolId})")
            .Build();

        OperationResponse grantResp = await KsefClient.GrantsAuthorizationPermissionAsync(
            requestPayload: grantReq,
            accessToken: _accessToken,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(grantResp);

        PermissionsOperationStatusResponse grantRespStatus = await KsefClient.OperationsStatusAsync(grantResp.ReferenceNumber, _accessToken);

        // Assert.Equal(200, grantRespStatus.Status.Code);

            // opcjonalnie: szybka walidacja listy grantów (w niektórych env może nie być 1:1)
            EntityAuthorizationsQueryRequest query = new EntityAuthorizationsQueryRequest
            {
                AuthorizingIdentifier = new EntityAuthorizationsAuthorizingEntityIdentifier
                {
                    Type = EntityAuthorizationsAuthorizingEntityIdentifierType.Nip,
                    Value = _companyNip
                },
                AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier
                {
                    Type = EntityAuthorizationsAuthorizedEntityIdentifierType.PeppolId,
                    Value = peppolId
                },
                QueryType = QueryType.Granted,
                PermissionTypes = new() { InvoicePermissionType.PefInvoicing }
            };

        PagedAuthorizationsResponse<AuthorizationGrant> authz = await KsefClient.SearchEntityAuthorizationGrantsAsync(
            requestPayload: query,
            accessToken: _accessToken,
            pageOffset: 0,
            pageSize: 10,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(authz);
    }

    // -----------------------------
    // KROK 3: Wysyłka PEF (sesja online)
    // -----------------------------
    private async Task<string> SendPefInvoiceFlowAsync(string providerToken)
    {
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        OpenOnlineSessionResponse openSession = await OnlineSessionUtils.OpenOnlineSessionAsync(
            ksefClient: KsefClient,
            encryptionData: encryptionData,
            accessToken: providerToken,
            systemCode: SystemCode.PEF);

        Assert.NotNull(openSession);
        Assert.False(string.IsNullOrWhiteSpace(openSession.ReferenceNumber));

        SendInvoiceResponse sendResp = await OnlineSessionUtils.SendPefInvoiceAsync(
            ksefClient: KsefClient,
            sessionReferenceNumber: openSession.ReferenceNumber,
            accessToken: providerToken,
            supplierNip: $"PL{_companyNip}",
            customerNip: $"PL{_buyerNip}",
            buyerReference: $"PL{_buyerNip}",
            iban: _iban,
            templatePath: _pefTemplate,
            encryptionData: encryptionData,
            cryptographyService: CryptographyService);

        Assert.NotNull(sendResp);

        // status: oczekujemy Processing (100), bez błędów
        SessionStatusResponse statusProcessing = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None);
        Assert.NotNull(statusProcessing);

        SessionInvoicesResponse failedInvoices;
        if (statusProcessing.FailedInvoiceCount is not null)
        {
            failedInvoices = await KsefClient.GetSessionFailedInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10, continuationToken: string.Empty, CancellationToken.None);
        }

        Assert.Null(statusProcessing.FailedInvoiceCount);
        Assert.Equal(OnlineSessionCodeResponse.SessionOpened, statusProcessing.Status.Code);
        Assert.Equal(OnlineSessionCodeResponse.SessionOpened, statusProcessing.Status.Code);

        await KsefClient.CloseOnlineSessionAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None);

        // czekamy na zamknięcie sesji deterministycznym pollingiem
        await AsyncPollingUtils.PollAsync(
           description: $"Sesja zamknięta dla {_peppol}",
           check: async () =>
           {
               SessionStatusResponse st = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None);
               return st?.Status?.Code == OnlineSessionCodeResponse.SessionClosed || st?.Status?.Code == OnlineSessionCodeResponse.ProcessedSuccessfully;
           },
           delay: TimeSpan.FromMilliseconds(SleepTime),
           maxAttempts: 10);

        SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10);
        Assert.NotNull(invoices);
        Assert.NotEmpty(invoices.Invoices);

        SessionInvoice sessionInvoice = invoices.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);

        // jeżeli faktura jeszcze jest w statusie „processing”, spróbuj odświeżyć kilka razy zamiast jednego sleepa
        if (sessionInvoice.Status.Code == InvoiceInSessionStatusCodeResponse.Processing)
        {
            await AsyncPollingUtils.PollAsync(
               description: "faktura gotowa (status inny niż processing)",
               check: async () =>
               {
                   SessionInvoicesResponse inv = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10);
                   SessionInvoice refreshed = inv.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);
                   return refreshed.Status.Code != InvoiceInSessionStatusCodeResponse.Processing;
               },
               delay: TimeSpan.FromMilliseconds(SleepTime),
               maxAttempts: 5);
        }

        // UPO:
        string upo = await KsefClient.GetSessionInvoiceUpoByReferenceNumberAsync(
            openSession.ReferenceNumber,
            sendResp.ReferenceNumber,
            providerToken);

        Assert.NotNull(upo);
        return upo;
    }

    // -----------------------------
    // KROK 4: Wysyłka Korekty PEF (sesja online)
    // -----------------------------
    private async Task<string> SendPefCorrectionInvoiceFlowAsync(string providerToken)
    {
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        OpenOnlineSessionResponse openSession = await OnlineSessionUtils.OpenOnlineSessionAsync(
            ksefClient: KsefClient,
            encryptionData: encryptionData,
            accessToken: providerToken,
            systemCode: SystemCode.PEFKOR);

        Assert.NotNull(openSession);
        Assert.False(string.IsNullOrWhiteSpace(openSession.ReferenceNumber));


        SendInvoiceResponse sendResp = await OnlineSessionUtils.SendPefInvoiceAsync(
            ksefClient: KsefClient,
            sessionReferenceNumber: openSession.ReferenceNumber,
            accessToken: providerToken,
            supplierNip: $"PL{_companyNip}",
            customerNip: $"PL{_buyerNip}",
            buyerReference: $"PL{_buyerNip}",
            iban: _iban,
            templatePath: _pefCorrectionTemplate,
            encryptionData: encryptionData,
            cryptographyService: CryptographyService);

        Assert.NotNull(sendResp);

        await Task.Delay(SleepTime);
        // status: oczekujemy Processing (100), bez błędów
        SessionStatusResponse statusProcessing = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None);
        Assert.NotNull(statusProcessing);

        SessionInvoicesResponse failedInvoices;
        if (statusProcessing.FailedInvoiceCount is not null)
        {
            failedInvoices = await KsefClient.GetSessionFailedInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10, continuationToken: string.Empty, CancellationToken.None);
        }

        Assert.Null(statusProcessing.FailedInvoiceCount);
        Assert.Equal(OnlineSessionCodeResponse.SessionOpened, statusProcessing.Status.Code);
        Assert.Equal(OnlineSessionCodeResponse.SessionOpened, statusProcessing.Status.Code);

        await KsefClient.CloseOnlineSessionAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None);

        // oczekiwanie na zamknięcie sesji deterministycznym pollingiem
        await AsyncPollingUtils.PollAsync(
           description: $"Sesja zamknięta dla {_peppol}",
           check: async () =>
           {
               SessionStatusResponse st = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None);
               return st?.Status?.Code == OnlineSessionCodeResponse.SessionClosed || st?.Status?.Code == OnlineSessionCodeResponse.ProcessedSuccessfully;
           },
           delay: TimeSpan.FromMilliseconds(SleepTime),
           maxAttempts: 10);

        SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10);
        Assert.NotNull(invoices);
        Assert.NotEmpty(invoices.Invoices);

        SessionInvoice sessionInvoice = invoices.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);

        // jeżeli faktura jeszcze jest w statusie „processing”, należy odświeżyć kilka razy zamiast jednego sleepa
        if (sessionInvoice.Status.Code == InvoiceInSessionStatusCodeResponse.Processing)
        {
            await AsyncPollingUtils.PollAsync(
               description: "faktura gotowa (status inny niż processing)",
               check: async () =>
               {
                   SessionInvoicesResponse inv = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10);
                   SessionInvoice refreshed = inv.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);
                   return refreshed.Status.Code != InvoiceInSessionStatusCodeResponse.Processing;
               },
               delay: TimeSpan.FromMilliseconds(SleepTime),
               maxAttempts: 5);
        }

        // UPO:
        string upo = await KsefClient.GetSessionInvoiceUpoByReferenceNumberAsync(
            openSession.ReferenceNumber,
            sendResp.ReferenceNumber,
            providerToken);

        Assert.NotNull(upo);
        return upo;
    }
}

