using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features;

[CollectionDefinition("RevokeCredentials.feature")]
[Trait("Category", "Features")]
[Trait("Features", "revoke_credentials.feature")]
public class CredentialsRevokeTests : KsefIntegrationTestBase
{
    [Fact]
    [Trait("Scenario", "Właściciel nadaje CredentialsManage delegatowi, delegat nadaje 'InvoiceWrite' PESEL-owi i następnie odbiera.")]
    public async Task DelegateGrantAndRevokeInvoiceWriteForPeselAsManagerLeavesNoActivePermission()
    {
        // Arrange
        string nipOwner = MiscellaneousUtils.GetRandomNip();
        string nipDelegate = MiscellaneousUtils.GetRandomNip();
        string pesel = MiscellaneousUtils.GetRandomPesel();

        string ownerToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nipOwner)).AccessToken.Token;
        string delegateToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nipDelegate)).AccessToken.Token;

        // Act
        // ========== Act: GRANT AS OWNER CredentialManage FOR DELEGATE ==========
        bool manageGranted = await GrantCredentialsManageToDelegateAsync(KsefClient, ownerToken, nipDelegate);
        Assert.True(manageGranted);

        // ========== Act: SEARCH CredentialManage FOR DELEGATE ==========
        IReadOnlyList<PersonPermission> delegatePermissions = await SearchPersonPermissionsAsync(KsefClient, ownerToken, PersonPermissionState.Active);
        PersonPermission delegatePermission = Assert.Single(delegatePermissions);

        // ========== Act: GRANT AS DELEGATE InvoiceWrite FOR PESEL ==========
        bool invoiceWriteGranted = await GrantInvoiceWriteToPeselAsManagerAsync(KsefClient, delegateToken, nipOwner, pesel);
        Assert.True(invoiceWriteGranted);

        IReadOnlyList<PersonPermission> peselPermissionsAfterGrant = await SearchPersonPermissionsAsync(KsefClient, delegateToken, PersonPermissionState.Inactive);
        PersonPermission grantedPermission = Assert.Single(peselPermissionsAfterGrant);

        // ========== Act: REVOKE AS DELEGATE InvoiceWrite FOR PESEL ==========
        bool revokeSuccessful = await RevokePersonPermissionAsync(KsefClient, delegateToken, grantedPermission.Id);
        Assert.True(revokeSuccessful);

        // Assert
        IReadOnlyList<PersonPermission> activePermissionsAfterRevoke = await SearchPersonPermissionsAsync(KsefClient, delegateToken, PersonPermissionState.Active);
        Assert.Empty(activePermissionsAfterRevoke);
    }

	/// <summary>
	/// Wyszukuje uprawnienia nadane osobom fizycznym w bieżącym kontekście, filtrowane po stanie uprawnienia.
	/// </summary>
	/// <param name="client">Klient KSeF używany do wywołań API.</param>
	/// <param name="token">Token dostępu używany do autoryzacji.</param>
	/// <param name="state">Stan uprawnienia do filtrowania (np. aktywne/nieaktywne).</param>
	/// <returns>Listę uprawnień osoby w formie tylko do odczytu.</returns>
	private async Task<IReadOnlyList<PersonPermission>> SearchPersonPermissionsAsync(
	IKSeFClient client, string token, PersonPermissionState state
		)
	=> await PermissionsUtils.SearchPersonPermissionsAsync(
		   client,
		   token,
		   PersonQueryType.PermissionsGrantedInCurrentContext,
		   state).ConfigureAwait(false);

	/// <summary>
	/// Nadaje uprawnienie CredentialsManage delegatowi zidentyfikowanemu przez NIP.
	/// </summary>
	/// <param name="client">Klient KSeF używany do wywołań API.</param>
	/// <param name="ownerToken">Token właściciela uprawnień (nadawcy).</param>
	/// <param name="delegateNip">NIP delegata, któremu zostanie nadane uprawnienie.</param>
	/// <returns>Prawda, jeśli operacja zakończyła się powodzeniem.</returns>
	private async Task<bool> GrantCredentialsManageToDelegateAsync(
		IKSeFClient client, string ownerToken, string delegateNip)
	{
		PersonPermissionSubjectDetails subjectDetails = new PersonPermissionSubjectDetails
		{
			SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
			PersonById = new PersonPermissionPersonById
			{
				FirstName = "Jan",
				LastName = "Testowy"
			}
		};

		GrantPermissionsPersonSubjectIdentifier subjectIdentifier = new() { Type = GrantPermissionsPersonSubjectIdentifierType.Nip, Value = delegateNip };
		PersonPermissionType[] permissions = [PersonPermissionType.CredentialsManage];

		OperationResponse operationResponse = await PermissionsUtils.GrantPersonPermissionsAsync(client, ownerToken, subjectIdentifier, permissions, subjectDetails).ConfigureAwait(false);

		return await ConfirmOperationSuccessAsync(client, operationResponse, ownerToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Odbiera (unieważnia) wskazane uprawnienie osoby po jego identyfikatorze.
	/// </summary>
	/// <param name="client">Klient KSeF używany do wywołań API.</param>
	/// <param name="token">Token dostępu używany do autoryzacji.</param>
	/// <param name="permissionId">Identyfikator uprawnienia do unieważnienia.</param>
	/// <returns>Prawda, jeśli operacja zakończyła się powodzeniem.</returns>
	private async Task<bool> RevokePersonPermissionAsync(
		IKSeFClient client, string token, string permissionId)
	{
		OperationResponse operationResponse = await PermissionsUtils.RevokePersonPermissionAsync(client, token, permissionId).ConfigureAwait(false);

		return await ConfirmOperationSuccessAsync(client, operationResponse, token).ConfigureAwait(false);
	}

	/// <summary>
	/// Nadaje uprawnienie InvoiceWrite osobie z PESEL-em w trybie pośrednim
	/// (subject: PESEL, target: NIP właściciela), korzystając z tokena delegata.
	/// </summary>
	/// <param name="client">Klient KSeF używany do wywołań API.</param>
	/// <param name="delegateToken">Token delegata posiadającego prawo nadawania uprawnień.</param>
	/// <param name="nipOwner">NIP właściciela (target), w którego kontekście nadawane jest uprawnienie.</param>
	/// <param name="pesel">PESEL osoby, której nadawane jest uprawnienie.</param>
	/// <returns>Prawda, jeśli operacja zakończyła się powodzeniem.</returns>
	private async Task<bool> GrantInvoiceWriteToPeselAsManagerAsync(
		IKSeFClient client, string delegateToken, string nipOwner, string pesel)
	{
		IndirectEntitySubjectIdentifier subjectIdentifier = new()
		{
			Type = IndirectEntitySubjectIdentifierType.Pesel,
			Value = pesel
		};

		IndirectEntityTargetIdentifier targetIdentifier = new()
		{
			Type = IndirectEntityTargetIdentifierType.Nip,
			Value = nipOwner
		};

		IndirectEntityStandardPermissionType[] permissions = [IndirectEntityStandardPermissionType.InvoiceWrite];

		OperationResponse operationResponse = await PermissionsUtils.GrantIndirectPermissionsAsync(client, delegateToken, subjectIdentifier, targetIdentifier, permissions).ConfigureAwait(false);

		return await ConfirmOperationSuccessAsync(client, operationResponse, delegateToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Pomocnicza metoda potwierdzająca powodzenie operacji nadawania/odbierania uprawnień.
	/// Czeka krótką chwilę, a następnie sprawdza status operacji po numerze referencyjnym.
	/// </summary>
	/// <param name="client">Klient KSeF używany do wywołań API.</param>
	/// <param name="operationResponse">Odpowiedź inicjująca operację (z numerem referencyjnym).</param>
	/// <param name="token">Token dostępu używany do autoryzacji odczytu statusu operacji.</param>
	/// <returns>Prawda, jeżeli status operacji zwróci kod 200.</returns>
	private async Task<bool> ConfirmOperationSuccessAsync(
		IKSeFClient client, OperationResponse operationResponse, string token)
	{
		if (string.IsNullOrWhiteSpace(operationResponse?.ReferenceNumber))
		{
			return false;
		}

		// Krótkie odczekanie, aby backend zdążył przetworzyć operację
		await Task.Delay(1000).ConfigureAwait(false);

		PermissionsOperationStatusResponse status = await PermissionsUtils.GetPermissionsOperationStatusAsync(client, operationResponse.ReferenceNumber!, token).ConfigureAwait(false);
		return status?.Status?.Code == OperationStatusCodeResponse.Success;
	}
}
