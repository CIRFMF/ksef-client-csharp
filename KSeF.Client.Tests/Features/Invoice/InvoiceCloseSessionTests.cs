#nullable enable
using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features.Invoice;

/// <summary>
/// Testy integracyjne weryfikujące scenariusze zamknięcia sesji online w systemie KSeF.
/// Sesję może zamknąć wyłącznie właściciel sesji lub właściciel kontekstu.
/// Każdy test obejmuje pełny cykl: otwarcie sesji → zamknięcie sesji → weryfikacja statusu sesji.
/// </summary>
public class InvoiceCloseSessionTests : KsefIntegrationTestBase
{
	private readonly string _ownerNip;
	private readonly string _ownerAccessToken;
	private readonly string _ownerRefreshToken;

	private const SystemCode DefaultSystemCode = SystemCode.FA3;
	private const string InvoiceTemplatePath = "invoice-template-fa-3.xml";
	/// <summary>
	/// Inicjalizacja wspólnych danych dla wszystkich testów: NIP właściciela oraz token autoryzacyjny.
	/// </summary>
	public InvoiceCloseSessionTests()
	{
		_ownerNip = MiscellaneousUtils.GetRandomNip();

		AuthenticationOperationStatusResponse ownerAuthInfo = AuthenticationUtils
			.AuthenticateAsync(AuthorizationClient, _ownerNip)
			.GetAwaiter().GetResult();

		_ownerAccessToken = ownerAuthInfo.AccessToken.Token;
		_ownerRefreshToken = ownerAuthInfo.RefreshToken.Token;
	}

	/// <summary>
	/// Weryfikacja możliwości zamknięcia sesji przy użyciu nowego tokenu dostępu uzyskanego po ponownym uwierzytelnieniu.
	/// Test sprawdza, że sesja otwarta przy użyciu jednego tokenu może zostać zamknięta tokenem z nowej sesji uwierzytelnienia.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Przygotowanie danych szyfrowania i otwarcie sesji online
	/// 2. Ponowne uwierzytelnienie właściciela i pobranie nowego tokenu dostępu
	/// 3. Zamknięcie sesji przy użyciu nowego tokenu
	/// 4. Weryfikacja statusu zamkniętej sesji
	/// </remarks>
	[Fact]
	public async Task CloseSession_WithNewAccessToken_SessionIsClosed()
	{
		// Krok 1: Przygotowanie danych szyfrowania, otwarcie sesji online oraz wysyłka faktury
		OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAndSendInvoiceAsync(_ownerAccessToken);

		Assert.False(string.IsNullOrEmpty(openSessionResponse.ReferenceNumber));

		// Krok 2: Ponowne uwierzytelnienie właściciela i pobranie nowego tokenu dostępu
		AuthenticationOperationStatusResponse ownerAuthInfo = await AuthenticationUtils
			.AuthenticateAsync(AuthorizationClient, _ownerNip).ConfigureAwait(false);

		string ownerAccessToken = ownerAuthInfo.AccessToken.Token;

		// Krok 3: Zamknięcie sesji przy użyciu nowego tokenu dostępu
		await KsefClient.CloseOnlineSessionAsync(openSessionResponse.ReferenceNumber, ownerAccessToken)
			.ConfigureAwait(false);

		// Krok 4: Weryfikacja statusu zamkniętej sesji
		SessionStatusResponse sessionStatus = await AsyncPollingUtils.PollAsync(
			async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
				KsefClient,
				openSessionResponse.ReferenceNumber,
				ownerAccessToken).ConfigureAwait(false),
			result => result is not null && result.Status.Code == OnlineSessionCodeResponse.ProcessedSuccessfully,
			cancellationToken: CancellationToken.None).ConfigureAwait(false);

