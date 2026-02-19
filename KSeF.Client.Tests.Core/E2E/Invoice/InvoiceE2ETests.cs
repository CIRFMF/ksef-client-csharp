using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Invoice;

[Collection("InvoicesScenario")]
public class InvoiceE2ETests : TestBase
{
    private const int PageOffset = 0;
    private const int PageSize = 10;
    private const int DateRangeDays = 30;
    private const int PollingIntervalSeconds = 10;
    private const int MinInvoiceCount = 1;
    private const int FromOffsetDays = -1;
    private const int ToOffsetDays = 1;
    private readonly string _sellerNip;
    private string _subjectNip;
    private readonly string _accessToken;
    private string _subjectAccessToken;

    /// <summary>
    /// Konstruktor testów E2E faktur. Ustawia token dostępu na podstawie uwierzytelnienia.
    /// </summary>
    public InvoiceE2ETests()
    {
        _sellerNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse authOperationStatusResponse =
            AuthenticationUtils.AuthenticateAsync(AuthorizationClient, _sellerNip).GetAwaiter().GetResult();
        _accessToken = authOperationStatusResponse.AccessToken.Token;
    }

    /// <summary>
    /// Pobiera metadane faktury na podstawie zapytania.
    /// Kroki:
    /// 1) przygotowanie filtrów,
    /// 2) wykonanie zapytania o metadane,
    /// 3) weryfikacja zakresu dat w wynikach,
    /// 4) weryfikacja stronnicowania.
    /// </summary>
    [Fact]
    public async Task Invoice_GetInvoiceMetadataAsync_ReturnsMetadata()
    {
        // Krok 1: przygotowanie filtrów
        InvoiceQueryFilters invoiceMetadataQueryRequest = new()
        {
            SubjectType = InvoiceSubjectType.Subject1,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddDays(-DateRangeDays),
                To = DateTime.UtcNow,
                DateType = DateType.Issue
            }
        };

        // Krok 2: wykonanie zapytania o metadane
        PagedInvoiceResponse metadata = await KsefClient.QueryInvoiceMetadataAsync(
            requestPayload: invoiceMetadataQueryRequest,
            accessToken: _accessToken,
            cancellationToken: CancellationToken,
            pageOffset: PageOffset,
            pageSize: PageSize);

