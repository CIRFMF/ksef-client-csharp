using KSeF.Client.Core.Exceptions;
using KSeF.Client.DI;
using KSeF.Client.Http;
using System.Net;

namespace KSeF.Client.Tests;

public class KsefCircuitBreakerHandlerTests
{
    private const string TestKsefUrl = KsefEnvironmentsUris.TEST;

    [Fact]
    public async Task SendAsync_WhenConsecutiveTransientFailuresReachThreshold_OpensCircuitAndShortCircuitsNextCall()
    {
        SequenceHandler innerHandler = new(
            HttpStatusCode.InternalServerError,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.OK);

        KsefCircuitBreakerHandler breaker = new(new KsefCircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            BreakDurationSeconds = 1
        })
        {
            InnerHandler = innerHandler
        };

        using HttpClient httpClient = new(breaker);

        HttpResponseMessage first = await httpClient.GetAsync(TestKsefUrl);
        HttpResponseMessage second = await httpClient.GetAsync(TestKsefUrl);

        Assert.Equal(HttpStatusCode.InternalServerError, first.StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, second.StatusCode);

        await Assert.ThrowsAsync<KsefCircuitBreakerOpenException>(() => httpClient.GetAsync(TestKsefUrl));

        Assert.Equal(2, innerHandler.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenOpenDurationPasses_AllowsProbeAndClosesOnSuccess()
    {
        SequenceHandler innerHandler = new(
            HttpStatusCode.InternalServerError,
            HttpStatusCode.OK,
            HttpStatusCode.OK);

        KsefCircuitBreakerHandler breaker = new(new KsefCircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 1,
            BreakDurationSeconds = 1
        })
        {
            InnerHandler = innerHandler
        };

        using HttpClient httpClient = new(breaker);

        HttpResponseMessage first = await httpClient.GetAsync(TestKsefUrl);
        Assert.Equal(HttpStatusCode.InternalServerError, first.StatusCode);

        await Assert.ThrowsAsync<KsefCircuitBreakerOpenException>(() => httpClient.GetAsync(TestKsefUrl));

        await Task.Delay(TimeSpan.FromMilliseconds(1200));

        HttpResponseMessage probe = await httpClient.GetAsync(TestKsefUrl);
        Assert.Equal(HttpStatusCode.OK, probe.StatusCode);

        HttpResponseMessage next = await httpClient.GetAsync(TestKsefUrl);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);

        Assert.Equal(3, innerHandler.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenResponseIsNonTransient4xx_DoesNotCountAsFailure()
    {
        SequenceHandler innerHandler = new(
            HttpStatusCode.BadRequest,
            HttpStatusCode.BadRequest,
            HttpStatusCode.OK);

        KsefCircuitBreakerHandler breaker = new(new KsefCircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 1,
            BreakDurationSeconds = 1
        })
        {
            InnerHandler = innerHandler
        };

        using HttpClient httpClient = new(breaker);

        HttpResponseMessage first = await httpClient.GetAsync(TestKsefUrl);
        HttpResponseMessage second = await httpClient.GetAsync(TestKsefUrl);
        HttpResponseMessage third = await httpClient.GetAsync(TestKsefUrl);

        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal(HttpStatusCode.OK, third.StatusCode);
        Assert.Equal(3, innerHandler.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenResponseIs429_CountsAsTransientFailureAndOpensCircuit()
    {
        SequenceHandler innerHandler = new(
            (HttpStatusCode)429,
            HttpStatusCode.OK);

        KsefCircuitBreakerHandler breaker = new(new KsefCircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 1,
            BreakDurationSeconds = 1
        })
        {
            InnerHandler = innerHandler
        };

        using HttpClient httpClient = new(breaker);

        HttpResponseMessage first = await httpClient.GetAsync(TestKsefUrl);
        Assert.Equal((HttpStatusCode)429, first.StatusCode);

        await Assert.ThrowsAsync<KsefCircuitBreakerOpenException>(() => httpClient.GetAsync(TestKsefUrl));
        Assert.Equal(1, innerHandler.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenRequestIsCancelledByCaller_DoesNotOpenCircuit()
    {
        DelayedHandler innerHandler = new(TimeSpan.FromMilliseconds(200));
        KsefCircuitBreakerHandler breaker = new(new KsefCircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 1,
            BreakDurationSeconds = 1
        })
        {
            InnerHandler = innerHandler
        };

        using HttpClient httpClient = new(breaker);

        using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => httpClient.GetAsync(TestKsefUrl, cts.Token));

        // Gdyby anulowanie było liczone jako failure przy progu=1, tutaj poleciałby open-circuit.
        HttpResponseMessage next = await httpClient.GetAsync(TestKsefUrl);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
    }

    private sealed class SequenceHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> statuses = new(statuses);

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            HttpStatusCode statusCode = statuses.Count > 0
                ? statuses.Dequeue()
                : HttpStatusCode.OK;

            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class DelayedHandler(TimeSpan delay) : HttpMessageHandler
    {
        private readonly TimeSpan delay = delay;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
