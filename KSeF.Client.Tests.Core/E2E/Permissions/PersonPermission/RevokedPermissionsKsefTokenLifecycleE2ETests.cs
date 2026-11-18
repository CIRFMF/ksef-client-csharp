#nullable enable
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermission;

[Collection("OnlineSessionScenario")]
public class RevokedPermissionsKsefTokenLifecycleE2ETests : TestBase
{
	private const string GrantDescription = "E2E grant InvoiceWrite+InvoiceRead for KSeF token lifecycle test";
	private const int SuccessfulAuthStatusCode = 200;
	private const int TokenActivationTimeoutSeconds = 60;

	/// <summary>
	/// Właściciel nadaje pracownikowi uprawnienia i odbiera je dopiero po tym, jak pracownik wygenerował
	/// sobie token KSeF i uwierzytelnił się nim, uzyskując parę access/refresh token.
	///
	/// Oczekiwane zachowanie:
	/// - dopóki wcześniej wydany access token jest ważny, pracownik może nadal wykonywać operacje
	///   wymagające odebranych uprawnień (uprawnienia są "zamrożone" w już wydanym tokenie),
	/// - odświeżenie access tokena (refresh token) zwraca token bez uprawnień - kolejna próba operacji kończy się błędem,
	/// - ponowne uwierzytelnienie tym samym tokenem KSeF od zera kończy się błędem już na etapie uwierzytelnienia,
	///   ponieważ token nie ma już żadnych aktywnych uprawnień.
	/// </summary>
	[Fact]
	public async Task RevokedPermissions_OldAccessTokenStillWorks_ButRefreshAndReauthenticationLoseAccess()
	{
		string ownerNip = MiscellaneousUtils.GetRandomNip();
		string employeeNip = MiscellaneousUtils.GetRandomNip();

		// Właściciel uwierzytelnia się i nadaje pracownikowi InvoiceWrite oraz InvoiceRead
		AuthenticationOperationStatusResponse ownerAuth =
			await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, ownerNip);
		string ownerAccessToken = ownerAuth.AccessToken.Token;

		Client.Core.Models.OperationResponse grantToEmployee = await PermissionsUtils.GrantPersonPermissionsAsync(
			KsefClient,
			ownerAccessToken,
			new GrantPermissionsPersonSubjectIdentifier
			{
				Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
				Value = employeeNip
			},
			[PersonPermissionType.InvoiceWrite, PersonPermissionType.InvoiceRead],
			new PersonPermissionSubjectDetails
			{
				SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
				{
					FirstName = "Jan",
					LastName = "Pracownik"
				}
			},
			GrantDescription);

		PermissionsOperationStatusResponse grantToEmployeeStatus = 
			await PermissionsUtils.WaitForOperationCompletionAsync(KsefClient, grantToEmployee.ReferenceNumber, ownerAccessToken);

		Assert.Equal(OperationStatusCodeResponse.Success, grantToEmployeeStatus.Status.Code);