		Assert.Equal(OnlineSessionCodeResponse.ProcessedSuccessfully, sessionStatus.Status.Code);
	}

	/// <summary>
	/// Weryfikacja możliwości zamknięcia sesji przy użyciu odświeżonego tokenu dostępu.
	/// Test sprawdza, że token uzyskany przez operację refresh może zostać użyty do zamknięcia aktywnej sesji.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Przygotowanie danych szyfrowania i otwarcie sesji online
	/// 2. Odświeżenie tokenu dostępu
	/// 3. Zamknięcie sesji przy użyciu odświeżonego tokenu
	/// 4. Weryfikacja statusu zamkniętej sesji
	/// </remarks>
	[Fact]
	public async Task CloseSession_WithRefreshedAccessToken_SessionIsClosed()
	{
		// Krok 1: Przygotowanie danych szyfrowania, otwarcie sesji online oraz wysyłka faktury
		OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAndSendInvoiceAsync(_ownerAccessToken);

		Assert.False(string.IsNullOrEmpty(openSessionResponse.ReferenceNumber));

		// Krok 2: Odświeżenie tokenu dostępu
		RefreshTokenResponse refreshTokenResponse = await KsefClient
			.RefreshAccessTokenAsync(_ownerRefreshToken)
			.ConfigureAwait(false);

		string refreshedOwnerAccessToken = refreshTokenResponse.AccessToken.Token;

		// Krok 3: Zamknięcie sesji przy użyciu odświeżonego tokenu
		await KsefClient.CloseOnlineSessionAsync(openSessionResponse.ReferenceNumber, refreshedOwnerAccessToken)
			.ConfigureAwait(false);

		// Krok 4: Weryfikacja statusu zamkniętej sesji
		SessionStatusResponse sessionStatus = await AsyncPollingUtils.PollAsync(
			async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
				KsefClient,
				openSessionResponse.ReferenceNumber,
				refreshedOwnerAccessToken).ConfigureAwait(false),
			result => result is not null && result.Status.Code == OnlineSessionCodeResponse.ProcessedSuccessfully,
			cancellationToken: CancellationToken.None).ConfigureAwait(false);

		Assert.Equal(OnlineSessionCodeResponse.ProcessedSuccessfully, sessionStatus.Status.Code);
	}

	/// <summary>
	/// Weryfikacja braku możliwości zamknięcia sesji przez podmiot z uprawnieniami InvoiceWrite
	/// Podmiot autoryzowany nie jest właścicielem sesji ani właścicielem kontekstu,
	/// dlatego próba zamknięcia sesji powinna zakończyć się błędem.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Otwarcie sesji online przez właściciela oraz wysyłka faktury
	/// 2. Nadanie uprawnień InvoiceWrite podmiotowi autoryzowanemu przez właściciela
	/// 3. Uwierzytelnienie podmiotu autoryzowanego w kontekście właściciela
	/// 4. Próba zamknięcia sesji przez podmiot autoryzowany — oczekiwany wyjątek KsefApiException
	/// </remarks>
	[Fact]
	public async Task CloseSession_ByAuthorizedSubjectInOwnerContext_ThrowsException()
	{
		// Krok 1: Otwarcie sesji online przez właściciela oraz wysyłka faktury
		OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAndSendInvoiceAsync(_ownerAccessToken);

		Assert.False(string.IsNullOrEmpty(openSessionResponse.ReferenceNumber));

		// Krok 2: Nadanie uprawnień InvoiceWrite podmiotowi autoryzowanemu przez właściciela
		string authorizedSubjectNip = MiscellaneousUtils.GetRandomNip();

		await GrantInvoiceWritePermissionAndWaitForConfirmationAsync(
			subjectNip: authorizedSubjectNip,
			firstName: "Anna",
			lastName: "Testowa",
			description: "Dostęp do wystawiania faktur dla podmiotu autoryzowanego");

		// Krok 3: Uwierzytelnienie podmiotu autoryzowanego w kontekście właściciela
		string authorizedSubjectAccessToken = await AuthenticateAndGetTokenAsync(authorizedSubjectNip, _ownerNip);

		// Krok 4: Próba zamknięcia sesji przez podmiot autoryzowany — podmiot nie jest właścicielem sesji
		// ani właścicielem kontekstu, dlatego operacja powinna zakończyć się błędem
		await Assert.ThrowsAsync<KsefApiException>(async () =>
			await KsefClient.CloseOnlineSessionAsync(openSessionResponse.ReferenceNumber, authorizedSubjectAccessToken)
				.ConfigureAwait(false));
	}

	/// <summary>
	/// Weryfikacja braku możliwości zamknięcia sesji przez drugi podmiot z uprawnieniami InvoiceWrite,
	/// który nie jest właścicielem sesji ani właścicielem kontekstu.
	/// Drugi podmiot nie jest właścicielem sesji ani właścicielem kontekstu, dlatego próba zamknięcia
	/// sesji otwartej przez pierwszy podmiot przez drugi podmiot powinna zakończyć się błędem.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Nadanie uprawnień InvoiceWrite pierwszemu podmiotowi przez właściciela
	/// 2. Nadanie uprawnień InvoiceWrite drugiemu podmiotowi przez właściciela
	/// 3. Uwierzytelnienie obu podmiotów w kontekście właściciela
	/// 4. Otwarcie sesji przez pierwszy podmiot oraz wysyłka faktury
	/// 5. Próba zamknięcia sesji przez drugi podmiot — oczekiwany wyjątek KsefApiException
	/// </remarks>
	[Fact]
	public async Task CloseSession_OpenedByOneAuthorizedSubject_ClosedByAnother_ThrowsException()
	{
		// Krok 1: Nadanie uprawnień InvoiceWrite pierwszemu podmiotowi przez właściciela
		string firstSubjectNip = MiscellaneousUtils.GetRandomNip();

		await GrantInvoiceWritePermissionAndWaitForConfirmationAsync(
			subjectNip: firstSubjectNip,
			firstName: "Anna",
			lastName: "Testowa",
			description: "Dostęp do wystawiania faktur dla pierwszego podmiotu autoryzowanego");

		// Krok 2: Nadanie uprawnień InvoiceWrite drugiemu podmiotowi przez właściciela
		string secondSubjectNip = MiscellaneousUtils.GetRandomNip();

		await GrantInvoiceWritePermissionAndWaitForConfirmationAsync(
			subjectNip: secondSubjectNip,
			firstName: "Jan",
			lastName: "Testowy",
			description: "Dostęp do wystawiania faktur dla drugiego podmiotu autoryzowanego");

		// Krok 3: Uwierzytelnienie obu podmiotów w kontekście właściciela
		string firstSubjectAccessToken = await AuthenticateAndGetTokenAsync(firstSubjectNip, _ownerNip);
		string secondSubjectAccessToken = await AuthenticateAndGetTokenAsync(secondSubjectNip, _ownerNip);

		// Krok 4: Otwarcie sesji przez pierwszy podmiot oraz wysyłka faktury
		OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAndSendInvoiceAsync(firstSubjectAccessToken);

		Assert.False(string.IsNullOrEmpty(openSessionResponse.ReferenceNumber));

		// Krok 5: Próba zamknięcia sesji przez drugi podmiot — drugi podmiot nie jest właścicielem sesji
		// ani właścicielem kontekstu, dlatego operacja powinna zakończyć się błędem
		await Assert.ThrowsAsync<KsefApiException>(async () =>
			await KsefClient.CloseOnlineSessionAsync(openSessionResponse.ReferenceNumber, secondSubjectAccessToken)
				.ConfigureAwait(false));
	}

	/// <summary>
	/// Weryfikacja możliwości zamknięcia przez właściciela kontekstu sesji otwartej przez podmiot autoryzowany.
	/// Właściciel kontekstu może zamknąć każdą sesję otwartą w jego kontekście, niezależnie od tego kto ją otworzył.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Nadanie uprawnień InvoiceWrite podmiotowi autoryzowanemu przez właściciela
	/// 2. Uwierzytelnienie podmiotu autoryzowanego w kontekście właściciela
	/// 3. Otwarcie sesji przez podmiot autoryzowany oraz wysyłka faktury
	/// 4. Zamknięcie sesji przez właściciela kontekstu
	/// 5. Weryfikacja statusu zamkniętej sesji
	/// </remarks>
	[Fact]
	public async Task CloseSession_OpenedByAuthorizedSubject_ClosedByOwner_SessionIsClosed()
	{
		// Krok 1: Nadanie uprawnień InvoiceWrite podmiotowi autoryzowanemu przez właściciela
		string authorizedSubjectNip = MiscellaneousUtils.GetRandomNip();

		await GrantInvoiceWritePermissionAndWaitForConfirmationAsync(
			subjectNip: authorizedSubjectNip,
			firstName: "Anna",
			lastName: "Testowa",
			description: "Dostęp do wystawiania faktur dla podmiotu autoryzowanego");

		// Krok 2: Uwierzytelnienie podmiotu autoryzowanego w kontekście właściciela
		string authorizedSubjectAccessToken = await AuthenticateAndGetTokenAsync(authorizedSubjectNip, _ownerNip);

		// Krok 3: Otwarcie sesji przez podmiot autoryzowany oraz wysyłka faktury
		OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAndSendInvoiceAsync(authorizedSubjectAccessToken);

		Assert.False(string.IsNullOrEmpty(openSessionResponse.ReferenceNumber));

		// Krok 4: Zamknięcie sesji przez właściciela kontekstu
		await KsefClient.CloseOnlineSessionAsync(openSessionResponse.ReferenceNumber, _ownerAccessToken)
			.ConfigureAwait(false);

		// Krok 5: Weryfikacja statusu zamkniętej sesji
		SessionStatusResponse sessionStatus = await AsyncPollingUtils.PollAsync(
			async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
				KsefClient,
				openSessionResponse.ReferenceNumber,
				_ownerAccessToken).ConfigureAwait(false),
			result => result is not null && result.Status.Code == OnlineSessionCodeResponse.ProcessedSuccessfully,
			cancellationToken: CancellationToken.None).ConfigureAwait(false);

		Assert.Equal(OnlineSessionCodeResponse.ProcessedSuccessfully, sessionStatus.Status.Code);
	}

	/// <summary>
	/// Przygotowanie danych szyfrowania, otwarcie sesji online oraz wysyłka faktury dla wskazanego tokenu dostępu.
	/// </summary>
	/// <param name="accessToken">Token dostępu podmiotu otwierającego sesję.</param>
	/// <returns>Odpowiedź zawierająca numer referencyjny otwartej sesji.</returns>
	private async Task<OpenOnlineSessionResponse> OpenOnlineSessionAndSendInvoiceAsync(string accessToken)
	{
		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
			KsefClient,
			encryptionData,
			accessToken,
			DefaultSystemCode).ConfigureAwait(false);

		await OnlineSessionUtils.SendInvoiceAsync(
			KsefClient,
			openSessionResponse.ReferenceNumber,
			accessToken,
			_ownerNip,
			InvoiceTemplatePath,
			encryptionData,
			CryptographyService,
			true).ConfigureAwait(false);

		return openSessionResponse;
	}

	/// <summary>
	/// Przeprowadzenie uwierzytelnienia dla wskazanego NIP i zwrócenie tokenu dostępu.
	/// </summary>
	/// <param name="authorizedNip">NIP podmiotu do uwierzytelnienia.</param>
	/// <param name="contextNip">NIP kontekstu do uwierzytelnienia.</param>
	/// <returns>Token dostępu uzyskany po uwierzytelnieniu.</returns>
	private async Task<string> AuthenticateAndGetTokenAsync(string authorizedNip, string contextNip)
	{
		AuthenticationOperationStatusResponse authInfo = await AuthenticationUtils
			.AuthenticateAsync(AuthorizationClient, identifierValue: authorizedNip, contextIdentifierValue: contextNip).ConfigureAwait(false);

		return authInfo.AccessToken.Token;
	}

	/// <summary>
	/// Nadanie uprawnień InvoiceWrite wskazanemu podmiotowi przez właściciela oraz oczekiwanie na potwierdzenie operacji.
	/// </summary>
	/// <param name="subjectNip">NIP podmiotu, któremu nadawane są uprawnienia.</param>
	/// <param name="firstName">Imię osoby reprezentującej podmiot.</param>
	/// <param name="lastName">Nazwisko osoby reprezentującej podmiot.</param>
	/// <param name="description">Opis nadawanych uprawnień.</param>
	private async Task GrantInvoiceWritePermissionAndWaitForConfirmationAsync(
		string subjectNip,
		string firstName,
		string lastName,
		string description)
	{
		GrantPermissionsPersonRequest grantPermissionsRequest = GrantPersonPermissionsRequestBuilder
			.Create()
			.WithSubject(new GrantPermissionsPersonSubjectIdentifier
			{
				Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
				Value = subjectNip
			})
			.WithPermissions(PersonPermissionType.InvoiceWrite)
			.WithDescription(description)
			.WithSubjectDetails(new PersonPermissionSubjectDetails
			{
				SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
				{
					FirstName = firstName,
					LastName = lastName
				}
			})
			.Build();

		OperationResponse grantPermissionsOperationResponse = await KsefClient
			.GrantsPermissionPersonAsync(grantPermissionsRequest, _ownerAccessToken).ConfigureAwait(false);

		await AsyncPollingUtils.PollAsync(
			action: () => KsefClient.OperationsStatusAsync(grantPermissionsOperationResponse.ReferenceNumber, _ownerAccessToken),
			condition: r => r.Status.Code == OperationStatusCodeResponse.Success,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: 30,
			cancellationToken: CancellationToken.None).ConfigureAwait(false);
	}
}