using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Api.Builders.IndirectEntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.CollectiveIdentifiers;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.CollectiveIdentifiers;

/// <summary>
/// Testy end-to-end nadawania uprawnienia CollectiveIdentifierManage:
/// - dla osoby (Person) wraz z weryfikacją wygenerowania identyfikatora zbiorczego,
/// - dla podmiotu (Entity) wraz z zapytaniem o nadane uprawnienia,
/// - w sposób pośredni (Indirect) przez uprawnionego pośrednika.
/// </summary>
public class CollectiveIdentifierPermissionsE2ETests : TestBase
{
    private const string InvoiceTemplate = "invoice-template-fa-3-with-custom-Subject2.xml";
    private const int MaxPollingAttempts = 30;
    private const int InvoicesCount = 3;
    private const int PermissionPropagationMaxAttempts = 30;
    private static readonly TimeSpan PermissionPropagationDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Weryfikuje, że osoba, której nadano w kontekście właściciela uprawnienia `InvoiceRead` i `CollectiveIdentifierManage`
    /// przez endpoint nadawania uprawnień osobowych (`POST /permissions/persons/grants`), może w tym kontekście
    /// wygenerować identyfikator zbiorczy.
    /// </summary>
    [Fact]
    public async Task GrantCollectiveIdentifierManagePermission_ToPerson_ThenGenerateCollectiveIdentifier()
    {
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string authorizedPesel = MiscellaneousUtils.GetRandomPesel();

        AuthenticationOperationStatusResponse ownerAuth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient, ownerNip).ConfigureAwait(false);
        string ownerToken = ownerAuth.AccessToken.Token;

        string buyerNip = MiscellaneousUtils.GetRandomNip();
        List<string> ksefNumbers = new List<string>();
        for (int i = 0; i < InvoicesCount; i++)
        {
            string ksefNumber = await SendInvoiceAndGetKsefNumberAsync(ownerNip, buyerNip, ownerToken).ConfigureAwait(false);
            ksefNumbers.Add(ksefNumber);
        }

        GrantPermissionsPersonSubjectIdentifier subject = new GrantPermissionsPersonSubjectIdentifier
        {
            Type = GrantPermissionsPersonSubjectIdentifierType.Pesel,
            Value = authorizedPesel
        };

        PersonPermissionSubjectDetails subjectDetails = new PersonPermissionSubjectDetails
        {
            SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
            PersonById = new PersonPermissionPersonById { FirstName = "Jan", LastName = "Testowy" }
        };

