using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Tests.Core.Utils.RateLimit;
using KSeF.Client.Tests.Utils;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KSeF.Client.Tests.Core.E2E.Invoice;

/// <summary>
/// Scenariusz E2E prezentujący wzorcowy mechanizm przyrostowego pobierania faktur z KSeF.
/// Kroki:
/// 1. Przygotowanie przykładowej paczki faktur poprzez sesję wsadową (batch session)
/// 2. Okienkowy eksport faktur (windowing) z obsługą limitów i retry po HTTP 429
/// 3. Pobranie i odszyfrowanie pakietów eksportu przy użyciu CryptographyService
/// 4. De-duplikacja faktur na podstawie pliku _metadata.json znajdującego się w paczkach
/// </summary>
[Collection("InvoicesScenario")]
public class IncrementalInvoiceRetrievalE2ETests : TestBase
{
    private const int InvoicesToCreate = 10;
    private const int MaxExportStatusRetries = 60;
    private const int SuccessStatusCode = 200;
    private static readonly TimeSpan ExportPollingDelay = TimeSpan.FromSeconds(2);
    private const string MetadataEntryName = "_metadata.json";
    private const string XmlFileExtension = ".xml";
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) }
    };

    private readonly string _accessToken;
    private readonly string _sellerNip;
    private readonly Dictionary<string, EncryptionData> _exportEncryptionByOperation = new(StringComparer.OrdinalIgnoreCase);

    public IncrementalInvoiceRetrievalE2ETests()
    {
        _sellerNip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse authOperationStatusResponse = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, SignatureService, _sellerNip)
            .GetAwaiter()
            .GetResult();

        _accessToken = authOperationStatusResponse.AccessToken.Token;
    }

    [Theory]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    public async Task IncrementalInvoiceRetrieval_E2E_WithDeduplication(SystemCode systemCode, string invoiceTemplatePath)
    {
        // 1. Generowanie faktur poprzez sesję wsadową w celu uzyskania danych do eksportu
        DateTime batchCreationStart = DateTime.UtcNow;
        (string sessionReferenceNumber, HashSet<string> expectedKsefNumbers) = 
            await CreateInvoicesViaBatchSessionAsync(InvoicesToCreate, _sellerNip, invoiceTemplatePath, systemCode);
        DateTime batchCreationCompleted = DateTime.UtcNow;

        // Kolekcje do deduplikacji oraz weryfikacji rezultatów
        Dictionary<string, InvoiceSummary> uniqueInvoices = new(StringComparer.OrdinalIgnoreCase);
        bool hasDuplicates = false;
        int totalMetadataEntries = 0;

        // Słownik do śledzenia punktu kontynuacji dla każdego SubjectType
        Dictionary<InvoiceSubjectType, DateTime?> continuationPoints = new();

        // 2. Budowanie listy okien czasowych. Zachodzą na siebie celowo w celu wymuszenia konieczności deduplikacji.
        IReadOnlyList<(DateTime From, DateTime To)> windows = BuildIncrementalWindows(batchCreationStart, batchCreationCompleted);

        // Tworzenie planu eksportu - krotki (okno czasowe, typ podmiotu)
        IEnumerable<InvoiceSubjectType> subjectTypes = Enum.GetValues<InvoiceSubjectType>().Where(x => x != InvoiceSubjectType.SubjectAuthorized);
        IOrderedEnumerable<ExportTask> exportTasks = windows
            .SelectMany(window => subjectTypes, (window, subjectType) => new ExportTask(window.From, window.To, subjectType))
            .OrderBy(task => task.From)
            .ThenBy(task => task.SubjectType);

        await Task.Delay(120000);

        foreach (ExportTask task in exportTasks)
        {
            DateTime effectiveFrom = GetEffectiveStartDate(continuationPoints, task.SubjectType, task.From);

            OperationResponse? exportResponse = await InitiateInvoiceExportAsync(effectiveFrom, task.To, task.SubjectType);
            if (exportResponse == null || string.IsNullOrWhiteSpace(exportResponse.ReferenceNumber))
            {
                continue;
            }

            InvoiceExportStatusResponse? exportStatus = await WaitForExportCompletionAsync(exportResponse.ReferenceNumber);
            if (exportStatus?.Package?.Parts == null || exportStatus.Package.Parts.Count == 0)
            {
                continue;
            }

            EncryptionData encryptionData = GetEncryptionDataForOperation(exportResponse.ReferenceNumber);
            PackageProcessingResult packageResult = await DownloadAndProcessPackageAsync(exportStatus.Package, encryptionData);

            totalMetadataEntries += packageResult.MetadataSummaries.Count;

            // Dodawanie unikalnych faktur i wykrywanie duplikatów
            hasDuplicates = packageResult.MetadataSummaries
                .DistinctBy(s => s.KsefNumber, StringComparer.OrdinalIgnoreCase)
                .Any(summary => !uniqueInvoices.TryAdd(summary.KsefNumber, summary));

            // Obsługa flagi isTruncated - aktualizacja punktu kontynuacji, jeśli paczka została obcięta.
            // Zakres okna pozostaje bez zmian, aktualizowany jest tylko punkt kontynuacji
            // na LastPermanentStorageDate z paczki, jeśli jest obcięta.
            // Zapewnia to, że następne okno zacznie się od miejsca,
            // gdzie poprzednie zostało przerwane, a nie od początku okna.
            UpdateContinuationPointIfNeeded(continuationPoints, task.SubjectType, exportStatus.Package);

            _exportEncryptionByOperation.Remove(exportResponse.ReferenceNumber);
        }

        
        // Weryfikacja, że wszystkie utworzone faktury zostały znalezione w eksporcie
        HashSet<string> missingInvoices = new(expectedKsefNumbers, StringComparer.OrdinalIgnoreCase);
        missingInvoices.ExceptWith(uniqueInvoices.Keys);
        Assert.Empty(missingInvoices);

        // Weryfikacja, że metadane zawierają co najmniej tyle wpisów, co unikalne faktury (może być więcej przez duplikaty)
        Assert.True(totalMetadataEntries >= uniqueInvoices.Count, 
            $"Metadane powinny zawierać co najmniej {uniqueInvoices.Count} wpisów, znaleziono: {totalMetadataEntries}");
        
        // Weryfikacja, że deduplikacja faktycznie była potrzebna (przez nakładające się okna czasowe)
        Assert.True(hasDuplicates, "Oczekiwano wykrycia duplikatów z powodu nakładających się okien czasowych");
    }

    private async Task<(string SessionReferenceNumber, HashSet<string> KsefNumbers)> CreateInvoicesViaBatchSessionAsync(
        int invoiceCount, 
        string sellerNip, 
        string invoiceTemplatePath, 
        SystemCode systemCode)
    {
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            invoiceCount,
            sellerNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);

        // wylicza optymalną liczbę części (max 100MB każda)
        List<BatchPartSendingInfo> encryptedParts =
            BatchUtils.EncryptAndSplit(zipBytes, encryptionData, CryptographyService);

        OpenBatchSessionRequest openRequest = BatchUtils.BuildOpenBatchRequest(zipMetadata, encryptionData, encryptedParts, systemCode);

        OpenBatchSessionResponse openResponse = await BatchUtils.OpenBatchAsync(KsefClient, openRequest, _accessToken);
        Assert.False(string.IsNullOrWhiteSpace(openResponse?.ReferenceNumber));

        await KsefClient.SendBatchPartsAsync(openResponse, encryptedParts, CancellationToken);

        await AsyncPollingUtils.PollAsync(
            action: async () =>
            {
                await BatchUtils.CloseBatchAsync(KsefClient, openResponse.ReferenceNumber, _accessToken);
                return true;
            },
            condition: closed => closed,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 10,
            shouldRetryOnException: _ => true,
            cancellationToken: CancellationToken);

        SessionStatusResponse batchStatus = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.GetSessionStatusAsync(openResponse.ReferenceNumber, _accessToken, CancellationToken),
            condition: status => status?.Status?.Code == SuccessStatusCode && status.SuccessfulInvoiceCount == invoiceCount,
            delay: TimeSpan.FromSeconds(2),
            maxAttempts: MaxExportStatusRetries,
            cancellationToken: CancellationToken);

        Assert.NotNull(batchStatus);
        Assert.Equal(invoiceCount, batchStatus.SuccessfulInvoiceCount);
        Assert.Equal(0, batchStatus.FailedInvoiceCount);

        // Pobranie numerów KSeF utworzonych faktur z sesji wsadowej
        SessionInvoicesResponse sessionInvoices = await KsefClient.GetSessionInvoicesAsync(
            openResponse.ReferenceNumber, 
            _accessToken, 
            pageSize: invoiceCount,
            cancellationToken: CancellationToken);

        HashSet<string> ksefNumbers = new(StringComparer.OrdinalIgnoreCase);
        if (sessionInvoices?.Invoices != null)
        {
            foreach (SessionInvoice? invoice in sessionInvoices.Invoices)
            {
                if (!string.IsNullOrWhiteSpace(invoice.KsefNumber))
                {
                    ksefNumbers.Add(invoice.KsefNumber);
                }
            }
        }

        Assert.Equal(invoiceCount, ksefNumbers.Count);

        return (openResponse.ReferenceNumber, ksefNumbers);
    }

    private async Task<OperationResponse?> InitiateInvoiceExportAsync(DateTime windowFromUtc, DateTime windowToUtc, InvoiceSubjectType subjectType)
    {
        if (windowToUtc <= windowFromUtc)
        {
            return null;
        }

        EncryptionData exportEncryption = CryptographyService.GetEncryptionData();

        InvoiceQueryFilters filters = new()
        {
            SubjectType = subjectType,
            DateRange = new DateRange
            {
                DateType = DateType.PermanentStorage,
                From = windowFromUtc,
                To = windowToUtc
            }
        };

        InvoiceExportRequest request = new()
        {
            Filters = filters,
            Encryption = exportEncryption.EncryptionInfo
        };

        OperationResponse response = await KsefRateLimitWrapper.ExecuteWithRetryAsync(
            ksefApiCall: ct => KsefClient.ExportInvoicesAsync(request, _accessToken, ct, includeMetadata: true),
            endpoint: KsefApiEndpoint.InvoiceExport,
            cancellationToken: CancellationToken,
            limitsClient: LimitsClient,
            accessToken: _accessToken);

        if (!string.IsNullOrWhiteSpace(response?.ReferenceNumber))
        {
            _exportEncryptionByOperation[response.ReferenceNumber] = exportEncryption;
        }

        return response;
    }

    private async Task<InvoiceExportStatusResponse?> WaitForExportCompletionAsync(string referenceNumber)
    {
        return await AsyncPollingUtils.PollAsync(
            action: async () => await KsefRateLimitWrapper.ExecuteWithRetryAsync(
                ct => KsefClient.GetInvoiceExportStatusAsync(referenceNumber, _accessToken, ct),
                KsefApiEndpoint.InvoiceExport,
                cancellationToken: CancellationToken,
                limitsClient: LimitsClient,
                accessToken: _accessToken),
            condition: status => status?.Status?.Code == SuccessStatusCode,
            delay: ExportPollingDelay,
            maxAttempts: MaxExportStatusRetries,
            cancellationToken: CancellationToken);
    }

    private EncryptionData GetEncryptionDataForOperation(string referenceNumber)
    {
        if (_exportEncryptionByOperation.TryGetValue(referenceNumber, out EncryptionData? encryption) && encryption != null)
        {
            return encryption;
        }

        throw new InvalidOperationException($"Brak danych szyfrujących dla eksportu {referenceNumber}.");
    }

    private async Task<PackageProcessingResult> DownloadAndProcessPackageAsync(InvoiceExportPackage package, EncryptionData encryptionData)
    {
        List<InvoiceSummary> metadataSummaries = new();
        Dictionary<string, string> invoiceXmlFiles = new(StringComparer.OrdinalIgnoreCase);

        // Pobranie, odszyfrowanie i połączenie wszystkich części w jeden strumień
        using MemoryStream decryptedArchiveStream = await BatchUtils.DownloadAndDecryptPackagePartsAsync(
            package.Parts, 
            encryptionData, 
            CryptographyService, 
            cancellationToken: CancellationToken);

        // Rozpakowanie ZIP
        Dictionary<string, string> unzippedFiles = await BatchUtils.UnzipAsync(decryptedArchiveStream, CancellationToken);

        foreach ((string fileName, string content) in unzippedFiles)
        {
            if (fileName.Equals(MetadataEntryName, StringComparison.OrdinalIgnoreCase))
            {
                InvoicePackageMetadata? metadata = JsonSerializer.Deserialize<InvoicePackageMetadata>(content, MetadataSerializerOptions);
                if (metadata?.Invoices != null)
                {
                    metadataSummaries.AddRange(metadata.Invoices);
                }
            }
            else if (fileName.EndsWith(XmlFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                invoiceXmlFiles[fileName] = content;
            }
        }

        return new PackageProcessingResult(
            new ReadOnlyCollection<InvoiceSummary>(metadataSummaries),
            new ReadOnlyDictionary<string, string>(invoiceXmlFiles));
    }

    private static IReadOnlyList<(DateTime From, DateTime To)> BuildIncrementalWindows(DateTime batchStartUtc, DateTime batchCompletedUtc)
    {
        // Celowe przygotowanie okien, które mają duże pokrycie w celu zasymulowania deduplikacji.
        DateTime firstWindowStart = batchStartUtc.AddMinutes(-10);
        DateTime firstWindowEnd = batchCompletedUtc.AddMinutes(5);

        DateTime secondWindowStart = batchStartUtc;
        DateTime secondWindowEnd = batchCompletedUtc.AddMinutes(10);

        return new List<(DateTime From, DateTime To)>
        {
            (firstWindowStart, firstWindowEnd),
            (secondWindowStart, secondWindowEnd)
        };
    }

    /// <summary>
    /// Zwraca efektywną datę rozpoczęcia eksportu, uwzględniając punkt kontynuacji dla obciętych paczek.
    /// Jeśli poprzednia paczka dla danego SubjectType została obcięta (IsTruncated=true), 
    /// wykorzystywane jest LastPermanentStorageDate z tej paczki jako punkt startowy w celu zapewnienia ciągłości pobierania.
    /// </summary>
    private static DateTime GetEffectiveStartDate(
        Dictionary<InvoiceSubjectType, DateTime?> continuationPoints, 
        InvoiceSubjectType subjectType, 
        DateTime windowFrom)
    {
        if (continuationPoints.TryGetValue(subjectType, out DateTime? continuationPoint) && continuationPoint.HasValue)
        {
            return continuationPoint.Value;
        }

        return windowFrom;
    }

    /// <summary>
    /// Aktualizuje punkt kontynuacji dla danego SubjectType, jeśli paczka została obcięta (IsTruncated=true).
    /// Punkt kontynuacji to LastPermanentStorageDate z obciętej paczki i służy jako punkt startowy dla następnego eksportu
    /// w celu zapewnienia, że żadne faktury nie zostaną pominięte.
    /// </summary>
    private static void UpdateContinuationPointIfNeeded(
        Dictionary<InvoiceSubjectType, DateTime?> continuationPoints,
        InvoiceSubjectType subjectType,
        InvoiceExportPackage package)
    {
        if (package.IsTruncated && package.LastPermanentStorageDate.HasValue)
        {
            continuationPoints[subjectType] = package.LastPermanentStorageDate.Value.UtcDateTime;
        }
        else
        {
            // Jeśli paczka nie jest obcięta, usunięcie punktu kontynuacji - zakres został w pełni przetworzony
            continuationPoints.Remove(subjectType);
        }
    }

    private sealed record ExportTask(DateTime From, DateTime To, InvoiceSubjectType SubjectType);

    private sealed record PackageProcessingResult(
        IReadOnlyCollection<InvoiceSummary> MetadataSummaries,
        IReadOnlyDictionary<string, string> InvoiceXmlFiles);
}
