#nullable enable
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Invoice;

/// <summary>
/// Testy E2E dla wysyłki faktur z załącznikami.
/// Weryfikują cały proces: nadawanie uprawnień, wysyłkę faktur i przetwarzanie sesji wsadowej.
/// </summary>
[Collection("InvoicesScenario")]
public class InvoiceWithAttachmentE2ETests : TestBase
{
	private const int TotalInvoicesCount = 5;
	private const int BatchPartQuantity = 1;
	private const int PermissionPollingMaxAttempts = 30;
	private const string StatusDescriptionInvalidPermissionScope = "Nieprawidłowy zakres uprawnień";

	/// <summary>
	/// Test pozytywny: Weryfikuje kompletny proces wysyłki faktur z załącznikami.
	/// Scenariusz:
	/// 1. Utworzenie testowej osoby fizycznej
	/// 2. Uwierzytelnienie w systemie KSeF
	/// 3. Nadanie uprawnień do załączników
	/// 4. Weryfikacja nadania uprawnień (z pollingiem)
	/// 5. Wysłanie faktur z załącznikami w trybie wsadowym
	/// 6. Weryfikacja pomyślnego przetworzenia wszystkich faktur
	/// </summary>
	[Theory]
	[InlineData(SystemCode.FA3, "invoice-template-fa-3-with-attachment.xml")]
	public async Task SendInvoiceWithAttachment(SystemCode systemCode, string invoiceTemplatePath)
	{
		// Krok 1: Arrange - Przygotowanie testowej osoby fizycznej
		string sellerNip = MiscellaneousUtils.GetRandomNip();
		string sellerPesel = MiscellaneousUtils.GetRandomPesel();

		PersonCreateRequest personRequest = new PersonCreateRequest
		{
			Nip = sellerNip,
			Pesel = sellerPesel,
			IsBailiff = false,
			Description = "Osoba fizyczna testowa - test faktur z załącznikami"
		};

		await TestDataClient.CreatePersonAsync(personRequest);

		// Krok 2: Arrange - Uwierzytelnienie sprzedawcy
		AuthenticationOperationStatusResponse authenticationResponse =
			await AuthenticationUtils.AuthenticateAsync(KsefClient, sellerNip);

		string accessToken = authenticationResponse.AccessToken.Token;

		// Krok 3: Arrange - Nadanie uprawnień do wysyłki faktur z załącznikami
		AttachmentPermissionGrantRequest grantRequest = new()
		{
			Nip = sellerNip
		};
		await TestDataClient.EnableAttachmentAsync(grantRequest);

		// Krok 4: Act - Pobranie statusu uprawnienia do załączników z pollingiem
		PermissionsAttachmentAllowedResponse grantedPermissionStatus = await AsyncPollingUtils.PollAsync(
			action: () => KsefClient.GetAttachmentPermissionStatusAsync(accessToken),
			condition: r => r is not null && r.IsAttachmentAllowed == true,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: PermissionPollingMaxAttempts,
			cancellationToken: CancellationToken);

		// Assert - Weryfikacja nadania uprawnienia
		Assert.NotNull(grantedPermissionStatus);
		Assert.True(grantedPermissionStatus.IsAttachmentAllowed,
			"Uprawnienie do wysyłki załączników powinno być aktywne po nadaniu");

		// Krok 5: Arrange - Przygotowanie zaszyfrowanych faktur
		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
			count: TotalInvoicesCount,
			nip: sellerNip,
			templatePath: invoiceTemplatePath);

		(byte[] zipBytes, FileMetadata zipMetadata) =
			BatchUtils.BuildZip(invoices, CryptographyService);

		List<BatchPartSendingInfo> encryptedBatchParts =
			BatchUtils.EncryptAndSplit(zipBytes, encryptionData, CryptographyService, BatchPartQuantity);

		// Act - Otwarcie sesji batch
		OpenBatchSessionRequest openBatchRequest =
			BatchUtils.BuildOpenBatchRequest(zipMetadata, encryptionData, encryptedBatchParts, systemCode);

		OpenBatchSessionResponse batchSession =
			await BatchUtils.OpenBatchAsync(KsefClient, openBatchRequest, accessToken);

		// Act - Wysłanie części batch
		await KsefClient.SendBatchPartsAsync(batchSession, encryptedBatchParts);

		// Act - Zamknięcie sesji batch
		await KsefClient.CloseBatchSessionAsync(batchSession.ReferenceNumber, accessToken);

		// Act - Oczekiwanie na zakończenie przetwarzania
		SessionInvoicesResponse invoicesMetadata = await AsyncPollingUtils.PollAsync(
			async () => await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(
				KsefClient,
				batchSession.ReferenceNumber,
				accessToken).ConfigureAwait(false),
			result => result is not null && result.Invoices is { Count: TotalInvoicesCount },
			delay: TimeSpan.FromSeconds(1),
			cancellationToken: CancellationToken).ConfigureAwait(false);

		Assert.NotNull(invoicesMetadata);

		InvoiceQueryFilters invoiceQueryFilters = new()
		{
			DateRange = new DateRange
			{
				From = DateTime.UtcNow.AddMinutes(-5),
				To = DateTime.UtcNow.AddMinutes(5),
				DateType = DateType.Invoicing
			},
			SubjectType = InvoiceSubjectType.Subject1
		};