        OperationResponse grantResponse = await PermissionsUtils.GrantPersonPermissionsAsync(
            KsefClient,
            ownerToken,
            subject,
            new[] { PersonPermissionType.InvoiceRead, PersonPermissionType.CollectiveIdentifierManage },
            subjectDetails,
            "E2E CollectiveIdentifierManage Person test").ConfigureAwait(false);

        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrWhiteSpace(grantResponse.ReferenceNumber));

        PermissionsOperationStatusResponse grantStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(grantResponse.ReferenceNumber, ownerToken),
            condition: s => s?.Status?.Code == OperationStatusCodeResponse.Success,
            delay: PermissionPropagationDelay,
            maxAttempts: PermissionPropagationMaxAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        Assert.Equal(OperationStatusCodeResponse.Success, grantStatus.Status.Code);

        AuthenticationOperationStatusResponse authorizedAuth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient, authorizedPesel, ownerNip).ConfigureAwait(false);
        string authorizedToken = authorizedAuth.AccessToken.Token;

        GenerateCollectiveIdentifierResponse generateResponse = await CollectiveIdentifiersClient.GenerateCollectiveIdentifierAsync(
            new GenerateCollectiveIdentifierRequest
            {
                Invoices = ksefNumbers.Select(ksefNum => new CollectiveIdentifierInvoice { KsefNumber = ksefNum }).ToList()
            },
            authorizedToken, CancellationToken).ConfigureAwait(false);

        Assert.NotNull(generateResponse);
        Assert.False(string.IsNullOrWhiteSpace(generateResponse.CollectiveIdentifierNumber));
    }

    /// <summary>
    /// Weryfikuje nadanie uprawnienia `CollectiveIdentifierManage` podmiotowi (`POST /permissions/entities/grants`)
    /// oraz wygenerowanie identyfikatora zbiorczego przez uprawniony podmiot w kontekście właściciela.
    /// </summary>
    [Fact]
    public async Task GrantCollectiveIdentifierManagePermission_ToEntity_ThenGenerateCollectiveIdentifier()
    {
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string entityNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse ownerAuth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient, ownerNip).ConfigureAwait(false);
        string ownerToken = ownerAuth.AccessToken.Token;

        // Sprzedawca wystawia faktury
        string buyerNip = MiscellaneousUtils.GetRandomNip();
        List<string> ksefNumbers = new List<string>();
        for (int i = 0; i < InvoicesCount; i++)
        {
            string ksefNumber = await SendInvoiceAndGetKsefNumberAsync(ownerNip, buyerNip, ownerToken).ConfigureAwait(false);
            ksefNumbers.Add(ksefNumber);
        }

        GrantPermissionsEntitySubjectIdentifier subject = new GrantPermissionsEntitySubjectIdentifier
        {
            Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
            Value = entityNip
        };

        GrantPermissionsEntityRequest grantRequest = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(
                EntityPermission.New(EntityStandardPermissionType.CollectiveIdentifierManage, canDelegate: false)
            )
            .WithDescription("E2E CollectiveIdentifierManage Entity test")
            .WithSubjectDetails(new PermissionsEntitySubjectDetails
            {
                FullName = $"Firma Partnerska {entityNip}"
            })
            .Build();

        OperationResponse grantResponse = await KsefClient.GrantsPermissionEntityAsync(
            grantRequest, ownerToken, CancellationToken).ConfigureAwait(false);

        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrWhiteSpace(grantResponse.ReferenceNumber));

        PermissionsOperationStatusResponse grantStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(grantResponse.ReferenceNumber, ownerToken),
            condition: s => s?.Status?.Code == OperationStatusCodeResponse.Success,
            delay: PermissionPropagationDelay,
            maxAttempts: PermissionPropagationMaxAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        Assert.Equal(OperationStatusCodeResponse.Success, grantStatus.Status.Code);

        // Uwierzytelnienie podmiotu w kontekście właściciela
        AuthenticationOperationStatusResponse entityAuthInOwnerContext = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            identifierValue: entityNip,
            contextIdentifierValue: ownerNip).ConfigureAwait(false);
        string entityToken = entityAuthInOwnerContext.AccessToken.Token;

		// Generowanie identyfikatora zbiorczego przez podmiot w kontekście właściciela
		GenerateCollectiveIdentifierResponse generateResponse = await CollectiveIdentifiersClient.GenerateCollectiveIdentifierAsync(
            new GenerateCollectiveIdentifierRequest
            {
                Invoices = ksefNumbers.Select(ksefNum => new CollectiveIdentifierInvoice { KsefNumber = ksefNum }).ToList()
            },
            entityToken, CancellationToken).ConfigureAwait(false);

        Assert.NotNull(generateResponse);
        Assert.False(string.IsNullOrWhiteSpace(generateResponse.CollectiveIdentifierNumber));
    }

    /// <summary>
    /// Weryfikuje nadanie uprawnienia `CollectiveIdentifierManage` w sposób pośredni (`POST /permissions/indirect/grants`),
    /// odpytanie o nadane uprawnienia przez podmiot docelowy w kontekście właściciela
    /// oraz wygenerowanie identyfikatora zbiorczego.
    /// 1) Właściciel wystawia faktury.
    /// 2) Właściciel nadaje podmiotowi pośredniczącemu uprawnienie CollectiveIdentifierManage z prawem dalszej delegacji.
    /// 3) Pośrednik nadaje podmiotowi docelowemu uprawnienie CollectiveIdentifierManage w kontekście właściciela.
    /// 4) Podmiot docelowy uwierzytelnia się w kontekście właściciela i odpytuje o uprawnienia.
    /// 5) Podmiot docelowy generuje identyfikator zbiorczy dla faktur właściciela.
    /// </summary>
    [Fact]
    public async Task GrantCollectiveIdentifierManagePermission_Indirectly_ThenGenerateCollectiveIdentifier()
    {
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string delegateNip = MiscellaneousUtils.GetRandomNip();
        string targetSubjectNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse ownerAuth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient, ownerNip).ConfigureAwait(false);
        string ownerToken = ownerAuth.AccessToken.Token;

        // Właściciel wystawia faktury
        string buyerNip = MiscellaneousUtils.GetRandomNip();
        List<string> ksefNumbers = new List<string>();
        for (int i = 0; i < InvoicesCount; i++)
        {
            string ksefNumber = await SendInvoiceAndGetKsefNumberAsync(ownerNip, buyerNip, ownerToken).ConfigureAwait(false);
            ksefNumbers.Add(ksefNumber);
        }

        // 1. Nadanie pośrednikowi uprawnienia CollectiveIdentifierManage z możliwością dalszej delegacji
        GrantPermissionsEntityRequest grantToDelegateRequest = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(new GrantPermissionsEntitySubjectIdentifier
            {
                Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                Value = delegateNip
            })
            .WithPermissions(
                EntityPermission.New(EntityStandardPermissionType.CollectiveIdentifierManage, canDelegate: true)
            )
            .WithDescription("E2E grant CollectiveIdentifierManage with delegation")
            .WithSubjectDetails(new PermissionsEntitySubjectDetails
            {
                FullName = $"Podmiot pośredniczący {delegateNip}"
            })
            .Build();

        OperationResponse grantToDelegateResponse = await KsefClient.GrantsPermissionEntityAsync(
            grantToDelegateRequest, ownerToken).ConfigureAwait(false);

        PermissionsOperationStatusResponse delegateGrantStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(grantToDelegateResponse.ReferenceNumber, ownerToken),
            condition: s => s?.Status?.Code == OperationStatusCodeResponse.Success,
            delay: PermissionPropagationDelay,
            maxAttempts: PermissionPropagationMaxAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        Assert.Equal(OperationStatusCodeResponse.Success, delegateGrantStatus.Status.Code);

        // 2. Uwierzytelnienie pośrednika
        AuthenticationOperationStatusResponse delegateAuth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient, identifierValue: delegateNip).ConfigureAwait(false);
        string delegateToken = delegateAuth.AccessToken.Token;

        // 3. Pośrednik nadaje uprawnienie CollectiveIdentifierManage w kontekście właściciela
        GrantPermissionsIndirectEntityRequest indirectRequest = GrantIndirectEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(new IndirectEntitySubjectIdentifier
            {
                Type = IndirectEntitySubjectIdentifierType.Nip,
                Value = targetSubjectNip
            })
            .WithContext(new IndirectEntityTargetIdentifier
            {
                Type = IndirectEntityTargetIdentifierType.Nip,
                Value = ownerNip
            })
            .WithPermissions(
                IndirectEntityStandardPermissionType.CollectiveIdentifierManage
            )
            .WithDescription("E2E indirect grant CollectiveIdentifierManage")
            .WithSubjectDetails(new PermissionsIndirectEntitySubjectDetails
            {
                SubjectDetailsType = PermissionsIndirectEntitySubjectDetailsType.PersonByIdentifier,
                PersonById = new PermissionsIndirectEntityPersonByIdentifier { FirstName = "Klient", LastName = "Końcowy" }
            })
            .Build();

        OperationResponse indirectGrantResponse = await KsefClient.GrantsPermissionIndirectEntityAsync(
            indirectRequest, delegateToken, CancellationToken).ConfigureAwait(false);

        Assert.NotNull(indirectGrantResponse);
        Assert.False(string.IsNullOrWhiteSpace(indirectGrantResponse.ReferenceNumber));

        PermissionsOperationStatusResponse indirectGrantStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(indirectGrantResponse.ReferenceNumber, delegateToken),
            condition: s => s?.Status?.Code == OperationStatusCodeResponse.Success,
            delay: PermissionPropagationDelay,
            maxAttempts: PermissionPropagationMaxAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        Assert.Equal(OperationStatusCodeResponse.Success, indirectGrantStatus.Status.Code);

        // 4. Uwierzytelnienie podmiotu docelowego w kontekście właściciela
        AuthenticationOperationStatusResponse targetAuth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            identifierValue: targetSubjectNip,
            contextIdentifierValue: ownerNip).ConfigureAwait(false);
        string targetToken = targetAuth.AccessToken.Token;

        // 5. Odpytanie o uprawnienia nadane w kontekście właściciela
        PersonalPermissionsQueryRequest personalQuery = new PersonalPermissionsQueryRequest();

        PersonalPermissionsQueryResponse permissionsResponse = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(personalQuery, targetToken),
            condition: r => r?.Permissions is not null && r.Permissions.Any(p => p.PermissionScope == PersonalPermission.PersonalPermissionScopeType.CollectiveIdentifierManage),
            delay: PermissionPropagationDelay,
            maxAttempts: PermissionPropagationMaxAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        Assert.NotNull(permissionsResponse);
        Assert.Contains(permissionsResponse.Permissions, p => p.PermissionScope == PersonalPermission.PersonalPermissionScopeType.CollectiveIdentifierManage);

        // 6. Generowanie identyfikatora zbiorczego przez podmiot docelowy w kontekście właściciela
        GenerateCollectiveIdentifierResponse generateResponse = await CollectiveIdentifiersClient.GenerateCollectiveIdentifierAsync(
            new GenerateCollectiveIdentifierRequest
            {
                Invoices = ksefNumbers.Select(ksefNum => new CollectiveIdentifierInvoice { KsefNumber = ksefNum }).ToList()
            },
            targetToken, CancellationToken).ConfigureAwait(false);

        Assert.NotNull(generateResponse);
        Assert.False(string.IsNullOrWhiteSpace(generateResponse.CollectiveIdentifierNumber));
    }

    /// <summary>
    /// Wystawia fakturę i czeka na jej trwałe zapisanie, zwracając nadany numer KSeF.
    /// </summary>
    private async Task<string> SendInvoiceAndGetKsefNumberAsync(string sellerNip, string buyerNip, string sellerToken)
    {
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        OpenOnlineSessionResponse session = await OnlineSessionUtils.OpenOnlineSessionAsync(
            KsefClient, encryptionData, sellerToken).ConfigureAwait(false);

        SendInvoiceResponse invoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
            KsefClient, session.ReferenceNumber, sellerToken, sellerNip, buyerNip,
            InvoiceTemplate, encryptionData, CryptographyService).ConfigureAwait(false);

        await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient, session.ReferenceNumber, sellerToken).ConfigureAwait(false);

        SessionInvoice processedInvoice = await AsyncPollingUtils.PollAsync(
            action: () => OnlineSessionUtils.GetSessionInvoiceStatusAsync(
                KsefClient, session.ReferenceNumber, invoiceResponse.ReferenceNumber, sellerToken),
            condition: inv => inv?.PermanentStorageDate is not null,
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxPollingAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        Assert.NotNull(processedInvoice?.KsefNumber);
        string ksefNumber = processedInvoice.KsefNumber;

        await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.GetInvoiceAsync(ksefNumber, sellerToken),
            condition: xml => !string.IsNullOrWhiteSpace(xml),
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxPollingAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        return ksefNumber;
    }
}

