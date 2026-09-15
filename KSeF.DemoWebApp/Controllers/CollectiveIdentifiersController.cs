namespace KSeF.DemoWebApp.Controllers;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.CollectiveIdentifiers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("collective-identifiers")]
public class CollectiveIdentifiersController(IKSeFClient ksefClient) : ControllerBase
{
    /// <summary>
    /// Wygenerowanie identyfikatora zbiorczego dla wskazanych faktur.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GenerateCollectiveIdentifierResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerateCollectiveIdentifierResponse>> Generate(
        string accessToken,
        [FromBody] GenerateCollectiveIdentifierRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await ksefClient.GenerateCollectiveIdentifierAsync(request, accessToken, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Pobranie listy identyfikatorów zbiorczych wygenerowanych w bieżącym kontekście.
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(CollectiveIdentifiersQueryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectiveIdentifiersQueryResponse>> Query(
        string accessToken,
        [FromBody] CollectiveIdentifiersQueryRequest request,
        [FromQuery] int? pageSize,
        [FromQuery] string continuationToken,
        CancellationToken cancellationToken)
    {
        return Ok(await ksefClient.QueryCollectiveIdentifiersAsync(request, accessToken, pageSize, continuationToken, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Pobranie listy identyfikatorów zbiorczych po numerze KSeF faktury.
    /// </summary>
    [HttpGet("ksef/{ksefNumber}")]
    [ProducesResponseType(typeof(CollectiveIdentifiersByKsefNumberQueryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectiveIdentifiersByKsefNumberQueryResponse>> GetByKsefNumber(
        string accessToken,
        string ksefNumber,
        [FromQuery] string continuationToken,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return Ok(await ksefClient.GetCollectiveIdentifiersByKsefNumberAsync(ksefNumber, accessToken, continuationToken, pageSize, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Pobranie listy faktur wchodzących w skład wskazanych identyfikatorów zbiorczych (maksymalnie 10 na żądanie).
    /// </summary>
    [HttpPost("invoices")]
    [ProducesResponseType(typeof(CollectiveIdentifierInvoicesQueryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectiveIdentifierInvoicesQueryResponse>> GetInvoices(
        string accessToken,
        [FromBody] CollectiveIdentifierInvoicesQueryRequest request,
        [FromQuery] string continuationToken,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return Ok(await ksefClient.GetCollectiveIdentifierInvoicesAsync(request, accessToken, continuationToken, pageSize, cancellationToken).ConfigureAwait(false));
    }
}
