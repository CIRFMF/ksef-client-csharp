#nullable enable
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Tests.Utils.Upo;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Http;

namespace KSeF.Client.Tests.Core.E2E.BatchSession;

[Collection("BatchSessionScenario")]
public partial class BatchSessionE2ETests : TestBase
{
    private const int TotalInvoices = 20;
    private const int PartQuantity = 11;
    private const int ExpectedFailedInvoiceCount = 0;
    private const int ExpectedSessionStatusCode = 200;
    private const int ExportMaxAttempts = 45;
    private const int ExportMetadataMaxAttempts = 60;
    private const int TotalInvoices10k = 10_000;
    private const int SessionMaxAttempts10k = 120;
    private const int ExportMaxAttempts10k = 120;
    private const string MetadataEntryName = "_metadata.json";
    private const string XmlFileExtension = ".xml";
    private static readonly TimeSpan ExportPollingDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ExportMetadataPollingDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ExportOperationTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan SessionTimeout10k = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan ExportTimeout10k = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ExportMetadataPollingDelay10k = TimeSpan.FromSeconds(10);

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
    /// End-to-end test weryfikujący pełny, poprawny przebieg przetwarzania sesji wsadowej w KSeF
    /// dla paczki ZIP.
    /// Generuje 20 faktur z szablonu, szyfruje i dzieli paczkę na części, otwiera sesję,
    /// wysyła wszystkie części, zamyka sesję, sprawdza status przetwarzania oraz pobiera UPO
    /// pojedynczej faktury i UPO zbiorcze sesji.
    /// </summary>
    /// <remarks>
    /// Kroki:
    /// 1. Przygotowanie paczki ZIP, szyfrowanie, podział na części i otwarcie sesji.
    /// 2. Wysłanie wszystkich zaszyfrowanych części.
    /// 3. Zamknięcie sesji i oczekiwanie na zakończenie przetwarzania faktur.
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
        OpenBatchSessionResult openResult = await PrepareAndOpenBatchSessionAsync(
            CryptographyService,
            TotalInvoices,
            PartQuantity,
            sellerNip,
            systemCode,
            invoiceTemplatePath,
            accessToken
        );

