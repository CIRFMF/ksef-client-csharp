using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;

namespace WebApplication.Controllers;
[Route("active-sessions")]
[ApiController]
public class ActiveSessionsController : ControllerBase
{
    private readonly IActiveSessionsClient _activeSessionsClient;

    public ActiveSessionsController(IActiveSessionsClient activeSessionsClient)
    {
       _activeSessionsClient = activeSessionsClient;
    }

    /// <summary>
    /// Pobranie listy aktywnych sesji.
    /// </summary>
    [HttpGet("list")]
    public async Task<ActionResult<ICollection<AuthenticationListItem>>> GetSessionsAsync([FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        const int pageSize = 20;
        string? continuationToken = null;
        List<AuthenticationListItem> activeSessions = new List<AuthenticationListItem>();
        do
        {
            AuthenticationListResponse response = await _activeSessionsClient.GetActiveSessions(accessToken, pageSize, continuationToken, cancellationToken);
            continuationToken = response.ContinuationToken;
            activeSessions.AddRange(response.Items);
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        return Ok(activeSessions);
    }

    /// <summary>
    /// Unieważnia sesję powiązaną z tokenem użytym do wywołania tej operacji.
    /// </summary>
    /// <param name="token">Access token lub Refresh token.</param>
    /// <param name="cancellationToken"></param>
    [HttpDelete("revoke-current-session")]
    public async Task<ActionResult> RevokeCurrentSessionAsync([FromQuery] string token, CancellationToken cancellationToken)
    {
        await _activeSessionsClient.RevokeCurrentSessionAsync(token, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Unieważnia sesję o podanym numerze referencyjnym.
    /// </summary>
    [HttpDelete("revoke-session")]
    public async Task<ActionResult> RevokeSessionAsync([FromQuery] string sessionReferenceNumber, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        await _activeSessionsClient.RevokeSessionAsync(sessionReferenceNumber, accessToken, cancellationToken);
        return NoContent();
    }
}