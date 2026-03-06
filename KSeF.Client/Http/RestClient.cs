using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Http.Helpers;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace KSeF.Client.Http;

/// <inheritdoc />
public sealed class RestClient(HttpClient httpClient) : IRestClient
{
    private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <summary>
    /// Domyślny typ treści żądania REST.
    /// </summary>
    public const string DefaultContentType = "application/json";

    /// <summary>
    /// Typ treści XML.
    /// </summary>
    public const string XmlContentType = "application/xml";

    private const string UnauthorizedText = "Unauthorized";
    private const string ForbiddenText = "Forbidden";
    private const string UnknownText = "Unknown";
    private const string ProblemDetailsText = "ProblemDetails";
    private const string ServiceNameText = "KSeF API";
    private const string NotFoundText = "Not found";
    private const string RateLimitText = "Przekroczono limit ilości zapytań do API (HTTP 429)";
    private const string BearerScheme = "Bearer";
    private const string UnknownMediaTypeText = "nieznany";

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TResponse, TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string token = null,
        string contentType = "application/json",
        CancellationToken cancellationToken = default)
    {
        return await SendAsync<TResponse, TRequest>(method, url, requestBody, token, contentType, additionalHeaders: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TResponse, TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string token = null,
        string contentType = RestContentTypeExtensions.DefaultContentType,
        Dictionary<string, string> additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        RestResponse<TResponse> response = await SendWithHeadersAsync<TResponse, TRequest>(
            method,
            url,
            requestBody,
            token,
            contentType,
            additionalHeaders,
            cancellationToken).ConfigureAwait(false);

        return response.Body;
    }

    /// <inheritdoc />
    public async Task<RestResponse<TResponse>> SendWithHeadersAsync<TResponse, TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string token = null,
        string contentType = RestContentTypeExtensions.DefaultContentType,
        Dictionary<string, string> additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(method);
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Adres URL nie może być pusty.", nameof(url));
        }

        using HttpRequestMessage httpRequestMessage = new(method, url);

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

        if (!string.IsNullOrWhiteSpace(token))
        {
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, token);
        }

        if (additionalHeaders is not null)
        {
            foreach (KeyValuePair<string, string> header in additionalHeaders)
            {
                httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return await SendCoreWithHeadersAsync<TResponse>(httpRequestMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync(
        HttpMethod method,
        string url,
        HttpContent content,
        IDictionary<string, string> additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(method);
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Adres URL nie może być pusty.", nameof(url));
        }

        Guard.ThrowIfNull(content);

        using HttpRequestMessage httpRequestMessage = new(method, url)
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

    /// <inheritdoc />
    public async Task SendAsync<TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string token = null,
        string contentType = RestContentTypeExtensions.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        await SendAsync<object, TRequest>(method, url, requestBody, token, contentType, additionalHeaders: null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync(
        HttpMethod method,
        string url,
        string token = null,
        string contentType = RestContentTypeExtensions.DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(method);
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Adres URL nie może być pusty.", nameof(url));
        }

        using HttpRequestMessage httpRequestMessage = new(method, url);

        if (!string.IsNullOrWhiteSpace(token))
        {
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, token);
        }

        await SendCoreAsync<string>(httpRequestMessage, cancellationToken).ConfigureAwait(false);
    }

    // ================== RestRequest overloads ==================
    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TResponse>(RestRequest request, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(request);

        using HttpRequestMessage httpRequestMessage = request.ToHttpRequestMessage(httpClient);
        using CancellationTokenSource cancellationTokenSource = CreateTimeoutCancellationTokenSource(request.Timeout, cancellationToken);

        return await SendCoreAsync<TResponse>(httpRequestMessage, cancellationTokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task SendAsync(RestRequest request, CancellationToken cancellationToken = default)
        => SendAsync<object>(request, cancellationToken);

    /// <inheritdoc />
    public Task<TResponse> ExecuteAsync<TResponse>(RestRequest request, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(request, cancellationToken);

    /// <inheritdoc />
    public Task ExecuteAsync(RestRequest request, CancellationToken cancellationToken = default)
        => SendAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<TResponse> ExecuteAsync<TResponse, TRequest>(RestRequest<TRequest> request, CancellationToken cancellationToken = default)
        => SendAsync<TResponse, TRequest>(request, cancellationToken);

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TResponse, TRequest>(RestRequest<TRequest> request, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(request);

        using HttpRequestMessage httpRequestMessage = request.ToHttpRequestMessage(httpClient, DefaultContentType);
        using CancellationTokenSource cancellationTokenSource = CreateTimeoutCancellationTokenSource(request.Timeout, cancellationToken);

        return await SendCoreAsync<TResponse>(httpRequestMessage, cancellationTokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
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
                string responseText = await ReadContentAsync(httpResponseMessage, cancellationToken).ConfigureAwait(false);
                return (T)(object)(responseText ?? string.Empty);
            }

            MediaTypeHeaderValue contentTypeHeader = httpResponseMessage.Content?.Headers?.ContentType;
            string mediaType = contentTypeHeader?.MediaType;

            if (!IsJsonMediaType(mediaType))
            {
                throw new KsefApiException($"Nieoczekiwany typ treści '{mediaType ?? UnknownMediaTypeText}' dla {typeof(T).Name}.", httpResponseMessage.StatusCode);
            }

#if NETSTANDARD2_0
            using Stream responseStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
            using Stream responseStream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#endif
            return await JsonUtil.DeserializeAsync<T>(responseStream).ConfigureAwait(false);
        }

        await HandleInvalidStatusCode(httpResponseMessage, cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException("HandleInvalidStatusCode musi zgłosić wyjątek.");
    }

    private async Task<RestResponse<T>> SendCoreWithHeadersAsync<T>(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
    {
        using HttpResponseMessage httpResponseMessage = await httpClient
            .SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        bool hasContent = httpResponseMessage.HasBody(httpRequestMessage.Method);

        Dictionary<string, IEnumerable<string>> headers = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, IEnumerable<string>> header in httpResponseMessage.Headers)
        {
            headers[header.Key] = header.Value;
        }

        if (httpResponseMessage.Content is not null)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in httpResponseMessage.Content.Headers)
            {
                headers[header.Key] = header.Value;
            }
        }

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            if (!hasContent || typeof(T) == typeof(object))
            {
                return new RestResponse<T>(default!, headers);
            }

            if (typeof(T) == typeof(string))
            {
                string responseText = await ReadContentAsync(httpResponseMessage, cancellationToken).ConfigureAwait(false);
                return new RestResponse<T>((T)(object)(responseText ?? string.Empty), headers);
            }

            MediaTypeHeaderValue contentTypeHeader = httpResponseMessage.Content?.Headers?.ContentType;
            string mediaType = contentTypeHeader?.MediaType;

            if (!IsJsonMediaType(mediaType))
            {
                throw new KsefApiException($"Nieoczekiwany typ treści '{mediaType ?? UnknownMediaTypeText}' dla {typeof(T).Name}.", httpResponseMessage.StatusCode);
            }

#if NETSTANDARD2_0
            using Stream responseStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
            using Stream responseStream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#endif
            T body = await JsonUtil.DeserializeAsync<T>(responseStream).ConfigureAwait(false);
            return new RestResponse<T>(body, headers);
        }

        await HandleInvalidStatusCode(httpResponseMessage, cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException("HandleInvalidStatusCode musi zgłosić wyjątek.");
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
                throw new KsefApiException(NotFoundText, response.StatusCode);

            case System.Net.HttpStatusCode.Unauthorized:
                await HandleUnauthorizedAsync(response, cancellationToken).ConfigureAwait(false);
                return;

            case System.Net.HttpStatusCode.Forbidden:
                await HandleForbiddenAsync(response, cancellationToken).ConfigureAwait(false);
                return;

#if NETSTANDARD2_0
            case (System.Net.HttpStatusCode)429:
#else
            case System.Net.HttpStatusCode.TooManyRequests:
#endif
                await HandleTooManyRequestsAsync(response, cancellationToken).ConfigureAwait(false);
                return;

            default:
                await HandleOtherErrorsAsync(response, cancellationToken).ConfigureAwait(false);
                return;
        }

        static bool TryExtractRetryAfterHeaderValue(HttpResponseMessage responseMessage, out string retryAfterHeaderValue)
        {
            retryAfterHeaderValue = null;

            if (responseMessage.Headers.RetryAfter?.Delta is TimeSpan delta)
            {
                retryAfterHeaderValue = ((int)delta.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                return true;
            }
            if (responseMessage.Headers.RetryAfter?.Date is DateTimeOffset date)
            {
                retryAfterHeaderValue = date.ToString("R");
                return true;
            }
            if (responseMessage.Headers.TryGetValues("Retry-After", out IEnumerable<string> values))
            {
                string headerValue = values.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(headerValue))
                {
                    retryAfterHeaderValue = headerValue;
                    return true;
                }
            }
            return false;
        }

        static bool IsJsonContent(HttpResponseMessage responseMessage)
        {
            MediaTypeHeaderValue contentTypeHeader = responseMessage.Content?.Headers?.ContentType;
            return IsJsonMediaType(contentTypeHeader?.MediaType);
        }

        static string BuildErrorMessageFromDetails(ApiErrorResponse apiErrorResponse)
        {
            if (apiErrorResponse?.Exception?.ExceptionDetailList is not { Count: > 0 })
            {
                return string.Empty;
            }

            IEnumerable<string> parts = apiErrorResponse.Exception.ExceptionDetailList.Select(detail =>
            {
                string detailsText = (detail.Details is { Count: > 0 }) ? string.Join("; ", detail.Details) : string.Empty;
                return string.IsNullOrEmpty(detailsText)
                    ? $"{detail.ExceptionCode}: {detail.ExceptionDescription}"
                    : $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {detailsText}";
            });

            return string.Join(" | ", parts);
        }

        static ApiErrorResponse MapProblemDetailsToApiErrorResponse(
            string title,
            int status,
            string detail,
            string traceId = null,
            string instance = null,
            string reasonCode = null,
            object security = null)
        {
            List<string> details = new List<string>();

            static void AddIfNotEmpty(List<string> list, string value, string prefix = "")
            {
                if (!string.IsNullOrWhiteSpace(value))
                    list.Add(prefix + value);
            }

            AddIfNotEmpty(details, detail);
            AddIfNotEmpty(details, instance, "instance: ");
            AddIfNotEmpty(details, reasonCode, "reasonCode: ");

            if (security is not null)
            {
                try
                {
                    string secJson = JsonUtil.Serialize(security);
                    if (!string.IsNullOrWhiteSpace(secJson) && secJson != "null")
                        details.Add($"security: {secJson}");
                }
                catch
                {
                }
            }

            AddIfNotEmpty(details, traceId, "traceId: ");

            return new ApiErrorResponse
            {
                Exception = new ApiExceptionContent
                {
                    Timestamp = DateTime.UtcNow,
                    ServiceName = ServiceNameText,
                    ReferenceNumber = traceId,
                    ExceptionDetailList = new List<ApiExceptionDetail>
                    {
                        new ApiExceptionDetail(
                            status,
                            string.IsNullOrWhiteSpace(reasonCode)
                                ? (title ?? ProblemDetailsText)
                                : $"{title ?? ProblemDetailsText} ({reasonCode})",
                            details)
                    }
                }
            };
        }

        static bool TryDeserializeJson<T>(string json, out T result)
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
            string rateLimitMessage = RateLimitText;

            TryExtractRetryAfterHeaderValue(responseMessage, out string retryAfterHeaderValue);

            string responseBody = await ReadContentAsync(responseMessage, innerCancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(responseBody) && IsJsonContent(responseMessage))
            {
                if (TryDeserializeJson<TooManyRequestsErrorResponse>(
                        responseBody,
                        out TooManyRequestsErrorResponse statusErrorResponse)
                    && statusErrorResponse?.Status?.Details?.Any() == true)
                {
                    string rateLimitDetails = string.Join(" ", statusErrorResponse.Status.Details);
                    rateLimitMessage = rateLimitDetails;
                }
            }

            throw KsefRateLimitException.FromRetryAfterHeader(rateLimitMessage, retryAfterHeaderValue);
        }

        static async Task HandleOtherErrorsAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
        {
            string responseBody = await ReadContentAsync(responseMessage, innerCancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(responseBody))
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? UnknownText}",
                    responseMessage.StatusCode);
            }

            if (!IsJsonContent(responseMessage))
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? UnknownText}",
                    responseMessage.StatusCode);
            }

            try
            {
                ApiErrorResponse apiErrorResponse = JsonUtil.Deserialize<ApiErrorResponse>(responseBody);
                string fullMessage = BuildErrorMessageFromDetails(apiErrorResponse);
                string exceptionMessage = string.IsNullOrWhiteSpace(fullMessage) ? responseBody : fullMessage;
                throw new KsefApiException(exceptionMessage, responseMessage.StatusCode, apiErrorResponse);
            }
            catch (KsefApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? UnknownText}, AdditionalInfo: {ex.Message}",
                    responseMessage.StatusCode,
                    innerException: ex);
            }
        }

        static async Task HandleUnauthorizedAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
        {
            string responseBody = await ReadContentAsync(responseMessage, innerCancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(responseBody))
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? UnauthorizedText}",
                    responseMessage.StatusCode);
            }

            if (!IsJsonContent(responseMessage))
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? UnauthorizedText}",
                    responseMessage.StatusCode);
            }

            if (TryDeserializeJson<UnauthorizedProblemDetails>(responseBody, out UnauthorizedProblemDetails unauthorizedDetails) && unauthorizedDetails is not null)
            {
                string detailsText = string.IsNullOrWhiteSpace(unauthorizedDetails.Detail) ? unauthorizedDetails.Title ?? UnauthorizedText : unauthorizedDetails.Detail;
                if (!string.IsNullOrWhiteSpace(unauthorizedDetails.TraceId))
                {
                    detailsText = detailsText + $" (traceId: {unauthorizedDetails.TraceId})";
                }

                ApiErrorResponse mapped = MapProblemDetailsToApiErrorResponse(
                    title: unauthorizedDetails.Title ?? UnauthorizedText,
                    status: unauthorizedDetails.Status,
                    detail: unauthorizedDetails.Detail,
                    traceId: unauthorizedDetails.TraceId,
                    instance: unauthorizedDetails.Instance);

                throw new KsefApiException(detailsText, responseMessage.StatusCode, mapped);
            }

            if (TryDeserializeJson<ApiErrorResponse>(responseBody, out ApiErrorResponse apiError) && apiError is not null)
            {
                string errorMessage = BuildErrorMessageFromDetails(apiError);
                throw new KsefApiException(string.IsNullOrEmpty(errorMessage) ? responseBody : errorMessage, responseMessage.StatusCode, apiError);
            }

            throw new KsefApiException($"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? UnauthorizedText}", responseMessage.StatusCode);
        }

        static async Task HandleForbiddenAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
        {
            string responseBody = await ReadContentAsync(responseMessage, innerCancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(responseBody))
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? ForbiddenText}",
                    responseMessage.StatusCode);
            }

            if (!IsJsonContent(responseMessage))
            {
                throw new KsefApiException(
                    $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? ForbiddenText}",
                    responseMessage.StatusCode);
            }

            if (TryDeserializeJson<ForbiddenProblemDetails>(responseBody, out ForbiddenProblemDetails forbiddenDetails) && forbiddenDetails is not null)
            {
                StringBuilder messageBuilder = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(forbiddenDetails.ReasonCode))
                {
                    messageBuilder.Append(forbiddenDetails.ReasonCode);
                }

                if (!string.IsNullOrWhiteSpace(forbiddenDetails.Detail))
                {
                    if (messageBuilder.Length > 0)
                    {
                        messageBuilder.Append(": ");
                    }
                    messageBuilder.Append(forbiddenDetails.Detail);
                }

                if (forbiddenDetails.Security is not null && forbiddenDetails.Security.Count > 0)
                {
                    try
                    {
                        string securityJson = JsonUtil.Serialize(forbiddenDetails.Security);
                        if (!string.IsNullOrWhiteSpace(securityJson))
                        {
                            messageBuilder.Append($" (security: {securityJson})");
                        }
                    }
                    catch
                    {
                    }
                }

                if (!string.IsNullOrWhiteSpace(forbiddenDetails.TraceId))
                {
                    if (messageBuilder.Length > 0)
                    {
                        messageBuilder.Append(" ");
                    }
                    messageBuilder.Append($"(traceId: {forbiddenDetails.TraceId})");
                }

                string finalMessage = messageBuilder.Length > 0 ? messageBuilder.ToString() : (forbiddenDetails.Title ?? ForbiddenText);

                ApiErrorResponse mapped = MapProblemDetailsToApiErrorResponse(
                    title: forbiddenDetails.Title ?? ForbiddenText,
                    status: forbiddenDetails.Status,
                    detail: forbiddenDetails.Detail,
                    traceId: forbiddenDetails.TraceId,
                    instance: forbiddenDetails.Instance,
                    reasonCode: forbiddenDetails.ReasonCode,
                    security: forbiddenDetails.Security);

                throw new KsefApiException(finalMessage, responseMessage.StatusCode, mapped);
            }

            if (TryDeserializeJson<ApiErrorResponse>(responseBody, out ApiErrorResponse apiError) && apiError is not null)
            {
                string errorMessage = BuildErrorMessageFromDetails(apiError);
                throw new KsefApiException(string.IsNullOrEmpty(errorMessage) ? responseBody : errorMessage, responseMessage.StatusCode, apiError);
            }

            throw new KsefApiException($"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? ForbiddenText}", responseMessage.StatusCode);
        }
    }

    private static bool IsJsonMediaType(string mediaType)
    {
        return !string.IsNullOrEmpty(mediaType) &&
               mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
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

    private static async Task<string> ReadContentAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp?.Content is null)
        {
            return null;
        }

#if NETSTANDARD2_0
        return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
        return await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#endif
    }
}