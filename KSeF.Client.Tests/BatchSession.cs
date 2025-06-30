using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeFClient.Http;
using System.IO.Compression;

namespace KSeF.Client.Tests;

public class BatchSessionScenarioFixture
{
    public string? ReferenceNumber { get; set; }
    public string? KsefNumber { get; set; }
    public string? UpoDocument { get; set; }
    public string? AccessToken { get; set; }
    public OpenBatchSessionResponse? OpenBatchSessionResponse { get; internal set; }
    public string? UpoReferenceNumber { get; set; }

    public List<BatchPartSendingInfo> EncryptedParts { get; set; } 
}

[CollectionDefinition("BatchSessionScenario")]
public class BatchSessionScenarioCollection : ICollectionFixture<BatchSessionScenarioFixture> { }

[Collection("BatchSessionScenario")]
public class BatchSession : TestBase
{
    private readonly BatchSessionScenarioFixture _fixture;
    private readonly string BatchPartsDirectory = Path.Combine(AppContext.BaseDirectory, "BatchParts");
    private readonly string InvoicesDirectory = Path.Combine(AppContext.BaseDirectory, "Invoices");

    public BatchSession(BatchSessionScenarioFixture fixture)
    {
        _fixture = fixture;
        _fixture.AccessToken = AccessToken;
    }



    [Fact]
    public async Task BatchSession_E2E_WorksCorrectly()
    {
        // Step 1: Open session
        await Step1_OpenBatchSession_ReturnsReference();

        // Step 2: Send invoice
        await Step2_SendBatchPartsAsync();

        await Task.Delay(2000); // Wait for processing

        // Step 3: Close session
        await Step3_CloseBatchSessionAsync_ClosesSessionSuccessfully();

        await Task.Delay(8000); // Wait for processing

        // Step 4: Check status
        await Step4_GetBatchSessionStatusByAsync_ReturnsStatus();

        // Step 5: Get documents
        await Step5_GetBatchSessionDocumentsAync_ReturnsDocuments();

        // Step 6: Get UPO
        await Step6_GetBatchSessionInvoiceUpoAsync_ReturnsUpo();

        // Step 7: Get session UPO
        await Step7_GetBatchSessionUpoAsync_ReturnsSessionUpo();
    }


    private async Task Step1_OpenBatchSession_ReturnsReference()
    {
        var restClient = new RestClient(new HttpClient { BaseAddress = new Uri(env) });
        var cryptographyService = new CryptographyService(kSeFClient, restClient) as ICryptographyService;


        var encryptionData = cryptographyService.GetEncryptionData();
        string invoicePath = Path.Combine("invoices", "faktura-template.xml");

        var invoices = new List<string>();
        if (!Directory.Exists(InvoicesDirectory))
            Directory.CreateDirectory(InvoicesDirectory);

        for (var i = 0; i < 20; i++)
        {
            var inv = File.ReadAllText(invoicePath).Replace("#nip#", base.NIP).Replace("#invoice_number#", Guid.NewGuid().ToString());
            var invoiceName = $"faktura_{i + 1}.xml";
            invoices.Add(Path.Combine(InvoicesDirectory, invoiceName));
            File.WriteAllText(Path.Combine(InvoicesDirectory, invoiceName), inv);
        }

        if (!Directory.Exists(BatchPartsDirectory))
            Directory.CreateDirectory(BatchPartsDirectory);

        // 1. Wczytaj pliki do pamięci
        var files = invoices.Select(f => new { FileName = Path.GetFileName(f), Content = File.ReadAllBytes(f) }).ToList();

        // 2. Stwórz ZIP w pamięci
        byte[] zipBytes;
        using (var zipStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    var entry = archive.CreateEntry(file.FileName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    entryStream.Write(file.Content, 0, file.Content.Length);
                }
            }
            zipBytes = zipStream.ToArray();
        }

        // 3. Pobierz metadane ZIP-a (przed szyfrowaniem)
        var zipMetadata = cryptographyService.GetMetaData(zipBytes);

        // 4. Podziel ZIP na 11 partów
        int partCount = 11;
        int partSize = (int)Math.Ceiling((double)zipBytes.Length / partCount);
        var zipParts = new List<byte[]>();
        for (int i = 0; i < partCount; i++)
        {
            int start = i * partSize;
            int size = Math.Min(partSize, zipBytes.Length - start);
            if (size <= 0) break;
            var part = new byte[size];
            Array.Copy(zipBytes, start, part, 0, size);
            zipParts.Add(part);
        }

