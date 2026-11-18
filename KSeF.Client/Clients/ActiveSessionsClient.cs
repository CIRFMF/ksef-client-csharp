using System;
using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using KSeF.Client.Core.Models.Sessions.ActiveSessions.Rev2;
using System.Text;
using System.Text.RegularExpressions;
using KSeF.Client.Http.Helpers;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class ActiveSessionsClient(IRestClient restClient, IRouteBuilder routeBuilder) : ClientBase(restClient, routeBuilder), IActiveSessionsClient
{
    /// <inheritdoc />
    [Obsolete("Operacja GET /auth/sessions jest przestarzała i zostanie wyłączona 15.06.2027. Użyj GetActiveSessionsRev2Async.")]
    public Task<AuthenticationListResponse> GetActiveSessions(string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        StringBuilder urlBuilder = new(Routes.ActiveSessions.Session);

        PaginationHelper.AppendPagination(null, pageSize, urlBuilder);

        string url = urlBuilder.ToString();
        return ExecuteAsync<AuthenticationListResponse>(url, HttpMethod.Get, accessToken,
                                                                          !string.IsNullOrEmpty(continuationToken) ?
                                                                               new Dictionary<string, string> { { "x-continuation-token", Regex.Unescape(continuationToken) } }
                                                                               : null,
                                                                          cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthenticationListResponseRev2> GetActiveSessionsRev2Async(string accessToken, int? pageSize = null, string continuationToken = null, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        StringBuilder urlBuilder = new(Routes.ActiveSessions.SessionRev2);
        PaginationHelper.AppendPagination(null, pageSize, urlBuilder);

        return ExecuteAsync<AuthenticationListResponseRev2>(urlBuilder.ToString(), HttpMethod.Get, accessToken,
                                                           !string.IsNullOrEmpty(continuationToken)
                                                               ? new Dictionary<string, string> { { "x-continuation-token", Regex.Unescape(continuationToken) } }
                                                               : null,
                                                           cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeCurrentSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(token);

        return ExecuteAsync(Routes.ActiveSessions.CurrentSession, HttpMethod.Delete, token, cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = $"{Routes.ActiveSessions.Session}/{Uri.EscapeDataString(sessionReferenceNumber)}";
        return ExecuteAsync(endpoint, HttpMethod.Delete, accessToken, cancellationToken);
    }
}
