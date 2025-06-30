namespace KSeFClient.Core.Interfaces;

public interface IRestClient
{
    Task<TResponse> SendAsync<TResponse, TRequest>(HttpMethod method, string url, TRequest requestBody = default, string bearerToken = null, string contentType = "application/json", CancellationToken cancellationToken = default, Dictionary<string, string> additionalHeaders = null);
    Task SendAsync<TRequest>(HttpMethod method, string url, TRequest requestBody = default, string bearerToken = null, string contentType = "application/json", CancellationToken cancellationToken = default);
    Task SendAsync(HttpMethod method, string url, string bearerToken = null, string contentType = "application/json", CancellationToken cancellationToken = default);
    Task<string> GetPemAsync(CancellationToken cancellation = default);
}

