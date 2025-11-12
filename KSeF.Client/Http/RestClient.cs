using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Http.Helpers;
using System.Net.Http.Headers;
using System.Text;

namespace KSeF.Client.Http;

/// <summary>
/// Generyczny klient REST obsługujący żądania GET, POST i DELETE z opcjonalną autoryzacją,
/// serializacją/deserializacją treści oraz strukturalną obsługą błędów.
/// </summary>
public sealed class RestClient : IRestClient
{
    private readonly HttpClient httpClient;

    public const string DefaultContentType = "application/json";
    public const string XmlContentType = "application/xml";

    public RestClient(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Wysyła żądanie HTTP i zwraca odpowiedź w postaci obiektu typu TResponse.
    /// </summary>
    public async Task<TResponse> SendAsync<TResponse, TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string bearerToken = null,
        string contentType = RestContentTypeExtensions.DefaultContentType,
        CancellationToken cancellationToken = default,
        Dictionary<string, string> additionalHeaders = null)
    {
        if (method is null) throw new ArgumentNullException(nameof(method));
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, url);

        bool shouldSendBody = method != HttpMethod.Get &&
                              !EqualityComparer<TRequest>.Default.Equals(requestBody, default);

        if (shouldSendBody)
        {
            string requestContent = RestContentTypeExtensions.IsDefaultType(contentType)
                ? JsonUtil.Serialize(requestBody)
                : requestBody?.ToString();

            if (!string.IsNullOrEmpty(requestContent))
            {
                httpRequestMessage.Content = new StringContent(requestContent, Encoding.UTF8, contentType);
            }
        }

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (additionalHeaders is not null)
        {
            foreach (KeyValuePair<string, string> header in additionalHeaders)
            {
                httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return await SendCoreAsync<TResponse>(httpRequestMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Wysyła żądanie HTTP z podanym HttpContent, bez serializacji obiektów.
    /// Przeznaczone do ręcznego przesyłania danych binarnych (np. plików, strumieni).
    /// Nie modyfikuje zawartości ani nie zmienia nagłówków.
    /// </summary>
    public async Task SendAsync(
        HttpMethod method,
        string url,
        HttpContent content,
        IDictionary<string, string> additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        if (method is null) throw new ArgumentNullException(nameof(method));
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        if (content is null) throw new ArgumentNullException(nameof(content));

        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, url)
        {
            Content = content
        };

        if (additionalHeaders is not null)
        {
            foreach (KeyValuePair<string, string> header in additionalHeaders)
            {
                httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        await SendCoreAsync<object>(httpRequestMessage, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync<TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string token = null,
        string contentType = RestContentTypeExtensions.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        await SendAsync<object, TRequest>(method, url, requestBody, token, contentType, cancellationToken, additionalHeaders: null)
            .ConfigureAwait(false);
    }

    public async Task SendAsync(
        HttpMethod method,
        string url,
        string token = null,
        string contentType = RestContentTypeExtensions.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        if (method is null) throw new ArgumentNullException(nameof(method));
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, url);

        if (!string.IsNullOrWhiteSpace(token))
        {
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        await SendCoreAsync<string>(httpRequestMessage, cancellationToken).ConfigureAwait(false);
    }

    // ================== RestRequest overloads ==================
    public async Task<TResponse> SendAsync<TResponse>(RestRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using HttpRequestMessage httpRequestMessage = request.ToHttpRequestMessage(httpClient);
        using CancellationTokenSource cancellationTokenSource = CreateTimeoutCancellationTokenSource(request.Timeout, cancellationToken);

        return await SendCoreAsync<TResponse>(httpRequestMessage, cancellationTokenSource.Token).ConfigureAwait(false);        
    }

    /// <summary>
    /// Wysyła żądanie zdefiniowane przez <see cref="RestRequest"/> bez deserializacji zwróconego obiektu.
    /// </summary>
    public Task SendAsync(RestRequest request, CancellationToken cancellationToken = default)
        => SendAsync<object>(request, cancellationToken);

    public Task<TResponse> ExecuteAsync<TResponse>(RestRequest request, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(request, cancellationToken);

    public Task ExecuteAsync(RestRequest request, CancellationToken cancellationToken = default)
        => SendAsync(request, cancellationToken);

    public Task<TResponse> ExecuteAsync<TResponse, TRequest>(RestRequest<TRequest> request, CancellationToken cancellationToken = default)
        => SendAsync<TResponse, TRequest>(request, cancellationToken);

    public async Task<TResponse> SendAsync<TResponse, TRequest>(RestRequest<TRequest> request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using HttpRequestMessage httpRequestMessage = request.ToHttpRequestMessage(httpClient, DefaultContentType);
        using CancellationTokenSource cancellationTokenSource = CreateTimeoutCancellationTokenSource(request.Timeout, cancellationToken);

        return await SendCoreAsync<TResponse>(httpRequestMessage, cancellationTokenSource.Token).ConfigureAwait(false);        
    }

    public Task SendAsync<TRequest>(RestRequest<TRequest> request, CancellationToken cancellationToken = default)
        => SendAsync<object, TRequest>(request, cancellationToken);

    // ================== Core ==================
    private async Task<T> SendCoreAsync<T>(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
    {
        using HttpResponseMessage httpResponseMessage = await httpClient
            .SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        bool hasContent = httpResponseMessage.HasBody(httpRequestMessage.Method);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            if (!hasContent || typeof(T) == typeof(object))
            {
                return default!;
            }

            if (typeof(T) == typeof(string))
            {
                string responseText = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return (T)(object)(responseText ?? string.Empty);
            }

            MediaTypeHeaderValue? contentTypeHeader = httpResponseMessage.Content?.Headers?.ContentType;
            string? mediaType = contentTypeHeader?.MediaType;

            if (!IsJsonMediaType(mediaType))
            {
                throw new KsefApiException($"Unexpected content type '{mediaType ?? "unknown"}' for {typeof(T).Name}.", httpResponseMessage.StatusCode);
            }

            using Stream responseStream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonUtil.DeserializeAsync<T>(responseStream).ConfigureAwait(false);
        }

        await HandleInvalidStatusCode(httpResponseMessage, cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException("HandleInvalidStatusCode must throw.");
    }

    /// <summary>
    /// Mapuje nie-2xx odpowiedzi na wyjątki.
    /// Guard na Content-Type.
    /// </summary>
    private static async Task HandleInvalidStatusCode(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.NotFound:
                throw new KsefApiException("Not found", response.StatusCode);

            case System.Net.HttpStatusCode.TooManyRequests:
                await HandleTooManyRequestsAsync(response, cancellationToken);
                return;

            default:
                await HandleOtherErrorsAsync(response, cancellationToken);
                return;
        }

        static bool TryExtractRetryAfterHeaderValue(HttpResponseMessage responseMessage, out string? retryAfterHeaderValue)
        {
            retryAfterHeaderValue = null;

            if (responseMessage.Headers.RetryAfter?.Delta is TimeSpan delta)
            {
                retryAfterHeaderValue = ((int)delta.TotalSeconds).ToString();
                return true;
            }
            if (responseMessage.Headers.RetryAfter?.Date is DateTimeOffset date)
            {
                retryAfterHeaderValue = date.ToString("R");
                return true;
            }
            if (responseMessage.Headers.TryGetValues("Retry-After", out IEnumerable<string>? values))
            {
                string? headerValue = values.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(headerValue))
                {
                    retryAfterHeaderValue = headerValue;
                    return true;
                }
            }
            return false;
        }

        static bool IsJsonMediaType(string? mediaType)
        {
            return !string.IsNullOrEmpty(mediaType) &&
                   mediaType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsJsonContent(HttpResponseMessage responseMessage)
        {
            MediaTypeHeaderValue? contentTypeHeader = responseMessage.Content?.Headers?.ContentType;
            return IsJsonMediaType(contentTypeHeader?.MediaType);
        }

        static string BuildErrorMessageFromDetails(ApiErrorResponse? apiErrorResponse)
        {
            if (apiErrorResponse?.Exception?.ExceptionDetailList is not { Count: > 0 })
                return string.Empty;

            IEnumerable<string> parts = apiErrorResponse.Exception.ExceptionDetailList.Select(detail =>
            {
                string detailsText = (detail.Details is { Count: > 0 }) ? string.Join("; ", detail.Details) : string.Empty;
                return string.IsNullOrEmpty(detailsText)
                    ? $"{detail.ExceptionCode}: {detail.ExceptionDescription}"
                    : $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {detailsText}";
            });

            return string.Join(" | ", parts);
        }

        static bool TryDeserializeJson<T>(string json, out T? result)
        {
            try
            {
                result = JsonUtil.Deserialize<T>(json);
                return true;
            }
            catch (Exception)
            {
                result = default;
                return false;
            }
        }

        static async Task HandleTooManyRequestsAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
        {
            string rateLimitMessage = "Przekroczono limit ilości zapytań do API (HTTP 429)";

            TryExtractRetryAfterHeaderValue(responseMessage, out string? retryAfterHeaderValue);

            string? responseBody = responseMessage.Content is null
                ? null
                : await responseMessage.Content.ReadAsStringAsync(innerCancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(responseBody) && IsJsonContent(responseMessage))
            {
                if (TryDeserializeJson<ApiErrorResponse>(responseBody, out ApiErrorResponse? apiErrorResponse) && apiErrorResponse is not null)
                {
                    string detailedMessage = BuildErrorMessageFromDetails(apiErrorResponse);
                    if (!string.IsNullOrWhiteSpace(detailedMessage))
                    {
                        rateLimitMessage = detailedMessage;
                    }

                    throw KsefRateLimitException.FromRetryAfterHeader(
                        rateLimitMessage,
                        retryAfterHeaderValue,
                        apiErrorResponse);
                }
            }

            throw KsefRateLimitException.FromRetryAfterHeader(rateLimitMessage, retryAfterHeaderValue);
        }

        static async Task HandleOtherErrorsAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
        {
            string? responseBody = responseMessage.Content is null
                ? null
                : await responseMessage.Content.ReadAsStringAsync(innerCancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(responseBody))
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? "Unknown"}",
                    responseMessage.StatusCode);
            }

            if (!IsJsonContent(responseMessage))
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? "Unknown"}",
                    responseMessage.StatusCode);
            }

            try
            {
                ApiErrorResponse apiErrorResponse = JsonUtil.Deserialize<ApiErrorResponse>(responseBody);
                string fullMessage = BuildErrorMessageFromDetails(apiErrorResponse);
                throw new KsefApiException(fullMessage, responseMessage.StatusCode, apiErrorResponse);
            }
            catch (KsefApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? "Unknown"}, AdditionalInfo: {ex.Message}",
                    responseMessage.StatusCode,
                    innerException: ex);
            }
        }
    }

    private static bool IsJsonMediaType(string? mediaType)
    {
        return !string.IsNullOrEmpty(mediaType) &&
               mediaType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static CancellationTokenSource CreateTimeoutCancellationTokenSource(TimeSpan? perRequestTimeout, CancellationToken cancellationToken)
    {
        if (perRequestTimeout is null)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(perRequestTimeout.Value);
        return cancellationTokenSource;
    }
}
