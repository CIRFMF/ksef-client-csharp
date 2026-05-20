using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Token;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features.Credentials
{
    [Collection("CredentialsGrantScenario")]
    [Trait("Category", "Features")]
    [Trait("Features", "credentials_grant.feature")]
    public class CredentialsGrantTests : KsefIntegrationTestBase
    {
		[Theory]
		[InlineData("90091309123",
		GrantPermissionsPersonSubjectIdentifierType.Pesel,
		new[]
	    {
		    PersonPermissionType.InvoiceWrite,
		    PersonPermissionType.InvoiceRead,
		    PersonPermissionType.Introspection,
		    PersonPermissionType.CredentialsRead,
		    PersonPermissionType.CredentialsManage
	    })]
		[InlineData("6651887777",
		GrantPermissionsPersonSubjectIdentifierType.Nip, 
        new[]
	    {
		    PersonPermissionType.InvoiceWrite,
		    PersonPermissionType.InvoiceRead,
		    PersonPermissionType.Introspection,
		    PersonPermissionType.CredentialsRead,
		    PersonPermissionType.CredentialsManage
	    })]
		[Trait("Scenario", "Nadanie uprawnienia wystawianie faktur")]
        public async Task GivenOwnerIsAuthenticated_WhenGrantPermissionsToEntity_ThenPermissionsAreActiveAndReflectedInToken(
            string identifier,
			GrantPermissionsPersonSubjectIdentifierType identifierType,
			PersonPermissionType[] permissions)
        {
			// Arrange
			string ownerNip = MiscellaneousUtils.GetRandomNip();
			string ownerAuthToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, ownerNip)).AccessToken.Token;

			GrantPermissionsPersonSubjectIdentifier subjectIdentifier = new()
			{
				Type = identifierType,
				Value = identifier
			};

			PersonPermissionSubjectDetails subjectDetails = new PersonPermissionSubjectDetails
			{
				SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
				{
					FirstName = "Anna",
					LastName = "Testowa"
				}
			};

            // Nadanie uprawnień przez właściciela
			OperationResponse grantPermissionsResponse = await PermissionsUtils.GrantPersonPermissionsAsync(KsefClient,
                    ownerAuthToken,
                    subjectIdentifier,
                    permissions, 
                    subjectDetails, 
                    "CredentialsGrantTests"
            );

			PermissionsOperationStatusResponse grantStatus = await AsyncPollingUtils.PollAsync(
				async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, grantPermissionsResponse.ReferenceNumber, ownerAuthToken).ConfigureAwait(false),
				status => status?.Status?.Code == 200,
				delay: TimeSpan.FromSeconds(2),
				maxAttempts: 30,
				cancellationToken: CancellationToken.None);

			// Liczba aktywnych uprawnień odpowiada liczbie nadanych
			IReadOnlyList <PersonPermission> activePermissions = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, ownerAuthToken, PersonPermissionState.Active);
			IEnumerable<PersonPermissionType> activePermissionTypes = activePermissions.Select(p => p.PermissionScope);
			Assert.Equal(
				permissions.OrderBy(x => x),
				activePermissionTypes.OrderBy(x => x));

			// Token uwierzytelnionego podmiotu zawiera nadane uprawnienia
			AuthenticationOperationStatusResponse authContext = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, identifier, ownerNip);
            Assert.NotNull(authContext);
            PersonToken personToken = TokenService.MapFromJwt(authContext.AccessToken.Token);
            Assert.True(personToken.Permissions.Length == permissions.Length);

            foreach (PersonPermission item in activePermissions)
            {
                OperationResponse revokeSuccessful = await PermissionsUtils.RevokePersonPermissionAsync(KsefClient, ownerAuthToken, item.Id);
                Assert.NotNull(revokeSuccessful);

                PermissionsOperationStatusResponse revokePermissionsActionStatus = await AsyncPollingUtils.PollAsync(
                    async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, revokeSuccessful.ReferenceNumber, ownerAuthToken).ConfigureAwait(false),
                    status => status is not null &&
                             status.Status is not null &&
                             status.Status.Code == 200,
                    delay: TimeSpan.FromSeconds(2),
                    maxAttempts: 30,
                    cancellationToken: CancellationToken.None);
            }

			// Po cofnięciu nie ma żadnych aktywnych uprawnień
			IReadOnlyList<PersonPermission> permissionsAfterRevoke = await PermissionsUtils.SearchPersonPermissionsAsync(
				KsefClient, ownerAuthToken, PersonPermissionState.Active);

			Assert.Empty(permissionsAfterRevoke);
		}

		[Theory]
		[InlineData("90091309123",
		GrantPermissionsPersonSubjectIdentifierType.Pesel,
		new[]
		{
			PersonPermissionType.InvoiceRead,
			PersonPermissionType.InvoiceWrite,
			PersonPermissionType.CredentialsManage,
			PersonPermissionType.CredentialsRead,
			PersonPermissionType.Introspection,
			PersonPermissionType.SubunitManage
		})]
		[InlineData("6651887777",
		GrantPermissionsPersonSubjectIdentifierType.Nip,
		new[]
		{
			PersonPermissionType.InvoiceWrite,
			PersonPermissionType.InvoiceRead,
			PersonPermissionType.Introspection,
			PersonPermissionType.CredentialsRead,
			PersonPermissionType.CredentialsManage
		})]
		[Trait("Scenario", "Nadanie uprawnień przez osobę z uprawnieniem do zarządzania uprawnieniami")]
		public async Task GivenDelegateHasCredentialsManage_WhenGrantsPermissionsToEntity_ThenPermissionsAreActiveAndReflectedInToken(
			string identifier,
			GrantPermissionsPersonSubjectIdentifierType identifierType,
			PersonPermissionType[] permissions)
		{
			// Arrange
			string ownerNip = MiscellaneousUtils.GetRandomNip();
			string ownerAuthToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, ownerNip)).AccessToken.Token;

			string delegateNip = MiscellaneousUtils.GetRandomNip();

			// Właściciel nadaje delegatowi wyłącznie uprawnienie CredentialsManage
			GrantPermissionsPersonSubjectIdentifier delegateIdentifier = new()
			{
				Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
				Value = delegateNip
			};

			PersonPermissionSubjectDetails delegateDetails = new()
			{
				SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
				{
					FirstName = "Anna",
					LastName = "Testowa"
				}
			};

			OperationResponse delegateGrantResponse = await PermissionsUtils.GrantPersonPermissionsAsync(
				KsefClient, ownerAuthToken, delegateIdentifier,
				new[] { PersonPermissionType.CredentialsManage },
				delegateDetails);

			await AsyncPollingUtils.PollAsync(
				async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, delegateGrantResponse.ReferenceNumber, ownerAuthToken).ConfigureAwait(false),
				status => status?.Status?.Code == 200,
				delay: TimeSpan.FromMilliseconds(SleepTime),
				maxAttempts: 60,
				cancellationToken: CancellationToken.None);

			// Delegat uwierzytelnia się w kontekście właściciela i nadaje uprawnienia docelowemu podmiotowi
			string delegateAuthToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, delegateNip, ownerNip)).AccessToken.Token;

			GrantPermissionsPersonSubjectIdentifier targetIdentifier = new()
			{
				Type = identifierType,
				Value = identifier
			};

			PersonPermissionSubjectDetails targetDetails = new()
			{
				SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
				{
					FirstName = "Jan",
					LastName = "Testowy"
				}
			};

			OperationResponse targetGrantResponse = await PermissionsUtils.GrantPersonPermissionsAsync(
				KsefClient, delegateAuthToken, targetIdentifier, permissions, targetDetails, "CredentialsGrantTests");

			await AsyncPollingUtils.PollAsync(
				async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, targetGrantResponse.ReferenceNumber, delegateAuthToken).ConfigureAwait(false),
				status => status?.Status?.Code == 200,
				delay: TimeSpan.FromSeconds(5),
				maxAttempts: 60,
				cancellationToken: CancellationToken.None);

			// Aktywne uprawnienia docelowego podmiotu odpowiadają nadanym
			IReadOnlyList<PersonPermission> activePermissions = await PermissionsUtils.SearchPersonPermissionsAsync(
				KsefClient, delegateAuthToken, PersonPermissionState.Active);

			IEnumerable<PersonPermissionType> activePermissionTypes = activePermissions
				.Where(p => p.AuthorizedIdentifier.Value == identifier)
				.Select(p => p.PermissionScope);

			Assert.Equal(
				permissions.OrderBy(x => x),
				activePermissionTypes.OrderBy(x => x));

			// Token docelowego podmiotu zawiera nadane uprawnienia
			AuthenticationOperationStatusResponse authContext = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, identifier, ownerNip);
			Assert.NotNull(authContext);

			PersonToken personToken = TokenService.MapFromJwt(authContext.AccessToken.Token);
			Assert.Equal(permissions.Length, personToken.Permissions.Length);

			// Właściciel cofa wszystkie nadane uprawnienia
			foreach (PersonPermission permission in activePermissions)
			{
				OperationResponse revokeResponse = await PermissionsUtils.RevokePersonPermissionAsync(
					KsefClient, ownerAuthToken, permission.Id);

				Assert.NotNull(revokeResponse);

				await AsyncPollingUtils.PollAsync(
					async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, revokeResponse.ReferenceNumber, ownerAuthToken).ConfigureAwait(false),
					status => status?.Status?.Code == 200,
					delay: TimeSpan.FromMilliseconds(SleepTime),
					maxAttempts: 60,
					cancellationToken: CancellationToken.None);
			}

			// Po cofnięciu delegat nie widzi żadnych aktywnych uprawnień w kontekście właściciela
			IReadOnlyList<PersonPermission> permissionsAfterRevoke = await PermissionsUtils.SearchPersonPermissionsAsync(
				KsefClient, delegateAuthToken, PersonPermissionState.Active);

			Assert.Empty(permissionsAfterRevoke);
		}
	}
}