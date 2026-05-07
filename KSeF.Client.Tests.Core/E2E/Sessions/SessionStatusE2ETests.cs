using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;
using System.Text;

namespace KSeF.Client.Tests.Core.E2E.Sessions;

/// <summary>
/// Testy end-to-end dla operacji związanych ze statusem sesji KSeF.
/// </summary>
public class SessionStatusE2ETests : TestBase
{
	private const SystemCode DefaultSystemCode = SystemCode.FA3;
	private const SessionType OnlineSession = SessionType.Online;
	private const string DefaultSchemaVersion = "1-0E";
	private const string DefaultFormCodeValue = "FA";
	private const int DefaultPageSize = 10;

	private string _nip;
	private string _accessToken;
	private EncryptionData _encryptionData;

	public SessionStatusE2ETests()
	{
		_nip = MiscellaneousUtils.GetRandomNip();

		AuthenticationOperationStatusResponse authInfo = AuthenticationUtils
		.AuthenticateAsync(AuthorizationClient, _nip)
		.GetAwaiter().GetResult();

		_accessToken = authInfo.AccessToken.Token;
		_encryptionData = CryptographyService.GetEncryptionData();
	}

	/// <summary>
	/// Weryfikacja poprawności działania endpointu pobierającego listę sesji.
	/// Test sprawdza czy sesja po wysłaniu faktury pojawia się na liście sesji z poprawnymi danymi.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Otwarcie nowej sesji interaktywnej
	/// 2. Wysłanie faktury w ramach sesji
	/// 3. Zamknięcie sesji
	/// 4. Pobranie listy wszystkich sesji
	/// 5. Weryfikacja struktury i zawartości odpowiedzi
	/// </remarks>
	[Fact]
	public async Task GetSessions_ReturnsActiveSessions()
	{
		// 1) Otwarcie sesji interaktywnej
		OpenOnlineSessionResponse openOnlineSessionResponse = await OpenOnlineSessionAsync();

		// 2) Wysłanie testowej faktury w ramach sesji
		SendInvoiceResponse sessionResponse = await SendTestInvoiceAsync(openOnlineSessionResponse.ReferenceNumber);

		// 3) Zamknięcie sesji
		await KsefClient.CloseOnlineSessionAsync(openOnlineSessionResponse.ReferenceNumber, _accessToken);

		// 4) Pobranie listy sesji
		SessionsListResponse sessionList = await KsefClient.GetSessionsAsync(
			OnlineSession,
			_accessToken,
			pageSize: DefaultPageSize,
			continuationToken: null,
			cancellationToken: CancellationToken.None
		);

		// 5) Sprawdzenie podstawowej struktury odpowiedzi
		// Assert - weryfikacja wyników

		Assert.NotNull(sessionList);
		Assert.NotNull(sessionList.Sessions);
		Assert.NotEmpty(sessionList.Sessions);

		foreach (Session session in sessionList.Sessions)
		{
			Assert.NotNull(session.ReferenceNumber);
			Assert.NotNull(session.Status);

			Assert.NotEqual(default, session.DateCreated);
			Assert.NotEqual(default, session.DateUpdated);
			Assert.NotEqual(default, session.ValidUntil);

			Assert.True(session.TotalInvoiceCount >= 0);
			Assert.True(session.SuccessfulInvoiceCount >= 0);
			Assert.True(session.FailedInvoiceCount >= 0);
		}
	}

