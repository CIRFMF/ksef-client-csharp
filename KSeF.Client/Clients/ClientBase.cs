using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Infrastructure.Rest;

namespace KSeF.Client.Clients;

public abstract class ClientBase(IRestClient restClient, IRouteBuilder routeBuilder)
{
    protected readonly IRestClient _restClient = restClient;
    protected readonly IRouteBuilder _routeBuilder = routeBuilder;

    protected virtual Task ExecuteAsync(string relativeEndpoint, HttpMethod httpMethod, CancellationToken cancellationToken)
    {
        string path = _routeBuilder.Build(relativeEndpoint);
        RestRequest req = RestRequest
            .New(path, httpMethod);

        return _restClient.ExecuteAsync(req, cancellationToken);
    }

    protected virtual Task ExecuteAsync<TRequest>(string relativeEndpoint, TRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        string path = _routeBuilder.Build(relativeEndpoint);
        RestRequest<TRequest> req = RestRequest
            .New(path, HttpMethod.Post)
            .WithBody(body);

        return _restClient.ExecuteAsync<object, TRequest>(req, cancellationToken);
    }

    protected virtual Task ExecuteAsync<TRequest>(string relativeEndpoint, TRequest body, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        string path = _routeBuilder.Build(relativeEndpoint);
        RestRequest<TRequest> req = RestRequest
            .New(path, HttpMethod.Post)
            .WithBody(body)
            .AddAccessToken(accessToken);

        return _restClient.ExecuteAsync<object, TRequest>(req, cancellationToken);
    }

    protected virtual Task<TResponse> ExecuteAsync<TResponse, TRequest>(string relativeEndpoint, TRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        string path = _routeBuilder.Build(relativeEndpoint);
        RestRequest<TRequest> req = RestRequest
            .New(path, HttpMethod.Post)
            .WithBody(body);

        return _restClient.ExecuteAsync<TResponse, TRequest>(req, cancellationToken);
    }

    protected virtual Task<TResponse> ExecuteAsync<TResponse>(string relativeEndpoint, HttpMethod httpMethod, CancellationToken cancellationToken)
    {
        string path = _routeBuilder.Build(relativeEndpoint);
        RestRequest req = RestRequest
            .New(path, httpMethod);

        return _restClient.ExecuteAsync<TResponse>(req, cancellationToken);
    }

    protected virtual Task<TResponse> ExecuteAsync<TResponse>(string relativeEndpoint, HttpMethod httpMethod, string accessToken, CancellationToken cancellationToken)
    {
        string path = _routeBuilder.Build(relativeEndpoint);
        RestRequest req = RestRequest
            .New(path, httpMethod)
            .AddAccessToken(accessToken);

        return _restClient.ExecuteAsync<TResponse>(req, cancellationToken);
    }

    protected virtual Task ExecuteAsync(string relativeEndpoint, HttpMethod httpMethod, string accessToken, CancellationToken cancellationToken)
    {
        string path = _routeBuilder.Build(relativeEndpoint);
        RestRequest req = RestRequest
            .New(path, httpMethod)
            .AddAccessToken(accessToken);

        return _restClient.ExecuteAsync(req, cancellationToken);
    }

  
    protected virtual Task<TResponse> ExecuteAsync<TResponse>(string relativeEndpoint, HttpMethod httpMethod, string accessToken, IDictionary<string, string> additionalHeaders, CancellationToken cancellationToken)
    {
        string path = _routeBuilder.Build(relativeEndpoint);
        RestRequest req = RestRequest
            .New(path, httpMethod)
            .AddAccessToken(accessToken);

        if (additionalHeaders is { Count: > 0 })
        {
            foreach (KeyValuePair<string, string> header in additionalHeaders)
            {
                req.AddHeader(header.Key, header.Value);
            }
        }

        return _restClient.ExecuteAsync<TResponse>(req, cancellationToken);
    }

}