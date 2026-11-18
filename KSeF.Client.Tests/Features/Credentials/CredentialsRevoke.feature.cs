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
        // Przygotowanie: właściciel, delegat (oba NIP) oraz osoba fizyczna (PESEL)
        string nipOwner = MiscellaneousUtils.GetRandomNip();
        string nipDelegate = MiscellaneousUtils.GetRandomNip();
        string pesel = MiscellaneousUtils.GetRandomPesel();

        string ownerToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nipOwner)).AccessToken.Token;
        string delegateToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nipDelegate)).AccessToken.Token;

        // 1) Właściciel nadaje delegatowi uprawnienie CredentialsManage
        bool manageGranted = await GrantCredentialsManageToDelegateAsync(KsefClient, ownerToken, nipDelegate);
        Assert.True(manageGranted);

        // 2) Uprawnienie delegata jest widoczne jako aktywne u właściciela
        IReadOnlyList<PersonPermission> delegatePermissions =
            await SearchPersonPermissionsAsync(KsefClient, ownerToken, PersonPermissionState.Active);
        Assert.Single(delegatePermissions);

        // 3) Delegat nadaje osobie z PESEL-em uprawnienie InvoiceWrite w kontekście właściciela
        bool invoiceWriteGranted = await GrantInvoiceWriteToPeselAsManagerAsync(KsefClient, delegateToken, nipOwner, pesel);
        Assert.True(invoiceWriteGranted);

        // Świeżo nadane uprawnienie widoczne jest u delegata w stanie Inactive
        IReadOnlyList<PersonPermission> peselPermissionsAfterGrant =
            await SearchPersonPermissionsAsync(KsefClient, delegateToken, PersonPermissionState.Inactive);
        PersonPermission grantedPermission = Assert.Single(peselPermissionsAfterGrant);

        // 4) Delegat odbiera nadane uprawnienie
        bool revokeSuccessful = await RevokePersonPermissionAsync(KsefClient, delegateToken, grantedPermission.Id);
        Assert.True(revokeSuccessful);

        // 5) Po odebraniu nie zostaje żadne aktywne uprawnienie
        IReadOnlyList<PersonPermission> activePermissionsAfterRevoke =
            await SearchPersonPermissionsAsync(KsefClient, delegateToken, PersonPermissionState.Active);
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

		OperationResponse operationResponse = 
			await PermissionsUtils.GrantPersonPermissionsAsync(client, ownerToken, subjectIdentifier, permissions, subjectDetails)
			.ConfigureAwait(false);

		PermissionsOperationStatusResponse status = 
			await PermissionsUtils.WaitForOperationCompletionAsync(client, operationResponse.ReferenceNumber, ownerToken)
			.ConfigureAwait(false);

		return status.Status.Code == OperationStatusCodeResponse.Success;
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

		PermissionsOperationStatusResponse status = await PermissionsUtils.WaitForOperationCompletionAsync(client, operationResponse.ReferenceNumber, token).ConfigureAwait(false);
		return status.Status.Code == OperationStatusCodeResponse.Success;
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

		PermissionsOperationStatusResponse status = await PermissionsUtils.WaitForOperationCompletionAsync(client, operationResponse.ReferenceNumber, delegateToken).ConfigureAwait(false);
		return status.Status.Code == OperationStatusCodeResponse.Success;
	}
}
