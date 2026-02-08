#nullable enable
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Tests.Utils.Upo;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Sessions.BatchSession;

namespace KSeF.Client.Tests.Core.E2E.BatchSession;

[Collection("BatchSessionScenario")]
public partial class BatchSessionE2ETests : TestBase
{
    private const int TotalInvoices = 20;
    private const int PartQuantity = 11;
    private const int ExpectedFailedInvoiceCount = 0;
    private const int ExpectedSessionStatusCode = 200;

    private readonly string accessToken = string.Empty;
    private readonly string sellerNip = string.Empty;

    private string batchSessionReferenceNumber;
    private string ksefNumber;
    private string upoReferenceNumber;
    private OpenBatchSessionResponse? openBatchSessionResponse;
    private List<BatchPartSendingInfo>? encryptedParts;

    public BatchSessionE2ETests()
    {
        // Autoryzacja do testów – jednorazowa, dane zapisane w readonly properties
        string nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, nip)
            .GetAwaiter().GetResult();

        accessToken = authInfo.AccessToken.Token;
        sellerNip = nip;
    }

    /// <summary>
    /// End-to-end test weryfikujący pełny, poprawny przebieg przetwarzania sesji wsadowej w KSeF.
    /// Generuje 20 faktur z szablonu, szyfruje i dzieli paczkę na części, otwiera sesję,
    /// wysyła wszystkie części, zamyka sesję, sprawdza status przetwarzania oraz pobiera UPO
    /// pojedynczej faktury i UPO zbiorcze sesji.
    /// </summary>
    /// <remarks>
    /// Kroki:
    /// 1. Przygotowanie paczki (ZIP, szyfrowanie, podział) i otwarcie sesji; zapis numeru referencyjnego.
    /// 2. Wysłanie wszystkich zaszyfrowanych części i krótka pauza.
    /// 3. Zamknięcie sesji i dłuższa pauza na zakończenie przetwarzania.
    /// 4. Weryfikacja statusu sesji: SuccessfulInvoiceCount == 20, FailedInvoiceCount == 0, Status.Code == 200; pobranie numeru referencyjnego UPO.
    /// 5. Pobranie dokumentów sesji i zapis pierwszego numeru KSeF.
    /// 6. Pobranie UPO faktury po numerze KSeF.
    /// 7. Pobranie UPO zbiorczego sesji.
    /// </remarks>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    public async Task BatchSessionFullIntegrationFlowReturnsUpo(SystemCode systemCode, string invoiceTemplatePath)
    {
        // 1. Przygotowanie paczki i otwarcie sesji
        OpenBatchSessionResult openResult = await PrepareAndOpenBatchSessionAsync(
            CryptographyService,
            TotalInvoices,
            PartQuantity,
            sellerNip,
            systemCode,
            invoiceTemplatePath,
            accessToken
        );

        // Asercje kroku 1
        Assert.NotNull(openResult);
        Assert.False(string.IsNullOrWhiteSpace(openResult.ReferenceNumber));
        Assert.NotNull(openResult.OpenBatchSessionResponse);
        Assert.False(string.IsNullOrWhiteSpace(openResult.OpenBatchSessionResponse.ReferenceNumber));
        Assert.NotNull(openResult.OpenBatchSessionResponse.PartUploadRequests);

        foreach (PackagePartSignatureInitResponseType? part in openResult.OpenBatchSessionResponse.PartUploadRequests)
        {
            Assert.True(!string.IsNullOrWhiteSpace(part.Method));
            Assert.NotNull(part.OrdinalNumber);
            Assert.NotNull(part.Url);
            Assert.True(!string.IsNullOrWhiteSpace(part.Method));
            Assert.NotNull(part.Headers);
        }

        Assert.NotNull(openResult.EncryptedParts);
        Assert.NotEmpty(openResult.EncryptedParts);

        batchSessionReferenceNumber = openResult.ReferenceNumber;
        openBatchSessionResponse = openResult.OpenBatchSessionResponse;
        encryptedParts = openResult.EncryptedParts;

        // 2. Wysłanie wszystkich części
        await KsefClient.SendBatchPartsAsync(openBatchSessionResponse, encryptedParts);

        // 3. Zamknięcie sesji – zamiast stałego opóźnienia użyjemy pollingu aż zamknięcie powiedzie się
        Assert.False(string.IsNullOrWhiteSpace(batchSessionReferenceNumber));
        await AsyncPollingUtils.PollAsync(
            action: async () =>
            {
                await BatchUtils.CloseBatchAsync(KsefClient, batchSessionReferenceNumber!, accessToken).ConfigureAwait(false);
                return true; // jeśli dotarliśmy tutaj, zamknięcie się powiodło
            },
            condition: closed => closed,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 30,
            shouldRetryOnException: _ => true, // ponawiaj przy dowolnym wyjątku
            cancellationToken: CancellationToken
        );
        
        // 4. Status sesji
        SessionStatusResponse statusResponse = await AsyncPollingUtils.PollWithBackoffAsync(
                                action: () => KsefClient.GetSessionStatusAsync(batchSessionReferenceNumber!, accessToken),
                                condition: s => s.Status.Code is ExpectedSessionStatusCode,
                                initialDelay: TimeSpan.FromSeconds(1),
                                maxDelay: TimeSpan.FromSeconds(5),
                                maxAttempts: 30,
                                cancellationToken: CancellationToken);
        
    

        Assert.NotNull(statusResponse);
        Assert.True(statusResponse.SuccessfulInvoiceCount == TotalInvoices);
        Assert.Equal(ExpectedFailedInvoiceCount, statusResponse.FailedInvoiceCount);
        Assert.NotNull(statusResponse.Upo);
        Assert.NotNull(statusResponse.Upo.Pages);
        Assert.True(statusResponse.Upo.Pages.First().DownloadUrlExpirationDate < DateTime.Now.AddDays(4));
        Assert.NotNull(statusResponse.Upo.Pages.First().DownloadUrl);
        Assert.False(string.IsNullOrWhiteSpace(statusResponse.Upo.Pages.First().ReferenceNumber));
        Assert.NotNull(statusResponse.ValidUntil);
        Assert.Equal(ExpectedSessionStatusCode, statusResponse.Status.Code);

        upoReferenceNumber = statusResponse.Upo.Pages.First().ReferenceNumber;

        // 5. Dokumenty sesji
        SessionInvoicesResponse documents = await BatchUtils.GetSessionInvoicesAsync(KsefClient, batchSessionReferenceNumber!, accessToken, TotalInvoices);

        Assert.NotNull(documents);
        Assert.Null(documents.ContinuationToken);
        Assert.NotEmpty(documents.Invoices);
        Assert.Equal(TotalInvoices, documents.Invoices.Count);

        ksefNumber = documents.Invoices.First().KsefNumber;

        // 6. pobranie UPO faktury z URL zawartego w metadanych faktury
        Uri upoDownloadUrl = documents.Invoices.First().UpoDownloadUrl;
        string invoiceUpoXml = await UpoUtils.GetUpoAsync(KsefClient, upoDownloadUrl);
        Assert.False(string.IsNullOrWhiteSpace(invoiceUpoXml));
        InvoiceUpoV4_3 invoiceUpo = UpoUtils.UpoParse<InvoiceUpoV4_3>(invoiceUpoXml);
        Assert.Equal(invoiceUpo.Document.KSeFDocumentNumber, ksefNumber);
        Assert.True(!string.IsNullOrWhiteSpace(invoiceUpo.ReceivingEntityName));
        Assert.True(!string.IsNullOrWhiteSpace(invoiceUpo.SessionReferenceNumber));
        Assert.NotNull(invoiceUpo.Authentication);
        Assert.True(!string.IsNullOrWhiteSpace(invoiceUpo.LogicalStructureName));
        Assert.True(!string.IsNullOrWhiteSpace(invoiceUpo.FormCode));
        Assert.NotNull(invoiceUpo.Signature);
        Assert.Equal(invoiceUpo.Document.SellerNip, sellerNip);

        // 7. Pobranie UPO zbiorczego sesji
        string sessionUpo = await KsefClient.GetSessionUpoAsync(
            batchSessionReferenceNumber!,
            upoReferenceNumber!,
            accessToken,
            CancellationToken
        );
        Assert.False(string.IsNullOrWhiteSpace(sessionUpo));
    }

    /// <summary>
    /// Generuje faktury z szablonu (Templates/invoice-template-fa-{x}.xml), buduje ZIP, szyfruje i dzieli paczkę na części
    /// Zwraca numer referencyjny sesji, odpowiedź otwarcia sesji i listę zaszyfrowanych części.
    /// </summary>
    private async Task<OpenBatchSessionResult> PrepareAndOpenBatchSessionAsync(
        ICryptographyService cryptographyService,
        int invoiceCount,
        int partQuantity,
        string sellerNip,
        SystemCode systemCode,
        string invoiceTemplatePath,
        string accessToken)
    {
        EncryptionData encryptionData = cryptographyService.GetEncryptionData();

        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            count: invoiceCount,
            nip: sellerNip,
            templatePath: invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMeta) =
            BatchUtils.BuildZip(invoices, cryptographyService);

        List<BatchPartSendingInfo> encryptedParts =
            BatchUtils.EncryptAndSplit(zipBytes, encryptionData, cryptographyService, partQuantity);

        OpenBatchSessionRequest openBatchRequest =
            BatchUtils.BuildOpenBatchRequest(zipMeta, encryptionData, encryptedParts, systemCode);

        OpenBatchSessionResponse openBatchSessionResponse =
            await BatchUtils.OpenBatchAsync(KsefClient, openBatchRequest, accessToken).ConfigureAwait(false);

        return new OpenBatchSessionResult(
            openBatchSessionResponse.ReferenceNumber,
            openBatchSessionResponse,
            encryptedParts
        );
    }
}