#if NETFRAMEWORK
using KSeF.Client.Tests.Compatibility;
#endif
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Http;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Tests;

/// <summary>
/// Testy jednostkowe obsługi odpowiedzi Problem Details (HTTP 400 i 429) w RestClient.
/// Weryfikują zachowanie RestClient dla obu formatów: nowego Problem Details
/// (application/problem+json) i starszego formatu application/json.
/// </summary>
public class RestClientProblemDetailsTests
{

    // ===========================
    // HTTP 400 – Problem Details
    // ===========================

    [Fact]
    public async Task SendAsync_WhenHttp400WithBadRequestProblemDetails_ThrowsKsefApiExceptionWith400()
    {
        // Arrange
        string json = @"{
            ""title"": ""Bad Request"",
            ""status"": 400,
            ""instance"": ""/v2/sessions/batch"",
            ""detail"": ""Żądanie jest nieprawidłowe."",
            ""errors"": [
                {
                    ""code"": 21405,
                    ""description"": ""Błąd walidacji danych wejściowych."",
                    ""details"": [""Wskazany kod formularza nie jest wspierany.""]
                }
            ],
            ""traceId"": ""abc123"",
            ""timestamp"": ""2026-03-27T11:51:14Z""
        }";

        RestClient client = CreateClient((HttpStatusCode)400, json);

