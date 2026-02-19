using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Tests.Utils.Upo;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Api.Builders.Batch;

namespace KSeF.Client.Tests.Core.E2E.BatchSession;

[Collection("BatchSessionScenario")]
public class BatchSessionStreamE2ETests : TestBase
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

    public BatchSessionStreamE2ETests()
    {
        string nip = MiscellaneousUtils.GetRandomNip();
        Client.Core.Models.Authorization.AuthenticationOperationStatusResponse authOperationStatusResponse = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, nip)
            .GetAwaiter().GetResult();

        accessToken = authOperationStatusResponse.AccessToken.Token;
        sellerNip = nip;
    }

    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    public async Task BatchSessionStreamBasedFullIntegrationFlowReturnsUpo(SystemCode systemCode, string invoiceTemplatePath)
    {
        // 1. Przygotowanie paczki i otwarcie sesji
        (string referenceNumber, OpenBatchSessionResponse openBatchSessionResponse, List<BatchPartStreamSendingInfo> streamParts) =
            await PrepareAndOpenBatchSessionWithStreamsAsync(
                CryptographyService,
                TotalInvoices,
                PartQuantity,
                sellerNip,
                systemCode,
                invoiceTemplatePath,
                accessToken);

        Assert.NotNull(referenceNumber);
        Assert.NotEmpty(referenceNumber);
        Assert.NotNull(openBatchSessionResponse);
        Assert.NotNull(streamParts);
        Assert.NotEmpty(streamParts);

        batchSessionReferenceNumber = referenceNumber;

        // 2. Wysłanie wszystkich części
        await KsefClient.SendBatchPartsWithStreamAsync(openBatchSessionResponse, streamParts);

        // 3. Zamknięcie sesji (powtarzanie na wypadek chwilowych błędów)
        Assert.False(string.IsNullOrWhiteSpace(batchSessionReferenceNumber));
        await AsyncPollingUtils.PollAsync(
            action: async () =>
            {
                await KsefClient.CloseBatchSessionAsync(batchSessionReferenceNumber!, accessToken, CancellationToken).ConfigureAwait(false);
                return true;
            },
            condition: closed => closed,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 30,
            shouldRetryOnException: _ => true,
            cancellationToken: CancellationToken);

        // 4. Status sesji (powtarzanie aż do Code == 200)
        SessionStatusResponse statusResponse = await AsyncPollingUtils.PollWithBackoffAsync(
            action: () => KsefClient.GetSessionStatusAsync(batchSessionReferenceNumber!, accessToken, CancellationToken),
            condition: s => s.Status.Code is ExpectedSessionStatusCode,
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(5),
            maxAttempts: 30,
            cancellationToken: CancellationToken);

        Assert.NotNull(statusResponse);
        Assert.True(statusResponse.SuccessfulInvoiceCount == TotalInvoices);
        Assert.Equal(ExpectedFailedInvoiceCount, statusResponse.FailedInvoiceCount);
        Assert.NotNull(statusResponse.Upo);
        Assert.NotNull(statusResponse.ValidUntil);
        Assert.Equal(ExpectedSessionStatusCode, statusResponse.Status.Code);

        upoReferenceNumber = statusResponse.Upo.Pages.First().ReferenceNumber;

        // 5. Dokumenty sesji
        SessionInvoicesResponse documents = await KsefClient.GetSessionInvoicesAsync(batchSessionReferenceNumber!, accessToken, TotalInvoices, null, CancellationToken);

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
    }

    private async Task<(string ReferenceNumber, OpenBatchSessionResponse OpenResp, List<BatchPartStreamSendingInfo> EncryptedParts)> PrepareAndOpenBatchSessionWithStreamsAsync(
        ICryptographyService cryptographyService,
        int invoiceCount,
        int partQuantity,
        string sellerNip,
        SystemCode systemCode,
        string invoiceTemplatePath,
        string accessToken)
    {
        // Dane szyfrowania (RSA dla klucza + AES dla treści)
        EncryptionData encryptionData = cryptographyService.GetEncryptionData();

        // 1) Generuj faktury w pamięci z Templates
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            count: invoiceCount,
            nip: sellerNip,
            templatePath: invoiceTemplatePath);

        // 2) Zbuduj ZIP do MemoryStream
        using MemoryStream zipStream = new();
        using (System.IO.Compression.ZipArchive archive = new(zipStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach ((string FileName, byte[] Content) in invoices)
            {
                System.IO.Compression.ZipArchiveEntry entry = archive.CreateEntry(FileName, System.IO.Compression.CompressionLevel.Optimal);
                using Stream entryStream = entry.Open();
#if NETFRAMEWORK
                await entryStream.WriteAsync(Content, 0, Content.Length, CancellationToken).ConfigureAwait(false);
#else
                await entryStream.WriteAsync(Content, CancellationToken).ConfigureAwait(false);
#endif
            }
        }
        zipStream.Position = 0;

        // 3) Metadane ZIP
        FileMetadata zipMeta = await cryptographyService.GetMetaDataAsync(zipStream, CancellationToken).ConfigureAwait(false);

        // 4) Podział ZIP na części
        byte[] zipBytes = zipStream.ToArray();
        List<byte[]> rawParts = BatchUtils.Split(zipBytes, partQuantity);

        // 5) Szyfrowanie każdej części przy użyciu EncryptStreamWithAES256Async + metadane
        List<BatchPartStreamSendingInfo> encryptedParts = new(rawParts.Count);
        for (int i = 0; i < rawParts.Count; i++)
        {
            using MemoryStream partInput = new(rawParts[i], writable: false);
            MemoryStream encryptedOutput = new();
            await cryptographyService.EncryptStreamWithAES256Async(partInput, encryptedOutput, encryptionData.CipherKey, encryptionData.CipherIv, CancellationToken).ConfigureAwait(false);

            if (encryptedOutput.CanSeek)
            {
                encryptedOutput.Position = 0;
            }

            FileMetadata partMeta = await cryptographyService.GetMetaDataAsync(encryptedOutput, CancellationToken).ConfigureAwait(false);
            if (encryptedOutput.CanSeek)
            {
                encryptedOutput.Position = 0; // reset po odczycie do metadanych
            }

            encryptedParts.Add(new BatchPartStreamSendingInfo
            {
                DataStream = encryptedOutput,
                OrdinalNumber = i + 1,
                Metadata = partMeta
            });
        }

        // 6) Budowa requestu otwarcia sesji wsadowej
        IOpenBatchSessionRequestBuilderBatchFile builder = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(
                systemCode: SystemCodeHelper.GetSystemCode(systemCode),
                schemaVersion: SystemCodeHelper.GetSchemaVersion(systemCode),
                value: SystemCodeHelper.GetValue(systemCode))
            .WithBatchFile(fileSize: zipMeta.FileSize, fileHash: zipMeta.HashSHA);

        foreach (BatchPartStreamSendingInfo p in encryptedParts)
        {
            builder = builder.AddBatchFilePart(
                ordinalNumber: p.OrdinalNumber,
                fileSize: p.Metadata.FileSize,
                fileHash: p.Metadata.HashSHA);
        }

        OpenBatchSessionRequest openBatchSessionRequest = builder
            .EndBatchFile()
            .WithEncryption(
                encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                initializationVector: encryptionData.EncryptionInfo.InitializationVector)
            .Build();

        OpenBatchSessionResponse openResp = await KsefClient.OpenBatchSessionAsync(openBatchSessionRequest, accessToken,cancellationToken: CancellationToken).ConfigureAwait(false);

        return (openResp.ReferenceNumber, openResp, encryptedParts);
    }
}
