using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Lighthouse;
using KSeF.Client.DI;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public sealed class LighthouseClient : ClientBase, ILighthouseClient
{
    private readonly Uri _statusUri;
    private readonly Uri _messagesUri;

    public LighthouseClient(IRestClient restClient, IRouteBuilder routeBuilder, LighthouseClientOptions options)
        : base(restClient, routeBuilder)
    {
        Guard.ThrowIfNull(options);

        string baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? LighthouseEnvironmentsUris.PROD
            : options.BaseUrl;

        Uri baseUri = new Uri(baseUrl, UriKind.Absolute);
        _statusUri = new Uri(baseUri, "/status");
        _messagesUri = new Uri(baseUri, "/messages");
    }

    /// <inheritdoc />
    public Task<KsefStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync<KsefStatusResponse>(_statusUri, HttpMethod.Get, cancellationToken);

    /// <inheritdoc />
    public Task<KsefMessagesResponse> GetMessagesAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync<KsefMessagesResponse>(_messagesUri, HttpMethod.Get, cancellationToken);
}
