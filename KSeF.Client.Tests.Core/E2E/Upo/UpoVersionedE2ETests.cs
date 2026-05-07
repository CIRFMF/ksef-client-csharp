using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Tests.Utils.Upo;
using System.Text;

namespace KSeF.Client.Tests.Core.E2E.Upo;

/// <summary>
/// Testy E2E weryfikujące pobieranie UPO w określonej wersji (v4-3).
/// Wersja UPO jest przekazywana poprzez nagłówek podczas otwierania sesji (online i batch).
/// </summary>
[Collection("UpoScenario")]
public class UpoVersionedE2ETests : TestBase
{
	// Konfiguracja sesji
	private const SystemCode DefaultSystemCode = SystemCode.FA3;
	private const string DefaultSchemaVersion = "1-0E";
	private const string DefaultFormCodeValue = "FA";

	// Wersja UPO używana w testach
	private const string UpoVersion = "upo-v4-3";

	// Konfiguracja sesji wsadowej
	private const int TotalInvoices = 20;
	private const int PartQuantity = 1;
	private const int ExpectedSuccessfulInvoiceCount = 20;

	private string _nip;
	private string _accessToken;

	public UpoVersionedE2ETests()
	{
		_nip = MiscellaneousUtils.GetRandomNip();
		AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, _nip)
												  .GetAwaiter()
												  .GetResult();
		_accessToken = authInfo.AccessToken.Token;
	}

	/// <summary>
	/// Test weryfikujący pobieranie UPO w określonej wersji dla sesji interaktywnej (online).
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Otwarcie sesji interaktywnej z przekazaniem nagłówka wersji UPO
	/// 2. Wysłanie zaszyfrowanej faktury
	/// 3. Polling statusu sesji - oczekiwanie na przetworzenie faktury (kod 100)
	/// 4. Zamknięcie sesji interaktywnej
	/// 5. Pobranie listy faktur sesji
	/// 6. Weryfikacja statusu zamkniętej sesji (kod 200) i pobranie numeru referencyjnego UPO
	/// 7. Pobranie i weryfikacja UPO faktury z URL zawartego w metadanych faktury
	/// 8. Deserializacja i weryfikacja pól specyficznych dla wersji v4-3
	/// </remarks>
	[Theory]
	[InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
	public async Task OnlineSessionReturnsVersionedUpoData(SystemCode systemCode, string invoiceTemplatePath)
	{
		// Arrange
		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		// 1) Otwarcie sesji interaktywnej z nagłówkiem wersji UPO
		OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAsync(encryptionData, systemCode);
		Assert.NotNull(openSessionResponse);
		Assert.False(string.IsNullOrWhiteSpace(openSessionResponse.ReferenceNumber));

		// 2) Wysłanie faktury
		SendInvoiceResponse sendInvoiceResponse = await SendEncryptedInvoiceAsync(openSessionResponse.ReferenceNumber, encryptionData, invoiceTemplatePath);
		Assert.NotNull(sendInvoiceResponse);
		Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));

		// 3) Polling statusu sesji
		SessionStatusResponse statusAfterSend = await AsyncPollingUtils.PollAsync(
			action: async () => await KsefClient.GetSessionStatusAsync(
				openSessionResponse.ReferenceNumber,
				_accessToken
			).ConfigureAwait(false),
			condition: result => result?.SuccessfulInvoiceCount is not null,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			cancellationToken: CancellationToken
		);

		Assert.NotNull(statusAfterSend);
		Assert.NotNull(statusAfterSend.SuccessfulInvoiceCount);
		Assert.Equal(1, statusAfterSend.SuccessfulInvoiceCount);
		await Task.Delay(SleepTime);

		// 4) Zamknięcie sesji
		await KsefClient.CloseOnlineSessionAsync(
			openSessionResponse.ReferenceNumber,
			_accessToken
		);
		await Task.Delay(SleepTime);

		// 5) Pobranie faktur sesji (powinna być jedna)
		SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(
			openSessionResponse.ReferenceNumber,
			_accessToken
		);
		Assert.NotNull(invoices);
		Assert.NotEmpty(invoices.Invoices);
		Assert.Single(invoices.Invoices);
		string ksefNumber = invoices.Invoices.First().KsefNumber;

		// 6) Status po zamknięciu (kod 200) i numer referencyjny UPO
		SessionStatusResponse statusAfterClose = await KsefClient.GetSessionStatusAsync(
			openSessionResponse.ReferenceNumber,
			_accessToken
		);
		Assert.NotNull(statusAfterClose);
		Assert.Equal(200, statusAfterClose.Status.Code);

		// 7) pobranie UPO faktury z URL zawartego w metadanych faktury razem z hash z nagłówka
		Uri upoDownloadUrl = invoices.Invoices.First().UpoDownloadUrl;
		UpoWithHash upoWithHash = await UpoUtils.GetUpoWithHashAsync(this.RestClient, upoDownloadUrl, CancellationToken).ConfigureAwait(false);
		Assert.False(string.IsNullOrWhiteSpace(upoWithHash.Xml));
		Assert.False(string.IsNullOrWhiteSpace(upoWithHash.HashHeaderBase64));

		// Weryfikacja zgodności hash: nagłówek vs lokalnie wyliczony hash faktury
		FileMetadata invoiceMetadata = CryptographyService.GetMetaData(Encoding.UTF8.GetBytes(upoWithHash.Xml));
		Assert.Equal(invoiceMetadata.HashSHA, upoWithHash.HashHeaderBase64);

		InvoiceUpoV4_3 invoiceUpo = UpoUtils.UpoParse<InvoiceUpoV4_3>(upoWithHash.Xml);
		Assert.Equal(invoiceUpo.Document.KSeFDocumentNumber, ksefNumber);
		Assert.NotNull(invoiceUpo.Document.InvoicingMode);
	}

	/// <summary>
	/// Test weryfikujący pobieranie UPO w określonej wersji dla sesji wsadowej (batch).
	/// </summary>
	/// <remarks>
	/// Kroki testu:
	/// 1. Przygotowanie paczki (ZIP, szyfrowanie, podział) i otwarcie sesji z nagłówkiem wersji UPO
	/// 2. Wysłanie wszystkich zaszyfrowanych części
	/// 3. Zamknięcie sesji z pollingiem do momentu powodzenia
	/// 4. Weryfikacja statusu sesji - oczekiwanie na przetworzenie wszystkich faktur
	/// 5. Pobranie dokumentów sesji
	/// 6. Pobranie i weryfikacja UPO pojedynczej faktury
	/// 7. Weryfikacja pól specyficznych dla wersji v4-3
	/// </remarks>
	[Theory]
	[InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
	public async Task BatchSessionReturnsVersionedUpoData(
		SystemCode systemCode,
		string invoiceTemplatePath)
	{
		// Arrange
		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		// 1) Przygotowanie paczki i otwarcie sesji wsadowej z nagłówkiem wersji UPO
		List<BatchPartSendingInfo> encryptedParts = PrepareBatchParts(
			encryptionData,
			invoiceTemplatePath,
			out FileMetadata zipMetadata);

		OpenBatchSessionResponse openBatchResponse = await OpenBatchSessionAsync(
			encryptionData,
			systemCode,
			zipMetadata,
			encryptedParts);
		Assert.NotNull(openBatchResponse);
		Assert.False(string.IsNullOrWhiteSpace(openBatchResponse.ReferenceNumber));

		// 2) Wysłanie wszystkich zaszyfrowanych części
		await KsefClient.SendBatchPartsAsync(openBatchResponse, encryptedParts);

		// 3) Zamknięcie sesji z pollingiem do momentu powodzenia
		await KsefClient.CloseBatchSessionAsync(openBatchResponse.ReferenceNumber, _accessToken);

		// 4) Polling statusu sesji - oczekiwanie na przetworzenie wszystkich faktur
		SessionStatusResponse statusResponse = await AsyncPollingUtils.PollWithBackoffAsync(
			action: () => KsefClient.GetSessionStatusAsync(openBatchResponse.ReferenceNumber!, _accessToken),
			condition: s => s.Status.Code is BatchSessionCodeResponse.ProcessedSuccessfully,
			initialDelay: TimeSpan.FromSeconds(1),
			maxDelay: TimeSpan.FromSeconds(5),
			maxAttempts: 30,
			cancellationToken: CancellationToken);

		Assert.NotNull(statusResponse);
		Assert.Equal(ExpectedSuccessfulInvoiceCount, statusResponse.SuccessfulInvoiceCount);
		Assert.NotNull(statusResponse.Upo);
		Assert.Equal(BatchSessionCodeResponse.ProcessedSuccessfully, statusResponse.Status.Code);

		// 5) Pobranie dokumentów sesji
		SessionInvoicesResponse documents = await BatchUtils.GetSessionInvoicesAsync(
			KsefClient,
			openBatchResponse.ReferenceNumber,
			_accessToken
		);
		Assert.NotNull(documents);
		Assert.NotEmpty(documents.Invoices);

		// 6) Pobranie i weryfikacja UPO pojedynczej faktury
		string ksefNumber = documents.Invoices.First().KsefNumber;
		Uri upoDownloadUrl = documents.Invoices.First().UpoDownloadUrl;

		UpoWithHash upoWithHash = await UpoUtils.GetUpoWithHashAsync(this.RestClient, upoDownloadUrl, CancellationToken).ConfigureAwait(false);
		Assert.False(string.IsNullOrWhiteSpace(upoWithHash.Xml));
		Assert.False(string.IsNullOrWhiteSpace(upoWithHash.HashHeaderBase64));

		FileMetadata invoiceMetadata = CryptographyService.GetMetaData(Encoding.UTF8.GetBytes(upoWithHash.Xml));
		Assert.Equal(invoiceMetadata.HashSHA, upoWithHash.HashHeaderBase64);

		InvoiceUpoV4_3 invoiceUpo = UpoUtils.UpoParse<InvoiceUpoV4_3>(upoWithHash.Xml);

		// 7) Weryfikacja pól specyficznych dla wersji v4-3
		Assert.Equal(ksefNumber, invoiceUpo.Document.KSeFDocumentNumber);
		Assert.NotNull(invoiceUpo.Document.InvoicingMode);
	}

	/// <summary>
	/// Buduje i wysyła żądanie otwarcia sesji interaktywnej na podstawie danych szyfrowania.
	/// Zwraca odpowiedź z numerem referencyjnym sesji.
	/// </summary>
	private async Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(EncryptionData encryptionData, SystemCode systemCode = DefaultSystemCode)
	{
		OpenOnlineSessionRequest openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
			.Create()
			.WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(systemCode), schemaVersion: DefaultSchemaVersion, value: DefaultFormCodeValue)
			.WithEncryption(
				encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
				initializationVector: encryptionData.EncryptionInfo.InitializationVector,
				publicKeyId: encryptionData.EncryptionInfo.PublicKeyId)
			.Build();

		OpenOnlineSessionResponse openOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, _accessToken, UpoVersion, cancellationToken: CancellationToken).ConfigureAwait(false);
		return openOnlineSessionResponse;
	}

	/// <summary>
	/// Generuje faktury, tworzy ZIP, szyfruje i dzieli na części.
	/// </summary>
	private List<BatchPartSendingInfo> PrepareBatchParts(
		EncryptionData encryptionData,
		string invoiceTemplatePath,
		out FileMetadata zipMetadata)
	{
		List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
			count: TotalInvoices,
			nip: _nip,
			templatePath: invoiceTemplatePath);

		(byte[] zipBytes, FileMetadata zipMeta) = BatchUtils.BuildZip(invoices, CryptographyService);
		zipMetadata = zipMeta;

		List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
			zipBytes,
			encryptionData,
			CryptographyService,
			PartQuantity);

		return encryptedParts;
	}

	/// <summary>
	/// Otwiera sesję wsadową z nagłówkiem wersji UPO.
	/// </summary>
	private async Task<OpenBatchSessionResponse> OpenBatchSessionAsync(
		EncryptionData encryptionData,
		SystemCode systemCode,
		FileMetadata zipMetadata,
		List<BatchPartSendingInfo> encryptedParts)
	{
		OpenBatchSessionRequest openBatchRequest = BatchUtils.BuildOpenBatchRequest(
			zipMetadata,
			encryptionData,
			encryptedParts,
			systemCode);

		OpenBatchSessionResponse openBatchResponse = await KsefClient.OpenBatchSessionAsync(
			openBatchRequest,
			_accessToken,
			UpoVersion,
			cancellationToken: CancellationToken)
			.ConfigureAwait(false);

		return openBatchResponse;
	}

	/// <summary>
	/// Przygotowuje fakturę z szablonu, szyfruje ją i wysyła w ramach sesji interaktywnej.
	/// </summary>
	private async Task<SendInvoiceResponse> SendEncryptedInvoiceAsync(
		string sessionReferenceNumber,
		EncryptionData encryptionData,
		string invoiceTemplatePath)
	{
		string path = Path.Combine(AppContext.BaseDirectory, "Templates", invoiceTemplatePath);
		string xml = File.ReadAllText(path, Encoding.UTF8);
		xml = xml.Replace("#nip#", _nip);
		xml = xml.Replace("#invoice_number#", Guid.NewGuid().ToString());

		byte[] invoice = Encoding.UTF8.GetBytes(xml);

		byte[] encryptedInvoice = CryptographyService.EncryptBytesWithAES256(
			invoice,
			encryptionData.CipherKey,
			encryptionData.CipherIv);

		FileMetadata invoiceMetadata = CryptographyService.GetMetaData(invoice);
		FileMetadata encryptedInvoiceMetadata = CryptographyService.GetMetaData(encryptedInvoice);

		SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
			.Create()
			.WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
			.WithEncryptedDocumentHash(encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
			.WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
			.Build();

		SendInvoiceResponse sendInvoiceResponse = await KsefClient.SendOnlineSessionInvoiceAsync(
			sendOnlineInvoiceRequest,
			sessionReferenceNumber,
			_accessToken)
			.ConfigureAwait(false);

		return sendInvoiceResponse;
	}

	/// <summary>
	/// Pobiera UPO faktury z URL i deserializuje do modelu v4-3.
	/// </summary>
	private async Task<InvoiceUpoV4_3> GetAndParseInvoiceUpoAsync(Uri upoDownloadUrl)
	{
		UpoWithHash upoWithHash = await UpoUtils.GetUpoWithHashAsync(this.RestClient, upoDownloadUrl, CancellationToken).ConfigureAwait(false);

		Assert.False(string.IsNullOrWhiteSpace(upoWithHash.Xml));
		Assert.False(string.IsNullOrWhiteSpace(upoWithHash.HashHeaderBase64));

		FileMetadata invoiceMetadata = CryptographyService.GetMetaData(Encoding.UTF8.GetBytes(upoWithHash.Xml));
		Assert.Equal(invoiceMetadata.HashSHA, upoWithHash.HashHeaderBase64);

		InvoiceUpoV4_3 invoiceUpo = UpoUtils.UpoParse<InvoiceUpoV4_3>(upoWithHash.Xml);
		return invoiceUpo;
	}
}