#nullable enable
using System.Collections.Concurrent;

using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.RateLimits;

namespace KSeF.Client.Tests.Core.Utils.RateLimit;

/// <summary>
/// Prosty wrapper do obsługi rate limiting dla KsefClient.
/// Automatycznie obsługuje KsefRateLimitException z retry wykorzystując Retry-After header.
/// </summary>
public static class KsefRateLimitWrapper
{
    private const int DefaultMaxRetryAttempts = 5;

    /// <summary>
    /// Wykonuje wywołanie KSeF API z automatyczną obsługą rate limiting.
    /// Automatycznie ponawiaj próby po HTTP 429 zgodnie z Retry-After header.
    /// </summary>
    /// <typeparam name="T">Typ zwracanej odpowiedzi</typeparam>
    /// <param name="ksefApiCall">Funkcja wykonująca wywołanie KSeF API</param>
    /// <param name="endpoint">Typ endpointu (używany do sprawdzenia limitów)</param>
    /// <param name="maxRetryAttempts">Maksymalna liczba prób</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <param name="limitsClient">Opcjonalny klient do pobierania dynamicznych limitów API</param>
    /// <param name="accessToken">Opcjonalny token dostępu używany do pobrania limitów</param>
    /// <returns>Odpowiedź z KSeF API</returns>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> ksefApiCall,
        KsefApiEndpoint endpoint,
        ILimitsClient? limitsClient = null,
        int maxRetryAttempts = DefaultMaxRetryAttempts,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        // Odczyt profilu limitów danego endpointu (RPS/RPM/RPH)
        ApiLimits limits = await ResolveApiLimitsAsync(endpoint, limitsClient, accessToken, cancellationToken).ConfigureAwait(false);

