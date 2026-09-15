using KSeF.Client.Core.Models.Sessions;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;


namespace KSeF.DemoWebApp.Controllers;

[Route("[controller]")]
[ApiController]
public class SessionController(IKSeFClient ksefClient) : ControllerBase
{

    private readonly IKSeFClient ksefClient = ksefClient;

    [HttpGet("online-sessions")]
    public async Task<ActionResult<ICollection<Session>>> GetOnlineSessionsAsync([FromQuery] string accessToken, [FromQuery] SessionsFilter sessionsFilter, CancellationToken cancellationToken)
    {
        List<Session> sessions = [];
        const int pageSize = 20;
        string continuationToken = string.Empty;
        do
        {
            SessionsListResponse response = await ksefClient.GetSessionsAsync(SessionType.Online, accessToken, pageSize, continuationToken, sessionsFilter, cancellationToken).ConfigureAwait(false);
            continuationToken = response.ContinuationToken;
            sessions.AddRange(response.Sessions);
        } while (!string.IsNullOrEmpty(continuationToken));

        return Ok(sessions);
    }

    [HttpGet("batch-sessions")]
    public async Task<ActionResult<ICollection<Session>>> GetbatchSessionsAsync([FromQuery] string accessToken, [FromQuery] SessionsFilter sessionsFilter, CancellationToken cancellationToken)
    {
        List<Session> sessions = [];
        const int pageSize = 20;
        string continuationToken = string.Empty;
        do
        {
            SessionsListResponse response = await ksefClient.GetSessionsAsync(SessionType.Batch, accessToken, pageSize, continuationToken, sessionsFilter, cancellationToken).ConfigureAwait(false);
            continuationToken = response.ContinuationToken;
            sessions.AddRange(response.Sessions);
        } while (!string.IsNullOrEmpty(continuationToken));

        return Ok(sessions);
    }


    [HttpGet("status")]
    public async Task<SessionStatusResponse> GetStatusAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        SessionStatusResponse status = await ksefClient.GetSessionStatusAsync(sessionReferenceNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return status;
    }

    [HttpGet("invoice-upo-by-ksef-number")]
    public async Task<string> GetInvoiceUpoByKsefNumberAsync(string sessionReferenceNumber, string ksefNumber, string accessToken, CancellationToken cancellationToken)
    {
        string upo = await ksefClient.GetSessionInvoiceUpoByKsefNumberAsync(sessionReferenceNumber, ksefNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return upo;
    }

    [HttpGet("session-upo")]
    public async Task<string> GetSessionUpoAsync(string sessionReferenceNumber, string upoReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        string upo = await ksefClient.GetSessionUpoAsync(sessionReferenceNumber, upoReferenceNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return upo;
    }

    [HttpGet("session-documents")]
    public async Task<ActionResult<SessionInvoicesResponse>> GetSessionDocumentsAsync(string accessToken, string sessionReferenceNumber, CancellationToken cancellationToken)
    {
        SessionInvoicesResponse sessionDocuments = await ksefClient.GetSessionInvoicesAsync(sessionReferenceNumber, accessToken, null, null, cancellationToken).ConfigureAwait(false);
        return Ok(sessionDocuments);
    }

    [HttpGet("failed-invoices")]
    public async Task<ActionResult<SessionInvoicesResponse>> GetFailedInvoicesAsync(string accessToken, string sessionReferenceNumber, CancellationToken cancellationToken)
    {
        SessionInvoicesResponse failedInvoices = await ksefClient.GetSessionFailedInvoicesAsync(sessionReferenceNumber, accessToken, null, null, cancellationToken).ConfigureAwait(false);
        return Ok(failedInvoices);
    }

    [HttpGet("invoice")]
    public async Task<ActionResult<SessionInvoice>> GetSessionInvoiceAsync(string sessionReferenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        SessionInvoice invoice = await ksefClient.GetSessionInvoiceAsync(sessionReferenceNumber, invoiceReferenceNumber, accessToken, cancellationToken).ConfigureAwait(false);
        return Ok(invoice);
    }

    [HttpGet("invoice-upo-by-invoice-reference-number")]
    public async Task<string> GetInvoiceUpoByReferenceNumberAsync(string sessionReferenceNumber, string ksefNumber, string accessToken, CancellationToken cancellationToken)
    {
        string upo = await ksefClient.GetSessionInvoiceUpoByReferenceNumberAsync(sessionReferenceNumber, ksefNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return upo;
    }

}
