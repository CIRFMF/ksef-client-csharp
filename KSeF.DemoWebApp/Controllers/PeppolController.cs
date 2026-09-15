namespace KSeF.DemoWebApp.Controllers;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Peppol;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class PeppolController(IKSeFClient client) : ControllerBase
{
    /// <summary>
    /// Pobranie listy dostawców usług Peppol.
    /// </summary>
    [HttpGet("query")]
    [ProducesResponseType(typeof(QueryPeppolProvidersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryPeppolProvidersResponse>> QueryProviders(
        string accessToken,
        [FromQuery] int? pageOffset,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        QueryPeppolProvidersResponse result = await client.QueryPeppolProvidersAsync(accessToken, pageOffset, pageSize, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
