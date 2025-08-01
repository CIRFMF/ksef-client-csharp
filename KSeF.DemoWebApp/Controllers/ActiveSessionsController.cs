﻿using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace WebApplication.Controllers;
[Route("active-sessions")]
[ApiController]
public class ActiveSessionsController : ControllerBase
{
    private readonly IKSeFClient ksefClient;

    public ActiveSessionsController(IKSeFClient ksefClient)
    {
        this.ksefClient = ksefClient;
    }

    /// <summary>
    /// Pobranie listy aktywnych sesji.
    /// </summary>
    [HttpGet("list")]
    public async Task<ActionResult<ICollection<Item>>> GetSessionsAsync([FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        const int pageSize = 20;
        string? continuationToken = null;
        var activeSessions = new List<Item>();
        do
        {
            var response = await ksefClient.GetActiveSessions(accessToken, pageSize, continuationToken, cancellationToken);
            continuationToken = response.ContinuationToken;
            activeSessions.AddRange(response.Items);
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        return Ok(activeSessions);
    }

    /// <summary>
    /// Unieważnia sesję powiązaną z tokenem użytym do wywołania tej operacji.
    /// </summary>
    /// <param name="token">Acces token lub Refresh token.</param>
    /// <param name="cancellationToken"></param>
    [HttpDelete("revoke-current-session")]
    public async Task<ActionResult> RevokeCurrentSessionAsync([FromQuery] string token, CancellationToken cancellationToken)
    {
        await ksefClient.RevokeCurrentSessionAsync(token, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Unieważnia sesję o podanym numerze referencyjnym.
    /// </summary>
    [HttpDelete("revoke-session")]
    public async Task<ActionResult> RevokeSessionAsync([FromQuery] string referenceNumber, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        await ksefClient.RevokeSessionAsync(referenceNumber, accessToken, cancellationToken);
        return NoContent();
    }
}