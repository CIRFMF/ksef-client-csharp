using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;


namespace WebApplication.Controllers;

[Route("[controller]")]
[ApiController]
public class OnlineSessionController : ControllerBase
{
    private readonly ICryptographyService cryptographyService;
    private static EncryptionData? encryptionData;
    private readonly IKSeFClient kseClient;

    public OnlineSessionController(IKSeFClient ksefClient, ICryptographyService cryptographyService)
    {
        this.cryptographyService = cryptographyService;
        this.kseClient = ksefClient;
    }

    [HttpPost("open-session")]
    public async Task<ActionResult<OpenOnlineSessionResponse>> OpenOnlineSessionAsync(string accessToken, CancellationToken cancellationToken)
    {
        encryptionData = cryptographyService.GetEncryptionData();
        var openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
         .Create()
         .WithFormCode(systemCode: "FA (2)", schemaVersion: "1-0E", value: "FA")
         .WithEncryption(
             encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
             initializationVector: encryptionData.EncryptionInfo.InitializationVector)
         .Build();

        var openSessionResponse = await kseClient.OpenOnlineSessionAsync(openOnlineSessionRequest, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return Ok(openSessionResponse);
    }

    [HttpPost("send-invoice")]
    public async Task<ActionResult<SendInvoiceResponse>> SendInvoiceOnlineSessionAsync(string referenceNumber, string accesToken, CancellationToken cancellationToken)
    {
        var invoice = System.IO.File.ReadAllBytes("faktura-online.xml");

        var encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData!.CipherKey, encryptionData!.CipherIv);

        var invoiceMetadata = cryptographyService.GetMetaData(invoice);
        var encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        var sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithDocumentHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(
               encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .Build();

        var sendInvoiceResponse = await kseClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, referenceNumber, accesToken, cancellationToken)
            .ConfigureAwait(false);
        return sendInvoiceResponse;
    }

    [HttpPost("close-session")]
    public async Task CloseOnlineSessionAsync(string referenceNumber, string accesToken, CancellationToken cancellationToken)
    {
        await kseClient.CloseOnlineSessionAsync(referenceNumber, accesToken, cancellationToken)
            .ConfigureAwait(false);
    }
}