		PagedInvoiceResponse invoiceQueryResponse = await KsefClient.QueryInvoiceMetadataAsync(
			requestPayload: invoiceQueryFilters,
			accessToken: accessToken,
			cancellationToken: CancellationToken.None,
			pageOffset: 0,
			pageSize: 30);


		// Krok 6: Assert - Wszystkie faktury z załącznikiem zostały pomyślnie przetworzone
		Assert.Equal(TotalInvoicesCount, invoiceQueryResponse.Invoices.Count);
		Assert.True(invoiceQueryResponse.Invoices.All(x => x.HasAttachment == true),
			"Wszystkie faktury powinny zawierać załącznik");
	}

	/// <summary>
	/// Test negatywny: Weryfikuje że faktury z załącznikami NIE są przetwarzane bez nadanych uprawnień.
	/// Scenariusz:
	/// 1. Utworzenie testowej osoby fizycznej
	/// 2. Uwierzytelnienie w systemie KSeF
	/// 3. Pominięcie nadania uprawnień do załączników
	/// 4. Próba wysłania faktur z załącznikami w trybie batch
	/// 5. Weryfikacja że wszystkie faktury zostały odrzucone
	/// </summary>
	[Theory]
	[InlineData(SystemCode.FA3, "invoice-template-fa-3-with-attachment.xml")]
	public async Task SendInvoiceWithAttachment_WithoutGrantedPermission_ShouldRejectAllInvoices(
		SystemCode systemCode,
		string invoiceTemplatePath)
	{
		// Krok 1: Arrange - Przygotowanie testowej osoby fizycznej
		string sellerNip = MiscellaneousUtils.GetRandomNip();
		string sellerPesel = MiscellaneousUtils.GetRandomPesel();

		PersonCreateRequest personRequest = new PersonCreateRequest
		{
			Nip = sellerNip,
			Pesel = sellerPesel,
			IsBailiff = false,
			Description = "Osoba fizyczna testowa - test negatywny załączników"
		};
		await TestDataClient.CreatePersonAsync(personRequest);

		// Krok 2: Arrange - Uwierzytelnienie sprzedawcy
		AuthenticationOperationStatusResponse authResponse =
			await AuthenticationUtils.AuthenticateAsync(KsefClient, sellerNip);
		string accessToken = authResponse.AccessToken.Token;

		// Krok 3: Act - pominięcie EnableAttachmentAsync
		// Weryfikacja że uprawnienia nie są nadane
		PermissionsAttachmentAllowedResponse grantedPermissionStatus = await AsyncPollingUtils.PollAsync(
			action: () => KsefClient.GetAttachmentPermissionStatusAsync(accessToken),
			condition: r => r is not null && r.IsAttachmentAllowed == false,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: PermissionPollingMaxAttempts,
			cancellationToken: CancellationToken);

		Assert.False(grantedPermissionStatus?.IsAttachmentAllowed ?? false,
			"Uprawnienie do wysyłki załączników nie powinno być aktywne przed rozpoczęciem testu");

		// Krok 4: Arrange - Przygotowanie zaszyfrowanych faktur
		EncryptionData encryptionData = CryptographyService.GetEncryptionData();

		List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
			count: TotalInvoicesCount,
			nip: sellerNip,
			templatePath: invoiceTemplatePath);

		(byte[] zipBytes, FileMetadata zipMetadata) =
			BatchUtils.BuildZip(invoices, CryptographyService);

		List<BatchPartSendingInfo> encryptedBatchParts =
			BatchUtils.EncryptAndSplit(zipBytes, encryptionData, CryptographyService, BatchPartQuantity);

		// Act - Otwarcie sesji batch
		OpenBatchSessionRequest openBatchRequest =
			BatchUtils.BuildOpenBatchRequest(zipMetadata, encryptionData, encryptedBatchParts, systemCode);

		OpenBatchSessionResponse batchSession =
			await BatchUtils.OpenBatchAsync(KsefClient, openBatchRequest, accessToken);

		// Act - Wysłanie części batch
		await KsefClient.SendBatchPartsAsync(batchSession, encryptedBatchParts);

		// Act - Zamknięcie sesji batch
		await KsefClient.CloseBatchSessionAsync(batchSession.ReferenceNumber, accessToken);

		// Act - Oczekiwanie na zakończenie przetwarzania
		SessionInvoicesResponse invoicesMetadata = await AsyncPollingUtils.PollAsync(
			async () => await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(
				KsefClient,
				batchSession.ReferenceNumber,
				accessToken).ConfigureAwait(false),
			result => result is not null && result.Invoices is { Count: TotalInvoicesCount },
			delay: TimeSpan.FromSeconds(1),
			cancellationToken: CancellationToken).ConfigureAwait(false);

		// Krok 5: Assert - Weryfikacja że sesja zakończyła przetwarzanie
		Assert.NotNull(invoicesMetadata);
		Assert.True(invoicesMetadata.Invoices.All(x =>
			x.Status.Code == InvoiceInSessionStatusCodeResponse.InvalidPermissions &&
			x.Status.Description == StatusDescriptionInvalidPermissionScope &&
			x.Status.Details.First() == $"Sprzedawca {sellerNip} nie posiada zgody do wysyłania faktur z załącznikami"),
			"Wszystkie faktury powinny zostać odrzucone z kodem 410 - brak uprawnień do załączników");
	}
}