		// Pracownik uwierzytelnia się (certyfikatem) w kontekście właściciela - pierwsze logowanie,
		// potrzebne wyłącznie po to, by móc wygenerować sobie własny token KSeF
		AuthenticationOperationStatusResponse employeeCertAuth =
			await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, employeeNip, ownerNip);
		string employeeCertAccessToken = employeeCertAuth.AccessToken.Token;

		// Pracownik generuje sobie token KSeF w kontekście właściciela i czeka na jego aktywację
		KsefTokenRequest generateTokenRequest = new()
		{
			Permissions = [KsefTokenPermissionType.InvoiceWrite, KsefTokenPermissionType.InvoiceRead],
			Description = "E2E employee KSeF token"
		};

		KsefTokenResponse ksefTokenResponse = await KsefClient.GenerateKsefTokenAsync(
			generateTokenRequest, employeeCertAccessToken, CancellationToken);
		Assert.False(string.IsNullOrWhiteSpace(ksefTokenResponse.Token));

		int activationAttempts = Math.Max(1, (TokenActivationTimeoutSeconds * 1000) / SleepTime);
		AuthenticationKsefToken activeKsefToken = await AsyncPollingUtils.PollAsync(
			() => KsefClient.GetKsefTokenAsync(ksefTokenResponse.ReferenceNumber, employeeCertAccessToken, CancellationToken),
			result => result is not null && result.Status == AuthenticationKsefTokenStatus.Active,
			maxAttempts: activationAttempts,
			cancellationToken: CancellationToken);
		Assert.Equal(AuthenticationKsefTokenStatus.Active, activeKsefToken.Status);

		// Pracownik uwierzytelnia się tokenem KSeF w kontekście właściciela - to ten dostęp będzie testowany
		AuthenticationOperationStatusResponse employeeTokenAuth =
			await AuthenticateWithKsefTokenAsync(ksefTokenResponse.Token, ownerNip);
		string employeeAccessToken = employeeTokenAuth.AccessToken.Token;
		string employeeRefreshToken = employeeTokenAuth.RefreshToken.Token;

		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		// Sanity check - dzięki nadanym uprawnieniom pracownik może otworzyć sesję interaktywną
		OpenOnlineSessionResponse sessionBeforeRevoke =
			await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient, encryptionData, employeeAccessToken);
		Assert.False(string.IsNullOrWhiteSpace(sessionBeforeRevoke.ReferenceNumber));

		// Właściciel odbiera pracownikowi wszystkie nadane uprawnienia
		await RevokeAllGrantedPermissionsAsync(ownerAccessToken);

		// Mimo odebrania uprawnień, dotychczas wydany access token nadal działa (uprawnienia "zamrożone" w tokenie)
		OpenOnlineSessionResponse sessionWithStillValidToken =
			await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient, encryptionData, employeeAccessToken);
		Assert.False(string.IsNullOrWhiteSpace(sessionWithStillValidToken.ReferenceNumber));

		// Odświeżenie access tokena zwraca token, który nie ma już nadanych uprawnień
		RefreshTokenResponse refreshedToken =
			await AuthorizationClient.RefreshAccessTokenAsync(employeeRefreshToken, CancellationToken);
		Assert.NotNull(refreshedToken.AccessToken);

		await Assert.ThrowsAsync<KsefApiException>(async () =>
		{
			await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient, encryptionData, refreshedToken.AccessToken.Token).ConfigureAwait(false);
		});

		// Ponowne uwierzytelnienie od zera tym samym tokenem KSeF nie powiedzie się -
		// token nie ma już żadnych aktywnych uprawnień, więc uwierzytelnienie kończy się błędem już na tym etapie.
		await Assert.ThrowsAsync<KsefApiException>(async () =>
		{
			await AuthenticateWithKsefTokenAsync(ksefTokenResponse.Token, ownerNip).ConfigureAwait(false);
		});
	}

	/// <summary>
	/// Wyszukuje uprawnienia nadane pracownikowi (po opisie grantu) w bieżącym kontekście i odwołuje każde z nich.
	/// </summary>
	private async Task RevokeAllGrantedPermissionsAsync(string ownerAccessToken)
	{
		PersonPermissionsQueryRequest query = new()
		{
			PermissionTypes = [PersonPermissionType.InvoiceWrite, PersonPermissionType.InvoiceRead]
		};

		PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> grantedPermissions =
			await AsyncPollingUtils.PollAsync(
				() => KsefClient.SearchGrantedPersonPermissionsAsync(query, ownerAccessToken, pageOffset: 0, pageSize: 50, CancellationToken),
				result => result?.Permissions is not null && result.Permissions.Any(p => p.Description == GrantDescription),
				description: "Czekam na widoczność nadanych uprawnień pracownika",
				delay: TimeSpan.FromMilliseconds(SleepTime),
				maxAttempts: 60,
				cancellationToken: CancellationToken).ConfigureAwait(false);

		List<Client.Core.Models.Permissions.PersonPermission> toRevoke =
			[.. grantedPermissions.Permissions.Where(p => p.Description == GrantDescription)];
		Assert.NotEmpty(toRevoke);

		foreach (Client.Core.Models.Permissions.PersonPermission permission in toRevoke)
		{
			Client.Core.Models.OperationResponse revoke =
				await KsefClient.RevokeCommonPermissionAsync(permission.Id, ownerAccessToken, CancellationToken).ConfigureAwait(false);

			await AsyncPollingUtils.PollAsync(
				() => KsefClient.OperationsStatusAsync(revoke.ReferenceNumber, ownerAccessToken),
				result => result?.Status?.Code == 200,
				description: "Czekam na potwierdzenie odebrania uprawnienia",
				delay: TimeSpan.FromMilliseconds(SleepTime),
				maxAttempts: 60,
				cancellationToken: CancellationToken).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Przeprowadza pełny proces uwierzytelnienia przy użyciu tokena KSeF w podanym kontekście (NIP).
	/// </summary>
	private async Task<AuthenticationOperationStatusResponse> AuthenticateWithKsefTokenAsync(string ksefToken, string contextNip)
	{
		AuthenticationChallengeResponse challenge = await AuthorizationClient.GetAuthChallengeAsync(CancellationToken).ConfigureAwait(false);
		long timestampMs = challenge.Timestamp.ToUnixTimeMilliseconds();

		string tokenWithTimestamp = $"{ksefToken}|{timestampMs}";
		byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenWithTimestamp);
		byte[] encrypted = CryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes);
		string encryptedTokenB64 = Convert.ToBase64String(encrypted);

		AuthenticationKsefTokenRequest request = new()
		{
			Challenge = challenge.Challenge,
			ContextIdentifier = new AuthenticationTokenContextIdentifier
			{
				Type = AuthenticationTokenContextIdentifierType.Nip,
				Value = contextNip
			},
			EncryptedToken = encryptedTokenB64,
			AuthorizationPolicy = null
		};

		SignatureResponse signature = await AuthorizationClient.SubmitKsefTokenAuthRequestAsync(request, CancellationToken).ConfigureAwait(false);

		TimeSpan pollTimeout = TimeSpan.FromMinutes(2);
		int statusAttempts = Math.Max(1, (int)(pollTimeout.TotalMilliseconds / SleepTime));

		AuthStatus status = await AsyncPollingUtils.PollAsync(
			() => AuthorizationClient.GetAuthStatusAsync(signature.ReferenceNumber, signature.AuthenticationToken.Token, CancellationToken),
			result => result is not null && result.Status?.Code != 100,
			maxAttempts: statusAttempts,
			cancellationToken: CancellationToken).ConfigureAwait(false);

		if (status.Status.Code != SuccessfulAuthStatusCode)
		{
			throw new KsefApiException(
				$"Uwierzytelnienie tokenem KSeF zakończyło się niepowodzeniem: {string.Join("; ", status.Status.Details ?? [])}",
				System.Net.HttpStatusCode.Forbidden);
		}

		return await AuthorizationClient.GetAccessTokenAsync(signature.AuthenticationToken.Token, CancellationToken).ConfigureAwait(false);
	}
}
