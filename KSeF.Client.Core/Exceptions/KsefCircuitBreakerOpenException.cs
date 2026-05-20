using System;

namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Wyjątek zgłaszany, gdy lokalny Circuit Breaker jest otwarty
    /// i żądanie HTTP zostało zablokowane (fail-fast).
    /// </summary>
    public sealed class KsefCircuitBreakerOpenException : Exception
    {
        /// <summary>
        /// Szacowany czas do kolejnej próby półotwartego obwodu.
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>
        /// Inicjalizuje nową instancję klasy <see cref="KsefCircuitBreakerOpenException"/>.
        /// </summary>
        /// <param name="message">Szczegóły wyjątku.</param>
        /// <param name="retryAfter">Szacowany czas ponownej próby.</param>
        public KsefCircuitBreakerOpenException(string message, TimeSpan? retryAfter = null)
            : base(message)
        {
            RetryAfter = retryAfter;
        }
    }
}
