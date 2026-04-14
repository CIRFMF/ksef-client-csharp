#nullable enable
using KSeF.Client.Api.Builders.AuthorizationEntityPermissions;
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
using KSeF.Client.Validation;
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
public partial class PeppolPefE2ETests : TestBase
{
    protected ITestDataClient TestClient => Get<ITestDataClient>();

    // Wymaganie PeppolId (CN):
    private static readonly Regex PeppolIdRegex = RegexPatterns.PeppolId;

    private string accessToken; // token firmy (NIP) – do odczytów i ewentualnie innych operacji
    private readonly string refreshToken;
    private readonly string companyNip;
    private readonly string buyerNip;
    private readonly string iban;
    private string peppol;
    private string privateKeyBase64;
    private string publicKeyBase64;

    private string _pefTemplate;
    private string _pefCorrectionTemplate;

    public PeppolPefE2ETests() : base()
    {
        // Token firmy (XAdES) – jak w pozostałych E2E (posłuży do odczytów/list i ewentualnej sesji, ale wysyłkę robi dostawca)
        companyNip = MiscellaneousUtils.GetRandomNip();
        buyerNip = MiscellaneousUtils.GetRandomNip();
        iban = MiscellaneousUtils.GeneratePolishIban();

        AuthenticationOperationStatusResponse auth = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, companyNip)
            .GetAwaiter().GetResult();

