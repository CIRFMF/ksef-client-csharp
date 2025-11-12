using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Api.Builders.SubUnitPermissions;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.E2E.Permissions.SubunitPermissions;

/// <summary>
/// Pobranie listy uprawnień w jednostkach podrzędnych jako jednostka nadrzędna grupy VAT.
/// Scenariusz:
/// 1) Utwórzenie podmiotu typu Grupa VAT z jednostką podrzędną
/// 2) Uwierzytelnienie się jako jednostka nadrzędna (Grupa VAT)
/// 3) Uwierzytelnienie się w kontekście jednostki podrzędnej (certyfikat osobisty)
/// 4) Nadanie uprawnienienia administratora jednostki podrzędnej (w kontekście jednostki podrzędnej)
/// 5) Jako jednostka nadrzędna pobranie listy uprawnień w jednostkach podrzędnych i weryfikacja wyniku
/// 6) Cofnięcie nadanych uprawnień i sprzątnięcie danych testowych
/// </summary>
public class VatGroupParentSubunitPermissionsQueryE2ETests : TestBase
{
    private const int DefaultPageOffset = 0;
    private const int DefaultPageSize = 10;

    private readonly string _vatGroupNip = MiscellaneousUtils.GetRandomNip();
    private readonly string _subunitNip = MiscellaneousUtils.GetRandomNip();
    private string _parentInternalId => _vatGroupNip + "-00001";

    private string _parentAccessToken = string.Empty;
    private string _subunitAccessToken = string.Empty;
    private string _grantedAdminSubjectNip = string.Empty;

    [Fact]
    public async Task SubunitPermissions_AsVatGroupParent_ShouldReturnList()
    {
        // Arrange: utworzenie grupy VAT z jednostką podrzędną
        await CreateVatGroupWithSubunitAsync();

        // Arrange: uwierzytelnienie jednostki nadrzędnej
        _parentAccessToken = await AuthenticateAsync(_vatGroupNip);
        Assert.False(string.IsNullOrWhiteSpace(_parentAccessToken));

        // Arrange: nadanie jednostce podrzędnej uprawnień SubunitManage i CredentialsManage w kontekście jednostki nadrzędnej
        OperationResponse personGrantOperation = await GrantPersonPermissionsForSubunitAsync();
        Assert.NotNull(personGrantOperation);
        Assert.False(string.IsNullOrEmpty(personGrantOperation.ReferenceNumber));

        // Assert: status operacji nadania uprawnień osobowych = 200
        PermissionsOperationStatusResponse personGrantStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(personGrantOperation.ReferenceNumber, _parentAccessToken),
            condition: status => status?.Status?.Code == OperationStatusCodeResponse.Success,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken);
        Assert.Equal(OperationStatusCodeResponse.Success, personGrantStatus.Status.Code);

        // Act: uwierzytelnienie jako jednostka podrzędna w kontekście jednostki nadrzędnej (certyfikat osobisty)
        _subunitAccessToken = await AuthenticateAsSubunitAsync(_vatGroupNip, _subunitNip);
        Assert.False(string.IsNullOrWhiteSpace(_subunitAccessToken));

