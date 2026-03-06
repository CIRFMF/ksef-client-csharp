using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Api.Builders.SubEntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.E2E.Permissions.SubunitPermission;

/// <summary>
/// Testy end-to-end uprawnień jednostek podrzędnych w systemie KSeF.
/// Obejmuje scenariusze nadawania i odwoływania uprawnień oraz ich weryfikację.
/// </summary>

[Collection("SubunitPermissionsScenarioE2ECollection")]
public class SubunitPermissionsE2ETests : TestBase
{
    private readonly SubunitPermissionsScenarioE2EFixture _fixture;

    private const int DefaultPageOffset = 0;
    private const int DefaultPageSize = 10;

    private const string GivenNameJan = "Jan";
    private const string SurnameTestowy = "Testowy";
    private const string PersonCertSerialPrefixTinpl = "TINPL";
    private const string PersonCertCommonNameJanTestowy = "Certyfikat Jana Testowego";

    private const string E2EGrantPersonPermissionsDescription = "Test E2E – nadanie uprawnień osobowych do zarządzania jednostką podrzędną";
    private const string E2EGrantSubunitPermissionsDescription = "Test E2E – nadanie uprawnień jednostce podrzędnej";
    private const string E2ESubunitName = "Jednostka podrzędna – Test E2E";
    private const string E2EGrantMinimalSubunitManageDescription = "Test E2E – nadanie SubunitManage w celu włączenia widoczności ról podrzędnych";

    private const string FirstNameJan = "Jan";
    private const string LastNameTestowy = "Testowy";
    private const string LastNameKowalski = "Kowalski";

    private string _unitAccessToken = string.Empty;
    private string _subunitAccessToken = string.Empty;


    public SubunitPermissionsE2ETests()
    {
        _fixture = new SubunitPermissionsScenarioE2EFixture();
    }