        accessToken = auth.AccessToken.Token;
        refreshToken = auth.RefreshToken.Token;
    }

    [Theory]
    [InlineData("invoice-template-fa-3-pef.xml", false)]
    [InlineData("invoice-template-fa-3-pef-with-attachment.xml", true)]
    public async Task Peppol_PEF_FullFlow_AutoRegister_Grant_Send(string template, bool hasAttachment)
    {
        _pefTemplate = template;

        if (hasAttachment)
        {
            Client.Core.Models.TestData.AttachmentPermissionGrantRequest request = new() { Nip = companyNip };
            await TestClient.EnableAttachmentAsync(request);
            await Task.Delay(SleepTime);

            RefreshTokenResponse newRefreshToken = await AuthorizationClient.RefreshAccessTokenAsync(refreshToken);
            accessToken = newRefreshToken.AccessToken.Token;
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
        peppol = peppolId;

        // === 1) RESOLVE PROVIDER===
        // Arrange

        // Act
        PeppolProvider? provider = await FindProviderAsync(peppolId);

        // Assert
        Assert.NotNull(provider);
        Assert.NotNull(provider.Name);
        Assert.Equal(peppolId, provider!.Id);

        // === 2) GRANT: Firma -> Dostawca (PefInvoicing) ===
        // Arrange
        // (peppolId + _accessToken)

        // Act
        await GrantPefInvoicingAsync(peppolId);

        // Assert (wstępna weryfikacja, bez przeszukiwania całej listy)
        EntityAuthorizationsQueryRequest query = new()
        {
            AuthorizingIdentifier = new EntityAuthorizationsAuthorizingEntityIdentifier { Type = EntityAuthorizationsAuthorizingEntityIdentifierType.Nip, Value = companyNip },
            AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier { Type = EntityAuthorizationsAuthorizedEntityIdentifierType.PeppolId, Value = peppolId },
            QueryType = QueryType.Granted,
            PermissionTypes = [InvoicePermissionType.PefInvoicing]
        };

        PagedAuthorizationsResponse<AuthorizationGrant> authz = await KsefClient.SearchEntityAuthorizationGrantsAsync(
            requestPayload: query,
            accessToken: accessToken,
            pageOffset: 0,
            pageSize: 10,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(authz);

        // === 3) WYSYŁKA PEF przez dostawcę ===
        // Arrange
        // (providerToken, NIPy, IBAN, template)

        // Act
        (string upo, _) = await SendPefInvoiceFlowAsync(providerToken);

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
            Client.Core.Models.TestData.AttachmentPermissionGrantRequest request = new() { Nip = companyNip };
            await TestClient.EnableAttachmentAsync(request);
            await Task.Delay(SleepTime);

            RefreshTokenResponse newRefreshToken = await AuthorizationClient.RefreshAccessTokenAsync(refreshToken);
            accessToken = newRefreshToken.AccessToken.Token;
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
        peppol = peppolId;

        // === 1) RESOLVE PROVIDER===
        // Arrange

        // Act
        PeppolProvider? provider = await FindProviderAsync(peppolId);

        // Assert
        Assert.NotNull(provider);
        Assert.NotNull(provider.Name);
        Assert.Equal(peppolId, provider!.Id);

        // === 2) GRANT: Firma -> Dostawca (PefInvoicing) ===
        // Arrange
        // (peppolId + _accessToken)

        // Act
        await GrantPefInvoicingAsync(peppolId);

            // Assert (wstępna weryfikacja, bez przeszukiwania całej listy)
        EntityAuthorizationsQueryRequest query = new()
        {
            AuthorizingIdentifier = new EntityAuthorizationsAuthorizingEntityIdentifier { Type = EntityAuthorizationsAuthorizingEntityIdentifierType.Nip, Value = companyNip },
            AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier { Type = EntityAuthorizationsAuthorizedEntityIdentifierType.PeppolId, Value = peppolId },
            QueryType = QueryType.Granted,
            PermissionTypes = [InvoicePermissionType.PefInvoicing]
        };

        PagedAuthorizationsResponse<AuthorizationGrant> authz = await KsefClient.SearchEntityAuthorizationGrantsAsync(
            requestPayload: query,
            accessToken: accessToken,
            pageOffset: 0,
            pageSize: 10,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(authz);
        Assert.NotNull(authz.AuthorizationGrants.First().Id);
        Assert.True(authz.AuthorizationGrants.Count() == 1);
        Assert.NotNull(authz.AuthorizationGrants.First().Description);
        Assert.NotNull(authz.AuthorizationGrants.First().AuthorIdentifier);
        Assert.NotNull(authz.AuthorizationGrants.First().AuthorizingEntityIdentifier);
        Assert.NotNull(authz.AuthorizationGrants.First().AuthorizedEntityIdentifier);
        Assert.NotNull(authz.AuthorizationGrants.First().SubjectEntityDetails);

        // === 3) WYSYŁKA PEF przez dostawcę ===
        // Arrange
        // (providerToken, NIPy, IBAN, template)

        // Act
        (string upo, string ksefNumber) = await SendPefInvoiceFlowAsync(providerToken);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(upo));


        await Task.Delay(SleepTime * 2);

        // === 4) Wysyłka KOREKTY do faktury bazowej ===
        // Przygotowanie template'u korekty: wstrzyknięcie numeru KSeF oryginału + przyczyna + pozycje
        string correctionUpo = await SendPefCorrectionInvoiceFlowAsync(
            providerToken,
            originalKsefNumber: ksefNumber
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
        Assert.True(PeppolIdRegex.IsMatch(peppolId), $"PeppolId '{peppolId}' nie spełnia {PeppolIdRegex}");

        string organizationName = Environment.GetEnvironmentVariable("KSEF_PEP_ORG") ?? "E2E Peppol Test Provider";
        string organizationIdentifier = Environment.GetEnvironmentVariable("KSEF_PEP_ORG_ID") ?? peppolId;

        // Pieczęć ustawia O + CN (spełniamy wymagania Peppol)
        X509Certificate2 providerSeal = CertificateUtils.GetCompanySeal(
            organizationName: organizationName,
            organizationIdentifier: organizationIdentifier,
            commonName: peppolId);

        using RSA rsaPrivateKey = providerSeal.GetRSAPrivateKey() ?? throw new InvalidCastException();
        using RSA rsaPublicKey = providerSeal.GetRSAPublicKey() ?? throw new InvalidCastException();

        if (rsaPrivateKey is null)
        {
            throw new InvalidOperationException("Provider certificate does not contain a private key.");
        }

        if (rsaPublicKey is null)
        {
            throw new InvalidOperationException("Provider certificate does not contain a public key.");
        }

        privateKeyBase64 =
            "-----BEGIN PRIVATE KEY-----\n" +
            Convert.ToBase64String(rsaPrivateKey.ExportPkcs8PrivateKey(), Base64FormattingOptions.InsertLineBreaks) +
            "\n-----END PRIVATE KEY-----";

        publicKeyBase64 =
            "-----BEGIN PUBLIC KEY-----\n" +
            Convert.ToBase64String(rsaPublicKey.ExportSubjectPublicKeyInfo(), Base64FormattingOptions.InsertLineBreaks) +
            "\n-----END PUBLIC KEY-----";

        AuthenticationOperationStatusResponse providerAuth = await AuthenticationUtils.AuthenticateAsync(
            authorizationClient: AuthorizationClient,
            certificate: providerSeal,
            contextIdentifierType: AuthenticationTokenContextIdentifierType.PeppolId,
            contextIdentifierValue: peppolId).ConfigureAwait(false);

        Assert.NotNull(providerAuth?.AccessToken);
        Assert.NotNull(providerAuth?.RefreshToken);
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
                int pageOffset = 0;
                const int pageSize = 100;
                int guardPages = 100;
                QueryPeppolProvidersResponse page;
                do
                {
                     page = await KsefClient.QueryPeppolProvidersAsync(
                        accessToken: accessToken,
                        pageOffset: pageOffset,
                        pageSize: pageSize,
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    PeppolProvider? hit = page?.PeppolProviders?.FirstOrDefault(p => p.Id == peppolId);
                    if (hit != null)
                    {
                        resolved = hit;
                        return true;
                    }

                    if (page?.HasMore == true)
                    {
                        pageOffset++;
                    }
                    else
                    {
                        break;
                    }
                } while (guardPages-- > 0 || page?.HasMore == true);

                return false;
            },
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: 3).ConfigureAwait(false);

        Assert.True(resolved != null,
            $"Brak PeppolId '{peppolId}' po auto-rejestracji. Sprawdź CN/O oraz zwiększ retry.");

        return resolved;
    }

    // -----------------------------
    // KROK 2: Grant PefInvoicing - Firma nadaje uprawnienie PefInvoiceWrite (PefInvoicing - uprawnienie sesyjne) DOSTAWCY (kontekst żądania: PeppolId)
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
            .WithDescription($"E2E: Nadanie uprawnienia do wystawiania faktur PEF firmie {companyNip} (na wniosek {peppolId})")
            .WithSubjectDetails(new PermissionsAuthorizationSubjectDetails
            {
                FullName = "Podmiot Testowy 1"
            })
            .Build();

        OperationResponse grantResp = await KsefClient.GrantsAuthorizationPermissionAsync(
            requestPayload: grantReq,
            accessToken: accessToken,
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        Assert.NotNull(grantResp);
        Assert.NotNull(grantResp.ReferenceNumber);

        PermissionsOperationStatusResponse grantRespStatus = await KsefClient.OperationsStatusAsync(grantResp.ReferenceNumber, accessToken).ConfigureAwait(false);

        Assert.NotNull(grantRespStatus);
        Assert.NotNull(grantRespStatus.Status);
        Assert.Null(grantRespStatus.Status.Details);

        // opcjonalnie: szybka walidacja listy grantów (w niektórych env może nie być 1:1)
        EntityAuthorizationsQueryRequest query = new()
        {
            AuthorizingIdentifier = new EntityAuthorizationsAuthorizingEntityIdentifier
            {
                Type = EntityAuthorizationsAuthorizingEntityIdentifierType.Nip,
                Value = companyNip
            },
            AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier
            {
                Type = EntityAuthorizationsAuthorizedEntityIdentifierType.PeppolId,
                Value = peppolId
            },
            QueryType = QueryType.Granted,
            PermissionTypes = [InvoicePermissionType.PefInvoicing]
        };

        PagedAuthorizationsResponse<AuthorizationGrant> authz = await KsefClient.SearchEntityAuthorizationGrantsAsync(
            requestPayload: query,
            accessToken: accessToken,
            pageOffset: 0,
            pageSize: 10,
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        Assert.NotNull(authz);
        Assert.NotNull(authz.AuthorizationGrants.Count > 0);
        Assert.NotNull(authz.AuthorizationGrants.First().SubjectEntityDetails);
        Assert.NotNull(authz.AuthorizationGrants.First().AuthorIdentifier.Value);
        Assert.NotNull(authz.AuthorizationGrants.First().Id);
        Assert.NotNull(authz.AuthorizationGrants.First().AuthorizationScope);
        Assert.NotNull(authz.AuthorizationGrants.First().AuthorizationScope);
        Assert.NotNull(authz.AuthorizationGrants.First().AuthorizedEntityIdentifier);
        Assert.NotNull(authz.AuthorizationGrants.First().AuthorizingEntityIdentifier);
    }

    // -----------------------------
    // KROK 3: Wysyłka PEF (sesja online)
    // -----------------------------
    private async Task<(string upo, string ksefNumber)> SendPefInvoiceFlowAsync(string providerToken)
    {
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        OpenOnlineSessionResponse openSession = await OnlineSessionUtils.OpenOnlineSessionAsync(
            ksefClient: KsefClient,
            encryptionData: encryptionData,
            accessToken: providerToken,
            systemCode: SystemCode.PEF).ConfigureAwait(false);

        Assert.NotNull(openSession);
        Assert.False(string.IsNullOrWhiteSpace(openSession.ReferenceNumber));

        SendInvoiceResponse sendResp = await OnlineSessionUtils.SendPefInvoiceAsync(
            ksefClient: KsefClient,
            sessionReferenceNumber: openSession.ReferenceNumber,
            accessToken: providerToken,
            supplierNip: $"PL{companyNip}",
            customerNip: $"PL{buyerNip}",
            buyerReference: $"PL{buyerNip}",
            iban: iban,
            templatePath: _pefTemplate,
            encryptionData: encryptionData,
            cryptographyService: CryptographyService).ConfigureAwait(false);

        Assert.NotNull(sendResp);

        // status: oczekujemy Processing (100), bez błędów
        SessionStatusResponse statusProcessing = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None).ConfigureAwait(false);
        Assert.NotNull(statusProcessing);

        SessionInvoicesResponse failedInvoices;
        if (statusProcessing.FailedInvoiceCount is not null)
        {
            failedInvoices = await KsefClient.GetSessionFailedInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10, continuationToken: string.Empty, CancellationToken.None).ConfigureAwait(false);
        }

        Assert.Null(statusProcessing.FailedInvoiceCount);
        Assert.NotNull(statusProcessing.Status);
        Assert.Equal(OnlineSessionCodeResponse.SessionOpened, statusProcessing.Status.Code);
        Assert.Null(statusProcessing.Upo);
        Assert.NotNull(statusProcessing.InvoiceCount);
        Assert.True(statusProcessing.InvoiceCount.Value > 0);

        await KsefClient.CloseOnlineSessionAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None).ConfigureAwait(false);

        // czekamy na zamknięcie sesji deterministycznym pollingiem
        await AsyncPollingUtils.PollAsync(
           description: $"Sesja zamknięta dla {peppol}",
           check: async () =>
           {
               SessionStatusResponse st = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None).ConfigureAwait(false);
               return st?.Status?.Code == OnlineSessionCodeResponse.SessionClosed || st?.Status?.Code == OnlineSessionCodeResponse.ProcessedSuccessfully;
           },
           delay: TimeSpan.FromMilliseconds(SleepTime),
           maxAttempts: 10).ConfigureAwait(false);

        SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10).ConfigureAwait(false);
        Assert.NotNull(invoices);
        Assert.NotEmpty(invoices.Invoices);

        SessionInvoice sessionInvoice = invoices.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);

        // jeżeli faktura jeszcze jest w statusie „processing”, spróbuj odświeżyć kilka razy zamiast jednego sleepa
        if (string.IsNullOrWhiteSpace(sessionInvoice.KsefNumber))
        {
            await AsyncPollingUtils.PollAsync(
               description: "Oczekiwanie na numer KSeF faktury",
               check: async () =>
               {
                   SessionInvoicesResponse inv = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10).ConfigureAwait(false);
                   SessionInvoice refreshed = inv.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);
                   if (!string.IsNullOrWhiteSpace(refreshed.KsefNumber))
                   {
                       sessionInvoice = refreshed;
                       return true;
                   }
                   return false;
               },
               delay: TimeSpan.FromMilliseconds(SleepTime),
               maxAttempts: 10).ConfigureAwait(false);
        }

        // UPO:
        string upo = await KsefClient.GetSessionInvoiceUpoByReferenceNumberAsync(
            openSession.ReferenceNumber,
            sendResp.ReferenceNumber,
            providerToken).ConfigureAwait(false);

        Assert.NotNull(upo);
        Assert.False(string.IsNullOrWhiteSpace(sessionInvoice.KsefNumber));
        return (upo, sessionInvoice.KsefNumber);
    }

    // -----------------------------
    // KROK 4: Wysyłka Korekty PEF (sesja online)
    // -----------------------------
    private async Task<string> SendPefCorrectionInvoiceFlowAsync(string providerToken, string originalKsefNumber)
    {
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        OpenOnlineSessionResponse openSession = await OnlineSessionUtils.OpenOnlineSessionAsync(
            ksefClient: KsefClient,
            encryptionData: encryptionData,
            accessToken: providerToken,
            systemCode: SystemCode.PEFKOR).ConfigureAwait(false);

        Assert.NotNull(openSession);
        Assert.False(string.IsNullOrWhiteSpace(openSession.ReferenceNumber));


        SendInvoiceResponse sendResp = await OnlineSessionUtils.SendPefInvoiceAsync(
            ksefClient: KsefClient,
            sessionReferenceNumber: openSession.ReferenceNumber,
            accessToken: providerToken,
            supplierNip: $"PL{companyNip}",
            customerNip: $"PL{buyerNip}",
            buyerReference: $"PL{buyerNip}",
            iban: iban,
            templatePath: _pefCorrectionTemplate,
            encryptionData: encryptionData,
            cryptographyService: CryptographyService,
            originalKsefNumber: originalKsefNumber).ConfigureAwait(false);

        Assert.NotNull(sendResp);

        await Task.Delay(SleepTime).ConfigureAwait(false);
        // status: oczekujemy Processing (100), bez błędów
        SessionStatusResponse statusProcessing = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None).ConfigureAwait(false);
        Assert.NotNull(statusProcessing);

        string failureInfo = string.Empty;
        if (statusProcessing.FailedInvoiceCount is not null)
        {
            SessionInvoicesResponse failedInvoices = await KsefClient.GetSessionFailedInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10, continuationToken: string.Empty, CancellationToken.None).ConfigureAwait(false);
            SessionInvoice? fi = failedInvoices?.Invoices?.FirstOrDefault();
            failureInfo = $"Code={fi?.Status?.Code}, Desc={fi?.Status?.Description}, Details={string.Join(";", fi?.Status?.Details ?? [])}";
        }

        Assert.True(statusProcessing.FailedInvoiceCount is null, $"Korekta odrzucona przez KSeF: {failureInfo}");
        Assert.Equal(OnlineSessionCodeResponse.SessionOpened, statusProcessing.Status.Code);

        await KsefClient.CloseOnlineSessionAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None).ConfigureAwait(false);

        // oczekiwanie na zamknięcie sesji deterministycznym pollingiem
        await AsyncPollingUtils.PollAsync(
           description: $"Sesja zamknięta dla {peppol}",
           check: async () =>
           {
               SessionStatusResponse st = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None).ConfigureAwait(false);
               return st?.Status?.Code == OnlineSessionCodeResponse.SessionClosed || st?.Status?.Code == OnlineSessionCodeResponse.ProcessedSuccessfully;
           },
           delay: TimeSpan.FromMilliseconds(SleepTime),
           maxAttempts: 10).ConfigureAwait(false);

        SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10).ConfigureAwait(false);
        Assert.NotNull(invoices);
        Assert.NotEmpty(invoices.Invoices);

        SessionInvoice sessionInvoice = invoices.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);

        // jeżeli faktura jeszcze jest w statusie „processing”, należy odświeżyć kilka razy zamiast jednego sleepa
        if (sessionInvoice.Status.Code == InvoiceInSessionStatusCodeResponse.AcceptedForProcessing ||
            sessionInvoice.Status.Code == InvoiceInSessionStatusCodeResponse.Processing)
        {
            await AsyncPollingUtils.PollAsync(
               description: "Oczekiwanie na zakończenie przetwarzania faktury korekty",
               check: async () =>
               {
                   SessionInvoicesResponse inv = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10).ConfigureAwait(false);
                   SessionInvoice refreshed = inv.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);
                   return refreshed.Status.Code != InvoiceInSessionStatusCodeResponse.AcceptedForProcessing &&
                          refreshed.Status.Code != InvoiceInSessionStatusCodeResponse.Processing;
               },
               delay: TimeSpan.FromMilliseconds(SleepTime),
               maxAttempts: 5).ConfigureAwait(false);
        }

        // UPO:
        string upo = await KsefClient.GetSessionInvoiceUpoByReferenceNumberAsync(
            openSession.ReferenceNumber,
            sendResp.ReferenceNumber,
            providerToken).ConfigureAwait(false);

        Assert.NotNull(upo);
        return upo;
    }
}
