using KSeF.Client.Http;
using KSeFClient.Core.Exceptions;
using KSeFClient.Core.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace KSeFClient.Http;

/// <summary>
/// A generic REST client that supports GET, POST, and DELETE requests with optional authorization,
/// content serialization/deserialization, and structured error handling.
/// </summary>
public class RestClient : IRestClient
{
    private readonly HttpClient httpClient;

    public const string DefaultContentType = "application/json";
    public const string XmlContentType = "application/xml";


    public RestClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <summary>
    /// Sends an HTTP request and returns the deserialized response.
    /// </summary>
    public async Task<TResponse> SendAsync<TResponse, TRequest>(
    HttpMethod method,
    string url,
    TRequest requestBody = default,
    string bearerToken = null,
    string contentType = DefaultContentType,
    CancellationToken cancellationToken = default,
    Dictionary<string,string> additionalHeaders = default)
    {        
            var request = new HttpRequestMessage(method, url);

            if (requestBody != null && method != HttpMethod.Get)
            {
                var requestContent = contentType == DefaultContentType ? JsonUtil.Serialize(requestBody) : requestBody.ToString();
                request.Content = new StringContent(requestContent!, Encoding.UTF8, contentType);
            }

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }
            if(additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }


        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new KsefApiException("Not found", response.StatusCode);
                }
                try
                {
                    var error = await JsonUtil.DeserializeAsync<ApiErrorResponse>(content);
                    if (error?.Exception?.ExceptionDetailList != null)
                    {
                        var errorMessages = error.Exception.ExceptionDetailList.Select(detail =>
                            $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {string.Join("; ", detail.Details ?? new List<string>())}");

                        var fullMessage = string.Join(" | ", errorMessages);
                        throw new KsefApiException(fullMessage, response.StatusCode, error.Exception.ServiceCode);
                    }

                    throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
                }
                catch (JsonException)
                {
                    throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
                }
            }

            if (content == null || content.Length == 0)
                return default;

        var responseString = await response.Content.ReadAsStringAsync();
        
        if (typeof(TResponse) == typeof(string))
            return (TResponse)(object)responseString;

        return await JsonUtil.DeserializeAsync<TResponse>(content);
    }

    /// <summary>
    /// Sends an HTTP request.
    /// </summary>
    public async Task SendAsync<TRequest>(
    HttpMethod method,
    string url,
    TRequest requestBody = default,
    string bearerToken = null,
    string contentType = DefaultContentType,
    CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, url);

        if (requestBody != null && method != HttpMethod.Get)
        {
            if (contentType == null)
                contentType = DefaultContentType;
            var requestContent = contentType == DefaultContentType ? JsonUtil.Serialize(requestBody) : requestBody.ToString();
            request.Content = new StringContent(requestContent!, Encoding.UTF8, contentType);
        }

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }


        using var response = await httpClient.SendAsync(request, cancellationToken);        

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new KsefApiException("Not found", response.StatusCode);
            }
            try
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = JsonUtil.Deserialize<ApiErrorResponse>(content);
                if (error?.Exception?.ExceptionDetailList != null)
                {
                    var errorMessages = error.Exception.ExceptionDetailList.Select(detail =>
                        $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {string.Join("; ", detail.Details ?? new List<string>())}");

                    var fullMessage = string.Join(" | ", errorMessages);
                    throw new KsefApiException(fullMessage, response.StatusCode, error.Exception.ServiceCode);
                }

                throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
            }
            catch (JsonException)
            {
                throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
            }
        }
    }

    public async Task SendAsync(HttpMethod method, string url, string bearerToken = null, string contentType = "application/json", CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, url);


        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }


        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new KsefApiException("Not found", response.StatusCode);
            }
            try
            {
                var content = await response.Content.ReadAsStreamAsync(cancellationToken);
                var error = await JsonUtil.DeserializeAsync<ApiErrorResponse>(content);
                if (error?.Exception?.ExceptionDetailList != null)
                {
                    var errorMessages = error.Exception.ExceptionDetailList.Select(detail =>
                        $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {string.Join("; ", detail.Details ?? new List<string>())}");

                    var fullMessage = string.Join(" | ", errorMessages);
                    throw new KsefApiException(fullMessage, response.StatusCode, error.Exception.ServiceCode);
                }

                throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
            }
            catch (JsonException)
            {
                throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
            }
        }
    }

    public async Task<string> GetPemAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/public-keys/publicKey.pem");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new KsefApiException("Not found", response.StatusCode);
            } 
        }
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new KsefApiException("Empty response from PEM endpoint", response.StatusCode);
        }
        return content;
    }
}
