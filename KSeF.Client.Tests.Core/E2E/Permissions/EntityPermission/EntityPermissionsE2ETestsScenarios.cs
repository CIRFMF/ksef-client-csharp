using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;
namespace KSeF.Client.Tests.Core.E2E.Permissions.EntityPermission;


public class EntityPermissionsE2ETestsScenarios : TestBase
{
    private const int ExpectedPermissionsCount = 2;

    /// <summary>
    /// Potwierdza że grantor widzi uprawnienia które nadał innemu podmiotowi.
    /// </summary>
    [Fact]
    public async Task GrantPermissions_E2E_ShouldReturnPermissionsGrantedByGrantor()
    {
        // Arrange: NIP-y i Subjecty
        string contextNip = MiscellaneousUtils.GetRandomNip();
        string subjectNip = MiscellaneousUtils.GetRandomNip();

        GrantPermissionsEntitySubjectIdentifier brSubject =
            new()
            {
                Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                Value = subjectNip
            };

        // Auth
        AuthenticationOperationStatusResponse authorizationInfo =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextNip);

        Client.Core.Models.Permissions.Entity.EntityPermission[] permissions =
        [
            Client.Core.Models.Permissions.Entity.EntityPermission.New(
                EntityStandardPermissionType.InvoiceRead, true),
            Client.Core.Models.Permissions.Entity.EntityPermission.New(
                EntityStandardPermissionType.InvoiceWrite, false)
        ];

        (OperationResponse grantsPermissionsResponse, PermissionsEntitySubjectDetails subjectDetails) =
            await GrantPermissionsAsync(
                brSubject,
                authorizationInfo,
                permissions);

        Assert.NotNull(grantsPermissionsResponse);
        Assert.False(string.IsNullOrWhiteSpace(grantsPermissionsResponse.ReferenceNumber));

        PersonPermissionsQueryRequest queryForAllPermissions =
            new()
            {
                QueryType = PersonQueryType.PermissionsGrantedInCurrentContext
            };

        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission>
            queryForAllPermissionsResponse =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonPermissionsAsync(
                    queryForAllPermissions,
                    authorizationInfo.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count == ExpectedPermissionsCount,
                cancellationToken: CancellationToken);

