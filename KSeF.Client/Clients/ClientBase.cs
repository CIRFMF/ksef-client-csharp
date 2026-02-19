using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Infrastructure.Rest;

namespace KSeF.Client.Clients;

public abstract class ClientBase(IRestClient restClient, IRouteBuilder routeBuilder)
{   
    protected virtual Task ExecuteAsync(string relativeEndpoint, HttpMethod httpMethod, CancellationToken cancellationToken)
    {
        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest requestPayload =  RestRequest
            .New(path, httpMethod);

        return restClient.ExecuteAsync(requestPayload, cancellationToken);
    }

    protected virtual Task ExecuteAsync<TRequest>(string relativeEndpoint, TRequest body, CancellationToken cancellationToken)
    {
        Guard.ThrowIfNull(body);

        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest<TRequest>requestPayload = RestRequest
            .New(path, HttpMethod.Post)
            .WithBody(body);

        return restClient.ExecuteAsync<object, TRequest>(requestPayload, cancellationToken);
    }

    protected virtual Task ExecuteAsync<TRequest>(string relativeEndpoint, TRequest body, string accessToken, CancellationToken cancellationToken)
    {
        Guard.ThrowIfNull(body);

        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest<TRequest> request = RestRequest
            .New(path, HttpMethod.Post)
            .WithBody(body)
            .AddAccessToken(accessToken);

        return restClient.ExecuteAsync<object, TRequest>(request, cancellationToken);
    }

    protected virtual Task<TResponse> ExecuteAsync<TResponse, TRequest>(string relativeEndpoint, TRequest body, CancellationToken cancellationToken)
    {
        Guard.ThrowIfNull(body);

        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest<TRequest>requestPayload = RestRequest
            .New(path, HttpMethod.Post)
            .WithBody(body);

        return restClient.ExecuteAsync<TResponse, TRequest>(requestPayload, cancellationToken);
    }

    protected virtual Task<TResponse> ExecuteAsync<TResponse, TRequest>(string relativeEndpoint, TRequest body, string accessToken, CancellationToken cancellationToken)
    {
        Guard.ThrowIfNull(body);

        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest<TRequest>requestPayload = RestRequest
            .New(path, HttpMethod.Post)
            .WithBody(body)
            .AddAccessToken(accessToken);

        return restClient.ExecuteAsync<TResponse, TRequest>(requestPayload, cancellationToken);
    }

    protected virtual Task<TResponse> ExecuteAsync<TResponse>(string relativeEndpoint, HttpMethod httpMethod, CancellationToken cancellationToken)
    {
        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest requestPayload =  RestRequest
            .New(path, httpMethod);

        return restClient.ExecuteAsync<TResponse>(requestPayload, cancellationToken);
    }

    protected virtual Task<TResponse> ExecuteAsync<TResponse>(string relativeEndpoint, HttpMethod httpMethod, string accessToken, CancellationToken cancellationToken)
    {
        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest requestPayload =  RestRequest
            .New(path, httpMethod)
            .AddAccessToken(accessToken);

        return restClient.ExecuteAsync<TResponse>(requestPayload, cancellationToken);
    }

    protected virtual Task ExecuteAsync(string relativeEndpoint, HttpMethod httpMethod, string accessToken, CancellationToken cancellationToken)
    {
        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest requestPayload =  RestRequest
            .New(path, httpMethod)
            .AddAccessToken(accessToken);

        return restClient.ExecuteAsync(requestPayload, cancellationToken);
    }
      
    protected virtual Task<TResponse> ExecuteAsync<TResponse>(string relativeEndpoint, HttpMethod httpMethod, string accessToken, IDictionary<string, string> additionalHeaders, CancellationToken cancellationToken)
    {
        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest requestPayload =  RestRequest
            .New(path, httpMethod)
            .AddAccessToken(accessToken);

        if (additionalHeaders is { Count: > 0 })
        {
            foreach (KeyValuePair<string, string> header in additionalHeaders)
            {
                requestPayload.AddHeader(header.Key, header.Value);
            }
        }

        return restClient.ExecuteAsync<TResponse>(requestPayload, cancellationToken);
    }

    protected virtual Task<TResponse> ExecuteAsync<TResponse, TRequest>(string relativeEndpoint, TRequest body, string accessToken, IDictionary<string, string> additionalHeaders, CancellationToken cancellationToken)
    {
        Guard.ThrowIfNull(body);

        string path = routeBuilder.Build(relativeEndpoint);
        RestRequest<TRequest>requestPayload = RestRequest
            .New(path, HttpMethod.Post)
            .WithBody(body)
            .AddAccessToken(accessToken);

        if (additionalHeaders is { Count: > 0 })
        {
            foreach (KeyValuePair<string, string> header in additionalHeaders)
            {
                requestPayload.AddHeader(header.Key, header.Value);
            }
        }

        return restClient.ExecuteAsync<TResponse, TRequest>(requestPayload, cancellationToken);
    }

    protected virtual Task<TResponse> ExecuteAsync<TResponse>(Uri absoluteUri, HttpMethod httpMethod, CancellationToken cancellationToken)
    {
        Guard.ThrowIfNull(absoluteUri);

        RestRequest requestPayload =  RestRequest
            .New(absoluteUri.ToString(), httpMethod);

        return restClient.ExecuteAsync<TResponse>(requestPayload, cancellationToken);
    }
}