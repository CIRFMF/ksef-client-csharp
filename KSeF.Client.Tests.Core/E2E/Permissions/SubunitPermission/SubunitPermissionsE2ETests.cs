using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Api.Builders.SubUnitPermissions;
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
/// Testy end-to-end dla uprawnień jednostek podrzędnych w systemie KSeF.
/// Obejmuje scenariusze nadawania i odwoływania uprawnień oraz ich weryfikację.
/// </summary>

[Collection("SubunitPermissionsScenarioE2ECollection")]
public class SubunitPermissionsE2ETests : TestBase
{
    private readonly SubunitPermissionsScenarioE2EFixture _fixture;

    private const int DefaultPageOffset = 0;
    private const int DefaultPageSize = 10;

    private string _unitAccessToken = string.Empty;
    private string _subunitAccessToken = string.Empty;
    

    public SubunitPermissionsE2ETests()
    {
        _fixture = new SubunitPermissionsScenarioE2EFixture();

        _fixture.UnitNipInternal = _fixture.Unit.Value + "-00001";
    }

    /// <summary>
    /// Test end-to-end dla pełnego cyklu zarządzania uprawnieniami jednostki podrzędnej:
    /// 1. Inicjalizacja i uwierzytelnienie jednostki głównej
    /// 2. Nadanie uprawnień do zarządzania jednostką podrzędną
    /// 3. Uwierzytelnienie w kontekście jednostki podrzędnej
    /// 4. Nadanie uprawnień administratora podmiotu podrzędnego
    /// 5. Weryfikacja nadanych uprawnień
    /// 6. Odwołanie uprawnień i weryfikacja
    /// </summary>
    [Fact]
    public async Task SubUnitPermission_E2E_GrantAndRevoke()
    {
        #region Inicjalizuje uwierzytelnienie jednostki głównej.
        // Arrange

        // Act
        _unitAccessToken = await AuthenticateAsUnitAsync();

        // Assert
        Assert.NotEmpty(_unitAccessToken);
        #endregion

        #region Nadanie uprawnienia SubunitManage, CredentialsManage do zarządzania jednostką podrzędną
        // Arrange & Act
        OperationResponse personGrantOperation = await GrantPersonPermissionsAsync();

        // Assert
        Assert.NotNull(personGrantOperation);
        Assert.False(string.IsNullOrEmpty(personGrantOperation.ReferenceNumber));

        // Polling do uzyskania statusu 200 
        PermissionsOperationStatusResponse grantOperationStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(personGrantOperation.ReferenceNumber, _unitAccessToken),
            condition: status => status?.Status?.Code == OperationStatusCodeResponse.Success,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );

        Assert.Equal(OperationStatusCodeResponse.Success, grantOperationStatus.Status.Code);
        #endregion

        #region Uwierzytelnia w kontekście jednostki głównej jako jednostka podrzędna przy użyciu certyfikatu osobistego.
        // Arrange & Act
        _subunitAccessToken = await AuthenticateAsSubunitAsync();

        // Assert
        Assert.NotEmpty(_subunitAccessToken);
        #endregion

        #region Nadanie uprawnień administratora podmiotu podrzędnego jako jednostka podrzędna
        // Arrange & Act
        OperationResponse grantSubunitResponse = await GrantSubunitPermissionsAsync();
        _fixture.GrantResponse = grantSubunitResponse;

        // Assert
        Assert.NotNull(_fixture.GrantResponse);
        Assert.False(string.IsNullOrEmpty(_fixture.GrantResponse.ReferenceNumber));