        // Act & Assert
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        Assert.Equal((HttpStatusCode)400, ex.StatusCode);
    }

    [Fact]
    public async Task SendAsync_WhenHttp400WithBadRequestProblemDetails_ExceptionMessageContainsErrorCodeAndDescription()
    {
        // Arrange
        string json = @"{
            ""title"": ""Bad Request"",
            ""status"": 400,
            ""instance"": ""/v2/sessions/batch"",
            ""detail"": ""Żądanie jest nieprawidłowe."",
            ""errors"": [
                {
                    ""code"": 21405,
                    ""description"": ""Błąd walidacji danych wejściowych."",
                    ""details"": [""Wskazany kod formularza nie jest wspierany.""]
                }
            ],
            ""traceId"": ""abc123"",
            ""timestamp"": ""2026-03-27T11:51:14Z""
        }";

        RestClient client = CreateClient((HttpStatusCode)400, json);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert: wiadomość zawiera kod błędu i opis
        Assert.Contains("21405", ex.Message);
        Assert.Contains("Błąd walidacji danych wejściowych.", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenHttp400WithMultipleBadRequestErrors_AllErrorsMappedToExceptionDetailList()
    {
        // Arrange
        string json = @"{
            ""title"": ""Bad Request"",
            ""status"": 400,
            ""instance"": ""/v2/sessions/batch"",
            ""detail"": ""Żądanie jest nieprawidłowe."",
            ""errors"": [
                {
                    ""code"": 21405,
                    ""description"": ""Błąd walidacji danych wejściowych."",
                    ""details"": [""Wskazany kod formularza nie jest wspierany.""]
                },
                {
                    ""code"": 21157,
                    ""description"": ""Nieprawidłowy rozmiar części pakietu."",
                    ""details"": [""Rozmiar części 1 przekroczył dozwolony rozmiar 100MB.""]
                }
            ],
            ""traceId"": ""abc123"",
            ""timestamp"": ""2026-03-27T11:51:14Z""
        }";

        RestClient client = CreateClient((HttpStatusCode)400, json);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert: oba błędy zmapowane do ExceptionDetailList
        Assert.NotNull(ex.ErrorResponse);
        Assert.NotNull(ex.ErrorResponse.Exception);
        Assert.Equal(2, ex.ErrorResponse.Exception.ExceptionDetailList.Count);
        Assert.Equal(21405, ex.ErrorResponse.Exception.ExceptionDetailList[0].ExceptionCode);
        Assert.Equal(21157, ex.ErrorResponse.Exception.ExceptionDetailList[1].ExceptionCode);
        // wiadomość zawiera oba kody
        Assert.Contains("21405", ex.Message);
        Assert.Contains("21157", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenHttp400WithBadRequestProblemDetails_ErrorDetailsListIsPreserved()
    {
        // Arrange
        string json = @"{
            ""title"": ""Bad Request"",
            ""status"": 400,
            ""detail"": ""Żądanie jest nieprawidłowe."",
            ""errors"": [
                {
                    ""code"": 21405,
                    ""description"": ""Błąd walidacji."",
                    ""details"": [""Wskazany kod formularza nie jest wspierany."", ""Niepoprawny skrót.""]
                }
            ],
            ""traceId"": ""trace-xyz"",
            ""timestamp"": ""2026-03-27T11:51:14Z""
        }";

        RestClient client = CreateClient((HttpStatusCode)400, json);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert: lista details w ApiExceptionDetail jest zachowana
        ApiExceptionDetail detail = ex.ErrorResponse.Exception.ExceptionDetailList[0];
        Assert.Equal(2, detail.Details.Count);
        Assert.Contains("Wskazany kod formularza nie jest wspierany.", detail.Details);
        Assert.Contains("Niepoprawny skrót.", detail.Details);
    }

    [Fact]
    public async Task SendAsync_WhenHttp400WithBadRequestProblemDetails_TraceIdIsPreservedInReferenceNumber()
    {
        // Arrange
        string json = @"{
            ""title"": ""Bad Request"",
            ""status"": 400,
            ""detail"": ""Błąd."",
            ""errors"": [{ ""code"": 1, ""description"": ""Błąd."" }],
            ""traceId"": ""my-trace-id"",
            ""timestamp"": ""2026-03-27T11:51:14Z""
        }";

        RestClient client = CreateClient((HttpStatusCode)400, json);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert: traceId zmapowane do ReferenceNumber
        Assert.Equal("my-trace-id", ex.ErrorResponse.Exception.ReferenceNumber);
    }

    [Fact]
    public async Task SendAsync_WhenHttp400WithLegacyApiErrorResponseFormat_FallsBackAndThrowsKsefApiException()
    {
        // Arrange – format legacy (application/json)
        string json = @"{
            ""exception"": {
                ""exceptionDetailList"": [
                    {
                        ""exceptionCode"": 21405,
                        ""exceptionDescription"": ""Błąd walidacji danych wejściowych."",
                        ""details"": [""Wskazany kod formularza nie jest wspierany.""]
                    }
                ],
                ""serviceCode"": ""00-abc"",
                ""timestamp"": ""2026-03-27T13:58:40Z""
            }
        }";

        RestClient client = CreateClient((HttpStatusCode)400, json);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert: format legacy jest nadal obsługiwany
        Assert.Equal((HttpStatusCode)400, ex.StatusCode);
        Assert.Contains("21405", ex.Message);
        Assert.Contains("Błąd walidacji danych wejściowych.", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenHttp400WithEmptyBody_ThrowsKsefApiExceptionWithGenericMessage()
    {
        // Arrange
        RestClient client = CreateClient((HttpStatusCode)400, string.Empty);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert
        Assert.Equal((HttpStatusCode)400, ex.StatusCode);
    }

    // ===========================
    // HTTP 429 – Problem Details
    // ===========================

    [Fact]
    public async Task SendAsync_WhenHttp429WithTooManyRequestsProblemDetails_ThrowsKsefRateLimitException()
    {
        // Arrange – nowy format Problem Details
        string json = @"{
            ""title"": ""Too Many Requests"",
            ""status"": 429,
            ""instance"": ""/v2/invoices/exports"",
            ""detail"": ""Przekroczono limit 8 żądań na minutę. Spróbuj ponownie po 16 sekundach."",
            ""traceId"": ""9e53cdeb61d9b68c7bb65e58ba41aeaf"",
            ""timestamp"": ""2026-03-27T14:02:26Z""
        }";

        RestClient client = CreateClient((HttpStatusCode)429, json);

        // Act & Assert
        KsefRateLimitException ex = await Assert.ThrowsAsync<KsefRateLimitException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert: komunikat pochodzi z pola "detail"
        Assert.Equal("Przekroczono limit 8 żądań na minutę. Spróbuj ponownie po 16 sekundach.", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenHttp429WithTooManyRequestsProblemDetailsAndRetryAfterHeader_RetryAfterSecondsIsSet()
    {
        // Arrange
        string json = @"{
            ""title"": ""Too Many Requests"",
            ""status"": 429,
            ""detail"": ""Przekroczono limit 20 żądań na minutę. Spróbuj ponownie po 30 sekundach."",
            ""traceId"": ""abc"",
            ""timestamp"": ""2026-03-27T14:02:26Z""
        }";

        RestClient client = CreateClient((HttpStatusCode)429, json, retryAfter: "30");

        // Act
        KsefRateLimitException ex = await Assert.ThrowsAsync<KsefRateLimitException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert
        Assert.Equal(30, ex.RetryAfterSeconds);
        Assert.Equal("Przekroczono limit 20 żądań na minutę. Spróbuj ponownie po 30 sekundach.", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenHttp429WithLegacyTooManyRequestsErrorResponse_UsesDetailsField()
    {
        // Arrange – format legacy (application/json)
        string json = @"{
            ""status"": {
                ""code"": 429,
                ""description"": ""Too Many Requests"",
                ""details"": [""Przekroczono limit 8 żądań na minutę. Spróbuj ponownie po 41 sekundach.""]
            }
        }";

        RestClient client = CreateClient((HttpStatusCode)429, json);

        // Act
        KsefRateLimitException ex = await Assert.ThrowsAsync<KsefRateLimitException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert: wiadomość pochodzi z pola "status.details" (format legacy)
        Assert.Equal("Przekroczono limit 8 żądań na minutę. Spróbuj ponownie po 41 sekundach.", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenHttp429WithEmptyBody_ThrowsKsefRateLimitExceptionWithDefaultMessage()
    {
        // Arrange
        RestClient client = CreateClient((HttpStatusCode)429, string.Empty);

        // Act
        KsefRateLimitException ex = await Assert.ThrowsAsync<KsefRateLimitException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert: rzucony jest wyjątek odpowiedniego typu
        Assert.NotNull(ex.Message);
    }

    // =====================
    // Testy dla HTTP 410 Gone
    // =====================

    [Fact]
    public async Task SendAsync_WhenHttp410WithGoneProblemDetails_ThrowsKsefApiExceptionWithDetail()
    {
        // Arrange
        string problemDetailsJson = """
            {
                "title": "Gone",
                "status": 410,
                "instance": "/api/resource/123",
                "detail": "Zasób został trwale usunięty.",
                "timestamp": "2024-01-15T10:30:00Z",
                "traceId": "00-abc123-def456-01"
            }
            """;
        RestClient client = CreateClient(HttpStatusCode.Gone, problemDetailsJson);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert
        Assert.Equal(HttpStatusCode.Gone, ex.StatusCode);
        Assert.Contains("Zasób został trwale usunięty.", ex.Message);
        Assert.NotNull(ex.ErrorResponse);
        Assert.NotNull(ex.ErrorResponse.Exception);
        Assert.Equal("00-abc123-def456-01", ex.ErrorResponse.Exception.ReferenceNumber);
        Assert.Contains("Gone", ex.ErrorResponse.Exception.ExceptionDetailList[0].ExceptionDescription);
        Assert.Contains("Zasób został trwale usunięty.", ex.ErrorResponse.Exception.ExceptionDetailList[0].Details[0]);
        Assert.Contains("instance: /api/resource/123", ex.ErrorResponse.Exception.ExceptionDetailList[0].Details[1]);
    }

    [Fact]
    public async Task SendAsync_WhenHttp410WithGoneProblemDetailsAndNoDetail_UsesTitleAsMessage()
    {
        // Arrange
        string problemDetailsJson = """
            {
                "title": "Gone",
                "status": 410,
                "instance": "/api/resource/123",
                "detail": "",
                "timestamp": "2024-01-15T10:30:00Z",
                "traceId": "00-abc123-def456-01"
            }
            """;
        RestClient client = CreateClient(HttpStatusCode.Gone, problemDetailsJson);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert: gdy detail puste, wiadomość pochodzi z title
        Assert.Equal(HttpStatusCode.Gone, ex.StatusCode);
        Assert.Contains("Gone", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenHttp410WithLegacyApiErrorResponse_UsesLegacyHandler()
    {
        // Arrange
        string legacyJson = """
            {
                "exception": {
                    "serviceCtx": "test",
                    "serviceCode": "20250101-XX-111111-AA-222222",
                    "serviceName": "KSeF",
                    "timestamp": "2024-01-15T10:30:00+00:00",
                    "referenceNumber": null,
                    "exceptionDetailList": [
                        { "exceptionCode": 410, "exceptionDescription": "Zasób usunięty" }
                    ]
                }
            }
            """;
        RestClient client = CreateClient(HttpStatusCode.Gone, legacyJson);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert
        Assert.Equal(HttpStatusCode.Gone, ex.StatusCode);
        Assert.Contains("Zasób usunięty", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenHttp410WithEmptyBody_ThrowsKsefApiExceptionWithDefaultMessage()
    {
        // Arrange
        RestClient client = CreateClient(HttpStatusCode.Gone, string.Empty);

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert
        Assert.Equal(HttpStatusCode.Gone, ex.StatusCode);
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenHttp410WithNonJsonContentType_ThrowsKsefApiExceptionWithDefaultMessage()
    {
        // Arrange
        RestClient client = CreateClient(HttpStatusCode.Gone, "Gone", "text/plain");

        // Act
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            client.SendAsync<object, object>(HttpMethod.Post, "https://localhost/test", (object)null, null, "application/json", CancellationToken.None));

        // Assert
        Assert.Equal(HttpStatusCode.Gone, ex.StatusCode);
    }

    // =====================
    // Pomocnicze metody
    // =====================

    private static RestClient CreateClient(
        HttpStatusCode statusCode,
        string body,
        string contentType = "application/json",
        string retryAfter = null)
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler(statusCode, body, contentType, retryAfter);
        HttpClient http = new HttpClient(handler);
        return new RestClient(http);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;
        private readonly string _contentType;
        private readonly string _retryAfter;

        public FakeHttpMessageHandler(
            HttpStatusCode statusCode,
            string body,
            string contentType = "application/json",
            string retryAfter = null)
        {
            _statusCode = statusCode;
            _body = body;
            _contentType = contentType;
            _retryAfter = retryAfter;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage(_statusCode);

            if (!string.IsNullOrEmpty(_body))
            {
                response.Content = new StringContent(_body, Encoding.UTF8, _contentType);
            }

            if (_retryAfter != null)
            {
                response.Headers.TryAddWithoutValidation("Retry-After", _retryAfter);
            }

            return Task.FromResult(response);
        }
    }
}