	/// <summary>
	/// Weryfikacja poprawności działania endpointu pobierającego szczegółowy status sesji.
	/// Test sprawdza czy status zawiera aktualne informacje o liczbie przetworzonych faktur.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Otwarcie nowej sesji interaktywnej
	/// 2. Wysłanie faktury w ramach sesji
	/// 3. Zamknięcie sesji
	/// 4. Oczekiwanie (polling) na przetworzenie faktury
	/// 5. Weryfikacja danych statusu sesji
	/// </remarks>
	[Fact]
	public async Task GetSessionStatus_ReturnsCorrectStatus()
	{
		// 1: Otwarcie nowej sesji interaktywnej
		OpenOnlineSessionResponse openOnlineSessionResponse = await OpenOnlineSessionAsync();

		// 2: Wysłanie faktury w ramach sesji
		SendInvoiceResponse sessionResponse = await SendTestInvoiceAsync(openOnlineSessionResponse.ReferenceNumber);

		// 3) Zamknięcie sesji
		await KsefClient.CloseOnlineSessionAsync(openOnlineSessionResponse.ReferenceNumber, _accessToken);

		// 4) Oczekiwanie na przetworzenie faktury
		SessionStatusResponse sessionStatus = await AsyncPollingUtils.PollWithBackoffAsync(
			action: () => KsefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber!, _accessToken, CancellationToken),
			result => result is not null && result.SuccessfulInvoiceCount is not null,
			initialDelay: TimeSpan.FromSeconds(1),
			maxDelay: TimeSpan.FromSeconds(5),
			maxAttempts: 30,
			cancellationToken: CancellationToken);


		// 5) Sprawdzenie podstawowych danych statusu
		// Assert - weryfikacja wyników
		Assert.NotNull(sessionStatus.Status);

		Assert.True(sessionStatus.InvoiceCount >= 0);
		Assert.True(sessionStatus.SuccessfulInvoiceCount >= 0);