        // Act: nadanie uprawnienia administratora jednostki podrzędnej
        OperationResponse grantResponse = await GrantSubunitAdminPermissionAsync(_subunitAccessToken);
        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrWhiteSpace(grantResponse.ReferenceNumber));

        // Assert: status operacji nadania uprawnienia administratora = 200
        PermissionsOperationStatusResponse grantStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(grantResponse.ReferenceNumber, _subunitAccessToken),
            condition: s => s?.Status?.Code == OperationStatusCodeResponse.Success,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken);
        Assert.Equal(OperationStatusCodeResponse.Success, grantStatus.Status.Code);

        // Act: jako jednostka nadrzędna pobierz listę uprawnień w jednostkach podrzędnych
        SubunitPermissionsQueryRequest query = new SubunitPermissionsQueryRequest
        {
            SubunitIdentifier = new SubunitPermissionsSubunitIdentifier
            {
                Type = SubunitIQuerydentifierType.InternalId,
                Value = _parentInternalId
            }
        };

        PagedPermissionsResponse<SubunitPermission> permissions = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.SearchSubunitAdminPermissionsAsync(query, _parentAccessToken, DefaultPageOffset, DefaultPageSize, CancellationToken),
            condition: r => r is not null && r.Permissions is not null && r.Permissions.Count > 0,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken);

        // Assert: lista uprawnień nie jest pusta i zawiera oczekiwane uprawnienie
        Assert.NotNull(permissions);
        Assert.NotEmpty(permissions.Permissions);
        Assert.Contains(permissions.Permissions, p =>
            p is not null &&
            p.AuthorizedIdentifier is not null &&
            p.SubunitIdentifier is not null &&
            p.AuthorizedIdentifier.Type == SubunitPermissionAuthorizedIdentifierType.Nip &&
            p.AuthorizedIdentifier.Value == _grantedAdminSubjectNip &&
            p.SubunitIdentifier.Type == SubunitIdentifierType.InternalId &&
            p.SubunitIdentifier.Value == _parentInternalId);

        // Act: cofnięcie nadanych uprawnień (jako jednostka nadrzędna)
        List<PermissionsOperationStatusResponse> revokeStatuses = await RevokePermissionsAsync(permissions.Permissions, _parentAccessToken);

        // Assert: wszystkie operacje cofnięcia zakończyły się powodzeniem
        Assert.NotNull(revokeStatuses);
        Assert.NotEmpty(revokeStatuses);
        Assert.All(revokeStatuses, rs => Assert.Equal(OperationStatusCodeResponse.Success, rs.Status.Code));

        // Arrange/Act: sprzątanie danych testowych (bez asercji)
        await RemoveSubjectAsync();
    }

    /// <summary>
    /// Tworzy testowy podmiot typu Grupa VAT wraz z jedną jednostką podrzędną.
    /// </summary>
    /// <returns>Zadanie asynchroniczne bez wyniku.</returns>
    private async Task CreateVatGroupWithSubunitAsync()
    {
        ITestDataClient testData = TestDataClient;
        Client.Core.Models.TestData.SubjectCreateRequest createRequest = new Client.Core.Models.TestData.SubjectCreateRequest
        {
            SubjectNip = _vatGroupNip,
            SubjectType = Client.Core.Models.TestData.SubjectType.VatGroup,
            Subunits = new List<Client.Core.Models.TestData.SubjectSubunit>
            {
                new Client.Core.Models.TestData.SubjectSubunit
                {
                    SubjectNip = _subunitNip,
                    Description = "Jednostka podrzędna - Grupa VAT"
                }
            },
            Description = "Grupa VAT testowa"
        };

        await testData.CreateSubjectAsync(createRequest, CancellationToken);
    }

    /// <summary>
    /// Usuwa testowy podmiot główny (Grupa VAT) wraz z jednostkami podrzędnymi.
    /// </summary>
    /// <returns>Zadanie asynchroniczne bez wyniku.</returns>
    private async Task RemoveSubjectAsync()
    {
        await TestDataClient.RemoveSubjectAsync(new Client.Core.Models.TestData.SubjectRemoveRequest { SubjectNip = _vatGroupNip }, CancellationToken);
    }

    /// <summary>
    /// Uwierzytelnia podmiot po NIP i zwraca access token.
    /// </summary>
    /// <param name="nip">NIP podmiotu do uwierzytelnienia.</param>
    /// <returns>Access token ciąg znaków.</returns>
    private async Task<string> AuthenticateAsync(string nip)
    {
        AuthenticationOperationStatusResponse auth = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, nip);
        return auth.AccessToken.Token;
    }

    /// <summary>
    /// Uwierzytelnia jako jednostka podrzędna w kontekście jednostki nadrzędnej, wykorzystując certyfikat osobisty.
    /// </summary>
    /// <param name="unitNip">NIP jednostki nadrzędnej (kontekst).</param>
    /// <param name="subunitNip">NIP jednostki podrzędnej (serialNumber w certyfikacie).</param>
    /// <returns>Access token ciąg znaków.</returns>
    private async Task<string> AuthenticateAsSubunitAsync(string unitNip, string subunitNip)
    {
        X509Certificate2 personalCertificate = CertificateUtils.GetPersonalCertificate(
            givenName: "Anna",
            surname: "Testowa",
            serialNumberPrefix: "TINPL",
            serialNumber: subunitNip,
            commonName: "Anna Testowa - Jednostka podrzędna");

        AuthenticationOperationStatusResponse auth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            unitNip,
            AuthenticationTokenContextIdentifierType.Nip,
            personalCertificate);

        return auth.AccessToken.Token;
    }

    /// <summary>
    /// Nadaje jednostce podrzędnej osobowe uprawnienia SubunitManage i CredentialsManage w kontekście jednostki nadrzędnej.
    /// </summary>
    /// <returns>Odpowiedź operacji z numerem referencyjnym.</returns>
    private async Task<OperationResponse> GrantPersonPermissionsForSubunitAsync()
    {
        GrantPermissionsPersonRequest personGrantRequest = GrantPersonPermissionsRequestBuilder.Create()
            .WithSubject(new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
                Value = _subunitNip
            })
            .WithPermissions(PersonPermissionType.SubunitManage, PersonPermissionType.CredentialsManage)
            .WithDescription("E2E test - nadanie uprawnień osobowych do zarządzania jednostką podrzędną")
            .Build();

        OperationResponse operationResponse = await KsefClient.GrantsPermissionPersonAsync(personGrantRequest, _parentAccessToken);
        return operationResponse;
    }

    /// <summary>
    /// Nadaje uprawnienie administratora jednostki podrzędnej w kontekście tej jednostki.
    /// </summary>
    /// <param name="accessToken">Access token w kontekście jednostki podrzędnej.</param>
    /// <returns>Odpowiedź operacji z numerem referencyjnym.</returns>
    private async Task<OperationResponse> GrantSubunitAdminPermissionAsync(string accessToken)
    {
        string subjectNip = MiscellaneousUtils.GetRandomNip();
        _grantedAdminSubjectNip = subjectNip;

        GrantPermissionsSubunitRequest request = GrantSubunitPermissionsRequestBuilder
            .Create()
            .WithSubject(new SubunitSubjectIdentifier
            {
                Type = SubUnitSubjectIdentifierType.Nip,
                Value = subjectNip
            })
            .WithContext(new SubunitContextIdentifier
            {
                Type = SubunitContextIdentifierType.InternalId,
                Value = _parentInternalId
            })
            .WithSubunitName("E2E VATGroup Jednostka podrzędna")
            .WithDescription("E2E - nadanie uprawnień administratora w kontekście jednostki podrzędnej")
            .Build();

        return await KsefClient.GrantsPermissionSubUnitAsync(request, accessToken, CancellationToken);
    }

    /// <summary>
    /// Cofnięcie wskazanych uprawnień i odpytywanie o status aż do powodzenia.
    /// </summary>
    /// <param name="permissions">Lista uprawnień do cofnięcia.</param>
    /// <param name="accessToken">Access token użyty do cofnięcia.</param>
    /// <returns>Lista statusów operacji po zakończeniu (200).</returns>
    private async Task<List<PermissionsOperationStatusResponse>> RevokePermissionsAsync(IEnumerable<SubunitPermission> permissions, string accessToken)
    {
        List<OperationResponse> revokeOperations = new();
        foreach (SubunitPermission permission in permissions)
        {
            OperationResponse resp = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeOperations.Add(resp);
        }

        List<PermissionsOperationStatusResponse> statuses = new();
        foreach (OperationResponse revokeOperation in revokeOperations)
        {
            PermissionsOperationStatusResponse status = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.OperationsStatusAsync(revokeOperation.ReferenceNumber, accessToken),
                condition: s => s?.Status?.Code == OperationStatusCodeResponse.Success,
                delay: TimeSpan.FromSeconds(1),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

            statuses.Add(status);
        }

        return statuses;
    }
}
