using KSeF.Client.Core.Exceptions;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace KSeF.Client.Http;

public sealed partial class RestClient
{
    private static readonly IReadOnlyDictionary<System.Net.HttpStatusCode, Func<HttpResponseMessage, CancellationToken, Task>> StatusCodeHandlers =
        new Dictionary<System.Net.HttpStatusCode, Func<HttpResponseMessage, CancellationToken, Task>>
        {
            [System.Net.HttpStatusCode.NotFound]    = HandleNotFoundAsync,
            [System.Net.HttpStatusCode.BadRequest]  = HandleBadRequestAsync,
            [System.Net.HttpStatusCode.Unauthorized] = HandleUnauthorizedAsync,
            [System.Net.HttpStatusCode.Forbidden]   = HandleForbiddenAsync,
            [System.Net.HttpStatusCode.Gone]        = HandleGoneAsync,
            [(System.Net.HttpStatusCode)429]        = HandleTooManyRequestsAsync,
        };

    private static Task HandleNotFoundAsync(HttpResponseMessage responseMessage, CancellationToken _)
        => throw new KsefApiException(NotFoundText, responseMessage.StatusCode);

    private static async Task HandleBadRequestAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
    {
        string responseBody = await ReadContentAsync(responseMessage, innerCancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(responseBody))
        {
            throw new KsefApiException(
                $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? "Bad Request"}",
                responseMessage.StatusCode);
        }

