#nullable enable
#if NETFRAMEWORK
namespace KSeF.Client.Tests.Utils.Compatibility;

/// <summary>
/// Polyfill dla Random.NextInt64 niedostępnego na .NET Framework 4.8.
/// </summary>
internal static class RandomExtensions
{
    /// <summary>
    /// Generuje losową liczbę long w przedziale [minValue, maxValue).
    /// </summary>
    public static long NextInt64(this Random random, long minValue, long maxValue)
    {
        if (minValue >= maxValue)
            throw new ArgumentOutOfRangeException(nameof(minValue), "minValue musi być mniejsze od maxValue.");

        ulong range = (ulong)(maxValue - minValue);
        ulong limit = ulong.MaxValue - (ulong.MaxValue % range);
        ulong result;
        byte[] buf = new byte[8];
        do
        {
            random.NextBytes(buf);
            result = BitConverter.ToUInt64(buf, 0);
        } while (result >= limit);

        return (long)(result % range) + minValue;
    }
}
#endif