    /// <summary>
    /// Test end-to-end pełnego cyklu zarządzania uprawnieniami jednostki podrzędnej:
    /// 1. Inicjalizacja i uwierzytelnienie jednostki głównej
    /// 2. Nadanie uprawnień do zarządzania jednostką podrzędną
    /// 3. Uwierzytelnienie w kontekście jednostki podrzędnej
    /// 4. Nadanie uprawnień administratora podmiotu podrzędnego
    /// 5. Weryfikacja nadanych uprawnień
    /// 6. Odwołanie uprawnień i weryfikacja
    /// </summary>
    [Fact]
    public async Task SubUnitPermissionE2EGrantAndRevoke()
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
        PagedPermissionsResponse<Client.Core.Models.Permissions.SubunitPermission> pagedPermissions =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchSubunitAdminPermissionsAsync(
                    new SubunitPermissionsQueryRequest(),
                    _subunitAccessToken,
                    pageOffset: 0,
                    pageSize: 10,
                    CancellationToken
                ),
                condition: resp => resp is not null && resp.Permissions is not null && resp.Permissions.Count > 0,
                delay: TimeSpan.FromSeconds(1),
                maxAttempts: 60,
                cancellationToken: CancellationToken
            );

        _fixture.SearchResponse = pagedPermissions;

        // Assert
        Assert.NotNull(_fixture.SearchResponse);
        Assert.NotEmpty(_fixture.SearchResponse.Permissions);
        Assert.Contains(pagedPermissions.Permissions,
            x => x.PermissionScope == SubunitPermissionType.CredentialsManage);

        Assert.All(pagedPermissions.Permissions, permission =>
		{
			Assert.False(string.IsNullOrEmpty(permission.Id));

			Assert.NotNull(permission.AuthorizedIdentifier);
			Assert.Equal(SubunitPermissionAuthorizedIdentifierType.Nip, permission.AuthorizedIdentifier.Type);
            Assert.False(string.IsNullOrEmpty(permission.AuthorizedIdentifier.Value));

            Assert.NotNull(permission.SubunitIdentifier);
			Assert.Equal(SubunitIdentifierType.InternalId, permission.SubunitIdentifier.Type);
			Assert.False(string.IsNullOrEmpty(permission.SubunitIdentifier.Value));

            Assert.NotNull(permission.AuthorIdentifier);
			Assert.Equal(AuthorIdentifierType.Nip, permission.AuthorIdentifier.Type);
			Assert.False(string.IsNullOrEmpty(permission.AuthorIdentifier.Value));

			Assert.False(string.IsNullOrEmpty(permission.Description));
			Assert.NotEqual(default, permission.StartDate);
		});
		#endregion

		#region Wyszukaj uprawnienia nadane administratorowi jednostki podrzędnej - dedykowana końcówka do wyszukiwania uprawnień
		SubunitPermissionsQueryRequest request = new()
        {
            SubunitIdentifier = new SubunitPermissionsSubunitIdentifier
            {
                Type = SubunitIQuerydentifierType.InternalId,
                Value = _fixture.UnitNipInternal
            }
        };

        PagedPermissionsResponse<Client.Core.Models.Permissions.SubunitPermission> response = await KsefClient.SearchSubunitAdminPermissionsAsync(
            new SubunitPermissionsQueryRequest(),
            _subunitAccessToken,
            pageOffset: DefaultPageOffset,
            pageSize: DefaultPageSize,
            CancellationToken);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Permissions);
        #endregion

        #region Pobierz listę podmiotów podrzędnych jeżeli podmiot bieżącego kontekstu ma rolę podmiotu nadrzędnego
        SubordinateEntityRolesQueryRequest subordinateEntityRolesQueryRequest = new()
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
		Assert.All(searchSubordinateEntityInvoiceRolesResponse.Roles, role =>
		{
			Assert.NotNull(role.SubordinateEntityIdentifier);
			Assert.True(Enum.IsDefined(typeof(SubordinateEntityIdentifierType), role.SubordinateEntityIdentifier.Type));
			Assert.False(string.IsNullOrEmpty(role.SubordinateEntityIdentifier.Value));

			Assert.True(Enum.IsDefined(typeof(SubordinateEntityRoleType), role.Role));
			Assert.False(string.IsNullOrEmpty(role.Description));
			Assert.NotEqual(default, role.StartDate);
		});

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
        PagedPermissionsResponse<Client.Core.Models.Permissions.SubunitPermission> pagedPermissionsAfterRevoke =
            await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.SearchSubunitAdminPermissionsAsync(
                new SubunitPermissionsQueryRequest(),
                _subunitAccessToken,
                pageOffset: 0,
                pageSize: 10,
                CancellationToken
                ),
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
    /// Dedykowany test E2E metody SearchSubordinateEntityInvoiceRolesAsync.
    /// Kroki:
    /// 1) Uwierzytelnienie jednostki głównej
    /// 2) Nadanie minimalnych uprawnień osobowych umożliwiających zarządzanie subjednostką
    /// 3) Weryfikacja statusu nadania
    /// 4) Wywołanie SearchSubordinateEntityInvoiceRolesAsync i weryfikacja zwróconych ról
    /// </summary>
    [Fact]
    public async Task SearchSubordinateEntityInvoiceRolesReturnsRolesForParentEntity()
    {
        // 1) auth parent unit
        _unitAccessToken = await AuthenticateAsUnitAsync();
        Assert.False(string.IsNullOrWhiteSpace(_unitAccessToken));

		PersonPermissionSubjectDetails subjectDetails = new PersonPermissionSubjectDetails
		{
			SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
			PersonById = new PersonPermissionPersonById
			{
				FirstName = "Anna",
				LastName = "Testowa"
			}
		};

		// 2) Nadanie minimalnych uprawnień osobowych umożliwiających zarządzanie subjednostką
		GrantPermissionsPersonRequest personGrantRequest = GrantPersonPermissionsRequestBuilder.Create()
            .WithSubject(new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
                Value = _fixture.Subunit.Value
            })
            .WithPermissions(PersonPermissionType.SubunitManage)
            .WithDescription(E2EGrantMinimalSubunitManageDescription)
            .WithSubjectDetails(subjectDetails)
            .Build();

        OperationResponse grantResp = await KsefClient.GrantsPermissionPersonAsync(personGrantRequest, _unitAccessToken).ConfigureAwait(true);
        Assert.NotNull(grantResp);
        Assert.False(string.IsNullOrWhiteSpace(grantResp.ReferenceNumber));

        // 3) Weryfikacja statusu nadania
        PermissionsOperationStatusResponse grantStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(grantResp.ReferenceNumber, _unitAccessToken),
            condition: s => s?.Status?.Code == OperationStatusCodeResponse.Success,
            cancellationToken: CancellationToken
        );
        Assert.Equal(OperationStatusCodeResponse.Success, grantStatus.Status.Code);

        // 4) Wywołanie SearchSubordinateEntityInvoiceRolesAsync i weryfikacja zwróconych ról
        SubordinateEntityRolesQueryRequest query = new()
        {
            SubordinateEntityIdentifier = new EntityPermissionsSubordinateEntityIdentifier
            {
                Type = EntityPermissionsSubordinateEntityIdentifierType.Nip,
                Value = _fixture.Unit.Value
            }
        };

        PagedRolesResponse<SubordinateEntityRole> roles = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.SearchSubordinateEntityInvoiceRolesAsync(query, _unitAccessToken,
                pageOffset: DefaultPageOffset,
                pageSize: DefaultPageSize,
                CancellationToken),
            condition: r => r is not null && r.Roles is not null,
            cancellationToken: CancellationToken
        );

        Assert.NotNull(roles);
        Assert.NotNull(roles.Roles);
        
        Assert.All(roles.Roles, r =>
            Assert.Equal(_fixture.Unit.Value, r.SubordinateEntityIdentifier.Value));
        Assert.All(roles.Roles, r =>
            Assert.Equal(SubordinateEntityIdentifierType.Nip, r.SubordinateEntityIdentifier.Type));
    }

    /// <summary>
    /// Inicjalizuje uwierzytelnienie jednostki głównej.
    /// </summary>
    private async Task<string> AuthenticateAsUnitAsync()
    {
        AuthenticationOperationStatusResponse authInfo = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            _fixture.Unit.Value).ConfigureAwait(false);

        return authInfo.AccessToken.Token;
    }

    /// <summary>
    /// Uwierzytelnia w kontekście jednostki głównej jako jednostka podrzędna przy użyciu certyfikatu osobistego.
    /// </summary>
    private async Task<string> AuthenticateAsSubunitAsync()
    {
        X509Certificate2 personalCertificate = CertificateUtils.GetPersonalCertificate(
            givenName: GivenNameJan,
            surname: SurnameTestowy,
            serialNumberPrefix: PersonCertSerialPrefixTinpl,
            serialNumber: _fixture.Subunit.Value,
            commonName: PersonCertCommonNameJanTestowy);

        AuthenticationOperationStatusResponse ownerAuthInfo = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            _fixture.Unit.Value,
            AuthenticationTokenContextIdentifierType.Nip,
            personalCertificate).ConfigureAwait(false);

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
            .WithDescription(E2EGrantPersonPermissionsDescription)
            .WithSubjectDetails(new PersonPermissionSubjectDetails
            {
                SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
                PersonById = new PersonPermissionPersonById
                {
                    FirstName = FirstNameJan,
                    LastName = LastNameTestowy
                },
            })
            .Build();

        OperationResponse operationResponse = await KsefClient.GrantsPermissionPersonAsync(personGrantRequest, _unitAccessToken).ConfigureAwait(false);
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
            .WithSubunitName(E2ESubunitName)
            .WithDescription(E2EGrantSubunitPermissionsDescription)
            .WithSubjectDetails(new SubunitSubjectDetails
            {
                SubjectDetailsType = PermissionsSubunitSubjectDetailsType.PersonByIdentifier,
                PersonById = new PermissionsSubunitPersonByIdentifier { FirstName = FirstNameJan, LastName = LastNameKowalski }
            })
            .Build();

        OperationResponse operationResponse = await KsefClient
            .GrantsPermissionSubUnitAsync(subunitGrantRequest, _subunitAccessToken, CancellationToken).ConfigureAwait(false);

        return operationResponse;
    }

    /// <summary>
    /// Odwołuje uprawnienia nadane wskazanym uprawnieniom jednostki podrzędnej i zwraca statusy operacji po wypollowaniu.
    /// </summary>
    private async Task<List<PermissionsOperationStatusResponse>> RevokeSubUnitPermissionsAsync(IEnumerable<Client.Core.Models.Permissions.SubunitPermission> permissionsToRevoke)
    {
        List<OperationResponse> revokeResponses = [];
        foreach (Client.Core.Models.Permissions.SubunitPermission permission in permissionsToRevoke)
        {
            OperationResponse response =
                await KsefClient.RevokeCommonPermissionAsync(permission.Id, _subunitAccessToken, CancellationToken.None).ConfigureAwait(false);

            revokeResponses.Add(response);
        }

        List<PermissionsOperationStatusResponse> statuses = [];
        foreach (OperationResponse revokeResponse in revokeResponses)
        {
            PermissionsOperationStatusResponse revokeStatus = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.OperationsStatusAsync(revokeResponse.ReferenceNumber, _subunitAccessToken),
                condition: status => status?.Status?.Code == OperationStatusCodeResponse.Success,
                cancellationToken: CancellationToken
            ).ConfigureAwait(false);

            statuses.Add(revokeStatus);
        }

        return statuses;
    }
}