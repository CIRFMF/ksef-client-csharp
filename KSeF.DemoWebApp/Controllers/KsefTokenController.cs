using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Authorization.Rev2;
using KSeF.Client.Core.Interfaces.Clients;

namespace KSeF.DemoWebApp.Controllers;

[Route("[controller]")]
[ApiController]
public class KsefTokenController(IKSeFClient ksefClient) : ControllerBase
{
    private readonly IKSeFClient ksefClient = ksefClient;

    [HttpGet("get-new-token")]
    public async Task<ActionResult<KsefTokenResponse>> GetNewTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        KsefTokenRequest tokenRequest = new()
        {
            Permissions = [
                KsefTokenPermissionType.InvoiceRead,
                KsefTokenPermissionType.InvoiceWrite
                ],
            Description = "Demo token",
        };
        KsefTokenResponse ksefToken = await ksefClient.GenerateKsefTokenAsync(tokenRequest, accessToken, cancellationToken).ConfigureAwait(false);
        return Ok(ksefToken);
    }

    [HttpGet("query-tokens")]
    public async Task<ActionResult<AuthenticationKsefToken>> QueryTokensAsync(string accessToken, CancellationToken cancellationToken)
    {
        List<AuthenticationKsefToken> result = [];
        const int pageSize = 20;
        AuthenticationKsefTokenStatus status = AuthenticationKsefTokenStatus.Active;
        string continuationToken = string.Empty;

        do
        {
            QueryKsefTokensResponse tokens = await ksefClient.QueryKsefTokensAsync(accessToken, [status], continuationToken, pageSize : pageSize, cancellationToken: cancellationToken).ConfigureAwait(false);
            result.AddRange(tokens.Tokens);
            continuationToken = tokens.ContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        return Ok(result);
    }
    [HttpGet("get-token")]
    public async Task<ActionResult<AuthenticationKsefToken>> GetTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        AuthenticationKsefToken ksefToken = await ksefClient.GetKsefTokenAsync(tokenReferenceNumber, accessToken, cancellationToken).ConfigureAwait(false);
        return Ok(ksefToken);
    }

    [HttpDelete]
    public async Task<ActionResult> RevokeAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        await ksefClient.RevokeKsefTokenAsync(tokenReferenceNumber, accessToken, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Generuje token KSeF o określonym okresie ważności (POST /tokens/rev2).
    /// </summary>
    [HttpGet("get-new-token-rev2")]
    public async Task<ActionResult<KsefTokenResponseRev2>> GetNewTokenRev2Async(
        string accessToken,
        DateTimeOffset? validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken)
    {
        KsefTokenRequestRev2 tokenRequest = new()
        {
            Permissions = [
                KsefTokenPermissionType.InvoiceRead,
                KsefTokenPermissionType.InvoiceWrite
                ],
            Description = "Demo token Rev2",
            ValidFrom = validFrom,
            ValidTo = validTo
        };
        KsefTokenResponseRev2 ksefToken = await ksefClient.GenerateKsefTokenRev2Async(tokenRequest, accessToken, cancellationToken).ConfigureAwait(false);
        return Ok(ksefToken);
    }

    /// <summary>
    /// Zwraca wszystkie tokeny Rev2 o podanym statusie (GET /tokens/rev2), obsługując paginację.
    /// </summary>
    [HttpGet("query-tokens-rev2")]
    public async Task<ActionResult<List<AuthenticationKsefTokenRev2>>> QueryTokensRev2Async(
        string accessToken,
        KsefTokenStatusFilterRev2 status,
        DateTimeOffset? validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken)
    {
        List<AuthenticationKsefTokenRev2> result = [];
        const int pageSize = 20;
        string? continuationToken = null;

        do
        {
            QueryKsefTokensResponseRev2 tokens = await ksefClient.QueryKsefTokensRev2Async(
                accessToken,
                statuses: [status],
                validFrom: validFrom,
                validTo: validTo,
                continuationToken: continuationToken,
                pageSize: pageSize,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            result.AddRange(tokens.Tokens);
            continuationToken = tokens.ContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        return Ok(result);
    }

    /// <summary>
    /// Pobiera status tokena w kontrakcie Rev2 (GET /tokens/rev2/{referenceNumber}).
    /// </summary>
    [HttpGet("get-token-rev2")]
    public async Task<ActionResult<AuthenticationKsefTokenRev2>> GetTokenRev2Async(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        AuthenticationKsefTokenRev2 ksefToken = await ksefClient.GetKsefTokenRev2Async(tokenReferenceNumber, accessToken, cancellationToken).ConfigureAwait(false);
        return Ok(ksefToken);
    }

    /// <summary>
    /// Zatwierdza token (POST /tokens/{referenceNumber}/approve).
    /// </summary>
    [HttpPost("approve")]
    public async Task<ActionResult> ApproveAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        await ksefClient.ApproveKsefTokenAsync(referenceNumber, accessToken, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