		Assert.True(sessionStatus.ValidUntil.HasValue && sessionStatus.ValidUntil.Value != default);
	}

	/// <summary>
	/// Weryfikacja poprawności działania endpointu pobierającego listę faktur w sesji.
	/// Test sprawdza czy wysłana faktura pojawia się na liście z kompletnymi danymi po przetworzeniu.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Otwarcie nowej sesji interaktywnej
	/// 2. Wysłanie faktury w ramach sesji
	/// 3. Zamknięcie sesji
	/// 4. Oczekiwanie (polling) na trwały zapis faktury
	/// 5. Weryfikacja szczegółów każdej faktury na liście
	/// </remarks>
	[Fact]
	public async Task GetSessionInvoices_ContainsSentInvoice()
	{
		// 1) Otwarcie nowej sesji interaktywnej
		OpenOnlineSessionResponse openOnlineSessionResponse = await OpenOnlineSessionAsync();

		// 2) Wysłanie testowej faktury w ramach sesji
		SendInvoiceResponse sessionResponse = await SendTestInvoiceAsync(openOnlineSessionResponse.ReferenceNumber);

		// 3) Zamknięcie sesji
		await KsefClient.CloseOnlineSessionAsync(openOnlineSessionResponse.ReferenceNumber, _accessToken);

		// 4) Oczekiwanie na trwały zapis faktury w repozytorium KSeF
		SessionInvoicesResponse sessionInvoices = await AsyncPollingUtils.PollWithBackoffAsync(
			action: () => KsefClient.GetSessionInvoicesAsync(
				openOnlineSessionResponse.ReferenceNumber!,
				_accessToken,
				cancellationToken: CancellationToken.None),
			result => result is not null && result.Invoices.First().PermanentStorageDate is not null,
			initialDelay: TimeSpan.FromSeconds(1),
			maxDelay: TimeSpan.FromSeconds(5),
			maxAttempts: 30,
			cancellationToken: CancellationToken);

		// 5) Walidacja każdej faktury na liście
		// Assert - weryfikacja wyników
		foreach (SessionInvoice sessionInvoice in sessionInvoices.Invoices)
		{
			Assert.True(sessionInvoice.OrdinalNumber >= 0);

			Assert.NotNull(sessionInvoice.InvoiceNumber);
			Assert.NotNull(sessionInvoice.KsefNumber);
			Assert.NotNull(sessionInvoice.InvoiceHash);
			Assert.NotNull(sessionInvoice.ReferenceNumber);
			Assert.NotNull(sessionInvoice.UpoDownloadUrl);

			//Uzupełniane jedyne dla wysyłki wsadowej
			Assert.Null(sessionInvoice.InvoiceFileName);

			Assert.NotEqual(default, sessionInvoice.AcquisitionDate);
			Assert.NotEqual(default, sessionInvoice.InvoicingDate);
			Assert.NotEqual(default, sessionInvoice.PermanentStorageDate);
			Assert.NotEqual(default, sessionInvoice.UpoDownloadUrlExpirationDate);

			Assert.Equal(InvoicingMode.Offline, sessionInvoice.InvoicingMode);
		}
	}

	/// <summary>
	/// Weryfikacja poprawności działania endpointu pobierającego szczegóły pojedynczej faktury.
	/// Test sprawdza czy zwrócone dane faktury zawierają wszystkie wymagane pola.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Otwarcie nowej sesji interaktywnej
	/// 2. Wysłanie faktury w ramach sesji
	/// 3. Zamknięcie sesji
	/// 4. Oczekiwanie (polling) na trwały zapis faktury
	/// 5. Weryfikacja szczegółowych danych faktury
	/// </remarks>
	[Fact]
	public async Task GetSessionInvoice_ReturnsInvoiceDetails()
	{
		// 1) Otwarcie nowej sesji interaktywnej
		OpenOnlineSessionResponse openOnlineSessionResponse = await OpenOnlineSessionAsync();

		// 2) Wysłanie testowej faktury w ramach sesji
		SendInvoiceResponse sessionResponse = await SendTestInvoiceAsync(openOnlineSessionResponse.ReferenceNumber);

		// 3) Zamknięcie sesji
		await KsefClient.CloseOnlineSessionAsync(openOnlineSessionResponse.ReferenceNumber, _accessToken);

		// 4) Oczekiwanie na trwały zapis faktury
		SessionInvoice sessionInvoice = await AsyncPollingUtils.PollWithBackoffAsync(
			action: () => KsefClient.GetSessionInvoiceAsync(
				openOnlineSessionResponse.ReferenceNumber!,
				sessionResponse.ReferenceNumber,
				_accessToken,
				cancellationToken: CancellationToken.None),
			result => result is not null && result.PermanentStorageDate is not null,
			initialDelay: TimeSpan.FromSeconds(1),
			maxDelay: TimeSpan.FromSeconds(5),
			maxAttempts: 30,
			cancellationToken: CancellationToken);

		// 5) Sprawdzenie numeru porządkowego faktury
		// Assert - weryfikacja wyników
		Assert.True(sessionInvoice.OrdinalNumber >= 0);

		Assert.NotNull(sessionInvoice.InvoiceNumber);
		Assert.NotNull(sessionInvoice.KsefNumber);
		Assert.NotNull(sessionInvoice.InvoiceHash);
		Assert.NotNull(sessionInvoice.ReferenceNumber);
		Assert.NotNull(sessionInvoice.UpoDownloadUrl);

		// Uzupełniane jedyne dla wysyłki wsadowej
		Assert.Null(sessionInvoice.InvoiceFileName);

		Assert.NotEqual(default, sessionInvoice.AcquisitionDate);
		Assert.NotEqual(default, sessionInvoice.InvoicingDate);
		Assert.NotEqual(default, sessionInvoice.PermanentStorageDate);
		Assert.NotEqual(default, sessionInvoice.UpoDownloadUrlExpirationDate);

		Assert.Equal(InvoicingMode.Offline, sessionInvoice.InvoicingMode);
	}

	/// <summary>
	/// Weryfikacja poprawności działania endpointu pobierającego UPO (Urzędowe Poświadczenie Odbioru) 
	/// na podstawie numeru KSeF.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Otwarcie nowej sesji interaktywnej
	/// 2. Wysłanie faktury w ramach sesji
	/// 3. Zamknięcie sesji
	/// 4. Oczekiwanie (polling) na przydzielenie numeru KSeF
	/// 5. Pobranie UPO używając numeru KSeF
	/// 6. Weryfikacja poprawności pobranego UPO
	/// </remarks>
	[Fact]
	public async Task GetSessionInvoiceUpoByKsefNumber_ReturnsUpo()
	{
		// 1) Otwarcie nowej sesji interaktywnej
		OpenOnlineSessionResponse openOnlineSessionResponse = await OpenOnlineSessionAsync();

		// 2) Wysłanie testowej faktury w ramach sesji
		SendInvoiceResponse sessionResponse = await SendTestInvoiceAsync(openOnlineSessionResponse.ReferenceNumber);

		// 3) Zamknięcie sesji
		await KsefClient.CloseOnlineSessionAsync(openOnlineSessionResponse.ReferenceNumber, _accessToken);

		// 4) Oczekiwanie na przydzielenie numeru KSeF dla faktury
		SessionInvoice sessionInvoice = await AsyncPollingUtils.PollWithBackoffAsync(
			action: () => KsefClient.GetSessionInvoiceAsync(
				openOnlineSessionResponse.ReferenceNumber!,
				sessionResponse.ReferenceNumber,
				_accessToken,
				cancellationToken: CancellationToken.None),
			result => result is not null && result.KsefNumber is not null,
			initialDelay: TimeSpan.FromSeconds(1),
			maxDelay: TimeSpan.FromSeconds(5),
			maxAttempts: 30,
			cancellationToken: CancellationToken);

		// 5) Pobranie UPO używając numeru KSeF
		// Act - wykonanie testowanej operacji
		string upo = await KsefClient.GetSessionInvoiceUpoByKsefNumberAsync(
			openOnlineSessionResponse.ReferenceNumber,
			sessionInvoice.KsefNumber,
			_accessToken,
			cancellationToken: CancellationToken.None
		);

		// 6) Sprawdzenie czy UPO zostało zwrócone (nie jest puste)
		Assert.False(string.IsNullOrEmpty(upo));
	}

	/// <summary>
	/// Weryfikacja poprawności działania endpointu pobierającego UPO (Urzędowe Poświadczenie Odbioru)
	/// na podstawie numeru referencyjnego faktury.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Otwarcie nowej sesji interaktywnej
	/// 2. Wysłanie faktury w ramach sesji
	/// 3. Zamknięcie sesji
	/// 4. Pobranie UPO używając numeru referencyjnego
	/// 5. Weryfikacja poprawności pobranego UPO
	/// </remarks>
	[Fact]
	public async Task GetSessionInvoiceUpoByReferenceNumber_ReturnsUpo()
	{
		// 1) Otwarcie nowej sesji interaktywnej
		OpenOnlineSessionResponse openOnlineSessionResponse = await OpenOnlineSessionAsync();

		// 2) Wysłanie testowej faktury w ramach sesji
		SendInvoiceResponse sessionResponse = await SendTestInvoiceAsync(openOnlineSessionResponse.ReferenceNumber);

		// 3) Zamknięcie sesji
		await KsefClient.CloseOnlineSessionAsync(openOnlineSessionResponse.ReferenceNumber, _accessToken);

		await Task.Delay(5 * SleepTime);

		// 4) Pobranie UPO używając numeru referencyjnego faktury
		// Act - wykonanie testowanej operacji
		string upo = await KsefClient.GetSessionInvoiceUpoByReferenceNumberAsync(
			openOnlineSessionResponse.ReferenceNumber,
			sessionResponse.ReferenceNumber,
			_accessToken,
			cancellationToken: CancellationToken.None
		);

		// 5) Sprawdzenie czy UPO zostało zwrócone
		Assert.False(string.IsNullOrEmpty(upo));
	}

	/// <summary>
	/// Weryfikacja poprawności działania endpointu pobierającego UPO sesji (zbiorcze poświadczenie).
	/// Test sprawdza czy można pobrać UPO dla całej sesji po jej zamknięciu.
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Otwarcie nowej sesji interaktywnej
	/// 2. Wysłanie faktury w ramach sesji
	/// 3. Zamknięcie sesji
	/// 4. Oczekiwanie (polling) na wygenerowanie UPO sesji
	/// 5. Pobranie UPO sesji
	/// 6. Weryfikacja poprawności pobranego UPO
	/// </remarks>
	[Fact]
	public async Task GetSessionUpo_ReturnsUpo()
	{
		// 1) Otwarcie nowej sesji interaktywnej
		OpenOnlineSessionResponse openOnlineSessionResponse = await OpenOnlineSessionAsync();

		// 2) Wysłanie testowej faktury w ramach sesji
		SendInvoiceResponse sessionResponse = await SendTestInvoiceAsync(openOnlineSessionResponse.ReferenceNumber);

		// 3) Zamknięcie sesji
		await KsefClient.CloseOnlineSessionAsync(openOnlineSessionResponse.ReferenceNumber, _accessToken);

		// 4) Oczekiwanie na wygenerowanie UPO sesji
		SessionStatusResponse sessionStatus = await AsyncPollingUtils.PollWithBackoffAsync(
			action: () => KsefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber!, _accessToken, CancellationToken),
			result => result is not null && result.Upo is not null,
			initialDelay: TimeSpan.FromSeconds(1),
			maxDelay: TimeSpan.FromSeconds(5),
			maxAttempts: 30,
			cancellationToken: CancellationToken);

		// 5) Pobranie UPO sesji używając numeru referencyjnego pierwszej strony
		// Act - wykonanie testowanej operacji
		string upo = await KsefClient.GetSessionUpoAsync(
			openOnlineSessionResponse.ReferenceNumber,
			sessionStatus.Upo.Pages.First().ReferenceNumber,
			_accessToken,
			cancellationToken: CancellationToken.None
		);

		// 6) Sprawdzenie czy UPO zostało zwrócone
		Assert.False(string.IsNullOrEmpty(upo));
	}

	/// <summary>
	/// Wysyła testową fakturę i zwraca jej dane
	/// </summary>
	private async Task<SendInvoiceResponse> SendTestInvoiceAsync(string sessionReferenceNumber)
	{
		return await SendEncryptedInvoiceAsync(
			sessionReferenceNumber,
			_encryptionData,
			CryptographyService,
			"invoice-template-fa-3.xml",
			_nip)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Buduje i wysyła żądanie otwarcia sesji interaktywnej na podstawie danych szyfrowania.
	/// Zwraca odpowiedź z numerem referencyjnym sesji.
	/// </summary>
	private async Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(
		SystemCode systemCode = DefaultSystemCode)
	{
		OpenOnlineSessionRequest openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
			.Create()
			.WithFormCode(
				systemCode: SystemCodeHelper.GetSystemCode(systemCode),
				schemaVersion: DefaultSchemaVersion,
				value: DefaultFormCodeValue)
			.WithEncryption(
				encryptedSymmetricKey: _encryptionData.EncryptionInfo.EncryptedSymmetricKey,
				initializationVector: _encryptionData.EncryptionInfo.InitializationVector,
				publicKeyId: _encryptionData.EncryptionInfo.PublicKeyId)
			.Build();

		OpenOnlineSessionResponse openOnlineSessionResponse = await KsefClient
			.OpenOnlineSessionAsync(
				openOnlineSessionRequest,
				_accessToken,
				cancellationToken: CancellationToken)
			.ConfigureAwait(false);

		return openOnlineSessionResponse;
	}

	/// <summary>
	/// Przygotowuje fakturę z szablonu, szyfruje ją i wysyła w ramach sesji interaktywnej.
	/// Zwraca odpowiedź z numerem referencyjnym faktury.
	/// </summary>
	private async Task<SendInvoiceResponse> SendEncryptedInvoiceAsync(
		string sessionReferenceNumber,
		EncryptionData encryptionData,
		ICryptographyService cryptographyService,
		string invoiceTemplatePath,
		string nip)
	{
		string path = Path.Combine(AppContext.BaseDirectory, "Templates", invoiceTemplatePath);
		string xml = File.ReadAllText(path, Encoding.UTF8);
		xml = xml.Replace("#nip#", nip);
		xml = xml.Replace("#invoice_number#", $"{Guid.NewGuid()}");

		using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes(xml));
		byte[] invoice = memoryStream.ToArray();

		byte[] encryptedInvoice = cryptographyService.EncryptBytesWithAES256(
			invoice,
			encryptionData.CipherKey,
			encryptionData.CipherIv);

		FileMetadata invoiceMetadata = cryptographyService.GetMetaData(invoice);
		FileMetadata encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

		SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
			.Create()
			.WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
			.WithEncryptedDocumentHash(
				encryptedInvoiceMetadata.HashSHA,
				encryptedInvoiceMetadata.FileSize)
			.WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
			.Build();

		SendInvoiceResponse sendInvoiceResponse = await KsefClient
			.SendOnlineSessionInvoiceAsync(
				sendOnlineInvoiceRequest,
				sessionReferenceNumber,
				_accessToken)
			.ConfigureAwait(false);

		return sendInvoiceResponse;
	}
}
