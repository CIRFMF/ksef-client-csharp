#nullable enable
using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;
using System.Text;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermission;

public class IntrospectionPermissionE2ETests : TestBase
{
	private const SystemCode DefaultSystemCode = SystemCode.FA3;
	private const string DefaultSchemaVersion = "1-0E";
	private const string DefaultFormCodeValue = "FA";
	private const string InvoiceTemplatePath = "invoice-template-fa-3.xml";
	private const int SuccessfulInvoiceCountExpected = 1;
	private const int MaxRetries = 60;

	/// <summary>
	/// Osoba posiadająca uprawnienie Introspection ma dostęp do historii/statusu cudzej sesji (tylko wgląd),
	/// ale nie może zamknąć sesji, której nie jest właścicielem.
	/// </summary>
	[Fact]
	public async Task PersonWithIntrospection_CanViewForeignSessionStatus_ButCannotCloseIt()
	{
		string ownerNip = MiscellaneousUtils.GetRandomNip();
		string employeeNip = MiscellaneousUtils.GetRandomNip();
		string thirdPersonNip = MiscellaneousUtils.GetRandomNip();

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
			"E2E grant InvoiceWrite+InvoiceRead to employee");

		PermissionsOperationStatusResponse grantToEmployeeStatus = 
			await PermissionsUtils.WaitForOperationCompletionAsync(KsefClient, grantToEmployee.ReferenceNumber, ownerAccessToken);

		Assert.Equal(OperationStatusCodeResponse.Success, grantToEmployeeStatus.Status.Code);

