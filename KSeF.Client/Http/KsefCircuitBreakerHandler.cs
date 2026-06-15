using KSeF.Client.Core.Exceptions;
using KSeF.Client.DI;
using System.Net;

namespace KSeF.Client.Http;

/// <summary>
/// Circuit Breaker dla wywołań HTTP.
/// </summary>
public sealed class KsefCircuitBreakerHandler : DelegatingHandler
{
    private readonly KsefCircuitBreakerOptions options;
    private readonly object gate = new();
    private readonly TimeSpan breakDuration;

    private CircuitState state = CircuitState.Closed;
    private DateTimeOffset openUntilUtc = DateTimeOffset.MinValue;
    private int consecutiveFailures;
    private bool halfOpenProbeInProgress;

    /// <summary>
    /// Inicjalizuje nową instancję klasy <see cref="KsefCircuitBreakerHandler"/>.
    /// </summary>
    /// <param name="options">Opcje Circuit Breakera.</param>
    public KsefCircuitBreakerHandler(KsefCircuitBreakerOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        if (options.FailureThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "CircuitBreaker.FailureThreshold musi być większe od zera.");
        }

        if (options.BreakDurationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "CircuitBreaker.BreakDurationSeconds musi być większe od zera.");
        }

        breakDuration = TimeSpan.FromSeconds(options.BreakDurationSeconds);
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (!TryAcquirePermission(out TimeSpan retryAfter))
        {
            throw new KsefCircuitBreakerOpenException(
                $"Circuit Breaker jest otwarty. Kolejna próba możliwa za około {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))} s.",
                retryAfter);
        }

        try
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (IsTransientFailure(response.StatusCode))
            {
                OnFailure();
            }
            else
            {
                OnSuccess();
            }

            return response;
        }
        catch (Exception ex) when (ShouldCountAsFailure(ex, cancellationToken))
        {
            OnFailure();
            throw;
        }
        catch
        {
            // Zwolnienie półotwartego obwodu dla wyjątków niezaliczanych jako failure.
            ReleaseHalfOpenProbeWithoutStateChange();
            throw;
        }
    }

    private bool TryAcquirePermission(out TimeSpan retryAfter)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        lock (gate)
        {
            if (state == CircuitState.Open)
            {
                if (now < openUntilUtc)
                {
                    retryAfter = openUntilUtc - now;
                    return false;
                }

                state = CircuitState.HalfOpen;
                halfOpenProbeInProgress = false;
            }

            if (state == CircuitState.HalfOpen)
            {
                if (halfOpenProbeInProgress)
                {
                    retryAfter = breakDuration;
                    return false;
                }

                halfOpenProbeInProgress = true;
            }

            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    private void OnSuccess()
    {
        lock (gate)
        {
            consecutiveFailures = 0;
            if (state == CircuitState.HalfOpen)
            {
                state = CircuitState.Closed;
                halfOpenProbeInProgress = false;
                openUntilUtc = DateTimeOffset.MinValue;
            }
        }
    }

    private void OnFailure()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        lock (gate)
        {
            if (state == CircuitState.HalfOpen)
            {
                OpenCircuit(now);
                return;
            }

            if (state == CircuitState.Closed)
            {
                consecutiveFailures++;
                if (consecutiveFailures >= options.FailureThreshold)
                {
                    OpenCircuit(now);
                }
            }
        }
    }

    private void OpenCircuit(DateTimeOffset now)
    {
        state = CircuitState.Open;
        openUntilUtc = now.Add(breakDuration);
        consecutiveFailures = 0;
        halfOpenProbeInProgress = false;
    }

    private void ReleaseHalfOpenProbeWithoutStateChange()
    {
        lock (gate)
        {
            if (state == CircuitState.HalfOpen)
            {
                halfOpenProbeInProgress = false;
            }
        }
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode)
    {
        int numericStatus = (int)statusCode;
        return numericStatus >= 500 || statusCode == HttpStatusCode.RequestTimeout || statusCode == (HttpStatusCode)429;
    }

    private static bool ShouldCountAsFailure(Exception ex, CancellationToken cancellationToken)
    {
        if (ex is HttpRequestException)
        {
            return true;
        }

        if (ex is OperationCanceledException)
        {
            return !cancellationToken.IsCancellationRequested;
        }

        return false;
    }

    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }
}
