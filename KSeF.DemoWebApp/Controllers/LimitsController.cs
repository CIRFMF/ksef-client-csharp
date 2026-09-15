namespace KSeF.DemoWebApp.Controllers;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.RateLimits;
using KSeF.Client.Core.Models.TestData;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class LimitsController(ILimitsClient limitsClient) : ControllerBase
{
    /// <summary>
    /// Pobranie aktualnych limitów bieżącego kontekstu (sesje, faktury).
    /// </summary>
    [HttpGet("context")]
    [ProducesResponseType(typeof(SessionLimitsInCurrentContextResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionLimitsInCurrentContextResponse>> GetContextLimits(
        string accessToken,
        CancellationToken cancellationToken)
    {
        return Ok(await limitsClient.GetLimitsForCurrentContextAsync(accessToken, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Pobranie aktualnych limitów bieżącego podmiotu (m.in. certyfikaty).
    /// </summary>
    [HttpGet("subject")]
    [ProducesResponseType(typeof(CertificatesLimitInCurrentSubjectResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CertificatesLimitInCurrentSubjectResponse>> GetSubjectLimits(
        string accessToken,
        CancellationToken cancellationToken)
    {
        return Ok(await limitsClient.GetLimitsForCurrentSubjectAsync(accessToken, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Pobranie aktualnych limitów żądań (rate limits) obowiązujących w API.
    /// </summary>
    [HttpGet("rate-limits")]
    [ProducesResponseType(typeof(EffectiveApiRateLimits), StatusCodes.Status200OK)]
    public async Task<ActionResult<EffectiveApiRateLimits>> GetRateLimits(
        string accessToken,
        CancellationToken cancellationToken)
    {
        return Ok(await limitsClient.GetRateLimitsAsync(accessToken, cancellationToken).ConfigureAwait(false));
    }
}
