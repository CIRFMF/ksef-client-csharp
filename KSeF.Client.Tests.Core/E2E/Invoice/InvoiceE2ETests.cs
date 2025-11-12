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
    private const int MaxRetries = 60;
    
    private readonly string _sellerNip;
    private readonly string _accessToken;

    /// <summary>
    /// Konstruktor testów E2E dla faktur. Ustawia token dostępu na podstawie uwierzytelnienia.
    /// </summary>
    public InvoiceE2ETests()
    {
        _sellerNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse authOperationStatusResponse =
            AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, _sellerNip).GetAwaiter().GetResult();
        _accessToken = authOperationStatusResponse.AccessToken.Token;
    }

    /// <summary>
    /// Pobiera metadane faktury na podstawie zapytania i sprawdza czy odpowiedź nie jest pusta.
    /// </summary>
    [Fact]
    public async Task Invoice_GetInvoiceMetadataAsync_ReturnsMetadata()
    {
        // Arrange
        InvoiceQueryFilters invoiceMetadataQueryRequest = new InvoiceQueryFilters
        {
            SubjectType = InvoiceSubjectType.Subject1,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddDays(-DateRangeDays),
                To = DateTime.UtcNow,
                DateType = DateType.Issue
            }
        };

        // Act
        PagedInvoiceResponse metadata = await KsefClient.QueryInvoiceMetadataAsync(
            requestPayload: invoiceMetadataQueryRequest,
            accessToken: _accessToken,
            cancellationToken: CancellationToken,
            pageOffset: PageOffset,
            pageSize: PageSize);

        // Assert
        Assert.NotNull(metadata);
    }

    [Theory]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    public async Task Invoice_GetInvoiceAsync_ReturnsInvoiceXml(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Arrange
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // 1. Rozpocznij sesję online
        OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
            KsefClient,
            encryptionData,
            _accessToken,
            systemCode);

        Assert.NotNull(openSessionResponse?.ReferenceNumber);

        // 2. Wyślij fakturę
        SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            _accessToken,
            _sellerNip,
            invoiceTemplatePath,
            encryptionData,
            CryptographyService);

        Assert.NotNull(sendInvoiceResponse);

        // 3. Czekaj aż sesja przetworzy wszystkie faktury (Successful == Total)
        SessionStatusResponse sendInvoiceStatus = await AsyncPollingUtils.PollAsync(
            async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _accessToken),
            result => result is not null && result.InvoiceCount == result.SuccessfulInvoiceCount,
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxRetries,
            cancellationToken: CancellationToken);

        Assert.NotNull(sendInvoiceStatus);
        Assert.Equal(sendInvoiceStatus.InvoiceCount, sendInvoiceStatus.SuccessfulInvoiceCount);

        // 4. Zamknij sesję
        await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient,
             openSessionResponse.ReferenceNumber,
             _accessToken);

        // 5. Czekaj aż metadane sesji będą dostępne i niepuste
        SessionInvoicesResponse invoicesMetadata = await AsyncPollingUtils.PollAsync(
            async () => await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _accessToken),
            result => result is not null && result.Invoices is { Count: > 0 },
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxRetries,
            cancellationToken: CancellationToken);

        Assert.NotNull(invoicesMetadata);
        Assert.NotEmpty(invoicesMetadata.Invoices);

        // 6. Pobierz numer pierwszej faktury z listy metadanych
        string ksefInvoiceNumber = invoicesMetadata.Invoices.First().KsefNumber;
        Assert.False(string.IsNullOrWhiteSpace(ksefInvoiceNumber));

        // 7. Pobierz fakturę po jej numerze KSeF - dostępne tylko dla wystawcy faktury (sprzedawcy)
        string invoice = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetInvoiceAsync(ksefInvoiceNumber, _accessToken, CancellationToken),
            result => !string.IsNullOrWhiteSpace(result),
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxRetries,
            cancellationToken: CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(invoice));

        // 8. Przygotuj zapytanie o faktury
        InvoiceQueryFilters query = new InvoiceQueryFilters
        {
            DateRange = new DateRange
            {
                From = DateTime.Now.AddDays(-1),
                To = DateTime.Now.AddDays(1),
                DateType = DateType.Invoicing
            },
            SubjectType = InvoiceSubjectType.Subject1
        };

        // 9. Pobierz metadane faktury
        PagedInvoiceResponse invoicesMetadataForSeller = await KsefClient.QueryInvoiceMetadataAsync(query, _accessToken, cancellationToken: CancellationToken);
        Assert.NotNull(invoicesMetadataForSeller);

        // 10. Zainicjuj eksport faktur
        InvoiceExportRequest invoiceExportRequest = new InvoiceExportRequest
        {
            Encryption = encryptionData.EncryptionInfo,
            Filters = query
        };

        OperationResponse invoicesForSellerResponse = await KsefClient.ExportInvoicesAsync(
            invoiceExportRequest,
            _accessToken,
            CancellationToken);

        Assert.NotNull(invoicesForSellerResponse?.ReferenceNumber);

        // 11. Czekaj na zakończenie eksportu (status 200)
        InvoiceExportStatusResponse exportStatus = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetInvoiceExportStatusAsync(
                invoicesForSellerResponse.ReferenceNumber,
                _accessToken,
                CancellationToken),
            result => result?.Status?.Code == InvoiceExportStatusCodeResponse.ExportSuccess,
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxRetries * 10,
            cancellationToken: CancellationToken);

        Assert.NotNull(exportStatus);
        Assert.Equal(InvoiceExportStatusCodeResponse.ExportSuccess, exportStatus.Status.Code);
        Assert.NotNull(exportStatus.Package);
        Assert.NotEmpty(exportStatus.Package.Parts);
    }
}