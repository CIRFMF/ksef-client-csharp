using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Authorization;
using System.Text;
using System.Text.RegularExpressions;
using KSeF.Client.Http.Helpers;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class KsefTokenClient(IRestClient restClient, IRouteBuilder routeBuilder) : ClientBase(restClient, routeBuilder), IKsefTokenClient
{
    /// <inheritdoc />
    public Task<KsefTokenResponse> GenerateKsefTokenAsync(KsefTokenRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<KsefTokenResponse, KsefTokenRequest>(Routes.Tokens.Root, requestPayload, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryKsefTokensResponse> QueryKsefTokensAsync(
        string accessToken,
        ICollection<AuthenticationKsefTokenStatus> statuses = null,
        string authorIdentifier = null,
        Core.Models.Token.TokenContextIdentifierType? authorIdentifierType = null,
        string description = null,
        string continuationToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        StringBuilder urlBuilder = new(Routes.Tokens.Root);
        bool hasQuery = false;

        void AppendQuery(string name, string value)
        {
            if (!hasQuery)
            {
                urlBuilder.Append('?');
                hasQuery = true;
            }
            else
            {
                urlBuilder.Append('&');
            }
            urlBuilder.Append(name);
            urlBuilder.Append('=');
            urlBuilder.Append(Uri.EscapeDataString(value));
        }

        if (statuses is { Count: > 0 })
        {
            foreach (AuthenticationKsefTokenStatus s in statuses)
            {
                AppendQuery("status", s.ToString());
            }
        }
        if (!string.IsNullOrWhiteSpace(authorIdentifier))
        {
            AppendQuery("authorIdentifier", authorIdentifier);
        }
        if (authorIdentifierType.HasValue)
        {
            AppendQuery("authorIdentifierType", authorIdentifierType.Value.ToString());
        }
        if (!string.IsNullOrWhiteSpace(description))
        {
            AppendQuery("description", description);
        }

        PaginationHelper.AppendPagination(null, pageSize, urlBuilder);

        return ExecuteAsync<QueryKsefTokensResponse>(
            urlBuilder.ToString(),
            HttpMethod.Get,
            accessToken,
            !string.IsNullOrWhiteSpace(continuationToken)
                ? new Dictionary<string, string> { { "x-continuation-token", Regex.Unescape(continuationToken) } }
                : null,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthenticationKsefToken> GetKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(tokenReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Tokens.ByReference(Uri.EscapeDataString(tokenReferenceNumber));
        return ExecuteAsync<AuthenticationKsefToken>(endpoint, HttpMethod.Get, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(tokenReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Tokens.ByReference(Uri.EscapeDataString(tokenReferenceNumber));
        return ExecuteAsync(endpoint, HttpMethod.Delete, accessToken, cancellationToken);
    }
}