        Assert.NotNull(queryForAllPermissionsResponse);
        Assert.NotEmpty(queryForAllPermissionsResponse.Permissions);
        Assert.Equal(ExpectedPermissionsCount, queryForAllPermissionsResponse.Permissions.Count);
        Assert.True(queryForAllPermissionsResponse.Permissions.All(p => p.SubjectEntityDetails.FullName == subjectDetails.FullName));
    }

    /// <summary>
    /// Potwierdza że podmiot któremu nadano uprawnienia widzi je w swoim kontekście
    /// (<c>Permissions</c>), a jednocześnie - jako właściciel własnego kontekstu NIP -
    /// widzi swoje uprawnienia właścicielskie (<c>OwnerPermissions</c>).
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GrantPermissions_E2E_ShouldReturnPersonalPermissions()
    {
        // Arrange + Grants
        string contextNip = MiscellaneousUtils.GetRandomNip();
        string subjectNip = MiscellaneousUtils.GetRandomNip();

        GrantPermissionsEntitySubjectIdentifier subject =
            new()
            {
                Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                Value = subjectNip
            };

        AuthenticationOperationStatusResponse authorizationInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextNip);

        Client.Core.Models.Permissions.Entity.EntityPermission[] permissions =
        [
            Client.Core.Models.Permissions.Entity.EntityPermission.New(
                EntityStandardPermissionType.InvoiceRead, true),
            Client.Core.Models.Permissions.Entity.EntityPermission.New(
                EntityStandardPermissionType.InvoiceWrite, true)
        ];

        (OperationResponse grantPermissionsEntityResponse, PermissionsEntitySubjectDetails subjectDetails) = await GrantPermissionsAsync(
            subject,
            authorizationInfo,
            permissions);

        Assert.NotNull(grantPermissionsEntityResponse);
        Assert.False(string.IsNullOrWhiteSpace(grantPermissionsEntityResponse.ReferenceNumber));

        // Auth: Entity we własnym kontekście
        AuthenticationOperationStatusResponse entityAuthorizationInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, subjectNip);

        PersonalPermissionsQueryRequest queryForAllPermissions = new();
        PersonalPermissionsQueryResponse queryForAllPermissionsResponse =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    queryForAllPermissions, entityAuthorizationInfo.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count == ExpectedPermissionsCount,
                cancellationToken: CancellationToken);

        Assert.NotNull(queryForAllPermissionsResponse);
        Assert.NotEmpty(queryForAllPermissionsResponse.Permissions);
        Assert.Equal(ExpectedPermissionsCount, queryForAllPermissionsResponse.Permissions.Count);

        // entityAuthorizationInfo uwierzytelnia subjectNip jako właściciela jego własnego kontekstu NIP,
        // więc odpowiedź powinna jednocześnie zawierać uprawnienia nadane przez inny podmiot (Permissions)
        // oraz uprawnienia właścicielskie (OwnerPermissions).
        Assert.NotNull(queryForAllPermissionsResponse.OwnerPermissions);
        Assert.NotEmpty(queryForAllPermissionsResponse.OwnerPermissions);

        Assert.Contains(queryForAllPermissionsResponse.Permissions,
			x => x.PermissionScope == PersonalPermission.PersonalPermissionScopeType.InvoiceWrite);
		Assert.Contains(queryForAllPermissionsResponse.Permissions,
			x => x.PermissionScope == PersonalPermission.PersonalPermissionScopeType.InvoiceRead);

		Assert.All(queryForAllPermissionsResponse.Permissions, permission =>
		{
			Assert.False(string.IsNullOrEmpty(permission.Id));
			Assert.False(string.IsNullOrEmpty(permission.Description));
			Assert.Equal(PersonalPermission.PersonalPermissionState.Active, permission.PermissionState);
			Assert.NotEqual(default, permission.StartDate);
		});

		List<PersonalPermission> permissionsGrantedByEntity = queryForAllPermissionsResponse.Permissions
            .Where(p => p.ContextIdentifier.Value == contextNip)
            .ToList();
        Assert.Equal(ExpectedPermissionsCount, permissionsGrantedByEntity.Count);

        // Weryfikacja SubjectDetails
        Assert.True(permissionsGrantedByEntity.All(p =>
            p.SubjectEntityDetails is not null &&
            p.SubjectEntityDetails.FullName == subjectDetails.FullName));
    }

    /// <summary>
    /// Potwierdza że podmiot któremu nadano uprawnienia widzi je w swoim kontekście, 
    /// a nie widzi uprawnień nadanych przez inny podmiot.
    /// </summary>
    [Fact]
    public async Task GrantPermissions_E2E_ShouldReturnPermissionsSearchedBySubjectInEntityContext()
    {
        // Arrange
        string jdgNip = MiscellaneousUtils.GetRandomNip(); // jdg
        string otherJdgNip = MiscellaneousUtils.GetRandomNip(); // inna jdg
        string brNip = MiscellaneousUtils.GetRandomNip(); // biuro rachunkowe
        string kdpNip = MiscellaneousUtils.GetRandomNip(); // kancelaria doradztwa podatkowego

        GrantPermissionsEntitySubjectIdentifier brSubject =
            new()
            {
                Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                Value = brNip
            };

        GrantPermissionsEntitySubjectIdentifier kdpSubject =
            new()
            {
                Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                Value = kdpNip
            };

        Client.Core.Models.Permissions.Entity.EntityPermission[] permissions =
        [
            Client.Core.Models.Permissions.Entity.EntityPermission.New(
                EntityStandardPermissionType.InvoiceRead, true),
            Client.Core.Models.Permissions.Entity.EntityPermission.New(
                EntityStandardPermissionType.InvoiceWrite, false)
        ];

        // Act
        // uwierzytelnienie jdg we własnym kontekście
        AuthenticationOperationStatusResponse authorizationInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, jdgNip);
        // nadanie uprawnień biuru rachunkowemu
        (OperationResponse brGrantInJdg, PermissionsEntitySubjectDetails brGrantSubjectDetails) = await GrantPermissionsAsync(brSubject, authorizationInfo, permissions);
        Assert.NotNull(brGrantInJdg);
        Assert.False(string.IsNullOrWhiteSpace(brGrantInJdg.ReferenceNumber));
        // nadanie uprawnień kancelarii doradztwa podatkowego
        (OperationResponse kdpGrantInJdg, PermissionsEntitySubjectDetails kdpSubjectDetails) = await GrantPermissionsAsync(kdpSubject, authorizationInfo, permissions);
        Assert.NotNull(kdpGrantInJdg);
        Assert.False(string.IsNullOrWhiteSpace(kdpGrantInJdg.ReferenceNumber));

        // uwierzytelnienie otherJdg we własnym kontekście
        AuthenticationOperationStatusResponse otherJdgAuthorizationInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, otherJdgNip);
        // nadanie uprawnień biuru rachunkowemu
        (OperationResponse brGrantInOtherJdg, PermissionsEntitySubjectDetails brGrantSubjectDetailsInOtherJdg) = await GrantPermissionsAsync(brSubject, otherJdgAuthorizationInfo, permissions);
        Assert.NotNull(brGrantInOtherJdg);
        Assert.False(string.IsNullOrWhiteSpace(brGrantInOtherJdg.ReferenceNumber));
        // nadanie uprawnień kancelarii doradztwa podatkowego
        (OperationResponse kdpGrantInOtherJdg, PermissionsEntitySubjectDetails kdpGrantSubjectDetails) = await GrantPermissionsAsync(kdpSubject, otherJdgAuthorizationInfo, permissions);
        Assert.NotNull(kdpGrantInOtherJdg);
        Assert.False(string.IsNullOrWhiteSpace(kdpGrantInOtherJdg.ReferenceNumber));

        // w tym momencie:
        // biuro rachunkowe ma uprawnienia w kontekście jdg i otherJdg (razem 4 uprawnienia)
        // kancelaria doradztwa podatkowego ma uprawnienia w kontekście jdg i otherJdg (razem 4 uprawnienia)

        // Assert
        // uwierzytelnienie: biuro rachunkowe w kontekście jdg
        X509Certificate2 personalCertificate = CertificateUtils.GetPersonalCertificate(
            givenName: "Jan",
            surname: "Kowalski",
            serialNumberPrefix: "TINPL",
            serialNumber: brNip,
            commonName: "Jan Kowalski Certificate");

        AuthenticationOperationStatusResponse entityAuthorizationInfo = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            jdgNip,
            AuthenticationTokenContextIdentifierType.Nip,
            personalCertificate);

        PersonalPermissionsQueryRequest queryForContextPermissions =
            new();

        // uprawnienia biura rachunkowego w kontekście jdg
        PersonalPermissionsQueryResponse
            queryForContextPermissionsResponse =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    queryForContextPermissions,
                    entityAuthorizationInfo.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count == ExpectedPermissionsCount,
                cancellationToken: CancellationToken);

        Assert.NotNull(queryForContextPermissionsResponse);
        Assert.Equal(ExpectedPermissionsCount, queryForContextPermissionsResponse.Permissions.Count); // biuro rachunkowe ma 2 uprawnienia w kontekście jdg

        Assert.True(queryForContextPermissionsResponse.Permissions.All(p => p.SubjectEntityDetails.FullName == brGrantSubjectDetails.FullName));
    }

    private async Task<(OperationResponse, PermissionsEntitySubjectDetails)> GrantPermissionsAsync(
            GrantPermissionsEntitySubjectIdentifier subject,
            AuthenticationOperationStatusResponse authorizationInfo,
            Client.Core.Models.Permissions.Entity.EntityPermission[] permissions)
    {
        PermissionsEntitySubjectDetails subjectDetails = new()
        {
            FullName = $"Podmiot {subject.Value}"
        };
        GrantPermissionsEntityRequest grantEntityPermissionsRequest = GrantEntityPermissionsRequestBuilder
                    .Create()
                    .WithSubject(subject)
                    .WithPermissions(permissions)
                    .WithDescription("Uprawnienia do odczytu i wystawiania faktur")
                    .WithSubjectDetails(subjectDetails)
                    .Build();

        OperationResponse grantEntityPermissionsResponse = await KsefClient.GrantsPermissionEntityAsync(
            grantEntityPermissionsRequest, authorizationInfo.AccessToken.Token, CancellationToken).ConfigureAwait(false);

        return (grantEntityPermissionsResponse, subjectDetails);
    }
}