using System.Collections.Concurrent;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Api.Builders.Online;


namespace KSeF.DemoWebApp.Controllers;

[Route("[controller]")]
[ApiController]
public class OnlineSessionController(IKSeFClient ksefClient, ICryptographyService cryptographyService, IConfiguration configuration) : ControllerBase
{
    private readonly ICryptographyService cryptographyService = cryptographyService;
    private readonly IKSeFClient ksefClient = ksefClient;
    private readonly string contextIdentifier = configuration["Tools:contextIdentifier"]!;

    private static readonly string InvoiceTemplatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "invoice-template-fa-3.xml");
    private static readonly ConcurrentDictionary<string, EncryptionData> SessionEncryption = new();

    [HttpPost("open-session")]
    public async Task<ActionResult<OpenOnlineSessionResponse>> OpenOnlineSessionAsync(string accessToken, CancellationToken cancellationToken)
    {
        EncryptionData encryptionData = cryptographyService.GetEncryptionData();
        OpenOnlineSessionRequest request = OpenOnlineSessionRequestBuilder
         .Create()
         .WithFormCode(systemCode: "FA (3)", schemaVersion: "1-0E", value: "FA")
         .WithEncryption(
             encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
             initializationVector: encryptionData.EncryptionInfo.InitializationVector,
             publicKeyId: encryptionData.EncryptionInfo.PublicKeyId)
         .Build();

        OpenOnlineSessionResponse openSessionResponse = await ksefClient.OpenOnlineSessionAsync(request, accessToken, cancellationToken: cancellationToken).ConfigureAwait(false);

        SessionEncryption[openSessionResponse.ReferenceNumber] = encryptionData;

        return Ok(openSessionResponse);
    }

    [HttpPost("send-invoice")]
    public async Task<ActionResult<SendInvoiceResponse>> SendInvoiceOnlineSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        if (!SessionEncryption.TryGetValue(sessionReferenceNumber, out EncryptionData? encryptionData))
        {
            return BadRequest($"Nie znaleziono otwartej sesji o numerze referencyjnym '{sessionReferenceNumber}'. Otwórz sesję przez open-session.");
        }

        byte[] invoice = ReadInvoiceTemplate();
        byte[] encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData.CipherKey, encryptionData.CipherIv);

        FileMetadata invoiceMetadata = cryptographyService.GetMetaData(invoice);
        FileMetadata encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(
               encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .WithOfflineMode(false)
            .Build();

        SendInvoiceResponse sendInvoiceResponse = await ksefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return sendInvoiceResponse;
    }


    [HttpPost("send-technical-correction")]
    public async Task<ActionResult<SendInvoiceResponse>> SendTechnicalCorrectionAsync(string sessionReferenceNumber, string hashOfCorrectedInvoice, string accessToken, CancellationToken cancellationToken)
    {
        if (!SessionEncryption.TryGetValue(sessionReferenceNumber, out EncryptionData? encryptionData))
        {
            return BadRequest($"Nie znaleziono otwartej sesji o numerze referencyjnym '{sessionReferenceNumber}'. Otwórz sesję przez open-session.");
        }

        byte[] invoice = ReadInvoiceTemplate();
        byte[] encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData.CipherKey, encryptionData.CipherIv);

        FileMetadata invoiceMetadata = cryptographyService.GetMetaData(invoice);
        FileMetadata encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(
               encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .WithOfflineMode(true)
            .WithHashOfCorrectedInvoice(hashOfCorrectedInvoice)
            .Build();

        SendInvoiceResponse sendInvoiceResponse = await ksefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);

        return sendInvoiceResponse;
    }

    [HttpPost("close-session")]
    public async Task CloseOnlineSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        await ksefClient.CloseOnlineSessionAsync(sessionReferenceNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);

        SessionEncryption.TryRemove(sessionReferenceNumber, out _);
    }

    private byte[] ReadInvoiceTemplate()
    {
        string invoice = System.IO.File.ReadAllText(InvoiceTemplatePath)
            .Replace("#nip#", contextIdentifier, StringComparison.Ordinal)
            .Replace("#invoice_number#", Guid.NewGuid().ToString(), StringComparison.Ordinal);

        return System.Text.Encoding.UTF8.GetBytes(invoice);
    }
}
