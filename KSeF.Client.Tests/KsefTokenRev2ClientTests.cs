#nullable enable
using KSeF.Client.Clients;
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Authorization.Rev2;
using KSeF.Client.Core.Models.Token;
using KSeF.Client.Http;
using System.Net;
using System.Text;

namespace KSeF.Client.Tests;

/// <summary>
/// Testy jednostkowe obsługi tokenów KSeF w kontrakcie Rev2:
/// POST/GET /tokens/rev2, GET /tokens/rev2/{referenceNumber} oraz POST /tokens/{referenceNumber}/approve.
/// Każdy scenariusz jest uruchamiany dla <see cref="KsefTokenClient"/> oraz <see cref="KSeFClient"/>.
/// </summary>
public class KsefTokenRev2ClientTests
{
    private const string AccessToken = "access-token";

    private const string StatusJson = """
        {
          "referenceNumber": "20251001-EC-220B0CE000-E228129563-96",
          "authorIdentifier": { "type": { "code": "Nip", "rawCode": "Nip", "displayName": "Nip" }, "value": "7762811692" },
          "contextIdentifier": { "type": { "code": "InternalId", "rawCode": "InternalId", "displayName": "Identyfikator wewnetrzny" }, "value": "5265877635-12345" },
          "description": "Wystawianie i przegladanie faktur.",
          "requestedPermissions": [
            { "code": "InvoiceRead", "rawCode": "InvoiceRead", "displayName": "Przegladanie faktur" },
            { "code": "FutureValue", "rawCode": "SomethingNew", "displayName": "Cos nowego" }
          ],
          "dateCreated": "2026-11-10T12:23:56.0154302+00:00",
          "validFrom": "2026-11-14T09:48:26.8700925+00:00",
          "validTo": "2026-11-20T09:48:26.8700925+00:00",
          "status": { "code": "NotYetActive", "rawCode": "NotYetActive", "displayName": "Token jeszcze nieaktywny" },
          "failure": { "code": "token.unknown-error", "description": "Wystapil nieznany blad", "details": ["a", "b"] }
        }
        """;

    public enum ClientKind
    {
        KsefTokenClient,
        KSeFClient
    }

    [Theory]
    [InlineData(ClientKind.KsefTokenClient)]
    [InlineData(ClientKind.KSeFClient)]
    public async Task GenerateKsefTokenRev2Async_SendsPostToRev2AndMapsResponse(ClientKind kind)
    {
        CapturingHandler handler = new(HttpStatusCode.Accepted, """{ "referenceNumber": "ref-1", "token": "tok-1" }""");
        IKsefTokenClient client = CreateClient(kind, handler);

        DateTimeOffset validFrom = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);
        DateTimeOffset validTo = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

        KsefTokenResponseRev2 response = await client.GenerateKsefTokenRev2Async(
            new KsefTokenRequestRev2
            {
                Permissions = [KsefTokenPermissionType.InvoiceRead, KsefTokenPermissionType.InvoiceWrite],
                Description = "Opis tokena",
                ValidFrom = validFrom,
                ValidTo = validTo
            },
            AccessToken);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/v2/tokens/rev2", handler.PathAndQuery);
        Assert.Equal("ref-1", response.ReferenceNumber);
        Assert.Equal("tok-1", response.Token);

        string body = handler.Body!;
        Assert.DoesNotContain("canGenerateTokens", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validFrom", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validTo", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("InvoiceRead", body);
        Assert.Contains("InvoiceWrite", body);
    }

    [Theory]
    [InlineData(ClientKind.KsefTokenClient)]
    [InlineData(ClientKind.KSeFClient)]
    public async Task GenerateKsefTokenRev2Async_WithoutValidity_DoesNotSendValidityFields(ClientKind kind)
    {
        CapturingHandler handler = new(HttpStatusCode.Accepted, """{ "referenceNumber": "ref-1", "token": "tok-1" }""");
        IKsefTokenClient client = CreateClient(kind, handler);

        await client.GenerateKsefTokenRev2Async(
            new KsefTokenRequestRev2
            {
                Permissions = [KsefTokenPermissionType.InvoiceRead],
                Description = "Opis tokena"
            },
            AccessToken);

        string body = handler.Body!;
        Assert.DoesNotContain("canGenerateTokens", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validFrom", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validTo", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ClientKind.KsefTokenClient)]
    [InlineData(ClientKind.KSeFClient)]
    public async Task QueryKsefTokensRev2Async_BuildsQueryWithAllFiltersAndContinuationHeader(ClientKind kind)
    {
        CapturingHandler handler = new(HttpStatusCode.OK, """{ "continuationToken": "next", "tokens": [] }""");
        IKsefTokenClient client = CreateClient(kind, handler);

        DateTimeOffset validFrom = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);
        DateTimeOffset validTo = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

