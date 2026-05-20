
namespace KSeF.Client.Tests.Core.E2E.Limits
{
    /// <summary>
    /// Struktura przechowująca maksymalne dopuszczalne wartości limitów danej kategorii
    /// (na sekundę, na minutę, na godzinę).
    /// </summary>
    internal readonly struct RateMax
    {
        public RateMax(int perSecond, int perMinute, int perHour)
        {
            PerSecond = perSecond;
            PerMinute = perMinute;
            PerHour = perHour;
        }
        public int PerSecond { get; }
        public int PerMinute { get; }
        public int PerHour { get; }
    }
}
