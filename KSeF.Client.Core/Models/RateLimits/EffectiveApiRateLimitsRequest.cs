namespace KSeF.Client.Core.Models.RateLimits
{
    /// <summary>
    /// Żądanie ustawienia limitów żądań API dla bieżącego kontekstu.
    /// </summary>
    public class EffectiveApiRateLimitsRequest
    {
        /// <summary>
        /// Nadpisywalne limity żądań API.
        /// </summary>
        public ApiRateLimitsChangeRequest RateLimits { get; set; }
    }
}