        QueryKsefTokensResponseRev2 response = await client.QueryKsefTokensRev2Async(
            AccessToken,
            statuses: [
                KsefTokenStatusFilterRev2.Active,
                KsefTokenStatusFilterRev2.NotYetActive,
                KsefTokenStatusFilterRev2.AwaitingApproval,
                KsefTokenStatusFilterRev2.ApprovalExpired,
                KsefTokenStatusFilterRev2.Expired
            ],
            authorIdentifier: "5265877635",
            authorIdentifierType: TokenContextIdentifierType.Nip,
            description: "opis tokena",
            validFrom: validFrom,
            validTo: validTo,
            continuationToken: "abc",
            pageSize: 25);

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal(
            "/v2/tokens/rev2?status=Active&status=NotYetActive&status=AwaitingApproval&status=ApprovalExpired&status=Expired&description=opis%20tokena&authorIdentifier=5265877635&authorIdentifierType=Nip&validFrom=2026-11-14T09%3A00%3A00.0000000%2B00%3A00&validTo=2026-11-20T09%3A00%3A00.0000000%2B00%3A00&pageSize=25",
            handler.PathAndQuery);
        Assert.Equal("abc", Assert.Single(handler.Headers["x-continuation-token"]));
        Assert.Equal("next", response.ContinuationToken);
        Assert.Empty(response.Tokens);
    }

    [Theory]
    [InlineData(ClientKind.KsefTokenClient)]
    [InlineData(ClientKind.KSeFClient)]
    public async Task QueryKsefTokensRev2Async_WithoutFilters_UsesRootRoute(ClientKind kind)
    {
        CapturingHandler handler = new(HttpStatusCode.OK, """{ "tokens": [] }""");
        IKsefTokenClient client = CreateClient(kind, handler);

        await client.QueryKsefTokensRev2Async(AccessToken);

        Assert.Equal("/v2/tokens/rev2", handler.PathAndQuery);
        Assert.False(handler.Headers.ContainsKey("x-continuation-token"));
    }

    [Theory]
    [InlineData(ClientKind.KsefTokenClient)]
    [InlineData(ClientKind.KSeFClient)]
    public async Task GetKsefTokenRev2Async_MapsFullStatusResponse(ClientKind kind)
    {
        CapturingHandler handler = new(HttpStatusCode.OK, StatusJson);
        IKsefTokenClient client = CreateClient(kind, handler);

        AuthenticationKsefTokenRev2 token = await client.GetKsefTokenRev2Async("20251001-EC-220B0CE000-E228129563-96", AccessToken);

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("/v2/tokens/rev2/20251001-EC-220B0CE000-E228129563-96", handler.PathAndQuery);

        Assert.Equal("20251001-EC-220B0CE000-E228129563-96", token.ReferenceNumber);
        Assert.Equal(KsefTokenAuthorIdentifierTypeCodeRev2.Nip, token.AuthorIdentifier.Type.Code);
        Assert.Equal("7762811692", token.AuthorIdentifier.Value);
        Assert.Equal(KsefTokenContextIdentifierTypeCodeRev2.InternalId, token.ContextIdentifier.Type.Code);
        Assert.Equal("5265877635-12345", token.ContextIdentifier.Value);
        Assert.Equal(KsefTokenStatusRev2Code.NotYetActive, token.Status.Code);
        Assert.Equal("NotYetActive", token.Status.RawCode);
        Assert.Equal(new DateTime(2026, 11, 14), token.ValidFrom.UtcDateTime.Date);
        Assert.Equal(new DateTime(2026, 11, 20), token.ValidTo.UtcDateTime.Date);
        Assert.Null(token.LastUseDate);
        Assert.Equal("token.unknown-error", token.Failure.Code);
        Assert.Equal(new[] { "a", "b" }, token.Failure.Details.ToArray());
    }

    [Fact]
    public async Task GetKsefTokenRev2Async_FutureValueFromApi_KeepsRealCodeInRawCode()
    {
        CapturingHandler handler = new(HttpStatusCode.OK, StatusJson);
        IKsefTokenClient client = CreateClient(ClientKind.KsefTokenClient, handler);

        AuthenticationKsefTokenRev2 token = await client.GetKsefTokenRev2Async("ref", AccessToken);

        KsefTokenPermissionRev2[] permissions = [.. token.RequestedPermissions];
        Assert.Equal(KsefTokenPermissionCodeRev2.InvoiceRead, permissions[0].Code);
        Assert.Equal(KsefTokenPermissionCodeRev2.FutureValue, permissions[1].Code);
        Assert.Equal("SomethingNew", permissions[1].RawCode);
        Assert.Equal("Cos nowego", permissions[1].DisplayName);
    }

    [Fact]
    public async Task GetKsefTokenRev2Async_FutureValueStatusFromApi_KeepsRealCodeInRawCode()
    {
        string json = StatusJson.Replace(
            "\"status\": { \"code\": \"NotYetActive\", \"rawCode\": \"NotYetActive\"",
            "\"status\": { \"code\": \"FutureValue\", \"rawCode\": \"Paused\"");
        CapturingHandler handler = new(HttpStatusCode.OK, json);
        IKsefTokenClient client = CreateClient(ClientKind.KsefTokenClient, handler);

        AuthenticationKsefTokenRev2 token = await client.GetKsefTokenRev2Async("ref", AccessToken);

        Assert.Equal(KsefTokenStatusRev2Code.FutureValue, token.Status.Code);
        Assert.Equal("Paused", token.Status.RawCode);
    }

    [Theory]
    [InlineData(ClientKind.KsefTokenClient)]
    [InlineData(ClientKind.KSeFClient)]
    public async Task ApproveKsefTokenAsync_SendsPostToApproveRoute(ClientKind kind)
    {
        CapturingHandler handler = new(HttpStatusCode.NoContent, body: null);
        IKsefTokenClient client = CreateClient(kind, handler);

        await client.ApproveKsefTokenAsync("20251001-EC-220B0CE000-E228129563-96", AccessToken);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/v2/tokens/20251001-EC-220B0CE000-E228129563-96/approve", handler.PathAndQuery);
    }

    [Theory]
    [InlineData(ClientKind.KsefTokenClient)]
    [InlineData(ClientKind.KSeFClient)]
    public async Task Rev2Methods_WithMissingArguments_ThrowArgumentException(ClientKind kind)
    {
        IKsefTokenClient client = CreateClient(kind, new CapturingHandler(HttpStatusCode.OK, "{}"));

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.GenerateKsefTokenRev2Async(null!, AccessToken));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.GenerateKsefTokenRev2Async(new KsefTokenRequestRev2(), " "));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.GetKsefTokenRev2Async("", AccessToken));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.QueryKsefTokensRev2Async(""));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.ApproveKsefTokenAsync(" ", AccessToken));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.ApproveKsefTokenAsync("ref", ""));
    }

    [Theory]
    [InlineData(ClientKind.KsefTokenClient)]
    [InlineData(ClientKind.KSeFClient)]
    public async Task GetKsefTokenRev2Async_WhenRevisionIsSunset_ThrowsKsefApiExceptionWithGone(ClientKind kind)
    {
        // Po zakończeniu okresu wycofywania rewizji operacji jej dotychczasowy adres zwraca HTTP 410 Gone.
        CapturingHandler handler = new(HttpStatusCode.Gone, """{ "title": "Gone", "status": 410, "detail": "Rewizja operacji została wyłączona." }""");
        IKsefTokenClient client = CreateClient(kind, handler);

        KsefApiException exception = await Assert.ThrowsAsync<KsefApiException>(() => client.GetKsefTokenRev2Async("ref", AccessToken));

        Assert.Equal(HttpStatusCode.Gone, exception.StatusCode);
    }

    private static IKsefTokenClient CreateClient(ClientKind kind, CapturingHandler handler)
    {
        HttpClient http = new(handler) { BaseAddress = new Uri("https://localhost") };
        RestClient restClient = new(http);

        return kind switch
        {
            ClientKind.KsefTokenClient => new KsefTokenClient(restClient, new RouteBuilder("v2")),
            _ => new KSeFClient(restClient)
        };
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode, string? body) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? PathAndQuery { get; private set; }
        public string? Body { get; private set; }
        public Dictionary<string, List<string>> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            PathAndQuery = request.RequestUri!.PathAndQuery;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                Headers[header.Key] = [.. header.Value];
            }

            HttpResponseMessage response = new(statusCode);
            if (body is not null)
            {
                response.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            return response;
        }
    }
}
