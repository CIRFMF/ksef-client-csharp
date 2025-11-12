using System;
using System.Net;

namespace KSeF.Client.Core.Exceptions
{
    
    /// <summary>
    /// Reprezentuje wyjątek ograniczenia częstotliwości (HTTP 429 Too Many Requests) z API KSeF.
    /// Zawiera informacje o ponownych próbach z nagłówka Retry-After.
    /// </summary>
    public class KsefRateLimitException : KsefApiException
    {
        /// <summary>
        /// Pobiera opóźnienie ponawiania w sekundach z nagłówka Retry-After, jeśli zostało podane.
        /// </summary>
        public int? RetryAfterSeconds { get; }
    
        /// <summary>
        /// Pobiera datę ponawiania z nagłówka Retry-After, jeśli zostało podane jako data.
        /// </summary>
        public DateTimeOffset? RetryAfterDate { get; }

        /// <summary>
        /// Pobiera zalecane opóźnienie przed następną próbą.
        /// Obliczone na podstawie nagłówka Retry-After lub domyślnie z wykładniczym wycofywaniem.
        /// </summary>
        public TimeSpan RecommendedDelay { get; }

        /// <summary>
        /// Inicjalizuje nową instancję klasy <see cref="KsefRateLimitException"/>.
        /// </summary>
        /// <param name="message">Szczegółowy komunikat wyjątku.</param>
        /// <param name="retryAfterSeconds">Opóźnienie ponawiania w sekundach z nagłówka Retry-After.</param>
        /// <param name="retryAfterDate">Data ponawiania z nagłówka Retry-After.</param>
        /// <param name="error">Opcjonalna strukturalna odpowiedź błędu z API.</param>
        public KsefRateLimitException(
            string message, 
            int? retryAfterSeconds = null, 
            DateTimeOffset? retryAfterDate = null,
            ApiErrorResponse error = null)
            : base(message,
                   (HttpStatusCode)429,
                   error)
        {
            RetryAfterSeconds = retryAfterSeconds;
            RetryAfterDate = retryAfterDate;
            RecommendedDelay = CalculateRecommendedDelay();
        }

        /// <summary>
        /// Tworzy KsefRateLimitException na podstawie wartości nagłówka Retry-After.
        /// </summary>
        /// <param name="message">Komunikat wyjątku.</param>
        /// <param name="retryAfterHeaderValue">Wartość nagłówka Retry-After.</param>
        /// <param name="error">Opcjonalna strukturalna odpowiedź błędu z API.</param>
        /// <returns>Nowa instancja KsefRateLimitException.</returns>
        public static KsefRateLimitException FromRetryAfterHeader(
            string message,
            string retryAfterHeaderValue,
            ApiErrorResponse error = null)
        {
            int? retryAfterSeconds = null;
            DateTimeOffset? retryAfterDate = null;

            if (!string.IsNullOrEmpty(retryAfterHeaderValue))
            {
                // Najpierw próba parsowania jako sekundy
                if (int.TryParse(retryAfterHeaderValue, out int seconds))
                {
                    retryAfterSeconds = seconds;
                }
                // Próba parsowania jako data HTTP
                else if (DateTimeOffset.TryParse(retryAfterHeaderValue, out DateTimeOffset date))
                {
                    retryAfterDate = date;
                }
            }

            return new KsefRateLimitException(message, retryAfterSeconds, retryAfterDate, error);
        }

        /// <summary>
        /// Oblicza zalecane opóźnienie na podstawie nagłówka Retry-After lub używa domyślnego wykładniczego wycofywania.
        /// </summary>
        /// <returns>Zalecane opóźnienie przed następną próbą.</returns>
        private TimeSpan CalculateRecommendedDelay()
        {
            // Jeśli dostępne są sekundy z Retry-After, wykorzystanie ich wartości
            if (RetryAfterSeconds.HasValue)
            {
                return TimeSpan.FromSeconds(RetryAfterSeconds.Value);
            }

            // Jeśli dostępna jest data z Retry-After, obliczenie delty
            if (RetryAfterDate.HasValue)
            {
                TimeSpan delta = RetryAfterDate.Value - DateTimeOffset.UtcNow;
                return delta > TimeSpan.Zero ? delta : TimeSpan.FromSeconds(1);
            }

            // Domyślnie: 1 sekunda dla pierwszej próby (wykładnicze wycofywanie może być zastosowane zewnętrznie)
            return TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Pobiera przyjazny użytkownikowi opis sytuacji ograniczenia częstotliwości.
        /// </summary>
        /// <returns>Opis ograniczenia częstotliwości i sugerowany czas ponawiania.</returns>
        public string GetRateLimitDescription()
        {
            if (RetryAfterSeconds.HasValue)
            {
                return $"Przekroczono limit częstotliwości. Spróbuj ponownie po {RetryAfterSeconds.Value} sekundach.";
            }

            if (RetryAfterDate.HasValue)
            {
                return $"Przekroczono limit częstotliwości. Spróbuj ponownie po {RetryAfterDate.Value:yyyy-MM-dd HH:mm:ss UTC}.";
            }

            return "Przekroczono limit częstotliwości. Rozważ implementację wykładniczego wycofywania.";
        }
    }
}