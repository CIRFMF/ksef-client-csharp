using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using System.Text;
using System.Text.RegularExpressions;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class ActiveSessionsClient(IRestClient restClient, IRouteBuilder routeBuilder) : ClientBase(restClient, routeBuilder), IActiveSessionsClient
{
    /// <inheritdoc />
    public Task<AuthenticationListResponse> GetActiveSessions(string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        StringBuilder urlBuilder = new StringBuilder(Routes.ActiveSessions.Session);

        if (pageSize.HasValue)
        {
            urlBuilder.Append($"?pageSize={pageSize.Value}");
        }

        string url = urlBuilder.ToString();
        return ExecuteAsync<AuthenticationListResponse>(url, HttpMethod.Get, accessToken,
                                                                          !string.IsNullOrEmpty(continuationToken) ?
                                                                               new Dictionary<string, string> { { "x-continuation-token", Regex.Unescape(continuationToken) } }
                                                                               : null,
                                                                          cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeCurrentSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return ExecuteAsync(Routes.ActiveSessions.CurrentSession, HttpMethod.Delete, token, cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = $"{Routes.ActiveSessions.Session}/{Uri.EscapeDataString(sessionReferenceNumber)}";
        return ExecuteAsync(endpoint, HttpMethod.Delete, accessToken, cancellationToken);
    }
}