        // Polling statusu operacji nadania uprawnień jednostce podrzędnej
        PermissionsOperationStatusResponse grantSubunitStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(_fixture.GrantResponse.ReferenceNumber, _subunitAccessToken),
            condition: status => status?.Status?.Code == OperationStatusCodeResponse.Success,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );

        Assert.Equal(OperationStatusCodeResponse.Success, grantSubunitStatus.Status.Code);
        #endregion

        #region Wyszukaj uprawnienia nadane administratorowi jednostki podrzędnej
        // Arrange & Act - polling aż pojawią się uprawnienia
        PagedPermissionsResponse<SubunitPermission> pagedPermissions =
            await AsyncPollingUtils.PollAsync(
                action: () => SearchSubUnitAsync(),
                condition: resp => resp is not null && resp.Permissions is not null && resp.Permissions.Count > 0,
                delay: TimeSpan.FromSeconds(1),
                maxAttempts: 60,
                cancellationToken: CancellationToken
            );

        _fixture.SearchResponse = pagedPermissions;

        // Assert
        Assert.NotNull(_fixture.SearchResponse);
        Assert.NotEmpty(_fixture.SearchResponse.Permissions);
        #endregion

        #region Wyszukaj uprawnienia nadane administratorowi jednostki podrzędnej - dedykowana końcówka do wyszukiwania uprawnień
        SubunitPermissionsQueryRequest request = new SubunitPermissionsQueryRequest
        {
            SubunitIdentifier = new SubunitPermissionsSubunitIdentifier
            {
                Type = SubunitIQuerydentifierType.InternalId,
                Value = _fixture.UnitNipInternal
            }
        };

        PagedPermissionsResponse<SubunitPermission> response = await KsefClient.SearchSubunitAdminPermissionsAsync(
            new SubunitPermissionsQueryRequest(),
            _subunitAccessToken,
            pageOffset: DefaultPageOffset,
            pageSize: DefaultPageSize,
            CancellationToken);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Permissions);
        #endregion

        #region Pobierz listę podmiotów podrzędnych jeżeli podmiot bieżącego kontekstu ma rolę podmiotu nadrzędnego
        SubordinateEntityRolesQueryRequest subordinateEntityRolesQueryRequest = new SubordinateEntityRolesQueryRequest
        {
            SubordinateEntityIdentifier = new EntityPermissionsSubordinateEntityIdentifier
            {
                Type = EntityPermissionsSubordinateEntityIdentifierType.Nip,
                Value = _fixture.Unit.Value
            }
        };

        PagedRolesResponse<SubordinateEntityRole> searchSubordinateEntityInvoiceRolesResponse = 
            await KsefClient.SearchSubordinateEntityInvoiceRolesAsync(subordinateEntityRolesQueryRequest, _unitAccessToken);
        
        Assert.NotNull(searchSubordinateEntityInvoiceRolesResponse);    
        #endregion

        #region Cofnij uprawnienia nadane administratorowi jednostki podrzędnej
        // Arrange &  Act - odwołanie uprawnień + polling statusów każdej operacji
        List<PermissionsOperationStatusResponse> revokeStatuses =
            await RevokeSubUnitPermissionsAsync(_fixture.SearchResponse.Permissions);

        _fixture.RevokeStatusResults = revokeStatuses;

        // Assert
        Assert.NotNull(_fixture.RevokeStatusResults);
        Assert.NotEmpty(_fixture.RevokeStatusResults);
        Assert.Equal(_fixture.SearchResponse.Permissions.Count, _fixture.RevokeStatusResults.Count);
        Assert.All(_fixture.RevokeStatusResults, r =>
            Assert.True(r.Status.Code == OperationStatusCodeResponse.Success,
                $"Operacja cofnięcia uprawnień nie powiodła się: {r.Status.Description}, szczegóły: [{string.Join(",", r.Status.Details ?? Array.Empty<string>())}]")
        );
        #endregion

        #region Sprawdź czy uprawnienia administratora jednostki podrzędnej zostały cofnięte
        // Arrange & Act - polling aż lista uprawnień będzie pusta
        PagedPermissionsResponse<SubunitPermission> pagedPermissionsAfterRevoke =
            await AsyncPollingUtils.PollAsync(
                action: () => SearchSubUnitAsync(),
                condition: resp => resp is not null && resp.Permissions is not null && resp.Permissions.Count == 0,
                delay: TimeSpan.FromSeconds(1),
                maxAttempts: 60,
                cancellationToken: CancellationToken
            );

        _fixture.SearchResponse = pagedPermissionsAfterRevoke;

        // Assert
        Assert.Empty(pagedPermissionsAfterRevoke.Permissions);
        #endregion
    }

    /// <summary>
    /// Inicjalizuje uwierzytelnienie jednostki głównej.
    /// </summary>
    private async Task<string> AuthenticateAsUnitAsync()
    {
        AuthenticationOperationStatusResponse authInfo = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            _fixture.Unit.Value);

        return authInfo.AccessToken.Token;
    }

    /// <summary>
    /// Uwierzytelnia w kontekście jednostki głównej jako jednostka podrzędna przy użyciu certyfikatu osobistego.
    /// </summary>
    private async Task<string> AuthenticateAsSubunitAsync()
    {
        X509Certificate2 personalCertificate = CertificateUtils.GetPersonalCertificate(
            givenName: "Jan",
            surname: "Testowy",
            serialNumberPrefix: "TINPL",
            serialNumber: _fixture.Subunit.Value,
            commonName: "Jan Testowy Certificate");

        AuthenticationOperationStatusResponse ownerAuthInfo = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            _fixture.Unit.Value,
            AuthenticationTokenContextIdentifierType.Nip,
            personalCertificate);

        return ownerAuthInfo.AccessToken.Token;
    }

    /// <summary>
    /// Nadaje uprawnienia osobowe do zarządzania jednostką podrzędną (SubunitManage, CredentialsManage).
    /// Zwraca numer referencyjny operacji.
    /// </summary>
    private async Task<OperationResponse> GrantPersonPermissionsAsync()
    {
        GrantPermissionsPersonRequest personGrantRequest = GrantPersonPermissionsRequestBuilder.Create()
            .WithSubject(new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
                Value = _fixture.Subunit.Value
            })
            .WithPermissions(PersonPermissionType.SubunitManage, PersonPermissionType.CredentialsManage)
            .WithDescription("E2E test - nadanie uprawnień osobowych do zarządzania jednostką podrzędną")
            .Build();

        OperationResponse operationResponse = await KsefClient.GrantsPermissionPersonAsync(personGrantRequest, _unitAccessToken);
        return operationResponse;
    }

    /// <summary>
    /// Nadaje uprawnienia jednostce podrzędnej w kontekście jednostki głównej.
    /// </summary>
    /// <returns>Numer referencyjny operacji.</returns>
    private async Task<OperationResponse> GrantSubunitPermissionsAsync()
    {
        GrantPermissionsSubunitRequest subunitGrantRequest =
            GrantSubunitPermissionsRequestBuilder
            .Create()
            .WithSubject(_fixture.SubjectIdentifier)
            .WithContext(new SubunitContextIdentifier
            {
                Type = SubunitContextIdentifierType.InternalId,
                Value = _fixture.UnitNipInternal
            })
            .WithSubunitName("E2E Test Subunit")
            .WithDescription("E2E test grant sub-unit")
            .Build();

        OperationResponse operationResponse = await KsefClient
            .GrantsPermissionSubUnitAsync(subunitGrantRequest, _subunitAccessToken, CancellationToken);

        return operationResponse;
    }

    /// <summary>
    /// Wyszukuje uprawnienia nadane jednostce podrzędnej.
    /// </summary>
    /// <returns>Stronicowana lista uprawnień nadanych jednostce podrzędnej.</returns>
    private async Task<PagedPermissionsResponse<SubunitPermission>> SearchSubUnitAsync()
    {
        PagedPermissionsResponse<SubunitPermission> pagedSubunitPermissions =
            await SearchSubUnitAdminPermissionsAsync();

        return pagedSubunitPermissions;
    }

    /// <summary>
    /// Wyszukuje uprawnienia administratorskie nadane jednostce podrzędnej.
    /// </summary>
    /// <returns>Stronicowana lista uprawnień administratorskich nadanych jednostce podrzędnej.</returns>
    private async Task<PagedPermissionsResponse<SubunitPermission>> SearchSubUnitAdminPermissionsAsync()
    {
        SubunitPermissionsQueryRequest subunitPermissionsQueryRequest = new SubunitPermissionsQueryRequest();
        PagedPermissionsResponse<SubunitPermission> response =
            await KsefClient
            .SearchSubunitAdminPermissionsAsync(
                subunitPermissionsQueryRequest,
                _subunitAccessToken,
                pageOffset: 0,
                pageSize: 10,
                CancellationToken);

        return response;
    }

    /// <summary>
    /// Odwołuje uprawnienia nadane wskazanym uprawnieniom jednostki podrzędnej i zwraca statusy operacji po wypollowaniu.
    /// </summary>
    private async Task<List<PermissionsOperationStatusResponse>> RevokeSubUnitPermissionsAsync(IEnumerable<SubunitPermission> permissionsToRevoke)
    {
        List<OperationResponse> revokeResponses = new();
        foreach (SubunitPermission permission in permissionsToRevoke)
        {
            OperationResponse response =
                await KsefClient.RevokeCommonPermissionAsync(permission.Id, _subunitAccessToken, CancellationToken.None);

            revokeResponses.Add(response);
        }

        List<PermissionsOperationStatusResponse> statuses = new();
        foreach (OperationResponse revokeResponse in revokeResponses)
        {
            PermissionsOperationStatusResponse revokeStatus = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.OperationsStatusAsync(revokeResponse.ReferenceNumber, _subunitAccessToken),
                condition: status => status?.Status?.Code == OperationStatusCodeResponse.Success,
                delay: TimeSpan.FromSeconds(1),
                maxAttempts: 60,
                cancellationToken: CancellationToken
            );

            statuses.Add(revokeStatus);
        }

        return statuses;
    }
}