        if (!IsJsonContent(responseMessage))
        {
            throw new KsefApiException(
                $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? "Bad Request"}",
                responseMessage.StatusCode);
        }

        if (TryDeserializeJson(responseBody, out BadRequestProblemDetails badRequestDetails)
            && badRequestDetails?.Errors is { Count: > 0 })
        {
            ApiErrorResponse mapped = MapBadRequestProblemDetailsToApiErrorResponse(badRequestDetails);
            string fullMessage = BuildErrorMessageFromDetails(mapped);
            string exceptionMessage = string.IsNullOrWhiteSpace(fullMessage)
                ? (badRequestDetails.Detail ?? responseBody)
                : fullMessage;
            throw new KsefApiException(exceptionMessage, responseMessage.StatusCode, mapped);
        }

        // Fallback do formatu legacy ApiErrorResponse
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
                $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? "Bad Request"}, AdditionalInfo: {ex.Message}",
                responseMessage.StatusCode,
                innerException: ex);
        }
    }

    private static async Task HandleUnauthorizedAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
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

        if (TryDeserializeJson(responseBody, out UnauthorizedProblemDetails unauthorizedDetails) && unauthorizedDetails is not null)
        {
            string detailsText = string.IsNullOrWhiteSpace(unauthorizedDetails.Detail)
                ? unauthorizedDetails.Title ?? UnauthorizedText
                : unauthorizedDetails.Detail;

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

        if (TryDeserializeJson(responseBody, out ApiErrorResponse apiError) && apiError is not null)
        {
            string errorMessage = BuildErrorMessageFromDetails(apiError);
            throw new KsefApiException(
                string.IsNullOrEmpty(errorMessage) ? responseBody : errorMessage,
                responseMessage.StatusCode,
                apiError);
        }

        throw new KsefApiException(
            $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? UnauthorizedText}",
            responseMessage.StatusCode);
    }

    private static async Task HandleForbiddenAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
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

        if (TryDeserializeJson(responseBody, out ForbiddenProblemDetails forbiddenDetails) && forbiddenDetails is not null)
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
                    messageBuilder.Append(' ');
                }
                messageBuilder.Append($"(traceId: {forbiddenDetails.TraceId})");
            }

            string finalMessage = messageBuilder.Length > 0
                ? messageBuilder.ToString()
                : (forbiddenDetails.Title ?? ForbiddenText);

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

        if (TryDeserializeJson(responseBody, out ApiErrorResponse apiError) && apiError is not null)
        {
            string errorMessage = BuildErrorMessageFromDetails(apiError);
            throw new KsefApiException(
                string.IsNullOrEmpty(errorMessage) ? responseBody : errorMessage,
                responseMessage.StatusCode,
                apiError);
        }

        throw new KsefApiException(
            $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? ForbiddenText}",
            responseMessage.StatusCode);
    }

    private static async Task HandleGoneAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
    {
        string responseBody = await ReadContentAsync(responseMessage, innerCancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(responseBody))
        {
            throw new KsefApiException(
                $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? GoneText}",
                responseMessage.StatusCode);
        }

        if (!IsJsonContent(responseMessage))
        {
            throw new KsefApiException(
                $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? GoneText}",
                responseMessage.StatusCode);
        }

        if (TryDeserializeJson(responseBody, out GoneProblemDetails goneDetails) && goneDetails?.Title is not null)
        {
            string detailsText = string.IsNullOrWhiteSpace(goneDetails.Detail)
                ? goneDetails.Title ?? GoneText
                : goneDetails.Detail;

            ApiErrorResponse mappedError = MapProblemDetailsToApiErrorResponse(
                title: goneDetails.Title ?? GoneText,
                status: goneDetails.Status,
                detail: detailsText,
                traceId: goneDetails.TraceId,
                instance: goneDetails.Instance);

            throw new KsefApiException(detailsText, responseMessage.StatusCode, mappedError);
        }

        if (TryDeserializeJson(responseBody, out ApiErrorResponse apiError) && apiError is not null)
        {
            string errorMessage = BuildErrorMessageFromDetails(apiError);
            throw new KsefApiException(
                string.IsNullOrEmpty(errorMessage) ? responseBody : errorMessage,
                responseMessage.StatusCode,
                apiError);
        }

        throw new KsefApiException(
            $"HTTP {(int)responseMessage.StatusCode}: {responseMessage.ReasonPhrase ?? GoneText}",
            responseMessage.StatusCode);
    }

    private static async Task HandleTooManyRequestsAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
    {
        string rateLimitMessage = RateLimitText;

        TryExtractRetryAfterHeaderValue(responseMessage, out string retryAfterHeaderValue);

        string responseBody = await ReadContentAsync(responseMessage, innerCancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(responseBody) && IsJsonContent(responseMessage))
        {
            if (TryDeserializeJson(responseBody, out TooManyRequestsProblemDetails problemDetails)
                && !string.IsNullOrWhiteSpace(problemDetails?.Detail))
            {
                rateLimitMessage = problemDetails.Detail;
            }
            else if (TryDeserializeJson(responseBody, out TooManyRequestsErrorResponse statusErrorResponse)
                && statusErrorResponse?.Status?.Details?.Count > 0)
            {
                rateLimitMessage = string.Join(" ", statusErrorResponse.Status.Details);
            }
        }

        throw KsefRateLimitException.FromRetryAfterHeader(rateLimitMessage, retryAfterHeaderValue);
    }

    private static async Task HandleOtherErrorsAsync(HttpResponseMessage responseMessage, CancellationToken innerCancellationToken)
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

    private static bool IsJsonContent(HttpResponseMessage responseMessage)
    {
        MediaTypeHeaderValue contentTypeHeader = responseMessage.Content?.Headers?.ContentType;
        return IsJsonMediaType(contentTypeHeader?.MediaType);
    }

    private static string BuildErrorMessageFromDetails(ApiErrorResponse apiErrorResponse)
    {
        if (apiErrorResponse?.Exception?.ExceptionDetailList is not { Count: > 0 })
        {
            return string.Empty;
        }

        IEnumerable<string> parts = apiErrorResponse.Exception.ExceptionDetailList.Select(detail =>
        {
            string detailsText = (detail.Details is { Count: > 0 })
                ? string.Join("; ", detail.Details)
                : string.Empty;

            return string.IsNullOrEmpty(detailsText)
                ? $"{detail.ExceptionCode}: {detail.ExceptionDescription}"
                : $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {detailsText}";
        });

        return string.Join(" | ", parts);
    }

    private static ApiErrorResponse MapProblemDetailsToApiErrorResponse(
        string title,
        int status,
        string detail,
        string traceId = null,
        string instance = null,
        string reasonCode = null,
        object security = null)
    {
        List<string> details = new List<string>();

        AddIfNotEmpty(details, detail);
        AddIfNotEmpty(details, instance, "instance: ");
        AddIfNotEmpty(details, reasonCode, "reasonCode: ");

        if (security is not null)
        {
            try
            {
                string secJson = JsonUtil.Serialize(security);
                if (!string.IsNullOrWhiteSpace(secJson) && secJson != "null")
                {
                    details.Add($"security: {secJson}");
                }
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

    private static ApiErrorResponse MapBadRequestProblemDetailsToApiErrorResponse(BadRequestProblemDetails details)
    {
        List<ApiExceptionDetail> exceptionDetails = details.Errors
            ?.Select(e => new ApiExceptionDetail(e.Code, e.Description, e.Details))
            .ToList() ?? new List<ApiExceptionDetail>();

        return new ApiErrorResponse
        {
            Exception = new ApiExceptionContent
            {
                Timestamp = DateTime.UtcNow,
                ServiceName = ServiceNameText,
                ReferenceNumber = details.TraceId,
                ExceptionDetailList = exceptionDetails
            }
        };
    }

    private static bool TryDeserializeJson<T>(string json, out T result)
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

    private static bool TryExtractRetryAfterHeaderValue(HttpResponseMessage responseMessage, out string retryAfterHeaderValue)
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

    private static void AddIfNotEmpty(List<string> list, string value, string prefix = "")
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            list.Add(prefix + value);
        }
    }
}
