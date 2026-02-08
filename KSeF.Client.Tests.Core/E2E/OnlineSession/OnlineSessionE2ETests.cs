#nullable enable
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Tests.Utils.Upo;
using System.Text;

namespace KSeF.Client.Tests.Core.E2E.OnlineSession;

[Collection("OnlineSessionScenario")]
public class OnlineSessionE2ETests : TestBase
{
	private const SystemCode DefaultSystemCode = SystemCode.FA3;
	private const string DefaultSchemaVersion = "1-0E";
	private const string DefaultFormCodeValue = "FA";
	private const int SuccessfulInvoiceCountExpected = 1;
	private const int MaxRetries = 60;

	private readonly string accessToken = string.Empty;
	private string Nip { get; }

	public OnlineSessionE2ETests()
	{
		Nip = MiscellaneousUtils.GetRandomNip();
		AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, Nip)
										  .GetAwaiter()
										  .GetResult();
		accessToken = authInfo.AccessToken.Token;
	}

	[Theory]
	[InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
	public async Task OnlineSessionAsync_TryReadInvoiceWithNoPermission_ShouldThrowException(SystemCode systemCode, string invoiceTemplatePath)
	{
		string authorizedNip = MiscellaneousUtils.GetRandomNip();
		Client.Core.Models.OperationResponse operationResponse = await PermissionsUtils.GrantPersonPermissionsAsync(
			KsefClient,
			accessToken,
			new GrantPermissionsPersonSubjectIdentifier
			{
				Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
				Value = authorizedNip
			},
			[
				PersonPermissionType.InvoiceWrite
			],
			new PersonPermissionSubjectDetails
			{
				SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
				{
					FirstName = "Anna",
					LastName = "Testowa"
				}
			},
		"Grant write invoices permission for testing.");

		bool isSuccess = await PermissionsUtils.ConfirmOperationSuccessAsync(KsefClient, operationResponse, accessToken);

		// Arrange
		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		// 1) Otwarcie sesji
		OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAsync(encryptionData, systemCode);
		Assert.NotNull(openSessionResponse);
		Assert.False(string.IsNullOrWhiteSpace(openSessionResponse.ReferenceNumber));
		await Task.Delay(SleepTime);

		// 2) Wysłanie faktury
		SendInvoiceResponse sendInvoiceResponse = await SendEncryptedInvoiceAsync(openSessionResponse.ReferenceNumber, encryptionData, CryptographyService, invoiceTemplatePath, Nip);
		Assert.NotNull(sendInvoiceResponse);
		Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));
		await Task.Delay(SleepTime);

		SessionStatusResponse statusAfterSend = await AsyncPollingUtils.PollAsync(
			async () => await KsefClient.GetSessionStatusAsync(
				openSessionResponse.ReferenceNumber,
				accessToken
			).ConfigureAwait(false),
			result => result is not null && result.SuccessfulInvoiceCount is not null,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: MaxRetries,
			cancellationToken: CancellationToken);

		Assert.NotNull(statusAfterSend);
		Assert.True(statusAfterSend.SuccessfulInvoiceCount is not null);
		Assert.Equal(SuccessfulInvoiceCountExpected, statusAfterSend.SuccessfulInvoiceCount);
		Assert.True(statusAfterSend.FailedInvoiceCount is null);
		Assert.Null(statusAfterSend.Upo);

		SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(openSessionResponse.ReferenceNumber, accessToken);

		// 3) Autoryzacja drugiego nipu
		AuthenticationOperationStatusResponse unauthorizedNipToken = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, authorizedNip);

		// 4) Próba pobrania faktur sesji przez nieautoryzowany nip
		await Assert.ThrowsAsync<KsefApiException>(async () =>
		{
			await KsefClient.GetSessionInvoicesAsync(
				openSessionResponse.ReferenceNumber,
				unauthorizedNipToken.AccessToken.Token
			).ConfigureAwait(false);
		});

		// 5) Próba pobrania faktury przez nadany numer KSeF przez nieautoryzowany nip
		await Assert.ThrowsAsync<KsefApiException>(async () =>
		{
			await KsefClient.GetInvoiceAsync(invoices.Invoices.First().KsefNumber, unauthorizedNipToken.AccessToken.Token).ConfigureAwait(false);
		});
	}

	[Theory]
	[InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
	[InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
	public async Task OnlineSessionAsync_FullIntegrationFlow_AllStepsSucceed(SystemCode systemCode, string invoiceTemplatePath)
	{
		// Arrange
		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		// 1) Otwarcie sesji
		OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAsync(encryptionData, systemCode);
		Assert.NotNull(openSessionResponse);
		Assert.False(string.IsNullOrWhiteSpace(openSessionResponse.ReferenceNumber));
		await Task.Delay(SleepTime);

		// 2) Wysłanie faktury
		SendInvoiceResponse sendInvoiceResponse = await SendEncryptedInvoiceAsync(openSessionResponse.ReferenceNumber, encryptionData, CryptographyService, invoiceTemplatePath, Nip);
		Assert.NotNull(sendInvoiceResponse);
		Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));
		await Task.Delay(SleepTime);

		// 3) Status po wysłaniu faktury (oczekujemy 1 sukcesu, brak błędów, brak UPO, kod 100)
		SessionStatusResponse statusAfterSend = await GetSessionStatusUntilInvoiceCountAvailableAsync(openSessionResponse.ReferenceNumber);
		Assert.NotNull(statusAfterSend);
		Assert.True(statusAfterSend.SuccessfulInvoiceCount is not null);
		Assert.Equal(SuccessfulInvoiceCountExpected, statusAfterSend.SuccessfulInvoiceCount);
		Assert.True(statusAfterSend.FailedInvoiceCount is null);
		Assert.Null(statusAfterSend.Upo);
		Assert.Equal(OnlineSessionCodeResponse.SessionOpened, statusAfterSend.Status.Code);
		await Task.Delay(SleepTime);

		// 4) Zamknięcie sesji
		await KsefClient.CloseOnlineSessionAsync(
			openSessionResponse.ReferenceNumber,
			accessToken
		);
		await Task.Delay(SleepTime);

		// 5) Pobranie faktur sesji (powinna być jedna)
		SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(
			openSessionResponse.ReferenceNumber,
			accessToken
		);
		Assert.NotNull(invoices);
		Assert.NotEmpty(invoices.Invoices);
		Assert.Single(invoices.Invoices);
		string ksefNumber = invoices.Invoices.First().KsefNumber;

		// 6) Status po zamknięciu (kod 200) i numer referencyjny UPO
		SessionStatusResponse statusAfterClose = await KsefClient.GetSessionStatusAsync(
			openSessionResponse.ReferenceNumber,
			accessToken
		);
		Assert.NotNull(statusAfterClose);
		Assert.Equal(InvoiceInSessionStatusCodeResponse.Success, statusAfterClose.Status.Code);
		string upoReferenceNumber = statusAfterClose.Upo.Pages.First().ReferenceNumber;
		Assert.NotNull(upoReferenceNumber);

		// 7) pobranie UPO faktury z URL zawartego w metadanych faktury
		Uri upoDownloadUrl = invoices.Invoices.First().UpoDownloadUrl;
		string invoiceUpoXml = await UpoUtils.GetUpoAsync(KsefClient, upoDownloadUrl);
		Assert.False(string.IsNullOrWhiteSpace(invoiceUpoXml));
		InvoiceUpoV4_3 invoiceUpo = UpoUtils.UpoParse<InvoiceUpoV4_3>(invoiceUpoXml);
		Assert.Equal(invoiceUpo.Document.KSeFDocumentNumber, ksefNumber);
		Assert.False(string.IsNullOrWhiteSpace(invoiceUpo.ReceivingEntityName));
		Assert.False(string.IsNullOrWhiteSpace(invoiceUpo.SessionReferenceNumber));
		Assert.NotNull(invoiceUpo.Authentication);
		Assert.False(string.IsNullOrWhiteSpace(invoiceUpo.LogicalStructureName));
		Assert.False(string.IsNullOrWhiteSpace(invoiceUpo.FormCode));
		Assert.NotNull(invoiceUpo.Signature);
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
				initializationVector: encryptionData.EncryptionInfo.InitializationVector)
			.Build();

		OpenOnlineSessionResponse openOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, accessToken, cancellationToken: CancellationToken).ConfigureAwait(false);
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

		byte[] encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData.CipherKey, encryptionData.CipherIv);
		FileMetadata invoiceMetadata = cryptographyService.GetMetaData(invoice);
		FileMetadata encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

		SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
			.Create()
			.WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
			.WithEncryptedDocumentHash(encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
			.WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
			.Build();

		SendInvoiceResponse sendInvoiceResponse = await KsefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accessToken).ConfigureAwait(false);
		return sendInvoiceResponse;
	}

	/// <summary>
	/// Wykonuje pobieranie statusu sesji do momentu, aż licznik poprawnych faktur nie będzie null.
	/// Zwraca aktualny status sesji.
	/// </summary>
	private async Task<SessionStatusResponse> GetSessionStatusUntilInvoiceCountAvailableAsync(string sessionReferenceNumber)
	{
		SessionStatusResponse? statusResponse;
		do
		{
			statusResponse = await KsefClient.GetSessionStatusAsync(sessionReferenceNumber, accessToken).ConfigureAwait(false);
			await Task.Delay(SleepTime).ConfigureAwait(false);
		} while (statusResponse.SuccessfulInvoiceCount is null);

		return statusResponse;
	}
}