        // Krok 3: weryfikacja zakresu dat w wynikach
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.Invoices);
        foreach (InvoiceSummary inv in metadata.Invoices)
        {
            DateTime issueDateUtc = inv.IssueDate.UtcDateTime.Date;
            DateTime fromUtcDate = invoiceMetadataQueryRequest.DateRange.From.Date;
            DateTime toUtcDate = invoiceMetadataQueryRequest.DateRange.To.GetValueOrDefault(DateTime.UtcNow).Date;
            Assert.True(issueDateUtc >= fromUtcDate && issueDateUtc <= toUtcDate,
                $"Invoice {inv.KsefNumber} IssueDate {inv.IssueDate} poza zakresem [{invoiceMetadataQueryRequest.DateRange.From}, {invoiceMetadataQueryRequest.DateRange.To}].");
        }

        // Krok 4: weryfikacja stronnicowania
        Assert.InRange(metadata.Invoices.Count, PageOffset, PageSize);
    }

    /// <summary>
    /// Pełny przepływ wysłania i pobrania faktury oraz eksportu.
    /// Kroki:
    /// 1) otwarcie sesji online,
    /// 2) wysłanie faktury,
    /// 3) oczekiwanie na przetworzenie faktur w sesji,
    /// 4) zamknięcie sesji,
    /// 5) pobranie metadanych sesji,
    /// 6) pobranie numeru KSeF pierwszej faktury,
    /// 7) pobranie faktury po numerze KSeF,
    /// 8) przygotowanie zapytania o metadane sprzedażowe (Subject1),
    /// 9) pobranie i weryfikacja metadanych sprzedażowych,
    /// 10) inicjacja eksportu faktur,
    /// 11) oczekiwanie na zakończenie eksportu i weryfikacja paczki.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    public async Task Invoice_GetInvoiceAsync_ReturnsInvoiceXml(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Krok 0: przygotowanie danych szyfrowania
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // Krok 1: otwarcie sesji online
        OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
            KsefClient,
            encryptionData,
            _accessToken,
            systemCode);
        Assert.NotNull(openSessionResponse?.ReferenceNumber);
        Assert.True(openSessionResponse?.ValidUntil <= DateTime.UtcNow.AddDays(1));

        // Krok 2: wysłanie faktury
        SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            _accessToken,
            _sellerNip,
            invoiceTemplatePath,
            encryptionData,
            CryptographyService,
            true);
        Assert.NotNull(sendInvoiceResponse);

        // Krok 3: oczekiwanie na przetworzenie faktur w sesji
        SessionStatusResponse sendInvoiceStatus = await AsyncPollingUtils.PollAsync(
            async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _accessToken).ConfigureAwait(false),
            result => result is not null && result.InvoiceCount == result.SuccessfulInvoiceCount,
            cancellationToken: CancellationToken);
        Assert.NotNull(sendInvoiceStatus);
        Assert.Equal(sendInvoiceStatus.InvoiceCount, sendInvoiceStatus.SuccessfulInvoiceCount);

        // Krok 4: zamknięcie sesji
        await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient,
             openSessionResponse.ReferenceNumber,
             _accessToken);

        // Krok 5: pobranie metadanych sesji
        SessionInvoicesResponse invoicesMetadata = await AsyncPollingUtils.PollAsync(
            async () => await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _accessToken).ConfigureAwait(false),
            result => result is not null && result.Invoices is { Count: > 0 },
            delay: TimeSpan.FromSeconds(PollingIntervalSeconds),
            cancellationToken: CancellationToken);
        Assert.NotNull(invoicesMetadata);
        Assert.NotEmpty(invoicesMetadata.Invoices);
        Assert.Null(invoicesMetadata.ContinuationToken);

        // Krok 6: pobranie numeru KSeF pierwszej faktury
        string ksefInvoiceNumber = invoicesMetadata.Invoices.First().KsefNumber;
        Assert.False(string.IsNullOrWhiteSpace(ksefInvoiceNumber));

        // Krok 7: pobranie faktury po numerze KSeF (dla wystawcy)
        string invoice = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetInvoiceAsync(ksefInvoiceNumber, _accessToken, CancellationToken).ConfigureAwait(false),
            result => !string.IsNullOrWhiteSpace(result),
            cancellationToken: CancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(invoice));

        // Krok 8: przygotowanie zapytania o metadane sprzedażowe (Subject1)
        InvoiceQueryFilters query = new()
        {
            DateRange = new DateRange
            {
                // Użycie DateTime.UtcNow zamiast DateTime.Now — czas lokalny maszyny
                // może różnić się od UTC, co powoduje niespójne wyniki zapytań DateRange
                // (np. runner CI w strefie UTC vs. deweloper w CET/CEST).
                From = DateTime.UtcNow.AddDays(FromOffsetDays),
                To = DateTime.UtcNow.AddDays(ToOffsetDays),
                DateType = DateType.Invoicing
            },
            SubjectType = InvoiceSubjectType.Subject1
        };

        // Krok 9: pobranie i weryfikacja metadanych sprzedażowych
        PagedInvoiceResponse invoicesMetadataForSeller = await KsefClient.QueryInvoiceMetadataAsync(query, _accessToken, cancellationToken: CancellationToken);
        Assert.NotNull(invoicesMetadataForSeller);
        Assert.NotNull(invoicesMetadataForSeller.Invoices);
        foreach (InvoiceSummary inv in invoicesMetadataForSeller.Invoices)
        {
            DateTime invoicingDateUtc = inv.InvoicingDate.UtcDateTime.Date;
            DateTime fromDate = query.DateRange.From.Date;
            DateTime toDate = query.DateRange.To.GetValueOrDefault(DateTime.UtcNow).Date;
            Assert.True(invoicingDateUtc >= fromDate && invoicingDateUtc <= toDate,
                $"Invoice {inv.KsefNumber} InvoicingDate {inv.InvoicingDate} poza zakresem [{query.DateRange.From}, {query.DateRange.To}].");
        }
        Assert.InRange(invoicesMetadataForSeller.Invoices.Count, MinInvoiceCount, PageSize);

        // Krok 10: inicjacja eksportu faktur
        InvoiceExportRequest invoiceExportRequest = new()
        {
            Encryption = encryptionData.EncryptionInfo,
            Filters = query
        };

        OperationResponse invoicesForSellerResponse = await KsefClient.ExportInvoicesAsync(
            invoiceExportRequest,
            _accessToken,
            cancellationToken: CancellationToken);
        Assert.NotNull(invoicesForSellerResponse?.ReferenceNumber);

        // Krok 11: oczekiwanie na zakończenie eksportu i weryfikacja paczki
        InvoiceExportStatusResponse exportStatus = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetInvoiceExportStatusAsync(
                invoicesForSellerResponse.ReferenceNumber,
                _accessToken,
                CancellationToken).ConfigureAwait(false),
            result => result?.Status?.Code == InvoiceExportStatusCodeResponse.ExportSuccess,
            cancellationToken: CancellationToken);
        Assert.NotNull(exportStatus);
        Assert.Equal(InvoiceExportStatusCodeResponse.ExportSuccess, exportStatus.Status.Code);
        Assert.NotNull(exportStatus.Package);
        Assert.NotEmpty(exportStatus.Package.Parts);
    }

    /// <summary>
    /// Przepływ weryfikujący metadane zakupowe (Subject2).
    /// Kroki:
    /// 1) otwarcie sesji online,
    /// 2) wysłanie faktury z nabywcą = sprzedawca,
    /// 3) oczekiwanie na przetworzenie,
    /// 4) zamknięcie sesji,
    /// 5) pobranie metadanych sesji,
    /// 6) pobranie numeru KSeF pierwszej faktury,
    /// 7) przygotowanie zapytania o metadane zakupowe (Subject2),
    /// 8) pobranie i weryfikacja metadanych zakupowych, w tym obecności wysłanej faktury,
    /// 9) weryfikacja kluczowych pól i stronnicowania.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3-with-custom-Subject2.xml")]
    public async Task Invoice_PurchaseMetadataFlow_ValidatesBuyerMetadata(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Krok 0: przygotowanie danych szyfrowania
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        _subjectNip = MiscellaneousUtils.GetRandomNip();

        // Krok 1: otwarcie sesji online
        OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
            KsefClient,
            encryptionData,
            _accessToken,
            systemCode);
        Assert.NotNull(openSessionResponse?.ReferenceNumber);

        // Krok 2: wysłanie faktury z nabywcą = sprzedawca (ten sam NIP)
        SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            _accessToken,
            _sellerNip,
            _subjectNip,
            invoiceTemplatePath,
            encryptionData,
            CryptographyService,
            true);
        Assert.NotNull(sendInvoiceResponse);

        // Krok 3: oczekiwanie na przetworzenie
        SessionStatusResponse sendInvoiceStatus = await AsyncPollingUtils.PollAsync(
            async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _accessToken).ConfigureAwait(false),
            result => result is not null && result.InvoiceCount == result.SuccessfulInvoiceCount,
            delay: TimeSpan.FromSeconds(PollingIntervalSeconds),
            cancellationToken: CancellationToken);
        Assert.NotNull(sendInvoiceStatus);
        Assert.Equal(sendInvoiceStatus.InvoiceCount, sendInvoiceStatus.SuccessfulInvoiceCount);

        // Krok 4: zamknięcie sesji
        await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient,
             openSessionResponse.ReferenceNumber,
             _accessToken);

        // Krok 5: pobranie metadanych sesji
        SessionInvoicesResponse invoicesMetadata = await AsyncPollingUtils.PollAsync(
            async () => await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _accessToken).ConfigureAwait(false),
            result => result is not null && result.Invoices is { Count: > 0 },
            delay: TimeSpan.FromSeconds(PollingIntervalSeconds),
            cancellationToken: CancellationToken);
        Assert.NotNull(invoicesMetadata);
        Assert.NotEmpty(invoicesMetadata.Invoices);

        // Krok 6: pobranie numeru KSeF pierwszej faktury
        string ksefInvoiceNumber = invoicesMetadata.Invoices.First().KsefNumber;
        Assert.False(string.IsNullOrWhiteSpace(ksefInvoiceNumber));


        AuthenticationOperationStatusResponse authOperationStatusResponse =
                    AuthenticationUtils.AuthenticateAsync(AuthorizationClient, _subjectNip).GetAwaiter().GetResult();
        _subjectAccessToken = authOperationStatusResponse.AccessToken.Token;

        // Krok 7: przygotowanie zapytania o metadane zakupowe (Subject2)
        InvoiceQueryFilters query = new()
        {
            DateRange = new DateRange
            {
                // Użycie DateTime.UtcNow zamiast DateTime.Now — zapytania DateRange
                // powinny być w UTC, aby uniknąć niespójności ze strefą czasową runnera.
                From = DateTime.UtcNow.AddDays(FromOffsetDays),
                To = DateTime.UtcNow.AddDays(ToOffsetDays),
                DateType = DateType.Invoicing
            },
            SubjectType = InvoiceSubjectType.Subject2
        };

        // Krok 8: pobranie i weryfikacja metadanych zakupowych, w tym obecności wysłanej faktury
        PagedInvoiceResponse buyerInvoicesMetadata = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.QueryInvoiceMetadataAsync(query, _subjectAccessToken, cancellationToken: CancellationToken).ConfigureAwait(false),
            result => result is not null && result.Invoices is { Count: > 0 } && result.Invoices.Any(i => i.KsefNumber == ksefInvoiceNumber),
            delay: TimeSpan.FromSeconds(PollingIntervalSeconds),
            cancellationToken: CancellationToken);
        Assert.NotNull(buyerInvoicesMetadata);
        Assert.NotNull(buyerInvoicesMetadata.Invoices);
        Assert.Contains(buyerInvoicesMetadata.Invoices, i => i.KsefNumber == ksefInvoiceNumber);

        // Krok 9: weryfikacja kluczowych pól i stronnicowania
        foreach (InvoiceSummary inv in buyerInvoicesMetadata.Invoices)
        {
            DateTime invoicingDateUtc = inv.InvoicingDate.UtcDateTime.Date;
            DateTime fromDate = query.DateRange.From.Date;
            DateTime toDate = query.DateRange.To.GetValueOrDefault(DateTime.UtcNow).Date;
            Assert.True(invoicingDateUtc >= fromDate && invoicingDateUtc <= toDate,
                $"Invoice {inv.KsefNumber} InvoicingDate {inv.InvoicingDate} poza zakresem [{query.DateRange.From}, {query.DateRange.To}].");
            Assert.False(string.IsNullOrWhiteSpace(inv.InvoiceNumber));
        }
        Assert.InRange(buyerInvoicesMetadata.Invoices.Count, MinInvoiceCount, PageSize);
    }
}