        await ExecuteBatchSessionFullIntegrationFlowAsync(openResult, TotalInvoices);
    }

    /// <summary>
    /// End-to-end test weryfikujący pełny, poprawny przebieg przetwarzania sesji wsadowej w KSeF
    /// dla paczki TAR.GZ.
    /// Generuje 20 faktur z szablonu, szyfruje i dzieli paczkę na części, otwiera sesję
    /// z CompressionType.TarGz, wysyła wszystkie części, zamyka sesję, sprawdza status przetwarzania
    /// oraz pobiera UPO pojedynczej faktury i UPO zbiorcze sesji.
    /// </summary>
    /// <remarks>
    /// Kroki:
    /// 1. Przygotowanie paczki TAR.GZ, szyfrowanie, podział na części i otwarcie sesji.
    /// 2. Sprawdzenie, że żądanie otwarcia sesji wskazuje CompressionType.TarGz.
    /// 3. Wysłanie wszystkich zaszyfrowanych części.
    /// 4. Zamknięcie sesji i oczekiwanie na zakończenie przetwarzania faktur.
    /// 5. Weryfikacja statusu sesji: SuccessfulInvoiceCount == 20, FailedInvoiceCount == 0, Status.Code == 200; pobranie numeru referencyjnego UPO.
    /// 6. Pobranie dokumentów sesji i zapis pierwszego numeru KSeF.
    /// 7. Pobranie UPO faktury po numerze KSeF.
    /// 8. Pobranie UPO zbiorczego sesji.
    /// </remarks>
    [Theory]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    public async Task BatchSession_WithTarGzInputPackage_ShouldProcessInvoicesAndReturnUpo(
        SystemCode systemCode,
        string invoiceTemplatePath)
    {
        OpenBatchSessionResult openResult = await PrepareAndOpenBatchSessionWithTarGzAsync(
            CryptographyService,
            TotalInvoices,
            PartQuantity,
            sellerNip,
            systemCode,
            invoiceTemplatePath,
            accessToken
        );

        Assert.Equal(CompressionType.TarGz, openResult.OpenBatchSessionRequest.BatchFile.CompressionType);

        await ExecuteBatchSessionFullIntegrationFlowAsync(openResult, TotalInvoices);
    }

    /// <summary>
    /// Sprawdza, czy format paczki użyty przy wysyłce batch nie ogranicza formatu późniejszego eksportu.
    /// Eksport paczki faktur również obsługuje wskazanie typu kompresji przez InvoiceExportRequest.CompressionType.
    /// Dla paczek TAR.GZ ustawiamy CompressionType.TarGz, dla ZIP można jawnie wskazać CompressionType.Zip.
    /// Brak wartości zachowuje domyślną kompatybilność API (ZIP).
    /// </summary>
    /// <remarks>
    /// Kroki:
    /// 1. Wysyła faktury w paczce ZIP albo TAR.GZ, jawnie lub przez domyślny format API.
    /// 2. Czeka, aż faktura będzie dostępna dla eksportu.
    /// 3. Eksportuje ją jako ZIP albo TAR.GZ, jawnie lub przez domyślny format API.
    /// 4. Sprawdza format pobranej paczki po nagłówku pliku.
    /// 5. Sprawdza, czy paczka zawiera _metadata.json i XML faktury z bieżącej sesji.
    /// </remarks>
    [Theory]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", CompressionType.Zip, CompressionType.Zip)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", CompressionType.Zip, CompressionType.TarGz)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", CompressionType.Zip, null)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", CompressionType.TarGz, CompressionType.Zip)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", CompressionType.TarGz, CompressionType.TarGz)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", CompressionType.TarGz, null)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", null, CompressionType.Zip)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", null, CompressionType.TarGz)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", null, null)]
    public async Task BatchSession_ShouldExportInvoicePackageWithRequestedOrDefaultCompression(
        SystemCode systemCode,
        string invoiceTemplatePath,
        CompressionType? inputCompressionType,
        CompressionType? exportCompressionType)
    {
        OpenBatchSessionResult openResult = await PrepareAndOpenBatchSessionAsync(
            CryptographyService,
            TotalInvoices,
            PartQuantity,
            sellerNip,
            systemCode,
            invoiceTemplatePath,
            accessToken,
            inputCompressionType);

        Assert.Equal(inputCompressionType, openResult.OpenBatchSessionRequest.BatchFile.CompressionType);

        await ExecuteBatchSessionFullIntegrationFlowAsync(openResult, TotalInvoices);

        await VerifyInvoiceExportPackageAsync(CryptographyService.GetEncryptionData(), exportCompressionType);
    }

    /// <summary>
    /// Eksportuje fakturę utworzoną w bieżącej sesji wsadowej i sprawdza zawartość paczki eksportu.
    /// Widoczność faktury w metadanych jest sprawdzana osobno, bo zakończenie sesji wsadowej
    /// nie zawsze oznacza natychmiastową dostępność faktury dla query/export.
    /// </summary>
    private async Task VerifyInvoiceExportPackageAsync(
        EncryptionData encryptionData,
        CompressionType? exportCompressionType)
    {
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        timeoutCts.CancelAfter(ExportOperationTimeout);
        CancellationToken exportCancellationToken = timeoutCts.Token;

        InvoiceQueryFilters query = new()
        {
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddDays(-1),
                To = DateTime.UtcNow.AddDays(1),
                DateType = DateType.Invoicing
            },
            SubjectType = InvoiceSubjectType.Subject1,
            KsefNumber = ksefNumber
        };

        await WaitForInvoiceVisibleForExportAsync(query, exportCancellationToken).ConfigureAwait(false);

        InvoiceExportRequest invoiceExportRequest = new()
        {
            Encryption = encryptionData.EncryptionInfo,
            CompressionType = exportCompressionType,
            Filters = query
        };

        OperationResponse exportResponse = await KsefClient.ExportInvoicesAsync(
            invoiceExportRequest,
            accessToken,
            cancellationToken: exportCancellationToken).ConfigureAwait(false);
        Assert.NotNull(exportResponse?.ReferenceNumber);

        InvoiceExportStatusResponse exportStatus = await WaitForInvoiceExportFinishedAsync(
            exportResponse.ReferenceNumber,
            exportCancellationToken).ConfigureAwait(false);

        Assert.Equal(
            InvoiceExportStatusCodeResponse.ExportSuccess,
            exportStatus.Status.Code);
        Assert.NotNull(exportStatus.Package);
        Assert.True(exportStatus.Package.InvoiceCount > 0, $"Eksport faktur powinien zawierać fakturę {ksefNumber}.");
        Assert.NotEmpty(exportStatus.Package.Parts);

        Assert.False(
            exportStatus.Package.IsTruncated,
            $"Paczka eksportu nie powinna być obcięta (IsTruncated=true) dla {exportStatus.Package.InvoiceCount} faktur. " +
            $"LastPermanentStorageDate={exportStatus.Package.LastPermanentStorageDate}, " +
            $"PermanentStorageHwmDate={exportStatus.Package.PermanentStorageHwmDate}");

        Assert.Null(exportStatus.Package.LastPermanentStorageDate);
        Assert.Null(exportStatus.Package.PermanentStorageHwmDate);

        using MemoryStream decryptedStream = await BatchUtils.DownloadAndDecryptPackagePartsAsync(
            exportStatus.Package.Parts,
            encryptionData,
            CryptographyService,
            cancellationToken: exportCancellationToken).ConfigureAwait(false);

        CompressionType actualCompressionType = await DetectPackageCompressionTypeAsync(decryptedStream, exportCancellationToken).ConfigureAwait(false);
        // Gdy CompressionType nie jest podany, API zwraca paczkę ZIP (zachowanie wstecznie kompatybilne).
        CompressionType expectedCompressionType = exportCompressionType ?? CompressionType.Zip;
        Assert.Equal(expectedCompressionType, actualCompressionType);

        Dictionary<string, string> packageFiles = await UnpackExportPackageAsync(
            decryptedStream,
            actualCompressionType,
            exportCancellationToken).ConfigureAwait(false);
        VerifyExportPackageContent(packageFiles);
    }

    private async Task WaitForInvoiceVisibleForExportAsync(
        InvoiceQueryFilters query,
        CancellationToken cancellationToken,
        TimeSpan? pollingDelay = null,
        int? maxAttempts = null)
    {
        await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.QueryInvoiceMetadataAsync(
                query,
                accessToken,
                pageSize: 10,
                cancellationToken: cancellationToken).ConfigureAwait(false),
            result => result?.Invoices is not null
                && result.Invoices.Any(invoice => string.Equals(invoice.KsefNumber, ksefNumber, StringComparison.OrdinalIgnoreCase)),
            description: $"Faktura {ksefNumber} powinna być widoczna w metadanych przed eksportem.",
            delay: pollingDelay ?? ExportMetadataPollingDelay,
            maxAttempts: maxAttempts ?? ExportMetadataMaxAttempts,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Czeka na zakończenie eksportu. Polling kończy się na pierwszym statusie terminalnym,
    /// a osobna asercja sprawdza dopiero, czy był to sukces.
    /// </summary>
    private async Task<InvoiceExportStatusResponse> WaitForInvoiceExportFinishedAsync(
        string referenceNumber,
        CancellationToken cancellationToken)
    {
        return await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetInvoiceExportStatusAsync(
                referenceNumber,
                accessToken,
                cancellationToken).ConfigureAwait(false),
            IsInvoiceExportFinished,
            description: $"Eksport faktur {referenceNumber} powinien zakończyć się statusem terminalnym.",
            delay: ExportPollingDelay,
            maxAttempts: ExportMaxAttempts,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Dla testu status inny niż ExportInProgress traktujemy jako terminalny.
    /// Dzięki temu błąd eksportu kończy test od razu, zamiast dopiero po limicie czasu.
    /// </summary>
    private static bool IsInvoiceExportFinished(InvoiceExportStatusResponse? response)
    {
        return response?.Status?.Code is not null
            && response.Status.Code != InvoiceExportStatusCodeResponse.ExportInProgress;
    }

    /// <summary>
    /// Sprawdza zawartość odszyfrowanej i rozpakowanej paczki eksportu:
    /// metadane muszą wskazywać fakturę z bieżącej sesji, a paczka musi zawierać jej XML.
    /// </summary>
    private void VerifyExportPackageContent(Dictionary<string, string> packageFiles)
    {
        Assert.NotEmpty(packageFiles);

        Assert.True(
            packageFiles.TryGetValue(MetadataEntryName, out string? metadataJson),
            $"Paczka eksportu powinna zawierać {MetadataEntryName}.");
        Assert.False(string.IsNullOrWhiteSpace(metadataJson));

        InvoicePackageMetadata metadata = JsonUtil.Deserialize<InvoicePackageMetadata>(metadataJson);

        Assert.NotNull(metadata.Invoices);
        Assert.Contains(metadata.Invoices, invoice => string.Equals(invoice.KsefNumber, ksefNumber, StringComparison.OrdinalIgnoreCase));

        KeyValuePair<string, string>[] invoiceXmlFiles = packageFiles
            .Where(file => file.Key.EndsWith(XmlFileExtension, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(invoiceXmlFiles);
        Assert.Contains(invoiceXmlFiles, file => !string.IsNullOrWhiteSpace(file.Value));
        Assert.Contains(invoiceXmlFiles, file => file.Value.Contains(sellerNip));
    }

    /// <summary>
    /// Wykrywa format odszyfrowanej paczki eksportu po nagłówku pliku.
    /// </summary>
    private static async Task<CompressionType> DetectPackageCompressionTypeAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        byte[] magic = new byte[2];
#if NETFRAMEWORK
        int read = await stream.ReadAsync(magic, 0, 2, cancellationToken).ConfigureAwait(false);
#else
        int read = await stream.ReadAsync(magic.AsMemory(0, 2), cancellationToken).ConfigureAwait(false);
#endif
        stream.Position = 0;

        if (read == 2 && magic[0] == 0x1F && magic[1] == 0x8B)
        {
            return CompressionType.TarGz;
        }

        if (read == 2 && magic[0] == 0x50 && magic[1] == 0x4B)
        {
            return CompressionType.Zip;
        }

        throw new InvalidOperationException("Nieznany format paczki eksportu.");
    }

    /// <summary>
    /// Rozpakowuje odszyfrowaną paczkę eksportu jako TAR.GZ albo ZIP.
    /// Format eksportu sprawdzamy po nagłówku pliku, ponieważ nie musi być taki sam jak format wejściowej paczki batch.
    /// </summary>
    private static async Task<Dictionary<string, string>> UnpackExportPackageAsync(
        Stream stream,
        CompressionType compressionType,
        CancellationToken cancellationToken)
    {
        stream.Position = 0;

        return compressionType == CompressionType.TarGz
            ? await BatchUtils.UnzipTarGzAsync(stream, cancellationToken).ConfigureAwait(false)
            : await BatchUtils.UnzipAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteBatchSessionFullIntegrationFlowAsync(
        OpenBatchSessionResult openResult,
        int expectedInvoiceCount)
    {
        // Asercje kroku 1
        Assert.NotNull(openResult);
        Assert.False(string.IsNullOrWhiteSpace(openResult.ReferenceNumber));
        Assert.NotNull(openResult.OpenBatchSessionResponse);
        Assert.False(string.IsNullOrWhiteSpace(openResult.OpenBatchSessionResponse.ReferenceNumber));
        Assert.NotNull(openResult.OpenBatchSessionResponse.PartUploadRequests);

        foreach (PackagePartSignatureInitResponseType? part in openResult.OpenBatchSessionResponse.PartUploadRequests)
        {
            Assert.True(!string.IsNullOrWhiteSpace(part.Method));
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
        await KsefClient.SendBatchPartsAsync(openBatchSessionResponse, encryptedParts).ConfigureAwait(false);

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
        ).ConfigureAwait(false);
        
        // 4. Status sesji
        SessionStatusResponse statusResponse = await AsyncPollingUtils.PollWithBackoffAsync(
                                action: () => KsefClient.GetSessionStatusAsync(batchSessionReferenceNumber!, accessToken),
                                condition: s => s.Status.Code is ExpectedSessionStatusCode,
                                initialDelay: TimeSpan.FromSeconds(1),
                                maxDelay: TimeSpan.FromSeconds(5),
                                maxAttempts: 30,
                                cancellationToken: CancellationToken).ConfigureAwait(false);
        
    

        Assert.NotNull(statusResponse);
        Assert.True(statusResponse.SuccessfulInvoiceCount == expectedInvoiceCount);
        Assert.Equal(ExpectedFailedInvoiceCount, statusResponse.FailedInvoiceCount);
        Assert.NotNull(statusResponse.Upo);
        Assert.NotNull(statusResponse.Upo.Pages);
        // Porównanie z DateTime.UtcNow — data wygaśnięcia URL UPO z API jest w UTC.
        // DateTime.Now zwraca czas lokalny maszyny, co może dawać fałszywe wyniki
        // (np. +2h w CEST -> asercja przepuszcza większy zakres niż zamierzony).
        Assert.True(statusResponse.Upo.Pages.First().DownloadUrlExpirationDate < DateTime.UtcNow.AddDays(4));
        Assert.NotNull(statusResponse.Upo.Pages.First().DownloadUrl);
        Assert.False(string.IsNullOrWhiteSpace(statusResponse.Upo.Pages.First().ReferenceNumber));
        Assert.NotNull(statusResponse.ValidUntil);
        Assert.Equal(ExpectedSessionStatusCode, statusResponse.Status.Code);

        upoReferenceNumber = statusResponse.Upo.Pages.First().ReferenceNumber;

        // 5. Dokumenty sesji
        SessionInvoicesResponse documents = await BatchUtils.GetSessionInvoicesAsync(KsefClient, batchSessionReferenceNumber!, accessToken, expectedInvoiceCount).ConfigureAwait(false);

        Assert.NotNull(documents);
        Assert.Null(documents.ContinuationToken);
        Assert.NotEmpty(documents.Invoices);
        Assert.Equal(expectedInvoiceCount, documents.Invoices.Count);

        ksefNumber = documents.Invoices.First().KsefNumber;

        // 6. Pobranie UPO faktury z URL zawartego w metadanych faktury
        Uri upoDownloadUrl = documents.Invoices.First().UpoDownloadUrl;
        string invoiceUpoXml = await UpoUtils.GetUpoAsync(KsefClient, upoDownloadUrl).ConfigureAwait(false);
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
        ).ConfigureAwait(false);
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
        string accessToken,
        CompressionType? compressionType = null)
    {
        EncryptionData encryptionData = cryptographyService.GetEncryptionData();

        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            count: invoiceCount,
            nip: sellerNip,
            templatePath: invoiceTemplatePath);

        CompressionType packageCompressionType = compressionType ?? CompressionType.Zip;

        (byte[] packageBytes, FileMetadata packageMetadata) = packageCompressionType == CompressionType.TarGz
            ? BatchUtils.BuildTarGz(invoices, cryptographyService)
            : BatchUtils.BuildZip(invoices, cryptographyService);

        List<BatchPartSendingInfo> encryptedParts =
            BatchUtils.EncryptAndSplit(packageBytes, encryptionData, cryptographyService, partQuantity);

        OpenBatchSessionRequest openBatchRequest = compressionType.HasValue
            ? BatchUtils.BuildOpenBatchRequest(
                packageMetadata,
                encryptionData,
                encryptedParts,
                systemCode,
                SystemCodeHelper.GetSchemaVersion(systemCode),
                SystemCodeHelper.GetValue(systemCode),
                compressionType.Value)
            : BatchUtils.BuildOpenBatchRequest(packageMetadata, encryptionData, encryptedParts, systemCode);

        OpenBatchSessionResponse openBatchSessionResponse =
            await BatchUtils.OpenBatchAsync(KsefClient, openBatchRequest, accessToken).ConfigureAwait(false);

        return new OpenBatchSessionResult(
            openBatchSessionResponse.ReferenceNumber,
            openBatchRequest,
            openBatchSessionResponse,
            encryptedParts
        );
    }

    private async Task<OpenBatchSessionResult> PrepareAndOpenBatchSessionWithTarGzAsync(
        ICryptographyService cryptographyService,
        int invoiceCount,
        int partQuantity,
        string sellerNip,
        SystemCode systemCode,
        string invoiceTemplatePath,
        string accessToken)
    {
        return await PrepareAndOpenBatchSessionAsync(
            cryptographyService,
            invoiceCount,
            partQuantity,
            sellerNip,
            systemCode,
            invoiceTemplatePath,
            accessToken,
            CompressionType.TarGz).ConfigureAwait(false);
    }

    /// <summary>
    /// Eksport paczki dokładnie 10 000 faktur nie powinien zwracać IsTruncated=true.
    /// </summary>
    /// <remarks>
    /// Kroki:
    /// 1. Wysyła 10 000 faktur w paczce TAR.GZ przez sesję wsadową.
    /// 2. Czeka na zakończenie przetwarzania sesji.
    /// 3. Eksportuje faktury z żądaniem formatu TAR.GZ.
    /// 4. Weryfikuje, że IsTruncated=false, a LastPermanentStorageDate i PermanentStorageHwmDate są null.
    /// </remarks>
    [Theory]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", CompressionType.TarGz, CompressionType.TarGz)]
    public async Task BatchSession_ExportOf10kInvoices_ShouldNotBeTruncated(
        SystemCode systemCode,
        string invoiceTemplatePath,
        CompressionType? inputCompressionType,
        CompressionType? exportCompressionType)
    {
        // 1. Wysyła 10 000 faktur w paczce TAR.GZ przez sesję wsadową.
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            count: TotalInvoices10k,
            nip: sellerNip,
            templatePath: invoiceTemplatePath);

        (byte[] packageBytes, FileMetadata packageMetadata) = inputCompressionType == CompressionType.TarGz
            ? BatchUtils.BuildTarGz(invoices, CryptographyService)
            : BatchUtils.BuildZip(invoices, CryptographyService);

        List<BatchPartSendingInfo> encryptedParts =
            BatchUtils.EncryptAndSplit(packageBytes, encryptionData, CryptographyService);

        OpenBatchSessionRequest openBatchRequest = BatchUtils.BuildOpenBatchRequest(
            packageMetadata,
            encryptionData,
            encryptedParts,
            systemCode,
            SystemCodeHelper.GetSchemaVersion(systemCode),
            SystemCodeHelper.GetValue(systemCode),
            inputCompressionType!.Value);

        OpenBatchSessionResponse openBatchSessionResponse =
            await BatchUtils.OpenBatchAsync(KsefClient, openBatchRequest, accessToken);

        batchSessionReferenceNumber = openBatchSessionResponse.ReferenceNumber;

        await KsefClient.SendBatchPartsAsync(openBatchSessionResponse, encryptedParts);

        await AsyncPollingUtils.PollAsync(
            action: async () =>
            {
                await BatchUtils.CloseBatchAsync(KsefClient, batchSessionReferenceNumber, accessToken).ConfigureAwait(false);
                return true;
            },
            condition: closed => closed,
            delay: TimeSpan.FromSeconds(2),
            maxAttempts: 30,
            shouldRetryOnException: _ => true,
            cancellationToken: CancellationToken);

        // 2. Czeka na zakończenie przetwarzania sesji.
        using CancellationTokenSource sessionCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        sessionCts.CancelAfter(SessionTimeout10k);

        SessionStatusResponse statusResponse = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetSessionStatusAsync(batchSessionReferenceNumber, accessToken, sessionCts.Token).ConfigureAwait(false),
            s => s?.Status?.Code == ExpectedSessionStatusCode,
            delay: TimeSpan.FromSeconds(5),
            maxAttempts: SessionMaxAttempts10k,
            cancellationToken: sessionCts.Token);

        Assert.NotNull(statusResponse);
        Assert.Equal(TotalInvoices10k, statusResponse.SuccessfulInvoiceCount);
        Assert.Equal(ExpectedFailedInvoiceCount, statusResponse.FailedInvoiceCount);

        // 3. Eksportuje faktury z żądaniem formatu TAR.GZ.
        using CancellationTokenSource exportCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        exportCts.CancelAfter(ExportTimeout10k);

        SessionInvoicesResponse documents = await BatchUtils.GetSessionInvoicesAsync(
            KsefClient, batchSessionReferenceNumber, accessToken, pageSize: 10);
        ksefNumber = documents.Invoices.First().KsefNumber;

        DateRange exportDateRange = new()
        {
            From = DateTime.UtcNow.AddDays(-1),
            To = DateTime.UtcNow.AddDays(1),
            DateType = DateType.Invoicing
        };

        InvoiceQueryFilters visibilityQuery = new()
        {
            DateRange = exportDateRange,
            SubjectType = InvoiceSubjectType.Subject1,
            KsefNumber = ksefNumber
        };

        await WaitForInvoiceVisibleForExportAsync(
            visibilityQuery,
            exportCts.Token,
            pollingDelay: ExportMetadataPollingDelay10k,
            maxAttempts: ExportMaxAttempts10k);

        InvoiceQueryFilters exportQuery = new()
        {
            DateRange = exportDateRange,
            SubjectType = InvoiceSubjectType.Subject1
        };

        InvoiceExportRequest invoiceExportRequest = new()
        {
            Encryption = encryptionData.EncryptionInfo,
            CompressionType = exportCompressionType,
            Filters = exportQuery
        };

        OperationResponse exportResponse = await KsefClient.ExportInvoicesAsync(
            invoiceExportRequest,
            accessToken,
            cancellationToken: exportCts.Token);
        Assert.NotNull(exportResponse?.ReferenceNumber);

        InvoiceExportStatusResponse exportStatus = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetInvoiceExportStatusAsync(
                exportResponse.ReferenceNumber,
                accessToken,
                exportCts.Token).ConfigureAwait(false),
            IsInvoiceExportFinished,
            description: $"Eksport 10k faktur {exportResponse.ReferenceNumber} powinien zakończyć się statusem terminalnym.",
            delay: TimeSpan.FromSeconds(5),
            maxAttempts: ExportMaxAttempts10k,
            cancellationToken: exportCts.Token);

        // 4. Weryfikuje, że IsTruncated=false, a LastPermanentStorageDate i PermanentStorageHwmDate są null.
        Assert.Equal(InvoiceExportStatusCodeResponse.ExportSuccess, exportStatus.Status.Code);
        Assert.NotNull(exportStatus.Package);
        Assert.True(
            exportStatus.Package.InvoiceCount >= TotalInvoices10k,
            $"Eksport powinien zawierać co najmniej {TotalInvoices10k} faktur, zwrócił {exportStatus.Package.InvoiceCount}.");
        Assert.NotEmpty(exportStatus.Package.Parts);

        Assert.False(
            exportStatus.Package.IsTruncated,
            $"Paczka eksportu 10 000 faktur nie powinna być obcięta (IsTruncated=true). " +
            $"InvoiceCount={exportStatus.Package.InvoiceCount}, " +
            $"LastPermanentStorageDate={exportStatus.Package.LastPermanentStorageDate}, " +
            $"PermanentStorageHwmDate={exportStatus.Package.PermanentStorageHwmDate}");

        Assert.Null(exportStatus.Package.LastPermanentStorageDate);
        Assert.Null(exportStatus.Package.PermanentStorageHwmDate);
    }
}
