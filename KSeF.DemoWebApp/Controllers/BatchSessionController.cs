using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Models.Sessions;
using System.IO.Compression;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Api.Builders.Batch;

namespace KSeF.DemoWebApp.Controllers;

[Route("[controller]")]
[ApiController]
public class BatchSessionController(ICryptographyService cryptographyService, IKSeFClient ksefClient, IConfiguration configuration) : ControllerBase
{
    private readonly IKSeFClient ksefClient = ksefClient;
    private readonly ICryptographyService cryptographyService = cryptographyService;
    private readonly EncryptionData encryptionData = cryptographyService.GetEncryptionData();
    private static readonly string BatchPartsDirectory = Path.Combine(AppContext.BaseDirectory, "BatchParts");
    private static readonly string InvoicesDirectory = Path.Combine(AppContext.BaseDirectory, "Invoices");
    private readonly string contextIdentifier = configuration["Tools:contextIdentifier"]!;

    [HttpPost("open-session")]
    public async Task<ActionResult> OpenBatchSessionAsync(string accessToken, CancellationToken cancellationToken)
    {
        string invoicePath = "faktura-template-fa(3).xml";

        List<string> invoices = [];
        if (!Directory.Exists(InvoicesDirectory))
        {
            Directory.CreateDirectory(InvoicesDirectory);
        }

        for (int i =0; i < 20; i++)
        {
            string inv = System.IO.File.ReadAllText(invoicePath).Replace("#nip#", contextIdentifier).Replace("#invoice_number#", Guid.NewGuid().ToString());
            string invoiceName = $"faktura_{i + 1}.xml";
            invoices.Add(Path.Combine(InvoicesDirectory, invoiceName));
            System.IO.File.WriteAllText(Path.Combine(InvoicesDirectory, invoiceName), inv);
        }

        if (!Directory.Exists(BatchPartsDirectory))
        {
            Directory.CreateDirectory(BatchPartsDirectory);
        }

        // 1. Wczytaj pliki do pamięci        
        List<(string FileName, byte[] Content)> files =
            [.. invoices.Select(f => (Path.GetFileName(f), System.IO.File.ReadAllBytes(f)))];

        // 2. Stwórz ZIP w pamięci
        byte[] zipBytes;
        using MemoryStream zipStream = new();
        using ZipArchive archive = new(zipStream, ZipArchiveMode.Create, leaveOpen: true);
        
        foreach ((string FileName, byte[] Content) file in files)
        {
            ZipArchiveEntry entry = archive.CreateEntry(file.FileName, CompressionLevel.Optimal);
            using Stream entryStream = entry.Open();
            entryStream.Write(file.Content);
        }
        
        archive.Dispose();
        zipBytes = zipStream.ToArray();

        // 3. Pobierz metadane ZIP-a (przed szyfrowaniem)
        FileMetadata zipMetadata = cryptographyService.GetMetaData(zipBytes);

        // 4. Podziel ZIP na 11 partów
        int partCount = 11;
        int partSize = (int)Math.Ceiling((double)zipBytes.Length / partCount);
        List<byte[]> zipParts = [];
        for (int i = 0; i < partCount; i++)
        {
            int start = i * partSize;
            int size = Math.Min(partSize, zipBytes.Length - start);
            if (size <= 0)
            {
                break;
            }

            byte[] part = new byte[size];
            Array.Copy(zipBytes, start, part, 0, size);
            zipParts.Add(part);
        }

        // 5. Szyfruj każdy part i pobierz metadane
        List<BatchPartSendingInfo> encryptedParts = [];
        for (int i = 0; i < zipParts.Count; i++)
        {
            byte[] encrypted = cryptographyService.EncryptBytesWithAES256(zipParts[i], encryptionData.CipherKey, encryptionData.CipherIv);
            FileMetadata metadata = cryptographyService.GetMetaData(encrypted);
            encryptedParts.Add(new BatchPartSendingInfo { Data = encrypted, OrdinalNumber = i+1, Metadata = metadata});
        }

        // 6. Buduj request
        IOpenBatchSessionRequestBuilderBatchFile batchFileInfoBuilder = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(systemCode: "FA (2)", schemaVersion: "1-0E", value: "FA")
            .WithOfflineMode(false)
            .WithBatchFile(
                fileSize: zipMetadata.FileSize,
                fileHash: zipMetadata.HashSHA);

        for (int i = 0; i < encryptedParts.Count; i++)
        {
            batchFileInfoBuilder = batchFileInfoBuilder.AddBatchFilePart(
                ordinalNumber: i + 1,
                fileSize: encryptedParts[i].Metadata.FileSize,
                fileHash: encryptedParts[i].Metadata.HashSHA);
        }

        OpenBatchSessionRequest openBatchRequest = batchFileInfoBuilder.EndBatchFile()
            .WithEncryption(
                encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                initializationVector: encryptionData.EncryptionInfo.InitializationVector)
        .Build();

        OpenBatchSessionResponse openBatchSessionResponse = await ksefClient.OpenBatchSessionAsync(openBatchRequest, accessToken, cancellationToken: cancellationToken).ConfigureAwait(false);
       await ksefClient.SendBatchPartsAsync(openBatchSessionResponse, encryptedParts, cancellationToken).ConfigureAwait(false);
       return Ok($"Wysłano, zamknij sesję, żeby zacząć przetwarzanie i sprawdź status sesji, {openBatchSessionResponse.ReferenceNumber}");
    }

    [HttpPost("close-session")]
    public async Task<ActionResult> CloseBatchSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        await ksefClient.CloseBatchSessionAsync(sessionReferenceNumber, accessToken, cancellationToken).ConfigureAwait(false);
        return Ok();
    }
  
}
