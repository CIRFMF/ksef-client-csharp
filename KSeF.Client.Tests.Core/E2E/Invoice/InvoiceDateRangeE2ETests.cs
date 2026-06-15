#nullable enable
using System.Globalization;
using System.Text;
using System.Xml.Linq;
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
        /// * "2026-02-12T10:00:00Z" - 10:00:00 UTC (bez zmian)
        /// * "2026-02-12T11:00:00+01:00" - 10:00:00 UTC (odejmuje offset 1h)
        /// * "2026-02-12T12:00:00+02:00" - 10:00:00 UTC (odejmuje offset 2h)
        /// * "2026-02-12T10:00:00" (bez offsetu) - interpretowane jako czas lokalny strefy Europe/Warsaw
        /// 
        /// WŁAŚCIWOŚCI DateRange W .NET:
        /// DateRange.From i DateRange.To są typu DateTimeOffset.
        /// DateTimeOffset składa się z: DateTime (wartość czasu) + TimeSpan (offset od UTC).
        /// 
        /// POPRAWNE PODEJŚCIA (zwrócą faktury):
        /// 1) DateTimeOffset.UtcNow
        ///     wysyła: "2026-02-12T10:00:00+00:00"
        ///     API interpretuje: 10:00:00 UTC
        /// 
        /// 2) DateTimeOffset.Now
        ///     wysyła: "2026-02-12T11:00:00+01:00" (Warszawa)
        ///     API interpretuje: 10:00:00 UTC (poprawnie konwertuje)
        ///
        /// 3) new DateTimeOffset(DateTime.Now.Date, local_offset)
        ///     zakres od północy lokalnej do teraz (poprawnie)
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
        /// Edge-case walidacji DateRange (maks 3 miesiące) + faktury z kontrolowaną datą P_1.
        /// 
        /// Kluczowe: aby test miał sens E2E (a nie tylko walidacja requestu), tworzymy faktury z P_1 ustawionym
        /// tak, żeby mieściły się w danym zakresie (dla przypadków PASS).
        /// 
        /// A) PASS: zakres tuż poniżej 3 miesięcy (UTC) -> wysyłamy faktury z P_1 wewnątrz tego zakresu -> query powinno zwrócić nasze faktury.
        /// B) FAIL: zakres tuż powyżej 3 miesięcy (UTC) -> query powinno polecieć wyjątkiem (walidacja po stronie klienta/serwera).
        /// C) PASS: UTC minimalnie > 3 miesiące, ale PL <= 3 miesiące -> wysyłamy faktury z P_1 wewnątrz -> query powinno zwrócić nasze faktury.
        /// D) FAIL: przekracza 3 miesiące w UTC i PL -> query powinno polecieć wyjątkiem.
        /// </summary>
        [Theory]
        [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", DateType.Invoicing)]
        [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", DateType.PermanentStorage)]
        public async Task QueryInvoiceMetadata_DateRange_ThreeMonths_EdgeCases(
            SystemCode systemCode,
            string invoiceTemplatePath,
            DateType dateType)
        {
            DateTime today = DateTime.UtcNow.Date;
            await SendInvoicesWithIssueDateAndWaitForProcessingAsync(systemCode, invoiceTemplatePath, today).ConfigureAwait(false);
            // Give backend time to index invoices for metadata queries
            await Task.Delay(SleepTime).ConfigureAwait(false);

            DateTimeOffset from = today;
            DateTimeOffset to = from.AddMonths(3).AddSeconds(-1);
            InvoiceQueryFilters filtersA = new InvoiceQueryFilters
            {
                DateRange = new DateRange { From = from, To = to, DateType = dateType },
                SubjectType = InvoiceSubjectType.Subject1
            };
            PagedInvoiceResponse respA = await KsefClient.QueryInvoiceMetadataAsync(filtersA, _accessToken, cancellationToken: CancellationToken).ConfigureAwait(false);

            Assert.NotNull(respA);
            Assert.NotEmpty(respA.Invoices);
            VerifyInvoicesInDateRange(respA.Invoices, filtersA.DateRange);

            DateTimeOffset toB = from.AddMonths(3).AddTicks(1);
            InvoiceQueryFilters filtersB = new InvoiceQueryFilters
            {
                DateRange = new DateRange { From = from, To = toB, DateType = dateType },
                SubjectType = InvoiceSubjectType.Subject1
            };

            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await KsefClient.QueryInvoiceMetadataAsync(filtersB, _accessToken, cancellationToken: CancellationToken).ConfigureAwait(false);
            });

            DateTimeOffset toC = from.AddMonths(3).AddMinutes(-1);
            InvoiceQueryFilters filtersC = new InvoiceQueryFilters
            {
                DateRange = new DateRange { From = from, To = toC, DateType = dateType },
                SubjectType = InvoiceSubjectType.Subject1
            };

            PagedInvoiceResponse respC = await KsefClient.QueryInvoiceMetadataAsync(filtersC, _accessToken, cancellationToken: CancellationToken).ConfigureAwait(false);
            Assert.NotNull(respC);
            Assert.NotEmpty(respC.Invoices);
            VerifyInvoicesInDateRange(respC.Invoices, filtersC.DateRange);

            DateTimeOffset toD = from.AddMonths(3).AddDays(1);
            InvoiceQueryFilters filtersD = new InvoiceQueryFilters
            {
                DateRange = new DateRange { From = from, To = toD, DateType = dateType },
                SubjectType = InvoiceSubjectType.Subject1
            };

            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await KsefClient.QueryInvoiceMetadataAsync(filtersD, _accessToken, cancellationToken: CancellationToken).ConfigureAwait(false);
            });
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
        /// Wysyła InvoiceCount faktur, ale najpierw generuje ich XML z kontrolowaną datą P_1 (yyyy-MM-dd).
        /// Dzięki temu testy DateRange mają "deterministyczne" dane do pobrania.
        /// </summary>
        private async Task<SessionInvoicesResponse> SendInvoicesWithIssueDateAndWaitForProcessingAsync(
            SystemCode systemCode,
            string invoiceTemplatePath,
            DateTime issueDate)
        {
            EncryptionData encryptionData = CryptographyService.GetEncryptionData();

            OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
                KsefClient,
                encryptionData,
                _accessToken,
                systemCode).ConfigureAwait(false);

            Assert.NotNull(openSessionResponse?.ReferenceNumber);

            for (int i = 0; i < InvoiceCount; i++)
            {
                string invoiceXmlTemplate = InvoiceDateRangeE2ETests.PrepareInvoiceFromTemplate(invoiceTemplatePath, _sellerNip);
                string invoiceXmlWithDate = ReplaceXmlElementValue(
                    invoiceXmlTemplate,
                    "P_1",
                    issueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                // If PermanentStorage, also set P_60 to match issueDate
                if (invoiceTemplatePath.Contains("fa-3") && invoiceXmlWithDate.Contains("<P_60>"))
                {
                    invoiceXmlWithDate = ReplaceXmlElementValue(
                        invoiceXmlWithDate,
                        "P_60",
                        issueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                string generatedPath = GenerateInvoiceTemplateWithIssueDate(invoiceTemplatePath, invoiceXmlWithDate);
                SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
                    KsefClient,
                    openSessionResponse.ReferenceNumber,
                    _accessToken,
                    _sellerNip,
                    generatedPath,
                    encryptionData,
                    CryptographyService,
                    true).ConfigureAwait(false);
                Assert.NotNull(sendInvoiceResponse);
            }

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

            await OnlineSessionUtils.CloseOnlineSessionAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _accessToken).ConfigureAwait(false);

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
        /// Przygotowanie faktury z szablonu:
        /// - wczytanie xml z Templates
        /// - podstawienie #nip#
        /// - podstawienie #invoice_number# (unikalny numer)
        /// </summary>
        private static string PrepareInvoiceFromTemplate(string invoiceTemplateFileName, string taxpayerNip)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Templates", invoiceTemplateFileName);
            string xml = File.ReadAllText(path, Encoding.UTF8);
            xml = xml.Replace("#nip#", taxpayerNip);
            xml = xml.Replace("#invoice_number#", $"{Guid.NewGuid()}");
            return xml;
        }

        /// <summary>
        /// Podmienia wartość elementu XML po LocalName (ignorując namespace), np. P_1.
        /// Zwraca string xml (bez formatowania).
        /// </summary>
        private static string ReplaceXmlElementValue(string xml, string elementLocalName, string newValue)
        {
            XDocument doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);

            XElement? element = doc
                .Descendants()
                .FirstOrDefault(e => string.Equals(e.Name.LocalName, elementLocalName, StringComparison.Ordinal));

            if (element is null)
                throw new InvalidOperationException($"Nie znaleziono elementu XML '{elementLocalName}' do podmiany.");

            element.Value = newValue;

            // DisableFormatting żeby nie rozjechać whitespace w szablonie (czasem podpis/format jest wrażliwy)
            return doc.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Zapisuje wygenerowany XML do tymczasowego pliku i zwraca ścieżkę.
        /// OnlineSessionUtils.SendInvoiceAsync w tym projekcie przyjmuje ścieżkę do pliku XML.
        /// </summary>
        private static string GenerateInvoiceTemplateWithIssueDate(string originalTemplateFileName, string invoiceXml)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ksef-e2e");
            Directory.CreateDirectory(tempDir);

            string fileName = $"{Path.GetFileNameWithoutExtension(originalTemplateFileName)}-{Guid.NewGuid():N}.xml";
            string path = Path.Combine(tempDir, fileName);

            File.WriteAllText(path, invoiceXml, Encoding.UTF8);
            return path;
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