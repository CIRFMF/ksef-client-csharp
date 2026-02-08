using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Core.Models.Sessions;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class OnlineSessionClient(IRestClient restClient, IRouteBuilder routeBuilder)
    : ClientBase(restClient, routeBuilder), IOnlineSessionClient
{
    /// <inheritdoc />
    public Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(OpenOnlineSessionRequest requestPayload, string accessToken, string upoVersion = null, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<OpenOnlineSessionResponse, OpenOnlineSessionRequest>(
            Routes.Sessions.Online.Open,
            requestPayload,
            accessToken,
			!string.IsNullOrEmpty(upoVersion) ?
            new Dictionary<string, string> 
                { { "X-KSeF-Feature", upoVersion } } : null,
			cancellationToken);
    }

    /// <inheritdoc />
    public Task<SendInvoiceResponse> SendOnlineSessionInvoiceAsync(SendInvoiceRequest requestPayload, string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Sessions.Online.Invoices(Uri.EscapeDataString(sessionReferenceNumber));
        return ExecuteAsync<SendInvoiceResponse, SendInvoiceRequest>(endpoint, requestPayload, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task CloseOnlineSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Sessions.Online.Close(Uri.EscapeDataString(sessionReferenceNumber));
        return ExecuteAsync(endpoint, HttpMethod.Post, accessToken, cancellationToken);
    }
}
