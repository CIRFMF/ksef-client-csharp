#nullable enable
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Invoice
{
	[Collection("InvoicesScenario")]
	public class InvoiceDateRangeE2ETests : TestBase
	{
		private const int InvoiceCount = 5;
		private string _accessToken;
		private string _sellerNip;

		public InvoiceDateRangeE2ETests()
		{
			_sellerNip = MiscellaneousUtils.GetRandomNip();

			AuthenticationOperationStatusResponse authOperationStatusResponse =
				AuthenticationUtils.AuthenticateAsync(AuthorizationClient, _sellerNip).GetAwaiter().GetResult();
			_accessToken = authOperationStatusResponse.AccessToken.Token;
		}

		/// <summary>
		/// Test weryfikujący zachowanie API QueryInvoiceMetadataAsync w zależności od sposobu utworzenia DateTimeOffset w .NET.
		/// 
		/// SPECYFIKACJA API KSeF - DateRange:
		/// Typ i zakres dat, według którego filtrowane są faktury. Maksymalny dozwolony okres wynosi 3 miesiące w strefie UTC lub w strefie Europe/Warsaw (WAW).
		/// 
		/// Format daty:
		/// * Daty muszą być przekazane w formacie ISO 8601, np. `yyyy-MM-ddTHH:mm:ss`.
		/// * Dopuszczalne są następujące warianty:
		///   * z sufiksem `Z` (czas UTC),
		///   * z jawnym offsetem, np. `+01:00`, `+03:00`,
		///   * bez offsetu (interpretowane jako czas lokalny strefy Europe/Warsaw).
		/// 
		/// JAK API INTERPRETUJE DATY:
		/// API zawsze konwertuje otrzymane daty na UTC przed porównaniem z datami faktur:
		/// * "2026-02-12T10:00:00Z" → 10:00:00 UTC (bez zmian)
		/// * "2026-02-12T11:00:00+01:00" → 10:00:00 UTC (odejmuje offset 1h)
		/// * "2026-02-12T12:00:00+02:00" → 10:00:00 UTC (odejmuje offset 2h)
		/// * "2026-02-12T10:00:00" (bez offsetu) → interpretowane jako czas lokalny strefy Europe/Warsaw
		/// 
		/// WŁAŚCIWOŚCI DateRange W .NET:
		/// DateRange.From i DateRange.To są typu DateTimeOffset.
		/// DateTimeOffset składa się z: DateTime (wartość czasu) + TimeSpan (offset od UTC).
		/// 
		/// POPRAWNE PODEJŚCIA (zwrócą faktury):
		/// 1) DateTimeOffset.UtcNow
		///    → wysyła: "2026-02-12T10:00:00+00:00"
		///    → API interpretuje: 10:00:00 UTC
		/// 
		/// 2) DateTimeOffset.Now
		///    → wysyła: "2026-02-12T11:00:00+01:00" (w Warsaw)
		///    → API interpretuje: 10:00:00 UTC (poprawnie konwertuje)
		///
		/// 3) new DateTimeOffset(DateTime.Now.Date, local_offset)
		///    → zakres od północy lokalnej do teraz (poprawnie)
		/// Kroki:
		/// 1) wysłanie 5 faktur przez pomocniczą metodę,
		/// 2) weryfikacja różnych sposobów utworzenia DateTimeOffset,
		/// 3) sprawdzenie które podejścia zwracają faktury a które nie.
		/// </summary>
		[Theory]
		[InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", DateType.Invoicing)]
		[InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", DateType.PermanentStorage)]
		public async Task QueryInvoiceMetadata_WithVariousDateTimeOffsetFormats_ShowsOffsetImpact(
			SystemCode systemCode,
			string invoiceTemplatePath,
			DateType dateType)
		{
			// Krok 1: wysłanie 5 faktur przez pomocniczą metodę
			SessionInvoicesResponse invoicesMetadata = await SendInvoicesAndWaitForProcessingAsync(systemCode, invoiceTemplatePath);

			// Przygotowanie listy oczekiwanych numerów KSeF z wysłanych faktur
			HashSet<string> expectedKsefNumbers = invoicesMetadata.Invoices
				.Select(inv => inv.KsefNumber)
				.ToHashSet();

			// Krok 2: weryfikacja różnych sposobów utworzenia DateTimeOffset

			// PRZYPADEK 1: DateTimeOffset.UtcNow (POPRAWNE - ZALECANE)
			// Zakres: 5 minut temu do teraz w UTC
			// Klient wysyła: "2026-02-12T09:55:00+00:00" do "2026-02-12T10:00:00+00:00"
			// API interpretuje: 09:55:00 UTC do 10:00:00 UTC
			// Rezultat: Zwróci 5 faktur
			InvoiceQueryFilters queryDateTimeOffsetUtcNow = new()
			{
				DateRange = new DateRange
				{
					From = DateTimeOffset.UtcNow.AddMinutes(-5),
					To = DateTimeOffset.UtcNow.AddMinutes(5),
					DateType = dateType
				},
				SubjectType = InvoiceSubjectType.Subject1
			};

			PagedInvoiceResponse responseDateTimeOffsetUtcNow = await KsefClient.QueryInvoiceMetadataAsync(
				queryDateTimeOffsetUtcNow,
				_accessToken,
				cancellationToken: CancellationToken);

			Assert.NotNull(responseDateTimeOffsetUtcNow);
			Assert.NotEmpty(responseDateTimeOffsetUtcNow.Invoices);
			Assert.True(responseDateTimeOffsetUtcNow.Invoices.Count >= 5);

			VerifyReturnedInvoicesMatchSent(responseDateTimeOffsetUtcNow.Invoices, expectedKsefNumbers);
			VerifyInvoicesInDateRange(responseDateTimeOffsetUtcNow.Invoices, queryDateTimeOffsetUtcNow.DateRange);


			// PRZYPADEK 2: DateTimeOffset.Now (POPRAWNE)
			// Zakres: 5 minut temu do teraz w czasie lokalnym z lokalnym offsetem
			// W Warsaw (UTC+1): "2026-02-12T10:55:00+01:00" do "2026-02-12T11:00:00+01:00"
			// Klient wysyła: z offsetem +01:00
			// API interpretuje: 09:55:00 UTC do 10:00:00 UTC
			// Rezultat: Zwróci 5 faktur
			InvoiceQueryFilters queryDateTimeOffsetNow = new()
			{
				DateRange = new DateRange
				{
					From = DateTimeOffset.Now.AddMinutes(-5),
					To = DateTimeOffset.Now.AddMinutes(5),
					DateType = dateType
				},
				SubjectType = InvoiceSubjectType.Subject1
			};

			PagedInvoiceResponse responseDateTimeOffsetNow = await KsefClient.QueryInvoiceMetadataAsync(
				queryDateTimeOffsetNow,
				_accessToken,
				cancellationToken: CancellationToken);

			Assert.NotNull(responseDateTimeOffsetNow);
			Assert.NotEmpty(responseDateTimeOffsetNow.Invoices);
			Assert.True(responseDateTimeOffsetNow.Invoices.Count >= 5);

			VerifyReturnedInvoicesMatchSent(responseDateTimeOffsetNow.Invoices, expectedKsefNumbers);
			VerifyInvoicesInDateRange(responseDateTimeOffsetNow.Invoices, queryDateTimeOffsetNow.DateRange);

			// PRZYPADEK 3: new DateTimeOffset(DateTime.Now.Date, lokalny offset) (POPRAWNE dla zakresu dziennego)
			// Zakres: od północy lokalnej (z poprawnym offsetem) do teraz
			// DateTime.Now.Date to 00:00:00 lokalnie, z offsetem +01:00
			// Klient wysyła wysyła: "2026-02-12T00:00:00+01:00"
			// API interpretuje: 2026-02-11T23:00:00 UTC (poprzedni dzień)
			// To jest POPRAWNE dla zakresu "od początku dnia lokalnego"
			// Rezultat: Zwróci wszystkie faktury od północy lokalnej
			TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

			InvoiceQueryFilters queryDateTimeNowDateWithLocalOffset = new()
			{
				DateRange = new DateRange
				{
					From = new DateTimeOffset(DateTime.Now.Date, localOffset),
					To = DateTimeOffset.Now.AddMinutes(5),
					DateType = dateType
				},
				SubjectType = InvoiceSubjectType.Subject1
			};

			PagedInvoiceResponse responseDateTimeNowDateWithLocalOffset = await KsefClient.QueryInvoiceMetadataAsync(
				queryDateTimeNowDateWithLocalOffset,
				_accessToken,
				cancellationToken: CancellationToken);

			Assert.NotNull(responseDateTimeNowDateWithLocalOffset);
			Assert.NotEmpty(responseDateTimeNowDateWithLocalOffset.Invoices);
			Assert.True(responseDateTimeNowDateWithLocalOffset.Invoices.Count >= 5);

			VerifyReturnedInvoicesMatchSent(responseDateTimeNowDateWithLocalOffset.Invoices, expectedKsefNumbers);
			VerifyInvoicesInDateRange(responseDateTimeNowDateWithLocalOffset.Invoices, queryDateTimeNowDateWithLocalOffset.DateRange);
		}

		/// <summary>
		/// Otwarcie sesji, wysyłka faktur oraz oczekiwanie na ich przetworzenie.
		/// Kroki:
		/// 1) przygotowanie danych szyfrowania,
		/// 2) otwarcie sesji online,
		/// 3) wysłanie podanej liczby faktur,
		/// 4) oczekiwanie na przetworzenie faktur w sesji,
		/// 5) zamknięcie sesji,
		/// 6) pobranie metadanych sesji.
		/// </summary>
		private async Task<SessionInvoicesResponse> SendInvoicesAndWaitForProcessingAsync(
			SystemCode systemCode,
			string invoiceTemplatePath)
		{
			// Krok 1: przygotowanie danych szyfrowania
			EncryptionData encryptionData = CryptographyService.GetEncryptionData();

			// Krok 2: otwarcie sesji online
			OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
				KsefClient,
				encryptionData,
				_accessToken,
				systemCode).ConfigureAwait(false);

			Assert.NotNull(openSessionResponse?.ReferenceNumber);

			// Krok 3: wysłanie podanej liczby faktur
			for (int i = 0; i < InvoiceCount; i++)
			{
				SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
					KsefClient,
					openSessionResponse.ReferenceNumber,
					_accessToken,
					_sellerNip,
					invoiceTemplatePath,
					encryptionData,
					CryptographyService,
					true).ConfigureAwait(false);

				Assert.NotNull(sendInvoiceResponse);
			}

			// Krok 4: oczekiwanie na przetworzenie faktur w sesji
			SessionStatusResponse sendInvoiceStatus = await AsyncPollingUtils.PollAsync(
				async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
					KsefClient,
					openSessionResponse.ReferenceNumber,
					_accessToken).ConfigureAwait(false),
				result => result is not null && result.InvoiceCount == InvoiceCount && result.SuccessfulInvoiceCount == InvoiceCount,
				cancellationToken: CancellationToken).ConfigureAwait(false);

			Assert.NotNull(sendInvoiceStatus);
			Assert.Equal(InvoiceCount, sendInvoiceStatus.InvoiceCount);
			Assert.Equal(InvoiceCount, sendInvoiceStatus.SuccessfulInvoiceCount);

			// Krok 5: zamknięcie sesji
			await OnlineSessionUtils.CloseOnlineSessionAsync(
				KsefClient,
				openSessionResponse.ReferenceNumber,
				_accessToken).ConfigureAwait(false);

			// Krok 6: pobranie metadanych sesji
			SessionInvoicesResponse invoicesMetadata = await AsyncPollingUtils.PollAsync(
				async () => await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(
					KsefClient,
					openSessionResponse.ReferenceNumber,
					_accessToken).ConfigureAwait(false),
				result => result is not null && result.Invoices is { Count: InvoiceCount },
				delay: TimeSpan.FromSeconds(1),
				cancellationToken: CancellationToken).ConfigureAwait(false);

			Assert.NotNull(invoicesMetadata);
			Assert.Equal(InvoiceCount, invoicesMetadata.Invoices.Count);

			return invoicesMetadata;
		}

		/// <summary>
		/// Weryfikuje czy wszystkie faktury mieszczą się w podanym zakresie dat.
		/// </summary>
		private void VerifyInvoicesInDateRange(ICollection<InvoiceSummary> invoices, DateRange dateRange)
		{
			DateTimeOffset from = dateRange.From;
			DateTimeOffset to = dateRange.To ?? DateTimeOffset.UtcNow;

			// porównuje w UTC (jedna skala czasu)
			DateTimeOffset fromUtc = from.ToUniversalTime();
			DateTimeOffset toUtc = to.ToUniversalTime();

			foreach (InvoiceSummary inv in invoices)
			{
				Assert.NotNull(inv.InvoicingDate);

				DateTimeOffset invoiceUtc = inv.InvoicingDate.ToUniversalTime();

				Assert.True(
					invoiceUtc >= fromUtc && invoiceUtc <= toUtc,
					$"Faktura {inv.KsefNumber} z datą {invoiceUtc:O} jest poza zakresem [{fromUtc:O}, {toUtc:O}]");
			}
		}

		/// <summary>
		/// Weryfikuje czy zwrócone faktury zawierają wszystkie oczekiwane numery KSeF.
		/// </summary>
		/// <param name="returnedInvoices">Faktury zwrócone przez QueryInvoiceMetadataAsync</param>
		/// <param name="expectedKsefNumbers">Oczekiwane numery KSeF faktur wysłanych w sesji</param>
		private void VerifyReturnedInvoicesMatchSent(
			ICollection<InvoiceSummary> returnedInvoices,
			HashSet<string> expectedKsefNumbers)
		{
			HashSet<string> returnedKsefNumbers = returnedInvoices
				.Select(inv => inv.KsefNumber)
				.ToHashSet();

			// Sprawdź czy wszystkie wysłane faktury zostały zwrócone
			foreach (string expectedKsefNumber in expectedKsefNumbers)
			{
				Assert.Contains(expectedKsefNumber, returnedKsefNumbers);
			}

			// Opcjonalnie: sprawdź czy nie ma duplikatów
			Assert.Equal(returnedInvoices.Count, returnedKsefNumbers.Count);
		}
	}
}