        for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            try
            {
                // Lokalny ograniczenie przepustowości przed wywołaniem API – oczekiwanie na odnowienie limitów
                await WaitForRateWindowAsync(endpoint, limits, cancellationToken).ConfigureAwait(false);

                T result = await ksefApiCall(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (KsefRateLimitException rateLimitEx)
            {
                // Ostatnia próba - rzuć dalej wyjątek
                if (attempt == maxRetryAttempts)
                {
                    throw;
                }

                // Czekaj zgodnie z Retry-After header lub użyj fallback
                await Task.Delay(rateLimitEx.RecommendedDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw; // Operacja anulowana przez użytkownika
            }
            catch (Exception)
            {
                throw;
            }
        }

        throw new InvalidOperationException($"Nieoczekiwane zakończenie pętli powtórzeń dla {endpoint}");
    }

    private static readonly ConcurrentDictionary<KsefApiEndpoint, EndpointRateTracker> Trackers = new();

    private static async Task<ApiLimits> ResolveApiLimitsAsync(
        KsefApiEndpoint endpoint,
        ILimitsClient? limitsClient,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        // Próba pobrania limitów z API jeśli dostępny klient i token dostepu.
        if (limitsClient is not null && !string.IsNullOrWhiteSpace(accessToken))
        {

            EffectiveApiRateLimits serverLimits = await limitsClient.GetRateLimitsAsync(accessToken!, cancellationToken).ConfigureAwait(false);
            EffectiveApiRateLimitValues? values = MapEndpointToValues(endpoint, serverLimits);
            if (values is not null)
            {
                return new ApiLimits
                {
                    RequestsPerSecond = values.PerSecond,
                    RequestsPerMinute = values.PerMinute,
                    RequestsPerHour = values.PerHour
                };
            }
        }

        // Fallback do statycznych limitów testowych
        return KsefApiLimits.GetLimits(endpoint);
    }

    private static EffectiveApiRateLimitValues? MapEndpointToValues(KsefApiEndpoint endpoint, EffectiveApiRateLimits limits)
        => endpoint switch
        {
            KsefApiEndpoint.InvoiceQueryMetadata => limits.InvoiceMetadata,
            KsefApiEndpoint.InvoiceExport => limits.InvoiceExport,
            KsefApiEndpoint.InvoiceGetByNumber => limits.InvoiceDownload,
            KsefApiEndpoint.SessionBatchOpen => limits.BatchSession,
            KsefApiEndpoint.SessionBatchClose => limits.BatchSession,
            KsefApiEndpoint.SessionOnlineOpen => limits.OnlineSession,
            KsefApiEndpoint.SessionOnlineSendInvoice => limits.InvoiceSend,
            KsefApiEndpoint.SessionOnlineClose => limits.OnlineSession,
            KsefApiEndpoint.SessionInvoiceStatus => limits.InvoiceStatus,
            _ => limits.Other
        };

    private static async Task WaitForRateWindowAsync(
        KsefApiEndpoint endpoint,
        ApiLimits limits,
        CancellationToken cancellationToken)
    {
        if (limits is null)
        {
            return;
        }

        // Współdzielony licznik wywołań per endpoint (bez uderzania w serwer)
        EndpointRateTracker tracker = Trackers.GetOrAdd(endpoint, _ => new EndpointRateTracker());

        while (true)
        {
            TimeSpan requiredDelay;

            await tracker.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                tracker.Trim(now);
                requiredDelay = tracker.CalculateDelay(now, limits);

                if (requiredDelay <= TimeSpan.Zero)
                {
                    // Rejestrujemy wywołanie – dalsze zapytania muszą się do niego odnieść
                    tracker.Register(now);
                    return;
                }
            }
            finally
            {
                tracker.Semaphore.Release();
            }

            if (requiredDelay > TimeSpan.Zero)
            {
                await Task.Delay(requiredDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class EndpointRateTracker
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public void Trim(DateTimeOffset now)
        {
            TrimWindow(_perSecond, now - TimeSpan.FromSeconds(1));
            TrimWindow(_perMinute, now - TimeSpan.FromMinutes(1));
            TrimWindow(_perHour, now - TimeSpan.FromHours(1));
        }

        private readonly Queue<DateTimeOffset> _perSecond = new();
        private readonly Queue<DateTimeOffset> _perMinute = new();
        private readonly Queue<DateTimeOffset> _perHour = new();

        public TimeSpan CalculateDelay(DateTimeOffset now, ApiLimits limits)
        {
            // Zbieramy potrzebne opóźnienia dla każdego progu i wybieramy najdłuższe
            List<TimeSpan> delays = new(capacity: 3);

            if (limits.RequestsPerSecond > 0 && _perSecond.Count >= limits.RequestsPerSecond)
            {
                DateTimeOffset waitUntil = _perSecond.Peek().AddSeconds(1);
                TimeSpan delay = waitUntil - now;
                if (delay > TimeSpan.Zero)
                {
                    delays.Add(delay);
                }
            }

            if (limits.RequestsPerMinute > 0 && _perMinute.Count >= limits.RequestsPerMinute)
            {
                DateTimeOffset waitUntil = _perMinute.Peek().AddMinutes(1);
                TimeSpan delay = waitUntil - now;
                if (delay > TimeSpan.Zero)
                {
                    delays.Add(delay);
                }
            }

            if (limits.RequestsPerHour > 0 && _perHour.Count >= limits.RequestsPerHour)
            {
                DateTimeOffset waitUntil = _perHour.Peek().AddHours(1);
                TimeSpan delay = waitUntil - now;
                if (delay > TimeSpan.Zero)
                {
                    delays.Add(delay);
                }
            }

            return delays.Count == 0 ? TimeSpan.Zero : delays.Max();
        }

        public void Register(DateTimeOffset timestamp)
        {
            _perSecond.Enqueue(timestamp);
            _perMinute.Enqueue(timestamp);
            _perHour.Enqueue(timestamp);
        }

        private static void TrimWindow(Queue<DateTimeOffset> queue, DateTimeOffset threshold)
        {
            while (queue.Count > 0 && queue.Peek() <= threshold)
            {
                queue.Dequeue();
            }
        }
    }
}