using KSeF.Client.Core.Models.Invoices;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication.Controllers;
[Route("[controller]")]
[ApiController]
public class InvoicesController : ControllerBase
{
    private readonly IKSeFClient ksefClient;

    public InvoicesController(IKSeFClient ksefClient)
    {
        this.ksefClient = ksefClient;
    }

    /// <summary>
    /// Pobranie faktury po numerze referencyjnym.
    /// </summary>
    [HttpGet("single")]
    public async Task<ActionResult<string>> GetInvoiceAsync([FromQuery] string ksefReferenceNumber, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        return await ksefClient.GetInvoiceAsync(ksefReferenceNumber, accessToken, cancellationToken);
    }

    /// <summary>
    /// Pobieranie faktury na podstawie danych faktury.
    /// </summary>
    [HttpPost("download")]
    public async Task<ActionResult<string>> DownloadInvoiceAsync([FromBody] InvoiceRequest body, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        return await ksefClient.DownloadInvoiceAsync(body, accessToken, cancellationToken);
    }

    /// <summary>
    /// Zapytanie o metadane faktur według filtrów.
    /// </summary>
    [HttpPost("query")]
    public async Task<ActionResult<PagedInvoiceResponse>> QueryInvoicesAsync(
        [FromBody] QueryInvoiceRequest body,
        [FromQuery] string accessToken,
        [FromQuery] int? pageOffset,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return await ksefClient.QueryInvoicesAsync(body, accessToken, pageOffset, pageSize, cancellationToken);
    }

    /// <summary>
    /// Rozpoczęcie asynchronicznego wyszukiwania faktur.
    /// </summary>
    [HttpPost("query-async")]
    public async Task<ActionResult<OperationStatusResponse>> AsyncQueryInvoicesAsync(
        [FromBody] AsyncQueryInvoiceRequest body,
        [FromQuery] string accessToken,
        CancellationToken cancellationToken)
    {
        return await ksefClient.AsyncQueryInvoicesAsync(body, accessToken, cancellationToken);
    }

    /// <summary>
    /// Pobranie statusu zapytania asynchronicznego.
    /// </summary>
    [HttpGet("query-async/status")]
    public async Task<ActionResult<AsyncQueryInvoiceStatusResponse>> GetAsyncQueryInvoicesStatusAsync(
        [FromQuery] string operationReferenceNumber,
        [FromQuery] string accessToken,
        CancellationToken cancellationToken)
    {
        return await ksefClient.GetAsyncQueryInvoicesStatusAsync(operationReferenceNumber, accessToken, cancellationToken);
    }
}