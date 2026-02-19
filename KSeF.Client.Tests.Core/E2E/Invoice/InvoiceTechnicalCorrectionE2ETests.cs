using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace KSeF.Client.Tests.Core.E2E.Invoice;

/// <summary>
/// Testy end-to-end dla funkcjonalności korekty technicznej faktur w systemie KSeF.
/// Korekta techniczna umożliwia ponowne przesłanie faktury wystawionej w trybie offline,
/// która została odrzucona z powodu błędów technicznych (np. błąd walidacji semantycznej).
/// 
/// Zgodnie z dokumentacją KSeF, korekta techniczna:
/// - Może być przesyłana wyłącznie w sesji interaktywnej
/// - Dotyczy faktur offline odrzuconych zarówno w sesji interaktywnej, jak i wsadowej
/// - Nie pozwala na korygowanie treści faktury - dotyczy tylko problemów technicznych
/// - Wymaga podania HashOfCorrectedInvoice z pierwotnej, odrzuconej faktury
/// </summary>
public class InvoiceTechnicalCorrectionE2ETests : TestBase
{
	private readonly string _nip;
	private readonly string _accessToken;

	public InvoiceTechnicalCorrectionE2ETests()
	{
		_nip = MiscellaneousUtils.GetRandomNip();

		AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, _nip)
						  .GetAwaiter()
						  .GetResult();
		_accessToken = authInfo.AccessToken.Token;
	}

	/// <summary>
	/// Testuje scenariusz korekty technicznej faktury w ramach tej samej sesji interaktywnej.
	/// Scenariusz testu:
	/// 1. Otwiera sesję interaktywną z szyfrowaniem AES-256
	/// 2. Wysyła fakturę z błędem semantycznym (data w przyszłości - jutro UTC)
	/// 3. Oczekuje na odrzucenie faktury z kodem błędu 450 (InvoiceSemanticValidationError)
	/// 4. Wysyła korektę techniczną z poprawną fakturą, podając hash odrzuconej faktury
	/// 5. Weryfikuje pomyślne przyjęcie korekty (kod 200 - Success)
	/// </summary>
	/// <param name="systemCode">Kod systemu KSeF (np. FA3 dla standardowych faktur)</param>
	/// <param name="invoiceTemplatePath">Ścieżka do pliku szablonu XML faktury w katalogu Templates</param>
	[Theory]
	[InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
	public async Task InvoiceTechnicalCorrection(
	SystemCode systemCode, string invoiceTemplatePath)
	{
		// 1) Arrange - Przygotowanie danych szyfrujących i otwarcie sesji
		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient, encryptionData, _accessToken, systemCode);

		// Przygotowanie faktury z błędem semantycznym (data w przyszłości)
		string invoiceXmlTemplate = PrepareInvoiceFromTemplate(invoiceTemplatePath, _nip);

		DateTime tomorrowUtcDate = DateTime.UtcNow.Date.AddDays(1);
		string invalidInvoiceXml = ReplaceXmlElementValue(
			invoiceXmlTemplate,
			"P_1",
			tomorrowUtcDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

		// 2) Act - Wysłanie faktury z błędem i oczekiwanie na odrzucenie
		SendInvoiceResponse invalidInvoiceResponse = await SendEncryptedInvoiceInOnlineSessionAsync(
			openSessionResponse.ReferenceNumber,
			encryptionData,
			invalidInvoiceXml);

		Assert.False(string.IsNullOrWhiteSpace(invalidInvoiceResponse?.ReferenceNumber));

		SessionInvoice rejectedInvoiceStatus = await AsyncPollingUtils.PollAsync(
			action: async () => await OnlineSessionUtils.GetSessionInvoiceStatusAsync(
				KsefClient,
				openSessionResponse.ReferenceNumber,
				invalidInvoiceResponse.ReferenceNumber,
				_accessToken)
			.ConfigureAwait(false),
			condition: result => result is not null && result.Status.Code != InvoiceInSessionStatusCodeResponse.Processing,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: 30);

		// 3) Assert - Weryfikacja odrzucenia z błędem semantycznym (kod 450)
		Assert.NotNull(rejectedInvoiceStatus);
		Assert.Equal(
			InvoiceInSessionStatusCodeResponse.InvoiceSemanticValidationError,
			rejectedInvoiceStatus.Status.Code);

		// 4) Act - Wysłanie korekty technicznej z poprawną fakturą
		SendInvoiceResponse technicalCorrectionResponse = await SendEncryptedInvoiceInOnlineSessionAsync(
			openSessionResponse.ReferenceNumber,
			encryptionData,
			invoiceXmlTemplate,
			rejectedInvoiceStatus.InvoiceHash);

		Assert.False(string.IsNullOrWhiteSpace(technicalCorrectionResponse?.ReferenceNumber));

		SessionInvoice correctedInvoiceStatus = await AsyncPollingUtils.PollAsync(
			action: async () => await OnlineSessionUtils.GetSessionInvoiceStatusAsync(
				KsefClient,
				openSessionResponse.ReferenceNumber,
				technicalCorrectionResponse.ReferenceNumber,
				_accessToken)
				.ConfigureAwait(false),
			condition: result => result is not null && result.Status.Code == InvoiceInSessionStatusCodeResponse.Success,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: 30);

		// 5) Assert - Weryfikacja pomyślnego przyjęcia korekty technicznej
		Assert.NotNull(correctedInvoiceStatus);
		Assert.Equal(InvoiceInSessionStatusCodeResponse.Success, correctedInvoiceStatus.Status.Code);

		await KsefClient.CloseOnlineSessionAsync(openSessionResponse.ReferenceNumber, _accessToken);
	}

	/// <summary>
	/// Testuje scenariusz korekty technicznej faktury w ramach nowej sesji interaktywnej.
	/// Ten test weryfikuje, że korekta techniczna może być wysłana w innej sesji niż ta,
	/// w której faktura została pierwotnie odrzucona.
	/// 
	/// Scenariusz testu:
	/// 1. Otwiera pierwszą sesję interaktywną z szyfrowaniem AES-256
	/// 2. Wysyła fakturę z błędem semantycznym (data w przyszłości - jutro UTC)
	/// 3. Oczekuje na odrzucenie faktury z kodem błędu 450 (InvoiceSemanticValidationError)
	/// 4. Zamyka pierwszą sesję
	/// 5. Otwiera nową, drugą sesję interaktywną
	/// 6. Wysyła korektę techniczną z poprawną fakturą w nowej sesji
	/// 7. Weryfikuje pomyślne przyjęcie korekty (kod 200 - Success)
	/// 8. Zamyka drugą sesję
	/// </summary>
	/// <param name="systemCode">Kod systemu KSeF (np. FA3 dla standardowych faktur)</param>
	/// <param name="invoiceTemplatePath">Ścieżka do pliku szablonu XML faktury w katalogu Templates</param>
	[Theory]
	[InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
	public async Task ShouldSuccessfullyProcessTechnicalCorrectionInDifferentSession(
		SystemCode systemCode,
		string invoiceTemplatePath)
	{
		// 1) Arrange - Przygotowanie danych szyfrujących i otwarcie pierwszej sesji
		EncryptionData firstSessionEncryptionData = CryptographyService.GetEncryptionData();

		OpenOnlineSessionResponse firstSessionResponse = await OnlineSessionUtils
			.OpenOnlineSessionAsync(KsefClient, firstSessionEncryptionData, _accessToken, systemCode);

		Assert.False(string.IsNullOrWhiteSpace(firstSessionResponse?.ReferenceNumber));

		// Przygotowanie faktury z błędem semantycznym (data w przyszłości)
		string invoiceXmlTemplate = PrepareInvoiceFromTemplate(invoiceTemplatePath, _nip);

		DateTime tomorrowUtcDate = DateTime.UtcNow.Date.AddDays(1);
		string invalidInvoiceXml = ReplaceXmlElementValue(
			invoiceXmlTemplate,
			"P_1",
			tomorrowUtcDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

		// 2) Act - Wysłanie faktury z błędem i oczekiwanie na odrzucenie w pierwszej sesji
		SendInvoiceResponse invalidInvoiceResponse = await SendEncryptedInvoiceInOnlineSessionAsync(
			firstSessionResponse.ReferenceNumber,
			firstSessionEncryptionData,
			invalidInvoiceXml);

		Assert.False(string.IsNullOrWhiteSpace(invalidInvoiceResponse?.ReferenceNumber));

		SessionInvoice rejectedInvoiceStatus = await AsyncPollingUtils.PollAsync(
			action: async () => await OnlineSessionUtils.GetSessionInvoiceStatusAsync(
				KsefClient,
				firstSessionResponse.ReferenceNumber,
				invalidInvoiceResponse.ReferenceNumber,
				_accessToken)
				.ConfigureAwait(false),
			condition: result => result is not null && result.Status.Code != InvoiceInSessionStatusCodeResponse.Processing,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: 30);

		// 3) Assert - Weryfikacja odrzucenia z błędem semantycznym (kod 450)
		Assert.NotNull(rejectedInvoiceStatus);
		Assert.Equal(
			InvoiceInSessionStatusCodeResponse.InvoiceSemanticValidationError,
			rejectedInvoiceStatus.Status.Code);

		// 4) Cleanup - Zamknięcie pierwszej sesji
		await KsefClient.CloseOnlineSessionAsync(firstSessionResponse.ReferenceNumber, _accessToken);

		// 5) Arrange - Otwarcie nowej, drugiej sesji dla wysłania korekty technicznej
		EncryptionData secondSessionEncryptionData = CryptographyService.GetEncryptionData();

		OpenOnlineSessionResponse secondSessionResponse = await OnlineSessionUtils
			.OpenOnlineSessionAsync(KsefClient, secondSessionEncryptionData, _accessToken, systemCode);

		Assert.False(string.IsNullOrWhiteSpace(secondSessionResponse?.ReferenceNumber));

		// 6) Act - Wysłanie korekty technicznej z poprawną fakturą w nowej sesji
		SendInvoiceResponse technicalCorrectionResponse = await SendEncryptedInvoiceInOnlineSessionAsync(
			secondSessionResponse.ReferenceNumber,
			secondSessionEncryptionData,
			invoiceXmlTemplate,
			rejectedInvoiceStatus.InvoiceHash);

		Assert.False(string.IsNullOrWhiteSpace(technicalCorrectionResponse?.ReferenceNumber));

		SessionInvoice correctedInvoiceStatus = await AsyncPollingUtils.PollAsync(
			action: async () => await OnlineSessionUtils.GetSessionInvoiceStatusAsync(
				KsefClient,
				secondSessionResponse.ReferenceNumber,
				technicalCorrectionResponse.ReferenceNumber,
				_accessToken)
				.ConfigureAwait(false),
			condition: result => result is not null && result.Status.Code == InvoiceInSessionStatusCodeResponse.Success,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: 30);

		// 7) Assert - Weryfikacja pomyślnego przyjęcia korekty technicznej
		Assert.NotNull(correctedInvoiceStatus);
		Assert.Equal(InvoiceInSessionStatusCodeResponse.Success, correctedInvoiceStatus.Status.Code);

		// 8) Cleanup - Zamknięcie drugiej sesji
		await KsefClient.CloseOnlineSessionAsync(secondSessionResponse.ReferenceNumber, _accessToken);
	}

	/// <summary>
	/// Przygotowuje fakturę z szablonu, szyfruje ją i wysyła w ramach sesji interaktywnej.
	/// </summary>
	/// <param name="sessionReferenceNumber">Numer referencyjny otwartej sesji interaktywnej</param>
	/// <param name="encryptionData">Dane szyfrujące (klucz AES, wektor IV)</param>
	/// <param name="invoiceXml">Zawartość faktury w formacie XML</param>
	/// <param name="hashOfCorrectedInvoice">
	/// Hash faktury, którą korygujemy technicznie. 
	/// Wymagany przy korekcie technicznej - identyfikuje odrzuconą fakturę.
	/// Dla pierwszej próby wysłania faktury wartość NULL.
	/// </param>
	/// <returns>Odpowiedź z systemu KSeF zawierająca numer referencyjny wysłanej faktury</returns>
	private async Task<SendInvoiceResponse> SendEncryptedInvoiceInOnlineSessionAsync(
		string sessionReferenceNumber,
		EncryptionData encryptionData,
		string invoiceXml,
		string hashOfCorrectedInvoice = null)
	{
		byte[] invoice = Encoding.UTF8.GetBytes(invoiceXml);

		byte[] encryptedInvoice = CryptographyService.EncryptBytesWithAES256(
			invoice,
			encryptionData.CipherKey,
			encryptionData.CipherIv);

		FileMetadata invoiceMetadata = CryptographyService.GetMetaData(invoice);
		FileMetadata encryptedInvoiceMetadata = CryptographyService.GetMetaData(encryptedInvoice);

		ISendInvoiceOnlineSessionRequestBuilderBuild requestBuilder = SendInvoiceOnlineSessionRequestBuilder
			.Create()
			.WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
			.WithEncryptedDocumentHash(encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
			.WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
			.WithOfflineMode(true);

		// Dodanie hasha korygowanej faktury dla korekty technicznej
		if (!string.IsNullOrEmpty(hashOfCorrectedInvoice))
		{
			requestBuilder = requestBuilder.WithHashOfCorrectedInvoice(hashOfCorrectedInvoice);
		}

		SendInvoiceRequest sendInvoiceRequest = requestBuilder.Build();

		SendInvoiceResponse sendInvoiceResponse = await KsefClient.SendOnlineSessionInvoiceAsync(
			sendInvoiceRequest,
			sessionReferenceNumber,
			_accessToken)
			.ConfigureAwait(false);

		return sendInvoiceResponse;
	}

	/// <summary>
	/// Ustawia wartość elementu XML o podanej nazwie lokalnej.
	/// </summary>
	/// <param name="xmlContent">Zawartość dokumentu XML jako string</param>
	/// <param name="elementLocalName">Lokalna nazwa elementu XML (bez prefiksu przestrzeni nazw)</param>
	/// <param name="newValue">Nowa wartość do ustawienia w elemencie</param>
	/// <returns>Zmodyfikowany dokument XML jako string</returns>
	/// <exception cref="InvalidOperationException">
	/// Rzucany gdy element o podanej nazwie nie został znaleziony w dokumencie XML
	/// </exception>
	private static string ReplaceXmlElementValue(string xmlContent, string elementLocalName, string newValue)
	{
		XDocument doc = XDocument.Parse(xmlContent, LoadOptions.PreserveWhitespace);

		XElement el = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == elementLocalName)
				 ?? throw new InvalidOperationException($"Element '{elementLocalName}' nie znaleziono w Templates.");
		el.Value = newValue;

		return doc.ToString(SaveOptions.DisableFormatting);
	}

	/// <summary>
	/// Wczytuje szablon faktury XML z pliku, podstawia wartości dynamiczne i zwraca gotowy XML.
	/// </summary>
	/// <param name="invoiceTemplateFileName">Nazwa pliku szablonu w katalogu Templates</param>
	/// <param name="taxpayerNip">Numer NIP podatnika do podstawienia w szablonie</param>
	/// <returns>Przygotowany dokument XML faktury z podstawionymi wartościami</returns>
	private string PrepareInvoiceFromTemplate(string invoiceTemplateFileName, string taxpayerNip)
	{
		string path = Path.Combine(AppContext.BaseDirectory, "Templates", invoiceTemplateFileName);
		string xml = File.ReadAllText(path, Encoding.UTF8);
		xml = xml.Replace("#nip#", taxpayerNip);
		xml = xml.Replace("#invoice_number#", $"{Guid.NewGuid()}");

		return xml;
	}
}