        // 5. Szyfruj każdy part i pobierz metadane
        var encryptedParts = new List<BatchPartSendingInfo>();
        for (int i = 0; i < zipParts.Count; i++)
        {
            var encrypted = cryptographyService.EncryptBytesWithAES256(zipParts[i], encryptionData.CipherKey, encryptionData.CipherIv);
            var metadata = cryptographyService.GetMetaData(encrypted);
            encryptedParts.Add(new BatchPartSendingInfo { Data = encrypted, OrdinalNumber = i + 1, Metadata = metadata });
        }

        // 6. Buduj request
        var batchFileInfoBuilder = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(systemCode: "FA (2)", schemaVersion: "1-0E", value: "FA")
            .WithBatchFile(
                fileSize: zipMetadata.FileSize,
                fileHash: zipMetadata.HashSHA);

        for (int i = 0; i < encryptedParts.Count; i++)
        {
            batchFileInfoBuilder = batchFileInfoBuilder.AddBatchFilePart(
                ordinalNumber: i + 1,
                fileName: $"faktura_part{i + 1}.zip.aes",
                fileSize: encryptedParts[i].Metadata.FileSize,
                fileHash: encryptedParts[i].Metadata.HashSHA);
        }

        var openBatchRequest = batchFileInfoBuilder.EndBatchFile()
            .WithEncryption(
                encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                initializationVector: encryptionData.EncryptionInfo.InitializationVector)
        .Build();

        var openBatchSessionResponse = await kSeFClient.OpenBatchSessionAsync(openBatchRequest, AccessToken, CancellationToken.None);

        Assert.NotNull(openBatchSessionResponse);
        Assert.NotNull(openBatchSessionResponse.ReferenceNumber);
        _fixture.ReferenceNumber = openBatchSessionResponse.ReferenceNumber;
        _fixture.OpenBatchSessionResponse = openBatchSessionResponse;
        _fixture.EncryptedParts = encryptedParts; // Przechowaj zaszyfrowane części do wysłania
    }

    private async Task Step2_SendBatchPartsAsync()
    {
        await kSeFClient.SendBatchPartsAsync(_fixture.OpenBatchSessionResponse, _fixture.EncryptedParts);
    }

    private async Task Step3_CloseBatchSessionAsync_ClosesSessionSuccessfully()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber)); // Wymuś wykonie kroku 1

        await kSeFClient.CloseBatchSessionAsync(_fixture.ReferenceNumber, _fixture.AccessToken);
    }

    private async Task Step4_GetBatchSessionStatusByAsync_ReturnsStatus()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));

        var statusResponse = await kSeFClient.GetSessionStatusAsync(_fixture.ReferenceNumber, _fixture.AccessToken);

        Assert.NotNull(statusResponse);
        Assert.True(statusResponse.SuccessfulInvoiceCount == 20);
        Assert.True(statusResponse.FailedInvoiceCount == 0);
        Assert.NotNull(statusResponse.Upo);
        //sesja zamknięta
        Assert.True(statusResponse.Status.Code == 200);
        _fixture.UpoReferenceNumber = statusResponse.Upo.Pages.First().ReferenceNumber;
    }

    private async Task Step5_GetBatchSessionDocumentsAync_ReturnsDocuments()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));
        var documents = await kSeFClient.GetSessionInvoicesAsync(_fixture.ReferenceNumber, _fixture.AccessToken, 0, 20);
        Assert.NotNull(documents);
        Assert.NotEmpty(documents.Invoices);
        Assert.Equal(20, documents.Invoices.Count);

        _fixture.KsefNumber = documents.Invoices.First().KsefNumber;
    }

    private async Task Step6_GetBatchSessionInvoiceUpoAsync_ReturnsUpo()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));

        var upoResponse = await kSeFClient.GetSessionInvoiceUpoByKsefNumberAsync(_fixture.ReferenceNumber, _fixture.KsefNumber, _fixture.AccessToken, CancellationToken.None);

        Assert.NotNull(upoResponse);
        Assert.False(string.IsNullOrWhiteSpace(upoResponse));
        _fixture.UpoDocument = upoResponse;
    }

    private async Task Step7_GetBatchSessionUpoAsync_ReturnsSessionUpo()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));
        var upoResponse = await kSeFClient.GetSessionUpoAsync(_fixture.ReferenceNumber, _fixture.UpoReferenceNumber, _fixture.AccessToken, CancellationToken.None);
        Assert.NotNull(upoResponse);
        Assert.False(string.IsNullOrWhiteSpace(upoResponse));
        _fixture.UpoDocument = upoResponse;
    }
}
