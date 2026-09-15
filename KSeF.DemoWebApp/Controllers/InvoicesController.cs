using KSeF.Client.Core.Models.Invoices;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;

namespace KSeF.DemoWebApp.Controllers;
[Route("[controller]")]
[ApiController]
public class InvoicesController(IKSeFClient ksefClient) : ControllerBase
{
    private readonly IKSeFClient ksefClient = ksefClient;

    /// <summary>
    /// Pobranie faktury po numerze referencyjnym.
    /// </summary>
    [HttpGet("single")]
    public async Task<ActionResult<string>> GetInvoiceAsync([FromQuery] string ksefReferenceNumber, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        return await ksefClient.GetInvoiceAsync(ksefReferenceNumber, accessToken, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Zapytanie o metadane faktur według filtrów.
    /// </summary>
    [HttpPost("query")]
    public async Task<ActionResult<PagedInvoiceResponse>> QueryInvoicesAsync(
        [FromBody] InvoiceQueryFilters body,
        [FromQuery] string accessToken,
        [FromQuery] int? pageOffset,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return await ksefClient.QueryInvoiceMetadataAsync(body, accessToken, pageOffset, pageSize, SortOrder.Asc, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Eksport faktur zgodnie z podanymi filtrami.
    /// </summary>
    [HttpPost("exports")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationResponse>> ExportInvoices(
        [FromBody] InvoiceExportRequest request,
        string accessToken,
        CancellationToken cancellationToken)
    {
        OperationResponse result = await ksefClient.ExportInvoicesAsync(request, accessToken, cancellationToken:cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Pobiera status operacji eksportu faktur.
    /// </summary>
    [HttpGet("exports/{operationReferenceNumber}")]
    [ProducesResponseType(typeof(InvoiceExportStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceExportStatusResponse>> GetInvoiceExportStatus(
        string operationReferenceNumber,
        string accessToken,
        CancellationToken cancellationToken)
    {
        InvoiceExportStatusResponse result = await ksefClient.GetInvoiceExportStatusAsync(operationReferenceNumber, accessToken, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}