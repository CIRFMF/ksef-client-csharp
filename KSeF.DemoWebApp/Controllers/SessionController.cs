using KSeF.Client.Core.Models.Sessions;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;


namespace WebApplication.Controllers;

[Route("[controller]")]
[ApiController]
public class SessionController : ControllerBase
{

    private readonly IKSeFClient ksefClient;

    public SessionController(IKSeFClient ksefClient)
    {
        this.ksefClient = ksefClient;
    }

    [HttpGet("status")]
    public async Task<SessionStatusResponse> GetStatusAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        var status = await ksefClient.GetSessionStatusAsync(referenceNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return status;
    }

    [HttpGet("invoice-upo-by-ksef-number")]
    public async Task<string> GetInvoiceUpoByKsefNumberAsync(string referenceNumber, string ksefNumber, string accessToken, CancellationToken cancellationToken)
    {
        var upo = await ksefClient.GetSessionInvoiceUpoByKsefNumberAsync(referenceNumber, ksefNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return upo;
    }

    [HttpGet("session-upo")]
    public async Task<string> GetSessionUpoAsync(string referenceNumber, string upoReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        var upo = await ksefClient.GetSessionUpoAsync(referenceNumber, upoReferenceNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return upo;
    }

    [HttpGet("session-documents")]
    public async Task<ActionResult<SessionInvoicesResponse>> GetSessionDocumentsAsync(string accessToken, string referenceNumber, CancellationToken cancellationToken)
    {
        var sessionDocuments = await ksefClient.GetSessionInvoicesAsync(referenceNumber, accessToken,null, null, cancellationToken);
        return Ok(sessionDocuments);
    }

    [HttpGet("failed-invoices")]
    public async Task<ActionResult<SessionInvoicesResponse>> GetFailedInvoicesAsync(string accessToken, string referenceNumber, CancellationToken cancellationToken)
    {
        var failedInvoices = await ksefClient.GetSessionFailedInvoicesAsync(referenceNumber, accessToken, null, null, cancellationToken);
        return Ok(failedInvoices);
    }

    [HttpGet("invoice-upo-by-invoice-reference-number")]
    public async Task<string> GetInvoiceUpoByReferenceNumberAsync(string referenceNumber, string ksefNumber, string accessToken, CancellationToken cancellationToken)
    {
        var upo = await ksefClient.GetSessionInvoiceUpoByReferenceNumberAsync(referenceNumber, ksefNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return upo;
    }

}