		// Pracownik uwierzytelnia się w kontekście właściciela i wysyła fakturę w sesji interaktywnej (sesja pozostaje otwarta)
		AuthenticationOperationStatusResponse employeeAuth =
			await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, employeeNip, ownerNip);
		string employeeAccessToken = employeeAuth.AccessToken.Token;

		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAsync(employeeAccessToken, encryptionData);
		Assert.NotNull(openSessionResponse);
		Assert.False(string.IsNullOrWhiteSpace(openSessionResponse.ReferenceNumber));
		await Task.Delay(SleepTime);

		SendInvoiceResponse sendInvoiceResponse = await SendEncryptedInvoiceAsync(
			employeeAccessToken, openSessionResponse.ReferenceNumber, encryptionData, ownerNip);
		Assert.NotNull(sendInvoiceResponse);
		Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));

		SessionStatusResponse statusAfterSend = await AsyncPollingUtils.PollAsync(
			action: () => KsefClient.GetSessionStatusAsync(openSessionResponse.ReferenceNumber, employeeAccessToken),
			condition: r => r is not null && r.SuccessfulInvoiceCount is not null,
			description: "Czekam na przetworzenie faktury w sesji interaktywnej",
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: MaxRetries,
			cancellationToken: CancellationToken);

		Assert.NotNull(statusAfterSend);
		Assert.Equal(SuccessfulInvoiceCountExpected, statusAfterSend.SuccessfulInvoiceCount);
		Assert.True(statusAfterSend.FailedInvoiceCount is null);
		Assert.Equal(OnlineSessionCodeResponse.SessionOpened, statusAfterSend.Status.Code);

		// Właściciel nadaje trzeciej osobie na razie tylko InvoiceWrite oraz InvoiceRead (bez Introspection)
		Client.Core.Models.OperationResponse grantToThirdPerson = await PermissionsUtils.GrantPersonPermissionsAsync(
			KsefClient,
			ownerAccessToken,
			new GrantPermissionsPersonSubjectIdentifier
			{
				Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
				Value = thirdPersonNip
			},
			[PersonPermissionType.InvoiceWrite, PersonPermissionType.InvoiceRead],
			new PersonPermissionSubjectDetails
			{
				SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
				{
					FirstName = "Anna",
					LastName = "Trzecia"
				}
			},
			"E2E grant InvoiceWrite+InvoiceRead to third person");

		PermissionsOperationStatusResponse grantToThirdPersonStatus = await PermissionsUtils.WaitForOperationCompletionAsync(KsefClient, grantToThirdPerson.ReferenceNumber, ownerAccessToken);
		Assert.Equal(OperationStatusCodeResponse.Success, grantToThirdPersonStatus.Status.Code);

		// Trzecia osoba uwierzytelnia się w kontekście właściciela
		AuthenticationOperationStatusResponse thirdPersonAuth =
			await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, thirdPersonNip, ownerNip);
		string thirdPersonAccessToken = thirdPersonAuth.AccessToken.Token;

		// Trzecia osoba działa jako pracownik uwierzytelniony w kontekście właściciela, ale bez Introspection
		// ma dostęp jedynie do sesji, które sama utworzyła w tym kontekście - nie do wszystkich sesji podmiotu.
		// Sesja utworzona przez innego pracownika (employeeNip) nie jest więc dla niej dostępna, dlatego odczyt jej statusu zwraca błąd.
		await Assert.ThrowsAsync<KsefApiException>(async () =>
		{
			await KsefClient.GetSessionStatusAsync(openSessionResponse.ReferenceNumber, thirdPersonAccessToken).ConfigureAwait(false);
		});

		// Właściciel dodatkowo nadaje trzeciej osobie (pracownikowi działającemu w jego kontekście) uprawnienie Introspection -
		// sam właściciel jako podmiot kontekstu zawsze widzi historię wszystkich sesji w swoim kontekście,
		// a Introspection rozszerza ten wgląd również na pracownika, któremu się je nada.
		Client.Core.Models.OperationResponse grantIntrospectionToThirdPerson = await PermissionsUtils.GrantPersonPermissionsAsync(
			KsefClient,
			ownerAccessToken,
			new GrantPermissionsPersonSubjectIdentifier
			{
				Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
				Value = thirdPersonNip
			},
			[PersonPermissionType.Introspection],
			new PersonPermissionSubjectDetails
			{
				SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
				{
					FirstName = "Anna",
					LastName = "Trzecia"
				}
			},
			"E2E grant Introspection to third person");

		PermissionsOperationStatusResponse grantIntrospectionToThirdPersonStatus = await PermissionsUtils.WaitForOperationCompletionAsync(KsefClient, grantIntrospectionToThirdPerson.ReferenceNumber, ownerAccessToken);
		Assert.Equal(OperationStatusCodeResponse.Success, grantIntrospectionToThirdPersonStatus.Status.Code);

		// Nowe uprawnienie wymaga ponownego uwierzytelnienia, żeby zostało uwzględnione w tokenie
		AuthenticationOperationStatusResponse thirdPersonAuthWithIntrospection =
			await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, thirdPersonNip, ownerNip);
		thirdPersonAccessToken = thirdPersonAuthWithIntrospection.AccessToken.Token;

		// Dzięki Introspection można teraz zobaczyć status cudzej sesji
		SessionStatusResponse statusVisibleToThirdPerson = await KsefClient.GetSessionStatusAsync(
			openSessionResponse.ReferenceNumber, thirdPersonAccessToken);
		Assert.NotNull(statusVisibleToThirdPerson);

		// Nie może zamknąć cudzej sesji (Introspection daje tylko wgląd, nie jest właścicielem sesji)
		await Assert.ThrowsAsync<KsefApiException>(async () =>
		{
			await KsefClient.CloseOnlineSessionAsync(
				openSessionResponse.ReferenceNumber,
				thirdPersonAccessToken
			).ConfigureAwait(false);
		});
	}

	/// <summary>
	/// Buduje i wysyła żądanie otwarcia sesji interaktywnej na podstawie danych szyfrowania.
	/// Zwraca odpowiedź z numerem referencyjnym sesji.
	/// </summary>
	private async Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(string accessToken, EncryptionData encryptionData)
	{
		OpenOnlineSessionRequest openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
			.Create()
			.WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(DefaultSystemCode), schemaVersion: DefaultSchemaVersion, value: DefaultFormCodeValue)
			.WithEncryption(
				encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
				initializationVector: encryptionData.EncryptionInfo.InitializationVector,
				publicKeyId: encryptionData.EncryptionInfo.PublicKeyId)
			.Build();

		return await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, accessToken, cancellationToken: CancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Przygotowuje fakturę z szablonu, szyfruje ją i wysyła w ramach sesji interaktywnej.
	/// Zwraca odpowiedź z numerem referencyjnym faktury.
	/// </summary>
	private async Task<SendInvoiceResponse> SendEncryptedInvoiceAsync(
		string accessToken, string sessionReferenceNumber, EncryptionData encryptionData, string sellerNip)
	{
		string path = Path.Combine(AppContext.BaseDirectory, "Templates", InvoiceTemplatePath);
		string xml = File.ReadAllText(path, Encoding.UTF8);
		xml = xml.Replace("#nip#", sellerNip);
		xml = xml.Replace("#invoice_number#", $"{Guid.NewGuid()}");

		byte[] invoice = Encoding.UTF8.GetBytes(xml);
		byte[] encryptedInvoice = CryptographyService.EncryptBytesWithAES256(invoice, encryptionData.CipherKey, encryptionData.CipherIv);
		FileMetadata invoiceMetadata = CryptographyService.GetMetaData(invoice);
		FileMetadata encryptedInvoiceMetadata = CryptographyService.GetMetaData(encryptedInvoice);

		SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
			.Create()
			.WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
			.WithEncryptedDocumentHash(encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
			.WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
			.Build();

		return await KsefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accessToken).ConfigureAwait(false);
	}
}
